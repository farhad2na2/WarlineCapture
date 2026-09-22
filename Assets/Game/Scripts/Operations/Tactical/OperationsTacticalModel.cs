using System;
using Game.Operations.Contracts;

namespace Game.Operations.Tactical
{
    public sealed class OperationsTacticalNodeAuthoring
    {
        public string NodeId;
        public OperationsObjectiveRuleKind Rule;
        public string[] RoleIds = Array.Empty<string>();
        public string[] TargetIds = Array.Empty<string>();
        public int TargetCount = 1;
        public int DurationTicks;
        public int RadiusMeters;
        public string[] Prerequisites = Array.Empty<string>();
        public OperationsActivationPolicyKind Activation = OperationsActivationPolicyKind.Launch;
        public OperationsGraphJoinKind Join = OperationsGraphJoinKind.AllOf;
        public bool Optional;
        public string ZoneAnchorId;
        public string RouteId;
        public string[] LegalRouteIds = Array.Empty<string>();
        public bool HiddenUntilObserved;
    }

    public sealed class OperationsTacticalSpawnAuthoring
    {
        public string ObjectId;
        public string RoleId;
        public OperationsRosterRoleKind RosterRole;
        public OperationsTacticalFaction Faction;
        public OperationsTacticalBodyKind Body;
        public string AnchorId;
        public int Group;
        public int Health = 100;
        public bool HasCargo;
    }

    public sealed class OperationsTacticalWaveAuthoring
    {
        public int Group;
        public OperationsWaveTriggerKind Trigger;
        public string TriggerNodeId;
        public int WarningSeconds = OperationsTacticalRules.WaveAWarningSeconds;
        public int ElapsedTicks;
    }

    public sealed class OperationsTacticalAuthoring
    {
        public string GraphId;
        public string MapId;
        public OperationsForcePackageKind ForcePackage = OperationsForcePackageKind.Light;
        public OperationsEnemyPackageKind EnemyPackage = OperationsEnemyPackageKind.Cell;
        public int Materials = 80;
        public int DeadlineTicks;
        public string[] PartialNodeIds = Array.Empty<string>();
        public string[] MandatoryEvidenceIds = Array.Empty<string>();
        public string[] LaunchRestoredSiteIds = Array.Empty<string>();
        public OperationsTacticalNodeAuthoring[] Nodes = Array.Empty<OperationsTacticalNodeAuthoring>();
        public OperationsTacticalSpawnAuthoring[] Spawns = Array.Empty<OperationsTacticalSpawnAuthoring>();
        public OperationsTacticalWaveAuthoring[] Waves = Array.Empty<OperationsTacticalWaveAuthoring>();
        public bool GenerateDefaultEnemySchedule;
    }

    public sealed class OperationsCompiledRoute
    {
        public string RouteId;
        public float[] Xs = Array.Empty<float>();
        public float[] Zs = Array.Empty<float>();
    }

    public sealed class OperationsCompiledNode
    {
        public string NodeId;
        public OperationsObjectiveRuleKind Rule;
        public string[] TargetIds = Array.Empty<string>();
        public int TargetCount;
        public int DurationTicks;
        public int RadiusMeters;
        public string[] Prerequisites = Array.Empty<string>();
        public OperationsActivationPolicyKind Activation;
        public OperationsGraphJoinKind Join;
        public bool Optional;
        public string ZoneAnchorId;
        public float ZoneX;
        public float ZoneZ;
        public OperationsCompiledRoute[] LegalRoutes = Array.Empty<OperationsCompiledRoute>();
        public bool HiddenUntilObserved;
        public bool Required => !Optional;
    }

    public sealed class OperationsCompiledSpawn
    {
        public string ObjectId;
        public string RoleId;
        public OperationsRosterRoleKind RosterRole;
        public OperationsTacticalFaction Faction;
        public OperationsTacticalBodyKind Body;
        public string AnchorId;
        public int Group;
        public float X;
        public float Z;
        public float StagingX;
        public float StagingZ;
        public int Health;
        public bool HasCargo;
        public bool HiddenUntilObserved;
        public bool Commandable;
        public bool OriginalInfantry;
    }

    public sealed class OperationsCompiledWave
    {
        public int Group;
        public OperationsWaveTriggerKind Trigger;
        public string TriggerNodeId;
        public int WarningSeconds;
        public int ElapsedTicks;
    }

    public sealed class OperationsCompiledTactical
    {
        public string GraphId;
        public string MapId;
        public int DistrictNumber;
        public int Materials;
        public int DeadlineTicks;
        public string[] PartialNodeIds = Array.Empty<string>();
        public string[] MandatoryEvidenceIds = Array.Empty<string>();
        public string[] LaunchRestoredSiteIds = Array.Empty<string>();
        public string FirstRequiredNodeId;
        public string FinalRequiredNodeId;
        public OperationsCompiledNode[] Nodes = Array.Empty<OperationsCompiledNode>();
        public OperationsCompiledSpawn[] Spawns = Array.Empty<OperationsCompiledSpawn>();
        public OperationsCompiledWave[] Waves = Array.Empty<OperationsCompiledWave>();
        public float ExitX;
        public float ExitZ;
        public float[] BlockerAx = Array.Empty<float>();
        public float[] BlockerAz = Array.Empty<float>();
        public float[] BlockerBx = Array.Empty<float>();
        public float[] BlockerBz = Array.Empty<float>();
    }

    public readonly struct OperationsTacticalCompileResult
    {
        public OperationsTacticalCompileResult(bool accepted, string error, OperationsCompiledTactical definition)
        {
            Accepted = accepted;
            Error = error ?? string.Empty;
            Definition = definition;
        }

        public bool Accepted { get; }
        public string Error { get; }
        public OperationsCompiledTactical Definition { get; }
    }

    public readonly struct OperationsTacticalCommandResult
    {
        public OperationsTacticalCommandResult(bool accepted, OperationsTacticalRejectKind reason)
        {
            Accepted = accepted;
            Reason = reason;
        }

        public bool Accepted { get; }
        public OperationsTacticalRejectKind Reason { get; }

        public static OperationsTacticalCommandResult Ok() => new(true, OperationsTacticalRejectKind.None);
        public static OperationsTacticalCommandResult Reject(OperationsTacticalRejectKind reason) => new(false, reason);
    }

    public readonly struct OperationsTacticalFact
    {
        public OperationsTacticalFact(OperationsTacticalFactKind kind, string objectId, int tick, int sequence)
        {
            Kind = kind;
            ObjectId = objectId ?? string.Empty;
            Tick = tick;
            Sequence = sequence;
        }

        public OperationsTacticalFactKind Kind { get; }
        public string ObjectId { get; }
        public int Tick { get; }
        public int Sequence { get; }
    }

    public readonly struct OperationsTacticalNodeState
    {
        public OperationsTacticalNodeState(
            string nodeId,
            OperationsObjectiveRuleKind rule,
            bool optional,
            OperationsTacticalNodePhase phase,
            int progressTicks,
            int progressCount,
            int completionTick,
            bool materialsCharged)
        {
            NodeId = nodeId;
            Rule = rule;
            Optional = optional;
            Phase = phase;
            ProgressTicks = progressTicks;
            ProgressCount = progressCount;
            CompletionTick = completionTick;
            MaterialsCharged = materialsCharged;
        }

        public string NodeId { get; }
        public OperationsObjectiveRuleKind Rule { get; }
        public bool Optional { get; }
        public OperationsTacticalNodePhase Phase { get; }
        public int ProgressTicks { get; }
        public int ProgressCount { get; }
        public int CompletionTick { get; }
        public bool MaterialsCharged { get; }
    }

    public readonly struct OperationsTacticalActorState
    {
        public OperationsTacticalActorState(
            string objectId,
            string roleId,
            string sessionId,
            OperationsTacticalFaction faction,
            OperationsTacticalBodyKind body,
            OperationsRosterRoleKind rosterRole,
            bool alive,
            bool spawned,
            bool commandable,
            int group,
            float x,
            float z,
            int health,
            string carriedObjectId,
            int channelTicks)
        {
            ObjectId = objectId;
            RoleId = roleId;
            SessionId = sessionId;
            Faction = faction;
            Body = body;
            RosterRole = rosterRole;
            Alive = alive;
            Spawned = spawned;
            Commandable = commandable;
            Group = group;
            X = x;
            Z = z;
            Health = health;
            CarriedObjectId = carriedObjectId ?? string.Empty;
            ChannelTicks = channelTicks;
        }

        public string ObjectId { get; }
        public string RoleId { get; }
        public string SessionId { get; }
        public OperationsTacticalFaction Faction { get; }
        public OperationsTacticalBodyKind Body { get; }
        public OperationsRosterRoleKind RosterRole { get; }
        public bool Alive { get; }
        public bool Spawned { get; }
        public bool Commandable { get; }
        public int Group { get; }
        public float X { get; }
        public float Z { get; }
        public int Health { get; }
        public string CarriedObjectId { get; }
        public int ChannelTicks { get; }
    }
}
