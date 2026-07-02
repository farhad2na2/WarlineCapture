using Game.Components;

namespace Game.Runtime
{
    internal sealed class CitizenStatusTransitionCompositionSystemHelper
    {
        public delegate CitizenRecordComponent StoreCitizenAction(CitizenRecordComponent citizen);

        public static void SetCitizenStatus(
            CitizenStatusTransitionCompositionSystemHelper system,
            ref CitizenRecordComponent citizen,
            CitizenStatus status,
            int targetBuildingId,
            float stateDurationSeconds,
            float now)
        {
            if (system != null)
            {
                system.SetCitizenStatus(ref citizen, status, targetBuildingId, stateDurationSeconds, now);
                return;
            }

            SetCitizenStatusState(ref citizen, status, targetBuildingId, stateDurationSeconds, now);
        }

        public void SetCitizenStatus(
            ref CitizenRecordComponent citizen,
            CitizenStatus status,
            int targetBuildingId,
            float stateDurationSeconds,
            float now)
        {
            SetCitizenStatusState(ref citizen, status, targetBuildingId, stateDurationSeconds, now);
        }

        public static bool IsTravelStatus(CitizenStatusTransitionCompositionSystemHelper system, CitizenStatus status)
        {
            return system != null
                ? system.IsTravelStatus(status)
                : IsTravelStatusState(status);
        }

        public bool IsTravelStatus(CitizenStatus status)
        {
            return IsTravelStatusState(status);
        }

        public static bool ShouldUseTravelStatus(
            CitizenStatusTransitionCompositionSystemHelper system,
            CitizenPopulationStateCompositionSystemHelper state,
            CitizenRecordComponent citizen,
            CitizenStatus desiredStatus,
            int desiredTargetBuildingId)
        {
            return system != null
                ? system.ShouldUseTravelStatus(state, citizen, desiredStatus, desiredTargetBuildingId)
                : ShouldUseTravelStatusState(state, citizen, desiredStatus, desiredTargetBuildingId);
        }

        public bool ShouldUseTravelStatus(
            CitizenPopulationStateCompositionSystemHelper state,
            CitizenRecordComponent citizen,
            CitizenStatus desiredStatus,
            int desiredTargetBuildingId)
        {
            return ShouldUseTravelStatusState(state, citizen, desiredStatus, desiredTargetBuildingId);
        }

        public static CitizenStatus GetTravelStatusForDesiredStatus(CitizenStatusTransitionCompositionSystemHelper system, CitizenStatus desiredStatus)
        {
            return system != null
                ? system.GetTravelStatusForDesiredStatus(desiredStatus)
                : GetTravelStatusForDesiredStatusState(desiredStatus);
        }

        public CitizenStatus GetTravelStatusForDesiredStatus(CitizenStatus desiredStatus)
        {
            return GetTravelStatusForDesiredStatusState(desiredStatus);
        }

        public static CitizenStatus GetSettledStatus(CitizenStatusTransitionCompositionSystemHelper system, CitizenStatus status)
        {
            return system != null
                ? system.GetSettledStatus(status)
                : GetSettledStatusState(status);
        }

        public CitizenStatus GetSettledStatus(CitizenStatus status)
        {
            return GetSettledStatusState(status);
        }

        public static bool TrySetCitizenStatus(
            CitizenStatusTransitionCompositionSystemHelper system,
            CitizenPopulationStateCompositionSystemHelper state,
            int citizenId,
            CitizenStatus status,
            int targetBuildingId,
            float stateDurationSeconds,
            float now,
            StoreCitizenAction storeCitizen)
        {
            return system != null
                ? system.TrySetCitizenStatus(state, citizenId, status, targetBuildingId, stateDurationSeconds, now, storeCitizen)
                : TrySetCitizenStatusState(state, citizenId, status, targetBuildingId, stateDurationSeconds, now, storeCitizen);
        }

        public bool TrySetCitizenStatus(
            CitizenPopulationStateCompositionSystemHelper state,
            int citizenId,
            CitizenStatus status,
            int targetBuildingId,
            float stateDurationSeconds,
            float now,
            StoreCitizenAction storeCitizen)
        {
            return TrySetCitizenStatusState(state, citizenId, status, targetBuildingId, stateDurationSeconds, now, storeCitizen);
        }

        public static bool TryResolveCitizenArrival(
            CitizenStatusTransitionCompositionSystemHelper system,
            CitizenPopulationStateCompositionSystemHelper state,
            int citizenId,
            float now,
            StoreCitizenAction storeCitizen)
        {
            return system != null
                ? system.TryResolveCitizenArrival(state, citizenId, now, storeCitizen)
                : TryResolveCitizenArrivalState(state, citizenId, now, storeCitizen);
        }

        public bool TryResolveCitizenArrival(
            CitizenPopulationStateCompositionSystemHelper state,
            int citizenId,
            float now,
            StoreCitizenAction storeCitizen)
        {
            return TryResolveCitizenArrivalState(state, citizenId, now, storeCitizen);
        }

        public static bool TryMarkCitizenDead(
            CitizenStatusTransitionCompositionSystemHelper system,
            CitizenPopulationStateCompositionSystemHelper state,
            int citizenId,
            string reason,
            float now,
            StoreCitizenAction storeCitizen)
        {
            return system != null
                ? system.TryMarkCitizenDead(state, citizenId, reason, now, storeCitizen)
                : TryMarkCitizenDeadState(state, citizenId, reason, now, storeCitizen);
        }

        public bool TryMarkCitizenDead(
            CitizenPopulationStateCompositionSystemHelper state,
            int citizenId,
            string reason,
            float now,
            StoreCitizenAction storeCitizen)
        {
            return TryMarkCitizenDeadState(state, citizenId, reason, now, storeCitizen);
        }

        private static void SetCitizenStatusState(
            ref CitizenRecordComponent citizen,
            CitizenStatus status,
            int targetBuildingId,
            float stateDurationSeconds,
            float now)
        {
            citizen.Status = status;
            citizen.CurrentTargetBuildingId = targetBuildingId != 0 ? targetBuildingId : citizen.HomeBuildingId;
            citizen.StateStartedAt = now;
            citizen.StateEndsAt = stateDurationSeconds > 0f ? now + stateDurationSeconds : 0f;
            citizen.LifeState = status == CitizenStatus.Dead ? CitizenLifeState.Dead : CitizenLifeState.Alive;
        }

        private static bool IsTravelStatusState(CitizenStatus status)
        {
            return status == CitizenStatus.GoingToWork ||
                   status == CitizenStatus.GoingToShop ||
                   status == CitizenStatus.GoingToCityHall ||
                   status == CitizenStatus.GoingForWalk ||
                   status == CitizenStatus.ReturningHome ||
                   status == CitizenStatus.Fleeing ||
                   status == CitizenStatus.RefugeeSeekingShelter ||
                   status == CitizenStatus.RelocatingToNewHouse;
        }

        private static bool ShouldUseTravelStatusState(
            CitizenPopulationStateCompositionSystemHelper state,
            CitizenRecordComponent citizen,
            CitizenStatus desiredStatus,
            int desiredTargetBuildingId)
        {
            if (!state.VisibleCitizensById.ContainsKey(citizen.CitizenId))
                return false;

            CitizenStatus settledStatus = GetSettledStatusState(citizen.Status);
            return settledStatus != desiredStatus || citizen.CurrentTargetBuildingId != desiredTargetBuildingId;
        }

        private static CitizenStatus GetTravelStatusForDesiredStatusState(CitizenStatus desiredStatus)
        {
            return desiredStatus switch
            {
                CitizenStatus.AtWork => CitizenStatus.GoingToWork,
                CitizenStatus.AtShop => CitizenStatus.GoingToShop,
                CitizenStatus.GoingToCityHall => CitizenStatus.GoingToCityHall,
                CitizenStatus.AtHome => CitizenStatus.ReturningHome,
                CitizenStatus.AtRefugeeTent => CitizenStatus.RefugeeSeekingShelter,
                CitizenStatus.GoingForWalk => CitizenStatus.GoingForWalk,
                CitizenStatus.Fleeing => CitizenStatus.Fleeing,
                _ => desiredStatus
            };
        }

        private static CitizenStatus GetSettledStatusState(CitizenStatus status)
        {
            return status switch
            {
                CitizenStatus.GoingToWork => CitizenStatus.AtWork,
                CitizenStatus.GoingToShop => CitizenStatus.AtShop,
                CitizenStatus.ReturningHome => CitizenStatus.AtHome,
                CitizenStatus.Fleeing => CitizenStatus.AtHome,
                CitizenStatus.RefugeeSeekingShelter => CitizenStatus.AtRefugeeTent,
                CitizenStatus.RelocatingToNewHouse => CitizenStatus.AtHome,
                _ => status
            };
        }

        private static bool TrySetCitizenStatusState(
            CitizenPopulationStateCompositionSystemHelper state,
            int citizenId,
            CitizenStatus status,
            int targetBuildingId,
            float stateDurationSeconds,
            float now,
            StoreCitizenAction storeCitizen)
        {
            if (!state.TryGetCitizen(citizenId, out CitizenRecordComponent citizen))
                return false;

            SetCitizenStatusState(ref citizen, status, targetBuildingId, stateDurationSeconds, now);
            storeCitizen(citizen);
            return true;
        }

        private static bool TryResolveCitizenArrivalState(
            CitizenPopulationStateCompositionSystemHelper state,
            int citizenId,
            float now,
            StoreCitizenAction storeCitizen)
        {
            if (!state.TryGetCitizen(citizenId, out CitizenRecordComponent citizen))
                return false;

            CitizenStatus settledStatus = GetSettledStatusState(citizen.Status);
            if (settledStatus == citizen.Status)
                return false;

            SetCitizenStatusState(ref citizen, settledStatus, citizen.CurrentTargetBuildingId, 0f, now);
            storeCitizen(citizen);
            return true;
        }

        private static bool TryMarkCitizenDeadState(
            CitizenPopulationStateCompositionSystemHelper state,
            int citizenId,
            string reason,
            float now,
            StoreCitizenAction storeCitizen)
        {
            _ = reason;
            if (!state.TryGetCitizen(citizenId, out CitizenRecordComponent citizen))
                return false;
            if (citizen.LifeState == CitizenLifeState.Dead)
                return false;

            SetCitizenStatusState(ref citizen, CitizenStatus.Dead, citizen.CurrentTargetBuildingId, 0f, now);
            storeCitizen(citizen);
            return true;
        }
    }
}
