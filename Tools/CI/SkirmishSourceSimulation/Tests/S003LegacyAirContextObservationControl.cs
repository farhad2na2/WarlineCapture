using System;
using Game.Simulation;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;

// Explicitly synthetic compatibility unit control for the original coded S003
// AirMobile/Field/BaseAssault context. It creates no source world and makes no
// readiness, mission, or gameplay claim.
internal static class S003LegacyAirContextObservationControl
{
    static void Need(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    static bool Rejects(in GroundMissionContext context, in AriaS002Observation observation)
    {
        try
        {
            context.RequireCompatibleObservation(observation);
            return false;
        }
        catch (NotSupportedException) { return true; }
    }

    internal static JObject Run()
    {
        var syntheticS003Context = new GroundMissionContext(
            "S003", "skirmish.s003", 2, 1u, "synthetic-s003-legacy-context", 5,
            SkirmishObjectiveKind.BaseAssault, SkirmishArmyProfileId.AirMobile,
            SkirmishStartPackageId.FieldBase, SkirmishReadinessStage.Field,
            AriaGroundApproachKind.Ordinary, "synthetic-coded-s003-source");
        var syntheticAirObservation = new AriaS002Observation
        {
            Scope = AriaS002Scope.LogicalPublicData,
            SourceScenarioIndex = 5,
            ExpandedSession = true,
            ExpandedBaseAssault = true,
            AirProfile = true,
            CombinedArmsField = false
        };

        syntheticS003Context.RequireCompatibleObservation(syntheticAirObservation);

        var wrongIndex = syntheticAirObservation;
        wrongIndex.SourceScenarioIndex = 4;
        Need(Rejects(syntheticS003Context, wrongIndex),
            "Coded S003 context accepted a mismatched source scenario index");

        var falseAirProfile = syntheticAirObservation;
        falseAirProfile.AirProfile = false;
        Need(Rejects(syntheticS003Context, falseAirProfile),
            "Coded S003 context accepted AirProfile=false");

        var combinedArmsField = syntheticAirObservation;
        combinedArmsField.CombinedArmsField = true;
        Need(Rejects(syntheticS003Context, combinedArmsField),
            "Coded S003 context accepted CombinedArmsField=true");

        var changedSharedContext = new GroundMissionContext(
            "S003", "skirmish.s003", 2, 1u, "synthetic-s003-legacy-context", 5,
            SkirmishObjectiveKind.BaseAssault, SkirmishArmyProfileId.GroundManeuver,
            SkirmishStartPackageId.FieldBase, SkirmishReadinessStage.Field,
            AriaGroundApproachKind.Ordinary, "synthetic-coded-s003-source");
        Need(Rejects(changedSharedContext, syntheticAirObservation),
            "Changed shared context accepted the original S003 AirMobile observation");

        return new JObject
        {
            ["kind"] = "S003LegacyAirContextObservationSourceOnlyUnitControl",
            ["synthetic_context"] = true,
            ["synthetic_observation"] = true,
            ["coded_s003_air_observation_accepted"] = true,
            ["wrong_scenario_index_rejected"] = true,
            ["false_air_profile_rejected"] = true,
            ["combined_arms_field_rejected"] = true,
            ["changed_shared_context_rejected"] = true,
            ["world_created"] = false,
            ["gameplay_input_sent"] = false,
            ["source_fit_certified"] = false,
            ["mission_ready"] = false
        };
    }
}
