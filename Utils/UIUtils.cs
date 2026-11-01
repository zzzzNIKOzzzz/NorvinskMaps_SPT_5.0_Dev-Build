using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP.Configuration;
using DG.Tweening;
using DG.Tweening.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NorvinskMaps.Utils
{
    public static class UIUtils
    {
        public static void ResetRectTransform(this GameObject gameObject)
        {
            if (gameObject == null) return;

            var rectTransform = gameObject.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                NorvinskMapsPlugin.Log.LogWarning($"[UIUtils.ResetRectTransform] {gameObject.name} has no RectTransform");
                return;
            }

            rectTransform.localPosition = Vector3.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.anchoredPosition3D = Vector3.zero;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        public static RectTransform GetRectTransform(this GameObject gameObject)
        {
            if (gameObject == null) return null;
            return gameObject.GetComponent<RectTransform>();
        }

        public static RectTransform GetRectTransform(this Component component)
        {
            if (component == null) return null;
            return component.GetComponent<RectTransform>();
        }

        public static Tween TweenColor(this Image image, Color to, float duration)
        {
            var from = image.color;
            return DOTween.To(
                (DG.Tweening.Core.DOGetter<float>)(() => 0f),
                (DG.Tweening.Core.DOSetter<float>)((t) => image.color = Color.Lerp(from, to, t)),
                1f,
                duration);
        }

        public static GameObject CreateUIGameObject(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.AddComponent<RectTransform>();

            if (parent != null)
            {
                go.transform.SetParent(parent.transform, false);
            }

            return go;
        }

        public static bool TrySetTMPOutline(TextMeshProUGUI text)
        {
            if (text == null)
            {
                NorvinskMapsPlugin.Log.LogWarning($"TrySetTMPOutline: text cannot be null");
                return false;
            }

            try
            {
                text.outlineColor = new Color32(0, 0, 0, 255);
                text.outlineWidth = 0.15f;
                text.fontStyle = FontStyles.Bold;
                text.ForceMeshUpdate(true, true);
                return true;
            }
            catch
            {
                // ничего
            }

            return false;
        }

        public static bool BetterIsPressed(this KeyboardShortcut key)
        {
            if (!Input.GetKey(key.MainKey))
            {
                return false;
            }

            foreach (var modifier in key.Modifiers)
            {
                if (!Input.GetKey(modifier))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool BetterIsDown(this KeyboardShortcut key)
        {
            if (!Input.GetKeyDown(key.MainKey))
            {
                return false;
            }

            foreach (var modifier in key.Modifiers)
            {
                if (!Input.GetKey(modifier))
                {
                    return false;
                }
            }

            return true;
        }
    }
}