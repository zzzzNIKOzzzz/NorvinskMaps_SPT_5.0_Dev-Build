using NorvinskMaps.Data;
using NorvinskMaps.Utils;
using Il2CppInterop.Runtime.Attributes;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace NorvinskMaps.UI.Components
{
    public enum LayerStatus
    {
        Hidden,
        Underneath,
        OnTop,
        FullReveal
    }

    public interface ILayerBound
    {
        Vector3 Position { get; set; }
        void HandleNewLayerStatus(LayerStatus status);
    }

    public class MapLayer : MonoBehaviour, ILayerBound
    {
        private static float _fadeMultiplierPerLayer = 0.5f;
        private static float _defaultLevelFallbackAlpha = 0.1f;

        private const int MaxRetryAttempts = 10;
        private const float RetryDelay = 0.15f;

        private string _name;
        private Image _image;
        private int _retryAttempt = 0;

        public RectTransform RectTransform => gameObject.transform as RectTransform;

        public int Level => _def.Level;
        public LayerStatus Status { get; private set; }
        public bool IsOnDefaultLevel { get; set; }

        private MapLayerDef _def = new MapLayerDef();

        [HideFromIl2Cpp]
        public static MapLayer Create(GameObject parent, string name, MapLayerDef def, float degreesRotation)
        {
            var go = new GameObject(name);
            go.AddComponent<RectTransform>();
            go.AddComponent<CanvasRenderer>();

            if (parent != null)
            {
                go.transform.SetParent(parent.transform, false);
            }
            else
            {
                NorvinskMapsPlugin.Log.LogWarning($"[MapLayer] '{name}' has NULL parent — will render in scene root!");
            }

            var rectTransform = go.GetRectTransform();
            if (rectTransform == null)
            {
                NorvinskMapsPlugin.Log.LogError($"[MapLayer] Failed to get RectTransform on newly created GameObject: {name}");
                Destroy(go);
                return null;
            }

            var layer = go.AddComponent<MapLayer>();
            layer._name = name;
            layer._def = def;
            layer._retryAttempt = 0;
            layer.CancelInvoke(nameof(TryLoadSpriteOnce));

            var size = def.ImageBounds.Max - def.ImageBounds.Min;
            var rotatedSize = MathUtils.GetRotatedRectangle(size, degreesRotation);
            rectTransform.sizeDelta = rotatedSize;

            var offset = MathUtils.GetMidpoint(def.ImageBounds.Min, def.ImageBounds.Max);
            rectTransform.anchoredPosition = offset;

            rectTransform.localRotation = Quaternion.Euler(0, 0, degreesRotation);

            // ВСЕГДА создаём Image сразу, чтобы OnTopLevelSelected не падал.
            layer._image = go.AddComponent<Image>();
            if (layer._image.material == null)
            {
                layer._image.material = Graphic.defaultGraphicMaterial;
            }
            layer._image.raycastTarget = false;
            layer._image.preserveAspect = false;
            layer._image.color = Color.white;

            var sprite = SvgUtils.GetOrLoadCachedSprite(def);

            if (sprite != null)
            {
                layer._image.sprite = sprite;
            }
            else
            {
                // Мягкий лог — спрайт может догрузиться через пару кадров
                NorvinskMapsPlugin.Log.LogDebug(
                    $"[MapLayer] Sprite not yet available for '{name}', scheduling retry");
                layer.StartSpriteRetry();
            }

            return layer;
        }

        // Запуск серии повторных попыток через Invoke (IL2CPP-совместимо).
        private void StartSpriteRetry()
        {
            _retryAttempt = 0;
            CancelInvoke(nameof(TryLoadSpriteOnce));
            Invoke(nameof(TryLoadSpriteOnce), RetryDelay);
        }

        // Одна попытка получить спрайт из кэша. Если не удалось — Invoke ещё раз.
        private void TryLoadSpriteOnce()
        {
            if (this == null || _image == null) return;
            if (_image.sprite != null) return;

            _retryAttempt++;

            try
            {
                var sprite = SvgUtils.GetOrLoadCachedSprite(_def);
                if (sprite != null)
                {
                    if (_image != null)
                    {
                        _image.sprite = sprite;
                        NorvinskMapsPlugin.Log.LogDebug(
                            $"[MapLayer] Sprite loaded on retry #{_retryAttempt} for '{_name}'");
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                NorvinskMapsPlugin.Log.LogDebug(
                    $"[MapLayer] Retry #{_retryAttempt} exception for '{_name}': {ex.Message}");
            }

            if (_retryAttempt >= MaxRetryAttempts)
            {
                NorvinskMapsPlugin.Log.LogWarning(
                    $"[MapLayer] Could not load sprite for layer: {_name} (after {MaxRetryAttempts} retries)");
                return;
            }

            Invoke(nameof(TryLoadSpriteOnce), RetryDelay);
        }

        public bool IsCoordinateInLayer(Vector3 coordinate)
        {
            if (_def.GameBounds == null) return false;

            foreach (var gameBound in _def.GameBounds)
            {
                if (coordinate.x >= gameBound.Min.x && coordinate.x <= gameBound.Max.x &&
                    coordinate.y >= gameBound.Min.y && coordinate.y <= gameBound.Max.y &&
                    coordinate.z >= gameBound.Min.z && coordinate.z <= gameBound.Max.z)
                    return true;
            }

            return false;
        }

        public float GetMatchingBoundVolume(Vector3 coordinate)
        {
            if (_def.GameBounds == null) return 0f;

            foreach (var gameBound in _def.GameBounds)
            {
                if (coordinate.x >= gameBound.Min.x && coordinate.x <= gameBound.Max.x &&
                    coordinate.y >= gameBound.Min.y && coordinate.y <= gameBound.Max.y &&
                    coordinate.z >= gameBound.Min.z && coordinate.z <= gameBound.Max.z)
                {
                    return (gameBound.Max.x - gameBound.Min.x) *
                           (gameBound.Max.y - gameBound.Min.y) *
                           (gameBound.Max.z - gameBound.Min.z);
                }
            }

            return 0f;
        }

        public void OnTopLevelSelected(int newLevel)
        {
            Status = LayerStatus.Hidden;

            if (Level == newLevel) Status = LayerStatus.OnTop;
            else if (Level < newLevel) Status = LayerStatus.Underneath;

            bool isActive = true;
            float alpha = 1f;
            float contrast = 1f;

            if (Status == LayerStatus.Hidden)
            {
                isActive = false;
                if (IsOnDefaultLevel)
                {
                    alpha = _defaultLevelFallbackAlpha;
                    isActive = true;
                }
            }
            else
            {
                if (Status == LayerStatus.Underneath)
                {
                    int levelDelta = newLevel - Level;
                    if (levelDelta > 0)
                        contrast = Mathf.Clamp01(Mathf.Pow(_fadeMultiplierPerLayer, levelDelta));
                }
            }

            if (_image != null)
            {
                _image.color = new Color(contrast, contrast, contrast, alpha);
            }

            gameObject.SetActive(isActive);
        }

        public Vector3 Position
        {
            get => RectTransform ? RectTransform.anchoredPosition : Vector3.zero;
            set
            {
                if (RectTransform != null)
                    RectTransform.anchoredPosition = value;
                _positionCache = value;
            }
        }

        private Vector3 _positionCache = Vector3.zero;

        public void HandleNewLayerStatus(LayerStatus status)
        {
            Status = status;
        }
    }
}