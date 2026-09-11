using System;
using System.Collections.Generic;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    // Serialized, bounded presentation. No world discovery, simulation mutation or duplicated balance table.
    public sealed class MissionFieldGuideView : MonoBehaviour
    {
        [SerializeField] private ScriptableObject guide, extractionGuide;
        private ScriptableObject defenseGuide;
        private IUiMissionGuideSession guideSession;
        private UiMissionGuideCatalog content;
        private bool extractionContext;
        [SerializeField] private Button topicsButton,classesButton,previousButton,nextButton,closeButton,filterButton,radioButton;
        [SerializeField] private Image radioPanel;
        [SerializeField] private Sprite radio16x9,radio20x9;
        [SerializeField] private TMP_InputField search;
        [SerializeField] private Image portrait;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private RectTransform exampleCard,mistakeCard,diagramCard;
        [SerializeField] private GameObject topicsSelected,classesSelected,radioSelected;
        [SerializeField] private V3LocalizedTextBindingView title,body,example,mistake,diagram,page,filterLabel;
        private readonly List<int> matches=new(57);
        private bool classes;
        private bool radio;
        private bool largeText;
        private int index,filter=-1;
        private V3LocalizedTextBindingView scopedTitle;
        public int ResidentClassCount=>guideSession?.ResidentClassCount ?? 0;
        public bool ClassLoadPending=>guideSession?.ClassLoadPending ?? false;
        public bool ClassLoadFailed=>guideSession?.ClassLoadFailed ?? false;
        public int VisibleClassCount=>matches.Count;
        public UiMissionGuideCatalog Guide=>content;

        private void OnEnable()
        {
            if(defenseGuide==null)defenseGuide=guide;
            guide=UiShellRuntimeGateway.IsExtractionGuideContext() && extractionGuide!=null ? extractionGuide : defenseGuide;
            extractionContext=guide==extractionGuide;
            guideSession=UiShellRuntimeGateway.OpenMissionGuide(guide);
            content=guideSession?.Catalog;
            if(guideSession!=null)guideSession.Changed+=Refresh;
            radio=classes=false; index=0; filter=-1;
            largeText=SettingsService.Load().Accessibility.LargeText;
            foreach(var binding in GetComponentsInChildren<V3LocalizedTextBindingView>(true))
            {binding.SetPresentationScale(largeText ? 1.15f : 1);if(binding.LocalizationKey=="mission.m03.guide.title")scopedTitle=binding;}
            RefreshScopedTitle();
            topicsButton.onClick.AddListener(ShowTopics); classesButton.onClick.AddListener(ShowClasses);
            if(radioButton!=null) radioButton.onClick.AddListener(ShowRadio);
            previousButton.onClick.AddListener(Previous); nextButton.onClick.AddListener(Next); closeButton.onClick.AddListener(Close);
            filterButton.onClick.AddListener(CycleFilter); search.onValueChanged.AddListener(Search);
            UiShellRuntimeGateway.Localization.LocaleChanged+=LocaleChanged;
            RebuildMatches(); Refresh();
        }
        private void OnDisable()
        {
            topicsButton.onClick.RemoveListener(ShowTopics); classesButton.onClick.RemoveListener(ShowClasses);
            if(radioButton!=null) radioButton.onClick.RemoveListener(ShowRadio);
            previousButton.onClick.RemoveListener(Previous); nextButton.onClick.RemoveListener(Next); closeButton.onClick.RemoveListener(Close);
            filterButton.onClick.RemoveListener(CycleFilter); search.onValueChanged.RemoveListener(Search);
            UiShellRuntimeGateway.Localization.LocaleChanged-=LocaleChanged;
            ReleaseUnit();
            if(guideSession!=null) {guideSession.Changed-=Refresh; guideSession.Dispose(); guideSession=null;}
            content=null;
        }
        private void LocaleChanged() {RefreshScopedTitle();RebuildMatches(); Refresh();}
        private void RefreshScopedTitle()=>scopedTitle?.SetLocalizedValue(UiShellRuntimeGateway.Localization.Get(extractionContext?"mission.m04.guide.title":"mission.m03.guide.title"));
        private string AvailabilityKey(int value)=>extractionContext&&value<4?"mission.m04.guide.availability."+value:"mission.m03.guide.availability."+value;
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
            matches.Clear(); if(content==null) return;
            string query=search.text.Trim();
            for(int i=0;i<content.Classes.Length;i++)
            {
                var entry=content.Classes[i];
                if(filter>=0 && (int)entry.Availability!=filter) continue;
                if(query.Length!=0 && entry.Id.IndexOf(query,StringComparison.OrdinalIgnoreCase)<0 &&
                    UiShellRuntimeGateway.Localization.Get(entry.NameKey,entry.Id).IndexOf(query,StringComparison.OrdinalIgnoreCase)<0) continue;
                matches.Add(i);
            }
        }
        public void Refresh()
        {
            if(content==null) return;
            LayoutReadingPage();
            if(topicsSelected!=null)topicsSelected.SetActive(!classes && !radio);
            if(classesSelected!=null)classesSelected.SetActive(classes);
            if(radioSelected!=null)radioSelected.SetActive(radio);
            search.gameObject.SetActive(classes); filterButton.gameObject.SetActive(classes);
            if(radioButton!=null) {radioButton.gameObject.SetActive(!extractionContext); radioButton.interactable=UiShellRuntimeGateway.TryReadMissionRadioArchive();}
            SetTabState(topicsButton,!classes&&!radio);SetTabState(classesButton,classes);SetTabState(radioButton,radio);
            if(radioPanel!=null) radioPanel.gameObject.SetActive(radio);
            portrait.gameObject.SetActive(false);
            if(radio)
            {
                ReleaseUnit(); previousButton.interactable=nextButton.interactable=false;
                Set(title,"mission.m03.guide.radio"); body.SetLocalizedValue(UiShellRuntimeGateway.Localization.Get(content.RadioTextKey));
                example.SetLocalizedValue(""); mistake.SetLocalizedValue(""); diagram.SetLocalizedValue(""); page.SetLocalizedValue("1 / 1");
                float radioTop=96+FitText(body)+32;
                var radioRect=radioPanel.rectTransform; radioRect.anchoredPosition=new Vector2(18,-radioTop);
                radioRect.sizeDelta=new Vector2(scroll.viewport.rect.width-36,(scroll.viewport.rect.width-36)*.45f);
                scroll.content.sizeDelta=new Vector2(scroll.content.sizeDelta.x,radioTop+radioRect.sizeDelta.y+24);
                if(radioPanel!=null) radioPanel.sprite=Screen.width/(float)Mathf.Max(1,Screen.height)>2 ? radio20x9 : radio16x9;
                RefreshCards(); return;
            }
            int count=classes ? matches.Count : content.Topics.Length;
            index=Mathf.Clamp(index,0,Mathf.Max(0,count-1));
            previousButton.interactable=index>0; nextButton.interactable=index+1<count;
            page.SetLocalizedValue(count==0 ? "0 / 0" : (index+1)+" / "+count);
            filterLabel.SetLocalizedValue(UiShellRuntimeGateway.Localization.Get(filter<0 ? "mission.m03.guide.all" : AvailabilityKey(filter)));
            if(!classes)
            {
                ReleaseUnit();
                var topic=content.Topics[index];
                Set(title,topic.TitleKey); Set(body,topic.BodyKey);
                example.SetLocalizedValue(UiShellRuntimeGateway.Localization.Get("mission.m03.guide.example")+"\n"+UiShellRuntimeGateway.Localization.Get(topic.ExampleKey));
                mistake.SetLocalizedValue(UiShellRuntimeGateway.Localization.Get("mission.m03.guide.mistake")+"\n"+UiShellRuntimeGateway.Localization.Get(topic.MistakeKey));
                Set(diagram,topic.DiagramKey); FitReadingCopy(); return;
            }
            if(count==0) {ReleaseUnit(); Set(title,"mission.m03.guide.no_match"); body.SetLocalizedValue(""); example.SetLocalizedValue(""); mistake.SetLocalizedValue(""); diagram.SetLocalizedValue(""); return;}
            var card=content.Classes[matches[index]];
            Set(title,card.NameKey);
            var unit=RequestUnit(card);
            string stats=unit==null ? UiShellRuntimeGateway.Localization.Get(card.Availability==UiMissionGuideAvailability.Unavailable ? "mission.m03.guide.unavailable_stats" :
                ClassLoadFailed ? "mission.m03.guide.load_failed" : "mission.m03.guide.loading") : UiShellRuntimeGateway.Localization.Format("mission.m03.guide.stats","",
                unit.MaxHealth,unit.Speed.ToString("0.#",System.Globalization.CultureInfo.InvariantCulture),
                unit.CanAttack ? unit.AttackDamage.ToString() : UiShellRuntimeGateway.Localization.Get("mission.m03.guide.unarmed"),unit.CanAttack ? unit.AttackRange : 0,
                (unit?.GroundSensorRadius ?? 0),(unit?.AirSensorRadius ?? 0),unit.SoldierTransportCapacity,unit.VehicleTransportCapacity);
            body.SetLocalizedValue(UiShellRuntimeGateway.Localization.Get(card.CategoryKey)+" · "+UiShellRuntimeGateway.Localization.Get(AvailabilityKey((int)card.Availability))+"\n\n"+stats);
            string role=card.Availability==UiMissionGuideAvailability.Protected ? "protected" :
                (unit?.GroundSensorRadius ?? 0)>0 ? "sensor" :
                unit!=null && unit.SoldierTransportCapacity>0 ? "transport" : unit!=null && unit.CanAttack ? "armed" : "unarmed";
            string roleKey=extractionContext&&(role=="protected"||role=="transport")?"mission.m04.guide.role.":"mission.m03.guide.role.";
            example.SetLocalizedValue(unit==null ? "" : UiShellRuntimeGateway.Localization.Get(roleKey+role));
            Set(mistake,extractionContext?"mission.m04.guide.notice":"mission.m03.guide.reference_notice");
            diagram.SetLocalizedValue(card.Id);
            FitReadingCopy();
            if(unit!=null) {portrait.sprite=unit.PortraitCardSprite!=null ? unit.PortraitCardSprite : unit.PortraitSprite; portrait.gameObject.SetActive(portrait.sprite!=null);}
        }
        private static void SetTabState(Button button,bool selected)
        {
            if(button==null)return;
            var group=button.GetComponent<CanvasGroup>();if(group!=null)group.alpha=button.interactable?1:.45f;
            var icon=button.GetComponentInChildren<Image>(true);
            if(icon!=null)icon.color=selected?new Color32(255,194,17,255):new Color32(170,180,182,255);
        }
        private void LayoutReadingPage()
        {
            if(scroll==null)return;
            float top=classes?208:110;
            scroll.viewport.anchoredPosition=new Vector2(236,-top);
            scroll.viewport.sizeDelta=new Vector2(1162,796-top);
            if(scroll.verticalScrollbar!=null)
            {
                var track=(RectTransform)scroll.verticalScrollbar.transform;
                track.anchoredPosition=new Vector2(1406,-top);track.sizeDelta=new Vector2(32,796-top);
            }
            float width=scroll.viewport.rect.width;
            float copyWidth=width-36, cardWidth=width-68;
            LayoutText(title,18,8,copyWidth,76);
            LayoutText(body,18,96,classes?copyWidth-256:copyWidth,classes?(largeText?420:350):136);
            portrait.rectTransform.anchoredPosition=new Vector2(width-238,-110);
            if(classes)
            {
                LayoutText(example,34,478,cardWidth,142);
                LayoutText(mistake,34,650,cardWidth,142);
                LayoutText(diagram,34,824,cardWidth,80);
            }
            else
            {
                float half=(width-104)/2;
                bool rtl=UiShellRuntimeGateway.Localization.IsRightToLeft;
                LayoutText(example,rtl?70+half:34,262,half,132);
                LayoutText(mistake,rtl?34:70+half,262,half,132);
                LayoutText(diagram,34,444,cardWidth,62);
            }
            RefreshCards();scroll.StopMovement();scroll.verticalNormalizedPosition=1;
        }
        private void FitReadingCopy()
        {
            float bodyHeight=FitText(body);
            float cardsTop=body.GetComponent<RectTransform>().anchoredPosition.y*-1+bodyHeight+42;
            SetTop(example,cardsTop);float exampleHeight=FitText(example);
            SetTop(mistake,classes?cardsTop+exampleHeight+42:cardsTop);float mistakeHeight=FitText(mistake);
            float diagramTop=classes?cardsTop+exampleHeight+mistakeHeight+84:cardsTop+Mathf.Max(exampleHeight,mistakeHeight)+42;
            SetTop(diagram,diagramTop);float diagramHeight=FitText(diagram);
            scroll.content.sizeDelta=new Vector2(scroll.content.sizeDelta.x,Mathf.Max(scroll.viewport.rect.height,diagramTop+diagramHeight+20));
            RefreshCards();
        }
        private static float FitText(V3LocalizedTextBindingView binding)
        {
            var text=binding.GetComponent<TMP_Text>();text.ForceMeshUpdate(true,true);
            var rect=text.rectTransform;float height=Mathf.Max(rect.sizeDelta.y,text.GetPreferredValues(text.text,rect.rect.width,float.PositiveInfinity).y+6);
            rect.sizeDelta=new Vector2(rect.sizeDelta.x,height);return height;
        }
        private static void LayoutText(V3LocalizedTextBindingView binding,float x,float y,float width,float height)
        {var rect=binding.GetComponent<RectTransform>();rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);}
        private void RefreshCards()
        {FitCard(exampleCard,example);FitCard(mistakeCard,mistake);FitCard(diagramCard,diagram);}
        private void FitCard(RectTransform card,V3LocalizedTextBindingView binding)
        {
            if(card==null)return;card.gameObject.SetActive(!radio);
            var text=binding.GetComponent<RectTransform>();card.anchoredPosition=text.anchoredPosition+new Vector2(-16,14);card.sizeDelta=text.sizeDelta+new Vector2(32,28);
        }
        private static void SetTop(V3LocalizedTextBindingView binding,float top)
        {var rect=binding.GetComponent<RectTransform>(); rect.anchoredPosition=new Vector2(rect.anchoredPosition.x,-top);}
        private static void Set(V3LocalizedTextBindingView text,string key)=>text.SetLocalizedValue(UiShellRuntimeGateway.Localization.Get(key));
        private UiMissionGuideUnitModel RequestUnit(UiMissionGuideClass card) => guideSession?.RequestUnit(in card);
        private void ReleaseUnit()
        {portrait.sprite=null; guideSession?.ReleaseUnit();}
    }
}
