#if UNITY_EDITOR || WARLINE_VALIDATION_PLAYER
using Unity.Entities;
using UnityEngine;
namespace Game.Runtime
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial class SkirmishValidationClockSampler : SystemBase
    {
        protected override void OnUpdate()
            => SkirmishValidationClock.Sample(UnityEngine.Time.frameCount, UnityEngine.Time.timeAsDouble, UnityEngine.Time.deltaTime,
                UnityEngine.Time.unscaledDeltaTime, UnityEngine.Time.timeScale == 0f);
    }
}
#endif
