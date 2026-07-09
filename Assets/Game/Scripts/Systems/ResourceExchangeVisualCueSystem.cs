using Game.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public partial struct ResourceExchangeVisualCueSystem : ISystem
    {
        private const float PlaneLandingProgress = 0.10f;
        private const float ResourceTransferProgress = 0.35f;
        private const float PlaneDepartingProgress = 0.75f;

        public void OnUpdate(ref SystemState state)
        {
            foreach (var (
                         enabled,
                         queue,
                         visualRequests,
                         anchors)
                     in SystemAPI.Query<
                         RefRO<ResourceExchangeEnabledComponent>,
                         DynamicBuffer<ResourceExchangeQueueComponent>,
                         DynamicBuffer<ResourceExchangeVisualRequestComponent>,
                         DynamicBuffer<ResourceExchangePresentationAnchorComponent>>())
            {
                EmitVisualCues(enabled.ValueRO, queue, visualRequests, anchors);
            }
        }

        public static void EmitVisualCues(
            in ResourceExchangeEnabledComponent enabled,
            DynamicBuffer<ResourceExchangeQueueComponent> queue,
            DynamicBuffer<ResourceExchangeVisualRequestComponent> visualRequests,
            DynamicBuffer<ResourceExchangePresentationAnchorComponent> anchors)
        {
            if (enabled.Enabled == 0 || enabled.AllowWorldPresentation == 0 || queue.Length == 0)
                return;

            for (int i = 0; i < queue.Length; i++)
            {
                ResourceExchangeQueueComponent item = queue[i];
                byte factionId = item.FactionId != 0 ? item.FactionId : enabled.FactionId;
                if (enabled.FactionId != 0 && factionId != enabled.FactionId)
                    continue;

                bool changed = false;
                if (item.State == ResourceExchangeQueueState.InProgress ||
                    item.State == ResourceExchangeQueueState.Completing)
                {
                    EmitActiveQueueCues(
                        ref item,
                        factionId,
                        visualRequests,
                        anchors,
                        ref changed);
                }
                else if (item.State == ResourceExchangeQueueState.Completed)
                {
                    if (item.VisualCompletionEmitted == 0)
                    {
                        EmitCue(
                            item,
                            factionId,
                            ResourceExchangeVisualCueKind.ExchangeCompleted,
                            ResourceExchangePresentationAnchorKind.BaseDepot,
                            visualRequests,
                            anchors,
                            out _);
                        item.VisualCompletionEmitted = 1;
                        changed = true;
                    }
                }
                else if (item.State == ResourceExchangeQueueState.Cancelled)
                {
                    if (item.VisualCancellationEmitted == 0)
                    {
                        EmitCue(
                            item,
                            factionId,
                            ResourceExchangeVisualCueKind.ExchangeCancelled,
                            ResourceExchangePresentationAnchorKind.FallbackSafe,
                            visualRequests,
                            anchors,
                            out _);
                        item.VisualCancellationEmitted = 1;
                        changed = true;
                    }
                }

                if (changed)
                    queue[i] = item;
            }
        }

        private static void EmitActiveQueueCues(
            ref ResourceExchangeQueueComponent item,
            byte factionId,
            DynamicBuffer<ResourceExchangeVisualRequestComponent> visualRequests,
            DynamicBuffer<ResourceExchangePresentationAnchorComponent> anchors,
            ref bool changed)
        {
            if (item.VisualStartedEmitted == 0)
            {
                EmitCue(
                    item,
                    factionId,
                    ResourceExchangeVisualCueKind.ExchangeStarted,
                    ResourceExchangePresentationAnchorKind.BaseDepot,
                    visualRequests,
                    anchors,
                    out _);
                item.VisualStartedEmitted = 1;
                changed = true;
            }

            float progress = CalculateProgress01(item);
            if (progress >= PlaneLandingProgress && item.VisualLandingEmitted == 0)
            {
                EmitPresentationCue(
                    ref item,
                    factionId,
                    ResourceExchangeVisualCueKind.TransportPlaneLanding,
                    ResourceExchangePresentationAnchorKind.RunwayLandingZone,
                    visualRequests,
                    anchors,
                    ref changed);
                item.VisualLandingEmitted = 1;
            }

            if (item.RouteType == ResourceExchangeRouteType.Export)
            {
                if (progress >= ResourceTransferProgress && item.VisualLoadEmitted == 0)
                {
                    EmitPresentationCue(
                        ref item,
                        factionId,
                        ResourceExchangeVisualCueKind.ExportLoadStarted,
                        ResourceExchangePresentationAnchorKind.Storage,
                        visualRequests,
                        anchors,
                        ref changed);
                    item.VisualLoadEmitted = 1;
                }
            }
            else if (progress >= ResourceTransferProgress && item.VisualUnloadEmitted == 0)
            {
                EmitPresentationCue(
                    ref item,
                    factionId,
                    ResourceExchangeVisualCueKind.ImportUnloadStarted,
                    ResourceExchangePresentationAnchorKind.Storage,
                    visualRequests,
                    anchors,
                    ref changed);
                item.VisualUnloadEmitted = 1;
            }

            if (progress >= PlaneDepartingProgress && item.VisualDepartingEmitted == 0)
            {
                EmitPresentationCue(
                    ref item,
                    factionId,
                    ResourceExchangeVisualCueKind.TransportPlaneDeparting,
                    ResourceExchangePresentationAnchorKind.RunwayLandingZone,
                    visualRequests,
                    anchors,
                    ref changed);
                item.VisualDepartingEmitted = 1;
            }
        }

        private static void EmitPresentationCue(
            ref ResourceExchangeQueueComponent item,
            byte factionId,
            ResourceExchangeVisualCueKind cueKind,
            ResourceExchangePresentationAnchorKind requestedAnchorKind,
            DynamicBuffer<ResourceExchangeVisualRequestComponent> visualRequests,
            DynamicBuffer<ResourceExchangePresentationAnchorComponent> anchors,
            ref bool changed)
        {
            EmitCue(
                item,
                factionId,
                cueKind,
                requestedAnchorKind,
                visualRequests,
                anchors,
                out bool anchorResolved);
            if (anchorResolved)
                item.PresentationStarted = 1;
            changed = true;
        }

        private static void EmitCue(
            in ResourceExchangeQueueComponent item,
            byte factionId,
            ResourceExchangeVisualCueKind cueKind,
            ResourceExchangePresentationAnchorKind requestedAnchorKind,
            DynamicBuffer<ResourceExchangeVisualRequestComponent> visualRequests,
            DynamicBuffer<ResourceExchangePresentationAnchorComponent> anchors,
            out bool anchorResolved)
        {
            anchorResolved = ResourceExchangePresentationAnchorUtility.TryResolveAnchor(
                anchors,
                factionId,
                requestedAnchorKind,
                out ResourceExchangePresentationAnchorComponent anchor,
                out ResourceExchangePresentationAnchorKind resolvedKind,
                out byte usedFallback);

            visualRequests.Add(new ResourceExchangeVisualRequestComponent
            {
                QueueItemId = item.QueueItemId,
                FactionId = factionId,
                CueKind = cueKind,
                RecipeId = item.RecipeId,
                RouteType = item.RouteType,
                InputResource = item.InputResource,
                OutputResource = item.OutputResource,
                InputAmount = item.InputAmount,
                OutputAmount = item.OutputAmount,
                RequestedAnchorKind = requestedAnchorKind,
                ResolvedAnchorKind = anchorResolved ? resolvedKind : ResourceExchangePresentationAnchorKind.None,
                AnchorPosition = anchorResolved ? anchor.Position : default,
                AnchorRotation = anchorResolved ? anchor.Rotation : quaternion.identity,
                AnchorRadius = anchorResolved ? anchor.Radius : 0f,
                AnchorResolved = anchorResolved ? (byte)1 : (byte)0,
                UsedFallbackAnchor = usedFallback
            });
        }

        private static float CalculateProgress01(in ResourceExchangeQueueComponent item)
        {
            if (item.DurationSeconds <= 0f)
                return 1f;

            float remaining = math.clamp(item.RemainingSeconds, 0f, item.DurationSeconds);
            return math.saturate(1f - remaining / item.DurationSeconds);
        }
    }
}
