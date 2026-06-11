using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public sealed class SelectionSummaryQuerySystem
{
    public readonly struct Summary
    {
        public readonly int UnitCount;
        public readonly int SoldierCount;
        public readonly int VehicleCount;
        public readonly int AircraftCount;
        public readonly int TransportCount;
        public readonly int BuildingCount;
        public readonly string Title;
        public readonly string Subtitle;
        public readonly string OrderText;
        public readonly string HealthText;
        public readonly float Health01;
        public readonly SelectionSummaryPortraitKind PortraitKind;

        public Summary(
            int unitCount,
            int soldierCount,
            int vehicleCount,
            int aircraftCount,
            int transportCount,
            int buildingCount,
            string title,
            string subtitle,
            string orderText,
            string healthText,
            float health01,
            SelectionSummaryPortraitKind portraitKind)
        {
            UnitCount = unitCount;
            SoldierCount = soldierCount;
            VehicleCount = vehicleCount;
            AircraftCount = aircraftCount;
            TransportCount = transportCount;
            BuildingCount = buildingCount;
            Title = title;
            Subtitle = subtitle;
            OrderText = orderText;
            HealthText = healthText;
            Health01 = health01;
            PortraitKind = portraitKind;
        }
    }

    public Summary BuildSelectedSummary(EntityManager em, SelectionUiQuerySystem selectionUiQuerySystem, bool includeSelectedBuilding)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
        if (query.IsEmptyIgnoreFilter)
        {
            int noSelectionBuildingCount = includeSelectedBuilding ? 1 : 0;
            return new Summary(
                0,
                0,
                0,
                0,
                0,
                noSelectionBuildingCount,
                noSelectionBuildingCount > 0 ? "1 STRUCTURE" : "NO SELECTION",
                noSelectionBuildingCount > 0 ? "Building selected" : string.Empty,
                noSelectionBuildingCount > 0 ? "Structure selected" : "Idle",
                "Health: -",
                0f,
                noSelectionBuildingCount > 0 ? SelectionSummaryPortraitKind.Buildings : SelectionSummaryPortraitKind.None);
        }

        int unitCount = 0;
        int soldierCount = 0;
        int vehicleCount = 0;
        int aircraftCount = 0;
        int transportCount = 0;
        int currentTotal = 0;
        int maxTotal = 0;
        bool hasOrder = false;
        bool mixedOrders = false;
        SelectionUiQuerySystem.FocusedUnitUiStatus firstOrder = SelectionUiQuerySystem.FocusedUnitUiStatus.Idle;

        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity))
                continue;

            unitCount++;
            UnitCategory category = ResolveCategory(em, entity);
            soldierCount += category == UnitCategory.Soldier ? 1 : 0;
            vehicleCount += category == UnitCategory.Vehicle ? 1 : 0;
            aircraftCount += category == UnitCategory.Aircraft ? 1 : 0;
            transportCount += category == UnitCategory.Transport ? 1 : 0;

            if (em.HasComponent<UnitHealth>(entity))
            {
                UnitHealth health = em.GetComponentData<UnitHealth>(entity);
                currentTotal += math.max(0, health.Current);
                maxTotal += math.max(0, health.Max);
            }

            SelectionUiQuerySystem.FocusedUnitUiStatus order = selectionUiQuerySystem.GetFocusedUnitUiStatus(em, entity);
            if (!hasOrder)
            {
                firstOrder = order;
                hasOrder = true;
            }
            else if (firstOrder != order)
            {
                mixedOrders = true;
            }
        }

        int buildingCount = includeSelectedBuilding ? 1 : 0;
        string healthText = maxTotal > 0 ? $"Health: {currentTotal}/{maxTotal}" : "Health: -";
        float health01 = maxTotal > 0 ? math.saturate((float)currentTotal / maxTotal) : 0f;
        string orderText = mixedOrders ? "Mixed orders" : ToOrderText(firstOrder);
        SelectionSummaryPortraitKind portraitKind = ResolvePortraitKind(soldierCount, vehicleCount, aircraftCount, transportCount, buildingCount);

        return new Summary(
            unitCount,
            soldierCount,
            vehicleCount,
            aircraftCount,
            transportCount,
            buildingCount,
            ResolveTitle(unitCount, soldierCount, vehicleCount, aircraftCount, transportCount, buildingCount),
            ResolveSubtitle(unitCount, soldierCount, vehicleCount, aircraftCount, transportCount, buildingCount),
            orderText,
            healthText,
            health01,
            portraitKind);
    }

    private static string ResolveTitle(int unitCount, int soldierCount, int vehicleCount, int aircraftCount, int transportCount, int buildingCount)
    {
        if (unitCount <= 0)
            return buildingCount == 1 ? "1 STRUCTURE" : "NO SELECTION";
        if (buildingCount > 0)
            return "MIXED SELECTION";
        if (soldierCount == unitCount)
            return unitCount == 1 ? "1 SOLDIER" : $"{unitCount} SOLDIERS";
        if (transportCount == unitCount)
            return unitCount == 1 ? "1 TRANSPORT" : $"{unitCount} TRANSPORTS";
        if (aircraftCount == unitCount)
            return unitCount == 1 ? "1 AIRCRAFT" : $"{unitCount} AIRCRAFT";
        if (vehicleCount == unitCount)
            return unitCount == 1 ? "1 VEHICLE" : $"{unitCount} VEHICLES";
        if (aircraftCount > 0 && soldierCount + vehicleCount + transportCount > 0)
            return "MIXED FORCE";

        return "MIXED SQUAD";
    }

    private static string ResolveSubtitle(int unitCount, int soldierCount, int vehicleCount, int aircraftCount, int transportCount, int buildingCount)
    {
        if (unitCount <= 0)
            return buildingCount > 0 ? "Building group" : string.Empty;
        if (buildingCount > 0)
            return $"{unitCount} units / {buildingCount} structure";
        if (soldierCount == unitCount)
            return "Infantry squad";
        if (transportCount == unitCount)
            return "Transport group";
        if (aircraftCount == unitCount)
            return "Air wing";
        if (vehicleCount == unitCount)
            return "Vehicle squad";

        int groundCount = soldierCount + vehicleCount + transportCount;
        if (aircraftCount > 0 && groundCount > 0)
            return $"{groundCount} ground / {aircraftCount} air";
        if (soldierCount > 0 && vehicleCount + transportCount > 0)
            return $"{soldierCount} infantry / {vehicleCount + transportCount} vehicles";

        return $"{unitCount} selected units";
    }

    private static SelectionSummaryPortraitKind ResolvePortraitKind(int soldierCount, int vehicleCount, int aircraftCount, int transportCount, int buildingCount)
    {
        int categories = 0;
        categories += soldierCount > 0 ? 1 : 0;
        int groundVehicleCount = vehicleCount + transportCount;
        categories += groundVehicleCount > 0 ? 1 : 0;
        categories += aircraftCount > 0 ? 1 : 0;
        categories += buildingCount > 0 ? 1 : 0;

        if (buildingCount > 0)
            return SelectionSummaryPortraitKind.MixedForce;
        if (soldierCount > 0 && groundVehicleCount > 0 && aircraftCount > 0)
            return SelectionSummaryPortraitKind.MixedSoldierVehicleAircraft;
        if (soldierCount > 0 && aircraftCount > 0)
            return SelectionSummaryPortraitKind.MixedSoldierAircraft;
        if (groundVehicleCount > 0 && aircraftCount > 0)
            return SelectionSummaryPortraitKind.MixedVehicleAircraft;
        if (soldierCount > 0 && groundVehicleCount > 0)
            return SelectionSummaryPortraitKind.MixedSoldierVehicle;
        if (categories != 1)
            return SelectionSummaryPortraitKind.MixedForce;
        if (soldierCount > 0)
            return SelectionSummaryPortraitKind.Soldiers;
        if (transportCount > 0)
            return SelectionSummaryPortraitKind.Vehicles;
        if (aircraftCount > 0)
            return SelectionSummaryPortraitKind.Aircraft;
        if (vehicleCount > 0)
            return SelectionSummaryPortraitKind.Vehicles;
        if (buildingCount > 0)
            return SelectionSummaryPortraitKind.Buildings;

        return SelectionSummaryPortraitKind.GenericSquad;
    }

    private static UnitCategory ResolveCategory(EntityManager em, Entity entity)
    {
        string source = ResolveSource(em, entity);
        string lower = source.ToLowerInvariant();
        bool isAir = em.HasComponent<UnitAirMovement>(entity);
        bool hasTransportCapacity = em.HasComponent<UnitTransportCapacity>(entity) &&
                                    em.GetComponentData<UnitTransportCapacity>(entity).SoldierCapacity > 0;
        bool usesVehicleMotion = isAir ||
                                 (em.HasComponent<UnitMovementBehavior>(entity) &&
                                  em.GetComponentData<UnitMovementBehavior>(entity).UsesVehicleMotion != 0);
        bool namedTransport = ContainsAny(lower, "transport", "apc", "truck", "tanker", "hauler", "canopy");
        if (isAir)
            return UnitCategory.Aircraft;
        if (hasTransportCapacity || namedTransport && usesVehicleMotion)
            return UnitCategory.Transport;
        if (usesVehicleMotion || lower.Contains("unit_veh_", System.StringComparison.OrdinalIgnoreCase))
            return UnitCategory.Vehicle;

        return UnitCategory.Soldier;
    }

    private static string ResolveSource(EntityManager em, Entity entity)
    {
        if (em.HasComponent<UnitSourcePrefabKey>(entity))
        {
            string source = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
            if (!string.IsNullOrWhiteSpace(source))
                return source;
        }

        if (em.HasComponent<UnitDisplayInfo>(entity))
        {
            string displayName = em.GetComponentData<UnitDisplayInfo>(entity).Name.ToString();
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName;
        }

        return em.GetName(entity);
    }

    private static string ToOrderText(SelectionUiQuerySystem.FocusedUnitUiStatus status)
    {
        return status switch
        {
            SelectionUiQuerySystem.FocusedUnitUiStatus.ReturningToBase => "Returning to base",
            SelectionUiQuerySystem.FocusedUnitUiStatus.MissileLaunched => "Missile launched",
            SelectionUiQuerySystem.FocusedUnitUiStatus.AirspaceClear => "Airspace clear",
            SelectionUiQuerySystem.FocusedUnitUiStatus.TrackingAirTarget => "Tracking air target",
            SelectionUiQuerySystem.FocusedUnitUiStatus.InterceptingMissile => "Intercepting missile",
            SelectionUiQuerySystem.FocusedUnitUiStatus.AirDefenseReloading => "Reloading",
            SelectionUiQuerySystem.FocusedUnitUiStatus.Engaged => "Engaging target",
            SelectionUiQuerySystem.FocusedUnitUiStatus.Moving => "Moving",
            _ => "Idle"
        };
    }

    private static bool ContainsAny(string value, params string[] needles)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        for (int i = 0; i < needles.Length; i++)
        {
            if (value.Contains(needles[i], System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private enum UnitCategory
    {
        Soldier = 0,
        Vehicle = 1,
        Aircraft = 2,
        Transport = 3
    }
}
