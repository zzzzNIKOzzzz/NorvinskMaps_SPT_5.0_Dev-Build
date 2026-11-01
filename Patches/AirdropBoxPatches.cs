using System;
using System.Collections.Generic;
using NorvinskMaps.Utils;
using EFT.Airdrop;
using EFT.SynchronizableObjects;
using HarmonyLib;

namespace NorvinskMaps.Patches
{
    [HarmonyPatch(typeof(ClientAirDrop), "InitializeLandedAirdrop")]
    internal static class AirdropBoxOnBoxLandPatch
    {
        public static readonly List<AirdropSynchronizableObject> Airdrops = new();
        public static event Action<AirdropSynchronizableObject> OnAirdropLanded;

        [HarmonyPostfix]
        private static void Postfix(ClientAirDrop __instance)
        {
            try
            {
                var sync = Traverse.Create(__instance)
                    .Field<AirdropSynchronizableObject>("_syncObject").Value;
                if (sync == null) return;
                if (Airdrops.Contains(sync)) return;

                Airdrops.Add(sync);
                OnAirdropLanded?.Invoke(sync);
            }
            catch (Exception e)
            {
                NorvinskMapsPlugin.Log.LogError($"[AirdropBoxOnBoxLandPatch] {e.Message}");
            }
        }

        internal static void Reset() => Airdrops.Clear();
    }
}