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

        private static readonly System.Reflection.MemberInfo _playerCorpseMember =
    FindPlayerMember("Corpse", "corpse", "_corpse");
        private static readonly System.Reflection.MemberInfo _playerLastAggressorMember =
            FindPlayerMember("LastAggressor", "lastAggressor", "_lastAggressor");

        private static System.Reflection.MemberInfo FindPlayerMember(params string[] names)
        {
            var t = typeof(Player);
            var flags = System.Reflection.BindingFlags.Instance
                      | System.Reflection.BindingFlags.Public
                      | System.Reflection.BindingFlags.NonPublic;

            foreach (var name in names)
            {
                var f = t.GetField(name, flags);
                if (f != null) return f;

                var p = t.GetProperty(name, flags);
                if (p != null && p.GetGetMethod(true) != null) return p;
            }

            foreach (var m in t.GetMembers(flags))
            {
                var lower = m.Name.ToLowerInvariant();
                foreach (var name in names)
                {
                    if (lower.Contains(name.ToLowerInvariant()))
                        return m;
                }
            }
            return null;
        }

        private static object ReadPlayerMember(System.Reflection.MemberInfo member, Player player)
        {
            if (member == null || player == null) return null;
            try
            {
                if (member is System.Reflection.FieldInfo f) return f.GetValue(player);
                if (member is System.Reflection.PropertyInfo p) return p.GetValue(player);
            }
            catch { }
            return null;
        }

        private static int _cachedIntelLevel = -1;
        private static float _intelCacheTime = -1f;

        private static HashSet<MongoID> _wishlistCache;
        private static float _wishlistCacheTime = -1f;
        private const float WishlistCacheTtl = 2f;

        public static int GetIntelLevel()
        {
            var now = UnityEngine.Time.realtimeSinceStartup;
            if (_cachedIntelLevel >= 0 && now - _intelCacheTime < 5f)
                return _cachedIntelLevel;

            var profile = PlayerProfile;
            if (profile == null)
            {
                _cachedIntelLevel = 0;
                _intelCacheTime = now;
                return 0;
            }

            var areas = profile.Hideout?.Areas;
            if (areas == null)
            {
                _cachedIntelLevel = 0;
                _intelCacheTime = now;
                return 0;
            }

            var intelArea = areas.SingleOrDefault(a => a.AreaType == EAreaType.IntelligenceCenter);
            _cachedIntelLevel = intelArea?.Level ?? 0;
            _intelCacheTime = now;
            return _cachedIntelLevel;
        }

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

        public static Profile GetPlayerProfile() => PlayerProfile;

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
            if (string.IsNullOrWhiteSpace(id)) return "";
            return id.ToString().Localized();
        }

        public static string BSGLocalized(this string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return "";
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

            if (_playerLastAggressorMember != null)
            {
                try
                {
                    var aggressor = ReadPlayerMember(_playerLastAggressorMember, player as Player) as IPlayer;
                    if (aggressor != null)
                    {
                        var mainPlayer = GetMainPlayer();
                        return mainPlayer != null && aggressor.ProfileId == mainPlayer.ProfileId;
                    }
                    return false;
                }
                catch { }
            }

            var main = GetMainPlayer();
            if (main == null) return false;
            var victimId = player.ProfileId;
            if (string.IsNullOrEmpty(victimId) || victimId == main.ProfileId) return false;
            return MainPlayerKilledProfile(main, victimId);
        }

        public static bool DidTeammateKill(this IPlayer player)
        {
            if (player == null) return false;

            if (_playerLastAggressorMember != null)
            {
                try
                {
                    var aggressor = ReadPlayerMember(_playerLastAggressorMember, player as Player) as IPlayer;
                    if (aggressor == null || string.IsNullOrEmpty(aggressor.GroupId))
                        return false;

                    var mainPlayer = GetMainPlayer();
                    if (mainPlayer == null) return false;

                    return aggressor.ProfileId != mainPlayer.ProfileId
                        && aggressor.GroupId == mainPlayer.GroupId;
                }
                catch { }
            }

            var main = GetMainPlayer();
            if (main == null) return false;
            var victimId = player.ProfileId;
            if (string.IsNullOrEmpty(victimId)) return false;
            var myGroupId = main.GroupId;
            if (string.IsNullOrEmpty(myGroupId)) return false;

            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null) return false;

            foreach (var candidate in gameWorld.AllAlivePlayersList)
            {
                if (candidate == null) continue;
                if (candidate.ProfileId == main.ProfileId) continue;
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

            if (_playerCorpseMember != null)
            {
                try { return ReadPlayerMember(_playerCorpseMember, player) != null; }
                catch { }
            }

            try
            {
                var hc = player.HealthController;
                if (hc == null) return false;
                if (hc.IsAlive) return false;
            }
            catch { return false; }

            try
            {
                if (player.Profile?.Info == null) return false;
            }
            catch { return false; }

            return true;
        }

        public static bool IsHeadlessClient(this IPlayer player)
        {
            return player.Profile.Info.MemberCategory == EMemberCategory.UnitTest;
        }

        public static HashSet<MongoID> GetWishListSet()
        {
            var now = UnityEngine.Time.realtimeSinceStartup;
            if (_wishlistCache != null && now - _wishlistCacheTime < WishlistCacheTtl)
                return _wishlistCache;

            var set = new HashSet<MongoID>();
            var profile = PlayerProfile;
            if (profile != null)
            {
                var wishlist = profile.WishlistManager?.GetWishlist();
                if (wishlist != null)
                {
                    foreach (var kvp in wishlist)
                        set.Add(kvp.Key);
                }
            }

            _wishlistCache = set;
            _wishlistCacheTime = now;
            return _wishlistCache;
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

            var equipment = player.Inventory?.Equipment;
            if (equipment != null)
            {
                foreach (var item in equipment.GetAllItems())
                {
                    if (item.TemplateId == id) return true;
                }
            }
            return false;
        }
    }
}