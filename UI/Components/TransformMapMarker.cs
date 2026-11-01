using NorvinskMaps.Config;
using NorvinskMaps.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NorvinskMaps.UI.Components
{
    public class TransformMapMarker
    {
        private static readonly Vector2 _pivot = new Vector2(0.5f, 0.5f);

        public MapMarker Inner { get; private set; }
        public Transform FollowingTransform { get; private set; }
        public MathUtils.RotationAxis RotationAxis { get; set; } = MathUtils.RotationAxis.Y;

        private float _callbackTime = 0.5f;
        private bool _warnedAttachedIsDisabled = false;
        private Vector2 _targetAnchored;
        private Quaternion _targetRotation = Quaternion.identity;
        private bool _hasTarget;

        public string Text => Inner?.Text;
        public string Category => Inner?.Category;

        public MapView ContainingMapView
        {
            get => Inner?.ContainingMapView;
            set { if (Inner != null) Inner.ContainingMapView = value; }
        }

        public Image Image => Inner?.Image;
        public TextMeshProUGUI Label => Inner?.Label;
        public RectTransform RectTransform => Inner?.RectTransform;

        public string AssociatedItemId => Inner?.AssociatedItemId ?? "";

        public bool IsDynamic
        {
            get => Inner?.IsDynamic ?? false;
            set { if (Inner != null) Inner.IsDynamic = value; }
        }

        public bool ShowInRaid => Inner?.ShowInRaid ?? true;

        public Vector3 Position => Inner?.Position ?? Vector3.zero;

        public float Rotation => Inner?.Rotation ?? 0f;

        public Color Color
        {
            get => Inner?.Color ?? Color.white;
            set { if (Inner != null) Inner.Color = value; }
        }

        public Vector2 Size => Inner?.Size ?? Vector2.zero;

        public GameObject gameObject => Inner?.gameObject;
        public Transform transform => Inner?.transform;

        public void SetActive(bool value)
        {
            if (Inner?.gameObject != null)
                Inner.gameObject.SetActive(value);
        }

        public RectTransform GetRectTransform() => Inner?.RectTransform;

        private TransformMapMarker() { }

        public static TransformMapMarker Create(Transform followingTransform, GameObject parent, string imagePath,
                                                Color color, string name, string category, Vector2 size,
                                                float degreesRotation, float scale)
        {
            if (followingTransform == null || parent == null)
            {
                NorvinskMapsPlugin.Log.LogError("[TransformMapMarker] Invalid parameters for creation");
                return null;
            }

            MapMarker inner;
            try
            {
                inner = MapMarker.Create<MapMarker>(
                    parent, name, category, imagePath, color,
                    MathUtils.ConvertToMapPosition(followingTransform),
                    size, _pivot, degreesRotation, scale);
            }
            catch (System.Exception e)
            {
                NorvinskMapsPlugin.Log.LogError($"[TransformMapMarker] MapMarker.Create failed: {e.Message}");
                return null;
            }

            if (inner == null) return null;

            inner.IsDynamic = true;

            var wrapper = new TransformMapMarker
            {
                Inner = inner,
                FollowingTransform = followingTransform,
            };

            wrapper.ApplyAlphaDefaults();
            return wrapper;
        }

        private void ApplyAlphaDefaults()
        {
            if (Inner == null) return;

            Inner.ImageAlphaLayerStatus[LayerStatus.Hidden] = 0.25f;
            Inner.ImageAlphaLayerStatus[LayerStatus.Underneath] = 0.25f;
            Inner.ImageAlphaLayerStatus[LayerStatus.OnTop] = 1f;
            Inner.ImageAlphaLayerStatus[LayerStatus.FullReveal] = 1f;

            Inner.LabelAlphaLayerStatus[LayerStatus.Hidden] = 0.0f;
            Inner.LabelAlphaLayerStatus[LayerStatus.Underneath] = 0.0f;
            Inner.LabelAlphaLayerStatus[LayerStatus.OnTop] = 0.0f;
            Inner.LabelAlphaLayerStatus[LayerStatus.FullReveal] = 1.00f;
        }

        public void Tick()
        {
            if (Inner == null || Inner.gameObject == null) return;

            if (!IsFollowingTransformValid())
            {
                HandleInactiveFollowingTransform();
                return;
            }

            _warnedAttachedIsDisabled = false;

            var rt = Inner.RectTransform;
            if (rt == null) return;

            var (mapPosition, mapRotation) = GetCurrentTransformData();
            var positionChanged = !IsPositionAndRotationSame(mapPosition, mapRotation);

            _callbackTime += Time.deltaTime;
            var interval = Mathf.Max(0.001f, Settings.OtherMarkersUpdateInterval.Value);

            if (positionChanged && _callbackTime >= interval)
            {
                _callbackTime = 0f;

                try
                {
                    var prevAnchored = rt.anchoredPosition;
                    var prevRotation = rt.localRotation;

                    Inner.MoveAndRotate(mapPosition, mapRotation, true);

                    _targetAnchored = rt.anchoredPosition;
                    _targetRotation = rt.localRotation;
                    _hasTarget = true;

                    if (Settings.OtherMarkersSmoothing.Value > 0f)
                    {
                        rt.anchoredPosition = prevAnchored;
                        rt.localRotation = prevRotation;
                    }
                }
                catch (System.Exception e)
                {
                    NorvinskMapsPlugin.Log.LogError($"[TransformMapMarker.Tick] {e.Message}");
                }
            }

            if (!_hasTarget) return;

            var smoothingStrength = Settings.OtherMarkersSmoothing.Value;
            if (smoothingStrength <= 0f)
            {
                rt.anchoredPosition = _targetAnchored;
                rt.localRotation = _targetRotation;
            }
            else
            {
                var t = 1f - Mathf.Pow(1f - smoothingStrength, Time.deltaTime * 60f);
                rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, _targetAnchored, t);
                rt.localRotation = Quaternion.Slerp(rt.localRotation, _targetRotation, t);
            }
        }

        private void HandleInactiveFollowingTransform()
        {
            if (_warnedAttachedIsDisabled) return;
            NorvinskMapsPlugin.Log.LogWarning("FollowingTransform для TransformMapMarker отключен без удаления маркера");
            if (Inner != null) Inner.Color = Color.red;
            _warnedAttachedIsDisabled = true;
        }

        private bool IsPositionAndRotationSame(Vector3 mapPosition, float mapRotation)
        {
            if (Inner == null) return true;
            bool posMatch =
                MathUtils.ApproxEquals(Inner.Position.x, mapPosition.x) &&
                MathUtils.ApproxEquals(Inner.Position.y, mapPosition.y) &&
                MathUtils.ApproxEquals(Inner.Position.z, mapPosition.z);
            bool rotMatch = MathUtils.ApproxEquals(Inner.Rotation, mapRotation);
            return posMatch && rotMatch;
        }

        private bool IsFollowingTransformValid()
        {
            try
            {
                return FollowingTransform != null &&
                       FollowingTransform.gameObject != null &&
                       FollowingTransform.gameObject.activeInHierarchy;
            }
            catch { return false; }
        }

        private (Vector3 position, float rotation) GetCurrentTransformData()
        {
            if (!IsFollowingTransformValid()) return (Vector3.zero, 0f);
            return (
                MathUtils.ConvertToMapPosition(FollowingTransform),
                MathUtils.ConvertToMapRotation(FollowingTransform, RotationAxis)
            );
        }

        public void Destroy()
        {
            if (Inner != null && Inner.gameObject != null)
                UnityEngine.Object.Destroy(Inner.gameObject);
            Inner = null;
            FollowingTransform = null;
        }
    }
}