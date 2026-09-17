using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Configs;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class CampaignTutorialEntryEditorProbe
    {
        private static AudioClip comicClip;
        private static float comicProgress;
        private static readonly HashSet<string> checkedVoices = new();
        private static void AuditReadinessPresentation()
        {
            string locale = GameLocalization.CurrentLocaleCode;
            foreach (var source in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            {
                if (!source.isPlaying || source.clip == null) continue;
                string path = AssetDatabase.GetAssetPath(source.clip);
                if (!path.Contains("/Voice/", StringComparison.Ordinal) && !path.Contains("/voice/", StringComparison.Ordinal)) continue;
                bool fa = path.Contains("/fa/", StringComparison.Ordinal) || path.EndsWith("_fa.wav", StringComparison.Ordinal);
                if (fa != (locale == "fa-IR")) throw new InvalidOperationException("Voice/UI locale mismatch: " + locale + "/" + path);
                if (source.loop) throw new InvalidOperationException("Dialogue cannot loop: " + path);
                if (checkedVoices.Add(locale + "/" + path)) Debug.Log("[ReadinessVoice] locale=" + locale + " clip=" + path);
            }
            var view = UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>();
            var voice = view?.VoiceSource;
            var current = voice != null && voice.isPlaying ? voice.clip : null;
            if (current != comicClip)
            {
                if (comicClip != null)
                {
                    if (comicProgress < comicClip.length - .45f) throw new InvalidOperationException("Comic cut off early: " + comicClip.name + " heard=" + comicProgress + "/" + comicClip.length);
                    Debug.Log("[ReadinessComic] completed=" + AssetDatabase.GetAssetPath(comicClip) + " heard=" + comicProgress);
                }
                comicClip = current; comicProgress = 0;
                if (current != null) CaptureJourney("comic-" + current.name);
            }
            if (current != null) comicProgress = Mathf.Max(comicProgress, (float)voice.timeSamples / current.frequency);
            var aria = UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();
            if (stage == 2 && aria != null && aria.IsPresentationVisible && aria.BodyText.gameObject.activeInHierarchy && !string.IsNullOrWhiteSpace(aria.CurrentInstructionBody))
            {
                if (aria.BodyText.fontSize < 24 || aria.BodyText.enableAutoSizing)
                    throw new InvalidOperationException("ARIA instructions became small again: " + aria.BodyText.fontSize);
            }
        }
    }
}
