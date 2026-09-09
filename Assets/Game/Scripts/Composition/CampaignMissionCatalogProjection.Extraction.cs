using Game.Components;
using Game.Configs;
using Unity.Collections;

namespace Game.Composition
{
    internal static partial class CampaignMissionCatalogProjection
    {
        private static CampaignMissionExtractionDefinitionBlob ProjectExtraction(MissionExtractionDefinitionConfig source)
        {
            if (!source.Enabled) return default;
            return new CampaignMissionExtractionDefinitionBlob
            {
                Enabled = 1, VehiclesSelfSupplied = Flag(source.VehiclesSelfSupplied),
                AuthoredMapDefensesDormant = Flag(source.AuthoredMapDefensesDormant),
                PassengerRoleId = new FixedString64Bytes(source.PassengerRoleId),
                CarrierRoleId = new FixedString64Bytes(source.CarrierRoleId), AircraftRoleId = new FixedString64Bytes(source.AircraftRoleId),
                RescueAnchorId = new FixedString64Bytes(source.RescueAnchorId), LandingAnchorId = new FixedString64Bytes(source.LandingAnchorId),
                DepartureAnchorId = new FixedString64Bytes(source.DepartureAnchorId),
                RequiredPassengers = source.RequiredPassengers, SecureHoldMilliseconds = source.SecureHoldMilliseconds,
                DeadlineMilliseconds = source.DeadlineMilliseconds, LandingRadius = source.LandingRadius, DepartureRadius = source.DepartureRadius,
                CameraTour = ProjectCameraTour(source.CameraTour)
            };
        }

        private static bool MatchesExtraction(CampaignMissionExtractionDefinitionBlob value, MissionExtractionDefinitionConfig source)
        {
            if (value.Enabled != Flag(source.Enabled)) return false;
            if (!source.Enabled) return true;
            return Matches(value.PassengerRoleId, source.PassengerRoleId) && Matches(value.CarrierRoleId, source.CarrierRoleId) &&
                Matches(value.AircraftRoleId, source.AircraftRoleId) && Matches(value.RescueAnchorId, source.RescueAnchorId) &&
                Matches(value.LandingAnchorId, source.LandingAnchorId) && Matches(value.DepartureAnchorId, source.DepartureAnchorId) &&
                value.RequiredPassengers == source.RequiredPassengers && value.SecureHoldMilliseconds == source.SecureHoldMilliseconds &&
                value.DeadlineMilliseconds == source.DeadlineMilliseconds && value.LandingRadius == source.LandingRadius &&
                value.DepartureRadius == source.DepartureRadius && value.VehiclesSelfSupplied == Flag(source.VehiclesSelfSupplied) &&
                value.AuthoredMapDefensesDormant == Flag(source.AuthoredMapDefensesDormant) && MatchesCameraTour(value.CameraTour, source.CameraTour);
        }
    }
}
