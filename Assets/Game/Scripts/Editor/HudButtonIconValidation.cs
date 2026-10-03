using System;
using System.IO;
using Game.Configs;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class HudButtonIconValidation
    {
        public static void RunFocusedValidation()
        {
            const string path="Assets/Game/Resources/HudButtonIcons/hud-action-icons-v01.png";
            HudButtonIconImporter.Configure();
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            Require(importer!=null && importer.alphaIsTransparency && !importer.mipmapEnabled,"transparent UI import settings");
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
            GameObject root=null;
            string locale=GameLocalization.CurrentLocaleCode;
            try
            {
                Require(texture.LoadImage(File.ReadAllBytes(path)),"PNG load");
                var pixels=texture.GetPixels32();
                int transparent=0;
                foreach(var pixel in pixels)if(pixel.a==0)transparent++;
                Require(transparent>pixels.Length/3,"transparent atlas background");
                for(int index=0;index<16;index++)
                {
                    Require(HudIconButtonView.LoadIcon((HudButtonIcon)index)!=null,"icon "+index);
                    int x0=Mathf.RoundToInt(index%4*texture.width/4f),x1=Mathf.RoundToInt((index%4+1)*texture.width/4f);
                    int y0=Mathf.RoundToInt((3-index/4)*texture.height/4f),y1=Mathf.RoundToInt((4-index/4)*texture.height/4f);
                    int opaque=0;
                    for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)
                        if(pixels[y*texture.width+x].a>100)opaque++;
                    Require(opaque>1000,"nonempty atlas cell "+index);
                }
                root=new GameObject("HudIconButtonValidation",typeof(RectTransform),typeof(Canvas));
                var go=new GameObject("Action",typeof(RectTransform),typeof(Image),typeof(Button));
                go.transform.SetParent(root.transform,false);
                var button=go.GetComponent<Button>();
                var oldLayer=new GameObject("V3GradientLayer",typeof(RectTransform),typeof(V3GradientGraphic));
                oldLayer.transform.SetParent(go.transform,false);
                var textGo=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));
                textGo.transform.SetParent(go.transform,false);
                var label=textGo.GetComponent<TMP_Text>();
                label.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset");
                int clicks=0;button.onClick.AddListener(()=>clicks++);
                foreach(string language in new[]{"en","fa-IR"})
                {
                    GameLocalization.SetLocale(language,false);
                    foreach(float width in new[]{280f,356f})
                    {
                        ((RectTransform)go.transform).sizeDelta=new Vector2(width,72);
                        HudIconButtonView.Apply(button,HudButtonIcon.Guide,HudButtonRole.Information,"FIELD GUIDE","راهنمای میدان");
                        Require(label.fontSize==24 && !label.enableAutoSizing,"consistent size "+language);
                        Require(label.rectTransform.rect.width>=180,"label space");
                        var icon=go.transform.Find("HudActionIcon").GetComponent<Image>();
                        Require(icon.sprite!=null && !icon.raycastTarget,"decorative icon");
                        Require(!oldLayer.GetComponent<V3GradientGraphic>().enabled,"inherited background suppressed");
                        Require(button.targetGraphic.raycastTarget,"native hit target preserved");
                        bool rtl=UiShellRuntimeGateway.Localization.IsRightToLeft;
                        Require(icon.rectTransform.anchorMin.x==(rtl?1:0),"reading-direction icon");
                        Require(label.GetPreferredValues().x<=label.rectTransform.rect.width+1,"short label fits "+language);
                        HudIconButtonView.Apply(button,HudButtonIcon.Play,HudButtonRole.Play,"START","شروع");
                        Require(go.GetComponentsInChildren<HudIconButtonView>(true).Length==1,"idempotent style");
                        Require(go.GetComponentsInChildren<Image>(true).Length==2,"single icon");
                    }
                }
                button.onClick.Invoke();
                Require(clicks==1,"input listener preserved");
                button.interactable=false;
                HudIconButtonView.Apply(button,HudButtonIcon.Stop,HudButtonRole.Stop,"STOP ARIA","توقف آریا");
                Require(!button.interactable,"disabled control preserved");
                Debug.Log("[HudButtonIcons] result=Passed icons=16 locales=en,fa-IR labelSize=24 listeners=preserved");
            }
            finally
            {
                if(root!=null)UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(texture);
                GameLocalization.SetLocale(locale,false);
            }
        }
        private static void Require(bool passed,string detail)
        {if(!passed)throw new InvalidOperationException("[HudButtonIcons] "+detail);}
    }
}
