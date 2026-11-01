using NorvinskMaps.Data;
using NorvinskMaps.UI.Components;
using NorvinskMaps.Utils;
using EFT;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace NorvinskMaps
{
    public class PlayerDotSpawner : MonoBehaviour
    {
        private static float _spawnTime = 0.25f;
        private float _timeAccumulator = 0f;

        [HideFromIl2Cpp]
        public MapView MapView { get; set; }

        private void Update()
        {
            if (MapView == null)
            {
                return;
            }

            if (!Input.GetKey(KeyCode.M) || !Input.GetKey(KeyCode.LeftShift))
            {
                return;
            }

            _timeAccumulator += Time.deltaTime;
            if (_timeAccumulator <= _spawnTime && !Input.GetKeyDown(KeyCode.M))
            {
                return;
            }

            var markerDef = new MapMarkerDef
            {
                ImagePath = "Markers/dot.png",
                Position = MathUtils.ConvertToMapPosition(((IPlayer)GameUtils.GetMainPlayer()).Position)
            };

            MapView.AddMapMarker(markerDef);
            _timeAccumulator = 0f;
        }
    }
}