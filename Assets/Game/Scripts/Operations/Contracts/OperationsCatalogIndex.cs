using System;

namespace Game.Operations.Contracts
{
    public readonly struct OperationsCatalogEntry
    {
        public OperationsCatalogEntry(
            string missionId,
            int localSlot,
            OperationsMissionFamilyKind family,
            string scenarioId,
            string operationMapId,
            string districtId,
            string forcePackageToken,
            string enemyPackageToken,
            int hardDeadlineSeconds,
            int minIntel,
            int canonicalSeed)
        {
            MissionId = missionId;
            LocalSlot = localSlot;
            Family = family;
            ScenarioId = scenarioId;
            OperationMapId = operationMapId;
            DistrictId = districtId;
            ForcePackageToken = forcePackageToken;
            EnemyPackageToken = enemyPackageToken;
            HardDeadlineSeconds = hardDeadlineSeconds;
            MinIntel = minIntel;
            CanonicalSeed = canonicalSeed;
        }

        public string MissionId { get; }
        public int LocalSlot { get; }
        public OperationsMissionFamilyKind Family { get; }
        public string ScenarioId { get; }
        public string OperationMapId { get; }
        public string DistrictId { get; }
        public string ForcePackageToken { get; }
        public string EnemyPackageToken { get; }
        public int HardDeadlineSeconds { get; }
        public int MinIntel { get; }
        public int CanonicalSeed { get; }

        public OperationsMissionDefinitionSchema ToDefinitionSchema()
        {
            if (!OperationsForcePackageIds.TryParseCatalogToken(ForcePackageToken, out OperationsForcePackageKind force))
                throw new InvalidOperationException("Unknown force package " + ForcePackageToken);
            if (!OperationsForcePackageIds.TryParseEnemyToken(EnemyPackageToken, out OperationsEnemyPackageKind enemy))
                throw new InvalidOperationException("Unknown enemy package " + EnemyPackageToken);
            return new OperationsMissionDefinitionSchema(
                OperationsIdentityRules.CurrentSchemaVersion,
                MissionId,
                ScenarioId,
                OperationMapId,
                DistrictId,
                LocalSlot,
                Family,
                force,
                enemy,
                HardDeadlineSeconds,
                MinIntel,
                CanonicalSeed,
                "operations.mission." + MissionId.Replace('.', '_') + ".name",
                "operations.mission." + MissionId.Replace('.', '_') + ".brief",
                "operations.mission." + MissionId.Replace('.', '_') + ".debrief",
                OperationsRosterLedger.BaselineRequiredFeatureIds);
        }
    }

    public static class OperationsCatalogIndex
    {
        public static readonly OperationsCatalogEntry[] Entries =
        {
            new("operation.o001", 1, OperationsMissionFamilyKind.Recon, "scenario.operations.o001", "opmap.operations.old_quarter", "district.operations.d01", "FP_LIGHT", "EP_CELL", 720, 0, 1102),
            new("operation.o002", 2, OperationsMissionFamilyKind.Escort, "scenario.operations.o002", "opmap.operations.old_quarter", "district.operations.d01", "FP_SERVICE", "EP_RAIDERS", 840, 0, 1103),
            new("operation.o003", 3, OperationsMissionFamilyKind.Repair, "scenario.operations.o003", "opmap.operations.old_quarter", "district.operations.d01", "FP_SERVICE", "EP_RAIDERS", 900, 0, 1104),
            new("operation.o004", 4, OperationsMissionFamilyKind.Raid, "scenario.operations.o004", "opmap.operations.old_quarter", "district.operations.d01", "FP_LIGHT", "EP_CELL", 780, 40, 1105),
            new("operation.o005", 5, OperationsMissionFamilyKind.Rescue, "scenario.operations.o005", "opmap.operations.old_quarter", "district.operations.d01", "FP_SERVICE", "EP_RAIDERS", 900, 0, 1106),
            new("operation.o006", 6, OperationsMissionFamilyKind.Breach, "scenario.operations.o006", "opmap.operations.old_quarter", "district.operations.d01", "FP_GROUND", "EP_RAIDERS", 960, 40, 1107),
            new("operation.o007", 7, OperationsMissionFamilyKind.Patrol, "scenario.operations.o007", "opmap.operations.old_quarter", "district.operations.d01", "FP_LIGHT", "EP_CELL", 720, 0, 1108),
            new("operation.o008", 8, OperationsMissionFamilyKind.Interdict, "scenario.operations.o008", "opmap.operations.old_quarter", "district.operations.d01", "FP_GROUND", "EP_RAIDERS", 780, 0, 1109),
            new("operation.o009", 9, OperationsMissionFamilyKind.Defense, "scenario.operations.o009", "opmap.operations.old_quarter", "district.operations.d01", "FP_GROUND", "EP_RAIDERS", 900, 0, 1110),
            new("operation.o010", 10, OperationsMissionFamilyKind.Finale, "scenario.operations.o010", "opmap.operations.old_quarter", "district.operations.d01", "FP_COMBINED", "EP_FINALE", 1200, 0, 1111),
            new("operation.o011", 1, OperationsMissionFamilyKind.Recon, "scenario.operations.o011", "opmap.operations.civic_center", "district.operations.d02", "FP_LIGHT", "EP_CELL", 720, 0, 1112),
            new("operation.o012", 2, OperationsMissionFamilyKind.Patrol, "scenario.operations.o012", "opmap.operations.civic_center", "district.operations.d02", "FP_LIGHT", "EP_CELL", 780, 0, 1113),
            new("operation.o013", 3, OperationsMissionFamilyKind.Repair, "scenario.operations.o013", "opmap.operations.civic_center", "district.operations.d02", "FP_SERVICE", "EP_RAIDERS", 900, 0, 1114),
            new("operation.o014", 4, OperationsMissionFamilyKind.Escort, "scenario.operations.o014", "opmap.operations.civic_center", "district.operations.d02", "FP_SERVICE", "EP_RAIDERS", 840, 0, 1115),
            new("operation.o015", 5, OperationsMissionFamilyKind.Rescue, "scenario.operations.o015", "opmap.operations.civic_center", "district.operations.d02", "FP_SERVICE", "EP_RAIDERS", 900, 0, 1116),
            new("operation.o016", 6, OperationsMissionFamilyKind.Raid, "scenario.operations.o016", "opmap.operations.civic_center", "district.operations.d02", "FP_GROUND", "EP_RAIDERS", 960, 40, 1117),
            new("operation.o017", 7, OperationsMissionFamilyKind.Seize, "scenario.operations.o017", "opmap.operations.civic_center", "district.operations.d02", "FP_GROUND", "EP_MECHANIZED", 960, 0, 1118),
            new("operation.o018", 8, OperationsMissionFamilyKind.Airlift, "scenario.operations.o018", "opmap.operations.civic_center", "district.operations.d02", "FP_AIR", "EP_AIRFIELD", 1020, 0, 1119),
            new("operation.o019", 9, OperationsMissionFamilyKind.Defense, "scenario.operations.o019", "opmap.operations.civic_center", "district.operations.d02", "FP_GROUND", "EP_MECHANIZED", 960, 0, 1120),
            new("operation.o020", 10, OperationsMissionFamilyKind.Finale, "scenario.operations.o020", "opmap.operations.civic_center", "district.operations.d02", "FP_COMBINED", "EP_FINALE", 1200, 0, 1121),
            new("operation.o021", 1, OperationsMissionFamilyKind.Recon, "scenario.operations.o021", "opmap.operations.industrial_belt", "district.operations.d03", "FP_LIGHT", "EP_CELL", 780, 0, 1122),
            new("operation.o022", 2, OperationsMissionFamilyKind.Escort, "scenario.operations.o022", "opmap.operations.industrial_belt", "district.operations.d03", "FP_SERVICE", "EP_RAIDERS", 900, 0, 1123),
            new("operation.o023", 3, OperationsMissionFamilyKind.Repair, "scenario.operations.o023", "opmap.operations.industrial_belt", "district.operations.d03", "FP_SERVICE", "EP_RAIDERS", 1020, 0, 1124),
            new("operation.o024", 4, OperationsMissionFamilyKind.Rescue, "scenario.operations.o024", "opmap.operations.industrial_belt", "district.operations.d03", "FP_SERVICE", "EP_RAIDERS", 900, 0, 1125),
            new("operation.o025", 5, OperationsMissionFamilyKind.Interdict, "scenario.operations.o025", "opmap.operations.industrial_belt", "district.operations.d03", "FP_GROUND", "EP_MECHANIZED", 900, 0, 1126),
            new("operation.o026", 6, OperationsMissionFamilyKind.Breach, "scenario.operations.o026", "opmap.operations.industrial_belt", "district.operations.d03", "FP_GROUND", "EP_MECHANIZED", 1020, 40, 1127),
            new("operation.o027", 7, OperationsMissionFamilyKind.Seize, "scenario.operations.o027", "opmap.operations.industrial_belt", "district.operations.d03", "FP_GROUND", "EP_MECHANIZED", 960, 0, 1128),
            new("operation.o028", 8, OperationsMissionFamilyKind.Raid, "scenario.operations.o028", "opmap.operations.industrial_belt", "district.operations.d03", "FP_LIGHT", "EP_CELL", 840, 40, 1129),
            new("operation.o029", 9, OperationsMissionFamilyKind.Defense, "scenario.operations.o029", "opmap.operations.industrial_belt", "district.operations.d03", "FP_GROUND", "EP_MECHANIZED", 1020, 0, 1130),
            new("operation.o030", 10, OperationsMissionFamilyKind.Finale, "scenario.operations.o030", "opmap.operations.industrial_belt", "district.operations.d03", "FP_COMBINED", "EP_FINALE", 1200, 0, 1131),
            new("operation.o031", 1, OperationsMissionFamilyKind.Recon, "scenario.operations.o031", "opmap.operations.river_crossing", "district.operations.d04", "FP_LIGHT", "EP_CELL", 780, 0, 1132),
            new("operation.o032", 2, OperationsMissionFamilyKind.Rescue, "scenario.operations.o032", "opmap.operations.river_crossing", "district.operations.d04", "FP_SERVICE", "EP_RAIDERS", 960, 0, 1133),
            new("operation.o033", 3, OperationsMissionFamilyKind.Repair, "scenario.operations.o033", "opmap.operations.river_crossing", "district.operations.d04", "FP_SERVICE", "EP_RAIDERS", 960, 0, 1134),
            new("operation.o034", 4, OperationsMissionFamilyKind.Escort, "scenario.operations.o034", "opmap.operations.river_crossing", "district.operations.d04", "FP_SERVICE", "EP_RAIDERS", 960, 0, 1135),
            new("operation.o035", 5, OperationsMissionFamilyKind.Patrol, "scenario.operations.o035", "opmap.operations.river_crossing", "district.operations.d04", "FP_LIGHT", "EP_CELL", 840, 0, 1136),
            new("operation.o036", 6, OperationsMissionFamilyKind.Breach, "scenario.operations.o036", "opmap.operations.river_crossing", "district.operations.d04", "FP_GROUND", "EP_MECHANIZED", 1020, 40, 1137),
            new("operation.o037", 7, OperationsMissionFamilyKind.Seize, "scenario.operations.o037", "opmap.operations.river_crossing", "district.operations.d04", "FP_GROUND", "EP_MECHANIZED", 1020, 0, 1138),
            new("operation.o038", 8, OperationsMissionFamilyKind.Interdict, "scenario.operations.o038", "opmap.operations.river_crossing", "district.operations.d04", "FP_GROUND", "EP_MECHANIZED", 900, 0, 1139),
            new("operation.o039", 9, OperationsMissionFamilyKind.Escort, "scenario.operations.o039", "opmap.operations.river_crossing", "district.operations.d04", "FP_GROUND", "EP_MECHANIZED", 1020, 0, 1140),
            new("operation.o040", 10, OperationsMissionFamilyKind.Finale, "scenario.operations.o040", "opmap.operations.river_crossing", "district.operations.d04", "FP_COMBINED", "EP_FINALE", 1200, 0, 1141),
            new("operation.o041", 1, OperationsMissionFamilyKind.Recon, "scenario.operations.o041", "opmap.operations.highland_approach", "district.operations.d05", "FP_LIGHT", "EP_CELL", 840, 0, 1142),
            new("operation.o042", 2, OperationsMissionFamilyKind.Patrol, "scenario.operations.o042", "opmap.operations.highland_approach", "district.operations.d05", "FP_LIGHT", "EP_CELL", 900, 0, 1143),
            new("operation.o043", 3, OperationsMissionFamilyKind.Repair, "scenario.operations.o043", "opmap.operations.highland_approach", "district.operations.d05", "FP_SERVICE", "EP_RAIDERS", 1020, 0, 1144),
            new("operation.o044", 4, OperationsMissionFamilyKind.Escort, "scenario.operations.o044", "opmap.operations.highland_approach", "district.operations.d05", "FP_SERVICE", "EP_RAIDERS", 1020, 0, 1145),
            new("operation.o045", 5, OperationsMissionFamilyKind.Airlift, "scenario.operations.o045", "opmap.operations.highland_approach", "district.operations.d05", "FP_AIR", "EP_AIRFIELD", 1080, 0, 1146),
            new("operation.o046", 6, OperationsMissionFamilyKind.Breach, "scenario.operations.o046", "opmap.operations.highland_approach", "district.operations.d05", "FP_GROUND", "EP_MECHANIZED", 1080, 40, 1147),
            new("operation.o047", 7, OperationsMissionFamilyKind.Interdict, "scenario.operations.o047", "opmap.operations.highland_approach", "district.operations.d05", "FP_GROUND", "EP_MECHANIZED", 960, 0, 1148),
            new("operation.o048", 8, OperationsMissionFamilyKind.Seize, "scenario.operations.o048", "opmap.operations.highland_approach", "district.operations.d05", "FP_GROUND", "EP_MECHANIZED", 1020, 0, 1149),
            new("operation.o049", 9, OperationsMissionFamilyKind.Defense, "scenario.operations.o049", "opmap.operations.highland_approach", "district.operations.d05", "FP_GROUND", "EP_MECHANIZED", 1080, 0, 1150),
            new("operation.o050", 10, OperationsMissionFamilyKind.Finale, "scenario.operations.o050", "opmap.operations.highland_approach", "district.operations.d05", "FP_COMBINED", "EP_FINALE", 1200, 0, 1151),
            new("operation.o051", 1, OperationsMissionFamilyKind.Recon, "scenario.operations.o051", "opmap.operations.airport_perimeter", "district.operations.d06", "FP_LIGHT", "EP_CELL", 840, 0, 1152),
            new("operation.o052", 2, OperationsMissionFamilyKind.Rescue, "scenario.operations.o052", "opmap.operations.airport_perimeter", "district.operations.d06", "FP_SERVICE", "EP_RAIDERS", 1020, 0, 1153),
            new("operation.o053", 3, OperationsMissionFamilyKind.Repair, "scenario.operations.o053", "opmap.operations.airport_perimeter", "district.operations.d06", "FP_SERVICE", "EP_RAIDERS", 1020, 0, 1154),
            new("operation.o054", 4, OperationsMissionFamilyKind.Escort, "scenario.operations.o054", "opmap.operations.airport_perimeter", "district.operations.d06", "FP_SERVICE", "EP_RAIDERS", 1020, 0, 1155),
            new("operation.o055", 5, OperationsMissionFamilyKind.Raid, "scenario.operations.o055", "opmap.operations.airport_perimeter", "district.operations.d06", "FP_LIGHT", "EP_CELL", 900, 40, 1156),
            new("operation.o056", 6, OperationsMissionFamilyKind.Breach, "scenario.operations.o056", "opmap.operations.airport_perimeter", "district.operations.d06", "FP_GROUND", "EP_MECHANIZED", 1080, 40, 1157),
            new("operation.o057", 7, OperationsMissionFamilyKind.Seize, "scenario.operations.o057", "opmap.operations.airport_perimeter", "district.operations.d06", "FP_GROUND", "EP_AIRFIELD", 1020, 0, 1158),
            new("operation.o058", 8, OperationsMissionFamilyKind.Airlift, "scenario.operations.o058", "opmap.operations.airport_perimeter", "district.operations.d06", "FP_AIR", "EP_AIRFIELD", 1080, 0, 1159),
            new("operation.o059", 9, OperationsMissionFamilyKind.Defense, "scenario.operations.o059", "opmap.operations.airport_perimeter", "district.operations.d06", "FP_GROUND", "EP_MECHANIZED", 1080, 0, 1160),
            new("operation.o060", 10, OperationsMissionFamilyKind.Finale, "scenario.operations.o060", "opmap.operations.airport_perimeter", "district.operations.d06", "FP_COMBINED", "EP_FINALE", 1200, 0, 1161)
        };

        public static OperationsMissionCatalogSchema CreateDefinitionCatalog()
        {
            var missions = new OperationsMissionDefinitionSchema[Entries.Length];
            for (int index = 0; index < Entries.Length; index++)
                missions[index] = Entries[index].ToDefinitionSchema();
            return new OperationsMissionCatalogSchema(OperationsIdentityRules.CurrentSchemaVersion, missions);
        }
    }
}
