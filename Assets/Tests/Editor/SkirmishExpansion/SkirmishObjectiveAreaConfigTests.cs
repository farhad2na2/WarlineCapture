using System;
using System.Linq;
using Game.Configs;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishObjectiveAreaConfigTests
    {
        private static string H(char c) => new string(c, 64);
        private static SkirmishObjectiveAreaBinding Binding() => new SkirmishObjectiveAreaBinding
        {
            DefinitionId = "test.definition", SourceConfigId = "existing.campaign.source", SourceAssetGuid = new string('a', 32),
            SourceContentHash = "authored.source.v4", SourceSemanticFingerprint = H('b'), LayoutId = "measured.logical.layout",
            LayoutBindingFingerprint = H('c'), SourceMinX = -100, SourceMinZ = -200, SourceMaxX = 500, SourceMaxZ = 400
        };
        private static SkirmishObjectiveArea Rectangle(SkirmishObjectiveAreaRole role, string id) => new SkirmishObjectiveArea
        { StableAreaId = id, Role = role, Shape = SkirmishObjectiveAreaShape.AxisAlignedRectangle,
            Points = new[] { new SkirmishObjectiveAreaPoint(10, 20), new SkirmishObjectiveAreaPoint(30, 40) } };
        private static SkirmishObjectiveAreaConfig New(SkirmishObjectiveKind kind = SkirmishObjectiveKind.ConvoyEscort)
        {
            SkirmishObjectiveAreaRole[] roles;
            switch (kind)
            {
                case SkirmishObjectiveKind.FrontlineControl: roles = new[] { SkirmishObjectiveAreaRole.FrontlineA, SkirmishObjectiveAreaRole.FrontlineB, SkirmishObjectiveAreaRole.FrontlineC }; break;
                case SkirmishObjectiveKind.Breakthrough: roles = new[] { SkirmishObjectiveAreaRole.BreakthroughCorridorA, SkirmishObjectiveAreaRole.BreakthroughCorridorB, SkirmishObjectiveAreaRole.BreakthroughExit }; break;
                default: roles = new[] { SkirmishObjectiveAreaRole.ConvoyDestination }; break;
            }
            var config = new SkirmishObjectiveAreaConfig { AreaSetId = "test.logical.areas", ObjectiveKind = kind,
                Binding = Binding(), Areas = roles.Select((role,index) => { var area = Rectangle(role, "test.area." + role);
                    for(int i=0;i<area.Points.Length;i++) area.Points[i].WorldX += index*40; return area; }).ToArray() };
            Measure(config); return config;
        }
        // Synthetic test evidence reference only, never a native source-fit claim.
        private static void Measure(SkirmishObjectiveAreaConfig config) => config.Measurement = new SkirmishObjectiveAreaEvidence
        { ArtifactId = "unit-test-only.synthetic.measurement", ArtifactSha256 = H('d'), GeometryFingerprint = SkirmishObjectiveAreaCompiler.ComputeGeometryFingerprint(config) };
        private static SkirmishCompiledObjectiveAreas Compile(SkirmishObjectiveAreaConfig config)
        { Assert.IsTrue(SkirmishObjectiveAreaCompiler.TryCompile(config, Binding(), config.ObjectiveKind, out var result, out var error), error); return result; }
        private static void Reject(SkirmishObjectiveAreaConfig config)
        { Assert.IsFalse(SkirmishObjectiveAreaCompiler.TryCompile(config, Binding(), config.ObjectiveKind, out var result, out var error)); Assert.IsNull(result); Assert.IsNotEmpty(error); }

        [Test] public void AllThreeObjectiveKindsRequireExactTypedRoleSets()
        {
            foreach (var kind in new[] { SkirmishObjectiveKind.FrontlineControl, SkirmishObjectiveKind.Breakthrough, SkirmishObjectiveKind.ConvoyEscort })
            {
                var config = New(kind); Assert.AreEqual(kind, Compile(config).ObjectiveKind);
                config.Areas = config.Areas.Take(config.Areas.Length - 1).ToArray(); Measure(config); Reject(config);
            }
            var unsupported = New(); unsupported.ObjectiveKind = SkirmishObjectiveKind.BaseAssault; Measure(unsupported); Reject(unsupported);
        }
        [Test] public void RectangleUsesExplicitCornersAndInclusiveBoundaryWithoutRadius()
        {
            var compiled = Compile(New()); var role = SkirmishObjectiveAreaRole.ConvoyDestination;
            Assert.IsTrue(compiled.Contains(role, 20, 30)); Assert.IsTrue(compiled.Contains(role, 10, 20)); Assert.IsTrue(compiled.Contains(role, 30, 40));
            Assert.IsFalse(compiled.Contains(role, 9.999f, 30)); Assert.IsFalse(compiled.Contains(role, 20, 40.001f));
            Assert.IsFalse(compiled.Contains(SkirmishObjectiveAreaRole.FrontlineA, 20, 30)); Assert.IsFalse(compiled.Contains(role, float.NaN, 30));
        }
        [Test] public void ConcavePolygonBothWindingsPreserveNotchAndBoundary()
        {
            var config = New(); config.Areas[0].Shape = SkirmishObjectiveAreaShape.Polygon;
            config.Areas[0].Points = new[] { new SkirmishObjectiveAreaPoint(0, 0), new SkirmishObjectiveAreaPoint(4, 0), new SkirmishObjectiveAreaPoint(4, 1),
                new SkirmishObjectiveAreaPoint(1, 1), new SkirmishObjectiveAreaPoint(1, 4), new SkirmishObjectiveAreaPoint(0, 4) };
            foreach (bool reverse in new[] { false, true })
            {
                if (reverse) Array.Reverse(config.Areas[0].Points); Measure(config); var compiled = Compile(config);
                Assert.IsTrue(compiled.Contains(SkirmishObjectiveAreaRole.ConvoyDestination, .5f, 3));
                Assert.IsTrue(compiled.Contains(SkirmishObjectiveAreaRole.ConvoyDestination, 1, 2));
                Assert.IsFalse(compiled.Contains(SkirmishObjectiveAreaRole.ConvoyDestination, 2, 2));
            }
        }
        [Test] public void SelfIntersectionTouchRepeatedVertexAndBacktrackingAreRejected()
        {
            foreach (var points in new[] {
                new[] { P(0,0), P(4,4), P(0,4), P(4,0) },
                new[] { P(0,0), P(4,0), P(4,4), P(2,0), P(0,4) },
                new[] { P(0,0), P(4,0), P(4,4), P(0,0) },
                new[] { P(0,0), P(4,0), P(2,0), P(2,2), P(0,2) } })
            { var config = New(); config.Areas[0].Shape = SkirmishObjectiveAreaShape.Polygon; config.Areas[0].Points = points; Measure(config); Reject(config); }
        }
        private static SkirmishObjectiveAreaPoint P(float x, float z) => new SkirmishObjectiveAreaPoint(x, z);
        [Test] public void MissingNonfiniteDegenerateAndReversedGeometryFailClosed()
        {
            foreach (var points in new[] { null, new[] { P(1,1) }, new[] { P(1,1), P(1,2) }, new[] { P(2,2), P(1,1) }, new[] { P(float.NaN,0), P(2,2) }, new[] { P(0,0), P(float.PositiveInfinity,2) } })
            { var config = New(); config.Areas[0].Points = points; Measure(config); Reject(config); }
            var polygon = New(); polygon.Areas[0].Shape = SkirmishObjectiveAreaShape.Polygon; polygon.Areas[0].Points = new[] { P(0,0), P(1,1), P(2,2) }; Measure(polygon); Reject(polygon);
            var kind = New(); kind.Areas[0].Shape = SkirmishObjectiveAreaShape.None; Measure(kind); Reject(kind);
        }
        [Test] public void BoundsUseSelectedSourceAndRejectAnyOutsideVertex()
        {
            var config = New(); config.Areas[0].Points = new[] { P(-100,-200), P(500,400) }; Measure(config); Compile(config);
            config.Areas[0].Points[1] = P(500.001f,400); Measure(config); Reject(config);
        }
        [Test] public void DuplicateRoleIdentityAndWrongObjectiveRoleAreRejected()
        {
            var config = New(SkirmishObjectiveKind.FrontlineControl); config.Areas[1].Role = config.Areas[0].Role; Measure(config); Reject(config);
            config = New(SkirmishObjectiveKind.FrontlineControl); config.Areas[1].StableAreaId = config.Areas[0].StableAreaId; Measure(config); Reject(config);
            config = New(); config.Areas[0].Role = SkirmishObjectiveAreaRole.BreakthroughExit; Measure(config); Reject(config);
            config = New(SkirmishObjectiveKind.Breakthrough); config.Areas[1].Points = new[] { P(10,20), P(30,40) }; Measure(config); Reject(config);
            config = New(SkirmishObjectiveKind.Breakthrough); config.Areas[1].Shape = SkirmishObjectiveAreaShape.Polygon;
            config.Areas[1].Points = new[] { P(30,40), P(30,20), P(20,20), P(10,20), P(10,40) }; Measure(config); Reject(config);
        }
        [Test] public void MissingOrStaleMeasurementNeverCompiles()
        {
            var config = New(); config.Measurement = default; Reject(config);
            config = New(); config.Areas[0].Points[0].WorldX++; Reject(config);
            config = New(); config.Measurement.ArtifactSha256 = "not-a-sha"; Reject(config);
        }
        [Test] public void EverySourceAndLayoutPinMustMatchExpectedBinding()
        {
            Action<SkirmishObjectiveAreaConfig>[] mutations = { c => c.Binding.DefinitionId += ".other", c => c.Binding.SourceConfigId += ".other",
                c => c.Binding.SourceAssetGuid = new string('e',32), c => c.Binding.SourceContentHash += ".other", c => c.Binding.SourceSemanticFingerprint = H('e'),
                c => c.Binding.LayoutId += ".other", c => c.Binding.LayoutBindingFingerprint = H('e'), c => c.Binding.SourceMaxX++ };
            foreach (var mutate in mutations) { var config = New(); mutate(config); Measure(config); Reject(config); }
        }
        [Test] public void ExactRegistryLookupRejectsMissingAndAmbiguousEntries()
        {
            var config = New(); Assert.IsTrue(SkirmishObjectiveAreaCompiler.TryResolve(new[] { config }, Binding(), config.ObjectiveKind, out _, out _));
            Assert.IsFalse(SkirmishObjectiveAreaCompiler.TryResolve(new SkirmishObjectiveAreaConfig[0], Binding(), config.ObjectiveKind, out _, out _));
            Assert.IsFalse(SkirmishObjectiveAreaCompiler.TryResolve(new[] { config, New() }, Binding(), config.ObjectiveKind, out _, out _));
            config.Binding.DefinitionId += ".other"; Assert.IsFalse(SkirmishObjectiveAreaCompiler.TryResolve(new[] { config }, Binding(), config.ObjectiveKind, out _, out _));
        }
        [Test] public void FingerprintIsIndependentOfAreaArrayOrderAndObjectInstance()
        {
            var first = New(SkirmishObjectiveKind.FrontlineControl); var second = New(SkirmishObjectiveKind.FrontlineControl);
            Array.Reverse(second.Areas); Measure(second);
            Assert.AreEqual(Compile(first).GeometryFingerprint, Compile(second).GeometryFingerprint);
            Assert.AreEqual(Compile(first).LogicalFingerprint, Compile(second).LogicalFingerprint);
        }
        [Test] public void GeometrySourceAndEvidenceChangesAffectStableLogicalFingerprint()
        {
            var config = New(); var original = Compile(config);
            config.Areas[0].Points[0].WorldX++; Measure(config); var changed = Compile(config);
            Assert.AreNotEqual(original.GeometryFingerprint, changed.GeometryFingerprint); Assert.AreNotEqual(original.LogicalFingerprint, changed.LogicalFingerprint);
            var beforeEvidence = changed.LogicalFingerprint; config.Measurement.ArtifactSha256 = H('e');
            Assert.AreEqual(changed.GeometryFingerprint, Compile(config).GeometryFingerprint); Assert.AreNotEqual(beforeEvidence, Compile(config).LogicalFingerprint);
            var beforeSource = SkirmishObjectiveAreaCompiler.ComputeGeometryFingerprint(config); config.Binding.SourceContentHash += ".changed";
            Assert.AreNotEqual(beforeSource, SkirmishObjectiveAreaCompiler.ComputeGeometryFingerprint(config));
        }
        [Test] public void CompiledSnapshotAndReadbackCannotMutateOccupancy()
        {
            var config = New(); var compiled = Compile(config); config.Areas[0].Points[0] = P(25,35);
            Assert.IsTrue(compiled.Contains(SkirmishObjectiveAreaRole.ConvoyDestination, 10,20));
            Assert.IsTrue(compiled.TryRead(SkirmishObjectiveAreaRole.ConvoyDestination, out var read)); read.Points[0] = P(25,35);
            Assert.IsTrue(compiled.Contains(SkirmishObjectiveAreaRole.ConvoyDestination, 10,20));
        }
        [Test] public void NativeFitDeclarationIsSeparateBoundAndCannotGrantLogicalReadiness()
        {
            var config = New(); var compiled = Compile(config);
            var record = new SkirmishObjectiveAreaQualificationRecord();
            Assert.IsFalse(SkirmishObjectiveAreaCompiler.HasBoundNativeFitDeclaration(record, compiled));
            record = new SkirmishObjectiveAreaQualificationRecord { LogicalFingerprint = compiled.LogicalFingerprint,
                NativeSourceFit = new SkirmishObjectiveAreaEvidence { ArtifactId = "unit-test-only.synthetic.native-reference", ArtifactSha256 = H('e'), GeometryFingerprint = compiled.GeometryFingerprint } };
            Assert.IsTrue(SkirmishObjectiveAreaCompiler.HasBoundNativeFitDeclaration(record, compiled));
            Assert.AreEqual(compiled.LogicalFingerprint, Compile(config).LogicalFingerprint);
            record.NativeSourceFit.GeometryFingerprint = H('f'); Assert.IsFalse(SkirmishObjectiveAreaCompiler.HasBoundNativeFitDeclaration(record, compiled));
            record.NativeSourceFit.GeometryFingerprint = compiled.GeometryFingerprint; record.LogicalFingerprint = H('f');
            Assert.IsFalse(SkirmishObjectiveAreaCompiler.HasBoundNativeFitDeclaration(record, compiled));
        }
        [Test] public void InvalidSchemaBindingAndObjectiveMismatchReturnNoPartialSnapshot()
        {
            var config = New(); config.SchemaVersion++; Reject(config);
            config = New(); config.Binding.SourceAssetGuid = ""; Measure(config); Reject(config);
            config = New(); config.Binding.SourceMaxX = float.NaN; Measure(config); Reject(config);
            config = New(); Assert.IsFalse(SkirmishObjectiveAreaCompiler.TryCompile(config, Binding(), SkirmishObjectiveKind.Breakthrough, out var result, out _)); Assert.IsNull(result);
        }
        public static void RunFocusedValidation()
        {
            var s = new SkirmishObjectiveAreaConfigTests(); Action[] tests = {
                s.AllThreeObjectiveKindsRequireExactTypedRoleSets, s.RectangleUsesExplicitCornersAndInclusiveBoundaryWithoutRadius,
                s.ConcavePolygonBothWindingsPreserveNotchAndBoundary, s.SelfIntersectionTouchRepeatedVertexAndBacktrackingAreRejected,
                s.MissingNonfiniteDegenerateAndReversedGeometryFailClosed, s.BoundsUseSelectedSourceAndRejectAnyOutsideVertex,
                s.DuplicateRoleIdentityAndWrongObjectiveRoleAreRejected, s.MissingOrStaleMeasurementNeverCompiles,
                s.EverySourceAndLayoutPinMustMatchExpectedBinding, s.ExactRegistryLookupRejectsMissingAndAmbiguousEntries,
                s.FingerprintIsIndependentOfAreaArrayOrderAndObjectInstance, s.GeometrySourceAndEvidenceChangesAffectStableLogicalFingerprint,
                s.CompiledSnapshotAndReadbackCannotMutateOccupancy, s.NativeFitDeclarationIsSeparateBoundAndCannotGrantLogicalReadiness,
                s.InvalidSchemaBindingAndObjectiveMismatchReturnNoPartialSnapshot };
            try { foreach (var test in tests) test(); Debug.Log("[SkirmishObjectiveAreaConfigTests] result=Passed tests=15 nativeSourceFitCertified=0"); }
            catch (Exception error) { Debug.LogError("[SkirmishObjectiveAreaConfigTests] result=Failed\n" + error); throw; }
        }
    }
}
