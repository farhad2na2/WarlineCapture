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
    internal static partial class MissionSupportUiStyle
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

        internal static void Defense(GameObject root)
        {
            var actions=Find(root,"M03Actions");
            var aria=Find(root,"AriaAssistantButton");
            actions.SetParent(aria,false);
            Place(actions,20,392,360,72);
            Place(Find(root,"FieldGuide"),0,0,172,72);
            Place(Find(root,"SkipLesson"),182,0,178,72);
            StyleButton(Find(root,"FieldGuide").GetComponent<Button>(),true);
            StyleButton(Find(root,"SkipLesson").GetComponent<Button>(),false);
            var warning=(RectTransform)Find(root,"ReadWarning");
            var title=Find(Find(root,"ThreatJumpPanel").gameObject,"Title");
            warning.SetParent(Find(root,"ThreatJumpPanel"),false);
            Place(warning,0,0,440,79);
            Object.DestroyImmediate(warning.GetChild(0).gameObject);
            warning.GetComponent<Image>().color=Color.clear;
            warning.GetComponent<Button>().transition=Selectable.Transition.None;
            var restore=Find(root,"ReturnWarningCamera");restore.SetParent(Find(root,"HeaderContent"),false);
            Place(restore,414,98,225,72);StyleButton(restore.GetComponent<Button>(),true);
            ButtonIcon(restore,V3UiFoundationBuilder.MatchReturnIconPath);FollowHeaderLayout((RectTransform)restore);
            var status=Find(root,"RadarStatus");status.SetParent(Find(root,"SupportCommand"),false);
            Place(status,95,6,48,26);
            var text=status.GetComponent<TMP_Text>();text.font=Bold;text.fontSizeMax=19;text.fontSizeMin=14;
            text.alignment=TextAlignmentOptions.Center;text.color=new Color32(255,196,67,255);
            Place(Find(root,"SkipCameraTour"),765,94,360,72);
            StyleButton(Find(root,"SkipCameraTour").GetComponent<Button>(),false);
            foreach(var button in actions.GetComponentsInChildren<Button>(true))
            {
                var label=button.GetComponentInChildren<TMP_Text>(true);label.fontSizeMax=21;label.fontSizeMin=18;
                label.textWrappingMode=TextWrappingModes.Normal;
                var rect=label.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;
                rect.offsetMin=new Vector2(8,6);rect.offsetMax=new Vector2(-8,-6);
            }
            MobileSelection(root);
        }

        internal static void Extraction(GameObject root)
        {
            var actions=Find(root,"M04Actions");Place(actions,414,164,774,182);Frame(actions,Raised,Ink,Line,1);
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
                var button=actions.Find(buttons[i]);Place(button,10+i*191,78,181,72);StyleButton(button.GetComponent<Button>(),true);ButtonIcon(button,icons[i]);
            }
            var status=Find(root,"ExtractionStatus");Place(status,18,154,738,24);status.GetComponent<TMP_Text>().fontSizeMax=15;status.GetComponent<TMP_Text>().color=Cyan;
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
            Icon(button,"MissionIcon",path,4,(((RectTransform)button).sizeDelta.y-40)/2,40,Color.white);
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
