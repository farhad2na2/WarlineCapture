#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using UnityEditor;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishStartingGrantPlacementConstraintTests
    {
        private SkirmishMapLayoutConfig layout;

        public static void RunFocusedValidation()
        {
            int cases = 0;
            void Run(Action<SkirmishStartingGrantPlacementConstraintTests> test)
            {
                var instance = new SkirmishStartingGrantPlacementConstraintTests();
                instance.SetUp();
                try { test(instance); cases++; }
                finally { instance.TearDown(); }
            }
            Run(test => test.ExplicitBindingAllowsRelocationInsideTypedPadAndFiltersOutsideCandidates());
            Run(test => test.MissingExactInstanceBindingFailsClosed());
            Run(test => test.RotatedEffectiveFootprintAndSlantedPadCornerEscapeAreRejected());
            Run(test => test.SameTypeInstancesResolveTheirOwnOrdinalPads());
            Run(test => test.CanceledOwnerAndStaleLaunchIdentityRejectTypedGrant());
            Run(test => test.LegacyOrNonSkirmishRequestKeepsUnboundedPlacementPredicate());
            Run(test => test.LogicalFrameClipsCandidateEvenWhenPadExtendsPastIt());
            Run(test => test.CampaignPlayableBoundsClipGridAndPadValidCandidate());
            Run(test => test.SharedPlacementSearchSkipsCloserOutsideCandidateForFartherLegalOrigin());
            Run(test => test.CheckpointExceptionRequiresExactCorrelatedSnapshotRequest());
            Debug.Log($"[SkirmishStartingGrantPlacementConstraintValidation] result=Passed cases={cases}");
        }

        [SetUp]
        public void SetUp()
        {
            layout = ScriptableObject.CreateInstance<SkirmishMapLayoutConfig>();
            var serialized = new SerializedObject(layout);
            serialized.FindProperty("originX").floatValue = 0;
            serialized.FindProperty("originZ").floatValue = 0;
            serialized.FindProperty("worldWidthMetres").floatValue = 100;
            serialized.FindProperty("worldDepthMetres").floatValue = 100;
            var anchors = serialized.FindProperty("anchors");
            anchors.arraySize = 1;
            var anchor = anchors.GetArrayElementAtIndex(0);
            anchor.FindPropertyRelative("AnchorId").stringValue = "anchor.base.player.0";
            anchor.FindPropertyRelative("RoleId").stringValue = "base.player.0";
            anchor.FindPropertyRelative("NormalizedU").floatValue = .5f;
            anchor.FindPropertyRelative("NormalizedV").floatValue = .5f;
            anchor.FindPropertyRelative("WorldX").floatValue = 50;
            anchor.FindPropertyRelative("WorldZ").floatValue = 50;
            var binding = serialized.FindProperty("worldBinding");
            binding.FindPropertyRelative("OriginX").floatValue = 0;
            // TryProjectToMap centers its across coordinate at v=.5. Pin the
            // authoring frame's midpoint to world Z=50 so typed pad cells and
            // the synthetic test grid share a coordinate frame.
            binding.FindPropertyRelative("OriginZ").floatValue = 50;
            binding.FindPropertyRelative("ForwardX").floatValue = 1;
            binding.FindPropertyRelative("ForwardZ").floatValue = 0;
            binding.FindPropertyRelative("AcrossX").floatValue = 0;
            binding.FindPropertyRelative("AcrossZ").floatValue = 1;
            binding.FindPropertyRelative("ForwardMetres").floatValue = 100;
            binding.FindPropertyRelative("AcrossMetres").floatValue = 100;
            binding.FindPropertyRelative("PlayerAnchorId").stringValue = "player";
            binding.FindPropertyRelative("EnemyAnchorId").stringValue = "enemy";
            var pads = serialized.FindProperty("pads");
            pads.arraySize = 1;
            var pad = pads.GetArrayElementAtIndex(0);
            pad.FindPropertyRelative("PadId").stringValue = "pad.base.player.0";
            pad.FindPropertyRelative("AnchorId").stringValue = "anchor.base.player.0";
            pad.FindPropertyRelative("Kind").enumValueIndex = (int)SkirmishLegalPadKind.BaseBarracks;
            pad.FindPropertyRelative("FactionId").intValue = 1;
            pad.FindPropertyRelative("CenterX").floatValue = 50;
            pad.FindPropertyRelative("CenterZ").floatValue = 50;
            pad.FindPropertyRelative("WidthMetres").floatValue = 20;
            pad.FindPropertyRelative("DepthMetres").floatValue = 20;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            if (layout != null) UnityEngine.Object.DestroyImmediate(layout);
        }

        [Test]
        public void ExplicitBindingAllowsRelocationInsideTypedPadAndFiltersOutsideCandidates()
        {
            var setup = ExplicitSetup();
            layout.BindStructureInstancePads(new[] { new SkirmishStructureInstancePadBinding
            {
                SizeId = SkirmishSizeId.Standard, DifficultyId = SkirmishDifficultyId.Regular,
                FactionId = 1, StructureId = SkirmishStructureIds.Barracks, InstanceOrdinal = 0,
                AnchorId = "anchor.base.player.0", PadId = "pad.base.player.0"
            } });
            Assert.IsTrue(SkirmishStartingGrantPlacementConstraint.TryResolvePad(layout, setup, 0, out var pad));
            var grid = new GridConfig { Width = 100, Height = 100, CellSize = 1 };
            Assert.IsTrue(SkirmishStartingGrantPlacementConstraint.AllowsCandidate(true, layout, pad, grid, PlayableBounds(),
                new RectInt(45, 45, 4, 4)), "Nearest available relocation inside the authored typed pad remains legal.");
            Assert.IsFalse(SkirmishStartingGrantPlacementConstraint.AllowsCandidate(true, layout, pad, grid, PlayableBounds(),
                new RectInt(20, 20, 4, 4)), "A nearer free candidate outside the typed pad must be skipped.");
        }

        [Test]
        public void MissingExactInstanceBindingFailsClosed()
        {
            var setup = ExplicitSetup();
            Assert.IsFalse(SkirmishStartingGrantPlacementConstraint.TryResolvePad(layout, setup, 0, out _));
        }

        [Test]
        public void RotatedEffectiveFootprintAndSlantedPadCornerEscapeAreRejected()
        {
            var serialized = new SerializedObject(layout);
            var pad = serialized.FindProperty("pads").GetArrayElementAtIndex(0);
            pad.FindPropertyRelative("WidthMetres").floatValue = 20;
            pad.FindPropertyRelative("DepthMetres").floatValue = 3.4f;
            var binding = serialized.FindProperty("worldBinding");
            // 30-degree frame centered at world (50,50): use the exact affine
            // inverse origins so both pad and logical-frame projections remain pinned.
            binding.FindPropertyRelative("OriginX").floatValue = 6.6987298f;
            binding.FindPropertyRelative("OriginZ").floatValue = 25;
            binding.FindPropertyRelative("ForwardX").floatValue = .8660254f;
            binding.FindPropertyRelative("ForwardZ").floatValue = .5f;
            binding.FindPropertyRelative("AcrossX").floatValue = -.5f;
            binding.FindPropertyRelative("AcrossZ").floatValue = .8660254f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            layout.BindStructureInstancePads(new[] { Binding(0, "anchor.base.player.0", "pad.base.player.0") });
            var setup = ExplicitSetup();
            Assert.IsTrue(SkirmishStartingGrantPlacementConstraint.TryResolvePad(layout, setup, 0, out var typedPad));
            var grid = new GridConfig { Width = 100, Height = 100, CellSize = 1 };
            Assert.IsTrue(SkirmishStartingGrantPlacementConstraint.ContainsFootprint(layout, typedPad, grid, PlayableBounds(),
                new RectInt(45, 47, 3, 2)), "The 3x2 effective rectangle fits the narrow 30-degree pad.");
            Assert.IsFalse(SkirmishStartingGrantPlacementConstraint.ContainsFootprint(layout, typedPad, grid, PlayableBounds(),
                new RectInt(45, 47, 2, 3)), "Swapping the effective footprint dimensions for rotation no longer fits the narrow pad.");
            Assert.IsFalse(SkirmishStartingGrantPlacementConstraint.ContainsFootprint(layout, typedPad, grid, PlayableBounds(),
                new RectInt(41, 44, 3, 3)), "AABB overlap is insufficient when a corner lies outside the slanted typed pad.");
        }

        [Test]
        public void SameTypeInstancesResolveTheirOwnOrdinalPads()
        {
            var serialized = new SerializedObject(layout);
            var pads = serialized.FindProperty("pads");
            pads.arraySize = 2;
            var second = pads.GetArrayElementAtIndex(1);
            second.FindPropertyRelative("PadId").stringValue = "pad.base.player.1";
            second.FindPropertyRelative("AnchorId").stringValue = "anchor.base.player.1";
            second.FindPropertyRelative("Kind").enumValueIndex = (int)SkirmishLegalPadKind.BaseBarracks;
            second.FindPropertyRelative("FactionId").intValue = 1;
            second.FindPropertyRelative("CenterX").floatValue = 75;
            second.FindPropertyRelative("CenterZ").floatValue = 50;
            second.FindPropertyRelative("WidthMetres").floatValue = 12;
            second.FindPropertyRelative("DepthMetres").floatValue = 12;
            var anchors = serialized.FindProperty("anchors");
            anchors.arraySize = 2;
            var secondAnchor = anchors.GetArrayElementAtIndex(1);
            secondAnchor.FindPropertyRelative("AnchorId").stringValue = "anchor.base.player.1";
            secondAnchor.FindPropertyRelative("RoleId").stringValue = "base.player.1";
            secondAnchor.FindPropertyRelative("NormalizedU").floatValue = .75f;
            secondAnchor.FindPropertyRelative("NormalizedV").floatValue = .5f;
            secondAnchor.FindPropertyRelative("WorldX").floatValue = 75;
            secondAnchor.FindPropertyRelative("WorldZ").floatValue = 50;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            layout.BindStructureInstancePads(new[]
            {
                Binding(0, "anchor.base.player.0", "pad.base.player.0"),
                Binding(1, "anchor.base.player.1", "pad.base.player.1")
            });
            var setup = ExplicitSetup();
            setup.Structures = new[] { setup.Structures[0], setup.Structures[0] };
            setup.Structures[1].SpawnAnchorId = "anchor.base.player.1";
            Assert.IsTrue(SkirmishStartingGrantPlacementConstraint.TryResolvePad(layout, setup, 1, out var pad));
            Assert.AreEqual("pad.base.player.1", pad.PadId);
        }

        [Test]
        public void CanceledOwnerAndStaleLaunchIdentityRejectTypedGrant()
        {
            Assert.IsFalse(SkirmishStartingGrantPlacementConstraint.IsCurrentGrantBinding(false, true, true, true, true));
            Assert.IsFalse(SkirmishStartingGrantPlacementConstraint.IsCurrentGrantBinding(true, true, true, false, true));
            Assert.IsFalse(SkirmishStartingGrantPlacementConstraint.IsCurrentGrantBinding(true, true, true, true, false));
            Assert.IsFalse(SkirmishStartingGrantPlacementConstraint.IsCurrentGrantBinding(true, true, false, true, true),
                "An explicit grant cannot disable typed checks by changing request flags.");
            Assert.IsTrue(SkirmishStartingGrantPlacementConstraint.IsCurrentGrantBinding(true, false, false, false, false));
        }

        [Test]
        public void ExpandedStartingGrantFailsClosedOnMissingSetupButLegacyOwnersRemainSupported()
        {
            using var world = new World(nameof(ExpandedStartingGrantFailsClosedOnMissingSetupButLegacyOwnersRemainSupported));
            var em = world.EntityManager;
            Entity missing = em.CreateEntity(typeof(BuildingStartingGrantOwner), typeof(SkirmishExpandedSessionComponent));
            Assert.IsFalse(BuildingRuntimeProcessingCompositionSystemHelper.TryResolveStartingGrantSetup(
                em, missing, false, out _, out _, out _, out _),
                "An expanded session without its setup record must not fall through as an unbound legacy grant.");

            Entity nullSetup = em.CreateEntity(typeof(BuildingStartingGrantOwner), typeof(SkirmishExpandedSessionComponent));
            em.AddComponentObject(nullSetup, new SkirmishResolvedSetupRecord { Setup = null });
            Assert.IsFalse(BuildingRuntimeProcessingCompositionSystemHelper.TryResolveStartingGrantSetup(
                em, nullSetup, false, out _, out _, out _, out _),
                "A null setup inside a real expanded-session record is invalid provenance.");

            Entity clearedTypedFlag = em.CreateEntity(typeof(BuildingStartingGrantOwner), typeof(SkirmishExpandedSessionComponent));
            var requiringBinding = new SkirmishResolvedSetup
            { CatalogId = "authored-bound", ObjectiveKind = SkirmishObjectiveKind.FrontlineControl };
            em.AddComponentObject(clearedTypedFlag, new SkirmishResolvedSetupRecord { Setup = requiringBinding });
            Assert.IsFalse(BuildingRuntimeProcessingCompositionSystemHelper.TryResolveStartingGrantSetup(
                em, clearedTypedFlag, false, out _, out _, out _, out _),
                "Clearing RequireTypedPad cannot downgrade a setup that requires an explicit binding.");

            Entity legacySkirmish = em.CreateEntity(typeof(BuildingStartingGrantOwner), typeof(SkirmishExpandedSessionComponent));
            em.SetComponentData(legacySkirmish, new SkirmishExpandedSessionComponent { IsLegacy = 1 });
            var legacySetup = new SkirmishResolvedSetup
            { CatalogId = "S004", ContentVersion = 1, ObjectiveKind = SkirmishObjectiveKind.BaseAssault };
            em.AddComponentObject(legacySkirmish, new SkirmishResolvedSetupRecord { Setup = legacySetup });
            Assert.IsTrue(BuildingRuntimeProcessingCompositionSystemHelper.TryResolveStartingGrantSetup(
                em, legacySkirmish, false, out var resolvedLegacy, out _, out var legacyRequiresBinding, out _));
            Assert.AreSame(legacySetup, resolvedLegacy);
            Assert.IsFalse(legacyRequiresBinding);

            Entity campaign = em.CreateEntity(typeof(BuildingStartingGrantOwner), typeof(CampaignMissionRuntimeComponent));
            Assert.IsTrue(BuildingRuntimeProcessingCompositionSystemHelper.TryResolveStartingGrantSetup(
                em, campaign, false, out _, out _, out _, out _),
                "Campaign mission runtime roots retain their established untyped grant path.");
        }

        [Test]
        public void LogicalFrameClipsCandidateEvenWhenPadExtendsPastIt()
        {
            var serialized = new SerializedObject(layout);
            serialized.FindProperty("worldWidthMetres").floatValue = 50;
            serialized.FindProperty("worldBinding").FindPropertyRelative("ForwardMetres").floatValue = 50;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var grid = new GridConfig { Width = 100, Height = 100, CellSize = 1 };
            Assert.IsFalse(SkirmishStartingGrantPlacementConstraint.ContainsFootprint(layout,
                new SkirmishLegalPadConfig { CenterX = 50, CenterZ = 50, WidthMetres = 20, DepthMetres = 20 },
                grid, PlayableBounds(), new RectInt(48, 45, 4, 4)), "The candidate footprint escapes the authored logical layout frame.");
        }

        [Test]
        public void CampaignPlayableBoundsClipGridAndPadValidCandidate()
        {
            var grid = new GridConfig { Width = 100, Height = 100, CellSize = 1 };
            var pad = new SkirmishLegalPadConfig { CenterX = 50, CenterZ = 50, WidthMetres = 20, DepthMetres = 20 };
            var candidate = new RectInt(48, 45, 4, 4);
            Assert.IsTrue(SkirmishStartingGrantPlacementConstraint.ContainsFootprint(layout, pad, grid,
                PlayableBounds(), candidate), "Control candidate must fit the grid, pad, frame, and original source bounds.");
            Assert.IsFalse(SkirmishStartingGrantPlacementConstraint.ContainsFootprint(layout, pad, grid,
                PlayableBounds(maxX: 50), candidate),
                "A grid-valid, typed-pad-valid footprint outside the launch Campaign playable max must be rejected.");
            Assert.IsFalse(SkirmishStartingGrantPlacementConstraint.ContainsFootprint(layout, pad, grid,
                default, candidate), "Invalid/unpinned playable bounds must fail closed.");
        }

        [Test]
        public void SharedPlacementSearchSkipsCloserOutsideCandidateForFartherLegalOrigin()
        {
            var setup = ExplicitSetup();
            layout.BindStructureInstancePads(new[] { Binding(0, "anchor.base.player.0", "pad.base.player.0") });
            Assert.IsTrue(SkirmishStartingGrantPlacementConstraint.TryResolvePad(layout, setup, 0, out var pad));
            var grid = new GridConfig { Width = 100, Height = 100, CellSize = 1 };
            var observed = new List<Vector2Int>();
            int validationCalls = 0;
            var context = new BuildingRuntimeSpawnCompositionSystemHelper.Context(null, null, null, null, default,
                null, (_, _) => new Vector2Int(4, 4),
                (_, origin, actual, _) => new RectInt(origin, new Vector2Int(4, 4)),
                (_, origin, footprint, _, actual, _, _) =>
                {
                    validationCalls++;
                    return SkirmishStartingGrantPlacementConstraint.AllowsCandidate(true, layout, pad, actual, PlayableBounds(),
                        new RectInt(origin, footprint));
                }, null, null, null, null, null);
            var search = new BuildingRuntimeSpawnCompositionSystemHelper();
            var preferred = new Vector2Int(35, 50);
            // Independently enumerate legal origins and rank them by the search owner's
            // Chebyshev ring, then dy/dx order. This verifies the expected result without
            // calling the production search implementation to compute its own expectation.
            var nearestLegal = Enumerable.Range(preferred.y - 6, 13)
                .SelectMany(y => Enumerable.Range(preferred.x - 6, 13).Select(x => new Vector2Int(x, y)))
                .Where(origin => SkirmishStartingGrantPlacementConstraint.AllowsCandidate(true, layout, pad, grid,
                    PlayableBounds(), new RectInt(origin, new Vector2Int(4, 4))))
                .OrderBy(origin => Mathf.Max(Mathf.Abs(origin.x - preferred.x), Mathf.Abs(origin.y - preferred.y)))
                .ThenBy(origin => origin.y - preferred.y)
                .ThenBy(origin => origin.x - preferred.x)
                .First();
            Assert.AreEqual(new Vector2Int(40, 45), nearestLegal,
                "Independent bounded enumeration confirms the first legal radius-five candidate.");
            Assert.IsTrue(search.TryFindValidInitialBuildingOrigin(context, new BuildingDefinition(),
                preferred, false, grid, default, default, out var actual,
                candidateObserved: (origin, _, _) => observed.Add(origin)));
            Assert.Greater(validationCalls, 1, "The nearest requested origin must be evaluated and rejected first.");
            Assert.AreEqual(nearestLegal, actual, "The shared search must return the independently ranked nearest legal origin.");
            Assert.IsTrue(observed.Contains(preferred));
            Assert.IsTrue(SkirmishStartingGrantPlacementConstraint.ContainsFootprint(layout, pad, grid, PlayableBounds(),
                new RectInt(actual, new Vector2Int(4, 4))));
        }

        [Test]
        public void CheckpointExceptionRequiresExactCorrelatedSnapshotRequest()
        {
            using var world = new World(nameof(CheckpointExceptionRequiresExactCorrelatedSnapshotRequest));
            var em = world.EntityManager;
            Entity session = em.CreateEntity(typeof(BuildingStartingGrantOwner),
                typeof(SkirmishNativeCheckpointStructureRequestGuardTag));
            var identity = new SkirmishNativeCheckpointStructureRequestGuard.RequestIdentity(
                23, 0, "Building_Barrack", 2, new int2(10, 12), new int2(4, 5));
            em.AddComponentObject(session,
                SkirmishNativeCheckpointStructureRequestGuard.CreateValidated(session, new[] { identity }));
            var request = new BuildingRuntimeSpawnRequest
            {
                RequestId = 23, EntryIndex = 0, PlanEntity = session,
                BuildingId = new FixedString128Bytes("Building_Barrack"), FactionId = 2, HasOwnerFaction = 1,
                AuthoredStartingGrant = 1, RequirePreferredOrigin = 1, PreferredOrigin = new int2(10, 12)
            };
            Assert.IsTrue(BuildingRuntimeProcessingCompositionSystemHelper.IsExactCheckpointReconstructionRequest(
                em, request, new int2(4, 5)));
            Assert.IsFalse(BuildingRuntimeProcessingCompositionSystemHelper.IsExactCheckpointReconstructionRequest(
                em, request, new int2(2, 2)), "Changed configured footprint invalidates the request receipt.");
            request.AuthoredStartingGrant = 0;
            Assert.IsFalse(BuildingRuntimeProcessingCompositionSystemHelper.IsExactCheckpointReconstructionRequest(em, request, new int2(4, 5)),
                "A reconstruction marker cannot exempt a request that is not an authored starting grant.");
            request.AuthoredStartingGrant = 1;
            request.RequestId++;
            Assert.IsFalse(BuildingRuntimeProcessingCompositionSystemHelper.IsExactCheckpointReconstructionRequest(em, request, new int2(4, 5)),
                "A marker alone cannot exempt a request with stale correlation.");
            request.RequestId--;
            request.RequirePreferredOrigin = 0;
            Assert.IsFalse(BuildingRuntimeProcessingCompositionSystemHelper.IsExactCheckpointReconstructionRequest(em, request, new int2(4, 5)),
                "Checkpoint restoration must remain exact-origin.");
            request.RequirePreferredOrigin = 1;
            request.RequireTypedPad = 1;
            Assert.IsFalse(BuildingRuntimeProcessingCompositionSystemHelper.IsExactCheckpointReconstructionRequest(em, request, new int2(4, 5)),
                "A reconstruction row cannot opt out of normal typed-pad validation with an inconsistent flag.");
            request.RequireTypedPad = 0;
            request.RotateVertical = 1;
            Assert.IsFalse(BuildingRuntimeProcessingCompositionSystemHelper.IsExactCheckpointReconstructionRequest(
                em, request, new int2(4, 5)), "Checkpoint receipts cannot authorize a rotated footprint.");
            request.RotateVertical = 0;
            request.AllowExistingWallOverlap = 1;
            Assert.IsFalse(BuildingRuntimeProcessingCompositionSystemHelper.IsExactCheckpointReconstructionRequest(
                em, request, new int2(4, 5)), "Checkpoint receipts cannot authorize wall overlap.");
            request.AllowExistingWallOverlap = 0;
            em.RemoveComponent<BuildingStartingGrantOwner>(session);
            Assert.IsFalse(BuildingRuntimeProcessingCompositionSystemHelper.IsExactCheckpointReconstructionRequest(
                em, request, new int2(4, 5)), "A canceled grant owner invalidates its otherwise exact receipt.");
            em.AddComponent<BuildingStartingGrantOwner>(session);
            em.RemoveComponent<SkirmishNativeCheckpointStructureRequestGuardTag>(session);
            Assert.IsFalse(BuildingRuntimeProcessingCompositionSystemHelper.IsExactCheckpointReconstructionRequest(
                em, request, new int2(4, 5)), "A missing reconstruction owner tag fails closed.");
        }

        [Test]
        public void LegacyOrNonSkirmishRequestKeepsUnboundedPlacementPredicate()
        {
            Assert.IsFalse(SkirmishStartingGrantPlacementConstraint.RequiresTypedConstraint(true, false));
            var grid = new GridConfig { Width = 100, Height = 100, CellSize = 1 };
            Assert.IsTrue(SkirmishStartingGrantPlacementConstraint.AllowsCandidate(false, null, default, grid, default,
                new RectInt(2, 2, 4, 4)));
        }

        private static SkirmishResolvedSetup ExplicitSetup()
        {
            var setup = new SkirmishResolvedSetup
            {
                SizeId = SkirmishSizeId.Standard,
                DifficultyId = SkirmishDifficultyId.Regular,
                Structures = new[] { new SkirmishResolvedStructureEntry
                {
                    StructureId = SkirmishStructureIds.Barracks, FactionId = 1,
                    SpawnAnchorId = "anchor.base.player.0", SpawnWorldX = 50, SpawnWorldZ = 50
                } }
            };
            // The missing-binding case intentionally omits the layout's authored binding row.
            return setup;
        }

        private static SkirmishStructureInstancePadBinding Binding(int ordinal, string anchor, string pad) =>
            new SkirmishStructureInstancePadBinding
            {
                SizeId = SkirmishSizeId.Standard, DifficultyId = SkirmishDifficultyId.Regular,
                FactionId = 1, StructureId = SkirmishStructureIds.Barracks, InstanceOrdinal = ordinal,
                AnchorId = anchor, PadId = pad
            };

        private static OperationMapBoundsConfig PlayableBounds(float minX = 0, float maxX = 100,
            float minZ = 0, float maxZ = 100) => new OperationMapBoundsConfig(
                new Vector3(-100, -10, -100), new Vector3(200, 10, 200),
                new Vector3(minX, -5, minZ), new Vector3(maxX, 5, maxZ),
                new Vector3(-100, -10, -100), new Vector3(200, 10, 200));
    }
}
#endif
