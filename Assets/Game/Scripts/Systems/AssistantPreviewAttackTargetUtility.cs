using Game.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Runtime
{
    internal static class AssistantPreviewAttackTargetUtility
    {
        internal const float ScreenRadiusPixels = 110f;

        internal static bool TryResolve(
            Camera worldCamera,
            Vector2 screenPosition,
            EntityManager entityManager,
            out Entity targetEntity)
        {
            targetEntity = Entity.Null;
            if (worldCamera == null)
                return false;

            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<AssistantPreviewHighlightElement>());
            if (query.CalculateEntityCount() != 1)
                return false;

            DynamicBuffer<AssistantPreviewHighlightElement> highlights =
                entityManager.GetBuffer<AssistantPreviewHighlightElement>(query.GetSingletonEntity(), true);
            if (highlights.Length == 0)
                return false;

            AssistantPreviewHighlightElement highlight = highlights[0];
            Entity candidate = highlight.TargetEntity;
            if (highlight.Active == 0 ||
                highlight.RecommendationKind != AssistantRecommendationKind.Attack ||
                !IsDirectResolvedAttackTarget(entityManager, candidate))
            {
                return false;
            }

            float3 worldPosition = entityManager.GetComponentData<LocalTransform>(candidate).Position;
            Vector3 targetScreen = worldCamera.WorldToScreenPoint(
                new Vector3(worldPosition.x, worldPosition.y + 1.2f, worldPosition.z));
            if (targetScreen.z <= 0f ||
                Vector2.SqrMagnitude(screenPosition - (Vector2)targetScreen) >
                ScreenRadiusPixels * ScreenRadiusPixels)
            {
                return false;
            }

            targetEntity = candidate;
            return true;
        }

        internal static bool IsDirectResolvedAttackTarget(
            EntityManager entityManager,
            Entity targetEntity)
        {
            if (targetEntity == Entity.Null ||
                !entityManager.Exists(targetEntity) ||
                entityManager.HasComponent<RuntimeBuildingCombatTag>(targetEntity) ||
                entityManager.HasComponent<RuntimeBuildingCombatInfo>(targetEntity) ||
                entityManager.HasComponent<StaticGridBlocker>(targetEntity) ||
                !entityManager.HasComponent<Faction>(targetEntity) ||
                !entityManager.HasComponent<LocalTransform>(targetEntity))
            {
                return false;
            }

            if (!FactionIdentity.IsHostileToPlayer(
                    entityManager.GetComponentData<Faction>(targetEntity).Id))
            {
                return false;
            }

            return !entityManager.HasComponent<UnitHealth>(targetEntity) ||
                   entityManager.GetComponentData<UnitHealth>(targetEntity).Current > 0;
        }
    }
}
