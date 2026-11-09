using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace NorvinskMaps.Utils
{
    public static class QuestLocaleLoader
    {
        private static Dictionary<string, string> _questNames = new();
        private static bool _loaded = false;

        public static void Load()
        {
            if (_loaded) return;
            _loaded = true;

            try
            {
                string gameRoot = AppDomain.CurrentDomain.BaseDirectory;

                // 1. Загружаем базовые названия из quests.json
                string[] questPaths = {
                    Path.Combine(gameRoot, "SPT_Runtime", "SPT_Data", "database", "templates", "quests.json"),
                    Path.Combine(gameRoot, "SPT_Data", "database", "templates", "quests.json")
                };

                foreach (var path in questPaths)
                {
                    if (File.Exists(path)) { ParseQuestsJson(path); break; }
                }

                // 2. Загружаем глобальный ru.json / en.json
                string[] locales = { "ru", "en" };
                foreach (var lang in locales)
                {
                    string[] paths = {
                        Path.Combine(gameRoot, "SPT_Runtime", "SPT_Data", "database", "locales", "global", $"{lang}.json"),
                        Path.Combine(gameRoot, "SPT_Data", "database", "locales", "global", $"{lang}.json")
                    };

                    foreach (var path in paths)
                    {
                        if (File.Exists(path)) { ParseGlobalLocaleJson(path); break; }
                    }
                }

                // 3. Загружаем mainQuestNotes.json (там часто лежат названия сюжеток)
                string[] notesPaths = {
                    Path.Combine(gameRoot, "SPT_Runtime", "SPT_Data", "database", "templates", "mainQuestNotes.json"),
                    Path.Combine(gameRoot, "SPT_Data", "database", "templates", "mainQuestNotes.json")
                };

                foreach (var path in notesPaths)
                {
                    if (File.Exists(path)) { ParseMainQuestNotes(path); break; }
                }

                NorvinskMapsPlugin.Log.LogInfo($"[QuestLocale] Total loaded quest names: {_questNames.Count}");
            }
            catch (Exception e)
            {
                NorvinskMapsPlugin.Log.LogError($"[QuestLocale] Error: {e.Message}");
            }
        }

        private static void ParseMainQuestNotes(string path)
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    // Смотрим, есть ли внутри объекта поле "name" или "Name"
                    if (prop.Value.ValueKind == JsonValueKind.Object)
                    {
                        if (prop.Value.TryGetProperty("name", out var n) || prop.Value.TryGetProperty("Name", out n))
                        {
                            string name = n.GetString();
                            if (!string.IsNullOrWhiteSpace(name) && !_questNames.ContainsKey(prop.Name))
                            {
                                _questNames[prop.Name] = name;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private static void ParseQuestsJson(string path)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            foreach (var locale in new[] { "ru", "en" })
            {
                if (doc.RootElement.TryGetProperty(locale, out var localeBlock) && localeBlock.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in localeBlock.EnumerateObject())
                    {
                        if (prop.Name.EndsWith(" name", StringComparison.OrdinalIgnoreCase))
                        {
                            string questId = prop.Name.Substring(0, prop.Name.Length - 5);
                            string questName = prop.Value.GetString();
                            if (!string.IsNullOrWhiteSpace(questName))
                                _questNames[questId] = questName;
                        }
                    }
                }
            }
        }

        private static void ParseGlobalLocaleJson(string path)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Name.EndsWith(" name", StringComparison.OrdinalIgnoreCase))
                {
                    string questId = prop.Name.Substring(0, prop.Name.Length - 5);
                    string questName = prop.Value.GetString();
                    if (!string.IsNullOrWhiteSpace(questName) && !_questNames.ContainsKey(questId))
                        _questNames[questId] = questName;
                }
            }
        }

        public static string GetQuestName(string id)
        {
            if (!_loaded) Load();
            if (string.IsNullOrEmpty(id)) return null;

            if (_questNames.TryGetValue(id, out var name) && !string.IsNullOrWhiteSpace(name))
                return name;

            return null;
        }
    }
}