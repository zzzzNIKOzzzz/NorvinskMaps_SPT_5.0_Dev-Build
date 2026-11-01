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

            AddQuestObjectiveMarkers(map);
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

            TryRemoveMarkers();
            AddQuestObjectiveMarkers(map);
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

            TryRemoveMarkers();
            AddQuestObjectiveMarkers(_lastMapView);
        }

        private void AddQuestObjectiveMarkers(MapView map)
        {
            QuestUtils.TryCaptureQuestData();

            var player = GameUtils.GetMainPlayer();
            if (player == null)
            {
                return;
            }

            var markerDefs = QuestUtils.GetMarkerDefsForPlayer(player);

            foreach (var markerDef in markerDefs)
            {
                var marker = map.AddMapMarker(markerDef);
                _questMarkers.Add(marker);
            }
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