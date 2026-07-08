using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct AudioEventRequestSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            EnsureAudioEntity(state.EntityManager);
        }

        public void OnUpdate(ref SystemState state)
        {
            EnsureAudioEntity(state.EntityManager);
        }

        public static Entity EnsureAudioEntity(EntityManager em)
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<AudioPlaybackRequestQueueComponent>());
            if (!query.IsEmptyIgnoreFilter)
            {
                return query.GetSingletonEntity();
            }

            Entity entity = em.CreateEntity(
                typeof(AudioPlaybackRequestQueueComponent),
                typeof(AudioSettingsComponent),
                typeof(AudioMusicStateComponent),
                typeof(AudioListenerStateComponent));

            em.SetComponentData(entity, CreateDefaultSettings());
            em.SetComponentData(entity, new AudioListenerStateComponent
            {
                Forward = new float3(0f, 0f, 1f),
                MaxAudibleDistance = 120f
            });
            em.AddBuffer<AudioPlaybackRequestElement>(entity);
            em.AddBuffer<AudioPlaybackResultElement>(entity);
            em.AddBuffer<AudioCooldownStateElement>(entity);
            return entity;
        }

        public static int EnqueueOneShot(
            EntityManager em,
            FixedString64Bytes eventId,
            uint eventHash,
            FixedString32Bytes busId,
            AudioPlaybackPriority priority,
            float requestedAt,
            float cooldownSeconds = 0f,
            Entity sourceEntity = default,
            bool spatial = false,
            float3 worldPosition = default)
        {
            Entity audioEntity = EnsureAudioEntity(em);
            AudioPlaybackRequestQueueComponent queue = em.GetComponentData<AudioPlaybackRequestQueueComponent>(audioEntity);
            queue.LastRequestId++;
            queue.Version++;
            em.SetComponentData(audioEntity, queue);

            em.GetBuffer<AudioPlaybackRequestElement>(audioEntity).Add(new AudioPlaybackRequestElement
            {
                RequestId = queue.LastRequestId,
                Kind = AudioPlaybackRequestKind.OneShot,
                Priority = priority,
                Status = AudioPlaybackRequestStatus.Pending,
                EventHash = eventHash,
                EventId = eventId,
                BusId = busId,
                SourceEntity = sourceEntity,
                WorldPosition = worldPosition,
                VolumeDecibels = 0f,
                PitchMultiplier = 1f,
                RequestedAt = requestedAt,
                CooldownSeconds = math.max(0f, cooldownSeconds),
                HasWorldPosition = (byte)(spatial ? 1 : 0),
                Spatial = (byte)(spatial ? 1 : 0)
            });

            return queue.LastRequestId;
        }

        public static int EnqueueMusicState(
            EntityManager em,
            FixedString64Bytes eventId,
            uint eventHash,
            float requestedAt,
            float transitionSeconds,
            bool loop = true)
        {
            Entity audioEntity = EnsureAudioEntity(em);
            AudioMusicStateComponent musicState = em.GetComponentData<AudioMusicStateComponent>(audioEntity);
            musicState.Version++;
            musicState.RequestedEventHash = eventHash;
            musicState.RequestedEventId = eventId;
            musicState.RequestedAt = requestedAt;
            musicState.TransitionSeconds = math.max(0f, transitionSeconds);
            musicState.Loop = (byte)(loop ? 1 : 0);
            musicState.IsTransitioning = 1;
            em.SetComponentData(audioEntity, musicState);

            return EnqueueOneShot(
                em,
                eventId,
                eventHash,
                new FixedString32Bytes("Music"),
                AudioPlaybackPriority.High,
                requestedAt,
                cooldownSeconds: 0f);
        }

        private static AudioSettingsComponent CreateDefaultSettings()
        {
            return new AudioSettingsComponent
            {
                Version = 1,
                MasterVolume = 1f,
                UiVolume = 1f,
                SfxVolume = 1f,
                AlertsVolume = 1f,
                MusicVolume = 0.75f,
                MusicMuted = 1,
                AmbienceVolume = 0.75f,
                VoiceVolume = 1f
            };
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AudioEventRequestSystem))]
    public partial struct AudioCooldownSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            AudioEventRequestSystem.EnsureAudioEntity(state.EntityManager);
        }

        public void OnUpdate(ref SystemState state)
        {
            ProcessPendingRequests(state.EntityManager, (float)SystemAPI.Time.ElapsedTime);
        }

        public static void ProcessPendingRequests(EntityManager em, float now)
        {
            Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(em);
            DynamicBuffer<AudioPlaybackRequestElement> requests = em.GetBuffer<AudioPlaybackRequestElement>(audioEntity);
            DynamicBuffer<AudioPlaybackResultElement> results = em.GetBuffer<AudioPlaybackResultElement>(audioEntity);
            DynamicBuffer<AudioCooldownStateElement> cooldowns = em.GetBuffer<AudioCooldownStateElement>(audioEntity);

            for (int i = 0; i < requests.Length; i++)
            {
                AudioPlaybackRequestElement request = requests[i];
                if (request.Status != AudioPlaybackRequestStatus.Pending)
                {
                    continue;
                }

                AudioPlaybackRequestStatus status = ResolveRequestStatus(request, cooldowns, now);
                request.Status = status;
                requests[i] = request;

                if (status == AudioPlaybackRequestStatus.Accepted && request.EventHash != 0u && request.CooldownSeconds > 0f)
                {
                    UpsertCooldown(cooldowns, request.EventHash, now);
                }

                results.Add(new AudioPlaybackResultElement
                {
                    RequestId = request.RequestId,
                    Status = status,
                    EventHash = request.EventHash,
                    EventId = request.EventId,
                    Reason = CreateReason(status),
                    ProcessedAt = now
                });
            }
        }

        private static AudioPlaybackRequestStatus ResolveRequestStatus(
            AudioPlaybackRequestElement request,
            DynamicBuffer<AudioCooldownStateElement> cooldowns,
            float now)
        {
            if (request.EventHash == 0u || request.EventId.Length == 0)
            {
                return AudioPlaybackRequestStatus.MissingEvent;
            }

            if (request.CooldownSeconds <= 0f)
            {
                return AudioPlaybackRequestStatus.Accepted;
            }

            for (int i = 0; i < cooldowns.Length; i++)
            {
                AudioCooldownStateElement cooldown = cooldowns[i];
                if (cooldown.EventHash == request.EventHash &&
                    now - cooldown.LastAcceptedAt < request.CooldownSeconds)
                {
                    return AudioPlaybackRequestStatus.CooldownSkipped;
                }
            }

            return AudioPlaybackRequestStatus.Accepted;
        }

        private static void UpsertCooldown(DynamicBuffer<AudioCooldownStateElement> cooldowns, uint eventHash, float now)
        {
            for (int i = 0; i < cooldowns.Length; i++)
            {
                AudioCooldownStateElement cooldown = cooldowns[i];
                if (cooldown.EventHash == eventHash)
                {
                    cooldown.LastAcceptedAt = now;
                    cooldowns[i] = cooldown;
                    return;
                }
            }

            cooldowns.Add(new AudioCooldownStateElement
            {
                EventHash = eventHash,
                LastAcceptedAt = now
            });
        }

        private static FixedString64Bytes CreateReason(AudioPlaybackRequestStatus status)
        {
            return status switch
            {
                AudioPlaybackRequestStatus.Accepted => new FixedString64Bytes("Accepted"),
                AudioPlaybackRequestStatus.CooldownSkipped => new FixedString64Bytes("Cooldown"),
                AudioPlaybackRequestStatus.MissingEvent => new FixedString64Bytes("MissingEvent"),
                _ => new FixedString64Bytes("Rejected")
            };
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AudioEventRequestSystem))]
    public partial struct AudioMusicStateSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            AudioEventRequestSystem.EnsureAudioEntity(state.EntityManager);
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(state.EntityManager);
            AudioMusicStateComponent musicState = state.EntityManager.GetComponentData<AudioMusicStateComponent>(audioEntity);
            if (ApplyRequestedMusicState(ref musicState))
            {
                state.EntityManager.SetComponentData(audioEntity, musicState);
            }
        }

        public static bool ApplyRequestedMusicState(ref AudioMusicStateComponent musicState)
        {
            if (musicState.RequestedEventHash == 0u || musicState.RequestedEventHash == musicState.CurrentEventHash)
            {
                musicState.IsTransitioning = 0;
                return false;
            }

            musicState.Version++;
            musicState.CurrentEventHash = musicState.RequestedEventHash;
            musicState.CurrentEventId = musicState.RequestedEventId;
            musicState.RequestedEventHash = 0u;
            musicState.RequestedEventId = default;
            musicState.IsTransitioning = 0;
            return true;
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AudioEventRequestSystem))]
    public partial struct AudioSettingsSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            AudioEventRequestSystem.EnsureAudioEntity(state.EntityManager);
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(state.EntityManager);
            AudioSettingsComponent settings = state.EntityManager.GetComponentData<AudioSettingsComponent>(audioEntity);
            if (NormalizeSettings(ref settings))
            {
                state.EntityManager.SetComponentData(audioEntity, settings);
            }
        }

        public static bool NormalizeSettings(ref AudioSettingsComponent settings)
        {
            AudioSettingsComponent original = settings;
            settings.MasterVolume = math.saturate(settings.MasterVolume);
            settings.UiVolume = math.saturate(settings.UiVolume);
            settings.SfxVolume = math.saturate(settings.SfxVolume);
            settings.AlertsVolume = math.saturate(settings.AlertsVolume);
            settings.MusicVolume = math.saturate(settings.MusicVolume);
            settings.AmbienceVolume = math.saturate(settings.AmbienceVolume);
            settings.VoiceVolume = math.saturate(settings.VoiceVolume);

            bool changed =
                !NearlyEqual(original.MasterVolume, settings.MasterVolume) ||
                !NearlyEqual(original.UiVolume, settings.UiVolume) ||
                !NearlyEqual(original.SfxVolume, settings.SfxVolume) ||
                !NearlyEqual(original.AlertsVolume, settings.AlertsVolume) ||
                !NearlyEqual(original.MusicVolume, settings.MusicVolume) ||
                !NearlyEqual(original.AmbienceVolume, settings.AmbienceVolume) ||
                !NearlyEqual(original.VoiceVolume, settings.VoiceVolume);

            if (changed)
            {
                settings.Version++;
            }

            return changed;
        }

        private static bool NearlyEqual(float left, float right)
        {
            return math.abs(left - right) <= 0.0001f;
        }
    }
}
