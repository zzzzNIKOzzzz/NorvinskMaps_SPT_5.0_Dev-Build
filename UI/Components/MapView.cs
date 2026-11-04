using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using NorvinskMaps.Config;
using NorvinskMaps.Data;
using NorvinskMaps.Utils;
using EFT;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace NorvinskMaps.UI.Components
{
    public class MapView : MonoBehaviour
    {
        private static Vector2 _markerSize = new Vector2(30, 30);
        private static float _zoomMaxScaler = 10f;
        private static float _zoomMinScaler = 1.1f;

        [HideFromIl2Cpp]
        public event Action<int> OnLevelSelected;

        private RectTransform _rectTransform;

        public RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                {
                    if (gameObject == null)
                        return null;
                    _rectTransform = gameObject.GetComponent<RectTransform>();
                }
                return _rectTransform;
            }
        }

        private RectTransform _maskTransform;

        [HideFromIl2Cpp]
        public MapDef CurrentMapDef { get; private set; }
        public float CoordinateRotation { get; private set; }
        public int SelectedLevel { get; private set; }

        public GameObject MapMarkerContainer { get; private set; }
        public GameObject MapLabelsContainer { get; private set; }
        public GameObject MapLayerContainer { get; private set; }

        public float ZoomMin { get; private set; }
        public float ZoomMax { get; private set; }
        public float ZoomMain { get; set; }
        public float ZoomMini { get; set; }
        public bool IsMiniMapActive { get; set; }
        private float ZoomCurrent => IsMiniMapActive ? ZoomMini : ZoomMain;

        public Vector2 MainMapPos { get; set; } = Vector2.zero;

        private Vector2 _immediateMapAnchor = Vector2.zero;

        private List<MapMarker> _markers = new List<MapMarker>();
        private List<MapLayer> _layers = new List<MapLayer>();
        private List<MapLabel> _labels = new List<MapLabel>();
        private readonly List<PlayerMapMarker> _playerMarkers = new List<PlayerMapMarker>();
        private readonly List<TransformMapMarker> _transformMarkers = new List<TransformMapMarker>();

        [HideFromIl2Cpp]
        public static MapView Create(GameObject parent, string name)
        {
            var go = UIUtils.CreateUIGameObject(parent, name);
            if (go == null)
            {
                NorvinskMapsPlugin.Log.LogError("[MapView.Create] UIUtils.CreateUIGameObject returned NULL — aborting");
                return null;
            }

            if (go.GetComponent<RectTransform>() == null)
            {
                go.AddComponent<RectTransform>();
            }

            var existingImage = go.GetComponent<UnityEngine.UI.Image>();
            if (existingImage == null)
            {
                var img = go.AddComponent<UnityEngine.UI.Image>();
                img.color = new Color(0f, 0f, 0f, 0f);
                img.raycastTarget = false;
            }
            else
            {
                existingImage.raycastTarget = false;
            }

            var view = go.AddComponent<MapView>();
            if (view == null)
            {
                NorvinskMapsPlugin.Log.LogError("[MapView.Create] go.AddComponent<MapView>() returned NULL");
                return null;
            }

            return view;
        }

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();

            if (_rectTransform == null)
            {
                NorvinskMapsPlugin.Log.LogError("[MapView.Awake] RectTransform is NULL on MapView GameObject — this will break map rendering.");
                return;
            }

            MapLayerContainer = UIUtils.CreateUIGameObject(gameObject, "Layers");
            MapMarkerContainer = UIUtils.CreateUIGameObject(gameObject, "Markers");
            MapLabelsContainer = UIUtils.CreateUIGameObject(gameObject, "Labels");

            foreach (var c in new[] { MapLayerContainer, MapMarkerContainer, MapLabelsContainer })
            {
                var rt = c.GetRectTransform();
                if (rt != null)
                {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }

                var graphics = c.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
                foreach (var g in graphics)
                {
                    if (g != null) g.raycastTarget = false;
                }
            }

            MapLayerContainer.transform.SetAsFirstSibling();
            MapMarkerContainer.transform.SetAsLastSibling();
        }

        private void OnEnable()
        {
            Settings.OnZoomMainMapChanged += HandleZoomMainChanged;
            Settings.OnZoomMiniMapChanged += HandleZoomMiniChanged;
            Settings.OnMiniMapScaleMultiplierChanged += HandleMiniMapScaleMultiplierChanged;
        }

        private void OnDisable()
        {
            Settings.OnZoomMainMapChanged -= HandleZoomMainChanged;
            Settings.OnZoomMiniMapChanged -= HandleZoomMiniChanged;
            Settings.OnMiniMapScaleMultiplierChanged -= HandleMiniMapScaleMultiplierChanged;
        }

        private void Update()
        {
            for (int i = _playerMarkers.Count - 1; i >= 0; i--)
            {
                var m = _playerMarkers[i];
                if (m == null || m.Inner == null || m.Inner.gameObject == null)
                {
                    _playerMarkers.RemoveAt(i);
                    continue;
                }
                try { m.Tick(); }
                catch (Exception e) { NorvinskMapsPlugin.Log.LogError($"[MapView.Update] PlayerMapMarker.Tick: {e.Message}"); }
            }

            for (int i = _transformMarkers.Count - 1; i >= 0; i--)
            {
                var m = _transformMarkers[i];
                if (m == null || m.Inner == null || m.Inner.gameObject == null)
                {
                    _transformMarkers.RemoveAt(i);
                    continue;
                }
                try { m.Tick(); }
                catch (Exception e) { NorvinskMapsPlugin.Log.LogError($"[MapView.Update] TransformMapMarker.Tick: {e.Message}"); }
            }
        }

        [HideFromIl2Cpp]
        public bool IsDragging { get; set; } = false;

        [HideFromIl2Cpp]
        public void AddMapMarker(MapMarker marker)
        {
            if (marker == null) return;
            if (_markers.Contains(marker)) return;

            marker.OnPositionChanged += UpdateLayerBound;
            UpdateLayerBound(marker);
            marker.ContainingMapView = this;
            _markers.Add(marker);
        }

        [HideFromIl2Cpp]
        public MapMarker AddMapMarker(MapMarkerDef markerDef)
        {
            var marker = MapMarker.Create(MapMarkerContainer, markerDef, _markerSize, -CoordinateRotation, 1f / ZoomCurrent);
            AddMapMarker(marker);
            return marker;
        }

        [HideFromIl2Cpp]
        public void AddMapMarker(PlayerMapMarker marker)
        {
            if (marker == null) return;
            if (!_playerMarkers.Contains(marker))
                _playerMarkers.Add(marker);
            AddMapMarker(marker.Inner);
        }

        [HideFromIl2Cpp]
        public void RemoveMapMarker(PlayerMapMarker marker)
        {
            if (marker == null) return;
            _playerMarkers.Remove(marker);
            if (marker.Inner != null)
                RemoveMapMarker(marker.Inner);
        }

        [HideFromIl2Cpp]
        public void AddMapMarker(TransformMapMarker marker)
        {
            if (marker == null) return;
            if (!_transformMarkers.Contains(marker))
                _transformMarkers.Add(marker);
            AddMapMarker(marker.Inner);
        }

        [HideFromIl2Cpp]
        public void RemoveMapMarker(TransformMapMarker marker)
        {
            if (marker == null) return;
            _transformMarkers.Remove(marker);
            if (marker.Inner != null)
                RemoveMapMarker(marker.Inner);
        }

        [HideFromIl2Cpp]
        public PlayerMapMarker AddPlayerMarker(IPlayer player, string category, Color color, string imagePath)
        {
            var wrapper = PlayerMapMarker.Create(player, MapMarkerContainer, imagePath, color, category,
                                                 _markerSize, -CoordinateRotation, 1f / ZoomCurrent);
            if (wrapper == null || wrapper.Inner == null) return null;

            _playerMarkers.Add(wrapper);
            AddMapMarker(wrapper.Inner);
            return wrapper;
        }

        [HideFromIl2Cpp]
        public MapMarker AddTransformMarker(Transform followingTransform, string name, string category, Color color,
                                    string imagePath, Vector2 size)
        {
            var wrapper = TransformMapMarker.Create(followingTransform, MapMarkerContainer, imagePath, color, name, category,
                                                    size, -CoordinateRotation, 1f / ZoomCurrent);
            if (wrapper == null || wrapper.Inner == null) return null;

            _transformMarkers.Add(wrapper);
            AddMapMarker(wrapper.Inner);
            return wrapper.Inner;
        }

        [HideFromIl2Cpp]
        public IEnumerable<MapMarker> GetMapMarkersByCategory(string category)
        {
            return _markers.Where(m => m.Category == category);
        }

        [HideFromIl2Cpp]
        public void ChangeMarkerCategoryStatus(string category, bool status)
        {
            foreach (var marker in _markers)
            {
                if (marker.Category != category) continue;
                marker.gameObject.SetActive(status);
            }
        }

        [HideFromIl2Cpp]
        public void ChangeMarkerPartialCategoryStatus(string partial, bool status)
        {
            foreach (var marker in _markers)
            {
                if (!marker.Category.Contains(partial)) continue;
                marker.gameObject.SetActive(status);
            }
        }

        [HideFromIl2Cpp]
        public void RemoveMapMarker(MapMarker marker)
        {
            if (marker == null) return;
            if (!_markers.Contains(marker)) return;

            _markers.Remove(marker);
            marker.OnPositionChanged -= UpdateLayerBound;
            DOTween.Kill(marker.transform);
            marker.gameObject.SetActive(false);
            GameObject.Destroy(marker.gameObject);

            _playerMarkers.RemoveAll(pm => pm == null || pm.Inner == marker);
            _transformMarkers.RemoveAll(tm => tm == null || tm.Inner == marker);
        }

        [HideFromIl2Cpp]
        public void AddMapLabel(MapLabelDef labelDef)
        {
            var label = MapLabel.Create(MapLabelsContainer, labelDef, -CoordinateRotation, 1f / ZoomCurrent);
            UpdateLayerBound(label);
            _labels.Add(label);
        }

        [HideFromIl2Cpp]
        public void RemoveMapLabel(MapLabel label)
        {
            if (label == null) return;
            if (!_labels.Contains(label)) return;

            _labels.Remove(label);
            DOTween.Kill(label.transform);
            label.gameObject.SetActive(false);
            GameObject.Destroy(label.gameObject);
        }

        [HideFromIl2Cpp]
        public void LoadMap(MapDef mapDef, RectTransform maskTransform)
        {
            _maskTransform = maskTransform;
            if (mapDef == null || CurrentMapDef == mapDef) return;

            if (RectTransform == null)
            {
                NorvinskMapsPlugin.Log.LogError("[MapView.LoadMap] RectTransform is null — cannot load map.");
                return;
            }

            if (CurrentMapDef != null) UnloadMap();

            CurrentMapDef = mapDef;
            CoordinateRotation = mapDef.CoordinateRotation;

            var size = mapDef.Bounds.Max - mapDef.Bounds.Min;
            var rotatedSize = MathUtils.GetRotatedRectangle(size, CoordinateRotation);

            if (rotatedSize.x <= 0f || rotatedSize.y <= 0f)
                NorvinskMapsPlugin.Log.LogWarning($"[MapView.LoadMap] Rotated map size is invalid: {rotatedSize}. Map may render incorrectly.");

            RectTransform.sizeDelta = rotatedSize;
            RectTransform.localRotation = Quaternion.Euler(0, 0, CoordinateRotation);

            SetMinMaxZoom(maskTransform);

            foreach (var pair in mapDef.Layers.OrderBy(pair => pair.Value.Level))
            {
                var layerName = pair.Key;
                var layerDef = pair.Value;
                var layer = MapLayer.Create(MapLayerContainer, layerName, layerDef, -CoordinateRotation);
                layer.IsOnDefaultLevel = layerDef.Level == mapDef.DefaultLevel;
                _layers.Add(layer);
            }

            SelectTopLevel(mapDef.DefaultLevel);

            foreach (var markerDef in mapDef.StaticMarkers)
                AddMapMarker(markerDef);

            foreach (var labelDef in mapDef.Labels)
                AddMapLabel(labelDef);
        }

        [HideFromIl2Cpp]
        public void RecalculateZoom()
        {
            if (RectTransform == null) return;

            var parentRect = RectTransform.parent as RectTransform;
            if (parentRect == null) return;

            var parentSize = parentRect.rect.size;
            if (parentSize.x <= 0f || parentSize.y <= 0f)
            {
                return;
            }

            if (CurrentMapDef == null || _maskTransform == null || RectTransform == null) return;

            var prevMain = ZoomMain;
            var prevMini = ZoomMini;
            var normMain = ZoomMax > ZoomMin ? ActualToNormalized(prevMain) : 0f;
            var normMini = ZoomMax > ZoomMin ? ActualToNormalized(prevMini) : 0f;

            SetMinMaxZoom(_maskTransform, centerOnMap: !IsMiniMapActive);

            ZoomMain = NormalizedToActual(normMain);
            ZoomMini = NormalizedToActual(normMini);

            if (!IsMiniMapActive)
            {
                ApplyZoom(ZoomMain, 0f);
            }
        }

        [HideFromIl2Cpp]
        public void UnloadMap()
        {
            if (CurrentMapDef == null) return;

            var markersCopy = _markers.ToList();
            foreach (var marker in markersCopy) RemoveMapMarker(marker);
            markersCopy.Clear();
            _markers.Clear();

            var labelsCopy = _labels.ToList();
            foreach (var label in labelsCopy) RemoveMapLabel(label);
            labelsCopy.Clear();
            _labels.Clear();

            foreach (var layer in _layers)
            {
                if (layer != null && layer.gameObject != null)
                    GameObject.Destroy(layer.gameObject);
            }
            _layers.Clear();

            _playerMarkers.Clear();
            _transformMarkers.Clear();

            _immediateMapAnchor = Vector2.zero;
            CurrentMapDef = null;
            TextureUtils.ClearCache();
        }

        public void SelectTopLevel(int level)
        {
            foreach (var layer in _layers)
                layer.OnTopLevelSelected(level);

            SelectedLevel = level;
            UpdateLayerStatus();
            OnLevelSelected?.Invoke(level);
        }

        public void SelectLevelByCoords(Vector3 coords)
        {
            var matchingLayer = FindMatchingLayerByCoordinate(coords);
            if (matchingLayer == null) return;
            SelectTopLevel(matchingLayer.Level);
        }

        private float NormalizedToActual(float normalized) => Mathf.Lerp(ZoomMin, ZoomMax, normalized);
        private float ActualToNormalized(float actual) => Mathf.InverseLerp(ZoomMin, ZoomMax, actual);

        public void ClampToMapBounds()
        {
            if (CurrentMapDef == null || _maskTransform == null || RectTransform == null) return;
            if (DOTween.IsTweening(RectTransform, true)) return;

            var mapSize = CurrentMapDef.Bounds.Max - CurrentMapDef.Bounds.Min;
            var rotatedSize = MathUtils.GetRotatedRectangle(mapSize, CoordinateRotation);
            var scaledMapSize = rotatedSize * ZoomCurrent;
            var maskSize = _maskTransform.rect.size;

            var boundsCenter = MathUtils.GetMidpoint(CurrentMapDef.Bounds.Min, CurrentMapDef.Bounds.Max);
            var rotatedCenter = MathUtils.GetRotatedVector2(boundsCenter, CoordinateRotation);
            var scaledCenter = rotatedCenter * ZoomCurrent;

            var paddingX = maskSize.x * 0.5f;
            var paddingY = maskSize.y * 0.5f;
            var halfMap = scaledMapSize * 0.5f;
            var halfMask = maskSize * 0.5f;
            var sizeDiff = halfMap - halfMask;

            var rangeX = sizeDiff.x + paddingX;
            var rangeY = sizeDiff.y + paddingY;

            const float epsilon = 0.05f;
            if (Mathf.Abs(rangeX) < epsilon) rangeX = 0f;
            if (Mathf.Abs(rangeY) < epsilon) rangeY = 0f;

            rangeX = Mathf.Max(0f, rangeX);
            rangeY = Mathf.Max(0f, rangeY);

            var clamped = new Vector2(
                Mathf.Clamp(RectTransform.anchoredPosition.x, -scaledCenter.x - rangeX, -scaledCenter.x + rangeX),
                Mathf.Clamp(RectTransform.anchoredPosition.y, -scaledCenter.y - rangeY, -scaledCenter.y + rangeY));

            if (clamped != RectTransform.anchoredPosition)
            {
                _immediateMapAnchor = clamped;
                MainMapPos = clamped;
            }
        }

        public void SetMinMaxZoom(RectTransform parentTransform, bool centerOnMap = true)
        {
            if (RectTransform == null || parentTransform == null || CurrentMapDef == null) return;

            var mapSize = GetBaseMapSize();
            if (mapSize.x <= 0f || mapSize.y <= 0f)
            {
                NorvinskMapsPlugin.Log.LogWarning($"[MapView.SetMinMaxZoom] mapSize is invalid: {mapSize}");
                return;
            }

            const float RefWidth = 1920f;
            const float RefHeight = 1080f;

            ZoomMin = Mathf.Min(RefWidth / mapSize.x, RefHeight / mapSize.y) / _zoomMinScaler;
            ZoomMax = _zoomMaxScaler * ZoomMin;

            ZoomMain = ZoomMin;
            ZoomMini = NormalizedToActual(Settings.ZoomMiniMap.Value);

            ApplyZoom(ZoomCurrent, 0f);

            if (centerOnMap)
            {
                RectTransform.anchoredPosition = Vector2.zero;

                var midpoint = MathUtils.GetMidpoint(CurrentMapDef.Bounds.Min, CurrentMapDef.Bounds.Max);
                ShiftMapToCoordinate(midpoint, 0, false);
            }
            else
            {
                RectTransform.anchoredPosition = Vector2.zero;
                MainMapPos = Vector2.zero;
            }
        }

        public void SetMapZoom(float zoomNew, float tweenTime, bool updateMainZoom = true, bool updateMiniZoom = false)
        {
            zoomNew = Mathf.Clamp(zoomNew, ZoomMin, ZoomMax);

            if (updateMainZoom)
            {
                if (Mathf.Approximately(zoomNew, ZoomMain)) return;
                ZoomMain = zoomNew;
                Settings.ZoomMainMap.Value = ActualToNormalized(zoomNew);
            }

            if (updateMiniZoom)
            {
                if (Mathf.Approximately(zoomNew, ZoomMini)) return;
                ZoomMini = zoomNew;
                Settings.ZoomMiniMap.Value = ActualToNormalized(zoomNew);
            }

            ApplyZoom(ZoomCurrent, tweenTime);
        }

        public void ApplyMainMapZoom() => ApplyZoom(ZoomMain, 0f);
        public void ApplyMiniMapZoom() => ApplyZoom(ZoomMini, 0f);

        [HideFromIl2Cpp]
        public Vector2 GetBaseMapSize()
        {
            if (CurrentMapDef == null)
            {
                return RectTransform != null ? RectTransform.sizeDelta : Vector2.zero;
            }
            var size = CurrentMapDef.Bounds.Max - CurrentMapDef.Bounds.Min;
            return MathUtils.GetRotatedRectangle(size, CoordinateRotation);
        }

        [HideFromIl2Cpp]
        public void RestoreBaseSizeDelta()
        {
            if (RectTransform == null) return;
            var baseSize = GetBaseMapSize();
            if (baseSize.x > 0f && baseSize.y > 0f)
            {
                RectTransform.sizeDelta = baseSize;
            }
        }

        [HideFromIl2Cpp]
        public void EnlargeSizeDeltaForDrag(Vector2 viewportSize)
        {
            if (RectTransform == null) return;
            var baseSize = GetBaseMapSize();
            if (baseSize.x <= 0f || baseSize.y <= 0f) return;

            var targetSize = new Vector2(
                Mathf.Max(baseSize.x, viewportSize.x * 2f),
                Mathf.Max(baseSize.y, viewportSize.y * 2f));

            if (targetSize != RectTransform.sizeDelta)
            {
                RectTransform.sizeDelta = targetSize;
            }
        }

        private void ApplyZoom(float zoom, float tweenTime)
        {
            if (RectTransform == null) return;

            zoom = Mathf.Clamp(zoom, ZoomMin, ZoomMax);

            var multiplier = IsMiniMapActive ? Settings.MiniMapScaleMultiplier.Value : 1f;
            var effectiveScale = zoom * multiplier;

            RectTransform.DOScale(effectiveScale * Vector3.one, tweenTime);

            foreach (var marker in _markers)
            {
                var markerRt = marker.GetRectTransform();
                if (markerRt != null)
                    markerRt.DOScale(1 / effectiveScale * Vector3.one, tweenTime);
            }

            foreach (var label in _labels)
            {
                var labelRt = label.GetRectTransform();
                if (labelRt != null)
                    labelRt.DOScale(1 / effectiveScale * Vector3.one, tweenTime);
            }
        }

        [HideFromIl2Cpp]
        public void ApplyZoomExternal(float zoom)
        {
            if (RectTransform == null) return;
            zoom = Mathf.Clamp(zoom, ZoomMin, ZoomMax);
            ApplyZoom(zoom, 0f);
        }

        private void HandleZoomMainChanged(float normalized)
        {
            var actual = NormalizedToActual(normalized);
            ZoomMain = actual;
            if (!IsMiniMapActive) ApplyZoom(actual, 0);
        }

        private void HandleZoomMiniChanged(float normalized)
        {
            var actual = NormalizedToActual(normalized);
            ZoomMini = actual;
            if (IsMiniMapActive) ApplyZoom(actual, 0);
        }

        private void HandleMiniMapScaleMultiplierChanged(float _)
        {
            if (IsMiniMapActive) ApplyZoom(ZoomMini, 0f);
        }

        public void IncrementalZoomInto(float zoomDelta, Vector2 rectPoint, float zoomTweenTime)
        {
            if (RectTransform == null) return;
            CancelPositionTween();

            var zoomNew = Mathf.Clamp(ZoomMain + zoomDelta, ZoomMin, ZoomMax);
            var actualDelta = zoomNew - ZoomMain;
            var rotatedPoint = MathUtils.GetRotatedVector2(rectPoint, CoordinateRotation);

            ShiftMap(-rotatedPoint * actualDelta, zoomTweenTime, false);
            SetMapZoom(zoomNew, zoomTweenTime);
        }

        public void IncrementalZoomIntoMiniMap(float zoomDelta, Vector2 rectPoint, float zoomTweenTime)
        {
            if (RectTransform == null) return;
            CancelPositionTween();

            var zoomNew = Mathf.Clamp(ZoomMini + zoomDelta, ZoomMin, ZoomMax);
            var actualDelta = zoomNew - ZoomMini;
            var rotatedPoint = MathUtils.GetRotatedVector2(rectPoint, CoordinateRotation);

            ShiftMap(-rotatedPoint * actualDelta, zoomTweenTime, true);
            SetMapZoom(zoomNew, zoomTweenTime, false, true);
        }

        public void CancelPositionTween()
        {
            if (RectTransform == null) return;
            DOTween.Kill(RectTransform, true);
            _immediateMapAnchor = RectTransform.anchoredPosition;
        }

        public void ShiftMap(Vector2 shift, float tweenTime, bool isMini)
        {
            if (RectTransform == null) return;
            if (shift == Vector2.zero) return;

            if (!DOTween.IsTweening(RectTransform, true) || tweenTime == 0)
                _immediateMapAnchor = RectTransform.anchoredPosition;

            _immediateMapAnchor += shift;
            if (!isMini) MainMapPos = _immediateMapAnchor;

            RectTransform.DOAnchorPos(_immediateMapAnchor, tweenTime);
        }

        public void SetMapPos(Vector2 pos, float tweenTime)
        {
            if (RectTransform == null) return;
            MainMapPos = pos;
            RectTransform.DOAnchorPos(pos, tweenTime);
        }

        public void ShiftMapToCoordinate(Vector2 coord, float tweenTime, bool isMini)
        {
            if (RectTransform == null) return;
            var rotatedCoord = MathUtils.GetRotatedVector2(coord, CoordinateRotation);
            var currentCenter = RectTransform.anchoredPosition / ZoomCurrent;
            ShiftMap((-rotatedCoord - currentCenter) * ZoomCurrent, tweenTime, isMini);
        }

        public void ShiftMapToPlayer(Vector2 coord, float tweenTime, bool isMini)
        {
            if (RectTransform == null) return;
            var rotatedCoord = MathUtils.GetRotatedVector2(coord, CoordinateRotation);
            var currentCenter = RectTransform.anchoredPosition / ZoomMain;
            ShiftMap((-rotatedCoord - currentCenter) * ZoomMain, tweenTime, isMini);
        }

        public void ScaledShiftMap(Vector2 shiftIncrements, float incrementScale, bool isMini)
        {
            if (RectTransform == null || CurrentMapDef == null) return;

            var smallestDimension = Mathf.Min(CurrentMapDef.Bounds.Max.x - CurrentMapDef.Bounds.Min.x,
                                              CurrentMapDef.Bounds.Max.y - CurrentMapDef.Bounds.Min.y);

            var incrementSize = smallestDimension * ZoomCurrent * incrementScale;
            ShiftMap(shiftIncrements * incrementSize, 0, isMini);
        }

        private MapLayer FindMatchingLayerByCoordinate(Vector3 coordinate)
        {
            return _layers.Where(l => l.IsCoordinateInLayer(coordinate))
                          .OrderBy(l => l.GetMatchingBoundVolume(coordinate))
                          .FirstOrDefault();
        }

        [HideFromIl2Cpp]
        private void UpdateLayerBound(ILayerBound bound)
        {
            if (bound == null) return;
            var layer = FindMatchingLayerByCoordinate(bound.Position);
            if (layer is null) return;
            bound.HandleNewLayerStatus(layer.Status);
        }

        private void UpdateLayerStatus()
        {
            var theBound = _markers.Cast<ILayerBound>().Concat(_labels);
            foreach (var bound in theBound)
                UpdateLayerBound(bound);
        }
    }
}