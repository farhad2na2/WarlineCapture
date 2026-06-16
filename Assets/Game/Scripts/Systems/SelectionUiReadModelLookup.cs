using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class SelectionUiReadModelLookup
{
    public readonly struct TransportPassengerUiInfo
    {
        public readonly Entity Entity;
        public readonly string DisplayName;
        public readonly int HealthCurrent;
        public readonly int HealthMax;

        public TransportPassengerUiInfo(Entity entity, string displayName, int healthCurrent, int healthMax)
        {
            Entity = entity;
            DisplayName = displayName;
            HealthCurrent = healthCurrent;
            HealthMax = healthMax;
        }
    }

    public enum FocusedUnitUiStatus
    {
        Idle = 0,
        Moving = 1,
        Engaged = 2,
        ReturningToBase = 3,
        MissileLaunched = 4,
        AirspaceClear = 5,
        TrackingAirTarget = 6,
        InterceptingMissile = 7,
        AirDefenseReloading = 8
    }

    public bool HasFocusedUnit(EntityManager entityManager, Entity focusedUnit)
    {
        return focusedUnit != Entity.Null &&
               entityManager.Exists(focusedUnit) &&
               entityManager.HasComponent<Faction>(focusedUnit);
    }

    public bool HasAnySelectedUnits(EntityQuery selectedTagQuery)
    {
        return !selectedTagQuery.IsEmptyIgnoreFilter;
    }

    public string ResolveFocusedUnitName(EntityManager entityManager, Entity entity)
    {
        if (entityManager.HasComponent<UnitDisplayInfo>(entity))
        {
            string configuredName = entityManager.GetComponentData<UnitDisplayInfo>(entity).Name.ToString();
            if (!string.IsNullOrWhiteSpace(configuredName))
                return configuredName;
        }

        byte factionId = entityManager.HasComponent<Faction>(entity)
            ? entityManager.GetComponentData<Faction>(entity).Id
            : FactionIdentity.NeutralFactionId;
        if (!entityManager.HasComponent<UnitMove>(entity))
            return FactionIdentity.IsPlayerControlled(factionId) ? "Player Unit" : "Enemy Unit";

        bool isVehicle = IsVehicleUnit(entityManager, entity);
        if (!isVehicle)
            return FactionIdentity.IsPlayerControlled(factionId) ? "Soldier" : "Enemy Soldier";

        bool canAttack = entityManager.HasComponent<UnitCombat>(entity) && entityManager.GetComponentData<UnitCombat>(entity).CanAttack != 0;
        if (canAttack)
            return FactionIdentity.IsPlayerControlled(factionId) ? "Heavy APC" : "Enemy Heavy APC";

        float speed = entityManager.HasComponent<UnitMove>(entity) ? entityManager.GetComponentData<UnitMove>(entity).Speed : 0f;
        if (speed >= 10.5f)
            return FactionIdentity.IsPlayerControlled(factionId) ? "APC 02" : "Enemy APC 02";

        return FactionIdentity.IsPlayerControlled(factionId) ? "APC 01" : "Enemy APC 01";
    }

    public string ResolveFocusedUnitDescription(EntityManager entityManager, Entity entity)
    {
        if (entityManager.HasComponent<UnitDisplayInfo>(entity))
        {
            string configuredDescription = entityManager.GetComponentData<UnitDisplayInfo>(entity).Description.ToString();
            if (!string.IsNullOrWhiteSpace(configuredDescription))
                return configuredDescription;
        }

        byte factionId = entityManager.HasComponent<Faction>(entity)
            ? entityManager.GetComponentData<Faction>(entity).Id
            : FactionIdentity.NeutralFactionId;
        bool movable = entityManager.HasComponent<UnitMove>(entity);
        bool isVehicle = IsVehicleForVisibleSelection(entityManager, entity);
        bool canAttack = entityManager.HasComponent<UnitCombat>(entity) && entityManager.GetComponentData<UnitCombat>(entity).CanAttack != 0;

        if (FactionIdentity.IsPlayerControlled(factionId))
        {
            if (!movable)
                return "Player-controlled unit.";
            if (isVehicle)
                return canAttack
                    ? "Heavy combat APC. Faster than the base APC and can attack enemies."
                    : "Support APC vehicle. Mobile but cannot attack, and will retreat when attacked.";

            return "Player soldier. Click ground to issue a move order.";
        }

        if (!movable)
            return "Enemy unit. Read-only info.";
        if (isVehicle)
            return canAttack ? "Enemy combat vehicle." : "Enemy support vehicle.";
        return "Enemy mobile unit. Read-only info.";
    }

    public string ResolveFocusedUnitHealthText(EntityManager entityManager, Entity entity)
    {
        if (!TryGetFocusedUnitHealth(entityManager, entity, out int current, out int max))
            return "Health: -";

        return $"Health: {current}/{max}";
    }

    public bool TryGetFocusedUnitHealth(EntityManager entityManager, Entity entity, out int current, out int max)
    {
        current = 0;
        max = 0;

        if (!entityManager.Exists(entity) || !entityManager.HasComponent<UnitHealth>(entity))
            return false;

        UnitHealth health = entityManager.GetComponentData<UnitHealth>(entity);
        current = health.Current;
        max = health.Max;
        return true;
    }

    public bool TryGetFocusedUnitCapacityInfo(EntityManager entityManager, Entity entity, float timeSeconds, out int current, out int max, out float progress01)
    {
        current = 0;
        max = 0;
        progress01 = 0f;

        if (!entityManager.Exists(entity) || !entityManager.HasComponent<UnitResourceHauler>(entity))
            return false;

        UnitResourceHauler hauler = entityManager.GetComponentData<UnitResourceHauler>(entity);
        max = Mathf.Max(0, hauler.BarrelCapacity);
        if (max <= 0)
            return false;

        float cargo = Mathf.Clamp(hauler.CargoOilBarrels + hauler.CargoFuelBarrels, 0f, max);

        if (entityManager.HasComponent<UnitResourceHaulOrder>(entity))
        {
            const byte LoadingPhase = 2;
            const byte UnloadingPhase = 4;

            UnitResourceHaulOrder order = entityManager.GetComponentData<UnitResourceHaulOrder>(entity);
            if (order.ActionEndsAt > 0f)
            {
                if (order.Phase == LoadingPhase && hauler.FillDurationSeconds > 0.01f)
                {
                    float startedAt = order.ActionEndsAt - hauler.FillDurationSeconds;
                    float fill01 = Mathf.Clamp01((timeSeconds - startedAt) / hauler.FillDurationSeconds);
                    cargo = Mathf.Max(cargo, fill01 * max);
                }
                else if (order.Phase == UnloadingPhase && hauler.UnloadDurationSeconds > 0.01f)
                {
                    float startedAt = order.ActionEndsAt - hauler.UnloadDurationSeconds;
                    float unload01 = Mathf.Clamp01((timeSeconds - startedAt) / hauler.UnloadDurationSeconds);
                    cargo = Mathf.Min(cargo, (1f - unload01) * max);
                }
            }
        }

        progress01 = max > 0 ? Mathf.Clamp01(cargo / max) : 0f;
        current = Mathf.Clamp(Mathf.RoundToInt(cargo), 0, max);
        return true;
    }

    public bool IsOwnedByPlayer(EntityManager entityManager, Entity entity)
    {
        return entityManager.Exists(entity) &&
               entityManager.HasComponent<Faction>(entity) &&
               FactionIdentity.IsPlayerControlled(entityManager.GetComponentData<Faction>(entity).Id);
    }

    public bool IsVehicleUnit(EntityManager entityManager, Entity entity)
    {
        if (!entityManager.HasComponent<UnitFootprint>(entity) || !entityManager.HasComponent<UnitMovementBehavior>(entity))
            return false;

        return UnitVehicleMovementUtility.IsVehicle(
            entityManager.GetComponentData<UnitFootprint>(entity),
            entityManager.GetComponentData<UnitMovementBehavior>(entity));
    }

    public bool IsVehicleForVisibleSelection(EntityManager entityManager, Entity entity)
    {
        if (entityManager.HasComponent<UnitSourcePrefabKey>(entity))
        {
            string sourceKey = entityManager.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
            if (sourceKey.StartsWith("Unit_Veh_", System.StringComparison.OrdinalIgnoreCase))
                return true;
            if (sourceKey.StartsWith("Unit_Chr_", System.StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return IsVehicleUnit(entityManager, entity);
    }

    public bool CanAttack(EntityManager entityManager, Entity entity)
    {
        return entityManager.Exists(entity) &&
               entityManager.HasComponent<UnitCombat>(entity) &&
               entityManager.GetComponentData<UnitCombat>(entity).CanAttack != 0;
    }

    public bool CanHoldPosition(EntityManager entityManager, Entity entity, out TacticalCommandReasonCode reason)
    {
        return CanAcceptImmediateSelectedUnitCommand(entityManager, entity, out reason);
    }

    public bool CanStop(EntityManager entityManager, Entity entity, out TacticalCommandReasonCode reason)
    {
        return CanAcceptImmediateSelectedUnitCommand(entityManager, entity, out reason);
    }

    public bool CanScan(EntityManager entityManager, Entity entity, out TacticalCommandReasonCode reason)
    {
        if (!CanAcceptLivingOwnedUnit(entityManager, entity, out reason))
            return false;

        if (IsSelectedUnitScanCapable(entityManager, entity))
        {
            reason = TacticalCommandReasonCode.None;
            return true;
        }

        reason = TacticalCommandReasonCode.ScanUnavailable;
        return false;
    }

    public int GetTransportPassengerCount(EntityManager entityManager, Entity transport, UnitTransportCapacitySystem capacitySystem)
    {
        if (!entityManager.Exists(transport) || !capacitySystem.TryEnsureTransportCapacity(entityManager, transport))
            return 0;

        return entityManager.GetBuffer<UnitTransportPassengerElement>(transport).Length;
    }

    public int GetTransportPassengerCapacity(EntityManager entityManager, Entity transport, UnitTransportCapacitySystem capacitySystem)
    {
        if (!entityManager.Exists(transport) ||
            !capacitySystem.TryEnsureTransportCapacity(entityManager, transport) ||
            !entityManager.HasComponent<UnitTransportCapacity>(transport))
        {
            return 0;
        }

        return math.max(0, entityManager.GetComponentData<UnitTransportCapacity>(transport).SoldierCapacity);
    }

    public void GetTransportPassengers(
        EntityManager entityManager,
        Entity transport,
        UnitTransportCapacitySystem capacitySystem,
        List<TransportPassengerUiInfo> results)
    {
        if (results == null)
            return;

        results.Clear();
        if (!entityManager.Exists(transport) ||
            !capacitySystem.TryEnsureTransportCapacity(entityManager, transport) ||
            !entityManager.HasBuffer<UnitTransportPassengerElement>(transport))
        {
            return;
        }

        DynamicBuffer<UnitTransportPassengerElement> passengers = entityManager.GetBuffer<UnitTransportPassengerElement>(transport);
        for (int i = 0; i < passengers.Length; i++)
        {
            Entity passenger = passengers[i].Passenger;
            if (!entityManager.Exists(passenger))
                continue;

            TryGetFocusedUnitHealth(entityManager, passenger, out int current, out int max);
            results.Add(new TransportPassengerUiInfo(passenger, ResolveFocusedUnitName(entityManager, passenger), current, max));
        }
    }

    public bool TryGetFocusedUnitWorldPosition(EntityManager entityManager, Entity entity, out Vector3 worldPosition)
    {
        worldPosition = default;
        if (!entityManager.Exists(entity) || !entityManager.HasComponent<LocalToWorld>(entity))
            return false;

        worldPosition = entityManager.GetComponentData<LocalToWorld>(entity).Position;
        return true;
    }

    public FocusedUnitUiStatus GetFocusedUnitUiStatus(EntityManager entityManager, Entity entity)
    {
        if (!entityManager.Exists(entity))
            return FocusedUnitUiStatus.Idle;

        if (entityManager.HasComponent<UnitAirComponent>(entity) && entityManager.GetComponentData<UnitAirComponent>(entity).ReturningHome != 0)
            return FocusedUnitUiStatus.ReturningToBase;

        if (entityManager.HasComponent<GroundMissileInFlightComponent>(entity) ||
            HasCommandedGroundMissileTarget(entityManager, entity))
        {
            return FocusedUnitUiStatus.MissileLaunched;
        }

        if (HasAutoGroundMissileTarget(entityManager, entity))
            return FocusedUnitUiStatus.Idle;

        if (TryGetAirMissileLauncherStatus(entityManager, entity, out FocusedUnitUiStatus airDefenseStatus))
            return airDefenseStatus;

        if (entityManager.HasComponent<HoldPositionOrderTag>(entity))
            return FocusedUnitUiStatus.Idle;

        if (entityManager.HasComponent<EngageTarget>(entity))
            return FocusedUnitUiStatus.Engaged;

        if (entityManager.HasComponent<UnitTarget>(entity) ||
            entityManager.HasComponent<UnitPathRequest>(entity) ||
            entityManager.HasComponent<UnitPathFollow>(entity))
        {
            return FocusedUnitUiStatus.Moving;
        }

        return FocusedUnitUiStatus.Idle;
    }

    public bool TryGetFocusedUnitPortraitPose(EntityManager entityManager, Entity entity, out Vector3 worldPosition, out Vector3 forward)
    {
        worldPosition = default;
        forward = Vector3.forward;

        if (!entityManager.Exists(entity))
            return false;

        if (entityManager.HasComponent<LocalToWorld>(entity))
            worldPosition = entityManager.GetComponentData<LocalToWorld>(entity).Position;
        else if (entityManager.HasComponent<LocalTransform>(entity))
            worldPosition = entityManager.GetComponentData<LocalTransform>(entity).Position;
        else
            return false;

        ResolveForward(entityManager, entity, out forward);
        return true;
    }

    public bool TryGetSelectedUnitsPortraitPose(
        EntityManager entityManager,
        NativeArray<Entity> selectedEntities,
        Entity focusedUnit,
        out Vector3 centerWorldPosition,
        out Vector3 forward,
        out float framingRadius)
    {
        centerWorldPosition = default;
        forward = Vector3.forward;
        framingRadius = 1f;

        if (selectedEntities.Length == 0)
            return false;

        Vector3 sum = Vector3.zero;
        int counted = 0;
        Vector3 min = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        Entity forwardEntity = Entity.Null;

        for (int i = 0; i < selectedEntities.Length; i++)
        {
            Entity entity = selectedEntities[i];
            if (!entityManager.Exists(entity) || !entityManager.HasComponent<LocalToWorld>(entity))
                continue;

            Vector3 position = entityManager.GetComponentData<LocalToWorld>(entity).Position;
            sum += position;
            min = Vector3.Min(min, position);
            max = Vector3.Max(max, position);
            counted++;

            if (forwardEntity == Entity.Null)
                forwardEntity = entity;
        }

        if (counted == 0)
            return false;

        centerWorldPosition = sum / counted;
        Vector3 extents = max - min;
        framingRadius = Mathf.Max(1f, Mathf.Max(extents.x, extents.z) * 0.65f);

        Entity poseEntity = HasFocusedUnit(entityManager, focusedUnit) ? focusedUnit : forwardEntity;
        if (poseEntity != Entity.Null)
            ResolveForward(entityManager, poseEntity, out forward);

        return true;
    }

    public string ResolveHudSelectionStatus(EntityManager entityManager, Entity entity)
    {
        var parts = new List<string>();

        if (entityManager.HasComponent<Faction>(entity))
            parts.Add(FactionIdentity.IsPlayerControlled(entityManager.GetComponentData<Faction>(entity).Id) ? "PLAYER" : "ENEMY");

        if (entityManager.HasComponent<UnitHealth>(entity))
        {
            UnitHealth health = entityManager.GetComponentData<UnitHealth>(entity);
            parts.Add($"HP {health.Current}/{health.Max}");
        }

        if (entityManager.HasComponent<GroundMissileInFlightComponent>(entity) ||
            HasCommandedGroundMissileTarget(entityManager, entity))
        {
            parts.Add("MISSILE LAUNCHED");
        }
        else if (HasAutoGroundMissileTarget(entityManager, entity))
        {
            parts.Add("IDLE");
        }
        else if (TryGetAirMissileLauncherStatus(entityManager, entity, out FocusedUnitUiStatus airDefenseStatus))
        {
            parts.Add(ToAirMissileLauncherStatusText(airDefenseStatus));
        }
        else if (entityManager.HasComponent<HoldPositionOrderTag>(entity))
            parts.Add("HOLDING");
        else if (entityManager.HasComponent<EngageTarget>(entity))
            parts.Add("ENGAGED");
        else if (entityManager.HasComponent<UnitTarget>(entity) || entityManager.HasComponent<UnitPathRequest>(entity) || entityManager.HasComponent<UnitPathFollow>(entity))
            parts.Add("MOVING");
        else
            parts.Add("IDLE");

        return string.Join(" / ", parts);
    }

    private static bool TryGetAirMissileLauncherStatus(
        EntityManager entityManager,
        Entity entity,
        out FocusedUnitUiStatus status)
    {
        status = FocusedUnitUiStatus.Idle;
        if (!entityManager.HasComponent<AirMissileLauncherComponent>(entity) ||
            !entityManager.HasComponent<AirMissileLauncherStateComponent>(entity))
        {
            return false;
        }

        AirMissileLauncherStateComponent launcherState = entityManager.GetComponentData<AirMissileLauncherStateComponent>(entity);
        if (launcherState.Phase == (byte)AirMissileLauncherPhase.Reloading)
        {
            status = FocusedUnitUiStatus.AirDefenseReloading;
            return true;
        }

        if (launcherState.TargetEntity != Entity.Null)
        {
            status = launcherState.TargetKind == (byte)AirMissileTargetKind.IncomingGroundMissile
                ? FocusedUnitUiStatus.InterceptingMissile
                : FocusedUnitUiStatus.TrackingAirTarget;
            return true;
        }

        status = FocusedUnitUiStatus.AirspaceClear;
        return true;
    }

    private static string ToAirMissileLauncherStatusText(FocusedUnitUiStatus status)
    {
        return status switch
        {
            FocusedUnitUiStatus.TrackingAirTarget => "TRACKING AIR TARGET",
            FocusedUnitUiStatus.InterceptingMissile => "INTERCEPTING MISSILE",
            FocusedUnitUiStatus.AirDefenseReloading => "RELOADING",
            FocusedUnitUiStatus.AirspaceClear => "AIRSPACE CLEAR",
            _ => "IDLE"
        };
    }

    private static void ResolveForward(EntityManager entityManager, Entity entity, out Vector3 forward)
    {
        forward = Vector3.forward;
        if (!entityManager.Exists(entity) || !entityManager.HasComponent<LocalTransform>(entity))
            return;

        quaternion rotation = entityManager.GetComponentData<LocalTransform>(entity).Rotation;
        float3 facing = math.mul(rotation, new float3(0f, 0f, 1f));
        forward = new Vector3(facing.x, 0f, facing.z);
        if (forward.sqrMagnitude > 0.0001f)
            forward.Normalize();
        else
            forward = Vector3.forward;
    }

    private static bool HasCommandedGroundMissileTarget(EntityManager entityManager, Entity entity)
    {
        if (!entityManager.HasComponent<GroundMissileLauncherComponent>(entity) ||
            !entityManager.HasComponent<EngageTarget>(entity))
        {
            return false;
        }

        return entityManager.GetComponentData<EngageTarget>(entity).IsCommanded != 0;
    }

    private static bool HasAutoGroundMissileTarget(EntityManager entityManager, Entity entity)
    {
        if (!entityManager.HasComponent<GroundMissileLauncherComponent>(entity) ||
            !entityManager.HasComponent<EngageTarget>(entity))
        {
            return false;
        }

        return entityManager.GetComponentData<EngageTarget>(entity).IsCommanded == 0;
    }

    private static bool CanAcceptImmediateSelectedUnitCommand(
        EntityManager entityManager,
        Entity entity,
        out TacticalCommandReasonCode reason)
    {
        if (!CanAcceptLivingOwnedUnit(entityManager, entity, out reason))
            return false;

        if (!entityManager.HasComponent<UnitMove>(entity) ||
            entityManager.HasComponent<UnitTransportPassenger>(entity))
        {
            reason = TacticalCommandReasonCode.CommandUnavailable;
            return false;
        }

        reason = TacticalCommandReasonCode.None;
        return true;
    }

    private static bool CanAcceptLivingOwnedUnit(
        EntityManager entityManager,
        Entity entity,
        out TacticalCommandReasonCode reason)
    {
        if (entity == Entity.Null || !entityManager.Exists(entity))
        {
            reason = TacticalCommandReasonCode.NoSelection;
            return false;
        }

        if (!entityManager.HasComponent<Faction>(entity) ||
            !FactionIdentity.IsPlayerControlled(entityManager.GetComponentData<Faction>(entity).Id) ||
            entityManager.HasComponent<Disabled>(entity) ||
            entityManager.HasComponent<UnitDeathAnimationComponent>(entity))
        {
            reason = TacticalCommandReasonCode.CommandUnavailable;
            return false;
        }

        if (entityManager.HasComponent<UnitHealth>(entity) &&
            entityManager.GetComponentData<UnitHealth>(entity).Current <= 0)
        {
            reason = TacticalCommandReasonCode.CommandUnavailable;
            return false;
        }

        reason = TacticalCommandReasonCode.None;
        return true;
    }

    private static bool IsSelectedUnitScanCapable(EntityManager entityManager, Entity entity)
    {
        string source = ResolveScanCapabilitySource(entityManager, entity);
        if (ContainsToken(source, "Drone") ||
            ContainsToken(source, "Recon") ||
            ContainsToken(source, "Scout") ||
            ContainsToken(source, "Radar") ||
            ContainsToken(source, "Scan") ||
            ContainsToken(source, "Plane"))
        {
            return true;
        }

        return entityManager.HasComponent<UnitAirMovement>(entity) &&
               (ContainsToken(source, "Jet") ||
                ContainsToken(source, "Drone") ||
                ContainsToken(source, "Aircraft") ||
                ContainsToken(source, "Air"));
    }

    private static string ResolveScanCapabilitySource(EntityManager entityManager, Entity entity)
    {
        if (entityManager.HasComponent<UnitSourcePrefabKey>(entity))
        {
            string sourceKey = entityManager.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
            if (!string.IsNullOrWhiteSpace(sourceKey))
                return sourceKey;
        }

        if (entityManager.HasComponent<UnitDisplayInfo>(entity))
        {
            UnitDisplayInfo displayInfo = entityManager.GetComponentData<UnitDisplayInfo>(entity);
            string displayName = displayInfo.Name.ToString();
            string displayDescription = displayInfo.Description.ToString();
            return $"{displayName} {displayDescription}";
        }

        return entityManager.GetName(entity).ToString();
    }

    private static bool ContainsToken(string value, string token)
    {
        return !string.IsNullOrEmpty(value) &&
               value.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
