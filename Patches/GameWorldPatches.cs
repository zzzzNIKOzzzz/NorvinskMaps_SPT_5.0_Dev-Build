using System;
using System.Collections.Generic;
using System.Reflection;
using NorvinskMaps.Utils;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace NorvinskMaps.Patches
{
    [HarmonyPatch(typeof(GameWorld), "OnGameStarted")]
    internal static class GameStartedPatch
    {
        public static readonly List<LootableContainer> HiddenStashes = new();

        [HarmonyPostfix]
        private static void Postfix(GameWorld __instance)
        {
            try
            {
                var allObjects = LocationScene.GetAllObjects<LootableContainer>();
                foreach (var obj in allObjects)
                {
                    if (obj.name.StartsWith("scontainer_wood_CAP")
                    || obj.name.StartsWith("scontainer_Blue_Barrel_Base_Cap"))
                    {
                        HiddenStashes.Add(obj);
                    }
                }

                NorvinskMapsPlugin.Log.LogInfo(
                    $"[DIAG] GameStartedPatch: stashes={HiddenStashes.Count}");
            }
            catch (Exception e)
            {
                NorvinskMapsPlugin.Log.LogError($"[GameStartedPatch] {e.Message}");
            }
        }
    }

    [HarmonyPatch]
    internal static class GameWorldOnDestroyPatch
    {
        public static event Action OnRaidEnd;

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            var method = AccessTools.Method(typeof(GameWorld), "Dispose", Type.EmptyTypes);
            if (method == null)
                method = AccessTools.Method(typeof(GameWorld), "OnDestroy", Type.EmptyTypes);

            if (method == null)
                NorvinskMapsPlugin.Log.LogError("[GameWorldOnDestroyPatch] Neither Dispose nor OnDestroy found!");

            return method;
        }

        [HarmonyPrefix]
        private static void Prefix()
        {
            try { OnRaidEnd?.Invoke(); }
            catch (Exception e) { NorvinskMapsPlugin.Log.LogError($"[GameWorldOnDestroyPatch] {e.Message}"); }

            GameStartedPatch.HiddenStashes.Clear();
            PlayerInventoryThrowItemPatch.Reset();
            AirdropBoxOnBoxLandPatch.Reset();
        }
    }

    [HarmonyPatch(typeof(GameWorld), "UnregisterPlayer")]
    internal static class GameWorldUnregisterPlayerPatch
    {
        public static event Action<IPlayer> OnUnregisterPlayer;

        [HarmonyPostfix]
        private static void Postfix(IPlayer iPlayer)
        {
            try { OnUnregisterPlayer?.Invoke(iPlayer); }
            catch (Exception e) { NorvinskMapsPlugin.Log.LogError($"[GameWorldUnregisterPlayerPatch] {e.Message}"); }
        }
    }

    [HarmonyPatch(typeof(LootItem), "Init", new Type[]
    {
        typeof(Item), typeof(string), typeof(GameWorld), typeof(bool),
        typeof(Il2CppReferenceArray<MongoID>), typeof(string), typeof(bool)
    })]
    internal static class LootItemInitPatch
    {
        public static event Action<LootItem> OnRegisterLoot;

        [HarmonyPostfix]
        private static void Postfix(LootItem __instance)
        {
            try { OnRegisterLoot?.Invoke(__instance); }
            catch (Exception e) { NorvinskMapsPlugin.Log.LogError($"[LootItemInitPatch] {e.Message}"); }
        }
    }

    [HarmonyPatch(typeof(GameWorld), "DestroyLoot", new Type[] { typeof(IKillable) })]
    internal static class GameWorldDestroyLootPatch
    {
        public static event Action<LootItem> OnDestroyLoot;

        [HarmonyPrefix]
        private static void Prefix(IKillable loot)
        {
            try
            {
                if (loot is LootItem lootItem)
                    OnDestroyLoot?.Invoke(lootItem);
            }
            catch (Exception e) { NorvinskMapsPlugin.Log.LogError($"[GameWorldDestroyLootPatch] {e.Message}"); }
        }
    }
}