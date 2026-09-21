using Game.UI.Contracts;
using Unity.Entities;
using UnityEngine;

namespace Game.UI.Shell.Contracts.Ecs
{
    public struct AriaPlayObservationComponent : IComponentData
    {
        public AriaPlayObservationKind Kind;
        public int TargetId, GoalId, Frame;
        public Vector2 Position, DragEnd;
        public byte Drag;
        public float Time;
    }
    public struct AriaPlaySessionComponent : IComponentData
    {
        public AriaPlayPhase Phase;
        public int TargetId, GoalId, Attempts, Actions;
        public Vector2 Target, Contact, DragEnd;
        public byte Drag;
        public float DueAt, LastProgressAt;
        public float LastObjectiveProgressAt;
        public ulong VisitedGoalMask;
        public byte ObjectiveWatchdogInitialized;
        public byte GestureRequested, Pressed, StopReason;
    }
}
