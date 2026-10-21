using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public struct ModeResultContent
    {
        public ResultScreenMode Mode;
        public string MissionId, Context;
        public byte MedalTier, FailedObjectives, IncompleteObjectives, NeutralObjectives;
        public bool Victory;
        public string Title;
        public string Identity;
        public string Status;
        public string Elapsed;
        public int Stars;
        public string ObjectiveTitle;
        public string Objective1;
        public string Objective2;
        public string Objective3;
        public string Objective1State;
        public string Objective2State;
        public string Objective3State;
        public bool HasObjectiveFacts;
        public byte CompletedObjectives;
        public string Performance1;
        public string Performance1Value;
        public string Performance2;
        public string Performance2Value;
        public string Performance3;
        public string Performance3Value;
        public string Summary;
        public string LeaveLabel;
        public bool ShowAdjust;
        public bool ActionsEnabled;
        public string Signature;
        public bool ShowMedal;
        public string MedalText;
        public bool ShowBattlefieldBack;
        public string BattlefieldBackLabel;
    }

    /// <summary>Mode adapter for the one authored shared result prefab.</summary>
    [DisallowMultipleComponent]
    public sealed class CampaignStyleResultCard : MonoBehaviour
    {
        private SharedMissionResultView view;
        private Action retry, replay, leave, adjust, battlefield;
        public SharedMissionResultView SharedView => view;
        public static byte OperationsStars(bool victory,bool partial)=>victory?(byte)3:partial?(byte)2:(byte)0;
        public static byte SkirmishStars(bool victory,int buildingsLost,int unitsLost)=>!victory?(byte)0:(byte)(1+(buildingsLost==0?1:0)+(unitsLost<=2?1:0));
        public static CampaignStyleResultCard Mount(Transform parent,TMP_FontAsset fontAsset)
        {
            var native=SharedMissionResultView.Mount(parent);native.name="CampaignStyleResult";
            var card=native.gameObject.AddComponent<CampaignStyleResultCard>();card.view=native;return card;
        }
        public void Bind(Action onRetry,Action onReplay,Action onLeave,Action onAdjust)
        {retry=onRetry;replay=onReplay;leave=onLeave;adjust=onAdjust;}
        public void BindBattlefield(Action action)=>battlefield=action;
        public void Present(in ModeResultContent c)
        {
            bool skirmish=c.Mode==ResultScreenMode.Skirmish;
            string W(string key,string en,string fa)=>SharedMissionResultView.Words(key,en,fa);
            string[] names={c.Objective1,c.Objective2,c.Objective3},values={c.Objective1State,c.Objective2State,c.Objective3State};
            if(skirmish&&c.HasObjectiveFacts&&c.NeutralObjectives==0)
            {
                if((c.IncompleteObjectives&1)!=0)values[0]=W("not_complete","NOT COMPLETE","تکمیل نشده");
                values[2]=string.Format(W("loss_count","{0} LOST","{0} تلفات"),c.Performance1Value);
            }
            var objectives=new SharedResultObjective[3];
            for(int i=0;i<3;i++)
            {
                var state=c.HasObjectiveFacts?((c.CompletedObjectives&(1<<i))!=0?ResultObjectiveState.Complete:
                    (c.NeutralObjectives&(1<<i))!=0?ResultObjectiveState.Information:(c.IncompleteObjectives&(1<<i))!=0?ResultObjectiveState.Incomplete:ResultObjectiveState.Failed):SharedResultStateClassifier.Classify(values[i]);
                objectives[i]=new SharedResultObjective(names[i],values[i],state);
            }
            bool battlefieldBack=skirmish&&c.ShowBattlefieldBack;
            Action primary=c.Victory?(battlefieldBack?battlefield:leave):retry;
            Action secondary=c.Victory?replay:(battlefieldBack?battlefield:leave);
            Action tertiary=c.Victory?(skirmish?leave:null):(c.ShowAdjust?adjust:null);
            view.BindActions(primary,primary,secondary,tertiary);
            var model=new SharedMissionResultPresentation
            {
                Mode=c.Mode,Victory=c.Victory,Stars=c.Stars,UseRetry=!c.Victory,ActionsEnabled=c.ActionsEnabled,
                MissionId=c.MissionId,ModeLabel=skirmish?W("skirmish","SKIRMISH","درگیری"):W("operations","OPERATIONS","عملیات"),
                MissionTitle=string.IsNullOrWhiteSpace(c.MissionId)?(skirmish?LastLine(c.Identity):FirstLine(c.Identity)):c.MissionId+" • "+(skirmish?LastLine(c.Identity):FirstLine(c.Identity)),Context=c.Context,
                OutcomeTitle=c.Title=="VICTORY"?W("victory","VICTORY","پیروزی"):c.Title=="DEFEAT"?W("defeat","DEFEAT","شکست"):c.Title=="OPERATION FAILED"?W("operation_failed","OPERATION FAILED","عملیات ناموفق"):c.Title,Reason=skirmish?(c.Victory?c.ObjectiveTitle:c.Status==c.Title?c.Summary:c.Status):c.Status,Elapsed=c.Elapsed,Summary=c.Summary,Objectives=objectives,
                PerformanceLabels=new[]{c.Performance1,c.Performance2,c.Performance3},PerformanceValues=new[]{c.Performance1Value,c.Performance2Value,c.Performance3Value},
                InfoHeading=skirmish?W("medal","MEDAL","نشان"):W("report","OPERATION REPORT","گزارش عملیات"),
                InfoBody=skirmish?(c.ShowMedal?c.MedalText:W("no_medal","No new medal","نشان جدیدی دریافت نشد")):c.ObjectiveTitle,
                ShowMedal=skirmish,MedalEarned=c.ShowMedal,MedalTier=c.MedalTier,
                SaveStatus=c.ShowMedal?W("medal_recorded","Medal recorded.","نشان ثبت شد."):!skirmish?(c.ActionsEnabled?W("report_saved","Report saved.","گزارش ذخیره شد."):W("saving","Saving result…","در حال ذخیرهٔ نتیجه…")):"",
                PrimaryLabel=c.Victory?(battlefieldBack?W("battlefield","BATTLEFIELD","میدان نبرد"):skirmish?c.LeaveLabel:W("district_map","DISTRICT MAP","نقشهٔ منطقه")):skirmish?W("retry_battle","RETRY BATTLE","تلاش دوباره در نبرد"):W("retry_operation","RETRY OPERATION","تلاش دوباره در عملیات"),
                SecondaryLabel=secondary==null?"":c.Victory?W("replay","REPLAY","بازی دوباره"):battlefieldBack?W("battlefield","BATTLEFIELD","میدان نبرد"):skirmish?c.LeaveLabel:W("district_map","DISTRICT MAP","نقشهٔ منطقه"),
                TertiaryLabel=tertiary==null?"":c.Victory?c.LeaveLabel:W("adjust","ADJUST SETUP","تنظیم نبرد"),
                PrimaryTone=c.Victory?ResultActionTone.Navigation:ResultActionTone.Positive,SecondaryTone=c.Victory?ResultActionTone.Neutral:ResultActionTone.Navigation,TertiaryTone=ResultActionTone.Neutral,
                Signature=c.Signature
            };
            view.Present(model);
        }
        static string FirstLine(string text)=>(text??"").Split('\n')[0];
        static string LastLine(string text){var parts=(text??"").Split('\n');return parts.Length>1?parts[parts.Length-1]:text;}
    }

    [DisallowMultipleComponent]
    public sealed class ResultStarSequence : MonoBehaviour
    {
        private RectTransform[] stars;
        private GameObject[] outlines;
        private float[] startAt;
        private int earned;
        private bool playing;

        public void Play(RectTransform[] filled, int count, bool victory, GameObject[] outlineRoots = null)
        {
            stars = filled;
            outlines = outlineRoots;
            earned = Mathf.Clamp(count, 0, filled != null ? filled.Length : 0);
            bool instant = !Application.isPlaying || !victory || earned == 0 ||
                           SettingsService.Load().Accessibility.ReducedMotion;
            if (filled != null)
            {
                for (int i = 0; i < filled.Length; i++)
                {
                    if (filled[i] == null)
                        continue;
                    bool show = i < earned;
                    filled[i].gameObject.SetActive(show);
                    filled[i].localScale = instant && show ? Vector3.one : Vector3.zero;
                    SetOutline(i, !show || !instant);
                }
            }
            if (instant)
            {
                playing = false;
                return;
            }
            startAt = new float[earned];
            float now = Time.unscaledTime;
            for (int i = 0; i < earned; i++)
                startAt[i] = now + 0.28f + i * 0.46f;
            playing = true;
        }

        private void SetOutline(int index, bool visible)
        {
            if (outlines == null || index < 0 || index >= outlines.Length || outlines[index] == null)
                return;
            outlines[index].SetActive(visible);
        }

        private void Update()
        {
            if (!playing || stars == null)
                return;
            float now = Time.unscaledTime;
            bool finished = true;
            for (int i = 0; i < earned; i++)
            {
                if (stars[i] == null)
                    continue;
                float age = now - startAt[i];
                if (age < 0f)
                {
                    finished = false;
                    stars[i].localScale = Vector3.zero;
                    SetOutline(i, true);
                    continue;
                }
                SetOutline(i, false);
                float u = Mathf.Clamp01(age / 0.34f);
                if (u < 1f)
                    finished = false;
                float scale = u < 0.62f
                    ? Mathf.Lerp(0.15f, 1.32f, u / 0.62f)
                    : Mathf.Lerp(1.32f, 1f, (u - 0.62f) / 0.38f);
                stars[i].localScale = Vector3.one * scale;
            }
            if (finished)
                playing = false;
        }
    }
}
