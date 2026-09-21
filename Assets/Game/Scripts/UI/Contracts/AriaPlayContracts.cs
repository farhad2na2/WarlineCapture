using UnityEngine;

namespace Game.UI.Contracts
{
    public enum AriaPlayCapability : byte { None, GuidedCampaign, BaseAssault }
    public enum AriaPlayPhase : byte { Manual, Starting, Observing, Aiming, Touching, Verifying, Waiting, Blocked }
    public enum AriaPlayObservationKind : byte { Unavailable, Control, WorldTarget, Waiting, Cinematic, Finished }
    public readonly struct AriaPlayObservation
    {
        public readonly AriaPlayObservationKind Kind;
        public readonly int TargetId, GoalId, Frame;
        public readonly Vector2 Position;
        public readonly Vector2 DragEnd;
        public readonly bool Drag;
        public readonly float Time;
        public AriaPlayObservation(AriaPlayObservationKind kind, int targetId, int goalId, Vector2 position, int frame, float time,bool drag=false,Vector2 dragEnd=default)
        { Kind = kind; TargetId = targetId; GoalId = goalId; Position = position; Frame = frame; Time = time; Drag=drag; DragEnd=dragEnd; }
    }
    public readonly struct AriaPlayModel
    {
        public readonly AriaPlayPhase Phase;
        public readonly Vector2 Contact, Target;
        public readonly float AimDueAt;
        public readonly bool Pressed;
        public readonly int Actions;
        public bool Active => Phase is not AriaPlayPhase.Manual and not AriaPlayPhase.Blocked;
        public AriaPlayModel(AriaPlayPhase phase, Vector2 contact, bool pressed, int actions, Vector2 target = default, float aimDueAt = 0)
        { Phase = phase; Contact = contact; Pressed = pressed; Actions = actions; Target = target; AimDueAt = aimDueAt; }
    }
    public interface IUiAriaPlayGateway
    {
        AriaPlayCapability ReadAriaPlayCapability();
        void PublishAriaObservation(AriaPlayObservation observation);
        void PublishAriaSkirmishObservation(AriaSkirmishObservation observation);
        AriaSkirmishIntent ReadAriaSkirmishIntent();
        bool TryStartAriaPlay();
        void StopAriaPlay();
        AriaPlayModel ReadAriaPlay();
    }
}
