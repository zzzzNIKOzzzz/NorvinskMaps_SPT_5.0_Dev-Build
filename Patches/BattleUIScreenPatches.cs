using System;
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

        [HarmonyTargetMethod]
		private static MethodBase TargetMethod()
		{
			var method = AccessTools.Method(
				typeof(EftBattleUIScreen),
				"Show",
				new Type[] { typeof(EftBattleUIScreen.EftBattleUIScreenController) });

			if (method == null)
			{
				NorvinskMapsPlugin.Log.LogError("[BattleUIScreenShowPatch] Show(TController) not found on EftBattleUIScreen!");
			}
			return method;
		}

        [HarmonyPostfix]
		private static void Postfix(EftBattleUIScreen __instance)
		{
			try
			{
				if (!GameUtils.ShouldShowMapInRaid())
				{
					IsAttached = false;
					return;
				}

				NorvinskMapsPlugin.Instance?.TryAttachToBattleUIScreen(__instance);
				IsAttached = true;
			}
			catch (Exception e)
			{
				NorvinskMapsPlugin.Log.LogError($"[BattleUIScreenShowPatch] {e.Message}");
			}
		}
    }
}