using Game.UI.Contracts;
using TMPro;
using UnityEngine;
namespace Game.UI.Runtime
{
    public sealed partial class MissionResultPopupView
    {
        [SerializeField] private SharedMissionResultView sharedResult;
        public SharedMissionResultView SharedResult => sharedResult;
        private void ApplySharedResult(in UiMissionResultPopupModel model)
        {
            if(sharedResult==null)return;
            var statusRows=new[]{objectivePatrolStatusText,objectiveSquadStatusText,objectiveCivilianStatusText};
            var objectives=new SharedResultObjective[3];
            for(int i=0;i<3;i++)
            {
                string value=SharedMissionResultView.Source(statusRows[i]);
                objectives[i]=new SharedResultObjective(SharedMissionResultView.Source(RowLabel(statusRows[i])),value,SharedResultStateClassifier.Classify(value));
            }
            // First Contact has independent civilian/squad facts even after a defeat.
            if(model.MissionId=="saga.ch01.m01.first_contact")
            {
                objectives[0].Label=Words("patrol","Neutralize hostile patrol","خنثی‌سازی گشت دشمن");
                objectives[1].Label=Words("keep_squad","Keep command squad alive","زنده نگه داشتن گروه فرماندهی");
                objectives[2].Label=Words("civilians","Protect civilians","حفاظت از غیرنظامیان");
                objectives[0].Value=model.EnemiesDefeatedText;
                objectives[0].State=SharedResultStateClassifier.Classify(model.EnemiesDefeatedText);
                bool commandKnown=model.FirstContact.Applicable&&model.FirstContact.CommandSquadSpawned;
                objectives[1].State=!commandKnown?ResultObjectiveState.Information:model.FirstContact.CommandSquadAlive?ResultObjectiveState.Complete:ResultObjectiveState.Failed;
                objectives[1].Value=!commandKnown?Words("not_reported","Not reported","گزارش نشده"):model.FirstContact.CommandSquadAlive?Words("complete","COMPLETE","تکمیل شد"):Words("failed","FAILED","ناموفق");
                objectives[2].State=model.CivilianLossCount==0?ResultObjectiveState.Complete:ResultObjectiveState.Failed;
                objectives[2].Value=model.CivilianLossCount==0?Words("none_lost","0 LOST","۰ تلفات"):model.CivilianLossCount.ToString();
            }
            string reason=SharedMissionResultView.Source(missionStatusText);
            if(!string.IsNullOrEmpty(model.SummaryBody))reason=FirstSentence(UiLocalizedText.CatalogLabel(model.SummaryBody));
            string[] identity=SharedMissionResultView.Source(missionIdentityText).Split('\n');
            bool victory=model.Outcome==UiMissionResultOutcome.Victory;
            int chapter=Game.Missions.Contracts.CampaignMissionSequence.IndexOf(model.MissionId)/5;
            string[] chapters={"I","II","III","IV","V"},chaptersFa={"اول","دوم","سوم","چهارم","پنجم"};
            chapter=Mathf.Clamp(chapter,0,4);
            bool guide=defenseGuideButton!=null&&defenseGuideButton.gameObject.activeSelf;
            var modelView=new SharedMissionResultPresentation
            {
                Mode=ResultScreenMode.Campaign,MissionId=model.MissionId,Victory=victory,Stars=model.Stars,UseRetry=model.RetryVisible,
                ModeLabel=string.Format(Words("campaign_chapter","CAMPAIGN / CHAPTER {0}","کارزار / فصل {0}"),Game.Configs.GameLocalization.IsRightToLeft?chaptersFa[chapter]:chapters[chapter]),MissionTitle=identity.Length>0?identity[0]:model.Subtitle,Context=identity.Length>1?identity[1]:"",
                OutcomeTitle=Words(victory?"victory":"defeat",victory?"VICTORY":"DEFEAT",victory?"پیروزی":"شکست"),Reason=reason,Elapsed=model.ElapsedText,
                Summary=SharedMissionResultView.Source(summaryText),Objectives=objectives,
                PerformanceLabels=new[]{SharedMissionResultView.Source(RowLabel(squadLossText)),SharedMissionResultView.Source(RowLabel(enemiesDefeatedText)),SharedMissionResultView.Source(RowLabel(civilianLostText))},
                PerformanceValues=new[]{SharedMissionResultView.Source(squadLossText),SharedMissionResultView.Source(enemiesDefeatedText),SharedMissionResultView.Source(civilianLostText)},
                ShowRewardIcons=victory&&!string.IsNullOrWhiteSpace(model.RewardsText)&&model.RewardsText!="NO REWARD"&&model.RewardsText!="—",
                InfoHeading=Words("rewards","REWARDS","پاداش‌ها"),InfoBody=victory?SharedMissionResultView.Source(rewardsText):Words("no_clear_reward","No clear reward","بدون پاداش تکمیل مأموریت"),
                SaveStatus=model.SettlementFailed?Words("save_failed","Result not saved. Retry saving before continuing.","نتیجه ذخیره نشد. پیش از ادامه، ذخیره را دوباره امتحان کنید."):model.PrimaryActionEnabled?Words("saved","Progress saved.","پیشرفت ذخیره شد."):Words("saving","Saving result…","در حال ذخیرهٔ نتیجه…"),
                PrimaryLabel=model.RetryVisible&&!model.SettlementFailed?Words("retry_mission","RETRY MISSION","تلاش دوباره در مأموریت"):model.PrimaryActionLabel,PrimaryTone=ResultActionTone.Positive,ActionsEnabled=model.PrimaryActionEnabled,
                SecondaryLabel=guide?Words("review_briefing","REVIEW BRIEFING","مرور راهنمای مأموریت"):"",SecondaryTone=ResultActionTone.Navigation,
                Signature=model.MissionId+"|"+model.Version+"|"+Game.Configs.GameLocalization.CurrentLocaleCode
            };
            sharedResult.BindActions(OnPrimaryRequested,OnRetryRequested,guide?OpenDefenseGuide:null);
            sharedResult.Present(modelView);
        }
        private static string Words(string key,string en,string fa)=>SharedMissionResultView.Words(key,en,fa);
        private static string FirstSentence(string text)
        {
            if(string.IsNullOrWhiteSpace(text))return "";
            int end=text.IndexOfAny(new[]{'.','!','؟','\n'});
            return end>=0?text.Substring(0,end+1):text;
        }
    }
}
