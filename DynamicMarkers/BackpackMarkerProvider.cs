using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using NorvinskMaps.Config;
using NorvinskMaps.Data;
using NorvinskMaps.Patches;
using NorvinskMaps.UI.Components;
using NorvinskMaps.Utils;
using EFT;
using EFT.Interactive;
using EFT.UI.DragAndDrop;
using UnityEngine;

namespace NorvinskMaps.DynamicMarkers
{
    public class BackpackMarkerProvider : IDynamicMarkerProvider
    {
        private const string _backpackCategory = "Backpack";
        private const string _backpackImagePath = "Markers/backpack.png";
        private static Vector2 _backpackSize = new Vector2(30f, 30f);

        private MapView _lastMapView;
        private Dictionary<int, MapMarker> _backpackMarkers = new Dictionary<int, MapMarker>();

        public void OnShowInRaid(MapView map)
        {
            _lastMapView = map;

            RemoveStaleMarkers();
            AddThrownBackpacks();

            LootItemInitPatch.OnRegisterLoot += OnRegisterLoot;
            GameWorldDestroyLootPatch.OnDestroyLoot += OnDestroyLoot;
        }

        public void OnHideInRaid(MapView map)
        {
            LootItemInitPatch.OnRegisterLoot -= OnRegisterLoot;
            GameWorldDestroyLootPatch.OnDestroyLoot -= OnDestroyLoot;
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            _lastMapView = map;
            var gameWorld = Singleton<GameWorld>.Instance;

            foreach (var itemNetId in _backpackMarkers.Keys.ToList())
            {
                TryRemoveMarker(itemNetId);

                if (!gameWorld.LootItems.ContainsKey(itemNetId))
                {
                    continue;
                }

                TryAddMarker(map, gameWorld.LootItems.GetByKey(itemNetId));
            }
        }

        public void OnRaidEnd(MapView map)
        {
            LootItemInitPatch.OnRegisterLoot -= OnRegisterLoot;
            GameWorldDestroyLootPatch.OnDestroyLoot -= OnDestroyLoot;
            TryRemoveMarkers();
        }

        public void OnDisable(MapView map)
        {
            LootItemInitPatch.OnRegisterLoot -= OnRegisterLoot;
            GameWorldDestroyLootPatch.OnDestroyLoot -= OnDestroyLoot;
            TryRemoveMarkers();
        }

        private void AddThrownBackpacks()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            foreach (var pair in PlayerInventoryThrowItemPatch.ThrownItems)
            {
                var itemNetId = pair.Key;
                var item = pair.Value;
                var itemType = ItemViewFactory.GetItemType(Il2CppSystem.Type.GetType(item.GetType().FullName));

                if (_backpackMarkers.ContainsKey(itemNetId)
                    || itemType != EItemType.Backpack
                    || !gameWorld.LootItems.ContainsKey(itemNetId))
                {
                    continue;
                }

                TryAddMarker(_lastMapView, gameWorld.LootItems.GetByKey(itemNetId));
            }
        }

        private void OnRegisterLoot(LootItem lootItem)
        {
            if (lootItem is null || lootItem.Item is null)
            {
                return;
            }

            var itemNetId = lootItem.GetNetId();
            var itemType = ItemViewFactory.GetItemType(Il2CppSystem.Type.GetType(lootItem.Item.GetType().FullName));

            if (_backpackMarkers.ContainsKey(itemNetId)
                || itemType != EItemType.Backpack
                || !PlayerInventoryThrowItemPatch.ThrownItems.ContainsKey(itemNetId))
            {
                return;
            }

            TryAddMarker(_lastMapView, lootItem);
        }

        private void OnDestroyLoot(LootItem lootItem)
        {
            if (lootItem is null || lootItem.Item is null)
            {
                return;
            }

            TryRemoveMarker(lootItem.GetNetId());
        }

        private void RemoveStaleMarkers()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            foreach (var itemNetId in _backpackMarkers.Keys.ToList())
            {
                if (gameWorld.LootItems.ContainsKey(itemNetId))
                {
                    continue;
                }

                TryRemoveMarker(itemNetId);
            }
        }

        private void TryAddMarker(MapView map, LootItem lootItem)
        {
            if (lootItem is null || lootItem.Item is null)
            {
                return;
            }

            var itemNetId = lootItem.GetNetId();
            if (_backpackMarkers.ContainsKey(itemNetId))
            {
                return;
            }

            var color = Settings.BackpackColor.Value;
            var marker = map.AddTransformMarker(lootItem.TrackableTransform, lootItem.Item.ShortName.BSGLocalized(),
                                                _backpackCategory, color, _backpackImagePath, _backpackSize);

            _backpackMarkers[itemNetId] = marker;
        }

        private void TryRemoveMarker(int itemNetId)
        {
            if (!_backpackMarkers.ContainsKey(itemNetId))
            {
                return;
            }

            _backpackMarkers[itemNetId].ContainingMapView.RemoveMapMarker(_backpackMarkers[itemNetId]);
            _backpackMarkers.Remove(itemNetId);
        }

        private void TryRemoveMarkers()
        {
            foreach (var itemNetId in _backpackMarkers.Keys.ToList())
            {
                TryRemoveMarker(itemNetId);
            }
            _backpackMarkers.Clear();
        }

        public void OnHideOutOfRaid(MapView map)
        {
            // ничего
        }

        public void OnShowOutOfRaid(MapView map)
        {
            // ничего
        }
    }
}