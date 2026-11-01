using System;
using NorvinskMaps.Utils;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.Map;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NorvinskMaps.Patches
{
    [HarmonyPatch(typeof(MapScreen), nameof(MapScreen.Show))]
    internal static class MapScreenShowPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(MapScreen __instance,
                                   InventoryController inventoryController,
                                   CompoundItem container)
        {
            try
            {
                EnsureMapCreated(__instance);

                if (__instance != null)
                {
                    var mapPanelRt = __instance.gameObject.GetRectTransform();

                    if (mapPanelRt != null)
                    {
                        if (NorvinskMapsPlugin.Instance?.Map != null)
                            NorvinskMapsPlugin.Instance.Map.SavedTrueParent_ForceSet(mapPanelRt);

                        NorvinskMapsPlugin.MapScreenTrueParent = mapPanelRt;
                    }
                }

                if (!Config.Settings.ReplaceMapScreen.Value || !GameUtils.ShouldShowMapInRaid())
                {
                    NorvinskMapsPlugin.Instance?.Map?.OnMapScreenClose();
                    return true;
                }

                NorvinskMapsPlugin.Instance?.Map?.OnMapScreenShow();
                return true;
            }
            catch (Exception e)
            {
                NorvinskMapsPlugin.Log.LogError($"[MapScreenShowPatch] {e.Message}");
                return true;
            }
        }

        [HarmonyPostfix]
        private static void Postfix(MapScreen __instance)
        {
            try
            {
                if (!Config.Settings.ReplaceMapScreen.Value || !GameUtils.ShouldShowMapInRaid())
                    return;

                if (__instance == null)
                    return;

                if (!__instance.gameObject.activeInHierarchy)
                {
                    __instance.gameObject.SetActive(true);
                }

                if (NorvinskMapsPlugin.Instance?.Map != null)
                {
                    NorvinskMapsPlugin.Instance.Map.ForceRecalcAfterActivation();
                }
            }
            catch (Exception e)
            {
                NorvinskMapsPlugin.Log.LogError($"[MapScreenShowPatch.Postfix] {e.Message}");
            }
        }

        private static void EnsureMapCreated(MapScreen mapScreen)
        {
            if (NorvinskMapsPlugin.Instance?.Map != null) return;

            var commonUI = UnityEngine.Object.FindObjectOfType<CommonUI>();
            if (commonUI == null) return;

            CreateMapUnderParent(mapScreen != null ? mapScreen.gameObject : commonUI.gameObject,
                                 "[MapScreenShowPatch]");
        }

        internal static void CreateMapUnderParent(GameObject parent, string logPrefix)
        {
            if (parent == null) return;

            var go = UIUtils.CreateUIGameObject(parent, "ModdedMapBlock");
            if (go == null) return;

            go.transform.SetParent(parent.transform, false);

            var rt = go.GetRectTransform();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;
            }

            NorvinskMapsPlugin.Instance.Map = go.AddComponent<UI.ModdedMapScreen>();
            NorvinskMapsPlugin.MapScreenTrueParent = parent.transform as RectTransform;

            if (NorvinskMapsPlugin.Instance.Map != null)
            {
                NorvinskMapsPlugin.Instance.Map.SavedTrueParent_ForceSet(parent.transform as RectTransform);
            }
            else
            {
                NorvinskMapsPlugin.Log.LogError($"{logPrefix} AddComponent<ModdedMapScreen>() вернул null");
            }
        }
    }

    [HarmonyPatch(typeof(UnityEngine.UI.ScrollRect), nameof(UnityEngine.UI.ScrollRect.OnBeginDrag))]
    internal static class ScrollRectBeginDragPatch
    {
        [HarmonyPostfix]
        private static void Postfix(UnityEngine.UI.ScrollRect __instance, UnityEngine.EventSystems.PointerEventData eventData)
        {
            try
            {
                var map = NorvinskMapsPlugin.Instance?.Map;
                if (map == null) return;

                if (map.MapView != null)
                {
                    map.MapView.IsDragging = true;
                }
            }
            catch (Exception e)
            {
                NorvinskMapsPlugin.Log.LogError($"[ScrollRectBeginDragPatch] {e.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(UnityEngine.UI.ScrollRect), nameof(UnityEngine.UI.ScrollRect.OnEndDrag))]
    internal static class ScrollRectEndDragPatch
    {
        [HarmonyPostfix]
        private static void Postfix(UnityEngine.UI.ScrollRect __instance, UnityEngine.EventSystems.PointerEventData eventData)
        {
            try
            {
                var map = NorvinskMapsPlugin.Instance?.Map;
                if (map == null) return;

                if (map.MapView != null)
                {
                    map.MapView.IsDragging = false;
                    map.MapView.MainMapPos = map.MapView.RectTransform.anchoredPosition;
                }
            }
            catch (Exception e)
            {
                NorvinskMapsPlugin.Log.LogError($"[ScrollRectEndDragPatch] {e.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(MapScreen.MapTabController), nameof(MapScreen.MapTabController.Show))]
    internal static class MapTabControllerShowPatch
    {
        [HarmonyPostfix]
        private static void Postfix(MapScreen.MapTabController __instance)
        {
            try
            {
                var mapScreen = __instance.Content;
                if (mapScreen == null)
                {
                    NorvinskMapsPlugin.Log.LogWarning("[MapTabControllerShowPatch] Content (MapScreen) == null.");
                    return;
                }

                var mapPanelRt = mapScreen.transform as RectTransform;
                if (mapPanelRt != null)
                {
                    NorvinskMapsPlugin.MapScreenTrueParent = mapPanelRt;
                }
                else
                {
                    NorvinskMapsPlugin.Log.LogWarning("[MapTabControllerShowPatch] mapPanelRt == NULL (transform as RectTransform)");
                }

                if (NorvinskMapsPlugin.Instance?.Map == null)
                {
                    MapScreenShowPatch.CreateMapUnderParent(mapScreen.gameObject,
                                                    "[MapTabControllerShowPatch]");
                }

                if (NorvinskMapsPlugin.Instance?.Map != null && mapPanelRt != null)
                {
                    NorvinskMapsPlugin.Instance.Map.SavedTrueParent_ForceSet(mapPanelRt);
                }

                NorvinskMapsPlugin.Instance?.Map?.OnMapScreenShow();
            }
            catch (Exception e)
            {
                NorvinskMapsPlugin.Log.LogError($"[MapTabControllerShowPatch] {e.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(MapScreen), nameof(MapScreen.Close))]
    internal static class MapScreenClosePatch
    {
        [HarmonyPrefix]
        private static bool Prefix()
        {
            try
            {
                NorvinskMapsPlugin.Instance?.Map?.OnMapScreenClose();

                return true;
            }
            catch (Exception e)
            {
                NorvinskMapsPlugin.Log.LogError($"[MapScreenClosePatch] {e.Message}");
                return true;
            }
        }
    }
}