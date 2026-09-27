using System;
using Game.Catalog.Contracts;
using Game.Configs;
using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    // Uses the shipped narrative canvas and dialogue controls. Future missions do not
    // enter CampaignMissionSequence or the gameplay catalog until their scenarios exist.
    [DisallowMultipleComponent]
    public sealed class FutureMissionComicPreviewController : MonoBehaviour
    {
        private NarrativeSequenceView view;
        private NarrativeSpeakerCatalog speakers;
        private FutureMissionComicMission mission;
        private int page;
        private bool paused;
        private bool subtitles = true;
        private bool persian;
        private Sprite image;

        public bool IsOpen => mission != null;

        public bool Play(FutureMissionComicMission selectedMission)
        {
            Close();
            if (selectedMission == null || selectedMission.lines == null ||
                selectedMission.lines.Length == 0)
                return false;

            view = FindAnyObjectByType<NarrativeSequenceView>(FindObjectsInactive.Include);
            NarrativeSpeakerCatalog[] loadedSpeakers =
                Resources.FindObjectsOfTypeAll<NarrativeSpeakerCatalog>();
            speakers = loadedSpeakers.Length > 0 ? loadedSpeakers[0] : null;
            if (view == null || view.DialogueView == null || view.PlaybackControlsView == null)
            {
                Debug.LogError("[FutureMissionComics] Narrative canvas is unavailable.");
                return false;
            }

            image = Resources.Load<Sprite>("FutureMissionComics/" + selectedMission.image);
            if (image == null)
            {
                Debug.LogError($"[FutureMissionComics] Missing image for {selectedMission.id}.");
                return false;
            }

            mission = selectedMission;
            page = 0;
            paused = false;
            subtitles = true;
            persian = UiShellRuntimeGateway.Localization.IsRightToLeft;
            view.ApplyLanguage(persian, FallbackGameTextResolver.Instance);
            view.SetInteractiveState(NarrativeInteractiveStateKind.None);
            view.ApplyLocation(new NarrativeLocationPresentationModel { Visible = false });
            view.ApplyPanel(new NarrativePanelPresentationModel
            {
                StateId = selectedMission.id,
                PanelSprite = image,
                Tint = Color.white
            });
            view.DialogueView.BindInput(Advance);
            view.PlaybackControlsView.BindSkip(Close);
            view.PlaybackControlsView.BindTransport(TogglePause, ToggleSubtitles);
            view.SetSkipState(true, true, persian ? "بستن" : "CLOSE");
            ShowPage();
            view.SetVisible(true);
            return true;
        }

        private void Update()
        {
            if (mission == null)
                return;
            bool currentPersian = UiShellRuntimeGateway.Localization.IsRightToLeft;
            if (currentPersian == persian)
                return;
            persian = currentPersian;
            view.ApplyLanguage(persian, FallbackGameTextResolver.Instance);
            view.SetSkipState(true, true, persian ? "بستن" : "CLOSE");
            ShowPage();
        }

        private void Advance(NarrativeDialoguePhase phase)
        {
            if (mission == null || paused)
                return;
            if (phase == NarrativeDialoguePhase.Revealing)
            {
                view.DialogueView.CompleteLine();
                return;
            }
            if (++page >= mission.lines.Length)
                Close();
            else
                ShowPage();
        }

        private void ShowPage()
        {
            FutureMissionComicLine line = mission.lines[page];
            string imageName = mission.ImageForStage(line.stage);
            Sprite stageImage = Resources.Load<Sprite>("FutureMissionComics/" + imageName);
            if (stageImage == null)
            {
                Debug.LogError($"[FutureMissionComics] Missing {line.stage} image for {mission.id}.");
                Close();
                return;
            }
            if (stageImage != image)
            {
                image = stageImage;
                view.ApplyPanel(new NarrativePanelPresentationModel
                {
                    StateId = mission.id + "." + line.stage,
                    PanelSprite = image,
                    Tint = Color.white
                });
            }
            view.DialogueView.ApplySpeaker(ResolveSpeaker(line.speaker, line.stage));
            string caption = persian ? line.fa : line.en;
            view.DialogueView.SetAccessibilityText(caption);
            view.DialogueView.PrepareLine(caption, new NarrativeSubtitleStyle
            {
                Visible = true,
                FontSize = 50f,
                BackgroundOpacity = 0.75f,
                InstantText = true,
                ReducedMotion = true
            });
            view.DialogueView.SetSubtitlesVisible(subtitles);
            view.PlaybackControlsView.ApplyTransport(page + 1, mission.lines.Length, paused, subtitles);
        }

        private NarrativeSpeakerPresentationModel ResolveSpeaker(string speakerName, string stage)
        {
            NarrativeSpeakerRecord record = null;
            if (Enum.TryParse(speakerName, out NarrativeSpeakerId speakerId) && speakers != null)
                for (int index = 0; index < speakers.Speakers.Count; index++)
                    if (speakers.Speakers[index].SpeakerId == speakerId)
                    {
                        record = speakers.Speakers[index];
                        break;
                    }

            string role = stage switch
            {
                "brief" => persian ? "توجیه" : "BRIEFING",
                "comms" => persian ? "ارتباط" : "COMMS",
                _ => persian ? "جمع‌بندی" : "DEBRIEF"
            };
            return new NarrativeSpeakerPresentationModel
            {
                SpeakerId = record?.SpeakerId ?? NarrativeSpeakerId.Radio,
                DisplayName = record != null
                    ? UiShellRuntimeGateway.Localization.Get(record.NameKey, record.NameFallback)
                    : LocalizeSpeaker(speakerName),
                Role = role,
                AccessibleLabel = speakerName,
                IdentitySprite = record?.IdentitySprite,
                AccentColor = record?.AccentColor ?? Color.white,
                Treatment = record?.Treatment ?? NarrativeSpeakerTreatment.HumanPortrait
            };
        }

        private string LocalizeSpeaker(string speakerName)
        {
            if (!persian)
                return speakerName;
            return speakerName switch
            {
                "Karim" => "کریم",
                "Yusuf" => "یوسف",
                _ => speakerName
            };
        }

        private void TogglePause()
        {
            paused = !paused;
            view.PlaybackControlsView.ApplyTransport(page + 1, mission.lines.Length, paused, subtitles);
        }

        private void ToggleSubtitles()
        {
            subtitles = !subtitles;
            view.DialogueView.SetSubtitlesVisible(subtitles);
            view.PlaybackControlsView.ApplyTransport(page + 1, mission.lines.Length, paused, subtitles);
        }

        public void Close()
        {
            if (view != null && mission != null)
            {
                view.DialogueView.UnbindInput();
                view.PlaybackControlsView.UnbindSkip();
                view.PlaybackControlsView.UnbindTransport();
                view.SetSkipState(false, false, null);
                view.ClearPanel();
                view.SetVisible(false);
            }
            mission = null;
            image = null;
            view = null;
            speakers = null;
        }

        private void OnDisable() => Close();
    }
}
