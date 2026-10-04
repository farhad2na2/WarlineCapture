using System;
using System.Linq;
using Game.Configs;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    // Presentation follows the selected mission; deployment remains owned by the existing screen.
    [DefaultExecutionOrder(650)]
    public sealed class CampaignInnerMissionView : MonoBehaviour
    {
        [SerializeField] private MissionBriefingScreenView briefing;
        [SerializeField] private TMP_Text chapter, title, summary, objectives, conditions, intel, rewards, goals, forces;
        [SerializeField] private RawImage artwork;
        [SerializeField] private string[] artPrefixes;
        [SerializeField] private Texture[] artTextures;
        private IGameTextResolver gameText = FallbackGameTextResolver.Instance;
        public void BindGameTextResolver(IGameTextResolver resolver) { gameText=resolver??FallbackGameTextResolver.Instance;lastVersion=0; }
        private uint lastVersion; private string lastLocale, lastMission, lastSummary;
        public void Configure(MissionBriefingScreenView source, TMP_Text chapterText, TMP_Text titleText, TMP_Text summaryText,
            TMP_Text objectiveText, TMP_Text conditionText, TMP_Text intelText, TMP_Text rewardText, TMP_Text goalText,
            TMP_Text forceText, RawImage art, string[] prefixes, Texture[] textures)
        {
            briefing=source;chapter=chapterText;title=titleText;summary=summaryText;objectives=objectiveText;
            conditions=conditionText;intel=intelText;rewards=rewardText;goals=goalText;forces=forceText;artwork=art;artPrefixes=prefixes;artTextures=textures;
        }
        private static string Source(TMP_Text text) => text == null ? "" : text.GetComponent<V3LocalizedTextBindingView>()?.SourceValue ?? text.text;
        private static void Set(TMP_Text target,string copy)
        { if(target!=null) UiLocalizedText.Set(target,copy); }
        private void LateUpdate()
        {
            if(!UiShellRuntimeGateway.TryReadMissionBriefing(out var m)||!m.IsValid) return;
            string currentSummary=briefing!=null?Source(briefing.MissionSummary):"";
            if(lastVersion==m.Version&&lastLocale==GameLocalization.CurrentLocaleCode&&lastMission==m.MissionId&&lastSummary==currentSummary)return;
            lastVersion=m.Version;lastLocale=GameLocalization.CurrentLocaleCode;lastMission=m.MissionId;lastSummary=currentSummary;
            string prefix=m.DisplayNameKey.EndsWith(".name",StringComparison.Ordinal)?m.DisplayNameKey.Substring(0,m.DisplayNameKey.Length-4):"";
            int ch=1;var parts=m.MissionId.Split('.');if(parts.Length>1&&parts[1].StartsWith("ch"))int.TryParse(parts[1].Substring(2),out ch);
            string[] en={"FIRST RESPONSE","CITY LIFELINES","HIDDEN NETWORK","AIR AND ARMOR","CITYWIDE COMMAND"};
            string[] fa={"واکنش نخست","شریان‌های شهر","شبکهٔ پنهان","هوا و زره","فرماندهی سراسری شهر"};
            bool rtl=GameLocalization.IsRightToLeft;
            Set(chapter,(rtl?"فصل ":"CHAPTER ")+ch+" · "+(rtl?fa:en)[Math.Clamp(ch-1,0,4)]);
            Set(title,gameText.Get(m.DisplayNameKey,MissionBriefingScreenView.MissionTitleFromId(m.MissionId)));
            Set(summary,briefing!=null?GameLocalization.GetBySource(Source(briefing.MissionSummary)):gameText.Get(m.DisplaySummaryKey,MissionBriefingScreenView.SummaryFallback(m.MissionId)));
            Set(objectives,briefing!=null?string.Join("\n\n",briefing.ObjectiveLabels.Where(t=>t!=null&&!string.IsNullOrWhiteSpace(t.text)).Take(m.Objectives.Length).Select(t=>"• "+GameLocalization.GetBySource(Source(t)))):
                string.Join("\n\n",m.Objectives.Select(o=>"• "+MissionBriefingScreenView.FormatObjective(o,gameText))));
            string[] c={"resources","forces","deadline"};
            string[] values=new string[3];
            for(int i=0;i<3;i++) values[i]=briefing!=null&&briefing.ConditionLabels.Length>i?GameLocalization.GetBySource(Source(briefing.ConditionLabels[i])):GameLocalization.Get(prefix+c[i]);
            if(prefix=="mission.m01."||prefix=="mission.m02.")
            {
                values[0]=rtl?$"اعتبار: {m.StartingCredits} · مصالح: {m.StartingMaterials}":$"CREDITS  {m.StartingCredits} · MATERIALS  {m.StartingMaterials}";
                values[1]=rtl?"گروه فرماندهی":"COMMAND SQUAD";
                values[2]=m.BuildingDisabled?(rtl?"ساخت‌وساز در این ماموریت غیرفعال است.":"Building is disabled for this mission."):
                    (rtl?$"ساختمان‌های مجاز: {m.AllowedBuildingCount}":$"AUTHORIZED BUILDINGS  {m.AllowedBuildingCount}");
            }
            Set(conditions,string.Join("\n\n",values.Where(v=>!string.IsNullOrWhiteSpace(v))));
            Set(forces,values[1]);
            Set(intel,briefing!=null?GameLocalization.GetBySource(Source(briefing.EnemyIntelLabel)):GameLocalization.Get(prefix+"enemy_intel"));
            Set(rewards,briefing!=null?string.Join(" · ",briefing.RewardLabels.Zip(briefing.RewardValues,(l,v)=>(GameLocalization.GetBySource(Source(l))+" "+GameLocalization.GetBySource(Source(v))).Trim()).Where(value=>!string.IsNullOrWhiteSpace(value))):
                string.Join(" · ",m.Rewards.Select(r=>GameLocalization.Get(r.DisplayTextKey)+" "+r.Amount)));
            var star=Enumerable.Range(1,3).Select(i=>GameLocalization.Get(prefix+"star."+i)).Where(v=>!string.IsNullOrWhiteSpace(v)).ToArray();
            if(prefix=="mission.m01.")star=rtl?new[]{"ماموریت را کامل کنید","هیچ نیرویی از دست نرود","در کمتر از ۴ دقیقه تمام کنید"}:new[]{"Complete the mission","Lose no squad members","Finish within 4 minutes"};
            if(prefix=="mission.m02.")star=rtl?new[]{"ماموریت را کامل کنید","همهٔ غیرنظامیان زنده بمانند","پایگاه را در کمتر از ۵ دقیقه بسازید"}:new[]{"Complete the mission","Keep all civilians safe","Build the base within 5 minutes"};
            if(prefix=="mission.m03.")star=new[]{"complete","civilians_safe","post_undamaged"}.Select(k=>GameLocalization.Get(prefix+"star."+k)).ToArray();
            Set(goals,string.Join("\n\n",star.Select((v,i)=>(i+1)+". "+v)));
            if(artwork!=null)
            {
                if(briefing!=null) artwork.texture=briefing.MissionArtImage.texture;
                else
                {
                    int index=artPrefixes==null?-1:Array.IndexOf(artPrefixes,prefix);
                    Texture selected=index>=0&&index<artTextures.Length?artTextures[index]:null;
                    if(selected==null&&parts.Length>2&&parts[2].StartsWith("m")&&int.TryParse(parts[2].Substring(1),out int number))
                    {var comic=FutureMissionComicCatalog.Find(ch,number);if(comic!=null)selected=Resources.Load<Texture2D>("FutureMissionComics/"+comic.image);}
                    artwork.texture=selected;
                }
                if(artwork.texture!=null&&artwork.TryGetComponent<AspectRatioFitter>(out var fit))fit.aspectRatio=(float)artwork.texture.width/artwork.texture.height;
            }
        }
    }
}
