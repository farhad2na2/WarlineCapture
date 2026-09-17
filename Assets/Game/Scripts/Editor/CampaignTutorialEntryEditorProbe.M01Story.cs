using System;
using System.Linq;
using Game.Configs;
using Game.Runtime;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class CampaignTutorialEntryEditorProbe
    {
        public static void RunM01StoryReplay()
        {
            FirstLaunchSetupRetirement.Apply();
            Run();
            SessionState.SetBool(RecoveryJourney, false);
            SessionState.SetBool(CampaignChain, false);
            SessionState.SetBool(StoryReplay, true);
            checkedVoices.Clear(); comicClip = null; comicProgress = 0;
        }

        private static void AuditM01StorySetup()
        {
            var language = UnityEngine.Object.FindAnyObjectByType<FirstLaunchLanguageChoiceView>();
            if (language != null && language.IsVisible)
                throw new InvalidOperationException("M1 replay unexpectedly reopened language selection.");
            var narrative = UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>();
            if (narrative == null) return;
            if (narrative.GuidanceChoiceView != null)
                throw new InvalidOperationException("Retired guidance choice still exists in the menu instance.");
            if (Visible(narrative, "rootGroup") && narrative.CommanderIdentityView.gameObject.activeInHierarchy)
                throw new InvalidOperationException("M1 replay reopened commander setup.");
            if (stage >= 2 && Visible(narrative, "rootGroup"))
                throw new InvalidOperationException("M1 story reappeared after its gameplay handoff.");
            if (narrative.VoiceSource.isPlaying && narrative.VoiceSource.clip != null &&
                narrative.VoiceSource.clip.name.StartsWith("p14_commander", StringComparison.Ordinal))
            {
                var saved = SaveService.CreateDefault().LoadProfile();
                var identity = narrative.CommanderIdentityView;
                int expected = Mathf.Clamp(saved.firstLaunchCommanderPortraitIndex, 0, identity.PortraitOptionCount - 1);
                if (identity.SelectedPortraitIndex != expected || identity.SelectedPortrait == null ||
                    narrative.DialogueView.CurrentPortraitSprite != identity.SelectedPortrait || !narrative.DialogueView.IsPortraitVisible)
                    throw new InvalidOperationException("Commander dialogue did not use the saved avatar.");
                string marker = "portrait/" + GameLocalization.CurrentLocaleCode;
                if (checkedVoices.Add(marker))
                    Debug.Log("[M01StoryReplay] saved-commander=" + expected + " sprite=" + identity.SelectedPortrait.name + " locale=" + GameLocalization.CurrentLocaleCode);
            }
        }

        private static void VerifyM01StoryPlayback(string locale)
        {
            var heard = checkedVoices.Where(p => p.StartsWith(locale + "/", StringComparison.Ordinal) &&
                p.Contains("/FirstLaunch/Voice/", StringComparison.Ordinal)).ToArray();
            if (heard.Length < 12 || !heard.Any(p => p.EndsWith("/p02_radio.wav", StringComparison.Ordinal)) ||
                !heard.Any(p => p.EndsWith("/p18_aria.wav", StringComparison.Ordinal)) ||
                !checkedVoices.Contains("portrait/" + locale))
                throw new InvalidOperationException("M1 replay did not play its complete opening: " + locale + " clips=" + heard.Length);
            Debug.Log("[M01StoryReplay] full-opening=" + locale + " clips=" + heard.Length + " setup=none gameplay=ready");
        }
    }
}
