#if UNITY_EDITOR
using Game.Runtime;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    public sealed class SkirmishNativeObjectivePartialFailureCheckpointTests
    {
        [Test] public void ActualPartialDepartureFailureCannotCaptureNativeObjectivePayload()
        {
            SkirmishNativeObjectiveTickTests.WithPartialDepartureFailure((em,session)=>
            {
                int before=em.UniversalQuery.CalculateEntityCount();
                Assert.IsFalse(SkirmishNativeObjectiveCheckpoint.TryCapture(em,session,out var payload,out var error));
                Assert.IsNull(payload);Assert.IsNotEmpty(error);
                Assert.AreEqual(before,em.UniversalQuery.CalculateEntityCount());
            });
        }
        public static void RunFocusedValidation()
        {
            new SkirmishNativeObjectivePartialFailureCheckpointTests().ActualPartialDepartureFailureCannotCaptureNativeObjectivePayload();
            UnityEngine.Debug.Log("[SkirmishNativeObjectivePartialFailureCheckpointTests] result=Passed tests=1 nativeMission=0");
        }
    }
}
#endif
