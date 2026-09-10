using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class MissionResultPopupView
    {
        [System.Serializable] private struct DefenseLabel {public TMP_Text Text; public string Key;}
        [SerializeField] private DefenseLabel[] defenseLabels;
        [SerializeField] private Button defenseGuideButton;
        [SerializeField] private RectTransform defenseRewardsPanel;
        [SerializeField] private RawImage outcomeBackdrop;
        [SerializeField] private Texture m03ResultBackdrop;
        private bool reviewNarrated;
        private bool defenseLayoutActive;
        private MainMenuV3SectionLayoutView resultSectionLayout;
        private void BindDefenseLayout()
        {
            resultSectionLayout=GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
            if(resultSectionLayout!=null)resultSectionLayout.LayoutApplied+=LayoutDefenseResult;
        }
        private void UnbindDefenseLayout()
        {if(resultSectionLayout!=null)resultSectionLayout.LayoutApplied-=LayoutDefenseResult;}
        private void ApplyDefenseOutcome(in UiMissionResultPopupModel model)
        {
            defenseLayoutActive=model.Defense.Applicable;
            if(defenseGuideButton!=null) defenseGuideButton.gameObject.SetActive(model.Defense.Applicable);
            if(!model.Defense.Applicable) return;
            if(outcomeBackdrop!=null && m03ResultBackdrop!=null) outcomeBackdrop.texture=m03ResultBackdrop;
            if(!reviewNarrated && model.Defense.ReviewTutorial)
            {
                var settings=SettingsService.Load().Assistant;
                if(settings.AssistanceLevel is UIAssistanceLevel.FullGuidance or UIAssistanceLevel.HintsOnly)
                    reviewNarrated=UiShellRuntimeGateway.TryEnqueueTutorialNarration(12,12,UiTutorialNarrationPhase.PrimaryAction,UiShellRuntimeGateway.Localization.Get("mission.m03.tutorial.12.body"));
            }
            bool victory=model.Outcome==UiMissionResultOutcome.Victory;
            SetDefenseText(missionStatusText,model.Title);
            SetDefenseText(starCountText,UiShellRuntimeGateway.Localization.Format("mission.m03.result.stars","",model.Stars));
            SetDefenseText(civilianLostText,model.Defense.CivilianLosses.ToString());
            SetDefenseText(objectivePatrolStatusText,UiShellRuntimeGateway.Localization.Get("mission.m03.result."+(victory ? "stopped" : "incomplete")));
            SetDefenseText(objectiveSquadStatusText,UiShellRuntimeGateway.Localization.Get("mission.m03.result."+(model.Defense.PostDestroyed ? "destroyed" : model.Defense.PostDamaged ? "damaged" : "undamaged")));
            SetDefenseText(objectiveCivilianStatusText,UiShellRuntimeGateway.Localization.Get("mission.m03.result."+(model.Defense.CivilianLosses==0 ? "safe" : "losses")));
            if(defenseLabels!=null) foreach(var label in defenseLabels)
            {
                label.Text?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(18,27);
                SetDefenseText(label.Text,UiShellRuntimeGateway.Localization.Get(label.Key));
            }
            SetDefenseText(titleText,model.Title); SetDefenseText(missionNameText,model.Subtitle);
            SetDefenseText(missionIdentityText,BuildMissionIdentity(in model)); SetDefenseText(summaryText,model.SummaryBody);
            SetDefenseText(elapsedText,model.ElapsedText); SetDefenseText(squadLossText,model.SquadLossText);
            SetDefenseText(enemiesDefeatedText,model.EnemiesDefeatedText);
            SetDefenseText(primaryButtonLabel,model.PrimaryActionLabel); SetDefenseText(retryButtonLabel,model.PrimaryActionLabel);
            if(victoryRewardIconsRoot!=null) victoryRewardIconsRoot.SetActive(false);
            if(rewardsText!=null)
            {
                rewardsText.lineSpacing=0;
                rewardsText.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(17,22);
                SetDefenseText(rewardsText,BuildRewardDisplay(model.RewardsText));
            }
            foreach(var status in new[]{objectivePatrolStatusText,objectiveSquadStatusText,objectiveCivilianStatusText})
                status?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(18,22);
            missionIdentityText?.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(19,30);
            LayoutDefenseResult();
        }
        private void LayoutDefenseResult()
        {
            if(!defenseLayoutActive) return;
            if(defenseRewardsPanel!=null) defenseRewardsPanel.sizeDelta=new Vector2(defenseRewardsPanel.sizeDelta.x,245);
            if(rewardsText!=null)
            {rewardsText.rectTransform.anchoredPosition=new Vector2(24,-70); rewardsText.rectTransform.sizeDelta=new Vector2(427,166);}
            if(defenseLabels==null) return;
            foreach(var label in defenseLabels)
            {
                if(label.Text==null || label.Text.transform.parent is not RectTransform parent) continue;
                var rect=label.Text.rectTransform;
                if(parent.name.StartsWith("Objective_",System.StringComparison.Ordinal))
                    rect.sizeDelta=new Vector2(parent.rect.width-92-190-44,rect.sizeDelta.y);
                else if(label.Text.name=="SectionTitleText")
                    rect.sizeDelta=new Vector2(parent.rect.width-rect.anchoredPosition.x-24,rect.sizeDelta.y);
            }
            LayoutDefenseStatus(objectivePatrolStatusText); LayoutDefenseStatus(objectiveSquadStatusText); LayoutDefenseStatus(objectiveCivilianStatusText);
        }
        private static void LayoutDefenseStatus(TMP_Text text)
        {
            if(text==null || text.transform.parent is not RectTransform parent) return;
            text.rectTransform.anchoredPosition=new Vector2(parent.rect.width-214,-11);
            text.rectTransform.sizeDelta=new Vector2(190,64);
        }
        private void OpenDefenseGuide()=>UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.OpenGuide);
        private static void SetDefenseText(TMP_Text text,string value)
        {
            if(text==null) return;
            var binding=text.GetComponent<V3LocalizedTextBindingView>();
            if(binding!=null) binding.SetLocalizedValue(value);
            else text.text=V3LocalizedTextBindingView.ShapeForRendering(value);
        }
    }
}
