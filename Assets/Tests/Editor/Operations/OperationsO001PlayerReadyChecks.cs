using System;
using System.IO;
using Game.Operations.Content;
using Game.Operations.Loop;
using Game.Operations.Tactical;

namespace Game.Tests.Editor.Operations
{
    /// <summary>
    /// Host and Editor checks for the O001 player shell. They press the visible controls.
    /// They do not claim AriaWon.
    /// </summary>
    public static class OperationsO001PlayerReadyChecks
    {
        public const int ExpectedCheckCount = 3;
        public const string PassMarker = "[OperationsO001PlayerShellValidation] result=Passed checks=3";

        public static void RunAll()
        {
            string root = Path.Combine(Path.GetTempPath(), "operations-o001-player-" + Guid.NewGuid().ToString("N"));
            string launchDirectory = root + "-launch";
            string playDirectory = root + "-play";
            try
            {
                SharedLaunchEntersAuthoredO001(launchDirectory);
                VisibleControlsSettleAndReturn(playDirectory);
                ReloadedProfileKeepsSettlement(playDirectory);
            }
            finally
            {
                DeleteQuiet(launchDirectory);
                DeleteQuiet(playDirectory);
            }
        }

        public static void SharedLaunchEntersAuthoredO001(string directory)
        {
            OperationsO001PlayerShell shell = OperationsO001PlayerShell.CreateNew(directory, OperationsO001PlayerShell.RegularEnSeed);
            Require(!shell.Press(OperationsO001PlayerShell.ContinueId), shell.Describe());
            Require(!shell.Press(OperationsO001PlayerShell.DeployId), shell.Describe());
            Require(shell.Press(OperationsO001PlayerShell.LibraryId), shell.Describe());
            Require(shell.Read().Route == OperationsShellNames.MissionBriefing, shell.Describe());
            Require(shell.Press(OperationsO001PlayerShell.DeployId), shell.Describe());
            OperationsPlayerShellFrame frame = shell.Read();
            Require(frame.MissionId == OperationsO001PlayerShell.MissionId, shell.Describe());
            Require(frame.MapId == OperationsMapGreyboxCatalog.OldQuarterMapId, shell.Describe());
            Require(frame.ScenarioId == "scenario.operations.o001", shell.Describe());
            Require(frame.ContentHash == OperationsAuthoredMissions.O001Hash, shell.Describe());
            Require(frame.HasMission && frame.HasSharedLaunchRequest, shell.Describe());
            Require(!frame.InvokesSharedSceneView, shell.Describe());
            Require(shell.Session.CampaignEnvelope[0] == 9, "envelope");
        }

        public static void VisibleControlsSettleAndReturn(string directory)
        {
            OperationsO001PlayerShell shell = OperationsO001PlayerShell.CreateNew(directory, OperationsO001PlayerShell.RegularEnSeed);
            AdvanceUntil(shell, frame => frame.HasMission && frame.Tick >= 4, 120);
            OperationsPlayerShellFrame mid = shell.Read();
            Require(mid.MissionId == OperationsO001PlayerShell.MissionId, shell.Describe());
            Require(mid.ContentHash == OperationsAuthoredMissions.O001Hash, shell.Describe());
            Require(mid.MapId == OperationsMapGreyboxCatalog.OldQuarterMapId, shell.Describe());
            Require(!mid.InvokesSharedSceneView && mid.HasSharedLaunchRequest, shell.Describe());
            string sessionId = mid.SessionId;
            int tick = mid.Tick;

            shell = OperationsO001PlayerShell.Open(directory);
            OperationsPlayerShellFrame resumed = shell.Read();
            Require(resumed.SessionId == sessionId, shell.Describe());
            Require(resumed.HasMission, shell.Describe());
            Require(resumed.Tick == tick, shell.Describe());
            Require(resumed.MissionId == OperationsO001PlayerShell.MissionId, shell.Describe());
            Require(shell.Session.CampaignEnvelope.Length == 3 && shell.Session.CampaignEnvelope[0] == 9, "envelope");

            AdvanceUntil(
                shell,
                frame => frame.O001Victory && frame.ReturnAcknowledged && frame.Phase == OperationsLoopPhase.Dashboard,
                800);
            OperationsPlayerShellFrame done = shell.Read();
            Require(done.Credits == 120, shell.Describe());
            Require(done.CommanderXp == 50, shell.Describe());
            Require(done.ProfileRevision > 0, shell.Describe());
            Require(done.ResultHash.Length > 0, shell.Describe());
            AssertDistrict(shell, 40, 46, 45, 48, 38, 22, 45);
        }

        public static void ReloadedProfileKeepsSettlement(string directory)
        {
            OperationsO001PlayerShell first = OperationsO001PlayerShell.Open(directory);
            OperationsO001PlayerShell second = OperationsO001PlayerShell.Open(directory);
            OperationsPlayerShellFrame frame = first.Read();
            Require(frame.O001Victory, first.Describe());
            Require(frame.ReturnAcknowledged, first.Describe());
            Require(frame.Phase == OperationsLoopPhase.Dashboard, first.Describe());
            Require(frame.Credits == 120 && second.Read().Credits == 120, first.Describe());
            Require(frame.CommanderXp == 50 && second.Read().CommanderXp == 50, first.Describe());
            Require(frame.ProfileRevision == second.Read().ProfileRevision, first.Describe());
            Require(frame.ResultHash == second.Read().ResultHash && frame.ResultHash.Length > 0, first.Describe());
            Require(!first.Press(OperationsO001PlayerShell.ContinueId), first.Describe());
            Require(first.Read().Credits == 120, first.Describe());
            Require(!ContainsEnabled(first.Read(), OperationsO001PlayerShell.LibraryId), first.Describe());
            AssertDistrict(first, 40, 46, 45, 48, 38, 22, 45);
        }

        static void AdvanceUntil(OperationsO001PlayerShell shell, Func<OperationsPlayerShellFrame, bool> done, int maxSteps)
        {
            for (int step = 0; step < maxSteps; step++)
            {
                if (done(shell.Read()))
                    return;
                OperationsVisibleStepKind kind = OperationsAriaVisibleControls.Step(shell, out string detail);
                if (kind == OperationsVisibleStepKind.Stuck)
                    throw new InvalidOperationException(detail);
                if (kind == OperationsVisibleStepKind.Victory && done(shell.Read()))
                    return;
            }

            throw new InvalidOperationException("steps " + shell.Describe());
        }

        static void AssertDistrict(OperationsO001PlayerShell shell, params int[] expected)
        {
            var district = shell.Session.District(1);
            int[] actual =
            {
                district.Security,
                district.Trust,
                district.Infrastructure,
                district.EnemyInfluence,
                district.IntelConfidence,
                district.Heat,
                district.SupplyReadiness
            };
            Require(actual.Length == expected.Length, "district");
            for (int index = 0; index < expected.Length; index++)
                Require(actual[index] == expected[index], "district:" + index + "=" + actual[index]);
        }

        static bool ContainsEnabled(OperationsPlayerShellFrame frame, string controlId)
        {
            OperationsPlayerControl[] controls = frame.Controls ?? Array.Empty<OperationsPlayerControl>();
            for (int index = 0; index < controls.Length; index++)
            {
                if (controls[index].Enabled && controls[index].Id == controlId)
                    return true;
            }

            return false;
        }

        static void Require(bool condition, string detail)
        {
            if (!condition)
                throw new InvalidOperationException(detail);
        }

        static void DeleteQuiet(string directory)
        {
            try
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
