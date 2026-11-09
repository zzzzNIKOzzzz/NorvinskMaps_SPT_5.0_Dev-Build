using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppInterop.Runtime.Attributes;
using NorvinskMaps.Data;
using NorvinskMaps.DynamicMarkers;
using NorvinskMaps.UI.Components;
using NorvinskMaps.Utils;

namespace NorvinskMaps
{
    public class QuestMarkerProvider : IDynamicMarkerProvider
    {
        private Dictionary<string, (MapMarkerDef def, MapMarker marker)> _questMarkers = new();
        private MapView _lastMapView;

        public void OnShowInRaid(MapView map)
        {
            _lastMapView = map;
            if (GameUtils.IsScavRaid()) return;
            
            if (_questMarkers.Count == 0 || _questMarkers.Values.Any(v => v.marker == null))
            {
                RefreshMarkers();
            }
        }

        public void OnHideInRaid(MapView map)
        {
            // Ничего не делаем
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            _lastMapView = map;
            if (!GameUtils.IsInRaid()) return;

            TryRemoveMarkers();
            RefreshMarkers();
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
            if (markerDefs == null) return;

            var newDict = new Dictionary<string, (MapMarkerDef, MapMarker)>();
            int created = 0, kept = 0, removed = 0;

            foreach (var def in markerDefs)
            {
                string key = MakeMarkerKey(def);

                if (_questMarkers.TryGetValue(key, out var existing) && existing.marker != null)
                {
                    newDict[key] = existing;
                    kept++;
                    continue;
                }

                try
                {
                    var marker = _lastMapView.AddMapMarker(def);
                    if (marker != null)
                    {
                        newDict[key] = (def, marker);
                        created++;
                    }
                }
                catch (Exception e)
                {
                    NorvinskMapsPlugin.Log.LogWarning($"[QuestMarkerProvider] AddMapMarker: {e.Message}");
                }
            }

            foreach (var kvp in _questMarkers)
            {
                if (newDict.ContainsKey(kvp.Key)) continue;
                if (kvp.Value.marker == null) continue;

                try
                {
                    if (kvp.Value.marker.ContainingMapView != null)
                        kvp.Value.marker.ContainingMapView.RemoveMapMarker(kvp.Value.marker);
                    removed++;
                }
                catch { }
            }

            _questMarkers = newDict;

            if (created > 0 || removed > 0)
            {
                NorvinskMapsPlugin.Log.LogDebug(
                    $"[QuestMarkerProvider] diff: +{created} -{removed} ={kept}");
            }
        }

        [HideFromIl2Cpp]
        private static string MakeMarkerKey(MapMarkerDef def)
        {
            if (def == null) return "null";
            string text = def.Text ?? "";
            var p = def.Position;
            return $"{text}|{(int)Math.Round(p.x)}|{(int)Math.Round(p.y)}|{(int)Math.Round(p.z)}";
        }

        private void TryRemoveMarkers()
        {
            foreach (var kvp in _questMarkers)
            {
                if (kvp.Value.marker?.ContainingMapView == null) continue;
                try { kvp.Value.marker.ContainingMapView.RemoveMapMarker(kvp.Value.marker); }
                catch { }
            }
            _questMarkers.Clear();
        }

        public void OnShowOutOfRaid(MapView map) { }
        public void OnHideOutOfRaid(MapView map) { }
    }
}