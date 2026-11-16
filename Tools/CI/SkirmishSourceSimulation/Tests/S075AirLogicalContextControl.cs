using System;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Simulation;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Collections;

// Source-only unit control. Definition/setup/manifest/matrix/catalog are supplied
// by the actual compiled S075 source preflight. The session identity and public
// observation below are explicitly synthetic fixtures; this creates no World and
// makes no runtime, gameplay, source-fit, or readiness claim.
internal static class S075AirLogicalContextControl
{
    const int PlannedScenarioIndex = -1;
    const string SyntheticSessionId = "synthetic-s075-context-control";

    static void Need(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    static bool TryCreate(
        SkirmishScenarioDefinitionConfig definition,
        SkirmishResolvedSetup setup,
        SkirmishContentManifest manifest,
        IReadOnlyList<SkirmishSetupMatrixRow> matrix,
        SkirmishBattleCatalogConfig catalog,
        in SkirmishExpandedSessionComponent session,
        int playableScenarioIndex,
        out GroundMissionContext context,
        out string error) => GroundMissionContextFactory.TryCreateLogicalPublicData(
            definition, setup, manifest, matrix, catalog, session, playableScenarioIndex,
            SkirmishPresetConfig.SourcePresetIndex(PlannedScenarioIndex), out context, out error);

    internal static JObject Run(
        SkirmishScenarioDefinitionConfig actualCompiledDefinition,
        SkirmishResolvedSetup actualCompiledSetup,
        SkirmishContentManifest actualManifest,
        IReadOnlyList<SkirmishSetupMatrixRow> actualMatrix,
        SkirmishBattleCatalogConfig actualCanonicalCatalog)
    {
        Need(actualCompiledDefinition != null && actualCompiledSetup != null && actualManifest != null &&
             actualMatrix != null && actualCanonicalCatalog != null,
            "Actual compiled S075 authoring inputs and canonical catalog required");
        Need(actualCompiledSetup.CatalogId == "S075" && actualCompiledSetup.DefinitionId == "skirmish.s075" &&
             actualCompiledDefinition.CatalogId == "S075" && actualCompiledDefinition.DefinitionId == "skirmish.s075" &&
             actualCompiledSetup.ObjectiveKind == SkirmishObjectiveKind.BaseAssault &&
             actualCompiledSetup.ArmyProfileId == SkirmishArmyProfileId.AirMobile &&
             actualCompiledSetup.StartPackageId == SkirmishStartPackageId.FieldBase,
            "Actual compiled S075 AirMobile/Field/BaseAssault setup required");

        // Only session identity scaffolding is synthetic. Every identity value is
        // copied from the actual compiled setup; no setup or source fact is forged.
        var syntheticSession = new SkirmishExpandedSessionComponent
        {
            SessionId = new FixedString64Bytes(SyntheticSessionId),
            CatalogId = new FixedString64Bytes(actualCompiledSetup.CatalogId),
            DefinitionId = new FixedString64Bytes(actualCompiledSetup.DefinitionId),
            ContentVersion = actualCompiledSetup.ContentVersion,
            SetupHash = actualCompiledSetup.SetupHash,
            Seed = actualCompiledSetup.Seed,
            SizeId = actualCompiledSetup.SizeId,
            DifficultyId = actualCompiledSetup.DifficultyId,
            ObjectiveKind = actualCompiledSetup.ObjectiveKind,
            IsLegacy = 0
        };

        Need(TryCreate(actualCompiledDefinition, actualCompiledSetup, actualManifest, actualMatrix,
                actualCanonicalCatalog, syntheticSession, PlannedScenarioIndex,
                out var context, out string error),
            "S075 source context rejected: " + error);
        Need(context.CatalogId == "S075" && context.DefinitionId == "skirmish.s075" &&
             context.PlayableScenarioIndex == PlannedScenarioIndex &&
             context.ObservationScope == AriaS002Scope.LogicalPublicData &&
             GroundMissionContextFactory.MatchesLogicalSession(context, actualCompiledSetup, syntheticSession,
                 PlannedScenarioIndex),
            "Factory context did not retain exact compiled S075 logical identity");

        // Synthetic public projection fixture, limited to the fields consumed by
        // RequireCompatibleObservation. It represents only the actual S075 setup's
        // profile and session shape; no hidden contacts, controls, or gameplay facts.
        var syntheticObservation = new AriaS002Observation
        {
            Scope = AriaS002Scope.LogicalPublicData,
            SourceScenarioIndex = PlannedScenarioIndex,
            ExpandedSession = true,
            ExpandedBaseAssault = true,
            AirProfile = true,
            CombinedArmsField = false
        };
        context.RequireCompatibleObservation(syntheticObservation);

        var changedSeedSession = syntheticSession;
        changedSeedSession.Seed = unchecked(changedSeedSession.Seed + 1);
        Need(!TryCreate(actualCompiledDefinition, actualCompiledSetup, actualManifest, actualMatrix,
                actualCanonicalCatalog, changedSeedSession, PlannedScenarioIndex, out _, out _),
            "Changed synthetic session seed was accepted");
        Need(!TryCreate(actualCompiledDefinition, actualCompiledSetup, actualManifest, actualMatrix,
                actualCanonicalCatalog, syntheticSession, 0, out _, out _),
            "Playable S075 scenario index was accepted for the Planned/-1 canonical row");

        var originalArmy = actualCompiledSetup.ArmyProfileId;
        try
        {
            actualCompiledSetup.ArmyProfileId = SkirmishArmyProfileId.GroundManeuver;
            Need(!TryCreate(actualCompiledDefinition, actualCompiledSetup, actualManifest, actualMatrix,
                    actualCanonicalCatalog, syntheticSession, PlannedScenarioIndex, out _, out _),
                "Setup with altered army profile was accepted");
        }
        finally { actualCompiledSetup.ArmyProfileId = originalArmy; }

        var originalStart = actualCompiledSetup.StartPackageId;
        var originalReadiness = actualCompiledSetup.Readiness;
        try
        {
            actualCompiledSetup.StartPackageId = SkirmishStartPackageId.EstablishedBase;
            actualCompiledSetup.Readiness = SkirmishReadinessStage.Established;
            Need(!GroundMissionContextFactory.MatchesLogicalSession(context, actualCompiledSetup,
                    syntheticSession, PlannedScenarioIndex),
                "Changed supported start/readiness matched the original air context");
        }
        finally
        {
            actualCompiledSetup.StartPackageId = originalStart;
            actualCompiledSetup.Readiness = originalReadiness;
        }

        var originalBindingFingerprint = actualCompiledSetup.MissionLogicalBindingFingerprint;
        var originalDefinitionBinding = actualCompiledDefinition.MissionLogicalBinding;
        try
        {
            actualCompiledSetup.MissionLogicalBindingFingerprint = string.Empty;
            Need(!TryCreate(actualCompiledDefinition, actualCompiledSetup, actualManifest, actualMatrix,
                    actualCanonicalCatalog, syntheticSession, PlannedScenarioIndex, out _, out _),
                "Setup with missing logical binding fingerprint was accepted");
            actualCompiledSetup.MissionLogicalBindingFingerprint = originalBindingFingerprint;
            actualCompiledDefinition.BindMissionLogicalBinding(null);
            Need(!TryCreate(actualCompiledDefinition, actualCompiledSetup, actualManifest, actualMatrix,
                    actualCanonicalCatalog, syntheticSession, PlannedScenarioIndex, out _, out _),
                "Definition with missing logical binding was accepted");
        }
        finally
        {
            actualCompiledSetup.MissionLogicalBindingFingerprint = originalBindingFingerprint;
            actualCompiledDefinition.BindMissionLogicalBinding(originalDefinitionBinding);
        }

        bool falseAirRejected = false;
        try { context.RequireCompatibleObservation(new AriaS002Observation
            { Scope = AriaS002Scope.LogicalPublicData, SourceScenarioIndex = PlannedScenarioIndex,
              ExpandedSession = true, ExpandedBaseAssault = true, AirProfile = false, CombinedArmsField = false }); }
        catch (NotSupportedException) { falseAirRejected = true; }
        Need(falseAirRejected, "Observation with AirProfile=false was accepted");

        bool combinedFieldRejected = false;
        try { context.RequireCompatibleObservation(new AriaS002Observation
            { Scope = AriaS002Scope.LogicalPublicData, SourceScenarioIndex = PlannedScenarioIndex,
              ExpandedSession = true, ExpandedBaseAssault = true, AirProfile = true, CombinedArmsField = true }); }
        catch (NotSupportedException) { combinedFieldRejected = true; }
        Need(combinedFieldRejected, "Observation with CombinedArmsField=true was accepted");

        // Public construction remains data-only. This forged variant preserves
        // every accepted public identity/source field exactly, but cannot carry
        // the internal factory validation marker.
        var forgedPublicContext = new GroundMissionContext(
            context.CatalogId, context.DefinitionId, context.ContentVersion, context.SetupHash,
            context.SessionId, context.PlayableScenarioIndex, context.ObjectiveKind, context.ArmyProfileId,
            context.StartPackageId, context.Readiness, context.ApproachKind, context.SourceApproachIdentity,
            context.CanonicalMapId, context.SourcePresetLoadIndex, context.SourcePresetIndex,
            context.SourcePresetResourceName, context.ObservationScope);
        Need(forgedPublicContext.CatalogId == context.CatalogId &&
             forgedPublicContext.DefinitionId == context.DefinitionId &&
             forgedPublicContext.ContentVersion == context.ContentVersion &&
             forgedPublicContext.SetupHash == context.SetupHash &&
             forgedPublicContext.SessionId == context.SessionId &&
             forgedPublicContext.PlayableScenarioIndex == context.PlayableScenarioIndex &&
             forgedPublicContext.SourceApproachIdentity == context.SourceApproachIdentity &&
             forgedPublicContext.CanonicalMapId == context.CanonicalMapId &&
             forgedPublicContext.SourcePresetLoadIndex == context.SourcePresetLoadIndex &&
             forgedPublicContext.SourcePresetIndex == context.SourcePresetIndex &&
             forgedPublicContext.SourcePresetResourceName == context.SourcePresetResourceName &&
             forgedPublicContext.ObservationScope == context.ObservationScope,
            "Forged context must preserve every accepted public identity field");
        bool forgedContextRejected = false;
        try { forgedPublicContext.RequireCompatibleObservation(syntheticObservation); }
        catch (NotSupportedException) { forgedContextRejected = true; }
        Need(forgedContextRejected, "Forged public context accepted the S075 observation");

        return new JObject
        {
            ["kind"] = "S075AirLogicalContextSourceOnlyUnitControl",
            ["actual_compiled_inputs"] = true,
            ["synthetic_session_fixture"] = true,
            ["synthetic_observation_fixture"] = true,
            ["context_factory_accepted"] = true,
            ["observation_compatible"] = true,
            ["changed_seed_rejected"] = true,
            ["wrong_scenario_index_rejected"] = true,
            ["altered_army_rejected"] = true,
            ["changed_start_readiness_rejected"] = true,
            ["missing_setup_logical_binding_rejected"] = true,
            ["missing_definition_logical_binding_rejected"] = true,
            ["false_air_profile_rejected"] = true,
            ["combined_arms_field_rejected"] = true,
            ["forged_public_context_rejected"] = true,
            ["world_created"] = false,
            ["gameplay_input_sent"] = false,
            ["planner_invoked"] = false,
            ["source_fit_certified"] = false,
            ["mission_ready"] = false
        };
    }

    internal static JObject RunGround(
        SkirmishScenarioDefinitionConfig actualCompiledDefinition,
        SkirmishResolvedSetup actualCompiledSetup,
        SkirmishContentManifest actualManifest,
        IReadOnlyList<SkirmishSetupMatrixRow> actualMatrix,
        SkirmishBattleCatalogConfig actualCanonicalCatalog)
    {
        Need(actualCompiledDefinition != null && actualCompiledSetup != null && actualManifest != null &&
             actualMatrix != null && actualCanonicalCatalog != null,
            "Actual compiled S074 authoring inputs and canonical catalog required");
        Need(actualCompiledSetup.CatalogId == "S074" && actualCompiledSetup.DefinitionId == "skirmish.s074" &&
             actualCompiledDefinition.CatalogId == "S074" && actualCompiledDefinition.DefinitionId == "skirmish.s074" &&
             actualCompiledSetup.ObjectiveKind == SkirmishObjectiveKind.BaseAssault &&
             actualCompiledSetup.ArmyProfileId == SkirmishArmyProfileId.GroundManeuver,
            "Actual compiled S074 GroundManeuver/BaseAssault setup required");

        var syntheticSession = new SkirmishExpandedSessionComponent
        {
            SessionId = new FixedString64Bytes("synthetic-s074-context-control"),
            CatalogId = new FixedString64Bytes(actualCompiledSetup.CatalogId),
            DefinitionId = new FixedString64Bytes(actualCompiledSetup.DefinitionId),
            ContentVersion = actualCompiledSetup.ContentVersion,
            SetupHash = actualCompiledSetup.SetupHash,
            Seed = actualCompiledSetup.Seed,
            SizeId = actualCompiledSetup.SizeId,
            DifficultyId = actualCompiledSetup.DifficultyId,
            ObjectiveKind = actualCompiledSetup.ObjectiveKind,
            IsLegacy = 0
        };
        Need(TryCreate(actualCompiledDefinition, actualCompiledSetup, actualManifest, actualMatrix,
                actualCanonicalCatalog, syntheticSession, PlannedScenarioIndex,
                out var context, out string error),
            "S074 source context rejected: " + error);
        Need(context.CatalogId == "S074" && context.PlayableScenarioIndex == PlannedScenarioIndex &&
             context.ObservationScope == AriaS002Scope.LogicalPublicData,
            "Factory context did not retain exact compiled S074 logical identity");

        var syntheticGroundObservation = new AriaS002Observation
        {
            Scope = AriaS002Scope.LogicalPublicData,
            SourceScenarioIndex = PlannedScenarioIndex,
            ExpandedSession = true,
            ExpandedBaseAssault = true,
            AirProfile = false,
            CombinedArmsField = false
        };
        context.RequireCompatibleObservation(syntheticGroundObservation);
        bool airObservationRejected = false;
        try
        {
            context.RequireCompatibleObservation(new AriaS002Observation
            {
                Scope = AriaS002Scope.LogicalPublicData,
                SourceScenarioIndex = PlannedScenarioIndex,
                ExpandedSession = true,
                ExpandedBaseAssault = true,
                AirProfile = true,
                CombinedArmsField = false
            });
        }
        catch (NotSupportedException) { airObservationRejected = true; }
        Need(airObservationRejected, "S074 ground context accepted AirProfile=true");

        return new JObject
        {
            ["kind"] = "S074GroundLogicalContextSourceOnlyUnitControl",
            ["actual_compiled_inputs"] = true,
            ["synthetic_session_fixture"] = true,
            ["synthetic_ground_observation_fixture"] = true,
            ["context_factory_accepted"] = true,
            ["ground_observation_compatible"] = true,
            ["air_observation_rejected"] = true,
            ["world_created"] = false,
            ["gameplay_input_sent"] = false,
            ["planner_invoked"] = false,
            ["source_fit_certified"] = false,
            ["mission_ready"] = false
        };
    }
}
