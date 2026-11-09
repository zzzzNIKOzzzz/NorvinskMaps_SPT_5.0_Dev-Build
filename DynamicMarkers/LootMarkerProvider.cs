using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using NorvinskMaps.Config;
using NorvinskMaps.Data;
using NorvinskMaps.UI.Components;
using NorvinskMaps.Utils;
using EFT;
using EFT.Interactive;
using EFT.UI.DragAndDrop;

namespace NorvinskMaps.DynamicMarkers
{
    public class LootMarkerProvider : IDynamicMarkerProvider
    {
        private MapView _lastMapView;
        private Dictionary<LootItem, MapMarker> _lootMarkers = [];

        public void OnShowInRaid(MapView map)
        {
            _lastMapView = map;
            var wishSet = GameUtils.GetWishListSet();
            if (wishSet.Count == 0) return;

            var lootList = Singleton<GameWorld>.Instance.LootList;
            foreach (var item in lootList)
            {
                if (item is not LootItem loot) continue;

                if (wishSet.Contains(new MongoID(loot.TemplateId)))
                    TryAddMarker(loot);
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

            foreach (var item in _lootMarkers.Keys.ToList())
            {
                TryRemoveMarker(item);
                TryAddMarker(item);
            }
        }

        public void OnDisable(MapView map)
        {
            OnRaidEnd(map);
        }

        public void RefreshMarkers()
        {
            if (!GameUtils.IsInRaid()) return;

            foreach (var item in _lootMarkers.Keys.ToList())
            {
                TryRemoveMarker(item);
                TryAddMarker(item);
            }
        }

        private void TryAddMarker(LootItem item)
        {
            if (item == null || item.Item == null)
                return;

            if (_lootMarkers.ContainsKey(item)) return;
            if (Settings.ShowWishListItemsIntelLevel.Value > GameUtils.GetIntelLevel()) return;

            var staticIcons = EFTHardSettings.Instance.StaticIcons;
            var itemType = ItemViewFactory.GetItemType(Il2CppSystem.Type.GetType(item.Item.GetType().FullName));
            var itemSprite = staticIcons.ItemTypeSprites.GetValueOrDefault(itemType);

            var transform = item.transform;
            if (transform == null)
                return;

            var markerDef = new MapMarkerDef
            {
                Category = "Loot",
                Color = Settings.LootItemColor.Value,
                Sprite = itemSprite,
                Position = MathUtils.ConvertToMapPosition(transform),
                Text = item.Item.TemplateId.LocalizedName()
            };

            var marker = _lastMapView.AddMapMarker(markerDef);
            _lootMarkers[item] = marker;
        }

        private void TryRemoveMarkers()
        {
            foreach (var item in _lootMarkers.Keys.ToList())
            {
                TryRemoveMarker(item);
            }
        }

        private void TryRemoveMarker(LootItem item)
        {
            if (!_lootMarkers.ContainsKey(item)) return;

            _lootMarkers[item].ContainingMapView.RemoveMapMarker(_lootMarkers[item]);
            _lootMarkers.Remove(item);
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