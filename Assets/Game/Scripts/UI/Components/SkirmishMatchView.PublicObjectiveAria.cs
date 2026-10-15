using System;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    // Populated by the shared goal HUD only after that exact snapshot, inspected
    // route, roster and acceptance feedback have been rendered to the player.
    // Contains no Entity, hidden model, native collector or command service.
    public sealed class AriaPresentedObjectiveFrame
    {
        public SkirmishPublicGoalSnapshot Goal;
        public AriaPresentedConvoyService ConvoyService;
        public UiSkirmishPublicObjectiveModel OwnModel;
        public SkirmishPublicPlayerGroup[] Groups = Array.Empty<SkirmishPublicPlayerGroup>();
        public SkirmishPublicPlayerApproach[] Approaches = Array.Empty<SkirmishPublicPlayerApproach>();
        public AriaPresentedObjectiveTarget[] Targets = Array.Empty<AriaPresentedObjectiveTarget>();
        public bool Presented, CameraSettled, MoveMode, Paused;
        public float SimulationSeconds;
        public uint SignificantPublicEventRevision;
        public int Frame;
        public uint RosterRevision, PresentationEpoch, ExclusiveSelectedGroup, AcceptedOrderRevision, AcceptedGroup, PublicProgressRevision;
        public int AcceptedContactTargetId, AcceptedFrame;
        public uint AcceptedNavigationGroupId;
        public string AcceptedGoalId;
        public AriaObjectiveAction AcceptedAction;
        public bool ProgressKnown;
    }
    public struct AriaPresentedObjectiveTarget
    {
        public string PublicId;
        public uint GroupId,NavigationGroupId;
        public Button FocusButton, UnloadButton,OpenUnloadButton;
        public AriaTouchTarget Ground;
        public UnityEngine.Vector3 PublicWorld;
        public bool InView, GroundFitsPublicGoal;
    }
    public sealed partial class SkirmishMatchView
    {
        private AriaPresentedObjectiveFrame publicObjectiveFrame;
        public bool PublicObjectiveAriaExpected { get; private set; }
        // The shared HUD declares this from the selected objective definition at
        // attempt startup, before ARIA consent. Missing frames then fail closed.
        public void DeclarePublicObjectiveAria(bool nonBaseAssault)
        { PublicObjectiveAriaExpected=nonBaseAssault;publicObjectiveFrame=null; }
        // Shared HUD production calls this at its rendered-frame boundary; passing
        // null invalidates immediately. It must not publish hidden native facts.
        public void PresentPublicObjectiveForAria(AriaPresentedObjectiveFrame frame)
        { publicObjectiveFrame=frame;if(frame!=null)PublicObjectiveAriaExpected=true; }
        public bool TryReadPublicObjectiveForAria(int frame, out AriaPresentedObjectiveFrame value)
        {
            value = publicObjectiveFrame;
            return value != null && value.Presented && value.Frame == frame && value.Goal != null;
        }
    }
}
