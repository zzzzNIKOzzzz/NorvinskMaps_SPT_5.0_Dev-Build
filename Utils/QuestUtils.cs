using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Comfort.Common;
using NorvinskMaps.Data;
using EFT;
using EFT.Interactive;
using EFT.Quests;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

namespace NorvinskMaps.Utils
{
    public static class QuestUtils
    {
        private const string _questCategory = "Quest";
        private const string _questImagePath = "Markers/quest.png";
        private static Vector2 _questPivot = new Vector2(0.5f, 0f);
        private static Color _questColor = Color.green;

        public static List<TriggerWithId> TriggersWithIds;
        public static List<LootItem> QuestItems;
        private const float ZonePositionCacheTtl = 10f;
        private static Dictionary<string, List<Vector3>> _zonePositionCache;
        private static float _zonePositionCacheBuiltAt = float.NegativeInfinity;
        private static readonly HashSet<string> _warnedMissingZone = new HashSet<string>();
        private static readonly HashSet<string> _dumpedFieldSets = new HashSet<string>();
        // Кэш reflection для поля/свойства статуса квеста — ищем один раз за процесс
        private static FieldInfo _cachedQuestStatusField;
        private static PropertyInfo _cachedQuestStatusProp;
        private static bool _questStatusLookupTried = false;
        private static bool IsLikelyMongoId(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length != 24) return false;
            foreach (char c in s)
            {
                bool isHex = (c >= '0' && c <= '9')
                          || (c >= 'a' && c <= 'f')
                          || (c >= 'A' && c <= 'F');
                if (!isHex) return false;
            }
            return true;
        }

        private static bool TryGetQuestStatus(Quest quest, out EQuestStatus status)
        {
            status = default(EQuestStatus);

            if (!_questStatusLookupTried)
            {
                _questStatusLookupTried = true;
                var type = quest.GetType();

                foreach (var name in new[] { "Status", "QuestStatus", "_status", "state", "_state" })
                {
                    try
                    {
                        var prop = type.GetProperty(name,
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (prop != null && prop.PropertyType == typeof(EQuestStatus))
                        {
                            _cachedQuestStatusProp = prop;
                            break;
                        }
                    }
                    catch { }

                    try
                    {
                        var field = type.GetField(name,
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (field != null && field.FieldType == typeof(EQuestStatus))
                        {
                            _cachedQuestStatusField = field;
                            break;
                        }
                    }
                    catch { }
                }
            }

            if (_cachedQuestStatusProp != null)
            {
                try
                {
                    status = (EQuestStatus)_cachedQuestStatusProp.GetValue(quest);
                    return true;
                }
                catch { }
            }

            if (_cachedQuestStatusField != null)
            {
                try
                {
                    status = (EQuestStatus)_cachedQuestStatusField.GetValue(quest);
                    return true;
                }
                catch { }
            }

            return false;
        }

        internal static void TryCaptureQuestData()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null) return;

            // Пересобираем триггеры, если их нет ИЛИ они пустые
            // (бывает, что при первом вызове локация ещё не загрузилась)
            if (TriggersWithIds == null || TriggersWithIds.Count == 0)
            {
                TriggersWithIds = new List<TriggerWithId>();

                try
                {
                    var all = LocationScene.GetAllObjects<TriggerWithId>();
                    if (all != null)
                        foreach (var t in all)
                            if (t != null) TriggersWithIds.Add(t);
                }
                catch (Exception e)
                {
                    NorvinskMapsPlugin.Log.LogWarning(
                        $"[QuestUtils] GetAllObjects<TriggerWithId> threw: {e.Message}");
                }

                if (TriggersWithIds.Count == 0)
                {
                    try
                    {
                        var objs = UnityEngine.Object.FindObjectsOfType<TriggerWithId>();
                        if (objs != null)
                        {
                            for (int i = 0; i < objs.Length; i++)
                            {
                                var o = objs[i];
                                if (o == null) continue;
                                TriggersWithIds.Add(o);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        NorvinskMapsPlugin.Log.LogWarning(
                            $"[QuestUtils] FindObjectsOfType<TriggerWithId> threw: {e.Message}");
                    }
                }

                _zonePositionCache = null;
                _zonePositionCacheBuiltAt = float.NegativeInfinity;

                NorvinskMapsPlugin.Log.LogDebug(
                    $"[QuestUtils] Triggers captured: {TriggersWithIds.Count}");
            }

            if (QuestItems == null || QuestItems.Count == 0)
            {
                try
                {
                    var iteration = Traverse.Create(gameWorld)
                        .Field("LootItems")
                        .Field("_iteration")
                        .GetValue<List<LootItem>>();

                    QuestItems = iteration?.Where(i => i.Item.QuestItem).ToList()
                              ?? new List<LootItem>();

                    NorvinskMapsPlugin.Log.LogDebug(
                        $"[QuestUtils] QuestItems captured: {QuestItems.Count}");
                }
                catch (Exception e)
                {
                    NorvinskMapsPlugin.Log.LogWarning(
                        $"[QuestUtils] LootItems capture: {e.Message}");
                    if (QuestItems == null) QuestItems = new List<LootItem>();
                }
            }
        }

        internal static void DiscardQuestData()
        {
            if (TriggersWithIds != null) { TriggersWithIds.Clear(); TriggersWithIds = null; }
            if (QuestItems != null) { QuestItems.Clear(); QuestItems = null; }

            _zonePositionCache = null;
            _zonePositionCacheBuiltAt = float.NegativeInfinity;
            _warnedMissingZone.Clear();
            _dumpedFieldSets.Clear();
        }

        private static void RebuildZonePositionCacheIfNeeded()
        {
            if (_zonePositionCache != null &&
                Time.realtimeSinceStartup - _zonePositionCacheBuiltAt < ZonePositionCacheTtl)
                return;

            var cache = new Dictionary<string, List<Vector3>>(StringComparer.Ordinal);

            if (TriggersWithIds != null)
            {
                foreach (var t in TriggersWithIds)
                {
                    if (t == null) continue;

                    string id;
                    try { id = t.Id; }
                    catch { continue; }
                    if (string.IsNullOrEmpty(id)) continue;

                    Vector3 pos;
                    try { pos = MathUtils.ConvertToMapPosition(t.transform.position); }
                    catch { continue; }

                    if (!cache.TryGetValue(id, out var list))
                    {
                        list = new List<Vector3>(1);
                        cache[id] = list;
                    }
                    list.Add(pos);
                }
            }

            _zonePositionCache = cache;
            _zonePositionCacheBuiltAt = Time.realtimeSinceStartup;
        }

        internal static IEnumerable<MapMarkerDef> GetMarkerDefsForPlayer(Player player)
        {
            NorvinskMapsPlugin.Log.LogDebug(
                $"[QuestUtils] GetMarkerDefsForPlayer: " +
                $"triggers={(TriggersWithIds?.Count ?? -1)}, " +
                $"items={(QuestItems?.Count ?? -1)}");

            if (TriggersWithIds == null || QuestItems == null)
            {
                NorvinskMapsPlugin.Log.LogWarning(
                    $"TriggersWithIds null: {TriggersWithIds == null} or QuestItems null: {QuestItems == null}");
                return System.Linq.Enumerable.Empty<MapMarkerDef>();
            }

            var markers = new List<MapMarkerDef>();
            var quests = GetIncompleteQuests(player);
            foreach (var quest in quests)
            {
                markers.AddRange(GetMarkerDefsForQuest(player, quest));
            }

            return markers;
        }

        internal static IEnumerable<MapMarkerDef> GetMarkerDefsForQuest(Player player, Quest quest)
        {
            var markers = new List<MapMarkerDef>();
            var conditions = GetIncompleteQuestConditions(player, quest);

            // ==========================================
            // 1. ПОИСК ИМЕНИ КВЕСТА
            // ==========================================
            string questName = null;
            try
            {
                // Пробуем парсер (quests.json / ru.json / mainQuestNotes.json)
                questName = QuestLocaleLoader.GetQuestName(quest.Id.ToString());

                // Если нет — пробуем стандартную локализацию
                if (string.IsNullOrEmpty(questName))
                {
                    var key = quest.Template.NameLocaleKey;
                    if (key != null)
                    {
                        var bsgl = key.BSGLocalized();
                        if (!string.IsNullOrEmpty(bsgl) && !IsLikelyMongoId(bsgl))
                            questName = bsgl;
                    }
                }

                // Если всё еще пусто — вытаскиваем оригинальное имя из памяти IL2CPP
                if (string.IsNullOrEmpty(questName) || IsLikelyMongoId(questName))
                {
                    var nameProp = quest.Template.GetType().GetProperty("Name");
                    if (nameProp != null)
                    {
                        var val = nameProp.GetValue(quest.Template)?.ToString();
                        if (!string.IsNullOrEmpty(val) && !IsLikelyMongoId(val))
                            questName = val;
                    }
                }
            }
            catch { }

            // Если совсем ничего не нашли — пишем "Сюжетный квест"
            if (string.IsNullOrEmpty(questName) || IsLikelyMongoId(questName))
            {
                questName = "Сюжетный квест";
            }

            // ==========================================
            // 2. ПОИСК КООРДИНАТ И СОЗДАНИЕ МАРКЕРОВ
            // ==========================================
            // ВОТ ЭТОГО БЛОКА У ТЕБЯ НЕ ХВАТАЛО:
            foreach (var condition in conditions)
            {
                var conditionDescription = condition.id.BSGLocalized();

                // Получаем координаты для текущего условия (убийство, посещение зоны, предмет и т.д.)
                var positions = GetPositionsForCondition(condition, questName, conditionDescription);

                foreach (var position in positions)
                {
                    var isDuplicate = false;

                    // Проверка на дубликаты (чтобы маркеры не накладывались друг на друга)
                    foreach (var marker in markers)
                    {
                        if (MathUtils.ApproxEquals(marker.Position.x, position.x)
                         && MathUtils.ApproxEquals(marker.Position.y, position.y)
                         && MathUtils.ApproxEquals(marker.Position.z, position.z))
                        {
                            isDuplicate = true;
                            break;
                        }
                    }

                    if (isDuplicate)
                    {
                        continue;
                    }

                    // Создаем и добавляем маркер
                    markers.Add(CreateQuestMapMarkerDef(position, questName, conditionDescription));
                }
            }

            return markers;
        }

        private static IEnumerable<Vector3> GetPositionsForCondition(Condition condition, string questName,
                                                                     string conditionDescription)
        {
            if (condition == null) yield break;

            var typeName = Il2CppTypeHelper.GetClassName(condition);

            switch (typeName)
            {
                case "ConditionCounterCreator":
                    {
                        var counter = new ConditionCounterCreator(condition.Pointer);
                        foreach (var pos in GetPositionsForConditionCreator(counter, questName, conditionDescription))
                            yield return pos;
                        yield break;
                    }

                case "ConditionZone":
                    {
                        var zoneCondition = new ConditionZone(condition.Pointer);
                        var zoneIds = zoneCondition.zoneIds;
                        if (zoneIds != null)
                        {
                            foreach (var zoneId in zoneIds)
                            {
                                var idStr = ExtractMongoIdString(zoneId);
                                if (string.IsNullOrEmpty(idStr)) continue;
                                foreach (var pos in GetPositionsForZoneId(idStr, questName, conditionDescription))
                                    yield return pos;
                            }
                        }
                        yield break;
                    }

                case "ConditionLaunchFlare":
                    {
                        var flare = new ConditionLaunchFlare(condition.Pointer);
                        var zoneId = flare.zoneID;
                        if (!string.IsNullOrEmpty(zoneId))
                        {
                            foreach (var pos in GetPositionsForZoneId(zoneId, questName, conditionDescription))
                                yield return pos;
                        }
                        yield break;
                    }

                case "ConditionVisitPlace":
                    {
                        var place = new ConditionVisitPlace(condition.Pointer);
                        var zoneId = ExtractMongoIdString(place.target);
                        if (!string.IsNullOrEmpty(zoneId))
                        {
                            foreach (var pos in GetPositionsForZoneId(zoneId, questName, conditionDescription))
                                yield return pos;
                        }
                        else
                        {
                            WarnMissingZone(typeName, condition);
                        }
                        yield break;
                    }

                case "ConditionInZone":
                    {
                        var inZone = new ConditionInZone(condition.Pointer);
                        var zoneIds = inZone.zoneIds;
                        if (zoneIds != null)
                        {
                            foreach (var zoneId in zoneIds)
                            {
                                var idStr = ExtractMongoIdString(zoneId);
                                if (string.IsNullOrEmpty(idStr)) continue;
                                foreach (var pos in GetPositionsForZoneId(idStr, questName, conditionDescription))
                                    yield return pos;
                            }
                        }
                        yield break;
                    }

                case "ConditionFindItem":
                    {
                        var findItem = new ConditionFindItem(condition.Pointer);
                        var targets = findItem.target;
                        if (targets != null)
                        {
                            foreach (var pos in GetPositionsForQuestItems(targets, questName, conditionDescription))
                                yield return pos;
                        }
                        yield break;
                    }

                case "ConditionExitName":
                    {
                        var exitName = new ConditionExitName(condition.Pointer);
                        var exfils = Singleton<GameWorld>.Instance?.ExfiltrationController?.ExfiltrationPoints;
                        if (exfils != null)
                        {
                            var specifiedExit = exfils.FirstOrDefault(e => e.Settings.Name == exitName.exitName);
                            if (specifiedExit != null)
                                yield return MathUtils.ConvertToMapPosition(specifiedExit.transform);
                        }
                        yield break;
                    }

                case "ConditionLocation":
                    {
                        yield break;
                    }

                case "ConditionPlaceBeacon":
                case "ConditionLeaveItemAtLocation":
                    {
                        var zoneIds = GetZoneIdsViaConditionZoneWrapper(condition);
                        if (zoneIds != null && zoneIds.Count > 0)
                        {
                            foreach (var zid in zoneIds)
                            {
                                if (string.IsNullOrEmpty(zid)) continue;
                                foreach (var pos in GetPositionsForZoneId(zid, questName, conditionDescription))
                                    yield return pos;
                            }
                        }
                        yield break;
                    }

                default:
                    yield break;
            }
        }

        private static List<string> GetZoneIdsViaConditionZoneWrapper(Il2CppObjectBase condition)
        {
            if (condition == null || condition.Pointer == IntPtr.Zero) return null;

            try
            {
                var asZone = new ConditionZone(condition.Pointer);
                var zids = asZone.zoneIds;
                if (zids == null) return null;

                var result = new List<string>();
                try
                {
                    foreach (var item in zids)
                    {
                        if (item == null) continue;
                        var s = item.ToString();
                        if (!string.IsNullOrEmpty(s)) result.Add(s);
                    }
                }
                catch
                {
                    try
                    {
                        int count = zids.Count;
                        for (int i = 0; i < count; i++)
                        {
                            var item = zids[i];
                            if (item == null) continue;
                            var s = item.ToString();
                            if (!string.IsNullOrEmpty(s)) result.Add(s);
                        }
                    }
                    catch { }
                }

                return result;
            }
            catch
            {
                return null;
            }
        }

        private static IEnumerable<Vector3> GetPositionsForConditionCreator(ConditionCounterCreator conditionCreator,
                                                                            string questName, string conditionDescription)
        {
            var counter = conditionCreator._templateConditions;
            if (counter == null)
            {
                yield break;
            }

            var conds = counter.Conditions;
            if (conds == null)
            {
                yield break;
            }

            int innerCount = conds.Count;

            for (int i = 0; i < innerCount; i++)
            {
                Condition condition = null;
                try { condition = conds[i]; }
                catch (Exception e)
                {
                    NorvinskMapsPlugin.Log.LogError($"[CC] {questName} conds[{i}] threw: {e.Message}");
                    continue;
                }

                if (condition == null) continue;

                foreach (var pos in GetPositionsForCondition(condition, questName, conditionDescription))
                    yield return pos;
            }
        }

        private static IEnumerable<Vector3> GetPositionsForZoneId(string zoneId, string questName,
                                                                  string conditionDescription)
        {
            RebuildZonePositionCacheIfNeeded();

            if (_zonePositionCache == null) yield break;
            if (!_zonePositionCache.TryGetValue(zoneId, out var list) || list == null) yield break;

            for (int i = 0; i < list.Count; i++)
                yield return list[i];
        }

        private static IEnumerable<Vector3> GetPositionsForQuestItems(IEnumerable<string> questItemIds, string questName,
                                                                      string conditionDescription)
        {
            if (questItemIds == null) yield break;
            foreach (var questItemId in questItemIds)
            {
                var questItems = QuestItems?.Where(i => i.TemplateId == questItemId);
                if (questItems == null) continue;
                foreach (var item in questItems)
                    yield return MathUtils.ConvertToMapPosition(item.transform.position);
            }
        }

        private static IEnumerable<Condition> GetIncompleteQuestConditions(Player player, Quest quest)
        {
            if (quest?.Template?.Conditions == null)
                yield break;

            EFT.Quests.ConditionCollection conditions = null;
            if (!quest.Template.Conditions.TryGetValue(EQuestStatus.Started, out conditions) || conditions == null)
            {
                if (!quest.Template.Conditions.TryGetValue(EQuestStatus.AvailableForFinish, out conditions) || conditions == null)
                    yield break;
            }

            int collCount = conditions.Count;
            for (int i = 0; i < collCount; i++)
            {
                Condition condition = null;
                try { condition = conditions[i]; }
                catch (Exception e)
                {
                    NorvinskMapsPlugin.Log.LogError($"[QCond] {quest.Id} conditions[{i}] threw: {e.Message}");
                    continue;
                }

                if (condition == null) continue;

                bool completed = IsConditionCompleted(player, quest, condition);

                if (completed) continue;
                yield return condition;
            }
        }

        private static IEnumerable<Quest> GetIncompleteQuests(Player player)
        {
            var questController = player.QuestController;
            if (questController == null)
            {
                NorvinskMapsPlugin.Log.LogWarning("[QuestUtils] QuestController null");
                yield break;
            }

            var questBook = questController.Quests;
            if (questBook == null)
            {
                NorvinskMapsPlugin.Log.LogWarning("[QuestUtils] Quests null");
                yield break;
            }

            int count = 0;
            try { count = questBook.Count; } catch { }

            NorvinskMapsPlugin.Log.LogDebug($"[QuestUtils] total quests: {count}");

            int startedCount = 0;

            for (int i = 0; i < count; i++)
            {
                Quest quest = null;
                try { quest = questBook[i]; }
                catch (Exception e)
                {
                    NorvinskMapsPlugin.Log.LogError($"[QuestUtils] questBook[{i}] threw: {e.Message}");
                    continue;
                }

                if (quest == null) continue;
                if (quest.Template?.Conditions == null) continue;
                if (TryGetQuestStatus(quest, out var status))
                {
                    // Started и AvailableForFinish — оба должны давать маркеры.
                    // Story quests (основная сюжетная линия) часто висят в AvailableForFinish.
                    if (status != EQuestStatus.Started && status != EQuestStatus.AvailableForFinish)
                        continue;
                }
                else
                {
                    var tpl = quest.Template.Conditions;
                    if (!tpl.ContainsKey(EQuestStatus.Started) &&
                        !tpl.ContainsKey(EQuestStatus.AvailableForFinish))
                        continue;
                }

                startedCount++;
                yield return quest;
            }
        }

        private static bool IsConditionCompleted(Player player, Quest questData, Condition condition)
        {
            var questController = player.QuestController;
            if (questController == null) return false;

            var questBook = questController.Quests;
            if (questBook == null) return false;

            var quest = questBook.GetConditional(questData.Id);
            if (quest == null) return false;

            return quest.IsConditionDone(condition);
        }

        private static MapMarkerDef CreateQuestMapMarkerDef(Vector3 position, string questName, string conditionDescription)
        {
            return new MapMarkerDef
            {
                Category = _questCategory,
                Color = _questColor,
                ImagePath = _questImagePath,
                Position = position,
                Pivot = _questPivot,
                Text = questName
            };
        }

        private static string ReadZoneIdNative(Il2CppObjectBase obj, params string[] fieldNames)
        {
            if (obj == null || fieldNames == null) return null;
            var ptr = obj.Pointer;
            if (ptr == IntPtr.Zero) return null;

            var klass = IL2CPP.il2cpp_object_get_class(ptr);
            if (klass == IntPtr.Zero) return null;

            var wanted = new HashSet<string>(fieldNames, StringComparer.Ordinal);

            var currentKlass = klass;
            int depth = 0;
            while (currentKlass != IntPtr.Zero && depth < 16)
            {
                var iter = IntPtr.Zero;
                IntPtr fieldPtr;
                while ((fieldPtr = IL2CPP.il2cpp_class_get_fields(currentKlass, ref iter)) != IntPtr.Zero)
                {
                    var namePtr = IL2CPP.il2cpp_field_get_name(fieldPtr);
                    if (namePtr == IntPtr.Zero) continue;

                    var fname = Marshal.PtrToStringAnsi(namePtr);
                    if (string.IsNullOrEmpty(fname)) continue;
                    if (!wanted.Contains(fname)) continue;

                    var s = ReadFieldAsStringNative(ptr, fieldPtr);
                    if (!string.IsNullOrEmpty(s)) return s;
                }

                currentKlass = IL2CPP.il2cpp_class_get_parent(currentKlass);
                depth++;
            }

            return null;
        }

        private static string ReadFieldAsStringNative(IntPtr objPtr, IntPtr fieldPtr)
        {
            try
            {
                var offsetRaw = IL2CPP.il2cpp_field_get_offset(fieldPtr);
                long offsetLong = Convert.ToInt64(offsetRaw);
                var fieldAddr = new IntPtr(objPtr.ToInt64() + offsetLong);

                var fieldPtrValue = Marshal.ReadIntPtr(fieldAddr);

                string fieldClass = "-";
                if (fieldPtrValue != IntPtr.Zero)
                {
                    try
                    {
                        var klass = IL2CPP.il2cpp_object_get_class(fieldPtrValue);
                        if (klass != IntPtr.Zero)
                        {
                            var np = IL2CPP.il2cpp_class_get_name(klass);
                            if (np != IntPtr.Zero) fieldClass = Marshal.PtrToStringAnsi(np);
                        }
                    }
                    catch { }
                }

                if (fieldClass == "String")
                {
                    try
                    {
                        var s = IL2CPP.Il2CppStringToManaged(fieldPtrValue);
                        if (IsValidMongoId(s)) return s;
                    }
                    catch { }
                }

                if (fieldClass.EndsWith("[]"))
                {
                    foreach (var s in ReadStringArrayElements(fieldPtrValue))
                    {
                        if (IsValidMongoId(s)) return s;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static IEnumerable<string> ReadStringArrayElements(IntPtr arrayPtr)
        {
            if (arrayPtr == IntPtr.Zero) yield break;

            long len = 0;
            try { len = Marshal.ReadIntPtr(arrayPtr, 0x18).ToInt64(); }
            catch { yield break; }

            if (len <= 0 || len > 64) yield break;

            const int vecOffset = 0x20;

            for (long i = 0; i < len; i++)
            {
                IntPtr elemPtr = IntPtr.Zero;
                try { elemPtr = Marshal.ReadIntPtr(arrayPtr, vecOffset + (int)(i * 8)); }
                catch { continue; }

                if (elemPtr.ToInt64() < 0x10000) continue;

                string s = null;
                try { s = IL2CPP.Il2CppStringToManaged(elemPtr); }
                catch { continue; }

                if (!string.IsNullOrEmpty(s)) yield return s;
            }
        }

        private static bool IsValidMongoId(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length != 24) return false;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                bool isHex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!isHex) return false;
            }
            return true;
        }

        private static string ExtractMongoIdString(object value)
        {
            if (value == null) return null;

            if (value is string s)
                return string.IsNullOrEmpty(s) ? null : s;

            try
            {
                var str = value.ToString();
                if (IsValidMongoIdString(str)) return str;
            }
            catch { }

            try
            {
                var type = value.GetType();

                foreach (var prop in type.GetProperties())
                {
                    try
                    {
                        var v = prop.GetValue(value);
                        if (v == null) continue;
                        var vs = v.ToString();
                        if (IsValidMongoIdString(vs)) return vs;
                    }
                    catch { }
                }

                foreach (var field in type.GetFields())
                {
                    try
                    {
                        var v = field.GetValue(value);
                        if (v == null) continue;
                        var vs = v.ToString();
                        if (IsValidMongoIdString(vs)) return vs;
                    }
                    catch { }
                }
            }
            catch { }

            if (value is Il2CppObjectBase il2cppObj)
            {
                foreach (var name in new[] { "string_0", "id", "_id", "value", "_value", "String" })
                {
                    try
                    {
                        var t = Traverse.Create(il2cppObj);
                        if (t.Field(name).FieldExists())
                        {
                            var v = t.Field(name).GetValue();
                            var vs = v?.ToString();
                            if (IsValidMongoIdString(vs)) return vs;
                        }
                        if (t.Property(name).PropertyExists())
                        {
                            var v = t.Property(name).GetValue();
                            var vs = v?.ToString();
                            if (IsValidMongoIdString(vs)) return vs;
                        }
                    }
                    catch { }
                }
            }

            return null;
        }

        private static bool IsValidMongoIdString(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            if (s == "EFT.MongoID") return false;
            if (s.StartsWith("EFT.MongoID", StringComparison.Ordinal)) return false;
            if (s.StartsWith("Il2CppInterop", StringComparison.Ordinal)) return false;
            return true;
        }

        private static void WarnMissingZone(string typeName, Il2CppObjectBase obj)
        {
            if (_warnedMissingZone.Contains(typeName)) return;
            _warnedMissingZone.Add(typeName);

            DumpFieldsOnce(obj, typeName);

            NorvinskMapsPlugin.Log.LogWarning(
                $"[QPos] {typeName}: zone id not found. See field dump above.");
        }

        private static void DumpFieldsOnce(Il2CppObjectBase obj, string typeName)
        {
            if (_dumpedFieldSets.Contains(typeName)) return;
            _dumpedFieldSets.Add(typeName);

            try
            {
                var ptr = obj.Pointer;
                if (ptr == IntPtr.Zero) return;

                var klass = IL2CPP.il2cpp_object_get_class(ptr);
                if (klass == IntPtr.Zero) return;

                var currentKlass = klass;
                int depth = 0;
                while (currentKlass != IntPtr.Zero && depth < 16)
                {
                    var classNamePtr = IL2CPP.il2cpp_class_get_name(currentKlass);
                    var className = classNamePtr != IntPtr.Zero
                        ? Marshal.PtrToStringAnsi(classNamePtr)
                        : "?";

                    var names = new List<string>();
                    var iter = IntPtr.Zero;
                    IntPtr fieldPtr;
                    while ((fieldPtr = IL2CPP.il2cpp_class_get_fields(currentKlass, ref iter)) != IntPtr.Zero)
                    {
                        var namePtr = IL2CPP.il2cpp_field_get_name(fieldPtr);
                        if (namePtr != IntPtr.Zero)
                        {
                            var fname = Marshal.PtrToStringAnsi(namePtr);
                            if (!string.IsNullOrEmpty(fname)) names.Add(fname);
                        }
                    }

                    NorvinskMapsPlugin.Log.LogWarning(
                        $"[QPos] Fields of {className} (declared): [{string.Join(", ", names)}]");

                    currentKlass = IL2CPP.il2cpp_class_get_parent(currentKlass);
                    depth++;
                }
            }
            catch (Exception e)
            {
                NorvinskMapsPlugin.Log.LogWarning($"[QPos] DumpFieldsOnce({typeName}) threw: {e.Message}");
            }
        }

        public static class Il2CppTypeHelper
        {
            public static string GetClassName(Il2CppObjectBase obj)
            {
                if (obj == null) return null;
                var ptr = obj.Pointer;
                if (ptr == IntPtr.Zero) return null;

                var klass = IL2CPP.il2cpp_object_get_class(ptr);
                if (klass == IntPtr.Zero) return null;

                var namePtr = IL2CPP.il2cpp_class_get_name(klass);
                return namePtr == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(namePtr);
            }

            public static string GetNamespace(Il2CppObjectBase obj)
            {
                if (obj == null || obj.Pointer == IntPtr.Zero) return null;
                var klass = IL2CPP.il2cpp_object_get_class(obj.Pointer);
                if (klass == IntPtr.Zero) return null;
                var nsPtr = IL2CPP.il2cpp_class_get_namespace(klass);
                return nsPtr == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(nsPtr);
            }

            public static string GetFullName(Il2CppObjectBase obj)
            {
                var ns = GetNamespace(obj);
                var n = GetClassName(obj);
                if (n == null) return null;
                return string.IsNullOrEmpty(ns) ? n : $"{ns}.{n}";
            }
        }
    }
}