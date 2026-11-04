using System.Collections.Generic;
using NorvinskMaps.Data;
using NorvinskMaps.DynamicMarkers;
using NorvinskMaps.UI.Components;
using NorvinskMaps.Utils;

namespace NorvinskMaps
{
    public class QuestMarkerProvider : IDynamicMarkerProvider
    {
        private List<MapMarker> _questMarkers = [];
        private MapView _lastMapView;

        public void OnShowInRaid(MapView map)
        {
            _lastMapView = map;

            if (GameUtils.IsScavRaid())
            {
                return;
            }
        }

        public void OnHideInRaid(MapView map)
        {
            TryRemoveMarkers();
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            _lastMapView = map;

            if (!GameUtils.IsInRaid())
            {
                return;
            }
        }

        public void OnRaidEnd(MapView map)
        {
            QuestUtils.DiscardQuestData();
            TryRemoveMarkers();
            _lastMapView = null;
        }

        public void OnDisable(MapView map)
        {
            TryRemoveMarkers();
        }

        public void RefreshMarkers()
        {
            if (!GameUtils.IsInRaid()) return;
            if (_lastMapView == null) return;
            if (GameUtils.IsScavRaid()) return;

            var player = GameUtils.GetMainPlayer();
            if (player == null) return;

            QuestUtils.TryCaptureQuestData();

            var markerDefs = QuestUtils.GetMarkerDefsForPlayer(player);
            if (markerDefs == null)
            {
                return;
            }

            var newMarkers = new List<MapMarker>();
            foreach (var markerDef in markerDefs)
            {
                try
                {
                    var marker = _lastMapView.AddMapMarker(markerDef);
                    if (marker != null)
                        newMarkers.Add(marker);
                }
                catch (System.Exception e)
                {
                    NorvinskMapsPlugin.Log.LogWarning($"[QuestMarkerProvider] AddMapMarker: {e.Message}");
                }
            }

            TryRemoveMarkers();
            _questMarkers = newMarkers;
        }

        private void TryRemoveMarkers()
        {
            foreach (var marker in _questMarkers)
            {
                marker.ContainingMapView.RemoveMapMarker(marker);
            }
            _questMarkers.Clear();
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