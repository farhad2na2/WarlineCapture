using Game.Components;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.UI.Shell.Ecs
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(AssistantNarrationAudioRequestSystem))]
    public partial struct AssistantNarrationAudioResultProjectionSystem : ISystem
    {
        public const float PresentedPulseSeconds = 0.8f;

        private EntityQuery boundaryQuery;
        private EntityQuery matchStartQuery;
        private uint lastObservedResultVersion;
        private uint lastObservedAudioSettingsVersion;
        private int lastObservedAudioPlaybackRequestId;
        private float pulseExpiresAt;
        private byte hasAudioSettingsSnapshot;
        private byte lastEffectiveAudioEnabled;
        private byte pulseExpiryPending;

        public void OnCreate(ref SystemState state)
        {
            boundaryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UiShellStateComponent>(),
                ComponentType.ReadWrite<AssistantNarrationStateComponent>(),
                ComponentType.ReadOnly<AssistantNarrationRequestElement>());
            matchStartQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<MatchStartStateComponent>(),
                ComponentType.ReadOnly<MatchStartQueueComponent>());
            state.RequireForUpdate(boundaryQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity boundary = boundaryQuery.GetSingletonEntity();
            if (!AssistantNarrationRequestSystem.IsNarrationRuntimeActive(
                    state.EntityManager,
                    boundary,
                    matchStartQuery))
            {
                AssistantNarrationRequestSystem.ClearNarrationBoundary(state.EntityManager, boundary);
                ResetObservedState();
                return;
            }

            Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(state.EntityManager);
            AssistantNarrationStateComponent narrationState =
                state.EntityManager.GetComponentData<AssistantNarrationStateComponent>(boundary);
            AudioSettingsComponent audioSettings =
                state.EntityManager.GetComponentData<AudioSettingsComponent>(audioEntity);
            float now = Time.unscaledTime;
            bool changed = ObserveEffectiveAudioState(audioSettings);

            AudioPlaybackResultQueueComponent resultQueue =
                state.EntityManager.GetComponentData<AudioPlaybackResultQueueComponent>(audioEntity);
            int activeAudioPlaybackRequestId = narrationState.ActiveAudioPlaybackRequestId;
            bool resultSourceChanged =
                activeAudioPlaybackRequestId != lastObservedAudioPlaybackRequestId ||
                resultQueue.Version != lastObservedResultVersion;

            if (resultSourceChanged)
            {
                lastObservedAudioPlaybackRequestId = activeAudioPlaybackRequestId;
                lastObservedResultVersion = resultQueue.Version;

                if (activeAudioPlaybackRequestId > 0)
                {
                    DynamicBuffer<AudioPlaybackResultElement> results =
                        state.EntityManager.GetBuffer<AudioPlaybackResultElement>(audioEntity, true);
                    if (TryFindLatestResult(results, activeAudioPlaybackRequestId, out AudioPlaybackResultElement result))
                    {
                        changed |= ApplyLatestResult(
                            state.EntityManager,
                            boundary,
                            result,
                            now,
                            ref narrationState);
                        ConfigurePulseBoundary(result.Status, narrationState.LastPresentedAt, now);
                        if (result.Status == AudioPlaybackRequestStatus.Presented &&
                            pulseExpiryPending == 0 &&
                            now >= pulseExpiresAt &&
                            narrationState.LastPresentedAt != 0f)
                        {
                            narrationState.LastPresentedAt = 0f;
                            changed = true;
                        }
                    }
                }
                else
                {
                    pulseExpiryPending = 0;
                }
            }

            if (pulseExpiryPending != 0 && now >= pulseExpiresAt)
            {
                pulseExpiryPending = 0;
                narrationState.LastPresentedAt = 0f;
                changed = true;
            }

            if (!changed)
                return;

            narrationState.Version = NextVersion(narrationState.Version);
            narrationState.IsSpeaking = 0;
            narrationState.UiDirty = 1;
            state.EntityManager.SetComponentData(boundary, narrationState);
        }

        public static UiAssistantNarrationStateKind ResolveTruthState(
            AssistantSettingsComponent assistantSettings,
            AudioSettingsComponent audioSettings,
            AssistantNarrationRequestElement activeRequest,
            AssistantNarrationStateComponent narrationState)
        {
            if (assistantSettings.NarrationMode == AssistantNarrationMode.Off ||
                narrationState.Mode == AssistantNarrationMode.Off ||
                !IsEffectiveVoiceEnabled(audioSettings))
            {
                return UiAssistantNarrationStateKind.Off;
            }

            if (IsFailureStatus(narrationState.LastAudioStatus))
                return UiAssistantNarrationStateKind.Failed;

            if (activeRequest.Text.Length > 0 && activeRequest.AudioEventId.Length == 0)
                return UiAssistantNarrationStateKind.TextOnly;

            return narrationState.LastAudioStatus switch
            {
                AudioPlaybackRequestStatus.Accepted => UiAssistantNarrationStateKind.Accepted,
                AudioPlaybackRequestStatus.Presented => UiAssistantNarrationStateKind.Presented,
                _ => UiAssistantNarrationStateKind.Queued
            };
        }

        public static bool IsPresentationPulseActive(AssistantNarrationStateComponent narrationState, float now)
        {
            if (narrationState.LastAudioStatus != AudioPlaybackRequestStatus.Presented)
                return false;
            if (narrationState.LastPresentedAt <= 0f)
                return false;

            float age = now - narrationState.LastPresentedAt;
            return age >= 0f && age < PresentedPulseSeconds;
        }

        public static bool IsEffectiveVoiceEnabled(AudioSettingsComponent settings)
        {
            return settings.MasterMuted == 0 &&
                   settings.VoiceMuted == 0 &&
                   settings.MasterVolume > 0f &&
                   settings.VoiceVolume > 0f;
        }

        public static FixedString64Bytes ResolveSafeFailureReason(AudioPlaybackRequestStatus status)
        {
            return status switch
            {
                AudioPlaybackRequestStatus.Rejected => new FixedString64Bytes("Voice request rejected"),
                AudioPlaybackRequestStatus.CooldownSkipped => new FixedString64Bytes("Voice cooldown active"),
                AudioPlaybackRequestStatus.MissingEvent => new FixedString64Bytes("Voice event unavailable"),
                AudioPlaybackRequestStatus.MissingClip => new FixedString64Bytes("Voice clip unavailable"),
                AudioPlaybackRequestStatus.Culled => new FixedString64Bytes("Voice playback unavailable"),
                _ => new FixedString64Bytes("Voice playback failed")
            };
        }

        private bool ObserveEffectiveAudioState(AudioSettingsComponent audioSettings)
        {
            byte effectiveAudioEnabled = (byte)(IsEffectiveVoiceEnabled(audioSettings) ? 1 : 0);
            bool changed = hasAudioSettingsSnapshot != 0 &&
                           audioSettings.Version != lastObservedAudioSettingsVersion &&
                           effectiveAudioEnabled != lastEffectiveAudioEnabled;
            hasAudioSettingsSnapshot = 1;
            lastObservedAudioSettingsVersion = audioSettings.Version;
            lastEffectiveAudioEnabled = effectiveAudioEnabled;
            return changed;
        }

        private bool ApplyLatestResult(
            EntityManager em,
            Entity boundary,
            AudioPlaybackResultElement result,
            float now,
            ref AssistantNarrationStateComponent narrationState)
        {
            FixedString64Bytes failureReason = IsFailureStatus(result.Status)
                ? ResolveSafeFailureReason(result.Status)
                : default;
            float presentedAt = result.Status == AudioPlaybackRequestStatus.Presented
                ? result.ProcessedAt > 0f ? result.ProcessedAt : now
                : 0f;
            bool changed = narrationState.LastAudioStatus != result.Status ||
                           !narrationState.LastAudioFailureReason.Equals(failureReason) ||
                           narrationState.LastPresentedAt != presentedAt;
            if (!changed)
                return false;

            narrationState.LastAudioStatus = result.Status;
            narrationState.LastAudioFailureReason = failureReason;
            narrationState.LastPresentedAt = presentedAt;
            narrationState.IsSpeaking = 0;

            if (result.Status == AudioPlaybackRequestStatus.Presented &&
                TryFindNarrationMessageId(em, boundary, result.RequestId, out int messageId))
            {
                narrationState.LastSpokenMessageId = messageId;
                narrationState.LastSpokenAt = presentedAt;
            }

            return true;
        }

        private void ConfigurePulseBoundary(AudioPlaybackRequestStatus status, float presentedAt, float now)
        {
            if (status != AudioPlaybackRequestStatus.Presented)
            {
                pulseExpiryPending = 0;
                return;
            }

            pulseExpiresAt = presentedAt + PresentedPulseSeconds;
            pulseExpiryPending = (byte)(now < pulseExpiresAt ? 1 : 0);
        }

        private static bool TryFindLatestResult(
            DynamicBuffer<AudioPlaybackResultElement> results,
            int audioPlaybackRequestId,
            out AudioPlaybackResultElement latest)
        {
            for (int i = results.Length - 1; i >= 0; i--)
            {
                if (results[i].RequestId != audioPlaybackRequestId)
                    continue;

                latest = results[i];
                return true;
            }

            latest = default;
            return false;
        }

        private static bool TryFindNarrationMessageId(
            EntityManager em,
            Entity boundary,
            int audioPlaybackRequestId,
            out int messageId)
        {
            DynamicBuffer<AssistantNarrationRequestElement> requests =
                em.GetBuffer<AssistantNarrationRequestElement>(boundary, true);
            for (int i = requests.Length - 1; i >= 0; i--)
            {
                if (requests[i].AudioPlaybackRequestId != audioPlaybackRequestId)
                    continue;

                messageId = requests[i].MessageId;
                return true;
            }

            messageId = 0;
            return false;
        }

        private static bool IsFailureStatus(AudioPlaybackRequestStatus status)
        {
            return status == AudioPlaybackRequestStatus.Rejected ||
                   status == AudioPlaybackRequestStatus.CooldownSkipped ||
                   status == AudioPlaybackRequestStatus.MissingEvent ||
                   status == AudioPlaybackRequestStatus.MissingClip ||
                   status == AudioPlaybackRequestStatus.Culled;
        }

        private void ResetObservedState()
        {
            lastObservedResultVersion = 0u;
            lastObservedAudioSettingsVersion = 0u;
            lastObservedAudioPlaybackRequestId = 0;
            pulseExpiresAt = 0f;
            hasAudioSettingsSnapshot = 0;
            lastEffectiveAudioEnabled = 0;
            pulseExpiryPending = 0;
        }

        private static uint NextVersion(uint version)
        {
            uint next = version + 1u;
            return next == 0u ? 1u : next;
        }
    }
}
