using System.Linq;
using Game.UI.Runtime;
using TMPro;
using RTLTMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>Shared target-lock chrome for the mission extensions, using the existing HUD graphics and icons.</summary>
    internal static class MissionSupportUiStyle
    {
        internal static readonly Color Cyan = new Color32(0,198,235,255);
        private static readonly Color Line = new Color32(91,119,128,255);
        private static readonly Color Ink = new Color32(3,12,16,255);
        private static readonly Color Raised = new Color32(30,48,56,255);
        private static TMP_FontAsset Bold => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset");
        private static TMP_FontAsset Medium => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset");

        public static void Build()
        {
            M03RadarWarningUiBuilder.BuildGuide();
            M03RadarWarningUiBuilder.BuildHud();
            M04AirliftPresentationBuilder.BuildHud();
            M04AirliftPresentationBuilder.ImportCopy();
            MatchHudResourceTextRepair.Build();
            AssetDatabase.SaveAssets();
            Debug.Log("[MissionSupportUiStyle] result=Passed sharedGuide=true missionHud=M3,M4");
        }

        internal static void Guide(GameObject root)
        {
            var surface = Find(root,"Guide");
            Frame(surface, Raised, Ink, Cyan, 2);
            Box("HeaderBand",surface,2,2,1376,118,new Color32(15,35,45,255),Ink,Line,1).SetAsFirstSibling();
            Icon(surface,"MissionGuideIcon",V3UiFoundationBuilder.MatchInfoIconPath,30,29,48,Cyan);
            Place(Find(root,"Title"),96,8,1000,70); Find(root,"Title").GetComponent<TMP_Text>().font=Bold;
            Find(root,"Title").GetComponent<TMP_Text>().color=Cyan;
            Place(Find(root,"Paused"),96,78,980,40);
            Find(root,"Paused").GetComponent<TMP_Text>().color=new Color32(175,195,201,255);
            foreach(var button in root.GetComponentsInChildren<Button>(true)) StyleButton(button,button.name=="Next"||button.name=="Close");
            Frame(Find(root,"Search"),Ink,Raised,Line,1);
            var viewport=Find(root,"ReadingViewport");
            Box("ReadingFrame",surface,28,203,1322,536,Ink,Ink,Line,1).SetSiblingIndex(viewport.GetSiblingIndex());
            var track=Box("ReadingScrollbar",surface,1356,205,8,532,Ink,Ink,Line,1);
            var handle=Box("Handle",track,1,1,6,530,Cyan,Cyan,Cyan,0);
            handle.anchorMin=Vector2.zero;handle.anchorMax=Vector2.one;handle.offsetMin=Vector2.one;handle.offsetMax=-Vector2.one;
            var scrollbar=track.gameObject.AddComponent<Scrollbar>();scrollbar.handleRect=handle;scrollbar.targetGraphic=handle.GetComponent<V3GradientGraphic>();scrollbar.direction=Scrollbar.Direction.BottomToTop;
            viewport.GetComponent<ScrollRect>().verticalScrollbar=scrollbar;viewport.GetComponent<ScrollRect>().verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
            Find(root,"PageTitle").GetComponent<TMP_Text>().font=Bold;
            Find(root,"PageTitle").GetComponent<TMP_Text>().color=Cyan;
            Find(root,"ScrollHint").gameObject.SetActive(false);
            Place(Find(root,"PageNumber"),530,751,320,52);
            Box("PageBadge",surface,530,751,320,52,Raised,Ink,Line,1).SetAsFirstSibling();
            var data=new SerializedObject(root.GetComponent<MissionFieldGuideView>());
            foreach(string name in new[]{"Example","Mistake","Diagram"})
            {
                var text=Find(root,name);
                var card=Box(name+"Card",text.parent,0,0,100,100,Raised,Ink,name=="Mistake"?new Color32(169,121,49,255):Line,1);
                card.SetAsFirstSibling();
                data.FindProperty(char.ToLowerInvariant(name[0])+name.Substring(1)+"Card").objectReferenceValue=card;
            }
            var topics=Find(root,"Topics");var classes=Find(root,"Classes");
            data.FindProperty("topicsSelected").objectReferenceValue=Box("Selected",topics,4,57,220,3,Cyan,Cyan,Cyan,0).gameObject;
            data.FindProperty("classesSelected").objectReferenceValue=Box("Selected",classes,4,57,220,3,Cyan,Cyan,Cyan,0).gameObject;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void Defense(GameObject root)
        {
            var actions=Find(root,"M03Actions");
            var warning=(RectTransform)Find(root,"ThreatJumpPanel");
            float actionsTop=-warning.anchoredPosition.y+warning.sizeDelta.y+10;
            Place(actions,414,actionsTop,774,108); Frame(actions,Raised,Ink,Line,1);
            FollowHeaderLayout((RectTransform)actions);
            Place(Find(root,"FieldGuide"),10,10,240,44);
            Place(Find(root,"ReadWarning"),264,10,240,44);
            Place(Find(root,"SkipLesson"),518,10,246,44);
            Place(Find(root,"RadarStatus"),18,62,492,36);
            Place(Find(root,"ReturnWarningCamera"),518,60,246,38);
            foreach(var button in actions.GetComponentsInChildren<Button>(true))StyleButton(button,true);
            ButtonIcon(Find(root,"FieldGuide"),V3UiFoundationBuilder.MatchInfoIconPath);
            ButtonIcon(Find(root,"ReadWarning"),V3UiFoundationBuilder.MatchInvalidIconPath);
            ButtonIcon(Find(root,"ReturnWarningCamera"),V3UiFoundationBuilder.MatchReturnIconPath);
            StyleButton(Find(root,"SkipCameraTour").GetComponent<Button>(),false);
        }

        internal static void Extraction(GameObject root)
        {
            var actions=Find(root,"M04Actions");Place(actions,414,164,774,150);Frame(actions,Raised,Ink,Line,1);
            FollowHeaderLayout((RectTransform)actions);
            var data=new SerializedObject(actions.parent.GetComponent<MissionExtractionHudView>());
            string[] names={"Aboard","Carrier","Secure","Remaining"};
            string[] keys={"aboard","carrier","secure","remaining"};
            for(int i=0;i<4;i++)
            {
                float x=10+i*191;
                Box(names[i]+"Metric",actions,x,8,181,62,Ink,Ink,i==3?Cyan:Line,1);
                var caption=Text(names[i]+"Caption",actions,x+6,11,169,24,14,"mission.m04.hud."+keys[i]);caption.color=new Color32(170,193,202,255);
                var value=Text(names[i]+"Value",actions,x+6,34,169,32,25,"");value.font=Bold;value.color=i==3?new Color32(255,196,67,255):Color.white;
                data.FindProperty(keys[i]).objectReferenceValue=value.GetComponent<V3LocalizedTextBindingView>();
            }
            string[] buttons={"Guide","Team","Landing","Departure"};
            string[] icons={V3UiFoundationBuilder.MatchInfoIconPath,V3UiFoundationBuilder.MatchPlayerIconPath,V3UiFoundationBuilder.MatchJumpIconPath,V3UiFoundationBuilder.MatchAirTransportIconPath};
            for(int i=0;i<4;i++)
            {
                var button=actions.Find(buttons[i]);Place(button,10+i*191,78,181,44);StyleButton(button.GetComponent<Button>(),true);ButtonIcon(button,icons[i]);
            }
            var status=Find(root,"ExtractionStatus");Place(status,18,123,738,24);status.GetComponent<TMP_Text>().fontSizeMax=15;status.GetComponent<TMP_Text>().color=Cyan;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void FollowHeaderLayout(RectTransform actions)
        {
            var layout=actions.parent.GetComponent<MainMenuV3SectionLayoutView>();
            var data=new SerializedObject(layout);
            var targets=data.FindProperty("centerAnchoredTargets");
            var positions=data.FindProperty("centerTargetBasePositions");
            // Canonical builders replace mission panels. Drop their old null references
            // while preserving every existing header target and its authored position.
            for(int i=targets.arraySize-1;i>=0;i--)
            {
                if(targets.GetArrayElementAtIndex(i).objectReferenceValue!=null)continue;
                targets.DeleteArrayElementAtIndex(i);positions.DeleteArrayElementAtIndex(i);
            }
            int index=targets.arraySize;
            for(int i=0;i<targets.arraySize;i++)
                if(targets.GetArrayElementAtIndex(i).objectReferenceValue==actions){index=i;break;}
            if(index==targets.arraySize){targets.arraySize++;positions.arraySize++;}
            targets.GetArrayElementAtIndex(index).objectReferenceValue=actions;
            positions.GetArrayElementAtIndex(index).vector2Value=actions.anchoredPosition;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void StyleButton(Button button,bool blue)
        {
            var graphic=Frame(button.transform,blue?new Color32(22,94,126,255):Raised,blue?new Color32(2,29,42,255):Ink,blue?Cyan:Line,2);
            graphic.raycastTarget=true;button.targetGraphic=graphic;button.transition=Selectable.Transition.ColorTint;
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.18f,1.18f,1.18f);colors.pressedColor=new Color(.65f,.85f,.95f);colors.disabledColor=new Color(.45f,.5f,.52f,.75f);button.colors=colors;
            var label=button.GetComponentInChildren<TMP_Text>(true);if(label!=null){label.font=Bold;label.textWrappingMode=TextWrappingModes.NoWrap;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;}
        }
        private static void ButtonIcon(Transform button,string path)
        {
            Icon(button,"MissionIcon",path,4,2,40,Color.white);
            var label=button.GetComponentInChildren<TMP_Text>(true);var r=label.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(48,2);r.offsetMax=new Vector2(-6,-2);
        }
        private static void Icon(Transform parent,string name,string path,float x,float y,float size,Color color)
        {var r=Rect(name,parent,x,y,size,size);var image=r.gameObject.AddComponent<Image>();image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);image.color=color;image.preserveAspect=true;image.raycastTarget=false;}
        private static V3GradientGraphic Frame(Transform rect,Color top,Color bottom,Color border,float width)
        {
            var old=rect.GetComponent<Image>();if(old!=null)Object.DestroyImmediate(old);
            var graphic=rect.GetComponent<V3GradientGraphic>()??rect.gameObject.AddComponent<V3GradientGraphic>();graphic.Configure(top,bottom,border,width);graphic.color=Color.white;graphic.canvasRenderer.SetAlpha(1);graphic.raycastTarget=false;return graphic;
        }
        private static RectTransform Box(string name,Transform parent,float x,float y,float w,float h,Color top,Color bottom,Color line,float stroke)
        {var rect=Rect(name,parent,x,y,w,h);Frame(rect,top,bottom,line,stroke);return rect;}
        private static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);Place(rect,x,y,w,h);return rect;}
        private static TMP_Text Text(string name,Transform parent,float x,float y,float w,float h,float size,string key)
        {
            var rect=Rect(name,parent,x,y,w,h);var text=rect.gameObject.AddComponent<RTLTextMeshPro>();text.font=Medium;text.fontSize=size;text.enableAutoSizing=true;text.fontSizeMin=12;text.fontSizeMax=size;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;text.text=key.Length==0?"":Game.Configs.M04AirliftCopyCatalog.Ui.FirstOrDefault(e=>e.Key==key).English??key;
            text.gameObject.AddComponent<V3LocalizedTextBindingView>().Configure(key,text.text,false);return text;
        }
        private static Transform Find(GameObject root,string name)=>root.GetComponentsInChildren<Transform>(true).First(t=>t.name==name);
        private static void Place(Transform t,float x,float y,float w,float h)
        {var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
    }
}
