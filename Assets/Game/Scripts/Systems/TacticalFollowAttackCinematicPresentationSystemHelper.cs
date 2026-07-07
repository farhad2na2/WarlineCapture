using Unity.Mathematics;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    public static class TacticalFollowAttackCinematicPresentationSystemHelper
    {
        public static void ApplyTimeScale(
            ref TacticalFollowAttackCinematicStateComponent cinematic,
            float elapsedSeconds,
            bool isPlaying)
        {
            if (!isPlaying)
                return;

            if (cinematic.TimeScaleApplied == 0)
            {
                cinematic.SavedTimeScale = Time.timeScale;
                cinematic.TimeScaleApplied = 1;
            }

            if (cinematic.SavedTimeScale <= 0f)
            {
                Time.timeScale = 0f;
                return;
            }

            Time.timeScale = math.max(
                0.01f,
                cinematic.SavedTimeScale *
                TacticalFollowAttackCinematicHelper.EvaluateTimeScale(elapsedSeconds));
        }

        public static void RestoreTimeScale(
            ref TacticalFollowAttackCinematicStateComponent cinematic,
            bool isPlaying)
        {
            if (!isPlaying ||
                cinematic.TimeScaleApplied == 0)
            {
                return;
            }

            Time.timeScale = math.max(0f, cinematic.SavedTimeScale);
            cinematic.TimeScaleApplied = 0;
        }
    }
}
