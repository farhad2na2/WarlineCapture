using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public partial struct CampaignMissionPatrolOrderSystem
    {
        private static readonly FixedString64Bytes EstablishBaseMissionId =
            "saga.ch01.m02.establish_base";
        internal const int EstablishBaseOpeningHoldMilliseconds = 750;
        internal const int EstablishBaseOpeningFocusArrivalMilliseconds = 3250;
        internal const int EstablishBaseOpeningFocusHoldMilliseconds = 4250;
        internal const int EstablishBaseOpeningCompleteMilliseconds = 6750;
        internal const float EstablishBaseOpeningSmoothTimeSeconds = 2.25f;
        internal const byte EstablishBaseOpeningFocusAction = 1;
        internal const byte EstablishBaseOpeningReturnAction = 2;

        internal static bool ShouldUseEstablishBaseOpening(in FixedString64Bytes missionId) =>
            missionId.Equals(EstablishBaseMissionId);

        internal static bool CanAdvanceEstablishBaseOpening(MissionPhaseKind phase) =>
            phase is MissionPhaseKind.FindSquad or
                MissionPhaseKind.MoveToCover or
                MissionPhaseKind.ConfirmThreat or
                MissionPhaseKind.Engage or
                MissionPhaseKind.SecureCorridor;

        internal static bool ShouldEmitOpeningPanicAudio(in FixedString64Bytes missionId) =>
            missionId.Equals(FirstContactMissionId);

        internal static byte EvaluateEstablishBaseOpeningStage(
            byte stage,
            int elapsedMilliseconds,
            byte focusRequested,
            out byte cameraAction)
        {
            cameraAction = 0;
            if (stage == 0 && elapsedMilliseconds >= EstablishBaseOpeningHoldMilliseconds &&
                focusRequested == 0)
            {
                cameraAction = EstablishBaseOpeningFocusAction;
                return 1;
            }

            if (stage == 1 && elapsedMilliseconds >= EstablishBaseOpeningFocusArrivalMilliseconds)
                return 2;

            if (stage == 2 && elapsedMilliseconds >= EstablishBaseOpeningFocusHoldMilliseconds &&
                focusRequested == 0)
            {
                cameraAction = EstablishBaseOpeningReturnAction;
                return 3;
            }

            if (stage == 3 && elapsedMilliseconds >= EstablishBaseOpeningCompleteMilliseconds)
                return 6;

            return stage;
        }

        internal static RuntimeCameraFocusRequestComponent CreateEstablishBaseOpeningCameraRequest(
            byte cameraAction,
            in CampaignMissionOpeningPresentationComponent opening)
        {
            if (cameraAction != EstablishBaseOpeningFocusAction &&
                cameraAction != EstablishBaseOpeningReturnAction)
                return default;

            bool returnToRts = cameraAction == EstablishBaseOpeningReturnAction;
            return new RuntimeCameraFocusRequestComponent
            {
                Requested = 1,
                Smooth = 1,
                UseTacticalRevealZoom = returnToRts ? (byte)4 : (byte)3,
                SmoothTimeSeconds = EstablishBaseOpeningSmoothTimeSeconds,
                World = returnToRts ? opening.FriendlyFocus : opening.EstablishingFocus
            };
        }

        private static void AdvanceEstablishBaseOpening(
            EntityManager entityManager,
            Entity focusEntity,
            in RuntimeCameraFocusRequestComponent focus,
            ref CampaignMissionOpeningPresentationComponent opening)
        {
            byte nextStage = EvaluateEstablishBaseOpeningStage(
                opening.Stage,
                opening.ElapsedMilliseconds,
                focus.Requested,
                out byte cameraAction);
            if (cameraAction != 0)
                entityManager.SetComponentData(
                    focusEntity,
                    CreateEstablishBaseOpeningCameraRequest(cameraAction, in opening));

            opening.Stage = nextStage;
        }
    }
}
