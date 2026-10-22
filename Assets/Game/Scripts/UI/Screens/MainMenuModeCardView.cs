using System;
using Game.Configs;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class MainMenuModeCardView : MonoBehaviour
    {
        [SerializeField] private MainMenuMode mode;
        [SerializeField] private MissionAtlasArtworkView preview;
        [SerializeField] private Sprite operationsPreview;
        [SerializeField] private TMP_Text heading, next, progress, actionLabel, rank, detail;
        [SerializeField] private TMP_Text[] medalCounts;
        [SerializeField] private RectTransform progressFill, progressTrack, iconCell, icon, medalRow;
        [SerializeField] private Button action;
        [SerializeField] private V3GradientGraphic readability, actionFace;
        [SerializeField] private MainMenuModeProgress state;
        private bool expanded;
        private bool layoutDirty=true;
        private string layoutLocale;
        private float width, height;
        public MainMenuMode Mode => mode;
        public MainMenuModeProgress State => state;
        public Sprite PreviewSprite => preview != null ? preview.Comic : null;
        public Texture PreviewTexture => preview!=null?preview.Image.texture:null;
        public Button Action => action;
        private bool Fa => GameLocalization.CurrentLocaleCode == GameLocalization.PersianLocaleCode;
        private Color Accent => mode == MainMenuMode.Operations ? new Color32(25,210,114,255) : new Color32(255,112,24,255);

        public void Configure(MainMenuMode kind, MissionAtlasArtworkView art, Sprite operationArt, TMP_Text title,
            TMP_Text nextText, TMP_Text count, TMP_Text label, TMP_Text mastery, TMP_Text secondary,
            TMP_Text[] counts, RectTransform fill, RectTransform track, RectTransform emblemCell,
            RectTransform emblem, RectTransform medals, Button button, V3GradientGraphic shade, V3GradientGraphic face)
        {
            mode=kind;preview=art;operationsPreview=operationArt;heading=title;next=nextText;progress=count;
            actionLabel=label;rank=mastery;detail=secondary;medalCounts=counts;progressFill=fill;
            progressTrack=track;iconCell=emblemCell;icon=emblem;medalRow=medals;action=button;
            readability=shade;actionFace=face;
        }
        private void OnEnable() { if(action!=null)action.onClick.AddListener(Activate); }
        private void OnDisable() { if(action!=null)action.onClick.RemoveListener(Activate); }
        public void Present(MainMenuModeProgress model)
        {
            state=model;layoutDirty=true;
            Set(heading,T(mode==MainMenuMode.Operations?"OPERATIONS":"SKIRMISH",mode==MainMenuMode.Operations?"عملیات":"درگیری"));
            string name = string.IsNullOrEmpty(model.TitleKey) ? model.Title :
                UiShellRuntimeGateway.Localization.Get(model.TitleKey,model.Title);
            if(mode==MainMenuMode.Skirmish && model.HasNext &&
                SkirmishBattleCatalogConfig.Load() is SkirmishBattleCatalogConfig catalog &&
                catalog.TryGet(model.MissionId,out var catalogEntry) &&
                int.TryParse(model.MissionId.Substring(1),out int number))
                name=QuickCustomScreenView.AtlasMissionName(catalogEntry.ObjectiveId,(number-1)%6);
            string prefix=model.Resume?T("RESUME","ادامه"):model.Replay?T("REPLAY","تکرار"):T("NEXT","بعدی");
            string id = mode==MainMenuMode.Operations && !string.IsNullOrEmpty(model.MissionId)
                ? model.MissionId.Replace("operation.","").ToUpperInvariant() : model.MissionId;
            Set(next, model.HasNext ? prefix + ": " + id + " · " + name :
                T("REVIEW AVAILABLE MISSIONS","مرور ماموریت‌های موجود"));
            Set(progress,model.Available ? model.Completed+"/"+model.Total+" "+T("COMPLETED","تکمیل‌شده") :
                T("PROGRESS UNAVAILABLE","پیشرفت در دسترس نیست"));
            int[] values={model.Recruit,model.Regular,model.Veteran,model.Commander};
            for(int i=0;i<medalCounts.Length;i++)Set(medalCounts[i],model.Available?values[i].ToString():"—");
            string rankName=model.Completed>=60?T("COMMANDER","فرمانده"):model.Completed>=30?T("VETERAN","کهنه‌کار"):
                model.Completed>=15?T("TACTICIAN","تاکتیک‌دان"):model.Completed>=5?T("FIELD OFFICER","افسر میدان"):T("CADET","کارآموز");
            Set(rank,T("MODE RANK","رتبه حالت")+" · "+rankName);
            Set(detail,model.Published+" "+T("RELEASED","منتشرشده")+" · "+T("DEFAULT MISSION MEDALS","مدال‌های ماموریت پیش‌فرض"));
            string verb = model.Resume ? T("RESUME","ادامه") : model.HasNext ? T("CONTINUE","ادامه") : T("OPEN","باز کردن");
            Set(actionLabel, verb+" "+T(mode==MainMenuMode.Operations?"OPERATIONS":"SKIRMISH",mode==MainMenuMode.Operations?"عملیات":"درگیری")+"  ›");
            Texture2D illustration=model.HasNext?MissionAtlasMissionArtworkCatalog.Load()?.Find(model.MissionId):null;
            Sprite sprite=null;
            if(model.HasNext)
            {
                if(mode==MainMenuMode.Operations && model.MissionId=="operation.o001") sprite=operationsPreview;
                else if(mode==MainMenuMode.Skirmish)
                {
                    var registry=SkirmishLibrarySpriteRegistry.Load();
                    if(registry!=null)registry.TryGetSprite(model.PreviewKey,out sprite);
                }
            }
            // No other mission's art substitutes for a missing next-mission thumbnail.
            preview.Set(illustration==null?sprite:null,illustration);
            if(width>0)Layout(width,height,expanded);
        }
        public void Activate()
        {
            // Re-read at activation so an entitlement or result change cannot select stale content.
            Present(MainMenuModeProgressGateway.ReadMode(mode));
            if(UiShellRuntimeGateway.TryReadShellState(out var shell) && shell.IsTransitionRunning)return;
            if(mode==MainMenuMode.Skirmish && state.HasNext) SkirmishLibraryReturn.Queue(state.MissionId);
            UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.OpenMenuRoute,
                mode==MainMenuMode.Operations?UIRoute.Operations:UIRoute.QuickCustomSetup,true);
        }
        public void Layout(float w,float h,bool large)
        {
            if(!layoutDirty && Mathf.Approximately(width,w) && Mathf.Approximately(height,h) &&
                expanded==large && layoutLocale==GameLocalization.CurrentLocaleCode)return;
            width=w;height=h;expanded=large;layoutDirty=false;layoutLocale=GameLocalization.CurrentLocaleCode;
            Place(preview.transform,3,3,w-6,large?h*.43f:Mathf.Min(166,h*.62f));
            float band=large?h*.43f:Mathf.Min(111,h*.41f);
            Place(readability.transform,3,band,w-6,h-band-6);
            readability.Configure(new Color32(10,30,40,248),new Color32(3,13,21,255),Color.clear,0);
            var titleShade=transform.Find("ModeTitleShade");
            if(titleShade!=null){titleShade.gameObject.SetActive(large);Place(titleShade,3,3,w-6,118);}
            float titleY=large?18:band+7;
            Place(iconCell,3,large?3:band,large?96:74,large?110:h-band-3);
            Place(icon,8,large?15:(h-band-3-64)/2,large?80:58,large?80:64);
            Place(heading.rectTransform,large?116:92,titleY,w-(large?140:112),large?60:48);
            Size(heading,large?52:40,large?34:30);heading.color=Color.white;
            float nextY=large?band+18:band+80;
            Place(next.rectTransform,large?24:92,nextY,w-(large?48:110),large?70:32);
            Size(next,large?(Fa?30:38):25,large?(Fa?19:27):20);next.color=Accent;
            next.textWrappingMode=TextWrappingModes.Normal;
            float countY=large?nextY+76:h-68;
            float progressWidth=large?w-48:Mathf.Max(220,w*.30f);
            Place(progress.rectTransform,large?24:92,countY,large?w-48:progressWidth,large?44:36);
            Size(progress,large?34:27,large?27:22);
            Place(progressTrack,large?24:92,large?countY+43:h-24,progressWidth,large?14:10);
            float ratio=state.Total>0?Mathf.Clamp01((float)state.Completed/state.Total):0;
            Place(progressFill,2,2,Mathf.Max(0,(progressWidth-4)*ratio),large?10:6);
            progressFill.gameObject.SetActive(ratio>0);
            float medalsY=large?h-188:h-71;
            float medalsX=large?24:92+progressWidth+14;
            float medalWidth=large?Mathf.Min(350,w*.53f):Mathf.Max(150,w-medalsX-16);
            Place(medalRow,medalsX,medalsY,medalWidth,large?48:46);
            for(int i=0;i<4;i++)
            {
                float cell=medalWidth/4;
                var item=medalRow.GetChild(i) as RectTransform;
                Place(item,i*cell,0,cell,large?48:42);
                Place(item.Find("Star"),0,2,large?40:24,large?40:24);
                Place(medalCounts[i].rectTransform,large?42:26,0,cell-(large?44:28),large?48:31);
                Size(medalCounts[i],large?28:22,large?22:18);
            }
            rank.gameObject.SetActive(large);detail.gameObject.SetActive(large);
            Place(rank.rectTransform,24,h-134,w-48,30);Size(rank,24,20);rank.color=new Color32(231,192,111,255);
            Place(detail.rectTransform,24,countY+61,w-48,26);Size(detail,21,18);detail.color=new Color32(164,192,207,255);
            Place(action.transform,large?20:w-240,large?h-96:band+12,large?w-40:218,large?76:58);
            // Compact mode has one obvious arrow action; the next line keeps the entire name readable.
            if(!large)
            {
                Place(heading.rectTransform,92,band+7,w-348,50);
                Set(actionLabel,T("OPEN","باز کردن")+"  ›");
            }
            if(large)
            {
                string verb=state.Resume?T("RESUME","ادامه"):state.HasNext?T("CONTINUE","ادامه"):T("OPEN","باز کردن");
                Set(actionLabel,verb+" "+T(mode==MainMenuMode.Operations?"OPERATIONS":"SKIRMISH",mode==MainMenuMode.Operations?"عملیات":"درگیری")+"  ›");
            }
            Size(actionLabel,large?34:28,large?26:22);
            actionFace.Configure(new Color32(12,140,65,255),new Color32(6,42,27,255),Accent,2);
            if(mode==MainMenuMode.Skirmish)actionFace.Configure(new Color32(197,70,7,255),new Color32(91,25,4,255),Accent,2);
            preview.Refresh();
        }
        private void Place(Transform t,float x,float y,float w,float h)
        {
            if(t is not RectTransform r)return;
            if(Fa)x=(r.parent as RectTransform).rect.width-x-w;
            r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);
        }
        private void Size(TMP_Text text,float max,float min)
        {
            text.enableAutoSizing=true;text.fontSize=max;text.fontSizeMax=max;text.fontSizeMin=min;
            text.margin=new Vector4(2,0,2,0);text.overflowMode=TextOverflowModes.Ellipsis;
            text.alignment=Fa?TextAlignmentOptions.MidlineRight:TextAlignmentOptions.MidlineLeft;
            // Let the shared Arabic line-box fitter own its bounds and overflow after layout.
            // Replacing its Overflow with Ellipsis can reject the entire Noto Arabic line.
            text.GetComponent<V3LocalizedTextBindingView>()?.SetFontBounds(min,max);
            text.alignment=Fa?TextAlignmentOptions.MidlineRight:TextAlignmentOptions.MidlineLeft;
            if(text==actionLabel){text.alignment=TextAlignmentOptions.Center;text.margin=new Vector4(12,2,12,2);}
        }
        private static void Set(TMP_Text text,string value)
        { if(text!=null)UiLocalizedText.Set(text,value??""); }
        private string T(string en,string fa)=>Fa?fa:en;
    }
}
