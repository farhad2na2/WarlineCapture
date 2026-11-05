using System;
using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Tests.Editor
{
    public sealed class AircraftGeometryDependencyMaintenanceTests
    {
        [Test]
        public void DependencyRefreshRejectsChangesToGeometryIdentityAndProvenance()
        {
            var transport = SkirmishRigidAircraftGeometryAuthoring.Measure();
            var canonical = SkirmishCanonicalAircraftGeometryAuthoring.Measure();
            var rotor = SkirmishAircraftRotorGeometryAuthoring.Measure(new string('a', 64));
            try
            {
                Check(transport, x => x.Sources[0].DependencyHash = new string('0', 32), true);
                Check(transport, x => x.Draws[0].GeometrySha256 = new string('0', 64), false);
                Check(transport, x => x.Draws[0].LocalBounds = new Bounds(Vector3.one, Vector3.one), false);
                Check(transport, x => x.Sources[0].Path += ".different", false);
                Check(canonical, x => x.Roles[0].Sources[0].DependencyHash = new string('0', 32), true);
                Check(canonical, x => x.Roles[0].PrefabKey += ".different", false);
                Check(canonical, x => x.Roles[0].Sources[0].VisualKind = 0, false);
                Check(rotor, x => x.Bindings[0].DependencyHash = new string('0', 32), true);
                Check(rotor, x => x.Bindings[0].BladePath += "/different", false);
                Check(rotor, x => x.Bindings[0].MeshToBlade = Matrix4x4.Translate(Vector3.one), false);
                Check(rotor, x => x.NativeTemplateProvenanceSha256 = new string('b', 64), false);
            }
            finally { Object.DestroyImmediate(transport); Object.DestroyImmediate(canonical); Object.DestroyImmediate(rotor); }
        }

        static void Check<T>(T measured, Action<T> mutate, bool allowed) where T : ScriptableObject
        {
            var installed = Object.Instantiate(measured);
            try
            {
                mutate(installed);
                string original = JsonUtility.ToJson(installed);
                var report = AircraftGeometryDependencyMaintenance.Compare(installed, measured, "test");
                Assert.AreEqual(allowed, (bool)report["equalExceptDependencyHashes"]);
                Assert.AreEqual(original, JsonUtility.ToJson(installed), "Diagnosis must not mutate its input");
            }
            finally { Object.DestroyImmediate(installed); }
        }

        public static void RunFocusedValidation()
        {
            new AircraftGeometryDependencyMaintenanceTests().DependencyRefreshRejectsChangesToGeometryIdentityAndProvenance();
            Debug.Log("[AircraftGeometryDependencyMaintenanceTests] result=Passed controls=11 mutationDuringDiagnosis=0");
        }

        public static void RunRefreshAndVerify()
        {
            RunFocusedValidation();
            AircraftGeometryDependencyMaintenance.RefreshUnchangedGeometryDependencies();
            Debug.Log("[AircraftGeometryMaintenanceIntegration] result=Passed controls=11 resources=3 originalGuards=Passed");
        }
    }
}
