using System;
using System.Collections.Generic;
using Game.Configs;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Game.UI.Runtime
{
    // Serialized, bounded presentation. No world discovery, simulation mutation or duplicated balance table.
    public sealed class MissionFieldGuideView : MonoBehaviour
    {
        [SerializeField] private MissionFieldGuideConfig guide, extractionGuide;
        private MissionFieldGuideConfig defenseGuide;
        [SerializeField] private Button topicsButton,classesButton,previousButton,nextButton,closeButton,filterButton,radioButton;
        [SerializeField] private Image radioPanel;
        [SerializeField] private Sprite radio16x9,radio20x9;
        [SerializeField] private TMP_InputField search;
        [SerializeField] private Image portrait;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private V3LocalizedTextBinding title,body,example,mistake,diagram,page,filterLabel;
        private readonly List<int> matches=new(57);
        private bool classes;
        private bool radio;
        private bool largeText;
        private int index,filter=-1;
        private AsyncOperationHandle<UnitGridAuthoringConfig> unitHandle;
        private string unitKey;
        private V3LocalizedTextBinding scopedTitle;
        public int ResidentClassCount=>unitHandle.IsValid() ? 1 : 0;
        public bool ClassLoadPending=>unitHandle.IsValid() && !unitHandle.IsDone;
        public bool ClassLoadFailed=>unitHandle.IsValid() && unitHandle.IsDone && unitHandle.Status!=AsyncOperationStatus.Succeeded;
        public int VisibleClassCount=>matches.Count;
        public MissionFieldGuideConfig Guide=>guide;

        private void OnEnable()
        {
            if(defenseGuide==null)defenseGuide=guide;
            guide=UiShellRuntimeGateway.IsExtractionGuideContext() && extractionGuide!=null ? extractionGuide : defenseGuide;
            radio=classes=false; index=0; filter=-1;
            largeText=SettingsService.Load().Accessibility.LargeText;
            foreach(var binding in GetComponentsInChildren<V3LocalizedTextBinding>(true))
            {binding.SetPresentationScale(largeText ? 1.15f : 1);if(binding.LocalizationKey=="mission.m03.guide.title")scopedTitle=binding;}
            RefreshScopedTitle();
            topicsButton.onClick.AddListener(ShowTopics); classesButton.onClick.AddListener(ShowClasses);
            if(radioButton!=null) radioButton.onClick.AddListener(ShowRadio);
            previousButton.onClick.AddListener(Previous); nextButton.onClick.AddListener(Next); closeButton.onClick.AddListener(Close);
            filterButton.onClick.AddListener(CycleFilter); search.onValueChanged.AddListener(Search);
            GameLocalization.LocaleChanged+=LocaleChanged;
            RebuildMatches(); Refresh();
        }
        private void OnDisable()
        {
            topicsButton.onClick.RemoveListener(ShowTopics); classesButton.onClick.RemoveListener(ShowClasses);
            if(radioButton!=null) radioButton.onClick.RemoveListener(ShowRadio);
            previousButton.onClick.RemoveListener(Previous); nextButton.onClick.RemoveListener(Next); closeButton.onClick.RemoveListener(Close);
            filterButton.onClick.RemoveListener(CycleFilter); search.onValueChanged.RemoveListener(Search);
            GameLocalization.LocaleChanged-=LocaleChanged;
            ReleaseUnit();
        }
        private void LocaleChanged() {RefreshScopedTitle();RebuildMatches(); Refresh();}
        private void RefreshScopedTitle()=>scopedTitle?.SetLocalizedValue(GameText.Get(guide==extractionGuide?"mission.m04.guide.title":"mission.m03.guide.title"));
        private string AvailabilityKey(int value)=>guide==extractionGuide&&value<4?"mission.m04.guide.availability."+value:"mission.m03.guide.availability."+value;
        private void Close()=>UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.CloseGuide);
        public void ShowTopics() { radio=classes=false; index=0; Refresh(); }
        public void ShowClasses() { radio=false; classes=true; index=0; RebuildMatches(); Refresh(); }
        public void ShowRadio() {if(!UiShellRuntimeGateway.TryReadMissionRadioArchive()) return; radio=true; classes=false; index=0; Refresh();}
        public void Previous() { index=Mathf.Max(0,index-1); Refresh(); }
        public void Next() { index++; Refresh(); }
        private void CycleFilter() {filter=filter==4 ? -1 : filter+1; Search(search.text);}
        private void Search(string _) {index=0; RebuildMatches(); Refresh();}
        private void RebuildMatches()
        {
            matches.Clear(); if(guide==null) return;
            string query=search.text.Trim();
            for(int i=0;i<guide.Classes.Length;i++)
            {
                var entry=guide.Classes[i];
                if(filter>=0 && (int)entry.Availability!=filter) continue;
                if(query.Length!=0 && entry.Id.IndexOf(query,StringComparison.OrdinalIgnoreCase)<0 &&
                    GameText.Get(entry.NameKey,entry.Id).IndexOf(query,StringComparison.OrdinalIgnoreCase)<0) continue;
                matches.Add(i);
            }
        }
        public void Refresh()
        {
            if(guide==null) return;
            if(scroll!=null)
            {
                scroll.content.sizeDelta=new Vector2(scroll.content.sizeDelta.x,largeText ? classes ? 1060 : 850 : classes ? 925 : 715);
                var bodyRect=body.GetComponent<RectTransform>(); bodyRect.sizeDelta=new Vector2(1040,classes ? largeText ? 420 : 350 : largeText ? 200 : 140);
                SetTop(example,classes ? largeText ? 550 : 480 : largeText ? 330 : 270);
                SetTop(mistake,classes ? largeText ? 736 : 646 : largeText ? 516 : 436);
                SetTop(diagram,classes ? largeText ? 945 : 815 : largeText ? 735 : 600);
                scroll.StopMovement(); scroll.verticalNormalizedPosition=1;
            }
            search.gameObject.SetActive(classes); filterButton.gameObject.SetActive(classes);
            if(radioButton!=null) {radioButton.gameObject.SetActive(!classes && guide!=extractionGuide); radioButton.interactable=UiShellRuntimeGateway.TryReadMissionRadioArchive();}
            if(radioPanel!=null) radioPanel.gameObject.SetActive(radio);
            portrait.gameObject.SetActive(false);
            if(radio)
            {
                ReleaseUnit(); previousButton.interactable=nextButton.interactable=false;
                Set(title,"mission.m03.guide.radio"); body.SetLocalizedValue(GameText.Get(M03RadarWarningCopyCatalog.Comms[0].Key));
                example.SetLocalizedValue(""); mistake.SetLocalizedValue(""); diagram.SetLocalizedValue(""); page.SetLocalizedValue("1 / 1");
                scroll.content.sizeDelta=new Vector2(scroll.content.sizeDelta.x,810);
                body.GetComponent<RectTransform>().sizeDelta=new Vector2(1280,190);
                if(radioPanel!=null) radioPanel.sprite=Screen.width/(float)Mathf.Max(1,Screen.height)>2 ? radio20x9 : radio16x9;
                return;
            }
            int count=classes ? matches.Count : guide.Topics.Length;
            index=Mathf.Clamp(index,0,Mathf.Max(0,count-1));
            previousButton.interactable=index>0; nextButton.interactable=index+1<count;
            page.SetLocalizedValue(count==0 ? "0 / 0" : (index+1)+" / "+count);
            filterLabel.SetLocalizedValue(GameText.Get(filter<0 ? "mission.m03.guide.all" : AvailabilityKey(filter)));
            if(!classes)
            {
                ReleaseUnit();
                var topic=guide.Topics[index];
                Set(title,topic.TitleKey); Set(body,topic.BodyKey);
                example.SetLocalizedValue(GameText.Get("mission.m03.guide.example")+"\n"+GameText.Get(topic.ExampleKey));
                mistake.SetLocalizedValue(GameText.Get("mission.m03.guide.mistake")+"\n"+GameText.Get(topic.MistakeKey));
                Set(diagram,topic.DiagramKey); return;
            }
            if(count==0) {ReleaseUnit(); Set(title,"mission.m03.guide.no_match"); body.SetLocalizedValue(""); example.SetLocalizedValue(""); mistake.SetLocalizedValue(""); diagram.SetLocalizedValue(""); return;}
            var card=guide.Classes[matches[index]];
            Set(title,card.NameKey);
            var unit=RequestUnit(card);
            string stats=unit==null ? GameText.Get(card.Availability==MissionGuideAvailability.Unavailable ? "mission.m03.guide.unavailable_stats" :
                unitHandle.IsValid() && unitHandle.IsDone ? "mission.m03.guide.load_failed" : "mission.m03.guide.loading") : GameText.Format("mission.m03.guide.stats","",
                unit.MaxHealth,unit.Speed.ToString("0.#",System.Globalization.CultureInfo.InvariantCulture),
                unit.CanAttack ? unit.AttackDamage.ToString() : GameText.Get("mission.m03.guide.unarmed"),unit.CanAttack ? unit.AttackRange : 0,
                MissionGuideClass.GroundSensorRadius(unit),MissionGuideClass.AirSensorRadius(unit),unit.SoldierTransportCapacity,unit.VehicleTransportCapacity);
            body.SetLocalizedValue(GameText.Get(card.CategoryKey)+" · "+GameText.Get(AvailabilityKey((int)card.Availability))+"\n\n"+stats);
            string role=card.Availability==MissionGuideAvailability.Protected ? "protected" :
                MissionGuideClass.GroundSensorRadius(unit)>0 ? "sensor" :
                unit!=null && unit.SoldierTransportCapacity>0 ? "transport" : unit!=null && unit.CanAttack ? "armed" : "unarmed";
            string roleKey=guide==extractionGuide&&(role=="protected"||role=="transport")?"mission.m04.guide.role.":"mission.m03.guide.role.";
            example.SetLocalizedValue(unit==null ? "" : GameText.Get(roleKey+role));
            Set(mistake,guide==extractionGuide?"mission.m04.guide.notice":"mission.m03.guide.reference_notice");
            diagram.SetLocalizedValue(card.Id);
            if(unit!=null) {portrait.sprite=unit.PortraitCardSprite!=null ? unit.PortraitCardSprite : unit.PortraitSprite; portrait.gameObject.SetActive(portrait.sprite!=null);}
        }
        private static void SetTop(V3LocalizedTextBinding binding,float top)
        {var rect=binding.GetComponent<RectTransform>(); rect.anchoredPosition=new Vector2(rect.anchoredPosition.x,-top);}
        private static void Set(V3LocalizedTextBinding text,string key)=>text.SetLocalizedValue(GameText.Get(key));
        private UnitGridAuthoringConfig RequestUnit(MissionGuideClass card)
        {
            string key=card.UnitAsset!=null && card.UnitAsset.RuntimeKeyIsValid() ? card.UnitAsset.AssetGUID : null;
            if(key!=unitKey)
            {
                ReleaseUnit(); unitKey=key;
                if(key!=null) {unitHandle=Addressables.LoadAssetAsync<UnitGridAuthoringConfig>(card.UnitAsset.RuntimeKey); unitHandle.Completed+=UnitReady;}
            }
            return unitHandle.IsValid() && unitHandle.IsDone && unitHandle.Status==AsyncOperationStatus.Succeeded ? unitHandle.Result : null;
        }
        private void UnitReady(AsyncOperationHandle<UnitGridAuthoringConfig> handle)
        {if(isActiveAndEnabled && unitHandle.IsValid() && handle.Equals(unitHandle)) Refresh();}
        private void ReleaseUnit()
        {portrait.sprite=null; if(unitHandle.IsValid()) {unitHandle.Completed-=UnitReady; Addressables.Release(unitHandle);} unitHandle=default; unitKey=null;}
    }
}
