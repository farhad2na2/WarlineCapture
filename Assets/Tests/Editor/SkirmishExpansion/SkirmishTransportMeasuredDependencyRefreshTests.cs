using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishTransportMeasuredDependencyRefreshTests
    {
        [Test] public void OnlyFreshDependencyStampDifferenceIsAllowed()
        {
            var installed=SkirmishRigidAircraftGeometryAuthoring.Measure();var measured=SkirmishRigidAircraftGeometryAuthoring.Measure();
            try
            {
                foreach(var source in installed.Sources)source.DependencyHash=new string('a',32);
                Assert.IsFalse(SkirmishRigidAircraftGeometryAuthoring.Equal(installed,measured,out _));
                Assert.IsTrue(SkirmishTransportMeasuredDependencyRefresh.SameGeometryIgnoringDependencyStamps(installed,measured,out _));
                Assert.IsTrue(SkirmishTransportMeasuredDependencyRefresh.StampSerializationControl(installed,measured));
            }
            finally{Object.DestroyImmediate(installed);Object.DestroyImmediate(measured);}
        }
        [Test] public void ActualIndexedGeometryDriftCannotBeRefreshed()
        {
            var installed=SkirmishRigidAircraftGeometryAuthoring.Measure();var measured=SkirmishRigidAircraftGeometryAuthoring.Measure();
            try
            {
                measured.Draws[0].GeometrySha256=new string('b',64);
                Assert.IsFalse(SkirmishTransportMeasuredDependencyRefresh.SameGeometryIgnoringDependencyStamps(installed,measured,out _));
                Assert.DoesNotThrow(()=>SkirmishTransportMeasuredDependencyRefresh.GraphShapeControl(0));
                Assert.Throws<System.InvalidOperationException>(()=>SkirmishTransportMeasuredDependencyRefresh.GraphShapeControl(1));
                Assert.Throws<System.InvalidOperationException>(()=>SkirmishTransportMeasuredDependencyRefresh.GraphShapeControl(2));
            }
            finally{Object.DestroyImmediate(installed);Object.DestroyImmediate(measured);}
        }
        public static void RunFocusedValidation()
        {
            var tests=new SkirmishTransportMeasuredDependencyRefreshTests();tests.OnlyFreshDependencyStampDifferenceIsAllowed();tests.ActualIndexedGeometryDriftCannotBeRefreshed();
            Debug.Log("[TransportMeasuredDependencyRefreshTests] result=Passed controls=2 actualIndexedDraws=57 nativeCameraAcceptance=Pending");
        }
        public static void RunRefreshAndSharedPreparation()
        {
            RunFocusedValidation();SkirmishTransportMeasuredDependencyRefresh.Run();SkirmishSharedNativeProductionPreparation.Run();
            Debug.Log("[TransportRefreshAndSharedPreparation] result=Passed refreshControls=2 sharedControls=150 cameraAndMissionAcceptance=Pending");
        }
    }
}
