using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP.Configuration;
using Comfort.Common;
using DG.Tweening;
using NorvinskMaps.Config;
using NorvinskMaps.Data;
using NorvinskMaps.DynamicMarkers;
using NorvinskMaps.Patches;
using NorvinskMaps.UI.Components;
using NorvinskMaps.UI.Controls;
using NorvinskMaps.Utils;
using EFT;
using EFT.UI;
using Il2CppInterop.Runtime.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace NorvinskMaps.UI
{
    public class ModdedMapScreen : MonoBehaviour
    {
        #region Variables and Declarations
        private EventHandler _adjustMiniMapHandler;
        private EventHandler _markerSettingsHandler;

        private const string MapRelPath = "Maps";

        private static readonly float PositionTweenTime = 0.25f;
        private static readonly float ScrollZoomScaler = 1.75f;
        private float _targetZoom = 0f;
        private Vector2 _zoomFocusPoint = Vector2.zero;
        private static readonly float ScrollZoomLerpSpeed = 8f;
        private static readonly Vector2 MapSelectDropdownPosition = new Vector2(0f, -50f);
        private static readonly Vector2 MapSelectDropdownSize = new Vector2(360f, 30f);
        private static readonly Vector2 MaskSizeModifierInRaid = new Vector2(0, -42f);
        private static readonly Vector2 MaskPositionInRaid = new Vector2(15, -400f);
        private static readonly Vector2 MaskSizeModifierOutOfRaid = new Vector2(15, -400f);
        private static readonly Vector2 MaskPositionOutOfRaid = new Vector2(0, -5f);
        private static readonly Vector2 TextAnchor = new Vector2(0f, 1f);
        private static readonly Vector2 CursorPositionTextOffset = new Vector2(15f, -52f);
        private static readonly Vector2 PlayerPositionTextOffset = new Vector2(15f, -62f);
        private static readonly float PositionTextFontSize = 15f;

        private RectTransform RectTransform => gameObject.GetRectTransform();

        private RectTransform ParentTransform => gameObject.transform.parent as RectTransform;

        public RectTransform MapScreenTrueParent { get; set; }

        private bool _isShown = false;

        private ScrollRect _scrollRect;
        private Mask _scrollMask;
        internal MapView MapView => _mapView;
        private MapView _mapView;

        private LevelSelectSlider _levelSelectSlider;
        private MapSelectDropdown _mapSelectDropdown;
        private CursorPositionText _cursorPositionText;
        private PlayerPositionText _playerPositionText;

        private MapPeekComponent _peekComponent;
        private bool IsPeeking => _peekComponent != null && _peekComponent.IsPeeking;
        private bool ShowingMiniMap => _peekComponent != null && _peekComponent.ShowingMiniMap;
        private Vector2 _savedMainMapPos = Vector2.zero;

        private RectTransform _savedTrueParent;

        public RectTransform SavedTrueParent => _savedTrueParent;

        [HideFromIl2Cpp]
        public void SavedTrueParent_ForceSet(RectTransform parent)
        {
            if (parent == null) return;
            _savedTrueParent = parent;
        }

        public bool IsShowingMapScreen { get; private set; }

        public bool IsMapScreenShownFullscreen { get; private set; }

        private readonly Dictionary<Type, IDynamicMarkerProvider> _dynamicMarkerProviders = [];

        private readonly float _miniMapUpdateInterval = 0.033f;
        private float _miniMapUpdateTimer = 0f;
        private float _levelUpdateTimer = 0f;
        private const float LevelUpdateInterval = 0.3f;
        private int _lastAutoSelectedLevel = int.MinValue;

        private bool _autoCenterOnPlayerMarker = true;
        private bool _autoSelectLevel = true;
        private bool _resetZoomOnCenter = false;
        private bool _rememberMapPosition = true;
        private bool _transitionAnimations = true;
        private bool _firstFullscreenShown = false;
        private bool _pendingFullscreenRecalc = false;
        private float _updateLogTimer = 1f;
        private float _dynamicMarkerRefreshTimer = 1f;
        private const float DynamicMarkerRefreshInterval = 1f;

        private float _centeringZoomResetPoint = 0f;
        private KeyboardShortcut _centerPlayerShortcut;
        private KeyboardShortcut _dumpShortcut;
        private KeyboardShortcut _moveMapUpShortcut;
        private KeyboardShortcut _moveMapDownShortcut;
        private KeyboardShortcut _moveMapLeftShortcut;
        private KeyboardShortcut _moveMapRightShortcut;
        private float _moveMapSpeed = 0.25f;
        private KeyboardShortcut _moveMapLevelUpShortcut;
        private KeyboardShortcut _moveMapLevelDownShortcut;

        private bool _zoomMainMapToMouse;
        private KeyboardShortcut _zoomMainMapInShortcut;
        private KeyboardShortcut _zoomMainMapOutShortcut;

        private KeyboardShortcut _zoomMiniMapInShortcut;
        private KeyboardShortcut _zoomMiniMapOutShortcut;

        public static DmServerConfig ServerConfig;
        public class DmServerConfig
        {
            public bool AllowShowFriendlyPlayerMarkersInRaid;
            public bool AllowShowEnemyPlayerMarkersInRaid;
            public bool AllowShowBossMarkersInRaid;
            public bool AllowShowScavMarkersInRaid;
        }

        private float _zoomMapHotkeySpeed = 2.5f;

        #endregion

        internal static ModdedMapScreen Create(GameObject parent)
        {
            var go = UIUtils.CreateUIGameObject(parent, "ModdedMapBlock");
            return go.AddComponent<ModdedMapScreen>();
        }

        #region Unity Methods

        private void Awake()
        {
            // ФИКС DOTWEEN: увеличиваем пул твинов заранее, чтобы не было
            // авто-ресайза из 512/50 в 1280/50 (и выше) в рантайме.
            try { DG.Tweening.DOTween.SetTweensCapacity(2000, 200); }
            catch (Exception e) { NorvinskMapsPlugin.Log.LogDebug($"[Awake] DOTween capacity: {e.Message}"); }

            var scrollRectGo = UIUtils.CreateUIGameObject(gameObject, "Scroll");
            var scrollMaskGo = UIUtils.CreateUIGameObject(scrollRectGo, "ScrollMask");

            _adjustMiniMapHandler = OnMiniMapSettingChanged;
            Settings.MiniMapPosition.SettingChanged += _adjustMiniMapHandler;
            Settings.MiniMapScreenOffsetX.SettingChanged += _adjustMiniMapHandler;
            Settings.MiniMapScreenOffsetY.SettingChanged += _adjustMiniMapHandler;
            Settings.MiniMapSizeX.SettingChanged += _adjustMiniMapHandler;
            Settings.MiniMapSizeY.SettingChanged += _adjustMiniMapHandler;

            _markerSettingsHandler = OnMarkerSettingsChanged;
            Settings.ShowPlayerMarker.SettingChanged += _markerSettingsHandler;
            Settings.ShowFriendlyPlayerMarkersInRaid.SettingChanged += _markerSettingsHandler;
            Settings.ShowEnemyPlayerMarkersInRaid.SettingChanged += _markerSettingsHandler;
            Settings.ShowScavMarkersInRaid.SettingChanged += _markerSettingsHandler;
            Settings.ShowBossMarkersInRaid.SettingChanged += _markerSettingsHandler;
            Settings.ShowQuestsInRaid.SettingChanged += _markerSettingsHandler;
            Settings.ShowLockedDoorStatus.SettingChanged += _markerSettingsHandler;
            Settings.ShowDroppedBackpackInRaid.SettingChanged += _markerSettingsHandler;
            Settings.ShowBTRInRaid.SettingChanged += _markerSettingsHandler;
            Settings.ShowAirdropsInRaid.SettingChanged += _markerSettingsHandler;
            Settings.ShowWishListItemsInRaid.SettingChanged += _markerSettingsHandler;
            Settings.ShowHiddenStashesInRaid.SettingChanged += _markerSettingsHandler;
            Settings.ShowTransitPointsInRaid.SettingChanged += _markerSettingsHandler;
            Settings.ShowSecretPointsInRaid.SettingChanged += _markerSettingsHandler;
            Settings.ShowFriendlyCorpsesInRaid.SettingChanged += _markerSettingsHandler;
            Settings.ShowKilledCorpsesInRaid.SettingChanged += _markerSettingsHandler;
            Settings.ShowFriendlyKilledCorpsesInRaid.SettingChanged += _markerSettingsHandler;
            Settings.ShowBossCorpsesInRaid.SettingChanged += _markerSettingsHandler;
            Settings.ShowOtherCorpsesInRaid.SettingChanged += _markerSettingsHandler;

            _mapView = MapView.Create(scrollMaskGo, "MapView");

            var scrollMaskImage = scrollMaskGo.AddComponent<Image>();
            scrollMaskImage.color = new Color(0f, 0f, 0f, 0.5f);
            scrollMaskImage.raycastTarget = true;
            _scrollMask = scrollMaskGo.AddComponent<Mask>();

            _scrollRect = scrollRectGo.AddComponent<ScrollRect>();
            _scrollRect.scrollSensitivity = 0;
            _scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
            _scrollRect.inertia = false;
            _scrollRect.horizontal = true;
            _scrollRect.vertical = true;
            _scrollRect.viewport = _scrollMask.GetRectTransform();
            _scrollRect.content = _mapView.RectTransform;
            _scrollRect.enabled = true;

            var sliderPrefab = Singleton<CommonUI>.Instance.transform
                .Find("Common UI/InventoryScreen/Map Panel/MapBlock/ZoomScroll").gameObject;
            _levelSelectSlider = LevelSelectSlider.Create(sliderPrefab, RectTransform);
            _levelSelectSlider.OnLevelSelectedBySlider += _mapView.SelectTopLevel;
            _mapView.OnLevelSelected += (level) => _levelSelectSlider.SelectedLevel = level;
            _levelSelectSlider.gameObject.SetActive(false);

            var selectPrefab = Singleton<CommonUI>.Instance.transform
                .Find("Common UI/InventoryScreen/SkillsAndMasteringPanel/BottomPanel/SkillsPanel/Options/Filter").gameObject;
            _mapSelectDropdown = MapSelectDropdown.Create(selectPrefab, RectTransform);
            _mapSelectDropdown.OnMapSelected += ChangeMap;

            try
            {
                _cursorPositionText = CursorPositionText.Create(gameObject, _mapView.RectTransform, PositionTextFontSize);
                if (_cursorPositionText != null)
                {
                    _cursorPositionText.RectTransform.anchorMin = TextAnchor;
                    _cursorPositionText.RectTransform.anchorMax = TextAnchor;
                }

                _playerPositionText = PlayerPositionText.Create(gameObject, PositionTextFontSize);
                if (_playerPositionText != null)
                {
                    _playerPositionText.RectTransform.anchorMin = TextAnchor;
                    _playerPositionText.RectTransform.anchorMax = TextAnchor;
                    _playerPositionText.gameObject.SetActive(false);
                }
            }
            catch (Exception e)
            {
                NorvinskMapsPlugin.Log.LogError($"[ModdedMapScreen] Texts: {e.Message}");
            }

            ReadConfig();

            _mapSelectDropdown.LoadMapDefsFromPath(MapRelPath);

            if (_mapSelectDropdown != null)
            {
                _mapSelectDropdown.transform.SetAsLastSibling();
            }

            GameWorldOnDestroyPatch.OnRaidEnd += OnRaidEnd;

            if (ServerConfig == null)
            {
                ServerConfig = new DmServerConfig
                {
                    AllowShowFriendlyPlayerMarkersInRaid = true,
                    AllowShowEnemyPlayerMarkersInRaid = true,
                    AllowShowBossMarkersInRaid = true,
                    AllowShowScavMarkersInRaid = true,
                };
            }
        }

        [HideFromIl2Cpp]
        private void OnMiniMapSettingChanged(object sender, EventArgs e)
        {
            AdjustForMiniMap(false);
        }

        [HideFromIl2Cpp]
        private void OnMarkerSettingsChanged(object sender, EventArgs e)
        {
            try
            {
                ReadConfig();
            }
            catch (Exception ex)
            {
                NorvinskMapsPlugin.Log.LogError($"[OnMarkerSettingsChanged] {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            GameWorldOnDestroyPatch.OnRaidEnd -= OnRaidEnd;

            Settings.MiniMapPosition.SettingChanged -= _adjustMiniMapHandler;
            Settings.MiniMapScreenOffsetX.SettingChanged -= _adjustMiniMapHandler;
            Settings.MiniMapScreenOffsetY.SettingChanged -= _adjustMiniMapHandler;
            Settings.MiniMapSizeX.SettingChanged -= _adjustMiniMapHandler;
            Settings.MiniMapSizeY.SettingChanged -= _adjustMiniMapHandler;

            if (_markerSettingsHandler != null)
            {
                Settings.ShowPlayerMarker.SettingChanged -= _markerSettingsHandler;
                Settings.ShowFriendlyPlayerMarkersInRaid.SettingChanged -= _markerSettingsHandler;
                Settings.ShowEnemyPlayerMarkersInRaid.SettingChanged -= _markerSettingsHandler;
                Settings.ShowScavMarkersInRaid.SettingChanged -= _markerSettingsHandler;
                Settings.ShowBossMarkersInRaid.SettingChanged -= _markerSettingsHandler;
                Settings.ShowQuestsInRaid.SettingChanged -= _markerSettingsHandler;
                Settings.ShowLockedDoorStatus.SettingChanged -= _markerSettingsHandler;
                Settings.ShowDroppedBackpackInRaid.SettingChanged -= _markerSettingsHandler;
                Settings.ShowBTRInRaid.SettingChanged -= _markerSettingsHandler;
                Settings.ShowAirdropsInRaid.SettingChanged -= _markerSettingsHandler;
                Settings.ShowWishListItemsInRaid.SettingChanged -= _markerSettingsHandler;
                Settings.ShowHiddenStashesInRaid.SettingChanged -= _markerSettingsHandler;
                Settings.ShowTransitPointsInRaid.SettingChanged -= _markerSettingsHandler;
                Settings.ShowSecretPointsInRaid.SettingChanged -= _markerSettingsHandler;
                Settings.ShowFriendlyCorpsesInRaid.SettingChanged -= _markerSettingsHandler;
                Settings.ShowKilledCorpsesInRaid.SettingChanged -= _markerSettingsHandler;
                Settings.ShowFriendlyKilledCorpsesInRaid.SettingChanged -= _markerSettingsHandler;
                Settings.ShowBossCorpsesInRaid.SettingChanged -= _markerSettingsHandler;
                Settings.ShowOtherCorpsesInRaid.SettingChanged -= _markerSettingsHandler;
            }
        }

        [HideFromIl2Cpp]
        private bool IsActuallyInRaid()
        {
            if (_peekComponent == null) return false;
            try
            {
                return _peekComponent.gameObject != null;
            }
            catch
            {
                return false;
            }
        }

        [HideFromIl2Cpp]
        public void ForceAutoSelectLevel()
        {
            if (_mapView == null) return;
            if (ShowingMiniMap) return;

            var player = GameUtils.GetMainPlayer();
            if (player == null) return;

            var mapPosition = MathUtils.ConvertToMapPosition(((IPlayer)player).Position);
            _mapView.SelectLevelByCoords(mapPosition);

            _lastAutoSelectedLevel = _mapView.SelectedLevel;
        }

        private void Update()
        {
            var isFullscreenMap = !ShowingMiniMap && !IsPeeking;

            var scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f && isFullscreenMap)
            {
                if (_mapSelectDropdown == null || !_mapSelectDropdown.isActiveAndEnabled || !_mapSelectDropdown.IsDropdownOpen())
                {
                    OnScroll(scroll);
                }
            }

            if (isFullscreenMap)
            {
                OnScrollZoomUpdate();
            }

            if (!ShowingMiniMap && _levelSelectSlider != null)
            {
                if (_moveMapLevelUpShortcut.BetterIsDown()) _levelSelectSlider.ChangeLevelBy(1);
                if (_moveMapLevelDownShortcut.BetterIsDown()) _levelSelectSlider.ChangeLevelBy(-1);
            }

            var shiftMapX = 0f;
            var shiftMapY = 0f;

            if (!ShowingMiniMap)
            {
                if (_moveMapUpShortcut.BetterIsPressed()) shiftMapY += 1f;
                if (_moveMapDownShortcut.BetterIsPressed()) shiftMapY -= 1f;
                if (_moveMapLeftShortcut.BetterIsPressed()) shiftMapX -= 1f;
                if (_moveMapRightShortcut.BetterIsPressed()) shiftMapX += 1f;
            }

            if (shiftMapX != 0f || shiftMapY != 0f)
            {
                _targetZoom = 0f;
                _mapView.CancelPositionTween();
                _mapView.ScaledShiftMap(new Vector2(shiftMapX, shiftMapY), _moveMapSpeed * Time.deltaTime, false);
            }

            if (ShowingMiniMap) OnZoomMini();
            else OnZoomMain();

            OnCenter();

            if (!ShowingMiniMap && _autoSelectLevel && _mapView != null)
            {
                _levelUpdateTimer -= Time.deltaTime;
                if (_levelUpdateTimer <= 0f)
                {
                    _levelUpdateTimer = LevelUpdateInterval;

                    var player = GameUtils.GetMainPlayer();
                    if (player != null)
                    {
                        var mapPosition = MathUtils.ConvertToMapPosition(((IPlayer)player).Position);
                        _mapView.SelectLevelByCoords(mapPosition);
                        _lastAutoSelectedLevel = _mapView.SelectedLevel;
                    }
                }
            }

            if (_dumpShortcut.BetterIsDown())
            {
                DumpUtils.DumpExtracts();
                DumpUtils.DumpSwitches();
                DumpUtils.DumpLocks();
            }

            TryRecalcAfterParentActivates();

            _dynamicMarkerRefreshTimer -= Time.deltaTime;
            if (_dynamicMarkerRefreshTimer <= 0f)
            {
                _dynamicMarkerRefreshTimer = DynamicMarkerRefreshInterval;
                TryRefreshDynamicMarkers();
            }

            _updateLogTimer -= Time.deltaTime;
        }

        [HideFromIl2Cpp]
        internal void ReadConfig()
        {
            _centerPlayerShortcut = Settings.CenterOnPlayerHotkey.Value;
            _dumpShortcut = Settings.DumpInfoHotkey.Value;
            _moveMapUpShortcut = Settings.MoveMapUpHotkey.Value;
            _moveMapDownShortcut = Settings.MoveMapDownHotkey.Value;
            _moveMapLeftShortcut = Settings.MoveMapLeftHotkey.Value;
            _moveMapRightShortcut = Settings.MoveMapRightHotkey.Value;
            _moveMapSpeed = Settings.MapMoveHotkeySpeed.Value;
            _moveMapLevelUpShortcut = Settings.ChangeMapLevelUpHotkey.Value;
            _moveMapLevelDownShortcut = Settings.ChangeMapLevelDownHotkey.Value;
            _zoomMainMapInShortcut = Settings.ZoomMapInHotkey.Value;
            _zoomMainMapOutShortcut = Settings.ZoomMapOutHotkey.Value;
            _zoomMiniMapInShortcut = Settings.ZoomInMiniMapHotkey.Value;
            _zoomMiniMapOutShortcut = Settings.ZoomOutMiniMapHotkey.Value;
            _zoomMapHotkeySpeed = Settings.ZoomMapHotkeySpeed.Value;
            _zoomMainMapToMouse = Settings.ZoomMainMapToMouse.Value;
            _autoCenterOnPlayerMarker = Settings.AutoCenterOnPlayerMarker.Value;
            _resetZoomOnCenter = Settings.ResetZoomOnCenter.Value;
            _rememberMapPosition = Settings.RetainMapPosition.Value;
            _autoSelectLevel = Settings.AutoSelectLevel.Value;
            _centeringZoomResetPoint = Settings.CenteringZoomResetPoint.Value;
            _transitionAnimations = Settings.MapTransitionEnabled.Value;

            if (_peekComponent is not null)
            {
                _peekComponent.PeekShortcut = Settings.PeekShortcut.Value;
                _peekComponent.HoldForPeek = Settings.HoldForPeek.Value;
                _peekComponent.HideMinimapShortcut = Settings.MiniMapShowOrHide.Value;
            }

            AddRemoveMarkerProvider<PlayerMarkerProvider>(Settings.ShowPlayerMarker.Value);
            AddRemoveMarkerProvider<QuestMarkerProvider>(Settings.ShowQuestsInRaid.Value);
            AddRemoveMarkerProvider<ExtractMarkerProvider>(Settings.ShowExtractsInRaid.Value);
            AddRemoveMarkerProvider<LockedDoorMarkerMutator>(Settings.ShowLockedDoorStatus.Value);
            AddRemoveMarkerProvider<BackpackMarkerProvider>(Settings.ShowDroppedBackpackInRaid.Value);
            AddRemoveMarkerProvider<BTRMarkerProvider>(Settings.ShowBTRInRaid.Value);
            AddRemoveMarkerProvider<AirdropMarkerProvider>(Settings.ShowAirdropsInRaid.Value);
            AddRemoveMarkerProvider<LootMarkerProvider>(Settings.ShowWishListItemsInRaid.Value);
            AddRemoveMarkerProvider<HiddenStashMarkerProvider>(Settings.ShowHiddenStashesInRaid.Value);
            AddRemoveMarkerProvider<TransitMarkerProvider>(Settings.ShowTransitPointsInRaid.Value);
            AddRemoveMarkerProvider<SecretMarkerProvider>(Settings.ShowSecretPointsInRaid.Value);

            if (Settings.ShowAirdropsInRaid.Value)
            {
                var p = GetMarkerProvider<AirdropMarkerProvider>();
                if (p != null) p.RefreshMarkers();
            }

            if (Settings.ShowWishListItemsInRaid.Value)
            {
                var p = GetMarkerProvider<LootMarkerProvider>();
                if (p != null) p.RefreshMarkers();
            }

            if (Settings.ShowHiddenStashesInRaid.Value)
            {
                var p = GetMarkerProvider<HiddenStashMarkerProvider>();
                if (p != null) p.RefreshMarkers();
            }

            if (Settings.ShowTransitPointsInRaid.Value)
            {
                var p = GetMarkerProvider<TransitMarkerProvider>();
                if (p != null) p.RefreshMarkers(_mapView);
            }

            if (Settings.ShowSecretPointsInRaid.Value)
            {
                var provider = GetMarkerProvider<SecretMarkerProvider>();
                if (provider != null)
                    provider.ShowExtractStatusInRaid = Settings.ShowExtractStatusInRaid.Value;
            }

            var needOtherPlayerMarkers = Settings.ShowFriendlyPlayerMarkersInRaid.Value
                                      || Settings.ShowEnemyPlayerMarkersInRaid.Value
                                      || Settings.ShowBossMarkersInRaid.Value
                                      || Settings.ShowScavMarkersInRaid.Value;

            AddRemoveMarkerProvider<OtherPlayersMarkerProvider>(needOtherPlayerMarkers);

            if (needOtherPlayerMarkers)
            {
                var provider = GetMarkerProvider<OtherPlayersMarkerProvider>();
                if (provider != null)
                {
                    provider.ShowFriendlyPlayers = (ServerConfig?.AllowShowFriendlyPlayerMarkersInRaid ?? true) && Settings.ShowFriendlyPlayerMarkersInRaid.Value;
                    provider.ShowEnemyPlayers = (ServerConfig?.AllowShowEnemyPlayerMarkersInRaid ?? true) && Settings.ShowEnemyPlayerMarkersInRaid.Value;
                    provider.ShowScavs = (ServerConfig?.AllowShowScavMarkersInRaid ?? true) && Settings.ShowScavMarkersInRaid.Value;
                    provider.ShowBosses = (ServerConfig?.AllowShowBossMarkersInRaid ?? true) && Settings.ShowBossMarkersInRaid.Value;

                    provider.RefreshMarkers();
                }
            }

            var needCorpseMarkers = Settings.ShowFriendlyCorpsesInRaid.Value
                                 || Settings.ShowKilledCorpsesInRaid.Value
                                 || Settings.ShowFriendlyKilledCorpsesInRaid.Value
                                 || Settings.ShowBossCorpsesInRaid.Value
                                 || Settings.ShowOtherCorpsesInRaid.Value;

            AddRemoveMarkerProvider<CorpseMarkerProvider>(needCorpseMarkers);
            if (needCorpseMarkers)
            {
                var provider = GetMarkerProvider<CorpseMarkerProvider>();
                if (provider != null)
                {
                    provider.ShowFriendlyCorpses = Settings.ShowFriendlyCorpsesInRaid.Value;
                    provider.ShowKilledCorpses = Settings.ShowKilledCorpsesInRaid.Value;
                    provider.ShowFriendlyKilledCorpses = Settings.ShowFriendlyKilledCorpsesInRaid.Value;
                    provider.ShowBossCorpses = Settings.ShowBossCorpsesInRaid.Value;
                    provider.ShowOtherCorpses = Settings.ShowOtherCorpsesInRaid.Value;

                    provider.RefreshMarkers();
                }
            }
        }

        [HideFromIl2Cpp]
        private void TryRefreshDynamicMarkers()
        {
            if (!IsActuallyInRaid()) return;
            if (_mapView == null) return;

            try
            {
                var q = GetMarkerProvider<QuestMarkerProvider>();
                q?.RefreshMarkers();
            }
            catch (Exception e)
            {
                NorvinskMapsPlugin.Log.LogError($"[TryRefreshDynamicMarkers] Quest: {e.Message}");
            }

            try
            {
                var c = GetMarkerProvider<CorpseMarkerProvider>();
                c?.RefreshMarkers();
            }
            catch (Exception e)
            {
                NorvinskMapsPlugin.Log.LogError($"[TryRefreshDynamicMarkers] Corpse: {e.Message}");
            }

            try
            {
                var o = GetMarkerProvider<OtherPlayersMarkerProvider>();
                o?.RefreshMarkers();
            }
            catch (Exception e)
            {
                NorvinskMapsPlugin.Log.LogError($"[TryRefreshDynamicMarkers] OtherPlayers: {e.Message}");
            }
        }

        [HideFromIl2Cpp]
        private void TryRecalcAfterParentActivates()
        {
            if (!_pendingFullscreenRecalc) return;
            if (!IsMapScreenShownFullscreen) return;
            if (transform.parent == null) return;
            if (!transform.parent.gameObject.activeInHierarchy) return;

            _pendingFullscreenRecalc = false;

            var selfRt = RectTransform;
            if (selfRt != null)
            {
                selfRt.DOKill(false);
                selfRt.anchorMin = Vector2.zero;
                selfRt.anchorMax = Vector2.one;
                selfRt.pivot = new Vector2(0.5f, 0.5f);
                selfRt.offsetMin = Vector2.zero;
                selfRt.offsetMax = Vector2.zero;
                selfRt.anchoredPosition = Vector2.zero;
                selfRt.localScale = Vector3.one;
            }

            ForceRecalcAfterActivation();

            if (_mapView != null && _scrollMask != null)
            {
                var maskSize = _scrollMask.GetRectTransform().rect.size;
                _mapView.EnlargeSizeDeltaForDrag(maskSize);
            }

            if (!_firstFullscreenShown && _mapView != null && _mapView.RectTransform != null
                && !_mapView.IsMiniMapActive)
            {
                _mapView.RectTransform.anchoredPosition = Vector2.zero;
                _mapView.RectTransform.localScale = Vector3.one;
                _firstFullscreenShown = true;
            }

            if (_rememberMapPosition && _mapView != null && _mapView.MainMapPos != Vector2.zero)
            {
                try
                {
                    _mapView.SetMapPos(_savedMainMapPos, 0f);
                }
                catch (Exception e)
                {
                    NorvinskMapsPlugin.Log.LogError($"[TryRecalcAfterParentActivates] SetMapPos: {e.Message}");
                }
            }
        }

        #endregion

        #region Show And Hide Top Level

        internal void OnMapScreenShow()
        {
            if (_savedTrueParent == null && transform.parent != null)
            {
                var parentName = transform.parent.name;
                if (parentName != null && !parentName.Contains("MapPeek") && !parentName.Contains("EFTBattleUIScreen"))
                {
                    _savedTrueParent = transform.parent as RectTransform;
                }
            }

            RectTransform target = _savedTrueParent;
            if (target == null) target = NorvinskMapsPlugin.MapScreenTrueParent;
            if (target == null) target = _peekComponent?.MapScreenTrueParent;
            if (target == null) target = ParentTransform;

            if (target != null && target.gameObject != null)
            {
                transform.SetParent(target, false);
            }

            var selfRt = RectTransform;
            if (selfRt != null)
            {
                selfRt.DOKill(false);
                selfRt.pivot = new Vector2(0.5f, 0.5f);
                selfRt.localScale = Vector3.one;
                selfRt.localRotation = Quaternion.identity;

                RectTransform mapBlockRt = null;
                if (transform.parent != null)
                {
                    for (int i = 0; i < transform.parent.childCount; i++)
                    {
                        var child = transform.parent.GetChild(i);
                        if (child == null) continue;
                        if (child.name == "MapBlock")
                        {
                            mapBlockRt = child as RectTransform;
                            break;
                        }
                    }
                }

                if (mapBlockRt != null)
                {
                    selfRt.anchorMin = mapBlockRt.anchorMin;
                    selfRt.anchorMax = mapBlockRt.anchorMax;
                    selfRt.offsetMin = mapBlockRt.offsetMin;
                    selfRt.offsetMax = mapBlockRt.offsetMax;
                    selfRt.anchoredPosition = mapBlockRt.anchoredPosition;
                }
                else
                {
                    selfRt.anchorMin = Vector2.zero;
                    selfRt.anchorMax = Vector2.one;
                    selfRt.offsetMin = Vector2.zero;
                    selfRt.offsetMax = Vector2.zero;
                    selfRt.anchoredPosition = Vector2.zero;
                    NorvinskMapsPlugin.Log.LogWarning(
                        "[OnMapScreenShow] MapBlock not found — stretched to full parent");
                }
            }

            IsShowingMapScreen = true;
            IsMapScreenShownFullscreen = true;

            if (_mapView != null)
            {
                _mapView.IsMiniMapActive = false;
            }

            if (_peekComponent != null)
            {
                _peekComponent.IsPeeking = false;
                _peekComponent.ForceExitMiniMap();
            }

            if (transform.parent != null && transform.parent.name == "Map Panel")
            {
                var parent = transform.parent;
                for (int i = 0; i < parent.childCount; i++)
                {
                    var child = parent.GetChild(i);
                    if (child == null) continue;
                    if (child == transform) continue;

                    var childName = child.name;
                    if (childName == "ModdedMapBlock") continue;
                    if (childName == "Pocket Map Context") continue;

                    child.gameObject.SetActive(false);
                }

                parent.gameObject.SetActive(true);
            }

            transform.SetAsLastSibling();

            Show(false);

            ForceAutoSelectLevel();

            _pendingFullscreenRecalc = true;
        }

        internal void OnMapScreenClose()
        {
            if (_rememberMapPosition && _mapView != null)
            {
                _savedMainMapPos = _mapView.MainMapPos;
            }

            Hide();
            IsShowingMapScreen = false;
            IsMapScreenShownFullscreen = false;
            _pendingFullscreenRecalc = false;
        }

        [HideFromIl2Cpp]
        public void OnMapScreenCloseSuppressed()
        {
            IsShowingMapScreen = false;
            IsMapScreenShownFullscreen = false;
            _pendingFullscreenRecalc = false;

            if (!IsActuallyInRaid())
            {
                if (_rememberMapPosition && _mapView != null)
                    _savedMainMapPos = _mapView.MainMapPos;

                _mapSelectDropdown?.TryCloseDropdown();
                _isShown = false;
                gameObject.SetActive(false);
            }
        }

        [HideFromIl2Cpp]
        public void ResetFullscreenFlag()
        {
            IsMapScreenShownFullscreen = false;
        }

        internal void Show(bool playAnimation)
        {
            try
            {
                AdjustSizeAndPosition();

                _isShown = true;
                gameObject.SetActive(true);

                if (_mapSelectDropdown != null)
                {
                    _mapSelectDropdown.LoadMapDefsFromPath(MapRelPath);
                    _mapSelectDropdown.ClearFilter();
                }

                if (IsActuallyInRaid())
                {
                    OnShowInRaid(playAnimation);
                }
                else
                {
                    OnShowOutOfRaid();
                }
            }
            catch (Exception e)
            {
                NorvinskMapsPlugin.Log.LogError($"[ModdedMapScreen.Show] {e.Message}");
                NorvinskMapsPlugin.Log.LogError(e.StackTrace);
            }
        }

        internal void Hide(bool fullHide = true)
        {
            _mapSelectDropdown?.TryCloseDropdown();

            if (IsActuallyInRaid())
            {
                OnHideInRaid();
            }
            else
            {
                OnHideOutOfRaid();
            }

            _isShown = false;

            if (fullHide)
            {
                gameObject.SetActive(false);
            }
        }

        [HideFromIl2Cpp]
        private void OnRaidEnd()
        {
            _firstFullscreenShown = false;

            if (_peekComponent != null)
            {
                try { _peekComponent.EndPeek(); } catch { }
                try { _peekComponent.EndMiniMap(); } catch { }

                if (_peekComponent.gameObject != null)
                    Destroy(_peekComponent.gameObject);
                _peekComponent = null;
            }

            if (!BattleUIScreenShowPatch.IsAttached) return;

            _savedMainMapPos = Vector2.zero;
            _mapView.IsMiniMapActive = false;

            foreach (var dynamicProvider in _dynamicMarkerProviders.Values)
            {
                try { dynamicProvider.OnRaidEnd(_mapView); }
                catch (Exception e)
                {
                    NorvinskMapsPlugin.Log.LogError($"[OnRaidEnd] {dynamicProvider}: {e.Message}");
                }
            }

            _mapView.UnloadMap();
        }

        #endregion

        #region Size And Positioning

        internal void SaveMainMapPos()
        {
            if (_rememberMapPosition && _mapView != null)
            {
                _savedMainMapPos = _mapView.MainMapPos;
            }
        }

        [HideFromIl2Cpp]
        public void ForceRecalcAfterActivation()
        {
            if (_mapView == null || _scrollMask == null) return;

            var selfRt = RectTransform;
            if (selfRt != null)
            {
                selfRt.DOKill(false);
                selfRt.anchorMin = Vector2.zero;
                selfRt.anchorMax = Vector2.one;
                selfRt.pivot = new Vector2(0.5f, 0.5f);
                selfRt.offsetMin = Vector2.zero;
                selfRt.offsetMax = Vector2.zero;
                selfRt.anchoredPosition = Vector2.zero;
                selfRt.localScale = Vector3.one;
            }

            var parentRt = transform.parent as RectTransform;
            var parentSize = parentRt != null && parentRt.rect.size.x > 0f
                ? parentRt.rect.size
                : new Vector2(1920f, 1080f);

            var maskRt = _scrollMask.GetRectTransform();
            if (maskRt != null)
            {
                maskRt.anchorMin = new Vector2(0.5f, 0.5f);
                maskRt.anchorMax = new Vector2(0.5f, 0.5f);
                maskRt.pivot = new Vector2(0.5f, 0.5f);
                maskRt.sizeDelta = new Vector2(parentSize.x, parentSize.y - 70f);
                maskRt.anchoredPosition = new Vector2(0f, -5f);
            }

            if (_scrollRect != null)
            {
                var rectRt = _scrollRect.GetRectTransform();
                if (rectRt != null)
                    rectRt.sizeDelta = parentSize;
            }

            _mapView.RecalculateZoom();
        }

        private void AdjustSizeAndPosition()
        {
            if (ShowingMiniMap) return;
            try
            {
                var commonUI = Singleton<CommonUI>.Instance;
                if (commonUI == null)
                {
                    NorvinskMapsPlugin.Log.LogWarning("[AdjustSizeAndPosition] CommonUI.Instance is null");
                    return;
                }

                var inventoryScreen = commonUI.InventoryScreen;
                if (inventoryScreen == null)
                {
                    NorvinskMapsPlugin.Log.LogWarning("[AdjustSizeAndPosition] InventoryScreen is null");
                    return;
                }

                var rect = inventoryScreen.GetRectTransform().rect;
                RectTransform.sizeDelta = new Vector2(rect.width, rect.height);
                RectTransform.anchoredPosition = Vector2.zero;

                if (_scrollRect != null)
                {
                    var rt = _scrollRect.GetRectTransform();
                    if (rt != null) rt.sizeDelta = RectTransform.sizeDelta;
                }

                if (_scrollMask != null)
                {
                    var rt = _scrollMask.GetRectTransform();
                    if (rt != null)
                    {
                        rt.anchoredPosition = MaskPositionOutOfRaid;
                        rt.sizeDelta = RectTransform.sizeDelta + MaskSizeModifierOutOfRaid;
                    }
                }

                if (_mapView != null)
                {
                    var savedScale = _mapView.RectTransform?.localScale ?? Vector3.one;
                    var savedZoomMain = _mapView.ZoomMain;
                    var savedZoomMini = _mapView.ZoomMini;

                    _mapView.RecalculateZoom();

                    if (_mapView.IsMiniMapActive)
                    {
                        _mapView.RectTransform.localScale = savedScale;
                        _mapView.ZoomMini = savedZoomMini;
                    }
                }

                ApplyFixedLayoutAnchors();
            }
            catch (Exception e)
            {
                NorvinskMapsPlugin.Log.LogError($"[AdjustSizeAndPosition] {e.Message}");
                NorvinskMapsPlugin.Log.LogError(e.StackTrace);
            }
        }

        [HideFromIl2Cpp]
        private void ApplyFixedLayoutAnchors()
        {
            if (_levelSelectSlider != null)
            {
                try { _levelSelectSlider.ApplyFixedAnchors(); } catch { }
            }

            if (_mapSelectDropdown != null)
            {
                var rt = _mapSelectDropdown.RectTransform;
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 1f);
                    rt.anchorMax = new Vector2(0.5f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.sizeDelta = MapSelectDropdownSize;
                    rt.anchoredPosition = MapSelectDropdownPosition;
                }
            }

            if (_cursorPositionText != null)
            {
                var rt = _cursorPositionText.RectTransform;
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    rt.anchoredPosition = CursorPositionTextOffset;
                }
            }

            if (_playerPositionText != null)
            {
                var rt = _playerPositionText.RectTransform;
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    rt.anchoredPosition = PlayerPositionTextOffset;
                }
            }
        }

        private void AdjustForOutOfRaid()
        {
            if (_scrollMask == null) return;

            RestoreModdedMapBlockFullscreen(0f);

            var maskRt = _scrollMask.GetRectTransform();
            maskRt.DOKill(false);
            maskRt.anchoredPosition = MaskPositionOutOfRaid;
            maskRt.sizeDelta = RectTransform.sizeDelta + MaskSizeModifierOutOfRaid;

            if (_cursorPositionText != null) _cursorPositionText.gameObject.SetActive(true);
            if (_levelSelectSlider != null) _levelSelectSlider.gameObject.SetActive(true);
            if (_playerPositionText != null) _playerPositionText.gameObject.SetActive(false);

            ApplyFixedLayoutAnchors();
        }

        private void AdjustForInRaid(bool playAnimation)
        {
            if (_scrollMask == null) return;

            var speed = playAnimation ? 0.35f : 0f;
            var tweenTime = _transitionAnimations ? speed : 0f;

            RestoreModdedMapBlockFullscreen(tweenTime);

            var maskRt = _scrollMask.GetRectTransform();
            maskRt.DOKill(false);

            var targetSize = RectTransform.sizeDelta + MaskSizeModifierInRaid;

            if (tweenTime <= 0f)
            {
                // ФИКС DOTWEEN: без анимации — прямое присвоение, твин не создаётся
                maskRt.sizeDelta = targetSize;
                maskRt.anchoredPosition = MaskPositionInRaid;
            }
            else
            {
                maskRt.DOSizeDelta(targetSize, tweenTime);
                maskRt.DOAnchorPos(MaskPositionInRaid, tweenTime);
            }

            if (_cursorPositionText != null) _cursorPositionText.gameObject.SetActive(true);
            if (_playerPositionText != null) _playerPositionText.gameObject.SetActive(true);
            if (_levelSelectSlider != null) _levelSelectSlider.gameObject.SetActive(!ShowingMiniMap);

            ApplyFixedLayoutAnchors();
        }

        private void AdjustForPeek(bool playAnimation)
        {
            if (_scrollMask == null) return;

            var speed = playAnimation ? 0.35f : 0f;
            var tweenTime = _transitionAnimations ? speed : 0f;

            RestoreModdedMapBlockFullscreen(tweenTime);

            var parentRect = transform.parent as RectTransform;
            var targetSize = parentRect != null ? parentRect.rect.size : new Vector2(1920f, 1080f);

            var maskRt = _scrollMask.GetRectTransform();
            maskRt.DOKill(false);

            if (tweenTime <= 0f)
            {
                maskRt.anchoredPosition = Vector2.zero;
                maskRt.sizeDelta = targetSize;
            }
            else
            {
                maskRt.DOAnchorPos(Vector2.zero, tweenTime);
                maskRt.DOSizeDelta(targetSize, tweenTime);
            }

            if (_cursorPositionText != null) _cursorPositionText.gameObject.SetActive(false);
            if (_playerPositionText != null) _playerPositionText.gameObject.SetActive(false);
            if (_levelSelectSlider != null) _levelSelectSlider.gameObject.SetActive(false);

            if (_mapView != null && _mapView.RectTransform != null)
            {
                _mapView.RectTransform.anchoredPosition = Vector2.zero;
                _mapView.RectTransform.localScale = Vector3.one;
            }

            if (_mapView != null) _mapView.RecalculateZoom();
        }

        private void AdjustForMiniMap(bool playAnimation)
        {
            if (_scrollMask == null) return;

            // Здесь tweenTime всегда 0 — анимации нет, значит твины вообще не создаём
            var selfRt = RectTransform;
            selfRt.DOKill(false);

            var cornerPosition = ConvertEnumToScreenPos(Settings.MiniMapPosition.Value);
            var offset = new Vector2(Settings.MiniMapScreenOffsetX.Value, Settings.MiniMapScreenOffsetY.Value);
            offset *= ConvertEnumToScenePivot(Settings.MiniMapPosition.Value);
            var size = new Vector2(Settings.MiniMapSizeX.Value, Settings.MiniMapSizeY.Value);

            selfRt.anchorMin = cornerPosition;
            selfRt.anchorMax = cornerPosition;
            selfRt.pivot = cornerPosition;
            selfRt.offsetMin = Vector2.zero;
            selfRt.offsetMax = Vector2.zero;
            selfRt.sizeDelta = size;
            selfRt.anchoredPosition = offset;

            var maskRt = _scrollMask.GetRectTransform();
            maskRt.DOKill(false);
            maskRt.anchorMin = new Vector2(0.5f, 0.5f);
            maskRt.anchorMax = new Vector2(0.5f, 0.5f);
            maskRt.pivot = new Vector2(0.5f, 0.5f);
            maskRt.anchoredPosition = Vector2.zero;
            maskRt.sizeDelta = size;

            if (_scrollRect != null)
            {
                var rectRt = _scrollRect.GetRectTransform();
                rectRt.DOKill(false);
                rectRt.anchorMin = Vector2.zero;
                rectRt.anchorMax = Vector2.one;
                rectRt.offsetMin = Vector2.zero;
                rectRt.offsetMax = Vector2.zero;
                rectRt.anchoredPosition = Vector2.zero;
                rectRt.sizeDelta = size;
            }

            if (_cursorPositionText != null) _cursorPositionText.gameObject.SetActive(false);
            if (_playerPositionText != null) _playerPositionText.gameObject.SetActive(false);
            if (_levelSelectSlider != null) _levelSelectSlider.gameObject.SetActive(false);

            if (_mapView != null && _mapView.RectTransform != null)
            {
                _mapView.RectTransform.anchoredPosition = Vector2.zero;
                _mapView.RectTransform.localScale = Vector3.one;
            }

            if (_mapView != null) _mapView.RecalculateZoom();
        }

        private void RestoreModdedMapBlockFullscreen(float tweenTime)
        {
            var selfRt = RectTransform;

            selfRt.DOKill(false);

            selfRt.anchorMin = Vector2.zero;
            selfRt.anchorMax = Vector2.one;
            selfRt.pivot = new Vector2(0.5f, 0.5f);
            selfRt.offsetMin = Vector2.zero;
            selfRt.offsetMax = Vector2.zero;
            selfRt.anchoredPosition = Vector2.zero;
        }

        private Vector2 ConvertEnumToScreenPos(EMiniMapPosition pos)
        {
            switch (pos)
            {
                case EMiniMapPosition.TopRight: return new Vector2(1, 1);
                case EMiniMapPosition.BottomRight: return new Vector2(1, 0);
                case EMiniMapPosition.TopLeft: return new Vector2(0, 1);
                case EMiniMapPosition.BottomLeft: return new Vector2(0, 0);
            }
            return Vector2.zero;
        }

        private Vector2 ConvertEnumToScenePivot(EMiniMapPosition pos)
        {
            switch (pos)
            {
                case EMiniMapPosition.TopRight: return new Vector2(-1, -1);
                case EMiniMapPosition.BottomRight: return new Vector2(-1, 1);
                case EMiniMapPosition.TopLeft: return new Vector2(1, -1);
                case EMiniMapPosition.BottomLeft: return new Vector2(1, 1);
            }
            return Vector2.zero;
        }

        #endregion

        #region Show And Hide Bottom Level

        private void OnShowInRaid(bool playAnimation)
        {
            if (ShowingMiniMap) AdjustForMiniMap(playAnimation);
            else if (IsPeeking) AdjustForPeek(playAnimation);
            else AdjustForInRaid(playAnimation);

            if (_mapSelectDropdown != null)
            {
                var mapInternalName = GameUtils.GetCurrentMapInternalName();
                _mapSelectDropdown.FilterByInternalMapName(mapInternalName);
                _mapSelectDropdown.LoadFirstAvailableMap();
            }

            if (ShowingMiniMap && _levelSelectSlider != null)
                _levelSelectSlider.gameObject.SetActive(false);

            foreach (var dynamicProvider in _dynamicMarkerProviders.Values)
            {
                try { dynamicProvider.OnShowInRaid(_mapView); }
                catch (Exception e)
                {
                    NorvinskMapsPlugin.Log.LogError($"[OnShowInRaid] {dynamicProvider}: {e.Message}");
                }
            }

            var player = GameUtils.GetMainPlayer();
            if (player is null) return;

            var mapPosition = MathUtils.ConvertToMapPosition(((IPlayer)player).Position);

            if (_autoSelectLevel) _mapView.SelectLevelByCoords(mapPosition);

            if (_rememberMapPosition && !ShowingMiniMap && _mapView.MainMapPos != Vector2.zero)
            {
                _mapView.ApplyMainMapZoom();
                var tweenTime = _transitionAnimations && _mapView.MainMapPos != _savedMainMapPos ? 0.35f : 0f;
                _mapView.SetMapPos(_savedMainMapPos, tweenTime);
                return;
            }

            if (!_rememberMapPosition && !_autoCenterOnPlayerMarker && !ShowingMiniMap)
            {
                _mapView.ApplyMainMapZoom();
                _mapView.SetMapZoom(_mapView.ZoomMin, 0f);
                return;
            }

            if (_autoCenterOnPlayerMarker && !ShowingMiniMap && _mapView.MainMapPos == Vector2.zero)
            {
                _mapView.ApplyMainMapZoom();
                if (_resetZoomOnCenter && !ShowingMiniMap)
                {
                    _mapView.SetMapZoom(GetInRaidStartingZoom(), 0);
                }
                _mapView.ShiftMapToPlayer(mapPosition, 0, false);
            }
            else if (!ShowingMiniMap)
            {
                _mapView.ApplyMainMapZoom();
                if (_savedMainMapPos != Vector2.zero)
                {
                    _mapView.SetMapPos(_savedMainMapPos, 0f);
                }
            }
        }

        private void OnHideInRaid()
        {
            foreach (var dynamicProvider in _dynamicMarkerProviders.Values)
            {
                try { dynamicProvider.OnHideInRaid(_mapView); }
                catch (Exception e)
                {
                    NorvinskMapsPlugin.Log.LogError($"[OnHideInRaid] {dynamicProvider}: {e.Message}");
                }
            }
        }

        private void OnShowOutOfRaid()
        {
            AdjustForOutOfRaid();

            if (_mapSelectDropdown != null) _mapSelectDropdown.ClearFilter();

            if (_mapView.CurrentMapDef == null && _mapSelectDropdown != null)
            {
                _mapSelectDropdown.LoadFirstAvailableMap();
            }

            foreach (var dynamicProvider in _dynamicMarkerProviders.Values)
            {
                try { dynamicProvider.OnShowOutOfRaid(_mapView); }
                catch (Exception e)
                {
                    NorvinskMapsPlugin.Log.LogError($"[OnShowOutOfRaid] {dynamicProvider}: {e.Message}");
                }
            }
        }

        private void OnHideOutOfRaid()
        {
            foreach (var dynamicProvider in _dynamicMarkerProviders.Values)
            {
                try { dynamicProvider.OnHideOutOfRaid(_mapView); }
                catch (Exception e)
                {
                    NorvinskMapsPlugin.Log.LogError($"[OnHideOutOfRaid] {dynamicProvider}: {e.Message}");
                }
            }
        }

        #endregion

        #region Map Manipulation

        private void OnScroll(float scrollAmount)
        {
            if (IsPeeking || ShowingMiniMap) return;

            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (_levelSelectSlider != null)
                {
                    if (scrollAmount > 0) _levelSelectSlider.ChangeLevelBy(1);
                    else _levelSelectSlider.ChangeLevelBy(-1);
                }
                return;
            }

            if (_targetZoom == 0f) _targetZoom = _mapView.ZoomMain;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _mapView.RectTransform, Input.mousePosition, null, out Vector2 mouseRelative);

            _targetZoom = Mathf.Clamp(
                _targetZoom + scrollAmount * _targetZoom * ScrollZoomScaler,
                _mapView.ZoomMin,
                _mapView.ZoomMax);

            _zoomFocusPoint = mouseRelative;
        }

        private void OnScrollZoomUpdate()
        {
            if (_targetZoom == 0f || IsPeeking || ShowingMiniMap) return;

            if (_mapView != null && _mapView.IsDragging) return;

            var currentZoom = _mapView.ZoomMain;
            if (Mathf.Abs(currentZoom - _targetZoom) < 0.0001f)
            {
                _targetZoom = 0f;
                return;
            }

            var newZoom = Mathf.Lerp(currentZoom, _targetZoom, ScrollZoomLerpSpeed * Time.deltaTime);
            var zoomDelta = newZoom - currentZoom;

            _mapView.IncrementalZoomInto(zoomDelta, _zoomFocusPoint, 0f);
        }

        private void OnZoomMain()
        {
            var zoomAmount = 0f;

            if (_zoomMainMapOutShortcut.BetterIsPressed()) zoomAmount -= 1f;
            if (_zoomMainMapInShortcut.BetterIsPressed()) zoomAmount += 1f;

            if (zoomAmount != 0f)
            {
                _targetZoom = 0f;
                zoomAmount = _mapView.ZoomMain * zoomAmount * (_zoomMapHotkeySpeed * Time.deltaTime);

                if (IsPeeking)
                {
                    var player = GameUtils.GetMainPlayer();
                    var mapPosition = MathUtils.ConvertToMapPosition(((IPlayer)player).Position);
                    _mapView.IncrementalZoomInto(zoomAmount, mapPosition, 0f);
                }
                else
                {
                    if (_zoomMainMapToMouse)
                    {
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            _mapView.RectTransform, Input.mousePosition, null, out Vector2 mouseRelative);
                        _mapView.IncrementalZoomInto(zoomAmount, mouseRelative, 0f);
                    }
                    else
                    {
                        var currentCenter = _mapView.RectTransform.anchoredPosition / _mapView.ZoomMain;
                        _mapView.IncrementalZoomInto(zoomAmount, currentCenter, 0f);
                    }
                }

                return;
            }

            _mapView.SetMapZoom(_mapView.ZoomMain, 0f);
        }

        private void OnZoomMini()
        {
            var zoomAmount = 0f;

            if (_zoomMiniMapOutShortcut.BetterIsPressed()) zoomAmount -= 1f;
            if (_zoomMiniMapInShortcut.BetterIsPressed()) zoomAmount += 1f;

            if (zoomAmount != 0f)
            {
                _targetZoom = 0f;
                var player = GameUtils.GetMainPlayer();
                var mapPosition = MathUtils.ConvertToMapPosition(((IPlayer)player).Position);
                zoomAmount = _mapView.ZoomMini * zoomAmount * (_zoomMapHotkeySpeed * Time.deltaTime);

                _mapView.IncrementalZoomIntoMiniMap(zoomAmount, mapPosition, 0.0f);
                return;
            }

            _mapView.SetMapZoom(_mapView.ZoomMini, 0f, false, true);
        }

        private void OnCenter()
        {
            if (_mapView != null && _mapView.IsDragging && !ShowingMiniMap) return;

            if (_centerPlayerShortcut.BetterIsDown())
            {
                CenterOnPlayer(false);
                return;
            }

            if (ShowingMiniMap)
            {
                _miniMapUpdateTimer -= Time.deltaTime;
                if (_miniMapUpdateTimer > 0f) return;
                _miniMapUpdateTimer = _miniMapUpdateInterval;
                CenterOnPlayer(true);
            }
        }

        private void CenterOnPlayer(bool isMini)
        {
            var player = GameUtils.GetMainPlayer();
            if (player is null) return;

            var mapPosition = MathUtils.ConvertToMapPosition(((IPlayer)player).Position);
            _mapView.ShiftMapToCoordinate(mapPosition, isMini ? 0f : PositionTweenTime, isMini);
            _mapView.SelectLevelByCoords(mapPosition);
        }

        #endregion

        #region Config and Marker Providers

        [HideFromIl2Cpp]
        internal void TryAddPeekComponent(EftBattleUIScreen battleUI)
        {
            if (_peekComponent != null)
            {
                var peekGo = _peekComponent.gameObject;
                var peekParent = _peekComponent.transform != null ? _peekComponent.transform.parent : null;

                bool isAlive = peekGo != null
                            && peekParent != null
                            && peekGo.activeInHierarchy;

                if (isAlive)
                {
                    return;
                }

                NorvinskMapsPlugin.Log.LogWarning("[TryAddPeekComponent] _peekComponent is a zombie — destroying");

                if (_peekComponent.MapScreenTrueParent != null)
                {
                    _savedTrueParent = _peekComponent.MapScreenTrueParent;
                }

                try { Destroy(_peekComponent.gameObject); } catch { }
                _peekComponent = null;
            }

            _peekComponent = MapPeekComponent.Create(battleUI.gameObject);
            if (_peekComponent != null)
            {
                _peekComponent.MapScreen = this;

                _peekComponent.MapScreenTrueParent = _savedTrueParent
                                   ?? NorvinskMapsPlugin.MapScreenTrueParent
                                   ?? battleUI.GetRectTransform();
            }

            ReadConfig();
        }

        [HideFromIl2Cpp]
        public void AddRemoveMarkerProvider<T>(bool status) where T : IDynamicMarkerProvider, new()
        {
            if (status && !_dynamicMarkerProviders.ContainsKey(typeof(T)))
            {
                _dynamicMarkerProviders[typeof(T)] = new T();

                if (_isShown && GameUtils.IsInRaid())
                    _dynamicMarkerProviders[typeof(T)].OnShowInRaid(_mapView);
                else if (_isShown && !GameUtils.IsInRaid())
                    _dynamicMarkerProviders[typeof(T)].OnShowOutOfRaid(_mapView);
            }
            else if (!status && _dynamicMarkerProviders.ContainsKey(typeof(T)))
            {
                _dynamicMarkerProviders[typeof(T)].OnDisable(_mapView);
                _dynamicMarkerProviders.Remove(typeof(T));
            }
        }

        [HideFromIl2Cpp]
        private T GetMarkerProvider<T>() where T : IDynamicMarkerProvider
        {
            if (!_dynamicMarkerProviders.ContainsKey(typeof(T)))
                return default;

            return (T)_dynamicMarkerProviders[typeof(T)];
        }

        #endregion

        #region Utils And Caching

        private float GetInRaidStartingZoom()
        {
            var startingZoom = _mapView.ZoomMin;
            startingZoom += _centeringZoomResetPoint * (_mapView.ZoomMax - _mapView.ZoomMin);
            return startingZoom;
        }

        [HideFromIl2Cpp]
        private void ChangeMap(MapDef mapDef)
        {
            if (mapDef == null || _mapView.CurrentMapDef == mapDef) return;

            if (GameUtils.IsInRaid())
                AdjustSizeAndPosition();

            _mapView.CancelPositionTween();
            _targetZoom = 0f;
            _savedMainMapPos = Vector2.zero;

            _mapView.LoadMap(mapDef, _scrollMask.GetRectTransform());

            if (_mapSelectDropdown != null) _mapSelectDropdown.OnLoadMap(mapDef);
            if (_levelSelectSlider != null) _levelSelectSlider.OnLoadMap(mapDef, _mapView.SelectedLevel);

            foreach (var dynamicProvider in _dynamicMarkerProviders.Values)
            {
                try { dynamicProvider.OnMapChanged(_mapView, mapDef); }
                catch (Exception e)
                {
                    NorvinskMapsPlugin.Log.LogError($"[ChangeMap] {dynamicProvider}: {e.Message}");
                }
                ApplyFixedLayoutAnchors();

            }
        }

        #endregion
    }
}