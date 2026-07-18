using Game.Components;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.UI.Shell.Ecs
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct AssistantSettingsPersistenceSystem : ISystem
    {
        private static int s_SubscriptionCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeAppliedBridge()
        {
            s_SubscriptionCount = 0;
        }

        public void OnCreate(ref SystemState state)
        {
            RegisterRuntimeAppliedBridge();
            state.Enabled = false;
        }

        public void OnDestroy(ref SystemState state)
        {
            UnregisterRuntimeAppliedBridge();
        }

        public void OnUpdate(ref SystemState state)
        {
        }

        private static void RegisterRuntimeAppliedBridge()
        {
            if (s_SubscriptionCount++ == 0)
                SettingsService.RuntimeApplied += ApplyToDefaultWorld;
        }

        private static void UnregisterRuntimeAppliedBridge()
        {
            if (s_SubscriptionCount <= 0)
                return;

            s_SubscriptionCount--;
            if (s_SubscriptionCount == 0)
                SettingsService.RuntimeApplied -= ApplyToDefaultWorld;
        }

        private static void ApplyToDefaultWorld(UISettingsModel model)
        {
            AssistantSettingsPersistenceSystemHelper.ApplyToWorld(World.DefaultGameObjectInjectionWorld, model);
        }
    }

    public static class AssistantSettingsPersistenceSystemHelper
    {
        public static AssistantSettingsComponent ToAssistantSettingsComponent(UISettingsModel model)
        {
            return new AssistantSettingsComponent
            {
                GuidanceLevel = ToGuidanceLevel(model.Assistant.AssistanceLevel),
                NarrationMode = ToNarrationMode(model.Assistant.NarrationMode),
                AllowTakeover = model.Assistant.AllowTakeover ? (byte)1 : (byte)0,
                SubtitlesEnabled = model.Assistant.SubtitlesEnabled ? (byte)1 : (byte)0,
                LargeTextEnabled = model.Accessibility.LargeText ? (byte)1 : (byte)0,
                HighContrastEnabled = model.Accessibility.HighContrastUi ? (byte)1 : (byte)0
            };
        }

        public static AssistantSettingsComponent LoadSettingsComponent()
        {
            return ToAssistantSettingsComponent(SettingsService.Load());
        }

        public static bool TakeoverAllowed(EntityManager entityManager, Entity boundary)
        {
            if (!entityManager.Exists(boundary) || !entityManager.HasComponent<AssistantSettingsComponent>(boundary))
                return SettingsService.Load().Assistant.AllowTakeover;

            AssistantSettingsComponent settings = entityManager.GetComponentData<AssistantSettingsComponent>(boundary);
            return settings.AllowTakeover != 0;
        }

        public static void ApplyToWorld(World world, UISettingsModel model)
        {
            if (world == null || !world.IsCreated)
                return;

            EntityManager entityManager = world.EntityManager;
            EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<UiShellStateComponent>());
            using NativeArray<Entity> boundaries = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < boundaries.Length; i++)
                ApplyToBoundary(entityManager, boundaries[i], model);
        }

        public static void ApplyToBoundary(EntityManager entityManager, Entity boundary, UISettingsModel model)
        {
            if (!entityManager.Exists(boundary))
                return;

            AssistantSettingsComponent projected = ToAssistantSettingsComponent(model);
            if (entityManager.HasComponent<AssistantSettingsComponent>(boundary))
            {
                AssistantSettingsComponent current = entityManager.GetComponentData<AssistantSettingsComponent>(boundary);
                if (SettingsEqual(current, projected))
                    return;

                entityManager.SetComponentData(boundary, projected);
            }
            else
            {
                entityManager.AddComponentData(boundary, projected);
            }

            SyncDependentState(entityManager, boundary, projected);
        }

        private static void SyncDependentState(
            EntityManager entityManager,
            Entity boundary,
            AssistantSettingsComponent settings)
        {
            if (entityManager.HasComponent<AssistantStateComponent>(boundary))
            {
                AssistantStateComponent assistantState = entityManager.GetComponentData<AssistantStateComponent>(boundary);
                assistantState.GuidanceLevel = settings.GuidanceLevel;
                assistantState.UiDirty = 1;
                entityManager.SetComponentData(boundary, assistantState);
            }

            if (entityManager.HasComponent<AssistantNarrationStateComponent>(boundary))
            {
                AssistantNarrationStateComponent narrationState =
                    entityManager.GetComponentData<AssistantNarrationStateComponent>(boundary);
                narrationState.Mode = settings.NarrationMode;
                narrationState.UiDirty = 1;
                entityManager.SetComponentData(boundary, narrationState);
            }
        }

        private static bool SettingsEqual(AssistantSettingsComponent left, AssistantSettingsComponent right)
        {
            return left.GuidanceLevel == right.GuidanceLevel &&
                   left.NarrationMode == right.NarrationMode &&
                   left.AllowTakeover == right.AllowTakeover &&
                   left.SubtitlesEnabled == right.SubtitlesEnabled &&
                   left.LargeTextEnabled == right.LargeTextEnabled &&
                   left.HighContrastEnabled == right.HighContrastEnabled;
        }

        private static AssistantGuidanceLevel ToGuidanceLevel(UIAssistanceLevel level)
        {
            return level switch
            {
                UIAssistanceLevel.HintsOnly => AssistantGuidanceLevel.HintsOnly,
                UIAssistanceLevel.Minimal => AssistantGuidanceLevel.Minimal,
                UIAssistanceLevel.Off => AssistantGuidanceLevel.Off,
                _ => AssistantGuidanceLevel.FullGuidance
            };
        }

        private static AssistantNarrationMode ToNarrationMode(UIAssistantNarrationMode mode)
        {
            return mode switch
            {
                UIAssistantNarrationMode.Off => AssistantNarrationMode.Off,
                UIAssistantNarrationMode.CriticalOnly => AssistantNarrationMode.CriticalOnly,
                UIAssistantNarrationMode.All => AssistantNarrationMode.All,
                _ => AssistantNarrationMode.Important
            };
        }
    }
}
