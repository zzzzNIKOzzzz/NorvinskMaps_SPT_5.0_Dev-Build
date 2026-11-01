using System;
using System.Collections.Generic;
using UnityEngine;

namespace NorvinskMaps
{
    public static class Localization
    {
        private static string _currentLang = "En";

        private static readonly Dictionary<string, Dictionary<string, string>> _table =
            new Dictionary<string, Dictionary<string, string>>
            {
                ["En"] = new Dictionary<string, string>
                {
                    // --- UI ---
                    ["Cursor"] = "Cursor",
                    ["Player"] = "Player",
                    ["Level"] = "Level",
                    ["SelectMap"] = "Select a Map",
                    ["Airdrop"] = "Airdrop",
                    ["BTR"] = "BTR",
                    ["HiddenStash"] = "Hidden Stash",
                    ["UnknownPlayer"] = "UnknownPlayer",

                    // --- MiniMapPosition (для CustomDrawer) ---
                    ["MiniMapPos_TopRight"] = "Top-Right",
                    ["MiniMapPos_BottomRight"] = "Bottom-Right",
                    ["MiniMapPos_TopLeft"] = "Top-Left",
                    ["MiniMapPos_BottomLeft"] = "Bottom-Left",

                    // --- Section headers (для ConfigurationManagerAttributes.Category) ---
                    ["Section_General"] = "1. General",
                    ["Section_DynamicMarkers"] = "2. Dynamic Markers",
                    ["Section_Progression"] = "3. Progression",
                    ["Section_InRaid"] = "4. In-Raid",
                    ["Section_MiniMap"] = "5. Mini-map",
                    ["Section_MarkerColors"] = "6. Marker Colors",

                    // --- 1. General (Names) ---
                    ["Name_ReplaceMapScreen"] = "Replace Map Screen",
                    ["Name_CenterOnPlayerHotkey"] = "Center on Player Hotkey",
                    ["Name_MoveMapUpHotkey"] = "Move Map Up Hotkey",
                    ["Name_MoveMapDownHotkey"] = "Move Map Down Hotkey",
                    ["Name_MoveMapLeftHotkey"] = "Move Map Left Hotkey",
                    ["Name_MoveMapRightHotkey"] = "Move Map Right Hotkey",
                    ["Name_MapMoveHotkeySpeed"] = "Move Map Hotkey Speed",
                    ["Name_ChangeMapLevelUpHotkey"] = "Change Map Level Up Hotkey",
                    ["Name_ChangeMapLevelDownHotkey"] = "Change Map Level Down Hotkey",
                    ["Name_ZoomMainMapToMouse"] = "Zoom Menu Map to Mouse",
                    ["Name_ZoomMapInHotkey"] = "Zoom Map In Hotkey",
                    ["Name_ZoomMapOutHotkey"] = "Zoom Map Out Hotkey",
                    ["Name_ZoomMapHotkeySpeed"] = "Zoom Map Hotkey Speed",
                    ["Name_DumpInfoHotkey"] = "Dump Info Hotkey",

                    // --- 2. Dynamic Markers (Names) ---
                    ["Name_ShowPlayerMarker"] = "Show Player Marker",
                    ["Name_PlayerMarkerUpdateInterval"] = "Player marker update interval",
                    ["Name_PlayerMarkerSmoothing"] = "Player marker smoothing",
                    ["Name_OtherMarkersUpdateInterval"] = "Other markers update interval",
                    ["Name_OtherMarkersSmoothing"] = "Other markers smoothing",
                    ["Name_ShowFriendlyPlayerMarkersInRaid"] = "Show Friendly Player Markers",
                    ["Name_ShowEnemyPlayerMarkersInRaid"] = "Show Enemy Player Markers",
                    ["Name_ShowScavMarkersInRaid"] = "Show Scav Markers",
                    ["Name_ShowBossMarkersInRaid"] = "Show Boss Markers",
                    ["Name_ShowLockedDoorStatus"] = "Show Locked Door Status",
                    ["Name_ShowQuestsInRaid"] = "Show Quests In Raid",
                    ["Name_ShowExtractsInRaid"] = "Show Extracts In Raid",
                    ["Name_ShowExtractStatusInRaid"] = "Show Extracts Status In Raid",
                    ["Name_ShowTransitPointsInRaid"] = "Show Transit Points In Raid",
                    ["Name_ShowSecretPointsInRaid"] = "Show Secret Extracts In Raid",
                    ["Name_ShowDroppedBackpackInRaid"] = "Show Dropped Backpack In Raid",
                    ["Name_ShowWishListItemsInRaid"] = "Show wish listed items In Raid",
                    ["Name_ShowBTRInRaid"] = "Show BTR In Raid",
                    ["Name_ShowAirdropsInRaid"] = "Show Airdrops In Raid",
                    ["Name_ShowHiddenStashesInRaid"] = "Show Hidden Stashes In Raid",
                    ["Name_ShowFriendlyCorpsesInRaid"] = "Show Friendly Corpses In Raid",
                    ["Name_ShowKilledCorpsesInRaid"] = "Show Player-killed Corpses In Raid",
                    ["Name_ShowFriendlyKilledCorpsesInRaid"] = "Show Friendly-killed Corpses In Raid",
                    ["Name_ShowBossCorpsesInRaid"] = "Show Boss Corpses In Raid",
                    ["Name_ShowOtherCorpsesInRaid"] = "Show Other Corpses In Raid",

                    // --- 3. Progression (Names) ---
                    ["Name_RequireMapInInventory"] = "Require a map in your inventory",
                    ["Name_ShowPmcIntelLevel"] = "Intel level required to show PMCs",
                    ["Name_ShowBossIntelLevel"] = "Intel level required to show bosses",
                    ["Name_ShowScavIntelLevel"] = "Intel level required to show scavs",
                    ["Name_ShowFriendlyIntelLevel"] = "Intel level required to show friendly PMCs",
                    ["Name_ShowCorpseIntelLevel"] = "Intel level required to show corpses",
                    ["Name_ShowAirdropIntelLevel"] = "Intel level required to show airdrops",
                    ["Name_ShowWishListItemsIntelLevel"] = "Intel level required to show wish listed loot items",
                    ["Name_ShowHiddenStashIntelLevel"] = "Intel level required to show hidden stashes",

                    // --- 4. In-Raid (Names) ---
                    ["Name_AutoSelectLevel"] = "Auto Select Level",
                    ["Name_AutoCenterOnPlayerMarker"] = "Auto Center On Player Marker",
                    ["Name_ResetZoomOnCenter"] = "Reset Zoom On Center",
                    ["Name_RetainMapPosition"] = "Remember Map Position",
                    ["Name_CenteringZoomResetPoint"] = "Centering On Player Zoom Level",
                    ["Name_ZoomMainMap"] = "Main map zoom",
                    ["Name_PeekShortcut"] = "Peek at Map Shortcut",
                    ["Name_HoldForPeek"] = "Hold for Peek",
                    ["Name_MapTransitionEnabled"] = "Peek Transition enabled",

                    // --- 5. Mini-map (Names) ---
                    ["Name_MiniMapEnabled"] = "Mini-map enabled",
                    ["Name_MiniMapPosition"] = "Mini-map position",
                    ["Name_MiniMapSizeX"] = "Mini-map size horizontal",
                    ["Name_MiniMapSizeY"] = "Mini-map size vertical",
                    ["Name_MiniMapScreenOffsetX"] = "Mini-map offset horizontal",
                    ["Name_MiniMapScreenOffsetY"] = "Mini-map offset vertical",
                    ["Name_MiniMapShowOrHide"] = "Show or Hide the mini-map",
                    ["Name_ZoomMiniMap"] = "Mini map zoom",
                    ["Name_MiniMapScaleMultiplier"] = "Mini-map scale multiplier",
                    ["Name_ZoomInMiniMapHotkey"] = "Zoom in key bind",
                    ["Name_ZoomOutMiniMapHotkey"] = "Zoom out key bind",

                    // --- 6. Marker Colors (Names) ---
                    ["Name_PlayerColor"] = "Your player marker color",
                    ["Name_PmcBearColor"] = "Bear marker color",
                    ["Name_PmcUsecColor"] = "Usec marker color",
                    ["Name_ScavColor"] = "Scav marker color",
                    ["Name_BossColor"] = "Boss marker color",
                    ["Name_AirdropColor"] = "Airdrop marker color",
                    ["Name_BackpackColor"] = "Backpack marker color",
                    ["Name_LootItemColor"] = "Loot marker color",
                    ["Name_KilledCorpseColor"] = "Killed corpse marker color",
                    ["Name_KilledBossColor"] = "Killed boss corpse marker color",
                    ["Name_KilledOtherColor"] = "Killed by other corpse marker color",
                    ["Name_BtrColor"] = "BTR marker color",
                    ["Name_ExtractDefaultColor"] = "Extract default marker color",
                    ["Name_ExtractOpenColor"] = "Extract open marker color",
                    ["Name_ExtractClosedColor"] = "Extract closed marker color",
                    ["Name_ExtractHasRequirementsColor"] = "Extract has requirements marker color",
                    ["Name_TransPointColor"] = "Transit point marker color",
                    ["Name_SecretPointColor"] = "Secret exfil point marker color",
                    ["Name_HiddenStashColor"] = "Hidden stash marker color",

                    // --- 1. General (Descriptions) ---
                    ["Desc_ReplaceMapScreen"] = "If the map should replace the BSG default map screen, requires swapping away from modded map to refresh",
                    ["Desc_CenterOnPlayerHotkey"] = "Pressed while the map is open, centers the player",
                    ["Desc_MoveMapUpHotkey"] = "Hotkey to move the map up",
                    ["Desc_MoveMapDownHotkey"] = "Hotkey to move the map down",
                    ["Desc_MoveMapLeftHotkey"] = "Hotkey to move the map left",
                    ["Desc_MoveMapRightHotkey"] = "Hotkey to move the map right",
                    ["Desc_MapMoveHotkeySpeed"] = "How fast the map should move, units are map percent per second",
                    ["Desc_ChangeMapLevelUpHotkey"] = "Hotkey to move the map level up (shift-scroll-up also does this in map screen)",
                    ["Desc_ChangeMapLevelDownHotkey"] = "Hotkey to move the map level down (shift-scroll-down also does this in map screen)",
                    ["Desc_ZoomMainMapToMouse"] = "If the zoom hotkeys should zoom to mouse position or center when on Map Screen in inventory / out of raid",
                    ["Desc_ZoomMapInHotkey"] = "Hotkey to zoom the map in (scroll-up also does this in map screen)",
                    ["Desc_ZoomMapOutHotkey"] = "Hotkey to zoom the map out (scroll-down also does this in map screen)",
                    ["Desc_ZoomMapHotkeySpeed"] = "How fast the map should zoom by hotkey",
                    ["Desc_DumpInfoHotkey"] = "Pressed while the map is open, dumps json MarkerDefs for extracts, loot, and switches into root of plugin folder",

                    // --- 2. Dynamic Markers (Descriptions) ---
                    ["Desc_ShowPlayerMarker"] = "If the player marker should be shown in raid",
                    ["Desc_PlayerMarkerUpdateInterval"] = "How often (in seconds) the player marker polls the player position. Lower = more responsive. 0.016 \u2248 every frame, 0.05 = 20 Hz, 0.5 = old default (laggy).",
                    ["Desc_PlayerMarkerSmoothing"] = "Smoothing strength for the player marker (0 = off/snap, 0.95 = maximum smoothing). Interpolates the marker between polls for a fluid look.",
                    ["Desc_OtherMarkersUpdateInterval"] = "How often (in seconds) other dynamic markers (bots, friendly PMCs, BTR, airdrops) poll their transform. Lower = smoother but more CPU. 0.016 \u2248 every frame, 0.1 = 10 Hz, 0.5 = old default.",
                    ["Desc_OtherMarkersSmoothing"] = "Smoothing strength for other dynamic markers (0 = off/snap, 0.95 = maximum smoothing).",
                    ["Desc_ShowFriendlyPlayerMarkersInRaid"] = "If friendly player markers should be shown in-raid",
                    ["Desc_ShowEnemyPlayerMarkersInRaid"] = "If enemy player markers should be shown in-raid (generally for debug)",
                    ["Desc_ShowScavMarkersInRaid"] = "If enemy scav markers should be shown in-raid (generally for debug)",
                    ["Desc_ShowBossMarkersInRaid"] = "If enemy boss markers should be shown in-raid",
                    ["Desc_ShowLockedDoorStatus"] = "If locked door markers should be updated with status based on key acquisition",
                    ["Desc_ShowQuestsInRaid"] = "If quests should be shown in raid",
                    ["Desc_ShowExtractsInRaid"] = "If extracts should be shown in raid",
                    ["Desc_ShowExtractStatusInRaid"] = "If extracts should be colored according to their status in raid",
                    ["Desc_ShowTransitPointsInRaid"] = "If transits should be shown in raid",
                    ["Desc_ShowSecretPointsInRaid"] = "If secret extracts should be shown in raid",
                    ["Desc_ShowDroppedBackpackInRaid"] = "If the player's dropped backpacks (not anyone elses) should be shown in raid",
                    ["Desc_ShowWishListItemsInRaid"] = "Shows items that are in your wishlist on the map in raid",
                    ["Desc_ShowBTRInRaid"] = "If the BTR should be shown in raid",
                    ["Desc_ShowAirdropsInRaid"] = "If airdrops should be shown in raid when they land",
                    ["Desc_ShowHiddenStashesInRaid"] = "If hidden stashes should be shown in raid",
                    ["Desc_ShowFriendlyCorpsesInRaid"] = "If friendly corpses should be shown in raid",
                    ["Desc_ShowKilledCorpsesInRaid"] = "If corpses killed by the player should be shown in raid, killed bosses will be shown in another color",
                    ["Desc_ShowFriendlyKilledCorpsesInRaid"] = "If corpses killed by friendly players should be shown in raid, killed bosses will be shown in another color",
                    ["Desc_ShowBossCorpsesInRaid"] = "If boss corpses (other than ones killed by the player) should be shown in raid",
                    ["Desc_ShowOtherCorpsesInRaid"] = "If corpses (other than friendly ones or ones killed by the player) should be shown in raid",

                    // --- 3. Progression (Descriptions) ---
                    ["Desc_RequireMapInInventory"] = "Requires you to have a map in your inventory in order to view the map in raid.",
                    ["Desc_ShowPmcIntelLevel"] = "If intel level is at or above this value it will show PMCs",
                    ["Desc_ShowBossIntelLevel"] = "If intel level is at or above this value it will show bosses",
                    ["Desc_ShowScavIntelLevel"] = "If intel level is at or above this value it will show scavs",
                    ["Desc_ShowFriendlyIntelLevel"] = "If intel level is at or above this value it will show friendly PMCs",
                    ["Desc_ShowCorpseIntelLevel"] = "If intel level is at or above this value it will show corpses",
                    ["Desc_ShowAirdropIntelLevel"] = "If intel level is at or above this value it will show airdrops",
                    ["Desc_ShowWishListItemsIntelLevel"] = "If intel level is at or above this value it will show wish listed loot items",
                    ["Desc_ShowHiddenStashIntelLevel"] = "If intel level is at or above this value it will show hidden stashes",

                    // --- 4. In-Raid (Descriptions) ---
                    ["Desc_AutoSelectLevel"] = "If the level should be automatically selected based on the players position in raid",
                    ["Desc_AutoCenterOnPlayerMarker"] = "If the player marker should be centered when showing the map in raid (Conflicts with 'Remember Map Position')",
                    ["Desc_ResetZoomOnCenter"] = "If the zoom level should be reset each time that the map is opened while in raid (Conflicts with 'Remember Map Position')",
                    ["Desc_RetainMapPosition"] = "Should we remember the map position (Map position memory is only maintained for the current raid) (Conflicts with 'Auto Center On Player Marker' and 'Reset Zoom On Center')",
                    ["Desc_CenteringZoomResetPoint"] = "What zoom level should be used as while centering on the player (0 is fully zoomed out, and 1 is fully zoomed in)",
                    ["Desc_ZoomMainMap"] = "What zoom level should be used for the main map. (Tab view/Peek view) (0 is fully zoomed out, and 1 is fully zoomed in)",
                    ["Desc_PeekShortcut"] = "The keyboard shortcut to peek at the map",
                    ["Desc_HoldForPeek"] = "If the shortcut should be held to keep it open. If disabled, button toggles",
                    ["Desc_MapTransitionEnabled"] = "Enable the map transition animations (When disabled everything will snap)",

                    // --- 5. Mini-map (Descriptions) ---
                    ["Desc_MiniMapEnabled"] = "Enable the mini-map",
                    ["Desc_MiniMapPosition"] = "What corner is the mini-map displayed in",
                    ["Desc_MiniMapSizeX"] = "Horizontal size of the mini-map",
                    ["Desc_MiniMapSizeY"] = "Vertical size of the mini-map",
                    ["Desc_MiniMapScreenOffsetX"] = "Horizontal Offset from the edge (These values update according to screen resolution, REQUIRES RESTART IF YOU CHANGED YOUR RESOLUTION)",
                    ["Desc_MiniMapScreenOffsetY"] = "Vertical offset from the edge (These values update according to screen resolution, REQUIRES RESTART IF YOU CHANGED YOUR RESOLUTION)",
                    ["Desc_MiniMapShowOrHide"] = "Show or hide the mini-map",
                    ["Desc_ZoomMiniMap"] = "What zoom level should be used for the mini map. (0 is fully zoomed out, and 1 is fully zoomed in)",
                    ["Desc_MiniMapScaleMultiplier"] = "Base scale multiplier applied to the mini-map on top of the zoom. Raise for a bigger mini-map (e.g. 2.75 to restore the old hardcoded look), lower for a smaller one.",
                    ["Desc_ZoomInMiniMapHotkey"] = "Zoom in on mini map key bind",
                    ["Desc_ZoomOutMiniMapHotkey"] = "Zoom out on mini map key bind",

                    // --- 6. Marker Colors (Descriptions) ---
                    ["Desc_MarkerColor"] = "Color of the marker",
                },

                ["Ru"] = new Dictionary<string, string>
                {
                    // --- UI ---
                    ["Cursor"] = "Курсор",
                    ["Player"] = "Игрок",
                    ["Level"] = "Уровень",
                    ["SelectMap"] = "Выберите карту",
                    ["Airdrop"] = "Аирдроп",
                    ["BTR"] = "БТР",
                    ["HiddenStash"] = "Схрон",
                    ["UnknownPlayer"] = "Неизвестный игрок",

                    // --- MiniMapPosition ---
                    ["MiniMapPos_TopRight"] = "Сверху справа",
                    ["MiniMapPos_BottomRight"] = "Снизу справа",
                    ["MiniMapPos_TopLeft"] = "Сверху слева",
                    ["MiniMapPos_BottomLeft"] = "Снизу слева",

                    // --- Section headers ---
                    ["Section_General"] = "1. Общие",
                    ["Section_DynamicMarkers"] = "2. Динамические маркеры",
                    ["Section_Progression"] = "3. Прогрессия",
                    ["Section_InRaid"] = "4. В рейде",
                    ["Section_MiniMap"] = "5. Мини-карта",
                    ["Section_MarkerColors"] = "6. Цвета маркеров",

                    // --- 1. General (Names) ---
                    ["Name_ReplaceMapScreen"] = "Заменить экран карты",
                    ["Name_CenterOnPlayerHotkey"] = "Центрирование на игроке",
                    ["Name_MoveMapUpHotkey"] = "Перемещение карты вверх",
                    ["Name_MoveMapDownHotkey"] = "Перемещение карты вниз",
                    ["Name_MoveMapLeftHotkey"] = "Перемещение карты влево",
                    ["Name_MoveMapRightHotkey"] = "Перемещение карты вправо",
                    ["Name_MapMoveHotkeySpeed"] = "Скорость перемещения карты",
                    ["Name_ChangeMapLevelUpHotkey"] = "Уровень карты вверх",
                    ["Name_ChangeMapLevelDownHotkey"] = "Уровень карты вниз",
                    ["Name_ZoomMainMapToMouse"] = "Масштабирование к курсору",
                    ["Name_ZoomMapInHotkey"] = "Приблизить карту",
                    ["Name_ZoomMapOutHotkey"] = "Отдалить карту",
                    ["Name_ZoomMapHotkeySpeed"] = "Скорость масштабирования",
                    ["Name_DumpInfoHotkey"] = "Выгрузить информацию",

                    // --- 2. Dynamic Markers (Names) ---
                    ["Name_ShowPlayerMarker"] = "Показывать маркер игрока",
                    ["Name_PlayerMarkerUpdateInterval"] = "Интервал обновления маркера игрока",
                    ["Name_PlayerMarkerSmoothing"] = "Сглаживание маркера игрока",
                    ["Name_OtherMarkersUpdateInterval"] = "Интервал обновления других маркеров",
                    ["Name_OtherMarkersSmoothing"] = "Сглаживание других маркеров",
                    ["Name_ShowFriendlyPlayerMarkersInRaid"] = "Маркеры дружественных игроков",
                    ["Name_ShowEnemyPlayerMarkersInRaid"] = "Маркеры вражеских игроков",
                    ["Name_ShowScavMarkersInRaid"] = "Маркеры диких",
                    ["Name_ShowBossMarkersInRaid"] = "Маркеры боссов",
                    ["Name_ShowLockedDoorStatus"] = "Статус запертых дверей",
                    ["Name_ShowQuestsInRaid"] = "Показывать задания",
                    ["Name_ShowExtractsInRaid"] = "Показывать выходы",
                    ["Name_ShowExtractStatusInRaid"] = "Показывать статус выходов",
                    ["Name_ShowTransitPointsInRaid"] = "Показывать переходы",
                    ["Name_ShowSecretPointsInRaid"] = "Показывать секретные выходы",
                    ["Name_ShowDroppedBackpackInRaid"] = "Показывать выброшенные рюкзаки",
                    ["Name_ShowWishListItemsInRaid"] = "Показывать предметы из списка желаний",
                    ["Name_ShowBTRInRaid"] = "Показывать БТР",
                    ["Name_ShowAirdropsInRaid"] = "Показывать аирдропы",
                    ["Name_ShowHiddenStashesInRaid"] = "Показывать скрытые тайники",
                    ["Name_ShowFriendlyCorpsesInRaid"] = "Показывать трупы дружественных",
                    ["Name_ShowKilledCorpsesInRaid"] = "Показывать трупы, убитые игроком",
                    ["Name_ShowFriendlyKilledCorpsesInRaid"] = "Показывать трупы, убитые дружественными",
                    ["Name_ShowBossCorpsesInRaid"] = "Показывать трупы боссов",
                    ["Name_ShowOtherCorpsesInRaid"] = "Показывать другие трупы",

                    // --- 3. Progression (Names) ---
                    ["Name_RequireMapInInventory"] = "Требовать карту в инвентаре",
                    ["Name_ShowPmcIntelLevel"] = "Уровень разведки для ЧВК",
                    ["Name_ShowBossIntelLevel"] = "Уровень разведки для боссов",
                    ["Name_ShowScavIntelLevel"] = "Уровень разведки для диких",
                    ["Name_ShowFriendlyIntelLevel"] = "Уровень разведки для дружественных ЧВК",
                    ["Name_ShowCorpseIntelLevel"] = "Уровень разведки для трупов",
                    ["Name_ShowAirdropIntelLevel"] = "Уровень разведки для аирдропов",
                    ["Name_ShowWishListItemsIntelLevel"] = "Уровень разведки для предметов из списка желаний",
                    ["Name_ShowHiddenStashIntelLevel"] = "Уровень разведки для скрытых тайников",

                    // --- 4. In-Raid (Names) ---
                    ["Name_AutoSelectLevel"] = "Автовыбор уровня",
                    ["Name_AutoCenterOnPlayerMarker"] = "Автоцентрирование на игроке",
                    ["Name_ResetZoomOnCenter"] = "Сбрасывать масштаб при центрировании",
                    ["Name_RetainMapPosition"] = "Запоминать позицию карты",
                    ["Name_CenteringZoomResetPoint"] = "Масштаб при центрировании на игроке",
                    ["Name_ZoomMainMap"] = "Масштаб основной карты",
                    ["Name_PeekShortcut"] = "Клавиша подглядывания карты",
                    ["Name_HoldForPeek"] = "Удерживать для подглядывания",
                    ["Name_MapTransitionEnabled"] = "Анимация перехода подглядывания",

                    // --- 5. Mini-map (Names) ---
                    ["Name_MiniMapEnabled"] = "Мини-карта включена",
                    ["Name_MiniMapPosition"] = "Позиция мини-карты",
                    ["Name_MiniMapSizeX"] = "Ширина мини-карты",
                    ["Name_MiniMapSizeY"] = "Высота мини-карты",
                    ["Name_MiniMapScreenOffsetX"] = "Горизонтальное смещение мини-карты",
                    ["Name_MiniMapScreenOffsetY"] = "Вертикальное смещение мини-карты",
                    ["Name_MiniMapShowOrHide"] = "Показать или скрыть мини-карту",
                    ["Name_ZoomMiniMap"] = "Масштаб мини-карты",
                    ["Name_MiniMapScaleMultiplier"] = "Множитель масштаба мини-карты",
                    ["Name_ZoomInMiniMapHotkey"] = "Клавиша приближения мини-карты",
                    ["Name_ZoomOutMiniMapHotkey"] = "Клавиша отдаления мини-карты",

                    // --- 6. Marker Colors (Names) ---
                    ["Name_PlayerColor"] = "Цвет маркера игрока",
                    ["Name_PmcBearColor"] = "Цвет маркера Bear",
                    ["Name_PmcUsecColor"] = "Цвет маркера Usec",
                    ["Name_ScavColor"] = "Цвет маркера дикого",
                    ["Name_BossColor"] = "Цвет маркера босса",
                    ["Name_AirdropColor"] = "Цвет маркера аирдропа",
                    ["Name_BackpackColor"] = "Цвет маркера рюкзака",
                    ["Name_LootItemColor"] = "Цвет маркера лута",
                    ["Name_KilledCorpseColor"] = "Цвет маркера убитого трупа",
                    ["Name_KilledBossColor"] = "Цвет маркера убитого босса",
                    ["Name_KilledOtherColor"] = "Цвет маркера трупа, убитого другим",
                    ["Name_BtrColor"] = "Цвет маркера БТР",
                    ["Name_ExtractDefaultColor"] = "Цвет выхода по умолчанию",
                    ["Name_ExtractOpenColor"] = "Цвет открытого выхода",
                    ["Name_ExtractClosedColor"] = "Цвет закрытого выхода",
                    ["Name_ExtractHasRequirementsColor"] = "Цвет выхода с требованиями",
                    ["Name_TransPointColor"] = "Цвет маркера точки перехода",
                    ["Name_SecretPointColor"] = "Цвет маркера секретного выхода",
                    ["Name_HiddenStashColor"] = "Цвет маркера скрытого тайника",

                    // --- 1. General (Descriptions) ---
                    ["Desc_ReplaceMapScreen"] = "Если карта должна заменять стандартный экран карты BSG; для обновления требуется переключиться с модифицированной карты",
                    ["Desc_CenterOnPlayerHotkey"] = "Нажатие при открытой карте центрирует на игроке",
                    ["Desc_MoveMapUpHotkey"] = "Горячая клавиша для перемещения карты вверх",
                    ["Desc_MoveMapDownHotkey"] = "Горячая клавиша для перемещения карты вниз",
                    ["Desc_MoveMapLeftHotkey"] = "Горячая клавиша для перемещения карты влево",
                    ["Desc_MoveMapRightHotkey"] = "Горячая клавиша для перемещения карты вправо",
                    ["Desc_MapMoveHotkeySpeed"] = "Как быстро должна перемещаться карта; единицы — процент карты в секунду",
                    ["Desc_ChangeMapLevelUpHotkey"] = "Горячая клавиша для переключения уровня карты вверх (Shift+прокрутка вверх также работает на экране карты)",
                    ["Desc_ChangeMapLevelDownHotkey"] = "Горячая клавиша для переключения уровня карты вниз (Shift+прокрутка вниз также работает на экране карты)",
                    ["Desc_ZoomMainMapToMouse"] = "Должны ли горячие клавиши масштабирования приближать к позиции курсора или к центру на экране карты в инвентаре / вне рейда",
                    ["Desc_ZoomMapInHotkey"] = "Горячая клавиша для приближения карты (прокрутка вверх также работает на экране карты)",
                    ["Desc_ZoomMapOutHotkey"] = "Горячая клавиша для отдаления карты (прокрутка вниз также работает на экране карты)",
                    ["Desc_ZoomMapHotkeySpeed"] = "Как быстро карта масштабируется горячими клавишами",
                    ["Desc_DumpInfoHotkey"] = "Нажатие при открытой карте выгружает JSON MarkerDefs для выходов, лута и переключателей в корень папки плагина",

                    // --- 2. Dynamic Markers (Descriptions) ---
                    ["Desc_ShowPlayerMarker"] = "Показывать ли маркер игрока в рейде",
                    ["Desc_PlayerMarkerUpdateInterval"] = "Как часто (в секундах) маркер игрока опрашивает позицию игрока. Меньше = отзывчивее. 0.016 \u2248 каждый кадр, 0.05 = 20 Гц, 0.5 = старое значение по умолчанию (с задержкой).",
                    ["Desc_PlayerMarkerSmoothing"] = "Сила сглаживания маркера игрока (0 = выкл./привязка, 0.95 = максимальное сглаживание). Интерполирует маркер между опросами для плавного вида.",
                    ["Desc_OtherMarkersUpdateInterval"] = "Как часто (в секундах) другие динамические маркеры (боты, дружественные ЧВК, БТР, аирдропы) опрашивают свой transform. Меньше = плавнее, но больше нагрузка на CPU. 0.016 \u2248 каждый кадр, 0.1 = 10 Гц, 0.5 = старое значение по умолчанию.",
                    ["Desc_OtherMarkersSmoothing"] = "Сила сглаживания других динамических маркеров (0 = выкл./привязка, 0.95 = максимальное сглаживание).",
                    ["Desc_ShowFriendlyPlayerMarkersInRaid"] = "Показывать ли маркеры дружественных игроков в рейде",
                    ["Desc_ShowEnemyPlayerMarkersInRaid"] = "Показывать ли маркеры вражеских игроков в рейде (обычно для отладки)",
                    ["Desc_ShowScavMarkersInRaid"] = "Показывать ли маркеры вражеских диких в рейде (обычно для отладки)",
                    ["Desc_ShowBossMarkersInRaid"] = "Показывать ли маркеры вражеских боссов в рейде",
                    ["Desc_ShowLockedDoorStatus"] = "Обновлять ли маркеры запертых дверей статусом в зависимости от наличия ключа",
                    ["Desc_ShowQuestsInRaid"] = "Показывать ли задания в рейде",
                    ["Desc_ShowExtractsInRaid"] = "Показывать ли выходы в рейде",
                    ["Desc_ShowExtractStatusInRaid"] = "Окрашивать ли выходы в соответствии с их статусом в рейде",
                    ["Desc_ShowTransitPointsInRaid"] = "Показывать ли переходы в рейде",
                    ["Desc_ShowSecretPointsInRaid"] = "Показывать ли секретные выходы в рейде",
                    ["Desc_ShowDroppedBackpackInRaid"] = "Показывать ли выброшенные рюкзаки игрока (не чужие) в рейде",
                    ["Desc_ShowWishListItemsInRaid"] = "Показывает предметы из вашего списка желаний на карте в рейде",
                    ["Desc_ShowBTRInRaid"] = "Показывать ли БТР в рейде",
                    ["Desc_ShowAirdropsInRaid"] = "Показывать ли аирдропы в рейде после приземления",
                    ["Desc_ShowHiddenStashesInRaid"] = "Показывать ли скрытые тайники в рейде",
                    ["Desc_ShowFriendlyCorpsesInRaid"] = "Показывать ли трупы дружественных в рейде",
                    ["Desc_ShowKilledCorpsesInRaid"] = "Показывать ли трупы, убитые игроком, в рейде; убитые боссы будут отображаться другим цветом",
                    ["Desc_ShowFriendlyKilledCorpsesInRaid"] = "Показывать ли трупы, убитые дружественными игроками, в рейде; убитые боссы будут отображаться другим цветом",
                    ["Desc_ShowBossCorpsesInRaid"] = "Показывать ли трупы боссов (кроме убитых игроком) в рейде",
                    ["Desc_ShowOtherCorpsesInRaid"] = "Показывать ли другие трупы (кроме дружественных или убитых игроком) в рейде",

                    // --- 3. Progression (Descriptions) ---
                    ["Desc_RequireMapInInventory"] = "Требует наличия карты в инвентаре для просмотра карты в рейде.",
                    ["Desc_ShowPmcIntelLevel"] = "Если уровень разведки не ниже этого значения, будут показываться ЧВК",
                    ["Desc_ShowBossIntelLevel"] = "Если уровень разведки не ниже этого значения, будут показываться боссы",
                    ["Desc_ShowScavIntelLevel"] = "Если уровень разведки не ниже этого значения, будут показываться дикие",
                    ["Desc_ShowFriendlyIntelLevel"] = "Если уровень разведки не ниже этого значения, будут показываться дружественные ЧВК",
                    ["Desc_ShowCorpseIntelLevel"] = "Если уровень разведки не ниже этого значения, будут показываться трупы",
                    ["Desc_ShowAirdropIntelLevel"] = "Если уровень разведки не ниже этого значения, будут показываться аирдропы",
                    ["Desc_ShowWishListItemsIntelLevel"] = "Если уровень разведки не ниже этого значения, будут показываться предметы из списка желаний",
                    ["Desc_ShowHiddenStashIntelLevel"] = "Если уровень разведки не ниже этого значения, будут показываться скрытые тайники",

                    // --- 4. In-Raid (Descriptions) ---
                    ["Desc_AutoSelectLevel"] = "Автоматически выбирать уровень в зависимости от позиции игрока в рейде",
                    ["Desc_AutoCenterOnPlayerMarker"] = "Центрировать ли маркер игрока при показе карты в рейде (конфликтует с «Remember Map Position»)",
                    ["Desc_ResetZoomOnCenter"] = "Сбрасывать ли масштаб каждый раз при открытии карты в рейде (конфликтует с «Remember Map Position»)",
                    ["Desc_RetainMapPosition"] = "Запоминать ли позицию карты (память позиции карты сохраняется только для текущего рейда) (конфликтует с «Auto Center On Player Marker» и «Reset Zoom On Center»)",
                    ["Desc_CenteringZoomResetPoint"] = "Какой масштаб использовать при центрировании на игроке (0 — полностью отдалено, 1 — полностью приближено)",
                    ["Desc_ZoomMainMap"] = "Какой масштаб использовать для основной карты (вид по Tab / вид подглядывания) (0 — полностью отдалено, 1 — полностью приближено)",
                    ["Desc_PeekShortcut"] = "Комбинация клавиш для подглядывания карты",
                    ["Desc_HoldForPeek"] = "Удерживать ли клавишу, чтобы карта оставалась открытой. Если отключено, клавиша переключает",
                    ["Desc_MapTransitionEnabled"] = "Включить анимации перехода карты (при отключении всё будет резко переключаться)",

                    // --- 5. Mini-map (Descriptions) ---
                    ["Desc_MiniMapEnabled"] = "Включить мини-карту",
                    ["Desc_MiniMapPosition"] = "В каком углу отображается мини-карта",
                    ["Desc_MiniMapSizeX"] = "Горизонтальный размер мини-карты",
                    ["Desc_MiniMapSizeY"] = "Вертикальный размер мини-карты",
                    ["Desc_MiniMapScreenOffsetX"] = "Горизонтальное смещение от края (значения обновляются в соответствии с разрешением экрана; ТРЕБУЕТСЯ ПЕРЕЗАПУСК ПРИ СМЕНЕ РАЗРЕШЕНИЯ)",
                    ["Desc_MiniMapScreenOffsetY"] = "Вертикальное смещение от края (значения обновляются в соответствии с разрешением экрана; ТРЕБУЕТСЯ ПЕРЕЗАПУСК ПРИ СМЕНЕ РАЗРЕШЕНИЯ)",
                    ["Desc_MiniMapShowOrHide"] = "Показать или скрыть мини-карту",
                    ["Desc_ZoomMiniMap"] = "Какой масштаб использовать для мини-карты (0 — полностью отдалено, 1 — полностью приближено)",
                    ["Desc_MiniMapScaleMultiplier"] = "Базовый множитель масштаба мини-карты поверх зума. Увеличьте для большей мини-карты (например, 2.75, чтобы вернуть старый жёстко заданный вид), уменьшите для меньшей.",
                    ["Desc_ZoomInMiniMapHotkey"] = "Клавиша приближения мини-карты",
                    ["Desc_ZoomOutMiniMapHotkey"] = "Клавиша отдаления мини-карты",

                    // --- 6. Marker Colors (Descriptions) ---
                    ["Desc_MarkerColor"] = "Цвет маркера",
                },
            };

        public static string T(string key)
        {
            if (_table.TryGetValue(_currentLang, out var lang) && lang.TryGetValue(key, out var value))
                return value;

            if (_table["En"].TryGetValue(key, out var fallback))
                return fallback;

            return key;
        }

        public static void SetLanguage(string lang)
        {
            var normalized = NormalizeLanguage(lang);
            if (!string.IsNullOrEmpty(normalized))
            {
                _currentLang = normalized;
            }
            else
            {
                NorvinskMapsPlugin.Log?.LogWarning($"[Localization] SetLanguage: cannot normalize '{lang}', keeping '{_currentLang}'");
            }
        }

        public static string CurrentLanguage => _currentLang;

        public static void DetectLanguage()
        {
            var detected = TryGetSptLanguage();
            if (string.IsNullOrEmpty(detected))
                detected = TryGetUnityLanguage();

            var normalized = NormalizeLanguage(detected);

            if (!string.IsNullOrEmpty(normalized))
            {
                _currentLang = normalized;
                return;
            }

            _currentLang = "En";
        }

        private static string TryGetSptLanguage()
        {
            try
            {
                Type helperType = null;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var t = asm.GetType("EFT.LocalizedHelper", false)
                         ?? asm.GetType("LocalizedHelper", false);
                    if (t != null) { helperType = t; break; }
                }

                if (helperType == null)
                {
                    return null;
                }

                const System.Reflection.BindingFlags flags =
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Static;

                var prop = helperType.GetProperty("CurrentLanguage", flags)
                        ?? helperType.GetProperty("Language", flags);
                if (prop != null)
                {
                    var v = prop.GetValue(null);
                    if (v is string s && !string.IsNullOrEmpty(s))
                        return s;
                }

                var field = helperType.GetField("CurrentLanguage", flags)
                         ?? helperType.GetField("_currentLanguage", flags)
                         ?? helperType.GetField("Language", flags);
                if (field != null)
                {
                    var v = field.GetValue(null);
                    if (v is string s && !string.IsNullOrEmpty(s))
                        return s;

                    if (v != null) return v.ToString();
                }
            }
            catch (Exception e)
            {
                NorvinskMapsPlugin.Log?.LogWarning($"[Localization] SPT detection failed: {e.Message}");
            }

            return null;
        }

        private static string TryGetUnityLanguage()
        {
            try
            {
                switch (Application.systemLanguage)
                {
                    case SystemLanguage.Russian: return "ru";
                    case SystemLanguage.English: return "en";
                }
            }
            catch { }
            return null;
        }

        private static string NormalizeLanguage(string lang)
        {
            if (string.IsNullOrEmpty(lang)) return null;

            var main = lang.Split('-', '_')[0].Trim().ToLowerInvariant();

            switch (main)
            {
                case "ru": return "Ru";
                case "en": return "En";
                default: return null;
            }
        }
    }
}