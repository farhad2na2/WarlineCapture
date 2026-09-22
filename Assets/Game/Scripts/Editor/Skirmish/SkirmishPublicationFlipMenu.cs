#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Game.Configs;
using Game.Skirmish.Contracts;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishPublicationFlipMenu
    {
        private const string ManifestPath = "Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishPublicationManifest.asset";

        [MenuItem("Tools/Warline/Skirmish/Flip S002 Playable If Evidence Ready")]
        public static void FlipMenu() => Debug.Log(TryWriteS002Playable(true));

        public static void RunFocusedFlipDryRun() => Debug.Log(EvaluateS002PlayableFlip());

        public static string EvaluateS002PlayableFlip() => TryWriteS002Playable(false);

        public static string TryWriteS002Playable(bool confirmWrite)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            if (!SkirmishSetupMatrixTable.TryLoad(root, out List<SkirmishSetupMatrixRow> matrix, out string error))
                throw new InvalidOperationException(error);

            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            if (!authored.Publication.TryGet("S002", out SkirmishPublicationRowConfig row))
                throw new InvalidOperationException("S002 publication row is missing.");
            if (!SkirmishAcceptanceCensusCapture.TryCaptureRegularStandard(
                    authored,
                    matrix,
                    out SkirmishAcceptanceCensus census,
                    out List<SkirmishCompileReason> censusReasons))
                throw new InvalidOperationException(censusReasons == null || censusReasons.Count == 0
                    ? "census failed"
                    : censusReasons[0].ToString());

            var setup = new SkirmishResolvedSetup
            {
                CatalogId = census.CatalogId,
                DefinitionId = census.DefinitionId,
                ContentHash = census.ContentHash,
                SetupHash = census.SetupHash,
                DifficultyId = SkirmishDifficultyId.Regular,
                SizeId = SkirmishSizeId.Standard
            };
            var request = new SkirmishPlayableFlipRequest
            {
                CatalogId = census.CatalogId,
                DefinitionId = census.DefinitionId,
                ContentHash = census.ContentHash,
                SetupHash = census.SetupHash,
                DifficultyId = SkirmishDifficultyId.Regular,
                SizeId = SkirmishSizeId.Standard,
                EvidenceDirectory = SkirmishAcceptanceScaffold.ResolveEvidenceDirectory(root, File.Exists),
                RequiredRelativeFiles = SkirmishAcceptanceScaffold.RequiredGameViewEvidenceFiles,
                ConfirmWrite = confirmWrite
            };

            if (!SkirmishPublicationValidator.TryEvaluatePlayableFlip(
                    row,
                    authored.DefinitionS002,
                    setup,
                    in request,
                    File.Exists,
                    out SkirmishPublicationStatus decision,
                    out List<SkirmishCompileReason> reasons))
                throw new InvalidOperationException(reasons == null || reasons.Count == 0
                    ? "flip rejected"
                    : reasons[0].ToString());

            if (decision != SkirmishPublicationStatus.Playable)
            {
                return "[SkirmishPublicationFlip] result=Failed catalog=S002 playable=0 confirm=" +
                       (confirmWrite ? 1 : 0) +
                       " reason=" +
                       (reasons.Count == 0 ? "missing evidence" : reasons[0].ToString());
            }

            if (!confirmWrite)
            {
                return "[SkirmishPublicationFlip] result=Passed catalog=S002 wouldFlip=1 playable=0 confirm=0";
            }

            SkirmishPublicationConfig asset = AssetDatabase.LoadAssetAtPath<SkirmishPublicationConfig>(ManifestPath);
            if (asset == null)
                throw new InvalidOperationException("Publication manifest asset is missing.");
            if (!asset.TrySetStatus(
                    "S002",
                    SkirmishPublicationStatus.Playable,
                    "SK-13 Programmer 1 Game View evidence + matching hashes. Counted ARIA/device matrix still open."))
                throw new InvalidOperationException("Could not write S002 publication status.");
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return "[SkirmishPublicationFlip] result=Passed catalog=S002 playable=1 confirm=1";
        }

        [MenuItem("Tools/Warline/Skirmish/Flip S003 Playable If Evidence Ready")]
        public static void FlipS003Menu() => RunFocusedFlipS003Confirm();

        public static void RunFocusedFlipS003DryRun() => Debug.Log(EvaluateS003PlayableFlip());

        public static void RunFocusedFlipS003Confirm() => Debug.Log(TryWriteS003Playable(true));

        public static string EvaluateS003PlayableFlip() => TryWriteS003Playable(false);

        public static string TryWriteS003Playable(bool confirmWrite)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            if (!SkirmishSetupMatrixTable.TryLoad(root, out List<SkirmishSetupMatrixRow> matrix, out string error))
                throw new InvalidOperationException(error);

            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            if (!authored.Publication.TryGet("S003", out SkirmishPublicationRowConfig row))
                throw new InvalidOperationException("S003 publication row is missing.");
            if (!SkirmishAcceptanceCensusCapture.TryCaptureS003RegularStandard(
                    authored,
                    matrix,
                    out SkirmishAcceptanceCensus census,
                    out List<SkirmishCompileReason> censusReasons))
                throw new InvalidOperationException(censusReasons == null || censusReasons.Count == 0
                    ? "census failed"
                    : censusReasons[0].ToString());
            if (census.CatalogId != SkirmishAcceptanceCensusCapture.S003CatalogId ||
                census.DefinitionId != SkirmishAcceptanceCensusCapture.S003DefinitionId ||
                census.Seed != SkirmishS003FirstVisit.SeedA ||
                census.PlayableMarked)
                throw new InvalidOperationException("S003 flip census must be Regular Standard seed 104732 and must not mark Playable.");

            var setup = new SkirmishResolvedSetup
            {
                CatalogId = census.CatalogId,
                DefinitionId = census.DefinitionId,
                ContentHash = census.ContentHash,
                SetupHash = census.SetupHash,
                DifficultyId = SkirmishDifficultyId.Regular,
                SizeId = SkirmishSizeId.Standard
            };
            var request = new SkirmishPlayableFlipRequest
            {
                CatalogId = census.CatalogId,
                DefinitionId = census.DefinitionId,
                ContentHash = census.ContentHash,
                SetupHash = census.SetupHash,
                DifficultyId = SkirmishDifficultyId.Regular,
                SizeId = SkirmishSizeId.Standard,
                EvidenceDirectory = SkirmishAcceptanceScaffold.ResolveS003EvidenceDirectory(root, File.Exists),
                RequiredRelativeFiles = SkirmishAcceptanceScaffold.S003RequiredGameViewEvidenceFiles,
                ConfirmWrite = confirmWrite
            };

            if (!SkirmishPublicationValidator.TryEvaluatePlayableFlip(
                    row,
                    authored.DefinitionS003,
                    setup,
                    in request,
                    File.Exists,
                    out SkirmishPublicationStatus decision,
                    out List<SkirmishCompileReason> reasons))
                throw new InvalidOperationException(reasons == null || reasons.Count == 0
                    ? "flip rejected"
                    : reasons[0].ToString());

            if (decision != SkirmishPublicationStatus.Playable)
            {
                return "[SkirmishPublicationFlip] result=Failed catalog=S003 playable=0 confirm=" +
                       (confirmWrite ? 1 : 0) +
                       " reason=" +
                       (reasons.Count == 0 ? "missing evidence" : reasons[0].ToString());
            }

            if (!confirmWrite)
            {
                return "[SkirmishPublicationFlip] result=Passed catalog=S003 wouldFlip=1 playable=0 confirm=0";
            }

            SkirmishPublicationConfig asset = AssetDatabase.LoadAssetAtPath<SkirmishPublicationConfig>(ManifestPath);
            if (asset == null)
                throw new InvalidOperationException("Publication manifest asset is missing.");
            if (!asset.TryGet("S002", out SkirmishPublicationRowConfig s002Before))
                throw new InvalidOperationException("S002 publication row is missing.");
            if (!asset.TryGet("S003", out SkirmishPublicationRowConfig s003Before))
                throw new InvalidOperationException("S003 publication row is missing on the manifest asset.");

            if (!asset.TrySetStatus(
                    "S003",
                    SkirmishPublicationStatus.Playable,
                    "S003 Game View evidence + matching Regular Standard hashes. Air flight/refuel and counted ARIA matrix still open."))
                throw new InvalidOperationException("Could not write S003 publication status.");
            if (!asset.TryGet("S002", out SkirmishPublicationRowConfig s002After) ||
                s002After.Status != s002Before.Status)
            {
                asset.TrySetStatus("S003", s003Before.Status, s003Before.Notes);
                asset.TrySetStatus("S002", s002Before.Status, s002Before.Notes);
                throw new InvalidOperationException("S003 flip must not change S002 publication status.");
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return "[SkirmishPublicationFlip] result=Passed catalog=S003 playable=1 confirm=1";
        }

        [MenuItem("Tools/Warline/Skirmish/Flip S004 Playable If Evidence Ready")]
        public static void FlipS004Menu() => RunFocusedFlipS004Confirm();

        public static void RunFocusedFlipS004DryRun() => Debug.Log(EvaluateS004PlayableFlip());

        public static void RunFocusedFlipS004Confirm() => Debug.Log(TryWriteS004Playable(true));

        public static string EvaluateS004PlayableFlip() => TryWriteS004Playable(false);

        public static string TryWriteS004Playable(bool confirmWrite)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            if (!SkirmishSetupMatrixTable.TryLoad(root, out List<SkirmishSetupMatrixRow> matrix, out string error))
                throw new InvalidOperationException(error);

            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            if (!authored.Publication.TryGet("S004", out SkirmishPublicationRowConfig row))
                throw new InvalidOperationException("S004 publication row is missing.");
            if (!SkirmishAcceptanceCensusCapture.TryCaptureS004RegularStandard(
                    authored,
                    matrix,
                    out SkirmishAcceptanceCensus census,
                    out List<SkirmishCompileReason> censusReasons))
                throw new InvalidOperationException(censusReasons == null || censusReasons.Count == 0
                    ? "census failed"
                    : censusReasons[0].ToString());
            if (census.CatalogId != SkirmishAcceptanceCensusCapture.S004CatalogId ||
                census.DefinitionId != SkirmishAcceptanceCensusCapture.S004DefinitionId ||
                census.Seed != SkirmishS004FirstVisit.SeedA ||
                census.PlayableMarked)
                throw new InvalidOperationException("S004 flip census must be Regular Standard seed 104733 and must not mark Playable.");

            var setup = new SkirmishResolvedSetup
            {
                CatalogId = census.CatalogId,
                DefinitionId = census.DefinitionId,
                ContentHash = census.ContentHash,
                SetupHash = census.SetupHash,
                DifficultyId = SkirmishDifficultyId.Regular,
                SizeId = SkirmishSizeId.Standard
            };
            var request = new SkirmishPlayableFlipRequest
            {
                CatalogId = census.CatalogId,
                DefinitionId = census.DefinitionId,
                ContentHash = census.ContentHash,
                SetupHash = census.SetupHash,
                DifficultyId = SkirmishDifficultyId.Regular,
                SizeId = SkirmishSizeId.Standard,
                EvidenceDirectory = SkirmishAcceptanceScaffold.ResolveS004EvidenceDirectory(root, File.Exists),
                RequiredRelativeFiles = SkirmishAcceptanceScaffold.S004RequiredGameViewEvidenceFiles,
                ConfirmWrite = confirmWrite
            };

            if (!SkirmishPublicationValidator.TryEvaluatePlayableFlip(
                    row,
                    authored.DefinitionS004,
                    setup,
                    in request,
                    File.Exists,
                    out SkirmishPublicationStatus decision,
                    out List<SkirmishCompileReason> reasons))
                throw new InvalidOperationException(reasons == null || reasons.Count == 0
                    ? "flip rejected"
                    : reasons[0].ToString());

            if (decision != SkirmishPublicationStatus.Playable)
            {
                return "[SkirmishPublicationFlip] result=Failed catalog=S004 playable=0 confirm=" +
                       (confirmWrite ? 1 : 0) +
                       " reason=" +
                       (reasons.Count == 0 ? "missing evidence" : reasons[0].ToString());
            }

            if (!confirmWrite)
            {
                return "[SkirmishPublicationFlip] result=Passed catalog=S004 wouldFlip=1 playable=0 confirm=0";
            }

            SkirmishPublicationConfig asset = AssetDatabase.LoadAssetAtPath<SkirmishPublicationConfig>(ManifestPath);
            if (asset == null)
                throw new InvalidOperationException("Publication manifest asset is missing.");
            if (!asset.TryGet("S002", out SkirmishPublicationRowConfig s002Before))
                throw new InvalidOperationException("S002 publication row is missing.");
            if (!asset.TryGet("S003", out SkirmishPublicationRowConfig s003Before))
                throw new InvalidOperationException("S003 publication row is missing.");
            if (!asset.TryGet("S004", out SkirmishPublicationRowConfig s004Before))
                throw new InvalidOperationException("S004 publication row is missing on the manifest asset.");

            if (!asset.TrySetStatus(
                    "S004",
                    SkirmishPublicationStatus.Playable,
                    "S004 Game View evidence + matching Regular Standard hashes. Established air flight/refuel and counted ARIA matrix still open."))
                throw new InvalidOperationException("Could not write S004 publication status.");
            if (!asset.TryGet("S002", out SkirmishPublicationRowConfig s002After) ||
                s002After.Status != s002Before.Status ||
                !asset.TryGet("S003", out SkirmishPublicationRowConfig s003After) ||
                s003After.Status != s003Before.Status)
            {
                asset.TrySetStatus("S004", s004Before.Status, s004Before.Notes);
                asset.TrySetStatus("S002", s002Before.Status, s002Before.Notes);
                asset.TrySetStatus("S003", s003Before.Status, s003Before.Notes);
                throw new InvalidOperationException("S004 flip must not change S002 or S003 publication status.");
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return "[SkirmishPublicationFlip] result=Passed catalog=S004 playable=1 confirm=1";
        }

        [MenuItem("Tools/Warline/Skirmish/Flip S005 Playable If Evidence Ready")]
        public static void FlipS005Menu() => RunFocusedFlipS005Confirm();

        public static void RunFocusedFlipS005DryRun() => Debug.Log(EvaluateS005PlayableFlip());

        public static void RunFocusedFlipS005Confirm() => Debug.Log(TryWriteS005Playable(true));

        public static string EvaluateS005PlayableFlip() => TryWriteS005Playable(false);

        public static string TryWriteS005Playable(bool confirmWrite)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            if (!SkirmishSetupMatrixTable.TryLoad(root, out List<SkirmishSetupMatrixRow> matrix, out string error))
                throw new InvalidOperationException(error);

            SkirmishExpansionAuthoredSet authored = SkirmishExpansionCatalogFactory.CreateInMemory();
            if (!authored.Publication.TryGet("S005", out SkirmishPublicationRowConfig row))
                throw new InvalidOperationException("S005 publication row is missing.");
            if (!SkirmishAcceptanceCensusCapture.TryCaptureS005RegularStandard(
                    authored,
                    matrix,
                    out SkirmishAcceptanceCensus census,
                    out List<SkirmishCompileReason> censusReasons))
                throw new InvalidOperationException(censusReasons == null || censusReasons.Count == 0
                    ? "census failed"
                    : censusReasons[0].ToString());
            if (census.CatalogId != SkirmishAcceptanceCensusCapture.S005CatalogId ||
                census.DefinitionId != SkirmishAcceptanceCensusCapture.S005DefinitionId ||
                census.Seed != SkirmishS005FirstVisit.SeedA ||
                census.PlayableMarked)
                throw new InvalidOperationException("S005 flip census must be Regular Standard seed 104734 and must not mark Playable.");

            var setup = new SkirmishResolvedSetup
            {
                CatalogId = census.CatalogId,
                DefinitionId = census.DefinitionId,
                ContentHash = census.ContentHash,
                SetupHash = census.SetupHash,
                DifficultyId = SkirmishDifficultyId.Regular,
                SizeId = SkirmishSizeId.Standard
            };
            var request = new SkirmishPlayableFlipRequest
            {
                CatalogId = census.CatalogId,
                DefinitionId = census.DefinitionId,
                ContentHash = census.ContentHash,
                SetupHash = census.SetupHash,
                DifficultyId = SkirmishDifficultyId.Regular,
                SizeId = SkirmishSizeId.Standard,
                EvidenceDirectory = SkirmishAcceptanceScaffold.ResolveS005EvidenceDirectory(root, File.Exists),
                RequiredRelativeFiles = SkirmishAcceptanceScaffold.S005RequiredGameViewEvidenceFiles,
                ConfirmWrite = confirmWrite
            };

            if (!SkirmishPublicationValidator.TryEvaluatePlayableFlip(
                    row,
                    authored.DefinitionS005,
                    setup,
                    in request,
                    File.Exists,
                    out SkirmishPublicationStatus decision,
                    out List<SkirmishCompileReason> reasons))
                throw new InvalidOperationException(reasons == null || reasons.Count == 0
                    ? "flip rejected"
                    : reasons[0].ToString());

            if (decision != SkirmishPublicationStatus.Playable)
            {
                return "[SkirmishPublicationFlip] result=Failed catalog=S005 playable=0 confirm=" +
                       (confirmWrite ? 1 : 0) +
                       " reason=" +
                       (reasons.Count == 0 ? "missing evidence" : reasons[0].ToString());
            }

            if (!confirmWrite)
            {
                return "[SkirmishPublicationFlip] result=Passed catalog=S005 wouldFlip=1 playable=0 confirm=0";
            }

            SkirmishPublicationConfig asset = AssetDatabase.LoadAssetAtPath<SkirmishPublicationConfig>(ManifestPath);
            if (asset == null)
                throw new InvalidOperationException("Publication manifest asset is missing.");
            if (!asset.TryGet("S002", out SkirmishPublicationRowConfig s002Before))
                throw new InvalidOperationException("S002 publication row is missing.");
            if (!asset.TryGet("S003", out SkirmishPublicationRowConfig s003Before))
                throw new InvalidOperationException("S003 publication row is missing.");
            if (!asset.TryGet("S004", out SkirmishPublicationRowConfig s004Before))
                throw new InvalidOperationException("S004 publication row is missing.");
            if (!asset.TryGet("S005", out SkirmishPublicationRowConfig s005Before))
                throw new InvalidOperationException("S005 publication row is missing on the manifest asset.");

            if (!asset.TrySetStatus(
                    "S005",
                    SkirmishPublicationStatus.Playable,
                    "S005 Game View evidence + matching Regular Standard hashes. Combined Arms flight/refuel and counted ARIA matrix still open."))
                throw new InvalidOperationException("Could not write S005 publication status.");
            if (!asset.TryGet("S002", out SkirmishPublicationRowConfig s002After) ||
                s002After.Status != s002Before.Status ||
                !asset.TryGet("S003", out SkirmishPublicationRowConfig s003After) ||
                s003After.Status != s003Before.Status ||
                !asset.TryGet("S004", out SkirmishPublicationRowConfig s004After) ||
                s004After.Status != s004Before.Status)
            {
                asset.TrySetStatus("S005", s005Before.Status, s005Before.Notes);
                asset.TrySetStatus("S002", s002Before.Status, s002Before.Notes);
                asset.TrySetStatus("S003", s003Before.Status, s003Before.Notes);
                asset.TrySetStatus("S004", s004Before.Status, s004Before.Notes);
                throw new InvalidOperationException("S005 flip must not change S002, S003, or S004 publication status.");
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return "[SkirmishPublicationFlip] result=Passed catalog=S005 playable=1 confirm=1";
        }
    }
}
#endif
