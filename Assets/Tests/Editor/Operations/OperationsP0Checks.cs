using System;
using System.Collections.Generic;
using System.IO;
using Game.Operations.Contracts;

namespace Game.Tests.Editor.Operations
{
    public static class OperationsP0Checks
    {
        public const int ExpectedCheckCount = 13;
        public const string PassMarker = "[OperationsP0Validation] result=Passed checks=13";

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
            SharedIdentityAcceptsPublishedOperationsNamespace();
            HostFilesStayInsideOperationsOwnership();
            ShadowProjectIsIsolatedFromSharedCheckout();
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
            for (int roleIndex = 0; roleIndex < OperationsRosterLedger.Roles.Length; roleIndex++)
            {
                OperationsRosterRoleRecord roleRecord = OperationsRosterLedger.Roles[roleIndex];
                Require(!string.IsNullOrWhiteSpace(roleRecord.VerifiedTypeOrPath));
                Require(roleRecord.VerifiedTypeOrPath.Length <= OperationsIdentityRules.MaximumEvidenceLength);
                Require(!string.IsNullOrWhiteSpace(roleRecord.Notes));
                Require(roleRecord.Notes.Length <= OperationsIdentityRules.MaximumEvidenceLength);
            }
            bool sawMissingIdNamespace = false;
            for (int index = 0; index < OperationsRosterLedger.Features.Length; index++)
            {
                OperationsFeatureRecord feature = OperationsRosterLedger.Features[index];
                Require(!string.IsNullOrWhiteSpace(feature.VerifiedTypeOrPath));
                Require(feature.VerifiedTypeOrPath.Length <= OperationsIdentityRules.MaximumEvidenceLength);
                Require(!string.IsNullOrWhiteSpace(feature.Notes));
                Require(feature.Notes.Length <= OperationsIdentityRules.MaximumEvidenceLength);
                if (feature.FeatureId == "feature.operations.id_namespace")
                {
                    Require(feature.Availability == OperationsAvailabilityKind.Missing);
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

        public static void SharedIdentityAcceptsPublishedOperationsNamespace()
        {
            Require(OperationsIdentityRules.SharedValidatorCurrentlyAcceptsOperationsMapId);
            Require(OperationsIdentityRules.SharedValidatorCurrentlyAcceptsOperationsScenarioId);
            Require(OperationsIdentityRules.IsValidOperationMapId("opmap.operations.old_quarter"));
            Require(OperationsIdentityRules.IsValidScenarioId("scenario.operations.o001"));
            Require(!OperationsIdentityRules.IsValidOperationMapId("opmap.operations.unknown"));
            Require(!OperationsIdentityRules.IsValidScenarioId("scenario.operations.o000"));
            Require(!OperationsIdentityRules.IsValidScenarioId("scenario.operations.o061"));
            Require(!OperationsIdentityRules.IsValidOperationMapId("opmap.skirmish.desert_base_01"));
            Require(!OperationsIdentityRules.IsValidScenarioId("scenario.ch01.m01.first_contact"));
        }

        public static void HostFilesStayInsideOperationsOwnership()
        {
            string root = FindRepositoryRoot();
            foreach (string relative in OperationsAssemblyManifest.ForbiddenExistingAsmdefEdits)
                Require(File.Exists(CombineProjectPath(root, relative)), relative);
            foreach (string relative in OperationsAssemblyManifest.ForbiddenSharedSeams)
                Require(File.Exists(CombineProjectPath(root, relative)), relative);

            string contractsAsmdef = File.ReadAllText(
                CombineProjectPath(root, "Assets/Game/Scripts/Operations/Contracts/Game.Operations.Contracts.asmdef"));
            Require(contractsAsmdef.Contains("\"noEngineReferences\": true"));
            Require(contractsAsmdef.Contains("\"references\": []"));
            Require(!contractsAsmdef.Contains("Game.Configs"));
            Require(!contractsAsmdef.Contains("Game.Runtime"));

            string[] fixtures =
            {
                "Assets/Tests/Editor/Operations/Fixtures/operations_save_missing.json",
                "Assets/Tests/Editor/Operations/Fixtures/operations_save_legacy.json",
                "Assets/Tests/Editor/Operations/Fixtures/operations_save_current.json",
                "Assets/Tests/Editor/Operations/Fixtures/operations_save_unknown.json",
                "Tools/Operations/Ensure-OperationsShadowWorktree.ps1",
                "Tools/Operations/Invoke-OperationsP0Validation.ps1",
                "Design/Roadmap/Operations/P0_SHADOW_PROJECT.md"
            };
            for (int index = 0; index < fixtures.Length; index++)
                Require(File.Exists(CombineProjectPath(root, fixtures[index])), fixtures[index]);
        }

        public static void ShadowProjectIsIsolatedFromSharedCheckout()
        {
            Require(OperationsShadowProject.SharedWindowsCheckout == @"D:\Projects\WarlineCapture");
            Require(OperationsShadowProject.ShadowWindowsCheckout == @"D:\Projects\WarlineCapture-Operations");
            Require(OperationsShadowProject.IsSharedWindowsCheckout(@"D:\Projects\WarlineCapture"));
            Require(OperationsShadowProject.IsSharedWindowsCheckout(@"D:/Projects/WarlineCapture/Library"));
            Require(!OperationsShadowProject.IsSharedWindowsCheckout(@"D:\Projects\WarlineCapture-Operations"));
            Require(!OperationsShadowProject.IsSharedWindowsCheckout(@"D:\Projects\WarlineCapture-Operations\Library"));
            Require(OperationsShadowProject.IsShadowWindowsCheckout(@"D:\Projects\WarlineCapture-Operations"));
            Require(OperationsShadowProject.IsApprovedWindowsValidationPath(@"D:\Projects\WarlineCapture-Operations"));
            Require(!OperationsShadowProject.IsApprovedWindowsValidationPath(@"D:\Projects\WarlineCapture"));
            Require(OperationsShadowProject.TryRejectSharedWindowsCheckout(@"D:\Projects\WarlineCapture", out string error));
            Require(error.IndexOf("WarlineCapture-Operations", StringComparison.Ordinal) >= 0);
            ProveFindRepositoryRootAcceptsShadowFolderName();

#if UNITY_EDITOR
            string unityAssets = Path.GetFullPath(UnityEngine.Application.dataPath);
            string unityRoot = Path.GetDirectoryName(unityAssets);
            if (!string.IsNullOrEmpty(unityRoot) &&
                OperationsShadowProject.TryRejectSharedWindowsCheckout(unityRoot, out string unityError))
                throw new InvalidOperationException(unityError);
#endif
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

        public static string CombineProjectPath(string root, string relativeUnixPath)
        {
            if (string.IsNullOrEmpty(root))
                throw new ArgumentException("A project root is required.", nameof(root));

            string path = root;
            if (string.IsNullOrEmpty(relativeUnixPath))
                return path;

            string[] parts = relativeUnixPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            for (int index = 0; index < parts.Length; index++)
            {
                if (parts[index] == "." || parts[index] == "..")
                    throw new ArgumentException("Relative project paths must not contain '.' or '..'.", nameof(relativeUnixPath));
                path = Path.Combine(path, parts[index]);
            }

            return path;
        }

        public static bool IsOperationsProjectRoot(string directory)
        {
            if (string.IsNullOrEmpty(directory))
                return false;

            return File.Exists(CombineProjectPath(directory, "Assets/Game/Scripts/Operations/Contracts/Game.Operations.Contracts.asmdef"))
                || File.Exists(CombineProjectPath(directory, "Design/Roadmap/Operations/MISSION_CATALOG.csv"));
        }

        public static bool TryFindRepositoryRoot(IEnumerable<string> startPaths, out string root)
        {
            if (startPaths != null)
            {
                foreach (string startPath in startPaths)
                {
                    if (string.IsNullOrEmpty(startPath))
                        continue;

                    string directory;
                    try
                    {
                        directory = Path.GetFullPath(startPath);
                    }
                    catch (ArgumentException)
                    {
                        continue;
                    }
                    catch (NotSupportedException)
                    {
                        continue;
                    }

                    for (int depth = 0; depth < 16 && !string.IsNullOrEmpty(directory); depth++)
                    {
                        if (IsOperationsProjectRoot(directory))
                        {
                            root = directory;
                            return true;
                        }

                        DirectoryInfo parent = Directory.GetParent(directory);
                        directory = parent == null ? null : parent.FullName;
                    }
                }
            }

            root = null;
            return false;
        }

        private static string FindRepositoryRoot()
        {
            var candidates = new List<string>();
#if UNITY_EDITOR
            string dataPath = UnityEngine.Application.dataPath;
            if (!string.IsNullOrEmpty(dataPath))
            {
                string assets = Path.GetFullPath(dataPath);
                string project = Path.GetDirectoryName(assets);
                if (!string.IsNullOrEmpty(project))
                    candidates.Add(project);
                candidates.Add(assets);
            }
#endif
            if (Directory.Exists(OperationsShadowProject.ShadowWindowsCheckout))
                candidates.Add(OperationsShadowProject.ShadowWindowsCheckout);
            candidates.Add(Directory.GetCurrentDirectory());
            if (!string.IsNullOrEmpty(AppContext.BaseDirectory))
                candidates.Add(AppContext.BaseDirectory);

            if (TryFindRepositoryRoot(candidates, out string root))
                return root;

            throw new InvalidOperationException(
                "Could not locate the Operations repository root. Open the shadow project " +
                OperationsShadowProject.ShadowWindowsCheckout +
                ". A folder named WarlineCapture-Operations is a valid project root.");
        }

        private static void ProveFindRepositoryRootAcceptsShadowFolderName()
        {
            string workspace = Path.Combine(Path.GetTempPath(), "ops-p0-root-" + Guid.NewGuid().ToString("N"));
            string shadow = Path.Combine(workspace, "WarlineCapture-Operations");
            string shared = Path.Combine(workspace, "WarlineCapture");
            try
            {
                WriteMinimalProjectMarkers(shadow);
                WriteMinimalProjectMarkers(shared);
                Require(Path.GetFileName(shadow) == "WarlineCapture-Operations");
                Require(Path.GetFileName(shadow) != "WarlineCapture");
                Require(IsOperationsProjectRoot(shadow));
                Require(IsOperationsProjectRoot(shared));

                string startInsideShadow = CombineProjectPath(shadow, "Assets/Game/Scripts/Operations/Contracts");
                Require(TryFindRepositoryRoot(new[] { startInsideShadow }, out string foundShadow));
                Require(string.Equals(Path.GetFullPath(foundShadow), Path.GetFullPath(shadow), StringComparison.OrdinalIgnoreCase));

                string startInsideShared = CombineProjectPath(shared, "Assets");
                Require(TryFindRepositoryRoot(new[] { startInsideShared }, out string foundShared));
                Require(string.Equals(Path.GetFullPath(foundShared), Path.GetFullPath(shared), StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                if (Directory.Exists(workspace))
                    Directory.Delete(workspace, true);
            }
        }

        private static void WriteMinimalProjectMarkers(string root)
        {
            string asmdef = CombineProjectPath(root, "Assets/Game/Scripts/Operations/Contracts/Game.Operations.Contracts.asmdef");
            Directory.CreateDirectory(Path.GetDirectoryName(asmdef));
            File.WriteAllText(asmdef, "{}");
            string catalog = CombineProjectPath(root, "Design/Roadmap/Operations/MISSION_CATALOG.csv");
            Directory.CreateDirectory(Path.GetDirectoryName(catalog));
            File.WriteAllText(catalog, "mission_id\n");
        }
    }
}
