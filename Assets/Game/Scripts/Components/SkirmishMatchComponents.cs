using Unity.Collections;
using Unity.Entities;

namespace Game.Components
{
    public enum SkirmishPhase : byte { Queued, Preparing, Playing, Finished }
    public enum SkirmishStartupFailureCode : byte { None, BuildingPlacement, Content, Timeout }
    public enum SkirmishOutcome : byte { None, Victory, Defeat, Draw }
    public enum SkirmishEndReason : byte { None, MainBaseDestroyed, BothBasesDestroyed, TimeLimit, Surrender }

    public struct SkirmishMatchState : IComponentData
    {
        public FixedString64Bytes SessionId;
        public int Seed;
        public int ScenarioIndex;
        public SkirmishPhase Phase;
        public SkirmishStartupFailureCode StartupFailure;
        public SkirmishOutcome Outcome;
        public SkirmishEndReason Reason;
        public Entity PlayerMainBase, EnemyMainBase;
        public float ElapsedSeconds;
        public int PlayerUnitsLost, EnemyUnitsLost, PlayerBuildingsLost, EnemyBuildingsLost;
        public byte ResultSaved;
        public byte SurrenderRequested;
    }

    public struct SkirmishCombatTuned : IComponentData { }
    public struct SkirmishRallyAssigned : IComponentData { }
    public struct SkirmishBaseDefender : IComponentData { }
    public struct SkirmishSceneryNormalized : IComponentData { }

    public struct SkirmishSquadMember : IComponentData { public byte Slot; }
    public enum SkirmishAction : byte { FocusPlayer, FocusEnemy, Surrender, Replay, AdjustSetup, MainMenu, Restart }
    public struct SkirmishActionRequest : IBufferElementData { public SkirmishAction Action; }
    public struct SkirmishTrackedUnit : IBufferElementData
    { public Entity Entity; public byte FactionId; public byte Building; }
    public struct SkirmishReturnRequest : IComponentData { public SkirmishAction Action; public int Seed; public int ScenarioIndex; }

    // Identity is attached once to the starting Barracks, never inferred again after death.
    public struct SkirmishMainBase : IComponentData { public byte FactionId; }
}
