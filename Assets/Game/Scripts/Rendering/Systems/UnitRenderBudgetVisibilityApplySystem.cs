using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Game.Components;

namespace Game.Rendering
{
    public readonly struct UnitRenderBudgetVisibilityApply
    {
        public struct Lookups
        {
            public EntityStorageInfoLookup EntityStorageInfoLookup;
            public ComponentLookup<UnitRenderBudgetCulledUnitTag> CulledUnitLookup;
            public ComponentLookup<Disabled> DisabledLookup;
            public ComponentLookup<DisableRendering> DisableRenderingLookup;
            public ComponentLookup<UnitRenderBudgetCulledTag> CulledTagLookup;

            public void Update(ref SystemState state)
            {
                EntityStorageInfoLookup.Update(ref state);
                CulledUnitLookup.Update(ref state);
                DisabledLookup.Update(ref state);
                DisableRenderingLookup.Update(ref state);
                CulledTagLookup.Update(ref state);
            }
        }

        public readonly struct Result
        {
            public readonly int Shown;
            public readonly int Hidden;

            public Result(int shown, int hidden)
            {
                Shown = shown;
                Hidden = hidden;
            }
        }

        public Result Apply(
            EntityManager em,
            EntityCommandBuffer renderStateEcb,
            NativeList<Entity> unitsToShowDetailed,
            NativeList<Entity> unitsToShowFarImpostor,
            NativeList<Entity> entitiesToShow,
            NativeList<Entity> entitiesToHide)
        {
            return Apply(
                em,
                renderStateEcb,
                unitsToShowDetailed,
                unitsToShowFarImpostor,
                entitiesToShow,
                entitiesToHide,
                default,
                useLookups: false);
        }

        public Result Apply(
            EntityManager em,
            EntityCommandBuffer renderStateEcb,
            NativeList<Entity> unitsToShowDetailed,
            NativeList<Entity> unitsToShowFarImpostor,
            NativeList<Entity> entitiesToShow,
            NativeList<Entity> entitiesToHide,
            Lookups lookups)
        {
            return Apply(
                em,
                renderStateEcb,
                unitsToShowDetailed,
                unitsToShowFarImpostor,
                entitiesToShow,
                entitiesToHide,
                lookups,
                useLookups: true);
        }

        private Result Apply(
            EntityManager em,
            EntityCommandBuffer renderStateEcb,
            NativeList<Entity> unitsToShowDetailed,
            NativeList<Entity> unitsToShowFarImpostor,
            NativeList<Entity> entitiesToShow,
            NativeList<Entity> entitiesToHide,
            Lookups lookups,
            bool useLookups)
        {
            int shown = 0;
            int hidden = 0;
            using NativeHashSet<Entity> farImpostorUnitRequests = new(math.max(1, unitsToShowFarImpostor.Length), Allocator.Temp);
            using NativeHashSet<Entity> hiddenEntityRequests = new(math.max(1, entitiesToHide.Length), Allocator.Temp);
            using NativeHashSet<Entity> scheduledCulledUnitAdds = new(math.max(1, unitsToShowFarImpostor.Length), Allocator.Temp);
            using NativeHashSet<Entity> scheduledCulledUnitRemoves = new(math.max(1, unitsToShowDetailed.Length), Allocator.Temp);
            using NativeHashSet<Entity> scheduledDisableRenderingAdds = new(math.max(1, entitiesToHide.Length), Allocator.Temp);
            using NativeHashSet<Entity> scheduledDisableRenderingRemoves = new(math.max(1, entitiesToShow.Length), Allocator.Temp);
            using NativeHashSet<Entity> scheduledDisabledRemoves = new(math.max(1, entitiesToShow.Length), Allocator.Temp);
            using NativeHashSet<Entity> scheduledCulledTagAdds = new(math.max(1, entitiesToHide.Length), Allocator.Temp);
            using NativeHashSet<Entity> scheduledCulledTagRemoves = new(math.max(1, entitiesToShow.Length), Allocator.Temp);

            for (int i = 0; i < unitsToShowFarImpostor.Length; i++)
                farImpostorUnitRequests.Add(unitsToShowFarImpostor[i]);

            for (int i = 0; i < entitiesToHide.Length; i++)
                hiddenEntityRequests.Add(entitiesToHide[i]);

            for (int i = 0; i < unitsToShowDetailed.Length; i++)
            {
                Entity unit = unitsToShowDetailed[i];
                if (farImpostorUnitRequests.Contains(unit))
                    continue;

                if (Exists(em, lookups, unit, useLookups) &&
                    HasCulledUnitTag(em, lookups, unit, useLookups) &&
                    scheduledCulledUnitRemoves.Add(unit))
                {
                    renderStateEcb.RemoveComponent<UnitRenderBudgetCulledUnitTag>(unit);
                }
            }

            for (int i = 0; i < unitsToShowFarImpostor.Length; i++)
            {
                Entity unit = unitsToShowFarImpostor[i];
                if (Exists(em, lookups, unit, useLookups) &&
                    !HasCulledUnitTag(em, lookups, unit, useLookups) &&
                    scheduledCulledUnitAdds.Add(unit))
                {
                    renderStateEcb.AddComponent<UnitRenderBudgetCulledUnitTag>(unit);
                }
            }

            for (int i = 0; i < entitiesToShow.Length; i++)
            {
                Entity entity = entitiesToShow[i];
                if (!Exists(em, lookups, entity, useLookups) || hiddenEntityRequests.Contains(entity))
                    continue;

                if (HasDisabled(em, lookups, entity, useLookups) && scheduledDisabledRemoves.Add(entity))
                    renderStateEcb.RemoveComponent<Disabled>(entity);
                if (HasDisableRendering(em, lookups, entity, useLookups) && scheduledDisableRenderingRemoves.Add(entity))
                    renderStateEcb.RemoveComponent<DisableRendering>(entity);
                if (HasCulledTag(em, lookups, entity, useLookups) && scheduledCulledTagRemoves.Add(entity))
                    renderStateEcb.RemoveComponent<UnitRenderBudgetCulledTag>(entity);
                shown++;
            }

            for (int i = 0; i < entitiesToHide.Length; i++)
            {
                Entity entity = entitiesToHide[i];
                if (!Exists(em, lookups, entity, useLookups))
                    continue;

                if (!HasDisableRendering(em, lookups, entity, useLookups) && scheduledDisableRenderingAdds.Add(entity))
                    renderStateEcb.AddComponent<DisableRendering>(entity);
                if (!HasCulledTag(em, lookups, entity, useLookups) && scheduledCulledTagAdds.Add(entity))
                    renderStateEcb.AddComponent<UnitRenderBudgetCulledTag>(entity);
                hidden++;
            }

            renderStateEcb.Playback(em);
            renderStateEcb.Dispose();
            return new Result(shown, hidden);
        }

        private static bool Exists(EntityManager em, Lookups lookups, Entity entity, bool useLookups)
        {
            return useLookups ? lookups.EntityStorageInfoLookup.Exists(entity) : em.Exists(entity);
        }

        private static bool HasCulledUnitTag(EntityManager em, Lookups lookups, Entity entity, bool useLookups)
        {
            return useLookups
                ? lookups.CulledUnitLookup.HasComponent(entity)
                : em.HasComponent<UnitRenderBudgetCulledUnitTag>(entity);
        }

        private static bool HasDisabled(EntityManager em, Lookups lookups, Entity entity, bool useLookups)
        {
            return useLookups ? lookups.DisabledLookup.HasComponent(entity) : em.HasComponent<Disabled>(entity);
        }

        private static bool HasDisableRendering(EntityManager em, Lookups lookups, Entity entity, bool useLookups)
        {
            return useLookups ? lookups.DisableRenderingLookup.HasComponent(entity) : em.HasComponent<DisableRendering>(entity);
        }

        private static bool HasCulledTag(EntityManager em, Lookups lookups, Entity entity, bool useLookups)
        {
            return useLookups ? lookups.CulledTagLookup.HasComponent(entity) : em.HasComponent<UnitRenderBudgetCulledTag>(entity);
        }
    }
}
