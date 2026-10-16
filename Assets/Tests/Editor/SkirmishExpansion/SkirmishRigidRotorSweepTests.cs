using System;
using Game.Rendering.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Editor
{
    public static class SkirmishRigidRotorSweepTests
    {
        public static void RunFocusedValidation()
        {
            FullRotationContainsEveryVertexUnderShearedMirroredAncestors();
            OffCenterPivotAndAxialTranslationArePreserved();
            RejectsUnknownAxisMalformedMatricesAndNonfiniteGeometry();
            CylinderSupportHandlesArbitraryAffineRowDirections();
            Debug.Log("[SkirmishRigidRotorSweepTests] result=Passed controls=4 measuredRoleMetadata=Pending cameraAccepted=0");
        }

        [Test]
        public static void FullRotationContainsEveryVertexUnderShearedMirroredAncestors()
        {
            // Synthetic math fixtures only; not measured aircraft metadata.
            var vertices=new[]{new Vector3(-11,.3f,-.2f),new Vector3(13,-.7f,.1f),new Vector3(2,3,4),new Vector3(-4,-2,5)};
            var meshToBlade=Matrix4x4.TRS(new Vector3(3,-2,4),Quaternion.Euler(23,71,17),new Vector3(.7f,2,1.3f));
            var bladeToWorld=Matrix4x4.TRS(new Vector3(913,55,611),Quaternion.Euler(41,12,67),new Vector3(-3,.2f,4))*Matrix4x4.Rotate(Quaternion.Euler(17,34,21));
            for(byte axis=0;axis<3;axis++)
            {
                Assert.IsTrue(SkirmishRigidRotorSweep.TryMeasure(vertices,meshToBlade,axis,out var extent));
                Assert.IsTrue(SkirmishRigidRotorSweep.TryWorldEnvelope(extent,bladeToWorld,out var envelope));
                Vector3 unit=axis==0?Vector3.right:axis==1?Vector3.up:Vector3.forward;
                for(int angle=0;angle<=720;angle++)foreach(var vertex in vertices)
                {
                    Vector3 rotated=Quaternion.AngleAxis(angle*.5f,unit)*meshToBlade.MultiplyPoint3x4(vertex);
                    Assert.IsTrue(envelope.Contains(bladeToWorld.MultiplyPoint3x4(rotated)),"axis="+axis+" angle="+angle);
                }
            }
        }
        [Test]
        public static void OffCenterPivotAndAxialTranslationArePreserved()
        {
            Assert.IsTrue(SkirmishRigidRotorSweep.TryMeasure(new[]{new Vector3(3,5,4)},Matrix4x4.identity,1,out var extent));
            Assert.AreEqual(5,extent.MinimumAxial);Assert.AreEqual(5,extent.MaximumAxial);Assert.GreaterOrEqual(extent.MaximumRadius,5);
            Assert.IsTrue(SkirmishRigidRotorSweep.TryWorldEnvelope(extent,Matrix4x4.Translate(new Vector3(10,20,30)),out var bounds));
            foreach(var point in new[]{new Vector3(15,25,30),new Vector3(5,25,30),new Vector3(10,25,35),new Vector3(10,25,25)})Assert.IsTrue(bounds.Contains(point));
            Assert.Less(bounds.size.y,.001f); // never centered at the actor origin or a guessed body box.
        }
        [Test]
        public static void RejectsUnknownAxisMalformedMatricesAndNonfiniteGeometry()
        {
            Assert.IsFalse(SkirmishRigidRotorSweep.TryMeasure(Array.Empty<Vector3>(),Matrix4x4.identity,0,out _));
            Assert.IsFalse(SkirmishRigidRotorSweep.TryMeasure(new[]{Vector3.zero},Matrix4x4.identity,3,out _));
            Assert.IsFalse(SkirmishRigidRotorSweep.TryMeasure(new[]{new Vector3(float.NaN,0,0)},Matrix4x4.identity,0,out _));
            var perspective=Matrix4x4.identity;perspective[3,1]=.1f;
            Assert.IsFalse(SkirmishRigidRotorSweep.TryMeasure(new[]{Vector3.zero},perspective,0,out _));
            Assert.IsFalse(SkirmishRigidRotorSweep.TryWorldEnvelope(new SkirmishRigidRotorExtent(0,2,1,5),Matrix4x4.identity,out _));
            Assert.IsFalse(SkirmishRigidRotorSweep.TryWorldEnvelope(new SkirmishRigidRotorExtent(0,0,1,-1),Matrix4x4.identity,out _));
        }
        [Test]
        public static void CylinderSupportHandlesArbitraryAffineRowDirections()
        {
            var extent=new SkirmishRigidRotorExtent(2,-2,7,3);
            var affine=Matrix4x4.identity;affine.SetRow(0,new Vector4(3,4,2,11));affine.SetRow(1,new Vector4(-2,0,-5,13));affine.SetRow(2,new Vector4(0,7,0,-17));
            Assert.IsTrue(SkirmishRigidRotorSweep.TryWorldEnvelope(extent,affine,out var bounds));
            Assert.LessOrEqual(bounds.min.x,11-4-15);Assert.GreaterOrEqual(bounds.max.x,11+14+15);
            Assert.LessOrEqual(bounds.min.y,13-35-6);Assert.GreaterOrEqual(bounds.max.y,13+10+6);
            Assert.LessOrEqual(bounds.min.z,-17-21);Assert.GreaterOrEqual(bounds.max.z,-17+21);
        }
    }
}
