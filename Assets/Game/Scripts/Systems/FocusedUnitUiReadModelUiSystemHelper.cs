using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Configs;
using Game.Tactical.Contracts;
using Game.Components;

namespace Game.Runtime
{
    public sealed class FocusedUnitUiReadModelUiSystemHelper
    {
        private World _queryWorld;
        private EntityQuery _readModelQuery;
        private readonly List<SelectionUiReadModelLookup.TransportPassengerUiInfo> _passengerScratch = new();

        public void Publish(
            EntityManager em,
            SelectionStateCompositionSystemHelper selectionStateSystem,
            SelectionUiReadModelLookup selectionUiReadModelLookup,
            UnitTransportCapacitySystem transportCapacitySystem,
            float timeSeconds)
        {
            Entity readModelEntity = EnsureReadModelEntity(em);
            DynamicBuffer<FocusedUnitPassengerUiReadModelElement> passengerBuffer =
                em.GetBuffer<FocusedUnitPassengerUiReadModelElement>(readModelEntity);
            passengerBuffer.Clear();
            FocusedUnitUiReadModelComponent previousModel = em.GetComponentData<FocusedUnitUiReadModelComponent>(readModelEntity);

            Entity focusedUnit = selectionStateSystem.FocusedUnit;
            if (!selectionUiReadModelLookup.HasFocusedUnit(em, focusedUnit))
            {
                FocusedUnitUiReadModelComponent emptyModel = EmptyModel();
                emptyModel.CommandStateVersion = previousModel.CommandStateVersion == 0u || previousModel.HasFocusedUnit != 0
                    ? NextVersion(previousModel.CommandStateVersion)
                    : previousModel.CommandStateVersion;
                em.SetComponentData(readModelEntity, emptyModel);
                return;
            }

            bool sameFocusedUnit = previousModel.HasFocusedUnit != 0 && previousModel.FocusedUnit == focusedUnit;
            FocusedUnitUiReadModelComponent model = new()
            {
                FocusedUnit = focusedUnit,
                HasFocusedUnit = 1,
                OwnedByPlayer = selectionUiReadModelLookup.IsOwnedByPlayer(em, focusedUnit) ? (byte)1 : (byte)0,
                IsVehicle = selectionUiReadModelLookup.IsVehicleUnit(em, focusedUnit) ? (byte)1 : (byte)0,
                CanAttack = selectionUiReadModelLookup.CanAttack(em, focusedUnit) ? (byte)1 : (byte)0,
                Label = sameFocusedUnit
                    ? previousModel.Label
                    : ToFixed64(selectionUiReadModelLookup.ResolveFocusedUnitName(em, focusedUnit)),
                Description = sameFocusedUnit
                    ? previousModel.Description
                    : ToFixed128(selectionUiReadModelLookup.ResolveFocusedUnitDescription(em, focusedUnit)),
                Status = (int)selectionUiReadModelLookup.GetFocusedUnitUiStatus(em, focusedUnit)
            };

            model.CanHold = selectionUiReadModelLookup.CanHoldPosition(em, focusedUnit, out TacticalCommandReasonCode holdReason) ? (byte)1 : (byte)0;
            model.HoldDisabledReason = (int)holdReason;
            model.CanStop = selectionUiReadModelLookup.CanStop(em, focusedUnit, out TacticalCommandReasonCode stopReason) ? (byte)1 : (byte)0;
            model.StopDisabledReason = (int)stopReason;
            model.CanScan = selectionUiReadModelLookup.CanScan(em, focusedUnit, out TacticalCommandReasonCode scanReason) ? (byte)1 : (byte)0;
            model.ScanDisabledReason = (int)scanReason;
            model.CommandStateVersion = ShouldAdvanceCommandStateVersion(previousModel, model)
                ? NextVersion(previousModel.CommandStateVersion)
                : previousModel.CommandStateVersion;

            if (selectionUiReadModelLookup.TryGetFocusedUnitHealth(em, focusedUnit, out int healthCurrent, out int healthMax))
            {
                model.HasHealth = 1;
                model.HealthCurrent = healthCurrent;
                model.HealthMax = healthMax;
                model.HealthText = sameFocusedUnit &&
                                   previousModel.HasHealth != 0 &&
                                   previousModel.HealthCurrent == healthCurrent &&
                                   previousModel.HealthMax == healthMax
                    ? previousModel.HealthText
                    : ToHealthFixed32(healthCurrent, healthMax);
            }
            else
            {
                model.HealthText = sameFocusedUnit && previousModel.HasHealth == 0
                    ? previousModel.HealthText
                    : ToFixed32(GameText.Get("selection.health.empty", "Health: -"));
            }

            if (selectionUiReadModelLookup.TryGetFocusedUnitCapacityInfo(
                    em,
                    focusedUnit,
                    timeSeconds,
                    out int capacityCurrent,
                    out int capacityMax,
                    out float capacityProgress01))
            {
                model.HasCapacity = 1;
                model.CapacityCurrent = capacityCurrent;
                model.CapacityMax = capacityMax;
                model.CapacityProgress01 = capacityProgress01;
            }

            if (selectionUiReadModelLookup.TryGetFocusedUnitResourceCargoInfo(
                    em,
                    focusedUnit,
                    timeSeconds,
                    out int resourceOilBarrels,
                    out int resourceFuelBarrels,
                    out int resourceCapacity))
            {
                model.HasResourceCargo = 1;
                model.ResourceCargoOilBarrels = resourceOilBarrels;
                model.ResourceCargoFuelBarrels = resourceFuelBarrels;
                model.ResourceCargoCapacity = resourceCapacity;
                model.ResourceCargoStatusText = ResolveResourceCargoStatusText(em, focusedUnit);
            }

            if (selectionUiReadModelLookup.TryGetTransportPassengerBreakdown(
                    em,
                    focusedUnit,
                    transportCapacitySystem,
                    out int soldierPassengerCount,
                    out int soldierPassengerCapacity,
                    out int vehiclePassengerCount,
                    out int vehiclePassengerCapacity))
            {
                model.TransportSoldierPassengerCount = soldierPassengerCount;
                model.TransportSoldierPassengerCapacity = soldierPassengerCapacity;
                model.TransportVehiclePassengerCount = vehiclePassengerCount;
                model.TransportVehiclePassengerCapacity = vehiclePassengerCapacity;
                model.PassengerCount = soldierPassengerCount + vehiclePassengerCount;
                model.TransportPassengerCapacity = soldierPassengerCapacity + vehiclePassengerCapacity;
            }
            _passengerScratch.Clear();
            selectionUiReadModelLookup.GetTransportPassengers(em, focusedUnit, transportCapacitySystem, _passengerScratch);
            for (int i = 0; i < _passengerScratch.Count; i++)
            {
                SelectionUiReadModelLookup.TransportPassengerUiInfo passenger = _passengerScratch[i];
                passengerBuffer.Add(new FocusedUnitPassengerUiReadModelElement
                {
                    Passenger = passenger.Entity,
                    DisplayName = ToFixed64(passenger.DisplayName),
                    HealthCurrent = passenger.HealthCurrent,
                    HealthMax = passenger.HealthMax
                });
            }

            if (selectionUiReadModelLookup.TryGetFocusedUnitWorldPosition(em, focusedUnit, out Vector3 worldPosition))
            {
                model.HasWorldPosition = 1;
                model.WorldPosition = worldPosition;
            }

            if (selectionUiReadModelLookup.TryGetFocusedUnitPortraitPose(em, focusedUnit, out Vector3 portraitWorldPosition, out Vector3 portraitForward))
            {
                model.HasPortraitPose = 1;
                model.PortraitWorldPosition = portraitWorldPosition;
                model.PortraitForward = portraitForward;
            }

            em.SetComponentData(readModelEntity, model);
        }

        public bool TryRead(
            EntityManager em,
            out FocusedUnitUiReadModelComponent model,
            out DynamicBuffer<FocusedUnitPassengerUiReadModelElement> passengers)
        {
            Entity readModelEntity = EnsureReadModelEntity(em);
            model = em.GetComponentData<FocusedUnitUiReadModelComponent>(readModelEntity);
            passengers = em.GetBuffer<FocusedUnitPassengerUiReadModelElement>(readModelEntity);
            return true;
        }

        private Entity EnsureReadModelEntity(EntityManager em)
        {
            World world = em.World;
            if (_queryWorld != world || world == null || !world.IsCreated)
            {
                _queryWorld = world;
                _readModelQuery = em.CreateEntityQuery(
                    ComponentType.ReadWrite<FocusedUnitUiReadModelComponent>(),
                    ComponentType.ReadWrite<FocusedUnitPassengerUiReadModelElement>());
            }

            if (!_readModelQuery.IsEmptyIgnoreFilter)
                return _readModelQuery.GetSingletonEntity();

            Entity entity = em.CreateEntity(typeof(FocusedUnitUiReadModelComponent));
            em.SetName(entity, "FocusedUnitUiReadModel");
            em.AddBuffer<FocusedUnitPassengerUiReadModelElement>(entity);
            em.SetComponentData(entity, EmptyModel());
            return entity;
        }

        private static FocusedUnitUiReadModelComponent EmptyModel()
        {
            return new FocusedUnitUiReadModelComponent
            {
                FocusedUnit = Entity.Null,
                Label = ToFixed64(GameText.Get("selection.focused_unit.empty_label", "Unit")),
                Description = ToFixed128(GameText.Get("selection.focused_unit.empty_description", "Select a unit to inspect it.")),
                HealthText = ToFixed32(GameText.Get("selection.health.empty", "Health: -")),
                PortraitForward = new float3(0f, 0f, 1f)
            };
        }

        private static bool ShouldAdvanceCommandStateVersion(
            in FocusedUnitUiReadModelComponent previous,
            in FocusedUnitUiReadModelComponent current)
        {
            return previous.CommandStateVersion == 0u ||
                   previous.FocusedUnit != current.FocusedUnit ||
                   previous.HasFocusedUnit != current.HasFocusedUnit ||
                   previous.OwnedByPlayer != current.OwnedByPlayer ||
                   previous.CanHold != current.CanHold ||
                   previous.HoldDisabledReason != current.HoldDisabledReason ||
                   previous.CanStop != current.CanStop ||
                   previous.StopDisabledReason != current.StopDisabledReason ||
                   previous.CanScan != current.CanScan ||
                   previous.ScanDisabledReason != current.ScanDisabledReason;
        }

        private static FixedString32Bytes ResolveResourceCargoStatusText(EntityManager em, Entity focusedUnit)
        {
            if (!em.HasComponent<UnitResourceHaulStatus>(focusedUnit))
                return default;

            UnitResourceHaulStatus status = em.GetComponentData<UnitResourceHaulStatus>(focusedUnit);
            FuelLogisticsTaskStatusCode statusCode = (FuelLogisticsTaskStatusCode)status.StatusCode;
            FuelLogisticsBlockReasonCode reasonCode = (FuelLogisticsBlockReasonCode)status.ReasonCode;
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind =
                (ResourceHaulerUtilitySystemHelper.ResourceHaulKind)status.ResourceKind;

            if (statusCode == FuelLogisticsTaskStatusCode.Blocked)
                return ResolveResourceCargoBlockText(reasonCode, resourceKind);

            return statusCode switch
            {
                FuelLogisticsTaskStatusCode.Idle => ToFixed32("IDLE"),
                FuelLogisticsTaskStatusCode.Assigned => ToFixed32("ASSIGNED"),
                FuelLogisticsTaskStatusCode.ToSource => ToFixed32("TO SOURCE"),
                FuelLogisticsTaskStatusCode.Loading => ToFixed32("LOADING"),
                FuelLogisticsTaskStatusCode.ToDestination => ToFixed32("TO STORAGE"),
                FuelLogisticsTaskStatusCode.Unloading => ToFixed32("UNLOADING"),
                _ => default
            };
        }

        private static FixedString32Bytes ResolveResourceCargoBlockText(
            FuelLogisticsBlockReasonCode reasonCode,
            ResourceHaulerUtilitySystemHelper.ResourceHaulKind resourceKind)
        {
            return reasonCode switch
            {
                FuelLogisticsBlockReasonCode.SourceUnavailable =>
                    resourceKind == ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Fuel
                        ? ToFixed32("WAITING FUEL")
                        : ToFixed32("WAITING OIL"),
                FuelLogisticsBlockReasonCode.DestinationUnavailable => ToFixed32("NO STORAGE"),
                FuelLogisticsBlockReasonCode.DestinationFull => ToFixed32("STORAGE FULL"),
                FuelLogisticsBlockReasonCode.RouteUnavailable => ToFixed32("NO ROUTE"),
                FuelLogisticsBlockReasonCode.ReservationFailed => ToFixed32("RESERVATION"),
                FuelLogisticsBlockReasonCode.HaulerUnavailable => ToFixed32("UNAVAILABLE"),
                FuelLogisticsBlockReasonCode.InsufficientUsableFuel => ToFixed32("NO FUEL"),
                _ => ToFixed32("BLOCKED")
            };
        }

        private static uint NextVersion(uint version)
        {
            uint next = version + 1u;
            return next == 0u ? 1u : next;
        }

        private static FixedString32Bytes ToFixed32(string value)
        {
            FixedString32Bytes result = default;
            result.Append(Trim(value, 29));
            return result;
        }

        private static FixedString32Bytes ToHealthFixed32(int current, int max)
        {
            return ToFixed32(GameText.Format("selection.health.value", "Health: {0}/{1}", current, max));
        }

        private static FixedString64Bytes ToFixed64(string value)
        {
            FixedString64Bytes result = default;
            result.Append(Trim(value, 61));
            return result;
        }

        private static FixedString128Bytes ToFixed128(string value)
        {
            FixedString128Bytes result = default;
            result.Append(Trim(value, 125));
            return result;
        }

        private static string Trim(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
