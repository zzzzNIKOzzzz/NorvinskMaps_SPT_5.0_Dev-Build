using System.IO;
using System.Linq;
using NorvinskMaps.Data;
using NorvinskMaps.UI.Components;
using NorvinskMaps.Utils;
using EFT.InventoryLogic;
using UnityEngine;

namespace NorvinskMaps.DynamicMarkers
{
    public class LockedDoorMarkerMutator : IDynamicMarkerProvider
    {
        private static string doorWithKeyPath = Path.Combine(NorvinskMapsPlugin.Path, "Markers/door_with_key.png");
        private static string doorWithLockPath = Path.Combine(NorvinskMapsPlugin.Path, "Markers/door_with_lock.png");

        public void OnShowInRaid(MapView map)
        {
            var player = GameUtils.GetMainPlayer();

            if (player == null) return;
            if (player.Inventory == null) return;
            if (player.Inventory.Equipment == null) return;

            var markers = map.GetMapMarkersByCategory("LockedDoor");

            foreach (var marker in markers)
            {
                if (string.IsNullOrWhiteSpace(marker.AssociatedItemId))
                {
                    continue;
                }

                var hasKey = false;
                foreach (var item in player.Inventory.Equipment.GetAllItems())
                {
                    if (item.TemplateId == marker.AssociatedItemId)
                    {
                        hasKey = true;
                        break;
                    }
                }

                marker.Image.sprite = hasKey
                    ? TextureUtils.GetOrLoadCachedSprite(doorWithKeyPath)
                    : TextureUtils.GetOrLoadCachedSprite(doorWithLockPath);

                marker.Color = hasKey
                    ? Color.green
                    : Color.red;
            }
        }

        public void OnShowOutOfRaid(MapView map)
        {
            var profile = GameUtils.GetPlayerProfile();

            if (profile == null) return;
            if (profile.Inventory == null) return;

            var markers = map.GetMapMarkersByCategory("LockedDoor");

            foreach (var marker in markers)
            {
                if (string.IsNullOrWhiteSpace(marker.AssociatedItemId))
                {
                    continue;
                }

                var keyInStash = false;
                if (profile.Inventory.Stash != null)
                {
                    foreach (var i in profile.Inventory.Stash.GetAllItems())
                    {
                        if (i.TemplateId == marker.AssociatedItemId) { keyInStash = true; break; }
                    }
                }

                var keyInEquipment = false;
                if (profile.Inventory.Equipment != null)
                {
                    foreach (var i in profile.Inventory.Equipment.GetAllItems())
                    {
                        if (i.TemplateId == marker.AssociatedItemId) { keyInEquipment = true; break; }
                    }
                }

                marker.Image.sprite = (keyInStash || keyInEquipment)
                    ? TextureUtils.GetOrLoadCachedSprite(doorWithKeyPath)
                    : TextureUtils.GetOrLoadCachedSprite(doorWithLockPath);

                marker.Color = keyInEquipment
                    ? Color.green
                    : keyInStash
                        ? Color.yellow
                        : Color.red;
            }
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            if (map == null || mapDef == null) return;

            if (GameUtils.IsInRaid())
            {
                OnShowInRaid(map);
            }
            else
            {
                OnShowOutOfRaid(map);
            }
        }

        public void OnDisable(MapView map)
        {
            if (map == null) return;

            var markers = map.GetMapMarkersByCategory("LockedDoor");

            foreach (var marker in markers)
            {
                if (string.IsNullOrWhiteSpace(marker.AssociatedItemId))
                {
                    continue;
                }

                marker.Image.sprite = TextureUtils.GetOrLoadCachedSprite(doorWithLockPath);
                marker.Color = Color.yellow;
            }
        }

        public void OnRaidEnd(MapView map)
        {
            // ничего
        }

        public void OnHideInRaid(MapView map)
        {
            // ничего
        }

        public void OnHideOutOfRaid(MapView map)
        {
            // ничего
        }
    }
}