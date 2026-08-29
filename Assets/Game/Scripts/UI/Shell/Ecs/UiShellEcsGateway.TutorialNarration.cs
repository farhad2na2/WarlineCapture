using System;
using Game.Components;
using Game.Configs;
using Game.Narrative.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiTutorialNarrationGateway
    {
        private const int TutorialMessageBaseId = 1000000;
        private const int TutorialMessageLimitExclusive = 2000000;
        private const float TutorialMessageLifetimeSeconds = 12f;

        private static FirstLaunchNarrativeLanguage cachedTutorialNarrationLanguage;
        private static bool hasCachedTutorialNarrationLanguage;
        private static int tutorialNarrationSequence;

        bool IUiTutorialNarrationGateway.TryEnqueueTutorialNarration(
            byte tutorialStep,
            byte tutorialStepCount,
            UiTutorialNarrationPhase phase,
            string text)
        {
            bool m01Step = tutorialStepCount == 5 && tutorialStep is >= 1 and <= 5;
            bool m02Step = tutorialStepCount == 9 && tutorialStep is >= 2 and <= 8;
            if ((!m01Step && !m02Step) || string.IsNullOrWhiteSpace(text) ||
                !TryGetBoundary(out EntityManager entityManager, out Entity boundary) ||
                !UiShellActionAdapter.IsAssistantRuntimeActive(entityManager, boundary) ||
                !entityManager.HasBuffer<AssistantMessageElement>(boundary))
            {
                return false;
            }

            FixedString64Bytes audioEventId = ResolveTutorialAudioEventId(
                tutorialStep,
                tutorialStepCount,
                phase,
                ResolveTutorialNarrationLanguage());
            if (audioEventId.Length == 0)
                return false;

            int sequence = NextTutorialNarrationSequence();
            int messageId = TutorialMessageBaseId + sequence;
            FixedString64Bytes suppressionKey = tutorialStepCount == 9
                ? new FixedString64Bytes("assistant.tutorial.m02.")
                : new FixedString64Bytes("assistant.tutorial.m01.");
            suppressionKey.Append(sequence);
            float now = (float)entityManager.World.Time.ElapsedTime;
            DynamicBuffer<AssistantMessageElement> messages =
                entityManager.GetBuffer<AssistantMessageElement>(boundary);
            RetirePreviousTutorialMessages(messages);
            messages.Add(new AssistantMessageElement
            {
                MessageId = messageId,
                SourceVersion = Math.Max(1, Time.frameCount),
                Priority = AssistantMessagePriority.High,
                RelatedKind = AssistantRecommendationKind.Explain,
                SuppressionKey = suppressionKey,
                Text = new FixedString512Bytes(text.Trim()),
                AudioEventId = audioEventId,
                CreatedAt = now,
                ExpiresAt = now + TutorialMessageLifetimeSeconds,
                RequiresNarration = 1,
                Acknowledged = 0
            });
            return true;
        }

        internal static int RetirePreviousTutorialMessages(
            DynamicBuffer<AssistantMessageElement> messages)
        {
            int retired = 0;
            for (int i = 0; i < messages.Length; i++)
            {
                AssistantMessageElement message = messages[i];
                if (message.MessageId < TutorialMessageBaseId ||
                    message.MessageId >= TutorialMessageLimitExclusive ||
                    message.Acknowledged != 0 && message.RequiresNarration == 0)
                {
                    continue;
                }

                message.Acknowledged = 1;
                message.RequiresNarration = 0;
                messages[i] = message;
                retired++;
            }

            return retired;
        }

        public static FixedString64Bytes ResolveTutorialAudioEventId(
            byte tutorialStep,
            byte tutorialStepCount,
            UiTutorialNarrationPhase phase,
            FirstLaunchNarrativeLanguage language)
        {
            bool persian = language == FirstLaunchNarrativeLanguage.Persian;
            if (tutorialStepCount == 9)
            {
                string m02EventId = tutorialStep switch
                {
                    2 => persian ? AudioEventIds.VOARIATutorialM02OpenBuildFa : AudioEventIds.VOARIATutorialM02OpenBuildEn,
                    3 => persian ? AudioEventIds.VOARIATutorialM02SelectBarracksFa : AudioEventIds.VOARIATutorialM02SelectBarracksEn,
                    4 => persian ? AudioEventIds.VOARIATutorialM02PlaceBarracksFa : AudioEventIds.VOARIATutorialM02PlaceBarracksEn,
                    5 => persian ? AudioEventIds.VOARIATutorialM02CheckCostFa : AudioEventIds.VOARIATutorialM02CheckCostEn,
                    6 => persian ? AudioEventIds.VOARIATutorialM02TrainRifleSquadFa : AudioEventIds.VOARIATutorialM02TrainRifleSquadEn,
                    7 => persian ? AudioEventIds.VOARIATutorialM02IncomingPatrolFa : AudioEventIds.VOARIATutorialM02IncomingPatrolEn,
                    8 => persian ? AudioEventIds.VOARIATutorialM02DefendPostFa : AudioEventIds.VOARIATutorialM02DefendPostEn,
                    _ => string.Empty
                };
                return new FixedString64Bytes(m02EventId);
            }

            string eventId = (tutorialStep, phase) switch
            {
                (1, _) => persian
                    ? AudioEventIds.VOARIATutorialM01FindSquadFa
                    : AudioEventIds.VOARIATutorialM01FindSquadEn,
                (2, UiTutorialNarrationPhase.PrimaryAction) => persian
                    ? AudioEventIds.VOARIATutorialM01MoveToCoverFa
                    : AudioEventIds.VOARIATutorialM01MoveToCoverEn,
                (2, UiTutorialNarrationPhase.WorldTarget) => persian
                    ? AudioEventIds.VOARIATutorialM01MoveDestinationFa
                    : AudioEventIds.VOARIATutorialM01MoveDestinationEn,
                (3, UiTutorialNarrationPhase.PrimaryAction) => persian
                    ? AudioEventIds.VOARIATutorialM01ConfirmThreatFa
                    : AudioEventIds.VOARIATutorialM01ConfirmThreatEn,
                (3, UiTutorialNarrationPhase.WorldTarget) => persian
                    ? AudioEventIds.VOARIATutorialM01AttackTargetFa
                    : AudioEventIds.VOARIATutorialM01AttackTargetEn,
                (4, UiTutorialNarrationPhase.PrimaryAction) => persian
                    ? AudioEventIds.VOARIATutorialM01EngageFa
                    : AudioEventIds.VOARIATutorialM01EngageEn,
                (4, UiTutorialNarrationPhase.WorldTarget) => persian
                    ? AudioEventIds.VOARIATutorialM01AttackTargetFa
                    : AudioEventIds.VOARIATutorialM01AttackTargetEn,
                (5, _) => persian
                    ? AudioEventIds.VOARIATutorialM01SecureCorridorFa
                    : AudioEventIds.VOARIATutorialM01SecureCorridorEn,
                _ => string.Empty
            };
            return new FixedString64Bytes(eventId);
        }

        private static FixedString64Bytes ResolveTutorialAudioEventId(
            byte tutorialStep,
            UiTutorialNarrationPhase phase,
            FirstLaunchNarrativeLanguage language) =>
            ResolveTutorialAudioEventId(tutorialStep, 5, phase, language);

        internal static bool TryResolveTutorialPresentationText(
            byte tutorialStep,
            FirstLaunchNarrativeLanguage language,
            out string title,
            out string body,
            out bool rightToLeft)
        {
            rightToLeft = language == FirstLaunchNarrativeLanguage.Persian;
            if (rightToLeft)
            {
                (title, body) = tutorialStep switch
                {
                    1 => ("گروه خود را پیدا کنید", "برای شروع، گروه فرماندهی را انتخاب کنید."),
                    2 => ("به پوشش حرکت کنید", "گروه را به موقعیت پوشش علامت‌گذاری‌شده منتقل کنید."),
                    3 => ("تهدید را بررسی کنید", "گشت مسلح نزدیک غیرنظامیان را بررسی کنید."),
                    4 => ("با گشت دشمن درگیر شوید", "به گشت دشمن تأییدشده حمله کنید."),
                    5 => ("مسیر را امن کنید", "هدف را بررسی کنید و مسیر غیرنظامیان را امن کنید."),
                    _ => (string.Empty, string.Empty)
                };
            }
            else
            {
                (title, body) = tutorialStep switch
                {
                    1 => ("Find your squad", "Select the command squad to begin."),
                    2 => ("Move to cover", "Move the squad to the marked cover position."),
                    3 => ("Confirm the threat", "Inspect the armed patrol near the civilians."),
                    4 => ("Engage the patrol", "Attack the confirmed hostile patrol."),
                    5 => ("Secure the corridor", "Check the objective and secure the civilian route."),
                    _ => (string.Empty, string.Empty)
                };
            }

            return tutorialStep is >= 1 and <= 5;
        }

        public static bool TryResolveM02GuidancePresentationText(
            in AssistantRecommendationElement recommendation,
            FirstLaunchNarrativeLanguage language,
            out string title,
            out string body,
            out bool rightToLeft)
        {
            rightToLeft = language == FirstLaunchNarrativeLanguage.Persian;
            return M02EstablishBaseLocalizedText.TryGetTutorial(
                recommendation.TutorialStep,
                language,
                out title,
                out body);
        }

        private static FirstLaunchNarrativeLanguage ResolveTutorialNarrationLanguage()
        {
            if (hasCachedTutorialNarrationLanguage)
                return cachedTutorialNarrationLanguage;

            cachedTutorialNarrationLanguage = FirstLaunchNarrativeLanguage.English;
            try
            {
                PlayerProfileSaveData profile = SaveService.CreateDefault().LoadProfile();
                if (Enum.TryParse(
                        profile.firstLaunchLanguage,
                        true,
                        out FirstLaunchNarrativeLanguage language) &&
                    language == FirstLaunchNarrativeLanguage.Persian)
                {
                    cachedTutorialNarrationLanguage = FirstLaunchNarrativeLanguage.Persian;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[ARIA Tutorial] Could not read the saved first-launch language; using English. {exception.Message}");
            }

            hasCachedTutorialNarrationLanguage = true;
            return cachedTutorialNarrationLanguage;
        }

        private static int NextTutorialNarrationSequence()
        {
            tutorialNarrationSequence++;
            if (tutorialNarrationSequence <= 0 || tutorialNarrationSequence >= 1000000)
                tutorialNarrationSequence = 1;
            return tutorialNarrationSequence;
        }

        private static void ResetTutorialNarrationSession()
        {
            cachedTutorialNarrationLanguage = FirstLaunchNarrativeLanguage.English;
            hasCachedTutorialNarrationLanguage = false;
            tutorialNarrationSequence = 0;
        }
    }
}
