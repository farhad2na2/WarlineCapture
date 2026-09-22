using System;
using System.Collections.Generic;
using Game.Operations.Contracts;
using Game.Operations.Strategic;
using Game.Operations.Tactical;

namespace Game.Operations.Loop
{
    public static class OperationsMissionProjection
    {
        public static OperationsMissionResult Project(
            OperationsTacticalSession mission,
            OperationsCompiledTactical definition,
            OperationsLoopDocument launch)
        {
            if (mission == null || !mission.IsTerminal || mission.Outcome == OperationsOutcomeKind.None)
                throw new InvalidOperationException("tactical_result_not_terminal");

            var mandatory = new List<OperationsObjectiveFact>();
            var optional = new List<OperationsObjectiveFact>();
            for (int index = 0; index < definition.Nodes.Length; index++)
            {
                OperationsCompiledNode node = definition.Nodes[index];
                if (!mission.TryGetNode(node.NodeId, out OperationsTacticalNodeState state))
                    continue;
                var fact = new OperationsObjectiveFact(
                    node.NodeId,
                    state.Phase == OperationsTacticalNodePhase.Complete,
                    state.Phase == OperationsTacticalNodePhase.Failed,
                    state.ProgressCount > 0 ? state.ProgressCount : state.ProgressTicks);
                if (node.Optional)
                    optional.Add(fact);
                else
                    mandatory.Add(fact);
            }

            OperationsTacticalActorState[] actors = mission.CopyActors();
            var sites = new List<OperationsObjectiveFact>();
            var cargo = new List<OperationsObjectiveFact>();
            int losses = 0;
            int initial = 0;
            int deaths = 0;
            for (int index = 0; index < actors.Length; index++)
            {
                OperationsTacticalActorState actor = actors[index];
                if (actor.Faction == OperationsTacticalFaction.Player && actor.Body == OperationsTacticalBodyKind.Infantry)
                {
                    initial++;
                    if (!actor.Alive)
                        losses++;
                }

                if (actor.Faction == OperationsTacticalFaction.Neutral && actor.Body == OperationsTacticalBodyKind.Infantry && !actor.Alive)
                    deaths++;
                if (actor.Body == OperationsTacticalBodyKind.Site)
                {
                    bool failed = !actor.Alive || actor.Health <= 0;
                    sites.Add(new OperationsObjectiveFact(actor.ObjectId, !failed, failed, actor.Health < 0 ? 0 : actor.Health));
                }
            }

            OperationsTacticalFact[] facts = mission.CopyFacts();
            var evidence = new List<string>();
            for (int index = 0; index < facts.Length; index++)
            {
                if (facts[index].Kind == OperationsTacticalFactKind.InteractCompleted &&
                    facts[index].ObjectId.IndexOf("evidence", StringComparison.Ordinal) >= 0 &&
                    !evidence.Contains(facts[index].ObjectId))
                    evidence.Add(facts[index].ObjectId);
                if (facts[index].Kind == OperationsTacticalFactKind.CargoDelivered)
                    cargo.Add(new OperationsObjectiveFact(facts[index].ObjectId, true, false, 1));
            }

            string hash = Hash(mission, mandatory, optional, sites, cargo, evidence, deaths, losses, initial);
            return new OperationsMissionResult(
                OperationsIdentityRules.CurrentSchemaVersion,
                launch.RunId,
                launch.OfferId,
                launch.MissionId,
                launch.SessionId,
                launch.AttemptOrdinal,
                launch.DefinitionVersion,
                mission.Outcome,
                mission.TerminalReason,
                mission.Tick,
                mandatory.ToArray(),
                optional.ToArray(),
                deaths,
                sites.ToArray(),
                cargo.ToArray(),
                evidence.ToArray(),
                losses,
                initial,
                hash);
        }

        public static string Encode(OperationsMissionResult result)
        {
            return string.Join("|", new[]
            {
                ((int)result.Outcome).ToString(),
                Escape(result.TerminalReason),
                result.ElapsedTicks.ToString(),
                result.CivilianDeaths.ToString(),
                result.TaskForceLosses.ToString(),
                result.InitialTaskForceCount.ToString(),
                Escape(result.ResultHash),
                Facts(result.MandatoryObjectiveFacts),
                Facts(result.OptionalObjectiveFacts),
                Facts(result.ProtectedSiteFacts),
                Facts(result.DeliveredCargoFacts),
                Join(result.ExtractedEvidenceIds)
            });
        }

        public static OperationsMissionResult Decode(OperationsLoopDocument launch, string text)
        {
            string[] parts = (text ?? string.Empty).Split('|');
            if (parts.Length != 12)
                throw new InvalidOperationException("result_encoding");
            return new OperationsMissionResult(
                OperationsIdentityRules.CurrentSchemaVersion,
                launch.RunId,
                launch.OfferId,
                launch.MissionId,
                launch.SessionId,
                launch.AttemptOrdinal,
                launch.DefinitionVersion,
                (OperationsOutcomeKind)int.Parse(parts[0]),
                Unescape(parts[1]),
                int.Parse(parts[2]),
                ParseFacts(parts[7]),
                ParseFacts(parts[8]),
                int.Parse(parts[3]),
                ParseFacts(parts[9]),
                ParseFacts(parts[10]),
                Split(parts[11]),
                int.Parse(parts[4]),
                int.Parse(parts[5]),
                Unescape(parts[6]));
        }

        private static string Hash(
            OperationsTacticalSession mission,
            List<OperationsObjectiveFact> mandatory,
            List<OperationsObjectiveFact> optional,
            List<OperationsObjectiveFact> sites,
            List<OperationsObjectiveFact> cargo,
            List<string> evidence,
            int deaths,
            int losses,
            int initial)
        {
            uint hash = OperationsStableIds.Mix(2166136261u, (int)mission.Outcome);
            hash = OperationsStableIds.Mix(hash, mission.TerminalReason);
            hash = OperationsStableIds.Mix(hash, mission.Tick);
            hash = OperationsStableIds.Mix(hash, deaths);
            hash = OperationsStableIds.Mix(hash, losses);
            hash = OperationsStableIds.Mix(hash, initial);
            hash = MixFacts(hash, mandatory);
            hash = MixFacts(hash, optional);
            hash = MixFacts(hash, sites);
            hash = MixFacts(hash, cargo);
            for (int index = 0; index < evidence.Count; index++)
                hash = OperationsStableIds.Mix(hash, evidence[index]);
            return "rh" + OperationsStableIds.Hex8(hash) + OperationsStableIds.Hex8(OperationsStableIds.Mix(hash, mission.MissionId));
        }

        private static uint MixFacts(uint hash, List<OperationsObjectiveFact> facts)
        {
            for (int index = 0; index < facts.Count; index++)
            {
                hash = OperationsStableIds.Mix(hash, facts[index].NodeId);
                hash = OperationsStableIds.Mix(hash, facts[index].Completed ? 1 : 0);
                hash = OperationsStableIds.Mix(hash, facts[index].Failed ? 1 : 0);
                hash = OperationsStableIds.Mix(hash, facts[index].Count);
            }

            return hash;
        }

        private static string Facts(OperationsObjectiveFact[] facts)
        {
            if (facts == null || facts.Length == 0)
                return "-";
            var parts = new string[facts.Length];
            for (int index = 0; index < facts.Length; index++)
            {
                OperationsObjectiveFact fact = facts[index];
                parts[index] = fact.NodeId + "," + (fact.Completed ? "1" : "0") + "," + (fact.Failed ? "1" : "0") + "," + fact.Count;
            }

            return string.Join("+", parts);
        }

        private static OperationsObjectiveFact[] ParseFacts(string text)
        {
            if (text == "-")
                return Array.Empty<OperationsObjectiveFact>();
            string[] parts = text.Split('+');
            var facts = new OperationsObjectiveFact[parts.Length];
            for (int index = 0; index < parts.Length; index++)
            {
                string[] fields = parts[index].Split(',');
                facts[index] = new OperationsObjectiveFact(fields[0], fields[1] == "1", fields[2] == "1", int.Parse(fields[3]));
            }

            return facts;
        }

        private static string Join(string[] values)
        {
            if (values == null || values.Length == 0)
                return "-";
            return string.Join("+", values);
        }

        private static string[] Split(string text)
        {
            if (text == "-")
                return Array.Empty<string>();
            return text.Split('+');
        }

        private static string Escape(string value) => (value ?? string.Empty).Replace("\\", "\\\\").Replace("|", "\\p");

        private static string Unescape(string value) => (value ?? string.Empty).Replace("\\p", "|").Replace("\\\\", "\\");
    }
}
