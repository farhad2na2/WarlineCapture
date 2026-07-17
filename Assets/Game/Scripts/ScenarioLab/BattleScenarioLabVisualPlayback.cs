using System.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    public sealed class BattleScenarioLabVisualPlayback : MonoBehaviour
    {
        [SerializeField] private Camera scenarioCamera;
        [SerializeField] private Transform groundLauncherRoot;
        [SerializeField] private Transform airLauncherRoot;
        [SerializeField] private Transform radarRoot;
        [SerializeField] private Transform defendedTargetVisual;
        [SerializeField, Min(0.1f)] private float entityWaitTimeoutSeconds = 30f;

        private const string GroundLauncherKey = "Unit_Veh_Missle_Launcher_Ground";
        private const string AirLauncherKey = "Unit_Veh_Missle_Launcher_Air";
        private const string RadarKey = "Unit_Veh_Radar_Tank";
        private const string JetTargetKey = "Unit_Veh_Jet_01";
        private const string HelicopterTargetKey = "Unit_Veh_Helicopter_Attack";
        private const string DroneTargetKey = "Unit_Veh_Drone";
        private const string SoldierPassengerKey = "Unit_Chr_Soldier_Male_02_Alt_04";
        private const string GroundVehicleTransportKey = "Unit_Veh_APC_Heavy";
        private const string HelicopterTransportKey = "Unit_Veh_Helicopter_Transport";
        private const string PlaneTransportKey = "Unit_Veh_Plane_Transport";
        private const string VehicleCargoPassengerKey = "Unit_Veh_Tank_USA";
        private const float VisualInterceptProximityFuseRadius = 0.35f;
        private const float VisualAirTargetProximityFuseRadius = 4f;
        private const float VisualGroundMissileArcHeight = 8f;
        private const float ScenarioGroundMissileBaseFlightSeconds = 8f;
        private const float ScenarioAirBaseDetectionRange = 140f;
        private const float ScenarioAirMaxDetectionRange = 260f;
        private const float ScenarioAirMissileSpeed = 95f;
        private const float ScenarioAirMissileTurnRateDegreesPerSecond = 140f;
        private const float ScenarioAirTargetMissileTurnRateDegreesPerSecond = 170f;
        private const float ScenarioAirMissileLifetimeSeconds = 5f;
        private const float ScenarioAirLockSeconds = 0.9f;
        private const float ScenarioAirLaunchDelaySeconds = 0.1f;
        private const float ScenarioAirTrackingQuality = 0.75f;
        private const float ScenarioAirTargetTrackingQuality = 0.8f;

        private static readonly float3 AirLauncherPosition = new(0f, 0f, 0f);
        private static readonly float3 DefendedTargetPosition = new(-40f, 0f, 0f);
        private static readonly quaternion AirLauncherRotation = quaternion.RotateY(math.radians(90f));
        private static readonly quaternion GroundLauncherRotation = quaternion.RotateY(math.radians(-90f));
        private static readonly int2 TransportPlaneSoldierAirdropDropCell = new(23, 23);
        private static readonly int2 TransportPlaneVehicleCargoDropCell = new(24, 24);
        private static readonly int2 TransportPlaneMixedAirdropDropCell = new(25, 25);
        private static readonly Vector3 DefaultCameraPosition = new(80f, 45f, -80f);
        private static readonly Vector3 DefaultCameraLookAt = new(16f, 0f, 16f);

        private Coroutine playbackRoutine;
        public bool CanPlay(BattleScenarioDefinition definition)
        {
            if (definition == null)
                return true;

            if (TransportBoardingScenarioCatalog.TryGetScenario(definition.ScenarioId, out TransportBoardingScenarioDescriptor transportScenario) &&
                IsWiredTransportBoardingVisualScenario(transportScenario.Kind))
            {
                return true;
            }

            BattleScenarioVariant[] variants = definition.ScenarioVariants;
            if (variants == null || variants.Length == 0)
                return string.Equals(definition.ScenarioId, BattleScenarioAd001Runner.ScenarioId, System.StringComparison.Ordinal);

            for (int i = 0; i < variants.Length; i++)
                if (CanPlayVariant(variants[i]))
                    return true;

            return false;
        }

        public void Play(BattleScenarioDefinition definition, BattleScenarioVariant variant, BattleScenarioMetrics metrics)
        {
            if (!isActiveAndEnabled)
                return;

            StopPlaybackAndClear();

            playbackRoutine = StartCoroutine(PlayLiveEcsRoutine(definition, variant));
        }

        public void StopPlaybackAndClear()
        {
            if (playbackRoutine != null)
            {
                StopCoroutine(playbackRoutine);
                playbackRoutine = null;
            }

            HidePreviewMarkers();
            ClearPooledPresentationVfx();
            World world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
            {
                DisposeScenarioGrid(world.EntityManager);
                ResetPreviousRun(world.EntityManager);
            }

            ResetCameraView();
        }

        private IEnumerator PlayLiveEcsRoutine(BattleScenarioDefinition definition, BattleScenarioVariant variant)
        {
            if (definition != null &&
                TransportBoardingScenarioCatalog.TryGetScenario(definition.ScenarioId, out TransportBoardingScenarioDescriptor transportScenario))
            {
                yield return PlayTransportBoardingLiveEcsRoutine(transportScenario);
                yield break;
            }

            BattleScenarioVariant resolvedVariant = ResolveVariant(definition, variant);
            if (IsAirTargetKind(resolvedVariant.IncomingThreatKind))
            {
                yield return PlayAirTargetLiveEcsRoutine(resolvedVariant);
                yield break;
            }

            yield return PlayGroundMissileLiveEcsRoutine(resolvedVariant);
        }

        private IEnumerator PlayGroundMissileLiveEcsRoutine(BattleScenarioVariant variant)
        {
            PositionGroundMissileAuthoringRoots(variant);
            SetCamera(new Vector3(112f, 50f, -88f), new Vector3(58f, 8f, 0f));

            World world = null;
            Entity airLauncherPrefab = Entity.Null;
            Entity groundLauncherPrefab = Entity.Null;
            Entity radarPrefab = Entity.Null;
            float waitStart = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - waitStart < entityWaitTimeoutSeconds)
            {
                world = World.DefaultGameObjectInjectionWorld;
                if (world != null &&
                    TryResolveUnitPrefab(world.EntityManager, AirLauncherKey, out airLauncherPrefab) &&
                    TryResolveUnitPrefab(world.EntityManager, GroundLauncherKey, out groundLauncherPrefab))
                {
                    TryResolveUnitPrefab(world.EntityManager, RadarKey, out radarPrefab);
                    break;
                }

                yield return null;
            }

            if (world == null || airLauncherPrefab == Entity.Null || groundLauncherPrefab == Entity.Null)
            {
                Debug.LogError(
                    "[BattleScenarioLab] Live ECS visual run could not resolve baked production launcher prefab entities " +
                    $"within {entityWaitTimeoutSeconds:0.#}s. The scene must autoload the Scenario Lab prefab registry SubScene.");
                yield break;
            }

            EntityManager em = world.EntityManager;
            ResetPreviousRun(em);
            Entity airLauncher = InstantiateUnitPrefab(em, airLauncherPrefab);
            Entity groundLauncher = InstantiateUnitPrefab(em, groundLauncherPrefab);
            Entity radar = radarPrefab != Entity.Null ? InstantiateUnitPrefab(em, radarPrefab) : Entity.Null;
            ConfigureLiveScenario(em, variant, airLauncher, groundLauncher, radar);

            yield return CameraOpeningRoutine(em, variant, airLauncher, groundLauncher);
        }

        private IEnumerator PlayAirTargetLiveEcsRoutine(BattleScenarioVariant variant)
        {
            PositionAirTargetAuthoringRoots(variant);
            SetCamera(new Vector3(98f, 34f, -86f), new Vector3(46f, 11f, 0f));

            World world = null;
            Entity airLauncherPrefab = Entity.Null;
            Entity targetPrefab = Entity.Null;
            Entity radarPrefab = Entity.Null;
            string targetKey = ResolveAirTargetPrefabKey(variant);
            float waitStart = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - waitStart < entityWaitTimeoutSeconds)
            {
                world = World.DefaultGameObjectInjectionWorld;
                if (world != null &&
                    TryResolveUnitPrefab(world.EntityManager, AirLauncherKey, out airLauncherPrefab) &&
                    TryResolveUnitPrefab(world.EntityManager, targetKey, out targetPrefab))
                {
                    TryResolveUnitPrefab(world.EntityManager, RadarKey, out radarPrefab);
                    break;
                }

                yield return null;
            }

            if (world == null || airLauncherPrefab == Entity.Null || targetPrefab == Entity.Null)
            {
                Debug.LogError(
                    "[BattleScenarioLab] Live ECS visual run could not resolve baked production air launcher/air target prefab entities " +
                    $"within {entityWaitTimeoutSeconds:0.#}s. The scene must autoload the Scenario Lab prefab registry SubScene.");
                yield break;
            }

            EntityManager em = world.EntityManager;
            ResetPreviousRun(em);
            Entity airLauncher = InstantiateUnitPrefab(em, airLauncherPrefab);
            Entity target = InstantiateUnitPrefab(em, targetPrefab);
            Entity radar = radarPrefab != Entity.Null ? InstantiateUnitPrefab(em, radarPrefab) : Entity.Null;
            ConfigureLiveAirTargetScenario(em, variant, airLauncher, target, radar);

            yield return CameraAirTargetRoutine(em, variant, airLauncher, target);
        }

        private IEnumerator PlayTransportBoardingLiveEcsRoutine(TransportBoardingScenarioDescriptor scenario)
        {
            if (!scenario.VisualProofRequired)
            {
                ResetCameraView();
                Debug.Log($"[BattleScenarioLab] {scenario.ScenarioId} is an automated audit scenario and has no live visual playback by design.");
                yield break;
            }

            switch (scenario.Kind)
            {
                case TransportBoardingScenarioKind.GroundVehicleBoardAndExit:
                    yield return PlayGroundVehicleTransportBoardingLiveEcsRoutine();
                    yield break;

                case TransportBoardingScenarioKind.HelicopterBoardAndRopeExit:
                    yield return PlayHelicopterTransportBoardingLiveEcsRoutine();
                    yield break;

                case TransportBoardingScenarioKind.HelicopterAirPickup:
                    yield return PlayHelicopterAirPickupLiveEcsRoutine();
                    yield break;

                case TransportBoardingScenarioKind.PlaneRampBoardAndExit:
                    yield return PlayTransportPlaneRampBoardingLiveEcsRoutine();
                    yield break;

                case TransportBoardingScenarioKind.PlaneSoldierAirdrop:
                    yield return PlayTransportPlaneSoldierAirdropLiveEcsRoutine();
                    yield break;

                case TransportBoardingScenarioKind.PlaneVehicleCargoGroundExit:
                    yield return PlayTransportPlaneVehicleCargoGroundExitLiveEcsRoutine();
                    yield break;

                case TransportBoardingScenarioKind.PlaneVehicleCargoAirdrop:
                    yield return PlayTransportPlaneVehicleCargoAirdropLiveEcsRoutine();
                    yield break;

                case TransportBoardingScenarioKind.PlaneMixedLoadAirdrop:
                    yield return PlayTransportPlaneMixedLoadAirdropLiveEcsRoutine();
                    yield break;

                case TransportBoardingScenarioKind.NextCleanup:
                    yield return PlayTransportBoardingNextCleanupProofRoutine();
                    yield break;

                case TransportBoardingScenarioKind.CameraProofPath:
                    yield return PlayTransportBoardingCameraProofPathRoutine();
                    yield break;

                default:
                    Debug.LogWarning($"[BattleScenarioLab] Transport boarding visual playback is not wired yet for {scenario.ScenarioId}.");
                    yield break;
            }
        }

        private IEnumerator PlayGroundVehicleTransportBoardingLiveEcsRoutine()
        {
            SetCamera(new Vector3(18f, 13f, -18f), new Vector3(9f, 1.5f, 8f));

            World world = null;
            Entity soldierPrefab = Entity.Null;
            Entity transportPrefab = Entity.Null;
            float waitStart = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - waitStart < entityWaitTimeoutSeconds)
            {
                world = World.DefaultGameObjectInjectionWorld;
                if (world != null &&
                    TryResolveUnitPrefab(world.EntityManager, SoldierPassengerKey, out soldierPrefab) &&
                    TryResolveUnitPrefab(world.EntityManager, GroundVehicleTransportKey, out transportPrefab))
                {
                    break;
                }

                yield return null;
            }

            if (world == null || soldierPrefab == Entity.Null || transportPrefab == Entity.Null)
            {
                Debug.LogError(
                    "[BattleScenarioLab] TB-001 visual run could not resolve production soldier/APC prefab entities " +
                    $"within {entityWaitTimeoutSeconds:0.#}s. The Scenario Lab prefab registry SubScene must include transport boarding prefabs.");
                yield break;
            }

            EntityManager em = world.EntityManager;
            DisposeScenarioGrid(em);
            ResetPreviousRun(em);
            CreateScenarioGrid(em, 32, 32);

            Entity transport = InstantiateUnitPrefab(em, transportPrefab);
            Entity soldier = InstantiateUnitPrefab(em, soldierPrefab);
            ConfigureTb001GroundVehicleScenario(em, transport, soldier);

            yield return CameraTransportBoardAndGroundExitRoutine(em, transport, soldier);
        }

        private IEnumerator PlayHelicopterTransportBoardingLiveEcsRoutine()
        {
            SetCamera(new Vector3(21f, 16f, -23f), new Vector3(11f, 2f, 10f));

            World world = null;
            Entity soldierPrefab = Entity.Null;
            Entity transportPrefab = Entity.Null;
            float waitStart = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - waitStart < entityWaitTimeoutSeconds)
            {
                world = World.DefaultGameObjectInjectionWorld;
                if (world != null &&
                    TryResolveUnitPrefab(world.EntityManager, SoldierPassengerKey, out soldierPrefab) &&
                    TryResolveUnitPrefab(world.EntityManager, HelicopterTransportKey, out transportPrefab))
                {
                    break;
                }

                yield return null;
            }

            if (world == null || soldierPrefab == Entity.Null || transportPrefab == Entity.Null)
            {
                Debug.LogError(
                    "[BattleScenarioLab] TB-002 visual run could not resolve production soldier/helicopter transport prefab entities " +
                    $"within {entityWaitTimeoutSeconds:0.#}s. The Scenario Lab prefab registry SubScene must include transport boarding prefabs.");
                yield break;
            }

            EntityManager em = world.EntityManager;
            DisposeScenarioGrid(em);
            ResetPreviousRun(em);
            CreateScenarioGrid(em, 34, 34);

            Entity transport = InstantiateUnitPrefab(em, transportPrefab);
            Entity soldier = InstantiateUnitPrefab(em, soldierPrefab);
            ConfigureTb002HelicopterRopeScenario(em, transport, soldier);

            yield return CameraTransportBoardAndRopeExitRoutine(em, transport, soldier);
        }

        private IEnumerator PlayHelicopterAirPickupLiveEcsRoutine()
        {
            SetCamera(new Vector3(27f, 18f, -23f), new Vector3(15f, 4f, 13f));

            World world = null;
            Entity soldierPrefab = Entity.Null;
            Entity transportPrefab = Entity.Null;
            float waitStart = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - waitStart < entityWaitTimeoutSeconds)
            {
                world = World.DefaultGameObjectInjectionWorld;
                if (world != null &&
                    TryResolveUnitPrefab(world.EntityManager, SoldierPassengerKey, out soldierPrefab) &&
                    TryResolveUnitPrefab(world.EntityManager, HelicopterTransportKey, out transportPrefab))
                {
                    break;
                }

                yield return null;
            }

            if (world == null || soldierPrefab == Entity.Null || transportPrefab == Entity.Null)
            {
                Debug.LogError(
                    "[BattleScenarioLab] TB-003 visual run could not resolve production soldier/helicopter transport prefab entities " +
                    $"within {entityWaitTimeoutSeconds:0.#}s. The Scenario Lab prefab registry SubScene must include transport boarding prefabs.");
                yield break;
            }

            EntityManager em = world.EntityManager;
            DisposeScenarioGrid(em);
            ResetPreviousRun(em);
            CreateScenarioGrid(em, 36, 36);

            Entity transport = InstantiateUnitPrefab(em, transportPrefab);
            Entity soldier = InstantiateUnitPrefab(em, soldierPrefab);
            ConfigureTb003HelicopterAirPickupScenario(em, transport, soldier);

            yield return CameraTransportAirPickupBoardAndRopeExitRoutine(em, transport, soldier);
        }

        private IEnumerator PlayTransportPlaneRampBoardingLiveEcsRoutine()
        {
            SetCamera(new Vector3(28f, 16f, -18f), new Vector3(17f, 1.5f, 13f));

            World world = null;
            Entity soldierPrefab = Entity.Null;
            Entity transportPrefab = Entity.Null;
            float waitStart = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - waitStart < entityWaitTimeoutSeconds)
            {
                world = World.DefaultGameObjectInjectionWorld;
                if (world != null &&
                    TryResolveUnitPrefab(world.EntityManager, SoldierPassengerKey, out soldierPrefab) &&
                    TryResolveUnitPrefab(world.EntityManager, PlaneTransportKey, out transportPrefab))
                {
                    break;
                }

                yield return null;
            }

            if (world == null || soldierPrefab == Entity.Null || transportPrefab == Entity.Null)
            {
                Debug.LogError(
                    "[BattleScenarioLab] TB-005 visual run could not resolve production soldier/transport-plane prefab entities " +
                    $"within {entityWaitTimeoutSeconds:0.#}s. The Scenario Lab prefab registry SubScene must include transport boarding prefabs.");
                yield break;
            }

            EntityManager em = world.EntityManager;
            DisposeScenarioGrid(em);
            ResetPreviousRun(em);
            CreateScenarioGrid(em, 42, 42);

            Entity transport = InstantiateUnitPrefab(em, transportPrefab);
            Entity soldier = InstantiateUnitPrefab(em, soldierPrefab);
            ConfigureTb005TransportPlaneRampScenario(em, transport, soldier);

            yield return CameraTransportPlaneRampBoardAndGroundExitRoutine(em, transport, soldier);
        }

        private IEnumerator PlayTransportPlaneSoldierAirdropLiveEcsRoutine()
        {
            SetCamera(new Vector3(34f, 28f, -30f), new Vector3(18f, 18f, 18f));

            World world = null;
            Entity soldierPrefab = Entity.Null;
            Entity transportPrefab = Entity.Null;
            float waitStart = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - waitStart < entityWaitTimeoutSeconds)
            {
                world = World.DefaultGameObjectInjectionWorld;
                if (world != null &&
                    TryResolveUnitPrefab(world.EntityManager, SoldierPassengerKey, out soldierPrefab) &&
                    TryResolveUnitPrefab(world.EntityManager, PlaneTransportKey, out transportPrefab))
                {
                    break;
                }

                yield return null;
            }

            if (world == null || soldierPrefab == Entity.Null || transportPrefab == Entity.Null)
            {
                Debug.LogError(
                    "[BattleScenarioLab] TB-006 visual run could not resolve production soldier/transport-plane prefab entities " +
                    $"within {entityWaitTimeoutSeconds:0.#}s. The Scenario Lab prefab registry SubScene must include transport boarding prefabs.");
                yield break;
            }

            EntityManager em = world.EntityManager;
            DisposeScenarioGrid(em);
            ResetPreviousRun(em);
            CreateScenarioGrid(em, 48, 48);

            Entity transport = InstantiateUnitPrefab(em, transportPrefab);
            Entity soldier = InstantiateUnitPrefab(em, soldierPrefab);
            ConfigureTb006TransportPlaneSoldierAirdropScenario(em, transport, soldier);

            yield return CameraTransportPlaneSoldierAirdropRoutine(em, transport, soldier);
        }

        private IEnumerator PlayTransportPlaneVehicleCargoGroundExitLiveEcsRoutine()
        {
            SetCamera(new Vector3(31f, 17f, -21f), new Vector3(17f, 1.7f, 13f));

            World world = null;
            Entity vehiclePrefab = Entity.Null;
            Entity transportPrefab = Entity.Null;
            float waitStart = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - waitStart < entityWaitTimeoutSeconds)
            {
                world = World.DefaultGameObjectInjectionWorld;
                if (world != null &&
                    TryResolveUnitPrefab(world.EntityManager, VehicleCargoPassengerKey, out vehiclePrefab) &&
                    TryResolveUnitPrefab(world.EntityManager, PlaneTransportKey, out transportPrefab))
                {
                    break;
                }

                yield return null;
            }

            if (world == null || vehiclePrefab == Entity.Null || transportPrefab == Entity.Null)
            {
                Debug.LogError(
                    "[BattleScenarioLab] TB-007 visual run could not resolve production vehicle-cargo/transport-plane prefab entities " +
                    $"within {entityWaitTimeoutSeconds:0.#}s. The Scenario Lab prefab registry SubScene must include transport boarding prefabs.");
                yield break;
            }

            EntityManager em = world.EntityManager;
            DisposeScenarioGrid(em);
            ResetPreviousRun(em);
            CreateScenarioGrid(em, 48, 48);

            Entity transport = InstantiateUnitPrefab(em, transportPrefab);
            Entity vehicle = InstantiateUnitPrefab(em, vehiclePrefab);
            ConfigureTb007TransportPlaneVehicleCargoScenario(em, transport, vehicle);

            yield return CameraTransportPlaneVehicleCargoGroundExitRoutine(em, transport, vehicle);
        }

        private IEnumerator PlayTransportPlaneVehicleCargoAirdropLiveEcsRoutine()
        {
            SetCamera(new Vector3(38f, 30f, -34f), new Vector3(18f, 19f, 18f));

            World world = null;
            Entity vehiclePrefab = Entity.Null;
            Entity transportPrefab = Entity.Null;
            float waitStart = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - waitStart < entityWaitTimeoutSeconds)
            {
                world = World.DefaultGameObjectInjectionWorld;
                if (world != null &&
                    TryResolveUnitPrefab(world.EntityManager, VehicleCargoPassengerKey, out vehiclePrefab) &&
                    TryResolveUnitPrefab(world.EntityManager, PlaneTransportKey, out transportPrefab))
                {
                    break;
                }

                yield return null;
            }

            if (world == null || vehiclePrefab == Entity.Null || transportPrefab == Entity.Null)
            {
                Debug.LogError(
                    "[BattleScenarioLab] TB-008 visual run could not resolve production vehicle-cargo/transport-plane prefab entities " +
                    $"within {entityWaitTimeoutSeconds:0.#}s. The Scenario Lab prefab registry SubScene must include transport boarding prefabs.");
                yield break;
            }

            EntityManager em = world.EntityManager;
            DisposeScenarioGrid(em);
            ResetPreviousRun(em);
            CreateScenarioGrid(em, 52, 52);

            Entity transport = InstantiateUnitPrefab(em, transportPrefab);
            Entity vehicle = InstantiateUnitPrefab(em, vehiclePrefab);
            ConfigureTb008TransportPlaneVehicleCargoAirdropScenario(em, transport, vehicle);

            yield return CameraTransportPlaneVehicleCargoAirdropRoutine(em, transport, vehicle);
        }

        private IEnumerator PlayTransportPlaneMixedLoadAirdropLiveEcsRoutine()
        {
            SetCamera(new Vector3(40f, 32f, -36f), new Vector3(18f, 20f, 18f));

            World world = null;
            Entity soldierPrefab = Entity.Null;
            Entity vehiclePrefab = Entity.Null;
            Entity transportPrefab = Entity.Null;
            float waitStart = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - waitStart < entityWaitTimeoutSeconds)
            {
                world = World.DefaultGameObjectInjectionWorld;
                if (world != null &&
                    TryResolveUnitPrefab(world.EntityManager, SoldierPassengerKey, out soldierPrefab) &&
                    TryResolveUnitPrefab(world.EntityManager, VehicleCargoPassengerKey, out vehiclePrefab) &&
                    TryResolveUnitPrefab(world.EntityManager, PlaneTransportKey, out transportPrefab))
                {
                    break;
                }

                yield return null;
            }

            if (world == null || soldierPrefab == Entity.Null || vehiclePrefab == Entity.Null || transportPrefab == Entity.Null)
            {
                Debug.LogError(
                    "[BattleScenarioLab] TB-009 visual run could not resolve production soldier/vehicle-cargo/transport-plane prefab entities " +
                    $"within {entityWaitTimeoutSeconds:0.#}s. The Scenario Lab prefab registry SubScene must include transport boarding prefabs.");
                yield break;
            }

            EntityManager em = world.EntityManager;
            DisposeScenarioGrid(em);
            ResetPreviousRun(em);
            CreateScenarioGrid(em, 56, 56);

            Entity transport = InstantiateUnitPrefab(em, transportPrefab);
            Entity soldier = InstantiateUnitPrefab(em, soldierPrefab);
            Entity vehicle = InstantiateUnitPrefab(em, vehiclePrefab);
            ConfigureTb009TransportPlaneMixedLoadAirdropScenario(em, transport, soldier, vehicle);

            yield return CameraTransportPlaneMixedLoadAirdropRoutine(em, transport, soldier, vehicle);
        }

        private IEnumerator PlayTransportBoardingCameraProofPathRoutine()
        {
            yield return PlayGroundVehicleTransportBoardingLiveEcsRoutine();
            yield return new WaitForSeconds(0.35f);
            yield return PlayHelicopterTransportBoardingLiveEcsRoutine();
            yield return new WaitForSeconds(0.35f);
            yield return PlayTransportPlaneRampBoardingLiveEcsRoutine();
            yield return new WaitForSeconds(0.35f);
            yield return PlayTransportPlaneSoldierAirdropLiveEcsRoutine();
            yield return new WaitForSeconds(0.35f);
            yield return PlayTransportPlaneVehicleCargoAirdropLiveEcsRoutine();
            yield return new WaitForSeconds(0.35f);
            yield return PlayTransportPlaneMixedLoadAirdropLiveEcsRoutine();
        }

        private IEnumerator PlayTransportBoardingNextCleanupProofRoutine()
        {
            yield return PlayTransportPlaneVehicleCargoAirdropLiveEcsRoutine();
            yield return new WaitForSeconds(0.5f);

            World world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
            {
                EntityManager em = world.EntityManager;
                ResetPreviousRun(em);
                DisposeScenarioGrid(em);
            }

            ResetCameraView();
        }

        private static BattleScenarioVariant ResolveVariant(BattleScenarioDefinition definition, BattleScenarioVariant variant)
        {
            if (!string.IsNullOrWhiteSpace(variant.VariantId))
                return variant;

            if (definition != null && definition.ScenarioVariants != null && definition.ScenarioVariants.Length > 0)
                return definition.ScenarioVariants[0];

            BattleScenarioVariant[] variants = BattleScenarioAd001Runner.CreateDefaultVariants();
            return variants.Length > 0 ? variants[^1] : variant;
        }

        private static bool CanPlayVariant(BattleScenarioVariant variant)
        {
            return variant.IncomingThreatKind == BattleScenarioIncomingThreatKind.GroundMissile ||
                   IsAirTargetKind(variant.IncomingThreatKind);
        }

        private static bool IsWiredTransportBoardingVisualScenario(TransportBoardingScenarioKind kind)
        {
            return kind == TransportBoardingScenarioKind.GroundVehicleBoardAndExit ||
                   kind == TransportBoardingScenarioKind.HelicopterBoardAndRopeExit ||
                   kind == TransportBoardingScenarioKind.HelicopterAirPickup ||
                   kind == TransportBoardingScenarioKind.PlaneRampBoardAndExit ||
                   kind == TransportBoardingScenarioKind.PlaneSoldierAirdrop ||
                   kind == TransportBoardingScenarioKind.PlaneVehicleCargoGroundExit ||
                   kind == TransportBoardingScenarioKind.PlaneVehicleCargoAirdrop ||
                   kind == TransportBoardingScenarioKind.PlaneMixedLoadAirdrop ||
                   kind == TransportBoardingScenarioKind.NextCleanup ||
                   kind == TransportBoardingScenarioKind.CameraProofPath;
        }

        private static bool IsAirTargetKind(BattleScenarioIncomingThreatKind threatKind)
        {
            return threatKind == BattleScenarioIncomingThreatKind.Jet ||
                   threatKind == BattleScenarioIncomingThreatKind.Drone ||
                   threatKind == BattleScenarioIncomingThreatKind.Helicopter;
        }

        private void PositionGroundMissileAuthoringRoots(BattleScenarioVariant variant)
        {
            Vector3 groundPosition = new(Mathf.Max(40f, variant.IncomingThreatStartDistance), 0f, 0f);
            if (groundLauncherRoot != null)
            {
                groundLauncherRoot.gameObject.SetActive(false);
                groundLauncherRoot.SetPositionAndRotation(groundPosition, Quaternion.Euler(0f, -90f, 0f));
            }

            if (airLauncherRoot != null)
            {
                airLauncherRoot.gameObject.SetActive(false);
                airLauncherRoot.SetPositionAndRotation((Vector3)AirLauncherPosition, Quaternion.Euler(0f, 90f, 0f));
            }

            if (radarRoot != null)
            {
                bool radarEnabled = variant.SupportMode == BattleScenarioSupportMode.RadarNear;
                radarRoot.gameObject.SetActive(false);
                radarRoot.SetPositionAndRotation(new Vector3(Mathf.Max(0f, variant.RadarDistanceFromLauncher), 0f, -12f), Quaternion.identity);
                radarRoot.localScale = radarEnabled ? Vector3.one : Vector3.one * 0.0001f;
            }

            if (defendedTargetVisual != null)
                defendedTargetVisual.gameObject.SetActive(false);
        }

        private void PositionAirTargetAuthoringRoots(BattleScenarioVariant variant)
        {
            if (groundLauncherRoot != null)
                groundLauncherRoot.gameObject.SetActive(false);

            if (airLauncherRoot != null)
            {
                airLauncherRoot.gameObject.SetActive(false);
                airLauncherRoot.SetPositionAndRotation((Vector3)AirLauncherPosition, Quaternion.Euler(0f, 90f, 0f));
            }

            if (radarRoot != null)
            {
                bool radarEnabled = variant.SupportMode == BattleScenarioSupportMode.RadarNear;
                radarRoot.gameObject.SetActive(false);
                radarRoot.SetPositionAndRotation(new Vector3(Mathf.Max(0f, variant.RadarDistanceFromLauncher), 0f, -12f), Quaternion.identity);
                radarRoot.localScale = radarEnabled ? Vector3.one : Vector3.one * 0.0001f;
            }

            if (defendedTargetVisual != null)
                defendedTargetVisual.gameObject.SetActive(false);
        }

        private static string ResolveAirTargetPrefabKey(BattleScenarioVariant variant)
        {
            return variant.IncomingThreatKind switch
            {
                BattleScenarioIncomingThreatKind.Helicopter => HelicopterTargetKey,
                BattleScenarioIncomingThreatKind.Drone => DroneTargetKey,
                _ => JetTargetKey
            };
        }

        private static float3 ResolveAirTargetPosition(BattleScenarioVariant variant)
        {
            float distance = math.max(45f, variant.IncomingThreatStartDistance);
            float altitude = math.max(8f, variant.IncomingThreatAltitude);
            float z = variant.IncomingThreatKind == BattleScenarioIncomingThreatKind.Helicopter ? 12f : 0f;
            if (variant.IncomingThreatKind == BattleScenarioIncomingThreatKind.Drone)
                z = -10f;
            if (!string.IsNullOrWhiteSpace(variant.VariantId) &&
                variant.VariantId.IndexOf("Attacking", System.StringComparison.OrdinalIgnoreCase) >= 0)
                z = 8f;

            return new float3(distance, altitude, z);
        }

        private static void ConfigureLiveScenario(
            EntityManager em,
            BattleScenarioVariant variant,
            Entity airLauncher,
            Entity groundLauncher,
            Entity radar)
        {
            SetFaction(em, airLauncher, FactionIdentity.PlayerFactionId);
            SetFaction(em, groundLauncher, FactionIdentity.EnemyFactionId);

            SetLocalTransform(em, airLauncher, AirLauncherPosition, AirLauncherRotation, 1f);
            SetLocalTransform(
                em,
                groundLauncher,
                new float3(math.max(40f, variant.IncomingThreatStartDistance), 0f, 0f),
                GroundLauncherRotation,
                1f);

            ConfigureRadar(em, variant, radar);
            ResetAirLauncherForGroundMissile(em, airLauncher);
            StartGroundMissileLaunch(em, groundLauncher, variant);
        }

        private static void ConfigureLiveAirTargetScenario(
            EntityManager em,
            BattleScenarioVariant variant,
            Entity airLauncher,
            Entity target,
            Entity radar)
        {
            SetFaction(em, airLauncher, FactionIdentity.PlayerFactionId);
            SetFaction(em, target, FactionIdentity.EnemyFactionId);

            SetLocalTransform(em, airLauncher, AirLauncherPosition, AirLauncherRotation, 1f);
            SetLocalTransform(
                em,
                target,
                ResolveAirTargetPosition(variant),
                quaternion.RotateY(math.radians(-90f)),
                1f);

            ConfigureRadar(em, variant, radar);
            ConfigureAirTarget(em, target, variant);
            ResetAirLauncherForAirTarget(em, airLauncher);
        }

        private static void ConfigureRadar(EntityManager em, BattleScenarioVariant variant, Entity radar)
        {
            if (radar == Entity.Null || !em.Exists(radar))
                return;

            bool enabled = variant.SupportMode == BattleScenarioSupportMode.RadarNear;
            SetFaction(em, radar, FactionIdentity.PlayerFactionId);
            SetLocalTransform(
                em,
                radar,
                new float3(math.max(0f, variant.RadarDistanceFromLauncher), 0f, -12f),
                quaternion.identity,
                enabled ? 1f : 0.0001f);

            if (!em.HasComponent<AirDefenseSupportProviderComponent>(radar))
                return;

            em.SetComponentData(radar, new AirDefenseSupportProviderComponent
            {
                Kind = (byte)AirDefenseSupportProviderKind.Radar,
                Level = 1,
                SupportRadius = enabled ? 90f : 0f,
                RangeBonus = AirDefenseSupportTuning.RadarRangeBonus,
                LockTimeMultiplier = AirDefenseSupportTuning.RadarLockTimeMultiplier,
                TrackingBonus = AirDefenseSupportTuning.RadarTrackingBonus,
                TurnRateBonus = AirDefenseSupportTuning.RadarTurnRateBonus
            });
        }

        private static void ConfigureAirTarget(EntityManager em, Entity target, BattleScenarioVariant variant)
        {
            if (target == Entity.Null || !em.Exists(target))
                return;

            float3 targetPosition = ResolveAirTargetPosition(variant);
            if (em.HasComponent<UnitHealth>(target))
            {
                int health = variant.IncomingThreatKind == BattleScenarioIncomingThreatKind.Helicopter ? 130 : 100;
                em.SetComponentData(target, new UnitHealth { Current = health, Max = health });
            }
            else
            {
                em.AddComponentData(target, new UnitHealth { Current = 100, Max = 100 });
            }

            if (em.HasComponent<UnitPrevWorldPos>(target))
                em.SetComponentData(target, new UnitPrevWorldPos { Value = targetPosition });
            else
                em.AddComponentData(target, new UnitPrevWorldPos { Value = targetPosition });

            if (!em.HasComponent<UnitAirMovement>(target))
            {
                em.AddComponentData(target, new UnitAirMovement
                {
                    CruiseHeight = math.max(8f, variant.IncomingThreatAltitude),
                    RunwayTaxiSpeed = 12f
                });
            }

            if (em.HasComponent<UnitAirComponent>(target))
            {
                UnitAirComponent air = em.GetComponentData<UnitAirComponent>(target);
                air.HomePosition = new float3(targetPosition.x, 0f, targetPosition.z);
                air.HomeInitialized = 1;
                air.Airborne = 1;
                air.ReturningHome = 0;
                air.TakeoffRolling = 0;
                air.LandingRolling = 0;
                air.AttackRunActive = (byte)(IsAttackRunVariant(variant) ? 1 : 0);
                em.SetComponentData(target, air);
            }
            else
            {
                em.AddComponentData(target, new UnitAirComponent
                {
                    HomePosition = new float3(targetPosition.x, 0f, targetPosition.z),
                    HomeInitialized = 1,
                    Airborne = 1,
                    AttackRunActive = (byte)(IsAttackRunVariant(variant) ? 1 : 0)
                });
            }
        }

        private static bool IsAttackRunVariant(BattleScenarioVariant variant)
        {
            return !string.IsNullOrWhiteSpace(variant.VariantId) &&
                   variant.VariantId.IndexOf("Attacking", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void CreateScenarioGrid(EntityManager em, int width, int height)
        {
            int gridSize = width * height;
            Entity gridEntity = em.CreateEntity(
                typeof(BattleScenarioLabRuntimeGridTag),
                typeof(GridConfig),
                typeof(DynamicBlockerComponent),
                typeof(DynamicOccupancyComponent),
                typeof(PathPoolComponent));
            em.SetName(gridEntity, "BattleScenarioLabRuntimeGrid");
            em.SetComponentData(gridEntity, new GridConfig { Width = width, Height = height, CellSize = 1f, Origin = float3.zero });
            RuntimeGridPersistentStorageUtilitySystemHelper.EnsureStorage(em, gridEntity, gridSize);

            em.AddBuffer<GridWalkable>(gridEntity);
            em.AddBuffer<GridRoad>(gridEntity);
            em.AddBuffer<GridRoadSidewalk>(gridEntity);
            em.AddBuffer<GridRoadDirt>(gridEntity);

            DynamicBuffer<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity);
            DynamicBuffer<GridRoad> roads = em.GetBuffer<GridRoad>(gridEntity);
            DynamicBuffer<GridRoadSidewalk> sidewalks = em.GetBuffer<GridRoadSidewalk>(gridEntity);
            DynamicBuffer<GridRoadDirt> dirtRoads = em.GetBuffer<GridRoadDirt>(gridEntity);
            walkable.ResizeUninitialized(gridSize);
            roads.ResizeUninitialized(gridSize);
            sidewalks.ResizeUninitialized(gridSize);
            dirtRoads.ResizeUninitialized(gridSize);
            for (int i = 0; i < gridSize; i++)
            {
                walkable[i] = new GridWalkable { Value = 1 };
                roads[i] = new GridRoad { Value = 0 };
                sidewalks[i] = new GridRoadSidewalk { Value = 0 };
                dirtRoads[i] = new GridRoadDirt { Value = 0 };
            }

            EnsureScenarioRuntimeGameplayState(em);
        }

        private static GridConfig ResolveScenarioGrid(EntityManager em)
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
            return query.GetSingleton<GridConfig>();
        }

        private static void EnsureScenarioRuntimeGameplayState(EntityManager em)
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<RuntimeGameplayStateComponent>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            if (entities.Length == 0)
            {
                Entity entity = em.CreateEntity(
                    typeof(BattleScenarioLabRuntimeGameplayStateTag),
                    typeof(RuntimeGameplayStateComponent));
                em.SetName(entity, "BattleScenarioLabRuntimeGameplayState");
                em.SetComponentData(entity, new RuntimeGameplayStateComponent
                {
                    PlayRequested = 1,
                    SimulationActive = 1
                });
                return;
            }

            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                RuntimeGameplayStateComponent runtimeState = em.GetComponentData<RuntimeGameplayStateComponent>(entity);
                runtimeState.PlayRequested = 1;
                runtimeState.SimulationActive = 1;
                em.SetComponentData(entity, runtimeState);
            }
        }

        private void DisposeScenarioGrid(EntityManager em)
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<BattleScenarioLabRuntimeGridTag>());
            using NativeArray<Entity> grids = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < grids.Length; i++)
            {
                Entity grid = grids[i];
                if (!em.Exists(grid))
                    continue;

                RuntimeGridPersistentStorageUtilitySystemHelper.DisposeStorage(em, grid);
            }

        }

        private static void ConfigureTb001GroundVehicleScenario(EntityManager em, Entity transport, Entity soldier)
        {
            int2 transportCell = new(8, 8);
            int2 soldierCell = new(9, 8);
            float3 transportPosition = new(transportCell.x + 0.5f, 0f, transportCell.y + 0.5f);
            float3 soldierPosition = new(soldierCell.x + 0.5f, 0f, soldierCell.y + 0.5f);

            SetFaction(em, transport, FactionIdentity.PlayerFactionId);
            SetFaction(em, soldier, FactionIdentity.PlayerFactionId);
            SetLocalTransform(em, transport, transportPosition, quaternion.RotateY(math.radians(90f)), 1f);
            SetLocalTransform(em, soldier, soldierPosition, quaternion.identity, 1f);
            SetOrAdd(em, transport, new UnitGrid { Cell = transportCell });
            SetOrAdd(em, soldier, new UnitGrid { Cell = soldierCell });
            SetOrAdd(em, transport, new UnitFootprint { Size = new int2(3, 3) });
            SetOrAdd(em, soldier, new UnitFootprint { Size = new int2(1, 1) });
            SetOrAdd(em, transport, new UnitTransportCapacity { SoldierCapacity = 10 });
            SetOrAdd(em, soldier, new UnitMove { Speed = 4f, WalkSpeed = 1.5f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
            ConfigureScenarioGroundMovement(em, soldier, soldierPosition);
            SetOrAdd(em, soldier, new UnitTransportBoardingTarget { Transport = transport, Goal = soldierCell });
            if (!em.HasBuffer<UnitTransportPassengerElement>(transport))
                em.AddBuffer<UnitTransportPassengerElement>(transport);
            if (!em.HasBuffer<UnitTransportHiddenVisualScale>(soldier))
                em.AddBuffer<UnitTransportHiddenVisualScale>(soldier);
        }

        private static void ConfigureTb002HelicopterRopeScenario(EntityManager em, Entity transport, Entity soldier)
        {
            int2 transportCell = new(11, 10);
            int2 soldierCell = new(10, 10);
            float3 transportPosition = new(transportCell.x + 0.5f, 0f, transportCell.y + 0.5f);
            float3 soldierPosition = new(soldierCell.x + 0.5f, 0f, soldierCell.y + 0.5f);

            SetFaction(em, transport, FactionIdentity.PlayerFactionId);
            SetFaction(em, soldier, FactionIdentity.PlayerFactionId);
            SetLocalTransform(em, transport, transportPosition, quaternion.RotateY(math.radians(90f)), 1f);
            SetLocalTransform(em, soldier, soldierPosition, quaternion.identity, 1f);
            SetOrAdd(em, transport, new UnitGrid { Cell = transportCell });
            SetOrAdd(em, soldier, new UnitGrid { Cell = soldierCell });
            SetOrAdd(em, transport, new UnitFootprint { Size = new int2(3, 3) });
            SetOrAdd(em, soldier, new UnitFootprint { Size = new int2(1, 1) });
            SetOrAdd(em, transport, new UnitTransportCapacity { SoldierCapacity = 8 });
            SetOrAdd(em, transport, new UnitMove { Speed = 7f, WalkSpeed = 0f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.1f });
            SetOrAdd(em, transport, new UnitAirMovement { CruiseHeight = 8f, RunwayTaxiSpeed = 5f });
            SetOrAdd(em, transport, new UnitAirComponent
            {
                HomePosition = transportPosition,
                HomeCell = transportCell,
                HomeInitialized = 1,
                Airborne = 0,
                ReturningHome = 0,
                TakeoffRolling = 0,
                LandingRolling = 0,
                AttackRunActive = 0,
                ReturnApproachInitialized = 0
            });
            RemoveIfPresent<UnitTransportRopeDisembarkRequest>(em, transport);

            SetOrAdd(em, soldier, new UnitMove { Speed = 4f, WalkSpeed = 1.5f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
            ConfigureScenarioGroundMovement(em, soldier, soldierPosition);
            SetOrAdd(em, soldier, new UnitTransportBoardingTarget { Transport = transport, Goal = soldierCell });
            if (!em.HasBuffer<UnitTransportPassengerElement>(transport))
                em.AddBuffer<UnitTransportPassengerElement>(transport);
            if (!em.HasBuffer<UnitTransportHiddenVisualScale>(soldier))
                em.AddBuffer<UnitTransportHiddenVisualScale>(soldier);
        }

        private static void ConfigureTb003HelicopterAirPickupScenario(EntityManager em, Entity transport, Entity soldier)
        {
            int2 transportCell = new(4, 4);
            int2 soldierCell = new(16, 16);
            float3 transportGroundPosition = new(transportCell.x + 0.5f, 0f, transportCell.y + 0.5f);
            float3 transportAirPosition = transportGroundPosition + new float3(0f, 8f, 0f);
            float3 soldierPosition = new(soldierCell.x + 0.5f, 0f, soldierCell.y + 0.5f);

            SetFaction(em, transport, FactionIdentity.PlayerFactionId);
            SetFaction(em, soldier, FactionIdentity.PlayerFactionId);
            SetLocalTransform(em, transport, transportAirPosition, quaternion.RotateY(math.radians(45f)), 1f);
            SetLocalTransform(em, soldier, soldierPosition, quaternion.identity, 1f);
            SetOrAdd(em, transport, new UnitGrid { Cell = transportCell });
            SetOrAdd(em, soldier, new UnitGrid { Cell = soldierCell });
            SetOrAdd(em, transport, new UnitFootprint { Size = new int2(1, 1) });
            SetOrAdd(em, soldier, new UnitFootprint { Size = new int2(1, 1) });
            SetOrAdd(em, transport, new UnitTransportCapacity { SoldierCapacity = 8 });
            SetOrAdd(em, transport, new UnitMove { Speed = 9f, WalkSpeed = 0f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.1f });
            SetOrAdd(em, transport, new UnitAirMovement { CruiseHeight = 8f, RunwayTaxiSpeed = 5f });
            SetOrAdd(em, transport, new UnitAirComponent
            {
                HomePosition = transportGroundPosition,
                HomeCell = transportCell,
                HomeInitialized = 1,
                Airborne = 0,
                ReturningHome = 0,
                TakeoffRolling = 0,
                LandingRolling = 0,
                AttackRunActive = 0,
                ReturnApproachInitialized = 0
            });
            RemoveIfPresent<UnitTransportRopeDisembarkRequest>(em, transport);

            SetOrAdd(em, soldier, new UnitMove { Speed = 4f, WalkSpeed = 1.5f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
            ConfigureScenarioGroundMovement(em, soldier, soldierPosition);
            SetOrAdd(em, soldier, default(SelectedUnitTag));
            RemoveIfPresent<UnitTransportBoardingTarget>(em, soldier);
            if (!em.HasBuffer<UnitTransportPassengerElement>(transport))
                em.AddBuffer<UnitTransportPassengerElement>(transport);
            if (!em.HasBuffer<UnitTransportHiddenVisualScale>(soldier))
                em.AddBuffer<UnitTransportHiddenVisualScale>(soldier);
        }

        private static void ConfigureTb005TransportPlaneRampScenario(EntityManager em, Entity transport, Entity soldier)
        {
            int2 transportCell = new(17, 17);
            float3 transportPosition = new(transportCell.x + 0.5f, 0f, transportCell.y + 0.5f);
            quaternion transportRotation = quaternion.identity;

            SetFaction(em, transport, FactionIdentity.PlayerFactionId);
            SetLocalTransform(em, transport, transportPosition, transportRotation, 1f);
            SetOrAdd(em, transport, new UnitGrid { Cell = transportCell });
            SetOrAdd(em, transport, new UnitFootprint { Size = new int2(7, 7) });
            SetOrAdd(em, transport, new UnitTransportCapacity { SoldierCapacity = 24 });
            SetOrAdd(em, transport, new UnitTransportCargoCapacity
            {
                SoldierCapacity = 24,
                VehicleCapacity = 2,
                CargoWeightCapacity = 0
            });
            SetOrAdd(em, transport, new UnitMove { Speed = 9f, WalkSpeed = 0f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.1f });
            SetOrAdd(em, transport, new UnitAirMovement { CruiseHeight = 18f, RunwayTaxiSpeed = 5f });
            SetOrAdd(em, transport, new UnitAirComponent
            {
                HomePosition = transportPosition,
                HomeCell = transportCell,
                HomeInitialized = 1,
                Airborne = 0,
                ReturningHome = 0,
                TakeoffRolling = 0,
                LandingRolling = 0,
                AttackRunActive = 0,
                ReturnApproachInitialized = 0
            });
            SetOrAdd(em, transport, new UnitTransportPlaneDoorReference
            {
                DoorEntity = Entity.Null,
                ClosedLocalRotation = quaternion.identity,
                OpenLocalRotation = quaternion.identity,
                OpenSeconds = 1.1f,
                CloseSeconds = 0.9f,
                DoorLocalPosition = new float3(0f, 0f, -4f),
                InteriorLocalPosition = new float3(0f, 1.45f, 4f),
                ApproachLocalPosition = new float3(0f, 0f, -5f),
                RolloutLocalPosition = new float3(0f, 0f, -5f)
            });
            SetOrAdd(em, transport, default(UnitTransportPlaneDoorState));
            RemoveIfPresent<UnitTransportAirdropRequest>(em, transport);
            RemoveIfPresent<UnitTransportPlaneDoorOpenRequest>(em, transport);

            GridConfig grid = ResolveScenarioGrid(em);
            int2 rampCell = TransportBoardingCommandSystem.ResolvePlaneRampApproachCell(em, grid, transport);
            float3 soldierPosition = new(rampCell.x + 0.5f, 0f, rampCell.y + 0.5f);

            SetFaction(em, soldier, FactionIdentity.PlayerFactionId);
            SetLocalTransform(em, soldier, soldierPosition, quaternion.identity, 1f);
            SetOrAdd(em, soldier, new UnitGrid { Cell = rampCell });
            SetOrAdd(em, soldier, new UnitFootprint { Size = new int2(1, 1) });
            SetOrAdd(em, soldier, new UnitMove { Speed = 4f, WalkSpeed = 1.5f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
            ConfigureScenarioGroundMovement(em, soldier, soldierPosition);
            SetOrAdd(em, soldier, new UnitTransportBoardingTarget { Transport = transport, Goal = rampCell });
            if (!em.HasBuffer<UnitTransportPassengerElement>(transport))
                em.AddBuffer<UnitTransportPassengerElement>(transport);
            if (!em.HasBuffer<UnitTransportHiddenVisualScale>(soldier))
                em.AddBuffer<UnitTransportHiddenVisualScale>(soldier);
        }

        private static void ConfigureTb006TransportPlaneSoldierAirdropScenario(EntityManager em, Entity transport, Entity soldier)
        {
            int2 transportCell = new(18, 18);
            float3 transportPosition = new(transportCell.x + 0.5f, 38f, transportCell.y + 0.5f);
            float3 transportHomePosition = new(transportCell.x + 0.5f, 0f, transportCell.y + 0.5f);

            SetFaction(em, transport, FactionIdentity.PlayerFactionId);
            SetLocalTransform(em, transport, transportPosition, quaternion.identity, 1f);
            SetOrAdd(em, transport, new UnitGrid { Cell = transportCell });
            SetOrAdd(em, transport, new UnitFootprint { Size = new int2(7, 7) });
            SetOrAdd(em, transport, new UnitTransportCapacity { SoldierCapacity = 24 });
            SetOrAdd(em, transport, new UnitTransportCargoCapacity
            {
                SoldierCapacity = 24,
                VehicleCapacity = 2,
                CargoWeightCapacity = 0
            });
            SetOrAdd(em, transport, new UnitMove { Speed = 9f, WalkSpeed = 0f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.1f });
            SetOrAdd(em, transport, new UnitAirMovement { CruiseHeight = 38f, RunwayTaxiSpeed = 5f });
            SetOrAdd(em, transport, new UnitAirComponent
            {
                HomePosition = transportHomePosition,
                HomeCell = transportCell,
                HomeInitialized = 1,
                Airborne = 1,
                ReturningHome = 0,
                TakeoffRolling = 0,
                LandingRolling = 0,
                AttackRunActive = 0,
                ReturnApproachInitialized = 0
            });
            SetOrAdd(em, transport, new UnitTransportPlaneDoorReference
            {
                DoorEntity = Entity.Null,
                ClosedLocalRotation = quaternion.identity,
                OpenLocalRotation = quaternion.identity,
                OpenSeconds = 1.1f,
                CloseSeconds = 0.9f,
                DoorLocalPosition = new float3(0f, 0f, -4f),
                InteriorLocalPosition = new float3(0f, 1.45f, 4f),
                ApproachLocalPosition = new float3(0f, 0f, -5f),
                RolloutLocalPosition = new float3(0f, 0f, -5f)
            });
            SetOrAdd(em, transport, default(UnitTransportPlaneDoorState));
            RemoveIfPresent<UnitTransportAirdropRequest>(em, transport);
            RemoveIfPresent<UnitTransportPlaneDoorOpenRequest>(em, transport);
            if (!em.HasBuffer<UnitTransportPassengerElement>(transport))
                em.AddBuffer<UnitTransportPassengerElement>(transport);

            int2 passengerCell = transportCell;
            SetFaction(em, soldier, FactionIdentity.PlayerFactionId);
            SetLocalTransform(em, soldier, transportPosition, quaternion.identity, 1f);
            SetOrAdd(em, soldier, new UnitGrid { Cell = passengerCell });
            SetOrAdd(em, soldier, new UnitFootprint { Size = new int2(1, 1) });
            SetOrAdd(em, soldier, new UnitMove { Speed = 4f, WalkSpeed = 1.5f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
            ConfigureScenarioGroundMovement(em, soldier, transportPosition);
            SetOrAdd(em, soldier, new UnitTransportPassenger { Transport = transport });
            if (!em.HasComponent<Disabled>(soldier))
                em.AddComponent<Disabled>(soldier);
            if (!em.HasBuffer<UnitTransportHiddenVisualScale>(soldier))
                em.AddBuffer<UnitTransportHiddenVisualScale>(soldier);
            em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = soldier });
            UnitTransportVisualUtility.SetPassengerVisible(em, soldier, false);
        }

        private static void ConfigureTb007TransportPlaneVehicleCargoScenario(EntityManager em, Entity transport, Entity vehicle)
        {
            int2 transportCell = new(17, 17);
            float3 transportPosition = new(transportCell.x + 0.5f, 0f, transportCell.y + 0.5f);

            SetFaction(em, transport, FactionIdentity.PlayerFactionId);
            SetLocalTransform(em, transport, transportPosition, quaternion.identity, 1f);
            SetOrAdd(em, transport, new UnitGrid { Cell = transportCell });
            SetOrAdd(em, transport, new UnitFootprint { Size = new int2(7, 7) });
            SetOrAdd(em, transport, new UnitTransportCapacity { SoldierCapacity = 24 });
            SetOrAdd(em, transport, new UnitTransportCargoCapacity
            {
                SoldierCapacity = 24,
                VehicleCapacity = 2,
                CargoWeightCapacity = 0
            });
            SetOrAdd(em, transport, new UnitMove { Speed = 9f, WalkSpeed = 0f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.1f });
            SetOrAdd(em, transport, new UnitAirMovement { CruiseHeight = 18f, RunwayTaxiSpeed = 5f });
            SetOrAdd(em, transport, new UnitAirComponent
            {
                HomePosition = transportPosition,
                HomeCell = transportCell,
                HomeInitialized = 1,
                Airborne = 0,
                ReturningHome = 0,
                TakeoffRolling = 0,
                LandingRolling = 0,
                AttackRunActive = 0,
                ReturnApproachInitialized = 0
            });
            SetOrAdd(em, transport, new UnitTransportPlaneDoorReference
            {
                DoorEntity = Entity.Null,
                ClosedLocalRotation = quaternion.identity,
                OpenLocalRotation = quaternion.identity,
                OpenSeconds = 1.1f,
                CloseSeconds = 0.9f,
                DoorLocalPosition = new float3(0f, 0f, -4f),
                InteriorLocalPosition = new float3(0f, 1.45f, 4f),
                ApproachLocalPosition = new float3(0f, 0f, -5f),
                RolloutLocalPosition = new float3(0f, 0f, -5f)
            });
            SetOrAdd(em, transport, default(UnitTransportPlaneDoorState));
            RemoveIfPresent<UnitTransportAirdropRequest>(em, transport);
            RemoveIfPresent<UnitTransportPlaneDoorOpenRequest>(em, transport);

            GridConfig grid = ResolveScenarioGrid(em);
            int2 rampCell = TransportBoardingCommandSystem.ResolvePlaneRampApproachCell(em, grid, transport);
            float3 vehiclePosition = new(rampCell.x + 0.5f, 0f, rampCell.y + 0.5f);

            SetFaction(em, vehicle, FactionIdentity.PlayerFactionId);
            SetLocalTransform(em, vehicle, vehiclePosition, quaternion.identity, 1f);
            SetOrAdd(em, vehicle, new UnitGrid { Cell = rampCell });
            SetOrAdd(em, vehicle, new UnitFootprint { Size = new int2(3, 3) });
            SetOrAdd(em, vehicle, new UnitMove { Speed = 7f, WalkSpeed = 1.5f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
            ConfigureScenarioVehicleGroundMovement(em, vehicle, vehiclePosition);
            SetOrAdd(em, vehicle, new UnitTransportBoardingTarget
            {
                Transport = transport,
                Goal = rampCell,
                PassengerKind = UnitTransportPassengerKind.Vehicle,
                CargoWeight = 9
            });
            if (!em.HasBuffer<UnitTransportPassengerElement>(transport))
                em.AddBuffer<UnitTransportPassengerElement>(transport);
            if (!em.HasBuffer<UnitTransportHiddenVisualScale>(vehicle))
                em.AddBuffer<UnitTransportHiddenVisualScale>(vehicle);
        }

        private static void ConfigureTb008TransportPlaneVehicleCargoAirdropScenario(EntityManager em, Entity transport, Entity vehicle)
        {
            int2 transportCell = new(18, 18);
            float3 transportPosition = new(transportCell.x + 0.5f, 40f, transportCell.y + 0.5f);
            float3 transportHomePosition = new(transportCell.x + 0.5f, 0f, transportCell.y + 0.5f);

            SetFaction(em, transport, FactionIdentity.PlayerFactionId);
            SetLocalTransform(em, transport, transportPosition, quaternion.identity, 1f);
            SetOrAdd(em, transport, new UnitGrid { Cell = transportCell });
            SetOrAdd(em, transport, new UnitFootprint { Size = new int2(7, 7) });
            SetOrAdd(em, transport, new UnitTransportCapacity { SoldierCapacity = 24 });
            SetOrAdd(em, transport, new UnitTransportCargoCapacity
            {
                SoldierCapacity = 24,
                VehicleCapacity = 2,
                CargoWeightCapacity = 0
            });
            SetOrAdd(em, transport, new UnitMove { Speed = 9f, WalkSpeed = 0f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.1f });
            SetOrAdd(em, transport, new UnitAirMovement { CruiseHeight = 40f, RunwayTaxiSpeed = 5f });
            SetOrAdd(em, transport, new UnitAirComponent
            {
                HomePosition = transportHomePosition,
                HomeCell = transportCell,
                HomeInitialized = 1,
                Airborne = 1,
                ReturningHome = 0,
                TakeoffRolling = 0,
                LandingRolling = 0,
                AttackRunActive = 0,
                ReturnApproachInitialized = 0
            });
            SetOrAdd(em, transport, new UnitTransportPlaneDoorReference
            {
                DoorEntity = Entity.Null,
                ClosedLocalRotation = quaternion.identity,
                OpenLocalRotation = quaternion.identity,
                OpenSeconds = 1.1f,
                CloseSeconds = 0.9f,
                DoorLocalPosition = new float3(0f, 0f, -4f),
                InteriorLocalPosition = new float3(0f, 1.45f, 4f),
                ApproachLocalPosition = new float3(0f, 0f, -5f),
                RolloutLocalPosition = new float3(0f, 0f, -5f)
            });
            SetOrAdd(em, transport, default(UnitTransportPlaneDoorState));
            RemoveIfPresent<UnitTransportAirdropRequest>(em, transport);
            RemoveIfPresent<UnitTransportPlaneDoorOpenRequest>(em, transport);
            if (!em.HasBuffer<UnitTransportPassengerElement>(transport))
                em.AddBuffer<UnitTransportPassengerElement>(transport);

            SetFaction(em, vehicle, FactionIdentity.PlayerFactionId);
            SetLocalTransform(em, vehicle, transportPosition, quaternion.identity, 1f);
            SetOrAdd(em, vehicle, new UnitGrid { Cell = transportCell });
            SetOrAdd(em, vehicle, new UnitFootprint { Size = new int2(3, 3) });
            SetOrAdd(em, vehicle, new UnitMove { Speed = 7f, WalkSpeed = 1.5f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
            ConfigureScenarioVehicleGroundMovement(em, vehicle, transportPosition);
            SetOrAdd(em, vehicle, new UnitTransportPassenger { Transport = transport });
            SetOrAdd(em, vehicle, new UnitTransportCargoPassenger
            {
                Transport = transport,
                PassengerKind = UnitTransportPassengerKind.Vehicle,
                CargoWeight = 9
            });
            if (!em.HasComponent<Disabled>(vehicle))
                em.AddComponent<Disabled>(vehicle);
            if (!em.HasBuffer<UnitTransportHiddenVisualScale>(vehicle))
                em.AddBuffer<UnitTransportHiddenVisualScale>(vehicle);
            em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = vehicle });
            UnitTransportVisualUtility.SetPassengerVisible(em, vehicle, false);
        }

        private static void ConfigureTb009TransportPlaneMixedLoadAirdropScenario(EntityManager em, Entity transport, Entity soldier, Entity vehicle)
        {
            int2 transportCell = new(18, 18);
            float3 transportPosition = new(transportCell.x + 0.5f, 42f, transportCell.y + 0.5f);
            float3 transportHomePosition = new(transportCell.x + 0.5f, 0f, transportCell.y + 0.5f);

            SetFaction(em, transport, FactionIdentity.PlayerFactionId);
            SetLocalTransform(em, transport, transportPosition, quaternion.identity, 1f);
            SetOrAdd(em, transport, new UnitGrid { Cell = transportCell });
            SetOrAdd(em, transport, new UnitFootprint { Size = new int2(7, 7) });
            SetOrAdd(em, transport, new UnitTransportCapacity { SoldierCapacity = 24 });
            SetOrAdd(em, transport, new UnitTransportCargoCapacity
            {
                SoldierCapacity = 24,
                VehicleCapacity = 2,
                CargoWeightCapacity = 0
            });
            SetOrAdd(em, transport, new UnitMove { Speed = 9f, WalkSpeed = 0f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.1f });
            SetOrAdd(em, transport, new UnitAirMovement { CruiseHeight = 42f, RunwayTaxiSpeed = 5f });
            SetOrAdd(em, transport, new UnitAirComponent
            {
                HomePosition = transportHomePosition,
                HomeCell = transportCell,
                HomeInitialized = 1,
                Airborne = 1,
                ReturningHome = 0,
                TakeoffRolling = 0,
                LandingRolling = 0,
                AttackRunActive = 0,
                ReturnApproachInitialized = 0
            });
            SetOrAdd(em, transport, new UnitTransportPlaneDoorReference
            {
                DoorEntity = Entity.Null,
                ClosedLocalRotation = quaternion.identity,
                OpenLocalRotation = quaternion.identity,
                OpenSeconds = 1.1f,
                CloseSeconds = 0.9f,
                DoorLocalPosition = new float3(0f, 0f, -4f),
                InteriorLocalPosition = new float3(0f, 1.45f, 4f),
                ApproachLocalPosition = new float3(0f, 0f, -5f),
                RolloutLocalPosition = new float3(0f, 0f, -5f)
            });
            SetOrAdd(em, transport, default(UnitTransportPlaneDoorState));
            RemoveIfPresent<UnitTransportAirdropRequest>(em, transport);
            RemoveIfPresent<UnitTransportPlaneDoorOpenRequest>(em, transport);
            if (!em.HasBuffer<UnitTransportPassengerElement>(transport))
                em.AddBuffer<UnitTransportPassengerElement>(transport);

            SetFaction(em, soldier, FactionIdentity.PlayerFactionId);
            SetLocalTransform(em, soldier, transportPosition, quaternion.identity, 1f);
            SetOrAdd(em, soldier, new UnitGrid { Cell = transportCell });
            SetOrAdd(em, soldier, new UnitFootprint { Size = new int2(1, 1) });
            SetOrAdd(em, soldier, new UnitMove { Speed = 4f, WalkSpeed = 1.5f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
            ConfigureScenarioGroundMovement(em, soldier, transportPosition);
            SetOrAdd(em, soldier, new UnitTransportPassenger { Transport = transport });
            if (!em.HasComponent<Disabled>(soldier))
                em.AddComponent<Disabled>(soldier);
            if (!em.HasBuffer<UnitTransportHiddenVisualScale>(soldier))
                em.AddBuffer<UnitTransportHiddenVisualScale>(soldier);
            em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = soldier });
            UnitTransportVisualUtility.SetPassengerVisible(em, soldier, false);

            SetFaction(em, vehicle, FactionIdentity.PlayerFactionId);
            SetLocalTransform(em, vehicle, transportPosition, quaternion.identity, 1f);
            SetOrAdd(em, vehicle, new UnitGrid { Cell = transportCell });
            SetOrAdd(em, vehicle, new UnitFootprint { Size = new int2(3, 3) });
            SetOrAdd(em, vehicle, new UnitMove { Speed = 7f, WalkSpeed = 1.5f, RoadSpeedMultiplier = 1f, ArriveDistance = 0.05f });
            ConfigureScenarioVehicleGroundMovement(em, vehicle, transportPosition);
            SetOrAdd(em, vehicle, new UnitTransportPassenger { Transport = transport });
            SetOrAdd(em, vehicle, new UnitTransportCargoPassenger
            {
                Transport = transport,
                PassengerKind = UnitTransportPassengerKind.Vehicle,
                CargoWeight = 9
            });
            if (!em.HasComponent<Disabled>(vehicle))
                em.AddComponent<Disabled>(vehicle);
            if (!em.HasBuffer<UnitTransportHiddenVisualScale>(vehicle))
                em.AddBuffer<UnitTransportHiddenVisualScale>(vehicle);
            em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement { Passenger = vehicle });
            UnitTransportVisualUtility.SetPassengerVisible(em, vehicle, false);
        }

        private static void ConfigureScenarioGroundMovement(EntityManager em, Entity entity, float3 worldPosition)
        {
            RemoveIfPresent<UnitAirMovement>(em, entity);
            RemoveIfPresent<UnitAirComponent>(em, entity);
            SetOrAdd(em, entity, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 0 });
            SetOrAdd(em, entity, new UnitVehicleMovement
            {
                TurnSpeedDegrees = 720f,
                Acceleration = 20f,
                Braking = 20f,
                RearPivotOffset = 0f
            });
            SetOrAdd(em, entity, new UnitVehicleKinematics { CurrentSpeed = 0f, StallSeconds = 0f });
            SetOrAdd(em, entity, new UnitPrevWorldPos { Value = worldPosition });
            SetOrAdd(em, entity, new UnitMoveVisualComponent { IsMoving = 0, StillSeconds = 0f });
        }

        private static void ConfigureScenarioVehicleGroundMovement(EntityManager em, Entity entity, float3 worldPosition)
        {
            ConfigureScenarioGroundMovement(em, entity, worldPosition);
            SetOrAdd(em, entity, new UnitMovementBehavior { AllowIdleWander = 0, UsesVehicleMotion = 1 });
            SetOrAdd(em, entity, new UnitVehicleMovement
            {
                TurnSpeedDegrees = 360f,
                Acceleration = 18f,
                Braking = 18f,
                RearPivotOffset = 0f
            });
        }

        private static void QueueDisembarkCommand(EntityManager em, Entity transport)
        {
            QueueDisembarkCommand(em, transport, default, false);
        }

        private static void QueueDisembarkCommand(EntityManager em, Entity transport, int2 targetCell, bool hasTargetCell)
        {
            Entity commandEntity = CreateScenarioCommandQueue(em, "BattleScenarioLabTransportCommand");
            DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests =
                em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
            requests.Add(new RtsSelectionCommandIntentRequestElement
            {
                Kind = RtsSelectionCommandIntentKind.DisembarkTransport,
                TargetEntity = transport,
                TargetCell = targetCell,
                TargetKind = hasTargetCell
                    ? RtsSelectionCommandTargetKind.Cell
                    : RtsSelectionCommandTargetKind.Entity,
                HasTargetEntity = 1,
                HasTargetCell = hasTargetCell ? (byte)1 : (byte)0
            });
        }

        private static void QueueBoardTransportCommand(EntityManager em, Entity transport)
        {
            Entity commandEntity = CreateScenarioCommandQueue(em, "BattleScenarioLabTransportBoardCommand");
            DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests =
                em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
            requests.Add(new RtsSelectionCommandIntentRequestElement
            {
                Kind = RtsSelectionCommandIntentKind.BoardTransport,
                TargetEntity = transport,
                HasTargetEntity = 1
            });
        }

        private static Entity CreateScenarioCommandQueue(EntityManager em, string name)
        {
            DestroyEntitiesWithTree<BattleScenarioLabCommandTag>(em);
            Entity commandEntity = em.CreateEntity(typeof(BattleScenarioLabCommandTag), typeof(RtsSelectionInputStateComponent));
            em.SetName(commandEntity, name);
            em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
            em.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);
            return commandEntity;
        }

        private static void ResetAirLauncherForGroundMissile(EntityManager em, Entity airLauncher)
        {
            ResetAirLauncher(
                em,
                airLauncher,
                airTargetPriority: 25f,
                incomingMissilePriority: 100f,
                turnRateDegreesPerSecond: ScenarioAirMissileTurnRateDegreesPerSecond,
                proximityFuseRadius: VisualInterceptProximityFuseRadius,
                trackingQuality: ScenarioAirTrackingQuality,
                airTargetDamage: 120);
        }

        private static void ResetAirLauncherForAirTarget(EntityManager em, Entity airLauncher)
        {
            ResetAirLauncher(
                em,
                airLauncher,
                airTargetPriority: 100f,
                incomingMissilePriority: 25f,
                turnRateDegreesPerSecond: ScenarioAirTargetMissileTurnRateDegreesPerSecond,
                proximityFuseRadius: VisualAirTargetProximityFuseRadius,
                trackingQuality: ScenarioAirTargetTrackingQuality,
                airTargetDamage: 140);
        }

        private static void ResetAirLauncher(
            EntityManager em,
            Entity airLauncher,
            float airTargetPriority,
            float incomingMissilePriority,
            float turnRateDegreesPerSecond,
            float proximityFuseRadius,
            float trackingQuality,
            int airTargetDamage)
        {
            if (!em.HasComponent<AirMissileLauncherComponent>(airLauncher) ||
                !em.HasComponent<AirMissileLauncherStateComponent>(airLauncher))
            {
                return;
            }

            AirMissileLauncherComponent launcher = em.GetComponentData<AirMissileLauncherComponent>(airLauncher);
            launcher.MinRange = 4f;
            launcher.BaseDetectionRange = ScenarioAirBaseDetectionRange;
            launcher.MaxDetectionRange = ScenarioAirMaxDetectionRange;
            launcher.AirTargetPriority = math.max(launcher.AirTargetPriority, airTargetPriority);
            launcher.IncomingMissilePriority = math.max(launcher.IncomingMissilePriority, incomingMissilePriority);
            launcher.TurretYawSpeedDegreesPerSecond = math.max(launcher.TurretYawSpeedDegreesPerSecond, 900f);
            launcher.AimToleranceDegrees = math.max(launcher.AimToleranceDegrees, 5f);
            launcher.LockSeconds = ScenarioAirLockSeconds;
            launcher.LaunchDelaySeconds = ScenarioAirLaunchDelaySeconds;
            launcher.MissileSpeed = ScenarioAirMissileSpeed;
            launcher.MissileAcceleration = 0f;
            launcher.MissileTurnRateDegreesPerSecond = turnRateDegreesPerSecond;
            launcher.MissileLifetimeSeconds = ScenarioAirMissileLifetimeSeconds;
            launcher.ProximityFuseRadius = proximityFuseRadius;
            launcher.AirTargetDamage = math.max(launcher.AirTargetDamage, airTargetDamage);
            launcher.IncomingMissileDamage = math.max(launcher.IncomingMissileDamage, 9999);
            launcher.TrackingQuality = trackingQuality;
            launcher.MaxSupportRangeBonus = math.max(launcher.MaxSupportRangeBonus, 120f);
            launcher.MaxSupportTrackingBonus = math.max(launcher.MaxSupportTrackingBonus, 0.3f);
            em.SetComponentData(airLauncher, launcher);

            em.SetComponentData(airLauncher, new AirMissileLauncherStateComponent
            {
                Phase = (byte)AirMissileLauncherPhase.Idle,
                TargetEntity = Entity.Null,
                TargetKind = (byte)AirMissileTargetKind.None,
                TargetWorldPosition = float3.zero,
                PredictedInterceptPosition = float3.zero,
                Timer = 0f,
                SelectedMissileSlot = -1,
                EffectiveRange = launcher.BaseDetectionRange,
                EffectiveLockSeconds = launcher.LockSeconds,
                EffectiveTrackingQuality = launcher.TrackingQuality,
                EffectiveTurnRateDegreesPerSecond = launcher.MissileTurnRateDegreesPerSecond
            });

            if (em.HasComponent<AirMissileLauncherTargetComponent>(airLauncher))
                em.RemoveComponent<AirMissileLauncherTargetComponent>(airLauncher);
        }

        private static void StartGroundMissileLaunch(EntityManager em, Entity groundLauncher, BattleScenarioVariant variant)
        {
            if (!em.HasComponent<GroundMissileLauncherComponent>(groundLauncher) ||
                !em.HasComponent<GroundMissileLauncherStateComponent>(groundLauncher) ||
                !em.HasComponent<LocalTransform>(groundLauncher))
            {
                return;
            }

            GroundMissileLauncherComponent launcher = em.GetComponentData<GroundMissileLauncherComponent>(groundLauncher);
            LocalTransform launcherTransform = em.GetComponentData<LocalTransform>(groundLauncher);
            float horizontalDistance = math.distance(
                new float2(launcherTransform.Position.x, launcherTransform.Position.z),
                new float2(DefendedTargetPosition.x, DefendedTargetPosition.z));
            float flightSeconds = ScenarioGroundMissileBaseFlightSeconds /
                                  math.max(0.1f, variant.IncomingThreatSpeedMultiplier);
            launcher.RocketSpeed = horizontalDistance / math.max(0.35f, flightSeconds);
            launcher.ArcHeight = math.min(
                math.max(0f, variant.IncomingThreatAltitude),
                VisualGroundMissileArcHeight);
            em.SetComponentData(groundLauncher, launcher);

            int rocketCount = em.HasBuffer<GroundMissileLauncherRocketVisualComponent>(groundLauncher)
                ? em.GetBuffer<GroundMissileLauncherRocketVisualComponent>(groundLauncher).Length
                : 0;

            em.SetComponentData(groundLauncher, new GroundMissileLauncherStateComponent
            {
                Phase = (byte)GroundMissileLauncherPhase.Preparing,
                TargetEntity = Entity.Null,
                TargetCell = default,
                TargetWorldPosition = DefendedTargetPosition,
                Timer = GroundMissileLauncherTiming.PrepareAndHoldSeconds(launcher.PrepareSeconds),
                SelectedRocketSlot = rocketCount > 0 ? 0 : -1
            });
        }

        private static void ResetPreviousRun(EntityManager em)
        {
            RestoreAirMissileVisuals(em);
            RestoreGroundRocketVisuals(em);
            DestroyScenarioLabUnits(em);
            DestroyEntitiesWithTree<GroundMissileProjectileComponent>(em);
            DestroyEntitiesWithTree<AirMissileProjectileComponent>(em);
            DestroyEntitiesWithTree<GroundMissileImpactRequestComponent>(em);
            DestroyEntitiesWithTree<VehicleWreckComponent>(em);
            DestroyEntitiesWithTree<VehicleDestroyedVisualSpawnRequest>(em);
            DestroyEntitiesWithTree<BattleScenarioLabRuntimeGridTag>(em);
            DestroyEntitiesWithTree<BattleScenarioLabRuntimeGameplayStateTag>(em);
            DestroyEntitiesWithTree<BattleScenarioLabCommandTag>(em);
            DestroyOrphanRenderableEntities(em);
            RemoveComponentFromAll<AirMissileImpactRequestComponent>(em);
            RemoveComponentFromAll<AirMissileProjectileTrailComponent>(em);
            RemoveComponentFromAll<AirMissileLauncherTargetComponent>(em);
            RemoveComponentFromAll<MissileInterceptedComponent>(em);
        }

        private void HidePreviewMarkers()
        {
            if (groundLauncherRoot != null)
                groundLauncherRoot.gameObject.SetActive(false);
            if (airLauncherRoot != null)
                airLauncherRoot.gameObject.SetActive(false);
            if (radarRoot != null)
                radarRoot.gameObject.SetActive(false);
            if (defendedTargetVisual != null)
                defendedTargetVisual.gameObject.SetActive(false);
        }

        private static void ClearPooledPresentationVfx()
        {
            MissileTrailVfxView.ClearAll();
            UnitAttackImpactVfxView.ClearAll();
        }

        private static void DestroyScenarioLabUnits(EntityManager em)
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<UnitSourcePrefabKey>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!em.Exists(entity) || !em.HasComponent<UnitSourcePrefabKey>(entity) || em.HasComponent<Prefab>(entity))
                    continue;

                FixedString64Bytes key = em.GetComponentData<UnitSourcePrefabKey>(entity).Value;
                if (SourceKeyMatches(key, GroundLauncherKey) ||
                    SourceKeyMatches(key, AirLauncherKey) ||
                    SourceKeyMatches(key, RadarKey) ||
                    SourceKeyMatches(key, JetTargetKey) ||
                    SourceKeyMatches(key, HelicopterTargetKey) ||
                    SourceKeyMatches(key, DroneTargetKey) ||
                    SourceKeyMatches(key, SoldierPassengerKey) ||
                    SourceKeyMatches(key, GroundVehicleTransportKey) ||
                    SourceKeyMatches(key, HelicopterTransportKey) ||
                    SourceKeyMatches(key, PlaneTransportKey) ||
                    SourceKeyMatches(key, VehicleCargoPassengerKey))
                {
                    DestroyScenarioLabUnitTransientVisuals(em, entity);
                    DestroyLinkedEntityGroup(em, entity);
                }
            }
        }

        private static void DestroyScenarioLabUnitTransientVisuals(EntityManager em, Entity entity)
        {
            if (em.HasComponent<VehicleDestroyedVisualInstanceReference>(entity))
            {
                VehicleDestroyedVisualInstanceReference destroyedVisual = em.GetComponentData<VehicleDestroyedVisualInstanceReference>(entity);
                VehicleVisualEntityUtility.DestroyVisualTree(em, destroyedVisual.Instance);
            }

            if (em.HasComponent<UnitSelectionMarkerInstanceReference>(entity))
            {
                UnitSelectionMarkerInstanceReference marker = em.GetComponentData<UnitSelectionMarkerInstanceReference>(entity);
                VehicleVisualEntityUtility.DestroyVisualTree(em, marker.Instance);
            }

            if (em.HasComponent<UnitHealthBarInstanceReference>(entity))
            {
                UnitHealthBarInstanceReference healthBar = em.GetComponentData<UnitHealthBarInstanceReference>(entity);
                VehicleVisualEntityUtility.DestroyVisualTree(em, healthBar.Instance);
            }
        }

        private static void DestroyLinkedEntityGroup(EntityManager em, Entity root)
        {
            if (!em.Exists(root))
                return;

            NativeList<Entity> entities = new(16, Allocator.Temp);
            try
            {
                if (em.HasBuffer<LinkedEntityGroup>(root))
                {
                    DynamicBuffer<LinkedEntityGroup> linkedGroup = em.GetBuffer<LinkedEntityGroup>(root);
                    for (int i = 0; i < linkedGroup.Length; i++)
                        CollectEntityTree(em, linkedGroup[i].Value, ref entities);
                }
                else
                {
                    CollectEntityTree(em, root, ref entities);
                }

                for (int i = entities.Length - 1; i >= 0; i--)
                {
                    Entity entity = entities[i];
                    if (em.Exists(entity))
                        em.DestroyEntity(entity);
                }
            }
            finally
            {
                entities.Dispose();
            }
        }

        private static void CollectEntityTree(EntityManager em, Entity entity, ref NativeList<Entity> entities)
        {
            if (entity == Entity.Null || !em.Exists(entity) || Contains(entities, entity))
                return;

            entities.Add(entity);

            if (em.HasComponent<UnitModelInstanceReference>(entity))
                CollectEntityTree(em, em.GetComponentData<UnitModelInstanceReference>(entity).Instance, ref entities);
            if (em.HasComponent<UnitDetailedVisualReference>(entity))
                CollectEntityTree(em, em.GetComponentData<UnitDetailedVisualReference>(entity).Root, ref entities);
            if (em.HasComponent<VehicleDestroyedVisualInstanceReference>(entity))
                CollectEntityTree(em, em.GetComponentData<VehicleDestroyedVisualInstanceReference>(entity).Instance, ref entities);
            if (em.HasComponent<UnitSelectionMarkerInstanceReference>(entity))
                CollectEntityTree(em, em.GetComponentData<UnitSelectionMarkerInstanceReference>(entity).Instance, ref entities);
            if (em.HasComponent<UnitHealthBarInstanceReference>(entity))
                CollectEntityTree(em, em.GetComponentData<UnitHealthBarInstanceReference>(entity).Instance, ref entities);

            if (!em.HasBuffer<Child>(entity))
                return;

            DynamicBuffer<Child> children = em.GetBuffer<Child>(entity);
            for (int i = 0; i < children.Length; i++)
                CollectEntityTree(em, children[i].Value, ref entities);
        }

        private static bool Contains(NativeList<Entity> entities, Entity entity)
        {
            for (int i = 0; i < entities.Length; i++)
                if (entities[i] == entity)
                    return true;

            return false;
        }

        private static void RestoreAirMissileVisuals(EntityManager em)
        {
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<AirMissileFlyingVisualComponent>(),
                ComponentType.ReadWrite<LocalTransform>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                AirMissileFlyingVisualComponent visual = em.GetComponentData<AirMissileFlyingVisualComponent>(entity);
                if (visual.OriginalParent != Entity.Null && em.Exists(visual.OriginalParent) && !em.HasComponent<Parent>(entity))
                    em.AddComponentData(entity, new Parent { Value = visual.OriginalParent });

                em.SetComponentData(
                    entity,
                    LocalTransform.FromPositionRotationScale(
                        visual.InitialLocalPosition,
                        visual.InitialLocalRotation,
                        math.max(0.0001f, visual.InitialLocalScale)));

                if (em.HasComponent<AirMissileProjectileComponent>(entity))
                    em.RemoveComponent<AirMissileProjectileComponent>(entity);
                if (em.HasComponent<AirMissileProjectileTrailComponent>(entity))
                    em.RemoveComponent<AirMissileProjectileTrailComponent>(entity);
                if (em.HasComponent<AirMissileImpactRequestComponent>(entity))
                    em.RemoveComponent<AirMissileImpactRequestComponent>(entity);
                em.RemoveComponent<AirMissileFlyingVisualComponent>(entity);
            }
        }

        private static void RestoreGroundRocketVisuals(EntityManager em)
        {
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<GroundMissileFlyingRocketVisualComponent>(),
                ComponentType.ReadWrite<LocalTransform>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                GroundMissileFlyingRocketVisualComponent visual = em.GetComponentData<GroundMissileFlyingRocketVisualComponent>(entity);
                if (visual.OriginalParent != Entity.Null && em.Exists(visual.OriginalParent) && !em.HasComponent<Parent>(entity))
                    em.AddComponentData(entity, new Parent { Value = visual.OriginalParent });

                em.SetComponentData(
                    entity,
                    LocalTransform.FromPositionRotationScale(
                        visual.InitialLocalPosition,
                        visual.InitialLocalRotation,
                        math.max(0.0001f, visual.InitialLocalScale)));
                em.RemoveComponent<GroundMissileFlyingRocketVisualComponent>(entity);
            }
        }

        private IEnumerator CameraAirTargetRoutine(EntityManager em, BattleScenarioVariant variant, Entity airLauncher, Entity target)
        {
            float startedAt = Time.time;
            bool sawAirProjectile = false;
            bool sawImpact = false;
            Vector3 lastFocus = TryGetPosition(em, target, out float3 initialTargetPosition)
                ? (Vector3)initialTargetPosition
                : (Vector3)ResolveAirTargetPosition(variant);

            while (Time.time - startedAt < 14f)
            {
                Entity airProjectile = FindFirstEntity<AirMissileProjectileComponent>(em);
                bool hasAirProjectile = TryGetPosition(em, airProjectile, out float3 airPosition);
                bool hasTarget = TryGetPosition(em, target, out float3 targetPosition);

                if (hasAirProjectile)
                {
                    sawAirProjectile = true;
                    lastFocus = (Vector3)airPosition;
                }

                if (target != Entity.Null &&
                    em.Exists(target) &&
                    em.HasComponent<UnitHealth>(target) &&
                    em.GetComponentData<UnitHealth>(target).Current <= 0)
                {
                    sawImpact = true;
                }

                if (hasAirProjectile && hasTarget)
                {
                    Vector3 midpoint = ((Vector3)airPosition + (Vector3)targetPosition) * 0.5f;
                    SetCamera(midpoint + new Vector3(18f, 10f, -30f), midpoint);
                }
                else if (hasAirProjectile)
                {
                    SetCamera((Vector3)airPosition + new Vector3(18f, 9f, -24f), lastFocus);
                }
                else if (hasTarget && !sawAirProjectile)
                {
                    Vector3 targetVector = (Vector3)targetPosition;
                    SetCamera(targetVector + new Vector3(34f, 15f, -44f), targetVector + new Vector3(-24f, 0f, 0f));
                }
                else if (sawImpact)
                {
                    SetCamera(lastFocus + new Vector3(18f, 12f, -30f), lastFocus);
                    yield return new WaitForSeconds(2f);
                    break;
                }
                else
                {
                    TryGetPosition(em, airLauncher, out float3 airLauncherPosition);
                    SetCamera((Vector3)airLauncherPosition + new Vector3(26f, 13f, -30f), (Vector3)airLauncherPosition + new Vector3(36f, 10f, 0f));
                }

                if (sawAirProjectile && !hasAirProjectile)
                {
                    SetCamera(lastFocus + new Vector3(18f, 12f, -30f), lastFocus);
                    yield return new WaitForSeconds(2f);
                    break;
                }

                yield return null;
            }
        }

        private IEnumerator CameraOpeningRoutine(EntityManager em, BattleScenarioVariant variant, Entity airLauncher, Entity groundLauncher)
        {
            float startedAt = Time.time;
            bool sawGroundProjectile = false;
            bool sawAirProjectile = false;
            Vector3 lastFocus = new(Mathf.Max(40f, variant.IncomingThreatStartDistance) * 0.5f, 10f, 0f);

            while (Time.time - startedAt < 16f)
            {
                Entity groundProjectile = FindFirstEntity<GroundMissileProjectileComponent>(em);
                Entity airProjectile = FindFirstEntity<AirMissileProjectileComponent>(em);
                bool hasGroundProjectile = TryGetPosition(em, groundProjectile, out float3 groundPosition);
                bool hasAirProjectile = TryGetPosition(em, airProjectile, out float3 airPosition);

                if (hasGroundProjectile)
                {
                    sawGroundProjectile = true;
                    lastFocus = (Vector3)groundPosition;
                }

                if (hasAirProjectile)
                {
                    sawAirProjectile = true;
                    lastFocus = (Vector3)airPosition;
                }

                if (hasAirProjectile && hasGroundProjectile)
                {
                    Vector3 midpoint = ((Vector3)airPosition + (Vector3)groundPosition) * 0.5f;
                    SetCamera(midpoint + new Vector3(22f, 13f, -32f), midpoint);
                }
                else if (hasAirProjectile)
                {
                    SetCamera((Vector3)airPosition + new Vector3(18f, 9f, -24f), Vector3.Lerp((Vector3)airPosition, lastFocus, 0.45f));
                }
                else if (hasGroundProjectile)
                {
                    SetCamera((Vector3)groundPosition + new Vector3(24f, 10f, -30f), (Vector3)groundPosition + Vector3.left * 12f);
                }
                else if (!sawGroundProjectile)
                {
                    TryGetPosition(em, groundLauncher, out float3 groundLauncherPosition);
                    SetCamera((Vector3)groundLauncherPosition + new Vector3(26f, 12f, -28f), (Vector3)groundLauncherPosition + new Vector3(-18f, 4f, 0f));
                }
                else if (sawAirProjectile)
                {
                    SetCamera(lastFocus + new Vector3(24f, 14f, -34f), lastFocus);
                    yield return new WaitForSeconds(2f);
                    break;
                }
                else
                {
                    TryGetPosition(em, airLauncher, out float3 airLauncherPosition);
                    SetCamera((Vector3)airLauncherPosition + new Vector3(26f, 13f, -30f), (Vector3)airLauncherPosition + new Vector3(26f, 8f, 0f));
                }

                yield return null;
            }
        }

        private IEnumerator CameraTransportBoardAndGroundExitRoutine(EntityManager em, Entity transport, Entity passenger)
        {
            float startedAt = Time.time;
            bool sawBoarded = false;
            bool queuedExit = false;
            bool sawExited = false;
            Vector3 fallbackFocus = new(8.8f, 1.2f, 8.4f);

            while (Time.time - startedAt < 14f)
            {
                bool hasTransportPosition = TryGetPosition(em, transport, out float3 transportPosition);
                bool hasPassengerPosition = TryGetPosition(em, passenger, out float3 passengerPosition);
                bool passengerLoaded = passenger != Entity.Null &&
                                       em.Exists(passenger) &&
                                       em.HasComponent<UnitTransportPassenger>(passenger);
                bool passengerVisible = passenger != Entity.Null &&
                                        em.Exists(passenger) &&
                                        !em.HasComponent<Disabled>(passenger) &&
                                        !em.HasComponent<UnitTransportPassenger>(passenger);

                Vector3 focus = hasTransportPosition ? (Vector3)transportPosition : fallbackFocus;
                if (hasPassengerPosition && !passengerLoaded)
                    focus = Vector3.Lerp((Vector3)passengerPosition, focus, 0.55f);

                if (!sawBoarded && passengerLoaded)
                    sawBoarded = true;

                if (sawBoarded && !queuedExit)
                {
                    yield return new WaitForSeconds(1.2f);
                    if (em.Exists(transport))
                        QueueDisembarkCommand(em, transport);
                    queuedExit = true;
                }

                if (sawBoarded && queuedExit && passengerVisible && hasPassengerPosition)
                    sawExited = true;

                if (!sawBoarded)
                {
                    SetCamera(focus + new Vector3(7f, 5f, -9f), focus);
                }
                else if (!sawExited)
                {
                    SetCamera(focus + new Vector3(5f, 4f, -8f), focus + Vector3.up * 0.5f);
                }
                else
                {
                    Vector3 passengerFocus = hasPassengerPosition ? (Vector3)passengerPosition : focus;
                    SetCamera(passengerFocus + new Vector3(6f, 4f, -8f), Vector3.Lerp(passengerFocus, focus, 0.45f));
                    yield return new WaitForSeconds(2f);
                    break;
                }

                yield return null;
            }
        }

        private IEnumerator CameraTransportBoardAndRopeExitRoutine(EntityManager em, Entity transport, Entity passenger)
        {
            float startedAt = Time.time;
            bool sawBoarded = false;
            bool queuedExit = false;
            bool sawRopeDrop = false;
            bool sawRopeSettled = false;
            Vector3 fallbackFocus = new(11f, 2f, 10f);

            while (Time.time - startedAt < 18f)
            {
                bool hasTransportPosition = TryGetPosition(em, transport, out float3 transportPosition);
                bool hasPassengerPosition = TryGetPosition(em, passenger, out float3 passengerPosition);
                bool passengerLoaded = passenger != Entity.Null &&
                                       em.Exists(passenger) &&
                                       em.HasComponent<UnitTransportPassenger>(passenger);
                bool passengerDropping = passenger != Entity.Null &&
                                         em.Exists(passenger) &&
                                         em.HasComponent<UnitTransportRopeDropComponent>(passenger);
                bool passengerSettled = passenger != Entity.Null &&
                                        em.Exists(passenger) &&
                                        !em.HasComponent<Disabled>(passenger) &&
                                        !em.HasComponent<UnitTransportPassenger>(passenger) &&
                                        !em.HasComponent<UnitTransportRopeDropComponent>(passenger) &&
                                        sawRopeDrop;

                Vector3 transportFocus = hasTransportPosition ? (Vector3)transportPosition : fallbackFocus;
                Vector3 focus = transportFocus;
                if (hasPassengerPosition && (!passengerLoaded || passengerDropping))
                    focus = Vector3.Lerp((Vector3)passengerPosition, transportFocus, passengerDropping ? 0.2f : 0.55f);

                if (!sawBoarded && passengerLoaded)
                    sawBoarded = true;

                if (sawBoarded && !queuedExit)
                {
                    yield return new WaitForSeconds(1.2f);
                    if (em.Exists(transport))
                        QueueDisembarkCommand(em, transport);
                    queuedExit = true;
                }

                if (passengerDropping)
                    sawRopeDrop = true;

                if (passengerSettled)
                    sawRopeSettled = true;

                if (!sawBoarded)
                {
                    SetCamera(focus + new Vector3(8f, 5f, -10f), focus);
                }
                else if (!sawRopeDrop)
                {
                    SetCamera(transportFocus + new Vector3(9f, 7f, -12f), transportFocus + Vector3.up * 2f);
                }
                else if (!sawRopeSettled)
                {
                    SetCamera(focus + new Vector3(7f, 4f, -8f), focus + Vector3.up * 0.5f);
                }
                else
                {
                    Vector3 passengerFocus = hasPassengerPosition ? (Vector3)passengerPosition : focus;
                    SetCamera(passengerFocus + new Vector3(7f, 5f, -9f), Vector3.Lerp(passengerFocus, transportFocus, 0.35f));
                    yield return new WaitForSeconds(2f);
                    break;
                }

                yield return null;
            }
        }

        private IEnumerator CameraTransportPlaneRampBoardAndGroundExitRoutine(EntityManager em, Entity transport, Entity passenger)
        {
            float startedAt = Time.time;
            bool sawBoarded = false;
            bool queuedExit = false;
            bool sawDoorOpen = false;
            bool sawExited = false;
            Vector3 fallbackFocus = new(17.5f, 1.5f, 13f);

            while (Time.time - startedAt < 18f)
            {
                bool hasTransportPosition = TryGetPosition(em, transport, out float3 transportPosition);
                bool hasPassengerPosition = TryGetPosition(em, passenger, out float3 passengerPosition);
                bool passengerLoaded = passenger != Entity.Null &&
                                       em.Exists(passenger) &&
                                       em.HasComponent<UnitTransportPassenger>(passenger);
                bool passengerVisible = passenger != Entity.Null &&
                                        em.Exists(passenger) &&
                                        !em.HasComponent<Disabled>(passenger) &&
                                        !em.HasComponent<UnitTransportPassenger>(passenger) &&
                                        !em.HasComponent<UnitTransportBoardingTarget>(passenger);

                if (!sawBoarded && passengerLoaded)
                    sawBoarded = true;

                if (!sawDoorOpen && IsPlaneDoorOpening(em, transport))
                    sawDoorOpen = true;

                if (sawBoarded && !queuedExit)
                {
                    yield return new WaitForSeconds(1.1f);
                    if (em.Exists(transport))
                        QueueDisembarkCommand(em, transport);
                    queuedExit = true;
                }

                if (queuedExit && !sawDoorOpen && IsPlaneDoorOpening(em, transport))
                    sawDoorOpen = true;

                if (sawBoarded && queuedExit && passengerVisible && hasPassengerPosition)
                    sawExited = true;

                Vector3 transportFocus = hasTransportPosition ? (Vector3)transportPosition : fallbackFocus;
                Vector3 passengerFocus = hasPassengerPosition ? (Vector3)passengerPosition : fallbackFocus;
                Vector3 focus = Vector3.Lerp(passengerFocus, transportFocus, passengerLoaded ? 0.65f : 0.35f);

                if (!sawBoarded)
                {
                    SetCamera(focus + new Vector3(11f, 6f, -11f), focus + Vector3.up * 1.2f);
                }
                else if (!queuedExit || !sawDoorOpen)
                {
                    SetCamera(transportFocus + new Vector3(12f, 6f, -13f), Vector3.Lerp(transportFocus, passengerFocus, 0.35f) + Vector3.up * 1.4f);
                }
                else if (!sawExited)
                {
                    SetCamera(passengerFocus + new Vector3(8f, 5f, -9f), Vector3.Lerp(passengerFocus, transportFocus, 0.45f) + Vector3.up);
                }
                else
                {
                    SetCamera(passengerFocus + new Vector3(8f, 5f, -9f), Vector3.Lerp(passengerFocus, transportFocus, 0.35f) + Vector3.up);
                    yield return new WaitForSeconds(2f);
                    break;
                }

                yield return null;
            }
        }

        private IEnumerator CameraTransportPlaneSoldierAirdropRoutine(EntityManager em, Entity transport, Entity passenger)
        {
            float startedAt = Time.time;
            bool queuedExit = false;
            bool sawDrop = false;
            bool sawSettled = false;
            Vector3 fallbackFocus = new(18.5f, 24f, 18.5f);

            while (Time.time - startedAt < 22f)
            {
                bool hasTransportPosition = TryGetPosition(em, transport, out float3 transportPosition);
                bool hasPassengerPosition = TryGetPosition(em, passenger, out float3 passengerPosition);
                bool passengerLoaded = passenger != Entity.Null &&
                                       em.Exists(passenger) &&
                                       em.HasComponent<UnitTransportPassenger>(passenger);
                bool passengerDropping = passenger != Entity.Null &&
                                         em.Exists(passenger) &&
                                         em.HasComponent<UnitTransportParachuteDropComponent>(passenger);
                bool passengerSettled = passenger != Entity.Null &&
                                        em.Exists(passenger) &&
                                        !em.HasComponent<Disabled>(passenger) &&
                                        !em.HasComponent<UnitTransportPassenger>(passenger) &&
                                        !em.HasComponent<UnitTransportParachuteDropComponent>(passenger) &&
                                        sawDrop;

                if (!queuedExit)
                {
                    yield return new WaitForSeconds(0.85f);
                    if (em.Exists(transport))
                        QueueDisembarkCommand(em, transport, TransportPlaneSoldierAirdropDropCell, true);
                    queuedExit = true;
                }

                if (passengerDropping)
                    sawDrop = true;

                if (passengerSettled)
                    sawSettled = true;

                Vector3 transportFocus = hasTransportPosition ? (Vector3)transportPosition : fallbackFocus;
                Vector3 passengerFocus = hasPassengerPosition ? (Vector3)passengerPosition : fallbackFocus;
                Vector3 focus = passengerLoaded && !passengerDropping
                    ? transportFocus
                    : Vector3.Lerp(passengerFocus, transportFocus, passengerDropping ? 0.15f : 0.35f);

                if (!sawDrop)
                {
                    SetCamera(transportFocus + new Vector3(18f, 12f, -24f), transportFocus + Vector3.down * 8f);
                }
                else if (!sawSettled)
                {
                    SetCamera(focus + new Vector3(10f, 8f, -13f), focus + Vector3.down * 2f);
                }
                else
                {
                    SetCamera(passengerFocus + new Vector3(8f, 5f, -10f), passengerFocus + Vector3.up);
                    yield return new WaitForSeconds(2f);
                    break;
                }

                yield return null;
            }
        }

        private IEnumerator CameraTransportPlaneVehicleCargoGroundExitRoutine(EntityManager em, Entity transport, Entity vehicle)
        {
            float startedAt = Time.time;
            bool sawBoarded = false;
            bool queuedExit = false;
            bool sawDoorOpen = false;
            bool sawExited = false;
            Vector3 fallbackFocus = new(17.5f, 1.6f, 13f);

            while (Time.time - startedAt < 22f)
            {
                bool hasTransportPosition = TryGetPosition(em, transport, out float3 transportPosition);
                bool hasVehiclePosition = TryGetPosition(em, vehicle, out float3 vehiclePosition);
                bool vehicleLoaded = vehicle != Entity.Null &&
                                     em.Exists(vehicle) &&
                                     em.HasComponent<UnitTransportPassenger>(vehicle) &&
                                     em.HasComponent<UnitTransportCargoPassenger>(vehicle);
                bool vehicleVisible = vehicle != Entity.Null &&
                                      em.Exists(vehicle) &&
                                      !em.HasComponent<Disabled>(vehicle) &&
                                      !em.HasComponent<UnitTransportPassenger>(vehicle) &&
                                      !em.HasComponent<UnitTransportCargoPassenger>(vehicle) &&
                                      !em.HasComponent<UnitTransportBoardingTarget>(vehicle);

                if (!sawBoarded && vehicleLoaded)
                    sawBoarded = true;

                if (!sawDoorOpen && IsPlaneDoorOpening(em, transport))
                    sawDoorOpen = true;

                if (sawBoarded && !queuedExit)
                {
                    yield return new WaitForSeconds(1.1f);
                    if (em.Exists(transport))
                        QueueDisembarkCommand(em, transport);
                    queuedExit = true;
                }

                if (queuedExit && !sawDoorOpen && IsPlaneDoorOpening(em, transport))
                    sawDoorOpen = true;

                if (sawBoarded && queuedExit && vehicleVisible && hasVehiclePosition)
                    sawExited = true;

                Vector3 transportFocus = hasTransportPosition ? (Vector3)transportPosition : fallbackFocus;
                Vector3 vehicleFocus = hasVehiclePosition ? (Vector3)vehiclePosition : fallbackFocus;
                Vector3 focus = Vector3.Lerp(vehicleFocus, transportFocus, vehicleLoaded ? 0.65f : 0.3f);

                if (!sawBoarded)
                {
                    SetCamera(focus + new Vector3(13f, 6.5f, -13f), focus + Vector3.up * 1.2f);
                }
                else if (!queuedExit || !sawDoorOpen)
                {
                    SetCamera(transportFocus + new Vector3(13f, 7f, -14f), transportFocus + Vector3.up * 1.5f);
                }
                else if (!sawExited)
                {
                    SetCamera(vehicleFocus + new Vector3(10f, 5.5f, -10f), Vector3.Lerp(vehicleFocus, transportFocus, 0.45f) + Vector3.up);
                }
                else
                {
                    SetCamera(vehicleFocus + new Vector3(10f, 5.5f, -10f), Vector3.Lerp(vehicleFocus, transportFocus, 0.35f) + Vector3.up);
                    yield return new WaitForSeconds(2f);
                    break;
                }

                yield return null;
            }
        }

        private IEnumerator CameraTransportPlaneVehicleCargoAirdropRoutine(EntityManager em, Entity transport, Entity vehicle)
        {
            float startedAt = Time.time;
            bool queuedExit = false;
            bool sawDrop = false;
            bool sawSettled = false;
            Vector3 fallbackFocus = new(18.5f, 24f, 18.5f);

            while (Time.time - startedAt < 24f)
            {
                bool hasTransportPosition = TryGetPosition(em, transport, out float3 transportPosition);
                bool hasVehiclePosition = TryGetPosition(em, vehicle, out float3 vehiclePosition);
                bool vehicleLoaded = vehicle != Entity.Null &&
                                     em.Exists(vehicle) &&
                                     em.HasComponent<UnitTransportPassenger>(vehicle) &&
                                     em.HasComponent<UnitTransportCargoPassenger>(vehicle);
                bool vehicleDropping = vehicle != Entity.Null &&
                                       em.Exists(vehicle) &&
                                       em.HasComponent<UnitTransportCargoDropComponent>(vehicle);
                bool vehicleSettled = vehicle != Entity.Null &&
                                      em.Exists(vehicle) &&
                                      !em.HasComponent<Disabled>(vehicle) &&
                                      !em.HasComponent<UnitTransportPassenger>(vehicle) &&
                                      !em.HasComponent<UnitTransportCargoPassenger>(vehicle) &&
                                      !em.HasComponent<UnitTransportCargoDropComponent>(vehicle) &&
                                      sawDrop;

                if (!queuedExit)
                {
                    yield return new WaitForSeconds(0.85f);
                    if (em.Exists(transport))
                        QueueDisembarkCommand(em, transport, TransportPlaneVehicleCargoDropCell, true);
                    queuedExit = true;
                }

                if (vehicleDropping)
                    sawDrop = true;

                if (vehicleSettled)
                    sawSettled = true;

                Vector3 transportFocus = hasTransportPosition ? (Vector3)transportPosition : fallbackFocus;
                Vector3 vehicleFocus = hasVehiclePosition ? (Vector3)vehiclePosition : fallbackFocus;
                Vector3 focus = vehicleLoaded && !vehicleDropping
                    ? transportFocus
                    : Vector3.Lerp(vehicleFocus, transportFocus, vehicleDropping ? 0.15f : 0.35f);

                if (!sawDrop)
                {
                    SetCamera(transportFocus + new Vector3(19f, 13f, -25f), transportFocus + Vector3.down * 9f);
                }
                else if (!sawSettled)
                {
                    SetCamera(focus + new Vector3(12f, 9f, -14f), focus + Vector3.down * 2f);
                }
                else
                {
                    SetCamera(vehicleFocus + new Vector3(11f, 6f, -11f), vehicleFocus + Vector3.up);
                    yield return new WaitForSeconds(2f);
                    break;
                }

                yield return null;
            }
        }

        private IEnumerator CameraTransportPlaneMixedLoadAirdropRoutine(EntityManager em, Entity transport, Entity soldier, Entity vehicle)
        {
            float startedAt = Time.time;
            bool queuedExit = false;
            bool sawSoldierDrop = false;
            bool sawVehicleDrop = false;
            bool sawSoldierSettled = false;
            bool sawVehicleSettled = false;
            Vector3 fallbackFocus = new(18.5f, 24f, 18.5f);

            while (Time.time - startedAt < 28f)
            {
                bool hasTransportPosition = TryGetPosition(em, transport, out float3 transportPosition);
                bool hasSoldierPosition = TryGetPosition(em, soldier, out float3 soldierPosition);
                bool hasVehiclePosition = TryGetPosition(em, vehicle, out float3 vehiclePosition);
                bool soldierDropping = soldier != Entity.Null &&
                                       em.Exists(soldier) &&
                                       em.HasComponent<UnitTransportParachuteDropComponent>(soldier);
                bool vehicleDropping = vehicle != Entity.Null &&
                                       em.Exists(vehicle) &&
                                       em.HasComponent<UnitTransportCargoDropComponent>(vehicle);
                bool soldierSettled = soldier != Entity.Null &&
                                      em.Exists(soldier) &&
                                      !em.HasComponent<Disabled>(soldier) &&
                                      !em.HasComponent<UnitTransportPassenger>(soldier) &&
                                      !em.HasComponent<UnitTransportParachuteDropComponent>(soldier) &&
                                      sawSoldierDrop;
                bool vehicleSettled = vehicle != Entity.Null &&
                                      em.Exists(vehicle) &&
                                      !em.HasComponent<Disabled>(vehicle) &&
                                      !em.HasComponent<UnitTransportPassenger>(vehicle) &&
                                      !em.HasComponent<UnitTransportCargoPassenger>(vehicle) &&
                                      !em.HasComponent<UnitTransportCargoDropComponent>(vehicle) &&
                                      sawVehicleDrop;

                if (!queuedExit)
                {
                    yield return new WaitForSeconds(0.85f);
                    if (em.Exists(transport))
                        QueueDisembarkCommand(em, transport, TransportPlaneMixedAirdropDropCell, true);
                    queuedExit = true;
                }

                if (soldierDropping)
                    sawSoldierDrop = true;
                if (vehicleDropping)
                    sawVehicleDrop = true;
                if (soldierSettled)
                    sawSoldierSettled = true;
                if (vehicleSettled)
                    sawVehicleSettled = true;

                Vector3 transportFocus = hasTransportPosition ? (Vector3)transportPosition : fallbackFocus;
                Vector3 soldierFocus = hasSoldierPosition ? (Vector3)soldierPosition : fallbackFocus;
                Vector3 vehicleFocus = hasVehiclePosition ? (Vector3)vehiclePosition : fallbackFocus;
                Vector3 dropFocus = Vector3.Lerp(soldierFocus, vehicleFocus, 0.5f);

                if (!sawSoldierDrop && !sawVehicleDrop)
                {
                    SetCamera(transportFocus + new Vector3(20f, 14f, -27f), transportFocus + Vector3.down * 10f);
                }
                else if (!sawSoldierSettled || !sawVehicleSettled)
                {
                    SetCamera(dropFocus + new Vector3(13f, 10f, -16f), dropFocus + Vector3.down * 2f);
                }
                else
                {
                    SetCamera(dropFocus + new Vector3(12f, 6f, -12f), dropFocus + Vector3.up);
                    yield return new WaitForSeconds(2f);
                    break;
                }

                yield return null;
            }
        }

        private IEnumerator CameraTransportAirPickupBoardAndRopeExitRoutine(EntityManager em, Entity transport, Entity passenger)
        {
            float startedAt = Time.time;
            bool queuedBoard = false;
            bool sawPickupCommand = false;
            bool sawLanded = false;
            bool sawBoarded = false;
            bool queuedExit = false;
            bool sawRopeDrop = false;
            bool sawRopeSettled = false;
            Vector3 fallbackFocus = new(14f, 3f, 13f);

            while (Time.time - startedAt < 30f)
            {
                bool hasTransportPosition = TryGetPosition(em, transport, out float3 transportPosition);
                bool hasPassengerPosition = TryGetPosition(em, passenger, out float3 passengerPosition);
                bool passengerLoaded = passenger != Entity.Null &&
                                       em.Exists(passenger) &&
                                       em.HasComponent<UnitTransportPassenger>(passenger);
                bool passengerDropping = passenger != Entity.Null &&
                                         em.Exists(passenger) &&
                                         em.HasComponent<UnitTransportRopeDropComponent>(passenger);
                bool passengerSettled = passenger != Entity.Null &&
                                        em.Exists(passenger) &&
                                        !em.HasComponent<Disabled>(passenger) &&
                                        !em.HasComponent<UnitTransportPassenger>(passenger) &&
                                        !em.HasComponent<UnitTransportRopeDropComponent>(passenger) &&
                                        sawRopeDrop;

                if (!queuedBoard)
                {
                    yield return new WaitForSeconds(0.75f);
                    if (em.Exists(transport))
                        QueueBoardTransportCommand(em, transport);
                    queuedBoard = true;
                }

                if (!sawPickupCommand && em.Exists(transport) && em.HasComponent<UnitTarget>(transport))
                    sawPickupCommand = true;

                if (!sawLanded && IsAirTransportGrounded(em, transport))
                    sawLanded = true;

                if (!sawBoarded && passengerLoaded)
                    sawBoarded = true;

                if (sawBoarded && !queuedExit)
                {
                    yield return new WaitForSeconds(1.2f);
                    if (em.Exists(transport))
                        QueueDisembarkCommand(em, transport);
                    queuedExit = true;
                }

                if (passengerDropping)
                    sawRopeDrop = true;

                if (passengerSettled)
                    sawRopeSettled = true;

                Vector3 transportFocus = hasTransportPosition ? (Vector3)transportPosition : fallbackFocus;
                Vector3 passengerFocus = hasPassengerPosition ? (Vector3)passengerPosition : fallbackFocus;
                Vector3 focus = Vector3.Lerp(passengerFocus, transportFocus, passengerDropping ? 0.2f : 0.5f);

                if (!sawPickupCommand)
                {
                    SetCamera(Vector3.Lerp(passengerFocus, transportFocus, 0.45f) + new Vector3(12f, 9f, -15f), Vector3.Lerp(passengerFocus, transportFocus, 0.5f));
                }
                else if (!sawLanded || !sawBoarded)
                {
                    SetCamera(focus + new Vector3(10f, 7f, -13f), focus + Vector3.up * 1.5f);
                }
                else if (!sawRopeDrop)
                {
                    SetCamera(transportFocus + new Vector3(9f, 7f, -12f), transportFocus + Vector3.up * 2f);
                }
                else if (!sawRopeSettled)
                {
                    SetCamera(focus + new Vector3(7f, 4f, -8f), focus + Vector3.up * 0.5f);
                }
                else
                {
                    SetCamera(passengerFocus + new Vector3(7f, 5f, -9f), Vector3.Lerp(passengerFocus, transportFocus, 0.35f));
                    yield return new WaitForSeconds(2f);
                    break;
                }

                yield return null;
            }
        }

        private static bool IsPlaneDoorOpening(EntityManager em, Entity transport)
        {
            if (transport == Entity.Null || !em.Exists(transport))
                return false;

            if (em.HasComponent<UnitTransportPlaneDoorOpenRequest>(transport))
                return true;

            if (!em.HasComponent<UnitTransportPlaneDoorState>(transport))
                return false;

            UnitTransportPlaneDoorState doorState = em.GetComponentData<UnitTransportPlaneDoorState>(transport);
            return doorState.TargetOpen != 0 || doorState.Open01 > 0.01f;
        }

        private static bool IsAirTransportGrounded(EntityManager em, Entity transport)
        {
            if (transport == Entity.Null ||
                !em.Exists(transport) ||
                !em.HasComponent<UnitAirComponent>(transport) ||
                !em.HasComponent<LocalTransform>(transport))
            {
                return false;
            }

            UnitAirComponent air = em.GetComponentData<UnitAirComponent>(transport);
            LocalTransform transform = em.GetComponentData<LocalTransform>(transport);
            float groundY = air.HomeInitialized != 0 ? air.HomePosition.y : 0f;
            return air.Airborne == 0 &&
                   air.TakeoffRolling == 0 &&
                   air.LandingRolling == 0 &&
                   transform.Position.y <= groundY + TransportBoardingData.AirBoardingGroundedHeightTolerance;
        }

        private static void SetFaction(EntityManager em, Entity entity, byte factionId)
        {
            if (entity == Entity.Null || !em.Exists(entity))
                return;

            if (em.HasComponent<Faction>(entity))
                em.SetComponentData(entity, new Faction { Id = factionId });
            else
                em.AddComponentData(entity, new Faction { Id = factionId });
        }

        private static void SetOrAdd<T>(EntityManager em, Entity entity, T component)
            where T : unmanaged, IComponentData
        {
            if (entity == Entity.Null || !em.Exists(entity))
                return;

            if (em.HasComponent<T>(entity))
                em.SetComponentData(entity, component);
            else
                em.AddComponentData(entity, component);
        }

        private static void RemoveIfPresent<T>(EntityManager em, Entity entity)
            where T : unmanaged, IComponentData
        {
            if (entity != Entity.Null && em.Exists(entity) && em.HasComponent<T>(entity))
                em.RemoveComponent<T>(entity);
        }

        private static void SetLocalTransform(EntityManager em, Entity entity, float3 position, quaternion rotation, float scale)
        {
            if (entity == Entity.Null || !em.Exists(entity))
                return;

            SetOrAdd(em, entity, LocalTransform.FromPositionRotationScale(position, rotation, math.max(0.0001f, scale)));
        }

        private static bool TryResolveUnitPrefab(EntityManager em, string sourceKey, out Entity prefab)
        {
            prefab = Entity.Null;
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitPrefabRegistryTag>(),
                ComponentType.ReadOnly<UnitPrefabRegistryEntry>());
            if (query.CalculateEntityCount() <= 0)
                return false;

            using NativeArray<Entity> registries = query.ToEntityArray(Allocator.Temp);
            for (int registryIndex = 0; registryIndex < registries.Length; registryIndex++)
            {
                Entity registry = registries[registryIndex];
                DynamicBuffer<UnitPrefabRegistryEntry> entries = em.GetBuffer<UnitPrefabRegistryEntry>(registry);
                for (int i = 0; i < entries.Length; i++)
                {
                    Entity candidate = entries[i].Prefab;
                    if (candidate == Entity.Null || !em.Exists(candidate) || !em.HasComponent<Prefab>(candidate))
                        continue;

                    if (EntityMatchesSourceKey(em, candidate, sourceKey))
                    {
                        prefab = candidate;
                        return true;
                    }
                }
            }

            return false;
        }

        private static Entity InstantiateUnitPrefab(EntityManager em, Entity prefab)
        {
            Entity instance = em.Instantiate(prefab);
            if (em.HasComponent<Disabled>(instance))
                em.RemoveComponent<Disabled>(instance);
            return instance;
        }

        private static bool EntityMatchesSourceKey(EntityManager em, Entity candidate, string sourceKey)
        {
            if (em.HasComponent<UnitSourcePrefabKey>(candidate) &&
                SourceKeyMatches(em.GetComponentData<UnitSourcePrefabKey>(candidate).Value, sourceKey))
            {
                return true;
            }

            return SourceKeyMatches(em.GetName(candidate), sourceKey);
        }

        private static bool SourceKeyMatches(FixedString64Bytes candidate, string sourceKey)
        {
            return SourceKeyMatches(candidate.ToString(), sourceKey);
        }

        private static bool SourceKeyMatches(string candidate, string sourceKey)
        {
            string normalizedCandidate = NormalizeSourceKey(candidate);
            string normalizedSource = NormalizeSourceKey(sourceKey);
            return !string.IsNullOrEmpty(normalizedCandidate) &&
                   string.Equals(normalizedCandidate, normalizedSource, System.StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeSourceKey(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Replace(" (Clone)", string.Empty).Trim().ToLowerInvariant();
        }

        private static Entity FindFirstEntity<T>(EntityManager em)
            where T : unmanaged, IComponentData
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<T>(), ComponentType.ReadOnly<LocalTransform>());
            if (query.CalculateEntityCount() <= 0)
                return Entity.Null;

            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            return entities[0];
        }

        private static bool TryGetPosition(EntityManager em, Entity entity, out float3 position)
        {
            if (entity != Entity.Null && em.Exists(entity) && em.HasComponent<LocalTransform>(entity))
            {
                position = em.GetComponentData<LocalTransform>(entity).Position;
                return true;
            }

            position = float3.zero;
            return false;
        }

        private static void DestroyEntitiesWith<T>(EntityManager em)
            where T : unmanaged, IComponentData
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
                if (em.Exists(entities[i]))
                    em.DestroyEntity(entities[i]);
        }

        private static void DestroyEntitiesWithTree<T>(EntityManager em)
            where T : unmanaged, IComponentData
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
                if (em.Exists(entities[i]) && !em.HasComponent<Prefab>(entities[i]))
                    DestroyLinkedEntityGroup(em, entities[i]);
        }

        private static void DestroyOrphanRenderableEntities(EntityManager em)
        {
            System.Type materialMeshInfoType = System.Type.GetType("Unity.Rendering.MaterialMeshInfo, Unity.Entities.Graphics");
            if (materialMeshInfoType == null)
                return;

            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly(materialMeshInfoType));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!em.Exists(entity) || em.HasComponent<Prefab>(entity))
                    continue;

                DestroyLinkedEntityGroup(em, entity);
            }
        }

        private static void RemoveComponentFromAll<T>(EntityManager em)
            where T : unmanaged, IComponentData
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
                if (em.Exists(entities[i]) && em.HasComponent<T>(entities[i]))
                    em.RemoveComponent<T>(entities[i]);
        }

        private void SetCamera(Vector3 position, Vector3 lookAt)
        {
            if (scenarioCamera == null)
                return;

            scenarioCamera.transform.position = position;
            Vector3 direction = lookAt - position;
            if (direction.sqrMagnitude > 0.001f)
                scenarioCamera.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private void ResetCameraView()
        {
            SetCamera(DefaultCameraPosition, DefaultCameraLookAt);
        }
    }

    public struct BattleScenarioLabRuntimeGridTag : IComponentData
    {
    }

    public struct BattleScenarioLabRuntimeGameplayStateTag : IComponentData
    {
    }

    public struct BattleScenarioLabCommandTag : IComponentData
    {
    }
}
