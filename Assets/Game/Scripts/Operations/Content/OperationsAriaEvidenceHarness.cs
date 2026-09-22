using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Game.Operations.Contracts;
using Game.Operations.Loop;
using Game.Operations.Tactical;

namespace Game.Operations.Content
{
    /// <summary>
    /// Machine-readable Operations ARIA evidence fields aligned with ACCEPTANCE.md.
    /// Placeholders for hashes/captures remain until a live Windows Watch/Play Mode run fills them.
    /// </summary>
    public sealed class OperationsAriaEvidenceRecord
    {
        public string SchemaVersion = "operations-aria-evidence-v1";
        public string Status = "PendingAriaWon";
        public string MissionId = string.Empty;
        public string DistrictId = string.Empty;
        public string OfferId = string.Empty;
        public string RunId = string.Empty;
        public string SessionId = string.Empty;
        public string ScenarioId = string.Empty;
        public int Seed;
        public string Difficulty = "Regular";
        public string Language = "en";
        public string Platform = "host";
        public string InputSource = "ARIA";
        public string InputPipeline = "operations_loop_visible_controls";
        public string WatchVirtualTouch = "PendingSeam";
        public int RestartCount;
        public int AttemptOrdinal;
        public string ContentHash = string.Empty;
        public string ConfigHashPlaceholder = "PENDING_LIVE_BUILD";
        public string CodeHashPlaceholder = "PENDING_LIVE_BUILD";
        public string TerminalReason = string.Empty;
        public string TerminalOutcome = string.Empty;
        public bool Victory;
        public int ObjectiveCompletionTick = -1;
        public int CivilianDeaths;
        public int TaskForceLosses;
        public int ReceivedCredits;
        public int ReceivedXp;
        public string SettlementTransactionId = string.Empty;
        public string ResultHash = string.Empty;
        /// <summary>
        /// City-profile revision written by a successful settlement.
        /// The profile starts at 0, so 0 is a real revision. -1 means unset.
        /// A Victory record requires this value to be ≥ 0 and equal to the save revision.
        /// </summary>
        public int SettledRevision = -1;
        public int[] BeforeDistrict = Array.Empty<int>();
        public int[] AfterDistrict = Array.Empty<int>();
        public string[] IntentTrace = Array.Empty<string>();
        public string CapturePathPlaceholder = "PENDING_SCREEN_CAPTURE";
        public string Notes = string.Empty;

        public string ToJson()
        {
            var builder = new StringBuilder(2048);
            builder.Append("{\n");
            Append(builder, "schema_version", SchemaVersion, true);
            Append(builder, "status", Status, true);
            Append(builder, "mission_id", MissionId, true);
            Append(builder, "district_id", DistrictId, true);
            Append(builder, "offer_id", OfferId, true);
            Append(builder, "run_id", RunId, true);
            Append(builder, "session_id", SessionId, true);
            Append(builder, "scenario_id", ScenarioId, true);
            Append(builder, "seed", Seed, false);
            Append(builder, "difficulty", Difficulty, true);
            Append(builder, "language", Language, true);
            Append(builder, "platform", Platform, true);
            Append(builder, "input_source", InputSource, true);
            Append(builder, "input_pipeline", InputPipeline, true);
            Append(builder, "watch_virtual_touch", WatchVirtualTouch, true);
            Append(builder, "restart_count", RestartCount, false);
            Append(builder, "attempt_ordinal", AttemptOrdinal, false);
            Append(builder, "content_hash", ContentHash, true);
            Append(builder, "config_hash", ConfigHashPlaceholder, true);
            Append(builder, "code_hash", CodeHashPlaceholder, true);
            Append(builder, "terminal_reason", TerminalReason, true);
            Append(builder, "terminal_outcome", TerminalOutcome, true);
            Append(builder, "victory", Victory, false);
            Append(builder, "objective_completion_tick", ObjectiveCompletionTick, false);
            Append(builder, "civilian_deaths", CivilianDeaths, false);
            Append(builder, "task_force_losses", TaskForceLosses, false);
            Append(builder, "received_credits", ReceivedCredits, false);
            Append(builder, "received_xp", ReceivedXp, false);
            Append(builder, "settlement_transaction_id", SettlementTransactionId, true);
            Append(builder, "result_hash", ResultHash, true);
            Append(builder, "settled_revision", SettledRevision, false);
            AppendInts(builder, "before_district", BeforeDistrict);
            AppendInts(builder, "after_district", AfterDistrict);
            AppendStrings(builder, "intent_trace", IntentTrace);
            Append(builder, "capture_path", CapturePathPlaceholder, true);
            Append(builder, "notes", Notes, true, true);
            builder.Append("}\n");
            return builder.ToString();
        }

        static void Append(StringBuilder builder, string key, string value, bool quote, bool last = false)
        {
            builder.Append("  \"").Append(key).Append("\": ");
            if (quote)
                builder.Append('"').Append(Escape(value)).Append('"');
            else
                builder.Append(value ?? string.Empty);
            builder.Append(last ? "\n" : ",\n");
        }

        static void Append(StringBuilder builder, string key, int value, bool last)
        {
            builder.Append("  \"").Append(key).Append("\": ").Append(value.ToString(CultureInfo.InvariantCulture));
            builder.Append(last ? "\n" : ",\n");
        }

        static void Append(StringBuilder builder, string key, bool value, bool last)
        {
            builder.Append("  \"").Append(key).Append("\": ").Append(value ? "true" : "false");
            builder.Append(last ? "\n" : ",\n");
        }

        static void AppendInts(StringBuilder builder, string key, int[] values)
        {
            builder.Append("  \"").Append(key).Append("\": [");
            for (int index = 0; index < values.Length; index++)
            {
                if (index > 0)
                    builder.Append(", ");
                builder.Append(values[index].ToString(CultureInfo.InvariantCulture));
            }

            builder.Append("],\n");
        }

        static void AppendStrings(StringBuilder builder, string key, string[] values)
        {
            builder.Append("  \"").Append(key).Append("\": [");
            for (int index = 0; index < values.Length; index++)
            {
                if (index > 0)
                    builder.Append(", ");
                builder.Append('"').Append(Escape(values[index])).Append('"');
            }

            builder.Append("],\n");
        }

        static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", string.Empty);
        }
    }

    /// <summary>
    /// Runs O001–O003 through planner-driven visible controls and builds ACCEPTANCE-shaped evidence records.
    /// </summary>
    public static class OperationsAriaEvidenceHarness
    {
        public const string EvidenceRootRelative = "Design/AgentReports/Operations";
        public const string BuildFolder = "host-aria-evidence";
        public static readonly string[] VerticalSliceMissions =
        {
            "operation.o001",
            "operation.o002",
            "operation.o003"
        };

        public static readonly int[] CanonicalRegularSeeds = { 1102, 1103, 1104 };

        public static string EvidenceRelativePath(string missionId, string difficulty, int seed) =>
            EvidenceRootRelative + "/" + BuildFolder + "/" + missionId + "/" + difficulty + "/" +
            seed.ToString(CultureInfo.InvariantCulture);

        public static string ResultFileName(string language) =>
            "result." + (string.IsNullOrEmpty(language) ? "en" : language) + ".json";

        /// <summary>
        /// Victory evidence is legal only after an accepted settlement whose revision
        /// is ≥ 0 and matches both the city profile and the campaign run save.
        /// A failed settle, or revision -1, must not stamp Victory.
        /// </summary>
        public static bool MayStampVictoryEvidence(
            bool settlementAccepted,
            int settledRevision,
            int profileRevision,
            int campaignRunRevision)
        {
            if (!settlementAccepted || settledRevision < 0)
                return false;
            return settledRevision == profileRevision && settledRevision == campaignRunRevision;
        }

        public static bool DistrictsMatch(int[] left, int[] right)
        {
            if (left == null || right == null || left.Length != right.Length || left.Length == 0)
                return false;
            for (int index = 0; index < left.Length; index++)
            {
                if (left[index] != right[index])
                    return false;
            }

            return true;
        }

        public static bool TryRunMission(
            string missionId,
            int seed,
            string language,
            out OperationsAriaEvidenceRecord record,
            out string failure)
        {
            record = null;
            failure = string.Empty;
            OperationsLoopSession loop = OperationsLoopSession.Create(seed, new byte[] { 9, 9, 9 }, new byte[] { 8, 8 });
            if (missionId == "operation.o003" && !loop.TryOffer(missionId, out _))
            {
                if (!TryDeployPlaySettle(loop, "operation.o001", CanonicalRegularSeeds[0], language, out _, out failure))
                    return false;
                if (!loop.RequestEndDay(NextId()).Accepted)
                {
                    failure = "end_day";
                    return false;
                }
            }

            return TryDeployPlaySettle(loop, missionId, seed, language, out record, out failure);
        }

        public static bool TryRunVerticalSliceRegular(
            string language,
            out OperationsAriaEvidenceRecord[] records,
            out string failure)
        {
            var list = new List<OperationsAriaEvidenceRecord>();
            for (int index = 0; index < VerticalSliceMissions.Length; index++)
            {
                if (!TryRunMission(VerticalSliceMissions[index], CanonicalRegularSeeds[index], language, out OperationsAriaEvidenceRecord record, out failure))
                {
                    records = list.ToArray();
                    return false;
                }

                list.Add(record);
            }

            records = list.ToArray();
            failure = string.Empty;
            return true;
        }

        static bool TryDeployPlaySettle(
            OperationsLoopSession loop,
            string missionId,
            int seed,
            string language,
            out OperationsAriaEvidenceRecord record,
            out string failure)
        {
            record = null;
            failure = string.Empty;
            if (!loop.TryOffer(missionId, out OperationsOfferSaveData offer))
            {
                failure = "offer";
                return false;
            }

            int districtNumber = DistrictNumber(offer.districtId);
            if (!loop.OpenDistrict(districtNumber).Accepted || !loop.OpenBriefing(offer.offerId).Accepted)
            {
                failure = "briefing";
                return false;
            }

            string deployId = NextId();
            if (!loop.BeginDeploy(deployId).Accepted || !loop.CompleteAttempt(deployId).Accepted)
            {
                failure = "deploy";
                return false;
            }

            if (!loop.BeginLaunch().Accepted || !loop.CompleteLaunch().Accepted)
            {
                failure = "launch";
                return false;
            }

            // District the settlement revision will diff. Deploy does not apply the outcome delta.
            int[] before = SnapshotDistrict(loop, districtNumber);

            if (!loop.BeginActive(true, true, true, loop.ContentHash).Accepted || !loop.CompleteActive().Accepted)
            {
                failure = "active";
                return false;
            }

            var trace = new List<string>();
            bool won = PlayWithTrace(loop, trace);
            int completionTick = -1;
            if (loop.TryMissionTick(out int tick))
                completionTick = tick;

            if (!loop.BeginResult().Accepted || !loop.CompleteResult().Accepted)
            {
                failure = "result";
                return false;
            }

            string settleId = NextId();
            OperationsCommandResult begun = loop.BeginSettlement(settleId);
            OperationsCommandResult settled = begun.Accepted
                ? loop.CompleteSettlement(settleId)
                : begun;
            if (!begun.Accepted || !settled.Accepted ||
                !MayStampVictoryEvidence(
                    settled.Accepted,
                    settled.NewRevision,
                    loop.ProfileRevision,
                    loop.CampaignRunRevision))
            {
                failure = begun.Accepted && settled.Accepted ? "settlement_revision" : "settlement";
                return false;
            }

            int[] after = SnapshotDistrict(loop, districtNumber);
            if (!loop.BeginReturn().Accepted || !loop.CompleteReturn().Accepted)
            {
                failure = "return";
                return false;
            }

            int[] afterReturn = SnapshotDistrict(loop, districtNumber);
            if (!loop.TryReadResult(out OperationsResultFrame settledFrame) ||
                !DistrictsMatch(before, settledFrame.Before) ||
                !DistrictsMatch(after, settledFrame.After) ||
                !DistrictsMatch(after, afterReturn))
            {
                failure = "district_delta";
                return false;
            }

            bool victory = won && loop.MissionVictory(missionId) && settled.NewRevision >= 0;
            string terminalReason = string.Empty;
            string terminalOutcome = loop.MissionOutcome.ToString();
            int civilianDeaths = 0;
            int taskForceLosses = 0;
            string resultHash = loop.ResultHash;
            if (loop.TryCommittedResult(out OperationsMissionResult missionResult))
            {
                terminalReason = missionResult.TerminalReason;
                terminalOutcome = missionResult.Outcome.ToString();
                civilianDeaths = missionResult.CivilianDeaths;
                taskForceLosses = missionResult.TaskForceLosses;
                resultHash = missionResult.ResultHash;
            }

            record = new OperationsAriaEvidenceRecord
            {
                Status = victory ? "HostUnassistedVictoryRecorded" : "HostAttemptFailed",
                MissionId = missionId,
                DistrictId = offer.districtId,
                OfferId = offer.offerId,
                RunId = loop.RunId,
                SessionId = loop.SessionId,
                ScenarioId = loop.ScenarioId,
                Seed = seed,
                Difficulty = loop.Difficulty.ToString(),
                Language = string.IsNullOrEmpty(language) ? "en" : language,
                Platform = "host",
                InputSource = "ARIA",
                InputPipeline = "operations_loop_visible_controls",
                WatchVirtualTouch = "PendingSeam",
                RestartCount = loop.RestartCount,
                AttemptOrdinal = loop.AttemptOrdinal,
                ContentHash = loop.ContentHash,
                TerminalReason = terminalReason,
                TerminalOutcome = terminalOutcome,
                Victory = victory,
                ObjectiveCompletionTick = completionTick,
                CivilianDeaths = civilianDeaths,
                TaskForceLosses = taskForceLosses,
                ReceivedCredits = loop.Credits,
                ReceivedXp = loop.CommanderXp,
                SettlementTransactionId = settleId,
                ResultHash = resultHash,
                SettledRevision = settled.NewRevision,
                BeforeDistrict = before,
                AfterDistrict = after,
                IntentTrace = trace.ToArray(),
                Notes = victory
                    ? "Host planner-driven visible-control win. AriaWon Pending until Windows Play Mode / Watch capture on WarlineCapture-Operations."
                    : "Host unassisted attempt did not reach Victory."
            };

            if (!victory)
            {
                failure = "outcome:" + record.TerminalOutcome;
                return false;
            }

            return true;
        }

        static bool PlayWithTrace(OperationsLoopSession loop, List<string> trace)
        {
            const int maxSteps = 1200;
            for (int step = 0; step < maxSteps && !loop.MissionTerminal; step++)
            {
                OperationsAriaIntent[] plan = OperationsAriaObjectivePlanner.Plan(loop);
                OperationsAriaIntent intent = default;
                bool haveIntent = false;
                for (int index = 0; index < plan.Length; index++)
                {
                    if (plan[index].Skill == OperationsAriaSkillKind.Focus)
                        continue;
                    intent = plan[index];
                    haveIntent = true;
                    break;
                }

                if (!haveIntent)
                {
                    loop.Advance(1);
                    continue;
                }

                if (intent.Skill == OperationsAriaSkillKind.Extract)
                {
                    for (int index = 0; index < plan.Length; index++)
                    {
                        if (plan[index].Skill != OperationsAriaSkillKind.Extract)
                            continue;
                        OperationsTacticalCommandResult extract = OperationsAriaInputSkills.TryExecute(loop, plan[index]);
                        trace.Add(step + ":Extract:" + plan[index].ActorId + ":" + extract.Reason);
                    }

                    loop.Advance(1);
                    continue;
                }

                if (intent.ActorId.Length > 0 &&
                    loop.TryActor(intent.ActorId, out OperationsTacticalActorState actor) &&
                    actor.ChannelTicks > 0 &&
                    (intent.Skill == OperationsAriaSkillKind.Scan ||
                     intent.Skill == OperationsAriaSkillKind.Interact ||
                     intent.Skill == OperationsAriaSkillKind.Repair ||
                     intent.Skill == OperationsAriaSkillKind.Observe))
                {
                    trace.Add(step + ":wait_channel:" + intent.ActorId + ":" + actor.ChannelTicks);
                    loop.Advance(1);
                    continue;
                }

                OperationsTacticalCommandResult result = OperationsAriaInputSkills.TryExecute(loop, intent);
                trace.Add(step + ":" + intent.Skill + ":" + intent.ActorId + "->" + intent.TargetId + "/" + intent.RouteId + ":" + result.Reason);
                loop.Advance(1);
            }

            return loop.MissionTerminal && loop.MissionOutcome == OperationsOutcomeKind.Victory;
        }

        static int[] SnapshotDistrict(OperationsLoopSession loop, int number)
        {
            var district = loop.District(number);
            return new[]
            {
                district.Security,
                district.Trust,
                district.Infrastructure,
                district.EnemyInfluence,
                district.IntelConfidence,
                district.Heat,
                district.SupplyReadiness
            };
        }

        static int DistrictNumber(string districtId)
        {
            for (int number = 1; number <= 6; number++)
            {
                if (OperationsIdentityRules.DistrictId(number) == districtId)
                    return number;
            }

            throw new InvalidOperationException(districtId);
        }

        static int _serial = 0xE100;
        static string NextId() => "cmd.operations." + (_serial++).ToString("x8");
    }
}
