using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class AriaTutorialBriefingView
    {
        private RectTransform contentActions;
        [SerializeField] private RectTransform utilityActions, extractionActions, openingLayout;
        [SerializeField] private TMP_Text alertCopy;
        private TMP_Text openingCopy;
        private string measuredTitle, measuredBody, measuredAlert, measuredOpening;
        private string measuredWatchExplanation;
        private float measuredWidth;
        private int measuredState=-1;
        private TMP_FontAsset measuredFont;
        private bool _layoutDirty=true;
        private MissionHudTouchLayoutView mapDock;
        private RectTransform bodyViewport;
        private ScrollRect bodyScroll;
        private Scrollbar bodyScrollbar;
        private float measuredAvailable;

        private void OnDisable() => Canvas.willRenderCanvases -= RefreshContentLayout;

        private void ApplyMissionLayout(bool active,bool large=false)
        {
            _missionLayoutActive=active; _missionLayoutLarge=large;
            RefreshContentLayout();
        }

        public void RefreshContentLayout()
        {
            // The standalone briefing popup owns a different layout; this is the HUD rail.
            if(missionPortraitClip==null || missionPortraitStage==null || !TryBindHierarchy()) return;
            ApplyHudButtonIcons();
            var rail=(RectTransform)transform;
            if(contentActions==null) contentActions=(RectTransform)showMeButton.transform.parent;
            if(openingCopy==null && openingLayout!=null) openingCopy=openingLayout.GetComponentInChildren<TMP_Text>(true);
            bool tutorial=briefingLayout.gameObject.activeSelf && (HasText(titleText) || HasText(bodyText));
            bool opening=openingCopy!=null && openingCopy.gameObject.activeInHierarchy && HasText(openingCopy);
            bool alert=!tutorial && !opening && alertCopy!=null && alertCopy.gameObject.activeSelf && HasText(alertCopy);
            bool hasText=tutorial || opening || alert;
            doItButton.gameObject.SetActive(false);
            bool selectionActive=selectionButton!=null && selectionButton.gameObject.activeSelf;
            int actionCount=(showMeButton.gameObject.activeSelf?1:0)+(ContinueButton.gameObject.activeSelf?1:0)+(selectionActive?1:0);
            bool hasActions=actionCount>0;
            // W39 Board 3: 16 px insets, 12 px gaps, 72-unit (83 px) buttons.
            float inset=Inset, gap=Gap, stride=ButtonStride;
            float actionGap=ContinueButton.gameObject.activeSelf?44:gap;
            float actionSpacing=hasActions?actionGap+actionCount*stride:gap;
            int utilityCount=ActiveButtonCount(utilityActions), extractionCount=ActiveButtonCount(extractionActions), watchCount=ActiveButtonCount(watchActions);
            contentActions.gameObject.SetActive(hasActions);
            if(mapDock==null) mapDock=GetComponentInParent<MissionHudTouchLayoutView>();
            float available=900;
            if(mapDock!=null && mapDock.Minimap!=null)
                available=mapDock.AssistantHeight(rail);
            bool paused=!hasText && watchCount>0 && !watchShowsStop && !watchConfirming;
            int state=(hasActions?64:0)|(tutorial?1:0)|(opening?2:0)|(alert?4:0)|(_missionLayoutLarge?8:0)|(_rightToLeft?1<<28:0)|
                (watchCount<<20)|(utilityCount<<8)|(extractionCount<<12)|(actionCount<<16)|(ContinueButton.gameObject.activeSelf?32:0)|(selectionActive?128:0)|
                (paused?1<<29:0)|(watchShowsStop?1<<30:0);
            if(!_layoutDirty && Mathf.Abs(measuredAvailable-available)<.1f && measuredState==state && measuredWidth==rail.rect.width && measuredFont==bodyText.font &&
                measuredTitle==titleText.text && measuredBody==bodyText.text && measuredAlert==alertCopy?.text && measuredOpening==openingCopy?.text &&
                measuredWatchExplanation==(watchExplanation != null && watchExplanation.gameObject.activeSelf ? watchExplanation.text : null)) return;
            bool newInstruction=measuredTitle!=titleText.text;
            // A fitting ScrollRect can report zero even though the reader never scrolled.
            // When the dock settles and the copy starts overflowing, open at the first line.
            bool preserveScroll=bodyScroll!=null && bodyScroll.vertical && !newInstruction;
            float scrollPosition=preserveScroll ? Mathf.Clamp01(bodyScroll.verticalNormalizedPosition) : 1f;
            if(!preserveScroll && bodyScroll!=null) bodyScroll.StopMovement();
            _layoutDirty=false; measuredAvailable=available; measuredState=state; measuredWidth=rail.rect.width; measuredFont=bodyText.font;
            measuredTitle=titleText.text; measuredBody=bodyText.text; measuredAlert=alertCopy?.text; measuredOpening=openingCopy?.text;
            measuredWatchExplanation=watchExplanation != null && watchExplanation.gameObject.activeSelf ? watchExplanation.text : null;
            ApplyInstructionTypography();
            float width=rail.rect.width-2*inset;
            float titleHeight=tutorial ? Measure(titleText,width,26*InstructionScale) : 0;
            float bodyHeight=tutorial ? Measure(bodyText,width,InstructionBodySize) : 0;
            float watchExplanationHeight = HasText(watchExplanation) && watchExplanation.gameObject.activeSelf
                ? Measure(watchExplanation,width,InstructionBodySize)+gap : 0;
            float utilityHeight=(utilityCount+extractionCount+watchCount)*stride+watchExplanationHeight;
            // Reserve readable controls and a text viewport. Prose length never
            // determines the portrait size; additional copy belongs in the scroll area.
            float fixedContent=20+titleHeight+56+(titleHeight>0?8:0)+(tutorial?actionSpacing:0)+utilityHeight;
            // Without guidance copy the header stays compact (ARIA, status, portrait) and the
            // panel never reserves an empty portrait stage.
            float portraitHeight=hasText ? Mathf.Clamp(available-fixedContent,88,124) : CompactHeaderHeight;
            ApplyPortraitHeader(portraitHeight,true,alignEnd:!hasText);
            if(missionTelemetry!=null) missionTelemetry.SetActive(false);
            ApplyStatusLine(paused);
            PanelState=AriaPanelStates.Classify(hasText,(tutorial?actionCount:0)+utilityCount+extractionCount,watchCount>0,watchShowsStop);
            float y=8+portraitHeight+gap;
            bool placedBelowHeader=false;
            if(tutorial)
            {
                float visibleBody=Mathf.Min(bodyHeight,Mathf.Max(56,available-y-titleHeight-(titleHeight>0?8:0)-actionSpacing-utilityHeight));
                EnsureBodyViewport();
                Place(titleText.rectTransform,0,0,width,titleHeight);
                Place(bodyViewport,0,titleHeight+(titleHeight>0?8:0),width,visibleBody);
                Place(bodyText.rectTransform,0,0,width,bodyHeight);
                bodyScroll.vertical=visibleBody<bodyHeight;
                Place((RectTransform)bodyScrollbar.transform,width+1,titleHeight+(titleHeight>0?8:0),10,visibleBody);
                bodyScrollbar.gameObject.SetActive(bodyScroll.vertical);
                bodyScroll.verticalNormalizedPosition=bodyScroll.vertical ? scrollPosition : 1f;
                bodyViewport.GetComponent<Image>().raycastTarget=bodyScroll.vertical;
                titleText.gameObject.SetActive(titleHeight>0); bodyText.gameObject.SetActive(bodyHeight>0);
                float textHeight=titleHeight+visibleBody+(titleHeight>0 && bodyHeight>0?8:0);
                Place(contentActions,0,textHeight+actionGap,width,Mathf.Max(0,actionCount*stride-gap));
                float actionY=0;
                if(selectionActive) {Place((RectTransform)selectionButton.transform,0,actionY,width,ButtonHeight);actionY+=stride;}
                if(ContinueButton.gameObject.activeSelf) {Place((RectTransform)ContinueButton.transform,0,actionY,width,ButtonHeight);actionY+=stride;}
                Place((RectTransform)showMeButton.transform,0,actionY,width,ButtonHeight);
                Place(briefingLayout,inset,y,width,textHeight+(hasActions?actionSpacing-gap:0));
                y+=textHeight+actionSpacing; placedBelowHeader=true;
            }
            else
            {
                // No reserved tutorial rectangle or inactive text raycast surface.
                if(briefingLayout.gameObject.activeSelf) briefingLayout.gameObject.SetActive(false);
                if(opening) {float h=Measure(openingCopy,width,InstructionBodySize);Place(openingLayout,inset,y,width,h);Place(openingCopy.rectTransform,0,0,width,h);y+=h+gap;placedBelowHeader=true;}
                else if(alert) {float h=Measure(alertCopy,width,InstructionBodySize);Place(alertCopy.rectTransform,inset,y,width,h);y+=h+gap;placedBelowHeader=true;}
            }
            if(utilityActions!=null && utilityActions.gameObject.activeSelf && utilityCount>0)
            {PlaceUtilityRow(utilityActions,y,width);y+=utilityCount*stride;placedBelowHeader=true;}
            if(extractionActions!=null && extractionActions.gameObject.activeSelf && extractionCount>0)
            {PlaceUtilityRow(extractionActions,y,width);y+=extractionCount*stride;placedBelowHeader=true;}
            if(watchActions!=null && watchActions.gameObject.activeSelf)
            {
                if(watchExplanationHeight>0)
                {
                    Place(watchExplanation.rectTransform,inset,y,width,watchExplanationHeight-gap);
                    y+=watchExplanationHeight;placedBelowHeader=true;
                }
                if(watchCount>0) {PlaceUtilityRow(watchActions,y,width);y+=watchCount*stride;placedBelowHeader=true;}
            }
            // Size from content: the last trailing gap becomes the 16 px bottom inset.
            rail.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,placedBelowHeader ? y-gap+inset : 8+portraitHeight+8);
        }

        private static int ActiveButtonCount(RectTransform row)
        {
            if(row==null || !row.gameObject.activeSelf) return 0;
            int count=0;
            foreach(Transform child in row)
                if(child.gameObject.activeSelf && child.TryGetComponent<Button>(out _)) count++;
            return count;
        }

        // W39 Board 3 spacing in HUD units (1080/941 px per unit on the reference screen).
        internal static float Inset => MatchHudSidePanelLayout.Units(MatchHudSidePanelLayout.AriaInsetPx);
        internal static float Gap => MatchHudSidePanelLayout.Units(MatchHudSidePanelLayout.AriaGapPx);
        internal const float ButtonHeight = 72f;
        internal static float ButtonStride => ButtonHeight + Gap;
        internal const float CompactHeaderHeight = 88f;

        private static void PlaceUtilityRow(RectTransform row,float y,float width)
        {
            Place(row,Inset,y,width,Mathf.Max(0,ActiveButtonCount(row)*ButtonStride-Gap));
            float offset=0;
            foreach(Transform child in row)
                if(child.gameObject.activeSelf && child.TryGetComponent<Button>(out _))
                {Place((RectTransform)child,0,offset,width,ButtonHeight);offset+=ButtonStride;}
        }

        internal AriaPanelState PanelState { get; private set; }
        internal TMP_Text StatusLine => statusLine;
        private TMP_Text statusLine;
        // Stopped state: a short status under the ARIA name instead of an empty portrait stage.
        private void ApplyStatusLine(bool paused)
        {
            if(statusLine==null)
            {
                if(!paused) return;
                var go=new GameObject("AriaStatus",typeof(RectTransform),typeof(TextMeshProUGUI));
                go.transform.SetParent(transform,false);
                statusLine=go.GetComponent<TMP_Text>();
                statusLine.raycastTarget=false; statusLine.enableAutoSizing=false;
                statusLine.textWrappingMode=TextWrappingModes.NoWrap; statusLine.overflowMode=TextOverflowModes.Overflow;
                statusLine.color=new Color(.95f,.92f,.82f,1f);
            }
            statusLine.gameObject.SetActive(paused);
            if(!paused) return;
            statusLine.transform.SetAsLastSibling();
            // Same font and RTL shaping path as the ARIA title and body copy.
            statusLine.font=titleText.font;
            statusLine.enableAutoSizing=false;
            statusLine.fontSize=_rightToLeft ? 24 : 20;
            statusLine.alignment=_rightToLeft ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            float panelWidth=((RectTransform)transform).rect.width;
            float width=Mathf.Max(80,panelWidth-2*Inset-136);
            Place(statusLine.rectTransform,_rightToLeft ? panelWidth-Inset-width : Inset,56,width,30);
            SetLocalizedText(statusLine,_rightToLeft ? "راهنمایی متوقف شده" : "Guidance paused");
        }

        private void EnsureBodyViewport()
        {
            if(bodyViewport!=null) return;
            var go=new GameObject("InstructionTextViewport",typeof(RectTransform),typeof(Image),typeof(RectMask2D),typeof(ScrollRect));
            bodyViewport=(RectTransform)go.transform; bodyViewport.SetParent(briefingLayout,false);
            go.GetComponent<Image>().color=Color.clear;
            bodyText.rectTransform.SetParent(bodyViewport,false);
            bodyScroll=go.GetComponent<ScrollRect>();bodyScroll.viewport=bodyViewport;bodyScroll.content=bodyText.rectTransform;
            bodyScroll.horizontal=false;bodyScroll.movementType=ScrollRect.MovementType.Clamped;
            // The full text area accepts swipes; this narrow rail is a non-interactive overflow cue.
            var track=new GameObject("InstructionScrollIndicator",typeof(RectTransform),typeof(Image),typeof(Scrollbar));
            track.transform.SetParent(briefingLayout,false);
            var trackImage=track.GetComponent<Image>();trackImage.color=new Color(.3f,.4f,.42f,.5f);trackImage.raycastTarget=false;
            var handle=new GameObject("Thumb",typeof(RectTransform),typeof(Image));
            handle.transform.SetParent(track.transform,false);
            var thumbRect=(RectTransform)handle.transform;
            thumbRect.anchorMin=Vector2.zero;thumbRect.anchorMax=Vector2.one;
            thumbRect.offsetMin=thumbRect.offsetMax=Vector2.zero;
            var thumb=handle.GetComponent<Image>();thumb.color=new Color(0,.8f,.95f,1);thumb.raycastTarget=false;
            bodyScrollbar=track.GetComponent<Scrollbar>();bodyScrollbar.handleRect=(RectTransform)handle.transform;
            bodyScrollbar.targetGraphic=thumb;bodyScrollbar.direction=Scrollbar.Direction.BottomToTop;
            bodyScrollbar.interactable=false;bodyScrollbar.transition=Selectable.Transition.None;
            bodyScroll.verticalScrollbar=bodyScrollbar;
        }

        private static bool HasText(TMP_Text text) => text!=null && !string.IsNullOrWhiteSpace(text is RTLTMPro.RTLTextMeshPro rtl ? rtl.OriginalText : text.text);
        private static float Measure(TMP_Text text,float width,float size)
        {
            if(!HasText(text)) return 0;
            text.enableAutoSizing=false; text.fontSize=size;
            text.textWrappingMode=TextWrappingModes.Normal;
            return Mathf.Ceil(text.GetPreferredValues(text.text,width,Mathf.Infinity).y)+4;
        }
        private static void Place(RectTransform rect,float x,float y,float width,float height)
        {
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);
        }
    }
}
