using System;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using NorvinskMaps.Config;
using EFT;
using EFT.InventoryLogic;
using EFT.Vehicle;
using HarmonyLib;

namespace NorvinskMaps.Utils
{
    public static class GameUtils
    {
        public static Profile PlayerProfile => GetMainPlayer()?.Profile;

        private static readonly HashSet<WildSpawnType> _trackedBosses = new HashSet<WildSpawnType>
        {
            WildSpawnType.bossBoar,
            WildSpawnType.bossBully,
            WildSpawnType.bossGluhar,
            WildSpawnType.bossKilla,
            WildSpawnType.bossKnight,
            WildSpawnType.followerBigPipe,
            WildSpawnType.followerBirdEye,
            WildSpawnType.bossKolontay,
            WildSpawnType.bossKojaniy,
            WildSpawnType.bossSanitar,
            WildSpawnType.bossTagilla,
            WildSpawnType.bossPartisan,
            WildSpawnType.bossZryachiy,
            WildSpawnType.gifter,
            WildSpawnType.arenaFighterEvent,
            WildSpawnType.sectantPriest,
            WildSpawnType.bossTagillaAgro,
            WildSpawnType.bossKillaAgro,
            WildSpawnType.tagillaHelperAgro,
            (WildSpawnType) 199,
            (WildSpawnType) 801,
        };

        private static readonly Dictionary<string, string> MapLookUp = new()
        {
            { "factory4_day",   "574eb85c245977648157eec3" },
            { "factory4_night", "574eb85c245977648157eec3" },
            { "Woods",          "5900b89686f7744e704a8747" },
            { "bigmap",         "5798a2832459774b53341029" },
            { "Interchange",    "5be4038986f774527d3fae60" },
            { "RezervBase",     "6738034a9713b5f42b4a8b78" },
            { "Shoreline",      "5a8036fb86f77407252ddc02" },
            { "laboratory",     "6738034e9d22459ad7cd1b81" },
            { "Lighthouse",     "6738035350b24a4ae4a57997" },
            { "TarkovStreets",  "673803448cb3819668d77b1b" },
            { "Sandbox",        "6738033eb7305d3bdafe9518" },
            { "Sandbox_high",   "6738033eb7305d3bdafe9518" },
            { "Labyrinth",      "68f1ad32317cc52f4c0b6fae" }
        };

        public static bool IsInRaid()
        {
            var game = Singleton<AbstractGame>.Instance;
            var botGame = Singleton<IBotGame>.Instance;

            return ((game != null) && game.InRaid)
                || ((botGame != null) && botGame.Status != GameStatus.Stopped
                                      && botGame.Status != GameStatus.Stopping
                                      && botGame.Status != GameStatus.SoftStopping);
        }

        public static string GetCurrentMapInternalName()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            return gameWorld?.MainPlayer?.Location;
        }

        public static Player GetMainPlayer()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            return gameWorld?.MainPlayer;
        }

        public static Profile GetPlayerProfile()
        {
            return PlayerProfile;
        }

        public static BTRView GetBTRView()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            return gameWorld?.BtrController?.BtrView;
        }

        public static bool IsScavRaid()
        {
            var player = GetMainPlayer();
            return IsInRaid() && (player != null) && player.Side == EPlayerSide.Savage;
        }

        public static string BSGLocalized(this MongoID id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return "";
            }

            return id.ToString().Localized();
        }

        public static string BSGLocalized(this string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return "";
            }

            return id.Localized();
        }

        public static bool IsGroupedWithMainPlayer(this IPlayer player)
        {
            var mainPlayer = GetMainPlayer();
            if (mainPlayer == null) return false;

            var mainPlayerGroupId = mainPlayer.GroupId;
            return !string.IsNullOrEmpty(mainPlayerGroupId) && player.GroupId == mainPlayerGroupId;
        }

        public static bool IsTrackedBoss(this IPlayer player)
        {
            return player.Profile.Side == EPlayerSide.Savage
                && _trackedBosses.Contains(player.Profile.Info.Settings.Role);
        }

        public static bool IsPMC(this IPlayer player)
        {
            return player.Profile.Side == EPlayerSide.Bear || player.Profile.Side == EPlayerSide.Usec;
        }

        public static bool IsScav(this IPlayer player)
        {
            return player.Profile.Side == EPlayerSide.Savage;
        }

        public static bool DidMainPlayerKill(this IPlayer player)
        {
            if (player == null) return false;

            var mainPlayer = GetMainPlayer();
            if (mainPlayer == null) return false;

            var victimId = player.ProfileId;
            if (string.IsNullOrEmpty(victimId)) return false;

            if (victimId == mainPlayer.ProfileId) return false;

            return MainPlayerKilledProfile(mainPlayer, victimId);
        }

        public static bool DidTeammateKill(this IPlayer player)
        {
            if (player == null) return false;

            var mainPlayer = GetMainPlayer();
            if (mainPlayer == null) return false;

            var victimId = player.ProfileId;
            if (string.IsNullOrEmpty(victimId)) return false;

            var myGroupId = mainPlayer.GroupId;
            if (string.IsNullOrEmpty(myGroupId)) return false;

            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null) return false;

            foreach (var candidate in gameWorld.AllAlivePlayersList)
            {
                if (candidate == null) continue;
                if (candidate.ProfileId == mainPlayer.ProfileId) continue;
                if (candidate.GroupId != myGroupId) continue;

                if (MainPlayerKilledProfile(candidate, victimId)) return true;
            }

            return false;
        }

        private static bool MainPlayerKilledProfile(Player killer, string victimProfileId)
        {
            try
            {
                var stats = killer.Profile?.EftStats;
                if (stats?.Victims == null) return false;

                foreach (var v in stats.Victims)
                {
                    if (v == null) continue;
                    if (string.IsNullOrEmpty(v.ProfileId)) continue;
                    if (v.ProfileId == victimProfileId) return true;
                }
            }
            catch (Exception e)
            {
                NorvinskMapsPlugin.Log.LogWarning(
                    $"[MainPlayerKilledProfile] {e.GetType().Name}: {e.Message}");
            }
            return false;
        }

        public static bool IsBTRShooter(this IPlayer player)
        {
            return player.Profile.Side == EPlayerSide.Savage
                && player.Profile.Info.Settings.Role == WildSpawnType.shooterBTR;
        }

        public static bool HasCorpse(this Player player)
        {
            if (player == null) return false;

            try
            {
                var hc = player.HealthController;
                if (hc == null) return false;
                if (hc.IsAlive) return false;
            }
            catch
            {
                return false;
            }

            try
            {
                if (player.Profile?.Info == null) return false;
            }
            catch
            {
                return false;
            }

            return true;
        }

        public static bool IsHeadlessClient(this IPlayer player)
        {
            return player.Profile.Info.MemberCategory == EMemberCategory.UnitTest;
        }

        public static int GetIntelLevel()
        {
            var profile = PlayerProfile;
            if (profile == null) return 0;

            var areas = profile.Hideout?.Areas;
            if (areas == null) return 0;

            var intelArea = areas.SingleOrDefault(a => a.AreaType == EAreaType.IntelligenceCenter);
            return intelArea?.Level ?? 0;
        }

        public static MongoID[] GetWishListItems()
        {
            var profile = PlayerProfile;
            if (profile == null) return Array.Empty<MongoID>();

            var wishlist = profile.WishlistManager?.GetWishlist();
            if (wishlist == null) return Array.Empty<MongoID>();

            var keys = new List<MongoID>();
            foreach (var kvp in wishlist)
                keys.Add(kvp.Key);
            return keys.ToArray();
        }

        public static bool ShouldShowMapInRaid()
        {
            if (!Settings.RequireMapInInventory.Value || !IsInRaid()) return true;

            var player = GetMainPlayer();
            if (player == null) return true;

            var currentLocation = player.Location;

            if (!MapLookUp.TryGetValue(currentLocation, out var id))
            {
                NorvinskMapsPlugin.Log.LogWarning($"Could not find map id for location {currentLocation} — new location?");
                return true;
            }

            var isMapInInventory = false;
            var equipment = player.Inventory?.Equipment;
            if (equipment != null)
            {
                var allItems = equipment.GetAllItems();
                foreach (var item in allItems)
                {
                    if (item.TemplateId == id)
                    {
                        isMapInInventory = true;
                        break;
                    }
                }
            }
            return isMapInInventory;
        }
    }
}