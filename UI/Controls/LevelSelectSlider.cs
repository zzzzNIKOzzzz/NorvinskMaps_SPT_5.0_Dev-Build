using System;
using System.Collections.Generic;
using System.Linq;
using NorvinskMaps.Data;
using NorvinskMaps.Utils;
using EFT.UI;
using Il2CppInterop.Runtime.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NorvinskMaps.UI.Controls
{
    public class LevelSelectSlider : MonoBehaviour
    {
        private static readonly float LevelTextSize = 15f;

        private static readonly Vector2 SliderSize = new Vector2(35f, 250f);
        private static readonly float ButtonSize = 18f;
        private static readonly float ButtonMargin = -5f;

        [HideFromIl2Cpp]
        public event Action<int> OnLevelSelectedBySlider;

        public RectTransform RectTransform => gameObject.transform as RectTransform;

        private int _selectedLevel = int.MinValue;
        public int SelectedLevel
        {
            get => _selectedLevel;
            set
            {
                if (_selectedLevel == value && !_levels.Contains(value)) return;

                if (_scrollbar == null) return;

                if (_levels.Count == 1)
                {
                    _scrollbar.value = 0.5f;
                }
                else
                {
                    var idx = _levels.IndexOf(value);
                    if (idx >= 0)
                        _scrollbar.value = idx / (_levels.Count - 1f);
                }

                if (_text != null)
                    _text.text = $"{Localization.T("Level")} {value}";
                _selectedLevel = value;
            }
        }

        private TextMeshProUGUI _text;
        private Scrollbar _scrollbar;
        private List<int> _levels = new List<int>();
        private bool _hasSetOutline = false;

        [HideFromIl2Cpp]
        public static LevelSelectSlider Create(GameObject prefab, Transform parent)
        {
            var go = GameObject.Instantiate(prefab);
            go.name = "LevelSelectSlider";

            var rectTransform = go.GetRectTransform();
            rectTransform.SetParent(parent, false);
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;

            var slider = go.AddComponent<LevelSelectSlider>();
            slider.ApplyFixedAnchors();
            return slider;
        }

        [HideFromIl2Cpp]
        public void ApplyFixedAnchors()
        {
            var rt = RectTransform;
            if (rt == null) return;

            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(15f, 100f);
            rt.sizeDelta = SliderSize;
            rt.localScale = Vector3.one;
        }

        private void Awake()
        {
            try
            {
                var scrollbarTransform = gameObject.transform.Find("Scrollbar");
                if (scrollbarTransform == null)
                {
                    NorvinskMapsPlugin.Log.LogWarning("[LevelSelectSlider] Scrollbar не найден — компонент отключён.");
                    enabled = false;
                    return;
                }

                var textGO = UIUtils.CreateUIGameObject(gameObject, "LevelText");
                _text = textGO.AddComponent<TextMeshProUGUI>();
                _text.fontSize = LevelTextSize;
                _text.alignment = TextAlignmentOptions.Left;

                var textRt = _text.GetRectTransform();
                textRt.anchorMin = new Vector2(1f, 0.5f);
                textRt.anchorMax = new Vector2(1f, 0.5f);
                textRt.pivot = new Vector2(0f, 0.5f);
                textRt.anchoredPosition = new Vector2(6f, 0f);
                textRt.sizeDelta = new Vector2(150f, 30f);

                _hasSetOutline = UIUtils.TrySetTMPOutline(_text);

                _scrollbar = scrollbarTransform.GetComponent<Scrollbar>();
                if (_scrollbar == null)
                {
                    NorvinskMapsPlugin.Log.LogWarning("[LevelSelectSlider] Scrollbar компонент не найден.");
                    enabled = false;
                    return;
                }

                _scrollbar.direction = Scrollbar.Direction.BottomToTop;
                _scrollbar.onValueChanged.AddListener((UnityAction<float>)OnScrollbarChanged);
                _scrollbar.onValueChanged.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);

                var plusButton = gameObject.transform.Find("Plus")?.GetComponent<Button>();
                if (plusButton != null)
                {
                    var rt = plusButton.GetRectTransform();
                    if (rt != null)
                    {
                        rt.anchorMin = new Vector2(0.5f, 1f);
                        rt.anchorMax = new Vector2(0.5f, 1f);
                        rt.pivot = new Vector2(0.5f, 1f);
                        rt.anchoredPosition = new Vector2(0f, -ButtonMargin);
                        rt.sizeDelta = new Vector2(ButtonSize, ButtonSize);
                        rt.localScale = Vector3.one;
                    }

                    plusButton.onClick.AddListener((UnityAction)(() => ChangeLevelBy(1)));
                    plusButton.onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);
                }

                var minusButton = gameObject.transform.Find("Minus")?.GetComponent<Button>();
                if (minusButton != null)
                {
                    var rt = minusButton.GetRectTransform();
                    if (rt != null)
                    {
                        rt.anchorMin = new Vector2(0.5f, 0f);
                        rt.anchorMax = new Vector2(0.5f, 0f);
                        rt.pivot = new Vector2(0.5f, 0f);
                        rt.anchoredPosition = new Vector2(0f, ButtonMargin);
                        rt.sizeDelta = new Vector2(ButtonSize, ButtonSize);
                        rt.localScale = Vector3.one;
                    }

                    minusButton.onClick.AddListener((UnityAction)(() => ChangeLevelBy(-1)));
                    minusButton.onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);
                }

                if (scrollbarTransform is RectTransform scrollbarRt)
                {
                    scrollbarRt.anchorMin = Vector2.zero;
                    scrollbarRt.anchorMax = Vector2.one;
                    scrollbarRt.offsetMin = new Vector2(0f, ButtonSize + ButtonMargin * 2f);
                    scrollbarRt.offsetMax = new Vector2(0f, -(ButtonSize + ButtonMargin * 2f));
                }

                gameObject.SetActive(false);
            }
            catch (Exception e)
            {
                NorvinskMapsPlugin.Log.LogError($"[LevelSelectSlider.Awake] {e.Message}");
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_hasSetOutline || _text == null) return;

            _hasSetOutline = UIUtils.TrySetTMPOutline(_text);
            if (_text != null) _text.text = _text.text;
        }

        private void OnDestroy()
        {
            OnLevelSelectedBySlider = null;
        }

        private void OnScrollbarChanged(float newValue)
        {
            if (_levels.Count == 0 || _scrollbar == null) return;

            if (_levels.Count == 1)
            {
                _scrollbar.value = 0.5f;
                return;
            }

            var levelIndex = Mathf.RoundToInt(newValue * (_levels.Count - 1));
            levelIndex = Mathf.Clamp(levelIndex, 0, _levels.Count - 1);
            var level = _levels[levelIndex];
            if (_selectedLevel != int.MinValue && _selectedLevel != level)
            {
                OnLevelSelectedBySlider?.Invoke(level);
            }
        }

        [HideFromIl2Cpp]
        public void ChangeLevelBy(int delta)
        {
            if (_levels.Count == 0) return;

            var newIndex = _levels.IndexOf(_selectedLevel) + delta;
            if (newIndex < 0 || newIndex >= _levels.Count) return;

            OnLevelSelectedBySlider?.Invoke(_levels[newIndex]);
        }

        [HideFromIl2Cpp]
        public void OnLoadMap(MapDef mapDef, int initialLevel)
        {
            if (mapDef?.Layers == null) return;

            _levels.Clear();
            _selectedLevel = int.MinValue;

            var uniqueLevels = new HashSet<int>();
            foreach (var layerDef in mapDef.Layers.Values)
            {
                uniqueLevels.Add(layerDef.Level);
            }

            _levels = uniqueLevels.ToList();
            _levels.Sort();

            if (_scrollbar != null)
                _scrollbar.numberOfSteps = _levels.Count;

            SelectedLevel = initialLevel;

            gameObject.SetActive(_levels.Count > 1);
        }
    }
}