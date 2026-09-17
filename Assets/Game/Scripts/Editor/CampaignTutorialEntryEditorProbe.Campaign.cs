using System;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class CampaignTutorialEntryEditorProbe
    {
        private const string CampaignChain = "Warline.Readiness.CampaignChain";
        private static readonly int[] ContinuityChain = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 8, 5, 7, 9, 6 };
        private static readonly int[] RecoveryChain = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        private static int[] Chain => SessionState.GetBool(RecoveryJourney, false) ? RecoveryChain : ContinuityChain;
        private static int chainIndex, firstChainIndex;
        private static string settledProfile;
        private static string ProfileDirectory => Path.Combine(Output, SessionState.GetBool(CampaignChain, false)
            ? (entry < 5 ? "campaign-en" : "campaign-fa") : "profile");
        private static string CapturePrefix => SessionState.GetBool(CampaignChain, false)
            ? $"chain-{chainIndex + 1:D2}-m{entry % 5 + 1}-{(entry < 5 ? "en" : "fa")}" : (entry + 1).ToString();

        public static void RunCampaignCompletion()
        {
            RunJourney(0);
            chainIndex = firstChainIndex = 0;
            SessionState.SetBool(CampaignChain, true);
            Unity.Collections.NativeLeakDetection.Mode = Unity.Collections.NativeLeakDetectionMode.EnabledWithStackTrace;
        }

        public static void RunReadinessRecoveryCampaign()
        {
            RunCampaignCompletion();
            SessionState.SetBool(RecoveryJourney, true);
            MainMenuV3PrefabBuilder.SetGameViewResolution(1280, 720);
        }

        public static void ResumeReadinessRecoveryAtM03()
        {
            var arguments = Environment.GetCommandLineArgs();
            int argument = Array.IndexOf(arguments, "-readinessResumeFrom");
            if (argument < 0 || argument + 1 >= arguments.Length)
                throw new ArgumentException("Supply the actual previously settled campaign-en profile directory.");
            string previousProfile = Path.Combine(arguments[argument + 1], "profile.json");
            if (!File.Exists(previousProfile)) throw new FileNotFoundException("Previous campaign profile not found", previousProfile);
            RunReadinessRecoveryCampaign();
            entry = chainIndex = firstChainIndex = 2;
            Directory.CreateDirectory(ProfileDirectory);
            File.Copy(previousProfile, Path.Combine(ProfileDirectory, "profile.json"));
            Debug.Log("[CampaignContinuity] resumed-real-profile=" + previousProfile + "; M1/M2 completed in previous run; no unlocks or outcomes seeded");
        }

        public static void RunReadinessRecoveryFarsiCampaign()
        {
            RunReadinessRecoveryCampaign();
            entry = chainIndex = firstChainIndex = 5;
            MainMenuV3PrefabBuilder.SetGameViewResolution(2400, 1080);
        }

        private static void CheckCampaignBeforeDeploy(UiCampaignOperationsModel model)
        {
            int mission = entry % 5;
            byte expectedAvailable = chainIndex >= 10 ? (byte)31 : (byte)((1 << (mission + 1)) - 1);
            byte expectedCompleted = chainIndex >= 10 ? (byte)31 : (byte)((1 << mission) - 1);
            if (model.AvailableMissionMask != expectedAvailable || model.CompletedMissionMask != expectedCompleted ||
                !model.SelectedMission.Available || model.SelectedMission.PrimaryAction == UiCampaignMissionPrimaryActionKind.Locked)
                throw new InvalidOperationException($"Campaign unlock mismatch at {CapturePrefix}: available={model.AvailableMissionMask}/{expectedAvailable} completed={model.CompletedMissionMask}/{expectedCompleted}");
            Debug.Log($"[CampaignContinuity] deploy={CapturePrefix} available={model.AvailableMissionMask} completed={model.CompletedMissionMask} action={model.SelectedMission.PrimaryAction}");
        }

        private static void CheckCampaignSettlement(EntityManager em, Entity root, CampaignMissionRuntimeComponent runtime)
        {
            var save = new SaveService(new JsonSaveRepository(ProfileDirectory));
            var profile = save.LoadProfile();
            var progress = profile.campaignMissionProgress.Single(p => p.missionId == Missions[entry % 5]);
            string token = runtime.SessionToken + ":" + runtime.AttemptOrdinal;
            if (!progress.firstClearCompleted || !progress.firstClearRewardSettled || progress.pendingResume ||
                progress.settledTokens.Count(t => t == token) != 1 || progress.successfulReplayCount != (chainIndex >= 10 ? 1 : 0))
                throw new InvalidOperationException("Campaign settlement missing, duplicated, or incorrect replay reward: " + CapturePrefix);
            settledProfile = JsonUtility.ToJson(profile);
            File.WriteAllText(Path.Combine(Output, CapturePrefix + "-settled-profile.json"), JsonUtility.ToJson(profile, true));
            Debug.Log($"[CampaignContinuity] settled={CapturePrefix} token={token} replayCount={progress.successfulReplayCount} xp={profile.commanderXp} credits={profile.credits}");
        }

        private static void AdvanceCampaignChain(EntityManager em, Entity root)
        {
            if (SessionState.GetBool(RecoveryJourney, false)) VerifyRecoveryCoverage();
            var save = new SaveService(new JsonSaveRepository(ProfileDirectory));
            if (JsonUtility.ToJson(save.LoadProfile()) != settledProfile)
                throw new InvalidOperationException("Profile changed unexpectedly between result and campaign return: " + CapturePrefix);
            // Recreate the repository and store from disk between missions. Progress is not seeded.
            em.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(root).Store = new CampaignMissionProgressStore(save);
            Debug.Log("[CampaignContinuity] disk-reload-return=" + CapturePrefix);
            if (++chainIndex == Chain.Length)
            {
                Complete(true, SessionState.GetBool(RecoveryJourney, false)
                    ? $"Recovery chain entries {firstChainIndex + 1}-{Chain.Length}, player actions, full comic playback, disk reloads and settlements; EN 1280x720 / FA 2400x1080"
                    : "Fresh EN and FA M1-M5 chains plus FA M4-M1-M3-M5-M2 replay chain, disk reloads and once-only settlements");
                return;
            }
            entry = Chain[chainIndex];
            if (chainIndex == 5)
            {
                prepared = false;
                if (SessionState.GetBool(RecoveryJourney, false)) MainMenuV3PrefabBuilder.SetGameViewResolution(2400, 1080);
            }
            journeyStep = 0;
            lastJourneyAction = capturedJourneyAction = null;
            trainingWaitEntry = gatePoseCheckedEntry = lastStableStep = defenseStatusCapture = -1;
            missingDefenseInstructionSince = missingDefenseStatusSince = 0;
            Next(0);
        }
    }
}
