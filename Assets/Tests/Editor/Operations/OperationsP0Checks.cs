using System;
using System.Collections.Generic;
using System.IO;
using Game.Operations.Contracts;

namespace Game.Tests.Editor.Operations
{
    public static class OperationsP0Checks
    {
        public const int ExpectedCheckCount = 12;
        public const string PassMarker = "[OperationsP0Validation] result=Passed checks=12";

        public static void RunAll()
        {
            IdentityAcceptsCanonicalValues();
            IdentityRejectsLegacyAndMalformedValues();
            LaunchAndCommandContractsFailClosed();
            CatalogSchemaMatchesSixtyMissions();
            ObjectiveGraphRejectsCycles();
            ForceAndActionSchemasMatchStrategicRules();
            MigrationFixturesCoverEmptyUnknownAndCurrent();
            RosterLedgerDoesNotInventExistingTypes();
            DashboardOrdinalsMatchVerifiedUiEnum();
            AssemblyManifestForbidsSharedEdits();
            SharedIdentityStillRejectsOperationsNamespace();
            HostFilesStayInsideOperationsOwnership();
        }

        public static void IdentityAcceptsCanonicalValues()
        {
            Require(OperationsIdentityRules.IsValidMissionId("operation.o001"));
            Require(OperationsIdentityRules.IsValidMissionId("operation.o060"));
            Require(OperationsIdentityRules.IsValidScenarioId("scenario.operations.o001"));
            Require(OperationsIdentityRules.IsValidOperationMapId("opmap.operations.old_quarter"));
            Require(OperationsIdentityRules.IsValidDistrictId("district.operations.d01"));
            Require(OperationsIdentityRules.IsValidActionId("action.operations.analyze"));
            Require(OperationsIdentityRules.IsValidAnchorId("anchor.operations.d01.clinic"));
            Require(OperationsIdentityRules.IsValidRouteId("route.main"));
            Require(OperationsIdentityRules.IsValidGeneratedId("run.operations.a1b2c3d4", "run"));
        }

        public static void IdentityRejectsLegacyAndMalformedValues()
        {
            Require(!OperationsIdentityRules.IsValidMissionId("saga.ch01.m01.first_contact"));
            Require(!OperationsIdentityRules.IsValidScenarioId("scenario.ch01.m01.first_contact"));
            Require(!OperationsIdentityRules.IsValidScenarioId("scenario.skirmish.desert_base_standard"));
            Require(!OperationsIdentityRules.IsValidOperationMapId("opmap.skirmish.desert_base_01"));
            Require(!OperationsIdentityRules.IsValidOperationMapId("opmap.ch01.district_edge_01"));
            Require(!OperationsIdentityRules.IsValidMissionId("operation.o000"));
            Require(!OperationsIdentityRules.IsValidMissionId("operation.o061"));
            Require(!OperationsIdentityRules.IsValidDistrictId("district.operations.d07"));
            Require(!OperationsIdentityRules.IsValidActionId("action.operations.raid"));
        }

        public static void LaunchAndCommandContractsFailClosed()
        {
            OperationsLaunchPayload payload = CreateLaunch();
            Require(payload == CreateLaunch());
            RequireThrows<ArgumentException>(() => CreateLaunch(missionId: "saga.ch01.m01.first_contact"));
            RequireThrows<ArgumentOutOfRangeException>(() => CreateLaunch(seed: 0));
            OperationsCommand command = new("cmd.operations.abcd1234", 1, OperationsCommandKind.Deploy, "district.operations.d01", "offer.operations.abcd1234", string.Empty);
            OperationsCommandResult accepted = new("cmd.operations.abcd1234", true, OperationsReasonCode.None, 2, "txn.operations.abcd1234");
            Require(accepted.Accepted);
            RequireThrows<ArgumentOutOfRangeException>(() =>
                new OperationsCommandResult("cmd.operations.abcd1234", false, OperationsReasonCode.None, 1, string.Empty));
            Require(command.Kind == OperationsCommandKind.Deploy);
        }

        public static void CatalogSchemaMatchesSixtyMissions()
        {
            Require(OperationsCatalogIndex.Entries.Length == 60);
            OperationsMissionCatalogSchema catalog = OperationsCatalogIndex.CreateDefinitionCatalog();
            Require(catalog.TryValidate(out string error), error);
            Require(OperationsIdentityRules.IsValidMissionId(OperationsCatalogIndex.Entries[0].MissionId));
            Require(OperationsCatalogIndex.Entries[0].CanonicalSeed == 1102);
            Require(OperationsCatalogIndex.Entries[59].MissionId == "operation.o060");
        }

        public static void ObjectiveGraphRejectsCycles()
        {
            OperationsObjectiveGraphSchema valid = new(
                "graph.operations.o001",
                1,
                new[]
                {
                    Node("scan_sites", OperationsObjectiveRuleKind.Scan, Array.Empty<string>(), OperationsActivationPolicyKind.Launch),
                    Node("extract", OperationsObjectiveRuleKind.Extract, new[] { "scan_sites" }, OperationsActivationPolicyKind.Prerequisites)
                });
            Require(valid.TryValidate(out string validError), validError);

            OperationsObjectiveGraphSchema cyclic = new(
                "graph.operations.bad",
                1,
                new[]
                {
                    Node("a", OperationsObjectiveRuleKind.Scan, new[] { "b" }, OperationsActivationPolicyKind.Prerequisites),
                    Node("b", OperationsObjectiveRuleKind.Extract, new[] { "a" }, OperationsActivationPolicyKind.Prerequisites)
                });
            Require(!cyclic.TryValidate(out _));
        }

        public static void ForceAndActionSchemasMatchStrategicRules()
        {
            Require(OperationsForcePackageSchema.Create(OperationsForcePackageKind.Light).EntityCount == 16);
            Require(OperationsForcePackageSchema.Create(OperationsForcePackageKind.Service).EntityCount == 22);
            Require(OperationsForcePackageSchema.Create(OperationsForcePackageKind.Ground).EntityCount == 36);
            Require(OperationsForcePackageSchema.Create(OperationsForcePackageKind.Air).EntityCount == 22);
            Require(OperationsForcePackageSchema.Create(OperationsForcePackageKind.Combined).EntityCount == 40);
            Require(OperationsThreatProfileSchema.Create(OperationsEnemyPackageKind.Cell).EntityCount == 20);
            Require(OperationsThreatProfileSchema.Create(OperationsEnemyPackageKind.Finale).EntityCount == 44);
            OperationsActionSchema[] actions = OperationsActionSchema.CreateBaseline();
            Require(actions.Length == 6);
            Require(OperationsCampaignSchema.CreateBaseline().TryValidate(out string error), error);
            OperationsDistrictMetricTuple recon = OperationsDistrictMetricTuple.VictoryDelta(OperationsMissionFamilyKind.Recon);
            Require(recon.IntelConfidence == 18);
        }

        public static void MigrationFixturesCoverEmptyUnknownAndCurrent()
        {
            OperationsSaveMigrationResult missing = OperationsSaveMigration.Migrate(null);
            Require(missing.Disposition == OperationsSaveDispositionKind.Empty);
            Require(missing.CanWrite);
            Require(!OperationsSaveMigration.HasActiveRun(missing.Data));

            OperationsSaveMigrationResult unknown = OperationsSaveMigration.Migrate(new OperationsSaveData
            {
                schemaVersion = OperationsIdentityRules.CurrentSchemaVersion + 1,
                profileRevision = 9
            });
            Require(unknown.Disposition == OperationsSaveDispositionKind.ReadOnlyUnknown);
            Require(!unknown.CanWrite);
            Require(unknown.Data.profileRevision == 9);

            OperationsSaveMigrationResult current = OperationsSaveMigration.Migrate(new OperationsSaveData
            {
                schemaVersion = OperationsIdentityRules.CurrentSchemaVersion,
                activeRun = new OperationsRunSaveData
                {
                    runId = "run.operations.abcd1234",
                    phase = OperationsRunPhaseKind.Dashboard,
                    seed = 1102
                }
            });
            Require(current.Disposition == OperationsSaveDispositionKind.Current);
            Require(OperationsSaveMigration.HasActiveRun(current.Data));

            OperationsSaveMigrationResult legacy = OperationsSaveMigration.Migrate(new OperationsSaveData { schemaVersion = 0 });
            Require(legacy.Disposition == OperationsSaveDispositionKind.Migrated);
            Require(legacy.Data.schemaVersion == OperationsIdentityRules.CurrentSchemaVersion);
        }

        public static void RosterLedgerDoesNotInventExistingTypes()
        {
            Require(OperationsRosterLedger.Roles.Length == 12);
            Require(OperationsRosterLedger.TryGetRole(OperationsRosterRoleKind.RepairSpecialist, out OperationsRosterRoleRecord repair));
            Require(repair.Availability == OperationsAvailabilityKind.Missing);
            Require(OperationsRosterLedger.TryGetRole(OperationsRosterRoleKind.RifleInfantry, out OperationsRosterRoleRecord rifle));
            Require(rifle.Availability == OperationsAvailabilityKind.CampaignBound);
            Require(OperationsRosterLedger.CountResolvedRoles() >= 4);
            bool sawMissingIdNamespace = false;
            for (int index = 0; index < OperationsRosterLedger.Features.Length; index++)
            {
                if (OperationsRosterLedger.Features[index].FeatureId == "feature.operations.id_namespace")
                {
                    Require(OperationsRosterLedger.Features[index].Availability == OperationsAvailabilityKind.Missing);
                    sawMissingIdNamespace = true;
                }
            }

            Require(sawMissingIdNamespace);
        }

        public static void DashboardOrdinalsMatchVerifiedUiEnum()
        {
            Require((byte)OperationsDashboardActionKind.Patrol == 0);
            Require((byte)OperationsDashboardActionKind.DroneScan == 1);
            Require((byte)OperationsDashboardActionKind.Aid == 2);
            Require((byte)OperationsDashboardActionKind.Raid == 3);
            Require((byte)OperationsDashboardActionKind.Repair == 4);
            Require(OperationsDashboardActionMap.TryMapAbstract(OperationsDashboardActionKind.DroneScan, out OperationsAbstractActionKind analyze));
            Require(analyze == OperationsAbstractActionKind.Analyze);
            Require(OperationsDashboardActionMap.IsTacticalOfferAction(OperationsDashboardActionKind.Raid));
            Require(!OperationsDashboardActionMap.TryMapAbstract(OperationsDashboardActionKind.Raid, out _));
        }

        public static void AssemblyManifestForbidsSharedEdits()
        {
            Require(OperationsAssemblyManifest.IsContractsAssemblySelfContained);
            Require(OperationsAssemblyManifest.ForbiddenExistingAsmdefEdits.Length == 8);
            Require(OperationsAssemblyManifest.PendingGamePmReferences.Length >= 6);
        }

        public static void SharedIdentityStillRejectsOperationsNamespace()
        {
            Require(!OperationsIdentityRules.SharedValidatorCurrentlyAcceptsOperationsMapId);
            Require(!OperationsIdentityRules.SharedValidatorCurrentlyAcceptsOperationsScenarioId);
        }

        public static void HostFilesStayInsideOperationsOwnership()
        {
            string root = FindRepositoryRoot();
            foreach (string relative in OperationsAssemblyManifest.ForbiddenExistingAsmdefEdits)
                Require(File.Exists(Path.Combine(root, relative)), relative);
            foreach (string relative in OperationsAssemblyManifest.ForbiddenSharedSeams)
                Require(File.Exists(Path.Combine(root, relative)), relative);

            string contractsAsmdef = File.ReadAllText(
                Path.Combine(root, "Assets/Game/Scripts/Operations/Contracts/Game.Operations.Contracts.asmdef"));
            Require(contractsAsmdef.Contains("\"noEngineReferences\": true"));
            Require(contractsAsmdef.Contains("\"references\": []"));
            Require(!contractsAsmdef.Contains("Game.Configs"));
            Require(!contractsAsmdef.Contains("Game.Runtime"));

            string[] fixtures =
            {
                "Assets/Tests/Editor/Operations/Fixtures/operations_save_missing.json",
                "Assets/Tests/Editor/Operations/Fixtures/operations_save_legacy.json",
                "Assets/Tests/Editor/Operations/Fixtures/operations_save_current.json",
                "Assets/Tests/Editor/Operations/Fixtures/operations_save_unknown.json"
            };
            for (int index = 0; index < fixtures.Length; index++)
                Require(File.Exists(Path.Combine(root, fixtures[index])), fixtures[index]);
        }

        public static OperationsLaunchPayload CreateLaunch(
            string missionId = "operation.o001",
            int seed = 1102)
        {
            return new OperationsLaunchPayload(
                OperationsIdentityRules.CurrentSchemaVersion,
                "run.operations.abcd1234",
                "district.operations.d01",
                "offer.operations.abcd1234",
                missionId,
                "scenario.operations.o001",
                "opmap.operations.old_quarter",
                1,
                1,
                "contenthash0123456789abcdef0123456789abcdef01234567",
                OperationsDifficultyKind.Regular,
                seed,
                "txn.operations.abcd1234",
                "session.operations.abcd1234",
                0,
                "snapshothash0123456789abcdef0123456789abcdef012345",
                false);
        }

        private static OperationsObjectiveNodeSchema Node(
            string nodeId,
            OperationsObjectiveRuleKind rule,
            string[] prerequisites,
            OperationsActivationPolicyKind activation)
        {
            return new OperationsObjectiveNodeSchema(
                nodeId,
                rule,
                new[] { "role.friendly.command_squad" },
                1,
                0,
                0,
                8,
                prerequisites,
                activation,
                OperationsFailurePolicyKind.Mission,
                OperationsGraphJoinKind.AllOf,
                false,
                "operations.objective." + nodeId);
        }

        private static void Require(bool condition, string message = "Operations P0 check failed.")
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void RequireThrows<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static string FindRepositoryRoot()
        {
            var candidates = new List<string>
            {
#if UNITY_EDITOR
                Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..")),
#endif
                Directory.GetCurrentDirectory(),
                AppContext.BaseDirectory
            };

            for (int index = 0; index < candidates.Count; index++)
            {
                string directory = candidates[index];
                for (int depth = 0; depth < 12 && !string.IsNullOrEmpty(directory); depth++)
                {
                    if (File.Exists(Path.Combine(directory, "Design/Roadmap/Operations/MISSION_CATALOG.csv")))
                        return directory;
                    directory = Directory.GetParent(directory)?.FullName;
                }
            }

            throw new InvalidOperationException("Could not locate the WarlineCapture repository root.");
        }
    }
}
