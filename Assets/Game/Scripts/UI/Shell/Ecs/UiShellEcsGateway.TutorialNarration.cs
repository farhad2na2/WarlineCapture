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
            UiTutorialNarrationPhase phase,
            string text)
        {
            if (tutorialStep is < 1 or > 5 || string.IsNullOrWhiteSpace(text) ||
                !TryGetBoundary(out EntityManager entityManager, out Entity boundary) ||
                !UiShellActionAdapter.IsAssistantRuntimeActive(entityManager, boundary) ||
                !entityManager.HasBuffer<AssistantMessageElement>(boundary))
            {
                return false;
            }

            FixedString64Bytes audioEventId = ResolveTutorialAudioEventId(
                tutorialStep,
                phase,
                ResolveTutorialNarrationLanguage());
            if (audioEventId.Length == 0)
                return false;

            int sequence = NextTutorialNarrationSequence();
            int messageId = TutorialMessageBaseId + sequence;
            FixedString64Bytes suppressionKey = new("assistant.tutorial.m01.");
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
                Text = new FixedString128Bytes(text.Trim()),
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

        internal static FixedString64Bytes ResolveTutorialAudioEventId(
            byte tutorialStep,
            UiTutorialNarrationPhase phase,
            FirstLaunchNarrativeLanguage language)
        {
            bool persian = language == FirstLaunchNarrativeLanguage.Persian;
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
            title = string.Empty;
            body = string.Empty;
            rightToLeft = language == FirstLaunchNarrativeLanguage.Persian;
            string targetId = recommendation.TargetId.ToString();
            if (string.Equals(targetId, "ui.build_drawer.barracks", StringComparison.Ordinal))
            {
                if (rightToLeft)
                {
                    title = "پادگان را انتخاب کنید";
                    body = "پادگان را از فهرست ساختمان‌ها انتخاب کنید.";
                }
                else
                {
                    title = "Select Barracks";
                    body = "Select Barracks from the building catalog.";
                }

                return true;
            }

            if (!string.Equals(targetId, "ui.build_drawer.rifle", StringComparison.Ordinal))
                return false;

            if (rightToLeft)
            {
                title = "یک گروه تفنگدار در صف بگذارید";
                body = "بخش تولید را باز کنید، سربازان را انتخاب کنید و گروه تفنگدار موردنیاز را به صف آموزش اضافه کنید.";
            }
            else
            {
                title = "Queue a rifle squad";
                body = "Open production, select Soldiers, and recruit the required rifle squad.";
            }

            return true;
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
