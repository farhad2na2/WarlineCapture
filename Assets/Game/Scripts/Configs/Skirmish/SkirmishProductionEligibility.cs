using Game.Skirmish.Contracts;

namespace Game.Configs
{
    public static class SkirmishProductionEligibility
    {
        public static SkirmishProductionDecision Evaluate(
            SkirmishProductionRequest request,
            SkirmishArmyProfileConfig army,
            SkirmishReadinessStage readiness,
            SkirmishRoleOverlay[] overlays)
        {
            var decision = new SkirmishProductionDecision
            {
                Accepted = false,
                Reason = SkirmishReasonCode.ProductionRejected,
                Field = "roleId",
                MemberCount = 0,
                Producer = SkirmishProducerKind.None
            };
            if (army == null || string.IsNullOrEmpty(request.RoleId) || !army.Allows(request.RoleId))
            {
                decision.Reason = SkirmishReasonCode.UnsupportedRole;
                decision.Field = "armyProfile";
                return decision;
            }

            if (SkirmishRoleIds.IsOffensiveAir(request.RoleKind) && !army.AllowsOffensiveAir)
            {
                decision.Reason = SkirmishReasonCode.UnsupportedCapability;
                decision.Field = "offensive_air";
                return decision;
            }

            if (!SkirmishRoleOverlayCatalog.TryGet(overlays, request.RoleKind, out SkirmishRoleOverlay overlay))
            {
                decision.Reason = SkirmishReasonCode.UnsupportedRole;
                decision.Field = "overlay";
                return decision;
            }

            if ((int)readiness < (int)RequiredReadiness(request.RoleKind))
            {
                decision.Reason = SkirmishReasonCode.MissingReadiness;
                decision.Field = "readiness";
                return decision;
            }

            if (overlay.Producer == SkirmishProducerKind.Barracks && !request.BarracksPresent)
            {
                decision.Reason = SkirmishReasonCode.MissingProducer;
                decision.Field = "producer.barracks";
                return decision;
            }

            if (overlay.Producer == SkirmishProducerKind.GroundStaging && !request.GroundStagingPresent)
            {
                decision.Reason = SkirmishReasonCode.MissingProducer;
                decision.Field = "producer.ground_staging";
                return decision;
            }

            if (overlay.Producer == SkirmishProducerKind.Helipad && !request.HelipadPresent)
            {
                decision.Reason = SkirmishReasonCode.MissingProducer;
                decision.Field = "producer.helipad";
                return decision;
            }

            if (overlay.Producer == SkirmishProducerKind.Airport && !request.AirportPresent)
            {
                decision.Reason = SkirmishReasonCode.MissingProducer;
                decision.Field = "producer.airport";
                return decision;
            }

            if (overlay.Producer == SkirmishProducerKind.IntelStation && !request.IntelStationPresent)
            {
                decision.Reason = SkirmishReasonCode.MissingProducer;
                decision.Field = "producer.intel_station";
                return decision;
            }

            int squads = request.SquadCount < 1 ? 1 : request.SquadCount;
            int members = overlay.SquadMembers < 1 ? 1 : overlay.SquadMembers;
            if (overlay.Producer == SkirmishProducerKind.Barracks)
                members = SkirmishRoleIds.InfantrySquadMembers * squads;
            else
                members *= squads;

            int supply = overlay.SupplyCost * members;
            int materials = overlay.MaterialsCost * squads;
            int fuel = overlay.FuelCost * squads;
            if (request.EnforceStocks)
            {
                if (materials > request.MaterialsAvailable)
                {
                    decision.Reason = SkirmishReasonCode.InsufficientMaterials;
                    decision.Field = "materials";
                    decision.MaterialsCost = materials;
                    return decision;
                }

                if (fuel > request.FuelAvailable)
                {
                    decision.Reason = SkirmishReasonCode.InsufficientFuel;
                    decision.Field = "fuel";
                    decision.FuelCost = fuel;
                    return decision;
                }

                SkirmishPopulationCategory category = SkirmishRoleIds.Category(request.RoleKind);
                if (!FitsCap(category, members, supply, request))
                {
                    decision.Reason = SkirmishReasonCode.InsufficientCapacity;
                    decision.Field = "capacity";
                    decision.SupplyCost = supply;
                    return decision;
                }
            }

            decision.Accepted = true;
            decision.Reason = SkirmishReasonCode.None;
            decision.Field = string.Empty;
            decision.MemberCount = members;
            decision.Producer = overlay.Producer;
            decision.SupplyCost = supply;
            decision.MaterialsCost = materials;
            decision.FuelCost = fuel;
            return decision;
        }

        private static bool FitsCap(
            SkirmishPopulationCategory category,
            int members,
            int supply,
            SkirmishProductionRequest request)
        {
            int infantry = request.InfantryLive + request.InfantryReserved;
            int ground = request.GroundLive + request.GroundReserved;
            int air = request.AirLive + request.AirReserved;
            int usedSupply = request.SupplyLive + request.SupplyReserved;
            if (usedSupply + supply > request.SupplyCap)
                return false;
            if (category == SkirmishPopulationCategory.Infantry)
                return infantry + members <= request.InfantryCap;
            if (category == SkirmishPopulationCategory.Ground)
                return ground + members <= request.GroundCap;
            if (category == SkirmishPopulationCategory.Air)
                return air + members <= request.AirCap;
            return true;
        }

        public static SkirmishReadinessStage RequiredReadiness(SkirmishRoleKind kind)
        {
            switch (kind)
            {
                case SkirmishRoleKind.ApcHeavy:
                case SkirmishRoleKind.Tank:
                case SkirmishRoleKind.Radar:
                case SkirmishRoleKind.TransportHeli:
                case SkirmishRoleKind.AttackHeliLight:
                case SkirmishRoleKind.AttackHeli:
                case SkirmishRoleKind.Drone:
                    return SkirmishReadinessStage.Established;
                case SkirmishRoleKind.Siege:
                case SkirmishRoleKind.Fighter:
                case SkirmishRoleKind.Strike:
                case SkirmishRoleKind.TransportPlane:
                    return SkirmishReadinessStage.FullArsenal;
                default:
                    return SkirmishReadinessStage.Field;
            }
        }
    }
}
