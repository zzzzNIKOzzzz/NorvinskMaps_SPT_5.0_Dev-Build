using System.Collections.Generic;
using System.Linq;
using NorvinskMaps.Config;
using NorvinskMaps.Data;
using NorvinskMaps.Patches;
using NorvinskMaps.UI.Components;
using NorvinskMaps.Utils;
using EFT.SynchronizableObjects;
using UnityEngine;

namespace NorvinskMaps.DynamicMarkers
{
    public class AirdropMarkerProvider : IDynamicMarkerProvider
    {
        private MapView _lastMapView;
        private Dictionary<AirdropSynchronizableObject, MapMarker> _airdropMarkers = [];

        private static string _airdropName => Localization.T("Airdrop");
        private const string _airdropCategory = "Airdrop";
        private const string _airdropImagePath = "Markers/airdrop.png";
        private static Vector2 _airdropPivot = new Vector2(0.5f, 0.25f);
        //

        public void OnShowInRaid(MapView map)
        {
            _lastMapView = map;
            
            foreach (var airdrop in AirdropBoxOnBoxLandPatch.Airdrops)
            {
                TryAddMarker(airdrop);
            }

            AirdropBoxOnBoxLandPatch.OnAirdropLanded += TryAddMarker;
        }

        public void OnHideInRaid(MapView map)
        {
            AirdropBoxOnBoxLandPatch.OnAirdropLanded -= TryAddMarker;
        }

        public void OnRaidEnd(MapView map)
        {
            TryRemoveMarkers();
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            _lastMapView = map;
            foreach (var airdrop in _airdropMarkers.Keys.ToList())
            {
                TryRemoveMarker(airdrop);
                TryAddMarker(airdrop);
            }
        }

        public void OnDisable(MapView map)
        {
            OnRaidEnd(map);
        }

        public void RefreshMarkers()
        {
            if (!GameUtils.IsInRaid()) return;

            foreach (var drop in _airdropMarkers.ToArray())
            {
                TryRemoveMarker(drop.Key);
            }

            foreach (var drop in AirdropBoxOnBoxLandPatch.Airdrops)
            {
                TryAddMarker(drop);
            }
        }
        
        private void TryAddMarker(AirdropSynchronizableObject airdrop)
        {
            if (_airdropMarkers.ContainsKey(airdrop))
            {
                return;
            }

            var intelLevel = GameUtils.GetIntelLevel();

            if (Settings.ShowAirdropIntelLevel.Value > intelLevel) return;
            
            var markerDef = new MapMarkerDef
            {
                Category = _airdropCategory,
                Color = Settings.AirdropColor.Value,
                ImagePath = _airdropImagePath,
                Position = MathUtils.ConvertToMapPosition(airdrop.CollisionCollider.transform),
                Pivot = _airdropPivot,
                Text = _airdropName
            };

            var marker = _lastMapView.AddMapMarker(markerDef);
            _airdropMarkers[airdrop] = marker;
        }

        private void TryRemoveMarkers()
        {
            foreach (var airdrop in _airdropMarkers.Keys.ToList())
            {
                TryRemoveMarker(airdrop);
            }
        }

        private void TryRemoveMarker(AirdropSynchronizableObject airdrop)
        {
            if (!_airdropMarkers.ContainsKey(airdrop))
            {
                return;
            }

            _airdropMarkers[airdrop].ContainingMapView.RemoveMapMarker(_airdropMarkers[airdrop]);
            _airdropMarkers.Remove(airdrop);
        }

        public void OnShowOutOfRaid(MapView map)
        {
            // ничего
        }

        public void OnHideOutOfRaid(MapView map)
        {
            // ничего
        }
    }
}
