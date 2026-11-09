using System;
using System.Linq;
using System.Reflection;
using NorvinskMaps.Utils;
using EFT.UI;
using HarmonyLib;

namespace NorvinskMaps.Patches
{
    [HarmonyPatch]
    internal static class BattleUIScreenShowPatch
    {
        public static bool IsAttached { get; private set; }
        public static EftBattleUIScreen LastBattleUI { get; private set; }

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            var method = AccessTools.Method(
                typeof(EftBattleUIScreen),
                "Show",
                new Type[] { typeof(EftBattleUIScreen.EftBattleUIScreenController) });

            if (method != null)
            {
                return method;
            }

            method = AccessTools.Method(
                typeof(EftBattleUIScreen),
                "Show",
                new Type[] { typeof(EFT.GamePlayerOwner) });

            if (method != null)
            {
                return method;
            }

            var anyShow = typeof(EftBattleUIScreen)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(m => m.Name == "Show" && m.GetParameters().Length == 1);

            if (anyShow != null)
            {
                var p = anyShow.GetParameters()[0];
                NorvinskMapsPlugin.Log.LogInfo(
                    $"[BattleUIScreenShowPatch] Target: Show({p.ParameterType.Name}) — fallback");
                return anyShow;
            }

            var noArgShow = typeof(EftBattleUIScreen)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(m => m.Name == "Show" && m.GetParameters().Length == 0);

            if (noArgShow != null)
            {
                return noArgShow;
            }

            NorvinskMapsPlugin.Log.LogError(
                "[BattleUIScreenShowPatch] CRITICAL: no Show method found at all!");
            return null;
        }

        [HarmonyPostfix]
        private static void Postfix(EftBattleUIScreen __instance)
        {
            try
            {
                LastBattleUI = __instance;

                if (__instance == null) return;

                if (!GameUtils.ShouldShowMapInRaid())
                {
                    IsAttached = false;
                    return;
                }

                NorvinskMapsPlugin.Instance?.TryAttachToBattleUIScreen(__instance);
                IsAttached = true;
                try
                {
                    NorvinskMapsPlugin.Instance?.Map?.WarmupForCurrentRaid();
                }
                catch (Exception e)
                {
                    NorvinskMapsPlugin.Log.LogWarning($"[BattleUIScreenShowPatch] Warmup: {e.Message}");
                }
            }
            catch (Exception e)
            {
                NorvinskMapsPlugin.Log.LogError($"[BattleUIScreenShowPatch] {e.Message}\n{e.StackTrace}");
            }
        }

        internal static void TrySetLastBattleUI(EftBattleUIScreen ui)
        {
            if (ui != null) LastBattleUI = ui;
        }
    }
}