using NorvinskMaps.Data;
using NorvinskMaps.Utils;
using Il2CppInterop.Runtime.Attributes;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NorvinskMaps.UI.Components
{
    public class MapMarker : MonoBehaviour, ILayerBound, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly Dictionary<LayerStatus, float> DefaultImageAlpha = new Dictionary<LayerStatus, float>
        {
            {LayerStatus.Hidden, 0.50f},
            {LayerStatus.Underneath, 0.75f},
            {LayerStatus.OnTop, 1.0f},
            {LayerStatus.FullReveal, 1.0f},
        };

        private static readonly Dictionary<LayerStatus, float> DefaultLabelAlpha = new Dictionary<LayerStatus, float>
        {
            {LayerStatus.Hidden, 0.0f},
            {LayerStatus.Underneath, 0.0f},
            {LayerStatus.OnTop, 1.0f},
            {LayerStatus.FullReveal, 1.0f},
        };

        private static Vector2 _labelSizeMultiplier = new Vector2(2.5f, 2f);
        private static float _markerMinFontSize = 9f;
        private static float _markerMaxFontSize = 13f;

        [HideFromIl2Cpp]
        public event Action<ILayerBound> OnPositionChanged;

        public string Text { get; protected set; }
        public string Category { get; protected set; }

        [HideFromIl2Cpp]
        public MapView ContainingMapView { get; set; }

        public Image Image { get; protected set; }
        public TextMeshProUGUI Label { get; protected set; }

        private RectTransform _rectTransform;
        public RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                    _rectTransform = GetComponent<RectTransform>();
                return _rectTransform;
            }
        }

        public string AssociatedItemId { get; protected set; } = "";
        public bool IsDynamic { get; set; } = false;

        public bool ShowInRaid { get; protected set; } = true;

        private Vector3 _position;
        public Vector3 Position
        {
            get => _position;
            set => Move(value);
        }

        private float _rotation = 0f;
        public float Rotation
        {
            get => _rotation;
            set => SetRotation(value);
        }

        private Color _color = Color.white;
        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                if (Image != null) Image.color = value;
                if (Label != null) Label.color = value;
            }
        }

        private Vector2 _size = new Vector2(30f, 30f);
        public Vector2 Size
        {
            get => _size;
            set
            {
                _size = value;
                if (RectTransform != null) RectTransform.sizeDelta = _size;
                if (Image != null) Image.GetRectTransform().sizeDelta = _size;
                if (Label != null) Label.GetRectTransform().sizeDelta = _size * _labelSizeMultiplier;
            }
        }

        [HideFromIl2Cpp]
        public Dictionary<LayerStatus, float> ImageAlphaLayerStatus { get; protected set; } = new Dictionary<LayerStatus, float>(DefaultImageAlpha);

        [HideFromIl2Cpp]
        public Dictionary<LayerStatus, float> LabelAlphaLayerStatus { get; protected set; } = new Dictionary<LayerStatus, float>(DefaultLabelAlpha);

        private float _initialRotation;
        private bool _hasSetOutline = false;
        private bool _isInFullReveal = false;

        [HideFromIl2Cpp]
        public static MapMarker Create(GameObject parent, MapMarkerDef def, Vector2 size, float degreesRotation, float scale)
        {
            var mapMarker = Create<MapMarker>(parent, def.Text, def.Category, def.ImagePath, def.Color, def.Position, size,
                                            def.Pivot, degreesRotation, scale, def.ShowInRaid, def.Sprite);
            mapMarker.AssociatedItemId = def.AssociatedItemId;
            return mapMarker;
        }

        [HideFromIl2Cpp]
        public static T Create<T>(GameObject parent, string text, string category, string imageRelativePath, Color color,
                                    Vector3 position, Vector2 size, Vector2 pivot, float degreesRotation, float scale,
                                    bool showInRaid = true, Sprite sprite = null)
                where T : MapMarker
        {
            var go = UIUtils.CreateUIGameObject(parent, $"MapMarker {text}");

            var fakeImage = go.AddComponent<Image>();
            fakeImage.color = Color.clear;
            fakeImage.raycastTarget = true;

            var rectTransform = go.GetRectTransform();
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
            rectTransform.localScale = scale * Vector2.one;
            rectTransform.localRotation = Quaternion.Euler(0, 0, degreesRotation);
            rectTransform.pivot = pivot;

            var marker = go.AddComponent<T>();
            marker.Text = text;
            marker.Category = category;
            marker.Position = position;
            marker._initialRotation = degreesRotation;
            marker.ShowInRaid = showInRaid;

            var imageGO = UIUtils.CreateUIGameObject(go, "image");
            imageGO.AddComponent<CanvasRenderer>();
            imageGO.GetRectTransform().sizeDelta = size;
            imageGO.GetRectTransform().pivot = new Vector2(0.5f, 0.5f);
            marker.Image = imageGO.AddComponent<Image>();
            marker.Image.raycastTarget = false;
            marker.Image.sprite = sprite ?? TextureUtils.GetOrLoadCachedSprite(imageRelativePath);
            marker.Image.type = Image.Type.Simple;

            var labelGO = UIUtils.CreateUIGameObject(go, "label");
            labelGO.AddComponent<CanvasRenderer>();
            labelGO.GetRectTransform().anchorMin = new Vector2(0.5f, 0f);
            labelGO.GetRectTransform().anchorMax = new Vector2(0.5f, 0f);
            labelGO.GetRectTransform().pivot = new Vector2(0.5f, 1f);
            labelGO.GetRectTransform().sizeDelta = size * _labelSizeMultiplier;
            marker.Label = labelGO.AddComponent<TextMeshProUGUI>();
            marker.Label.alignment = TextAlignmentOptions.Top;
            marker.Label.enableWordWrapping = true;
            marker.Label.enableAutoSizing = true;
            marker.Label.fontSizeMin = _markerMinFontSize;
            marker.Label.fontSizeMax = _markerMaxFontSize;
            marker.Label.text = marker.Text;

            marker._hasSetOutline = UIUtils.TrySetTMPOutline(marker.Label);

            marker.Color = color;
            marker._size = size;

            if (marker.Label != null)
            {
                marker.Label.gameObject.SetActive(false);
            }

            return marker;
        }

        [HideFromIl2Cpp]
        public void ResetFromDef(MapMarkerDef def, float degreesRotation, float scale)
        {
            Text = def.Text ?? "";
            Category = def.Category ?? "None";
            IsDynamic = false;
            ShowInRaid = def.ShowInRaid;
            AssociatedItemId = def.AssociatedItemId ?? "";
            _initialRotation = degreesRotation;
            _isInFullReveal = false;
            _position = def.Position;
            _rotation = 0f;

            Sprite sprite = def.Sprite
                ?? (!string.IsNullOrEmpty(def.ImagePath)
                    ? TextureUtils.GetOrLoadCachedSprite(def.ImagePath)
                    : null);
            if (Image != null && sprite != null)
                Image.sprite = sprite;


            var rt = RectTransform;
            if (rt != null)
            {
                rt.sizeDelta = _size;
                rt.pivot = def.Pivot;
                rt.localScale = scale * Vector2.one;
                rt.localRotation = Quaternion.Euler(0, 0, degreesRotation);
                rt.anchoredPosition = def.Position;
            }

            if (Image != null && Image.gameObject != null)
            {
                var irt = Image.GetRectTransform();
                if (irt != null)
                {
                    irt.sizeDelta = _size;
                    irt.localRotation = Quaternion.identity;
                }
            }

            if (Label != null)
            {
                Label.text = Text;
                Label.gameObject.SetActive(false);
            }

            Color = def.Color;
        }

        protected virtual void OnEnable()
        {
            TrySetOutlineAndResize();
        }

        protected virtual void OnDestroy()
        {
            OnPositionChanged = null;
        }

        public void Move(Vector3 newPosition, bool callback = true)
        {
            if (RectTransform != null)
                RectTransform.anchoredPosition = newPosition;

            _position = newPosition;

            if (callback && OnPositionChanged != null)
            {
                OnPositionChanged.Invoke(this);
            }
        }

        public void SetRotation(float degreesRotation)
        {
            _rotation = degreesRotation;

            if (Image != null)
            {
                var rect = Image.gameObject.GetRectTransform();
                if (rect != null)
                    rect.localRotation = Quaternion.Euler(0, 0, degreesRotation - _initialRotation);
            }
        }

        public void MoveAndRotate(Vector3 newPosition, float rotation, bool callback = true)
        {
            Move(newPosition, callback);
            SetRotation(rotation);
        }

        public void HandleNewLayerStatus(LayerStatus status)
        {
            if (!ShowInRaid && GameUtils.IsInRaid())
            {
                gameObject.SetActive(false);
                return;
            }

            if (_isInFullReveal)
            {
                status = LayerStatus.FullReveal;
            }

            float imageAlpha = GetAlpha(ImageAlphaLayerStatus, status);
            float labelAlpha = GetAlpha(LabelAlphaLayerStatus, status);

            if (!string.IsNullOrEmpty(Category))
            {
                imageAlpha = GetCategoryAlpha(Category, status, imageAlpha);
                labelAlpha = GetCategoryAlpha(Category, status, labelAlpha);
            }

            ApplyAlpha(Image, imageAlpha);
            ApplyAlpha(Label, labelAlpha);

            if (Image != null && Image.gameObject != null)
                Image.gameObject.SetActive(imageAlpha > 0f);
            if (Label != null && Label.gameObject != null)
                Label.gameObject.SetActive(labelAlpha > 0f);

            gameObject.SetActive(labelAlpha > 0f || imageAlpha > 0f);
        }

        [HideFromIl2Cpp]
        private float GetAlpha(Dictionary<LayerStatus, float> alphaDict, LayerStatus status)
        {
            return alphaDict.TryGetValue(status, out float alpha) ? alpha : 0f;
        }

        private float GetCategoryAlpha(string category, LayerStatus status, float defaultAlpha)
        {
            return defaultAlpha;
        }

        private void ApplyAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null) return;

            var color = graphic.color;
            graphic.color = new Color(color.r, color.g, color.b, alpha);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            TrySetOutlineAndResize();

            _isInFullReveal = true;
            transform.SetAsLastSibling();
            HandleNewLayerStatus(LayerStatus.FullReveal);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isInFullReveal = false;
            OnPositionChanged?.Invoke(this);
        }

        private void TrySetOutlineAndResize()
        {
            if (_hasSetOutline || Label == null)
            {
                return;
            }

            Label.enableAutoSizing = true;
            Label.enableWordWrapping = true;
            Label.fontSizeMin = _markerMinFontSize;
            Label.fontSizeMax = _markerMaxFontSize;
            Label.alignment = TextAlignmentOptions.Top;
            Label.text = Label.text;

            _hasSetOutline = UIUtils.TrySetTMPOutline(Label);
        }
    }
}