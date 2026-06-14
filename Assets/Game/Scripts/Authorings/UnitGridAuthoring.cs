using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;

[DisallowMultipleComponent]
public class UnitGridAuthoring : MonoBehaviour
{
    [SerializeField] private UnitGridAuthoringConfig config;
    [SerializeField, HideInInspector] private bool allowIdleWander = true;
    [SerializeField, HideInInspector] private bool autoCalculateFootprint;
    [SerializeField, HideInInspector] private Vector2Int footprintCells = new(1, 1);
    [SerializeField, HideInInspector] private bool usesVehicleMotion;
    [SerializeField, HideInInspector] private bool isAirUnit;
    [SerializeField, HideInInspector] private bool canRequest = true;
    [SerializeField, HideInInspector, Min(0)] private int price;
    [SerializeField, HideInInspector, Min(0.01f)] private float productionDurationSeconds = 60f;
    [SerializeField, HideInInspector] private GameObject productionTransportPrefab;
    [SerializeField, HideInInspector] private bool isProductionTransportUnit;
    [SerializeField, HideInInspector, Min(0.01f)] private float productionTransportArrivalSeconds = 5f;
    [SerializeField, HideInInspector, Min(0.01f)] private float productionTransportHoldForNextReadySeconds = 4f;
    [SerializeField, HideInInspector, Min(1)] private int productionTransportMaxConcurrent = 1;
    [SerializeField, HideInInspector] private bool productionTransportRequiresAirportRunway;
    [SerializeField, HideInInspector] private bool productionTransportUsesRunwayLanding;
    [SerializeField, HideInInspector, Min(0)] private int soldierTransportCapacity;
    [SerializeField, HideInInspector, Min(0.01f)] private float runwayTaxiSpeed = 5f;
    [SerializeField, HideInInspector, Min(0.01f)] private float speed = 5f;
    [SerializeField, HideInInspector, Min(0.01f)] private float walkSpeed = 2f;
    [SerializeField, HideInInspector, Min(1f)] private float roadSpeedMultiplier = 1.2f;
    [SerializeField, HideInInspector, Min(0.001f)] private float arriveDistance = 0.05f;
    [SerializeField, HideInInspector] private float groundOffset;
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
        productionDurationSeconds = config.ProductionDurationSeconds;
        productionTransportPrefab = config.ProductionTransportPrefab;
        isProductionTransportUnit = config.IsProductionTransportUnit;
        productionTransportArrivalSeconds = config.ProductionTransportArrivalSeconds;
        productionTransportHoldForNextReadySeconds = config.ProductionTransportHoldForNextReadySeconds;
        productionTransportMaxConcurrent = config.ProductionTransportMaxConcurrent;
        productionTransportRequiresAirportRunway = config.ProductionTransportRequiresAirportRunway;
        productionTransportUsesRunwayLanding = config.ProductionTransportUsesRunwayLanding;
        soldierTransportCapacity = config.SoldierTransportCapacity;
        runwayTaxiSpeed = config.RunwayTaxiSpeed;
        speed = config.Speed;
        walkSpeed = config.WalkSpeed;
        roadSpeedMultiplier = config.RoadSpeedMultiplier;
        arriveDistance = config.ArriveDistance;
        groundOffset = config.GroundOffset;
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
    public bool ProductionTransportRequiresAirportRunway => productionTransportRequiresAirportRunway;
    public bool ProductionTransportUsesRunwayLanding => productionTransportUsesRunwayLanding;
    public int SoldierTransportCapacity => Mathf.Max(0, soldierTransportCapacity);
    public bool IsAirUnit => isAirUnit;
    public bool CanRequest => canRequest;
    public int Price => Mathf.Max(0, price);
    public bool ConfiguredAllowIdleWander => config != null ? config.AllowIdleWander : allowIdleWander;
    public float ConfiguredSpeed => Mathf.Max(0f, config != null ? config.Speed : speed);
    public int ConfiguredResourceHaulerBarrelCapacity => Mathf.Max(0, config != null ? config.ResourceHaulerBarrelCapacity : resourceHaulerBarrelCapacity);
    public bool ConfiguredCanAttack => config != null ? config.CanAttack : canAttack;
    public float ConfiguredAttackRange => Mathf.Max(0f, config != null ? config.AttackRange : attackRange);
    public int ConfiguredAttackDamage => Mathf.Max(0, config != null ? config.AttackDamage : attackDamage);
    public int ConfiguredMaxHealth => Mathf.Max(1, config != null ? config.MaxHealth : maxHealth);
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
    public GameObject AttackImpactPrefab => attackImpactPrefab;
    public GameObject MuzzleFlashPrefab => muzzleFlashPrefab;
    public float MuzzleFlashHeightOffset => Mathf.Max(0f, muzzleFlashHeightOffset);
    public float MuzzleFlashForwardOffset => Mathf.Max(0f, muzzleFlashForwardOffset);
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

    private class UnitGridBaker : Baker<UnitGridAuthoring>
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
            AddComponent(entity, new UnitMovementBehavior
            {
                AllowIdleWander = (byte)(authoring.allowIdleWander ? 1 : 0),
                UsesVehicleMotion = (byte)(authoring.usesVehicleMotion ? 1 : 0)
            });
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
            if (authoring.usesVehicleMotion && !authoring.isAirUnit)
            {
                AddComponent(entity, new VehicleSurfaceAlignmentComponent
                {
                    SurfaceNormal = new float3(0f, 1f, 0f),
                    PitchDegrees = 0f,
                    RollDegrees = 0f,
                    AlignmentWeight = 0f
                });
            }
            AddComponent(entity, new UnitGroundOffsetComponent { Value = authoring.groundOffset });
            if (authoring.soldierTransportCapacity > 0)
            {
                AddComponent(entity, new UnitTransportCapacity
                {
                    SoldierCapacity = math.max(0, authoring.soldierTransportCapacity)
                });
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
            if (authoring.isAirUnit)
            {
                AddComponent(entity, new UnitAirMovement
                {
                    CruiseHeight = math.max(3f, modelBounds.size.y > 0f ? modelBounds.size.y * 2f : 6f),
                    RunwayTaxiSpeed = math.max(0.01f, authoring.runwayTaxiSpeed)
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

            AddComponent(entity, new Faction { Id = FactionIdentitySystem.NeutralFactionId });
            AddComponent(entity, new UnitCombat
            {
                AggroRangeCells = math.max(0, authoring.aggroRangeCells),
                ChaseBreakDistance = math.max(0f, authoring.chaseBreakDistance),
                CanAttack = (byte)(authoring.canAttack ? 1 : 0),
                AutoEngage = (byte)(authoring.allowAutoEngage ? 1 : 0)
            });
            if (authoring.threatDetectionKind != ThreatDetectionKind.None && authoring.threatDetectionRadiusCells > 0)
            {
                AddComponent(entity, new ThreatDetector
                {
                    Kind = (byte)authoring.threatDetectionKind,
                    RadiusCells = math.max(0, authoring.threatDetectionRadiusCells)
                });
                AddAirDefenseSupportProvider(entity, authoring.threatDetectionKind, authoring.threatDetectionRadiusCells);
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
                Range = math.max(0f, authoring.attackRange),
                CooldownSeconds = math.max(0.01f, authoring.attackCooldownSeconds),
                Damage = math.max(0, authoring.attackDamage),
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
            if (authoring.config != null)
                DependsOn(authoring.config);
            GameObject impactPrefab = authoring.config != null && authoring.config.AttackImpactPrefab != null
                ? authoring.config.AttackImpactPrefab
                : authoring.attackImpactPrefab;
            GameObject muzzleFlashPrefab = authoring.config != null && authoring.config.MuzzleFlashPrefab != null
                ? authoring.config.MuzzleFlashPrefab
                : authoring.muzzleFlashPrefab;
            float muzzleFlashHeightOffset = authoring.config != null
                ? authoring.config.MuzzleFlashHeightOffset
                : authoring.muzzleFlashHeightOffset;
            float muzzleFlashForwardOffset = authoring.config != null
                ? authoring.config.MuzzleFlashForwardOffset
                : authoring.muzzleFlashForwardOffset;
            if (impactPrefab != null)
            {
                AddComponentObject(entity, new UnitAttackImpactVfxReference
                {
                    Prefab = impactPrefab
                });
            }
            if (muzzleFlashPrefab != null)
            {
                AddComponentObject(entity, new UnitMuzzleFlashVfxReference
                {
                    Prefab = muzzleFlashPrefab,
                    HeightOffset = math.max(0f, muzzleFlashHeightOffset),
                    ForwardOffset = math.max(0f, muzzleFlashForwardOffset)
                });
            }
            AddGroundMissileLauncherComponents(authoring, entity);
            AddAirMissileLauncherComponents(authoring, entity);

            int maxHp = math.max(1, authoring.maxHealth);
            AddComponent(entity, new UnitHealth { Current = maxHp, Max = maxHp });
            AddComponent(entity, new UnitAttackCooldownComponent { CooldownRemaining = 0f });
            AddComponent(entity, new UnitAttackTraceComponent { TimeRemaining = 0f, Phase = 0f });
            AddComponent(entity, new UnitRespawnPrefab { Prefab = Entity.Null });
            AddComponent(entity, new UnitSourcePrefabKey { Value = new FixedString64Bytes(authoring.gameObject.name) });
            AddComponent(entity, new UnitDisplayInfo
            {
                Name = new FixedString64Bytes(string.IsNullOrWhiteSpace(authoring.displayName) ? authoring.gameObject.name : authoring.displayName),
                Description = new FixedString128Bytes(authoring.description ?? string.Empty)
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

            Transform model = authoring.transform.Find("Model");
            Transform destroyed = authoring.transform.Find("Destroyed");
            if (model != null)
            {
                Entity modelEntity = GetEntity(model.gameObject, TransformUsageFlags.Renderable);
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

                if (authoring.MidLodPrefab != null)
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
            if (authoring.usesVehicleMotion)
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

            Transform turret = FindDescendantByName(authoring.transform, "Turret");
            if (authoring.UsesTurretAim && turret != null)
            {
                Entity turretEntity = GetEntity(turret.gameObject, TransformUsageFlags.Dynamic);
                AddComponent(entity, new UnitTurretReference
                {
                    Turret = turretEntity
                });
            }

            UnitAttachedLightSet attachedLights = BuildAttachedLightSet(authoring.transform);
            if (attachedLights != null)
                AddComponentObject(entity, attachedLights);
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

            AddComponentObject(entity, new GroundMissileLauncherVfxReferenceComponent
            {
                LauncherBackfirePrefab = missileConfig.LauncherBackfirePrefab,
                RocketTrailPrefab = missileConfig.RocketTrailPrefab,
                ImpactExplosionPrefab = missileConfig.ImpactExplosionPrefab,
                ImpactSmokePrefab = missileConfig.ImpactSmokePrefab
            });
        }

        private void AddAirMissileLauncherComponents(UnitGridAuthoring authoring, Entity entity)
        {
            AirMissileLauncherConfig missileConfig = authoring.AirMissileLauncherConfig;
            if (missileConfig == null)
                return;

            DependsOn(missileConfig);
            AddComponent(entity, new AirMissileLauncherComponent
            {
                MinRange = missileConfig.MinRange,
                BaseDetectionRange = missileConfig.BaseDetectionRange,
                MaxDetectionRange = missileConfig.MaxDetectionRange,
                AirTargetPriority = missileConfig.AirTargetPriority,
                IncomingMissilePriority = missileConfig.IncomingMissilePriority,
                TurretYawSpeedDegreesPerSecond = missileConfig.TurretYawSpeedDegreesPerSecond,
                AimToleranceDegrees = missileConfig.AimToleranceDegrees,
                LockSeconds = missileConfig.LockSeconds,
                LaunchDelaySeconds = missileConfig.LaunchDelaySeconds,
                ReloadSeconds = missileConfig.ReloadSeconds,
                MissileSpeed = missileConfig.MissileSpeed,
                MissileAcceleration = missileConfig.MissileAcceleration,
                MissileTurnRateDegreesPerSecond = missileConfig.MissileTurnRateDegreesPerSecond,
                MissileLifetimeSeconds = missileConfig.MissileLifetimeSeconds,
                ProximityFuseRadius = missileConfig.ProximityFuseRadius,
                AirTargetDamage = missileConfig.AirTargetDamage,
                IncomingMissileDamage = missileConfig.IncomingMissileDamage,
                TrackingQuality = missileConfig.TrackingQuality,
                MaxSupportRangeBonus = missileConfig.MaxSupportRangeBonus,
                MaxSupportTrackingBonus = missileConfig.MaxSupportTrackingBonus
            });
            AddComponent(entity, new AirMissileLauncherStateComponent
            {
                Phase = (byte)AirMissileLauncherPhase.Idle,
                TargetEntity = Entity.Null,
                TargetKind = (byte)AirMissileTargetKind.None,
                TargetWorldPosition = float3.zero,
                PredictedInterceptPosition = float3.zero,
                Timer = 0f,
                SelectedMissileSlot = -1,
                EffectiveRange = missileConfig.BaseDetectionRange,
                EffectiveLockSeconds = missileConfig.LockSeconds,
                EffectiveTrackingQuality = missileConfig.TrackingQuality,
                EffectiveTurnRateDegreesPerSecond = missileConfig.MissileTurnRateDegreesPerSecond
            });
            AddComponent(entity, new AirDefenseSupportLinkComponent
            {
                RangeBonus = 0f,
                LockTimeMultiplier = 1f,
                TrackingBonus = 0f,
                TurnRateBonus = 0f,
                RadarProvider = Entity.Null,
                SatelliteProvider = Entity.Null
            });

            Transform turret = authoring.AirMissileLauncherTurret;
            if (turret != null)
            {
                AddComponent(entity, new AirMissileLauncherVisualReferenceComponent
                {
                    Turret = GetEntity(turret.gameObject, TransformUsageFlags.Dynamic),
                    LaunchSpawn = authoring.AirMissileLauncherLaunchSpawn != null
                        ? GetEntity(authoring.AirMissileLauncherLaunchSpawn.gameObject, TransformUsageFlags.Dynamic)
                        : Entity.Null,
                    TurretDefaultLocalRotation = turret.localRotation,
                    TurretDefaultLocalPosition = turret.localPosition
                });
            }

            DynamicBuffer<AirMissileLauncherMissileVisualComponent> missiles =
                AddBuffer<AirMissileLauncherMissileVisualComponent>(entity);
            IReadOnlyList<Transform> missileReferences = authoring.AirMissileLauncherMissiles;
            if (missileReferences != null)
            {
                for (int i = 0; i < missileReferences.Count; i++)
                {
                    Transform missile = missileReferences[i];
                    if (missile == null)
                        continue;

                    missiles.Add(new AirMissileLauncherMissileVisualComponent
                    {
                        Missile = GetEntity(missile.gameObject, TransformUsageFlags.Dynamic),
                        SlotIndex = i,
                        InitialLocalPosition = missile.localPosition,
                        InitialLocalRotation = missile.localRotation,
                        InitialLocalScale = missile.localScale.x
                    });
                }
            }

            AddComponentObject(entity, new AirMissileLauncherVfxReferenceComponent
            {
                MissileVisualPrefab = missileConfig.MissileVisualPrefab,
                LaunchFlashPrefab = missileConfig.LaunchFlashPrefab,
                LaunchSmokePrefab = missileConfig.LaunchSmokePrefab,
                MissileTrailPrefab = missileConfig.MissileTrailPrefab,
                AirburstExplosionPrefab = missileConfig.AirburstExplosionPrefab,
                AirTargetImpactPrefab = missileConfig.AirTargetImpactPrefab,
                InterceptExplosionPrefab = missileConfig.InterceptExplosionPrefab
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
                RangeBonus = supportKind == (byte)AirDefenseSupportProviderKind.Satellite ? 120f : 80f,
                LockTimeMultiplier = supportKind == (byte)AirDefenseSupportProviderKind.Satellite ? 0.65f : 0.75f,
                TrackingBonus = supportKind == (byte)AirDefenseSupportProviderKind.Satellite ? 0.18f : 0.12f,
                TurnRateBonus = supportKind == (byte)AirDefenseSupportProviderKind.Satellite ? 50f : 35f
            });
        }

        private static UnitAttachedLightSet BuildAttachedLightSet(Transform root)
        {
            if (root == null)
                return null;

            Light[] lights = root.GetComponentsInChildren<Light>(true);
            if (lights == null || lights.Length == 0)
                return null;

            List<UnitAttachedLightSet.Entry> entries = null;
            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                if (light == null)
                    continue;

                Transform transform = light.transform;
                entries ??= new List<UnitAttachedLightSet.Entry>();
                entries.Add(new UnitAttachedLightSet.Entry
                {
                    Name = string.IsNullOrWhiteSpace(light.name) ? "UnitLight" : light.name,
                    Type = light.type,
                    Color = light.color,
                    Intensity = light.intensity,
                    Range = light.range,
                    SpotAngle = light.spotAngle,
                    InnerSpotAngle = light.innerSpotAngle,
                    CastShadows = light.shadows != LightShadows.None,
                    LocalPosition = root.InverseTransformPoint(transform.position),
                    LocalRotation = Quaternion.Inverse(root.rotation) * transform.rotation
                });
            }

            if (entries == null || entries.Count == 0)
                return null;

            return new UnitAttachedLightSet
            {
                Entries = entries.ToArray()
            };
        }

        private static bool ShouldUseDualSideAttackTrace(UnitGridAuthoring authoring)
        {
            if (authoring == null || !authoring.isAirUnit)
                return false;

            string sourceName = authoring.config != null ? authoring.config.name : authoring.gameObject.name;
            string display = authoring.ConfiguredDisplayName;
            return ContainsIgnoreCase(sourceName, "Veh_Helicopter_Attack") ||
                   ContainsIgnoreCase(display, "Attack Helicopter");
        }

        private static float ResolveDualSideAttackTraceLateralOffset(UnitGridAuthoring authoring)
        {
            string sourceName = authoring.config != null ? authoring.config.name : authoring.gameObject.name;
            string display = authoring.ConfiguredDisplayName;
            bool lightAttackHelicopter =
                ContainsIgnoreCase(sourceName, "Small") ||
                ContainsIgnoreCase(display, "Light Attack Helicopter");
            return lightAttackHelicopter ? 0.62f : 0.88f;
        }

        private static bool ContainsIgnoreCase(string value, string token)
        {
            return !string.IsNullOrEmpty(value) &&
                   !string.IsNullOrEmpty(token) &&
                   value.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static int2 ResolveFootprint(UnitGridAuthoring authoring, bool hasModelBounds, Bounds modelBounds)
        {
            int2 configured = new int2(math.max(1, authoring.footprintCells.x), math.max(1, authoring.footprintCells.y));

            if (!authoring.usesVehicleMotion)
                return configured;

            if (configured.x > 1 || configured.y > 1)
                return configured;

            if (!hasModelBounds)
                return configured;

            int2 modelFootprint = new int2(
                math.max(1, (int)math.ceil(modelBounds.size.x)),
                math.max(1, (int)math.ceil(modelBounds.size.z)));

            if (!authoring.autoCalculateFootprint)
                return configured;

            return modelFootprint;
        }

        private static UnitVehicleMovement ResolveVehicleMovement(UnitGridAuthoring authoring, int2 footprint, bool hasModelBounds, Bounds modelBounds)
        {
            bool isVehicle = authoring.usesVehicleMotion;
            float modelLength = hasModelBounds ? math.max(modelBounds.size.x, modelBounds.size.z) : math.max(footprint.x, footprint.y);

            return new UnitVehicleMovement
            {
                TurnSpeedDegrees = isVehicle ? 180f : 720f,
                Acceleration = isVehicle ? math.max(6f, modelLength * 3f) : 999f,
                Braking = isVehicle ? math.max(8f, modelLength * 4f) : 999f,
                RearPivotOffset = isVehicle ? math.max(0.35f, modelLength * 0.22f) : 0f
            };
        }

        private void AddHelicopterBladeReferences(DynamicBuffer<UnitHelicopterBladeReference> bladeBuffer, Transform root)
        {
            if (root == null)
                return;

            var stack = new Stack<Transform>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                Transform current = stack.Pop();
                if (TryGetBladeAxis(current.name, out byte axis))
                {
                    Entity bladeEntity = GetEntity(current.gameObject, TransformUsageFlags.Dynamic);
                    if (HasBladeReference(bladeBuffer, bladeEntity))
                    {
                        for (int i = 0; i < current.childCount; i++)
                            stack.Push(current.GetChild(i));
                        continue;
                    }

                    bladeBuffer.Add(new UnitHelicopterBladeReference
                    {
                        Blade = bladeEntity,
                        Axis = axis
                    });
                }

                for (int i = 0; i < current.childCount; i++)
                    stack.Push(current.GetChild(i));
            }
        }

        private static bool HasBladeReference(DynamicBuffer<UnitHelicopterBladeReference> bladeBuffer, Entity blade)
        {
            for (int i = 0; i < bladeBuffer.Length; i++)
            {
                if (bladeBuffer[i].Blade == blade)
                    return true;
            }

            return false;
        }

        private void AddUnitVisualPrefabReferences(UnitGridAuthoring authoring, Entity entity)
        {
            if (authoring.UnitSelectionMarkerPrefab != null)
            {
                AddComponent(entity, new UnitSelectionMarkerPrefabReference
                {
                    Prefab = GetEntity(authoring.UnitSelectionMarkerPrefab, TransformUsageFlags.Dynamic)
                });
            }

            if (authoring.UnitHealthBarPrefab != null)
            {
                AddComponent(entity, new UnitHealthBarPrefabReference
                {
                    Prefab = GetEntity(authoring.UnitHealthBarPrefab, TransformUsageFlags.Dynamic)
                });
            }
        }

        private void AddVehicleVisualPrefabReferences(UnitGridAuthoring authoring, Entity entity)
        {
            if (authoring.VehicleDestroyedVisualPrefab != null)
            {
                AddComponent(entity, new VehicleDestroyedVisualPrefabReference
                {
                    Prefab = GetEntity(authoring.VehicleDestroyedVisualPrefab, TransformUsageFlags.Dynamic)
                });
            }
        }

        private static bool TryGetBladeAxis(string name, out byte axis)
        {
            axis = 0;
            if (string.IsNullOrEmpty(name) || !name.Contains("Blade", System.StringComparison.Ordinal))
                return false;
            if (name.EndsWith("_X", System.StringComparison.Ordinal))
            {
                axis = 0;
                return true;
            }
            if (name.EndsWith("_Y", System.StringComparison.Ordinal))
            {
                axis = 1;
                return true;
            }
            if (name.EndsWith("_Z", System.StringComparison.Ordinal))
            {
                axis = 2;
                return true;
            }

            return false;
        }

        private static Transform FindDescendantByName(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name))
                return null;

            foreach (Transform child in root)
            {
                if (child.name == name)
                    return child;

                Transform nested = FindDescendantByName(child, name);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        private static bool TryGetModelLocalBounds(UnitGridAuthoring authoring, out Bounds combinedBounds)
        {
            combinedBounds = default;

            Transform modelRoot = authoring.transform.Find("Model");
            if (modelRoot == null)
                return false;

            return TryGetCombinedLocalBounds(modelRoot, authoring.transform.worldToLocalMatrix, out combinedBounds);
        }

        private static bool TryGetCombinedLocalBounds(Transform modelRoot, Matrix4x4 worldToLocal, out Bounds combinedBounds)
        {
            combinedBounds = default;
            if (modelRoot == null)
                return false;

            Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                Bounds localBounds = TransformBounds(worldToLocal * renderer.localToWorldMatrix, renderer.localBounds);
                if (!hasBounds)
                {
                    combinedBounds = localBounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(localBounds);
                }
            }

            return hasBounds;
        }

        private static Bounds TransformBounds(Matrix4x4 matrix, Bounds bounds)
        {
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;

            Vector3 min = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                        Vector3 transformed = matrix.MultiplyPoint3x4(corner);
                        min = Vector3.Min(min, transformed);
                        max = Vector3.Max(max, transformed);
                    }
                }
            }

            Bounds transformedBounds = new();
            transformedBounds.SetMinMax(min, max);
            return transformedBounds;
        }
    }
}
