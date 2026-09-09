using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Configs;
using Game.Composition;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using SettingsService = Game.UI.Runtime.SettingsService;

namespace Game.Editor
{
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        private const string ComicKey="Warline.M03.Probe.Comics";
        private static string comicPanel;
        private static int comicVariant;
        private static bool comicVariantReady;
        private static readonly HashSet<string> comicCaptures=new();
        private static UISubtitleSize comicRequestedSize, comicOriginalSize;
        public static void RunLargeComicValidation() => StartComicValidation(UISubtitleSize.Large);
        public static void RunExtraLargeComicValidation() => StartComicValidation(UISubtitleSize.ExtraLarge);
        public static void RunComicValidation() => StartComicValidation(UISubtitleSize.Standard);
        private static void StartComicValidation(UISubtitleSize size)=>RunChecked(()=>
        { PrepareComicValidation(size); StartResultValidation(false); });
        public static void RunFullGuidanceLargeComicValidation()=>RunChecked(()=>
        { PrepareComicValidation(UISubtitleSize.Large); RunFullGuidanceJourney(); });
        private static void PrepareComicValidation(UISubtitleSize size)
        {
            FirstLaunchNarrativeV3PrefabBuilder.RefreshComicSubtitleLayout();
            var settings=SettingsService.Load(); comicOriginalSize=settings.Narrative.SubtitleSize;
            comicRequestedSize=size; settings.Narrative.SubtitleSize=size; SettingsService.Save(settings);
            comicPanel=null; comicVariant=0; comicVariantReady=false; comicCaptures.Clear();
            SessionState.SetBool(ComicKey,true);
        }
        private static bool AdvanceComicValidation(EntityManager em)
        {
            if(!SessionState.GetBool(ComicKey,false)) return false;
            var narrative=UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>();
            if(narrative==null || !Visible(narrative,"rootGroup")) return false;
            if(!capturedBrief) {M03RadarWarningRuntimeGridProbe.Capture(em,Output); capturedBrief=true;}
            string sprite=narrative.CurrentPanelSprite?.name;
            if(sprite==null || sprite.Length<7 || !sprite.StartsWith("M03-",StringComparison.Ordinal)) return true;
            string id=sprite.Substring(0,7);
            if(id!=comicPanel) {comicPanel=id; comicVariant=0; comicVariantReady=false;}
            if(comicVariant>=4) return true;
            var input=typeof(NarrativeDialogueView).GetField("inputButton",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(narrative.DialogueView) as Button;
            if(input==null || !input.interactable) return true;
            if(!comicVariantReady)
            {
                GameLocalization.SetLocale(comicVariant%2==0 ? "en" : "fa-IR",false);
                MainMenuV3PrefabBuilder.SetGameViewResolution(comicVariant<2 ? 1920 : 2400,1080);
                comicVariantReady=true; return true;
            }
            if(narrative.DialogueView.Phase==NarrativeDialoguePhase.Revealing) {input.onClick.Invoke(); return true;}
            string capture=$"comic-{comicRequestedSize}-{id}-{(comicVariant%2==0 ? "en" : "fa")}-{(comicVariant<2 ? "16x9" : "20x9")}";
            if(!CaptureUiBeforeAction(capture)) return true;
            ValidateComicCaption(narrative);
            if(sprite!=id+(comicVariant<2 ? "-16x9" : "-20x9"))
                throw new InvalidOperationException("Comic has the wrong authored aspect crop: "+sprite);
            var owner=ReadComicPresentation();
            if(owner==null || owner.ResidentPanelAssetCount is <1 or >2)
                throw new InvalidOperationException("Comic must hold only its current/next Addressable panels.");
            comicCaptures.Add(capture);
            if(++comicVariant<4) {comicVariantReady=false; return true;}
            if(id.StartsWith("M03-D",StringComparison.Ordinal)) resultPanels.Add(id);
            GameLocalization.SetLocale("en",false); MainMenuV3PrefabBuilder.SetGameViewResolution(1920,1080);
            input.onClick.Invoke();
            Debug.Log($"[M03ComicProbe] panel={id} locales=2 aspects=2 fullCaptions=Passed captures={comicCaptures.Count}");
            return true;
        }
        private static void ValidateComicCaption(NarrativeSequenceView view)
        {
            var caption=view.DialogueView.transform.Find("DialogueText").GetComponent<TMP_Text>();
            float minimum=comicRequestedSize==UISubtitleSize.ExtraLarge ? 43.1f : comicRequestedSize==UISubtitleSize.Large ? 35.9f : 0;
            caption.ForceMeshUpdate();
            if(caption.fontSize<minimum) throw new InvalidOperationException("Comic shrank the requested "+comicRequestedSize+" caption: "+caption.fontSize);
            bool arabic=false;
            foreach(var text in view.DialogueView.GetComponentsInChildren<TMP_Text>())
            {
                if(string.IsNullOrWhiteSpace(text.text)) continue;
                arabic|=text.text.Any(c=>c>='\u0600' && c<='\u06ff' || c>='\ufb50' && c<='\ufeff');
                text.ForceMeshUpdate();
                if(text.isTextOverflowing || text.isTextTruncated || text.textInfo.characterInfo.Take(text.textInfo.characterCount)
                    .Any(c=>c.isVisible && (c.character=='\u25a1' || c.character=='\ufffd' || c.textElement==null)))
                    throw new InvalidOperationException($"Comic caption invalid: {text.name} locale={GameLocalization.CurrentLocaleCode} panel={comicPanel} overflow={text.isTextOverflowing} truncated={text.isTextTruncated} rect={text.rectTransform.rect} preferred={text.preferredWidth}/{text.preferredHeight} font={text.font.name} missing="+string.Join(",",text.textInfo.characterInfo.Take(text.textInfo.characterCount).Where(c=>c.isVisible && (c.character=='\u25a1' || c.character=='\ufffd' || c.textElement==null)).Select(c=>((int)c.character).ToString("x4"))));
            }
            if(arabic!=(comicVariant%2==1)) throw new InvalidOperationException("Comic caption did not update to the requested language.");
            var safeArea=view.transform.Find("SafeArea");
            int timelines=0;
            foreach(Transform child in safeArea) if(child.name=="ComicTimeline") timelines++;
            if(timelines!=1) throw new InvalidOperationException("Comic must contain exactly one visible progress header, got "+timelines);
            var transport=view.PlaybackControlsView;
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var title=typeof(NarrativePlaybackControlsView).GetField("stateLabel",flags).GetValue(transport) as TMP_Text;
            var page=typeof(NarrativePlaybackControlsView).GetField("pageLabel",flags).GetValue(transport) as TMP_Text;
            string titleSource=title is RTLTMPro.RTLTextMeshPro rtlTitle ? rtlTitle.OriginalText : title?.text;
            string pageSource=page is RTLTMPro.RTLTextMeshPro rtlPage ? rtlPage.OriginalText : page?.text;
            if(title==null || page==null || titleSource!=GameLocalization.Get("ui.narrative.story","STORY") ||
                comicVariant%2==1 && pageSource.Any(c=>c>='0' && c<='9') ||
                pageSource!=(comicVariant%2==1 ? ((char)('\u06f0'+comicPanel[^1]-'0'))+" / ۳" : comicPanel[^1]+" / 3"))
                throw new InvalidOperationException($"Comic progress chrome mismatch: title={titleSource}, page={pageSource}, panel={comicPanel}, locale={GameLocalization.CurrentLocaleCode}.");
        }
        private static void StopComicValidation()
        {
            if(!SessionState.GetBool(ComicKey,false)) return;
            var settings=SettingsService.Load(); settings.Narrative.SubtitleSize=comicOriginalSize; SettingsService.Save(settings);
        }
        private static FirstLaunchNarrativeSequencePresentationSystemHelper ReadComicPresentation()
        {
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var menu=UnityEngine.Object.FindAnyObjectByType<MenuBootstrapView>();
            object runtime=typeof(MenuBootstrapView).GetField("campaignMissionBootstrap",flags)?.GetValue(menu);
            object debrief=runtime?.GetType().GetField("debrief",flags)?.GetValue(runtime);
            return debrief?.GetType().GetField("presentation",flags)?.GetValue(debrief) as FirstLaunchNarrativeSequencePresentationSystemHelper;
        }
    }
}
