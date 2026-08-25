using Game.Components;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;

namespace Game.UI.Shell.Ecs
{
    public partial struct AssistantCommandIntentSystem
    {
        private static bool IsPreviewIntent(AssistantCommandIntentKind kind)
        {
            return kind == AssistantCommandIntentKind.ShowRecommendation ||
                   kind == AssistantCommandIntentKind.FocusCamera;
        }

        public static bool IsUiSurfacePreview(in AssistantCommandIntentRequestElement request) =>
            IsPreviewIntent(request.Kind) && request.TargetKind == AssistantTargetKind.UiSurface;

        private static bool TryHandleUiSurfacePreview(
            in AssistantCommandIntentRequestElement request,
            DynamicBuffer<AssistantPreviewHighlightElement> highlights,
            DynamicBuffer<AssistantCommandIntentResultElement> results,
            ref AssistantStateComponent assistantState)
        {
            if (!IsUiSurfacePreview(in request))
                return false;

            ClearPreviewHighlight(highlights);
            AddResult(results, request, AssistantCommandIntentStatus.Accepted, ReasonAccepted,
                new FixedString64Bytes("UI target preview queued."));
            AddResult(results, request, AssistantCommandIntentStatus.Completed, ReasonAccepted,
                new FixedString64Bytes("UI target preview active."));
            assistantState.ControlState = AssistantControlState.AssistantPreview;
            assistantState.ActiveRecommendationId = request.RecommendationId;
            assistantState.UiDirty = 1;
            return true;
        }
    }
}
