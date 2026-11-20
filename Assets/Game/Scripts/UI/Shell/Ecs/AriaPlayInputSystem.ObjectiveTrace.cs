using Game.Components;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using UnityEngine;

namespace Game.UI.Shell.Ecs
{
    // Evidence only: the FC/BT/CE objective skill (AriaSkirmishPlanSystem.ObjectiveSkill) as transitions:
    //   [AriaObjective] type=FC|BT|CE event=start|frame_missing|frame_restored|popup|popup_close|frame_lost|
    //   choice|order_tapped|order_accepted|order_retry|order_blocked|escort_offer|escort_accepted|escort_retry|
    //   recruit|score|finished ... t=<match seconds>
    // Reads the planner state and the observation it already consumed; never feeds a decision.
    public partial class AriaPlayInputSystem
    {
        private static readonly string[] ObjectiveTypeCodes = { "unknown", "FC", "BT", "CE" };
        private static readonly string[] ObjectiveFrameActions = { "proceed", "popup", "popup_close", "wait", "block" };
        private bool tracedObjectiveActive, tracedObjectiveFinished;
        private byte tracedObjectiveFrameAction;
        private int tracedObjectiveStage, tracedObjectiveFailures, tracedObjectiveAccepted, tracedEscortFailures, tracedEscortAccepted;
        private uint tracedObjectiveGroup, tracedEscortGroup; private ulong tracedObjectiveKey;
        private AriaObjectiveAction tracedObjectiveAction;
        private AriaObjectiveSupportAction tracedObjectiveSupport;
        private int tracedObjectiveScore;
        private byte tracedRocketeerStage; private int tracedCoverageDone, tracedTruckReleases;
        private static readonly string[] TruckReleaseReasons = { "-", "all", "bound", "none", "truck_first", "hold", "two" };
        private string tracedLatch; private int tracedLatchReleases;

        private void TraceObjective()
        {
            if (!AriaCommandEvidence.Active && !Debug.isDebugBuild) return;
            var query = ariaStateQuery;
            if (query.CalculateEntityCount() != 1) { tracedObjectiveActive = false; return; }
            var v = query.GetSingleton<AriaSkirmishObservationComponent>().Value;
            if (!v.Active || !v.ExpandedSession || !v.PublicObjective.Applicable) { tracedObjectiveActive = false; return; }
            var p = query.GetSingleton<AriaSkirmishPlanComponent>();
            var o = v.PublicObjective; var s = v.ObjectiveScore;
            float t = skirmishQuery.CalculateEntityCount() == 1 ? skirmishQuery.GetSingleton<SkirmishMatchState>().ElapsedSeconds : v.Time;
            string type = ObjectiveTypeCodes[v.ObjectiveType < ObjectiveTypeCodes.Length ? v.ObjectiveType : 0];
            string head = $"[AriaObjective] type={type}";
            if (!tracedObjectiveActive)
            {
                tracedObjectiveActive = true; tracedObjectiveFinished = false; tracedObjectiveFrameAction = 0;
                tracedObjectiveStage = tracedObjectiveFailures = tracedObjectiveAccepted = tracedEscortFailures = tracedEscortAccepted = 0;
                tracedObjectiveGroup = tracedEscortGroup = 0; tracedObjectiveKey = 0; tracedObjectiveAction = AriaObjectiveAction.None;
                tracedObjectiveSupport = AriaObjectiveSupportAction.None; tracedObjectiveScore = 0; tracedRocketeerStage = 0; tracedCoverageDone = 0; tracedTruckReleases = 0;
                tracedLatch = null; tracedLatchReleases = 0;
                Debug.Log($"{head} event=start presented={o.Presented && o.Supported} frameReason={v.ObjectiveFrameReason} infantry={v.Infantry} groups={v.ExpandedTotalGroups} t={t:F2}");
            }
            if (p.ObjectiveFrameAction != tracedObjectiveFrameAction)
            {
                byte prior = tracedObjectiveFrameAction; tracedObjectiveFrameAction = p.ObjectiveFrameAction;
                string e = p.ObjectiveFrameAction switch { 0 => prior == 3 ? "frame_restored" : null, 1 => "popup", 2 => "popup_close", 3 => "frame_missing", 4 => "frame_lost", _ => null };
                if (e != null)
                    Debug.Log($"{head} event={e} frameReason={v.ObjectiveFrameReason} prior={ObjectiveFrameActions[prior < ObjectiveFrameActions.Length ? prior : 0]} drawer={v.DrawerOpen} placement={v.PlacementOpen} map={v.MapOpen} t={t:F2}");
            }
            if (o.Presented && (o.GroupId != tracedObjectiveGroup || o.PublicKey != tracedObjectiveKey || o.Action != tracedObjectiveAction))
            {
                tracedObjectiveGroup = o.GroupId; tracedObjectiveKey = o.PublicKey; tracedObjectiveAction = o.Action;
                Debug.Log($"{head} event=choice group={o.GroupId} goal={o.PublicKey:X} action={o.Action} reason={o.Reason} intent={o.Intent} inView={o.GoalInView} ground={o.Ground.Available} t={t:F2}");
            }
            if (p.ObjectiveLatchReleases > tracedLatchReleases)
                Debug.Log($"[ObjectiveLatch] event=released releases={p.ObjectiveLatchReleases} objective={o.GroupId}/{o.PublicKey:X}/{o.Action} accepted={o.AcceptedGroupId}/{o.AcceptedPublicKey:X}/{o.AcceptedAction} t={t:F2}");
            tracedLatchReleases = p.ObjectiveLatchReleases;
            TraceObjectiveLatch(v, p, t);
            if (p.ObjectiveTouch.Stage != tracedObjectiveStage)
            {
                string e = p.ObjectiveTouch.Stage == 3 ? "order_tapped" : p.ObjectiveTouch.Stage == 4 ? "order_accepted" : null;
                tracedObjectiveStage = p.ObjectiveTouch.Stage;
                if (e != null) Debug.Log($"{head} event={e} group={p.ObjectiveTouch.GroupId} goal={p.ObjectiveTouch.PublicKey:X} action={p.ObjectiveTouch.Action} accepted={p.ObjectiveAccepted} t={t:F2}");
            }
            if (p.ObjectiveFailures > tracedObjectiveFailures)
                Debug.Log($"{head} event={(p.ObjectiveFailures >= Game.Simulation.AriaObjectiveSkillPolicy.MaxOrderFailures ? "order_blocked" : "order_retry")} failures={p.ObjectiveFailures} retryAt={p.ObjectiveRetryAt:F1} t={t:F2}");
            tracedObjectiveFailures = p.ObjectiveFailures;
            var escort = v.ObjectiveEscort;
            if (escort.Presented && escort.GroupId != 0 && escort.GroupId != tracedEscortGroup)
                Debug.Log($"{head} event=escort_offer group={escort.GroupId} goal={escort.PublicKey:X} inView={escort.GoalInView} ground={escort.Ground.Available} t={t:F2}");
            tracedEscortGroup = escort.Presented ? escort.GroupId : 0;
            if (p.EscortAccepted > tracedEscortAccepted)
                Debug.Log($"{head} event=escort_accepted group={p.EscortTouch.GroupId} accepted={p.EscortAccepted} t={t:F2}");
            tracedEscortAccepted = p.EscortAccepted;
            if (p.EscortFailures > tracedEscortFailures)
                Debug.Log($"{head} event=escort_retry failures={p.EscortFailures} t={t:F2}");
            tracedEscortFailures = p.EscortFailures;
            if (p.TruckReleases > tracedTruckReleases)
                Debug.Log($"[ConvoyCadence] escortsAccepted={p.TruckReleaseEscorts}/{p.TruckReleaseCapable} oldCount={p.TruckReleaseEscortsOld} truckReleased={TruckReleaseReasons[p.TruckReleaseReason < TruckReleaseReasons.Length ? p.TruckReleaseReason : 0]} group={o.GroupId} waited={p.TruckReleaseWaited:F1} t={t:F2}");
            tracedTruckReleases = p.TruckReleases;
            if (p.ObjectiveSupport.Action != tracedObjectiveSupport)
            {
                if (p.ObjectiveSupport.Action != AriaObjectiveSupportAction.None)
                    Debug.Log($"{head} event=recruit kind={p.ObjectiveSupport.Action} infantry={v.Infantry} target={v.ObjectiveSupport.InfantryTarget} materials={v.OwnMaterials} t={t:F2}");
                tracedObjectiveSupport = p.ObjectiveSupport.Action;
            }
            if (p.ObjectiveRocketeerStage != tracedRocketeerStage)
            {
                Debug.Log($"{head} event=rocketeer state={(p.ObjectiveRocketeerStage == 1 ? "buying" : v.RocketeerRecruitPending ? "queued" : "done")} materials={v.OwnMaterials} t={t:F2}");
                tracedRocketeerStage = p.ObjectiveRocketeerStage;
            }
            if (p.CoverageDone != tracedCoverageDone || p.ObjectiveCoverageTurn != 0 && tracedCoverageDone < 0)
            {
                Debug.Log($"{head} event=coverage done={p.CoverageDone} current={p.CoverageCurrent} breacher={p.BreacherStage} research={p.ResearchCurrent} build={p.BuildCurrent} afford={v.CoverageObjectiveAffordMask} pending={v.CoveragePendingMask} fielded={v.CoverageFieldedMask} materials={v.OwnMaterials} t={t:F2}");
                tracedCoverageDone = p.CoverageDone;
            }
            if (s.Known)
            {
                int key = unchecked(s.OwnZones * 7 + s.OpposingZones * 31 + s.Completed * 131 + s.Lost * 523 + (s.ExitOpen ? 4099 : 0) + (s.OwnTickets / 50) * 8191 + (s.OpposingTickets / 50) * 65537);
                if (key != tracedObjectiveScore)
                {
                    tracedObjectiveScore = key;
                    Debug.Log($"{head} event=score ownZones={s.OwnZones} opposingZones={s.OpposingZones} tickets={s.OwnTickets}/{s.OpposingTickets} completed={s.Completed}/{s.Required} alive={s.Alive} lost={s.Lost} exitOpen={s.ExitOpen} infantry={v.Infantry} t={t:F2}");
                }
            }
            if ((o.Finished || v.Finished) && !tracedObjectiveFinished)
            {
                tracedObjectiveFinished = true;
                Debug.Log($"{head} event=finished rocketeer={p.ObjectiveRocketeerStage} breacher={p.BreacherStage} research={p.ResearchDone} build={p.BuildDone} ownZones={s.OwnZones} tickets={s.OwnTickets}/{s.OpposingTickets} completed={s.Completed}/{s.Required} lost={s.Lost} orders={p.ObjectiveAccepted} escorts={p.EscortAccepted} coverageDone={p.CoverageDone} t={t:F2}");
            }
        }

        // W37e evidence only: why the presented objective order does or does not act (the planner's finished-objective
        // latch: same group/goal/action/epoch as an accepted ObjectiveTouch). Logged on change.
        private void TraceObjectiveLatch(in AriaSkirmishObservation v, in AriaSkirmishPlanComponent p, float t)
        {
            var o = v.PublicObjective; var c = p.ObjectiveTouch;
            bool sameGoal = c.GroupId == o.GroupId && c.PublicKey == o.PublicKey && c.Action == o.Action && c.PresentationEpoch == o.PresentationEpoch;
            bool pending = c.CommandPending || c.Stage == 3;
            bool wants = o.GroupId != 0 && o.PublicKey != 0 && o.Action != AriaObjectiveAction.None;
            string why = pending ? "pending" : !wants ? "none" : sameGoal && c.Stage == 4 ? "finished" : v.Time < p.ObjectiveRetryAt ? "retry" :
                sameGoal && c.Stage != 0 ? "in_sequence" : "ready";
            string key = $"{why}|{o.GroupId}|{o.PublicKey:X}|{o.Action}|{o.PresentationEpoch}|{c.GroupId}|{c.PublicKey:X}|{c.Action}|{c.PresentationEpoch}|{c.Stage}|{(why == "ready" ? p.Intent.ToString() : "")}";
            if (key == tracedLatch) return;
            tracedLatch = key;
            Debug.Log($"[ObjectiveLatch] why={why} finished={(sameGoal && c.Stage == 4 ? 1 : 0)} objective={o.GroupId}/{o.PublicKey:X}/{o.Action}/epoch{o.PresentationEpoch} touch={c.GroupId}/{c.PublicKey:X}/{c.Action}/epoch{c.PresentationEpoch}/stage{c.Stage} intent={p.Intent} owner={p.ObjectiveOwner} retryAt={p.ObjectiveRetryAt:F1} t={t:F2}");
        }
    }
}
