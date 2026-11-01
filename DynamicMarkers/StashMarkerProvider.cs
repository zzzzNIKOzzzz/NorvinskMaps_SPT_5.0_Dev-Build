using System.Collections.Generic;
using System.Linq;
using NorvinskMaps.Config;
using NorvinskMaps.Data;
using NorvinskMaps.Patches;
using NorvinskMaps.UI.Components;
using NorvinskMaps.Utils;
using EFT.Interactive;

namespace NorvinskMaps.DynamicMarkers;

public class HiddenStashMarkerProvider : IDynamicMarkerProvider
{
    private MapView _lastMapView;
    private Dictionary<LootableContainer, MapMarker> _stashMarkers = [];
    private const string _hiddenCacheImagePath = "Markers/stash.png";
    
    public void OnShowInRaid(MapView map)
    {
        _lastMapView = map;

        foreach (var stash in GameStartedPatch.HiddenStashes)
        {
            TryAddMarker(stash);
        }
    }

    public void OnHideInRaid(MapView map)
    {
        // ничего
    }

    public void OnRaidEnd(MapView map)
    {
        TryRemoveMarkers();
    }

    public void OnMapChanged(MapView map, MapDef mapDef)
    {
        _lastMapView = map;

        foreach (var stash in _stashMarkers.Keys.ToList())
        {
            TryRemoveMarker(stash);
            TryAddMarker(stash);
        }
    }

    public void OnDisable(MapView map)
    {
        OnRaidEnd(map);
    }

    public void RefreshMarkers()
    {
        if (!GameUtils.IsInRaid()) return;

        foreach (var stash in _stashMarkers.Keys.ToList())
        {
            TryRemoveMarker(stash);
            TryAddMarker(stash);
        }
    }

    private void TryAddMarker(LootableContainer stash)
    {
        if (_stashMarkers.ContainsKey(stash)) return;
        if (Settings.ShowHiddenStashIntelLevel.Value > GameUtils.GetIntelLevel()) return;

        var markerDef = new MapMarkerDef
        {
            Category = "Hidden Stash",
            Color = Settings.HiddenStashColor.Value,
            ImagePath = _hiddenCacheImagePath,
            Position = MathUtils.ConvertToMapPosition(stash.transform),
            Text = Localization.T("HiddenStash")
        };

        var marker = _lastMapView.AddMapMarker(markerDef);
        _stashMarkers[stash] = marker;
    }

    private void TryRemoveMarkers()
    {
        foreach (var stash in _stashMarkers.Keys.ToList())
        {
            TryRemoveMarker(stash);
        }
    }

    private void TryRemoveMarker(LootableContainer stash)
    {
        if (!_stashMarkers.ContainsKey(stash)) return;
        
        _stashMarkers[stash].ContainingMapView.RemoveMapMarker(_stashMarkers[stash]);
        _stashMarkers.Remove(stash);
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