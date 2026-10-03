using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Configs;
using System.Linq;

namespace Game.UI.Runtime
{
    // Runtime presenters may update copy or their old font ranges. Keep menu typography stable.
    public sealed class MenuTypographyView : MonoBehaviour
    {
        [SerializeField] private TMP_Text[] labels;
        [SerializeField] private float[] sizes;
        public void Configure(TMP_Text[] texts)
        {
            labels=texts;sizes=new float[texts.Length];
            for(int i=0;i<texts.Length;i++)sizes[i]=texts[i].fontSize;
        }
        private bool? rightToLeft;
        private void LateUpdate()
        {
            bool rtl=GameLocalization.CurrentLocaleCode==GameLocalization.PersianLocaleCode;
            if(rightToLeft!=rtl)
            {
                rightToLeft=rtl;
                foreach(var layout in GetComponentsInChildren<HorizontalLayoutGroup>(true))layout.reverseArrangement=rtl;
                foreach(var button in GetComponentsInChildren<Button>(true).Where(b=>b.name=="BackButton"||b.name=="MissionBackButton"))
                {
                    foreach(var image in button.GetComponentsInChildren<Image>(true).Where(i=>i.sprite!=null))
                    {
                        var icon=image.rectTransform;
                        Vector2 center=new Vector2(.5f,.5f);
                        icon.anchoredPosition+=Vector2.Scale(icon.sizeDelta,center-icon.pivot);
                        icon.pivot=center;icon.localScale=new Vector3(rtl?-1:1,1,1);
                    }
                }
            }
            if(labels==null)return;
            for(int i=0;i<labels.Length;i++)
            {
                var t=labels[i];if(t==null)continue;
                t.enableAutoSizing=false;t.fontSize=sizes[i];
            }
        }
    }
}
