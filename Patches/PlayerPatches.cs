using System;
using System.Collections.Generic;
using Comfort.Common;
using NorvinskMaps.Utils;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;

namespace NorvinskMaps.Patches
{
    [HarmonyPatch(typeof(Player), "OnDead")]
    internal static class PlayerOnDeadPatch
    {
        public static event Action<Player> OnDead;

        [HarmonyPostfix]
        private static void Postfix(Player __instance, EDamageType damageType)
        {
            try { OnDead?.Invoke(__instance); }
            catch (Exception e) { NorvinskMapsPlugin.Log.LogError($"[PlayerOnDeadPatch] {e.Message}"); }
        }
    }

    [HarmonyPatch(typeof(Player.PlayerInventoryController), "ThrowItem")]
    internal static class PlayerInventoryThrowItemPatch
    {
        public static readonly Dictionary<int, Item> ThrownItems = new();
        public static event Action<int, Item> OnThrowItem;

        [HarmonyPostfix]
        private static void Postfix(Player.PlayerInventoryController __instance,
                                    Item item, bool downDirection, Callback callback)
        {
            try
            {
                var player = __instance.Player;
                if (player == null || player != GameUtils.GetMainPlayer()) return;

                var itemNetId = item.Id.GetHashCode();
                ThrownItems[itemNetId] = item;
                OnThrowItem?.Invoke(itemNetId, item);
            }
            catch (Exception e)
            {
                NorvinskMapsPlugin.Log.LogError($"[PlayerInventoryThrowItemPatch] {e.Message}");
            }
        }

        public static void Reset() => ThrownItems.Clear();
    }
}