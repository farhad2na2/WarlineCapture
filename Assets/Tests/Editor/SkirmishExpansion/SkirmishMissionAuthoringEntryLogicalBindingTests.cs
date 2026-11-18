#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Game.Configs;
using Game.Editor;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor
{
    // AuthorFromDraft for FC/BT/CE (lane18 S007: "compile.Standard.Recruit MissingReference:missionLogicalBinding").
    // The normal compiler requires a definition-referenced logical binding for every non-Base-Assault objective, while
    // the entry clears the binding before compiling the variants it builds that binding from. These controls use the
    // disposable source-bound S031 fixture re-kinded to Frontline Control with synthetic areas: compiler/binding
    // identity only, no persistent assets, no native measurement, source fit or readiness.
    public sealed class SkirmishMissionAuthoringEntryLogicalBindingTests
    {
        private static readonly SkirmishDifficultyId[] Difficulties = { SkirmishDifficultyId.Recruit, SkirmishDifficultyId.Regular };

        private static void WithUnboundFrontline(System.Action<SkirmishScenarioDefinitionConfig, List<UnityEngine.Object>,
            List<SkirmishSetupMatrixRow>, SkirmishContentManifest> assertion)
        {
            SkirmishGenericMissionDispatchTests.WithSourceBoundUnqueuedDefinition("S031", (definition, owned) =>
            {
                var profiles = SkirmishExpansionCatalogConfig.Load().ToAuthoredSet();
                var objective = UnityEngine.Object.Instantiate(profiles.ObjectiveBa); owned.Add(objective);
                var serialized = new SerializedObject(objective);
                serialized.FindProperty("kind").intValue = (int)SkirmishObjectiveKind.FrontlineControl;
                serialized.FindProperty("objectiveId").stringValue = "FC";
                serialized.FindProperty("standardDeadlineSeconds").intValue = 1200;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                definition.Configure("S031", objective, profiles.ArmyGround, profiles.StartField,
                    profiles.Economy, profiles.Upgrades, profiles.Intel, profiles.Roles, definition.MapLayout,
                    featureIds: new[] { "ground", "intel", "transport", "advanced_ground", "objective_fc" });
                definition.BindMissionLogicalBinding(null);   // exactly what AuthorFromDraft does before the variant pass
                Assert.IsTrue(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out string error), error);
                assertion(definition, owned, matrix, new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds });
            });
        }

        // Three small rectangles inside the actual playable bounds (same synthetic geometry as the native fixture).
        private static SkirmishMissionAuthoringEntry.AreaDraft SyntheticFrontlineAreas(SkirmishMissionLogicalAuthoringInputs inputs)
        {
            var bounds = inputs.ActualAreaBinding;
            float width = bounds.SourceMaxX - bounds.SourceMinX, depth = bounds.SourceMaxZ - bounds.SourceMinZ;
            var areas = new SkirmishObjectiveArea[3];
            for (int i = 0; i < 3; i++)
            {
                float x = bounds.SourceMinX + width * (.2f + i * .2f), z = bounds.SourceMinZ + depth * .4f;
                var role = (SkirmishObjectiveAreaRole)((int)SkirmishObjectiveAreaRole.FrontlineA + i);
                areas[i] = new SkirmishObjectiveArea
                {
                    StableAreaId = "area.skirmish.s031." + role.ToString().ToLowerInvariant(), Role = role,
                    Shape = SkirmishObjectiveAreaShape.AxisAlignedRectangle,
                    Points = new[] { new SkirmishObjectiveAreaPoint(x, z), new SkirmishObjectiveAreaPoint(x + width * .05f, z + depth * .05f) }
                };
            }
            return new SkirmishMissionAuthoringEntry.AreaDraft
            {
                Kind = SkirmishObjectiveKind.FrontlineControl, Areas = areas,
                Artifact = "synthetic-unit-fixture-not-native-measurement", ArtifactSha256 = new string('d', 64)
            };
        }

        [Test]
        public void FrontlineUnboundPassAcceptsOnlyTheMissingBindingAndAuthorsABindingThatCompiles()
        {
            WithUnboundFrontline((definition, owned, matrix, manifest) =>
            {
                // The lane failure: the normal compiler refuses FC without a binding, and for that reason only.
                Assert.IsFalse(SkirmishSetupCompiler.TryCompile(definition, SkirmishDifficultyId.Recruit, SkirmishSizeId.Standard, 1,
                    manifest, matrix, out _, out var refused));
                Assert.IsTrue(refused.Count > 0 && refused.All(reason => reason.Field == SkirmishMissionAuthoringEntry.MissionLogicalBindingField),
                    string.Join(";", refused.Select(reason => reason.Field + ":" + reason.Detail)));

                var rows = new List<SkirmishMissionAuthoringEntry.Compiled>();
                foreach (var difficulty in Difficulties)
                {
                    Assert.IsTrue(SkirmishMissionAuthoringEntry.TryCompileUnbound(definition, difficulty, SkirmishSizeId.Standard, manifest, matrix,
                        out var row, out string failure), failure);
                    Assert.IsNull(row.Setup, "A rejected setup must never leave the compiler.");
                    Assert.IsNotNull(row.Inputs);
                    Assert.AreEqual(SkirmishObjectiveKind.FrontlineControl, row.Inputs.ObjectiveKind);
                    Assert.IsNotEmpty(row.Forces);
                    Assert.IsNotNull(row.Structures);
                    rows.Add(row);
                }

                var binding = SkirmishMissionAuthoringEntry.BuildBinding(definition, rows, SyntheticFrontlineAreas(rows[1].Inputs), out var view);
                owned.Add(view); owned.Add(binding);
                Assert.AreEqual(SkirmishObjectiveKind.FrontlineControl, binding.ObjectiveKind);
                Assert.AreEqual(Difficulties.Length, binding.Variants.Length);
                Assert.IsTrue(binding.Variants.All(variant => variant.Areas != null && ReferenceEquals(variant.View, view)));
                Assert.IsTrue(view.AuthoredNonBaseAssault);

                definition.BindMissionLogicalBinding(binding);
                Assert.DoesNotThrow(() => SkirmishMissionAuthoringEntry.VerifyBoundVariants(definition, rows, manifest, matrix));
                foreach (var row in rows)
                {
                    Assert.IsTrue(SkirmishSetupCompiler.TryCompile(definition, row.Difficulty, row.Size, 1, manifest, matrix, out var setup, out var reasons),
                        string.Join(";", reasons.Select(reason => reason.Field + ":" + reason.Detail)));
                    Assert.IsFalse(string.IsNullOrEmpty(setup.MissionLogicalBindingFingerprint));
                }
            });
        }

        [Test]
        public void UnboundPassStillRefusesOtherRejectionsAndABoundDefinition()
        {
            WithUnboundFrontline((definition, owned, matrix, manifest) =>
            {
                // A rejection that is not only the missing binding stays a failure, named by its first reason.
                Assert.IsFalse(SkirmishMissionAuthoringEntry.TryCompileUnbound(definition, SkirmishDifficultyId.Recruit, SkirmishSizeId.None,
                    manifest, matrix, out var row, out string failure));
                Assert.IsNull(row);
                StringAssert.StartsWith("compile.None.Recruit: ", failure);

                // The unbound pass never runs against a definition that still carries a binding.
                var stale = ScriptableObject.CreateInstance<SkirmishMissionLogicalBindingConfig>(); owned.Add(stale);
                definition.BindMissionLogicalBinding(stale);
                Assert.IsFalse(SkirmishMissionAuthoringEntry.TryCompileUnbound(definition, SkirmishDifficultyId.Recruit, SkirmishSizeId.Standard,
                    manifest, matrix, out row, out failure));
                Assert.IsNull(row);
                StringAssert.Contains("logical binding cleared first", failure);
            });
        }

        public static void RunFocusedValidation()
        {
            var tests = new SkirmishMissionAuthoringEntryLogicalBindingTests();
            tests.FrontlineUnboundPassAcceptsOnlyTheMissingBindingAndAuthorsABindingThatCompiles();
            tests.UnboundPassStillRefusesOtherRejectionsAndABoundDefinition();
            Debug.Log("[SkirmishMissionAuthoringEntryLogicalBindingTests] result=Passed controls=2 nativeMeasurement=0 readiness=0");
        }
    }
}
#endif
