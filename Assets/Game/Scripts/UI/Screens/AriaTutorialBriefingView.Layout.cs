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
            if(mapDock==null) mapDock=GetComponentInParent<MissionHudTouchLayoutView>();
            float available=900;
            if(mapDock!=null && mapDock.Minimap!=null)
            {mapDock.Minimap.GetWorldCorners(mapCorners);available=-rail.InverseTransformPoint(mapCorners[1]).y-12;}
            int state=(tutorial?1:0)|(opening?2:0)|(alert?4:0)|(_missionLayoutLarge?8:0)|
                (utilityActions!=null && utilityActions.gameObject.activeSelf?16:0)|(extractionActions!=null && extractionActions.gameObject.activeSelf?32:0);
            if(!_layoutDirty && Mathf.Abs(measuredAvailable-available)<.1f && measuredState==state && measuredWidth==rail.rect.width && measuredFont==bodyText.font &&
                measuredTitle==titleText.text && measuredBody==bodyText.text && measuredAlert==alertCopy?.text && measuredOpening==openingCopy?.text) return;
            _layoutDirty=false; measuredAvailable=available; measuredState=state; measuredWidth=rail.rect.width; measuredFont=bodyText.font;
            measuredTitle=titleText.text; measuredBody=bodyText.text; measuredAlert=alertCopy?.text; measuredOpening=openingCopy?.text;
            float width=rail.rect.width-40;
            float titleHeight=tutorial ? Measure(titleText,width,_missionLayoutLarge?22:20) : 0;
            float bodyHeight=tutorial ? Measure(bodyText,width,_missionLayoutLarge?19:17) : 0;
            float utilityHeight=(utilityActions!=null && utilityActions.gameObject.activeSelf ? 84 : 0)+(extractionActions!=null && extractionActions.gameObject.activeSelf ? 84 : 0);
            float required=20+titleHeight+bodyHeight+(titleHeight>0 && bodyHeight>0?8:0)+(tutorial?128:0)+utilityHeight;
            float portraitHeight=hasText ? Mathf.Clamp(available-required,62,114) : 230;
            missionPortraitStage.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,portraitHeight);
            missionPortraitClip.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,portraitHeight-12);
            var fitter=portraitImage.GetComponent<AspectRatioFitter>();
            if(fitter!=null) fitter.aspectMode=AspectRatioFitter.AspectMode.FitInParent;
            if(missionTelemetry!=null) missionTelemetry.SetActive(!hasText);
            float y=8+portraitHeight+12;
            if(tutorial)
            {
                float visibleBody=Mathf.Min(bodyHeight,Mathf.Max(56,available-y-titleHeight-(titleHeight>0?8:0)-128-utilityHeight));
                EnsureBodyViewport();
                Place(titleText.rectTransform,0,0,width,titleHeight);
                Place(bodyViewport,0,titleHeight+(titleHeight>0?8:0),width,visibleBody);
                Place(bodyText.rectTransform,0,0,width,bodyHeight);
                bodyScroll.vertical=visibleBody<bodyHeight; bodyScroll.verticalNormalizedPosition=1;
                bodyViewport.GetComponent<Image>().raycastTarget=bodyScroll.vertical;
                titleText.gameObject.SetActive(titleHeight>0); bodyText.gameObject.SetActive(bodyHeight>0);
                float textHeight=titleHeight+visibleBody+(titleHeight>0 && bodyHeight>0?8:0);
                Place(contentActions,0,textHeight+44,width,72);
                float columnWidth=(width-20)/2;
                Place((RectTransform)doItButton.transform,0,0,columnWidth,72);
                Place((RectTransform)showMeButton.transform,columnWidth+20,0,columnWidth,72);
                Place(briefingLayout,20,y,width,textHeight+116);
                y+=textHeight+128;
            }
            else
            {
                // No reserved tutorial rectangle or inactive text raycast surface.
                if(briefingLayout.gameObject.activeSelf) briefingLayout.gameObject.SetActive(false);
                if(opening) {float h=Measure(openingCopy,width,_missionLayoutLarge?19:17);Place(openingLayout,20,y,width,h);Place(openingCopy.rectTransform,0,0,width,h);y+=h+12;}
                else if(alert) {float h=Measure(alertCopy,width,_missionLayoutLarge?19:17);Place(alertCopy.rectTransform,20,y,width,h);y+=h+12;}
            }
            if(utilityActions!=null && utilityActions.gameObject.activeSelf)
            {PlaceUtilityRow(utilityActions,y,width);y+=84;}
            if(extractionActions!=null && extractionActions.gameObject.activeSelf)
            {PlaceUtilityRow(extractionActions,y,width);y+=84;}
            rail.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,y);
        }

        private static void PlaceUtilityRow(RectTransform row,float y,float width)
        {
            Place(row,20,y,width,72);
            float columnWidth=(width-20)/2;
            int column=0;
            // Team, Landing and Departure share the second column; only one is visible.
            foreach(Transform child in row)
                if(child.TryGetComponent<Button>(out _))
                    Place((RectTransform)child,column++==0?0:columnWidth+20,0,columnWidth,72);
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
