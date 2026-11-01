﻿using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using NorvinskMaps.Config;
using NorvinskMaps.UI;
using NorvinskMaps.UI.Components;
using NorvinskMaps.UI.Controls;
using NorvinskMaps.Utils;
using EFT;
using EFT.UI;
using EFT.UI.Map;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using System;
using System.Reflection;
using UnityEngine;

namespace NorvinskMaps
{
    [BepInPlugin("com.zzzNIKOzzz.NorvinskMaps", "NorvinskMaps", "1.0.0")]
    public class NorvinskMapsPlugin : BasePlugin
    {
        internal static NorvinskMapsPlugin Instance { get; private set; }
        internal static new ManualLogSource Log { get; private set; }
        internal static string Path { get; private set; } = string.Empty;

        public ModdedMapScreen Map;

        public static RectTransform MapScreenTrueParent;

        private Harmony _harmony;

        public override void Load()
        {
            Instance = this;
            Log = BepInEx.Logging.Logger.CreateLogSource("NorvinskMaps");
            Path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;

            Log.LogInfo("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz");
            Log.LogInfo("NorvinskMaps by zzzNIKOzzz загружается... ... ... ... ...");
            Log.LogInfo("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz");

            RegisterIl2CppTypes();
            RegisterColorConverter();

            Localization.DetectLanguage();
            Settings.Init(Config);
            ServerConfigLoader.CaptureMainThread();
            ServerConfigLoader.LoadAsync();

            _harmony = new Harmony("com.zzzNIKOzzz.NorvinskMaps");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public override bool Unload()
        {
            _harmony?.UnpatchSelf();
            Instance = null;
            return base.Unload();
        }

        private static void RegisterColorConverter()
        {
            TomlTypeConverter.AddConverter(typeof(Color), new TypeConverter
            {
                ConvertToString = (obj, type) =>
                {
                    var c = (Color)obj;
                    return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "{0},{1},{2},{3}", c.r, c.g, c.b, c.a);
                },
                ConvertToObject = (str, type) =>
                {
                    if (string.IsNullOrEmpty(str)) return Color.white;

                    var parts = str.Split(',');
                    float r = 0f, g = 0f, b = 0f, a = 1f;

                    if (parts.Length >= 3)
                    {
                        float.TryParse(parts[0], System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out r);
                        float.TryParse(parts[1], System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out g);
                        float.TryParse(parts[2], System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out b);
                    }
                    if (parts.Length >= 4)
                    {
                        float.TryParse(parts[3], System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out a);
                    }

                    return new Color(r, g, b, a);
                }
            });
        }

        private static void RegisterIl2CppTypes()
        {
            ClassInjector.RegisterTypeInIl2Cpp<ModdedMapScreen>();

            ClassInjector.RegisterTypeInIl2Cpp<MapView>();
            ClassInjector.RegisterTypeInIl2Cpp<MapLayer>();
            ClassInjector.RegisterTypeInIl2Cpp<MapLabel>();
            ClassInjector.RegisterTypeInIl2Cpp<MapMarker>();
            ClassInjector.RegisterTypeInIl2Cpp<MapPeekComponent>();

            ClassInjector.RegisterTypeInIl2Cpp<CursorPositionText>();
            ClassInjector.RegisterTypeInIl2Cpp<PlayerPositionText>();
            ClassInjector.RegisterTypeInIl2Cpp<LevelSelectSlider>();
            ClassInjector.RegisterTypeInIl2Cpp<MapSelectDropdown>();

            ClassInjector.RegisterTypeInIl2Cpp<PlayerDotSpawner>();
        }

        internal void TryAttachToMapScreen(MapScreen mapScreen)
        {
            if (Map != null) return;

            var commonUI = UnityEngine.Object.FindObjectOfType<CommonUI>();
            if (commonUI == null)
            {
                Log.LogWarning("CommonUI не найден. Возможно, сцена еще не загружена.");
                return;
            }

            Map = ModdedMapScreen.Create(commonUI.gameObject);
            if (Map != null && mapScreen != null)
            {
                Map.transform.SetParent(mapScreen.transform);

                NorvinskMapsPlugin.MapScreenTrueParent = mapScreen.transform as RectTransform;
            }
        }

        internal void TryAttachToBattleUIScreen(EftBattleUIScreen battleUI)
        {
            if (GameUtils.GetMainPlayer() is HideoutPlayer)
            {
                return;
            }

            if (Map == null)
            {
                var go = UIUtils.CreateUIGameObject(battleUI.gameObject, "ModdedMapBlock");
                if (go == null)
                {
                    NorvinskMapsPlugin.Log.LogError("[TryAttachToBattleUIScreen] UIUtils.CreateUIGameObject returned NULL");
                    return;
                }

                go.transform.SetParent(battleUI.transform, false);

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

                Map = go.AddComponent<ModdedMapScreen>();
                if (Map == null)
                {
                    NorvinskMapsPlugin.Log.LogError("[TryAttachToBattleUIScreen] AddComponent<ModdedMapScreen> returned NULL");
                    return;
                }
            }

            Map.TryAddPeekComponent(battleUI);
        }
    }
}