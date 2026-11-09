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
    public class CorpseMarkerProvider : IDynamicMarkerProvider
    {
        private const string _skullImagePath = "Markers/skull.png";

        private const string _friendlyCorpseCategory = "Friendly Corpse";
        private static Color _friendlyCorpseColor = Color.Lerp(Color.blue, Color.white, 0.5f);

        private const string _killedCorpseCategory = "Killed Corpse";
        private const string _killedBossCorpseCategory = "Killed Boss Corpse";
        private const string _bossCorpseCategory = "Boss Corpse";
        private const string _friendlyKilledCorpseCategory = "Friendly Killed Corpse";
        private const string _friendlyKilledBossCorpseCategory = "Friendly Killed Boss Corpse";
        private const string _otherCorpseCategory = "Other Corpse";
        private static Color _friendlyKilledCorpseColor = Color.Lerp(_friendlyCorpseColor, Color.red, 0.5f);

        private bool _showFriendlyCorpses = true;
        public bool ShowFriendlyCorpses { get => _showFriendlyCorpses; set => HandleSetBoolOption(ref _showFriendlyCorpses, value); }

        private bool _showKilledCorpses = true;
        public bool ShowKilledCorpses { get => _showKilledCorpses; set => HandleSetBoolOption(ref _showKilledCorpses, value); }

        private bool _showFriendlyKilledCorpses = true;
        public bool ShowFriendlyKilledCorpses { get => _showFriendlyKilledCorpses; set => HandleSetBoolOption(ref _showFriendlyKilledCorpses, value); }

        private bool _showBossCorpses = false;
        public bool ShowBossCorpses { get => _showBossCorpses; set => HandleSetBoolOption(ref _showBossCorpses, value); }

        private bool _showOtherCorpses = false;
        public bool ShowOtherCorpses { get => _showOtherCorpses; set => HandleSetBoolOption(ref _showOtherCorpses, value); }

        private MapView _lastMapView;
        private readonly Dictionary<Player, MapMarker> _corpseMarkers = new();
        private readonly Queue<Player> _pendingCorpses = new();
        private readonly HashSet<Player> _pendingCorpsesSet = new();
        private float _processTimer;
        private const float ProcessInterval = 0.05f;

        public void OnShowInRaid(MapView map)
        {
            _lastMapView = map;
            TryAddMarkers();  // существующие трупы — по одному в TryAddMarker (быстро в первый раз)

            GameWorldUnregisterPlayerPatch.OnUnregisterPlayer -= OnUnregisterPlayer;
            GameWorldUnregisterPlayerPatch.OnUnregisterPlayer += OnUnregisterPlayer;
        }

        public void OnHideInRaid(MapView map)
        {
            GameWorldUnregisterPlayerPatch.OnUnregisterPlayer -= OnUnregisterPlayer;
        }

        public void OnRaidEnd(MapView map)
        {
            GameWorldUnregisterPlayerPatch.OnUnregisterPlayer -= OnUnregisterPlayer;
            _pendingCorpses.Clear();
            _pendingCorpsesSet.Clear();
            _processTimer = 0f;
            _lastMapView = map;
            TryRemoveMarkers();
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            _lastMapView = map;

            foreach (var corpse in _corpseMarkers.Keys.ToList())
            {
                TryRemoveMarker(corpse);
                TryAddMarker(corpse);
            }
        }

        public void OnDisable(MapView map)
        {
            GameWorldUnregisterPlayerPatch.OnUnregisterPlayer -= OnUnregisterPlayer;
            _pendingCorpses.Clear();
            _pendingCorpsesSet.Clear();
            _processTimer = 0f;
            TryRemoveMarkers();
        }

        public void ProcessQueue(float deltaTime)
        {
            if (_pendingCorpses.Count == 0) return;

            _processTimer -= deltaTime;
            if (_processTimer > 0f) return;
            _processTimer = ProcessInterval;

            if (!GameUtils.IsInRaid() || _lastMapView == null)
            {
                _pendingCorpses.Clear();
                _pendingCorpsesSet.Clear();
                return;
            }

            var corpse = _pendingCorpses.Dequeue();
            _pendingCorpsesSet.Remove(corpse);
            TryAddMarker(corpse);
        }

        private void OnUnregisterPlayer(IPlayer iPlayer)
        {
            if (iPlayer is not Player player) return;
            if (player == null || !player.HasCorpse()) return;

            // Быстро: только enqueue, ~0.05 мс
            if (!_corpseMarkers.ContainsKey(player) && _pendingCorpsesSet.Add(player))
                _pendingCorpses.Enqueue(player);
        }

        private void TryAddMarkers()
        {
            if (!GameUtils.IsInRaid()) return;

            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null) return;

            foreach (var p in gameWorld.AllPlayersEverExisted)
            {
                if (p != null && p.HasCorpse() && !_corpseMarkers.ContainsKey(p))
                    TryAddMarker(p);
            }
        }

        public void RefreshMarkers()
        {
            if (!GameUtils.IsInRaid()) return;

            foreach (var marker in _corpseMarkers.ToArray())
            {
                if (marker.Key == null || marker.Value == null)
                    TryRemoveMarker(marker.Key);
            }

            TryAddMarkers();
        }

        private void TryRemoveMarkers()
        {
            foreach (var corpse in _corpseMarkers.Keys.ToList())
                TryRemoveMarker(corpse);
            _corpseMarkers.Clear();
        }

        private void TryAddMarker(Player player)
        {
            if (_lastMapView is null || player == null || _corpseMarkers.ContainsKey(player))
                return;

            var intelLevel = GameUtils.GetIntelLevel();
            if (Settings.ShowCorpseIntelLevel.Value > intelLevel) return;

            var category = _otherCorpseCategory;
            var color = Settings.KilledOtherColor.Value;

            if (player.IsGroupedWithMainPlayer())
            {
                category = _friendlyCorpseCategory;
                color = _friendlyCorpseColor;
            }
            else if (player.IsTrackedBoss() && player.DidMainPlayerKill())
            {
                category = _killedBossCorpseCategory;
                color = Settings.KilledBossColor.Value;
            }
            else if (player.DidMainPlayerKill())
            {
                category = _killedCorpseCategory;
                color = Settings.KilledCorpseColor.Value;
            }
            else if (player.IsTrackedBoss() && player.DidTeammateKill())
            {
                category = _friendlyKilledBossCorpseCategory;
                color = Settings.KilledBossColor.Value;
            }
            else if (player.DidTeammateKill())
            {
                category = _friendlyKilledCorpseCategory;
                color = _friendlyKilledCorpseColor;
            }
            else if (player.IsTrackedBoss())
            {
                category = _bossCorpseCategory;
                color = Settings.KilledBossColor.Value;
            }

            if (!ShouldShowCategory(category)) return;

            var markerDef = new MapMarkerDef
            {
                Category = category,
                ImagePath = _skullImagePath,
                Text = player.Profile.GetCorrectedNickname(),
                Color = color,
                Position = MathUtils.ConvertToMapPosition(((IPlayer)player).Position)
            };

            try
            {
                var marker = _lastMapView.AddMapMarker(markerDef);
                _corpseMarkers[player] = marker;
            }
            catch (Exception e)
            {
                NorvinskMapsPlugin.Log.LogWarning($"[CorpseMarkerProvider] Error adding marker: {e.Message}");
            }
        }

        private void RemoveDisabledMarkers()
        {
            foreach (var corpse in _corpseMarkers.Keys.ToList())
            {
                var marker = _corpseMarkers[corpse];
                if (!ShouldShowCategory(marker.Category))
                    TryRemoveMarker(corpse);
            }
        }

        private void TryRemoveMarker(Player player)
        {
            if (player == null || !_corpseMarkers.ContainsKey(player)) return;

            if (_corpseMarkers[player] != null && _corpseMarkers[player].ContainingMapView != null)
                _corpseMarkers[player].ContainingMapView.RemoveMapMarker(_corpseMarkers[player]);
            _corpseMarkers.Remove(player);
        }

        private bool ShouldShowCategory(string category)
        {
            switch (category)
            {
                case _friendlyCorpseCategory: return _showFriendlyCorpses;
                case _killedCorpseCategory:
                case _killedBossCorpseCategory: return _showKilledCorpses;
                case _friendlyKilledCorpseCategory:
                case _friendlyKilledBossCorpseCategory: return _showFriendlyKilledCorpses;
                case _bossCorpseCategory: return _showBossCorpses;
                case _otherCorpseCategory: return _showOtherCorpses;
                default: return false;
            }
        }

        private void HandleSetBoolOption(ref bool boolOption, bool value)
        {
            if (value == boolOption) return;
            boolOption = value;
            if (boolOption) TryAddMarkers();
            else RemoveDisabledMarkers();
        }

        public void OnShowOutOfRaid(MapView map) { }
        public void OnHideOutOfRaid(MapView map) { }
    }
}