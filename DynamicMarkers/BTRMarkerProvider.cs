using NorvinskMaps.Config;
using NorvinskMaps.Data;
using NorvinskMaps.UI.Components;
using NorvinskMaps.Utils;
using UnityEngine;

namespace NorvinskMaps.DynamicMarkers
{
    public class BTRMarkerProvider : IDynamicMarkerProvider
    {
        private static string _btrIconPath = "Markers/btr.png";
        private static Vector2 _btrSize = new(45, 45f);
        private static string _btrName => Localization.T("BTR");
        private static string _btrCategory = "BTR";
        private MapMarker _btrMarker;

        public void OnShowInRaid(MapView map)
        {
            if (_btrMarker is not null)
            {
                return;
            }

            TryAddMarker(map);
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            TryRemoveMarker();
            TryAddMarker(map);
        }

        public void OnRaidEnd(MapView map)
        {
            TryRemoveMarker();
        }

        public void OnDisable(MapView map)
        {
            TryRemoveMarker();
        }

        private void TryAddMarker(MapView map)
        {
            if (_btrMarker != null)
            {
                return;
            }

            var btrView = GameUtils.GetBTRView();
            if (btrView is null)
            {
                return;
            }

            var color = Settings.BtrColor.Value;

            _btrMarker = map.AddTransformMarker(btrView.transform, _btrName, _btrCategory, color,
                                                _btrIconPath, _btrSize);
        }

        private void TryRemoveMarker()
        {
            if (_btrMarker is null)
            {
                return;
            }

            _btrMarker.ContainingMapView.RemoveMapMarker(_btrMarker);
            _btrMarker = null;
        }

        public void OnHideInRaid(MapView map)
        {
            // ничего
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
