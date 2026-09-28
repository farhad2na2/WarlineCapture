using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Game.UI.Runtime
{
    [RequireComponent(typeof(Canvas),typeof(GraphicRaycaster))]
    public sealed class SupportPopupView : MonoBehaviour
    {
        [SerializeField] private SupportAbilityCardView[] cards;
        [SerializeField] private Sprite[] icons;
        [SerializeField] private Image detailArt;
        [SerializeField] private TMP_Text title,description,requirements,resources,mission,actionLabel;
        [SerializeField] private Button close,begin,play,stop;
        private UiSupportModel previous;private bool rendered,closing;
        public void Configure(SupportAbilityCardView[] cardViews,Sprite[] art,Image preview,TMP_Text nameText,TMP_Text body,TMP_Text facts,
            TMP_Text resourceText,TMP_Text missionText,TMP_Text nextLabel,Button closeButton,Button next,Button ariaPlay,Button ariaStop)
        {cards=cardViews;icons=art;detailArt=preview;title=nameText;description=body;requirements=facts;resources=resourceText;mission=missionText;
         actionLabel=nextLabel;close=closeButton;begin=next;play=ariaPlay;stop=ariaStop;}
        private void OnEnable()
        {
            close.onClick.AddListener(Close);begin.onClick.AddListener(Begin);play.onClick.AddListener(Play);stop.onClick.AddListener(Stop);
            UiShellRuntimeGateway.Localization.LocaleChanged+=LocaleChanged;rendered=closing=false;
            UiShellRuntimeGateway.SelectSupport(1);
        }
        private void OnDisable()
        {
            close.onClick.RemoveListener(Close);begin.onClick.RemoveListener(Begin);play.onClick.RemoveListener(Play);stop.onClick.RemoveListener(Stop);
            UiShellRuntimeGateway.Localization.LocaleChanged-=LocaleChanged;
        }
        private void LocaleChanged()=>rendered=false;
        private void Close()=>closing=UiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind.CloseSupport);
        private void Begin(){if(previous.Selected==4&&previous.SupplyReady&&previous.SupplyStock>0)UiShellRuntimeGateway.BeginSupplyCollection();else UiShellRuntimeGateway.BeginSupportTargeting();}
        private void Play()=>UiShellRuntimeGateway.TryStartAriaPlay();
        private void Stop(){UiShellRuntimeGateway.StopAriaPlay();UiShellRuntimeGateway.DeclineSupport();}
        private void Update()
        {
            if(!UiShellRuntimeGateway.TryReadSupport(out var model)) {begin.interactable=false;return;}
            if(closing)return;
            if(!model.Active){Close();return;}
            if(rendered && previous.Version==model.Version && previous.Fuel==model.Fuel && previous.Ability(model.Selected).Cooldown==model.Ability(model.Selected).Cooldown &&
                previous.Ability(model.Selected).Available==model.Ability(model.Selected).Available&&previous.SupplyStock==model.SupplyStock&&previous.SupplyReady==model.SupplyReady&&previous.SupplyClaimed==model.SupplyClaimed&&previous.SupplyFull==model.SupplyFull&&previous.CollectionReasonKey==model.CollectionReasonKey&&previous.LessonKind==model.LessonKind)return;
            Apply(model);previous=model;rendered=true;
        }
        public void Apply(UiSupportModel model)
        {
            byte selected=model.Selected==0?(byte)1:model.Selected;
            for(int i=0;i<cards.Length;i++)cards[i].Render(model.Ability((byte)(i+1)),selected==i+1);
            detailArt.sprite=icons[selected-1];UiLocalizedText.Set(title,UiShellRuntimeGateway.Localization.Get("support.name."+selected,""));
            UiLocalizedText.Set(description,UiShellRuntimeGateway.Localization.Get(model.LessonKind==selected?"support.lesson."+selected:"support.description."+selected,""));
            var a=model.Ability(selected);
            UiLocalizedText.Set(requirements,UiShellRuntimeGateway.Localization.Format("support.detail.cost","{0} charges left · {1} Fuel\n{2}",a.Charges,a.FuelCost,
                UiShellRuntimeGateway.Localization.Get(a.ReasonKey,"")));
            UiLocalizedText.Set(resources,UiShellRuntimeGateway.Localization.Format("support.fuel.available","Available Fuel: {0}",Mathf.FloorToInt(model.Fuel)));
            UiLocalizedText.Set(mission,UiShellRuntimeGateway.Localization.Get("support.optional", "Optional support"));
            if(UiShellRuntimeGateway.TryReadMissionBriefing(out var briefing) && briefing.MissionId==model.MissionId)
            {
                string context=UiShellRuntimeGateway.Localization.Get(briefing.DisplayNameKey,"");
                if(briefing.Objectives.Length>0)context+=" · "+UiShellRuntimeGateway.Localization.Get(briefing.Objectives[0].DisplayTextKey,"");
                UiLocalizedText.Set(mission,context);
            }
            bool collect=selected==4&&model.SupplyReady&&model.SupplyStock>0;
            begin.interactable=collect?!model.SupplyClaimed:a.Available;
            if(collect)UiLocalizedText.Set(requirements,UiShellRuntimeGateway.Localization.Format("support.supply.stock","",model.SupplyStock,UiShellRuntimeGateway.Localization.Get(!string.IsNullOrEmpty(model.CollectionReasonKey)&&model.CollectionReasonKey!="support.reason.0"?model.CollectionReasonKey:model.SupplyFull?"support.supply.full":model.SupplyClaimed?"support.supply.claimed":"support.supply.select","")));
            UiLocalizedText.Set(actionLabel,UiShellRuntimeGateway.Localization.Get(collect?"support.supply.collect":selected==1?"support.choose.area":selected==2?"support.choose.target":"support.choose.zone",""));
        }
    }
}
