using Game.Configs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Game.UI.Runtime
{
    public sealed partial class SharedMissionResultView
    {
        public void RefreshLayout()
        {
            if(ui==null||transform.parent is not RectTransform parent)return;
            var rect=(RectTransform)transform;Rect available=parent.rect;
            var canvas=GetComponentInParent<Canvas>();
            if(Application.isPlaying&&canvas!=null&&canvas.rootCanvas.renderMode!=RenderMode.WorldSpace)
            {
                Camera camera=canvas.rootCanvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.rootCanvas.worldCamera;
                if(RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,Screen.safeArea.min,camera,out var min)&&
                   RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,Screen.safeArea.max,camera,out var max))
                {
                    float x0=Mathf.Max(available.xMin,min.x),y0=Mathf.Max(available.yMin,min.y),x1=Mathf.Min(available.xMax,max.x),y1=Mathf.Min(available.yMax,max.y);
                    if(x1>x0&&y1>y0)available=Rect.MinMaxRect(x0,y0,x1,y1);
                }
            }
            if(available.width<10||available.height<10)return;
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
            rect.sizeDelta=new Vector2(1920,1080);rect.anchoredPosition=available.center-parent.rect.center;
            rect.localScale=Vector3.one*Mathf.Min(available.width/1920,available.height/1080);
            // Cover the entire parent while keeping the result content inside the safe area.
            float scale=rect.localScale.x;
            float backgroundW=Mathf.Max(1920,parent.rect.width/scale),backgroundH=Mathf.Max(1080,parent.rect.height/scale);
            float backgroundX=-(backgroundW-1920)*.5f-rect.anchoredPosition.x/scale;
            float backgroundY=-(backgroundH-1080)*.5f+rect.anchoredPosition.y/scale;
            Place(ui.Backdrop.rectTransform,backgroundX,backgroundY,backgroundW,backgroundH);
            Place(ui.BackdropShade.rectTransform,backgroundX,backgroundY,backgroundW,backgroundH);
            bool rtl=GameLocalization.IsRightToLeft;
            foreach(var text in new[]{ui.ObjectivesHeading,ui.PerformanceHeading,ui.DurationHeading,ui.Mode,ui.Mission,ui.Context,ui.Reason,ui.SummaryTitle,ui.InfoHeading,ui.SaveStatus})
                if(text!=null)text.alignment=rtl?TextAlignmentOptions.MidlineRight:TextAlignmentOptions.MidlineLeft;
            ui.DurationHeading.alignment=TextAlignmentOptions.Center;
            ui.Summary.alignment=rtl?TextAlignmentOptions.TopRight:TextAlignmentOptions.TopLeft;
            ui.Outcome.alignment=rtl?TextAlignmentOptions.MidlineRight:TextAlignmentOptions.MidlineLeft;
            for(int i=0;i<3;i++)
            {
                ui.ObjectiveLabels[i].alignment=rtl?TextAlignmentOptions.MidlineRight:TextAlignmentOptions.MidlineLeft;
                ui.ObjectiveValues[i].alignment=TextAlignmentOptions.Center;
                ui.PerformanceLabels[i].alignment=rtl?TextAlignmentOptions.MidlineRight:TextAlignmentOptions.MidlineLeft;
                ui.PerformanceValues[i].alignment=TextAlignmentOptions.Center;
                ui.PerformanceValues[i].color=current?.Victory==false&&(i==0&&current.Mode!=ResultScreenMode.Operations||i==1&&current.Mode==ResultScreenMode.Operations)?Red:Cyan;
                if(current?.Mode==ResultScreenMode.Operations&&i==0&&current.Objectives.Length>0&&current.Objectives[0].State!=ResultObjectiveState.Complete)ui.PerformanceValues[i].color=Amber;
            }
            Place(ui.SummaryTitle.rectTransform,24,300,600,72);
            Place(ui.SummaryDivider.rectTransform,24,380,600,1);
            Place(ui.SummaryViewport,24,390,600,140);
            Place(ui.Reason.rectTransform,592,16,920,92);CenterGlyphs(ui.Reason);
            Place(ui.SaveIcon.rectTransform,rtl?1800:24,16,32,32);
            Place(ui.SaveStatus.rectTransform,rtl?24:76,8,1756,48);CenterGlyphs(ui.SaveStatus);
            LayoutActions(rtl);
            FitScroll(ui.SummaryScroll,ui.Summary);
            LayoutInformation(rtl);
            if(ui.Artwork.texture!=null)
            {
                float target=ui.ArtworkClip.rect.width/ui.ArtworkClip.rect.height;
                float source=ui.Artwork.texture.width/(float)ui.Artwork.texture.height;
                ui.Artwork.uvRect=source>target?new Rect((1-target/source)*.5f,0,target/source,1):new Rect(0,(1-source/target)*.5f,1,source/target);
            }
        }
        void LayoutActions(bool rtl)
        {
            bool second=ui.Secondary.gameObject.activeSelf,third=ui.Tertiary.gameObject.activeSelf;
            float primaryX=32,primaryW=1856;
            if(second&&third)
            {
                bool menuFirst=current?.Mode==ResultScreenMode.Skirmish&&current.Victory;
                Place((RectTransform)(menuFirst?ui.Tertiary:ui.Secondary).transform,32,952,544,96);Place((RectTransform)(menuFirst?ui.Secondary:ui.Tertiary).transform,592,952,512,96);primaryX=1120;primaryW=768;
            }
            else if(second||third)
            {Place((RectTransform)(second?ui.Secondary:ui.Tertiary).transform,32,952,688,96);primaryX=736;primaryW=1152;}
            Place((RectTransform)ui.Primary.transform,primaryX,952,primaryW,96);
            Place((RectTransform)ui.Retry.transform,primaryX,952,primaryW,96);
            foreach(var pair in new[]{(ui.Primary,ui.PrimaryLabel,ui.PrimaryIcon),(ui.Retry,ui.RetryLabel,ui.RetryIcon),(ui.Secondary,ui.SecondaryLabel,ui.SecondaryIcon),(ui.Tertiary,ui.TertiaryLabel,ui.TertiaryIcon)})
            {
                var button=(RectTransform)pair.Item1.transform;var label=pair.Item2.rectTransform;
                float textWidth=Mathf.Min(button.rect.width-144,pair.Item2.GetPreferredValues(pair.Item2.text).x+24);
                float groupWidth=textWidth+80,start=(button.rect.width-groupWidth)*.5f;
                bool trailing=pair.Item1==ui.Primary&&current?.Victory==true&&current.PrimaryTone==ResultActionTone.Positive;
                bool iconRight=rtl?!trailing:trailing;
                Place(pair.Item3.rectTransform,iconRight?start+textWidth+24:start,20,56,56);
                Place(label,iconRight?start:start+80,8,textWidth,80);pair.Item2.alignment=TextAlignmentOptions.Center;CenterGlyphs(pair.Item2);
            }
        }
        static void CenterGlyphs(TMP_Text text)
        {
            if(text==null||!text.gameObject.activeInHierarchy||string.IsNullOrWhiteSpace(text.text))return;
            text.ForceMeshUpdate();
            // Center the visible glyph bounds, including Persian descenders, inside the authored cell.
            float offset=text.rectTransform.rect.center.y-text.textBounds.center.y;
            text.rectTransform.anchoredPosition+=new Vector2(0,offset);
        }
        static void FitScroll(ScrollRect scroll,TMP_Text text)
        {
            if(scroll==null||text==null||scroll.viewport.rect.width<=0)return;
            scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
            float width=scroll.viewport.rect.width-16;
            var content=scroll.content;var label=text.rectTransform;
            label.anchorMin=label.anchorMax=label.pivot=new Vector2(0,1);label.anchoredPosition=Vector2.zero;
            float layoutHeight=Mathf.Max(scroll.viewport.rect.height,text.GetPreferredValues(text.text,width,Mathf.Infinity).y+16);
            label.sizeDelta=new Vector2(width,layoutHeight);text.ForceMeshUpdate();
            // Scroll for visible paragraph geometry, not unused font leading below the last line.
            float height=Mathf.Max(scroll.viewport.rect.height,-text.textBounds.min.y+12);
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);
            if(scroll.verticalScrollbar!=null)scroll.verticalScrollbar.gameObject.SetActive(height>scroll.viewport.rect.height+1);
        }
        static void Place(RectTransform r,float x,float y,float w,float h)
        {r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
    }
}
