using Game.Operations.Contracts;

namespace Game.Operations.Strategic
{
    /// <summary>
    /// Field layout for the city ECS components in ARCHITECTURE.
    /// IComponentData / ISystem wrappers stay blocked on the Game.Components and
    /// Game.Runtime asmdefs. These records are the package-1 authority.
    /// </summary>
    public struct OperationsRunComponent
    {
        public string RunId;
        public int Revision;
        public int Day;
        public int ActionPoints;
        public OperationsDifficultyKind Difficulty;
        public int Seed;
        public OperationsRunPhaseKind Phase;
        public int ConsecutiveStableDays;
        public int DirectorVersion;
        public uint PrngState;
        public bool CityCompleted;
        public int LiveVictoriesToday;
    }

    public struct OperationsDistrictComponent
    {
        public string DistrictId;
        public int Number;
        public int Security;
        public int Trust;
        public int Infrastructure;
        public int EnemyInfluence;
        public int IntelConfidence;
        public int Heat;
        public int SupplyReadiness;
        public OperationsCivilianDensityKind CivilianDensity;
        public int ChangeVersion;
        public string PublicHintMissionId;
    }

    public struct OperationsSiteStateComponent
    {
        public string SiteId;
        public string DistrictId;
        public OperationsSiteStateKind State;
    }

    public struct OperationsRouteStateComponent
    {
        public string RouteId;
        public string DistrictId;
        public OperationsRouteStateKind State;
    }

    public struct OperationsMilestoneComponent
    {
        public string MissionId;
        public bool Attempted;
        public bool Victory;
        public int AttemptCount;
    }

    public struct OperationsOfferComponent
    {
        public string OfferId;
        public string MissionId;
        public string DistrictId;
        public int Day;
        public bool Deployable;
        public bool Urgent;
    }

    public struct OperationsIncidentComponent
    {
        public string IncidentId;
        public string DistrictId;
        public string MissionId;
        public string OfferId;
        public OperationsIncidentKind Kind;
        public int CreatedDay;
        public int DueDay;
        public string SiteId;
        public string RouteId;
    }

    public struct OperationsActionUseComponent
    {
        public string DistrictId;
        public OperationsAbstractActionKind Kind;
    }

    public struct OperationsAttemptComponent
    {
        public string SessionId;
        public string OfferId;
        public string MissionId;
        public string DistrictId;
        public int AttemptOrdinal;
        public string TransactionId;
        public string SnapshotHash;
        public string ResultHash;
        public byte Phase;
        public bool Practice;
        public bool ApRefunded;
    }

    public struct OperationsCooldownComponent
    {
        public string DistrictId;
        public OperationsIncidentKind Kind;
        public int AvailableOnDay;
    }

    public static class OperationsAttemptPhase
    {
        public const byte Reserved = 1;
        public const byte RolledBack = 2;
        public const byte Settled = 3;
    }
}
