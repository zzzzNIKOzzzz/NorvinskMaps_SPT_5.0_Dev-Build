using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP.Configuration;
using NorvinskMaps.Config;
using NorvinskMaps.Utils;
using EFT;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace NorvinskMaps.UI.Components
{
    internal class MapPeekComponent : MonoBehaviour
    {
        [HideFromIl2Cpp]
        public ModdedMapScreen MapScreen { get; set; }

        public RectTransform MapScreenTrueParent { get; set; }
        public RectTransform RectTransform { get; private set; }

        [HideFromIl2Cpp]
        public KeyboardShortcut PeekShortcut { get; set; }

        [HideFromIl2Cpp]
        public KeyboardShortcut HideMinimapShortcut { get; set; }

        public bool HoldForPeek { get; set; }
        public bool IsPeeking { get; set; }
        private static bool IsMiniMapEnabled => Settings.MiniMapEnabled.Value;
        public bool ShowingMiniMap { get; private set; }
        public bool WasMiniMapActive { get; set; }
        private bool IsMiniMapHidden = false;

        [HideFromIl2Cpp]
        internal static MapPeekComponent Create(GameObject parent)
        {
            var go = UIUtils.CreateUIGameObject(parent, "MapPeek");
            if (go == null) return null;
            go.SetActive(true);

            var rectTransform = go.GetRectTransform();
            if (rectTransform == null) return null;

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;

            var component = go.AddComponent<MapPeekComponent>();
            return component;
        }

        private void Awake()
        {
            RectTransform = gameObject.GetRectTransform();
            if (RectTransform == null)
            {
                Debug.LogError("RectTransform not found!");
                enabled = false;
            }
        }

        private void Update()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (!GameUtils.ShouldShowMapInRaid())
            {
                EndAllStates();
                return;
            }

            HandleMinimapState();
            HandlePeekState();
        }

        [HideFromIl2Cpp]
        private void EndAllStates()
        {
            if (ShowingMiniMap) EndMiniMap();
            if (IsPeeking) EndPeek();
        }

        [HideFromIl2Cpp]
        private void HandleMinimapState()
        {
            if (!IsMiniMapEnabled)
            {
                if (ShowingMiniMap) EndMiniMap();
                WasMiniMapActive = false;
                return;
            }

            if (HideMinimapShortcut.BetterIsDown())
            {
                IsMiniMapHidden = !IsMiniMapHidden;
                ToggleMiniMap();
                return;
            }

            if (IsMiniMapHidden) return;

            var shouldShowMiniMap = !IsPeeking && !(MapScreen?.IsMapScreenShownFullscreen ?? false);

            if (shouldShowMiniMap && !ShowingMiniMap)
            {
                BeginMiniMap();
            }
            else if (!shouldShowMiniMap && ShowingMiniMap)
            {
                EndMiniMap();
            }
        }

        [HideFromIl2Cpp]
        private void ToggleMiniMap()
        {
            if (!IsMiniMapHidden && !ShowingMiniMap)
            {
                BeginMiniMap(false);
            }
            else
            {
                EndMiniMap();
            }
        }

        [HideFromIl2Cpp]
        private void HandlePeekState()
        {
            if (HoldForPeek)
            {
                HandleHoldPeekState();
            }
            else if (PeekShortcut.BetterIsDown())
            {
                HandleTogglePeekState();
            }
        }

        [HideFromIl2Cpp]
        private void HandleHoldPeekState()
        {
            if (PeekShortcut.BetterIsPressed() != IsPeeking)
            {
                if (PeekShortcut.BetterIsPressed())
                {
                    BeginPeek();
                }
                else
                {
                    EndPeek();
                }
            }
        }

        [HideFromIl2Cpp]
        private void HandleTogglePeekState()
        {
            if (!IsPeeking)
            {
                BeginPeek();
            }
            else
            {
                EndPeek();
            }
        }

        [HideFromIl2Cpp]
        private void BeginPeek(bool playAnimation = true)
        {
            if (IsPeeking) return;
            if (MapScreen == null || RectTransform == null) return;

            if (transform != null) transform.SetAsLastSibling();

            IsPeeking = true;
            WasMiniMapActive = ShowingMiniMap;

            MapScreen.SaveMainMapPos();
            EndMiniMap();

            if (MapScreen.transform != null && RectTransform != null)
                MapScreen.transform.SetParent(RectTransform, false);

            if (MapScreen.MapView != null && MapScreen.MapView.RectTransform != null)
            {
                MapScreen.MapView.RectTransform.anchoredPosition = Vector2.zero;
                MapScreen.MapView.RectTransform.localScale = Vector3.one;
            }

            if (MapScreen != null)
                MapScreen.Show(playAnimation);
        }

        [HideFromIl2Cpp]
        internal void EndPeek()
        {
            if (!IsPeeking) return;
            IsPeeking = false;

            MapScreen?.Hide(false);

            if (MapScreen != null && !MapScreen.IsShowingMapScreen)
            {
                MapScreen.ResetFullscreenFlag();
            }

            if (MapScreen != null && MapScreen.transform != null)
            {
                var target = MapScreen?.SavedTrueParent ?? NorvinskMapsPlugin.MapScreenTrueParent ?? MapScreenTrueParent;
                if (target != null && target.gameObject != null)
                {
                    MapScreen.transform.SetParent(target, false);
                }
            }

            if (WasMiniMapActive && !IsMiniMapHidden)
                BeginMiniMap();
        }

        [HideFromIl2Cpp]
        internal void BeginMiniMap(bool playAnimation = true)
        {
            if (ShowingMiniMap) return;
            if (MapScreen == null || RectTransform == null) return;

            ShowingMiniMap = true;

            if (MapScreen?.MapView != null)
            {
                MapScreen.MapView.IsMiniMapActive = true;
                MapScreen.MapView.RestoreBaseSizeDelta();

                var savedZoomMini = MapScreen.MapView.ZoomMini;

                MapScreen.MapView.ApplyMiniMapZoom();
                MapScreen.MapView.ZoomMini = savedZoomMini;
                MapScreen.MapView.ApplyZoomExternal(savedZoomMini);
            }

            if (transform != null)
                transform.SetAsLastSibling();

            if (MapScreen.transform != null && RectTransform != null)
                MapScreen.transform.SetParent(RectTransform, false);

            if (MapScreen != null)
                MapScreen.Show(playAnimation);

            try
            {
                var player = GameUtils.GetMainPlayer();
                if (player != null && MapScreen?.MapView != null)
                {
                    var mapPosition = MathUtils.ConvertToMapPosition(player.Position);
                    MapScreen.MapView.ShiftMapToCoordinate(mapPosition, 0f, true);
                }
            }
            catch (System.Exception e)
            {
                NorvinskMapsPlugin.Log.LogError($"[BeginMiniMap] CenterOnPlayer: {e.Message}");
            }
        }

        [HideFromIl2Cpp]
        internal void ForceExitMiniMap()
        {
            if (!ShowingMiniMap) return;

            ShowingMiniMap = false;

            if (MapScreen?.MapView != null)
            {
                MapScreen.MapView.IsMiniMapActive = false;
            }
        }

        [HideFromIl2Cpp]
        internal void EndMiniMap()
        {
            if (!ShowingMiniMap) return;

            ShowingMiniMap = false;

            if (MapScreen?.MapView != null)
            {
                MapScreen.MapView.IsMiniMapActive = false;
            }

            if (MapScreen != null && MapScreen.IsMapScreenShownFullscreen)
            {
                IsPeeking = false;
            }

            MapScreen?.Hide(false);

            if (MapScreen != null && MapScreen.transform != null)
            {
                var target = MapScreen?.SavedTrueParent ?? NorvinskMapsPlugin.MapScreenTrueParent ?? MapScreenTrueParent;
                if (target != null && target.gameObject != null)
                {
                    MapScreen.transform.SetParent(target, false);
                }
            }
        }

        [HideFromIl2Cpp]
        private bool CanShowMap()
        {
            return GameUtils.ShouldShowMapInRaid() && MapScreen != null;
        }

        [HideFromIl2Cpp]
        private void SafeSetParent(Transform targetParent)
        {
            if (targetParent == null || MapScreen == null) return;
            MapScreen.transform.SetParent(targetParent);
        }
    }
}