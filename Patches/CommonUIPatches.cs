using NorvinskMaps.Utils;
using EFT.UI;
using EFT.UI.Map;
using HarmonyLib;

namespace NorvinskMaps.Patches
{
    [HarmonyPatch(typeof(CommonUI), "Awake")]
    internal static class CommonUIAwakePatch
    {
        [HarmonyPostfix]
        private static void Postfix(CommonUI __instance)
        {
            try
            {
                var mapScreen = Traverse.Create(__instance.InventoryScreen)
                    .Field<MapScreen>("_mapScreen").Value;

                if (mapScreen != null)
                    NorvinskMapsPlugin.Instance?.TryAttachToMapScreen(mapScreen);
            }
            catch (System.Exception e)
            {
                NorvinskMapsPlugin.Log.LogError($"[CommonUIAwakePatch] {e.Message}");
            }
        }
    }
}