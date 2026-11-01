using System;
using System.Collections.Generic;
using NorvinskMaps.Config;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;

namespace NorvinskMaps.Patches
{
    [HarmonyPatch(typeof(BaseItemContextInteractions), nameof(BaseItemContextInteractions.IsActive))]
    internal static class ShowViewButtonPatch
    {
        private static readonly HashSet<string> _mapsToIgnore = new()
        {
            "6738033eb7305d3bdafe9518",
            "673803448cb3819668d77b1b",
            "6738034a9713b5f42b4a8b78",
            "6738034e9d22459ad7cd1b81",
            "6738035350b24a4ae4a57997",
        };

        [HarmonyPostfix]
        private static void Postfix(BaseItemContextInteractions __instance,
                                    EItemInfoButton button,
                                    ref bool __result)
        {
            try
            {
                if (button != EItemInfoButton.ViewMap || Settings.ReplaceMapScreen.Value)
                    return;

                var item = __instance.Item;
                __result = !_mapsToIgnore.Contains(item.TemplateId);
            }
            catch (Exception ex)
            {
                NorvinskMapsPlugin.Log.LogError($"Ошибка в ShowViewButtonPatch: {ex.Message}");
            }
        }
    }
}