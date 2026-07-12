using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Configs;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.Components;
using Game.UI.Runtime;
using Game.Runtime;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        private static partial class UiShellReadModelAdapter
        {
        public static bool TryReadMatchHudSelection(out UiMatchHudSelectionPanelModel selection)
        {
            selection = UiMatchHudSelectionPanelModel.Hidden;

            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            if (cachedWorld != world)
            {
                cachedWorld = world;
                hasBoundaryQuery = false;
                hasFocusedSelectionQuery = false;
                hasSelectionInputQuery = false;
                hasSelectedUnitsQuery = false;
                hasMinimapMarkerQuery = false;
                hasGridConfigQuery = false;
            }

            if (!hasFocusedSelectionQuery)
            {
                focusedSelectionQuery =
                    world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<FocusedUnitUiReadModelComponent>());
                hasFocusedSelectionQuery = true;
            }

            if (focusedSelectionQuery.IsEmptyIgnoreFilter)
                return TryBuildSelectedGroupModel(world.EntityManager, out selection);

            FocusedUnitUiReadModelComponent component =
                focusedSelectionQuery.GetSingleton<FocusedUnitUiReadModelComponent>();
            if (component.HasFocusedUnit == 0)
                return TryBuildSelectedGroupModel(world.EntityManager, out selection);

            string title = component.Label.ToString();
            if (string.IsNullOrWhiteSpace(title))
                title = "SELECTED UNIT";

            string subtitle = component.Description.ToString();
            if (string.IsNullOrWhiteSpace(subtitle))
                subtitle = component.IsVehicle != 0 ? "VEHICLE" : "TACTICAL ASSET";

            string order = ToSelectionOrderText(component.Status);
            string healthText = component.HealthText.ToString();
            if (string.IsNullOrWhiteSpace(healthText))
            {
                healthText = component.HasHealth != 0 && component.HealthMax > 0
                    ? $"{component.HealthCurrent} / {component.HealthMax}"
                    : "HEALTH -";
            }

            float health01 = component.HasHealth != 0 && component.HealthMax > 0
                ? Mathf.Clamp01((float)component.HealthCurrent / component.HealthMax)
                : 0f;

            bool owned = component.OwnedByPlayer != 0;
            selection = new UiMatchHudSelectionPanelModel(
                true,
                title,
                subtitle,
                order,
                healthText,
                health01,
                component.IsVehicle == 0,
                owned,
                owned,
                ResolveBoardEnabled(world.EntityManager, component.FocusedUnit));
            return true;
        }

        private static bool TryBuildSelectedGroupModel(EntityManager entityManager, out UiMatchHudSelectionPanelModel selection)
        {
            selection = UiMatchHudSelectionPanelModel.Hidden;
            EnsureSelectedUnitsQuery(entityManager);
            if (selectedUnitsQuery.IsEmptyIgnoreFilter)
                return true;

            SelectedGroupSummary summary = BuildSelectedGroupSummary(entityManager);
            if (summary.SelectedCount <= 0)
                return true;

            selection = new UiMatchHudSelectionPanelModel(
                true,
                summary.Title,
                summary.Subtitle,
                summary.OrderText,
                string.IsNullOrWhiteSpace(summary.HealthText) ? "-" : summary.HealthText,
                summary.Health01,
                false,
                true,
                true,
                ResolveSelectedBoardEnabled(entityManager));
            return true;
        }

        private static SelectedGroupSummary BuildSelectedGroupSummary(EntityManager entityManager)
        {
            SelectedGroupSummary summary = new();
            EntityTypeHandle entityType = entityManager.GetEntityTypeHandle();
            using NativeArray<ArchetypeChunk> chunks = selectedUnitsQuery.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity entity = entities[i];
                    if (!entityManager.Exists(entity))
                        continue;

                    summary.SelectedCount++;
                    bool vehicle = IsVehicleUnit(entityManager, entity);
                    bool aircraft = entityManager.HasComponent<UnitAirComponent>(entity) ||
                                    entityManager.HasComponent<UnitAirMovement>(entity);
                    if (aircraft)
                        summary.AircraftCount++;
                    else if (vehicle)
                        summary.VehicleCount++;
                    else
                        summary.SoldierCount++;

                    if (entityManager.HasComponent<UnitHealth>(entity))
                    {
                        UnitHealth health = entityManager.GetComponentData<UnitHealth>(entity);
                        summary.HealthCurrent += math.max(0, health.Current);
                        summary.HealthMax += math.max(0, health.Max);
                    }

                    string order = ResolveEntityOrderText(entityManager, entity);
                    if (summary.OrderText == null)
                        summary.OrderText = order;
                    else if (summary.OrderText != order)
                        summary.MixedOrders = true;
                }
            }

            summary.OrderText = summary.MixedOrders
                ? GameText.Get("selection.order.mixed_orders", "Mixed orders")
                : summary.OrderText ?? GameText.Get("selection.order.idle", "Idle");
            if (summary.HealthMax > 0)
            {
                summary.Health01 = Mathf.Clamp01((float)summary.HealthCurrent / summary.HealthMax);
                summary.HealthText = GameText.Format("selection.health.summary_value", "{0} / {1}", summary.HealthCurrent, summary.HealthMax);
            }
            else
            {
                summary.Health01 = 0f;
                summary.HealthText = GameText.Get("selection.health.summary_empty", "HEALTH -");
            }

            if (summary.SelectedCount == summary.SoldierCount)
            {
                summary.Title = summary.SelectedCount == 1
                    ? GameText.Get("selection.shell.title.soldier", "SOLDIER")
                    : GameText.Format("selection.title.soldiers", "{0} SOLDIERS", summary.SelectedCount);
                summary.Subtitle = GameText.Get("selection.shell.subtitle.infantry_group", "INFANTRY GROUP");
            }
            else if (summary.SelectedCount == summary.VehicleCount)
            {
                summary.Title = summary.SelectedCount == 1
                    ? GameText.Get("selection.shell.title.vehicle", "VEHICLE")
                    : GameText.Format("selection.title.vehicles", "{0} VEHICLES", summary.SelectedCount);
                summary.Subtitle = GameText.Get("selection.shell.subtitle.armored_group", "ARMORED GROUP");
            }
            else if (summary.SelectedCount == summary.AircraftCount)
            {
                summary.Title = summary.SelectedCount == 1
                    ? GameText.Get("selection.shell.title.aircraft", "AIRCRAFT")
                    : GameText.Format("selection.title.aircraft", "{0} AIRCRAFT", summary.SelectedCount);
                summary.Subtitle = GameText.Get("selection.shell.subtitle.air_group", "AIR GROUP");
            }
            else
            {
                summary.Title = GameText.Format("selection.shell.title.selected", "{0} SELECTED", summary.SelectedCount);
                summary.Subtitle = GameText.Get("selection.shell.subtitle.mixed_group", "MIXED GROUP");
            }

            return summary;
        }

        private static string ResolveEntityOrderText(EntityManager entityManager, Entity entity)
        {
            if (entityManager.HasComponent<UnitTransportBoardingTarget>(entity))
                return GameText.Get("selection.order.boarding_transport", "Boarding transport");
            if (entityManager.HasComponent<EngageTarget>(entity))
                return GameText.Get("selection.order.engaging_target", "Engaging target");
            if (entityManager.HasComponent<ManualMoveOrderTag>(entity) ||
                entityManager.HasComponent<ManualMoveGroupMemberTag>(entity))
            {
                return GameText.Get("selection.order.moving", "Moving");
            }

            if (entityManager.HasComponent<HoldPositionOrderTag>(entity))
                return GameText.Get("selection.order.holding", "Holding");
            return GameText.Get("selection.order.idle", "Idle");
        }

        private static void EnsureSelectedUnitsQuery(EntityManager entityManager)
        {
            if (hasSelectedUnitsQuery && cachedWorld == entityManager.World)
                return;

            selectedUnitsQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
            hasSelectedUnitsQuery = true;
        }

        private static bool ResolveSelectedBoardEnabled(EntityManager entityManager)
        {
            EnsureSelectedUnitsQuery(entityManager);
            if (selectedUnitsQuery.IsEmptyIgnoreFilter)
                return false;

            EntityTypeHandle entityType = entityManager.GetEntityTypeHandle();
            using NativeArray<ArchetypeChunk> chunks = selectedUnitsQuery.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
                for (int i = 0; i < entities.Length; i++)
                {
                    if (ResolveBoardEnabled(entityManager, entities[i]))
                        return true;
                }
            }

            return false;
        }

        private static bool ResolveBoardEnabled(EntityManager entityManager, Entity entity)
        {
            if (!entityManager.Exists(entity) ||
                !entityManager.HasComponent<Faction>(entity) ||
                !FactionIdentity.IsPlayerControlled(entityManager.GetComponentData<Faction>(entity).Id))
            {
                return false;
            }

            if (entityManager.HasComponent<UnitTransportPassenger>(entity) ||
                entityManager.HasComponent<UnitTransportCargoPassenger>(entity))
            {
                return false;
            }

            if (IsTransportWithOpenCapacity(entityManager, entity))
                return true;

            return IsSoldierBoardingCandidate(entityManager, entity);
        }

        private static bool IsSoldierBoardingCandidate(EntityManager entityManager, Entity entity)
        {
            return entityManager.HasComponent<UnitMove>(entity) &&
                   !IsVehicleUnit(entityManager, entity) &&
                   !entityManager.HasComponent<UnitAirComponent>(entity) &&
                   !entityManager.HasComponent<UnitAirMovement>(entity);
        }

        private static bool IsTransportWithOpenCapacity(EntityManager entityManager, Entity entity)
        {
            int capacity = 0;
            if (entityManager.HasComponent<UnitTransportCapacity>(entity))
                capacity += math.max(0, entityManager.GetComponentData<UnitTransportCapacity>(entity).SoldierCapacity);
            if (entityManager.HasComponent<UnitTransportCargoCapacity>(entity))
            {
                UnitTransportCargoCapacity cargoCapacity = entityManager.GetComponentData<UnitTransportCargoCapacity>(entity);
                capacity += math.max(0, cargoCapacity.SoldierCapacity) + math.max(0, cargoCapacity.VehicleCapacity);
            }

            if (capacity <= 0)
                return false;

            int occupied = entityManager.HasBuffer<UnitTransportPassengerElement>(entity)
                ? entityManager.GetBuffer<UnitTransportPassengerElement>(entity, true).Length
                : 0;
            return occupied < capacity;
        }

        private static bool IsVehicleUnit(EntityManager entityManager, Entity entity)
        {
            if (!entityManager.HasComponent<UnitFootprint>(entity) ||
                !entityManager.HasComponent<UnitMovementBehavior>(entity))
            {
                return false;
            }

            return UnitVehicleMovementUtility.IsVehicle(
                entityManager.GetComponentData<UnitFootprint>(entity),
                entityManager.GetComponentData<UnitMovementBehavior>(entity));
        }

        private struct SelectedGroupSummary
        {
            public int SelectedCount;
            public int SoldierCount;
            public int VehicleCount;
            public int AircraftCount;
            public int HealthCurrent;
            public int HealthMax;
            public bool MixedOrders;
            public string Title;
            public string Subtitle;
            public string OrderText;
            public string HealthText;
            public float Health01;
        }


        }
    }
}
