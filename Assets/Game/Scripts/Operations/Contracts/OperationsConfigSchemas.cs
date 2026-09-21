using System;
using System.Collections.Generic;

namespace Game.Operations.Contracts
{
    public readonly struct OperationsDistrictMetricTuple
    {
        public OperationsDistrictMetricTuple(
            int security, int trust, int infrastructure, int enemyInfluence,
            int intelConfidence, int heat, int supplyReadiness)
        {
            Security = ClampMetric(security);
            Trust = ClampMetric(trust);
            Infrastructure = ClampMetric(infrastructure);
            EnemyInfluence = ClampMetric(enemyInfluence);
            IntelConfidence = ClampMetric(intelConfidence);
            Heat = ClampMetric(heat);
            SupplyReadiness = ClampMetric(supplyReadiness);
        }

        public int Security { get; }
        public int Trust { get; }
        public int Infrastructure { get; }
        public int EnemyInfluence { get; }
        public int IntelConfidence { get; }
        public int Heat { get; }
        public int SupplyReadiness { get; }

        public static int ClampMetric(int value) => value < 0 ? 0 : value > 100 ? 100 : value;

        public static OperationsDistrictMetricTuple StartingForDistrict(int districtNumber) => districtNumber switch
        {
            1 => new(40, 45, 45, 50, 20, 20, 45),
            2 => new(35, 40, 40, 55, 20, 25, 40),
            3 => new(35, 40, 35, 60, 15, 25, 35),
            4 => new(30, 45, 40, 55, 15, 20, 35),
            5 => new(30, 40, 35, 60, 15, 20, 30),
            6 => new(30, 35, 40, 65, 15, 30, 35),
            _ => throw new ArgumentOutOfRangeException(nameof(districtNumber))
        };

        public static OperationsDistrictMetricTuple VictoryDelta(OperationsMissionFamilyKind family) => family switch
        {
            OperationsMissionFamilyKind.Recon => new(0, 1, 0, -2, 18, 2, 0),
            OperationsMissionFamilyKind.Patrol => new(10, 3, 0, -5, 4, 2, 0),
            OperationsMissionFamilyKind.Raid => new(8, 1, 0, -14, 6, 7, 0),
            OperationsMissionFamilyKind.Rescue => new(3, 14, 0, -3, 3, 3, 0),
            OperationsMissionFamilyKind.Escort => new(4, 6, 2, -3, 0, 2, 14),
            OperationsMissionFamilyKind.Repair => new(3, 5, 18, -2, 0, 2, 6),
            OperationsMissionFamilyKind.Defense => new(12, 5, 4, -8, 2, 3, 2),
            OperationsMissionFamilyKind.Interdict => new(7, 1, 0, -12, 6, 5, 4),
            OperationsMissionFamilyKind.Seize => new(10, 2, 4, -10, 3, 5, 8),
            OperationsMissionFamilyKind.Airlift => new(4, 10, 0, -3, 2, 4, 10),
            OperationsMissionFamilyKind.Breach => new(8, 1, 0, -16, 8, 8, 0),
            OperationsMissionFamilyKind.Finale => new(12, 8, 10, -20, 5, -10, 10),
            _ => throw new ArgumentOutOfRangeException(nameof(family))
        };

        public static OperationsDistrictMetricTuple DefeatDelta => new(-4, -2, 0, 6, 0, 3, 0);
        public static OperationsDistrictMetricTuple WithdrawnDelta => new(-2, 0, 0, 3, 0, 0, 0);
    }

    public readonly struct OperationsAdjacencyPair
    {
        public OperationsAdjacencyPair(int leftDistrict, int rightDistrict)
        {
            if (leftDistrict is < 1 or > OperationsIdentityRules.DistrictCount)
                throw new ArgumentOutOfRangeException(nameof(leftDistrict));
            if (rightDistrict is < 1 or > OperationsIdentityRules.DistrictCount)
                throw new ArgumentOutOfRangeException(nameof(rightDistrict));
            if (leftDistrict == rightDistrict)
                throw new ArgumentException("Adjacency requires two districts.");
            LeftDistrict = Math.Min(leftDistrict, rightDistrict);
            RightDistrict = Math.Max(leftDistrict, rightDistrict);
        }

        public int LeftDistrict { get; }
        public int RightDistrict { get; }
    }

    public readonly struct OperationsCampaignSchema
    {
        public const int StartingActionPoints = 3;
        public const int StartingDay = 1;

        public OperationsCampaignSchema(
            int schemaVersion,
            string campaignId,
            OperationsAdjacencyPair[] adjacency,
            int startingActionPoints)
        {
            if (schemaVersion < 1)
                throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            OperationsContractText.RequireToken(campaignId, nameof(campaignId));
            if (startingActionPoints < 1)
                throw new ArgumentOutOfRangeException(nameof(startingActionPoints));
            SchemaVersion = schemaVersion;
            CampaignId = campaignId;
            Adjacency = adjacency ?? Array.Empty<OperationsAdjacencyPair>();
            StartingActionPoints = startingActionPoints;
        }

        public int SchemaVersion { get; }
        public string CampaignId { get; }
        public OperationsAdjacencyPair[] Adjacency { get; }
        public int StartingActionPoints { get; }

        public static OperationsCampaignSchema CreateBaseline() => new(
            OperationsIdentityRules.CurrentSchemaVersion,
            "campaign.operations.city_v1",
            new[]
            {
                new OperationsAdjacencyPair(1, 2),
                new OperationsAdjacencyPair(1, 4),
                new OperationsAdjacencyPair(2, 3),
                new OperationsAdjacencyPair(2, 6),
                new OperationsAdjacencyPair(3, 4),
                new OperationsAdjacencyPair(3, 6),
                new OperationsAdjacencyPair(4, 5),
                new OperationsAdjacencyPair(5, 6)
            },
            StartingActionPoints);

        public bool TryValidate(out string error)
        {
            if (Adjacency.Length != 8)
            {
                error = "Operations campaign adjacency must declare the eight authored pairs.";
                return false;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < Adjacency.Length; index++)
            {
                string key = Adjacency[index].LeftDistrict + "-" + Adjacency[index].RightDistrict;
                if (!seen.Add(key))
                {
                    error = "Operations campaign adjacency contains a duplicate pair.";
                    return false;
                }
            }

            error = null;
            return true;
        }
    }

    public readonly struct OperationsDistrictSchema
    {
        public OperationsDistrictSchema(
            int schemaVersion,
            string districtId,
            string operationMapId,
            string displayNameKey,
            OperationsDistrictMetricTuple startingMetrics,
            OperationsCivilianDensityKind civilianDensity)
        {
            if (schemaVersion < 1)
                throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            OperationsContractText.RequireId(districtId, nameof(districtId), OperationsIdentityRules.IsValidDistrictId);
            OperationsContractText.RequireId(operationMapId, nameof(operationMapId), OperationsIdentityRules.IsValidOperationMapId);
            OperationsContractText.RequireToken(displayNameKey, nameof(displayNameKey));
            SchemaVersion = schemaVersion;
            DistrictId = districtId;
            OperationMapId = operationMapId;
            DisplayNameKey = displayNameKey;
            StartingMetrics = startingMetrics;
            CivilianDensity = civilianDensity;
        }

        public int SchemaVersion { get; }
        public string DistrictId { get; }
        public string OperationMapId { get; }
        public string DisplayNameKey { get; }
        public OperationsDistrictMetricTuple StartingMetrics { get; }
        public OperationsCivilianDensityKind CivilianDensity { get; }
    }

    public readonly struct OperationsActionSchema
    {
        public OperationsActionSchema(
            int schemaVersion,
            OperationsAbstractActionKind kind,
            string actionId,
            bool citywideOncePerDay,
            int securityDelta,
            int trustDelta,
            int infrastructureDelta,
            int intelDelta,
            int heatDelta,
            int supplyDelta)
        {
            if (schemaVersion < 1)
                throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            if (kind == OperationsAbstractActionKind.None)
                throw new ArgumentOutOfRangeException(nameof(kind));
            OperationsContractText.RequireId(actionId, nameof(actionId), OperationsIdentityRules.IsValidActionId);
            SchemaVersion = schemaVersion;
            Kind = kind;
            ActionId = actionId;
            CitywideOncePerDay = citywideOncePerDay;
            SecurityDelta = securityDelta;
            TrustDelta = trustDelta;
            InfrastructureDelta = infrastructureDelta;
            IntelDelta = intelDelta;
            HeatDelta = heatDelta;
            SupplyDelta = supplyDelta;
        }

        public int SchemaVersion { get; }
        public OperationsAbstractActionKind Kind { get; }
        public string ActionId { get; }
        public bool CitywideOncePerDay { get; }
        public int SecurityDelta { get; }
        public int TrustDelta { get; }
        public int InfrastructureDelta { get; }
        public int IntelDelta { get; }
        public int HeatDelta { get; }
        public int SupplyDelta { get; }

        public static OperationsActionSchema[] CreateBaseline() => new[]
        {
            new OperationsActionSchema(1, OperationsAbstractActionKind.Analyze, "action.operations.analyze", false, 0, 0, 0, 12, 0, 0),
            new OperationsActionSchema(1, OperationsAbstractActionKind.Community, "action.operations.community", false, 0, 6, 0, 0, -4, 0),
            new OperationsActionSchema(1, OperationsAbstractActionKind.Service, "action.operations.service", false, 0, 0, 5, 0, 0, 0),
            new OperationsActionSchema(1, OperationsAbstractActionKind.Patrol, "action.operations.patrol", false, 5, 0, 0, 0, 2, 0),
            new OperationsActionSchema(1, OperationsAbstractActionKind.Allocate, "action.operations.allocate", true, 0, 0, 0, 0, 0, 8),
            new OperationsActionSchema(1, OperationsAbstractActionKind.Deescalate, "action.operations.deescalate", false, 0, 0, 0, -3, -10, 0)
        };
    }

    public readonly struct OperationsForceRoleLine
    {
        public OperationsForceRoleLine(OperationsRosterRoleKind role, int count)
        {
            if (role == OperationsRosterRoleKind.None)
                throw new ArgumentOutOfRangeException(nameof(role));
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count));
            Role = role;
            Count = count;
        }

        public OperationsRosterRoleKind Role { get; }
        public int Count { get; }
    }

    public readonly struct OperationsForcePackageSchema
    {
        public OperationsForcePackageSchema(
            OperationsForcePackageKind kind,
            OperationsForceRoleLine[] lines,
            int materials,
            int fuel,
            int oil)
        {
            if (kind == OperationsForcePackageKind.None)
                throw new ArgumentOutOfRangeException(nameof(kind));
            OperationsContractText.RequireNonNegative(materials, nameof(materials));
            OperationsContractText.RequireNonNegative(fuel, nameof(fuel));
            OperationsContractText.RequireNonNegative(oil, nameof(oil));
            Kind = kind;
            Lines = lines ?? Array.Empty<OperationsForceRoleLine>();
            Materials = materials;
            Fuel = fuel;
            Oil = oil;
        }

        public OperationsForcePackageKind Kind { get; }
        public OperationsForceRoleLine[] Lines { get; }
        public int Materials { get; }
        public int Fuel { get; }
        public int Oil { get; }

        public int EntityCount
        {
            get
            {
                int total = 0;
                for (int index = 0; index < Lines.Length; index++)
                    total += Lines[index].Count;
                return total;
            }
        }

        public static OperationsForcePackageSchema Create(OperationsForcePackageKind kind) => kind switch
        {
            OperationsForcePackageKind.Light => new(
                kind,
                new[]
                {
                    new OperationsForceRoleLine(OperationsRosterRoleKind.RifleInfantry, 12),
                    new OperationsForceRoleLine(OperationsRosterRoleKind.ReconInfantry, 2),
                    new OperationsForceRoleLine(OperationsRosterRoleKind.SupportInfantry, 2)
                },
                80, 0, 0),
            OperationsForcePackageKind.Service => new(
                kind,
                new[]
                {
                    new OperationsForceRoleLine(OperationsRosterRoleKind.RifleInfantry, 12),
                    new OperationsForceRoleLine(OperationsRosterRoleKind.ReconInfantry, 2),
                    new OperationsForceRoleLine(OperationsRosterRoleKind.SupportInfantry, 4),
                    new OperationsForceRoleLine(OperationsRosterRoleKind.RepairSpecialist, 2),
                    new OperationsForceRoleLine(OperationsRosterRoleKind.Apc, 2)
                },
                240, 240, 0),
            OperationsForcePackageKind.Ground => new(
                kind,
                new[]
                {
                    new OperationsForceRoleLine(OperationsRosterRoleKind.RifleInfantry, 20),
                    new OperationsForceRoleLine(OperationsRosterRoleKind.ReconInfantry, 2),
                    new OperationsForceRoleLine(OperationsRosterRoleKind.SupportInfantry, 4),
                    new OperationsForceRoleLine(OperationsRosterRoleKind.AntiArmorInfantry, 4),
                    new OperationsForceRoleLine(OperationsRosterRoleKind.RepairSpecialist, 2),
                    new OperationsForceRoleLine(OperationsRosterRoleKind.Apc, 2),
                    new OperationsForceRoleLine(OperationsRosterRoleKind.Tank, 2)
                },
                240, 360, 0),
            OperationsForcePackageKind.Air => new(
                kind,
                new[]
                {
                    new OperationsForceRoleLine(OperationsRosterRoleKind.RifleInfantry, 12),
                    new OperationsForceRoleLine(OperationsRosterRoleKind.ReconInfantry, 2),
                    new OperationsForceRoleLine(OperationsRosterRoleKind.SupportInfantry, 4),
                    new OperationsForceRoleLine(OperationsRosterRoleKind.AntiAirInfantry, 2),
                    new OperationsForceRoleLine(OperationsRosterRoleKind.TransportHelicopter, 2)
                },
                120, 480, 0),
            OperationsForcePackageKind.Combined => new(
                kind,
                new[]
                {
                    new OperationsForceRoleLine(OperationsRosterRoleKind.RifleInfantry, 20),
                    new OperationsForceRoleLine(OperationsRosterRoleKind.ReconInfantry, 2),
                    new OperationsForceRoleLine(OperationsRosterRoleKind.SupportInfantry, 4),
                    new OperationsForceRoleLine(OperationsRosterRoleKind.AntiArmorInfantry, 4),
                    new OperationsForceRoleLine(OperationsRosterRoleKind.AntiAirInfantry, 2),
                    new OperationsForceRoleLine(OperationsRosterRoleKind.RepairSpecialist, 2),
                    new OperationsForceRoleLine(OperationsRosterRoleKind.Apc, 2),
                    new OperationsForceRoleLine(OperationsRosterRoleKind.Tank, 2),
                    new OperationsForceRoleLine(OperationsRosterRoleKind.TransportHelicopter, 2)
                },
                320, 600, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    public readonly struct OperationsThreatProfileSchema
    {
        public OperationsThreatProfileSchema(
            OperationsEnemyPackageKind kind,
            OperationsForceRoleLine[] lines)
        {
            if (kind == OperationsEnemyPackageKind.None)
                throw new ArgumentOutOfRangeException(nameof(kind));
            Kind = kind;
            Lines = lines ?? Array.Empty<OperationsForceRoleLine>();
        }

        public OperationsEnemyPackageKind Kind { get; }
        public OperationsForceRoleLine[] Lines { get; }

        public int EntityCount
        {
            get
            {
                int total = 0;
                for (int index = 0; index < Lines.Length; index++)
                    total += Lines[index].Count;
                return total;
            }
        }

        public static OperationsThreatProfileSchema Create(OperationsEnemyPackageKind kind) => kind switch
        {
            OperationsEnemyPackageKind.Cell => new(kind, new[]
            {
                new OperationsForceRoleLine(OperationsRosterRoleKind.RifleInfantry, 18),
                new OperationsForceRoleLine(OperationsRosterRoleKind.SupportInfantry, 2)
            }),
            OperationsEnemyPackageKind.Raiders => new(kind, new[]
            {
                new OperationsForceRoleLine(OperationsRosterRoleKind.RifleInfantry, 24),
                new OperationsForceRoleLine(OperationsRosterRoleKind.SupportInfantry, 4),
                new OperationsForceRoleLine(OperationsRosterRoleKind.LightVehicle, 2)
            }),
            OperationsEnemyPackageKind.Mechanized => new(kind, new[]
            {
                new OperationsForceRoleLine(OperationsRosterRoleKind.RifleInfantry, 24),
                new OperationsForceRoleLine(OperationsRosterRoleKind.SupportInfantry, 4),
                new OperationsForceRoleLine(OperationsRosterRoleKind.AntiArmorInfantry, 4),
                new OperationsForceRoleLine(OperationsRosterRoleKind.Apc, 2),
                new OperationsForceRoleLine(OperationsRosterRoleKind.Tank, 2)
            }),
            OperationsEnemyPackageKind.Airfield => new(kind, new[]
            {
                new OperationsForceRoleLine(OperationsRosterRoleKind.RifleInfantry, 24),
                new OperationsForceRoleLine(OperationsRosterRoleKind.SupportInfantry, 4),
                new OperationsForceRoleLine(OperationsRosterRoleKind.AntiAirInfantry, 4),
                new OperationsForceRoleLine(OperationsRosterRoleKind.LightVehicle, 2)
            }),
            OperationsEnemyPackageKind.Finale => new(kind, new[]
            {
                new OperationsForceRoleLine(OperationsRosterRoleKind.RifleInfantry, 32),
                new OperationsForceRoleLine(OperationsRosterRoleKind.SupportInfantry, 4),
                new OperationsForceRoleLine(OperationsRosterRoleKind.AntiArmorInfantry, 4),
                new OperationsForceRoleLine(OperationsRosterRoleKind.Apc, 2),
                new OperationsForceRoleLine(OperationsRosterRoleKind.Tank, 2)
            }),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    public readonly struct OperationsRewardSchema
    {
        public const int FirstClearCredits = 100;
        public const int FirstClearCommanderXp = 50;
        public const int RepeatVictoryCredits = 20;
        public const int RepeatCreditsDailyCap = 60;
        public const int FirstEndDayWithVictoryCredits = 20;

        public OperationsRewardSchema(
            int schemaVersion,
            int firstClearCredits,
            int firstClearCommanderXp,
            int repeatVictoryCredits,
            int repeatCreditsDailyCap,
            int firstEndDayWithVictoryCredits)
        {
            if (schemaVersion < 1)
                throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            OperationsContractText.RequireNonNegative(firstClearCredits, nameof(firstClearCredits));
            OperationsContractText.RequireNonNegative(firstClearCommanderXp, nameof(firstClearCommanderXp));
            OperationsContractText.RequireNonNegative(repeatVictoryCredits, nameof(repeatVictoryCredits));
            OperationsContractText.RequireNonNegative(repeatCreditsDailyCap, nameof(repeatCreditsDailyCap));
            OperationsContractText.RequireNonNegative(firstEndDayWithVictoryCredits, nameof(firstEndDayWithVictoryCredits));
            SchemaVersion = schemaVersion;
            FirstClearCreditsAmount = firstClearCredits;
            FirstClearCommanderXpAmount = firstClearCommanderXp;
            RepeatVictoryCreditsAmount = repeatVictoryCredits;
            RepeatCreditsDailyCapAmount = repeatCreditsDailyCap;
            FirstEndDayWithVictoryCreditsAmount = firstEndDayWithVictoryCredits;
        }

        public int SchemaVersion { get; }
        public int FirstClearCreditsAmount { get; }
        public int FirstClearCommanderXpAmount { get; }
        public int RepeatVictoryCreditsAmount { get; }
        public int RepeatCreditsDailyCapAmount { get; }
        public int FirstEndDayWithVictoryCreditsAmount { get; }

        public static OperationsRewardSchema CreateBaseline() => new(
            OperationsIdentityRules.CurrentSchemaVersion,
            FirstClearCredits,
            FirstClearCommanderXp,
            RepeatVictoryCredits,
            RepeatCreditsDailyCap,
            FirstEndDayWithVictoryCredits);
    }

    public readonly struct OperationsBalanceSchema
    {
        public const int RaidMinimumIntel = 40;
        public const int LowSupplyThreshold = 30;
        public const int HighConfidenceThreshold = 70;
        public const int HighHeatThreshold = 70;
        public const int ServiceRouteInfrastructureThreshold = 60;

        public OperationsBalanceSchema(
            int schemaVersion,
            int raidMinimumIntel,
            int dailyActionPoints,
            int maxOffersPerDistrict,
            int maxActiveIncidents)
        {
            if (schemaVersion < 1)
                throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            OperationsContractText.RequireNonNegative(raidMinimumIntel, nameof(raidMinimumIntel));
            if (dailyActionPoints < 1)
                throw new ArgumentOutOfRangeException(nameof(dailyActionPoints));
            if (maxOffersPerDistrict < 1)
                throw new ArgumentOutOfRangeException(nameof(maxOffersPerDistrict));
            if (maxActiveIncidents < 1)
                throw new ArgumentOutOfRangeException(nameof(maxActiveIncidents));
            SchemaVersion = schemaVersion;
            RaidMinimumIntel = raidMinimumIntel;
            DailyActionPoints = dailyActionPoints;
            MaxOffersPerDistrict = maxOffersPerDistrict;
            MaxActiveIncidents = maxActiveIncidents;
        }

        public int SchemaVersion { get; }
        public int RaidMinimumIntel { get; }
        public int DailyActionPoints { get; }
        public int MaxOffersPerDistrict { get; }
        public int MaxActiveIncidents { get; }

        public static OperationsBalanceSchema CreateBaseline() => new(
            OperationsIdentityRules.CurrentSchemaVersion,
            RaidMinimumIntel,
            OperationsCampaignSchema.StartingActionPoints,
            2,
            2);
    }

    public readonly struct OperationsMissionDefinitionSchema
    {
        public OperationsMissionDefinitionSchema(
            int schemaVersion,
            string missionId,
            string scenarioId,
            string operationMapId,
            string districtId,
            int localSlot,
            OperationsMissionFamilyKind family,
            OperationsForcePackageKind forcePackage,
            OperationsEnemyPackageKind enemyPackage,
            int hardDeadlineSeconds,
            int minIntel,
            int canonicalSeed,
            string displayNameKey,
            string briefKey,
            string debriefKey,
            string[] requiredFeatureIds)
        {
            if (schemaVersion < 1)
                throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            OperationsContractText.RequireId(missionId, nameof(missionId), OperationsIdentityRules.IsValidMissionId);
            OperationsContractText.RequireId(scenarioId, nameof(scenarioId), OperationsIdentityRules.IsValidScenarioId);
            OperationsContractText.RequireId(operationMapId, nameof(operationMapId), OperationsIdentityRules.IsValidOperationMapId);
            OperationsContractText.RequireId(districtId, nameof(districtId), OperationsIdentityRules.IsValidDistrictId);
            if (localSlot is < 1 or > 10)
                throw new ArgumentOutOfRangeException(nameof(localSlot));
            if (family == OperationsMissionFamilyKind.None)
                throw new ArgumentOutOfRangeException(nameof(family));
            if (forcePackage == OperationsForcePackageKind.None)
                throw new ArgumentOutOfRangeException(nameof(forcePackage));
            if (enemyPackage == OperationsEnemyPackageKind.None)
                throw new ArgumentOutOfRangeException(nameof(enemyPackage));
            if (hardDeadlineSeconds < 1)
                throw new ArgumentOutOfRangeException(nameof(hardDeadlineSeconds));
            OperationsContractText.RequireNonNegative(minIntel, nameof(minIntel));
            if (canonicalSeed == 0)
                throw new ArgumentOutOfRangeException(nameof(canonicalSeed));
            OperationsContractText.RequireToken(displayNameKey, nameof(displayNameKey));
            OperationsContractText.RequireToken(briefKey, nameof(briefKey));
            OperationsContractText.RequireToken(debriefKey, nameof(debriefKey));

            SchemaVersion = schemaVersion;
            MissionId = missionId;
            ScenarioId = scenarioId;
            OperationMapId = operationMapId;
            DistrictId = districtId;
            LocalSlot = localSlot;
            Family = family;
            ForcePackage = forcePackage;
            EnemyPackage = enemyPackage;
            HardDeadlineSeconds = hardDeadlineSeconds;
            MinIntel = minIntel;
            CanonicalSeed = canonicalSeed;
            DisplayNameKey = displayNameKey;
            BriefKey = briefKey;
            DebriefKey = debriefKey;
            RequiredFeatureIds = requiredFeatureIds ?? Array.Empty<string>();
        }

        public int SchemaVersion { get; }
        public string MissionId { get; }
        public string ScenarioId { get; }
        public string OperationMapId { get; }
        public string DistrictId { get; }
        public int LocalSlot { get; }
        public OperationsMissionFamilyKind Family { get; }
        public OperationsForcePackageKind ForcePackage { get; }
        public OperationsEnemyPackageKind EnemyPackage { get; }
        public int HardDeadlineSeconds { get; }
        public int MinIntel { get; }
        public int CanonicalSeed { get; }
        public string DisplayNameKey { get; }
        public string BriefKey { get; }
        public string DebriefKey { get; }
        public string[] RequiredFeatureIds { get; }

        public bool TryValidate(out string error)
        {
            if (!OperationsIdentityRules.TryParseMissionNumber(MissionId, out int missionNumber) ||
                !OperationsIdentityRules.TryParseDistrictNumber(DistrictId, out int districtNumber))
            {
                error = "Mission definition IDs are not parseable.";
                return false;
            }

            if (!string.Equals(ScenarioId, OperationsIdentityRules.ScenarioId(missionNumber), StringComparison.Ordinal))
            {
                error = $"Mission '{MissionId}' scenario '{ScenarioId}' does not match ID grammar.";
                return false;
            }

            if (!string.Equals(OperationMapId, OperationsIdentityRules.MapIdForDistrict(districtNumber), StringComparison.Ordinal))
            {
                error = $"Mission '{MissionId}' map '{OperationMapId}' does not match district '{DistrictId}'.";
                return false;
            }

            int expectedSlot = ((missionNumber - 1) % 10) + 1;
            if (LocalSlot != expectedSlot)
            {
                error = $"Mission '{MissionId}' local slot {LocalSlot} does not match catalog order.";
                return false;
            }

            if (CanonicalSeed != 1101 + missionNumber)
            {
                error = $"Mission '{MissionId}' canonical seed must be 1101 + mission number.";
                return false;
            }

            for (int index = 0; index < RequiredFeatureIds.Length; index++)
            {
                if (!OperationsIdentityRules.IsValidFeatureId(RequiredFeatureIds[index]))
                {
                    error = $"Mission '{MissionId}' has invalid required feature '{RequiredFeatureIds[index]}'.";
                    return false;
                }
            }

            error = null;
            return true;
        }
    }

    public readonly struct OperationsMissionCatalogSchema
    {
        public OperationsMissionCatalogSchema(int schemaVersion, OperationsMissionDefinitionSchema[] missions)
        {
            if (schemaVersion < 1)
                throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            SchemaVersion = schemaVersion;
            Missions = missions ?? Array.Empty<OperationsMissionDefinitionSchema>();
        }

        public int SchemaVersion { get; }
        public OperationsMissionDefinitionSchema[] Missions { get; }

        public bool TryValidate(out string error)
        {
            if (Missions.Length != OperationsIdentityRules.MissionCount)
            {
                error = $"Operations catalog must contain exactly {OperationsIdentityRules.MissionCount} missions.";
                return false;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var families = new HashSet<OperationsMissionFamilyKind>();
            int[] districtCounts = new int[OperationsIdentityRules.DistrictCount];
            for (int index = 0; index < Missions.Length; index++)
            {
                OperationsMissionDefinitionSchema mission = Missions[index];
                if (!mission.TryValidate(out error))
                    return false;
                if (!ids.Add(mission.MissionId))
                {
                    error = $"Duplicate mission '{mission.MissionId}'.";
                    return false;
                }

                families.Add(mission.Family);
                if (!OperationsIdentityRules.TryParseDistrictNumber(mission.DistrictId, out int districtNumber))
                {
                    error = $"Mission '{mission.MissionId}' district is invalid.";
                    return false;
                }

                districtCounts[districtNumber - 1]++;
            }

            for (int district = 0; district < districtCounts.Length; district++)
            {
                if (districtCounts[district] != 10)
                {
                    error = $"District d{(district + 1):00} must contain exactly ten missions.";
                    return false;
                }
            }

            if (families.Count != 12)
            {
                error = "Catalog must cover all twelve mission families.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
