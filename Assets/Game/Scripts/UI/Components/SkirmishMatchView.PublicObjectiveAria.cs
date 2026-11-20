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
        // W37g (CE): the escort cover point ahead of the covered truck (null when none is published).
        public string CoverId;
        public uint CoverTruckGroup;
        public UnityEngine.Vector3 CoverWorld;
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
        { PublicObjectiveAriaExpected=nonBaseAssault;publicObjectiveFrame=null;publicObjectiveWithdrawReason=1; }
        // Shared HUD production calls this at its rendered-frame boundary; passing
        // null invalidates immediately. It must not publish hidden native facts.
        public void PresentPublicObjectiveForAria(AriaPresentedObjectiveFrame frame)
        { publicObjectiveFrame=frame;if(frame!=null){PublicObjectiveAriaExpected=true;publicObjectiveWithdrawReason=0;} }
        // Withdraws the frame and records why (evidence for [AriaObjective] event=frame_missing).
        // 2x model unavailable (UiSkirmishPublicObjectiveModel.UnavailableReason x), 3 goal node count,
        // 4 goal HUD hidden (drawer, full map, shell transition), 5 command feedback or ground cell, 6 no camera.
        private void WithdrawPublicObjectiveForAria(int reason)
        { PresentPublicObjectiveForAria(null);publicObjectiveWithdrawReason=reason; }
        private int publicObjectiveWithdrawReason;
        // 0 when TryReadPublicObjectiveForAria(frame) succeeds; 7 a rendered frame older than the previous one,
        // 8 an unpresented frame, 9 none rendered yet; otherwise the withdraw reason above (1 HUD redeclared).
        public int PublicObjectiveAriaUnavailableReason(int frame)
        {
            if (TryReadPublicObjectiveForAria(frame, out _)) return 0;
            if (publicObjectiveFrame == null) return publicObjectiveWithdrawReason != 0 ? publicObjectiveWithdrawReason : 9;
            return !publicObjectiveFrame.Presented || publicObjectiveFrame.Goal == null ? 8 : 7;
        }
        // The goal HUD renders in SkirmishMatchView.Update; ARIA's observer runs in the match UI update, whose
        // order against this MonoBehaviour is not fixed. A frame rendered on the previous frame is exactly what
        // the player sees now (lane 22: every S007/S013/S019 start failed this read on the first frame).
        public bool TryReadPublicObjectiveForAria(int frame, out AriaPresentedObjectiveFrame value)
        {
            value = publicObjectiveFrame;
            return value != null && value.Presented && (value.Frame == frame || value.Frame == frame - 1) && value.Goal != null;
        }
    }
}
