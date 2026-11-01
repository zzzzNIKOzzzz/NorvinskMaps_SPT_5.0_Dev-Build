using System;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using NorvinskMaps.Config;
using NorvinskMaps.Data;
using NorvinskMaps.Patches;
using NorvinskMaps.UI.Components;
using NorvinskMaps.Utils;
using EFT;
using UnityEngine;

namespace NorvinskMaps.DynamicMarkers
{
    public class OtherPlayersMarkerProvider : IDynamicMarkerProvider
    {
        private const string _arrowImagePath = "Markers/arrow.png";
        private const string _starImagePath = "Markers/boss.png";

        private const string _friendlyPlayerCategory = "Friendly Player";
        private const string _friendlyPlayerImagePath = _arrowImagePath;
        private static Color _friendlyPlayerColor = Color.Lerp(Color.blue, Color.white, 0.5f);

        private const string _enemyPlayerCategory = "Enemy Player";
        private const string _enemyPlayerImagePath = _arrowImagePath;

        private const string _scavCategory = "Scav";
        private const string _scavImagePath = _arrowImagePath;

        private const string _bossCategory = "Boss";
        private const string _bossImagePath = _starImagePath;

        private const string _pmcBearImagePath = "Markers/pmc_bear.png";
        private const string _pmcUsecImagePath = "Markers/pmc_usec.png";

        private bool _showFriendlyPlayers = true;
        public bool ShowFriendlyPlayers
        {
            get => _showFriendlyPlayers;
            set => HandleSetBoolOption(ref _showFriendlyPlayers, value);
        }

        private bool _showEnemyPlayers = false;
        public bool ShowEnemyPlayers
        {
            get => _showEnemyPlayers;
            set => HandleSetBoolOption(ref _showEnemyPlayers, value);
        }

        private bool _showScavs = false;
        public bool ShowScavs
        {
            get => _showScavs;
            set => HandleSetBoolOption(ref _showScavs, value);
        }

        private bool _showBosses = false;
        public bool ShowBosses
        {
            get => _showBosses;
            set => HandleSetBoolOption(ref _showBosses, value);
        }

        private MapView _lastMapView;

        private Dictionary<string, (Player player, PlayerMapMarker marker)> _playerMarkers = [];

        public void OnShowInRaid(MapView map)
        {
            _lastMapView = map;
            TryAddMarkers();
            RemoveNonActivePlayers();
        }

        public void OnHideInRaid(MapView map)
        {
        }

        public void OnRaidEnd(MapView map)
        {
            TryRemoveMarkers();
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            _lastMapView = map;

            var keys = _playerMarkers.Keys.ToList();
            foreach (var key in keys)
            {
                var entry = _playerMarkers[key];
                TryRemoveMarker(key);
                TryAddMarker(entry.player);
            }
        }

        public void OnDisable(MapView map)
        {
            TryRemoveMarkers();
        }

        private void TryRemoveMarkers()
        {
            foreach (var key in _playerMarkers.Keys.ToList())
            {
                TryRemoveMarker(key);
            }

            _playerMarkers.Clear();
        }

        private void TryAddMarkers()
        {
            if (!GameUtils.IsInRaid())
            {
                return;
            }

            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null) return;

            foreach (var player in gameWorld.AllAlivePlayersList)
            {
                if (player == null || player.IsYourPlayer)
                {
                    continue;
                }

                var playerId = player.Profile?.Id;
                if (string.IsNullOrEmpty(playerId) || _playerMarkers.ContainsKey(playerId))
                {
                    continue;
                }

                TryAddMarker(player);
            }
        }

        private void OnUnregisterPlayer(IPlayer iPlayer)
        {
            var player = iPlayer as Player;
            if (player is null) return;

            var playerId = player.Profile?.Id;
            if (!string.IsNullOrEmpty(playerId))
            {
                TryRemoveMarker(playerId);
            }
        }

        private void RemoveNonActivePlayers()
        {
            // ВАЖНО: Не используем AllAlivePlayersList для проверки, так как в IL2CPP он может временно очищаться.
            // Удаляем маркер только если игрок реально мёртв или его объект уничтожен.
            foreach (var kvp in _playerMarkers.ToList())
            {
                var playerId = kvp.Key;
                var player = kvp.Value.player;

                bool shouldRemove = false;
                try
                {
                    // Проверка на уничтоженный объект (IL2CPP)
                    if (player == null || player.WasCollected)
                    {
                        shouldRemove = true;
                    }
                    else if (player.HasCorpse())
                    {
                        shouldRemove = true;
                    }
                }
                catch
                {
                    // Если при обращении к player вылетело исключение — объект уничтожен
                    shouldRemove = true;
                }

                if (shouldRemove)
                {
                    TryRemoveMarker(playerId);
                }
            }
        }

        public void RefreshMarkers()
        {
            if (!GameUtils.IsInRaid()) return;

            RemoveNonActivePlayers();
            TryAddMarkers();
        }

        private void TryAddMarker(IPlayer iPlayer)
        {
            var player = iPlayer as Player;
            if (player == null || player.IsHeadlessClient())
            {
                return;
            }

            var playerId = player.Profile?.Id;
            if (string.IsNullOrEmpty(playerId))
            {
                return;
            }

            if (_lastMapView is null || player.IsBTRShooter() || _playerMarkers.ContainsKey(playerId))
            {
                return;
            }

            var category = string.Empty;
            var imagePath = string.Empty;
            var color = Color.clear;
            var intelLevel = GameUtils.GetIntelLevel();
            var profile = player.Profile;
            var infoSide = profile?.Info?.Side ?? EPlayerSide.Savage;
            var role = profile?.Info?.Settings?.Role ?? WildSpawnType.assault;

            var isPmc = infoSide == EPlayerSide.Bear
                     || infoSide == EPlayerSide.Usec
                     || role == WildSpawnType.pmcBEAR
                     || role == WildSpawnType.pmcUSEC;

            var isTrackedBoss = player.IsTrackedBoss();

            if (player.IsGroupedWithMainPlayer() && Settings.ShowFriendlyIntelLevel.Value <= intelLevel)
            {
                category = _friendlyPlayerCategory;
                imagePath = _friendlyPlayerImagePath;
                color = _friendlyPlayerColor;
            }
            else if (isTrackedBoss && Settings.ShowBossIntelLevel.Value <= intelLevel)
            {
                category = _bossCategory;
                imagePath = _bossImagePath;
                color = Settings.BossColor.Value;
            }
            else if (isPmc && Settings.ShowPmcIntelLevel.Value <= intelLevel)
            {
                var isBear = role == WildSpawnType.pmcBEAR
                        || infoSide == EPlayerSide.Bear;

                category = _enemyPlayerCategory;
                imagePath = isBear ? _pmcBearImagePath : _pmcUsecImagePath;
                color = isBear
                    ? Settings.PmcBearColor.Value
                    : Settings.PmcUsecColor.Value;
            }
            else if (!isPmc && !isTrackedBoss && Settings.ShowScavIntelLevel.Value <= intelLevel)
            {
                category = _scavCategory;
                imagePath = _scavImagePath;
                color = Settings.ScavColor.Value;
            }

            if (!ShouldShowCategory(category))
            {
                return;
            }

            try
            {
                var marker = _lastMapView.AddPlayerMarker(player, category, color, imagePath);
                _playerMarkers[playerId] = (player, marker);
            }
            catch (Exception e)
            {
                NorvinskMapsPlugin.Log.LogError($"[TryAddMarker] EX {player.Profile?.Nickname ?? "?"}: {e.Message}");
            }
        }

        private void RemoveDisabledMarkers()
        {
            foreach (var key in _playerMarkers.Keys.ToList())
            {
                var marker = _playerMarkers[key].marker;
                if (!ShouldShowCategory(marker.Category))
                {
                    TryRemoveMarker(key);
                }
            }
        }

        private void TryRemoveMarker(string playerId)
        {
            if (!_playerMarkers.ContainsKey(playerId))
            {
                return;
            }

            try
            {
                var entry = _playerMarkers[playerId];
                if (entry.marker != null && entry.marker.ContainingMapView != null)
                {
                    entry.marker.ContainingMapView.RemoveMapMarker(entry.marker);
                }
            }
            catch (Exception e)
            {
                NorvinskMapsPlugin.Log.LogWarning($"[TryRemoveMarker] Ошибка при удалении маркера: {e.Message}");
            }
            finally
            {
                _playerMarkers.Remove(playerId);
            }
        }

        private bool ShouldShowCategory(string category)
        {
            switch (category)
            {
                case _friendlyPlayerCategory: return _showFriendlyPlayers;
                case _enemyPlayerCategory: return _showEnemyPlayers;
                case _bossCategory: return _showBosses;
                case _scavCategory: return _showScavs;
                default: return false;
            }
        }

        private void HandleSetBoolOption(ref bool boolOption, bool value)
        {
            if (value == boolOption) return;

            boolOption = value;

            if (boolOption)
            {
                TryAddMarkers();
            }
            else
            {
                RemoveDisabledMarkers();
            }
        }

        public void OnShowOutOfRaid(MapView map)
        {
        }

        public void OnHideOutOfRaid(MapView map)
        {
        }
    }
}