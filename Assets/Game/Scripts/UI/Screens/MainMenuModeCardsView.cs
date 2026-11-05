using Game.Configs;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class MainMenuModeCardsView : MonoBehaviour
    {
        [SerializeField] private MainMenuCampaignCardView campaign;
        [SerializeField] private MainMenuModeCardView operations, skirmish;
        [SerializeField] private TMP_Text completeCount;
        [SerializeField] private MissionAtlasArtworkView completePanorama;
        [SerializeField] private RectTransform completeEmblem;
        private MainMenuV3SectionLayoutView frame;
        private double nextRead;
        private bool? fixtureCompletion;
        public bool Expanded { get; private set; }
        public void Configure(MainMenuCampaignCardView hero,MainMenuModeCardView op,MainMenuModeCardView sk,
            TMP_Text count,MissionAtlasArtworkView panorama,RectTransform emblem)
        {campaign=hero;operations=op;skirmish=sk;completeCount=count;completePanorama=panorama;completeEmblem=emblem;}
        private void OnEnable()
        {
            frame=GetComponent<MainMenuV3SectionLayoutView>();
            if(frame!=null)frame.LayoutApplied+=ApplyLayout;
            nextRead=0;
        }
        private void OnDisable(){if(frame!=null)frame.LayoutApplied-=ApplyLayout;}
        private void LateUpdate()
        {
            if(Time.unscaledTimeAsDouble>=nextRead){nextRead=Time.unscaledTimeAsDouble+.75;Refresh();}
            ApplyLayout();
        }
        public void Refresh()
        {
            operations.Present(MainMenuModeProgressGateway.ReadMode(MainMenuMode.Operations));
            skirmish.Present(MainMenuModeProgressGateway.ReadMode(MainMenuMode.Skirmish));
            ApplyLayout();
        }
        public void PresentForReview(bool complete,MainMenuModeProgress op,MainMenuModeProgress sk)
        {fixtureCompletion=complete;operations.Present(op);skirmish.Present(sk);ApplyLayout();}
        public void ApplyLayout()
        {
            if(campaign==null)return;
            bool full=fixtureCompletion??(UiShellRuntimeGateway.TryReadCampaignOperations(out var model)&&model.IsValid&&
                model.FullCampaignRegistered&&model.AllRequiredMissionsCompleted);
            Expanded=full;
            var root=(RectTransform)transform;
            float extraX=Mathf.Max(0,root.rect.width-1672),extraY=Mathf.Max(0,root.rect.height-941);
            float width=1270+extraX,cardW=(width-12)/2;
            float top=full?260:620+extraY;
            float height=full?661+extraY:301;
            Place(campaign.transform,14,108,width,full?140:500+extraY);
            Place(operations.transform,14,top,cardW,height);
            Place(skirmish.transform,26+cardW,top,cardW,height);
            operations.Layout(cardW,height,full);skirmish.Layout(cardW,height,full);
            completeCount.gameObject.SetActive(full);completePanorama.gameObject.SetActive(full);completeEmblem.gameObject.SetActive(full);
            var hero=campaign.transform;
            var shade=hero.Find("CampaignReadability")?.GetComponent<V3GradientGraphic>();
            if(shade!=null)shade.Configure(full?new Color(0.01f,.035f,.055f,.42f):new Color(0,0,0,.05f),
                full?new Color(0.01f,.025f,.04f,.64f):new Color(0,0,0,.95f),new Color32(62,83,93,255),2);
            SetActive(hero,"CampaignLabel",!full);SetActive(hero,"Purpose",!full);
            if(!full && !campaign.CompletionVisible)SetActive(hero,"Chapter",true);
            if(full)
            {
                SetActive(hero,"Chapter",false);SetActive(hero,"CampaignArt",false);
                Place(completePanorama.transform,3,3,width-6,134);
                completePanorama.Refresh();
                Place(completeEmblem,26,38,64,64);
                Place(hero.Find("Title"),112,19,width-650,62);
                Place(completeCount.transform,112,82,width-650,35);
                UiLocalizedText.Set(completeCount,"25/25 "+T("MISSIONS","ماموریت"));
                var title=hero.Find("Title")?.GetComponent<TMP_Text>();
                if(title!=null){UiLocalizedText.Set(title,T("CAMPAIGN COMPLETE","کارزار تکمیل شد"));Font(title,46,32);}
                Place(hero.Find("ContinueButton"),width-502,14,480,72);
                var replayLabel=hero.Find("ContinueButton/Label")?.GetComponent<TMP_Text>();
                if(replayLabel!=null)replayLabel.margin=new Vector4(24,0,24,0);
                UiLocalizedText.Set(hero.Find("ContinueButton/Label")?.GetComponent<TMP_Text>(),T("REPLAY CHAPTERS  ›","تکرار فصل‌ها  ‹"));
                Place(hero.Find("StoryArchiveButton"),width-502,96,480,32);
            }
            else
            {
                SetActive(hero,"CampaignArt",true);
                var continueLabel=hero.Find("ContinueButton/Label")?.GetComponent<TMP_Text>();
                if(continueLabel!=null)continueLabel.margin=new Vector4(18,0,18,0);
                Place(hero.Find("CampaignLabel"),32,151+extraY,width-64,42);
                Place(hero.Find("Chapter"),32,193+extraY,width-64,40);
                Place(hero.Find("Title"),32,233+extraY,width-64,72);
                Place(hero.Find("Purpose"),32,309+extraY,width-64,66);
                var purpose=hero.Find("Purpose")?.GetComponent<TMP_Text>();
                if(purpose!=null)
                {
                    purpose.textWrappingMode=TextWrappingModes.Normal;
                    purpose.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(Fa?18:22,Fa?24:27);
                }
                Place(hero.Find("ContinueButton"),32,396+extraY,650,82);
                Place(hero.Find("StoryArchiveButton"),710,396+extraY,360,82);
                var title=hero.Find("Title")?.GetComponent<TMP_Text>();if(title!=null)Font(title,60,36);
            }
        }
        private bool Fa=>GameLocalization.CurrentLocaleCode==GameLocalization.PersianLocaleCode;
        private string T(string en,string fa)=>Fa?fa:en;
        private void Place(Transform target,float x,float y,float w,float h)
        {
            if(target is not RectTransform rect)return;
            if(Fa)x=(rect.parent as RectTransform).rect.width-x-w;
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);
        }
        private static void Font(TMP_Text t,float max,float min)
        {t.fontSize=max;t.enableAutoSizing=true;t.fontSizeMax=max;t.fontSizeMin=min;t.margin=new Vector4(4,0,4,0);}
        private static void SetActive(Transform t,string name,bool value)
        {var child=t.Find(name);if(child!=null)child.gameObject.SetActive(value);}
    }
}
