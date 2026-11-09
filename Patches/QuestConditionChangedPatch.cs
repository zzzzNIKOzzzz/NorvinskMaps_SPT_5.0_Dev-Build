using System;
using HarmonyLib;
using NorvinskMaps.UI;

namespace NorvinskMaps.Patches
{
    [HarmonyPatch]
    internal static class QuestConditionChangedPatch
    {
        private static System.Reflection.MethodBase TargetMethod()
        {
            var questControllerType = AccessTools.TypeByName("EFT.Quests.QuestController")
                                     ?? AccessTools.TypeByName("QuestControllerClass");

            if (questControllerType == null)
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (type.Name.Contains("QuestController") &&
                            type.GetMethod("OnConditionValueChanged",
                                System.Reflection.BindingFlags.Instance |
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic) != null)
                        {
                            questControllerType = type;
                            break;
                        }
                    }
                    if (questControllerType != null) break;
                }
            }

            if (questControllerType == null)
            {
                NorvinskMapsPlugin.Log.LogWarning(
                    "[QuestConditionChangedPatch] QuestController type not found");
                return null;
            }

            var method = questControllerType.GetMethod("OnConditionValueChanged",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);

            if (method == null)
            {
                NorvinskMapsPlugin.Log.LogWarning(
                    "[QuestConditionChangedPatch] OnConditionValueChanged not found");
            }

            return method;
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            try
            {
                var map = NorvinskMapsPlugin.Instance?.Map;
                if (map != null)
                {
                    map.MarkQuestMarkersDirty();
                }
            }
            catch (Exception e)
            {
                NorvinskMapsPlugin.Log.LogError($"[QuestConditionChangedPatch] {e.Message}");
            }
        }
    }
}