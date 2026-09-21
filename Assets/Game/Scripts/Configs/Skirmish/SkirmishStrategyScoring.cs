using Game.Skirmish.Contracts;

namespace Game.Configs
{
    public static class SkirmishStrategyScoring
    {
        public const int ObjectiveGainWeight = 40;
        public const int PreventLossWeight = 100;
        public const int PreserveSubjectWeight = 80;
        public const int CounterAdvantageWeight = 20;
        public const int Hysteresis = 15;
        public const int ReserveSupplyPercent = 25;

        public static SkirmishStrategyScore ScoreBaseAssault(
            in SkirmishPublicPerception perception,
            SkirmishArmyProfileConfig army,
            SkirmishReadinessStage readiness,
            SkirmishRoleOverlay[] overlays,
            SkirmishStrategyPriority current)
        {
            var score = new SkirmishStrategyScore
            {
                Priority = SkirmishStrategyPriority.Reserve,
                Field = "reserve",
                RecruitRole = SkirmishRoleKind.None
            };
            if (!perception.Playing || perception.Finished)
            {
                score.Priority = SkirmishStrategyPriority.None;
                score.Field = "terminal";
                return score;
            }

            if (!perception.EnemyDesignatedAlive)
            {
                score.Priority = SkirmishStrategyPriority.None;
                score.PreventLoss = PreventLossWeight;
                score.Total = PreventLossWeight;
                score.Field = "home_lost";
                return score;
            }

            if (!perception.PlayerDesignatedAlive)
            {
                score.Priority = SkirmishStrategyPriority.Hold;
                score.ObjectiveGain = ObjectiveGainWeight;
                score.Total = ObjectiveGainWeight;
                score.Field = "objective_done";
                return score;
            }

            bool threatened = VisibleHostileCombat(perception) >= 8 &&
                              perception.OwnInfantryLive + perception.OwnGroundLive < 12;
            if (threatened)
            {
                score.Priority = SkirmishStrategyPriority.DefendHome;
                score.PreventLoss = PreventLossWeight;
                score.Total = PreventLossWeight;
                score.Field = "home_threat";
                return Prefer(current, score);
            }

            if (perception.VisibleHostileTanks > 0 &&
                TryAfford(army, readiness, overlays, perception, SkirmishRoleIds.Rocketeer, SkirmishRoleKind.Rocketeer))
            {
                score.Priority = SkirmishStrategyPriority.RecruitCounter;
                score.CounterAdvantage = CounterAdvantageWeight;
                score.ObjectiveGain = ObjectiveGainWeight / 2;
                score.Total = score.CounterAdvantage + score.ObjectiveGain;
                score.RecruitRole = SkirmishRoleKind.Rocketeer;
                score.Field = "counter.rocketeer";
                return Prefer(current, score);
            }

            int reserveFloor = perception.OwnSupplyCap * ReserveSupplyPercent / 100;
            if (perception.OwnSupplyLive > reserveFloor && VisibleHostileCombat(perception) >= 0)
            {
                score.Priority = SkirmishStrategyPriority.AttackBase;
                score.ObjectiveGain = ObjectiveGainWeight;
                score.Total = ObjectiveGainWeight;
                score.Field = "assault.base";
                return Prefer(current, score);
            }

            score.Priority = SkirmishStrategyPriority.Hold;
            score.Field = "hold_reserve";
            return Prefer(current, score);
        }

        public static bool TryAfford(
            SkirmishArmyProfileConfig army,
            SkirmishReadinessStage readiness,
            SkirmishRoleOverlay[] overlays,
            in SkirmishPublicPerception perception,
            string roleId,
            SkirmishRoleKind roleKind)
        {
            var request = new SkirmishProductionRequest
            {
                RoleId = roleId,
                RoleKind = roleKind,
                SquadCount = 1,
                BarracksPresent = true,
                GroundStagingPresent = true,
                EnforceStocks = true,
                MaterialsAvailable = perception.OwnMaterials,
                FuelAvailable = perception.OwnFuel,
                InfantryLive = perception.OwnInfantryLive,
                GroundLive = perception.OwnGroundLive,
                SupplyLive = perception.OwnSupplyLive,
                InfantryCap = 48,
                GroundCap = 8,
                AirCap = 2,
                SupplyCap = perception.OwnSupplyCap < 1 ? 128 : perception.OwnSupplyCap
            };
            SkirmishProductionDecision decision = SkirmishProductionEligibility.Evaluate(
                request, army, readiness, overlays);
            return decision.Accepted;
        }

        public static int HostileMaterialsOrUnknown(in SkirmishPublicPerception perception)
        {
            return perception.KnowsHostileMaterials ? perception.HostileMaterialsIfKnown : -1;
        }

        private static int VisibleHostileCombat(in SkirmishPublicPerception perception) =>
            perception.VisibleHostileInfantry + perception.VisibleHostileGround;

        private static SkirmishStrategyScore Prefer(
            SkirmishStrategyPriority current,
            SkirmishStrategyScore candidate)
        {
            if (current == SkirmishStrategyPriority.None || current == candidate.Priority)
                return candidate;
            candidate.Total -= Hysteresis;
            return candidate;
        }
    }
}
