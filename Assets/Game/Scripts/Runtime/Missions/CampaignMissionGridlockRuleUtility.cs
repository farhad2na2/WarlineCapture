using Game.Components;
using Unity.Mathematics;

namespace Game.Runtime
{
    /// <summary>Pure work/hold rules; world owners must confirm obstruction removal and actual arrival.</summary>
    internal static class CampaignMissionGridlockRuleUtility
    {
        internal static GridlockWorkStatus AdvanceWork(ref int accumulated, int required, int delta,
            bool active, bool fadiPresent, bool workerPresent, bool contested, bool removalRequested, bool removed)
        {
            if (removed) return GridlockWorkStatus.Complete;
            if (removalRequested) return GridlockWorkStatus.Clearing;
            if (contested) return GridlockWorkStatus.ThreatNearby;
            if (!fadiPresent || !workerPresent) return GridlockWorkStatus.CrewMissing;
            if (active) accumulated = AddClamped(accumulated, delta, required);
            return accumulated >= required ? GridlockWorkStatus.Clearing : GridlockWorkStatus.Working;
        }

        internal static void AdvanceHold(ref int accumulated, int required, int delta, bool active,
            bool sitesComplete, bool routeConnected, bool vehicleAtHospital, bool contested)
        {
            if (!active) return;
            if (!sitesComplete || !routeConnected || !vehicleAtHospital || contested) accumulated = 0;
            else accumulated = AddClamped(accumulated, delta, required);
        }

        internal static GridlockFailure Failure(in CampaignMissionGridlockState state, int deadline)
        {
            if (state.Failure == GridlockFailure.Integrity) return GridlockFailure.Integrity;
            if (state.Ready == 0) return GridlockFailure.None;
            if (state.LivingFadi == 0) return GridlockFailure.FadiLost;
            if (state.LivingWorkers == 0) return GridlockFailure.WorkersLost;
            if (state.LivingRifles == 0) return GridlockFailure.RiflesLost;
            if (state.LivingVehicle == 0) return GridlockFailure.VehicleLost;
            if (state.ElapsedMilliseconds >= deadline) return GridlockFailure.Deadline;
            return GridlockFailure.None;
        }

        internal static bool IsVictory(in CampaignMissionGridlockState state, bool sitesComplete, int hold, int deadline) =>
            state.Ready != 0 && Failure(state, deadline) == GridlockFailure.None && sitesComplete &&
            state.RouteConnected != 0 && state.VehicleArrived != 0 && state.HoldMilliseconds >= hold;

        private static int AddClamped(int current, int delta, int maximum) =>
            (int)math.min((long)math.max(0, maximum), (long)math.max(0, current) + math.max(0, delta));
    }
}
