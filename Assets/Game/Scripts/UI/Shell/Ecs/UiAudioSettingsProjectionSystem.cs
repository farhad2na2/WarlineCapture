using Game.Components;
using Game.Runtime;
using Game.UI.Runtime;
using Unity.Entities;
using UnityEngine;

namespace Game.UI.Shell.Ecs
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct UiAudioSettingsProjectionSystem : ISystem
    {
        private static int s_SubscriptionCount;

        public void OnCreate(ref SystemState state)
        {
            RegisterRuntimeAppliedBridge();
            ApplyToWorld(state.World, SettingsService.Load());
            state.Enabled = false;
        }

        public void OnDestroy(ref SystemState state)
        {
            UnregisterRuntimeAppliedBridge();
        }

        public void OnUpdate(ref SystemState state)
        {
        }

        public static AudioSettingsComponent ToAudioSettingsComponent(UISettingsModel model, uint version)
        {
            return new AudioSettingsComponent
            {
                Version = version,
                MasterVolume = NormalizePercent(model.Audio.MasterVolume),
                UiVolume = NormalizePercent(model.Audio.SfxVolume),
                SfxVolume = NormalizePercent(model.Audio.SfxVolume),
                AlertsVolume = NormalizePercent(model.Audio.AlertsVolume),
                MusicVolume = NormalizePercent(model.Audio.MusicVolume),
                MusicMuted = 1,
                AmbienceVolume = NormalizePercent(model.Audio.SfxVolume),
                VoiceVolume = NormalizePercent(model.Audio.VoiceVolume)
            };
        }

        public static void ApplyToWorld(World world, UISettingsModel model)
        {
            if (world == null || !world.IsCreated)
                return;

            EntityManager entityManager = world.EntityManager;
            Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(entityManager);
            AudioSettingsComponent current = entityManager.GetComponentData<AudioSettingsComponent>(audioEntity);
            AudioSettingsComponent projected = ToAudioSettingsComponent(model, current.Version + 1u);
            AudioSettingsSystem.NormalizeSettings(ref projected);
            entityManager.SetComponentData(audioEntity, projected);
            AudioListener.volume = projected.MasterVolume;
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
            ApplyToWorld(World.DefaultGameObjectInjectionWorld, model);
        }

        private static float NormalizePercent(float value)
        {
            return Mathf.Clamp01(value / 100f);
        }
    }
}
