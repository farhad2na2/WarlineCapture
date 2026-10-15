using System;
using Game.Configs;
using Game.Skirmish.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class UiSkirmishPublicObjectiveModel
    {
        public bool Applicable, Available, Paused, CommandFeedbackReady;
        public string AttemptId, UnavailableText,PolicyUnavailableText;
        public SkirmishPublicGoalSnapshot Goal;
        public SkirmishCompiledObjectiveAreas PublicAreas;
        public UiSkirmishPublicGroupRow[] OwnGroups = Array.Empty<UiSkirmishPublicGroupRow>();
        public UiSkirmishPublicGoalPosition[] Positions = Array.Empty<UiSkirmishPublicGoalPosition>();
        public UiSkirmishPublicPerimeter[] Perimeters = Array.Empty<UiSkirmishPublicPerimeter>();
        public float SimulationSeconds;
        public float GroundCellSize;
        public string UnloadStatus;
        public int UnloadCompleted,UnloadPending;
        public bool UnloadScopeCompleted,UnloadPartialPlacement;
        public UiSkirmishPublicOrderReadout Order;
        public int CompletedContactTargetId,CompletedContactFrame;
        public Vector2 CompletedContactPosition;
    }
    public sealed class UiSkirmishPublicGroupRow
    {
        public SkirmishPublicPlayerGroup Policy;
        public string Label;
        public bool Selected;
        public float Health01;
        public int AliveCount;
        public bool MoveActive,HoldActive;
        public uint UnloadCarrierGroupId;
        public Game.UI.Contracts.UiEntityHandle UnloadCarrier;
        public string UnloadCarrierId;
        public Vector2[] CurrentMoveGoals=Array.Empty<Vector2>();
        public Vector2[] OwnMemberPositions = Array.Empty<Vector2>();
    }
    public struct UiSkirmishPublicGoalPosition
    {
        public string PublicId;
        public Vector3 World;
        public bool GroundAvailable;
    }
    public struct UiSkirmishPublicPerimeter
    {
        public string PublicId;
        public SkirmishObjectiveAreaRole Role;
        public Vector3[] WorldPoints;
    }
    public struct UiSkirmishPublicOrderReadout
    {
        public bool Present,Accepted,WorldKnown;
        public uint Revision,GroupId,NavigationGroupId;
        public Game.UI.Contracts.AriaObjectiveAction Action;
        public int Frame,RequestId;
        public Vector2 Screen;
        public Vector3 World;
    }
    public interface IUiSkirmishPublicObjectiveGateway
    {
        bool TryReadSkirmishPublicObjective(out UiSkirmishPublicObjectiveModel model);
        bool TryFocusSkirmishPublicObjective(string publicId);
    }
    public static partial class UiShellRuntimeGateway
    {
        public static bool TryReadSkirmishPublicObjective(out UiSkirmishPublicObjectiveModel model)
        { model=null;return current is IUiSkirmishPublicObjectiveGateway gateway && gateway.TryReadSkirmishPublicObjective(out model); }
        public static bool TryFocusSkirmishPublicObjective(string publicId) =>
            current is IUiSkirmishPublicObjectiveGateway gateway && gateway.TryFocusSkirmishPublicObjective(publicId);
    }
}
