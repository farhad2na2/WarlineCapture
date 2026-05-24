using System;
using System.Collections.Generic;
using System.Collections;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using TMPro;
using Synty.Interface.MilitaryCombatHUD.Samples;
using System.Reflection;
using SnivelerCode.GpuAnimation.Scripts.Authoring;
using SnivelerCode.GpuAnimation.Scripts.Components;
using Object = UnityEngine.Object;

namespace Game.Scripts.UI
{
    public class MenuView : MonoBehaviour
    {
        public event Action GameRequested;

        private enum MenuType
        {
            Menu, Camp, Stats, Game  
        }
        
        private enum GameMenuType
        {
            Free, Select, Map  
        }

        private enum CampMenuType
        {
            Ammo,
            Soldiers,
            Vehicles,
            Buildings
        }
        
        private enum SelectionType
        {
            Single, Multi
        }
        
        private enum SingleSelectionType
        {
            Soldier, Vehicle, Building
        }

        private enum PreviewModelCategory
        {
            None,
            Character,
            Vehicle,
            Building
        }

        private enum PreviewScaleMode
        {
            Height,
            MaxDimension
        }

        private enum ConfirmMode
        {
            None,
            DestroyBuilding,
            DestroyUnit,
            PlaceBuilding
        }

        private readonly struct RuntimeLogEntry
        {
            public readonly string Message;
            public readonly LogType Type;

            public RuntimeLogEntry(string message, LogType type)
            {
                Message = message;
                Type = type;
            }
        }

        private MenuType menuType;
        private GameMenuType gameMenuType;
        private SelectionType selectionType;
        private SingleSelectionType singleSelectionType;
        
        private static readonly int FadeOut = Animator.StringToHash("FadeOut");
        private static readonly int FadeIn = Animator.StringToHash("FadeIn");

        private Animator panelCurrent;
        private bool _campOpenedFromGame;
        private bool _statsOpenedFromGame;
        private CampMenuType _campMenuType = CampMenuType.Buildings;
        
        public Animator panelMenu;
        public Animator panelCamp;
        public Animator panelGame;
        public Animator panelStats;
        
        public Animator gamePanelMap;
        public Animator gamePanelSelect;
        public Animator gamePanelFree;
        public Animator panelConfirm;
        public Animator panelWarning;

        public GameObject selectPanelSingle;
        public GameObject selectPanelMulti;
        public TMP_Text confirmLabel;
        public TMP_Text warningLabel;
        public GameObject tacticalWarningPanel;
        public TMP_Text tacticalWarningTypeLabel;
        public TMP_Text tacticalWarningDescriptionLabel;
        
        public GameObject panelLoading;
        public GameObject panelZoom;
        public GameObject panelTime;
        public GameObject panelCampButtons;
        public GameObject panelLog;
        public GameObject moveOrderScreenReticle;
        public GameObject attackOrderScreenReticle;
        public TMP_Text moneyAmountText;
        public TMP_Text dateText;
        public TMP_Text timeText;
        public TMP_Text fpsText;
        public TMP_Text logText;
        public ScrollRect logScrollRect;
        
        public Button buttonGame;
        public Button buttonStats;
        public Button buttonBack;

        public Button buttonCampAmmo;
        public Button buttonCampSoldiers;
        public Button buttonCampVehicles;
        public Button buttonCampBuildings;
        
        public Button buttonMap;
        
        public Button buttonZoomIn;
        public Button buttonZoomOut;
        public Button buttonCamera;
        public Button buttonDestroy;
        public Button buttonConfirm;
        public Button buttonSettings;
        public GameObject panelSettings;
        public TMP_Dropdown gameplaySpeedDropdown;
        public TMP_Dropdown aiDifficultyDropdown;
        public TMP_Dropdown aiStartingMoneyDropdown;
        public TMP_Dropdown aiIncomeMultiplierDropdown;
        public TMP_Dropdown aiBuildSpeedDropdown;
        public TMP_Dropdown aiUnitProductionSpeedDropdown;
        public TMP_Dropdown aiAttackGroupSizeDropdown;
        public TMP_Dropdown aiAttackFrequencyDropdown;
        public TMP_Dropdown aiAggressionDropdown;
        public TMP_Dropdown aiExpansionDropdown;
        public TMP_Dropdown aiTargetPriorityDropdown;
        public TMP_Dropdown aiPlayerAutoDropdown;
        public TMP_Dropdown aiEnemyCountDropdown;
        private const string AutoModeButtonName = "Button_AutoMode";
        private const string AutoModeLabelName = "Label_AutoMode";
        private static readonly string[] GameplaySpeedLabels =
        {
            "1x", "1.25x", "1.5x", "2x", "3x", "4x", "5x", "6x", "7x", "8x", "9x", "10x"
        };
        private static readonly string[] AIDifficultyLabels = { "Easy", "Normal", "Hard", "Brutal" };
        private static readonly string[] AIStartingMoneyLabels = { "Low", "Normal", "High" };
        private static readonly string[] AIIncomeMultiplierLabels = { "0.75x", "1x", "1.25x", "1.5x", "2x" };
        private static readonly float[] AIIncomeMultiplierValues = { 0.75f, 1f, 1.25f, 1.5f, 2f };
        private static readonly string[] AISpeedLabels = { "Slow", "Normal", "Fast" };
        private static readonly string[] AIAttackGroupSizeLabels = { "Small", "Normal", "Large" };
        private static readonly string[] AIAttackFrequencyLabels = { "Rare", "Normal", "Frequent" };
        private static readonly string[] AIAggressionLabels = { "Defensive", "Balanced", "Aggressive" };
        private static readonly string[] AIExpansionLabels = { "Off", "Slow", "Normal", "Fast" };
        private static readonly string[] AITargetPriorityLabels = { "Balanced", "Units", "Economy", "Production" };
        private static readonly string[] AIPlayerAutoLabels = { "Off", "On" };
        private static readonly string[] AIEnemyCountLabels = { "1", "2", "3" };
        private const int MaxVisibleLogEntries = 50;
        private readonly Queue<RuntimeLogEntry> _runtimeLogEntries = new(MaxVisibleLogEntries);
        private readonly System.Text.StringBuilder _runtimeLogBuilder = new(8192);
        private EventTrigger _fpsLogToggleTrigger;
        private EventTrigger.Entry _fpsLogToggleEntry;
        private RectTransform _logContentRect;
        private RectTransform _logTextRect;
        private bool _runtimeLogSubscribed;
        private bool _runtimeLogBufferReplayed;

        [Header("Camp Selected References")]
        public Button campPreviewPreviousButton;
        public Button campPreviewNextButton;
        public ScrollRect campScrollRect;
        public RectTransform campScrollContent;
        public GameObject campItemTemplate;
        public SoldierBadgeCatalogConfig soldierBadgeCatalogConfig;
        public Image campSelectedPortraitImage;
        public GameObject campSelectedWeaponRoot;
        public Image campSelectedWeaponImage;
        public TMP_Text campSelectedWeaponNameText;
        public TMP_Text campSelectedNameText;
        public TMP_Text campDescriptionText;
        public RectTransform campSelectedModelPreviewRoot;
        public PrefabPreviewCameraConfig campSelectedModelPreviewCameraConfig;
        public GameObject campSelectedModelWeaponRoot;
        public Image campSelectedModelWeaponImage;
        public RectTransform campSelectedBadgeContent;
        public TMP_Text campSelectedRankNameText;
        public TMP_Text campSelectedTierNumberText;
        public TMP_Text campSelectedPlayerRankText;
        public GameObject campSelectedInfoBottomRoot;
        public GameObject campRankScrollRoot;
        public ScrollRect campRankScrollRect;
        public RectTransform campRankScrollContent;
        public GameObject campRankBadgeTemplate;
        public Vector3 campRankBadgeContentScale = new(0.5f, 0.5f, 1f);

        private RTSSelectionSystem _selectionSystem;
        private BuildingUiCommandSystem _buildingUiCommandSystem;
        private BuildingUiCommandSystem.Context _buildingUiCommandContext;
        private BuildingUiQuerySystem _buildingUiQuerySystem;
        private BuildingUiQuerySystem.Context _buildingUiQueryContext;
        private Camera _worldCamera;
        private DayNightSystem _dayNightSystem;
        private CitizenPopulationSystem _citizenPopulationSystem;
        private readonly RuntimeGameplayStateSystem _runtimeGameplayStateSystem = new();
        private readonly List<Entity> _selectedUnits = new();
        private bool _initialized;
        private bool _gameStartPending;
        private int _nextMenuCanvasDiagFrame;
        private bool _confirmOpen;
        private bool _warningOpen;
        private bool _tacticalWarningOpen;
        private bool _wasPrimaryPointerPressed;
        private int _warningOpenedFrame = -1;
        private float _warningAutoCloseAt;
        private float _tacticalWarningAutoCloseAt;
        private ConfirmMode _confirmMode;
        private string _warningTargetName;
        private Transform _campListRoot;
        private Transform _campLayoutRoot;
        private GameObject _campSelectedPanel;
        private Image _campSelectedPortrait;
        private GameObject _campSelectedWeaponRoot;
        private Image _campSelectedWeapon;
        private TMP_Text _campSelectedWeaponName;
        private GameObject _campSelectedModelWeaponRoot;
        private Image _campSelectedModelWeaponImage;
        private TMP_Text _campSelectedName;
        private GameObject _campDescriptionPanel;
        private TMP_Text _campDescriptionText;
        private GameObject _campSelectedInfoBottomRoot;
        private GameObject _campRankScrollRoot;
        private TMP_Text _campPriceLabel;
        private GameObject _campRequestButton;
        private Button _campRequestButtonComponent;
        private GameObject _campSelectedBadgeInstance;
        private int _campSelectedBadgeIndex;
        private RectTransform _moveOrderScreenReticle;
        private RectTransform _attackOrderScreenReticle;
        private float _moveOrderScreenReticleHideTime = -1f;
        private float _attackOrderScreenReticleHideTime = -1f;
        private readonly List<GameObject> _campRankBadgeItems = new();
        private Vector3 _campRankBadgeTemplateContentScale = Vector3.one;
        private RawImage _campSelectedModelPreviewImage;
        private Camera _campSelectedModelPreviewCamera;
        private RenderTexture _campSelectedModelPreviewTexture;
        private GameObject _campSelectedModelPreviewInstance;
        private Transform _campSelectedModelPreviewContent;
        private GameObject _campSelectedModelPreviousPreviewInstance;
        private Transform _campSelectedModelPreviousPreviewContent;
        private Bounds _campSelectedModelPreviousLocalBounds;
        private float _campSelectedModelCarouselStartedAt;
        private MaterialAnimatorAuthoring _campSelectedModelGpuAnimator;
        private Renderer[] _campSelectedModelGpuRenderers;
        private MaterialPropertyBlock _campSelectedModelPropertyBlock;
        private int _campSelectedModelIdleAnimationIndex;
        private float _campSelectedModelAnimationStartedAt;
        private Bounds _campSelectedModelLocalBounds;
        private GameObject _campSelectedModelSourcePrefab;
        private PreviewModelCategory _campSelectedModelCategory = PreviewModelCategory.None;
        private readonly List<Renderer> _campSelectedModelVisibleRenderers = new();
        private Image _singleSelectionPortrait;
        private TMP_Text _singleSelectionName;
        private Slider _singleSelectionHealthSlider;
        private GameObject _singleSelectionUnitPanel;
        private GameObject _singleSelectionSoldierPanel;
        private GameObject _singleSelectionVehiclePanel;
        private GameObject _singleSelectionExitButton;
        private Button _singleSelectionExitButtonComponent;
        private GameObject _singleSelectionAttackButton;
        private Button _singleSelectionAttackButtonComponent;
        private GameObject _singleSelectionSoldierWeaponPanel;
        private Image _singleSelectionSoldierWeaponImage;
        private GameObject _singleSelectionVehicleOnboardPanel;
        private Transform _singleSelectionVehicleOnboardLayout;
        private GameObject _singleSelectionVehicleOnboardTemplate;
        private readonly List<OnboardPassengerItemView> _singleSelectionVehicleOnboardItems = new();
        private readonly List<RTSSelectionSystem.TransportPassengerUiInfo> _focusedTransportPassengers = new();
        private GameObject _singleStatusIdleIcon;
        private GameObject _singleStatusMovingIcon;
        private GameObject _singleStatusEngagedIcon;
        private GameObject _singleStatusReturningIcon;
        private RectTransform _requestPanelRoot;
        private GameObject _requestCountdownTemplate;
        private readonly List<BuildingUiQuerySystem.PendingProductionUiEntry> _pendingProductionEntries = new();
        private readonly Dictionary<string, RequestCountdownView> _requestCountdownViews = new();
        private Transform _statsLayoutRoot;
        private readonly Dictionary<string, TMP_Text> _statsAmountTexts = new();
        private BuildingUiCommandSystem.CampRequestFailure _campRequestFailure;
        private string _campRequestFailureBuildingName;
        private readonly List<GameObject> _campRequestGreens = new();
        private readonly List<GameObject> _campRequestReds = new();
        private GameObject _campAmmoSelected;
        private GameObject _campSoldiersSelected;
        private GameObject _campVehiclesSelected;
        private GameObject _campBuildingsSelected;
        private GameObject _cameraButtonSelected;
        private Button _buttonAutoMode;
        private TMP_Text _autoModeLabel;
        private bool _settingsOpen;
        private Button _buttonSelect;
        private Button _buttonSelectAll;
        private Button _buttonSelectAllSoldiers;
        private Button _buttonSelectAllSoldiersAlias;
        private Button _buttonSelectAllVehicles;
        private Button _buttonSelectAllVehiclesAlias;
        private Button _buttonDeselectAll;
        private readonly List<CampListItemView> _campItemViews = new();
        private readonly List<CampCatalogEntry> _campEntries = new();
        private readonly Dictionary<GameObject, Sprite> _campPreviewSprites = new();
        private int _campPreviewRevision = -1;
        private int _campSelectedIndex = -1;
        private bool _campHasOpenedOnce;
        private bool _campOpenHasExplicitTarget;
        private bool _campHasDeferredProductionFocus;
        private RectTransform _minimapRootRect;
        private RectTransform _minimapMapRect;
        private RectTransform _minimapElementsRect;
        private RectTransform _minimapRuntimeElementsRect;
        private Image _minimapMapImage;
        private Texture2D _minimapTexture;
        private Sprite _minimapSprite;
        private Color32[] _minimapPixels;
        private Color32[] _minimapStaticPixels;
        private float _nextMinimapDynamicRefreshTime;
        private bool _minimapStaticBuilt;
        private MinimapViewBounds _lastMinimapViewBounds;
        private RectTransform _minimapAllyTemplate;
        private RectTransform _minimapEnemyTemplate;
        private RectTransform _minimapNeutralTemplate;
        private RectTransform _minimapObjectiveTemplate;
        private RectTransform _minimapHomeTemplate;
        private RectTransform _minimapHomeNeutralTemplate;
        private RectTransform _minimapHomeEnemyTemplate;
        private RectTransform _minimapWallTemplate;
        private RectTransform _minimapWallEnemyTemplate;
        private RectTransform _minimapSelectedTemplate;
        private readonly HashSet<int> _minimapUnitIconPixelKeys = new();
        private readonly HashSet<int> _minimapObjectivePixelKeys = new();
        private Sprite _minimapAllySprite;
        private Sprite _minimapEnemySprite;
        private Sprite _minimapNeutralSprite;
        private Sprite _minimapObjectiveSprite;
        private Sprite _minimapHomeSprite;
        private Sprite _minimapHomeNeutralSprite;
        private Sprite _minimapHomeEnemySprite;
        private Sprite _minimapWallSprite;
        private Sprite _minimapWallEnemySprite;
        private Sprite _minimapSelectedSprite;
        private Sprite _minimapSelectedArrowSprite;
        private Color _minimapForcedIconColor = new(0.043137256f, 0.6156863f, 0.85882354f, 1f);
        private Color _minimapNeutralBuildingColor = Color.white;
        private Color _minimapEnemyBuildingColor = Color.white;
        private RectTransform _fullscreenMapRootRect;
        private RectTransform _fullscreenMapMapRect;
        private RectTransform _fullscreenMapElementsRect;
        private RectTransform _fullscreenMapRuntimeElementsRect;
        private Image _fullscreenMapImage;
        private Texture2D _fullscreenMapTexture;
        private Sprite _fullscreenMapSprite;
        private Color32[] _fullscreenMapPixels;
        private Color32[] _fullscreenMapStaticPixels;
        private RectTransform _fullscreenMapCameraRect;
        private bool _fullscreenMapCameraRectDragging;
        private Vector2 _fullscreenMapCameraRectDragOffset;
        private float _nextFullscreenMapDynamicRefreshTime;
        private bool _fullscreenMapStaticBuilt;
        private int _activeZoomDirection;
        private const int MinimapResolution = 192;
        private const int FullscreenMapResolution = 512;
        private const int MinimapMaxSelectedArrowIcons = 32;
        private const float MinimapDynamicRefreshInterval = 0.2f;
        private const float FullscreenMapDynamicRefreshInterval = 0.2f;
        private const float MinimapStaticRebuildThresholdWorld = 1.5f;
        private static readonly bool EnableMenuCanvasDiagnostics = false;
        private const double MenuCanvasDiagThresholdSeconds = 0.05d;
        private const int MenuCanvasDiagIntervalFrames = 120;
        private const float CampSelectedModelPreviewNearClip = 0.05f;
        private const float CampSelectedModelCarouselTransitionSeconds = 0.65f;
        private const int CampSelectedModelPreviewLayer = 31;
        private static readonly Vector3 CampSelectedModelPreviewOrigin = new(20000f, 20000f, 20000f);
        private static readonly Color32 MinimapBackgroundColor = new(20, 28, 33, 255);
        private static readonly Color32 MinimapGridColor = new(28, 36, 42, 255);
        private static readonly Color32 MinimapRoadColor = new(112, 96, 62, 255);
        private static readonly Color32 MinimapAllyUnitColor = new(11, 157, 219, 255);
        private static readonly Color32 MinimapEnemyUnitColor = new(220, 64, 64, 255);
        private static readonly Color32 MinimapNeutralUnitColor = new(235, 235, 235, 255);
        private static readonly Color32 MinimapSelectedUnitColor = new(255, 255, 255, 255);
        private static readonly FieldInfo CountdownCurrentTimeField = typeof(SampleCountdownLabel).GetField("currentTime", BindingFlags.Instance | BindingFlags.NonPublic);

        private sealed class CampListItemView
        {
            public GameObject Root;
            public Button Button;
            public Image PortraitImage;
            public GameObject SelectedRoot;
            public TMP_Text SelectedName;
            public Graphic ClickTarget;
        }

        private sealed class RequestCountdownView
        {
            public GameObject Root;
            public Image Portrait;
            public SampleCountdownLabel Countdown;
            public TMP_Text TimeLabel;
            public GameObject DialHealthy;
            public GameObject DialLow;
            public float ZeroReachedAt;
            public readonly List<Image> HealthyFillImages = new();
            public readonly List<Image> LowFillImages = new();
        }

        private sealed class OnboardPassengerItemView
        {
            public GameObject Root;
            public Image Portrait;
            public Slider HealthSlider;
            public TMP_Text NameLabel;
        }

        private readonly struct CampCatalogEntry
        {
            public readonly string DisplayName;
            public readonly string Description;
            public readonly GameObject Prefab;
            public readonly int Price;

            public CampCatalogEntry(string displayName, string description, GameObject prefab, int price)
            {
                DisplayName = displayName;
                Description = description;
                Prefab = prefab;
                Price = price;
            }
        }

        private readonly struct PreviewPanelFraming
        {
            public readonly Vector3 ModelPosition;
            public readonly Quaternion ModelRotation;
            public readonly Vector3 CameraPosition;
            public readonly Quaternion CameraRotation;
            public readonly float CarouselRadius;
            public readonly float TargetSize;
            public readonly PreviewScaleMode ScaleMode;

            public PreviewPanelFraming(Vector3 modelPosition, Quaternion modelRotation, Vector3 cameraPosition, Quaternion cameraRotation, float carouselRadius, float targetSize, PreviewScaleMode scaleMode)
            {
                ModelPosition = modelPosition;
                ModelRotation = modelRotation;
                CameraPosition = cameraPosition;
                CameraRotation = cameraRotation;
                CarouselRadius = Mathf.Max(0f, carouselRadius);
                TargetSize = Mathf.Max(0.01f, targetSize);
                ScaleMode = scaleMode;
            }
        }

        private readonly struct MinimapViewBounds
        {
            public readonly float MinX;
            public readonly float MaxX;
            public readonly float MinZ;
            public readonly float MaxZ;

            public MinimapViewBounds(float minX, float maxX, float minZ, float maxZ)
            {
                MinX = minX;
                MaxX = maxX;
                MinZ = minZ;
                MaxZ = maxZ;
            }

            public float Width => Mathf.Max(0.001f, MaxX - MinX);
            public float Height => Mathf.Max(0.001f, MaxZ - MinZ);
        }

        public void Init(
            RTSSelectionSystem selectionSystem,
            Camera worldCamera,
            DayNightSystem dayNightSystem = null,
            CitizenPopulationSystem citizenPopulationSystem = null,
            BuildingUiCommandSystem buildingUiCommandSystem = null,
            BuildingUiCommandSystem.Context buildingUiCommandContext = default)
        {
            if (_initialized)
                return;

            _initialized = true;
            _selectionSystem = selectionSystem;
            _buildingUiCommandSystem = buildingUiCommandSystem;
            _buildingUiCommandContext = buildingUiCommandContext;
            _worldCamera = worldCamera;
            _dayNightSystem = dayNightSystem;
            _citizenPopulationSystem = citizenPopulationSystem;
            if (_selectionSystem != null)
            {
                _selectionSystem.MoveOrderScreenMarkerRequested += ShowMoveOrderScreenReticle;
                _selectionSystem.AttackOrderScreenMarkerRequested += ShowAttackOrderScreenReticle;
                _selectionSystem.OrderScreenMarkersHideRequested += HideOrderScreenReticles;
            }

            ResolveGamePanels();
            ResolveAutoModeButton();
            RefreshAutoModeButton();
            BindGameplaySpeedDropdownVisuals();
            BindAISettingsDropdownVisuals();
            SyncSettingsPanel();
            ResolveRuntimeLogPanel();
            CloseTacticalWarningPanel();
            ResolveOrderScreenReticles();
            gameMenuType = GameMenuType.Free;
            if (panelLoading != null)
                panelLoading.SetActive(true);
            ShowMenuTypeMenu();
            buttonBack.onClick.AddListener(ButtonBackClicked);
            buttonStats.onClick.AddListener(ButtonStatsClicked);
            buttonGame.onClick.AddListener(ButtonGameClicked);
            if (buttonCampAmmo != null)
                buttonCampAmmo.onClick.AddListener(() => OpenCampFromCurrentMenu(CampMenuType.Ammo));
            if (buttonCampSoldiers != null)
                buttonCampSoldiers.onClick.AddListener(() => OpenCampFromCurrentMenu(CampMenuType.Soldiers));
            if (buttonCampVehicles != null)
                buttonCampVehicles.onClick.AddListener(() => OpenCampFromCurrentMenu(CampMenuType.Vehicles));
            if (buttonCampBuildings != null)
                buttonCampBuildings.onClick.AddListener(() => OpenCampFromCurrentMenu(CampMenuType.Buildings));
            if (buttonDestroy != null)
                buttonDestroy.onClick.AddListener(ButtonDestroyClicked);
            if (buttonConfirm != null)
                buttonConfirm.onClick.AddListener(ButtonConfirmClicked);
            if (buttonMap != null)
                buttonMap.onClick.AddListener(ButtonMapClicked);
            if (buttonCamera != null)
                buttonCamera.onClick.AddListener(ButtonCameraClicked);
            if (_buttonAutoMode != null)
            {
                _buttonAutoMode.onClick.RemoveListener(ButtonAutoModeClicked);
                _buttonAutoMode.onClick.AddListener(ButtonAutoModeClicked);
            }
            if (buttonSettings != null)
            {
                buttonSettings.onClick.RemoveListener(ButtonSettingsClicked);
                buttonSettings.onClick.AddListener(ButtonSettingsClicked);
            }
            if (campPreviewPreviousButton != null)
                campPreviewPreviousButton.onClick.AddListener(ButtonCampPreviewPreviousClicked);
            if (campPreviewNextButton != null)
                campPreviewNextButton.onClick.AddListener(ButtonCampPreviewNextClicked);
            ResolveCampPanels();
        }

        internal void BindBuildingUiQuerySystem(
            BuildingUiQuerySystem buildingUiQuerySystem,
            BuildingUiQuerySystem.Context buildingUiQueryContext)
        {
            _buildingUiQuerySystem = buildingUiQuerySystem;
            _buildingUiQueryContext = buildingUiQueryContext;
        }

        private void Awake()
        {
            ResolveRuntimeLogPanel();
            SubscribeRuntimeLog();
        }

        public void SyncRuntimeState()
        {
            if (!_initialized || _gameStartPending || HasModalPanelOpen())
                return;

            bool playRequested = _runtimeGameplayStateSystem.PlayRequested;
            if (playRequested)
                SyncTacticalWarningPanel();

            double start = Time.realtimeSinceStartupAsDouble;
            double afterSelection = start;
            double afterMoneyTime = start;
            double afterRequest = start;
            double afterPreview = start;
            double afterReticles = start;
            double afterMinimap = start;
            double afterFullscreenMap = start;

            if (menuType == MenuType.Game)
            {
                if (gameMenuType != GameMenuType.Map)
                    SyncGameSelectionPanels();

                UpdateCameraButtonState();
            }
            else if (menuType == MenuType.Camp)
            {
                if (_campPreviewRevision != SharedPrefabPreviewCache.Revision)
                    RefreshCampList();

                if (_campSelectedIndex >= 0 && _campSelectedIndex < _campEntries.Count)
                    UpdateCampPriceState();

                ApplyCampRankBadgeContentScale();
            }
            else if (menuType == MenuType.Stats)
            {
                UpdateStatsPanel();
            }
            afterSelection = Time.realtimeSinceStartupAsDouble;

            if (playRequested || menuType == MenuType.Camp || menuType == MenuType.Stats)
                UpdateMoneyLabel();
            RefreshAutoModeButton();
            if (playRequested)
                UpdateTimePanel();
            afterMoneyTime = Time.realtimeSinceStartupAsDouble;
            if (playRequested)
                UpdateRequestPanel();
            afterRequest = Time.realtimeSinceStartupAsDouble;
            if (menuType == MenuType.Camp)
                UpdateCampSelectedModelPreviewRuntime();
            afterPreview = Time.realtimeSinceStartupAsDouble;
            if (playRequested)
                UpdateOrderScreenReticles();
            afterReticles = Time.realtimeSinceStartupAsDouble;

            if (playRequested)
                UpdateCanvasMinimap();
            afterMinimap = Time.realtimeSinceStartupAsDouble;
            if (playRequested)
                UpdateFullscreenMap();
            afterFullscreenMap = Time.realtimeSinceStartupAsDouble;

            double total = afterFullscreenMap - start;
            if (EnableMenuCanvasDiagnostics && total >= MenuCanvasDiagThresholdSeconds && Time.frameCount >= _nextMenuCanvasDiagFrame)
            {
                _nextMenuCanvasDiagFrame = Time.frameCount + MenuCanvasDiagIntervalFrames;
                Debug.Log(
                    $"[MenuCanvasDiag] frame={Time.frameCount} total={(total * 1000d):F1}ms " +
                    $"selection={(afterSelection - start) * 1000d:F1}ms moneyTime={(afterMoneyTime - afterSelection) * 1000d:F1}ms " +
                    $"request={(afterRequest - afterMoneyTime) * 1000d:F1}ms preview={(afterPreview - afterRequest) * 1000d:F1}ms " +
                    $"reticles={(afterReticles - afterPreview) * 1000d:F1}ms minimap={(afterMinimap - afterReticles) * 1000d:F1}ms " +
                    $"fullscreen={(afterFullscreenMap - afterMinimap) * 1000d:F1}ms menu={menuType} game={gameMenuType}");
            }
        }

        public void SyncInputState()
        {
            if (!_initialized || _gameStartPending)
                return;

            bool primaryPointerPressed = IsPrimaryPointerPressed();
            if (_warningOpen)
            {
                if (Time.unscaledTime >= _warningAutoCloseAt)
                {
                    CloseGenericWarningPanel();
                }
                else if (Time.frameCount > _warningOpenedFrame && primaryPointerPressed && !_wasPrimaryPointerPressed)
                {
                    CloseGenericWarningPanel();
                }
            }
            if (_runtimeGameplayStateSystem.PlayRequested)
                SyncTacticalWarningPanel();

            _wasPrimaryPointerPressed = primaryPointerPressed;

            if (HasModalPanelOpen())
                return;

            SyncZoomHoldState();
        }

        public void SetFpsLabel(int fps)
        {
            if (fpsText == null)
                return;

            fpsText.text = Mathf.Max(0, fps).ToString();
        }

        public void NotifyBootstrapReady()
        {
            if (panelLoading != null)
                panelLoading.SetActive(false);
            UpdateMoneyLabel();
            UpdateTimePanel();
            RefreshAutoModeButton();
            if (menuType == MenuType.Menu)
                ShowMenuTypeMenu();
        }

        public void NotifyGameplayReady()
        {
            _gameStartPending = false;
            if (panelLoading != null)
                panelLoading.SetActive(false);
            InvalidateMinimap();
            InvalidateFullscreenMap();
            menuType = MenuType.Game;
            UpdateMoneyLabel();
            UpdateTimePanel();
            RefreshAutoModeButton();
            UpdatePanels();
        }
        
        public void HideComplete(Animator panel)
        {
            panel.gameObject.SetActive(false);
        }

        private void PanelHide(Animator panel)
        {
            panel.SetTrigger(FadeOut);
        }
        
        private void PanelShow(Animator panel)
        {
            panelCurrent = panel;
            panel.gameObject.SetActive(true);
            panel.SetTrigger(FadeIn);
        }

        private void ButtonGameClicked()
        {
            if (_gameStartPending)
                return;

            SuppressNextWorldClick();
            _gameStartPending = true;
            if (buttonGame != null)
                buttonGame.gameObject.SetActive(false);
            if (panelLoading != null)
                panelLoading.SetActive(true);
            GameRequested?.Invoke();
        }

        public void RequestGameStart()
        {
            ButtonGameClicked();
        }

        private void ButtonStatsClicked()
        {
            SuppressNextWorldClick();
            _statsOpenedFromGame = menuType == MenuType.Game;
            menuType = MenuType.Stats;
            UpdatePanels();
        }
        
        private void OpenCampFromCurrentMenu(CampMenuType type)
        {
            SuppressNextWorldClick();
            if (menuType == MenuType.Game)
                _campOpenedFromGame = true;
            else if (menuType != MenuType.Camp)
                _campOpenedFromGame = false;
            _campMenuType = type;
            _campOpenHasExplicitTarget = true;
            menuType = MenuType.Camp;
            UpdatePanels();
        }

        private void ButtonBackClicked()
        {
            SuppressNextWorldClick();
            if (_warningOpen)
            {
                CloseGenericWarningPanel();
                return;
            }

            if (_settingsOpen)
            {
                CloseSettingsPanel();
                return;
            }

            if (_confirmOpen)
            {
                if (_confirmMode == ConfirmMode.PlaceBuilding)
                    _buildingUiCommandSystem?.CancelBuildingPlacement(_buildingUiCommandContext);
                CloseConfirmPanel();
                return;
            }

            if (menuType == MenuType.Camp)
            {
                if (_campOpenedFromGame && _campHasDeferredProductionFocus)
                {
                    _buildingUiCommandSystem?.FocusLastCampProductionRequest(_buildingUiCommandContext);
                    _campHasDeferredProductionFocus = false;
                }
                menuType = _campOpenedFromGame ? MenuType.Game : MenuType.Menu;
            }
            else if (menuType == MenuType.Stats)
            {
                menuType = _statsOpenedFromGame ? MenuType.Game : MenuType.Menu;
            }
            else if (menuType == MenuType.Game)
            {
                if (gameMenuType == GameMenuType.Free)
                {
                    menuType = MenuType.Menu;
                }
                else
                {
                    ClearGameplaySelection();
                    menuType = MenuType.Game;
                    ShowGameMenuType(GameMenuType.Free);
                }
            }
            else
            {
                menuType = MenuType.Menu;
            }
            UpdatePanels();
        }

        private void ButtonDestroyClicked()
        {
            SuppressNextWorldClick();
            if (menuType != MenuType.Game || gameMenuType != GameMenuType.Select)
                return;

            if (_buildingUiCommandSystem != null && _buildingUiCommandSystem.HasActiveBuilding(_buildingUiCommandContext))
            {
                OpenDestroyConfirmPanel(true, _buildingUiCommandSystem.SelectedBuildingDisplayName(_buildingUiCommandContext));
                return;
            }

            if (_selectionSystem != null && _selectionSystem.CanDestroyFocusedUnit)
                OpenDestroyConfirmPanel(false, _selectionSystem.FocusedUnitLabel);
        }

        private void ButtonMapClicked()
        {
            SuppressNextWorldClick();
            if (menuType != MenuType.Game)
                return;

            ShowGameMenuType(GameMenuType.Map);
        }

        private void ButtonCameraClicked()
        {
            SuppressNextWorldClick();
            if (menuType != MenuType.Game || _selectionSystem == null)
                return;

            _selectionSystem.ToggleNormalIsoMode();
            UpdateCameraButtonState();
        }

        private void ButtonAutoModeClicked()
        {
            SuppressNextWorldClick();
            SetPlayerAutoMode(!_runtimeGameplayStateSystem.PlayerAutoModeEnabled);
            RefreshAutoModeButton();
        }

        private void ButtonSettingsClicked()
        {
            SuppressNextWorldClick();
            _settingsOpen = true;
            SyncSettingsPanel();
            if (buttonBack != null)
                buttonBack.gameObject.SetActive(true);
        }

        private void ApplyZoomDirectionState()
        {
            _runtimeGameplayStateSystem.ZoomInHeld = _activeZoomDirection > 0;
            _runtimeGameplayStateSystem.ZoomOutHeld = _activeZoomDirection < 0;
        }

        private void SyncZoomHoldState()
        {
            int direction = 0;
            if (menuType == MenuType.Game && !HasModalPanelOpen())
            {
                Vector2? screenPosition = GetPointerScreenPosition();
                bool pointerPressed = IsPrimaryPointerPressed();
                if (pointerPressed && screenPosition.HasValue)
                {
                    if (IsPointerOverButton(buttonZoomIn, screenPosition.Value))
                        direction = 1;
                    else if (IsPointerOverButton(buttonZoomOut, screenPosition.Value))
                        direction = -1;
                }
            }

            if (direction != 0 && _activeZoomDirection == 0)
                SuppressNextWorldClick();

            _activeZoomDirection = direction;
            ApplyZoomDirectionState();
        }

        private static bool IsPrimaryPointerPressed()
        {
            return global::GamePointerInput.IsPrimaryPointerPressed();
        }

        private static Vector2? GetPointerScreenPosition()
        {
            if (global::GamePointerInput.TryGetPointerPosition(out Vector2 screenPosition))
                return screenPosition;

            return null;
        }

        private bool IsPointerOverButton(Button button, Vector2 screenPosition)
        {
            if (button == null || !button.gameObject.activeInHierarchy)
                return false;

            RectTransform rectTransform = button.transform as RectTransform;
            if (rectTransform == null)
                return false;

            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition, null);
        }

        private void ButtonConfirmClicked()
        {
            SuppressNextWorldClick();
            if (!_confirmOpen)
                return;

            switch (_confirmMode)
            {
                case ConfirmMode.DestroyBuilding:
                    _buildingUiCommandSystem?.DeleteSelectedBuilding(_buildingUiCommandContext);
                    break;
                case ConfirmMode.DestroyUnit:
                    _selectionSystem?.DestroyFocusedUnit();
                    break;
                case ConfirmMode.PlaceBuilding:
                    if (_buildingUiCommandSystem == null || !_buildingUiCommandSystem.ConfirmBuildingPlacement(_buildingUiCommandContext))
                        return;
                    UpdateMoneyLabel();
                    break;
            }

            CloseConfirmPanel();
            SyncGameSelectionPanels();
        }

        private void ButtonSelectAllClicked()
        {
            SuppressNextWorldClick();
            _selectionSystem?.CaptureUiClickSequence();
            _buildingUiCommandSystem?.ClearSelectedBuilding(_buildingUiCommandContext, "MenuView.ButtonSelectAllClicked");
            _selectionSystem?.SelectAllVisiblePlayerUnits();
            SyncRuntimeState();
        }

        private void ButtonSelectAllSoldiersClicked()
        {
            SuppressNextWorldClick();
            _selectionSystem?.CaptureUiClickSequence();
            _buildingUiCommandSystem?.ClearSelectedBuilding(_buildingUiCommandContext, "MenuView.ButtonSelectAllSoldiersClicked");
            _selectionSystem?.SelectAllVisiblePlayerSoldiers();
            SyncRuntimeState();
        }

        private void ButtonSelectAllVehiclesClicked()
        {
            SuppressNextWorldClick();
            _selectionSystem?.CaptureUiClickSequence();
            _buildingUiCommandSystem?.ClearSelectedBuilding(_buildingUiCommandContext, "MenuView.ButtonSelectAllVehiclesClicked");
            _selectionSystem?.SelectAllVisiblePlayerVehicles();
            SyncRuntimeState();
        }

        private void ButtonDeselectAllClicked()
        {
            SuppressNextWorldClick();
            ClearGameplaySelection();
            SyncRuntimeState();
        }

        private void ButtonSelectClicked()
        {
            SuppressNextWorldClick();
            _selectionSystem?.CaptureUiClickSequence();
            _selectionSystem?.DeselectAllUnits("MenuView.ButtonSelectClicked");
            _buildingUiCommandSystem?.ExitBuildMode(_buildingUiCommandContext);
            _runtimeGameplayStateSystem.SelectionModeActive = true;
            if (_buttonSelect != null)
                _buttonSelect.gameObject.SetActive(false);
        }

        private void UpdatePanels()
        {
            Animator targetPanel = GetPanelForMenuType(menuType);
            if (targetPanel != null && panelCurrent != targetPanel)
                PanelHide(panelCurrent);

            switch (menuType)
            {
                case MenuType.Menu:
                    ShowMenuTypeMenu();
                    break;
                case MenuType.Camp:
                    ShowMenuTypeCamp();
                    break;
                case MenuType.Stats:
                    ShowMenuTypeStats();
                    break;
                case MenuType.Game:
                    ShowMenuTypeGame();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            RefreshAutoModeButton();
        }

        private Animator GetPanelForMenuType(MenuType type)
        {
            return type switch
            {
                MenuType.Menu => panelMenu,
                MenuType.Camp => panelCamp,
                MenuType.Stats => panelStats,
                MenuType.Game => panelGame,
                _ => null
            };
        }

        private void ShowMenuTypeMenu()
        {
            buttonGame.gameObject.SetActive(!_gameStartPending);
            SetCampButtonsPanelActive(true);
            buttonBack.gameObject.SetActive(false);
            panelTime.SetActive(false);
            panelZoom.SetActive(false);
            SyncModalPanels();
            EnsureTopLevelPanelShown(panelMenu);
        }
        
        private void ShowMenuTypeCamp()
        {
            SetCampButtonsPanelActive(true);
            buttonBack.gameObject.SetActive(true);
            SyncModalPanels();
            EnsureTopLevelPanelShown(panelCamp);
            ResolveCampPanels();
            if (_campRequestButton != null)
                _campRequestButton.SetActive(_campOpenedFromGame);

            bool selectDefaultCampTab = !_campHasOpenedOnce && !_campOpenHasExplicitTarget;
            if (selectDefaultCampTab)
                _campMenuType = CampMenuType.Buildings;

            UpdateCampTabSelection();
            RefreshCampList();
            StopCoroutine(nameof(SelectCurrentCampTabNextFrame));
            StartCoroutine(nameof(SelectCurrentCampTabNextFrame));
            _campOpenHasExplicitTarget = false;
            if (selectDefaultCampTab)
                _campHasOpenedOnce = true;
            else if (!_campHasOpenedOnce)
                _campHasOpenedOnce = true;
        }
        
        private void ShowMenuTypeStats()
        {
            SetCampButtonsPanelActive(false);
            buttonBack.gameObject.SetActive(true);
            SyncModalPanels();
            EnsureTopLevelPanelShown(panelStats);
        }
        
        private void ShowMenuTypeGame()
        {
            buttonGame.gameObject.SetActive(false);
            SetCampButtonsPanelActive(gameMenuType == GameMenuType.Free && !_confirmOpen);
            buttonBack.gameObject.SetActive(true);
            panelTime.SetActive(true);
            panelZoom.SetActive(true);
            EnsureTopLevelPanelShown(panelGame);
            ShowGameMenuType(gameMenuType);
        }

        private void ResolveOrderScreenReticles()
        {
            if (_moveOrderScreenReticle == null)
            {
                GameObject moveReticle = moveOrderScreenReticle != null
                    ? moveOrderScreenReticle
                    : FindChildObjectByName(transform.root, "HUD_Reticle_RingTriangular_02");
                _moveOrderScreenReticle = moveReticle != null ? moveReticle.GetComponent<RectTransform>() : null;
            }

            if (_attackOrderScreenReticle == null)
            {
                GameObject attackReticle = attackOrderScreenReticle != null
                    ? attackOrderScreenReticle
                    : FindChildObjectByName(transform.root, "HUD_Reticle_Crosshair_03");
                _attackOrderScreenReticle = attackReticle != null ? attackReticle.GetComponent<RectTransform>() : null;
            }

            if (_moveOrderScreenReticle != null)
                _moveOrderScreenReticle.gameObject.SetActive(false);
            if (_attackOrderScreenReticle != null)
                _attackOrderScreenReticle.gameObject.SetActive(false);
        }

        private void ShowMoveOrderScreenReticle(Vector2 screenPosition)
        {
            if (_moveOrderScreenReticle == null)
                ResolveOrderScreenReticles();
            ShowOrderScreenReticle(_moveOrderScreenReticle, screenPosition, ref _moveOrderScreenReticleHideTime);
        }

        private void ShowAttackOrderScreenReticle(Vector2 screenPosition)
        {
            if (_attackOrderScreenReticle == null)
                ResolveOrderScreenReticles();
            ShowOrderScreenReticle(_attackOrderScreenReticle, screenPosition, ref _attackOrderScreenReticleHideTime);
        }

        private void ShowOrderScreenReticle(RectTransform reticle, Vector2 screenPosition, ref float hideTime)
        {
            if (reticle == null)
                return;

            RectTransform parentRect = reticle.parent as RectTransform;
            if (parentRect != null)
            {
                Camera eventCamera = ResolveCanvasEventCamera(reticle);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPosition, eventCamera, out Vector2 localPoint))
                    reticle.anchoredPosition = localPoint;
                else
                    reticle.position = screenPosition;
            }
            else
            {
                reticle.position = screenPosition;
            }

            reticle.gameObject.SetActive(true);
            hideTime = Time.unscaledTime + 1.25f;
        }

        private static Camera ResolveCanvasEventCamera(RectTransform rectTransform)
        {
            Canvas canvas = rectTransform != null ? rectTransform.GetComponentInParent<Canvas>() : null;
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }

        private void UpdateOrderScreenReticles()
        {
            float now = Time.unscaledTime;
            if (_moveOrderScreenReticle != null && _moveOrderScreenReticle.gameObject.activeSelf && now >= _moveOrderScreenReticleHideTime)
                _moveOrderScreenReticle.gameObject.SetActive(false);
            if (_attackOrderScreenReticle != null && _attackOrderScreenReticle.gameObject.activeSelf && now >= _attackOrderScreenReticleHideTime)
                _attackOrderScreenReticle.gameObject.SetActive(false);
        }

        private void HideOrderScreenReticles()
        {
            if (_moveOrderScreenReticle != null)
                _moveOrderScreenReticle.gameObject.SetActive(false);
            if (_attackOrderScreenReticle != null)
                _attackOrderScreenReticle.gameObject.SetActive(false);

            _moveOrderScreenReticleHideTime = -1f;
            _attackOrderScreenReticleHideTime = -1f;
        }

        private void EnsureTopLevelPanelShown(Animator panel)
        {
            if (panel == null)
                return;

            if (panelCurrent != panel)
            {
                PanelShow(panel);
                return;
            }

            if (!panel.gameObject.activeSelf)
                panel.gameObject.SetActive(true);
        }

        private void ShowGameMenuType(GameMenuType type)
        {
            ResolveGamePanels();
            if (type == GameMenuType.Map && gameMenuType != GameMenuType.Map)
                InvalidateFullscreenMap();
            gameMenuType = type;
            if (menuType == MenuType.Game)
                SetCampButtonsPanelActive(!_confirmOpen && type == GameMenuType.Free);
            SetGameSubPanelActive(gamePanelFree, !_confirmOpen && type == GameMenuType.Free);
            SetGameSubPanelActive(gamePanelSelect, !_confirmOpen && type == GameMenuType.Select);
            SetGameSubPanelActive(gamePanelMap, type == GameMenuType.Map);
            SyncModalPanels();
            UpdateCameraButtonState();
            SyncSelectionChildren();
        }

        private void ResolveGamePanels()
        {
            if (panelGame == null)
                return;

            Transform gameRoot = panelGame.transform;
            if (gamePanelFree == null)
                gamePanelFree = FindAnimatorByName(gameRoot, "Panel_Free");
            if (gamePanelSelect == null)
                gamePanelSelect = FindAnimatorByName(gameRoot, "Panel_Selected");
            if (gamePanelMap == null)
                gamePanelMap = FindAnimatorByName(gameRoot, "Panel_Map");

            Transform selectRoot = gamePanelSelect != null ? gamePanelSelect.transform : null;
            if (selectPanelSingle == null)
                selectPanelSingle = FindChildObjectByName(selectRoot, "Panel_Single");
            if (selectPanelMulti == null)
                selectPanelMulti = FindChildObjectByName(selectRoot, "Panel_Multi");
            Transform freeSelectionRoot = gamePanelFree != null ? gamePanelFree.transform : gameRoot;
            GameObject freeSelectionPanel = FindChildObjectByName(freeSelectionRoot, "Panel_Select");
            if (freeSelectionPanel != null)
                freeSelectionRoot = freeSelectionPanel.transform;
            if (_buttonSelectAll == null)
            {
                GameObject selectAllObject = FindChildObjectByName(gameRoot, "Button_Select_All");
                _buttonSelectAll = selectAllObject != null ? selectAllObject.GetComponent<Button>() : null;
                if (_buttonSelectAll != null)
                    _buttonSelectAll.onClick.AddListener(ButtonSelectAllClicked);
            }
            if (_buttonSelectAllSoldiers == null)
            {
                _buttonSelectAllSoldiers = ResolveExclusiveButton(freeSelectionRoot, "Button_Select_All_Soldiers", ButtonSelectAllSoldiersClicked);
            }
            if (_buttonSelectAllSoldiersAlias == null)
            {
                _buttonSelectAllSoldiersAlias = ResolveExclusiveButton(freeSelectionRoot, "Button_Soldiers", ButtonSelectAllSoldiersClicked);
            }
            if (_buttonSelectAllVehicles == null)
            {
                _buttonSelectAllVehicles = ResolveExclusiveButton(freeSelectionRoot, "Button_Select_All_Vehicles", ButtonSelectAllVehiclesClicked);
            }
            if (_buttonSelectAllVehiclesAlias == null)
            {
                _buttonSelectAllVehiclesAlias = ResolveExclusiveButton(freeSelectionRoot, "Button_Vehicles", ButtonSelectAllVehiclesClicked);
            }
            if (_buttonSelect == null)
            {
                GameObject selectObject = FindChildObjectByName(gameRoot, "Button_Select");
                _buttonSelect = selectObject != null ? selectObject.GetComponent<Button>() : null;
                if (_buttonSelect != null)
                    _buttonSelect.onClick.AddListener(ButtonSelectClicked);
            }
            if (_buttonDeselectAll == null)
            {
                GameObject deselectAllObject = FindChildObjectByName(gameRoot, "Button_Deselect_All");
                _buttonDeselectAll = deselectAllObject != null ? deselectAllObject.GetComponent<Button>() : null;
                if (_buttonDeselectAll != null)
                    _buttonDeselectAll.onClick.AddListener(ButtonDeselectAllClicked);
            }
            if (_cameraButtonSelected == null && buttonCamera != null)
                _cameraButtonSelected = FindChildObjectByName(buttonCamera.transform, "Selected");

            if (_minimapRootRect == null)
            {
                GameObject minimapPanel = FindChildObjectByName(gameRoot, "Panel_Minimap");
                _minimapRootRect = minimapPanel != null ? minimapPanel.GetComponent<RectTransform>() : null;
            }
            if (_minimapMapRect == null)
            {
                Transform mapTransform = _minimapRootRect != null
                    ? _minimapRootRect.Find("Content/HUD_Minimap_Contents/Map_Container/Map")
                    : null;
                _minimapMapRect = mapTransform != null ? mapTransform.GetComponent<RectTransform>() : null;
                _minimapMapImage = mapTransform != null ? mapTransform.GetComponent<Image>() : null;
                if (_minimapMapImage != null)
                {
                    _minimapMapImage.preserveAspect = false;
                    _minimapMapImage.type = Image.Type.Simple;
                }
            }
            if (_minimapElementsRect == null)
            {
                Transform elementsTransform = _minimapRootRect != null
                    ? _minimapRootRect.Find("Content/HUD_Minimap_Contents/Elements_Container")
                    : null;
                _minimapElementsRect = elementsTransform != null ? elementsTransform.GetComponent<RectTransform>() : null;
            }

            Transform fullscreenRoot = gamePanelMap != null ? gamePanelMap.transform : null;
            if (_fullscreenMapRootRect == null)
            {
                GameObject minimapPanel = FindChildObjectByName(fullscreenRoot, "Panel_Minimap");
                _fullscreenMapRootRect = minimapPanel != null ? minimapPanel.GetComponent<RectTransform>() : null;
            }
            if (_fullscreenMapMapRect == null)
            {
                Transform mapTransform = _fullscreenMapRootRect != null
                    ? (_fullscreenMapRootRect.Find("Map_Container/Map") ?? FindDescendantByName(_fullscreenMapRootRect, "Map"))
                    : null;
                _fullscreenMapMapRect = mapTransform != null ? mapTransform.GetComponent<RectTransform>() : null;
                _fullscreenMapImage = mapTransform != null ? mapTransform.GetComponent<Image>() : null;
                if (_fullscreenMapImage != null)
                {
                    _fullscreenMapImage.preserveAspect = false;
                    _fullscreenMapImage.type = Image.Type.Simple;
                }
            }
            if (_fullscreenMapElementsRect == null)
            {
                Transform elementsTransform = _fullscreenMapRootRect != null
                    ? (_fullscreenMapRootRect.Find("Elements_Container") ?? FindDescendantByName(_fullscreenMapRootRect, "Elements_Container"))
                    : null;
                _fullscreenMapElementsRect = elementsTransform != null ? elementsTransform.GetComponent<RectTransform>() : null;
            }

            EnsureCanvasMinimapTemplates();
            EnsureFullscreenMapTemplates();
        }

        private void EnsureCanvasMinimapTemplates()
        {
            if (_minimapElementsRect == null)
                return;

            RectTransform runtimeRoot = GetOrCreateMinimapRuntimeElementsRoot();
            for (int i = 0; i < _minimapElementsRect.childCount; i++)
            {
                Transform child = _minimapElementsRect.GetChild(i);
                if (runtimeRoot != null && child == runtimeRoot)
                    continue;

                child.gameObject.SetActive(false);
            }

            if (_minimapAllyTemplate == null)
            {
                Transform ally = _minimapElementsRect.Find("Map_Icon_Ally") ?? FindDescendantByName(_minimapElementsRect, "Map_Icon_Ally");
                _minimapAllyTemplate = ally as RectTransform;
                if (_minimapAllyTemplate != null)
                {
                    _minimapAllySprite = FindPreferredMarkerImage(_minimapAllyTemplate)?.sprite;
                    ally.gameObject.SetActive(false);
                }

                Transform enemy = _minimapElementsRect.Find("Map_Icon_Enemy") ?? FindDescendantByName(_minimapElementsRect, "Map_Icon_Enemy");
                _minimapEnemyTemplate = enemy as RectTransform;
                if (_minimapEnemyTemplate != null)
                {
                    _minimapEnemySprite = FindPreferredMarkerImage(_minimapEnemyTemplate)?.sprite;
                    enemy.gameObject.SetActive(false);
                }

                Transform neutral = _minimapElementsRect.Find("Map_Icon_Neutral") ?? FindDescendantByName(_minimapElementsRect, "Map_Icon_Neutral");
                _minimapNeutralTemplate = neutral as RectTransform;
                if (_minimapNeutralTemplate != null)
                {
                    _minimapNeutralSprite = FindPreferredMarkerImage(_minimapNeutralTemplate)?.sprite;
                    neutral.gameObject.SetActive(false);
                }

                Transform objective = _minimapElementsRect.Find("Map_Icon_Objective") ?? FindDescendantByName(_minimapElementsRect, "Map_Icon_Objective");
                _minimapObjectiveTemplate = objective as RectTransform;
                if (_minimapObjectiveTemplate != null)
                {
                    _minimapObjectiveSprite = FindPreferredMarkerImage(_minimapObjectiveTemplate)?.sprite;
                    objective.gameObject.SetActive(false);
                }

                Transform home = _minimapElementsRect.Find("Map_Icon_Home") ?? FindDescendantByName(_minimapElementsRect, "Map_Icon_Home");
                _minimapHomeTemplate = home as RectTransform;
                if (_minimapHomeTemplate != null)
                {
                    Image homeImage = FindPreferredMarkerImage(_minimapHomeTemplate);
                    _minimapHomeSprite = homeImage?.sprite;
                    if (homeImage != null)
                        _minimapForcedIconColor = homeImage.color;
                    home.gameObject.SetActive(false);
                }

                Transform homeNeutral = _minimapElementsRect.Find("Map_Icon_Home_Neutral") ?? FindDescendantByName(_minimapElementsRect, "Map_Icon_Home_Neutral");
                _minimapHomeNeutralTemplate = homeNeutral as RectTransform;
                if (_minimapHomeNeutralTemplate != null)
                {
                    Image homeNeutralImage = FindPreferredMarkerImage(_minimapHomeNeutralTemplate);
                    _minimapHomeNeutralSprite = homeNeutralImage?.sprite;
                    if (homeNeutralImage != null)
                        _minimapNeutralBuildingColor = homeNeutralImage.color;
                    homeNeutral.gameObject.SetActive(false);
                }

                Transform homeEnemy = _minimapElementsRect.Find("Map_Icon_Home_Enemy") ?? FindDescendantByName(_minimapElementsRect, "Map_Icon_Home_Enemy");
                _minimapHomeEnemyTemplate = homeEnemy as RectTransform;
                if (_minimapHomeEnemyTemplate != null)
                {
                    Image homeEnemyImage = FindPreferredMarkerImage(_minimapHomeEnemyTemplate);
                    _minimapHomeEnemySprite = homeEnemyImage?.sprite;
                    if (homeEnemyImage != null)
                        _minimapEnemyBuildingColor = homeEnemyImage.color;
                    homeEnemy.gameObject.SetActive(false);
                }

                Transform wall = _minimapElementsRect.Find("Map_Icon_Wall") ?? FindDescendantByName(_minimapElementsRect, "Map_Icon_Wall");
                _minimapWallTemplate = wall as RectTransform;
                if (_minimapWallTemplate != null)
                {
                    _minimapWallSprite = FindPreferredMarkerImage(_minimapWallTemplate)?.sprite;
                    wall.gameObject.SetActive(false);
                }

                Transform wallEnemy = _minimapElementsRect.Find("Map_Icon_Wall_Enemy") ?? FindDescendantByName(_minimapElementsRect, "Map_Icon_Wall_Enemy");
                _minimapWallEnemyTemplate = wallEnemy as RectTransform;
                if (_minimapWallEnemyTemplate != null)
                {
                    _minimapWallEnemySprite = FindPreferredMarkerImage(_minimapWallEnemyTemplate)?.sprite;
                    wallEnemy.gameObject.SetActive(false);
                }

                Transform selected = _minimapElementsRect.Find("Map_Icon_Player") ?? FindDescendantByName(_minimapElementsRect, "Map_Icon_Player");
                _minimapSelectedTemplate = selected as RectTransform;
                if (_minimapSelectedTemplate != null)
                {
                    _minimapSelectedSprite = FindTemplateImage(selected)?.sprite;
                    Transform icnZ = selected != null ? FindDescendantByName(selected, "ICN_Z") : null;
                    Image arrowImage = FindTemplateImage(icnZ);
                    if (arrowImage != null)
                        _minimapSelectedArrowSprite = arrowImage.sprite;
                    selected.gameObject.SetActive(false);
                }
            }
        }

        private static Image FindTemplateImage(Transform root)
        {
            if (root == null)
                return null;

            Image image = root.GetComponent<Image>();
            if (image != null)
                return image;

            return root.GetComponentInChildren<Image>(true);
        }

        private static Image FindPreferredMarkerImage(Transform root)
        {
            if (root == null)
                return null;

            Transform exactIcon = root.Find("Icon/ICON");
            if (exactIcon != null)
            {
                Image exactImage = exactIcon.GetComponent<Image>();
                if (exactImage != null)
                    return exactImage;
            }

            Transform iconGroup = root.Find("Icon");
            if (iconGroup != null)
            {
                Image groupImage = iconGroup.GetComponent<Image>();
                if (groupImage != null)
                    return groupImage;
            }

            Transform namedIcon = FindDescendantByName(root, "ICON");
            if (namedIcon != null)
            {
                Image namedImage = namedIcon.GetComponent<Image>();
                if (namedImage != null)
                    return namedImage;
            }

            return FindTemplateImage(root);
        }

        private static Transform FindPreferredMarkerTransform(Transform root)
        {
            if (root == null)
                return null;

            return root.Find("Icon/ICON")
                   ?? root.Find("Icon")
                   ?? FindDescendantByName(root, "ICON");
        }

        private RectTransform GetOrCreateMinimapRuntimeElementsRoot()
        {
            if (_minimapRuntimeElementsRect != null)
            {
                _minimapRuntimeElementsRect.gameObject.SetActive(true);
                return _minimapRuntimeElementsRect;
            }

            if (_minimapMapRect == null)
                return null;

            Transform existing = _minimapMapRect.Find("Runtime_Elements");
            if (existing != null)
            {
                _minimapRuntimeElementsRect = existing.GetComponent<RectTransform>();
                _minimapRuntimeElementsRect.gameObject.SetActive(true);
                return _minimapRuntimeElementsRect;
            }

            GameObject runtimeRoot = new GameObject("Runtime_Elements", typeof(RectTransform));
            runtimeRoot.transform.SetParent(_minimapMapRect, false);
            runtimeRoot.SetActive(true);
            _minimapRuntimeElementsRect = runtimeRoot.GetComponent<RectTransform>();
            _minimapRuntimeElementsRect.anchorMin = Vector2.zero;
            _minimapRuntimeElementsRect.anchorMax = Vector2.one;
            _minimapRuntimeElementsRect.offsetMin = Vector2.zero;
            _minimapRuntimeElementsRect.offsetMax = Vector2.zero;
            _minimapRuntimeElementsRect.pivot = new Vector2(0.5f, 0.5f);
            return _minimapRuntimeElementsRect;
        }

        private void EnsureFullscreenMapTemplates()
        {
            if (_fullscreenMapElementsRect == null)
                return;

            RectTransform runtimeRoot = GetOrCreateFullscreenMapRuntimeElementsRoot();
            for (int i = 0; i < _fullscreenMapElementsRect.childCount; i++)
            {
                Transform child = _fullscreenMapElementsRect.GetChild(i);
                if (runtimeRoot != null && child == runtimeRoot)
                    continue;

                child.gameObject.SetActive(false);
            }
        }

        private RectTransform GetOrCreateFullscreenMapRuntimeElementsRoot()
        {
            if (_fullscreenMapRuntimeElementsRect != null)
            {
                _fullscreenMapRuntimeElementsRect.gameObject.SetActive(true);
                return _fullscreenMapRuntimeElementsRect;
            }

            if (_fullscreenMapMapRect == null)
                return null;

            Transform existing = _fullscreenMapMapRect.Find("Runtime_Elements");
            if (existing != null)
            {
                _fullscreenMapRuntimeElementsRect = existing.GetComponent<RectTransform>();
                _fullscreenMapRuntimeElementsRect.gameObject.SetActive(true);
                return _fullscreenMapRuntimeElementsRect;
            }

            GameObject runtimeRoot = new GameObject("Runtime_Elements", typeof(RectTransform));
            runtimeRoot.transform.SetParent(_fullscreenMapMapRect, false);
            runtimeRoot.SetActive(true);
            _fullscreenMapRuntimeElementsRect = runtimeRoot.GetComponent<RectTransform>();
            _fullscreenMapRuntimeElementsRect.anchorMin = Vector2.zero;
            _fullscreenMapRuntimeElementsRect.anchorMax = Vector2.one;
            _fullscreenMapRuntimeElementsRect.offsetMin = Vector2.zero;
            _fullscreenMapRuntimeElementsRect.offsetMax = Vector2.zero;
            _fullscreenMapRuntimeElementsRect.pivot = new Vector2(0.5f, 0.5f);
            return _fullscreenMapRuntimeElementsRect;
        }

        private void UpdateCanvasMinimap()
        {
            if (menuType != MenuType.Game || !_runtimeGameplayStateSystem.PlayRequested)
                return;

            ResolveGamePanels();
            if (_minimapMapImage == null || _minimapMapRect == null || _minimapElementsRect == null)
                return;

            EnsureCanvasMinimapResources();
            if (!TryGetGridConfig(out GridConfig grid))
                return;

            if (Time.unscaledTime < _nextMinimapDynamicRefreshTime)
                return;

            _nextMinimapDynamicRefreshTime = Time.unscaledTime + MinimapDynamicRefreshInterval;
            if (!TryGetMinimapViewBounds(grid, out MinimapViewBounds viewBounds))
                return;

            if (ShouldRebuildMinimapStatic(viewBounds))
            {
                RebuildCanvasMinimapStaticLayer(grid, viewBounds);
                _minimapTexture.SetPixels32(_minimapStaticPixels);
                _minimapTexture.Apply(false, false);
                _lastMinimapViewBounds = viewBounds;
                _minimapStaticBuilt = true;
            }

            Array.Copy(_minimapStaticPixels, _minimapPixels, _minimapStaticPixels.Length);
            ClearRuntimeMinimapIcons();
            bool minimapPixelsDirty = UpdateCanvasMinimapBuildings();
            minimapPixelsDirty |= UpdateCanvasMinimapUnits(grid);
            minimapPixelsDirty |= UpdateCanvasMinimapObjectives(grid);
            if (minimapPixelsDirty)
            {
                _minimapTexture.SetPixels32(_minimapPixels);
                _minimapTexture.Apply(false, false);
            }
        }

        private void UpdateFullscreenMap()
        {
            if (menuType != MenuType.Game || gameMenuType != GameMenuType.Map || !_runtimeGameplayStateSystem.PlayRequested)
                return;

            ResolveGamePanels();
            if (_fullscreenMapImage == null || _fullscreenMapMapRect == null)
                return;

            EnsureFullscreenMapResources();
            if (!TryGetGridConfig(out GridConfig grid))
                return;

            UpdateFullscreenMapCameraRect(grid);
            UpdateFullscreenMapCameraDrag(grid);

            if (!_fullscreenMapStaticBuilt)
            {
                RebuildFullscreenMapStaticLayer(grid);
                _fullscreenMapTexture.SetPixels32(_fullscreenMapStaticPixels);
                _fullscreenMapTexture.Apply(false, false);
                _fullscreenMapStaticBuilt = true;
            }

            if (Time.unscaledTime < _nextFullscreenMapDynamicRefreshTime)
                return;

            _nextFullscreenMapDynamicRefreshTime = Time.unscaledTime + FullscreenMapDynamicRefreshInterval;
            ClearFullscreenMapIcons();
            UpdateFullscreenMapBuildings(grid);
            UpdateFullscreenMapUnits(grid);
            UpdateFullscreenMapObjectives(grid);
        }

        private void EnsureCanvasMinimapResources()
        {
            if (_minimapTexture == null)
            {
                _minimapTexture = new Texture2D(MinimapResolution, MinimapResolution, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "CanvasMiniMap"
                };
            }

            if (_minimapSprite == null)
            {
                _minimapSprite = Sprite.Create(_minimapTexture, new Rect(0f, 0f, _minimapTexture.width, _minimapTexture.height), new Vector2(0.5f, 0.5f), 100f);
                _minimapSprite.hideFlags = HideFlags.HideAndDontSave;
                _minimapMapImage.sprite = _minimapSprite;
            }

            int pixelCount = MinimapResolution * MinimapResolution;
            if (_minimapPixels == null || _minimapPixels.Length != pixelCount)
                _minimapPixels = new Color32[pixelCount];
            if (_minimapStaticPixels == null || _minimapStaticPixels.Length != pixelCount)
                _minimapStaticPixels = new Color32[pixelCount];
        }

        private void EnsureFullscreenMapResources()
        {
            if (_fullscreenMapTexture == null)
            {
                _fullscreenMapTexture = new Texture2D(FullscreenMapResolution, FullscreenMapResolution, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "CanvasFullscreenMap"
                };
            }

            if (_fullscreenMapSprite == null)
            {
                _fullscreenMapSprite = Sprite.Create(_fullscreenMapTexture, new Rect(0f, 0f, _fullscreenMapTexture.width, _fullscreenMapTexture.height), new Vector2(0.5f, 0.5f), 100f);
                _fullscreenMapSprite.hideFlags = HideFlags.HideAndDontSave;
                _fullscreenMapImage.sprite = _fullscreenMapSprite;
            }

            int pixelCount = FullscreenMapResolution * FullscreenMapResolution;
            if (_fullscreenMapPixels == null || _fullscreenMapPixels.Length != pixelCount)
                _fullscreenMapPixels = new Color32[pixelCount];
            if (_fullscreenMapStaticPixels == null || _fullscreenMapStaticPixels.Length != pixelCount)
                _fullscreenMapStaticPixels = new Color32[pixelCount];

            if (_fullscreenMapCameraRect == null)
                _fullscreenMapCameraRect = CreateFullscreenMapCameraRect();
        }

        private void InvalidateFullscreenMap()
        {
            _fullscreenMapStaticBuilt = false;
            _nextFullscreenMapDynamicRefreshTime = 0f;
        }

        private void InvalidateMinimap()
        {
            _minimapStaticBuilt = false;
            _nextMinimapDynamicRefreshTime = 0f;
        }

        private bool ShouldRebuildMinimapStatic(MinimapViewBounds viewBounds)
        {
            if (!_minimapStaticBuilt)
                return true;

            return Mathf.Abs(viewBounds.MinX - _lastMinimapViewBounds.MinX) > MinimapStaticRebuildThresholdWorld ||
                   Mathf.Abs(viewBounds.MaxX - _lastMinimapViewBounds.MaxX) > MinimapStaticRebuildThresholdWorld ||
                   Mathf.Abs(viewBounds.MinZ - _lastMinimapViewBounds.MinZ) > MinimapStaticRebuildThresholdWorld ||
                   Mathf.Abs(viewBounds.MaxZ - _lastMinimapViewBounds.MaxZ) > MinimapStaticRebuildThresholdWorld;
        }

        private void RebuildCanvasMinimapStaticLayer(GridConfig grid, MinimapViewBounds viewBounds)
        {
            FillMinimap(MinimapBackgroundColor, _minimapStaticPixels);
            DrawMinimapGrid(_minimapStaticPixels);
            DrawMinimapRoads(grid, viewBounds, _minimapStaticPixels);
        }

        private void RebuildFullscreenMapStaticLayer(GridConfig grid)
        {
            FillMinimap(MinimapBackgroundColor, _fullscreenMapStaticPixels);
            DrawMinimapGrid(_fullscreenMapStaticPixels);
            DrawFullscreenMapRoads(grid, _fullscreenMapStaticPixels);
        }

        private bool UpdateCanvasMinimapUnits(GridConfig grid)
        {
            if (!TryGetEntityManager(out EntityManager em) || _worldCamera == null)
                return false;

            _minimapUnitIconPixelKeys.Clear();
            bool drewUnitPixels = false;
            int selectedArrowIcons = 0;
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<UnitGrid>(), ComponentType.ReadOnly<Faction>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                int2 cell = em.GetComponentData<UnitGrid>(entity).Cell;
                Vector3 worldPosition = em.HasComponent<LocalTransform>(entity)
                    ? (Vector3)em.GetComponentData<LocalTransform>(entity).Position
                    : new Vector3(
                        grid.Origin.x + ((cell.x + 0.5f) * grid.CellSize),
                        grid.Origin.y,
                        grid.Origin.z + ((cell.y + 0.5f) * grid.CellSize));
                if (!TryWorldToViewportNormalized(worldPosition, out Vector2 normalized))
                    continue;

                bool isSelected = em.HasComponent<SelectedUnitTag>(entity);
                bool isCivilian = em.HasComponent<CivilianUnitTag>(entity);
                int factionId = em.GetComponentData<Faction>(entity).Id;
                if (TryViewportNormalizedToMinimapPixel(normalized, out int pixelX, out int pixelY))
                {
                    int pixelKey = pixelX + (pixelY * MinimapResolution);
                    if (isSelected || _minimapUnitIconPixelKeys.Add(pixelKey))
                    {
                        Color32 color = isSelected
                            ? MinimapSelectedUnitColor
                            : (isCivilian ? MinimapNeutralUnitColor : (factionId == 0 ? MinimapAllyUnitColor : MinimapEnemyUnitColor));
                        DrawMinimapDot(pixelX, pixelY, color, isSelected ? 2 : 1, _minimapPixels);
                        drewUnitPixels = true;
                    }
                }

                if (!isSelected)
                    continue;
                if (selectedArrowIcons >= MinimapMaxSelectedArrowIcons)
                    continue;

                float arrowZ = 0f;
                if (em.HasComponent<LocalTransform>(entity))
                {
                    quaternion rotation = em.GetComponentData<LocalTransform>(entity).Rotation;
                    Quaternion unityRotation = new(rotation.value.x, rotation.value.y, rotation.value.z, rotation.value.w);
                    arrowZ = -unityRotation.eulerAngles.y;
                }

                CreateRuntimeMinimapIcon(
                    isSelected
                        ? _minimapSelectedTemplate
                        : (isCivilian ? _minimapNeutralTemplate : (factionId == 0 ? _minimapAllyTemplate : _minimapEnemyTemplate)),
                    isSelected
                        ? _minimapSelectedSprite
                        : (isCivilian ? _minimapNeutralSprite : (factionId == 0 ? _minimapAllySprite : _minimapEnemySprite)),
                    normalized,
                    isSelected ? _minimapSelectedArrowSprite : null,
                    arrowZ);
                selectedArrowIcons++;
            }

            return drewUnitPixels;
        }

        private void UpdateFullscreenMapUnits(GridConfig grid)
        {
            if (!TryGetEntityManager(out EntityManager em))
                return;

            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<UnitGrid>(), ComponentType.ReadOnly<Faction>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                Vector3 worldPosition = em.HasComponent<LocalTransform>(entity)
                    ? (Vector3)em.GetComponentData<LocalTransform>(entity).Position
                    : default;
                if (!TryWorldToMinimapNormalized(grid, worldPosition, out Vector2 normalized))
                    continue;

                bool isCivilian = em.HasComponent<CivilianUnitTag>(entity);
                int factionId = em.GetComponentData<Faction>(entity).Id;
                bool isSelected = em.HasComponent<SelectedUnitTag>(entity);
                float arrowZ = 0f;
                if (isSelected && em.HasComponent<LocalTransform>(entity))
                {
                    quaternion rotation = em.GetComponentData<LocalTransform>(entity).Rotation;
                    Quaternion unityRotation = new(rotation.value.x, rotation.value.y, rotation.value.z, rotation.value.w);
                    arrowZ = -unityRotation.eulerAngles.y;
                }

                CreateRuntimeMapIcon(
                    _fullscreenMapMapRect,
                    GetOrCreateFullscreenMapRuntimeElementsRoot(),
                    isSelected
                        ? _minimapSelectedTemplate
                        : (isCivilian ? _minimapNeutralTemplate : (factionId == 0 ? _minimapAllyTemplate : _minimapEnemyTemplate)),
                    isSelected
                        ? _minimapSelectedSprite
                        : (isCivilian ? _minimapNeutralSprite : (factionId == 0 ? _minimapAllySprite : _minimapEnemySprite)),
                    normalized,
                    isSelected ? _minimapSelectedArrowSprite : null,
                    arrowZ);
            }
        }

        private bool UpdateCanvasMinimapBuildings()
        {
            if (!TryGetEntityManager(out EntityManager em))
                return false;

            bool dirty = false;
            foreach (RuntimeBuildingEntityLink link in RuntimeBuildingEntityLink.GetActiveLinks())
            {
                if (link == null || !TryWorldToViewportNormalized(link.transform.position, out Vector2 normalized))
                    continue;

                ResolveBuildingMinimapVisual(em, link.BuildingId, link.Entity, out _, out _, out Color colorOverride);
                if (!TryViewportNormalizedToMinimapPixel(normalized, out int pixelX, out int pixelY))
                    continue;

                DrawMinimapDot(pixelX, pixelY, (Color32)colorOverride, 2, _minimapPixels);
                dirty = true;
            }

            return dirty;
        }

        private void UpdateFullscreenMapBuildings(GridConfig grid)
        {
            if (!TryGetEntityManager(out EntityManager em))
                return;

            foreach (RuntimeBuildingEntityLink link in RuntimeBuildingEntityLink.GetActiveLinks())
            {
                if (link == null || !TryWorldToMinimapNormalized(grid, link.transform.position, out Vector2 normalized))
                    continue;

                ResolveBuildingMinimapVisual(em, link.BuildingId, link.Entity, out RectTransform template, out Sprite sprite, out Color colorOverride);

                CreateRuntimeMapIcon(
                    _fullscreenMapMapRect,
                    GetOrCreateFullscreenMapRuntimeElementsRoot(),
                    template,
                    sprite,
                    normalized,
                    null,
                    0f,
                    colorOverride);
            }
        }

        private void ResolveBuildingMinimapVisual(EntityManager em, int buildingId, Entity buildingEntity, out RectTransform template, out Sprite sprite, out Color colorOverride)
        {
            bool isWall = _buildingUiCommandSystem != null &&
                          _buildingUiCommandSystem.IsRuntimeBuildingWall(_buildingUiCommandContext, buildingId);
            if (_buildingUiCommandSystem != null &&
                _buildingUiCommandSystem.IsRuntimeBuildingCityGenerated(_buildingUiCommandContext, buildingId))
            {
                template = _minimapHomeNeutralTemplate;
                sprite = _minimapHomeNeutralSprite;
                colorOverride = _minimapNeutralBuildingColor;
                return;
            }

            if (_buildingUiCommandSystem != null &&
                _buildingUiCommandSystem.TryGetRuntimeBuildingOwnerFaction(_buildingUiCommandContext, buildingId, out byte ownerFactionId))
            {
                if (ownerFactionId == 0)
                {
                    template = isWall && _minimapWallTemplate != null ? _minimapWallTemplate : _minimapHomeTemplate;
                    sprite = isWall && _minimapWallSprite != null ? _minimapWallSprite : _minimapHomeSprite;
                    colorOverride = _minimapForcedIconColor;
                    return;
                }

                template = isWall && _minimapWallEnemyTemplate != null ? _minimapWallEnemyTemplate : _minimapHomeEnemyTemplate;
                sprite = isWall && _minimapWallEnemySprite != null ? _minimapWallEnemySprite : _minimapHomeEnemySprite;
                colorOverride = _minimapEnemyBuildingColor;
                return;
            }

            bool hasFaction = buildingEntity != Entity.Null &&
                              em.Exists(buildingEntity) &&
                              em.HasComponent<Faction>(buildingEntity);

            if (!hasFaction)
            {
                template = isWall && _minimapWallTemplate != null ? _minimapWallTemplate : _minimapHomeTemplate;
                sprite = isWall && _minimapWallSprite != null ? _minimapWallSprite : _minimapHomeSprite;
                colorOverride = _minimapForcedIconColor;
                return;
            }

            byte factionId = em.GetComponentData<Faction>(buildingEntity).Id;
            if (factionId == 0)
            {
                template = isWall && _minimapWallTemplate != null ? _minimapWallTemplate : _minimapHomeTemplate;
                sprite = isWall && _minimapWallSprite != null ? _minimapWallSprite : _minimapHomeSprite;
                colorOverride = _minimapForcedIconColor;
                return;
            }

            template = isWall && _minimapWallEnemyTemplate != null ? _minimapWallEnemyTemplate : _minimapHomeEnemyTemplate;
            sprite = isWall && _minimapWallEnemySprite != null ? _minimapWallEnemySprite : _minimapHomeEnemySprite;
            colorOverride = _minimapEnemyBuildingColor;
        }

        private bool UpdateCanvasMinimapObjectives(GridConfig grid)
        {
            if (!TryGetEntityManager(out EntityManager em) || _worldCamera == null)
                return false;

            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<Faction>(), ComponentType.ReadOnly<UnitTarget>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            _minimapObjectivePixelKeys.Clear();
            bool dirty = false;
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (em.GetComponentData<Faction>(entity).Id != 0)
                    continue;

                UnitTarget target = em.GetComponentData<UnitTarget>(entity);
                int key = (target.Cell.x * 73856093) ^ (target.Cell.y * 19349663);
                if (!_minimapObjectivePixelKeys.Add(key))
                    continue;

                Vector3 worldPosition = new(
                    grid.Origin.x + ((target.Cell.x + 0.5f) * grid.CellSize),
                    grid.Origin.y,
                    grid.Origin.z + ((target.Cell.y + 0.5f) * grid.CellSize));
                if (!TryWorldToViewportNormalized(worldPosition, out Vector2 normalized))
                    continue;
                if (!TryViewportNormalizedToMinimapPixel(normalized, out int pixelX, out int pixelY))
                    continue;

                DrawMinimapOutline(pixelX, pixelY, MinimapSelectedUnitColor, 3, _minimapPixels);
                dirty = true;
            }

            return dirty;
        }

        private void UpdateFullscreenMapObjectives(GridConfig grid)
        {
            if (!TryGetEntityManager(out EntityManager em))
                return;

            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<Faction>(), ComponentType.ReadOnly<UnitTarget>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            HashSet<int> targetKeys = new();
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (em.GetComponentData<Faction>(entity).Id != 0)
                    continue;

                UnitTarget target = em.GetComponentData<UnitTarget>(entity);
                int key = (target.Cell.x * 73856093) ^ (target.Cell.y * 19349663);
                if (!targetKeys.Add(key))
                    continue;

                Vector3 worldPosition = new(
                    grid.Origin.x + ((target.Cell.x + 0.5f) * grid.CellSize),
                    grid.Origin.y,
                    grid.Origin.z + ((target.Cell.y + 0.5f) * grid.CellSize));
                if (!TryWorldToMinimapNormalized(grid, worldPosition, out Vector2 normalized))
                    continue;

                CreateRuntimeMapIcon(_fullscreenMapMapRect, GetOrCreateFullscreenMapRuntimeElementsRoot(), _minimapObjectiveTemplate, _minimapObjectiveSprite, normalized);
            }
        }

        private void ClearRuntimeMinimapIcons()
        {
            RectTransform runtimeRoot = GetOrCreateMinimapRuntimeElementsRoot();
            if (runtimeRoot == null)
                return;

            for (int i = runtimeRoot.childCount - 1; i >= 0; i--)
                Destroy(runtimeRoot.GetChild(i).gameObject);
        }

        private void ClearFullscreenMapIcons()
        {
            RectTransform runtimeRoot = GetOrCreateFullscreenMapRuntimeElementsRoot();
            if (runtimeRoot == null)
                return;

            for (int i = runtimeRoot.childCount - 1; i >= 0; i--)
                Destroy(runtimeRoot.GetChild(i).gameObject);
        }

        private void CreateRuntimeMinimapIcon(RectTransform template, Sprite fallbackSprite, Vector2 normalized, Sprite arrowSprite = null, float arrowRotationZ = 0f, Color? colorOverride = null)
        {
            CreateRuntimeMapIcon(_minimapMapRect, GetOrCreateMinimapRuntimeElementsRoot(), template, fallbackSprite, normalized, arrowSprite, arrowRotationZ, colorOverride);
        }

        private void CreateRuntimeMapIcon(RectTransform mapRect, RectTransform runtimeRoot, RectTransform template, Sprite fallbackSprite, Vector2 normalized, Sprite arrowSprite = null, float arrowRotationZ = 0f, Color? colorOverride = null)
        {
            if (template == null || mapRect == null || runtimeRoot == null)
                return;

            GameObject iconObject = Instantiate(template.gameObject, runtimeRoot);
            iconObject.name = "Runtime_Minimap_Icon";
            iconObject.SetActive(true);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            ResetRuntimeMinimapGraphics(iconObject.transform);
            iconRect.anchorMin = new Vector2(0f, 0f);
            iconRect.anchorMax = new Vector2(0f, 0f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(normalized.x * mapRect.rect.width, normalized.y * mapRect.rect.height);

            Transform runtimePreferredTransform = FindPreferredMarkerTransform(iconObject.transform);
            Transform templatePreferredTransform = FindPreferredMarkerTransform(template);
            if (arrowSprite == null)
                KeepOnlyMarkerVisual(iconObject.transform, runtimePreferredTransform);

            Image iconImage = FindPreferredMarkerImage(runtimePreferredTransform != null ? runtimePreferredTransform : iconObject.transform);
            Image templateImage = FindPreferredMarkerImage(templatePreferredTransform != null ? templatePreferredTransform : template);
            if (iconImage != null && templateImage != null)
            {
                iconImage.sprite = templateImage.sprite != null ? templateImage.sprite : fallbackSprite;
                iconImage.color = colorOverride ?? _minimapForcedIconColor;
                iconImage.type = templateImage.type;
                iconImage.preserveAspect = templateImage.preserveAspect;
                iconImage.material = templateImage.material;
            }
            else if (iconImage != null && fallbackSprite != null)
            {
                iconImage.sprite = fallbackSprite;
                iconImage.color = colorOverride ?? _minimapForcedIconColor;
            }

            if (arrowSprite == null)
                return;

            Transform arrowTransform = FindDescendantByName(iconObject.transform, "ICN_Z");
            Image arrowImage = FindTemplateImage(arrowTransform);
            if (arrowTransform != null)
                arrowTransform.gameObject.SetActive(true);
            if (arrowImage != null)
                arrowImage.sprite = arrowSprite;
            if (arrowTransform is RectTransform arrowRect)
                arrowRect.localEulerAngles = new Vector3(0f, 0f, arrowRotationZ);
        }

        private static void KeepOnlyMarkerVisual(Transform root, Transform keep)
        {
            if (root == null || keep == null)
                return;

            DisableSiblingBranches(root, keep);
        }

        private static bool DisableSiblingBranches(Transform current, Transform keep)
        {
            if (current == null)
                return false;

            bool containsKeep = current == keep;
            for (int i = 0; i < current.childCount; i++)
            {
                Transform child = current.GetChild(i);
                bool childContainsKeep = DisableSiblingBranches(child, keep);
                containsKeep |= childContainsKeep;
                if (!childContainsKeep)
                    child.gameObject.SetActive(false);
            }

            return containsKeep;
        }

        private static void ResetRuntimeMinimapGraphics(Transform root)
        {
            if (root == null)
                return;

            for (int i = 0; i < root.childCount; i++)
                ResetRuntimeMinimapGraphics(root.GetChild(i));

            if (root.TryGetComponent(out Graphic graphic))
                graphic.raycastTarget = false;
        }

        private static bool TryGetEntityManager(out EntityManager em)
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                em = default;
                return false;
            }

            em = world.EntityManager;
            return true;
        }

        private static bool TryGetGridConfig(out GridConfig grid)
        {
            grid = default;
            if (!TryGetEntityManager(out EntityManager em))
                return false;

            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
            if (query.IsEmptyIgnoreFilter)
                return false;

            grid = query.GetSingleton<GridConfig>();
            return true;
        }

        private static bool TryGetGridRoadBuffer(out DynamicBuffer<GridRoad> roads)
        {
            roads = default;
            if (!TryGetEntityManager(out EntityManager em))
                return false;

            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>(), ComponentType.ReadOnly<GridRoad>());
            if (query.IsEmptyIgnoreFilter)
                return false;

            roads = em.GetBuffer<GridRoad>(query.GetSingletonEntity());
            return true;
        }

        private static void FillMinimap(Color32 color, Color32[] targetPixels)
        {
            for (int i = 0; i < targetPixels.Length; i++)
                targetPixels[i] = color;
        }

        private static void DrawMinimapGrid(Color32[] targetPixels)
        {
            int resolution = GetMinimapResolution(targetPixels);
            if (resolution <= 0)
                return;

            const int step = 24;
            for (int x = 0; x < resolution; x += step)
                for (int y = 0; y < resolution; y++)
                    SetMinimapPixel(x, y, MinimapGridColor, targetPixels);

            for (int y = 0; y < resolution; y += step)
                for (int x = 0; x < resolution; x++)
                    SetMinimapPixel(x, y, MinimapGridColor, targetPixels);
        }

        private static void DrawMinimapRoads(GridConfig grid, MinimapViewBounds viewBounds, Color32[] targetPixels)
        {
            if (!TryGetGridRoadBuffer(out DynamicBuffer<GridRoad> roads))
                return;

            int width = grid.Width;
            int height = grid.Height;
            int minX = Mathf.Clamp(Mathf.FloorToInt((viewBounds.MinX - grid.Origin.x) / grid.CellSize) - 1, 0, width - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt((viewBounds.MaxX - grid.Origin.x) / grid.CellSize) + 1, 0, width - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt((viewBounds.MinZ - grid.Origin.z) / grid.CellSize) - 1, 0, height - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt((viewBounds.MaxZ - grid.Origin.z) / grid.CellSize) + 1, 0, height - 1);
            for (int y = minY; y <= maxY; y++)
            {
                int rowOffset = y * width;
                for (int x = minX; x <= maxX; x++)
                {
                    if (roads[rowOffset + x].Value == 0)
                        continue;

                    if (!TryGridCellToMinimapPixel(grid, viewBounds, new int2(x, y), out int pixelX, out int pixelY))
                        continue;

                    SetMinimapPixel(pixelX, pixelY, MinimapRoadColor, targetPixels);
                }
            }
        }

        private static void DrawMinimapDot(int centerX, int centerY, Color32 color, int radius, Color32[] targetPixels)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if ((x * x) + (y * y) > radius * radius)
                        continue;

                    SetMinimapPixel(centerX + x, centerY + y, color, targetPixels);
                }
            }
        }

        private static void DrawMinimapOutline(int centerX, int centerY, Color32 color, int radius, Color32[] targetPixels)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    int distanceSq = (x * x) + (y * y);
                    if (distanceSq < (radius - 1) * (radius - 1) || distanceSq > radius * radius)
                        continue;

                    SetMinimapPixel(centerX + x, centerY + y, color, targetPixels);
                }
            }
        }

        private static void SetMinimapPixel(int x, int y, Color32 color, Color32[] targetPixels)
        {
            int resolution = GetMinimapResolution(targetPixels);
            if (resolution <= 0 || (uint)x >= resolution || (uint)y >= resolution)
                return;

            int index = x + (y * resolution);
            if ((uint)index < (uint)targetPixels.Length)
                targetPixels[index] = color;
        }

        private static int GetMinimapResolution(Color32[] targetPixels)
        {
            if (targetPixels == null || targetPixels.Length == 0)
                return 0;

            return Mathf.RoundToInt(Mathf.Sqrt(targetPixels.Length));
        }

        private static bool TryGridCellToMinimapPixel(GridConfig grid, MinimapViewBounds viewBounds, int2 cell, out int pixelX, out int pixelY)
        {
            Vector3 worldPosition = new(
                grid.Origin.x + ((cell.x + 0.5f) * grid.CellSize),
                grid.Origin.y,
                grid.Origin.z + ((cell.y + 0.5f) * grid.CellSize));
            if (!TryWorldToLocalMinimapNormalized(viewBounds, worldPosition, out Vector2 normalized))
            {
                pixelX = 0;
                pixelY = 0;
                return false;
            }

            float normalizedX = normalized.x;
            float normalizedY = normalized.y;
            pixelX = Mathf.Clamp(Mathf.RoundToInt(normalizedX * (MinimapResolution - 1)), 0, MinimapResolution - 1);
            pixelY = Mathf.Clamp(Mathf.RoundToInt(normalizedY * (MinimapResolution - 1)), 0, MinimapResolution - 1);
            return true;
        }

        private static bool TryGridCellToMinimapNormalized(GridConfig grid, MinimapViewBounds viewBounds, int2 cell, out Vector2 normalized)
        {
            Vector3 worldPosition = new(
                grid.Origin.x + ((cell.x + 0.5f) * grid.CellSize),
                grid.Origin.y,
                grid.Origin.z + ((cell.y + 0.5f) * grid.CellSize));
            return TryWorldToLocalMinimapNormalized(viewBounds, worldPosition, out normalized);
        }

        private static bool TryWorldToMinimapPixel(GridConfig grid, MinimapViewBounds viewBounds, Vector3 worldPosition, out int pixelX, out int pixelY)
        {
            if (!TryWorldToLocalMinimapNormalized(viewBounds, worldPosition, out Vector2 normalized))
            {
                pixelX = 0;
                pixelY = 0;
                return false;
            }

            pixelX = Mathf.Clamp(Mathf.RoundToInt(normalized.x * (MinimapResolution - 1)), 0, MinimapResolution - 1);
            pixelY = Mathf.Clamp(Mathf.RoundToInt(normalized.y * (MinimapResolution - 1)), 0, MinimapResolution - 1);
            return true;
        }

        private static bool TryWorldToLocalMinimapNormalized(MinimapViewBounds viewBounds, Vector3 worldPosition, out Vector2 normalized)
        {
            float normalizedX = (worldPosition.x - viewBounds.MinX) / viewBounds.Width;
            float normalizedY = (worldPosition.z - viewBounds.MinZ) / viewBounds.Height;
            normalized = new Vector2(normalizedX, normalizedY);
            if (float.IsNaN(normalizedX) || float.IsNaN(normalizedY))
                return false;

            return normalizedX >= 0f && normalizedX <= 1f && normalizedY >= 0f && normalizedY <= 1f;
        }

        private bool TryWorldToViewportNormalized(Vector3 worldPosition, out Vector2 normalized)
        {
            normalized = default;
            if (_worldCamera == null)
                return false;

            Vector3 viewport = _worldCamera.WorldToViewportPoint(worldPosition);
            if (viewport.z <= 0f)
                return false;

            normalized = new Vector2(viewport.x, viewport.y);
            return viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
        }

        private static bool TryViewportNormalizedToMinimapPixel(Vector2 normalized, out int pixelX, out int pixelY)
        {
            if (normalized.x < 0f || normalized.x > 1f || normalized.y < 0f || normalized.y > 1f)
            {
                pixelX = 0;
                pixelY = 0;
                return false;
            }

            pixelX = Mathf.Clamp(Mathf.RoundToInt(normalized.x * (MinimapResolution - 1)), 0, MinimapResolution - 1);
            pixelY = Mathf.Clamp(Mathf.RoundToInt(normalized.y * (MinimapResolution - 1)), 0, MinimapResolution - 1);
            return true;
        }

        private static bool TryWorldToMinimapNormalized(GridConfig grid, Vector3 worldPosition, out Vector2 normalized)
        {
            float width = Mathf.Max(0.001f, grid.Width * grid.CellSize);
            float height = Mathf.Max(0.001f, grid.Height * grid.CellSize);
            float normalizedX = (worldPosition.x - grid.Origin.x) / width;
            float normalizedY = (worldPosition.z - grid.Origin.z) / height;
            normalized = new Vector2(normalizedX, normalizedY);
            return !float.IsNaN(normalizedX) && !float.IsNaN(normalizedY);
        }

        private static bool TryMinimapNormalizedToWorld(GridConfig grid, Vector2 normalized, out Vector3 worldPosition)
        {
            float width = Mathf.Max(0.001f, grid.Width * grid.CellSize);
            float height = Mathf.Max(0.001f, grid.Height * grid.CellSize);
            worldPosition = new Vector3(
                grid.Origin.x + (Mathf.Clamp01(normalized.x) * width),
                grid.Origin.y,
                grid.Origin.z + (Mathf.Clamp01(normalized.y) * height));
            return true;
        }

        private static Vector3 RaycastViewport(Camera camera, Plane groundPlane, Vector3 viewportPoint)
        {
            Ray ray = camera.ViewportPointToRay(viewportPoint);
            return groundPlane.Raycast(ray, out float distance) ? ray.GetPoint(distance) : Vector3.zero;
        }

        private bool TryGetMinimapViewBounds(GridConfig grid, out MinimapViewBounds viewBounds)
        {
            Camera worldCamera = _worldCamera;
            if (worldCamera == null)
            {
                viewBounds = default;
                return false;
            }

            Plane groundPlane = new(Vector3.up, new Vector3(0f, grid.Origin.y, 0f));
            Vector3[] viewportWorldCorners =
            {
                RaycastViewport(worldCamera, groundPlane, new Vector3(0f, 0f, 0f)),
                RaycastViewport(worldCamera, groundPlane, new Vector3(1f, 0f, 0f)),
                RaycastViewport(worldCamera, groundPlane, new Vector3(1f, 1f, 0f)),
                RaycastViewport(worldCamera, groundPlane, new Vector3(0f, 1f, 0f))
            };
            Vector3 center = RaycastViewport(worldCamera, groundPlane, new Vector3(0.5f, 0.5f, 0f));

            float halfWidth = 0f;
            float halfHeight = 0f;
            for (int i = 0; i < viewportWorldCorners.Length; i++)
            {
                Vector3 corner = viewportWorldCorners[i];
                halfWidth = Mathf.Max(halfWidth, Mathf.Abs(corner.x - center.x));
                halfHeight = Mathf.Max(halfHeight, Mathf.Abs(corner.z - center.z));
            }

            if (float.IsNaN(center.x) || float.IsNaN(center.z))
            {
                viewBounds = default;
                return false;
            }

            halfWidth = Mathf.Max(20f, halfWidth * 1.1f);
            halfHeight = Mathf.Max(20f, halfHeight * 1.1f);
            viewBounds = new MinimapViewBounds(center.x - halfWidth, center.x + halfWidth, center.z - halfHeight, center.z + halfHeight);
            return true;
        }

        private static void DrawFullscreenMapRoads(GridConfig grid, Color32[] targetPixels)
        {
            if (!TryGetGridRoadBuffer(out DynamicBuffer<GridRoad> roads))
                return;

            int resolution = GetMinimapResolution(targetPixels);
            if (resolution <= 0)
                return;

            int width = grid.Width;
            int height = grid.Height;
            for (int y = 0; y < height; y++)
            {
                int rowOffset = y * width;
                for (int x = 0; x < width; x++)
                {
                    if (roads[rowOffset + x].Value == 0)
                        continue;

                    int pixelX = Mathf.Clamp(Mathf.RoundToInt(((x + 0.5f) / Mathf.Max(1, width)) * (resolution - 1)), 0, resolution - 1);
                    int pixelY = Mathf.Clamp(Mathf.RoundToInt(((y + 0.5f) / Mathf.Max(1, height)) * (resolution - 1)), 0, resolution - 1);
                    SetMinimapPixel(pixelX, pixelY, MinimapRoadColor, targetPixels);
                }
            }
        }

        private RectTransform CreateFullscreenMapCameraRect()
        {
            if (_fullscreenMapMapRect == null)
                return null;

            GameObject rectObject = new GameObject("Runtime_CameraRect", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            rectObject.transform.SetParent(_fullscreenMapMapRect, false);
            RectTransform rect = rectObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            Image image = rectObject.GetComponent<Image>();
            image.color = new Color(1f, 0.9f, 0.2f, 0.08f);
            image.raycastTarget = true;
            Outline outline = rectObject.GetComponent<Outline>();
            outline.effectColor = new Color(1f, 0.9f, 0.2f, 1f);
            outline.effectDistance = new Vector2(2f, -2f);
            return rect;
        }

        private void UpdateFullscreenMapCameraRect(GridConfig grid)
        {
            if (_fullscreenMapCameraRect == null || _fullscreenMapMapRect == null || _worldCamera == null)
                return;

            Plane groundPlane = new(Vector3.up, new Vector3(0f, grid.Origin.y, 0f));
            Vector3[] viewportWorldCorners =
            {
                RaycastViewport(_worldCamera, groundPlane, new Vector3(0f, 0f, 0f)),
                RaycastViewport(_worldCamera, groundPlane, new Vector3(1f, 0f, 0f)),
                RaycastViewport(_worldCamera, groundPlane, new Vector3(1f, 1f, 0f)),
                RaycastViewport(_worldCamera, groundPlane, new Vector3(0f, 1f, 0f))
            };

            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;
            bool found = false;
            for (int i = 0; i < viewportWorldCorners.Length; i++)
            {
                if (!TryWorldToMinimapNormalized(grid, viewportWorldCorners[i], out Vector2 normalized))
                    continue;

                found = true;
                minX = Mathf.Min(minX, normalized.x);
                minY = Mathf.Min(minY, normalized.y);
                maxX = Mathf.Max(maxX, normalized.x);
                maxY = Mathf.Max(maxY, normalized.y);
            }

            if (!found)
                return;

            Rect rect = _fullscreenMapMapRect.rect;
            float left = rect.width * Mathf.Clamp01(minX);
            float top = rect.height * Mathf.Clamp01(1f - maxY);
            float width = Mathf.Max(6f, rect.width * Mathf.Clamp01(maxX - minX));
            float height = Mathf.Max(6f, rect.height * Mathf.Clamp01(maxY - minY));
            _fullscreenMapCameraRect.anchoredPosition = new Vector2(left, -top);
            _fullscreenMapCameraRect.sizeDelta = new Vector2(width, height);
        }

        private void UpdateFullscreenMapCameraDrag(GridConfig grid)
        {
            if (_fullscreenMapMapRect == null || _fullscreenMapCameraRect == null || _selectionSystem == null ||
                !global::GamePointerInput.TryGetPrimaryPointer(out global::GamePointerState pointer))
                return;

            Vector2 screenPosition = pointer.Position;
            bool isPressed = pointer.IsPressed;

            if (!_fullscreenMapCameraRectDragging)
            {
                if (!isPressed || !RectTransformUtility.RectangleContainsScreenPoint(_fullscreenMapCameraRect, screenPosition, null))
                    return;

                RectTransformUtility.ScreenPointToLocalPointInRectangle(_fullscreenMapCameraRect, screenPosition, null, out Vector2 rectLocalPoint);
                _fullscreenMapCameraRectDragOffset = new Vector2(
                    rectLocalPoint.x - _fullscreenMapCameraRect.rect.xMin,
                    _fullscreenMapCameraRect.rect.yMax - rectLocalPoint.y);
                _fullscreenMapCameraRectDragging = true;
                _runtimeGameplayStateSystem.SuppressNextWorldClick = true;
                return;
            }

            if (!isPressed)
            {
                _fullscreenMapCameraRectDragging = false;
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_fullscreenMapMapRect, screenPosition, null, out Vector2 localPoint))
                return;

            Rect rect = _fullscreenMapMapRect.rect;
            float localX = localPoint.x - rect.xMin;
            float localY = rect.yMax - localPoint.y;
            float rectWidth = Mathf.Max(6f, _fullscreenMapCameraRect.rect.width);
            float rectHeight = Mathf.Max(6f, _fullscreenMapCameraRect.rect.height);
            float left = Mathf.Clamp(localX - _fullscreenMapCameraRectDragOffset.x, 0f, Mathf.Max(0f, rect.width - rectWidth));
            float top = Mathf.Clamp(localY - _fullscreenMapCameraRectDragOffset.y, 0f, Mathf.Max(0f, rect.height - rectHeight));
            float normalizedX = (left + (rectWidth * 0.5f)) / rect.width;
            float normalizedY = 1f - ((top + (rectHeight * 0.5f)) / rect.height);
            if (!TryMinimapNormalizedToWorld(grid, new Vector2(normalizedX, normalizedY), out Vector3 focusWorldPosition))
                return;

            _selectionSystem.MoveCameraGroundCenterTo(focusWorldPosition);
            UpdateFullscreenMapCameraRect(grid);
        }

        private static Animator FindAnimatorByName(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName))
                return null;

            Transform child = root.Find(childName);
            return child != null ? child.GetComponent<Animator>() : null;
        }

        private static GameObject FindChildObjectByName(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName))
                return null;

            Transform child = FindDescendantByName(root, childName);
            return child != null ? child.gameObject : null;
        }

        private static Button ResolveExclusiveButton(Transform root, string childName, UnityAction action)
        {
            GameObject buttonObject = FindChildObjectByName(root, childName);
            Button button = buttonObject != null ? buttonObject.GetComponent<Button>() : null;
            if (button == null)
                return null;

            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(action);
            return button;
        }

        private static Transform FindDescendantByName(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName))
                return null;

            if (root.name == childName)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindDescendantByName(root.GetChild(i), childName);
                if (result != null)
                    return result;
            }

            return null;
        }

        private static TMP_Text FindTextByName(Transform root, string childName)
        {
            GameObject child = FindChildObjectByName(root, childName);
            return child != null ? child.GetComponent<TMP_Text>() : null;
        }

        private static Image FindImageByName(Transform root, string childName)
        {
            GameObject child = FindChildObjectByName(root, childName);
            return child != null ? child.GetComponent<Image>() : null;
        }

        private static void SetGameSubPanelActive(Animator panel, bool active)
        {
            if (panel == null)
                return;

            panel.gameObject.SetActive(active);
        }

        private void SyncGameSelectionPanels()
        {
            bool hasSelectedBuilding = _runtimeGameplayStateSystem.PlayRequested &&
                                       _buildingUiCommandSystem != null &&
                                       _buildingUiCommandSystem.HasActiveBuilding(_buildingUiCommandContext);

            int selectedUnitCount = 0;
            bool hasFocusedUnit = false;
            bool focusedUnitIsVehicle = false;
            if (!hasSelectedBuilding && _selectionSystem != null)
            {
                hasFocusedUnit = _selectionSystem.HasFocusedUnit;
                focusedUnitIsVehicle = _selectionSystem.FocusedUnitIsVehicle;

                if (_selectionSystem.HasAnySelectedUnits)
                {
                    _selectedUnits.Clear();
                    _selectionSystem.GetSelectedUnitEntities(_selectedUnits);
                    selectedUnitCount = _selectedUnits.Count;
                }
            }

            bool hasAnySelection = hasSelectedBuilding || hasFocusedUnit || selectedUnitCount > 0;
            GameMenuType targetGameMenuType = hasAnySelection
                ? GameMenuType.Select
                : GameMenuType.Free;

            UpdateSelectionToggleButtons(hasAnySelection);

            ShowGameMenuType(targetGameMenuType);

            if (targetGameMenuType == GameMenuType.Select)
            {
                selectionType = hasSelectedBuilding || selectedUnitCount <= 1
                    ? SelectionType.Single
                    : SelectionType.Multi;

                if (hasSelectedBuilding)
                {
                    singleSelectionType = SingleSelectionType.Building;
                }
                else
                {
                    singleSelectionType = ResolveFocusedUnitSelectionType(focusedUnitIsVehicle);
                }

                SyncSelectionChildren();
            }
        }

        private void SyncSelectionChildren()
        {
            bool showSingle = gameMenuType == GameMenuType.Select && selectionType == SelectionType.Single;
            bool showMulti = gameMenuType == GameMenuType.Select && selectionType == SelectionType.Multi;

            if (selectPanelSingle != null)
                selectPanelSingle.SetActive(showSingle);
            if (selectPanelMulti != null)
                selectPanelMulti.SetActive(showMulti);

            UpdateSingleSelectionUnitTypePanels(showSingle);

            if (showSingle)
            {
                UpdateSingleSelectionPortrait();
                UpdateSingleSelectionHealthSlider();
                UpdateSingleSelectionStatusIcons();
            }
        }

        private void UpdateSingleSelectionHealthSlider()
        {
            ResolveSingleSelectionHealthSlider();
            if (_singleSelectionHealthSlider == null)
                return;

            int current = 0;
            int max = 0;
            bool hasHealth;
            if (singleSelectionType == SingleSelectionType.Building)
            {
                hasHealth = _buildingUiCommandSystem != null &&
                            _buildingUiCommandSystem.TryGetSelectedBuildingHealth(_buildingUiCommandContext, out current, out max) &&
                            max > 0;
            }
            else
            {
                hasHealth = _selectionSystem != null &&
                            _selectionSystem.TryGetFocusedUnitHealth(out current, out max) &&
                            max > 0;
            }

            _singleSelectionHealthSlider.minValue = 0f;
            _singleSelectionHealthSlider.maxValue = 1f;
            _singleSelectionHealthSlider.value = hasHealth ? Mathf.Clamp01((float)current / max) : 0f;
        }

        private void UpdateSingleSelectionStatusIcons()
        {
            ResolveSingleSelectionStatusIcons();

            RTSSelectionSystem.FocusedUnitUiStatus status = RTSSelectionSystem.FocusedUnitUiStatus.Idle;
            bool showAny = singleSelectionType != SingleSelectionType.Building && _selectionSystem != null;
            if (showAny)
                status = _selectionSystem.GetFocusedUnitUiStatus();

            SetActiveIfNotNull(_singleStatusIdleIcon, showAny && status == RTSSelectionSystem.FocusedUnitUiStatus.Idle);
            SetActiveIfNotNull(_singleStatusMovingIcon, showAny && status == RTSSelectionSystem.FocusedUnitUiStatus.Moving);
            SetActiveIfNotNull(_singleStatusEngagedIcon, showAny && status == RTSSelectionSystem.FocusedUnitUiStatus.Engaged);
            SetActiveIfNotNull(_singleStatusReturningIcon, showAny && status == RTSSelectionSystem.FocusedUnitUiStatus.ReturningToBase);
        }

        private SingleSelectionType ResolveFocusedUnitSelectionType(bool focusedUnitIsVehicle)
        {
            if (_selectionSystem != null &&
                _selectionSystem.TryGetFocusedUnitEntityForUi(out Entity focusedUnit) &&
                _buildingUiCommandSystem != null &&
                _buildingUiCommandSystem.TryResolveLiveUnitPreviewPrefab(_buildingUiCommandContext, focusedUnit, out GameObject prefab) &&
                TryResolveUnitPrefabSelectionType(prefab, out SingleSelectionType prefabSelectionType))
            {
                return prefabSelectionType;
            }

            return focusedUnitIsVehicle ? SingleSelectionType.Vehicle : SingleSelectionType.Soldier;
        }

        private static bool TryResolveUnitPrefabSelectionType(GameObject prefab, out SingleSelectionType selectionType)
        {
            selectionType = SingleSelectionType.Soldier;
            if (prefab == null)
                return false;

            string prefabName = prefab.name;
            if (!string.IsNullOrWhiteSpace(prefabName))
            {
                if (prefabName.IndexOf("_Chr_", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    prefabName.StartsWith("Unit_Chr", StringComparison.OrdinalIgnoreCase))
                {
                    selectionType = SingleSelectionType.Soldier;
                    return true;
                }

                if (prefabName.IndexOf("_Veh_", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    prefabName.StartsWith("Unit_Veh", StringComparison.OrdinalIgnoreCase) ||
                    prefabName.IndexOf("Vehicle", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    selectionType = SingleSelectionType.Vehicle;
                    return true;
                }
            }

            UnitGridAuthoring authoring = prefab.GetComponent<UnitGridAuthoring>();
            if (authoring != null)
            {
                Vector2Int footprint = authoring.GetConfiguredFootprintCells();
                if (footprint.x > 1 || footprint.y > 1)
                {
                    selectionType = SingleSelectionType.Vehicle;
                    return true;
                }
            }

            return false;
        }

        private void UpdateSingleSelectionUnitTypePanels(bool showSingle)
        {
            ResolveSingleSelectionUnitTypePanels();

            bool showUnitPanel = showSingle && singleSelectionType != SingleSelectionType.Building;
            bool showSoldier = showUnitPanel && singleSelectionType == SingleSelectionType.Soldier;
            bool showVehicle = showUnitPanel && singleSelectionType == SingleSelectionType.Vehicle;

            if (_singleSelectionUnitPanel != null)
                _singleSelectionUnitPanel.SetActive(showUnitPanel);
            if (_singleSelectionSoldierPanel != null)
                _singleSelectionSoldierPanel.SetActive(showSoldier);
            if (_singleSelectionVehiclePanel != null)
                _singleSelectionVehiclePanel.SetActive(showVehicle);

            UpdateSingleSelectionSoldierWeaponPanel(showSoldier);
            UpdateSingleSelectionExitButton(showUnitPanel);
            UpdateSingleSelectionAttackButton(showUnitPanel);
            UpdateSingleSelectionVehicleOnboardPanel(showVehicle);
        }

        private void UpdateSingleSelectionSoldierWeaponPanel(bool showSoldier)
        {
            ResolveSingleSelectionUnitTypePanels();

            Sprite weaponSprite = null;
            if (showSoldier && TryGetFocusedUnitPrefab(out GameObject prefab) &&
                prefab != null &&
                prefab.TryGetComponent(out UnitGridAuthoring authoring))
            {
                weaponSprite = authoring.WeaponSprite;
            }

            bool showWeapon = showSoldier && weaponSprite != null;
            if (_singleSelectionSoldierWeaponPanel != null)
                _singleSelectionSoldierWeaponPanel.SetActive(showWeapon);
            if (_singleSelectionSoldierWeaponImage != null)
            {
                _singleSelectionSoldierWeaponImage.sprite = weaponSprite;
                _singleSelectionSoldierWeaponImage.enabled = showWeapon;
            }
        }

        private void UpdateSingleSelectionExitButton(bool showUnitPanel)
        {
            ResolveSingleSelectionUnitTypePanels();

            bool showExit = showUnitPanel &&
                            _selectionSystem != null &&
                            _selectionSystem.CanDisembarkFocusedTransport;

            if (_singleSelectionExitButton != null)
                _singleSelectionExitButton.SetActive(showExit);
        }

        private void ButtonExitTransportClicked()
        {
            SuppressNextWorldClick();
            _selectionSystem?.DisembarkFocusedTransport();
            SyncRuntimeState();
        }

        private void ButtonAttackClicked()
        {
            SuppressNextWorldClick();
            if (_selectionSystem != null && !_selectionSystem.IssueFocusedMissileLauncherRadarAttack())
                _selectionSystem.ArmFocusedAttackTargetMode();
            SyncRuntimeState();
        }

        private void UpdateSingleSelectionVehicleOnboardPanel(bool showVehicle)
        {
            ResolveSingleSelectionVehicleOnboardPanel();

            _focusedTransportPassengers.Clear();
            if (showVehicle && _selectionSystem != null)
                _selectionSystem.GetFocusedTransportPassengers(_focusedTransportPassengers);

            bool showOnboard = showVehicle && _focusedTransportPassengers.Count > 0;
            if (_singleSelectionVehicleOnboardPanel != null)
                _singleSelectionVehicleOnboardPanel.SetActive(showOnboard);
            if (!showOnboard)
            {
                SetOnboardPassengerItemCount(0);
                return;
            }

            SetOnboardPassengerItemCount(_focusedTransportPassengers.Count);
            for (int i = 0; i < _focusedTransportPassengers.Count; i++)
            {
                OnboardPassengerItemView item = _singleSelectionVehicleOnboardItems[i];
                RTSSelectionSystem.TransportPassengerUiInfo passenger = _focusedTransportPassengers[i];
                if (item.Root != null)
                    item.Root.SetActive(true);
                if (item.NameLabel != null)
                    item.NameLabel.text = string.IsNullOrWhiteSpace(passenger.DisplayName) ? "Soldier" : passenger.DisplayName;
                if (item.HealthSlider != null)
                {
                    item.HealthSlider.minValue = 0f;
                    item.HealthSlider.maxValue = 1f;
                    item.HealthSlider.value = passenger.HealthMax > 0 ? Mathf.Clamp01((float)passenger.HealthCurrent / passenger.HealthMax) : 0f;
                }
                if (item.Portrait != null)
                {
                    Sprite sprite = null;
                    if (_buildingUiCommandSystem != null &&
                        _buildingUiCommandSystem.TryResolveLiveUnitPreviewPrefab(_buildingUiCommandContext, passenger.Entity, out GameObject prefab) &&
                        prefab != null)
                    {
                        sprite = GetCampPreviewSprite(prefab);
                    }

                    item.Portrait.sprite = sprite;
                    item.Portrait.enabled = sprite != null;
                }
            }
        }

        private void ResolveSingleSelectionVehicleOnboardPanel()
        {
            if (_singleSelectionVehicleOnboardPanel != null &&
                _singleSelectionVehicleOnboardLayout != null &&
                _singleSelectionVehicleOnboardTemplate != null)
            {
                return;
            }

            ResolveSingleSelectionUnitTypePanels();
            if (_singleSelectionVehiclePanel == null)
                return;

            if (_singleSelectionVehicleOnboardPanel == null)
                _singleSelectionVehicleOnboardPanel = FindChildObjectByName(_singleSelectionVehiclePanel.transform, "Panel_Onboard");
            if (_singleSelectionVehicleOnboardPanel == null)
                return;

            if (_singleSelectionVehicleOnboardLayout == null)
            {
                GameObject layoutObject = FindChildObjectByName(_singleSelectionVehicleOnboardPanel.transform, "Vertical_Group");
                _singleSelectionVehicleOnboardLayout = layoutObject != null ? layoutObject.transform : _singleSelectionVehicleOnboardPanel.transform;
            }
            if (_singleSelectionVehicleOnboardTemplate == null && _singleSelectionVehicleOnboardLayout != null)
            {
                Transform template = _singleSelectionVehicleOnboardLayout.Find("Item_00");
                _singleSelectionVehicleOnboardTemplate = template != null ? template.gameObject : FindChildObjectByName(_singleSelectionVehicleOnboardLayout, "Item_00");
                if (_singleSelectionVehicleOnboardTemplate != null && _singleSelectionVehicleOnboardItems.Count == 0)
                    _singleSelectionVehicleOnboardItems.Add(CreateOnboardPassengerItemView(_singleSelectionVehicleOnboardTemplate));
            }
        }

        private void SetOnboardPassengerItemCount(int count)
        {
            ResolveSingleSelectionVehicleOnboardPanel();
            if (_singleSelectionVehicleOnboardTemplate == null || _singleSelectionVehicleOnboardLayout == null)
                return;

            while (_singleSelectionVehicleOnboardItems.Count < count)
            {
                GameObject instance = Instantiate(_singleSelectionVehicleOnboardTemplate, _singleSelectionVehicleOnboardLayout);
                instance.name = $"Item_{_singleSelectionVehicleOnboardItems.Count:00}";
                _singleSelectionVehicleOnboardItems.Add(CreateOnboardPassengerItemView(instance));
            }

            for (int i = 0; i < _singleSelectionVehicleOnboardItems.Count; i++)
            {
                OnboardPassengerItemView item = _singleSelectionVehicleOnboardItems[i];
                if (item.Root != null)
                    item.Root.SetActive(i < count);
            }
        }

        private static OnboardPassengerItemView CreateOnboardPassengerItemView(GameObject root)
        {
            Transform rootTransform = root != null ? root.transform : null;
            GameObject portraitObject = FindChildObjectByName(rootTransform, "SPR_Portrait");
            GameObject sliderObject = FindChildObjectByName(rootTransform, "Slider_Horizontal");
            GameObject nameObject = FindChildObjectByName(rootTransform, "Label_Name");
            return new OnboardPassengerItemView
            {
                Root = root,
                Portrait = portraitObject != null ? portraitObject.GetComponent<Image>() : null,
                HealthSlider = sliderObject != null ? sliderObject.GetComponent<Slider>() : null,
                NameLabel = nameObject != null ? nameObject.GetComponent<TMP_Text>() : null
            };
        }

        private void UpdateSingleSelectionAttackButton(bool showUnitPanel)
        {
            ResolveSingleSelectionUnitTypePanels();

            bool showAttack = showUnitPanel &&
                              _selectionSystem != null &&
                              _selectionSystem.FocusedUnitCanAttack;

            if (_singleSelectionAttackButton != null)
                _singleSelectionAttackButton.SetActive(showAttack);
        }

        private bool TryGetFocusedUnitPrefab(out GameObject prefab)
        {
            prefab = null;
            return _selectionSystem != null &&
                   _selectionSystem.TryGetFocusedUnitEntityForUi(out Entity focusedUnit) &&
                   _buildingUiCommandSystem != null &&
                   _buildingUiCommandSystem.TryResolveLiveUnitPreviewPrefab(_buildingUiCommandContext, focusedUnit, out prefab) &&
                   prefab != null;
        }

        private static bool IsExitSupportedVehiclePrefab(GameObject prefab)
        {
            if (prefab == null)
                return false;

            string prefabName = prefab.name;
            return string.Equals(prefabName, "Unit_Veh_APC_Fast", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(prefabName, "Unit_Veh_APC_Heavy", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(prefabName, "Unit_Veh_APC_Slow", StringComparison.OrdinalIgnoreCase);
        }

        private void UpdateSingleSelectionPortrait()
        {
            ResolveSingleSelectionPortrait();
            if (_singleSelectionPortrait == null)
                return;

            GameObject prefab = null;
            string displayName = string.Empty;
            if (singleSelectionType == SingleSelectionType.Building)
            {
                _buildingUiCommandSystem?.TryGetSelectedBuildingPreviewPrefab(_buildingUiCommandContext, out prefab);
                displayName = _buildingUiCommandSystem != null ? _buildingUiCommandSystem.SelectedBuildingDisplayName(_buildingUiCommandContext) : string.Empty;
            }
            else if (_selectionSystem != null &&
                     _selectionSystem.TryGetFocusedUnitEntityForUi(out Entity focusedUnit) &&
                     _buildingUiCommandSystem != null)
            {
                _buildingUiCommandSystem.TryResolveLiveUnitPreviewPrefab(_buildingUiCommandContext, focusedUnit, out prefab);
                displayName = _selectionSystem.FocusedUnitLabel;
            }

            Sprite sprite = prefab != null ? GetCampPreviewSprite(prefab) : null;
            _singleSelectionPortrait.sprite = sprite;
            _singleSelectionPortrait.enabled = sprite != null;

            if (_singleSelectionName != null)
                _singleSelectionName.text = string.IsNullOrWhiteSpace(displayName) ? "-" : displayName;
        }

        private void ResolveSingleSelectionPortrait()
        {
            if ((_singleSelectionPortrait != null && _singleSelectionName != null) || selectPanelSingle == null)
                return;

            Transform health = selectPanelSingle.transform.Find("Panel_Health");
            Transform portrait = health != null ? health.Find("Portrait") : null;
            Transform content = health != null ? health.Find("Content") : null;
            if (_singleSelectionPortrait == null)
                _singleSelectionPortrait = FindImageByName(portrait, "SPR_Portrait");
            if (_singleSelectionName == null)
                _singleSelectionName = FindTextByName(content, "Label_Name");
        }

        private void ResolveSingleSelectionHealthSlider()
        {
            if (_singleSelectionHealthSlider != null || selectPanelSingle == null)
                return;

            Transform health = selectPanelSingle.transform.Find("Panel_Health");
            Transform content = health != null ? health.Find("Content") : null;
            Transform barHp = content != null ? content.Find("Bar_HP") : null;
            Transform slider = barHp != null ? barHp.Find("Slider") : null;
            if (slider == null)
            {
                GameObject sliderObject = FindChildObjectByName(barHp != null ? barHp : content, "Slider");
                slider = sliderObject != null ? sliderObject.transform : null;
            }

            _singleSelectionHealthSlider = slider != null ? slider.GetComponent<Slider>() : null;
        }

        private void ResolveSingleSelectionStatusIcons()
        {
            if ((_singleStatusIdleIcon != null &&
                 _singleStatusMovingIcon != null &&
                 _singleStatusEngagedIcon != null &&
                 _singleStatusReturningIcon != null) ||
                selectPanelSingle == null)
            {
                return;
            }

            Transform health = selectPanelSingle.transform.Find("Panel_Health");
            Transform content = health != null ? health.Find("Content") : null;
            Transform statusRoot = content != null ? content.Find("Status") : null;
            if (statusRoot == null)
                return;

            _singleStatusIdleIcon ??= FindChildObjectByName(statusRoot, "Icon_Status_00");
            _singleStatusMovingIcon ??= FindChildObjectByName(statusRoot, "Icon_Status_01");
            _singleStatusEngagedIcon ??= FindChildObjectByName(statusRoot, "Icon_Status_02");
            _singleStatusReturningIcon ??= FindChildObjectByName(statusRoot, "Icon_Status_03");
        }

        private static void SetActiveIfNotNull(GameObject target, bool active)
        {
            if (target != null)
                target.SetActive(active);
        }

        private void ResolveSingleSelectionUnitTypePanels()
        {
            if (_singleSelectionSoldierPanel != null &&
                _singleSelectionVehiclePanel != null &&
                _singleSelectionExitButton != null &&
                _singleSelectionAttackButton != null &&
                _singleSelectionSoldierWeaponPanel != null &&
                _singleSelectionSoldierWeaponImage != null)
            {
                return;
            }

            if (selectPanelSingle == null)
                return;

            Transform panelUnit = selectPanelSingle.transform.Find("Panel_Unit");
            if (panelUnit == null)
                return;

            if (_singleSelectionUnitPanel == null)
                _singleSelectionUnitPanel = panelUnit.gameObject;
            if (_singleSelectionSoldierPanel == null)
                _singleSelectionSoldierPanel = FindChildObjectByName(panelUnit, "Panel_Soldier");
            if (_singleSelectionVehiclePanel == null)
                _singleSelectionVehiclePanel = FindChildObjectByName(panelUnit, "Panel_Vehicle");
            if (_singleSelectionExitButton == null)
                _singleSelectionExitButton = FindChildObjectByName(panelUnit, "Button_Exit");
            if (_singleSelectionExitButtonComponent == null && _singleSelectionExitButton != null)
            {
                _singleSelectionExitButtonComponent = _singleSelectionExitButton.GetComponent<Button>();
                if (_singleSelectionExitButtonComponent != null)
                {
                    _singleSelectionExitButtonComponent.onClick.RemoveListener(ButtonExitTransportClicked);
                    _singleSelectionExitButtonComponent.onClick.AddListener(ButtonExitTransportClicked);
                }
            }
            if (_singleSelectionAttackButton == null)
            {
                Transform buttons = panelUnit.Find("Buttons");
                _singleSelectionAttackButton = FindChildObjectByName(buttons != null ? buttons : panelUnit, "Button_Attack");
            }
            if (_singleSelectionAttackButtonComponent == null && _singleSelectionAttackButton != null)
            {
                _singleSelectionAttackButtonComponent = _singleSelectionAttackButton.GetComponent<Button>();
                if (_singleSelectionAttackButtonComponent != null)
                {
                    _singleSelectionAttackButtonComponent.onClick.RemoveListener(ButtonAttackClicked);
                    _singleSelectionAttackButtonComponent.onClick.AddListener(ButtonAttackClicked);
                }
            }
            if (_singleSelectionSoldierWeaponPanel == null && _singleSelectionSoldierPanel != null)
            {
                Transform weapon = _singleSelectionSoldierPanel.transform.Find("Panel_Weapon");
                _singleSelectionSoldierWeaponPanel = weapon != null ? weapon.gameObject : FindChildObjectByName(_singleSelectionSoldierPanel.transform, "Panel_Weapon");
            }
            if (_singleSelectionSoldierWeaponImage == null && _singleSelectionSoldierWeaponPanel != null)
            {
                Transform content = _singleSelectionSoldierWeaponPanel.transform.Find("Content");
                Transform icon = content != null ? content.Find("ICON") : null;
                _singleSelectionSoldierWeaponImage = icon != null ? icon.GetComponent<Image>() : FindImageByName(_singleSelectionSoldierWeaponPanel.transform, "ICON");
            }
        }

        private void UpdateCameraButtonState()
        {
            if (_cameraButtonSelected == null)
            {
                if (buttonCamera != null)
                    _cameraButtonSelected = FindChildObjectByName(buttonCamera.transform, "Selected");
                if (_cameraButtonSelected == null)
                    return;
            }

            _cameraButtonSelected.SetActive(_selectionSystem != null && _selectionSystem.IsNormalIsoModeActive);
        }

        private void UpdateSelectionToggleButtons(bool hasAnySelection)
        {
            bool hasVisibleSoldiers = !hasAnySelection && _selectionSystem != null && _selectionSystem.HasVisiblePlayerSoldiers();
            bool hasVisibleVehicles = !hasAnySelection && _selectionSystem != null && _selectionSystem.HasVisiblePlayerVehicles();
            bool hasVisibleUnits = hasVisibleSoldiers || hasVisibleVehicles || (!hasAnySelection && _selectionSystem != null && _selectionSystem.HasVisiblePlayerUnits());
            bool hasVisibleBuildings = !hasAnySelection &&
                                       _buildingUiCommandSystem != null &&
                                       _buildingUiCommandSystem.HasVisibleSelectableBuilding(_buildingUiCommandContext, _worldCamera);
            bool hasVisibleSelectable = hasVisibleUnits || hasVisibleBuildings;

            if (_buttonSelect != null)
                _buttonSelect.gameObject.SetActive(hasVisibleSelectable && !_runtimeGameplayStateSystem.SelectionModeActive);
            if (_buttonSelectAll != null)
                _buttonSelectAll.gameObject.SetActive(hasVisibleUnits);
            if (_buttonSelectAllSoldiers != null)
                _buttonSelectAllSoldiers.gameObject.SetActive(hasVisibleSoldiers);
            if (_buttonSelectAllSoldiersAlias != null)
                _buttonSelectAllSoldiersAlias.gameObject.SetActive(hasVisibleSoldiers);
            if (_buttonSelectAllVehicles != null)
                _buttonSelectAllVehicles.gameObject.SetActive(hasVisibleVehicles);
            if (_buttonSelectAllVehiclesAlias != null)
                _buttonSelectAllVehiclesAlias.gameObject.SetActive(hasVisibleVehicles);
            if (_buttonDeselectAll != null)
                _buttonDeselectAll.gameObject.SetActive(hasAnySelection);
        }

        private void ClearGameplaySelection()
        {
            _selectionSystem?.DeselectAllUnits("MenuView.ClearGameplaySelection");
            _buildingUiCommandSystem?.ClearSelectedBuilding(_buildingUiCommandContext, "MenuView.ButtonBackClicked");
        }

        private void ResolveCampPanels()
        {
            if (panelCamp == null)
                return;

            Transform campRoot = panelCamp.transform;
            if (_campListRoot == null && campScrollContent == null)
                _campListRoot = campRoot.Find("Panel_List");
            if (_campLayoutRoot == null && campScrollContent != null)
                _campLayoutRoot = campScrollContent;
            if (_campLayoutRoot == null && _campListRoot != null)
                _campLayoutRoot = _campListRoot.Find("Panel_Layout");
            if (campScrollRect == null && campScrollContent != null)
                campScrollRect = campScrollContent.GetComponentInParent<ScrollRect>();
            if (_campSelectedPanel == null)
                _campSelectedPanel = FindChildObjectByName(campRoot, "Panel_Selected");
            if (_campSelectedPortrait == null)
                _campSelectedPortrait = campSelectedPortraitImage;
            if (_campSelectedWeaponRoot == null)
                _campSelectedWeaponRoot = campSelectedWeaponRoot;
            if (_campSelectedWeapon == null)
                _campSelectedWeapon = campSelectedWeaponImage;
            if (_campSelectedWeaponName == null)
                _campSelectedWeaponName = campSelectedWeaponNameText;
            if (_campSelectedModelWeaponRoot == null)
                _campSelectedModelWeaponRoot = campSelectedModelWeaponRoot != null ? campSelectedModelWeaponRoot : ResolveCampSelectedModelWeaponRoot();
            if (_campSelectedModelWeaponImage == null)
                _campSelectedModelWeaponImage = campSelectedModelWeaponImage != null ? campSelectedModelWeaponImage : ResolveCampSelectedModelWeaponImage();
            if (_campSelectedName == null)
                _campSelectedName = campSelectedNameText;
            if (_campDescriptionPanel == null)
                _campDescriptionPanel = FindChildObjectByName(campRoot, "DescriptionPanel");
            if (_campDescriptionText == null)
                _campDescriptionText = campDescriptionText;
            if (_campSelectedInfoBottomRoot == null && _campSelectedPanel != null)
            {
                _campSelectedInfoBottomRoot = campSelectedInfoBottomRoot != null
                    ? campSelectedInfoBottomRoot
                    : FindChildObjectByName(_campSelectedPanel.transform, "Info_Bottom");
            }
            if (_campRankScrollRoot == null)
            {
                _campRankScrollRoot = campRankScrollRoot != null
                    ? campRankScrollRoot
                    : campRankScrollRect != null
                        ? campRankScrollRect.gameObject
                        : FindChildObjectByName(campRoot, "ScrollViewRanks");
            }
            UpdateCampSelectedBadge();
            UpdateCampRankBadgeList();
            if (_campPriceLabel == null)
            {
                GameObject pricePanel = FindChildObjectByName(campRoot, "Price_Panel");
                Transform priceRoot = pricePanel != null ? pricePanel.transform : null;
                _campPriceLabel = FindTextByName(priceRoot, "Label_ButtonName");
            }
            if (_campRequestButton == null)
                _campRequestButton = FindChildObjectByName(campRoot, "Button_Request");
            if (_campRequestButtonComponent == null && _campRequestButton != null)
            {
                _campRequestButtonComponent = _campRequestButton.GetComponent<Button>();
                if (_campRequestButtonComponent != null)
                {
                    _campRequestButtonComponent.onClick.RemoveAllListeners();
                    _campRequestButtonComponent.onClick.AddListener(ButtonRequestClicked);
                }
            }
            if (_campRequestGreens.Count == 0 && _campRequestReds.Count == 0 && _campRequestButton != null)
            {
                CollectDescendantsByName(_campRequestButton.transform, "SPR_Background_Green", _campRequestGreens);
                CollectDescendantsByName(_campRequestButton.transform, "SPR_Background_Red", _campRequestReds);
            }
            if (_campAmmoSelected == null && buttonCampAmmo != null)
                _campAmmoSelected = ResolveCampNavSelected(buttonCampAmmo);
            if (_campSoldiersSelected == null && buttonCampSoldiers != null)
                _campSoldiersSelected = ResolveCampNavSelected(buttonCampSoldiers);
            if (_campVehiclesSelected == null && buttonCampVehicles != null)
                _campVehiclesSelected = ResolveCampNavSelected(buttonCampVehicles);
            if (_campBuildingsSelected == null && buttonCampBuildings != null)
                _campBuildingsSelected = ResolveCampNavSelected(buttonCampBuildings);

            if (_campItemViews.Count > 0 || _campLayoutRoot == null)
                return;

            if (campItemTemplate != null)
            {
                if (campItemTemplate.transform.parent != _campLayoutRoot)
                    campItemTemplate.transform.SetParent(_campLayoutRoot, false);
                _campItemViews.Add(CreateCampListItemView(campItemTemplate, 0));
                return;
            }

            for (int i = 0; i < _campLayoutRoot.childCount; i++)
                _campItemViews.Add(CreateCampListItemView(_campLayoutRoot.GetChild(i).gameObject, i));
        }

        private CampListItemView CreateCampListItemView(GameObject root, int slotIndex)
        {
            CampListItemViewReferences references = root != null ? root.GetComponent<CampListItemViewReferences>() : null;
            Button button = references != null ? references.button : null;
            if (button == null && root != null)
                button = root.GetComponent<Button>();
            if (button == null && root != null)
                button = root.GetComponentInChildren<Button>(true);

            Graphic clickTarget = references != null ? references.clickTarget : null;
            if (clickTarget == null && button != null && button.targetGraphic != null && root != null && button.targetGraphic.transform.IsChildOf(root.transform))
                clickTarget = button.targetGraphic;
            if (clickTarget == null && root != null)
            {
                clickTarget = root.GetComponent<Graphic>();
                if (clickTarget == null)
                {
                    Image runtimeTarget = root.GetComponent<Image>();
                    if (runtimeTarget == null)
                        runtimeTarget = root.AddComponent<Image>();
                    runtimeTarget.color = new Color(1f, 1f, 1f, 0f);
                    runtimeTarget.raycastTarget = true;
                    clickTarget = runtimeTarget;
                }
            }
            else if (clickTarget != null)
            {
                clickTarget.raycastTarget = true;
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnCampItemClicked(slotIndex));
                if (clickTarget != null)
                    button.targetGraphic = clickTarget;
            }

            Image portrait = references != null ? references.portraitImage : null;
            GameObject selectedRoot = references != null ? references.selectedRoot : null;
            TMP_Text selectedName = references != null ? references.selectedName : null;

            if (selectedRoot != null)
                selectedRoot.SetActive(false);

            return new CampListItemView
            {
                Root = root,
                Button = button,
                PortraitImage = portrait,
                SelectedRoot = selectedRoot,
                SelectedName = selectedName,
                ClickTarget = clickTarget
            };
        }

        private void EnsureCampItemViewCount(int count)
        {
            if (_campLayoutRoot == null || campItemTemplate == null)
                return;

            ResolveCampPanels();
            while (_campItemViews.Count < count)
            {
                GameObject instance = Instantiate(campItemTemplate, _campLayoutRoot);
                instance.name = $"{campItemTemplate.name}_{_campItemViews.Count + 1:00}";
                _campItemViews.Add(CreateCampListItemView(instance, _campItemViews.Count));
            }
        }

        private void SetCampMenuType(CampMenuType type)
        {
            SuppressNextWorldClick();
            _campMenuType = type;
            UpdateCampTabSelection();
            if (menuType == MenuType.Camp)
                RefreshCampList();
        }

        private void RefreshCampList()
        {
            ResolveCampPanels();
            UpdateCampTabSelection();
            _campEntries.Clear();

            switch (_campMenuType)
            {
                case CampMenuType.Buildings:
                    PopulateCampBuildings();
                    break;
                case CampMenuType.Soldiers:
                    PopulateCampUnits(false);
                    break;
                case CampMenuType.Vehicles:
                    PopulateCampUnits(true);
                    break;
                case CampMenuType.Ammo:
                    break;
            }

            EnsureCampItemViewCount(_campEntries.Count);
            for (int i = 0; i < _campItemViews.Count; i++)
            {
                CampListItemView view = _campItemViews[i];
                bool hasEntry = i < _campEntries.Count;
                if (view.Root != null)
                    view.Root.SetActive(hasEntry);

                if (view.SelectedRoot != null)
                    view.SelectedRoot.SetActive(false);

                if (!hasEntry)
                    continue;

                if (view.SelectedName != null)
                    view.SelectedName.text = _campEntries[i].DisplayName;

                Sprite sprite = GetCampPreviewSprite(_campEntries[i].Prefab);
                if (view.PortraitImage != null)
                    view.PortraitImage.sprite = sprite;
            }

            if (_campEntries.Count > 0)
            {
                int targetIndex = _campSelectedIndex >= 0 && _campSelectedIndex < _campEntries.Count
                    ? _campSelectedIndex
                    : 0;
                SelectCampItem(targetIndex);
            }
            else
                ClearCampSelection();
        }

        private void PopulateCampBuildings()
        {
            if (_buildingUiCommandSystem == null)
                return;

            int count = _buildingUiCommandSystem.ConfiguredSpawnableCount(_buildingUiCommandContext);
            for (int i = 0; i < count; i++)
            {
                if (!_buildingUiCommandSystem.TryGetConfiguredSpawnable(_buildingUiCommandContext, i, out var entry) || entry.Prefab == null || !entry.CanRequest)
                    continue;

                _campEntries.Add(new CampCatalogEntry(entry.DisplayName, entry.Description, entry.Prefab, entry.Price));
            }
        }

        private void PopulateCampUnits(bool vehicles)
        {
            if (_buildingUiCommandSystem == null)
                return;

            int count = _buildingUiCommandSystem.ConfiguredUnitCount(_buildingUiCommandContext);
            for (int i = 0; i < count; i++)
            {
                if (!_buildingUiCommandSystem.TryGetConfiguredUnit(_buildingUiCommandContext, i, out var entry) || entry.Prefab == null || !entry.CanRequest || entry.IsVehicle != vehicles)
                    continue;

                _campEntries.Add(new CampCatalogEntry(entry.DisplayName, entry.Description, entry.Prefab, entry.Price));
            }
        }

        private void OnCampItemClicked(int index)
        {
            SuppressNextWorldClick();
            if (index == _campSelectedIndex)
                return;

            SelectCampItem(index);
        }

        private void ButtonCampPreviewPreviousClicked()
        {
            SuppressNextWorldClick();
            SelectCampItemWrapped(-1);
        }

        private void ButtonCampPreviewNextClicked()
        {
            SuppressNextWorldClick();
            SelectCampItemWrapped(1);
        }

        private void SelectCampItemWrapped(int direction)
        {
            if (_campEntries.Count == 0)
                return;

            int current = _campSelectedIndex >= 0 && _campSelectedIndex < _campEntries.Count ? _campSelectedIndex : 0;
            int next = (current + direction) % _campEntries.Count;
            if (next < 0)
                next += _campEntries.Count;
            if (next == _campSelectedIndex)
                return;

            SelectCampItem(next);
        }

        private void ButtonRequestClicked()
        {
            SuppressNextWorldClick();
            if (menuType != MenuType.Camp || _campSelectedIndex < 0 || _campSelectedIndex >= _campEntries.Count)
                return;

            if (_buildingUiCommandSystem == null)
                return;

            CampCatalogEntry entry = _campEntries[_campSelectedIndex];
            bool isBuildingRequest = _buildingUiCommandSystem.IsConfiguredSpawnablePrefab(_buildingUiCommandContext, entry.Prefab);
            BuildingUiCommandSystem.CampRequestFailure failure = _buildingUiCommandSystem.TryRequestCampItem(_buildingUiCommandContext, entry.Prefab, entry.Price, out string requiredBuildingName, isBuildingRequest);
            switch (failure)
            {
                case BuildingUiCommandSystem.CampRequestFailure.None:
                    UpdateMoneyLabel();
                    if (isBuildingRequest)
                    {
                        menuType = MenuType.Game;
                        UpdatePanels();
                        ShowGameMenuType(GameMenuType.Free);
                        OpenBuildingPlacementConfirmPanel();
                    }
                    else
                    {
                        _campHasDeferredProductionFocus = true;
                        UpdateCampPriceState();
                    }
                    break;
                case BuildingUiCommandSystem.CampRequestFailure.NotEnoughMoney:
                    OpenGenericWarningPanel("not_enough_money");
                    break;
                case BuildingUiCommandSystem.CampRequestFailure.MissingProducerBuilding:
                    OpenGenericWarningPanel("create_first", string.IsNullOrWhiteSpace(requiredBuildingName) ? "Building" : requiredBuildingName);
                    break;
            }

            UpdateCampPriceState();
        }

        private void SelectCampItem(int index)
        {
            if (index < 0 || index >= _campEntries.Count)
                return;

            UpdateCampSelectedBadge();
            _campSelectedIndex = index;
            CampCatalogEntry entry = _campEntries[index];
            if (_campSelectedPanel != null)
                _campSelectedPanel.SetActive(true);
            if (_campSelectedPortrait != null)
                _campSelectedPortrait.sprite = GetCampPreviewSprite(entry.Prefab);
            UpdateCampSelectedWeaponPanels(entry.Prefab);
            if (_campSelectedName != null)
                _campSelectedName.text = entry.DisplayName;
            if (_campDescriptionText != null)
                _campDescriptionText.text = string.IsNullOrWhiteSpace(entry.Description) ? entry.DisplayName : entry.Description;
            UpdateCampSelectedSoldierOnlyPanels();
            UpdateCampSelectedModelPreview(entry.Prefab);
            UpdateCampPriceState();

            for (int i = 0; i < _campItemViews.Count; i++)
            {
                CampListItemView view = _campItemViews[i];
                bool isSelected = i == index;
                if (view.Button != null)
                    view.Button.interactable = true;
                if (view.SelectedRoot != null)
                    view.SelectedRoot.SetActive(isSelected);
                if (view.SelectedName != null && i < _campEntries.Count)
                    view.SelectedName.text = _campEntries[i].DisplayName;
            }

            ScrollCampListToSelectedItem(index);
        }

        private void ScrollCampListToSelectedItem(int index)
        {
            if (campScrollRect == null || campScrollRect.content == null || index < 0 || index >= _campItemViews.Count)
                return;

            RectTransform content = campScrollRect.content;
            RectTransform viewport = campScrollRect.viewport != null ? campScrollRect.viewport : campScrollRect.GetComponent<RectTransform>();
            RectTransform selected = _campItemViews[index].Root != null ? _campItemViews[index].Root.transform as RectTransform : null;
            if (viewport == null || selected == null)
                return;

            Canvas.ForceUpdateCanvases();
            float contentWidth = content.rect.width;
            float viewportWidth = viewport.rect.width;
            if (contentWidth <= viewportWidth || contentWidth <= 0f)
            {
                campScrollRect.horizontalNormalizedPosition = 0f;
                return;
            }

            Vector3 selectedWorldCenter = selected.TransformPoint(selected.rect.center);
            float selectedContentCenterX = content.InverseTransformPoint(selectedWorldCenter).x;
            float contentLeft = content.rect.xMin;
            float targetLeft = selectedContentCenterX - viewportWidth * 0.5f;
            float normalized = Mathf.Clamp01((targetLeft - contentLeft) / (contentWidth - viewportWidth));
            campScrollRect.horizontalNormalizedPosition = normalized;
        }

        private void UpdateCampSelectedWeaponPanels(GameObject prefab)
        {
            Sprite weaponSprite = null;
            string weaponDisplayName = string.Empty;
            bool showWeapon = false;

            if (prefab != null &&
                ResolvePreviewModelCategory(prefab) == PreviewModelCategory.Character &&
                prefab.TryGetComponent<UnitGridAuthoring>(out UnitGridAuthoring authoring))
            {
                weaponSprite = authoring.WeaponSprite;
                weaponDisplayName = authoring.WeaponDisplayName;
                showWeapon = weaponSprite != null;
            }

            if (_campSelectedWeapon != null)
            {
                _campSelectedWeapon.sprite = weaponSprite;
                _campSelectedWeapon.gameObject.SetActive(showWeapon);
            }

            if (_campSelectedWeaponRoot != null)
                _campSelectedWeaponRoot.SetActive(showWeapon);

            if (_campSelectedWeaponName != null)
                _campSelectedWeaponName.text = showWeapon && !string.IsNullOrWhiteSpace(weaponDisplayName) ? weaponDisplayName : string.Empty;

            if (_campSelectedModelWeaponImage != null)
            {
                _campSelectedModelWeaponImage.sprite = weaponSprite;
                _campSelectedModelWeaponImage.gameObject.SetActive(showWeapon);
            }

            if (_campSelectedModelWeaponRoot != null)
                _campSelectedModelWeaponRoot.SetActive(showWeapon);
        }

        private void UpdateCampSelectedSoldierOnlyPanels()
        {
            ResolveCampPanels();
            bool showSoldierPanels = menuType == MenuType.Camp &&
                                     _campSelectedIndex >= 0 &&
                                     _campMenuType == CampMenuType.Soldiers;

            if (_campRankScrollRoot != null)
                _campRankScrollRoot.SetActive(showSoldierPanels);
            if (_campSelectedInfoBottomRoot != null)
                _campSelectedInfoBottomRoot.SetActive(showSoldierPanels);
        }

        private GameObject ResolveCampSelectedModelWeaponRoot()
        {
            Transform selectedRoot = _campSelectedPanel != null ? _campSelectedPanel.transform : null;
            Transform modelPanel = selectedRoot != null ? selectedRoot.Find("3D_Panel") : null;
            if (modelPanel == null)
            {
                GameObject modelPanelObject = FindChildObjectByName(selectedRoot, "3D_Panel");
                modelPanel = modelPanelObject != null ? modelPanelObject.transform : null;
            }

            Transform weaponRoot = modelPanel != null ? modelPanel.Find("Panel_Weapon") : null;
            if (weaponRoot == null)
            {
                GameObject weaponRootObject = FindChildObjectByName(modelPanel, "Panel_Weapon");
                weaponRoot = weaponRootObject != null ? weaponRootObject.transform : null;
            }

            return weaponRoot != null ? weaponRoot.gameObject : null;
        }

        private Image ResolveCampSelectedModelWeaponImage()
        {
            Transform weaponRoot = _campSelectedModelWeaponRoot != null ? _campSelectedModelWeaponRoot.transform : ResolveCampSelectedModelWeaponRoot()?.transform;
            Transform content = weaponRoot != null ? weaponRoot.Find("Content") : null;
            Transform icon = content != null ? content.Find("ICON") : null;
            if (icon == null)
            {
                GameObject iconObject = FindChildObjectByName(weaponRoot, "ICON");
                icon = iconObject != null ? iconObject.transform : null;
            }

            return icon != null ? icon.GetComponent<Image>() : null;
        }

        private void UpdateCampSelectedBadge()
        {
            if (soldierBadgeCatalogConfig == null ||
                soldierBadgeCatalogConfig.Badges == null ||
                soldierBadgeCatalogConfig.Badges.Count == 0)
            {
                return;
            }

            int badgeIndex = Mathf.Clamp(_campSelectedBadgeIndex, 0, soldierBadgeCatalogConfig.Badges.Count - 1);
            SoldierBadgeConfigEntry badge = soldierBadgeCatalogConfig.Badges[badgeIndex];
            if (badge == null)
                return;

            if (campSelectedRankNameText != null)
                campSelectedRankNameText.text = badge.DisplayName;
            if (campSelectedTierNumberText != null)
                campSelectedTierNumberText.text = badge.Tier.ToString("00");
            if (campSelectedPlayerRankText != null)
                campSelectedPlayerRankText.text = badge.Rank.ToString("00");

            if (campSelectedBadgeContent == null || badge.BadgePrefab == null)
                return;

            for (int i = campSelectedBadgeContent.childCount - 1; i >= 0; i--)
                Destroy(campSelectedBadgeContent.GetChild(i).gameObject);

            _campSelectedBadgeInstance = null;
            _campSelectedBadgeInstance = Instantiate(badge.BadgePrefab, campSelectedBadgeContent);
            Transform badgeTransform = _campSelectedBadgeInstance.transform;
            badgeTransform.localPosition = Vector3.zero;
            badgeTransform.localRotation = Quaternion.identity;
            badgeTransform.localScale = Vector3.one;

            if (badgeTransform is RectTransform rectTransform)
            {
                rectTransform.anchoredPosition3D = Vector3.zero;
                rectTransform.localRotation = Quaternion.identity;
                rectTransform.localScale = Vector3.one;
            }
        }

        private void UpdateCampRankBadgeList()
        {
            if (soldierBadgeCatalogConfig == null ||
                soldierBadgeCatalogConfig.Badges == null ||
                soldierBadgeCatalogConfig.Badges.Count == 0 ||
                campRankScrollContent == null ||
                campRankBadgeTemplate == null)
            {
                return;
            }

            if (_campRankBadgeItems.Count > 0)
            {
                ApplyCampRankBadgeContentScale();
                return;
            }

            if (campRankScrollRect == null)
                campRankScrollRect = campRankScrollContent.GetComponentInParent<ScrollRect>();

            _campRankBadgeTemplateContentScale = campRankBadgeContentScale;

            for (int i = 0; i < soldierBadgeCatalogConfig.Badges.Count; i++)
            {
                GameObject item = i == 0 ? campRankBadgeTemplate : Instantiate(campRankBadgeTemplate, campRankScrollContent);
                item.name = $"{campRankBadgeTemplate.name}_{i + 1:00}";
                item.SetActive(true);
                if (item.transform.parent != campRankScrollContent)
                    item.transform.SetParent(campRankScrollContent, false);

                PopulateCampRankBadgeItem(item, soldierBadgeCatalogConfig.Badges[i], _campRankBadgeTemplateContentScale);
                ConfigureCampRankBadgeClick(item, i);
                _campRankBadgeItems.Add(item);
            }

            if (campRankScrollRect != null)
                campRankScrollRect.horizontalNormalizedPosition = 0f;

            ApplyCampRankBadgeContentScale();
        }

        private void ConfigureCampRankBadgeClick(GameObject item, int badgeIndex)
        {
            if (item == null)
                return;

            Button button = item.GetComponent<Button>();
            if (button == null)
                button = item.GetComponentInChildren<Button>(true);

            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectCampRankBadge(badgeIndex));
        }

        private void SelectCampRankBadge(int badgeIndex)
        {
            if (soldierBadgeCatalogConfig == null ||
                soldierBadgeCatalogConfig.Badges == null ||
                soldierBadgeCatalogConfig.Badges.Count == 0)
            {
                return;
            }

            int clampedIndex = Mathf.Clamp(badgeIndex, 0, soldierBadgeCatalogConfig.Badges.Count - 1);
            if (_campSelectedBadgeIndex == clampedIndex)
                return;

            _campSelectedBadgeIndex = clampedIndex;
            UpdateCampSelectedBadge();
        }

        private void ApplyCampRankBadgeContentScale()
        {
            Vector3 targetScale = campRankBadgeContentScale;
            for (int i = 0; i < _campRankBadgeItems.Count; i++)
            {
                GameObject item = _campRankBadgeItems[i];
                if (item == null)
                    continue;

                Transform badgeRoot = FindBadgeVisualRoot(item.transform);
                if (badgeRoot == null)
                    continue;

                badgeRoot.localScale = targetScale;
                if (badgeRoot is RectTransform rectTransform)
                    rectTransform.localScale = targetScale;
            }
        }

        private static void PopulateCampRankBadgeItem(GameObject item, SoldierBadgeConfigEntry badge, Vector3 contentScale)
        {
            if (item == null || badge == null)
                return;

            Transform badgeRoot = FindBadgeVisualRoot(item.transform);
            if (badgeRoot == null || badge.BadgePrefab == null)
                return;

            Vector3 contentLocalPosition = badgeRoot.localPosition;
            Quaternion contentLocalRotation = badgeRoot.localRotation;
            Vector3 contentLocalScale = contentScale;
            Vector2 contentAnchoredPosition = badgeRoot is RectTransform contentRect ? contentRect.anchoredPosition : Vector2.zero;
            Vector2 contentSizeDelta = badgeRoot is RectTransform contentSizeRect ? contentSizeRect.sizeDelta : Vector2.zero;

            for (int i = badgeRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = badgeRoot.GetChild(i);
                if (child != null && child.name == "SPR_Background")
                    continue;

                Destroy(child.gameObject);
            }

            GameObject badgeInstance = Instantiate(badge.BadgePrefab, badgeRoot);
            Transform badgeTransform = badgeInstance.transform;
            badgeTransform.localPosition = Vector3.zero;
            badgeTransform.localRotation = Quaternion.identity;
            badgeTransform.localScale = Vector3.one;
            if (badgeTransform is RectTransform rectTransform)
            {
                rectTransform.anchoredPosition3D = Vector3.zero;
                rectTransform.localRotation = Quaternion.identity;
                rectTransform.localScale = Vector3.one;
            }

            badgeRoot.localPosition = contentLocalPosition;
            badgeRoot.localRotation = contentLocalRotation;
            badgeRoot.localScale = contentLocalScale;
            if (badgeRoot is RectTransform restoredContentRect)
            {
                restoredContentRect.anchoredPosition = contentAnchoredPosition;
                restoredContentRect.sizeDelta = contentSizeDelta;
                restoredContentRect.localScale = contentLocalScale;
            }
        }

        private static Transform FindBadgeVisualRoot(Transform itemRoot)
        {
            if (itemRoot == null)
                return null;

            Transform content = itemRoot.Find("Content");
            return content != null ? content : itemRoot;
        }

        private void UpdateCampSelectedModelPreview(GameObject prefab)
        {
            if (prefab == null || campSelectedModelPreviewRoot == null)
            {
                ClearCampSelectedModelPreview();
                return;
            }

            PreviewModelCategory nextCategory = ResolvePreviewModelCategory(prefab);
            bool canCarousel = _campSelectedModelPreviewInstance != null &&
                               _campSelectedModelCategory != PreviewModelCategory.None &&
                               _campSelectedModelCategory == nextCategory;

            if (_campSelectedModelPreviousPreviewInstance != null)
                Destroy(_campSelectedModelPreviousPreviewInstance);

            if (canCarousel)
            {
                _campSelectedModelPreviousPreviewInstance = _campSelectedModelPreviewInstance;
                _campSelectedModelPreviousPreviewContent = _campSelectedModelPreviewContent;
                _campSelectedModelPreviousLocalBounds = _campSelectedModelLocalBounds;
            }
            else
            {
                if (_campSelectedModelPreviewInstance != null)
                    Destroy(_campSelectedModelPreviewInstance);
                _campSelectedModelPreviousPreviewInstance = null;
                _campSelectedModelPreviousPreviewContent = null;
                _campSelectedModelPreviousLocalBounds = default;
            }

            EnsureCampSelectedModelPreviewRenderTarget();
            GameObject instance = Instantiate(prefab);
            instance.name = $"{prefab.name}_Camp3DPreview";
            _campSelectedModelPreviewInstance = instance;
            _campSelectedModelSourcePrefab = prefab;
            _campSelectedModelPreviewContent = ResolveModelPreviewContent(instance.transform);
            HideNonModelPreviewRenderers(instance, _campSelectedModelPreviewContent);
            DisableCampSelectedModelRuntimeComponents(instance);
            SetLayerRecursive(instance.transform, CampSelectedModelPreviewLayer);
            ConfigureCampSelectedModelPreviewBounds();
            ConfigureCampSelectedModelPreviewAnimation(prefab, instance);
            _campSelectedModelCategory = nextCategory;
            _campSelectedModelCarouselStartedAt = canCarousel ? Time.unscaledTime : Time.unscaledTime - CampSelectedModelCarouselTransitionSeconds;
            UpdateCampSelectedModelPreviewRuntime();
        }

        private void ClearCampSelectedModelPreview()
        {
            if (_campSelectedModelPreviewInstance != null)
                Destroy(_campSelectedModelPreviewInstance);
            if (_campSelectedModelPreviousPreviewInstance != null)
                Destroy(_campSelectedModelPreviousPreviewInstance);

            _campSelectedModelPreviewInstance = null;
            _campSelectedModelPreviewContent = null;
            _campSelectedModelPreviousPreviewInstance = null;
            _campSelectedModelPreviousPreviewContent = null;
            _campSelectedModelPreviousLocalBounds = default;
            _campSelectedModelGpuAnimator = null;
            _campSelectedModelGpuRenderers = null;
            _campSelectedModelIdleAnimationIndex = 0;
            _campSelectedModelAnimationStartedAt = 0f;
            _campSelectedModelLocalBounds = default;
            _campSelectedModelSourcePrefab = null;
            _campSelectedModelCategory = PreviewModelCategory.None;
            _campSelectedModelVisibleRenderers.Clear();
        }

        private void UpdateCampSelectedModelPreviewRuntime()
        {
            if (_campSelectedModelPreviewInstance == null || campSelectedModelPreviewRoot == null)
                return;

            EnsureCampSelectedModelPreviewRenderTarget();
            if (_campSelectedModelPreviewCamera == null)
                return;

            Transform previewTransform = _campSelectedModelPreviewInstance.transform;
            PreviewPanelFraming framing = ResolveCampSelectedModelPreviewFraming(_campSelectedModelSourcePrefab);

            float transition = Mathf.Clamp01((Time.unscaledTime - _campSelectedModelCarouselStartedAt) / CampSelectedModelCarouselTransitionSeconds);
            transition = transition * transition * (3f - 2f * transition);
            PositionCampSelectedModelOnCarousel(
                previewTransform,
                _campSelectedModelPreviewContent,
                _campSelectedModelLocalBounds,
                framing,
                Mathf.Lerp(90f, 0f, transition));

            if (_campSelectedModelPreviousPreviewInstance != null)
            {
                PositionCampSelectedModelOnCarousel(
                    _campSelectedModelPreviousPreviewInstance.transform,
                    _campSelectedModelPreviousPreviewContent,
                    _campSelectedModelPreviousLocalBounds,
                    framing,
                    Mathf.Lerp(0f, -90f, transition));

                if (transition >= 1f)
                {
                    Destroy(_campSelectedModelPreviousPreviewInstance);
                    _campSelectedModelPreviousPreviewInstance = null;
                    _campSelectedModelPreviousPreviewContent = null;
                    _campSelectedModelPreviousLocalBounds = default;
                }
            }

            _campSelectedModelPreviewCamera.transform.position = CampSelectedModelPreviewOrigin + framing.CameraPosition;
            _campSelectedModelPreviewCamera.transform.rotation = framing.CameraRotation;
            _campSelectedModelPreviewCamera.nearClipPlane = CampSelectedModelPreviewNearClip;
            _campSelectedModelPreviewCamera.farClipPlane = 1000f;
            Vector3 cameraSpaceModelPosition = Quaternion.Inverse(framing.CameraRotation) * (previewTransform.position - _campSelectedModelPreviewCamera.transform.position);
            SetCampSelectedModelRenderersVisible(cameraSpaceModelPosition.z > CampSelectedModelPreviewNearClip);

            UpdateCampSelectedModelGpuAnimation();
        }

        private static void PositionCampSelectedModelOnCarousel(Transform modelTransform, Transform content, Bounds localBounds, PreviewPanelFraming framing, float angleDegrees)
        {
            if (modelTransform == null)
                return;

            float radians = angleDegrees * Mathf.Deg2Rad;
            Vector3 circleOffset = new(Mathf.Sin(radians) * framing.CarouselRadius, 0f, Mathf.Cos(radians) * framing.CarouselRadius);
            modelTransform.rotation = framing.ModelRotation * Quaternion.Euler(0f, -angleDegrees, 0f);
            float modelSize = ResolvePreviewScaleSourceSize(localBounds, framing.ScaleMode);
            float modelScale = framing.TargetSize / modelSize;
            modelTransform.localScale = Vector3.one * modelScale;

            Vector3 targetPosition = CampSelectedModelPreviewOrigin + framing.ModelPosition + circleOffset;
            if (content != null && content != modelTransform)
            {
                modelTransform.position = targetPosition;
                content.localPosition = -localBounds.center;
            }
            else
            {
                modelTransform.position = targetPosition - modelTransform.rotation * (localBounds.center * modelScale);
            }
        }

        private static float ResolvePreviewScaleSourceSize(Bounds localBounds, PreviewScaleMode scaleMode)
        {
            Vector3 size = localBounds.size;
            return scaleMode == PreviewScaleMode.MaxDimension
                ? Mathf.Max(0.01f, size.x, size.y, size.z)
                : Mathf.Max(0.01f, size.y);
        }

        private void EnsureCampSelectedModelPreviewRenderTarget()
        {
            if (campSelectedModelPreviewRoot == null)
                return;

            if (_campSelectedModelPreviewImage == null)
            {
                GameObject imageObject = new("CampSelectedModelPreviewRender");
                imageObject.transform.SetParent(campSelectedModelPreviewRoot, false);
                _campSelectedModelPreviewImage = imageObject.AddComponent<RawImage>();
                _campSelectedModelPreviewImage.raycastTarget = false;
                RectTransform rect = imageObject.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one;
            }

            int width = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(1f, campSelectedModelPreviewRoot.rect.width)), 64, 1024);
            int height = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(1f, campSelectedModelPreviewRoot.rect.height)), 64, 1024);
            if (_campSelectedModelPreviewTexture == null ||
                _campSelectedModelPreviewTexture.width != width ||
                _campSelectedModelPreviewTexture.height != height)
            {
                if (_campSelectedModelPreviewTexture != null)
                    Destroy(_campSelectedModelPreviewTexture);

                _campSelectedModelPreviewTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
                {
                    name = "CampSelectedModelPreviewTexture",
                    hideFlags = HideFlags.HideAndDontSave
                };
                _campSelectedModelPreviewTexture.Create();
                _campSelectedModelPreviewImage.texture = _campSelectedModelPreviewTexture;
            }

            if (_campSelectedModelPreviewCamera == null)
            {
                GameObject cameraObject = new("CampSelectedModelPreviewCamera");
                cameraObject.hideFlags = HideFlags.HideAndDontSave;
                _campSelectedModelPreviewCamera = cameraObject.AddComponent<Camera>();
                _campSelectedModelPreviewCamera.clearFlags = CameraClearFlags.SolidColor;
                _campSelectedModelPreviewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                _campSelectedModelPreviewCamera.cullingMask = 1 << CampSelectedModelPreviewLayer;
                _campSelectedModelPreviewCamera.fieldOfView = 35f;
                _campSelectedModelPreviewCamera.enabled = true;
            }

            _campSelectedModelPreviewCamera.targetTexture = _campSelectedModelPreviewTexture;
        }

        private PreviewPanelFraming ResolveCampSelectedModelPreviewFraming(GameObject prefab)
        {
            if (campSelectedModelPreviewCameraConfig == null)
                return new PreviewPanelFraming(Vector3.zero, Quaternion.identity, new Vector3(0f, 1.5f, 6f), Quaternion.Euler(0f, 180f, 0f), 1.2f, 1.8f, PreviewScaleMode.Height);

            PreviewModelCategory category = ResolvePreviewModelCategory(prefab);
            if (category == PreviewModelCategory.Building)
            {
                return new PreviewPanelFraming(
                    campSelectedModelPreviewCameraConfig.BuildingModelPosition,
                    campSelectedModelPreviewCameraConfig.BuildingModelRotation,
                    campSelectedModelPreviewCameraConfig.BuildingCameraPosition,
                    campSelectedModelPreviewCameraConfig.BuildingCameraRotation,
                    campSelectedModelPreviewCameraConfig.BuildingCarouselRadius,
                    campSelectedModelPreviewCameraConfig.BuildingTargetHeight,
                    PreviewScaleMode.MaxDimension);
            }

            if (category == PreviewModelCategory.Vehicle)
            {
                return new PreviewPanelFraming(
                    campSelectedModelPreviewCameraConfig.VehicleModelPosition,
                    campSelectedModelPreviewCameraConfig.VehicleModelRotation,
                    campSelectedModelPreviewCameraConfig.VehicleCameraPosition,
                    campSelectedModelPreviewCameraConfig.VehicleCameraRotation,
                    campSelectedModelPreviewCameraConfig.VehicleCarouselRadius,
                    campSelectedModelPreviewCameraConfig.VehicleTargetHeight,
                    PreviewScaleMode.MaxDimension);
            }

            return new PreviewPanelFraming(
                campSelectedModelPreviewCameraConfig.CharacterModelPosition,
                campSelectedModelPreviewCameraConfig.CharacterModelRotation,
                campSelectedModelPreviewCameraConfig.CharacterCameraPosition,
                campSelectedModelPreviewCameraConfig.CharacterCameraRotation,
                campSelectedModelPreviewCameraConfig.CharacterCarouselRadius,
                campSelectedModelPreviewCameraConfig.CharacterTargetHeight,
                PreviewScaleMode.Height);
        }

        private static PreviewModelCategory ResolvePreviewModelCategory(GameObject prefab)
        {
            if (prefab == null)
                return PreviewModelCategory.None;
            if (prefab.GetComponent<BuildingDefinitionAuthoring>() != null)
                return PreviewModelCategory.Building;
            if (prefab.TryGetComponent<UnitGridAuthoring>(out UnitGridAuthoring unitAuthoring))
            {
                bool isVehicle = unitAuthoring.GetConfiguredFootprintCells().x > 1 ||
                                 unitAuthoring.GetConfiguredFootprintCells().y > 1 ||
                                 prefab.name.IndexOf("Veh_", StringComparison.OrdinalIgnoreCase) >= 0;
                return isVehicle ? PreviewModelCategory.Vehicle : PreviewModelCategory.Character;
            }

            return PreviewModelCategory.Character;
        }

        private void ConfigureCampSelectedModelPreviewBounds()
        {
            Renderer[] renderers = _campSelectedModelPreviewInstance != null
                ? _campSelectedModelPreviewInstance.GetComponentsInChildren<Renderer>(true)
                : Array.Empty<Renderer>();
            _campSelectedModelVisibleRenderers.Clear();

            bool hasBounds = false;
            Bounds localBounds = new(Vector3.zero, Vector3.one);
            Matrix4x4 rootToLocal = _campSelectedModelPreviewInstance.transform.worldToLocalMatrix;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled || renderer is ParticleSystemRenderer)
                    continue;

                _campSelectedModelVisibleRenderers.Add(renderer);
                Bounds rendererBounds = TransformBounds(rootToLocal, renderer.bounds);
                if (!hasBounds)
                {
                    localBounds = rendererBounds;
                    hasBounds = true;
                }
                else
                {
                    localBounds.Encapsulate(rendererBounds);
                }
            }

            _campSelectedModelLocalBounds = hasBounds ? localBounds : new Bounds(Vector3.zero, Vector3.one);
        }

        private void SetCampSelectedModelRenderersVisible(bool visible)
        {
            for (int i = 0; i < _campSelectedModelVisibleRenderers.Count; i++)
            {
                Renderer renderer = _campSelectedModelVisibleRenderers[i];
                if (renderer == null)
                    continue;

                renderer.enabled = visible;
            }
        }

        private void ConfigureCampSelectedModelPreviewAnimation(GameObject prefab, GameObject instance)
        {
            _campSelectedModelAnimationStartedAt = Time.unscaledTime;
            _campSelectedModelPropertyBlock ??= new MaterialPropertyBlock();
            _campSelectedModelGpuAnimator = null;
            _campSelectedModelGpuRenderers = null;
            _campSelectedModelIdleAnimationIndex = 0;

            bool isCharacter = prefab != null && prefab.name.StartsWith("Unit_Chr_", StringComparison.Ordinal);
            if (!isCharacter || instance == null)
                return;

            UnitGridAuthoring authoring = instance.GetComponent<UnitGridAuthoring>();
            MaterialAnimatorIndexAuthoring indexAuthoring = instance.GetComponentInChildren<MaterialAnimatorIndexAuthoring>(true);
            if (indexAuthoring == null || indexAuthoring.animator == null)
                return;

            _campSelectedModelGpuAnimator = indexAuthoring.animator.GetComponent<MaterialAnimatorAuthoring>();
            if (_campSelectedModelGpuAnimator == null || _campSelectedModelGpuAnimator.animations == null || _campSelectedModelGpuAnimator.animations.Count == 0)
            {
                _campSelectedModelGpuAnimator = null;
                return;
            }

            _campSelectedModelGpuRenderers = indexAuthoring.GetComponentsInChildren<Renderer>(true);
            _campSelectedModelIdleAnimationIndex = ResolveConfiguredPreviewAnimationIndex(authoring, UnitAnimationKind.Idle, UnitAnimationKind.Walk, UnitAnimationKind.Aim);
            ApplyCampSelectedModelGpuAnimation(_campSelectedModelIdleAnimationIndex, 0f);
        }

        private void UpdateCampSelectedModelGpuAnimation()
        {
            if (_campSelectedModelGpuAnimator == null || _campSelectedModelGpuRenderers == null)
                return;

            float elapsed = Mathf.Max(0f, Time.unscaledTime - _campSelectedModelAnimationStartedAt);
            ApplyCampSelectedModelGpuAnimation(_campSelectedModelIdleAnimationIndex, elapsed);
        }

        private void ApplyCampSelectedModelGpuAnimation(int animationIndex, float animationTime)
        {
            if (_campSelectedModelGpuAnimator == null || _campSelectedModelGpuRenderers == null || _campSelectedModelGpuAnimator.animations == null || _campSelectedModelGpuAnimator.animations.Count == 0)
                return;

            animationIndex = Mathf.Clamp(animationIndex, 0, _campSelectedModelGpuAnimator.animations.Count - 1);
            MaterialAnimatorBake animation = _campSelectedModelGpuAnimator.animations[animationIndex];
            int frameCount = Mathf.Max(1, animation.frames);
            int boneCount = Mathf.Max(1, _campSelectedModelGpuAnimator.bonesCount);
            float frameFloat = animationTime * Mathf.Max(1, animation.fps) * Mathf.Max(1, animation.speed);
            int frame = Mathf.FloorToInt(frameFloat) % frameCount;
            int nextFrame = (frame + 1) % frameCount;
            float blend = frameFloat - Mathf.Floor(frameFloat);
            Vector4 renderPixel = new(animation.start + frame * boneCount, animation.start + nextFrame * boneCount, blend, 0f);

            int modelShownId = Shader.PropertyToID("_SnivelerModelShown");
            int renderPixelId = Shader.PropertyToID("_SnivelerRenderPixel");
            for (int rendererIndex = 0; rendererIndex < _campSelectedModelGpuRenderers.Length; rendererIndex++)
            {
                Renderer renderer = _campSelectedModelGpuRenderers[rendererIndex];
                if (renderer == null)
                    continue;

                int materialCount = renderer.sharedMaterials != null ? renderer.sharedMaterials.Length : 0;
                for (int materialIndex = 0; materialIndex < materialCount; materialIndex++)
                {
                    renderer.GetPropertyBlock(_campSelectedModelPropertyBlock, materialIndex);
                    _campSelectedModelPropertyBlock.SetFloat(modelShownId, 1f);
                    _campSelectedModelPropertyBlock.SetVector(renderPixelId, renderPixel);
                    renderer.SetPropertyBlock(_campSelectedModelPropertyBlock, materialIndex);
                }
            }
        }

        private static int ResolveConfiguredPreviewAnimationIndex(UnitGridAuthoring authoring, UnitAnimationKind first, UnitAnimationKind second, UnitAnimationKind third)
        {
            if (authoring != null && authoring.AnimationOrder != null)
            {
                if (TryResolveConfiguredPreviewAnimationIndex(authoring.AnimationOrder, first, out int index))
                    return index;
                if (TryResolveConfiguredPreviewAnimationIndex(authoring.AnimationOrder, second, out index))
                    return index;
                if (TryResolveConfiguredPreviewAnimationIndex(authoring.AnimationOrder, third, out index))
                    return index;
            }

            return 0;
        }

        private static bool TryResolveConfiguredPreviewAnimationIndex(IReadOnlyList<UnitAnimationKind> animationOrder, UnitAnimationKind kind, out int animationIndex)
        {
            for (int i = 0; i < animationOrder.Count; i++)
            {
                if (animationOrder[i] != kind)
                    continue;

                animationIndex = i + 1;
                return true;
            }

            animationIndex = 0;
            return false;
        }

        private static Transform ResolveModelPreviewContent(Transform root)
        {
            if (root == null)
                return null;

            Transform model = root.Find("Model");
            if (model == null)
                model = root.Find("Mode");
            return model != null ? model : root;
        }

        private static void HideNonModelPreviewRenderers(GameObject instance, Transform modelRoot)
        {
            if (instance == null || modelRoot == null || modelRoot == instance.transform)
                return;

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || renderer.transform.IsChildOf(modelRoot))
                    continue;

                renderer.enabled = false;
            }
        }

        private static void SetLayerRecursive(Transform root, int layer)
        {
            if (root == null)
                return;

            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++)
                SetLayerRecursive(root.GetChild(i), layer);
        }

        private static void DisableCampSelectedModelRuntimeComponents(GameObject instance)
        {
            if (instance == null)
                return;

            Behaviour[] behaviours = instance.GetComponentsInChildren<Behaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                Behaviour behaviour = behaviours[i];
                if (behaviour == null ||
                    behaviour is Animator ||
                    behaviour is MaterialAnimatorAuthoring ||
                    behaviour is MaterialAnimatorIndexAuthoring)
                    continue;

                behaviour.enabled = false;
            }

            Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;

            Rigidbody[] rigidbodies = instance.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < rigidbodies.Length; i++)
                rigidbodies[i].isKinematic = true;

            Camera[] cameras = instance.GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < cameras.Length; i++)
                cameras[i].enabled = false;

            Canvas[] canvases = instance.GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
                canvases[i].enabled = false;
        }

        private static Bounds TransformBounds(Matrix4x4 matrix, Bounds bounds)
        {
            Vector3 center = matrix.MultiplyPoint3x4(bounds.center);
            Vector3 extents = bounds.extents;
            Vector3 axisX = matrix.MultiplyVector(new Vector3(extents.x, 0f, 0f));
            Vector3 axisY = matrix.MultiplyVector(new Vector3(0f, extents.y, 0f));
            Vector3 axisZ = matrix.MultiplyVector(new Vector3(0f, 0f, extents.z));
            extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
            extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
            extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);
            return new Bounds(center, extents * 2f);
        }

        private void ClearCampSelection()
        {
            _campSelectedIndex = -1;
            if (_campSelectedPanel != null)
                _campSelectedPanel.SetActive(false);
            if (_campSelectedWeapon != null)
            {
                _campSelectedWeapon.sprite = null;
                if (_campSelectedWeaponRoot != null)
                    _campSelectedWeaponRoot.SetActive(false);
                else
                    _campSelectedWeapon.gameObject.SetActive(false);
            }
            if (_campSelectedWeaponName != null)
                _campSelectedWeaponName.text = string.Empty;
            if (_campSelectedModelWeaponImage != null)
            {
                _campSelectedModelWeaponImage.sprite = null;
                _campSelectedModelWeaponImage.gameObject.SetActive(false);
            }
            if (_campSelectedModelWeaponRoot != null)
                _campSelectedModelWeaponRoot.SetActive(false);
            if (_campSelectedName != null)
                _campSelectedName.text = string.Empty;
            if (_campDescriptionText != null)
                _campDescriptionText.text = string.Empty;
            UpdateCampSelectedSoldierOnlyPanels();
            UpdateCampSelectedModelPreview(null);
            _campRequestFailure = BuildingUiCommandSystem.CampRequestFailure.InvalidSelection;
            _campRequestFailureBuildingName = string.Empty;
            UpdateCampPriceState();
            for (int i = 0; i < _campItemViews.Count; i++)
            {
                CampListItemView view = _campItemViews[i];
                if (view.Button != null)
                    view.Button.interactable = view.Root != null && view.Root.activeSelf;
                if (view.SelectedRoot != null)
                    view.SelectedRoot.SetActive(false);
            }
        }

        private void UpdateCampPriceState()
        {
            int price = (_campSelectedIndex >= 0 && _campSelectedIndex < _campEntries.Count)
                ? Mathf.Max(0, _campEntries[_campSelectedIndex].Price)
                : 0;

            if (_campPriceLabel != null)
                _campPriceLabel.text = price.ToString();

            _campRequestFailure = BuildingUiCommandSystem.CampRequestFailure.InvalidSelection;
            _campRequestFailureBuildingName = string.Empty;
            bool canRequest = false;
            if (_buildingUiCommandSystem != null && _campSelectedIndex >= 0 && _campSelectedIndex < _campEntries.Count)
            {
                CampCatalogEntry selectedEntry = _campEntries[_campSelectedIndex];
                _campRequestFailure = _buildingUiCommandSystem.GetCampRequestFailure(_buildingUiCommandContext, selectedEntry.Prefab, selectedEntry.Price, out _campRequestFailureBuildingName);
                canRequest = _campRequestFailure == BuildingUiCommandSystem.CampRequestFailure.None;
            }

            for (int i = 0; i < _campRequestGreens.Count; i++)
            {
                if (_campRequestGreens[i] != null)
                    _campRequestGreens[i].SetActive(canRequest);
            }

            for (int i = 0; i < _campRequestReds.Count; i++)
            {
                if (_campRequestReds[i] != null)
                    _campRequestReds[i].SetActive(!canRequest);
            }
        }

        private void UpdateCampTabSelection()
        {
            if (_campAmmoSelected != null)
                _campAmmoSelected.SetActive(_campMenuType == CampMenuType.Ammo);
            if (_campSoldiersSelected != null)
                _campSoldiersSelected.SetActive(_campMenuType == CampMenuType.Soldiers);
            if (_campVehiclesSelected != null)
                _campVehiclesSelected.SetActive(_campMenuType == CampMenuType.Vehicles);
            if (_campBuildingsSelected != null)
                _campBuildingsSelected.SetActive(_campMenuType == CampMenuType.Buildings);
        }

        private static GameObject ResolveCampNavSelected(Button button)
        {
            if (button == null)
                return null;

            Transform buttonRoot = button.transform;
            Transform itemRoot = buttonRoot.Find("Item");
            if (itemRoot == null)
                return null;

            Transform selectedRoot = itemRoot.Find("Selected");
            return selectedRoot != null ? selectedRoot.gameObject : null;
        }

        private static void CollectDescendantsByName(Transform root, string childName, List<GameObject> results)
        {
            if (root == null || string.IsNullOrEmpty(childName) || results == null)
                return;

            if (root.name == childName)
                results.Add(root.gameObject);

            for (int i = 0; i < root.childCount; i++)
                CollectDescendantsByName(root.GetChild(i), childName, results);
        }

        private static void CollectDescendantImagesByName(Transform root, string childName, List<Image> results)
        {
            if (root == null || string.IsNullOrEmpty(childName) || results == null)
                return;

            if (root.name == childName)
            {
                Image image = root.GetComponent<Image>();
                if (image != null)
                    results.Add(image);
            }

            for (int i = 0; i < root.childCount; i++)
                CollectDescendantImagesByName(root.GetChild(i), childName, results);
        }

        private Sprite GetCampPreviewSprite(GameObject prefab)
        {
            if (prefab == null)
                return null;

            SharedPrefabPreviewCache.RefreshConfig();

            if (_campPreviewRevision != SharedPrefabPreviewCache.Revision)
                ClearCampPreviewSpriteCache();

            if (_campPreviewSprites.TryGetValue(prefab, out Sprite sprite) && sprite != null)
                return sprite;

            if (!SharedPrefabPreviewCache.TryGetOrCreate(prefab, 1f, out RenderTexture texture) || texture == null)
                return null;

            Texture2D readable = RenderTextureToTexture2D(texture);
            if (readable == null)
                return null;

            readable.hideFlags = HideFlags.HideAndDontSave;
            sprite = Sprite.Create(readable, new Rect(0f, 0f, readable.width, readable.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            _campPreviewSprites[prefab] = sprite;
            return sprite;
        }

        private void ClearCampPreviewSpriteCache()
        {
            foreach (KeyValuePair<GameObject, Sprite> entry in _campPreviewSprites)
            {
                Sprite sprite = entry.Value;
                if (sprite == null)
                    continue;

                Texture2D texture = sprite.texture;
                Object.Destroy(sprite);
                if (texture != null)
                    Object.Destroy(texture);
            }

            _campPreviewSprites.Clear();
            _campPreviewRevision = SharedPrefabPreviewCache.Revision;
        }

        private static Texture2D RenderTextureToTexture2D(RenderTexture renderTexture)
        {
            if (renderTexture == null)
                return null;

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            try
            {
                Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0f, 0f, renderTexture.width, renderTexture.height), 0, 0);
                texture.Apply(false, false);
                return texture;
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        private void OpenConfirmPanel(ConfirmMode mode, string text)
        {
            _confirmOpen = true;
            _confirmMode = mode;

            if (confirmLabel != null)
                confirmLabel.text = text;

            SyncModalPanels();
            ShowGameMenuType(gameMenuType);
        }

        private void OpenDestroyConfirmPanel(bool destroyBuilding, string targetName)
        {
            _warningTargetName = string.IsNullOrWhiteSpace(targetName) ? "Object" : targetName;
            OpenConfirmPanel(
                destroyBuilding ? ConfirmMode.DestroyBuilding : ConfirmMode.DestroyUnit,
                GameStrings.Format("confirm_destroy", _warningTargetName));
        }

        private void OpenBuildingPlacementConfirmPanel()
        {
            OpenConfirmPanel(ConfirmMode.PlaceBuilding, GameStrings.Get("drag_building_to_final_position"));
        }

        private void CloseConfirmPanel()
        {
            _confirmOpen = false;
            _confirmMode = ConfirmMode.None;
            SyncModalPanels();
            ShowGameMenuType(gameMenuType);
        }

        private void OpenGenericWarningPanel(string key, params object[] args)
        {
            _warningOpen = true;
            _warningOpenedFrame = Time.frameCount;
            _warningAutoCloseAt = Time.unscaledTime + 3f;
            if (warningLabel != null)
                warningLabel.text = args != null && args.Length > 0 ? GameStrings.Format(key, args) : GameStrings.Get(key);

            SyncModalPanels();
        }

        private void CloseGenericWarningPanel()
        {
            _warningOpen = false;
            _warningOpenedFrame = -1;
            _warningAutoCloseAt = 0f;
            SyncModalPanels();
            if (menuType == MenuType.Game)
                ShowGameMenuType(gameMenuType);
        }

        private void SyncTacticalWarningPanel()
        {
            if (ThreatWarningRuntimeState.HasPendingWarning)
            {
                OpenTacticalWarningPanel(
                    ThreatWarningRuntimeState.PendingType,
                    ThreatWarningRuntimeState.PendingEtaSeconds);
                ThreatWarningRuntimeState.ClearPendingWarning();
            }

            if (_tacticalWarningOpen && Time.unscaledTime >= _tacticalWarningAutoCloseAt)
                CloseTacticalWarningPanel();
        }

        private void OpenTacticalWarningPanel(ThreatWarningType type, float etaSeconds)
        {
            _tacticalWarningOpen = true;
            _tacticalWarningAutoCloseAt = Time.unscaledTime + 3f;

            if (tacticalWarningTypeLabel != null)
            {
                tacticalWarningTypeLabel.text = type == ThreatWarningType.Air
                    ? GameStrings.Get("warning_air_attack_type")
                    : GameStrings.Get("warning_ground_attack_type");
            }

            if (tacticalWarningDescriptionLabel != null)
            {
                int roundedSeconds = Mathf.CeilToInt(Mathf.Max(0f, etaSeconds));
                tacticalWarningDescriptionLabel.text = GameStrings.Format("warning_attack_eta_seconds", roundedSeconds);
            }

            if (tacticalWarningPanel != null)
                tacticalWarningPanel.SetActive(true);
        }

        private void CloseTacticalWarningPanel()
        {
            _tacticalWarningOpen = false;
            _tacticalWarningAutoCloseAt = 0f;
            if (tacticalWarningPanel != null)
                tacticalWarningPanel.SetActive(false);
        }

        private void SyncModalPanels()
        {
            if (panelConfirm != null)
                panelConfirm.gameObject.SetActive(_confirmOpen);
            if (panelWarning != null)
                panelWarning.gameObject.SetActive(_warningOpen);
            SyncSettingsPanel();
        }

        private void CloseSettingsPanel()
        {
            _settingsOpen = false;
            SyncSettingsPanel();
            UpdatePanels();
        }

        private void SetCampButtonsPanelActive(bool active)
        {
            if (panelCampButtons != null)
                panelCampButtons.SetActive(active);
        }

        private void UpdateMoneyLabel()
        {
            if (moneyAmountText == null)
                ResolveMoneyPanel();

            if (moneyAmountText == null)
                return;

            int dollars = _buildingUiCommandSystem != null ? _buildingUiCommandSystem.CurrentDollars(_buildingUiCommandContext) : 0;
            moneyAmountText.text = dollars.ToString();
        }

        private void UpdateTimePanel()
        {
            if (dateText == null || timeText == null)
                ResolveTimePanel();

            if (dateText != null)
                dateText.text = _dayNightSystem != null ? _dayNightSystem.DayCount.ToString() : "-";

            if (timeText != null)
                timeText.text = _dayNightSystem != null ? $"{_dayNightSystem.Hour24:00}:{_dayNightSystem.Minute:00}" : "--:--";
        }

        private void UpdateRequestPanel()
        {
            ResolveRequestPanel();
            if (_requestPanelRoot == null || _requestCountdownTemplate == null)
                return;

            bool shouldShow = (menuType == MenuType.Game || (menuType == MenuType.Camp && _campOpenedFromGame)) && _buildingUiQuerySystem != null;
            if (!shouldShow)
            {
                _requestPanelRoot.gameObject.SetActive(false);
                return;
            }

            _buildingUiQuerySystem.GetFriendlyPendingProductionUiEntries(_buildingUiQueryContext, _pendingProductionEntries);
            bool hasEntries = _pendingProductionEntries.Count > 0;
            _requestPanelRoot.gameObject.SetActive(hasEntries);
            if (!hasEntries)
            {
                ClearObsoleteRequestCountdownViews(new HashSet<string>());
                return;
            }

            var activeKeys = new HashSet<string>();
            for (int i = 0; i < _pendingProductionEntries.Count; i++)
            {
                BuildingUiQuerySystem.PendingProductionUiEntry entry = _pendingProductionEntries[i];
                string key = BuildPendingProductionKey(entry);
                activeKeys.Add(key);
                if (!_requestCountdownViews.TryGetValue(key, out RequestCountdownView view) || view == null || view.Root == null)
                {
                    view = CreateRequestCountdownView();
                    if (view == null)
                        continue;
                    _requestCountdownViews[key] = view;
                }

                BindRequestCountdownView(view, entry);
            }

            ClearObsoleteRequestCountdownViews(activeKeys);
        }

        private void UpdateStatsPanel()
        {
            EnsureStatsBindings();
            if (_statsAmountTexts.Count == 0)
                return;

            GameRuntimeStats.Snapshot snapshot = GameRuntimeStats.GetSnapshot();
            int civilianDead = 0;
            _citizenPopulationSystem?.GetTotals(out _, out _, out _, out _, out civilianDead);

            foreach (KeyValuePair<string, TMP_Text> pair in _statsAmountTexts)
            {
                if (pair.Value == null)
                    continue;

                pair.Value.text = ResolveStatsAmount(pair.Key, snapshot, civilianDead).ToString();
            }
        }

        private void ResolveMoneyPanel()
        {
            if (moneyAmountText != null)
                return;

            Transform root = transform;
            Transform moneyButton = FindDescendantByName(root, "Button_Money");
            if (moneyButton == null)
                return;

            moneyAmountText = FindTextByName(moneyButton, "AmountText");
        }

        private void ResolveAutoModeButton()
        {
            if (_buttonAutoMode != null)
                return;

            Transform root = transform;
            Transform existingButton = FindDescendantByName(root, AutoModeButtonName);
            if (existingButton == null)
                return;

            _buttonAutoMode = existingButton.GetComponent<Button>();
            _autoModeLabel = FindTextByName(existingButton, AutoModeLabelName) ??
                             existingButton.GetComponentInChildren<TMP_Text>(true);
        }

        private void RefreshAutoModeButton()
        {
            if (_buttonAutoMode == null)
                ResolveAutoModeButton();
            if (_buttonAutoMode == null)
                return;

            bool isVisible = _runtimeGameplayStateSystem.PlayRequested && menuType == MenuType.Game;
            _buttonAutoMode.gameObject.SetActive(isVisible);
            if (_autoModeLabel == null)
                _autoModeLabel = _buttonAutoMode.GetComponentInChildren<TMP_Text>(true);
            if (_autoModeLabel != null)
                _autoModeLabel.text = _runtimeGameplayStateSystem.PlayerAutoModeEnabled ? "Auto" : "Manual";
        }

        private void BindGameplaySpeedDropdownVisuals()
        {
            if (gameplaySpeedDropdown == null)
                return;

            gameplaySpeedDropdown.ClearOptions();
            gameplaySpeedDropdown.AddOptions(new List<string>(GameplaySpeedLabels));
            gameplaySpeedDropdown.SetValueWithoutNotify(0);
            gameplaySpeedDropdown.RefreshShownValue();
        }

        private void BindAISettingsDropdownVisuals()
        {
            BindDropdown(aiDifficultyDropdown, AIDifficultyLabels, (int)AISettingsRuntimeState.Difficulty);
            BindDropdown(aiStartingMoneyDropdown, AIStartingMoneyLabels, (int)AISettingsRuntimeState.StartingMoney);
            BindDropdown(aiIncomeMultiplierDropdown, AIIncomeMultiplierLabels, ResolveIncomeMultiplierIndex(AISettingsRuntimeState.IncomeMultiplier));
            BindDropdown(aiBuildSpeedDropdown, AISpeedLabels, (int)AISettingsRuntimeState.BuildSpeed);
            BindDropdown(aiUnitProductionSpeedDropdown, AISpeedLabels, (int)AISettingsRuntimeState.UnitProductionSpeed);
            BindDropdown(aiAttackGroupSizeDropdown, AIAttackGroupSizeLabels, (int)AISettingsRuntimeState.AttackGroupSize);
            BindDropdown(aiAttackFrequencyDropdown, AIAttackFrequencyLabels, (int)AISettingsRuntimeState.AttackFrequency);
            BindDropdown(aiAggressionDropdown, AIAggressionLabels, (int)AISettingsRuntimeState.Aggression);
            BindDropdown(aiExpansionDropdown, AIExpansionLabels, (int)AISettingsRuntimeState.Expansion);
            BindDropdown(aiTargetPriorityDropdown, AITargetPriorityLabels, (int)AISettingsRuntimeState.TargetPriority);
            BindDropdown(aiPlayerAutoDropdown, AIPlayerAutoLabels, AISettingsRuntimeState.PlayerAutoAIEnabled ? 1 : 0);
            BindDropdown(aiEnemyCountDropdown, AIEnemyCountLabels, Mathf.Clamp(AISettingsRuntimeState.EnemyAICount, 1, 3) - 1);
        }

        private void BindDropdown(TMP_Dropdown dropdown, string[] labels, int selectedIndex)
        {
            if (dropdown == null)
                return;

            dropdown.onValueChanged.RemoveListener(AISettingsDropdownChanged);
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(labels));
            dropdown.SetValueWithoutNotify(Mathf.Clamp(selectedIndex, 0, labels.Length - 1));
            dropdown.RefreshShownValue();
            dropdown.onValueChanged.AddListener(AISettingsDropdownChanged);
        }

        private void AISettingsDropdownChanged(int _)
        {
            ApplyAISettingsFromDropdowns();
        }

        private void ApplyAISettingsFromDropdowns()
        {
            AISettingsRuntimeState.Difficulty = (AIDifficultySetting)GetDropdownValue(aiDifficultyDropdown, (int)AISettingsRuntimeState.Difficulty);
            AISettingsRuntimeState.StartingMoney = (AIStartingMoneySetting)GetDropdownValue(aiStartingMoneyDropdown, (int)AISettingsRuntimeState.StartingMoney);
            AISettingsRuntimeState.IncomeMultiplier = AIIncomeMultiplierValues[Mathf.Clamp(GetDropdownValue(aiIncomeMultiplierDropdown, ResolveIncomeMultiplierIndex(AISettingsRuntimeState.IncomeMultiplier)), 0, AIIncomeMultiplierValues.Length - 1)];
            AISettingsRuntimeState.BuildSpeed = (AISpeedSetting)GetDropdownValue(aiBuildSpeedDropdown, (int)AISettingsRuntimeState.BuildSpeed);
            AISettingsRuntimeState.UnitProductionSpeed = (AISpeedSetting)GetDropdownValue(aiUnitProductionSpeedDropdown, (int)AISettingsRuntimeState.UnitProductionSpeed);
            AISettingsRuntimeState.AttackGroupSize = (AIAttackGroupSizeSetting)GetDropdownValue(aiAttackGroupSizeDropdown, (int)AISettingsRuntimeState.AttackGroupSize);
            AISettingsRuntimeState.AttackFrequency = (AIAttackFrequencySetting)GetDropdownValue(aiAttackFrequencyDropdown, (int)AISettingsRuntimeState.AttackFrequency);
            AISettingsRuntimeState.Aggression = (AIAggressionSetting)GetDropdownValue(aiAggressionDropdown, (int)AISettingsRuntimeState.Aggression);
            AISettingsRuntimeState.Expansion = (AIExpansionSetting)GetDropdownValue(aiExpansionDropdown, (int)AISettingsRuntimeState.Expansion);
            AISettingsRuntimeState.TargetPriority = (AITargetPriority)GetDropdownValue(aiTargetPriorityDropdown, (int)AISettingsRuntimeState.TargetPriority);
            AISettingsRuntimeState.PlayerAutoAIEnabled = GetDropdownValue(aiPlayerAutoDropdown, AISettingsRuntimeState.PlayerAutoAIEnabled ? 1 : 0) > 0;
            AISettingsRuntimeState.EnemyAICount = Mathf.Clamp(GetDropdownValue(aiEnemyCountDropdown, Mathf.Clamp(AISettingsRuntimeState.EnemyAICount, 1, 3) - 1) + 1, 1, 3);

            SetPlayerAutoMode(AISettingsRuntimeState.PlayerAutoAIEnabled);
            RefreshAutoModeButton();
            AISettingsRuntimeState.ApplyToWorld(World.DefaultGameObjectInjectionWorld);
        }

        private static int GetDropdownValue(TMP_Dropdown dropdown, int fallback)
        {
            return dropdown != null ? dropdown.value : fallback;
        }

        private static int ResolveIncomeMultiplierIndex(float value)
        {
            int bestIndex = 0;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < AIIncomeMultiplierValues.Length; i++)
            {
                float distance = Mathf.Abs(AIIncomeMultiplierValues[i] - value);
                if (distance >= bestDistance)
                    continue;

                bestIndex = i;
                bestDistance = distance;
            }

            return bestIndex;
        }

        private void SyncSettingsPanel()
        {
            if (panelSettings != null)
                panelSettings.SetActive(_settingsOpen);
        }

        private void SetPlayerAutoMode(bool enabled)
        {
            _runtimeGameplayStateSystem.PlayerAutoModeEnabled = enabled;
            AISettingsRuntimeState.PlayerAutoAIEnabled = enabled;

            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return;

            EntityManager em = world.EntityManager;
            SetPlayerAutoControlEntry(em, enabled);
            SetPlayerAutoPlanStates(em, enabled);
        }

        private static void SetPlayerAutoControlEntry(EntityManager em, bool enabled)
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<FactionControlConfigTag>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            Entity configEntity = entities.Length > 0
                ? entities[0]
                : em.CreateEntity(typeof(FactionControlConfigTag));
            if (!em.HasBuffer<FactionControlEntry>(configEntity))
                em.AddBuffer<FactionControlEntry>(configEntity);

            DynamicBuffer<FactionControlEntry> controls = em.GetBuffer<FactionControlEntry>(configEntity);
            for (int i = 0; i < controls.Length; i++)
            {
                FactionControlEntry control = controls[i];
                if (control.FactionId != 0)
                    continue;

                control.AIControlled = enabled ? (byte)1 : (byte)0;
                control.IsPlayerFaction = 1;
                control.LastLogTime = -999f;
                controls[i] = control;
                return;
            }

            controls.Add(new FactionControlEntry
            {
                FactionId = 0,
                AIControlled = enabled ? (byte)1 : (byte)0,
                IsPlayerFaction = 1,
                LastLogTime = -999f
            });
        }

        private static void SetPlayerAutoPlanStates(EntityManager em, bool enabled)
        {
            byte enabledValue = enabled ? (byte)1 : (byte)0;
            using (EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<AIBuildPlan>()))
            {
                using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity entity = entities[i];
                    AIBuildPlan plan = em.GetComponentData<AIBuildPlan>(entity);
                    if (plan.FactionId != 0)
                        continue;

                    plan.Enabled = enabledValue;
                    plan.LastBuildTime = enabled ? -999f : plan.LastBuildTime;
                    plan.LastLogTime = -999f;
                    em.SetComponentData(entity, plan);
                }
            }

            using (EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<AIProductionPlan>()))
            {
                using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity entity = entities[i];
                    AIProductionPlan plan = em.GetComponentData<AIProductionPlan>(entity);
                    if (plan.FactionId != 0)
                        continue;

                    plan.Enabled = enabledValue;
                    plan.LastProductionTime = enabled ? -999f : plan.LastProductionTime;
                    plan.LastLogTime = -999f;
                    em.SetComponentData(entity, plan);
                }
            }

            using (EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<AISquadPlan>()))
            {
                using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity entity = entities[i];
                    AISquadPlan plan = em.GetComponentData<AISquadPlan>(entity);
                    if (plan.FactionId != 0)
                        continue;

                    plan.Enabled = enabledValue;
                    plan.LastLogTime = -999f;
                    em.SetComponentData(entity, plan);
                }
            }

            using (EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<FactionEconomy>(), ComponentType.ReadWrite<FactionEconomyPolicy>()))
            {
                using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity entity = entities[i];
                    FactionEconomy economy = em.GetComponentData<FactionEconomy>(entity);
                    if (economy.FactionId != 0)
                        continue;

                    FactionEconomyPolicy policy = em.GetComponentData<FactionEconomyPolicy>(entity);
                    policy.Enabled = enabledValue;
                    em.SetComponentData(entity, policy);
                }
            }
        }

        private void ResolveRuntimeLogPanel()
        {
            Transform root = transform;
            if (panelLog == null)
            {
                Transform panelLogTransform = FindDescendantByName(root, "Panel_Log");
                panelLog = panelLogTransform != null ? panelLogTransform.gameObject : null;
            }

            if (logText == null && panelLog != null)
                logText = FindTextByName(panelLog.transform, "Label_Log");

            ResolveRuntimeLogScrollReferences();

            if (logText != null)
            {
                logText.richText = true;
                ConfigureRuntimeLogLayout();
                RefreshRuntimeLogLabel(false);
            }

            if (panelLog != null)
                panelLog.SetActive(false);

            BindFpsLogToggle();
        }

        private void SubscribeRuntimeLog()
        {
            if (_runtimeLogSubscribed)
            {
                ReplayBufferedRuntimeLogs();
                return;
            }

            Application.logMessageReceived += HandleRuntimeLogMessage;
            _runtimeLogSubscribed = true;
            ReplayBufferedRuntimeLogs();
        }

        private void UnsubscribeRuntimeLog()
        {
            if (!_runtimeLogSubscribed)
                return;

            Application.logMessageReceived -= HandleRuntimeLogMessage;
            _runtimeLogSubscribed = false;
        }

        private void HandleRuntimeLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Log && string.IsNullOrWhiteSpace(condition))
                return;

            AddRuntimeLogEntry(BuildRuntimeLogMessage(condition, stackTrace, type), type, true);
        }

        private void ReplayBufferedRuntimeLogs()
        {
            if (_runtimeLogBufferReplayed)
                return;

            _runtimeLogBufferReplayed = true;
            IReadOnlyList<RuntimeLogBuffer.Entry> snapshot = RuntimeLogBuffer.Snapshot();
            if (snapshot == null || snapshot.Count == 0)
                return;

            bool shouldScrollToBottom = IsRuntimeLogScrolledToBottom();
            foreach (RuntimeLogBuffer.Entry entry in snapshot)
            {
                if (entry.Type == LogType.Log && string.IsNullOrWhiteSpace(entry.Condition))
                    continue;

                AddRuntimeLogEntry(BuildRuntimeLogMessage(entry.Condition, entry.StackTrace, entry.Type), entry.Type, false);
            }

            RefreshRuntimeLogLabel(shouldScrollToBottom);
        }

        private void AddRuntimeLogEntry(string message, LogType type, bool refresh)
        {
            if (_runtimeLogEntries.Count >= MaxVisibleLogEntries)
                _runtimeLogEntries.Dequeue();

            bool shouldScrollToBottom = refresh && IsRuntimeLogScrolledToBottom();
            _runtimeLogEntries.Enqueue(new RuntimeLogEntry(message ?? string.Empty, type));
            if (refresh)
                RefreshRuntimeLogLabel(shouldScrollToBottom);
        }

        private static string BuildRuntimeLogMessage(string condition, string stackTrace, LogType type)
        {
            string message = condition ?? string.Empty;
            bool includeStackTrace = type == LogType.Error || type == LogType.Exception || type == LogType.Assert;
            if (!includeStackTrace || string.IsNullOrWhiteSpace(stackTrace))
                return message;

            if (string.IsNullOrWhiteSpace(message))
                return stackTrace;

            return message + "\n" + stackTrace;
        }

        private void RefreshRuntimeLogLabel(bool scrollToBottom)
        {
            if (logText == null)
                return;

            _runtimeLogBuilder.Clear();
            foreach (RuntimeLogEntry entry in _runtimeLogEntries)
            {
                if (_runtimeLogBuilder.Length > 0)
                    _runtimeLogBuilder.Append('\n').Append('\n');

                string color = GetLogColor(entry.Type);
                if (!string.IsNullOrEmpty(color))
                    _runtimeLogBuilder.Append("<color=").Append(color).Append('>');

                _runtimeLogBuilder
                    .Append('[')
                    .Append(entry.Type)
                    .Append("] ")
                    .Append(EscapeRichText(entry.Message));

                if (!string.IsNullOrEmpty(color))
                    _runtimeLogBuilder.Append("</color>");
            }

            logText.text = _runtimeLogBuilder.ToString();
            ResizeRuntimeLogContent();
            if (scrollToBottom)
                ScrollRuntimeLogToBottom();
        }

        private void BindFpsLogToggle()
        {
            Transform fpsPanel = FindDescendantByName(transform, "Panel_FPS");
            if (fpsPanel == null)
                return;

            _fpsLogToggleTrigger = fpsPanel.GetComponent<EventTrigger>();
            if (_fpsLogToggleTrigger == null)
                _fpsLogToggleTrigger = fpsPanel.gameObject.AddComponent<EventTrigger>();

            if (_fpsLogToggleEntry != null && _fpsLogToggleTrigger.triggers.Contains(_fpsLogToggleEntry))
                return;

            _fpsLogToggleEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            _fpsLogToggleEntry.callback.AddListener(_ => ToggleRuntimeLogPanel());
            _fpsLogToggleTrigger.triggers.Add(_fpsLogToggleEntry);
        }

        private void ToggleRuntimeLogPanel()
        {
            ResolveRuntimeLogPanelReferencesOnly();
            if (panelLog == null)
                return;

            panelLog.SetActive(!panelLog.activeSelf);
            RefreshRuntimeLogLabel(panelLog.activeSelf);
            if (Application.isPlaying)
                SuppressNextWorldClick();
        }

        private void ResolveRuntimeLogPanelReferencesOnly()
        {
            if (panelLog == null)
            {
                Transform panelLogTransform = FindDescendantByName(transform, "Panel_Log");
                panelLog = panelLogTransform != null ? panelLogTransform.gameObject : null;
            }

            if (logText == null && panelLog != null)
            {
                logText = FindTextByName(panelLog.transform, "Label_Log");
                if (logText != null)
                    logText.richText = true;
            }

            ResolveRuntimeLogScrollReferences();
            ConfigureRuntimeLogLayout();
        }

        private void ResolveRuntimeLogScrollReferences()
        {
            if (panelLog == null)
                return;

            if (logScrollRect == null)
                logScrollRect = panelLog.GetComponentInChildren<ScrollRect>(true);

            if (logScrollRect != null)
                _logContentRect = logScrollRect.content;

            if (logText != null)
                _logTextRect = logText.rectTransform;
        }

        private void ConfigureRuntimeLogLayout()
        {
            if (logText == null)
                return;

            logText.richText = true;
            logText.textWrappingMode = TextWrappingModes.Normal;
            logText.alignment = TextAlignmentOptions.TopLeft;
            logText.overflowMode = TextOverflowModes.Overflow;

            if (logScrollRect != null)
            {
                logScrollRect.horizontal = false;
                logScrollRect.vertical = true;
                logScrollRect.movementType = ScrollRect.MovementType.Clamped;
            }

            if (_logContentRect != null)
            {
                ContentSizeFitter fitter = _logContentRect.GetComponent<ContentSizeFitter>();
                if (fitter != null)
                    fitter.enabled = false;

                _logContentRect.anchorMin = new Vector2(0f, 1f);
                _logContentRect.anchorMax = new Vector2(1f, 1f);
                _logContentRect.pivot = new Vector2(0.5f, 1f);
                _logContentRect.anchoredPosition = Vector2.zero;
            }

            if (_logTextRect != null)
            {
                _logTextRect.anchorMin = new Vector2(0f, 1f);
                _logTextRect.anchorMax = new Vector2(1f, 1f);
                _logTextRect.pivot = new Vector2(0f, 1f);
                _logTextRect.anchoredPosition = Vector2.zero;
            }
        }

        private void ResizeRuntimeLogContent()
        {
            if (logText == null)
                return;

            ResolveRuntimeLogScrollReferences();
            ConfigureRuntimeLogLayout();

            RectTransform viewport = logScrollRect != null && logScrollRect.viewport != null
                ? logScrollRect.viewport
                : null;
            float viewportWidth = viewport != null ? viewport.rect.width : 0f;
            float viewportHeight = viewport != null ? viewport.rect.height : 0f;
            float textWidth = _logTextRect != null && _logTextRect.rect.width > 1f
                ? _logTextRect.rect.width
                : Mathf.Max(1f, viewportWidth - 24f);

            Vector2 preferred = logText.GetPreferredValues(logText.text, textWidth, 0f);
            float textHeight = Mathf.Max(preferred.y, viewportHeight);
            float contentHeight = Mathf.Max(viewportHeight, textHeight + 24f);

            if (_logContentRect != null)
                _logContentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
            if (_logTextRect != null)
                _logTextRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textHeight);

            LayoutRebuilder.ForceRebuildLayoutImmediate(_logContentRect != null ? _logContentRect : _logTextRect);
        }

        private bool IsRuntimeLogScrolledToBottom()
        {
            ResolveRuntimeLogScrollReferences();
            if (logScrollRect == null || logScrollRect.content == null)
                return true;

            RectTransform viewport = logScrollRect.viewport != null
                ? logScrollRect.viewport
                : logScrollRect.GetComponent<RectTransform>();
            if (viewport == null || logScrollRect.content.rect.height <= viewport.rect.height + 1f)
                return true;

            return logScrollRect.verticalNormalizedPosition <= 0.02f;
        }

        private void ScrollRuntimeLogToBottom()
        {
            if (logScrollRect == null)
                return;

            Canvas.ForceUpdateCanvases();
            logScrollRect.StopMovement();
            logScrollRect.verticalNormalizedPosition = 0f;
            Canvas.ForceUpdateCanvases();
        }

        private static string GetLogColor(LogType type)
        {
            return type switch
            {
                LogType.Warning => "#FFA500",
                LogType.Error or LogType.Assert or LogType.Exception => "#FF4040",
                _ => null
            };
        }

        private static string EscapeRichText(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }

        private void ResolveTimePanel()
        {
            Transform root = transform;
            Transform timePanelRoot = panelTime != null ? panelTime.transform : FindDescendantByName(root, "Panel_Time");
            if (timePanelRoot == null)
                return;

            if (dateText == null)
                dateText = FindTextByName(timePanelRoot, "DateText");
            if (timeText == null)
                timeText = FindTextByName(timePanelRoot, "TimeText");
        }

        private void ResolveRequestPanel()
        {
            if (_requestPanelRoot != null && _requestCountdownTemplate != null)
                return;

            if (_requestPanelRoot == null)
            {
                Transform root = transform;
                Transform request = FindDescendantByName(root, "Panel_Request");
                _requestPanelRoot = request as RectTransform;
            }

            if (_requestPanelRoot == null || _requestCountdownTemplate != null)
                return;

            for (int i = 0; i < _requestPanelRoot.childCount; i++)
            {
                Transform child = _requestPanelRoot.GetChild(i);
                if (child.GetComponent<SampleCountdownLabel>() != null)
                {
                    _requestCountdownTemplate = child.gameObject;
                    _requestCountdownTemplate.SetActive(false);
                    break;
                }
            }
        }

        private RequestCountdownView CreateRequestCountdownView()
        {
            if (_requestCountdownTemplate == null || _requestPanelRoot == null)
                return null;

            GameObject instance = Object.Instantiate(_requestCountdownTemplate, _requestPanelRoot);
            instance.name = $"{_requestCountdownTemplate.name}_Runtime";
            RequestCountdownView view = new RequestCountdownView
            {
                Root = instance,
                Countdown = instance.GetComponent<SampleCountdownLabel>()
            };

            Transform content = instance.transform.Find("Content");
            Transform background = content != null ? content.Find("Background") : null;
            view.Portrait = FindImageByName(background, "SPR_Portrait");
            Transform timeRoot = content != null ? content.Find("Time") : null;
            view.TimeLabel = FindTextByName(timeRoot, "Label_Time");

            if (content != null)
            {
                Transform dialHealthy = content.Find("Dial/Dial_Healthy");
                Transform dialLow = content.Find("Dial/Dial_Low");
                view.DialHealthy = dialHealthy != null ? dialHealthy.gameObject : null;
                view.DialLow = dialLow != null ? dialLow.gameObject : null;
                CollectDescendantImagesByName(dialHealthy, "Fill", view.HealthyFillImages);
                CollectDescendantImagesByName(dialLow, "Fill", view.LowFillImages);
            }

            if (view.Countdown != null)
            {
                // The sample countdown animation rotates independently and does not match
                // gameplay production timing, so runtime queue items are driven manually.
                view.Countdown.enabled = false;
                if (view.Countdown.myAnimator != null)
                    view.Countdown.myAnimator.enabled = false;
            }

            instance.SetActive(true);
            return view;
        }

        private void BindRequestCountdownView(RequestCountdownView view, BuildingUiQuerySystem.PendingProductionUiEntry entry)
        {
            if (view == null || view.Root == null)
                return;

            if (view.Portrait != null)
                view.Portrait.sprite = GetCampPreviewSprite(entry.Prefab);

            if (view.Countdown != null)
            {
                view.Countdown.initialDelay = 0f;
                view.Countdown.countdownTime = entry.DurationSeconds;
                view.Countdown.updateInterval = 0.1f;
                view.Countdown.timerFormat = "F0";
                if (CountdownCurrentTimeField != null)
                    CountdownCurrentTimeField.SetValue(view.Countdown, entry.RemainingSeconds);
            }

            if (view.TimeLabel != null)
            {
                if (entry.RemainingSeconds <= 0f)
                {
                    if (view.ZeroReachedAt <= 0f)
                        view.ZeroReachedAt = Time.unscaledTime;

                    bool keepVisible = (Time.unscaledTime - view.ZeroReachedAt) < 1f;
                    view.TimeLabel.gameObject.SetActive(keepVisible);
                    if (keepVisible)
                        view.TimeLabel.SetText("0");
                }
                else
                {
                    view.ZeroReachedAt = 0f;
                    view.TimeLabel.gameObject.SetActive(true);
                    view.TimeLabel.SetText(Mathf.CeilToInt(entry.RemainingSeconds).ToString("0"));
                }
            }

            if (view.DialHealthy != null)
                view.DialHealthy.SetActive(true);
            if (view.DialLow != null)
                view.DialLow.SetActive(true);

            float fillAmount = Mathf.Clamp01(entry.Progress01);
            for (int i = 0; i < view.HealthyFillImages.Count; i++)
            {
                if (view.HealthyFillImages[i] != null)
                {
                    view.HealthyFillImages[i].fillClockwise = false;
                    view.HealthyFillImages[i].fillAmount = fillAmount;
                }
            }

            float lowFillAmount = 1f - Mathf.Clamp01(entry.Progress01);
            for (int i = 0; i < view.LowFillImages.Count; i++)
            {
                if (view.LowFillImages[i] != null)
                {
                    view.LowFillImages[i].fillClockwise = false;
                    view.LowFillImages[i].fillAmount = lowFillAmount;
                }
            }

            if (!view.Root.activeSelf)
                view.Root.SetActive(true);
        }

        private void ClearObsoleteRequestCountdownViews(HashSet<string> activeKeys)
        {
            List<string> keysToRemove = null;
            foreach (KeyValuePair<string, RequestCountdownView> pair in _requestCountdownViews)
            {
                if (activeKeys.Contains(pair.Key))
                    continue;

                if (pair.Value?.Root != null)
                    Object.Destroy(pair.Value.Root);
                keysToRemove ??= new List<string>();
                keysToRemove.Add(pair.Key);
            }

            if (keysToRemove == null)
                return;

            for (int i = 0; i < keysToRemove.Count; i++)
                _requestCountdownViews.Remove(keysToRemove[i]);
        }

        private static string BuildPendingProductionKey(BuildingUiQuerySystem.PendingProductionUiEntry entry)
        {
            string prefabKey = entry.Prefab != null ? entry.Prefab.name : "null";
            return $"{entry.BuildingId}:{prefabKey}:{entry.StartedAt:F3}:{entry.ReadyAt:F3}";
        }

        private void EnsureStatsBindings()
        {
            if (_statsLayoutRoot == null)
                ResolveStatsPanel();
            if (_statsLayoutRoot == null || _statsAmountTexts.Count > 0)
                return;

            for (int i = 0; i < _statsLayoutRoot.childCount; i++)
            {
                Transform row = _statsLayoutRoot.GetChild(i);
                TMP_Text amountText = FindStatsRowAmountText(row);
                if (amountText != null)
                    _statsAmountTexts[row.name] = amountText;
            }
        }

        private void ResolveStatsPanel()
        {
            Transform root = transform;
            Transform statsPanelRoot = panelStats != null ? panelStats.transform : FindDescendantByName(root, "Panel_Stats");
            if (statsPanelRoot == null)
                return;

            Transform content = FindDescendantByName(statsPanelRoot, "Content");
            _statsLayoutRoot = content != null ? FindDescendantByName(content, "Panel_Layout") : null;
        }

        private static TMP_Text FindStatsRowAmountText(Transform row)
        {
            if (row == null)
                return null;

            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);
            if (texts == null || texts.Length == 0)
                return null;

            for (int i = 0; i < texts.Length; i++)
            {
                string text = texts[i] != null ? texts[i].text : string.Empty;
                if (int.TryParse(text, out _))
                    return texts[i];
            }

            return texts[texts.Length - 1];
        }

        private static int ResolveStatsAmount(string rowName, GameRuntimeStats.Snapshot snapshot, int civilianDead)
        {
            return rowName switch
            {
                "Panel_Civilian_Dead" => civilianDead,
                "Panel_Own_Soldiers_Dead" => snapshot.OwnSoldiersDead,
                "Panel_Enemy_Soldiers_Dead" => snapshot.EnemySoldiersDead,
                "Panel_Oil_Extracted" => snapshot.OilExtracted,
                "Panel_Fuel_Produced" => snapshot.FuelProduced,
                "Panel_Vehicles_Ordered" => snapshot.VehiclesOrdered,
                "Panel_Soldiers_Ordered" => snapshot.SoldiersOrdered,
                "Panel_Ammo_Ordered" => snapshot.AmmoOrdered,
                "Panel_Buildings_Built" => snapshot.BuildingsBuilt,
                _ => 0
            };
        }

        private bool HasModalPanelOpen()
        {
            return _confirmOpen || _warningOpen || _settingsOpen;
        }

        private void OnDestroy()
        {
            if (_selectionSystem != null)
            {
                _selectionSystem.MoveOrderScreenMarkerRequested -= ShowMoveOrderScreenReticle;
                _selectionSystem.AttackOrderScreenMarkerRequested -= ShowAttackOrderScreenReticle;
                _selectionSystem.OrderScreenMarkersHideRequested -= HideOrderScreenReticles;
            }
            if (_buttonAutoMode != null)
                _buttonAutoMode.onClick.RemoveListener(ButtonAutoModeClicked);
            if (_singleSelectionAttackButtonComponent != null)
                _singleSelectionAttackButtonComponent.onClick.RemoveListener(ButtonAttackClicked);
            if (buttonSettings != null)
                buttonSettings.onClick.RemoveListener(ButtonSettingsClicked);
            if (_fpsLogToggleTrigger != null && _fpsLogToggleEntry != null)
                _fpsLogToggleTrigger.triggers.Remove(_fpsLogToggleEntry);
            UnsubscribeRuntimeLog();
            RemoveAISettingsDropdownListeners();

            foreach (KeyValuePair<string, RequestCountdownView> pair in _requestCountdownViews)
            {
                if (pair.Value?.Root != null)
                    Object.Destroy(pair.Value.Root);
            }
            _requestCountdownViews.Clear();

            if (_minimapSprite != null)
                Object.Destroy(_minimapSprite);
            if (_minimapTexture != null)
                Object.Destroy(_minimapTexture);
            ClearCampSelectedModelPreview();
            if (_campSelectedModelPreviewCamera != null)
                Object.Destroy(_campSelectedModelPreviewCamera.gameObject);
            if (_campSelectedModelPreviewTexture != null)
                Object.Destroy(_campSelectedModelPreviewTexture);
            ThreatWarningRuntimeState.Reset();
        }

        private void SuppressNextWorldClick()
        {
            _runtimeGameplayStateSystem.SuppressNextWorldClick = true;
            StopCoroutine(nameof(ClearWorldClickSuppressionAtEndOfFrame));
            StartCoroutine(nameof(ClearWorldClickSuppressionAtEndOfFrame));
        }

        private IEnumerator ClearWorldClickSuppressionAtEndOfFrame()
        {
            yield return null;
            _runtimeGameplayStateSystem.SuppressNextWorldClick = false;
        }

        private IEnumerator SelectCurrentCampTabNextFrame()
        {
            yield return null;

            UpdateCampTabSelection();

            Button targetButton = GetCurrentCampTabButton();
            if (targetButton == null)
                yield break;

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null)
                eventSystem.SetSelectedGameObject(targetButton.gameObject);
            else
                targetButton.Select();
        }

        private Button GetCurrentCampTabButton()
        {
            return _campMenuType switch
            {
                CampMenuType.Ammo => buttonCampAmmo,
                CampMenuType.Soldiers => buttonCampSoldiers,
                CampMenuType.Vehicles => buttonCampVehicles,
                CampMenuType.Buildings => buttonCampBuildings,
                _ => null
            };
        }

        private void RemoveAISettingsDropdownListeners()
        {
            RemoveAISettingsDropdownListener(aiDifficultyDropdown);
            RemoveAISettingsDropdownListener(aiStartingMoneyDropdown);
            RemoveAISettingsDropdownListener(aiIncomeMultiplierDropdown);
            RemoveAISettingsDropdownListener(aiBuildSpeedDropdown);
            RemoveAISettingsDropdownListener(aiUnitProductionSpeedDropdown);
            RemoveAISettingsDropdownListener(aiAttackGroupSizeDropdown);
            RemoveAISettingsDropdownListener(aiAttackFrequencyDropdown);
            RemoveAISettingsDropdownListener(aiAggressionDropdown);
            RemoveAISettingsDropdownListener(aiExpansionDropdown);
            RemoveAISettingsDropdownListener(aiTargetPriorityDropdown);
            RemoveAISettingsDropdownListener(aiPlayerAutoDropdown);
            RemoveAISettingsDropdownListener(aiEnemyCountDropdown);
        }

        private void RemoveAISettingsDropdownListener(TMP_Dropdown dropdown)
        {
            if (dropdown != null)
                dropdown.onValueChanged.RemoveListener(AISettingsDropdownChanged);
        }
    }
}
