using System;
using System.Collections.Generic;
using System.Text;
using Game.Operations.Contracts;
using Game.Operations.Loop;
using Game.Operations.Strategic;
using Game.Operations.Tactical;

namespace Game.Operations.Content
{
    /// <summary>
    /// P4R player shell for operation.o001. Hub, briefing, play, settle, and return
    /// go through Package 3. Deploy asks the shared match scene to open. Shipping
    /// controls and Regular EN Aria both call <see cref="Press"/>. The profile directory
    /// is packed into the shipping profile envelope on return.
    /// </summary>
    public sealed class OperationsO001PlayerShell
    {
        public const int RegularEnSeed = 1102;
        public const string MissionId = "operation.o001";
        public static readonly byte[] CampaignEnvelopeBytes = { 9, 9, 9 };
        public static readonly byte[] QuickEnvelopeBytes = { 8, 8 };

        public const string LibraryId = "library|operation.o001";
        public const string DeployId = "briefing|deploy";
        public const string BackId = "nav|back";
        public const string WaitId = "clock|wait";
        public const string ContinueId = "result|continue";

        readonly string _directory;
        readonly OperationsLoopSession _loop;
        string _selectedUnit = string.Empty;
        string _lastReject = string.Empty;
        int _commandSequence;

        OperationsO001PlayerShell(string directory, OperationsLoopSession loop)
        {
            _directory = directory;
            _loop = loop;
            _commandSequence = SeedCommandSequence(loop);
        }

        public OperationsLoopSession Session => _loop;
        public string LastReject => _lastReject;
        public string SelectedUnitId => _selectedUnit;

        public static OperationsO001PlayerShell CreateNew(string directory, int seed)
        {
            if (seed == 0)
                throw new ArgumentOutOfRangeException(nameof(seed));
            var shell = new OperationsO001PlayerShell(
                directory,
                OperationsLoopSession.Create(seed, CampaignEnvelopeBytes, QuickEnvelopeBytes));
            shell.Flush();
            return shell;
        }

        public static OperationsO001PlayerShell Open(string directory)
        {
            OperationsLoopSession loop = OperationsDurableProfile.Read(directory);
            return new OperationsO001PlayerShell(directory, loop);
        }

        public static OperationsO001PlayerShell Bind(string directory, int seed)
        {
            if (OperationsDurableProfile.Exists(directory))
                return Open(directory);
            return CreateNew(directory, seed);
        }

        public static string SelectId(string actorId) => "select|" + (actorId ?? string.Empty);

        public static string OrderId(OperationsAriaIntent intent) =>
            "order|" + intent.Skill + "|" + intent.ActorId + "|" + intent.TargetId + "|" + intent.RouteId;

        public OperationsPlayerShellFrame Read()
        {
            OperationsLoopDocument document = _loop.CopyDocument();
            OperationsShellFrame shell = _loop.ReadShell();
            bool shared = _loop.TryLaunchRequest(out OperationsModeLaunchRequest request);
            var frame = new OperationsPlayerShellFrame
            {
                Route = shell.Top ?? string.Empty,
                Phase = _loop.Phase,
                MissionId = _loop.MissionId ?? string.Empty,
                MapId = _loop.MapId ?? string.Empty,
                ScenarioId = _loop.ScenarioId ?? string.Empty,
                ContentHash = _loop.ContentHash ?? string.Empty,
                SessionId = _loop.SessionId ?? string.Empty,
                ResultHash = document.ResultHash ?? string.Empty,
                Outcome = _loop.MissionOutcome,
                Tick = _loop.TryMissionTick(out int tick) ? tick : -1,
                Credits = _loop.Credits,
                CommanderXp = _loop.CommanderXp,
                ProfileRevision = _loop.ProfileRevision,
                HasMission = _loop.HasMission,
                Terminal = _loop.MissionTerminal,
                O001Victory = _loop.MissionVictory(MissionId),
                ReturnAcknowledged = document.ReturnAcknowledged,
                HasSharedLaunchRequest = shared,
                InvokesSharedSceneView = shared && request.InvokesSharedSceneView,
                TitleKey = "operations.o001.title",
                BriefKey = "operations.o001.brief",
                ObjectiveKey = "operations.o001.objective.primary",
                Controls = BuildControls()
            };
            return frame;
        }

        public int ChannelTicks(string actorId)
        {
            if (string.IsNullOrEmpty(actorId) || !_loop.TryActor(actorId, out OperationsTacticalActorState actor))
                return 0;
            return actor.ChannelTicks;
        }

        public bool Press(string controlId)
        {
            _lastReject = string.Empty;
            if (string.IsNullOrEmpty(controlId))
            {
                _lastReject = "control";
                return false;
            }

            OperationsPlayerControl[] controls = BuildControls();
            OperationsPlayerControl match = default;
            bool found = false;
            for (int index = 0; index < controls.Length; index++)
            {
                if (controls[index].Id != controlId)
                    continue;
                match = controls[index];
                found = true;
                break;
            }

            if (!found || !match.Enabled)
            {
                _lastReject = found ? "disabled" : "hidden";
                return false;
            }

            if (controlId == LibraryId)
                return OpenLibrary();
            if (controlId == DeployId)
                return DeployAuthoredMission();
            if (controlId == BackId)
                return NavigateBack();
            if (controlId == WaitId)
                return PumpOneSecond();
            if (controlId == ContinueId)
                return SettleAndReturn();
            if (controlId.StartsWith("select|", StringComparison.Ordinal))
            {
                _selectedUnit = controlId.Substring("select|".Length);
                return true;
            }

            if (!controlId.StartsWith("order|", StringComparison.Ordinal))
            {
                _lastReject = "control";
                return false;
            }

            return IssueOrder(controlId);
        }

        public bool PumpOneSecond()
        {
            if (!_loop.HasMission || _loop.Phase != OperationsLoopPhase.Active || _loop.MissionTerminal)
            {
                _lastReject = "clock";
                return false;
            }

            _loop.Advance(1);
            if (!PublishCheckpoint())
                return false;
            Flush();
            return true;
        }

        public string Describe()
        {
            OperationsPlayerShellFrame frame = Read();
            var builder = new StringBuilder();
            builder.Append("phase=").Append(frame.Phase);
            builder.Append(" route=").Append(frame.Route);
            builder.Append(" tick=").Append(frame.Tick);
            builder.Append(" outcome=").Append(frame.Outcome);
            builder.Append(" mission=").Append(frame.MissionId);
            builder.Append(" hash=").Append(frame.ContentHash);
            builder.Append(" victory=").Append(frame.O001Victory);
            builder.Append(" returned=").Append(frame.ReturnAcknowledged);
            builder.Append(" reject=").Append(_lastReject);
            builder.Append(" controls=");
            for (int index = 0; index < frame.Controls.Length; index++)
            {
                if (index > 0)
                    builder.Append(',');
                builder.Append(frame.Controls[index].Id);
            }

            return builder.ToString();
        }

        bool OpenLibrary()
        {
            if (!_loop.TryOffer(MissionId, out OperationsOfferSaveData offer))
            {
                _lastReject = "offer";
                return false;
            }

            int district = DistrictNumber(offer.districtId);
            OperationsLoopStep opened = _loop.OpenDistrict(district);
            if (!opened.Accepted)
            {
                _lastReject = opened.Reason;
                return false;
            }

            OperationsLoopStep briefing = _loop.OpenBriefing(offer.offerId);
            if (!briefing.Accepted)
            {
                _loop.Back();
                _lastReject = briefing.Reason;
                return false;
            }

            return true;
        }

        bool DeployAuthoredMission()
        {
            if (!_loop.TryReadBriefing(out OperationsBriefingFrame briefing) || briefing.MissionId != MissionId)
            {
                _lastReject = "briefing";
                return false;
            }

            string commandId = NextCommandId();
            OperationsCommandResult deploy = _loop.BeginDeploy(commandId);
            if (!deploy.Accepted)
            {
                _lastReject = "deploy:" + deploy.ReasonCode;
                return false;
            }

            OperationsCommandResult reserved = _loop.CompleteAttempt(commandId);
            if (!reserved.Accepted)
            {
                _lastReject = "reserve:" + reserved.ReasonCode;
                return false;
            }

            OperationsLoopStep launch = _loop.BeginLaunch(true);
            if (!launch.Accepted)
            {
                _lastReject = "launch:" + launch.Reason;
                return false;
            }

            OperationsLoopStep launched = _loop.CompleteLaunch();
            if (!launched.Accepted)
            {
                _lastReject = "launch_commit:" + launched.Reason;
                return false;
            }

            if (!_loop.TryLaunchRequest(out OperationsModeLaunchRequest request) ||
                !request.InvokesSharedSceneView ||
                request.Mode != OperationsMatchMode.Operations ||
                request.MapId != OperationsMapGreyboxCatalog.OldQuarterMapId)
            {
                _lastReject = "launch_request";
                return false;
            }

            OperationsLoopStep active = _loop.BeginActive(true, true, true, _loop.ContentHash);
            if (!active.Accepted)
            {
                _lastReject = "active:" + active.Reason;
                return false;
            }

            OperationsLoopStep started = _loop.CompleteActive();
            if (!started.Accepted)
            {
                _lastReject = "active_commit:" + started.Reason;
                return false;
            }

            if (_loop.MissionId != MissionId ||
                _loop.MapId != OperationsMapGreyboxCatalog.OldQuarterMapId ||
                _loop.ScenarioId != "scenario.operations.o001" ||
                _loop.ContentHash != OperationsAuthoredMissions.O001Hash ||
                !_loop.HasMission)
            {
                _lastReject = "authored_mission";
                return false;
            }

            if (!PublishCheckpoint())
                return false;
            Flush();
            return true;
        }

        bool NavigateBack()
        {
            OperationsLoopStep back = _loop.Back();
            if (!back.Accepted)
            {
                _lastReject = back.Reason;
                return false;
            }

            return true;
        }

        bool SettleAndReturn()
        {
            if (_loop.Phase == OperationsLoopPhase.Active && _loop.HasMission && _loop.MissionTerminal)
            {
                OperationsLoopStep begun = _loop.BeginResult();
                if (!begun.Accepted)
                {
                    _lastReject = "result:" + begun.Reason;
                    return false;
                }

                OperationsLoopStep committed = _loop.CompleteResult();
                if (!committed.Accepted)
                {
                    _lastReject = "result_commit:" + committed.Reason;
                    return false;
                }
            }

            if (_loop.Phase == OperationsLoopPhase.PendingResult)
            {
                string commandId = NextCommandId();
                OperationsCommandResult begun = _loop.BeginSettlement(commandId);
                if (!begun.Accepted)
                {
                    _lastReject = "settlement:" + begun.ReasonCode;
                    return false;
                }

                OperationsCommandResult settled = _loop.CompleteSettlement(commandId);
                if (!settled.Accepted)
                {
                    _lastReject = "settlement_commit:" + settled.ReasonCode;
                    return false;
                }

                Flush();
            }

            if (_loop.Phase == OperationsLoopPhase.Settled && !_loop.CopyDocument().ReturnAcknowledged)
            {
                OperationsLoopStep begun = _loop.BeginReturn();
                if (!begun.Accepted)
                {
                    _lastReject = "return:" + begun.Reason;
                    return false;
                }

                OperationsLoopStep returned = _loop.CompleteReturn();
                if (!returned.Accepted)
                {
                    _lastReject = "return_commit:" + returned.Reason;
                    return false;
                }

                _selectedUnit = string.Empty;
                Flush();
                return true;
            }

            _lastReject = "continue";
            return false;
        }

        bool IssueOrder(string controlId)
        {
            string[] parts = controlId.Split('|');
            if (parts.Length != 5 || !Enum.TryParse(parts[1], out OperationsAriaSkillKind skill))
            {
                _lastReject = "order";
                return false;
            }

            string actorId = parts[2];
            string targetId = parts[3];
            string routeId = parts[4];
            OperationsTacticalCommandResult result;
            switch (skill)
            {
                case OperationsAriaSkillKind.Observe:
                    result = _loop.Observe(actorId, targetId);
                    break;
                case OperationsAriaSkillKind.Scan:
                    result = _loop.Scan(actorId, targetId);
                    break;
                case OperationsAriaSkillKind.Interact:
                    result = _loop.Interact(actorId, targetId);
                    break;
                case OperationsAriaSkillKind.Repair:
                    result = _loop.Repair(actorId, targetId);
                    break;
                case OperationsAriaSkillKind.Move:
                    result = _loop.Move(actorId, targetId);
                    break;
                case OperationsAriaSkillKind.Hold:
                    result = _loop.Hold(actorId, targetId);
                    break;
                case OperationsAriaSkillKind.EscortGo:
                    result = _loop.EscortGo(routeId);
                    break;
                case OperationsAriaSkillKind.EscortHold:
                    result = _loop.EscortHold();
                    break;
                case OperationsAriaSkillKind.Extract:
                    result = _loop.Extract(actorId);
                    break;
                case OperationsAriaSkillKind.Attack:
                    result = _loop.Attack(actorId, targetId);
                    break;
                case OperationsAriaSkillKind.Conclude:
                    result = _loop.ConcludeMission();
                    break;
                case OperationsAriaSkillKind.Withdraw:
                    result = _loop.WithdrawMission();
                    break;
                default:
                    _lastReject = "skill";
                    return false;
            }

            if (!result.Accepted)
            {
                _lastReject = result.Reason.ToString();
                return false;
            }

            if (!PublishCheckpoint())
                return false;
            Flush();
            return true;
        }

        OperationsPlayerControl[] BuildControls()
        {
            var controls = new List<OperationsPlayerControl>();
            OperationsShellFrame shell = _loop.ReadShell();
            string route = shell.Top ?? string.Empty;
            if (ShowContinue())
            {
                controls.Add(Control(ContinueId, "operations.result.continue", "Continue", true));
                return controls.ToArray();
            }

            if (_loop.Phase == OperationsLoopPhase.Dashboard && route == OperationsShellNames.Operations)
            {
                bool won = _loop.MissionVictory(MissionId);
                if (_loop.TryOffer(MissionId, out _))
                    controls.Add(Control(LibraryId, "operations.o001.title", "Street Signals", !won));
                return controls.ToArray();
            }

            if (route == OperationsShellNames.MissionBriefing)
            {
                controls.Add(Control(DeployId, string.Empty, "Deploy", true));
                controls.Add(Control(BackId, string.Empty, "Back", true));
                return controls.ToArray();
            }

            if (_loop.Phase != OperationsLoopPhase.Active || !_loop.HasMission || _loop.MissionTerminal)
                return controls.ToArray();

            OperationsTacticalActorState[] actors = _loop.CopyPublicActors();
            for (int index = 0; index < actors.Length; index++)
            {
                if (!IsCommandable(actors[index]))
                    continue;
                controls.Add(Control(SelectId(actors[index].ObjectId), string.Empty, actors[index].ObjectId, true));
            }

            OperationsAriaIntent[] plan = OperationsAriaObjectivePlanner.Plan(_loop);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < plan.Length; index++)
            {
                OperationsAriaIntent intent = plan[index];
                if (intent.Skill == OperationsAriaSkillKind.Focus)
                    continue;
                if (intent.ActorId.Length > 0 && intent.ActorId != _selectedUnit)
                    continue;
                string id = OrderId(intent);
                if (!seen.Add(id))
                    continue;
                controls.Add(OrderControl(intent));
            }

            bool channeling = false;
            for (int index = 0; index < actors.Length; index++)
            {
                if (actors[index].Alive && actors[index].ChannelTicks > 0)
                {
                    channeling = true;
                    break;
                }
            }

            controls.Add(Control(
                WaitId,
                channeling ? "operations.hud.channeling" : string.Empty,
                "Wait",
                true));
            return controls.ToArray();
        }

        static OperationsPlayerControl OrderControl(OperationsAriaIntent intent)
        {
            switch (intent.Skill)
            {
                case OperationsAriaSkillKind.Scan:
                    return Control(OrderId(intent), "operations.coach.o001.scan.title", "Scan", true);
                case OperationsAriaSkillKind.Interact:
                    return Control(OrderId(intent), "operations.coach.o001.evidence.title", "Evidence", true);
                case OperationsAriaSkillKind.Extract:
                    return Control(OrderId(intent), "operations.objective.extract_force", "Extract infantry", true);
                case OperationsAriaSkillKind.Repair:
                    return Control(OrderId(intent), "operations.controls.repair", "Repair", true);
                case OperationsAriaSkillKind.Hold:
                    return Control(OrderId(intent), "operations.controls.defend.hold", "Hold court", true);
                case OperationsAriaSkillKind.EscortGo:
                    return Control(OrderId(intent), "operations.controls.escort.go", "Go", true);
                case OperationsAriaSkillKind.EscortHold:
                    return Control(OrderId(intent), "operations.controls.escort.hold", "Hold", true);
                case OperationsAriaSkillKind.Conclude:
                    return Control(OrderId(intent), "operations.teach.partial.conclude.title", "Conclude (Partial)", true);
                case OperationsAriaSkillKind.Withdraw:
                    return Control(OrderId(intent), "operations.teach.partial.withdraw.title", "Withdraw", true);
                case OperationsAriaSkillKind.Move:
                    return Control(OrderId(intent), string.Empty, intent.TargetId, true);
                default:
                    return Control(OrderId(intent), string.Empty, intent.Skill.ToString(), true);
            }
        }

        bool ShowContinue()
        {
            if (_loop.Phase == OperationsLoopPhase.Active && _loop.HasMission && _loop.MissionTerminal)
                return true;
            if (_loop.Phase == OperationsLoopPhase.PendingResult)
                return true;
            return _loop.Phase == OperationsLoopPhase.Settled && !_loop.CopyDocument().ReturnAcknowledged;
        }

        bool PublishCheckpoint()
        {
            OperationsLoopStep begun = _loop.BeginCheckpoint();
            if (!begun.Accepted)
            {
                _lastReject = "checkpoint:" + begun.Reason;
                return false;
            }

            OperationsLoopStep published = _loop.PublishCheckpoint();
            if (!published.Accepted)
            {
                _lastReject = "checkpoint_publish:" + published.Reason;
                return false;
            }

            return true;
        }

        void Flush() => OperationsDurableProfile.Write(_directory, _loop);

        string NextCommandId()
        {
            _commandSequence++;
            return OperationsStableIds.Generated("cmd", _commandSequence);
        }

        static int SeedCommandSequence(OperationsLoopSession loop)
        {
            OperationsLoopDocument document = loop.CopyDocument();
            int seed = (loop.ProfileRevision * 1000) + (document.CheckpointSequence * 10) + document.RestartCount + 20;
            return seed < 20 ? 20 : seed;
        }

        static int DistrictNumber(string districtId)
        {
            for (int number = 1; number <= OperationsIdentityRules.DistrictCount; number++)
            {
                if (OperationsIdentityRules.DistrictId(number) == districtId)
                    return number;
            }

            throw new InvalidOperationException(districtId);
        }

        static bool IsCommandable(OperationsTacticalActorState actor) =>
            actor.Faction == OperationsTacticalFaction.Player &&
            actor.Alive &&
            actor.Spawned &&
            actor.Commandable &&
            (actor.Body == OperationsTacticalBodyKind.Infantry ||
             actor.RosterRole == OperationsRosterRoleKind.RepairSpecialist);

        static OperationsPlayerControl Control(string id, string labelKey, string label, bool enabled) =>
            new(id, labelKey, label, enabled);
    }

    public readonly struct OperationsPlayerControl
    {
        public OperationsPlayerControl(string id, string labelKey, string label, bool enabled)
        {
            Id = id ?? string.Empty;
            LabelKey = labelKey ?? string.Empty;
            Label = label ?? string.Empty;
            Enabled = enabled;
        }

        public string Id { get; }
        public string LabelKey { get; }
        public string Label { get; }
        public bool Enabled { get; }
    }

    public sealed class OperationsPlayerShellFrame
    {
        public string Route = string.Empty;
        public OperationsLoopPhase Phase;
        public string MissionId = string.Empty;
        public string MapId = string.Empty;
        public string ScenarioId = string.Empty;
        public string ContentHash = string.Empty;
        public string SessionId = string.Empty;
        public string ResultHash = string.Empty;
        public OperationsOutcomeKind Outcome;
        public int Tick = -1;
        public int Credits;
        public int CommanderXp;
        public int ProfileRevision;
        public bool HasMission;
        public bool Terminal;
        public bool O001Victory;
        public bool ReturnAcknowledged;
        public bool HasSharedLaunchRequest;
        public bool InvokesSharedSceneView;
        public string TitleKey = string.Empty;
        public string BriefKey = string.Empty;
        public string ObjectiveKey = string.Empty;
        public OperationsPlayerControl[] Controls = Array.Empty<OperationsPlayerControl>();
    }
}
