using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public partial struct CampaignMissionPatrolOrderSystem
    {
        private static readonly FixedString64Bytes EstablishBaseMissionId =
            "saga.ch01.m02.establish_base";
        internal const int EstablishBaseOpeningHoldMilliseconds = 750;
        internal const int EstablishBaseOpeningCompleteMilliseconds = 5000;
        internal const float EstablishBaseOpeningSmoothTimeSeconds = 2f;

        internal static bool ShouldUseEstablishBaseOpening(in FixedString64Bytes missionId) =>
            missionId.Equals(EstablishBaseMissionId);

        internal static bool ShouldEmitOpeningPanicAudio(in FixedString64Bytes missionId) =>
            missionId.Equals(FirstContactMissionId);

        internal static byte EvaluateEstablishBaseOpeningStage(
            byte stage,
            int elapsedMilliseconds,
            byte focusRequested,
            out byte queueSweep)
        {
            queueSweep = 0;
            if (stage == 0 && elapsedMilliseconds >= EstablishBaseOpeningHoldMilliseconds &&
                focusRequested == 0)
            {
                queueSweep = 1;
                return 1;
            }

            if (stage == 1 && elapsedMilliseconds >= EstablishBaseOpeningCompleteMilliseconds &&
                focusRequested == 0)
                return 6;

            return stage;
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
                out byte queueSweep);
            if (queueSweep != 0)
            {
                entityManager.SetComponentData(focusEntity, new RuntimeCameraFocusRequestComponent
                {
                    Requested = 1,
                    Smooth = 1,
                    UseTacticalRevealZoom = 4,
                    SmoothTimeSeconds = EstablishBaseOpeningSmoothTimeSeconds,
                    World = opening.HostileFocus
                });
            }

            opening.Stage = nextStage;
        }
    }
}
