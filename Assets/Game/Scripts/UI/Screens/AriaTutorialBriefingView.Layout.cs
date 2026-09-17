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
        private float measuredWidth;
        private int measuredState=-1;
        private TMP_FontAsset measuredFont;
        private bool _layoutDirty=true;
        private MissionHudTouchLayoutView mapDock;
        private RectTransform bodyViewport;
        private ScrollRect bodyScroll;
        private Scrollbar bodyScrollbar;
        private readonly Vector3[] mapCorners=new Vector3[4];
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
            float actionGap=ContinueButton.gameObject.activeSelf?44:16;
            float actionSpacing=hasActions?actionGap+actionCount*84:12;
            int utilityCount=ActiveButtonCount(utilityActions), extractionCount=ActiveButtonCount(extractionActions);
            contentActions.gameObject.SetActive(hasActions);
            if(mapDock==null) mapDock=GetComponentInParent<MissionHudTouchLayoutView>();
            float available=900;
            if(mapDock!=null && mapDock.Minimap!=null)
            {mapDock.Minimap.GetWorldCorners(mapCorners);available=-rail.InverseTransformPoint(mapCorners[1]).y-12;}
            int state=(hasActions?64:0)|(tutorial?1:0)|(opening?2:0)|(alert?4:0)|(_missionLayoutLarge?8:0)|
                (utilityCount<<8)|(extractionCount<<12)|(actionCount<<16)|(ContinueButton.gameObject.activeSelf?32:0)|(selectionActive?128:0);
            if(!_layoutDirty && Mathf.Abs(measuredAvailable-available)<.1f && measuredState==state && measuredWidth==rail.rect.width && measuredFont==bodyText.font &&
                measuredTitle==titleText.text && measuredBody==bodyText.text && measuredAlert==alertCopy?.text && measuredOpening==openingCopy?.text) return;
            bool newInstruction=measuredTitle!=titleText.text;
            // A fitting ScrollRect can report zero even though the reader never scrolled.
            // When the dock settles and the copy starts overflowing, open at the first line.
            bool preserveScroll=bodyScroll!=null && bodyScroll.vertical && !newInstruction;
            float scrollPosition=preserveScroll ? Mathf.Clamp01(bodyScroll.verticalNormalizedPosition) : 1f;
            if(!preserveScroll && bodyScroll!=null) bodyScroll.StopMovement();
            _layoutDirty=false; measuredAvailable=available; measuredState=state; measuredWidth=rail.rect.width; measuredFont=bodyText.font;
            measuredTitle=titleText.text; measuredBody=bodyText.text; measuredAlert=alertCopy?.text; measuredOpening=openingCopy?.text;
            float width=rail.rect.width-40;
            float titleHeight=tutorial ? Measure(titleText,width,26) : 0;
            float bodyHeight=tutorial ? Measure(bodyText,width,24) : 0;
            float utilityHeight=(utilityCount+extractionCount)*84;
            float required=20+titleHeight+bodyHeight+(titleHeight>0 && bodyHeight>0?8:0)+(tutorial?actionSpacing:0)+utilityHeight;
            float portraitHeight=hasText ? Mathf.Clamp(available-required,62,114) : 230;
            missionPortraitStage.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,portraitHeight);
            missionPortraitClip.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,portraitHeight-12);
            var fitter=portraitImage.GetComponent<AspectRatioFitter>();
            if(fitter!=null) fitter.aspectMode=AspectRatioFitter.AspectMode.FitInParent;
            if(missionTelemetry!=null) missionTelemetry.SetActive(!hasText);
            float y=8+portraitHeight+12;
            if(tutorial)
            {
                float visibleBody=Mathf.Min(bodyHeight,Mathf.Max(56,available-y-titleHeight-(titleHeight>0?8:0)-actionSpacing-utilityHeight));
                EnsureBodyViewport();
                Place(titleText.rectTransform,0,0,width,titleHeight);
                Place(bodyViewport,0,titleHeight+(titleHeight>0?8:0),width,visibleBody);
                Place(bodyText.rectTransform,0,0,width,bodyHeight);
                bodyScroll.vertical=visibleBody<bodyHeight;
                Place((RectTransform)bodyScrollbar.transform,width+2,titleHeight+(titleHeight>0?8:0),12,visibleBody);
                bodyScrollbar.gameObject.SetActive(bodyScroll.vertical);
                bodyScroll.verticalNormalizedPosition=bodyScroll.vertical ? scrollPosition : 1f;
                bodyViewport.GetComponent<Image>().raycastTarget=bodyScroll.vertical;
                titleText.gameObject.SetActive(titleHeight>0); bodyText.gameObject.SetActive(bodyHeight>0);
                float textHeight=titleHeight+visibleBody+(titleHeight>0 && bodyHeight>0?8:0);
                Place(contentActions,0,textHeight+actionGap,width,Mathf.Max(0,actionCount*84-12));
                float actionY=0;
                if(selectionActive) {Place((RectTransform)selectionButton.transform,0,actionY,width,72);actionY+=84;}
                if(ContinueButton.gameObject.activeSelf) {Place((RectTransform)ContinueButton.transform,0,actionY,width,72);actionY+=84;}
                Place((RectTransform)showMeButton.transform,0,actionY,width,72);
                Place(briefingLayout,20,y,width,textHeight+(hasActions?actionSpacing-12:0));
                y+=textHeight+actionSpacing;
            }
            else
            {
                // No reserved tutorial rectangle or inactive text raycast surface.
                if(briefingLayout.gameObject.activeSelf) briefingLayout.gameObject.SetActive(false);
                if(opening) {float h=Measure(openingCopy,width,24);Place(openingLayout,20,y,width,h);Place(openingCopy.rectTransform,0,0,width,h);y+=h+12;}
                else if(alert) {float h=Measure(alertCopy,width,24);Place(alertCopy.rectTransform,20,y,width,h);y+=h+12;}
            }
            if(utilityActions!=null && utilityActions.gameObject.activeSelf)
            {PlaceUtilityRow(utilityActions,y,width);y+=utilityCount*84;}
            if(extractionActions!=null && extractionActions.gameObject.activeSelf)
            {PlaceUtilityRow(extractionActions,y,width);y+=extractionCount*84;}
            rail.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,y);
        }

        private static int ActiveButtonCount(RectTransform row)
        {
            if(row==null || !row.gameObject.activeSelf) return 0;
            int count=0;
            foreach(Transform child in row)
                if(child.gameObject.activeSelf && child.TryGetComponent<Button>(out _)) count++;
            return count;
        }

        private static void PlaceUtilityRow(RectTransform row,float y,float width)
        {
            Place(row,20,y,width,Mathf.Max(0,ActiveButtonCount(row)*84-12));
            float offset=0;
            foreach(Transform child in row)
                if(child.gameObject.activeSelf && child.TryGetComponent<Button>(out _))
                {Place((RectTransform)child,0,offset,width,72);offset+=84;}
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
