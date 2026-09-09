using System;
using Game.Components;

namespace Game.Configs
{
    internal static class MissionDefenseDefinitionValidation
    {
        public static bool TryValidate(ScenarioSetupConfig scenario, out string error)
        {
            error = "Defense requires a finite, uniquely identified convoy, valid contact timing, core and sensor.";
            MissionDefenseDefinitionConfig defense = scenario.Defense;
            if (!scenario.MissionRuntime.Enabled || !defense.Enabled ||
                defense.ConvoyElements.Length is < 1 or > 4 ||
                string.IsNullOrWhiteSpace(defense.ForwardPostStableId) || defense.ForwardPostStableId.Length > 125 ||
                !HasAnchor(scenario, defense.InnerCoreAnchorId) ||
                !HasAnchor(scenario, defense.InitialProducerAnchorId) ||
                !float.IsFinite(defense.InnerCoreRadius) || defense.InnerCoreRadius <= 0f ||
                defense.RadarPingCharges is < 1 or > 8 || defense.RadarPingCooldownMilliseconds < 1000 ||
                !string.IsNullOrEmpty(scenario.MissionRuntime.DelayedWave.UnitGroupId)) return false;
            bool sensorFound = false;
            foreach (ScenarioUnitGroupConfig group in scenario.UnitGroups)
                foreach (ScenarioUnitEntryConfig unit in group.Units)
                    sensorFound |= group.FactionIndex == FactionIdentity.PlayerFactionId && unit.Count == 1 &&
                        unit.MissionRoleId == defense.SensorMissionRoleId;
            if (!sensorFound) return false;
            if (!defense.CameraTour.IsValid) return false;
            if (defense.GuidanceSteps.Length != 12) return false;
            for (int i=0;i<defense.GuidanceSteps.Length;i++)
            {
                var step=defense.GuidanceSteps[i];
                if (string.IsNullOrWhiteSpace(step.StepId) || step.StepId.Length>60 ||
                    string.IsNullOrWhiteSpace(step.TitleKey) || step.TitleKey.Length>60 ||
                    string.IsNullOrWhiteSpace(step.BodyKey) || step.BodyKey.Length>60 ||
                    !Enum.IsDefined(typeof(Game.Missions.Contracts.MissionGuidanceActionKind),step.Action) ||
                    !Enum.IsDefined(typeof(Game.Missions.Contracts.MissionGuidanceCompletionKind),step.Completion)) return false;
                for(int j=0;j<i;j++) if(step.StepId==defense.GuidanceSteps[j].StepId) return false;
            }
            for (int i = 0; i < defense.ConvoyElements.Length; i++)
            {
                MissionConvoyElementConfig element = defense.ConvoyElements[i];
                if (string.IsNullOrWhiteSpace(element.ElementId) || element.ElementId.Length > 60 ||
                    !HasAnchor(scenario, element.ContactAnchorId) ||
                    element.WarningAtMilliseconds < 0 || element.ActivationAtMilliseconds < element.WarningAtMilliseconds ||
                    element.ContactAtMilliseconds <= element.ActivationAtMilliseconds) return false;
                for (int j = 0; j < i; j++)
                    if (element.ElementId == defense.ConvoyElements[j].ElementId ||
                        element.UnitGroupId == defense.ConvoyElements[j].UnitGroupId ||
                        element.RouteId == defense.ConvoyElements[j].RouteId) return false;
                int groupMatches = 0, routeMatches = 0;
                foreach (ScenarioUnitGroupConfig group in scenario.UnitGroups)
                    if (group.GroupId == element.UnitGroupId && group.FactionIndex > FactionIdentity.PlayerFactionId)
                        groupMatches++;
                foreach (ScenarioPatrolRouteConfig route in scenario.PatrolRoutes)
                    if (route.RouteId == element.RouteId && route.UnitGroupId == element.UnitGroupId &&
                        route.StartDelayMilliseconds == element.ActivationAtMilliseconds && route.AnchorIds.Length >= 2)
                        routeMatches++;
                if (groupMatches != 1 || routeMatches != 1) return false;
            }
            foreach (ScenarioUnitGroupConfig group in scenario.UnitGroups)
            {
                if (group.FactionIndex <= FactionIdentity.PlayerFactionId) continue;
                bool included = false;
                foreach (MissionConvoyElementConfig element in defense.ConvoyElements)
                    included |= group.GroupId == element.UnitGroupId;
                if (!included) return false;
            }
            error = null;
            return true;
        }

        private static bool HasAnchor(ScenarioSetupConfig scenario, string anchorId)
        {
            if (string.IsNullOrWhiteSpace(anchorId)) return false;
            foreach (ScenarioAnchorRequirementConfig anchor in scenario.RequiredAnchors)
                if (anchor.AnchorId == anchorId) return true;
            return false;
        }
    }
}
