using NorvinskMaps.Config;
using NorvinskMaps.Utils;
using EFT;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NorvinskMaps.UI.Components
{
    public class PlayerMapMarker
    {
        private static readonly Vector2 _pivot = new Vector2(0.5f, 0.5f);

        public MapMarker Inner { get; private set; }
        public IPlayer Player { get; private set; }

        private float _callbackTime = 0.5f;
        private bool _isValid;
        private Vector2 _targetAnchored;
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
        private PlayerMapMarker() { }

        public static PlayerMapMarker Create(IPlayer player, GameObject parent, string imagePath, Color color,
                                             string category, Vector2 size, float degreesRotation, float scale)
        {
            if (player == null || parent == null)
            {
                NorvinskMapsPlugin.Log.LogError("[PlayerMapMarker] Invalid parameters for creation (player or parent is null)");
                return null;
            }

            var name = Localization.T("UnknownPlayer");
            try { name = player.Profile?.GetCorrectedNickname() ?? Localization.T("UnknownPlayer"); }
            catch { /* ignore */ }

            MapMarker inner;
            try
            {
                inner = MapMarker.Create<MapMarker>(
                    parent, name, category, imagePath, color,
                    MathUtils.ConvertToMapPosition(player.Position),
                    size, _pivot, degreesRotation, scale);
            }
            catch (System.Exception e)
            {
                NorvinskMapsPlugin.Log.LogError($"[PlayerMapMarker] MapMarker.Create failed: {e.Message}");
                return null;
            }

            if (inner == null)
            {
                NorvinskMapsPlugin.Log.LogError("[PlayerMapMarker] MapMarker.Create returned null");
                return null;
            }

            inner.IsDynamic = true;

            var wrapper = new PlayerMapMarker
            {
                Inner = inner,
                Player = player,
                _isValid = true,
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
            if (!_isValid || Inner == null || Inner.gameObject == null) return;
            if (!IsValidPlayer()) return;

            var rt = Inner.RectTransform;
            if (rt == null) return;

            _callbackTime += Time.deltaTime;
            var interval = Mathf.Max(0.001f, Settings.PlayerMarkerUpdateInterval.Value);

            if (_callbackTime >= interval)
            {
                _callbackTime = 0f;

                try
                {
                    var targetMapPos = MathUtils.ConvertToMapPosition(Player.Position);
                    var targetRot = -Player.Rotation.x;
                    var prevAnchored = rt.anchoredPosition;

                    Inner.MoveAndRotate(targetMapPos, targetRot, true);

                    _targetAnchored = rt.anchoredPosition;
                    _hasTarget = true;

                    if (Settings.PlayerMarkerSmoothing.Value > 0f)
                    {
                        rt.anchoredPosition = prevAnchored;
                    }
                }
                catch (System.Exception e)
                {
                    NorvinskMapsPlugin.Log.LogError($"[PlayerMapMarker.Tick] {e.Message}");
                }
            }

            if (!_hasTarget) return;

            var smoothingStrength = Settings.PlayerMarkerSmoothing.Value;
            if (smoothingStrength <= 0f)
            {
                rt.anchoredPosition = _targetAnchored;
            }
            else
            {
                var t = 1f - Mathf.Pow(1f - smoothingStrength, Time.deltaTime * 60f);
                rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, _targetAnchored, t);
            }
        }

        private bool IsValidPlayer()
        {
            if (Player == null) return false;
            try { return Player.Transform != null && Player.Transform.Original != null; }
            catch { return false; }
        }

        public void Destroy()
        {
            _isValid = false;
            if (Inner != null && Inner.gameObject != null)
                UnityEngine.Object.Destroy(Inner.gameObject);
            Inner = null;
            Player = null;
        }
    }
}