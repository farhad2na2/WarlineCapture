using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Game.Components;
using Game.Configs;

namespace Game.Authoring
{
    [DisallowMultipleComponent]
    public partial class UnitGridAuthoring : MonoBehaviour
    {
        [SerializeField] private UnitGridAuthoringConfig config;
        [Header("Visual Roots")]
        [SerializeField] private Transform modelRoot;
        [SerializeField] private Transform destroyedRoot;
        [SerializeField, HideInInspector] private bool allowIdleWander = true;
        [SerializeField, HideInInspector] private bool autoCalculateFootprint;
        [SerializeField, HideInInspector] private Vector2Int footprintCells = new(1, 1);
        [SerializeField, HideInInspector] private bool usesVehicleMotion;
        [SerializeField, HideInInspector] private bool isAirUnit;
        [SerializeField, HideInInspector] private bool canRequest = true;
        [SerializeField, HideInInspector, Min(0)] private int price;
        [SerializeField, HideInInspector, Min(0)] private int materialsCost;
        [SerializeField, HideInInspector, Min(0.01f)] private float productionDurationSeconds = 60f;
        [SerializeField, HideInInspector] private GameObject productionTransportPrefab;
        [SerializeField, HideInInspector] private bool isProductionTransportUnit;
        [SerializeField, HideInInspector, Min(0.01f)] private float productionTransportArrivalSeconds = 5f;
        [SerializeField, HideInInspector, Min(0.01f)] private float productionTransportHoldForNextReadySeconds = 4f;
        [SerializeField, HideInInspector, Min(1)] private int productionTransportMaxConcurrent = 1;
        [SerializeField, HideInInspector] private bool productionTransportRequiresAirportRunway;
        [SerializeField, HideInInspector] private bool productionTransportUsesRunwayLanding;
        [SerializeField, HideInInspector, Min(0)] private int soldierTransportCapacity;
        [SerializeField, HideInInspector, Min(0)] private int vehicleTransportCapacity;
        [SerializeField, HideInInspector, Min(0)] private int cargoWeightCapacity;
        [SerializeField, HideInInspector, Min(0f)] private float transportCruiseHeight;
        [SerializeField, HideInInspector] private GameObject soldierParachuteVisualPrefab;
        [SerializeField, HideInInspector] private GameObject vehicleEmergencyDropVisualPrefab;
        [SerializeField, HideInInspector, Min(0.01f)] private float runwayTaxiSpeed = 5f;
        [SerializeField, HideInInspector, Min(0.01f)] private float speed = 5f;
        [SerializeField, HideInInspector, Min(0.01f)] private float walkSpeed = 2f;
        [SerializeField, HideInInspector, Min(1f)] private float roadSpeedMultiplier = 1.2f;
        [SerializeField, HideInInspector, Min(0.001f)] private float arriveDistance = 0.05f;
        [SerializeField, HideInInspector] private float groundOffset;
        [SerializeField, HideInInspector, Min(0f)] private float groundFuelPerCell;
        [SerializeField, HideInInspector, Min(0f)] private float airFuelPerCell;
        [SerializeField, HideInInspector, Min(0)] private int resourceHaulerBarrelCapacity;
        [SerializeField, HideInInspector, Min(0.01f)] private float resourceHaulerFillDurationSeconds = 2f;
        [SerializeField, HideInInspector, Min(0.01f)] private float resourceHaulerUnloadDurationSeconds = 1.5f;
        [SerializeField, HideInInspector] private ThreatDetectionKind threatDetectionKind;
        [SerializeField, HideInInspector, Min(0)] private int threatDetectionRadiusCells;
        [SerializeField, HideInInspector] private string displayName;
        [SerializeField, HideInInspector, TextArea] private string description;
        [SerializeField, HideInInspector] private Sprite portraitSprite;
        [SerializeField, HideInInspector] private Sprite portraitCardSprite;
        [SerializeField, HideInInspector] private Sprite portraitActionSprite;
        [SerializeField, HideInInspector] private string weaponDisplayName;
        [SerializeField, HideInInspector] private GameObject unitSelectionMarkerPrefab;
        [SerializeField, HideInInspector] private GameObject unitHealthBarPrefab;
        [SerializeField, HideInInspector] private bool tintUnitModelRenderers = true;
        [SerializeField, HideInInspector] private GameObject vehicleDestroyedVisualPrefab;
        [SerializeField, HideInInspector] private GameObject vehicleSelectionMarkerPrefab;
        [SerializeField, HideInInspector] private GameObject vehicleHealthBarPrefab;
        [SerializeField, HideInInspector] private bool tintVehicleModelRenderers = true;

        [Header("Combat")]
        [SerializeField, HideInInspector] private bool canAttack = true;
        [SerializeField, HideInInspector] private bool allowAutoEngage = true;
        [SerializeField, HideInInspector] private bool usesTurretAim;
        [SerializeField, HideInInspector, Min(0)] private int aggroRangeCells = 6;
        [SerializeField, HideInInspector, Min(0f)] private float attackRange = 2f;
        [SerializeField, HideInInspector, Min(0f)] private float chaseBreakDistance = 8f;
        [SerializeField, HideInInspector, Min(0.01f)] private float attackCooldownSeconds = 1f;
        [SerializeField, HideInInspector, Min(0)] private int attackDamage = 10;
        [SerializeField, HideInInspector, Min(1)] private int maxHealth = 100;
        [SerializeField, HideInInspector] private GameObject attackImpactPrefab;
        [SerializeField, HideInInspector] private GameObject muzzleFlashPrefab;
        [SerializeField, HideInInspector, Min(0f)] private float muzzleFlashHeightOffset = 0.9f;
        [SerializeField, HideInInspector, Min(0f)] private float muzzleFlashForwardOffset = 0.45f;
        [SerializeField, HideInInspector] private GroundMissileLauncherConfig groundMissileLauncherConfig;
        [SerializeField, HideInInspector] private Transform groundMissileLauncherBattery;
        [SerializeField, HideInInspector] private Transform groundMissileLauncherSmokeSpawn;
        [SerializeField, HideInInspector] private List<Transform> groundMissileLauncherRockets = new();
        [SerializeField, HideInInspector] private AirMissileLauncherConfig airMissileLauncherConfig;
        [SerializeField, HideInInspector] private Transform airMissileLauncherTurret;
        [SerializeField, HideInInspector] private Transform airMissileLauncherLaunchSpawn;
        [SerializeField, HideInInspector] private List<Transform> airMissileLauncherMissiles = new();
        [SerializeField, HideInInspector] private Color attackTraceColor = new(1f, 0.85f, 0.2f, 1f);
        [Header("Attack Trace")]
        [SerializeField, HideInInspector, Min(0.01f)] private float attackTraceWidth = 0.18f;
        [SerializeField, HideInInspector, Min(0.1f)] private float attackTraceScrollSpeed = 10f;
        [SerializeField, HideInInspector, Min(1f)] private float attackTraceDashDensity = 10f;
        [SerializeField, HideInInspector, Min(0.01f)] private float attackTraceVisibleSeconds = 0.08f;
        [SerializeField, HideInInspector, Min(1)] private int attackTracerEveryNthShot = 1;
        [Header("Animation")]
        [SerializeField, HideInInspector, Min(0f)] private float idleDelayMinSeconds = 5f;
        [SerializeField, HideInInspector, Min(0f)] private float idleDelayMaxSeconds = 7f;
        [SerializeField, HideInInspector, Min(0f)] private float idleWanderDistanceMin = 3f;
        [SerializeField, HideInInspector, Min(0f)] private float idleWanderDistanceMax = 5f;
        [SerializeField, HideInInspector, Min(0.01f)] private float attackAnimationSeconds = 0.25f;
        [SerializeField, HideInInspector, Min(0.01f)] private float deathAnimationSeconds = 1.25f;
        [SerializeField, HideInInspector] private List<UnitAnimationKind> animationOrder = new();

        private void OnValidate()
        {
            ApplyConfigIfAvailable();
        }

        private void ApplyConfigIfAvailable()
        {
            if (config == null)
                return;

            allowIdleWander = config.AllowIdleWander;
            autoCalculateFootprint = config.AutoCalculateFootprint;
            footprintCells = config.FootprintCells;
            usesVehicleMotion = config.UsesVehicleMotion;
            isAirUnit = config.IsAirUnit;
            canRequest = config.CanRequest;
            price = config.Price;
            materialsCost = config.MaterialsCost;
            productionDurationSeconds = config.ProductionDurationSeconds;
            productionTransportPrefab = config.ProductionTransportPrefab;
            isProductionTransportUnit = config.IsProductionTransportUnit;
            productionTransportArrivalSeconds = config.ProductionTransportArrivalSeconds;
            productionTransportHoldForNextReadySeconds = config.ProductionTransportHoldForNextReadySeconds;
            productionTransportMaxConcurrent = config.ProductionTransportMaxConcurrent;
            productionTransportRequiresAirportRunway = config.ProductionTransportRequiresAirportRunway;
            productionTransportUsesRunwayLanding = config.ProductionTransportUsesRunwayLanding;
            soldierTransportCapacity = config.SoldierTransportCapacity;
            vehicleTransportCapacity = config.VehicleTransportCapacity;
            cargoWeightCapacity = config.CargoWeightCapacity;
            transportCruiseHeight = config.TransportCruiseHeight;
            soldierParachuteVisualPrefab = config.SoldierParachuteVisualPrefab;
            vehicleEmergencyDropVisualPrefab = config.VehicleEmergencyDropVisualPrefab;
            runwayTaxiSpeed = config.RunwayTaxiSpeed;
            speed = config.Speed;
            walkSpeed = config.WalkSpeed;
            roadSpeedMultiplier = config.RoadSpeedMultiplier;
            arriveDistance = config.ArriveDistance;
            groundOffset = config.GroundOffset;
            groundFuelPerCell = config.GroundFuelPerCell;
            airFuelPerCell = config.AirFuelPerCell;
            displayName = config.DisplayName;
            description = config.Description;
            portraitSprite = config.PortraitSprite;
            portraitCardSprite = config.PortraitCardSprite;
            portraitActionSprite = config.PortraitActionSprite;
            weaponDisplayName = config.WeaponDisplayName;
            unitSelectionMarkerPrefab = config.UnitSelectionMarkerPrefab;
            unitHealthBarPrefab = config.UnitHealthBarPrefab;
            tintUnitModelRenderers = config.TintUnitModelRenderers;
            vehicleDestroyedVisualPrefab = config.VehicleDestroyedVisualPrefab;
            vehicleSelectionMarkerPrefab = config.VehicleSelectionMarkerPrefab;
            vehicleHealthBarPrefab = config.VehicleHealthBarPrefab;
            tintVehicleModelRenderers = config.TintVehicleModelRenderers;
            resourceHaulerBarrelCapacity = config.ResourceHaulerBarrelCapacity;
            resourceHaulerFillDurationSeconds = config.ResourceHaulerFillDurationSeconds;
            resourceHaulerUnloadDurationSeconds = config.ResourceHaulerUnloadDurationSeconds;
            threatDetectionKind = config.ThreatDetectionKind;
            threatDetectionRadiusCells = config.ThreatDetectionRadiusCells;
            canAttack = config.CanAttack;
            allowAutoEngage = config.AllowAutoEngage;
            usesTurretAim = config.UsesTurretAim;
            aggroRangeCells = config.AggroRangeCells;
            attackRange = config.AttackRange;
            chaseBreakDistance = config.ChaseBreakDistance;
            attackCooldownSeconds = config.AttackCooldownSeconds;
            attackDamage = config.AttackDamage;
            maxHealth = config.MaxHealth;
            attackImpactPrefab = config.AttackImpactPrefab;
            muzzleFlashPrefab = config.MuzzleFlashPrefab;
            muzzleFlashHeightOffset = config.MuzzleFlashHeightOffset;
            muzzleFlashForwardOffset = config.MuzzleFlashForwardOffset;
            groundMissileLauncherConfig = config.GroundMissileLauncherConfig;
            airMissileLauncherConfig = config.AirMissileLauncherConfig;
            attackTraceColor = config.AttackTraceColor;
            attackTraceWidth = config.AttackTraceWidth;
            attackTraceScrollSpeed = config.AttackTraceScrollSpeed;
            attackTraceDashDensity = config.AttackTraceDashDensity;
            attackTraceVisibleSeconds = config.AttackTraceVisibleSeconds;
            attackTracerEveryNthShot = config.AttackTracerEveryNthShot;
            idleDelayMinSeconds = config.IdleDelayMinSeconds;
            idleDelayMaxSeconds = config.IdleDelayMaxSeconds;
            idleWanderDistanceMin = config.IdleWanderDistanceMin;
            idleWanderDistanceMax = config.IdleWanderDistanceMax;
            attackAnimationSeconds = config.AttackAnimationSeconds;
            deathAnimationSeconds = config.DeathAnimationSeconds;
            animationOrder = config.AnimationOrder != null ? new List<UnitAnimationKind>(config.AnimationOrder) : new List<UnitAnimationKind>();
        }

        public float ProductionDurationSeconds => Mathf.Max(0.01f, productionDurationSeconds);
        public GameObject ProductionTransportPrefab => productionTransportPrefab;
        public bool IsProductionTransportUnit => isProductionTransportUnit;
        public float ProductionTransportArrivalSeconds => Mathf.Max(0.01f, productionTransportArrivalSeconds);
        public float ProductionTransportHoldForNextReadySeconds => Mathf.Max(0.01f, productionTransportHoldForNextReadySeconds);
        public int ProductionTransportMaxConcurrent => Mathf.Max(1, productionTransportMaxConcurrent);
        public bool ProductionTransportRequiresAirportRunway => config != null ? config.ProductionTransportRequiresAirportRunway : productionTransportRequiresAirportRunway;
        public bool ProductionTransportUsesRunwayLanding => config != null ? config.ProductionTransportUsesRunwayLanding : productionTransportUsesRunwayLanding;
        public int SoldierTransportCapacity => Mathf.Max(0, config != null ? config.SoldierTransportCapacity : soldierTransportCapacity);
        public int VehicleTransportCapacity => Mathf.Max(0, config != null ? config.VehicleTransportCapacity : vehicleTransportCapacity);
        public int CargoWeightCapacity => Mathf.Max(0, config != null ? config.CargoWeightCapacity : cargoWeightCapacity);
        public float TransportCruiseHeight => Mathf.Max(0f, config != null ? config.TransportCruiseHeight : transportCruiseHeight);
        public GameObject SoldierParachuteVisualPrefab => config != null ? config.SoldierParachuteVisualPrefab : soldierParachuteVisualPrefab;
        public GameObject VehicleEmergencyDropVisualPrefab => config != null ? config.VehicleEmergencyDropVisualPrefab : vehicleEmergencyDropVisualPrefab;
        public bool UsesVehicleMotion => config != null ? config.UsesVehicleMotion : usesVehicleMotion;
        public bool IsAirUnit => config != null ? config.IsAirUnit : isAirUnit;
        public float RunwayTaxiSpeed => Mathf.Max(0.01f, config != null ? config.RunwayTaxiSpeed : runwayTaxiSpeed);
        public bool CanRequest => canRequest;
        public int Price => Mathf.Max(0, price);
        public int MaterialsCost => Mathf.Max(0, config != null
            ? config.MaterialsCost
            : materialsCost > 0
                ? materialsCost
                : UnitGridAuthoringConfig.ResolveLegacyMaterialsCost(price));
        public bool ConfiguredAllowIdleWander => config != null ? config.AllowIdleWander : allowIdleWander;
        public float ConfiguredSpeed => Mathf.Max(0f, config != null ? config.Speed : speed);
        public float GroundFuelPerCell => Mathf.Max(0f, config != null ? config.GroundFuelPerCell : groundFuelPerCell);
        public float AirFuelPerCell => Mathf.Max(0f, config != null ? config.AirFuelPerCell : airFuelPerCell);
        public int ConfiguredResourceHaulerBarrelCapacity => Mathf.Max(0, config != null ? config.ResourceHaulerBarrelCapacity : resourceHaulerBarrelCapacity);
        public bool ConfiguredCanAttack => config != null ? config.CanAttack : canAttack;
        public bool ConfiguredAllowAutoEngage => config != null ? config.AllowAutoEngage : allowAutoEngage;
        public int ConfiguredAggroRangeCells => Mathf.Max(0, config != null ? config.AggroRangeCells : aggroRangeCells);
        public float ConfiguredAttackRange => Mathf.Max(0f, config != null ? config.AttackRange : attackRange);
        public float ConfiguredChaseBreakDistance => Mathf.Max(0f, config != null ? config.ChaseBreakDistance : chaseBreakDistance);
        public float ConfiguredAttackCooldownSeconds => Mathf.Max(0.01f, config != null ? config.AttackCooldownSeconds : attackCooldownSeconds);
        public int ConfiguredAttackDamage => Mathf.Max(0, config != null ? config.AttackDamage : attackDamage);
        public int ConfiguredMaxHealth => Mathf.Max(1, config != null ? config.MaxHealth : maxHealth);
        public float ConfiguredGroundOffset => Mathf.Max(0f, config != null ? config.GroundOffset : groundOffset);
        public GroundMissileLauncherConfig GroundMissileLauncherConfig => config != null ? config.GroundMissileLauncherConfig : groundMissileLauncherConfig;
        public AirMissileLauncherConfig AirMissileLauncherConfig => config != null ? config.AirMissileLauncherConfig : airMissileLauncherConfig;
        public Transform GroundMissileLauncherBattery => groundMissileLauncherBattery;
        public Transform GroundMissileLauncherSmokeSpawn => groundMissileLauncherSmokeSpawn;
        public IReadOnlyList<Transform> GroundMissileLauncherRockets => groundMissileLauncherRockets;
        public Transform AirMissileLauncherTurret => airMissileLauncherTurret;
        public Transform AirMissileLauncherLaunchSpawn => airMissileLauncherLaunchSpawn;
        public IReadOnlyList<Transform> AirMissileLauncherMissiles => airMissileLauncherMissiles;
        public GameObject MidLodPrefab => config != null ? config.MidLodPrefab : null;
        public GameObject LowLodPrefab => config != null ? config.LowLodPrefab : MidLodPrefab;
        public bool UsesTurretAim => config != null ? config.UsesTurretAim : usesTurretAim;
        public GameObject AttackImpactPrefab => config != null ? config.AttackImpactPrefab : attackImpactPrefab;
        public GameObject MuzzleFlashPrefab => config != null ? config.MuzzleFlashPrefab : muzzleFlashPrefab;
        public float MuzzleFlashHeightOffset => Mathf.Max(0f, config != null ? config.MuzzleFlashHeightOffset : muzzleFlashHeightOffset);
        public float MuzzleFlashForwardOffset => Mathf.Max(0f, config != null ? config.MuzzleFlashForwardOffset : muzzleFlashForwardOffset);
        public GameObject UnitSelectionMarkerPrefab => config != null ? config.UnitSelectionMarkerPrefab : unitSelectionMarkerPrefab;
        public GameObject UnitHealthBarPrefab => config != null ? config.UnitHealthBarPrefab : unitHealthBarPrefab;
        public bool TintUnitModelRenderers => config != null ? config.TintUnitModelRenderers : tintUnitModelRenderers;
        public GameObject VehicleDestroyedVisualPrefab => config != null ? config.VehicleDestroyedVisualPrefab : vehicleDestroyedVisualPrefab;
        public GameObject VehicleSelectionMarkerPrefab => config != null ? config.VehicleSelectionMarkerPrefab : vehicleSelectionMarkerPrefab;
        public GameObject VehicleHealthBarPrefab => config != null ? config.VehicleHealthBarPrefab : vehicleHealthBarPrefab;
        public bool TintVehicleModelRenderers => config != null ? config.TintVehicleModelRenderers : tintVehicleModelRenderers;
        public Sprite PortraitSprite => config != null ? config.PortraitSprite : portraitSprite;
        public Sprite PortraitCardSprite => config != null ? config.PortraitCardSprite : portraitCardSprite;
        public Sprite PortraitActionSprite => config != null ? config.PortraitActionSprite : portraitActionSprite;
        public Sprite WeaponSprite => config != null ? config.WeaponSprite : null;
        public string WeaponDisplayName => config != null ? config.WeaponDisplayName : weaponDisplayName;
        public string ConfiguredDisplayName
        {
            get
            {
                if (config != null && !string.IsNullOrWhiteSpace(config.DisplayName))
                    return config.DisplayName;

                return string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
            }
        }

        public string ConfiguredDescription => config != null ? config.Description ?? string.Empty : description ?? string.Empty;
        public IReadOnlyList<UnitAnimationKind> AnimationOrder => animationOrder;
        public Vector2Int GetConfiguredFootprintCells() => footprintCells;

        [BakingVersion("WarlineCapture", 1)]
        private partial class UnitGridBaker : Baker<UnitGridAuthoring>
        {
            public override void Bake(UnitGridAuthoring authoring)
            {
                if (authoring.config != null)
                    DependsOn(authoring.config);

                authoring.ApplyConfigIfAvailable();
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                Bounds modelBounds;
                bool hasModelBounds = TryGetModelLocalBounds(authoring, out modelBounds);
                int2 footprint = ResolveFootprint(authoring, hasModelBounds, modelBounds);
                UnitVehicleMovement vehicleMovement = ResolveVehicleMovement(authoring, footprint, hasModelBounds, modelBounds);

                AddComponent(entity, new UnitGrid
                {
                    Cell = int2.zero
                });

                AddComponent(entity, new UnitMove
                {
                    Speed = authoring.speed,
                    WalkSpeed = math.min(math.max(0.01f, authoring.walkSpeed), math.max(0.01f, authoring.speed)),
                    RoadSpeedMultiplier = authoring.roadSpeedMultiplier,
                    ArriveDistance = authoring.arriveDistance
                });
                AddComponent(entity, new UnitFootprint { Size = footprint });
                if (hasModelBounds)
                {
                    AddComponent(entity, new UnitSelectionHitbox
                    {
                        Center = new float3(modelBounds.center.x, modelBounds.center.y, modelBounds.center.z),
                        Extents = new float3(modelBounds.extents.x, modelBounds.extents.y, modelBounds.extents.z)
                    });
                }
                AddComponent(entity, new UnitMovementBehavior
                {
                    AllowIdleWander = (byte)(authoring.allowIdleWander ? 1 : 0),
                    UsesVehicleMotion = (byte)(authoring.UsesVehicleMotion ? 1 : 0)
                });
                float groundFuelPerCell = math.max(0f, authoring.GroundFuelPerCell);
                float airFuelPerCell = math.max(0f, authoring.AirFuelPerCell);
                if ((authoring.UsesVehicleMotion || authoring.IsAirUnit) && (groundFuelPerCell > 0f || airFuelPerCell > 0f))
                {
                    AddComponent(entity, new UnitFuelConsumption
                    {
                        Enabled = 1,
                        GroundFuelPerCell = groundFuelPerCell,
                        AirFuelPerCell = airFuelPerCell
                    });
                    AddComponent(entity, new UnitFuelConsumptionState
                    {
                        LastCell = int2.zero,
                        Initialized = 0
                    });
                }
                AddComponent(entity, vehicleMovement);
                AddComponent(entity, new UnitVehicleKinematics { CurrentSpeed = 0f, StallSeconds = 0f });
                AddComponent(entity, new UnitSurfaceComponent
                {
                    SurfaceId = -1,
                    LayerId = 0,
                    LastSampledHeight = authoring.transform.position.y,
                    LastSampledNormal = new float3(0f, 1f, 0f),
                    HasSurface = 0,
                    IsGrounded = 0
                });
                if (authoring.UsesVehicleMotion && !authoring.IsAirUnit)
                {
                    AddComponent(entity, new VehicleSurfaceAlignmentComponent
                    {
                        SurfaceNormal = new float3(0f, 1f, 0f),
                        PitchDegrees = 0f,
                        RollDegrees = 0f,
                        AlignmentWeight = 0f
                    });
                }
                AddComponent(entity, new UnitGroundOffsetComponent { Value = authoring.ConfiguredGroundOffset });
                int soldierCapacity = math.max(0, authoring.SoldierTransportCapacity);
                int vehicleCapacity = math.max(0, authoring.VehicleTransportCapacity);
                int cargoWeightCapacity = math.max(0, authoring.CargoWeightCapacity);
                if (soldierCapacity > 0)
                {
                    AddComponent(entity, new UnitTransportCapacity
                    {
                        SoldierCapacity = soldierCapacity
                    });
                }
                if (vehicleCapacity > 0 || cargoWeightCapacity > 0)
                {
                    AddComponent(entity, new UnitTransportCargoCapacity
                    {
                        SoldierCapacity = soldierCapacity,
                        VehicleCapacity = vehicleCapacity,
                        CargoWeightCapacity = cargoWeightCapacity
                    });
                }
                if (soldierCapacity > 0 || vehicleCapacity > 0 || cargoWeightCapacity > 0)
                {
                    AddBuffer<UnitTransportPassengerElement>(entity);
                }
                if (authoring.resourceHaulerBarrelCapacity > 0)
                {
                    AddComponent(entity, new UnitResourceHauler
                    {
                        BarrelCapacity = math.max(0, authoring.resourceHaulerBarrelCapacity),
                        FillDurationSeconds = math.max(0.01f, authoring.resourceHaulerFillDurationSeconds),
                        UnloadDurationSeconds = math.max(0.01f, authoring.resourceHaulerUnloadDurationSeconds),
                        CargoOilBarrels = 0f,
                        CargoFuelBarrels = 0f
                    });
                }
                if (authoring.IsAirUnit)
                {
                    float configuredCruiseHeight = math.max(0f, authoring.TransportCruiseHeight);
                    AddComponent(entity, new UnitAirMovement
                    {
                        CruiseHeight = configuredCruiseHeight > 0f
                            ? configuredCruiseHeight
                            : math.max(3f, modelBounds.size.y > 0f ? modelBounds.size.y * 2f : 6f),
                        RunwayTaxiSpeed = math.max(0.01f, authoring.RunwayTaxiSpeed)
                    });
                    AddComponent(entity, new UnitAirComponent
                    {
                        HomePosition = authoring.transform.position,
                        HomeCell = int2.zero,
                        HomeInitialized = 0,
                        ReturningHome = 0,
                        Airborne = 0
                    });
                }

                Transform model = ResolveModelRoot(authoring);
                OperationMapEntityPresentationIdentityAuthoring operationMapIdentity =
                    model != null
                        ? model.GetComponent<OperationMapEntityPresentationIdentityAuthoring>()
                        : null;
                OperationMapAuthoredVehicleOwnershipAuthoring operationMapOwnership =
                    model != null
                        ? model.GetComponent<OperationMapAuthoredVehicleOwnershipAuthoring>()
                        : null;
                bool operationMapAuthoredVehicle = operationMapIdentity != null &&
                    operationMapIdentity.Role == OperationMapEntityPresentationRole.GameplayVehicles &&
                    operationMapOwnership != null;
                byte bakedFactionId = operationMapAuthoredVehicle
                    ? operationMapOwnership.FactionId
                    : FactionIdentity.NeutralFactionId;
                AddComponent(entity, new Faction { Id = bakedFactionId });
                AddComponent(entity, new UnitCombat
                {
                    AggroRangeCells = authoring.ConfiguredAggroRangeCells,
                    ChaseBreakDistance = authoring.ConfiguredChaseBreakDistance,
                    CanAttack = (byte)(authoring.ConfiguredCanAttack ? 1 : 0),
                    AutoEngage = (byte)(authoring.ConfiguredAllowAutoEngage ? 1 : 0)
                });
                ThreatDetectionKind threatDetectionKind = authoring.config != null
                    ? authoring.config.ThreatDetectionKind
                    : authoring.threatDetectionKind;
                int threatDetectionRadiusCells = authoring.config != null
                    ? authoring.config.ThreatDetectionRadiusCells
                    : authoring.threatDetectionRadiusCells;
                if (threatDetectionKind != ThreatDetectionKind.None && threatDetectionRadiusCells > 0)
                {
                    AddComponent(entity, new ThreatDetector
                    {
                        Kind = (byte)threatDetectionKind,
                        RadiusCells = math.max(0, threatDetectionRadiusCells)
                    });
                    AddAirDefenseSupportProvider(entity, threatDetectionKind, threatDetectionRadiusCells);
                }

                // Trace visuals resolve from the config asset when present, so config
                // edits (e.g. by the VFX generator) take effect without re-saving
                // every unit prefab.
                Color traceColor = authoring.config != null ? authoring.config.AttackTraceColor : authoring.attackTraceColor;
                float traceWidth = authoring.config != null ? authoring.config.AttackTraceWidth : authoring.attackTraceWidth;
                float traceScrollSpeed = authoring.config != null ? authoring.config.AttackTraceScrollSpeed : authoring.attackTraceScrollSpeed;
                float traceDashDensity = authoring.config != null ? authoring.config.AttackTraceDashDensity : authoring.attackTraceDashDensity;
                float traceVisibleSeconds = authoring.config != null ? authoring.config.AttackTraceVisibleSeconds : authoring.attackTraceVisibleSeconds;
                AddComponent(entity, new UnitAttack
                {
                    Range = authoring.ConfiguredAttackRange,
                    CooldownSeconds = authoring.ConfiguredAttackCooldownSeconds,
                    Damage = authoring.ConfiguredAttackDamage,
                    TraceColor = new float4(traceColor.r, traceColor.g, traceColor.b, traceColor.a),
                    TraceWidth = math.max(0.01f, traceWidth),
                    TraceScrollSpeed = math.max(0.1f, traceScrollSpeed),
                    TraceDashDensity = math.max(1f, traceDashDensity),
                    TraceVisibleSeconds = math.max(0.01f, traceVisibleSeconds),
                    TracerEveryNthShot = math.max(1, authoring.config != null
                        ? authoring.config.AttackTracerEveryNthShot
                        : authoring.attackTracerEveryNthShot)
                });
                if (ShouldUseDualSideAttackTrace(authoring))
                {
                    float lateralOffset = ResolveDualSideAttackTraceLateralOffset(authoring);
                    AddComponent(entity, new UnitAttackTraceOriginPattern
                    {
                        OriginCount = 2,
                        LateralOffset = lateralOffset,
                        TargetLateralOffset = lateralOffset * 0.25f
                    });
                }

                int maxHp = authoring.ConfiguredMaxHealth;
                AddComponent(entity, new UnitHealth { Current = maxHp, Max = maxHp });
                AddComponent(entity, new UnitAttackCooldownComponent { CooldownRemaining = 0f });
                AddComponent(entity, new UnitAttackTraceComponent { TimeRemaining = 0f, Phase = 0f });
                AddComponent(entity, new UnitRespawnPrefab { Prefab = Entity.Null });
                AddComponent(entity, new UnitSourcePrefabKey { Value = new FixedString64Bytes(authoring.gameObject.name) });
                AddComponent(entity, new UnitDisplayInfo
                {
                    Name = new FixedString64Bytes(authoring.ConfiguredDisplayName),
                    Description = new FixedString128Bytes(authoring.ConfiguredDescription)
                });

                AddComponent(entity, new UnitPrevWorldPos { Value = authoring.transform.position });
                AddComponent(entity, new UnitMoveVisualComponent { IsMoving = 0, StillSeconds = 0f });
                AddBuffer<UnitTransportHiddenVisualScale>(entity);
                AddComponent(entity, new UnitAnimationSettings
                {
                    IdleDelayMinSeconds = math.max(0f, authoring.idleDelayMinSeconds),
                    IdleDelayMaxSeconds = math.max(authoring.idleDelayMinSeconds, authoring.idleDelayMaxSeconds),
                    IdleWanderDistanceMin = math.max(0f, authoring.idleWanderDistanceMin),
                    IdleWanderDistanceMax = math.max(authoring.idleWanderDistanceMin, authoring.idleWanderDistanceMax),
                    AttackAnimationSeconds = math.max(0.01f, authoring.attackAnimationSeconds),
                    DeathAnimationSeconds = math.max(0.01f, authoring.deathAnimationSeconds)
                });
                AddComponent(entity, new UnitIdleWanderComponent
                {
                    RandomState = math.max(1u, math.hash(new int4(footprint.x, footprint.y, authoring.gameObject.name.GetHashCode(), 0x45d9f3b))),
                    RetrySeconds = 0f,
                    CurrentIdleDelaySeconds = 0f
                });
                AddComponent(entity, new UnitAttackAnimationComponent { TimeRemaining = 0f });
                AddComponent(entity, new UnitResolvedAnimationIndex { Value = byte.MaxValue });
                DynamicBuffer<UnitAnimationOrderEntry> animationOrderBuffer = AddBuffer<UnitAnimationOrderEntry>(entity);
                if (authoring.animationOrder != null)
                {
                    for (int i = 0; i < authoring.animationOrder.Count; i++)
                    {
                        animationOrderBuffer.Add(new UnitAnimationOrderEntry
                        {
                            Kind = (byte)authoring.animationOrder[i]
                        });
                    }
                }

                Transform destroyed = ResolveDestroyedRoot(authoring);
                if (model != null)
                {
                    Entity modelEntity = GetEntity(model.gameObject, TransformUsageFlags.Renderable);
                    if (operationMapAuthoredVehicle)
                    {
                        AddComponent(entity, new OperationMapAuthoredVehiclePresentation
                        {
                            PlacementIndex = operationMapIdentity.PlacementIndex,
                            FactionId = bakedFactionId
                        });
                    }
                    AddComponent(entity, new UnitModelLocalTransform
                    {
                        Position = model.localPosition,
                        Rotation = model.localRotation,
                        Scale = model.localScale.x
                    });
                    AddComponent(entity, new UnitDetailedVisualReference
                    {
                        Root = modelEntity
                    });

                    if (!operationMapAuthoredVehicle && authoring.MidLodPrefab != null)
                    {
                        Entity midLodPrefab = GetEntity(authoring.MidLodPrefab, TransformUsageFlags.Renderable);
                        AddComponent(entity, new UnitMidLodPrefabReference
                        {
                            Prefab = midLodPrefab
                        });
                        if (authoring.MidLodPrefab.GetComponent<UnitSafeVisibleCharacterLodAuthoring>() != null)
                            AddComponent<UnitUsesSafeVisibleCharacterLodTag>(entity);

                        GameObject lowLodPrefabObject = authoring.LowLodPrefab;
                        Entity lowLodPrefab = lowLodPrefabObject != null
                            ? GetEntity(lowLodPrefabObject, TransformUsageFlags.Renderable)
                            : midLodPrefab;
                        AddComponent(entity, new UnitLowLodPrefabReference
                        {
                            Prefab = lowLodPrefab
                        });
                    }

                    DynamicBuffer<UnitHelicopterBladeReference> bladeBuffer = AddBuffer<UnitHelicopterBladeReference>(entity);
                    AddHelicopterBladeReferences(bladeBuffer, model);
                    if (bladeBuffer.Length == 0)
                        AddHelicopterBladeReferences(bladeBuffer, authoring.transform);
                }
                AddUnitVisualPrefabReferences(authoring, entity);
                if (authoring.UsesVehicleMotion)
                {
                    AddVehicleVisualPrefabReferences(authoring, entity);
                }
                else if (destroyed != null)
                {
                    Entity destroyedEntity = GetEntity(destroyed.gameObject, TransformUsageFlags.Renderable);
                    AddComponent(entity, new UnitDestroyedVisualReference
                    {
                        AliveVisual = model != null ? GetEntity(model.gameObject, TransformUsageFlags.Renderable) : Entity.Null,
                        DestroyedVisual = destroyedEntity,
                        AliveVisibleScale = model != null && !Mathf.Approximately(model.localScale.x, 0f) ? model.localScale.x : 1f,
                        DestroyedVisibleScale = !Mathf.Approximately(destroyed.localScale.x, 0f) ? destroyed.localScale.x : 1f
                    });
                }

                if (authoring.config != null)
                    DependsOn(authoring.config);
                AddTransportAirdropVisualPrefabReferences(authoring, entity);
                AddTransportPlaneDoorMetadata(authoring, entity);
                GameObject impactPrefab = authoring.AttackImpactPrefab;
                GameObject muzzleFlashPrefab = authoring.MuzzleFlashPrefab;
                float muzzleFlashHeightOffset = authoring.MuzzleFlashHeightOffset;
                float muzzleFlashForwardOffset = authoring.MuzzleFlashForwardOffset;
                if (impactPrefab != null)
                {
                    AddComponent(entity, new UnitAttackImpactVfxReference
                    {
                        Prefab = impactPrefab
                    });
                }
                if (muzzleFlashPrefab != null)
                {
                    AddComponent(entity, new UnitMuzzleFlashVfxReference
                    {
                        Prefab = muzzleFlashPrefab,
                        HeightOffset = math.max(0f, muzzleFlashHeightOffset),
                        ForwardOffset = math.max(0f, muzzleFlashForwardOffset)
                    });
                }
                AddGroundMissileLauncherComponents(authoring, entity);
                AddAirMissileLauncherComponents(authoring, entity);

                Transform turret = FindDescendantByName(authoring.transform, "Turret");
                if (authoring.UsesTurretAim && turret != null)
                {
                    Entity turretEntity = GetEntity(turret.gameObject, TransformUsageFlags.Dynamic);
                    AddComponent(entity, new UnitTurretReference
                    {
                        Turret = turretEntity
                    });
                }

                AddAttachedLightSetup(authoring.transform, entity);
            }

            private void AddGroundMissileLauncherComponents(UnitGridAuthoring authoring, Entity entity)
            {
                GroundMissileLauncherConfig missileConfig = authoring.GroundMissileLauncherConfig;
                if (missileConfig == null)
                    return;

                DependsOn(missileConfig);
                AddComponent(entity, new GroundMissileLauncherComponent
                {
                    MinRange = missileConfig.MinRange,
                    MaxRange = math.max(missileConfig.MaxRange, authoring.ConfiguredAttackRange),
                    PrepareSeconds = missileConfig.PrepareSeconds,
                    ReloadSeconds = missileConfig.ReloadSeconds,
                    BatteryElevatedAngleDegrees = missileConfig.BatteryElevatedAngleDegrees,
                    RocketSpeed = missileConfig.RocketSpeed,
                    ArcHeight = missileConfig.ArcHeight,
                    DamageRadius = missileConfig.DamageRadius,
                    Damage = missileConfig.Damage
                });
                AddComponent(entity, new GroundMissileLauncherStateComponent
                {
                    Phase = (byte)GroundMissileLauncherPhase.Idle,
                    TargetEntity = Entity.Null,
                    TargetCell = int2.zero,
                    TargetWorldPosition = float3.zero,
                    Timer = 0f,
                    SelectedRocketSlot = -1
                });

                Transform battery = authoring.GroundMissileLauncherBattery;
                if (battery != null)
                {
                    AddComponent(entity, new GroundMissileLauncherVisualReferenceComponent
                    {
                        Battery = GetEntity(battery.gameObject, TransformUsageFlags.Dynamic),
                        SmokeSpawn = authoring.GroundMissileLauncherSmokeSpawn != null
                            ? GetEntity(authoring.GroundMissileLauncherSmokeSpawn.gameObject, TransformUsageFlags.Dynamic)
                            : Entity.Null,
                        BatteryDefaultLocalRotation = battery.localRotation,
                        BatteryDefaultLocalPosition = battery.localPosition
                    });
                }

                DynamicBuffer<GroundMissileLauncherRocketVisualComponent> rockets =
                    AddBuffer<GroundMissileLauncherRocketVisualComponent>(entity);
                IReadOnlyList<Transform> rocketReferences = authoring.GroundMissileLauncherRockets;
                if (rocketReferences != null)
                {
                    for (int i = 0; i < rocketReferences.Count; i++)
                    {
                        Transform rocket = rocketReferences[i];
                        if (rocket == null)
                            continue;

                        rockets.Add(new GroundMissileLauncherRocketVisualComponent
                        {
                            Rocket = GetEntity(rocket.gameObject, TransformUsageFlags.Dynamic),
                            SlotIndex = i,
                            InitialLocalPosition = rocket.localPosition,
                            InitialLocalRotation = rocket.localRotation,
                            InitialLocalScale = rocket.localScale.x
                        });
                    }
                }

                AddComponent(entity, new GroundMissileLauncherVfxReferenceComponent
                {
                    LauncherBackfirePrefab = missileConfig.LauncherBackfirePrefab,
                    RocketTrailPrefab = missileConfig.RocketTrailPrefab,
                    ImpactExplosionPrefab = missileConfig.ImpactExplosionPrefab,
                    ImpactSmokePrefab = missileConfig.ImpactSmokePrefab
                });
            }


            private void AddAirDefenseSupportProvider(Entity entity, ThreatDetectionKind kind, int radiusCells)
            {
                if (kind == ThreatDetectionKind.None || radiusCells <= 0)
                    return;

                byte supportKind = kind == ThreatDetectionKind.Air
                    ? (byte)AirDefenseSupportProviderKind.Satellite
                    : (byte)AirDefenseSupportProviderKind.Radar;
                AddComponent(entity, new AirDefenseSupportProviderComponent
                {
                    Kind = supportKind,
                    Level = 1,
                    SupportRadius = math.max(0, radiusCells),
                    RangeBonus = supportKind == (byte)AirDefenseSupportProviderKind.Satellite
                        ? AirDefenseSupportTuning.SatelliteRangeBonus
                        : AirDefenseSupportTuning.RadarRangeBonus,
                    LockTimeMultiplier = supportKind == (byte)AirDefenseSupportProviderKind.Satellite
                        ? AirDefenseSupportTuning.SatelliteLockTimeMultiplier
                        : AirDefenseSupportTuning.RadarLockTimeMultiplier,
                    TrackingBonus = supportKind == (byte)AirDefenseSupportProviderKind.Satellite
                        ? AirDefenseSupportTuning.SatelliteTrackingBonus
                        : AirDefenseSupportTuning.RadarTrackingBonus,
                    TurnRateBonus = supportKind == (byte)AirDefenseSupportProviderKind.Satellite
                        ? AirDefenseSupportTuning.SatelliteTurnRateBonus
                        : AirDefenseSupportTuning.RadarTurnRateBonus
                });
            }


            private static Transform ResolveModelRoot(UnitGridAuthoring authoring)
            {
                // Baker-only compatibility fallback for legacy prefabs that have not serialized explicit visual roots yet.
                return authoring.modelRoot != null ? authoring.modelRoot : authoring.transform.Find("Model");
            }

            private static Transform ResolveDestroyedRoot(UnitGridAuthoring authoring)
            {
                // Baker-only compatibility fallback for legacy prefabs that have not serialized explicit visual roots yet.
                return authoring.destroyedRoot != null ? authoring.destroyedRoot : authoring.transform.Find("Destroyed");
            }

        }
    }
}
