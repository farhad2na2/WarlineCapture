using System;
using System.Collections.Generic;
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
        private string activeId;
        private readonly List<ComicPage> pages = new();
        private int page;
        private bool paused;
        private bool subtitles = true;
        private bool persian;
        private Sprite image;
        private Action onCompleted;

        private sealed class ComicPage
        {
            public string image;
            public string speaker;
            public string en;
            public string fa;
            public string stage;
        }

        public bool IsOpen => activeId != null;

        public bool Play(FutureMissionComicMission selectedMission)
        {
            Close();
            if (selectedMission == null || selectedMission.lines == null ||
                selectedMission.lines.Length == 0)
                return false;

            if (selectedMission.number == 1 && selectedMission.chapter >= 4)
                if (!AppendSequence($"seq.ch{selectedMission.chapter:00}.open." +
                    (selectedMission.chapter == 4 ? "air_and_armor" : "citywide_command"), "opening"))
                    return false;
            foreach (FutureMissionComicLine line in selectedMission.lines)
                pages.Add(new ComicPage
                {
                    image = selectedMission.ImageForStage(line.stage),
                    speaker = line.speaker,
                    en = line.en,
                    fa = line.fa,
                    stage = line.stage
                });
            if (selectedMission.number == 5)
            {
                if (!AppendSequence($"seq.ch{selectedMission.chapter:00}.close.protocol_fragment_{selectedMission.chapter:00}", "chapter close"))
                    return false;
                if (selectedMission.chapter == 5)
                {
                    if (!AppendSequence("seq.campaign.epilogue.canonical", "epilogue") ||
                        !AppendSequence("seq.campaign.postscript.recovery_watch", "postscript"))
                        return false;
                }
            }
            return Begin(selectedMission.id);
        }

        public bool PlaySequence(string sequenceId, bool lowState = false, Action completed = null)
        {
            Close();
            if (!AppendSequence(sequenceId, "story", lowState))
                return false;
            onCompleted = completed;
            return Begin(sequenceId);
        }

        private bool AppendSequence(string sequenceId, string stage, bool lowState = false)
        {
            BookendComicSequence sequence = BookendComicCatalog.Find(sequenceId);
            if (sequence?.pages == null || sequence.pages.Length == 0)
            {
                Debug.LogError($"[BookendComics] Missing sequence {sequenceId}.");
                return false;
            }
            foreach (BookendComicPage panel in sequence.pages)
                pages.Add(new ComicPage
                {
                    image = panel.image,
                    speaker = panel.speaker,
                    en = lowState && !string.IsNullOrEmpty(panel.lowEn) ? panel.lowEn : panel.en,
                    fa = lowState && !string.IsNullOrEmpty(panel.lowFa) ? panel.lowFa : panel.fa,
                    stage = stage
                });
            return true;
        }

        private bool Begin(string sequenceId)
        {
            if (pages.Count == 0)
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

            image = Resources.Load<Sprite>("FutureMissionComics/" + pages[0].image);
            if (image == null)
            {
                Debug.LogError($"[FutureMissionComics] Missing image for {sequenceId}.");
                pages.Clear();
                return false;
            }

            activeId = sequenceId;
            page = 0;
            paused = false;
            subtitles = true;
            persian = UiShellRuntimeGateway.Localization.IsRightToLeft;
            view.ApplyLanguage(persian, FallbackGameTextResolver.Instance);
            view.SetInteractiveState(NarrativeInteractiveStateKind.None);
            view.ApplyLocation(new NarrativeLocationPresentationModel { Visible = false });
            view.ApplyPanel(new NarrativePanelPresentationModel
            {
                StateId = sequenceId,
                PanelSprite = image,
                Tint = Color.white
            });
            view.DialogueView.BindInput(Advance);
            view.PlaybackControlsView.BindSkip(Complete);
            view.PlaybackControlsView.BindTransport(TogglePause, ToggleSubtitles);
            view.SetSkipState(true, true, persian ? "بستن" : "CLOSE");
            ShowPage();
            view.SetVisible(true);
            return true;
        }

        private void Update()
        {
            if (!IsOpen)
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
            if (!IsOpen || paused)
                return;
            if (phase == NarrativeDialoguePhase.Revealing)
            {
                view.DialogueView.CompleteLine();
                return;
            }
            if (++page >= pages.Count)
                Complete();
            else
                ShowPage();
        }

        private void Complete()
        {
            Action callback = onCompleted;
            Close();
            callback?.Invoke();
        }

        private void ShowPage()
        {
            ComicPage line = pages[page];
            Sprite stageImage = Resources.Load<Sprite>("FutureMissionComics/" + line.image);
            if (stageImage == null)
            {
                Debug.LogError($"[FutureMissionComics] Missing {line.stage} image for {activeId}.");
                Close();
                return;
            }
            if (stageImage != image)
            {
                image = stageImage;
                view.ApplyPanel(new NarrativePanelPresentationModel
                {
                    StateId = activeId + "." + line.stage + "." + page,
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
            view.PlaybackControlsView.ApplyTransport(page + 1, pages.Count, paused, subtitles);
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
                "opening" => persian ? "آغاز فصل" : "CHAPTER OPENING",
                "chapter close" => persian ? "پایان فصل" : "CHAPTER CLOSE",
                "epilogue" => persian ? "فرجام" : "EPILOGUE",
                "postscript" => persian ? "پس‌گفتار" : "POSTSCRIPT",
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
            view.PlaybackControlsView.ApplyTransport(page + 1, pages.Count, paused, subtitles);
        }

        private void ToggleSubtitles()
        {
            subtitles = !subtitles;
            view.DialogueView.SetSubtitlesVisible(subtitles);
            view.PlaybackControlsView.ApplyTransport(page + 1, pages.Count, paused, subtitles);
        }

        public void Close()
        {
            if (view != null && IsOpen)
            {
                view.DialogueView.UnbindInput();
                view.PlaybackControlsView.UnbindSkip();
                view.PlaybackControlsView.UnbindTransport();
                view.SetSkipState(false, false, null);
                view.ClearPanel();
                view.SetVisible(false);
            }
            activeId = null;
            onCompleted = null;
            pages.Clear();
            image = null;
            view = null;
            speakers = null;
        }

        private void OnDisable() => Close();
    }
}
