using System.Collections.Generic;
using NorvinskMaps.Data;
using NorvinskMaps.Utils;
using Il2CppInterop.Runtime.Attributes;
using TMPro;
using UnityEngine;

namespace NorvinskMaps.UI.Components
{
    public class MapLabel : MonoBehaviour, ILayerBound
    {
        public string Text { get; protected set; }
        public string Category { get; protected set; }

        public TextMeshProUGUI Label { get; protected set; }

        public RectTransform RectTransform => gameObject.transform as RectTransform;

        private Vector3 _position;
        public Vector3 Position
        {
            get => _position;
            set
            {
                var rectTransform = gameObject.GetRectTransform();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = value;
                }
                _position = value;
            }
        }

        private Color _color = Color.white;
        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                if (Label != null)
                {
                    Label.color = value;
                }
            }
        }

        [HideFromIl2Cpp]
		public Dictionary<LayerStatus, float> LabelAlphaLayerStatus { get; set; } = new Dictionary<LayerStatus, float>
		{
			{ LayerStatus.Hidden, 0.0f },
			{ LayerStatus.Underneath, 0.0f },
			{ LayerStatus.OnTop, 1f },
			{ LayerStatus.FullReveal, 1f },
		};

        private bool _hasSetOutline = false;

        [HideFromIl2Cpp]
        public static MapLabel Create(GameObject parent, MapLabelDef def, float degreesRotation, float scale)
        {
            var go = UIUtils.CreateUIGameObject(parent, $"MapLabel {def.Text}");

            var rectTransform = go.GetRectTransform();
            if (rectTransform == null)
            {
                Debug.LogError($"[MapLabel] Failed to get RectTransform on GameObject: {go.name}");
                return null;
            }

            rectTransform.anchoredPosition = def.Position;
            rectTransform.localScale = scale * Vector2.one;
            rectTransform.localRotation = Quaternion.Euler(0, 0, degreesRotation - def.DegreesRotation);

            var label = go.AddComponent<MapLabel>();
            label.Text = def.Text;
            label.Category = def.Category;
            label.Position = def.Position;

            label.Label = go.AddComponent<TextMeshProUGUI>();

            label.Color = def.Color;
            label.Label.autoSizeTextContainer = true;
            label.Label.fontSize = def.FontSize;
            label.Label.alignment = TextAlignmentOptions.Center;
            label.Label.text = label.Text;

            label._hasSetOutline = UIUtils.TrySetTMPOutline(label.Label);

            return label;
        }

        private void OnEnable()
        {
            if (!_hasSetOutline && Label != null)
            {
                _hasSetOutline = UIUtils.TrySetTMPOutline(Label);
            }
        }

        public void HandleNewLayerStatus(LayerStatus status)
        {
            if (Label == null) return;

            if (LabelAlphaLayerStatus.TryGetValue(status, out float alpha))
            {
                var currentColor = Label.color;
                Label.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
            }
            else
            {
                Debug.LogWarning($"[MapLabel] Unknown LayerStatus: {status}. Using default alpha 0.");
                Label.color = new Color(Label.color.r, Label.color.g, Label.color.b, 0f);
            }
        }
    }
}