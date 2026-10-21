using System;
using Game.Configs;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed partial class SharedMissionResultView : MonoBehaviour
    {
        public const string ResourcePath = "UI/SharedMissionResult";
        public static readonly Color Cyan = new Color32(0,194,232,255), Green = new Color32(112,205,48,255),
            Red = new Color32(238,66,45,255), Amber = new Color32(255,184,22,255), Muted = new Color32(155,179,193,255),
            White = new Color32(244,246,242,255), Line = new Color32(69,103,119,255);
        [Serializable] public sealed class Bindings
        {
            public TMP_Text ObjectivesHeading, PerformanceHeading, DurationHeading;
            public TMP_Text Mode, Mission, Context, Duration, Outcome, Reason, SummaryTitle, Summary, InfoHeading, InfoBody, SaveStatus;
            public TMP_Text[] ObjectiveLabels, ObjectiveValues, PerformanceLabels, PerformanceValues;
            public RawImage Artwork, Backdrop;
            public Image BackdropShade;
            public RectTransform ArtworkClip, SummaryViewport, InfoViewport, SaveStrip;
            public ScrollRect SummaryScroll, InfoScroll;
            public V3GradientGraphic OutcomeSurface;
            public SharedResultSymbolGraphic OutcomeIcon, ReportIcon;
            public SharedResultSymbolGraphic[] ObjectiveIcons, StatusIcons;
            public V3GradientGraphic[] ObjectiveSurfaces, StatusSurfaces;
            public Image[] ObjectiveStripes;
            public GameObject[] ObjectiveRows;
            public Image[] RewardIcons;
            public V3StarGraphic[] FilledStars, OutlineStars;
            public RectTransform[] StarRects;
            public MissionAtlasStatusGraphic Medal;
            public Button Primary, Retry, Secondary, Tertiary;
            public TMP_Text PrimaryLabel, RetryLabel, SecondaryLabel, TertiaryLabel;
            public Image PrimaryIcon, RetryIcon, SecondaryIcon, TertiaryIcon;
            public Sprite BackIcon, ContinueIcon, ReplayIcon, MapIcon, AdjustIcon;
        }
        [SerializeField] private Bindings ui;
        private SharedMissionResultPresentation current;
        private UnityAction primaryAction, retryAction, secondaryAction, tertiaryAction;
        private string starSignature;
        private Action boundPrimary, boundRetry, boundSecondary, boundTertiary;
        public Bindings Controls => ui;
        public static SharedMissionResultView Mount(Transform parent)
        {
            var source = Resources.Load<GameObject>(ResourcePath);
            if (source == null) throw new InvalidOperationException("Shared mission result prefab is missing");
            var instance = Instantiate(source, parent, false); instance.name = "SharedMissionResult";
            return instance.GetComponent<SharedMissionResultView>();
        }
#if UNITY_EDITOR
        public void Author(Bindings bindings) => ui = bindings;
#endif
        public void BindActions(Action primary, Action retry, Action secondary = null, Action tertiary = null)
        {
            if(primaryAction!=null&&boundPrimary==primary&&boundRetry==retry&&boundSecondary==secondary&&boundTertiary==tertiary)return;
            UnbindActions();
            boundPrimary=primary;boundRetry=retry;boundSecondary=secondary;boundTertiary=tertiary;
            primaryAction = () => primary?.Invoke(); retryAction = () => retry?.Invoke();
            secondaryAction = () => secondary?.Invoke(); tertiaryAction = () => tertiary?.Invoke();
            ui.Primary.onClick.AddListener(primaryAction); ui.Retry.onClick.AddListener(retryAction);
            ui.Secondary.onClick.AddListener(secondaryAction); ui.Tertiary.onClick.AddListener(tertiaryAction);
        }
        public void UnbindActions()
        {
            if(ui == null) return;
            if(primaryAction != null) ui.Primary.onClick.RemoveListener(primaryAction);
            if(retryAction != null) ui.Retry.onClick.RemoveListener(retryAction);
            if(secondaryAction != null) ui.Secondary.onClick.RemoveListener(secondaryAction);
            if(tertiaryAction != null) ui.Tertiary.onClick.RemoveListener(tertiaryAction);
            primaryAction=retryAction=secondaryAction=tertiaryAction=null;
            boundPrimary=boundRetry=boundSecondary=boundTertiary=null;
        }
        public void Present(SharedMissionResultPresentation model)
        {
            current = model;
            Copy(ui.ObjectivesHeading,Words("objectives","OBJECTIVES","اهداف"));
            Copy(ui.PerformanceHeading,Words("performance","PERFORMANCE","عملکرد"));
            Copy(ui.DurationHeading,Words("duration","MISSION DURATION","زمان مأموریت"));
            Copy(ui.Mode, model.ModeLabel); Copy(ui.Mission, model.MissionTitle); Copy(ui.Context, model.Context);
            Copy(ui.Duration, model.Elapsed); Copy(ui.Outcome, model.OutcomeTitle); Copy(ui.Reason, model.Reason);
            Copy(ui.SummaryTitle, model.MissionTitle); Copy(ui.Summary, model.Summary);
            Copy(ui.InfoHeading, model.InfoHeading); Copy(ui.InfoBody, model.InfoBody); Copy(ui.SaveStatus, model.SaveStatus);
            ui.SaveStrip.gameObject.SetActive(!string.IsNullOrWhiteSpace(model.SaveStatus));
            Color outcome = model.Victory ? Green : Red;
            ui.Outcome.color = outcome;
            ui.OutcomeSurface.Configure(model.Victory ? new Color32(10,58,22,255) : new Color32(63,13,21,255),new Color32(3,16,24,255),outcome,2);
            ui.OutcomeIcon.Configure(model.Victory ? SharedResultSymbolGraphic.Kind.OutcomeSuccess : SharedResultSymbolGraphic.Kind.OutcomeFailure,outcome);
            for(int i=0;i<3;i++)
            {
                bool shown = i < model.Objectives.Length && !string.IsNullOrWhiteSpace(model.Objectives[i].Label);
                ui.ObjectiveRows[i].SetActive(shown);
                if(shown)
                {
                    var objective = model.Objectives[i]; Color ink = StatusColor(objective.State);
                    Copy(ui.ObjectiveLabels[i],objective.Label); Copy(ui.ObjectiveValues[i],objective.Value); ui.ObjectiveValues[i].color=ink;
                    var kind=model.Mode==ResultScreenMode.Operations?(i==0?SharedResultSymbolGraphic.Kind.Target:i==1?SharedResultSymbolGraphic.Kind.Document:SharedResultSymbolGraphic.Kind.Squad):
                        model.Mode==ResultScreenMode.Skirmish?(i==0?SharedResultSymbolGraphic.Kind.Target:i==1?SharedResultSymbolGraphic.Kind.Shield:SharedResultSymbolGraphic.Kind.Squad):(SharedResultSymbolGraphic.Kind)i;
                    ui.ObjectiveIcons[i].Configure(kind,ink);
                    ui.StatusIcons[i].Configure(objective.State==ResultObjectiveState.Complete ? SharedResultSymbolGraphic.Kind.Check : objective.State==ResultObjectiveState.Failed ? SharedResultSymbolGraphic.Kind.Cross : SharedResultSymbolGraphic.Kind.Minus,objective.State==ResultObjectiveState.Complete?White:ink);
                    ui.StatusSurfaces[i].Configure(objective.State==ResultObjectiveState.Complete?new Color32(38,119,28,255):new Color32(13,30,40,255),objective.State==ResultObjectiveState.Complete?new Color32(20,75,24,255):new Color32(3,15,24,255),ink,1);
                    ui.ObjectiveStripes[i].color=ink;
                    ui.ObjectiveSurfaces[i].SetBorder(Line,1);
                }
                Copy(ui.PerformanceLabels[i],i<model.PerformanceLabels.Length?model.PerformanceLabels[i]:"");
                Copy(ui.PerformanceValues[i],i<model.PerformanceValues.Length?model.PerformanceValues[i]:"");
            }
            var art = model.Artwork != null ? model.Artwork : SharedResultArtworkCatalog.Load()?.Find(model.MissionId);
            ui.Artwork.texture=art; ui.Artwork.enabled=art!=null; ui.Artwork.color=model.Victory?Color.white:new Color(.65f,.65f,.65f,1);
            if(ui.Backdrop!=null)ui.Backdrop.texture=SharedResultArtworkCatalog.Load()?.Backdrop;
            ui.Medal.gameObject.SetActive(model.ShowMedal); ui.Medal.Configure(MissionAtlasStatusGraphic.Kind.Medal,model.MedalEarned,model.MedalTier);
            if(ui.ReportIcon!=null){ui.ReportIcon.gameObject.SetActive(model.Mode==ResultScreenMode.Operations);ui.ReportIcon.Configure(SharedResultSymbolGraphic.Kind.Document,model.Victory?Green:Red);}
            ui.InfoBody.color=(model.ShowMedal&&model.MedalEarned)||model.ShowRewardIcons?Amber:White;
            if(ui.RewardIcons!=null&&ui.RewardIcons.Length>=2)
            {
                string rewards=model.InfoBody??"";
                bool experience=rewards.IndexOf("XP",StringComparison.OrdinalIgnoreCase)>=0||rewards.Contains("تجربه");
                bool credits=rewards.IndexOf("CREDIT",StringComparison.OrdinalIgnoreCase)>=0||rewards.Contains("اعتبار");
                ui.RewardIcons[0].gameObject.SetActive(model.ShowRewardIcons&&experience);
                ui.RewardIcons[1].gameObject.SetActive(model.ShowRewardIcons&&credits);
                Place(ui.RewardIcons[1].rectTransform,24,experience?124:84,52,44);
            }
            ConfigureAction(ui.Primary,ui.PrimaryLabel,ui.PrimaryIcon,model.PrimaryLabel,model.PrimaryTone,model.ActionsEnabled,model.PrimaryTone==ResultActionTone.Navigation?ui.MapIcon:model.Victory?ui.ContinueIcon:ui.ReplayIcon);
            ConfigureAction(ui.Retry,ui.RetryLabel,ui.RetryIcon,model.PrimaryLabel,model.PrimaryTone,model.ActionsEnabled,ui.ReplayIcon);
            ui.Primary.gameObject.SetActive(!model.UseRetry&&!string.IsNullOrWhiteSpace(model.PrimaryLabel)); ui.Retry.gameObject.SetActive(model.UseRetry&&!string.IsNullOrWhiteSpace(model.PrimaryLabel));
            ConfigureAction(ui.Secondary,ui.SecondaryLabel,ui.SecondaryIcon,model.SecondaryLabel,model.SecondaryTone,model.ActionsEnabled,model.Victory?ui.ReplayIcon:ui.BackIcon);
            ConfigureAction(ui.Tertiary,ui.TertiaryLabel,ui.TertiaryIcon,model.TertiaryLabel,model.TertiaryTone,model.ActionsEnabled,model.Victory?ui.BackIcon:ui.AdjustIcon);
            string signature=model.Signature+"|"+model.Stars+"|"+model.Victory;
            if(starSignature!=signature)
            {
                starSignature=signature;
                var outlines=new GameObject[3];
                for(int i=0;i<3;i++)
                {
                    ui.FilledStars[i].ConfigureOutlinedGradient(new Color32(255,208,64,255),new Color32(255,211,57,255),new Color32(214,143,15,255));
                    ui.OutlineStars[i].ConfigureOutlinedGradient(Muted,new Color32(3,15,24,255),new Color32(3,15,24,255),0.78f);
                    outlines[i]=ui.OutlineStars[i].gameObject;
                }
                var sequence=GetComponent<ResultStarSequence>();if(sequence==null)sequence=gameObject.AddComponent<ResultStarSequence>();
                sequence.Play(ui.StarRects,model.Stars,model.Victory,outlines);
            }
            RefreshLayout();
        }
        public static Color StatusColor(ResultObjectiveState state)=>state==ResultObjectiveState.Complete?Green:state==ResultObjectiveState.Failed?Red:state==ResultObjectiveState.Information?Cyan:Amber;
        public static string Words(string key,string en,string fa)=>GameLocalization.Get("ui.result.shared."+key,GameLocalization.IsRightToLeft?fa:en);
        public static string Source(TMP_Text t)=>t==null?"":t.GetComponent<V3LocalizedTextBindingView>()?.SourceValue ?? t.text;
        static void Copy(TMP_Text label,string value)
        {
            if(label==null)return;
            if(Source(label)!=(value??""))UiLocalizedText.Set(label,value??"");
        }
        static void ConfigureAction(Button b,TMP_Text text,Image icon,string caption,ResultActionTone tone,bool enabled,Sprite sprite)
        {
            b.gameObject.SetActive(!string.IsNullOrWhiteSpace(caption)); b.interactable=enabled;
            Copy(text,caption);icon.sprite=sprite;icon.enabled=sprite!=null;
            var surface=b.GetComponent<V3GradientGraphic>();
            surface.Configure(tone==ResultActionTone.Positive?new Color32(30,145,48,255):tone==ResultActionTone.Navigation?new Color32(10,98,163,255):new Color32(45,66,81,255),tone==ResultActionTone.Positive?new Color32(4,58,24,255):new Color32(3,24,39,255),tone==ResultActionTone.Positive?Green:tone==ResultActionTone.Navigation?Cyan:Line,2);
            HudActionVisuals.Refresh(b);
        }
        void LateUpdate()=>RefreshLayout();
    }
}
