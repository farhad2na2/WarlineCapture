using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Game.UI.Runtime
{
    public sealed partial class SharedMissionResultView
    {
        sealed class RewardRow
        {
            public RectTransform Root;
            public TMP_Text Label, Amount;
            public Image Divider;
        }
        readonly List<RewardRow> rewardRows=new();
        readonly List<(string label,string amount)> rewardValues=new();
        int experienceRow=-1,creditsRow=-1;
        void PresentInformation(SharedMissionResultPresentation model)
        {
            rewardValues.Clear();experienceRow=creditsRow=-1;
            if(model.ShowRewardIcons)
            {
                foreach(string line in (model.InfoBody??"").Split('\n'))
                {
                    if(string.IsNullOrWhiteSpace(line))continue;
                    var match=Regex.Match(line,@"^\s*(?<label>.*?)\s+(?<amount>\+[0-9۰-۹٠-٩][0-9۰-۹٠-٩,٬.\s]*)\s*$");
                    if(!match.Success){rewardValues.Clear();break;}
                    rewardValues.Add((match.Groups["label"].Value.Trim(),match.Groups["amount"].Value.Trim()));
                }
            }
            while(rewardRows.Count<rewardValues.Count)
            {
                var root=Instantiate(ui.RewardTemplate,ui.InfoScroll.content,false);root.name="RewardRow"+rewardRows.Count;
                rewardRows.Add(new RewardRow{Root=root,Label=root.Find("Label").GetComponent<TMP_Text>(),Amount=root.Find("Amount").GetComponent<TMP_Text>(),Divider=root.Find("Divider").GetComponent<Image>()});
            }
            for(int i=0;i<rewardRows.Count;i++)
            {
                var row=rewardRows[i];row.Root.gameObject.SetActive(i<rewardValues.Count);
                if(i>=rewardValues.Count)continue;
                var value=rewardValues[i];Copy(row.Label,Game.Configs.GameLocalization.IsRightToLeft?value.label:value.label.ToUpperInvariant());Copy(row.Amount,value.amount);row.Divider.gameObject.SetActive(i<rewardValues.Count-1);
                string key=value.label.ToUpperInvariant();
                if(key.Contains("XP")||key.Contains("تجربه"))experienceRow=i;
                if(key.Contains("CREDIT")||key.Contains("اعتبار"))creditsRow=i;
            }
            ui.InfoBody.gameObject.SetActive(rewardValues.Count==0);ui.InfoBody.color=White;
            Copy(ui.InfoCaption,model.InfoCaption);ui.InfoCaption.gameObject.SetActive(rewardValues.Count==0&&!string.IsNullOrWhiteSpace(model.InfoCaption));
            ui.RewardIcons[0].gameObject.SetActive(experienceRow>=0);
            ui.RewardIcons[1].gameObject.SetActive(creditsRow>=0||model.Mode==ResultScreenMode.Campaign&&!model.Victory);
            ui.RewardIcons[0].color=Amber;ui.RewardIcons[1].color=model.Victory?Color.white:new Color(.40f,.46f,.49f,1);
        }
        void LayoutInformation(bool rtl)
        {
            if(current==null)return;
            bool structured=rewardValues.Count>0,medal=current.ShowMedal,report=current.Mode==ResultScreenMode.Operations;
            bool emptyReward=current.Mode==ResultScreenMode.Campaign&&!current.Victory;
            float inset=medal?144:report?112:24,width=520-inset-24;
            Place(ui.InfoViewport,rtl&&(medal||report)?24:inset,76,width,148);
            Place(ui.Medal.rectTransform,rtl?392:24,100,104,104);
            Place(ui.ReportIcon.rectTransform,rtl?432:24,120,64,64);
            var content=ui.InfoScroll.content;float viewportHeight=ui.InfoScroll.viewport.rect.height;
            if(structured)
            {
                float height=Mathf.Max(viewportHeight,rewardValues.Count*72);
                content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);
                for(int i=0;i<rewardValues.Count;i++)
                {
                    var row=rewardRows[i];Place(row.Root,0,i*72,width,72);
                    Place(row.Label.rectTransform,rtl?152:76,0,width-228,72);
                    Place(row.Amount.rectTransform,rtl?0:width-140,0,140,72);
                    Place(row.Divider.rectTransform,0,71,width,1);
                    row.Label.alignment=rtl?TextAlignmentOptions.MidlineRight:TextAlignmentOptions.MidlineLeft;
                    row.Amount.alignment=rtl?TextAlignmentOptions.MidlineLeft:TextAlignmentOptions.MidlineRight;CenterGlyphs(row.Label);CenterGlyphs(row.Amount);
                }
                if(experienceRow>=0)Place(ui.RewardIcons[0].rectTransform,rtl?width-60:-4,experienceRow*72+4,64,64);
                if(creditsRow>=0)Place(ui.RewardIcons[1].rectTransform,rtl?width-66:-10,creditsRow*72-2,76,76);
            }
            else
            {
                float textInset=emptyReward?88:0,textWidth=width-textInset;
                float bodyHeight=ui.InfoBody.GetPreferredValues(ui.InfoBody.text,textWidth,Mathf.Infinity).y+8;
                bool caption=ui.InfoCaption.gameObject.activeSelf;
                float captionHeight=caption?ui.InfoCaption.GetPreferredValues(ui.InfoCaption.text,textWidth,Mathf.Infinity).y+8:0;
                float total=bodyHeight+(caption?captionHeight+8:0),height=Mathf.Max(viewportHeight,total);
                content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);
                Place(ui.InfoBody.rectTransform,rtl?0:textInset,(height-total)*.5f,textWidth,bodyHeight);
                Place(ui.InfoCaption.rectTransform,rtl?0:textInset,(height-total)*.5f+bodyHeight+8,textWidth,captionHeight);
                ui.InfoBody.alignment=ui.InfoCaption.alignment=rtl?TextAlignmentOptions.MidlineRight:TextAlignmentOptions.MidlineLeft;CenterGlyphs(ui.InfoBody);CenterGlyphs(ui.InfoCaption);
                if(emptyReward)Place(ui.RewardIcons[1].rectTransform,rtl?width-66:-10,(height-76)*.5f,76,76);
            }
            ui.InfoScroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
            if(ui.InfoScroll.verticalScrollbar!=null)ui.InfoScroll.verticalScrollbar.gameObject.SetActive(content.rect.height>viewportHeight+1);
        }
    }
}
