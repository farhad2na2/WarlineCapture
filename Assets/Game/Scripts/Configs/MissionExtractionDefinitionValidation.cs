using System;
using Game.Components;

namespace Game.Configs
{
    internal static class MissionExtractionDefinitionValidation
    {
        public static bool TryValidate(ScenarioSetupConfig scenario, out string error)
        {
            error = null;
            var extraction = scenario.Extraction;
            if (!extraction.Enabled) return true;
            var restrictions = scenario.Restrictions;
            if (scenario.Defense.Enabled || scenario.MissionRuntime.Enabled || restrictions.TransportDisabled || restrictions.AirDisabled ||
                !restrictions.BuildingDisabled || !restrictions.ProductionDisabled || !restrictions.EconomyDisabled ||
                extraction.RequiredPassengers < 1 || extraction.RequiredPassengers > 10 ||
                extraction.SecureHoldMilliseconds < 1000 || extraction.DeadlineMilliseconds <= extraction.SecureHoldMilliseconds ||
                !float.IsFinite(extraction.LandingRadius) || extraction.LandingRadius <= 0 ||
                !float.IsFinite(extraction.DepartureRadius) || extraction.DepartureRadius <= 0 ||
                !extraction.CameraTour.IsValid || string.IsNullOrWhiteSpace(extraction.PassengerRoleId) ||
                string.IsNullOrWhiteSpace(extraction.CarrierRoleId) || string.IsNullOrWhiteSpace(extraction.AircraftRoleId) ||
                extraction.PassengerRoleId == extraction.AircraftRoleId ||
                !HasAnchor(scenario, extraction.RescueAnchorId) || !HasAnchor(scenario, extraction.LandingAnchorId) ||
                !HasAnchor(scenario, extraction.DepartureAnchorId) || extraction.LandingAnchorId == extraction.DepartureAnchorId)
            { error = "Extraction requires its own bounded transport scenario and valid rescue/LZ/departure anchors."; return false; }
            int passengers = 0, carriers = 0, aircraft = 0;
            foreach (var group in scenario.UnitGroups)
            foreach (var unit in group.Units)
            {
                if (unit.MissionRoleId == extraction.PassengerRoleId) passengers += group.FactionIndex == FactionIdentity.PlayerFactionId ? unit.Count : -100;
                if (unit.MissionRoleId == extraction.CarrierRoleId) carriers += group.FactionIndex == FactionIdentity.PlayerFactionId ? unit.Count : -100;
                if (unit.MissionRoleId == extraction.AircraftRoleId) aircraft += group.FactionIndex == FactionIdentity.PlayerFactionId ? unit.Count : -100;
            }
            if (passengers != extraction.RequiredPassengers || carriers != 1 || aircraft != 1 ||
                extraction.PassengerRoleId == extraction.CarrierRoleId || extraction.CarrierRoleId == extraction.AircraftRoleId)
            { error = "Extraction manifest must contain the exact friendly passenger count, one APC and one aircraft."; return false; }
            return true;
        }
        private static bool HasAnchor(ScenarioSetupConfig scenario, string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length > 60) return false;
            foreach (var anchor in scenario.RequiredAnchors) if (anchor.AnchorId == id) return true;
            return false;
        }
    }
}
