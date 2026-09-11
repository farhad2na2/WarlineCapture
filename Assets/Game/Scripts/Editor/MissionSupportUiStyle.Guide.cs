using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    internal static partial class MissionSupportUiStyle
    {
        // The same palette, three-pixel frame and category hierarchy as SCN09 Build Drawer.
        private static readonly Color GuideTop = new Color32(28,38,43,252);
        private static readonly Color GuideBottom = new Color32(3,8,10,254);
        private static readonly Color GuideRaised = new Color32(53,65,70,252);
        private static readonly Color GuideLine = new Color32(112,127,131,255);
        private static readonly Color GuideGold = new Color32(255,194,17,255);

        internal static void Guide(GameObject root)
        {
            var surface=Find(root,"Guide");
            Frame(surface,GuideTop,GuideBottom,GuideLine,3);
            Find(root,"Scrim").GetComponent<Image>().color=new Color(0,.012f,.018f,.58f);
            Box("HeaderBand",surface,10,10,1428,84,GuideTop,GuideBottom,GuideLine,3).SetAsFirstSibling();
            Icon(surface,"MissionGuideIcon",V3UiFoundationBuilder.MatchInfoIconPath,30,26,52,GuideGold);
            Place(Find(root,"Title"),102,14,790,72);
            var title=Find(root,"Title").GetComponent<TMP_Text>();title.font=Bold;title.color=Color.white;title.alignment=TextAlignmentOptions.MidlineLeft;
            Place(Find(root,"Paused"),912,18,412,64);
            Find(root,"Paused").GetComponent<TMP_Text>().alignment=TextAlignmentOptions.Center;
            Find(root,"Paused").GetComponent<TMP_Text>().color=new Color32(170,180,182,255);
            foreach(var button in root.GetComponentsInChildren<Button>(true))
            {
                GuideButton(button,false);
                var label=button.GetComponentInChildren<TMP_Text>(true).rectTransform;
                label.anchorMin=Vector2.zero;label.anchorMax=Vector2.one;
                label.offsetMin=new Vector2(12,8);label.offsetMax=new Vector2(-12,-8);
            }
            var close=Find(root,"Close");Place(close,1352,14,72,72);
            close.GetComponentInChildren<TMP_Text>(true).gameObject.SetActive(false);
            Frame(close,GuideRaised,GuideBottom,GuideLine,3).raycastTarget=true;
            foreach(float angle in new[]{45f,-45f})
            {
                var stroke=Box(angle>0?"StrokeA":"StrokeB",close,0,0,7,42,Color.white,Color.white,Color.clear,0);
                stroke.anchorMin=stroke.anchorMax=stroke.pivot=new Vector2(.5f,.5f);
                stroke.anchoredPosition=Vector2.zero;stroke.localRotation=Quaternion.Euler(0,0,angle);
            }
            var data=new SerializedObject(root.GetComponent<MissionFieldGuideView>());
            GuideTab(root,data,"Topics","topicsSelected",110,V3UiFoundationBuilder.MatchInfoIconPath);
            GuideTab(root,data,"Classes","classesSelected",284,V3UiFoundationBuilder.CommanderRosterIconPath);
            GuideTab(root,data,"RadioArchive","radioSelected",458,V3UiFoundationBuilder.MissionRadioIconPath);
            Place(Find(root,"Filter"),236,110,558,84);
            Place(Find(root,"Search"),804,110,594,84);
            Frame(Find(root,"Search"),GuideBottom,GuideTop,GuideLine,3);
            foreach(string name in new[]{"Input","Placeholder"})Place(Find(root,name),18,10,558,64);
            var viewport=Find(root,"ReadingViewport");Place(viewport,236,110,1162,686);
            Frame(viewport,GuideBottom,GuideBottom,GuideLine,3).raycastTarget=true;
            var track=Box("ReadingScrollbar",surface,1406,110,32,686,GuideTop,GuideBottom,GuideLine,0);
            var handle=Box("Handle",track,0,0,32,200,GuideRaised,GuideLine,GuideLine,0);
            handle.anchorMin=Vector2.zero;handle.anchorMax=Vector2.one;handle.offsetMin=handle.offsetMax=Vector2.zero;
            var scrollbar=track.gameObject.AddComponent<Scrollbar>();scrollbar.handleRect=handle;
            scrollbar.targetGraphic=handle.GetComponent<V3GradientGraphic>();scrollbar.targetGraphic.raycastTarget=true;
            track.GetComponent<V3GradientGraphic>().raycastTarget=true;
            scrollbar.direction=Scrollbar.Direction.BottomToTop;
            viewport.GetComponent<ScrollRect>().verticalScrollbar=scrollbar;
            viewport.GetComponent<ScrollRect>().verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
            Find(root,"PageTitle").GetComponent<TMP_Text>().font=Bold;
            Find(root,"PageTitle").GetComponent<TMP_Text>().color=GuideGold;
            Find(root,"Example").GetComponent<TMP_Text>().color=Color.white;
            Find(root,"Mistake").GetComponent<TMP_Text>().color=GuideGold;
            Find(root,"ScrollHint").gameObject.SetActive(false);
            Place(Find(root,"Previous"),10,806,310,93);
            Place(Find(root,"Next"),966,806,472,93);GuideButton(Find(root,"Next").GetComponent<Button>(),true);
            Place(Find(root,"PageNumber"),330,806,626,93);
            Box("PageBadge",surface,330,806,626,93,GuideTop,GuideBottom,GuideLine,3).SetAsFirstSibling();
            foreach(string name in new[]{"Example","Mistake","Diagram"})
            {
                var text=Find(root,name);
                var card=Box(name+"Card",text.parent,0,0,100,100,GuideTop,GuideBottom,GuideLine,3);
                card.SetAsFirstSibling();
                data.FindProperty(char.ToLowerInvariant(name[0])+name.Substring(1)+"Card").objectReferenceValue=card;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void GuideTab(GameObject root,SerializedObject data,string name,string binding,float y,string icon)
        {
            var tab=Find(root,name);Place(tab,10,y,216,164);
            tab.gameObject.AddComponent<CanvasGroup>();
            var selected=Box("Selected",tab,0,0,216,164,new Color32(98,72,12,255),new Color32(28,19,3,255),GuideGold,3);
            selected.SetAsFirstSibling();data.FindProperty(binding).objectReferenceValue=selected.gameObject;
            Icon(tab,"Icon",icon,80,22,56,GuideGold);
            var label=tab.GetComponentInChildren<TMP_Text>(true);Place(label.transform,12,88,192,64);
            label.textWrappingMode=TextWrappingModes.Normal;label.fontSizeMax=25;
        }

        private static void GuideButton(Button button,bool primary)
        {
            var graphic=Frame(button.transform,primary?new Color32(62,161,57,255):GuideTop,
                primary?new Color32(12,58,15,255):GuideBottom,primary?new Color32(79,199,73,255):GuideLine,3);
            graphic.raycastTarget=true;button.targetGraphic=graphic;button.transition=Selectable.Transition.ColorTint;
            var label=button.GetComponentInChildren<TMP_Text>(true);label.font=Bold;label.color=Color.white;
            label.textWrappingMode=TextWrappingModes.Normal;label.alignment=TextAlignmentOptions.Center;
        }
    }
}
