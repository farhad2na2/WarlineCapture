using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.Components;

namespace Game.Runtime
{
    public sealed class SelectionUiReadModelUiSystemHelper : ISelectionUiReadModel
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

        private readonly FocusedUnitUiReadModelUiSystemHelper _focusedUnitUiReadModelSystem = new();
        private readonly SelectionUiReadModelLookup _selectionUiReadModelLookup = new();
        private readonly VisibleUnitSelectionCameraSystemHelper _visibleUnitSelectionSystem = new();

        private Unity.Entities.World _queryWorld;
        private EntityQuery _selectedTagQuery;

        public bool HasFocusedUnit =>
            TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model) &&
            model.HasFocusedUnit != 0;

        public bool HasAnySelectedUnits
        {
            get
            {
                if (!TryGetDefaultEntityManager(out EntityManager em))
                    return false;

                EnsureEntityQueries(em);
                return _selectionUiReadModelLookup.HasAnySelectedUnits(_selectedTagQuery);
            }
        }

        public uint CommandStateVersion =>
            TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model)
                ? model.CommandStateVersion
                : 0u;

        public string FocusedUnitLabel =>
            TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model)
                ? model.Label.ToString()
                : "Unit";

        public bool CanDestroyFocusedUnit => FocusedUnitOwnedByPlayer;

        public bool FocusedUnitOwnedByPlayer =>
            TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model) &&
            model.OwnedByPlayer != 0;

        public bool FocusedUnitIsVehicle =>
            TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model) &&
            model.IsVehicle != 0;

        public bool FocusedUnitCanAttack =>
            TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model) &&
            model.CanAttack != 0;

        public bool FocusedUnitCanHold =>
            TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model) &&
            model.CanHold != 0;

        public TacticalCommandReasonCode FocusedUnitHoldDisabledReason =>
            TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model)
                ? ToReasonCode(model.HoldDisabledReason)
                : TacticalCommandReasonCode.NoSelection;

        public bool FocusedUnitCanStop =>
            TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model) &&
            model.CanStop != 0;

        public TacticalCommandReasonCode FocusedUnitStopDisabledReason =>
            TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model)
                ? ToReasonCode(model.StopDisabledReason)
                : TacticalCommandReasonCode.NoSelection;

        public bool FocusedUnitCanScan =>
            TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model) &&
            model.CanScan != 0;

        public TacticalCommandReasonCode FocusedUnitScanDisabledReason =>
            TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model)
                ? ToReasonCode(model.ScanDisabledReason)
                : TacticalCommandReasonCode.NoSelection;

        public int FocusedTransportPassengerCount =>
            TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model)
                ? model.PassengerCount
                : 0;

        public bool CanDisembarkFocusedTransport => FocusedTransportPassengerCount > 0;

        public bool TryGetFocusedUnitHealth(out int current, out int max)
        {
            current = 0;
            max = 0;

            if (!TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model) || model.HasHealth == 0)
                return false;

            current = model.HealthCurrent;
            max = model.HealthMax;
            return true;
        }

        public bool TryGetFocusedUnitEntityForUi(out Entity entity)
        {
            entity = Entity.Null;
            if (!TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model) || model.HasFocusedUnit == 0)
                return false;

            entity = model.FocusedUnit;
            return true;
        }

        public FocusedUnitUiStatus GetFocusedUnitUiStatus()
        {
            return TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model)
                ? ToFocusedUnitUiStatus(model.Status)
                : FocusedUnitUiStatus.Idle;
        }

        public void GetFocusedTransportPassengers(List<TransportPassengerUiInfo> results)
        {
            if (results == null)
                return;

            results.Clear();
            if (!TryReadFocusedUnitUiModel(
                    out _,
                    out DynamicBuffer<FocusedUnitPassengerUiReadModelElement> passengers))
            {
                return;
            }

            for (int i = 0; i < passengers.Length; i++)
            {
                FocusedUnitPassengerUiReadModelElement passenger = passengers[i];
                results.Add(new TransportPassengerUiInfo(
                    passenger.Passenger,
                    passenger.DisplayName.ToString(),
                    passenger.HealthCurrent,
                    passenger.HealthMax));
            }
        }

        public bool HasVisiblePlayerUnits(Camera worldCamera)
        {
            return HasVisiblePlayerUnits(worldCamera, VisibleUnitSelectionCameraSystemHelper.Filter.All);
        }

        public bool HasVisiblePlayerSoldiers(Camera worldCamera)
        {
            return HasVisiblePlayerUnits(worldCamera, VisibleUnitSelectionCameraSystemHelper.Filter.Soldiers);
        }

        public bool HasVisiblePlayerVehicles(Camera worldCamera)
        {
            return HasVisiblePlayerUnits(worldCamera, VisibleUnitSelectionCameraSystemHelper.Filter.Vehicles);
        }

        private bool HasVisiblePlayerUnits(Camera worldCamera, VisibleUnitSelectionCameraSystemHelper.Filter filter)
        {
            if (worldCamera == null || !TryGetDefaultEntityManager(out EntityManager em))
                return false;

            EnsureEntityQueries(em);
            Rect screenRect = new(0f, 0f, Screen.width, Screen.height);
            return _visibleUnitSelectionSystem.HasVisiblePlayerUnits(
                em,
                worldCamera,
                _selectionUiReadModelLookup,
                screenRect,
                filter);
        }

        private bool TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model)
        {
            return TryReadFocusedUnitUiModel(out model, out _);
        }

        private bool TryReadFocusedUnitUiModel(
            out FocusedUnitUiReadModelComponent model,
            out DynamicBuffer<FocusedUnitPassengerUiReadModelElement> passengers)
        {
            model = default;
            passengers = default;
            if (!TryGetDefaultEntityManager(out EntityManager em))
                return false;

            _focusedUnitUiReadModelSystem.TryRead(em, out model, out passengers);
            return true;
        }

        private void EnsureEntityQueries(EntityManager em)
        {
            Unity.Entities.World world = em.World;
            if (_queryWorld == world && world != null && world.IsCreated)
                return;

            _queryWorld = world;
            _selectedTagQuery = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
            _visibleUnitSelectionSystem.EnsureEntityQueries(em);
        }

        private static bool TryGetDefaultEntityManager(out EntityManager em)
        {
            em = default;
            Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            em = world.EntityManager;
            return true;
        }

        private static FocusedUnitUiStatus ToFocusedUnitUiStatus(int status)
        {
            return (SelectionUiReadModelLookup.FocusedUnitUiStatus)status switch
            {
                SelectionUiReadModelLookup.FocusedUnitUiStatus.Moving => FocusedUnitUiStatus.Moving,
                SelectionUiReadModelLookup.FocusedUnitUiStatus.Engaged => FocusedUnitUiStatus.Engaged,
                SelectionUiReadModelLookup.FocusedUnitUiStatus.ReturningToBase => FocusedUnitUiStatus.ReturningToBase,
                SelectionUiReadModelLookup.FocusedUnitUiStatus.MissileLaunched => FocusedUnitUiStatus.MissileLaunched,
                SelectionUiReadModelLookup.FocusedUnitUiStatus.AirspaceClear => FocusedUnitUiStatus.AirspaceClear,
                SelectionUiReadModelLookup.FocusedUnitUiStatus.TrackingAirTarget => FocusedUnitUiStatus.TrackingAirTarget,
                SelectionUiReadModelLookup.FocusedUnitUiStatus.InterceptingMissile => FocusedUnitUiStatus.InterceptingMissile,
                SelectionUiReadModelLookup.FocusedUnitUiStatus.AirDefenseReloading => FocusedUnitUiStatus.AirDefenseReloading,
                _ => FocusedUnitUiStatus.Idle
            };
        }

        private static TacticalCommandReasonCode ToReasonCode(int reason)
        {
            return System.Enum.IsDefined(typeof(TacticalCommandReasonCode), reason)
                ? (TacticalCommandReasonCode)reason
                : TacticalCommandReasonCode.CommandUnavailable;
        }
    }
}
