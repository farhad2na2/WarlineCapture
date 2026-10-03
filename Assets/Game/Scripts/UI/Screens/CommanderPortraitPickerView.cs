using System;
using Game.Configs;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed class CommanderPortraitPickerView : MonoBehaviour
    {
        [SerializeField] private Button trigger;
        [SerializeField] private Sprite[] portraits;
        private GameObject modal;
        public void Configure(Button button, Sprite[] sprites) { trigger=button;portraits=sprites; }
        private void OnEnable() { if(trigger!=null)trigger.onClick.AddListener(Open); }
        private void OnDisable() { if(trigger!=null)trigger.onClick.RemoveListener(Open);Close(); }
        private void Close() { if(modal!=null)Destroy(modal);modal=null; }
        private void Open()
        {
            if(modal!=null)return;
            Canvas canvas=GetComponentInParent<Canvas>()?.rootCanvas;if(canvas==null)return;
            modal=new GameObject("CommanderPortraitChoice",typeof(RectTransform),typeof(Image));
            var rect=modal.GetComponent<RectTransform>();rect.SetParent(canvas.transform,false);
            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            modal.GetComponent<Image>().color=new Color(0,0,0,.92f);
            var panel=new GameObject("PortraitChoices",typeof(RectTransform)).GetComponent<RectTransform>();panel.SetParent(rect,false);
            panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,.5f);panel.sizeDelta=new Vector2(1020,820);
            var c=(RectTransform)canvas.transform;panel.localScale=Vector3.one*Mathf.Min((c.rect.width-48)/1020,(c.rect.height-48)/820);
            bool fa=GameLocalization.CurrentLocaleCode==GameLocalization.PersianLocaleCode;
            Label(panel,fa?"انتخاب فرمانده":"CHOOSE COMMANDER",0,0,1020,72);
            for(int i=0;i<portraits.Length;i++)
            {
                int id=i;var button=Button(panel,"Portrait"+i,(i%3)*344,84+(i/3)*300,332,288);
                var image=new GameObject("Portrait",typeof(RectTransform),typeof(Image)).GetComponent<Image>();image.transform.SetParent(button.transform,false);
                var r=image.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=Vector2.one*4;r.offsetMax=-Vector2.one*4;
                image.sprite=portraits[i];image.preserveAspect=true;image.raycastTarget=false;
                button.onClick.AddListener(()=> { if(UiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind.SelectCommanderPortrait,id))Close(); });
            }
            var cancel=Button(panel,"Cancel",0,704,1020,104);Label(cancel.transform,fa?"بازگشت":"CANCEL",0,0,1020,104);cancel.onClick.AddListener(Close);
        }
        private static Button Button(Transform parent,string name,float x,float y,float w,float h)
        {
            var r=new GameObject(name,typeof(RectTransform),typeof(V3GradientGraphic),typeof(Button)).GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);
            var g=r.GetComponent<V3GradientGraphic>();g.Configure(new Color(.08f,.15f,.18f),new Color(.02f,.04f,.06f),new Color(.3f,.5f,.55f),2);
            var b=r.GetComponent<Button>();b.targetGraphic=g;return b;
        }
        private static void Label(Transform parent,string value,float x,float y,float w,float h)
        {
            var r=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);
            var t=r.GetComponent<TMP_Text>();t.fontSize=38;t.color=Color.white;t.raycastTarget=false;t.alignment=TextAlignmentOptions.Center;UiLocalizedText.Set(t,value);
        }
    }
}
