using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP.Configuration;
using NorvinskMaps.Utils;
using UnityEngine;

namespace NorvinskMaps.Config
{
    internal static class Settings
    {
        private static readonly List<ConfigEntryBase> ConfigEntries = [];

        #region General

        private const string GeneralTitle = "1. General";
        public static ConfigEntry<bool> ReplaceMapScreen;
        public static ConfigEntry<KeyboardShortcut> CenterOnPlayerHotkey;
        public static ConfigEntry<KeyboardShortcut> DumpInfoHotkey;
        public static ConfigEntry<KeyboardShortcut> MoveMapUpHotkey;
        public static ConfigEntry<KeyboardShortcut> MoveMapDownHotkey;
        public static ConfigEntry<KeyboardShortcut> MoveMapLeftHotkey;
        public static ConfigEntry<KeyboardShortcut> MoveMapRightHotkey;
        public static ConfigEntry<float> MapMoveHotkeySpeed;
        public static ConfigEntry<KeyboardShortcut> ChangeMapLevelUpHotkey;
        public static ConfigEntry<KeyboardShortcut> ChangeMapLevelDownHotkey;
        public static ConfigEntry<bool> ZoomMainMapToMouse;
        public static ConfigEntry<KeyboardShortcut> ZoomMapInHotkey;
        public static ConfigEntry<KeyboardShortcut> ZoomMapOutHotkey;
        public static ConfigEntry<float> ZoomMapHotkeySpeed;

        #endregion

        #region Dynamic Markers

        private const string DynamicMarkerTitle = "2. Dynamic Markers";
        public static ConfigEntry<bool> ShowPlayerMarker;
        public static ConfigEntry<float> PlayerMarkerUpdateInterval;
        public static ConfigEntry<float> PlayerMarkerSmoothing;
        public static ConfigEntry<float> OtherMarkersUpdateInterval;
        public static ConfigEntry<float> OtherMarkersSmoothing;
        public static ConfigEntry<bool> ShowFriendlyPlayerMarkersInRaid;
        public static ConfigEntry<bool> ShowEnemyPlayerMarkersInRaid;
        public static ConfigEntry<bool> ShowScavMarkersInRaid;
        public static ConfigEntry<bool> ShowBossMarkersInRaid;
        public static ConfigEntry<bool> ShowLockedDoorStatus;
        public static ConfigEntry<bool> ShowQuestsInRaid;
        public static ConfigEntry<bool> ShowExtractsInRaid;
        public static ConfigEntry<bool> ShowExtractStatusInRaid;
        public static ConfigEntry<bool> ShowTransitPointsInRaid;
        public static ConfigEntry<bool> ShowSecretPointsInRaid;
        public static ConfigEntry<bool> ShowDroppedBackpackInRaid;
        public static ConfigEntry<bool> ShowWishListItemsInRaid;
        public static ConfigEntry<bool> ShowBTRInRaid;
        public static ConfigEntry<bool> ShowAirdropsInRaid;
        public static ConfigEntry<bool> ShowHiddenStashesInRaid;
        public static ConfigEntry<bool> ShowFriendlyCorpsesInRaid;
        public static ConfigEntry<bool> ShowKilledCorpsesInRaid;
        public static ConfigEntry<bool> ShowFriendlyKilledCorpsesInRaid;
        public static ConfigEntry<bool> ShowBossCorpsesInRaid;
        public static ConfigEntry<bool> ShowOtherCorpsesInRaid;

        #endregion

        #region Progression

        private const string ProgressionTitle = "3. Progression";
        public static ConfigEntry<bool> RequireMapInInventory;
        public static ConfigEntry<int> ShowPmcIntelLevel;
        public static ConfigEntry<int> ShowBossIntelLevel;
        public static ConfigEntry<int> ShowScavIntelLevel;
        public static ConfigEntry<int> ShowFriendlyIntelLevel;
        public static ConfigEntry<int> ShowCorpseIntelLevel;
        public static ConfigEntry<int> ShowAirdropIntelLevel;
        public static ConfigEntry<int> ShowWishListItemsIntelLevel;
        public static ConfigEntry<int> ShowHiddenStashIntelLevel;

        #endregion

        #region In Raid

        private const string InRaidTitle = "4. In-Raid";
        public static ConfigEntry<bool> ResetZoomOnCenter;
        public static ConfigEntry<float> CenteringZoomResetPoint;
        public static ConfigEntry<float> ZoomMainMap;
        public static ConfigEntry<bool> AutoCenterOnPlayerMarker;
        public static ConfigEntry<bool> RetainMapPosition;
        public static ConfigEntry<bool> AutoSelectLevel;
        public static ConfigEntry<KeyboardShortcut> PeekShortcut;
        public static ConfigEntry<bool> HoldForPeek;

        #endregion

        #region Mini Map

        private const string MiniMapTitle = "5. Mini-map";
        public static ConfigEntry<bool> MiniMapEnabled;
        public static ConfigEntry<EMiniMapPosition> MiniMapPosition;
        public static ConfigEntry<float> MiniMapSizeX;
        public static ConfigEntry<float> MiniMapSizeY;
        public static ConfigEntry<float> MiniMapScreenOffsetX;
        public static ConfigEntry<float> MiniMapScreenOffsetY;
        public static ConfigEntry<bool> MapTransitionEnabled;
        public static ConfigEntry<KeyboardShortcut> MiniMapShowOrHide;
        public static ConfigEntry<float> ZoomMiniMap;
        public static ConfigEntry<float> MiniMapScaleMultiplier;
        public static ConfigEntry<KeyboardShortcut> ZoomInMiniMapHotkey;
        public static ConfigEntry<KeyboardShortcut> ZoomOutMiniMapHotkey;

        #endregion

        #region Colors

        private const string MarkerColors = "6. Marker Colors";
        public static ConfigEntry<Color> PlayerColor;
        public static ConfigEntry<Color> PmcBearColor;
        public static ConfigEntry<Color> PmcUsecColor;
        public static ConfigEntry<Color> ScavColor;
        public static ConfigEntry<Color> BossColor;
        public static ConfigEntry<Color> AirdropColor;
        public static ConfigEntry<Color> BackpackColor;
        public static ConfigEntry<Color> LootItemColor;
        public static ConfigEntry<Color> KilledCorpseColor;
        public static ConfigEntry<Color> KilledBossColor;
        public static ConfigEntry<Color> KilledOtherColor;
        public static ConfigEntry<Color> BtrColor;
        public static ConfigEntry<Color> ExtractDefaultColor;
        public static ConfigEntry<Color> ExtractOpenColor;
        public static ConfigEntry<Color> ExtractClosedColor;
        public static ConfigEntry<Color> ExtractHasRequirementsColor;
        public static ConfigEntry<Color> TransPointColor;
        public static ConfigEntry<Color> SecretPointColor;
        public static ConfigEntry<Color> HiddenStashColor;

        #endregion

        public static void Init(ConfigFile config)
        {
            #region General

            ConfigEntries.Add(ReplaceMapScreen = config.Bind(
                GeneralTitle,
                "Replace Map Screen",
                true,
                new ConfigDescription(
                    Localization.T("Desc_ReplaceMapScreen"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ReplaceMapScreen") })));

            ConfigEntries.Add(CenterOnPlayerHotkey = config.Bind(
                GeneralTitle,
                "Center on Player Hotkey",
                new KeyboardShortcut(KeyCode.Semicolon),
                new ConfigDescription(
                    Localization.T("Desc_CenterOnPlayerHotkey"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_CenterOnPlayerHotkey") })));

            ConfigEntries.Add(MoveMapUpHotkey = config.Bind(
                GeneralTitle,
                "Move Map Up Hotkey",
                new KeyboardShortcut(KeyCode.UpArrow),
                new ConfigDescription(
                    Localization.T("Desc_MoveMapUpHotkey"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_MoveMapUpHotkey") })));

            ConfigEntries.Add(MoveMapDownHotkey = config.Bind(
                GeneralTitle,
                "Move Map Down Hotkey",
                new KeyboardShortcut(KeyCode.DownArrow),
                new ConfigDescription(
                    Localization.T("Desc_MoveMapDownHotkey"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_MoveMapDownHotkey") })));

            ConfigEntries.Add(MoveMapLeftHotkey = config.Bind(
                GeneralTitle,
                "Move Map Left Hotkey",
                new KeyboardShortcut(KeyCode.LeftArrow),
                new ConfigDescription(
                    Localization.T("Desc_MoveMapLeftHotkey"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_MoveMapLeftHotkey") })));

            ConfigEntries.Add(MoveMapRightHotkey = config.Bind(
                GeneralTitle,
                "Move Map Right Hotkey",
                new KeyboardShortcut(KeyCode.RightArrow),
                new ConfigDescription(
                    Localization.T("Desc_MoveMapRightHotkey"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_MoveMapRightHotkey") })));

            ConfigEntries.Add(MapMoveHotkeySpeed = config.Bind(
                GeneralTitle,
                "Move Map Hotkey Speed",
                0.25f,
                new ConfigDescription(
                    Localization.T("Desc_MapMoveHotkeySpeed"),
                    new AcceptableValueRange<float>(0.05f, 2f),
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_MapMoveHotkeySpeed") })));

            ConfigEntries.Add(ChangeMapLevelUpHotkey = config.Bind(
                GeneralTitle,
                "Change Map Level Up Hotkey",
                new KeyboardShortcut(KeyCode.Period),
                new ConfigDescription(
                    Localization.T("Desc_ChangeMapLevelUpHotkey"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ChangeMapLevelUpHotkey") })));

            ConfigEntries.Add(ChangeMapLevelDownHotkey = config.Bind(
                GeneralTitle,
                "Change Map Level Down Hotkey",
                new KeyboardShortcut(KeyCode.Comma),
                new ConfigDescription(
                    Localization.T("Desc_ChangeMapLevelDownHotkey"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ChangeMapLevelDownHotkey") })));

            ConfigEntries.Add(ZoomMainMapToMouse = config.Bind(
                GeneralTitle,
                "Zoom Menu Map to Mouse",
                true,
                new ConfigDescription(
                    Localization.T("Desc_ZoomMainMapToMouse"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ZoomMainMapToMouse") })));

            ConfigEntries.Add(ZoomMapInHotkey = config.Bind(
                GeneralTitle,
                "Zoom Map In Hotkey",
                new KeyboardShortcut(KeyCode.Equals),
                new ConfigDescription(
                    Localization.T("Desc_ZoomMapInHotkey"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ZoomMapInHotkey") })));

            ConfigEntries.Add(ZoomMapOutHotkey = config.Bind(
                GeneralTitle,
                "Zoom Map Out Hotkey",
                new KeyboardShortcut(KeyCode.Minus),
                new ConfigDescription(
                    Localization.T("Desc_ZoomMapOutHotkey"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ZoomMapOutHotkey") })));

            ConfigEntries.Add(ZoomMapHotkeySpeed = config.Bind(
                GeneralTitle,
                "Zoom Map Hotkey Speed",
                2.5f,
                new ConfigDescription(
                    Localization.T("Desc_ZoomMapHotkeySpeed"),
                    new AcceptableValueRange<float>(1f, 10f),
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ZoomMapHotkeySpeed") })));

            ConfigEntries.Add(DumpInfoHotkey = config.Bind(
                GeneralTitle,
                "Dump Info Hotkey",
                new KeyboardShortcut(KeyCode.D, KeyCode.LeftShift, KeyCode.LeftAlt),
                new ConfigDescription(
                    Localization.T("Desc_DumpInfoHotkey"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_DumpInfoHotkey"), IsAdvanced = true })));

            #endregion

            #region Dynamic Markers

            ConfigEntries.Add(ShowPlayerMarker = config.Bind(
                DynamicMarkerTitle,
                "Show Player Marker",
                true,
                new ConfigDescription(
                    Localization.T("Desc_ShowPlayerMarker"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ShowPlayerMarker") })));

            ConfigEntries.Add(PlayerMarkerUpdateInterval = config.Bind(
                DynamicMarkerTitle,
                "Player marker update interval",
                0.05f,
                new ConfigDescription(
                    Localization.T("Desc_PlayerMarkerUpdateInterval"),
                    new AcceptableValueRange<float>(0.016f, 0.5f),
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_PlayerMarkerUpdateInterval") })));

            ConfigEntries.Add(PlayerMarkerSmoothing = config.Bind(
                DynamicMarkerTitle,
                "Player marker smoothing",
                0.5f,
                new ConfigDescription(
                    Localization.T("Desc_PlayerMarkerSmoothing"),
                    new AcceptableValueRange<float>(0f, 0.95f),
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_PlayerMarkerSmoothing") })));

            ConfigEntries.Add(OtherMarkersUpdateInterval = config.Bind(
                DynamicMarkerTitle,
                "Other markers update interval",
                0.1f,
                new ConfigDescription(
                    Localization.T("Desc_OtherMarkersUpdateInterval"),
                    new AcceptableValueRange<float>(0.016f, 0.5f),
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_OtherMarkersUpdateInterval") })));

            ConfigEntries.Add(OtherMarkersSmoothing = config.Bind(
                DynamicMarkerTitle,
                "Other markers smoothing",
                0.5f,
                new ConfigDescription(
                    Localization.T("Desc_OtherMarkersSmoothing"),
                    new AcceptableValueRange<float>(0f, 0.95f),
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_OtherMarkersSmoothing") })));

            ConfigEntries.Add(ShowFriendlyPlayerMarkersInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Friendly Player Markers",
                true,
                new ConfigDescription(
                    Localization.T("Desc_ShowFriendlyPlayerMarkersInRaid"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ShowFriendlyPlayerMarkersInRaid") })));

            ConfigEntries.Add(ShowEnemyPlayerMarkersInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Enemy Player Markers",
                false,
                new ConfigDescription(
                    Localization.T("Desc_ShowEnemyPlayerMarkersInRaid"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ShowEnemyPlayerMarkersInRaid") })));

            ConfigEntries.Add(ShowScavMarkersInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Scav Markers",
                false,
                new ConfigDescription(
                    Localization.T("Desc_ShowScavMarkersInRaid"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ShowScavMarkersInRaid") })));

            ConfigEntries.Add(ShowBossMarkersInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Boss Markers",
                false,
                new ConfigDescription(
                    Localization.T("Desc_ShowBossMarkersInRaid"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ShowBossMarkersInRaid") })));

            ConfigEntries.Add(ShowLockedDoorStatus = config.Bind(
                DynamicMarkerTitle,
                "Show Locked Door Status",
                true,
                new ConfigDescription(
                    Localization.T("Desc_ShowLockedDoorStatus"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ShowLockedDoorStatus") })));

            ConfigEntries.Add(ShowQuestsInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Quests In Raid",
                true,
                new ConfigDescription(
                    Localization.T("Desc_ShowQuestsInRaid"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ShowQuestsInRaid") })));

            ConfigEntries.Add(ShowExtractsInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Extracts In Raid",
                true,
                new ConfigDescription(
                    Localization.T("Desc_ShowExtractsInRaid"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ShowExtractsInRaid") })));

            ConfigEntries.Add(ShowExtractStatusInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Extracts Status In Raid",
                true,
                new ConfigDescription(
                    Localization.T("Desc_ShowExtractStatusInRaid"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ShowExtractStatusInRaid") })));

            ConfigEntries.Add(ShowTransitPointsInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Transit Points In Raid",
                true,
                new ConfigDescription(
                    Localization.T("Desc_ShowTransitPointsInRaid"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ShowTransitPointsInRaid") })));

            ConfigEntries.Add(ShowSecretPointsInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Secret Extracts In Raid",
                true,
                new ConfigDescription(
                    Localization.T("Desc_ShowSecretPointsInRaid"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ShowSecretPointsInRaid") })));

            ConfigEntries.Add(ShowDroppedBackpackInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Dropped Backpack In Raid",
                true,
                new ConfigDescription(
                    Localization.T("Desc_ShowDroppedBackpackInRaid"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ShowDroppedBackpackInRaid") })));

            ConfigEntries.Add(ShowWishListItemsInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show wish listed items In Raid",
                true,
                new ConfigDescription(
                    Localization.T("Desc_ShowWishListItemsInRaid"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ShowWishListItemsInRaid") })));

            ConfigEntries.Add(ShowBTRInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show BTR In Raid",
                true,
                new ConfigDescription(
                    Localization.T("Desc_ShowBTRInRaid"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ShowBTRInRaid") })));

            ConfigEntries.Add(ShowAirdropsInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Airdrops In Raid",
                true,
                new ConfigDescription(
                    Localization.T("Desc_ShowAirdropsInRaid"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ShowAirdropsInRaid") })));

            ConfigEntries.Add(ShowHiddenStashesInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Hidden Stashes In Raid",
                true,
                new ConfigDescription(
                    Localization.T("Desc_ShowHiddenStashesInRaid"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ShowHiddenStashesInRaid") })));

            ConfigEntries.Add(ShowFriendlyCorpsesInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Friendly Corpses In Raid",
                true,
                new ConfigDescription(
                    Localization.T("Desc_ShowFriendlyCorpsesInRaid"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ShowFriendlyCorpsesInRaid") })));

            ConfigEntries.Add(ShowKilledCorpsesInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Player-killed Corpses In Raid",
                true,
                new ConfigDescription(
                    Localization.T("Desc_ShowKilledCorpsesInRaid"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ShowKilledCorpsesInRaid") })));

            ConfigEntries.Add(ShowFriendlyKilledCorpsesInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Friendly-killed Corpses In Raid",
                true,
                new ConfigDescription(
                    Localization.T("Desc_ShowFriendlyKilledCorpsesInRaid"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ShowFriendlyKilledCorpsesInRaid") })));

            ConfigEntries.Add(ShowBossCorpsesInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Boss Corpses In Raid",
                false,
                new ConfigDescription(
                    Localization.T("Desc_ShowBossCorpsesInRaid"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ShowBossCorpsesInRaid") })));

            ConfigEntries.Add(ShowOtherCorpsesInRaid = config.Bind(
                DynamicMarkerTitle,
                "Show Other Corpses In Raid",
                false,
                new ConfigDescription(
                    Localization.T("Desc_ShowOtherCorpsesInRaid"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ShowOtherCorpsesInRaid") })));

            #endregion

            #region Progression

            ConfigEntries.Add(RequireMapInInventory = config.Bind(
                ProgressionTitle,
                "Require a map in your inventory",
                false,
                new ConfigDescription(
                    Localization.T("Desc_RequireMapInInventory"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_RequireMapInInventory") })));

            ConfigEntries.Add(ShowPmcIntelLevel = config.Bind(
                ProgressionTitle,
                "Intel level required to show PMCs",
                0,
                new ConfigDescription(
                    Localization.T("Desc_ShowPmcIntelLevel"),
                    new AcceptableValueRange<int>(0, 3),
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ShowPmcIntelLevel") })));

            ConfigEntries.Add(ShowBossIntelLevel = config.Bind(
                ProgressionTitle,
                "Intel level required to show bosses",
                0,
                new ConfigDescription(
                    Localization.T("Desc_ShowBossIntelLevel"),
                    new AcceptableValueRange<int>(0, 3),
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ShowBossIntelLevel") })));

            ConfigEntries.Add(ShowScavIntelLevel = config.Bind(
                ProgressionTitle,
                "Intel level required to show scavs",
                0,
                new ConfigDescription(
                    Localization.T("Desc_ShowScavIntelLevel"),
                    new AcceptableValueRange<int>(0, 3),
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ShowScavIntelLevel") })));

            ConfigEntries.Add(ShowFriendlyIntelLevel = config.Bind(
                ProgressionTitle,
                "Intel level required to show friendly PMCs",
                0,
                new ConfigDescription(
                    Localization.T("Desc_ShowFriendlyIntelLevel"),
                    new AcceptableValueRange<int>(0, 3),
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ShowFriendlyIntelLevel") })));

            ConfigEntries.Add(ShowCorpseIntelLevel = config.Bind(
                ProgressionTitle,
                "Intel level required to show corpses",
                0,
                new ConfigDescription(
                    Localization.T("Desc_ShowCorpseIntelLevel"),
                    new AcceptableValueRange<int>(0, 3),
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ShowCorpseIntelLevel") })));

            ConfigEntries.Add(ShowAirdropIntelLevel = config.Bind(
                ProgressionTitle,
                "Intel level required to show airdrops",
                0,
                new ConfigDescription(
                    Localization.T("Desc_ShowAirdropIntelLevel"),
                    new AcceptableValueRange<int>(0, 3),
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ShowAirdropIntelLevel") })));

            ConfigEntries.Add(ShowWishListItemsIntelLevel = config.Bind(
                ProgressionTitle,
                "Intel level required to show wish listed loot items",
                0,
                new ConfigDescription(
                    Localization.T("Desc_ShowWishListItemsIntelLevel"),
                    new AcceptableValueRange<int>(0, 3),
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ShowWishListItemsIntelLevel") })));

            ConfigEntries.Add(ShowHiddenStashIntelLevel = config.Bind(
                ProgressionTitle,
                "Intel level required to show hidden stashes",
                0,
                new ConfigDescription(
                    Localization.T("Desc_ShowHiddenStashIntelLevel"),
                    new AcceptableValueRange<int>(0, 3),
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ShowHiddenStashIntelLevel") })));

            #endregion

            #region In Raid

            ConfigEntries.Add(AutoSelectLevel = config.Bind(
                InRaidTitle,
                "Auto Select Level",
                true,
                new ConfigDescription(
                    Localization.T("Desc_AutoSelectLevel"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_AutoSelectLevel") })));

            ConfigEntries.Add(AutoCenterOnPlayerMarker = config.Bind(
                InRaidTitle,
                "Auto Center On Player Marker",
                true,
                new ConfigDescription(
                    Localization.T("Desc_AutoCenterOnPlayerMarker"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_AutoCenterOnPlayerMarker") })));

            ConfigEntries.Add(ResetZoomOnCenter = config.Bind(
                InRaidTitle,
                "Reset Zoom On Center",
                false,
                new ConfigDescription(
                    Localization.T("Desc_ResetZoomOnCenter"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ResetZoomOnCenter") })));

            ConfigEntries.Add(RetainMapPosition = config.Bind(
                InRaidTitle,
                "Remember Map Position",
                false,
                new ConfigDescription(
                    Localization.T("Desc_RetainMapPosition"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_RetainMapPosition") })));

            ConfigEntries.Add(CenteringZoomResetPoint = config.Bind(
                InRaidTitle,
                "Centering On Player Zoom Level",
                0.15f,
                new ConfigDescription(
                    Localization.T("Desc_CenteringZoomResetPoint"),
                    new AcceptableValueRange<float>(0f, 1f),
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_CenteringZoomResetPoint") })));

            ConfigEntries.Add(ZoomMainMap = config.Bind(
                InRaidTitle,
                "Main map zoom",
                0f,
                new ConfigDescription(
                    Localization.T("Desc_ZoomMainMap"),
                    new AcceptableValueRange<float>(0f, 1f),
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ZoomMainMap") })));

            ConfigEntries.Add(PeekShortcut = config.Bind(
                InRaidTitle,
                "Peek at Map Shortcut",
                new KeyboardShortcut(KeyCode.M),
                new ConfigDescription(
                    Localization.T("Desc_PeekShortcut"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_PeekShortcut") })));

            ConfigEntries.Add(HoldForPeek = config.Bind(
                InRaidTitle,
                "Hold for Peek",
                true,
                new ConfigDescription(
                    Localization.T("Desc_HoldForPeek"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_HoldForPeek") })));

            ConfigEntries.Add(MapTransitionEnabled = config.Bind(
                InRaidTitle,
                "Peek Transition enabled",
                true,
                new ConfigDescription(
                    Localization.T("Desc_MapTransitionEnabled"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_MapTransitionEnabled") })));

            #endregion

            AutoCenterOnPlayerMarker.SettingChanged += OnAutoCenterOnPlayerEnable;
            ResetZoomOnCenter.SettingChanged += OnResetZoomEnable;
            RetainMapPosition.SettingChanged += OnPositionRetainEnable;
            ZoomMainMap.SettingChanged += ZoomMainMapChanged;

            #region Mini Map

            ConfigEntries.Add(MiniMapEnabled = config.Bind(
                MiniMapTitle,
                "Mini-map enabled",
                true,
                new ConfigDescription(
                    Localization.T("Desc_MiniMapEnabled"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_MiniMapEnabled") })));

            var miniMapPositionAttrs = new ConfigurationManagerAttributes
            {
                DispName = Localization.T("Name_MiniMapPosition"),
                CustomDrawer = (ConfigEntryBase entry) =>
                {
                    var configEntry = entry as ConfigEntry<EMiniMapPosition>;
                    if (configEntry == null) return;
                    var values = (EMiniMapPosition[])Enum.GetValues(typeof(EMiniMapPosition));
                    var current = configEntry.Value;

                    GUILayout.BeginHorizontal();

                    foreach (var v in values)
                    {
                        bool isSelected = (v == current);
                        var label = Localization.T($"MiniMapPos_{v}");
                        var prevBg = GUI.backgroundColor;
                        if (isSelected) GUI.backgroundColor = Color.green;

                        if (GUILayout.Button(label, GUILayout.ExpandWidth(true)) && !isSelected)
                        {
                            configEntry.Value = v;
                        }

                        GUI.backgroundColor = prevBg;
                    }

                    GUILayout.EndHorizontal();
                }
            };

            ConfigEntries.Add(MiniMapPosition = config.Bind(
                MiniMapTitle,
                "Mini-map position",
                EMiniMapPosition.TopRight,
                new ConfigDescription(
                    Localization.T("Desc_MiniMapPosition"),
                    null,
                    miniMapPositionAttrs)));

            ConfigEntries.Add(MiniMapSizeX = config.Bind(
                MiniMapTitle,
                "Mini-map size horizontal",
                275.0f,
                new ConfigDescription(
                    Localization.T("Desc_MiniMapSizeX"),
                    new AcceptableValueRange<float>(0f, 850f),
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_MiniMapSizeX") })));

            ConfigEntries.Add(MiniMapSizeY = config.Bind(
                MiniMapTitle,
                "Mini-map size vertical",
                275.0f,
                new ConfigDescription(
                    Localization.T("Desc_MiniMapSizeY"),
                    new AcceptableValueRange<float>(0f, 850f),
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_MiniMapSizeY") })));

            ConfigEntries.Add(MiniMapScreenOffsetX = config.Bind(
                MiniMapTitle,
                "Mini-map offset horizontal",
                0.0f,
                new ConfigDescription(
                    Localization.T("Desc_MiniMapScreenOffsetX"),
                    new AcceptableValueRange<float>(-Screen.width / 4f, Screen.width),
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_MiniMapScreenOffsetX") })));

            ConfigEntries.Add(MiniMapScreenOffsetY = config.Bind(
                MiniMapTitle,
                "Mini-map offset vertical",
                0.0f,
                new ConfigDescription(
                    Localization.T("Desc_MiniMapScreenOffsetY"),
                    new AcceptableValueRange<float>(-Screen.height / 4f, Screen.height),
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_MiniMapScreenOffsetY") })));

            ConfigEntries.Add(MiniMapShowOrHide = config.Bind(
                MiniMapTitle,
                "Show or Hide the mini-map",
                new KeyboardShortcut(KeyCode.End),
                new ConfigDescription(
                    Localization.T("Desc_MiniMapShowOrHide"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_MiniMapShowOrHide") })));

            ConfigEntries.Add(ZoomMiniMap = config.Bind(
                MiniMapTitle,
                "Mini map zoom",
                0.33f,
                new ConfigDescription(
                    Localization.T("Desc_ZoomMiniMap"),
                    new AcceptableValueRange<float>(0f, 1f),
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ZoomMiniMap") })));

            ConfigEntries.Add(MiniMapScaleMultiplier = config.Bind(
                MiniMapTitle,
                "Mini-map scale multiplier",
                1.0f,
                new ConfigDescription(
                    Localization.T("Desc_MiniMapScaleMultiplier"),
                    new AcceptableValueRange<float>(0.5f, 5f),
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_MiniMapScaleMultiplier") })));

            ConfigEntries.Add(ZoomInMiniMapHotkey = config.Bind(
                MiniMapTitle,
                "Zoom in key bind",
                new KeyboardShortcut(KeyCode.Keypad8),
                new ConfigDescription(
                    Localization.T("Desc_ZoomInMiniMapHotkey"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ZoomInMiniMapHotkey") })));

            ConfigEntries.Add(ZoomOutMiniMapHotkey = config.Bind(
                MiniMapTitle,
                "Zoom out key bind",
                new KeyboardShortcut(KeyCode.Keypad5),
                new ConfigDescription(
                    Localization.T("Desc_ZoomOutMiniMapHotkey"),
                    null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ZoomOutMiniMapHotkey") })));

            ZoomMiniMap.SettingChanged += ZoomMiniMapChanged;
            MiniMapScaleMultiplier.SettingChanged += MiniMapScaleMultiplierChanged;

            #endregion

            #region MarkerColors

            ConfigEntries.Add(PlayerColor = config.Bind(
                MarkerColors, "Your player marker color",
                new Color(0, 1, 0),
                new ConfigDescription(Localization.T("Desc_MarkerColor"), null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_PlayerColor") })));

            ConfigEntries.Add(PmcBearColor = config.Bind(
                MarkerColors, "Bear marker color",
                new Color(1, 0, 0),
                new ConfigDescription(Localization.T("Desc_MarkerColor"), null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_PmcBearColor") })));

            ConfigEntries.Add(PmcUsecColor = config.Bind(
                MarkerColors, "Usec marker color",
                new Color(1, 1, 0),
                new ConfigDescription(Localization.T("Desc_MarkerColor"), null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_PmcUsecColor") })));

            ConfigEntries.Add(ScavColor = config.Bind(
                MarkerColors, "Scav marker color",
                new Color(1, 0.45f, 0.007f),
                new ConfigDescription(Localization.T("Desc_MarkerColor"), null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ScavColor") })));

            ConfigEntries.Add(BossColor = config.Bind(
                MarkerColors, "Boss marker color",
                new Color(1f, 0.45f, 0.007f),
                new ConfigDescription(Localization.T("Desc_MarkerColor"), null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_BossColor") })));

            ConfigEntries.Add(AirdropColor = config.Bind(
                MarkerColors, "Airdrop marker color",
                new Color(1f, 0.30f, 0.007f),
                new ConfigDescription(Localization.T("Desc_MarkerColor"), null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_AirdropColor") })));

            ConfigEntries.Add(BackpackColor = config.Bind(
                MarkerColors, "Backpack marker color",
                new Color(0f, 1f, 0.0f),
                new ConfigDescription(Localization.T("Desc_MarkerColor"), null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_BackpackColor") })));

            ConfigEntries.Add(LootItemColor = config.Bind(
                MarkerColors, "Loot marker color",
                new Color(0.98f, 0.81f, 0.007f),
                new ConfigDescription(Localization.T("Desc_MarkerColor"), null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_LootItemColor") })));

            ConfigEntries.Add(KilledCorpseColor = config.Bind(
                MarkerColors, "Killed corpse marker color",
                new Color(1f, 0.0f, 0.0f),
                new ConfigDescription(Localization.T("Desc_MarkerColor"), null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_KilledCorpseColor") })));

            ConfigEntries.Add(KilledBossColor = config.Bind(
                MarkerColors, "Killed boss corpse marker color",
                new Color(1f, 0.0f, 1.0f),
                new ConfigDescription(Localization.T("Desc_MarkerColor"), null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_KilledBossColor") })));

            ConfigEntries.Add(KilledOtherColor = config.Bind(
                MarkerColors, "Killed by other corpse marker color",
                new Color(1f, 1f, 1f),
                new ConfigDescription(Localization.T("Desc_MarkerColor"), null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_KilledOtherColor") })));

            ConfigEntries.Add(BtrColor = config.Bind(
                MarkerColors, "BTR marker color",
                new Color(0.21f, 0.39f, 0.16f),
                new ConfigDescription(Localization.T("Desc_MarkerColor"), null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_BtrColor") })));

            ConfigEntries.Add(ExtractDefaultColor = config.Bind(
                MarkerColors, "Extract default marker color",
                new Color(1f, 0.92f, 0.01f),
                new ConfigDescription(Localization.T("Desc_MarkerColor"), null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ExtractDefaultColor") })));

            ConfigEntries.Add(ExtractOpenColor = config.Bind(
                MarkerColors, "Extract open marker color",
                new Color(0f, 1f, 0f),
                new ConfigDescription(Localization.T("Desc_MarkerColor"), null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ExtractOpenColor") })));

            ConfigEntries.Add(ExtractClosedColor = config.Bind(
                MarkerColors, "Extract closed marker color",
                new Color(1f, 0f, 0f),
                new ConfigDescription(Localization.T("Desc_MarkerColor"), null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ExtractClosedColor") })));

            ConfigEntries.Add(ExtractHasRequirementsColor = config.Bind(
                MarkerColors, "Extract has requirements marker color",
                new Color(1f, 0.92f, 0.01f),
                new ConfigDescription(Localization.T("Desc_MarkerColor"), null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_ExtractHasRequirementsColor") })));

            ConfigEntries.Add(TransPointColor = config.Bind(
                MarkerColors, "Transit point marker color",
                new Color(1f, 0.62f, 0.20f),
                new ConfigDescription(Localization.T("Desc_MarkerColor"), null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_TransPointColor") })));

            ConfigEntries.Add(SecretPointColor = config.Bind(
                MarkerColors, "Secret exfil point marker color",
                new Color(0.1f, 0.6f, 0.6f),
                new ConfigDescription(Localization.T("Desc_MarkerColor"), null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_SecretPointColor") })));

            ConfigEntries.Add(HiddenStashColor = config.Bind(
                MarkerColors, "Hidden stash marker color",
                new Color(1f, 0.92f, 0.01f),
                new ConfigDescription(Localization.T("Desc_MarkerColor"), null,
                    new ConfigurationManagerAttributes { DispName = Localization.T("Name_HiddenStashColor") })));

            #endregion

            RecalcOrder();
        }

        private static void RecalcOrder()
        {
            var settingOrder = ConfigEntries.Count;
            foreach (var entry in ConfigEntries)
            {
                var attributes = entry.Description.Tags[0] as ConfigurationManagerAttributes;
                if (attributes != null)
                {
                    attributes.Order = settingOrder;
                }

                settingOrder--;
            }
        }

        private static string LocalizeSection(string section)
        {
            switch (section)
            {
                case "1. General": return Localization.T("Section_General");
                case "2. Dynamic Markers": return Localization.T("Section_DynamicMarkers");
                case "3. Progression": return Localization.T("Section_Progression");
                case "4. In-Raid": return Localization.T("Section_InRaid");
                case "5. Mini-map": return Localization.T("Section_MiniMap");
                case "6. Marker Colors": return Localization.T("Section_MarkerColors");
                default: return section;
            }
        }

        private static void OnAutoCenterOnPlayerEnable(object sender, EventArgs e)
        {
            if (AutoCenterOnPlayerMarker.Value)
            {
                RetainMapPosition.Value = false;
            }
            else
            {
                ResetZoomOnCenter.Value = false;
            }
        }

        private static void OnPositionRetainEnable(object sender, EventArgs e)
        {
            if (RetainMapPosition.Value)
            {
                AutoCenterOnPlayerMarker.Value = false;
                ResetZoomOnCenter.Value = false;
            }
        }

        private static void OnResetZoomEnable(object sender, EventArgs e)
        {
            if (ResetZoomOnCenter.Value)
            {
                AutoCenterOnPlayerMarker.Value = true;
                RetainMapPosition.Value = false;
            }
        }

        public static event Action<float> OnZoomMainMapChanged;
        private static void ZoomMainMapChanged(object sender, EventArgs e)
        {
            if (ZoomMainMap == null)
                return;
            OnZoomMainMapChanged?.Invoke(ZoomMainMap.Value);
        }

        public static event Action<float> OnZoomMiniMapChanged;
        private static void ZoomMiniMapChanged(object sender, EventArgs e)
        {
            if (ZoomMiniMap == null)
                return;
            OnZoomMiniMapChanged?.Invoke(ZoomMiniMap.Value);
        }

        public static event Action<float> OnMiniMapScaleMultiplierChanged;
        private static void MiniMapScaleMultiplierChanged(object sender, EventArgs e)
        {
            if (MiniMapScaleMultiplier == null)
                return;
            OnMiniMapScaleMultiplierChanged?.Invoke(MiniMapScaleMultiplier.Value);
        }
    }
}