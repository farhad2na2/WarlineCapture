using System.Linq;
using TMPro;
using UnityEngine;

namespace Game.UI.Runtime
{
    // Measure localized copy after presenters update it, so long briefings stay readable.
    [DefaultExecutionOrder(500)]
    public sealed class MenuBriefingFlowView : MonoBehaviour
    {
        [SerializeField] private RectTransform[] rows;
        [SerializeField] private float[] minimumHeights;
        [SerializeField] private bool stretchTextRows;
        public void Configure(RectTransform[] ordered, bool stretchCopy = false)
        {
            stretchTextRows=stretchCopy;
            rows=ordered.Where(r=>r!=null).ToArray();
            minimumHeights=rows.Select(r=>r.sizeDelta.y).ToArray();
        }
        private void LateUpdate()
        {
            if(rows==null)return;
            float y=8;
            for(int i=0;i<rows.Length;i++)
            {
                var row=rows[i]; if(row==null||!row.gameObject.activeSelf)continue;
                var text=row.GetComponent<TMP_Text>()??row.Find("Label")?.GetComponent<TMP_Text>()??row.Find("Copy")?.GetComponent<TMP_Text>();
                if(stretchTextRows && row.GetComponent<TMP_Text>()!=null)row.sizeDelta=new Vector2(Mathf.Max(40,((RectTransform)transform).rect.width-48),row.sizeDelta.y);
                float height=minimumHeights[i];
                if(text!=null)
                {
                    text.textWrappingMode=TextWrappingModes.Normal;
                    text.overflowMode=TextOverflowModes.Overflow;
                    bool nested=text.transform!=row;
                    height=Mathf.Max(height,text.GetPreferredValues(text.text,text.rectTransform.rect.width,Mathf.Infinity).y+(nested?20:8));
                    if(nested)text.rectTransform.sizeDelta=new Vector2(text.rectTransform.sizeDelta.x,height-20);
                }
                row.anchoredPosition=new Vector2(row.anchoredPosition.x,-y);
                row.sizeDelta=new Vector2(row.sizeDelta.x,height);
                y+=height+12;
            }
            var content=(RectTransform)transform;content.sizeDelta=new Vector2(content.sizeDelta.x,y);
        }
    }
}
