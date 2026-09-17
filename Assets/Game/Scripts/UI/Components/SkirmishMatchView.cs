using System;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>Skirmish presentation only; all match changes cross the UI gateway.</summary>
    public sealed class SkirmishMatchView : MonoBehaviour
    {
        private GameObject hud, modal;
        private static TMP_FontAsset interfaceFont;
        private TMP_Text player, enemy, clock, title, detail, statistics, confirmText;
        private GameObject resultActions, confirmationActions;
        private UiSkirmishAction pendingAction;
        private bool confirming;
        private static readonly Color Background=new(.025f,.07f,.08f,.97f);
        private static readonly Color Cyan=new(0,.73f,.85f,1);

        public static SkirmishMatchView Create()
        {
            interfaceFont=null;
            foreach(var font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
                if(font.name=="Oxanium-Bold SDF"){interfaceFont=font;break;}
            var root=new GameObject("SkirmishMatchCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32700;
            var scale=root.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scale.referenceResolution=new Vector2(1920,1080);scale.matchWidthOrHeight=1;
            return root.AddComponent<SkirmishMatchView>();
        }
        private void Awake()
        {
            var strip=Panel("BaseObjectives",transform);hud=strip.gameObject;
            strip.anchorMin=new Vector2(.30f,1);strip.anchorMax=new Vector2(.73f,1);strip.pivot=new Vector2(.5f,1);
            strip.offsetMin=new Vector2(0,-225);strip.offsetMax=new Vector2(0,-110);
            var row=strip.gameObject.AddComponent<HorizontalLayoutGroup>();row.padding=new RectOffset(8,8,8,8);row.spacing=8;row.childControlWidth=true;row.childControlHeight=true;row.childForceExpandWidth=true;
            row.childScaleWidth=false;
            player=Button(strip,"YOUR MAIN BASE",()=>Send(UiSkirmishAction.FocusPlayer));
            player.transform.parent.GetComponent<LayoutElement>().preferredWidth=280;player.fontSize=28;
            clock=Label(strip,"TIME LEFT",30);clock.gameObject.AddComponent<LayoutElement>().preferredWidth=240;
            enemy=Button(strip,"ENEMY MAIN BASE",()=>Send(UiSkirmishAction.FocusEnemy));
            enemy.transform.parent.GetComponent<LayoutElement>().preferredWidth=280;enemy.fontSize=28;
            var shade=Panel("SkirmishResult",transform,false);modal=shade.gameObject;Stretch(shade);
            shade.gameObject.AddComponent<Image>().color=new Color(0,0,0,.72f);
            var box=Panel("ResultCard",shade);box.anchorMin=box.anchorMax=new Vector2(.5f,.5f);box.sizeDelta=new Vector2(830,620);
            var layout=box.gameObject.AddComponent<VerticalLayoutGroup>();layout.padding=new RectOffset(30,30,28,28);layout.spacing=16;
            layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandHeight=false;
            title=Label(box,"BASE ASSAULT",42,70);detail=Label(box,"",30,90);statistics=Label(box,"",28,80);
            confirmText=Label(box,"",30,90);
            var result=Panel("ResultActions",box,false);resultActions=result.gameObject;Vertical(result);
            Button(result,"REPLAY",()=>Send(UiSkirmishAction.Replay));
            Button(result,"ADJUST SETUP",()=>Send(UiSkirmishAction.AdjustSetup));
            Button(result,"MAIN MENU",()=>Send(UiSkirmishAction.MainMenu));
            var confirmation=Panel("ConfirmationActions",box,false);confirmationActions=confirmation.gameObject;Vertical(confirmation);
            Button(confirmation,"CONFIRM",()=>{confirming=false;Send(pendingAction);});
            Button(confirmation,"CANCEL",()=>{confirming=false;modal.SetActive(false);UiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind.ClosePause);});
            modal.SetActive(false);
        }
        public void Confirm(UiSkirmishAction action)
        {
            pendingAction=action;confirming=true;
            UiLocalizedText.Set(confirmText,action==UiSkirmishAction.Surrender?"Surrender this match? This records a defeat.":"Restart this skirmish with the same setup?");
        }
        private void Update()
        {
            if(!UiShellRuntimeGateway.TryReadSkirmish(out var model)){hud.SetActive(false);modal.SetActive(false);return;}
            bool ready=UiShellRuntimeGateway.TryReadShellState(out var shell)&&shell.CurrentMode==UiShellMode.MatchHud&&!shell.IsTransitionRunning;
            hud.SetActive(ready&&!model.Finished&&!confirming);
            UiLocalizedText.Set(player,model.PlayerBase);UiLocalizedText.Set(enemy,model.EnemyBase);UiLocalizedText.Set(clock,model.Clock);
            clock.color=model.Clock.Contains("  0:")?new Color(1,.65f,.1f):Color.white;
            modal.SetActive(model.Finished||confirming);
            if(!modal.activeSelf)return;
            UiLocalizedText.Set(title,model.Finished?model.ResultTitle:"BASE ASSAULT");
            UiLocalizedText.Set(detail,model.Finished?model.ResultDetail:model.Objective);
            statistics.gameObject.SetActive(model.Finished);UiLocalizedText.Set(statistics,model.Statistics);
            confirmText.gameObject.SetActive(confirming&&!model.Finished);
            resultActions.SetActive(model.Finished);confirmationActions.SetActive(confirming&&!model.Finished);
        }
        private static void Send(UiSkirmishAction action)=>UiShellRuntimeGateway.TryRequestSkirmish(action);
        private static RectTransform Panel(string name,Transform parent,bool visible=true)
        {
            var go=new GameObject(name,typeof(RectTransform));var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);
            if(visible){var graphic=go.AddComponent<V3GradientGraphic>();graphic.Configure(new Color(.10f,.14f,.15f),Background,Cyan,2);}
            return rect;
        }
        private static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
        private static void Vertical(RectTransform rect)
        {var layout=rect.gameObject.AddComponent<VerticalLayoutGroup>();layout.spacing=12;layout.childControlHeight=true;layout.childControlWidth=true;layout.childForceExpandHeight=false;rect.gameObject.AddComponent<LayoutElement>().preferredHeight=210;}
        private static TMP_Text Label(Transform parent,string text,float size,float height=0)
        {
            var rect=Panel("Label",parent,false);var label=rect.gameObject.AddComponent<TextMeshProUGUI>();label.fontSize=size;label.color=Color.white;
            label.font=interfaceFont!=null?interfaceFont:TMP_Settings.defaultFontAsset;label.alignment=TextAlignmentOptions.Center;label.textWrappingMode=TextWrappingModes.Normal;label.raycastTarget=false;
            if(height>0)rect.gameObject.AddComponent<LayoutElement>().preferredHeight=height;
            UiLocalizedText.Set(label,text);return label;
        }
        private static TMP_Text Button(Transform parent,string text,Action action)
        {
            var rect=Panel(text,parent);rect.gameObject.GetComponent<V3GradientGraphic>().Configure(new Color(.03f,.32f,.42f),new Color(.015f,.12f,.17f),Cyan,2);
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight=62;
            var button=rect.gameObject.AddComponent<Button>();button.onClick.AddListener(()=>action());
            var label=Label(rect,text,30);Stretch(label.rectTransform);label.rectTransform.offsetMin=new Vector2(8,5);label.rectTransform.offsetMax=new Vector2(-8,-5);return label;
        }
    }
}
