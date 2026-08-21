using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Configs;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.Components;
using Game.UI.Runtime;
using Game.Runtime;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        private static partial class UiShellReadModelAdapter
        {
        private static uint NextManagedAssistantPanelVersion(uint version)
        {
            uint next = version + 1u;
            return next == 0u ? 1u : next;
        }

        private static uint AssistantHighlightVersion(AssistantPreviewHighlightElement highlight)
        {
            uint combined = (uint)math.max(1, highlight.RequestId) * 397u ^ (uint)math.max(0, highlight.Frame);
            combined = combined * 397u ^ (uint)math.max(0, highlight.RecommendationId);
            combined = combined * 31u ^ (uint)highlight.RecommendationKind;
            combined = combined * 31u ^ (uint)highlight.TargetKind;
            combined = combined * 17u ^ (uint)math.asint(highlight.WorldPosition.x);
            combined = combined * 17u ^ (uint)math.asint(highlight.WorldPosition.y);
            combined = combined * 17u ^ (uint)math.asint(highlight.WorldPosition.z);
            return combined == 0u ? 1u : combined;
        }

        private static void BuildAssistantGoalRows(
            DynamicBuffer<AssistantGoalReadModelElement> goals,
            out UiAssistantGoalRowModel goal0,
            out UiAssistantGoalRowModel goal1,
            out UiAssistantGoalRowModel goal2)
        {
            goal0 = goals.Length > 0 ? ToGoalRow(goals[0]) : UiAssistantGoalRowModel.Empty;
            goal1 = goals.Length > 1 ? ToGoalRow(goals[1]) : UiAssistantGoalRowModel.Empty;
            goal2 = goals.Length > 2 ? ToGoalRow(goals[2]) : UiAssistantGoalRowModel.Empty;
        }

        private static UiAssistantGoalRowModel ToGoalRow(AssistantGoalReadModelElement goal)
        {
            return new UiAssistantGoalRowModel(
                goal.Title.Length > 0,
                goal.GoalId,
                goal.Title.ToString(),
                goal.Body.ToString(),
                (byte)goal.State,
                (byte)goal.Priority,
                goal.IsPrimary != 0);
        }

        private static void BuildAssistantMessageRows(
            DynamicBuffer<AssistantMessageElement> messages,
            out UiAssistantMessageRowModel alert0,
            out UiAssistantMessageRowModel alert1,
            out UiAssistantMessageRowModel alert2,
            out UiAssistantMessageRowModel report0,
            out UiAssistantMessageRowModel report1)
        {
            alert0 = UiAssistantMessageRowModel.Empty;
            alert1 = UiAssistantMessageRowModel.Empty;
            alert2 = UiAssistantMessageRowModel.Empty;
            report0 = UiAssistantMessageRowModel.Empty;
            report1 = UiAssistantMessageRowModel.Empty;
            int alertCount = 0;
            int reportCount = 0;
            float now = Time.time;
            for (int priority = (int)AssistantMessagePriority.Critical;
                 priority >= (int)AssistantMessagePriority.Low;
                 priority--)
            {
                for (int i = 0; i < messages.Length; i++)
                {
                    AssistantMessageElement message = messages[i];
                    if ((int)message.Priority != priority ||
                        message.Text.Length == 0 ||
                        message.Acknowledged != 0 ||
                        (message.ExpiresAt > 0f && now >= message.ExpiresAt))
                    {
                        continue;
                    }

                    UiAssistantMessageRowModel row = ToMessageRow(message, now);
                    if (message.Priority >= AssistantMessagePriority.High)
                    {
                        if (alertCount == 0) alert0 = row;
                        else if (alertCount == 1) alert1 = row;
                        else if (alertCount == 2) alert2 = row;
                        alertCount++;
                    }
                    else
                    {
                        if (reportCount == 0) report0 = row;
                        else if (reportCount == 1) report1 = row;
                        reportCount++;
                    }

                    if (alertCount >= 3 && reportCount >= 2)
                        return;
                }
            }
        }

        private static UiAssistantMessageRowModel ToMessageRow(AssistantMessageElement message, float now)
        {
            byte ageState = message.ExpiresAt > 0f && message.ExpiresAt - now < 1f
                ? (byte)3
                : now - message.CreatedAt < 5f
                    ? (byte)1
                    : (byte)2;
            return new UiAssistantMessageRowModel(
                true,
                message.MessageId,
                MessageTitle(message.RelatedKind),
                message.Text.ToString(),
                (byte)message.Priority,
                (byte)message.RelatedKind,
                ageState,
                message.RequiresNarration != 0,
                false);
        }

        private static string MessageTitle(AssistantRecommendationKind kind)
        {
            return kind switch
            {
                AssistantRecommendationKind.DefensiveAlert => "THREAT",
                AssistantRecommendationKind.Logistics => "LOGISTICS",
                AssistantRecommendationKind.Move => "COMMAND",
                AssistantRecommendationKind.Attack => "COMMAND",
                AssistantRecommendationKind.Select => "COMMAND",
                _ => "REPORT"
            };
        }

        private static UiAssistantTargetLockModel BuildAssistantTargetLockModel(
            AssistantTargetLockReadModelComponent targetLock)
        {
            if (targetLock.Visible == 0)
                return UiAssistantTargetLockModel.Empty;

            string distanceText = targetLock.HasDistance != 0
                ? $"{Mathf.RoundToInt(targetLock.Distance)} m"
                : string.Empty;
            string healthText = targetLock.HasHealth != 0
                ? $"{targetLock.HealthCurrent}/{targetLock.HealthMax}"
                : string.Empty;
            return new UiAssistantTargetLockModel(
                true,
                (byte)targetLock.State,
                (byte)targetLock.TargetKind,
                targetLock.TargetName.ToString(),
                targetLock.SourceName.ToString(),
                distanceText,
                healthText,
                FactionRelationText(targetLock.FactionRelation),
                TargetReadinessText(targetLock.State),
                targetLock.Reason.ToString());
        }

        private static UiAssistantNarrationModel BuildAssistantNarrationModel(
            EntityManager entityManager,
            AssistantSettingsComponent settings,
            AssistantNarrationStateComponent narrationState,
            DynamicBuffer<AssistantNarrationRequestElement> requests,
            bool waveformPulse)
        {
            AssistantNarrationRequestElement request = requests.IsCreated && requests.Length > 0
                ? requests[requests.Length - 1]
                : default;
            UiAssistantNarrationStateKind state = UiAssistantNarrationStateKind.Off;
            if (request.RequestId != 0)
            {
                Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(entityManager);
                AudioSettingsComponent audioSettings =
                    entityManager.GetComponentData<AudioSettingsComponent>(audioEntity);
                state = AssistantNarrationAudioResultProjectionSystem.ResolveTruthState(
                    settings,
                    audioSettings,
                    request,
                    narrationState);
            }

            return new UiAssistantNarrationModel(
                (byte)state,
                (byte)request.Priority,
                NarrationStateText(state),
                settings.SubtitlesEnabled != 0 ? request.Text.ToString() : string.Empty,
                state == UiAssistantNarrationStateKind.Failed
                    ? narrationState.LastAudioFailureReason.ToString()
                    : string.Empty,
                state == UiAssistantNarrationStateKind.Presented && waveformPulse);
        }

        private static string NarrationStateText(UiAssistantNarrationStateKind state)
        {
            return state switch
            {
                UiAssistantNarrationStateKind.TextOnly => "TEXT ONLY",
                UiAssistantNarrationStateKind.Queued => "QUEUED",
                UiAssistantNarrationStateKind.Accepted => "ACCEPTED",
                UiAssistantNarrationStateKind.Presented => "PRESENTED",
                UiAssistantNarrationStateKind.Failed => "FAILED",
                _ => "OFF"
            };
        }

        private static string TargetReadinessText(AssistantTargetLockState state)
        {
            return state switch
            {
                AssistantTargetLockState.Preview => "PREVIEW",
                AssistantTargetLockState.Executable => "READY",
                AssistantTargetLockState.Executing => "ACTIVE",
                AssistantTargetLockState.Invalid => "BLOCKED",
                _ => "BLOCKED"
            };
        }

        private static string FactionRelationText(AssistantFactionRelation relation)
        {
            return relation switch
            {
                AssistantFactionRelation.Friendly => "FRIENDLY",
                AssistantFactionRelation.Hostile => "HOSTILE",
                AssistantFactionRelation.Neutral => "NEUTRAL",
                AssistantFactionRelation.Protected => "PROTECTED",
                _ => string.Empty
            };
        }

        private static uint AssistantSettingsVersion(AssistantSettingsComponent settings)
        {
            return (uint)settings.GuidanceLevel |
                   (uint)settings.NarrationMode << 4 |
                   (uint)settings.AllowTakeover << 8 |
                   (uint)settings.SubtitlesEnabled << 9 |
                   (uint)settings.LargeTextEnabled << 10 |
                   (uint)settings.HighContrastEnabled << 11;
        }

        private static string PriorityText(AssistantMessagePriority priority)
        {
            return priority switch
            {
                AssistantMessagePriority.Critical => "CRITICAL",
                AssistantMessagePriority.High => "HIGH",
                AssistantMessagePriority.Normal => "NORMAL",
                _ => "LOW"
            };
        }

        private static string ControlStateText(AssistantControlState state)
        {
            return state switch
            {
                AssistantControlState.Guided => "GUIDED",
                AssistantControlState.AssistantPreview => "PREVIEW",
                AssistantControlState.AssistantTakeover => "ARIA CONTROL",
                AssistantControlState.PlayerOverridePending => "PLAYER OVERRIDE",
                _ => "PLAYER CONTROL"
            };
        }

        private static string ControlStateDetailText(AssistantControlState state)
        {
            return state switch
            {
                AssistantControlState.Guided => "ARIA is guiding the next action. You keep final control.",
                AssistantControlState.AssistantPreview => "ARIA is previewing a recommendation. STOP clears the preview.",
                AssistantControlState.AssistantTakeover => "ARIA is executing a bounded action. STOP returns control.",
                AssistantControlState.PlayerOverridePending => "Player input detected. ARIA is returning control.",
                _ => "You are issuing orders directly."
            };
        }

        private static bool CanStopAssistantControl(AssistantControlState state)
        {
            return state == AssistantControlState.Guided ||
                   state == AssistantControlState.AssistantPreview ||
                   state == AssistantControlState.AssistantTakeover ||
                   state == AssistantControlState.PlayerOverridePending;
        }


        }
    }
}
