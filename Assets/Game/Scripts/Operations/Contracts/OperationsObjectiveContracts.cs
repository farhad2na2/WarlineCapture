using System;
using System.Collections.Generic;

namespace Game.Operations.Contracts
{
    public enum OperationsObjectiveRuleKind : byte
    {
        None = 0,
        Scan = 1,
        Visit = 2,
        Hold = 3,
        Clear = 4,
        Interact = 5,
        Repair = 6,
        Escort = 7,
        Rescue = 8,
        Airlift = 9,
        Stop = 10,
        Breach = 11,
        Extract = 12,
        Protect = 13
    }

    public enum OperationsActivationPolicyKind : byte
    {
        Prerequisites = 0,
        Launch = 1
    }

    public enum OperationsFailurePolicyKind : byte
    {
        Node = 0,
        Mission = 1
    }

    public enum OperationsGraphJoinKind : byte
    {
        AllOf = 0,
        AnyOf = 1
    }

    public readonly struct OperationsObjectiveNodeSchema : IEquatable<OperationsObjectiveNodeSchema>
    {
        public OperationsObjectiveNodeSchema(
            string nodeId,
            OperationsObjectiveRuleKind ruleKind,
            string[] roleBindings,
            int targetCount,
            int durationTicks,
            int deadlineTicks,
            int radiusCells,
            string[] prerequisiteNodeIds,
            OperationsActivationPolicyKind activationPolicy,
            OperationsFailurePolicyKind failurePolicy,
            OperationsGraphJoinKind joinKind,
            bool optional,
            string localizationKey)
        {
            OperationsContractText.RequireToken(nodeId, nameof(nodeId));
            if (ruleKind == OperationsObjectiveRuleKind.None)
                throw new ArgumentOutOfRangeException(nameof(ruleKind));
            if (targetCount < 1)
                throw new ArgumentOutOfRangeException(nameof(targetCount));
            OperationsContractText.RequireNonNegative(durationTicks, nameof(durationTicks));
            OperationsContractText.RequireNonNegative(deadlineTicks, nameof(deadlineTicks));
            OperationsContractText.RequireNonNegative(radiusCells, nameof(radiusCells));
            OperationsContractText.RequireToken(localizationKey, nameof(localizationKey));

            NodeId = nodeId;
            RuleKind = ruleKind;
            RoleBindings = roleBindings ?? Array.Empty<string>();
            TargetCount = targetCount;
            DurationTicks = durationTicks;
            DeadlineTicks = deadlineTicks;
            RadiusCells = radiusCells;
            PrerequisiteNodeIds = prerequisiteNodeIds ?? Array.Empty<string>();
            ActivationPolicy = activationPolicy;
            FailurePolicy = failurePolicy;
            JoinKind = joinKind;
            Optional = optional;
            LocalizationKey = localizationKey;
        }

        public string NodeId { get; }
        public OperationsObjectiveRuleKind RuleKind { get; }
        public string[] RoleBindings { get; }
        public int TargetCount { get; }
        public int DurationTicks { get; }
        public int DeadlineTicks { get; }
        public int RadiusCells { get; }
        public string[] PrerequisiteNodeIds { get; }
        public OperationsActivationPolicyKind ActivationPolicy { get; }
        public OperationsFailurePolicyKind FailurePolicy { get; }
        public OperationsGraphJoinKind JoinKind { get; }
        public bool Optional { get; }
        public string LocalizationKey { get; }

        public bool Equals(OperationsObjectiveNodeSchema other) =>
            NodeId == other.NodeId &&
            RuleKind == other.RuleKind &&
            TargetCount == other.TargetCount &&
            DurationTicks == other.DurationTicks &&
            DeadlineTicks == other.DeadlineTicks &&
            RadiusCells == other.RadiusCells &&
            ActivationPolicy == other.ActivationPolicy &&
            FailurePolicy == other.FailurePolicy &&
            JoinKind == other.JoinKind &&
            Optional == other.Optional &&
            LocalizationKey == other.LocalizationKey;

        public override bool Equals(object obj) => obj is OperationsObjectiveNodeSchema other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(NodeId, RuleKind, TargetCount, Optional);
    }

    public readonly struct OperationsObjectiveGraphSchema
    {
        public OperationsObjectiveGraphSchema(string graphId, int schemaVersion, OperationsObjectiveNodeSchema[] nodes)
        {
            OperationsContractText.RequireToken(graphId, nameof(graphId));
            if (schemaVersion < 1)
                throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            GraphId = graphId;
            SchemaVersion = schemaVersion;
            Nodes = nodes ?? Array.Empty<OperationsObjectiveNodeSchema>();
        }

        public string GraphId { get; }
        public int SchemaVersion { get; }
        public OperationsObjectiveNodeSchema[] Nodes { get; }

        public bool TryValidate(out string error)
        {
            if (Nodes.Length == 0)
            {
                error = $"Objective graph '{GraphId}' has no nodes.";
                return false;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < Nodes.Length; index++)
            {
                OperationsObjectiveNodeSchema node = Nodes[index];
                if (!ids.Add(node.NodeId))
                {
                    error = $"Objective graph '{GraphId}' has duplicate node '{node.NodeId}'.";
                    return false;
                }
            }

            for (int index = 0; index < Nodes.Length; index++)
            {
                OperationsObjectiveNodeSchema node = Nodes[index];
                for (int prerequisite = 0; prerequisite < node.PrerequisiteNodeIds.Length; prerequisite++)
                {
                    string required = node.PrerequisiteNodeIds[prerequisite];
                    if (!ids.Contains(required))
                    {
                        error = $"Objective graph '{GraphId}' node '{node.NodeId}' references missing prerequisite '{required}'.";
                        return false;
                    }

                    if (string.Equals(required, node.NodeId, StringComparison.Ordinal))
                    {
                        error = $"Objective graph '{GraphId}' node '{node.NodeId}' depends on itself.";
                        return false;
                    }
                }
            }

            if (HasCycle())
            {
                error = $"Objective graph '{GraphId}' contains a cycle.";
                return false;
            }

            error = null;
            return true;
        }

        private bool HasCycle()
        {
            var outgoing = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            for (int index = 0; index < Nodes.Length; index++)
                outgoing[Nodes[index].NodeId] = new List<string>();
            for (int index = 0; index < Nodes.Length; index++)
            {
                OperationsObjectiveNodeSchema node = Nodes[index];
                for (int prerequisite = 0; prerequisite < node.PrerequisiteNodeIds.Length; prerequisite++)
                    outgoing[node.PrerequisiteNodeIds[prerequisite]].Add(node.NodeId);
            }

            var state = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (OperationsObjectiveNodeSchema node in Nodes)
            {
                if (Visit(node.NodeId, outgoing, state))
                    return true;
            }

            return false;
        }

        private static bool Visit(
            string nodeId,
            Dictionary<string, List<string>> outgoing,
            Dictionary<string, int> state)
        {
            if (state.TryGetValue(nodeId, out int mark))
            {
                if (mark == 1)
                    return true;
                if (mark == 2)
                    return false;
            }

            state[nodeId] = 1;
            List<string> next = outgoing[nodeId];
            for (int index = 0; index < next.Count; index++)
            {
                if (Visit(next[index], outgoing, state))
                    return true;
            }

            state[nodeId] = 2;
            return false;
        }
    }
}
