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
        [SerializeField] private Button close,begin,stop;
        [SerializeField] private TMP_Text chargesLeft,fuelCost,role;
        public Button AriaStopButton=>stop;
        internal static SupportPopupView Active {get;private set;}
        private UiSupportModel previous;private bool rendered,closing;
        public void Configure(SupportAbilityCardView[] cardViews,Sprite[] art,Image preview,TMP_Text nameText,TMP_Text body,TMP_Text facts,
            TMP_Text resourceText,TMP_Text missionText,TMP_Text nextLabel,Button closeButton,Button next,Button ariaStop)
        {cards=cardViews;icons=art;detailArt=preview;title=nameText;description=body;requirements=facts;resources=resourceText;mission=missionText;
         actionLabel=nextLabel;close=closeButton;begin=next;stop=ariaStop;}
        public void ConfigureStats(TMP_Text chargesText,TMP_Text fuelText,TMP_Text roleText)
        {chargesLeft=chargesText;fuelCost=fuelText;role=roleText;}
        private void OnEnable()
        {
            Active=this;
            close.onClick.AddListener(Close);begin.onClick.AddListener(Begin);stop.onClick.AddListener(Stop);
            UiShellRuntimeGateway.Localization.LocaleChanged+=LocaleChanged;rendered=closing=false;
            UiShellRuntimeGateway.SelectSupport(1);
        }
        private void OnDisable()
        {
            if(Active==this)Active=null;
            close.onClick.RemoveListener(Close);begin.onClick.RemoveListener(Begin);stop.onClick.RemoveListener(Stop);
            UiShellRuntimeGateway.Localization.LocaleChanged-=LocaleChanged;
        }
        private void LocaleChanged()=>rendered=false;
        private void Close()=>closing=UiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind.CloseSupport);
        private void Begin(){if(previous.Selected==4&&previous.SupplyReady&&previous.SupplyStock>0)UiShellRuntimeGateway.BeginSupplyCollection();else UiShellRuntimeGateway.BeginSupportTargeting();}
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
            detailArt.sprite=icons[selected-1];
            detailArt.GetComponent<AspectRatioFitter>().aspectRatio=detailArt.sprite.rect.width/detailArt.sprite.rect.height;
            UiLocalizedText.Set(title,UiShellRuntimeGateway.Localization.Get("support.name."+selected,""));
            UiLocalizedText.Set(description,UiShellRuntimeGateway.Localization.Get(model.LessonKind==selected?"support.lesson."+selected:"support.description."+selected,""));
            var a=model.Ability(selected);
            UiLocalizedText.Set(chargesLeft,a.Charges.ToString());
            UiLocalizedText.Set(fuelCost,a.FuelCost.ToString());
            UiLocalizedText.Set(role,UiShellRuntimeGateway.Localization.Get("support.ui.role."+selected,""));
            string reason=UiShellRuntimeGateway.Localization.Get(a.ReasonKey,"");
            if(a.Cooldown>0)reason=UiShellRuntimeGateway.Localization.Format("support.ui.cooldown","Ready in {0}s",a.Cooldown);
            UiLocalizedText.Set(requirements,reason);
            requirements.color=a.Available?new Color32(79,199,73,255):new Color32(54,174,215,255);
            UiLocalizedText.Set(resources,Mathf.FloorToInt(model.Fuel).ToString("N0"));
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
