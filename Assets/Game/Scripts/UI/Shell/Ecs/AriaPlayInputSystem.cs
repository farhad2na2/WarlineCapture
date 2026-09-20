using Game.Components;
using Game.Missions.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Entities;
using UnityEngine;

namespace Game.UI.Shell.Ecs
{
    /// <summary>Managed InputSystem edge only. Decisions belong to AriaPlayDecisionSystem.</summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class AriaPlayInputSystem : SystemBase
    {
        private AriaTouchInputUiSystemHelper touch;
        private EntityQuery missionQuery, skirmishQuery;
        protected override void OnCreate()
        {
            touch = new AriaTouchInputUiSystemHelper();
            skirmishQuery = GetEntityQuery(ComponentType.ReadOnly<SkirmishMatchState>());
            missionQuery = GetEntityQuery(ComponentType.ReadOnly<CampaignMissionRuntimeComponent>());
        }
        protected override void OnDestroy() { touch?.Dispose(); }
        public void Cancel() => touch?.Stop();

        // A completed campaign component can remain when the player starts Skirmish.
        // The current Skirmish owns its own terminal state.
        public static bool IsCurrentMatchFinished(bool hasSkirmish, SkirmishPhase skirmishPhase,
            bool hasCampaign, MissionOutcomeKind campaignOutcome) => hasSkirmish
                ? skirmishPhase == SkirmishPhase.Finished
                : hasCampaign && campaignOutcome != MissionOutcomeKind.None;

        protected override void OnUpdate()
        {
            bool found = false;
            // Terminal outcome is a public lifecycle signal, never a targeting/planning input.
            bool hasSkirmish = skirmishQuery.CalculateEntityCount() == 1;
            bool hasCampaign = missionQuery.CalculateEntityCount() == 1;
            bool ended = IsCurrentMatchFinished(hasSkirmish,
                hasSkirmish ? skirmishQuery.GetSingleton<SkirmishMatchState>().Phase : default,
                hasCampaign, hasCampaign ? missionQuery.GetSingleton<CampaignMissionRuntimeComponent>().Outcome : default);
            foreach (var (observation, session, shell) in SystemAPI.Query<RefRO<AriaPlayObservationComponent>, RefRW<AriaPlaySessionComponent>, RefRO<UiShellStateComponent>>())
            {
                found = true;
                ref var current = ref session.ValueRW;
                if (current.Phase is AriaPlayPhase.Manual or AriaPlayPhase.Blocked)
                { if (touch.IsRunning) touch.Stop(); current.Pressed = 0; continue; }
                if (ended || shell.ValueRO.ActiveRoute != UIRoute.Match || !Application.isFocused || UnityEngine.Time.timeScale <= 0 ||
                    (current.Phase != AriaPlayPhase.Starting && UnityEngine.Time.frameCount - observation.ValueRO.Frame > 5) ||
                    observation.ValueRO.Kind == AriaPlayObservationKind.Finished)
                {
                    touch.Stop(); current.Phase = AriaPlayPhase.Manual; current.Pressed = current.GestureRequested = 0;
                    current.StopReason = (byte)(ended ? 5 : !Application.isFocused ? 2 : UnityEngine.Time.timeScale <= 0 ? 3 : shell.ValueRO.ActiveRoute != UIRoute.Match ? 1 : 4);
                    continue;
                }
                if (current.Phase == AriaPlayPhase.Starting)
                {
                    if (UnityEngine.Time.unscaledTime < current.DueAt) continue;
                    if (UnityEngine.Time.frameCount - observation.ValueRO.Frame > 5)
                    {
                        if (UnityEngine.Time.unscaledTime - current.DueAt > 3)
                        { current.Phase = AriaPlayPhase.Blocked; current.StopReason = 4; }
                        continue;
                    }
                    if (touch.Start()) current.Phase = AriaPlayPhase.Observing;
                    else if (UnityEngine.Time.unscaledTime - current.DueAt > 3) current.Phase = AriaPlayPhase.Blocked;
                    continue;
                }
                if (!touch.IsRunning) { current.Phase = AriaPlayPhase.Manual; current.Pressed = current.GestureRequested = 0; current.StopReason = 6; continue; }
                if (current.GestureRequested != 0)
                {
                    current.GestureRequested = 0;
                    if (!touch.TryGesture(current.Target, current.Drag != 0 ? current.DragEnd : current.Target,
                        current.Drag != 0 ? .5f : hasSkirmish ? .15f : .3f, current.Drag != 0 ? .9f : 0, UnityEngine.Time.unscaledTime))
                    { touch.Stop(); current.Phase = AriaPlayPhase.Blocked; continue; }
                }
                touch.Tick(UnityEngine.Time.unscaledTime);
                current.Contact = touch.ContactPosition;
                current.Pressed = (byte)(touch.IsPressed ? 1 : 0);
                if (current.Phase == AriaPlayPhase.Touching && !touch.IsBusy)
                {
                    current.Actions++;
                    current.Phase = AriaPlayPhase.Verifying;
                    current.DueAt = UnityEngine.Time.unscaledTime + (hasSkirmish ? .65f : 1.5f);
                }
            }
            if (!found && touch.IsRunning) touch.Stop();
        }
    }
}
