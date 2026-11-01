using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace NorvinskMaps.Patches
{
    [HarmonyPatch]
    internal static class ConfigurationManagerSectionTranslator
    {
        [HarmonyPrepare]
        private static bool Prepare()
        {
            var cmAssembly = FindConfigurationManagerAssembly();
            if (cmAssembly == null)
            {
                NorvinskMapsPlugin.Log?.LogWarning(
                    "[CM.SectionTranslator] ConfigurationManager assembly not found — section headers will stay English");
                return false;
            }

            return true;
        }

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            var cmAssembly = FindConfigurationManagerAssembly();
            if (cmAssembly == null) return null;

            string[] candidateNames =
            {
                "ConfigurationManager.ConfigSettingEntry",
                "ConfigurationManager.SettingEntryBase",
            };

            foreach (var name in candidateNames)
            {
                var type = cmAssembly.GetType(name, false);
                if (type == null) continue;

                var prop = type.GetProperty(
                    "Category",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (prop == null) continue;

                var getter = prop.GetGetMethod(true);
                if (getter == null) continue;
                if (getter.IsAbstract) continue;

                return getter;
            }

            NorvinskMapsPlugin.Log?.LogWarning(
                "[CM.SectionTranslator] Category getter not found — section headers will stay English");
            return null;
        }

        [HarmonyPostfix]
        private static void Postfix(ref string __result)
        {
            if (string.IsNullOrEmpty(__result)) return;

            switch (__result)
            {
                case "1. General":         __result = Localization.T("Section_General"); break;
                case "2. Dynamic Markers": __result = Localization.T("Section_DynamicMarkers"); break;
                case "3. Progression":     __result = Localization.T("Section_Progression"); break;
                case "4. In-Raid":         __result = Localization.T("Section_InRaid"); break;
                case "5. Mini-map":        __result = Localization.T("Section_MiniMap"); break;
                case "6. Marker Colors":   __result = Localization.T("Section_MarkerColors"); break;
            }
        }

        private static Assembly FindConfigurationManagerAssembly()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "ConfigurationManager")
                    return asm;
            }
            return null;
        }
    }
}