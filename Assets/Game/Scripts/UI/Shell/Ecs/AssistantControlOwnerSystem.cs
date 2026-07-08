using Game.Components;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.UI.Shell.Ecs
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(AssistantCommandIntentSystem))]
    public partial struct AssistantControlOwnerSystem : ISystem
    {
        private const int DefaultMaxTakeoverActionCount = 3;
        private const float DefaultTakeoverSeconds = 30f;

        private EntityQuery boundaryQuery;

        public void OnCreate(ref SystemState state)
        {
            boundaryQuery = state.GetEntityQuery(ComponentType.ReadOnly<UiShellRootComponent>());
            state.RequireForUpdate(boundaryQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity boundary = boundaryQuery.GetSingletonEntity();
            AssistantGoalReadModelSystem.EnsureAssistantReadModelBoundary(ref state, boundary);
            EnsureControlOwnerBoundary(ref state, boundary);

            EntityManager em = state.EntityManager;
            AssistantStateComponent assistantState = em.GetComponentData<AssistantStateComponent>(boundary);
            AssistantControlOwnerComponent owner = em.GetComponentData<AssistantControlOwnerComponent>(boundary);
            DynamicBuffer<AssistantCommandIntentResultElement> results =
                em.GetBuffer<AssistantCommandIntentResultElement>(boundary);

            float now = UnityEngine.Time.time;
            bool assistantStateChanged = false;
            bool ownerChanged = false;

            if (assistantState.ControlState == AssistantControlState.Player ||
                owner.CancelRequested != 0 ||
                owner.PlayerOverrideRequested != 0)
            {
                ResetToPlayer(ref assistantState, ref owner);
                assistantStateChanged = true;
                ownerChanged = true;
            }
            else if (owner.State != assistantState.ControlState)
            {
                BeginOwnershipState(ref owner, assistantState, results, now);
                ownerChanged = true;
            }
            else
            {
                if (owner.ActiveRecommendationId != assistantState.ActiveRecommendationId)
                {
                    owner.ActiveRecommendationId = assistantState.ActiveRecommendationId;
                    ownerChanged = true;
                }

                if (owner.State == AssistantControlState.AssistantTakeover)
                {
                    if (ConsumeTakeoverActionResults(ref owner, results))
                        ownerChanged = true;

                    if (HasTakeoverExpired(owner, now) || HasReachedActionLimit(owner))
                    {
                        ResetToPlayer(ref assistantState, ref owner);
                        assistantStateChanged = true;
                        ownerChanged = true;
                    }
                }
            }

            if (assistantStateChanged)
                em.SetComponentData(boundary, assistantState);
            if (ownerChanged)
                em.SetComponentData(boundary, owner);
        }

        internal static void EnsureControlOwnerBoundary(ref SystemState state, Entity boundary)
        {
            EntityManager em = state.EntityManager;
            if (!em.HasComponent<AssistantControlOwnerComponent>(boundary))
                em.AddComponentData(boundary, default(AssistantControlOwnerComponent));

            if (!em.HasBuffer<AssistantCommandIntentResultElement>(boundary))
                em.AddBuffer<AssistantCommandIntentResultElement>(boundary);
        }

        private static void BeginOwnershipState(
            ref AssistantControlOwnerComponent owner,
            AssistantStateComponent assistantState,
            DynamicBuffer<AssistantCommandIntentResultElement> results,
            float now)
        {
            owner.State = assistantState.ControlState;
            owner.ActiveRecommendationId = assistantState.ActiveRecommendationId;
            owner.CancelRequested = 0;
            owner.PlayerOverrideRequested = 0;
            owner.ActiveIntentRequestId = LatestResultRequestId(results);

            if (assistantState.ControlState == AssistantControlState.AssistantTakeover)
            {
                owner.ActionCount = 0;
                owner.MaxActionCount = DefaultMaxTakeoverActionCount;
                owner.StartedAt = now;
                owner.TimeoutAt = now + DefaultTakeoverSeconds;
            }
            else
            {
                owner.ActionCount = 0;
                owner.MaxActionCount = 0;
                owner.StartedAt = 0f;
                owner.TimeoutAt = 0f;
            }
        }

        private static bool ConsumeTakeoverActionResults(
            ref AssistantControlOwnerComponent owner,
            DynamicBuffer<AssistantCommandIntentResultElement> results)
        {
            bool changed = false;
            int latestRequestId = owner.ActiveIntentRequestId;
            int actionCount = owner.ActionCount;

            for (int i = 0; i < results.Length; i++)
            {
                AssistantCommandIntentResultElement result = results[i];
                if (result.RequestId <= owner.ActiveIntentRequestId)
                    continue;

                latestRequestId = math.max(latestRequestId, result.RequestId);
                if (CountsAsTakeoverAction(result))
                {
                    actionCount++;
                    changed = true;
                }
            }

            if (latestRequestId != owner.ActiveIntentRequestId)
            {
                owner.ActiveIntentRequestId = latestRequestId;
                changed = true;
            }

            if (actionCount != owner.ActionCount)
            {
                owner.ActionCount = actionCount;
                changed = true;
            }

            return changed;
        }

        private static bool CountsAsTakeoverAction(AssistantCommandIntentResultElement result)
        {
            if (result.Status == AssistantCommandIntentStatus.Completed)
                return true;

            return result.Status == AssistantCommandIntentStatus.Accepted &&
                   result.Kind != AssistantCommandIntentKind.ShowRecommendation &&
                   result.Kind != AssistantCommandIntentKind.FocusCamera &&
                   result.Kind != AssistantCommandIntentKind.CancelPreview &&
                   result.Kind != AssistantCommandIntentKind.StopAssistantControl;
        }

        private static bool HasTakeoverExpired(AssistantControlOwnerComponent owner, float now)
        {
            return owner.State == AssistantControlState.AssistantTakeover &&
                   owner.TimeoutAt != 0f &&
                   now >= owner.TimeoutAt;
        }

        private static bool HasReachedActionLimit(AssistantControlOwnerComponent owner)
        {
            return owner.State == AssistantControlState.AssistantTakeover &&
                   owner.MaxActionCount > 0 &&
                   owner.ActionCount >= owner.MaxActionCount;
        }

        private static int LatestResultRequestId(DynamicBuffer<AssistantCommandIntentResultElement> results)
        {
            int latestRequestId = 0;
            for (int i = 0; i < results.Length; i++)
                latestRequestId = math.max(latestRequestId, results[i].RequestId);
            return latestRequestId;
        }

        private static void ResetToPlayer(
            ref AssistantStateComponent assistantState,
            ref AssistantControlOwnerComponent owner)
        {
            assistantState.ControlState = AssistantControlState.Player;
            assistantState.ActiveRecommendationId = 0;
            assistantState.UiDirty = 1;

            owner.State = AssistantControlState.Player;
            owner.ActiveIntentRequestId = 0;
            owner.ActiveRecommendationId = 0;
            owner.ActionCount = 0;
            owner.MaxActionCount = 0;
            owner.StartedAt = 0f;
            owner.TimeoutAt = 0f;
            owner.CancelRequested = 0;
            owner.PlayerOverrideRequested = 0;
        }
    }
}
