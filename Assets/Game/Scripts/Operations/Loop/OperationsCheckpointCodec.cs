using System;
using System.Collections.Generic;
using Game.Operations.Strategic;
using Game.Operations.Tactical;

namespace Game.Operations.Loop
{
    public sealed class OperationsCheckpointImage
    {
        public string SessionId = string.Empty;
        public string ContentHash = string.Empty;
        public int Tick;
        public bool Paused;
        public int Materials;
        public string Checksum = string.Empty;
        public List<OperationsLoopOrder> Orders = new();
    }

    public static class OperationsCheckpointCodec
    {
        public static string Write(
            OperationsTacticalSession mission,
            OperationsCompiledTactical definition,
            IReadOnlyList<OperationsLoopOrder> orders,
            string sessionId,
            string contentHash,
            int restartCount)
        {
            var lines = new List<string>
            {
                "schema=1",
                "session=" + sessionId,
                "content=" + contentHash,
                "tick=" + mission.Tick,
                "paused=" + (mission.IsPaused ? "1" : "0"),
                "materials=" + mission.Materials,
                "restart=" + restartCount
            };

            OperationsTacticalActorState[] actors = mission.CopyActors();
            Array.Sort(actors, (left, right) => string.CompareOrdinal(left.ObjectId, right.ObjectId));
            for (int index = 0; index < actors.Length; index++)
            {
                OperationsTacticalActorState actor = actors[index];
                lines.Add(string.Join("|", new[]
                {
                    "actor",
                    actor.ObjectId,
                    actor.RoleId,
                    Mm(actor.X).ToString(),
                    Mm(actor.Z).ToString(),
                    actor.Health.ToString(),
                    actor.Alive ? "1" : "0",
                    actor.Spawned ? "1" : "0",
                    string.IsNullOrEmpty(actor.CarriedObjectId) ? "-" : actor.CarriedObjectId,
                    actor.ChannelTicks.ToString()
                }));
            }

            for (int index = 0; index < definition.Nodes.Length; index++)
            {
                if (!mission.TryGetNode(definition.Nodes[index].NodeId, out OperationsTacticalNodeState node))
                    continue;
                lines.Add("node|" + node.NodeId + "|" + (int)node.Phase + "|" + node.ProgressTicks + "|" + node.ProgressCount);
            }

            for (int index = 0; index < definition.Waves.Length; index++)
            {
                int group = definition.Waves[index].Group;
                lines.Add("wave|" + group + "|" + (mission.IsWaveArmed(group) ? "1" : "0") + "|" + (mission.IsWaveSpawned(group) ? "1" : "0"));
            }

            OperationsTacticalFact[] facts = mission.CopyFacts();
            for (int index = 0; index < facts.Length; index++)
                lines.Add("fact|" + (int)facts[index].Kind + "|" + facts[index].ObjectId + "|" + facts[index].Tick + "|" + facts[index].Sequence);

            for (int index = 0; index < orders.Count; index++)
            {
                OperationsLoopOrder order = orders[index];
                lines.Add("order|" + order.Kind + "|" + Token(order.A) + "|" + Token(order.B) + "|" + order.N);
            }

            string body = string.Join("\n", lines);
            return body + "\nchecksum=" + Checksum(body);
        }

        public static bool TryRead(string text, out OperationsCheckpointImage image, out string error)
        {
            image = null;
            error = string.Empty;
            if (string.IsNullOrEmpty(text))
            {
                error = "empty";
                return false;
            }

            int split = text.LastIndexOf("\nchecksum=", StringComparison.Ordinal);
            if (split < 0)
            {
                error = "checksum_missing";
                return false;
            }

            string body = text.Substring(0, split);
            string checksum = text.Substring(split + "\nchecksum=".Length);
            if (checksum != Checksum(body))
            {
                error = "checksum";
                return false;
            }

            var parsed = new OperationsCheckpointImage { Checksum = checksum };
            string[] lines = body.Split('\n');
            for (int index = 0; index < lines.Length; index++)
            {
                string line = lines[index];
                if (line.StartsWith("session=", StringComparison.Ordinal))
                    parsed.SessionId = line.Substring("session=".Length);
                else if (line.StartsWith("content=", StringComparison.Ordinal))
                    parsed.ContentHash = line.Substring("content=".Length);
                else if (line.StartsWith("tick=", StringComparison.Ordinal))
                    parsed.Tick = int.Parse(line.Substring("tick=".Length));
                else if (line.StartsWith("paused=", StringComparison.Ordinal))
                    parsed.Paused = line.EndsWith("1", StringComparison.Ordinal);
                else if (line.StartsWith("materials=", StringComparison.Ordinal))
                    parsed.Materials = int.Parse(line.Substring("materials=".Length));
                else if (line.StartsWith("order|", StringComparison.Ordinal))
                {
                    string[] fields = line.Split('|');
                    parsed.Orders.Add(new OperationsLoopOrder(byte.Parse(fields[1]), Untoken(fields[2]), Untoken(fields[3]), int.Parse(fields[4])));
                }
            }

            image = parsed;
            return true;
        }

        public static string Corrupt(string text)
        {
            int split = text.LastIndexOf("\nchecksum=", StringComparison.Ordinal);
            if (split < 0)
                return text + "\nchecksum=deadbeef";
            return text.Substring(0, split) + "\nchecksum=deadbeef";
        }

        private static string Checksum(string body)
        {
            uint hash = 2166136261u;
            hash = OperationsStableIds.Mix(hash, body);
            return OperationsStableIds.Hex8(hash);
        }

        private static int Mm(float value) => (int)Math.Round(value * 1000.0, MidpointRounding.AwayFromZero);

        private static string Token(string value) => string.IsNullOrEmpty(value) ? "-" : value;

        private static string Untoken(string value) => value == "-" ? string.Empty : value;
    }
}
