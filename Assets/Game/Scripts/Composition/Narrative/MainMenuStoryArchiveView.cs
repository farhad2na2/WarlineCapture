using System;
using System.Collections.Generic;
using System.Linq;
using Game.UI.Runtime;
using Game.Configs;
using Game.Missions.Contracts;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Composition
{
    // Read-only playback on the existing narrative canvas; no mission or reward handoff.
    public sealed class MainMenuStoryArchiveView : MonoBehaviour
    {
        [SerializeField] private Button openButton;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private NarrativeSequenceConfig[] supplementalSequences;
        private GameObject overlay;
        private readonly FirstLaunchNarrativeSequencePresentationSystemHelper player = new();
        private FutureMissionComicPreviewController bookends;
        private NarrativeSequenceView narrative;
        private MenuBootstrapView bootstrap;
        private bool playing;
        private string locale;

        public bool IsOpen => overlay != null || playing;
        public bool IsPlaying => playing;
        public void Configure(Button button, TMP_FontAsset bodyFont, NarrativeSequenceConfig[] additionalSequences) { openButton=button; font=bodyFont; supplementalSequences=additionalSequences; }
        private void OnEnable() => openButton?.onClick.AddListener(Open);
        private void OnDisable() { openButton?.onClick.RemoveListener(Open); Close(); }
        public static bool IsMissionEarned(uint completed, int index) => index >= 0 && index < CampaignMissionSequence.RegisteredMissionCount && (completed & (1u << index)) != 0;
        public static bool IsChapterEarned(uint completed, int chapter)
        {
            int start=(chapter-1)*5;
            if(start<0 || start+5>CampaignMissionSequence.RegisteredMissionCount) return false;
            uint required=31u << start;
            return (completed & required)==required;
        }
        public void Open()
        {
            if(IsOpen || !UiShellRuntimeGateway.TryReadCampaignOperations(out var model) || !model.AllRequiredMissionsCompleted) return;
            bootstrap=FindAnyObjectByType<MenuBootstrapView>(FindObjectsInactive.Include);
            narrative=bootstrap?.FirstLaunchNarrativeView;
            BuildChooser();
        }
        private void BuildChooser()
        {
            if(overlay!=null) Destroy(overlay);
            locale=GameLocalization.CurrentLocaleCode;
            overlay=new GameObject("CompletedStoryArchive",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=overlay.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=4000;
            var scaler=overlay.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1672,941); scaler.matchWidthOrHeight=1;
            var backdrop=Rect("Backdrop",overlay.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero); backdrop.gameObject.AddComponent<Image>().color=new Color(0,0,0,.85f);
            var panel=Rect("Panel",overlay.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(1180,800),Vector2.zero);
            panel.gameObject.AddComponent<V3GradientGraphic>().Configure(new Color32(17,32,40,255),new Color32(4,12,20,255),new Color32(0,185,236,255),3);
            Text("Title",panel,Local("ui.home.story_archive","STORY ARCHIVE"),36,new Vector2(1080,60),new Vector2(0,350));
            Button("Close",panel,Local("ui.home.archive.close","CLOSE"),new Vector2(270,78),new Vector2(0,-340),Close);
            var viewport=Rect("Viewport",panel,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(1090,570),Vector2.zero);
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.vertical=true;scroll.viewport=viewport;
            var rows=new List<Action<RectTransform,int>>();
            if(UiShellRuntimeGateway.TryReadCampaignOperations(out var model))
            {
                for(int i=0;i<CampaignMissionSequence.RegisteredMissionCount;i++)
                {
                    if(!IsMissionEarned(model.CompletedMissionMask,i)) continue;
                    int index=i;
                    rows.Add((parent,row)=>BuildMissionRow(parent,row,index));
                }
                for(int chapter=1;chapter<=5;chapter++)
                {
                    if(!IsChapterEarned(model.CompletedMissionMask,chapter)) continue;
                    string sequence=$"seq.ch{chapter:00}.close.protocol_fragment_{chapter:00}";
                    int earnedChapter=chapter;
                    string opening = chapter switch { 3=>"hidden_network",4=>"air_and_armor",5=>"citywide_command",_=>null };
                    if(opening!=null && chapter != 5)
                    {
                        string openingSequence=$"seq.ch{chapter:00}.open.{opening}";
                        rows.Add((parent,row)=>Button("ChapterOpen"+earnedChapter,parent,UiShellRuntimeGateway.Localization.Format("ui.home.archive.chapter_open","CHAPTER {0} • OPENING",earnedChapter),new Vector2(1050,78),Vector2.zero,()=>PlayBookend(openingSequence,earnedChapter,false)));
                    }
                    rows.Add((parent,row)=>Button("ChapterClose"+earnedChapter,parent,UiShellRuntimeGateway.Localization.Format("ui.home.archive.chapter_close","CHAPTER {0} • CONCLUSION",earnedChapter),new Vector2(1050,78),Vector2.zero,()=>PlayBookend(sequence,earnedChapter,false)));
                }
                if (IsMissionEarned(model.CompletedMissionMask, 19))
                {
                    string[] evidenceKeys = { "fragment4", "authority", "two_keys" };
                    string[] evidenceTitles = { "PROTOCOL FRAGMENT IV", "CAPTURED AUTHORITY PACKAGE", "TWO-KEY AUTHORIZATION DIAGRAM" };
                    int[] evidenceStates = { 0, 0, 3 };
                    for (int e = 0; e < evidenceKeys.Length; e++)
                    {
                        string key = evidenceKeys[e], title = evidenceTitles[e]; int state = evidenceStates[e];
                        rows.Add((parent,row)=>Button("ArmorEvidence"+key,parent,Local("ui.home.archive.armor_break."+key,title),new Vector2(1050,78),Vector2.zero,
                            ()=>PlayRegistered("seq.ch04.close.protocol_fragment_04", "ArmorBreak-close-"+state)));
                    }
                }
                if (IsMissionEarned(model.CompletedMissionMask, CampaignMissionSequence.IndexOf(CampaignMissionSequence.CitywideAlert)))
                {
                    rows.Add((parent,row)=>Button("ChapterOpen5",parent,UiShellRuntimeGateway.Localization.Format("ui.home.archive.chapter_open","CHAPTER {0} • OPENING",5),new Vector2(1050,78),Vector2.zero,
                        ()=>PlayRegistered("seq.ch05.open.citywide_command", null)));
                    rows.Add((parent,row)=>Button("CitywideRelayEvidence",parent,Local("ui.home.archive.citywide_alert.relay_timing","CIVIC RELAY ATTACK TIMING"),new Vector2(1050,78),Vector2.zero,
                        ()=>PlayRegistered("seq.ch05.m01.debrief", "CitywideAlert-debrief-1")));
                }
                if (IsMissionEarned(model.CompletedMissionMask, CampaignMissionSequence.IndexOf(CampaignMissionSequence.TrustUnderFire)))
                    rows.Add((parent,row)=>Button("TrustRelayEvidence",parent,Local("ui.home.archive.trust_under_fire.relay","BROADCAST COMPOUND SIGNAL"),new Vector2(1050,78),Vector2.zero,
                        ()=>PlayRegistered("seq.ch05.m02.debrief", "TrustUnderFire-debrief-1")));
                if(IsMissionEarned(model.CompletedMissionMask,CampaignMissionSequence.IndexOf(CampaignMissionSequence.CommandNode)))
                {
                    string[] keys={"fragment5","audit","governance","emphasis","postscript"};
                    string[] labels={"PROTOCOL FRAGMENT V","COMPLETE QASSEM AUDIT","SHARED BOUNDED GOVERNANCE","RECORDED RECOVERY EMPHASIS","RECOVERY WATCH"};
                    for(int i=0;i<keys.Length;i++)
                    {
                        int choice=i;string key=keys[i],label=labels[i];
                        rows.Add((parent,row)=>Button("CommandNodeArchive"+key,parent,Local("ui.home.archive.command_node."+key,label),new Vector2(1050,78),Vector2.zero,
                            ()=>{if(choice==3)PlayRecordedFinaleEmphasis();else PlayRegistered(choice==4?"seq.campaign.postscript.recovery_watch":"seq.ch05.close.protocol_fragment_05",choice switch{1=>"CommandNode-close-0",2=>"CommandNode-close-3",_=>null});}));
                    }
                }
                if(model.FullCampaignRegistered && model.AllRequiredMissionsCompleted)
                    rows.Add((parent,row)=>Button("Epilogue",parent,Local("ui.home.complete","CAMPAIGN COMPLETE"),new Vector2(1050,78),Vector2.zero,()=>PlayBookend("seq.campaign.epilogue.canonical",5,true)));
            }
            var content=Rect("Content",viewport,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(1090,Mathf.Max(570,rows.Count*90)),Vector2.zero);content.pivot=new Vector2(.5f,1);scroll.content=content;
            for(int i=0;i<rows.Count;i++)
            {
                var row=Rect("Row"+i,content,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(1090,90),new Vector2(0,-i*90-45));
                rows[i](row,i);
            }
        }
        private void BuildMissionRow(RectTransform parent,int row,int index)
        {
            string label=UiShellRuntimeGateway.Localization.Format("ui.home.chapter_mission","CHAPTER {0} • MISSION {1}",index/5+1,index%5+1);
            Text("Mission"+index,parent,label,25,new Vector2(360,78),new Vector2(-340,0));
            string[] stages={"brief","comms","debrief"};string[] labels={"BRIEFING","RADIO","DEBRIEF"};
            for(int i=0;i<3;i++)
            {
                string stage=stages[i];
                var button=Button("Story"+index+stage,parent,Local("ui.home.archive."+stage,labels[i]),new Vector2(215,78),new Vector2(-30+i*235,0),()=>PlayMission(index,stage));
                button.interactable=FindSequence(index,stage)!=null;
            }
        }
        private NarrativeSequenceConfig FindSequence(int index,string stage)
        {
            string id=$"seq.ch{index/5+1:00}.m{index%5+1:00}."+stage;
            return bootstrap?.CampaignMissionNarrativeConfigs?.FirstOrDefault(s=>s!=null && s.SequenceId==id)
                ?? supplementalSequences?.FirstOrDefault(s=>s!=null && s.SequenceId==id);
        }
        public bool PlayMission(int index,string stage)
        {
            if(!UiShellRuntimeGateway.TryReadCampaignOperations(out var model) || !IsMissionEarned(model.CompletedMissionMask,index)) return false;
            var config=FindSequence(index,stage);
            if(config==null || narrative==null || bootstrap==null) return false;
            var persian=UiShellRuntimeGateway.Localization.IsRightToLeft?bootstrap.FirstLaunchPersianLocale:null;
            var legacy=persian!=null?new FirstLaunchNarrativeLocaleTextCompositionSystemHelper(FallbackGameTextResolver.Instance,persian):(IGameTextResolver)FallbackGameTextResolver.Instance;
            var resolver=new FirstLaunchNarrativeCompositionSystemHelper.SharedLocaleCompositionSystemHelper(legacy);
            if(!player.Initialize(config,bootstrap.FirstLaunchSpeakerCatalog,bootstrap.FirstLaunchPunctuationProfile,narrative,resolver,SettingsService.Load(),persian,storyOnly:true)) return false;
            player.HandoffRequested-=EndPlayback;player.HandoffRequested+=EndPlayback;
            if(!player.Start())return false;
            playing=true;overlay?.SetActive(false);
            narrative.PlaybackControlsView.BindSkip(ReturnToChooser);
            narrative.PlaybackControlsView.BindTransport(()=>{if(player.IsPaused)player.Resume();else player.Pause();},()=>player.SetSubtitlesEnabled(!player.SubtitlesEnabled));
            return true;
        }
        private bool PlayBookend(string id,int chapter,bool final)
        {
            if(!UiShellRuntimeGateway.TryReadCampaignOperations(out var model) || !IsChapterEarned(model.CompletedMissionMask,chapter) || final && (!model.FullCampaignRegistered || !model.AllRequiredMissionsCompleted)) return false;
            if (id == "seq.ch04.close.protocol_fragment_04" || id == "seq.ch05.open.citywide_command" || id == "seq.ch05.close.protocol_fragment_05" || id == "seq.campaign.epilogue.canonical") return PlayRegistered(id, null);
            bookends??=gameObject.AddComponent<FutureMissionComicPreviewController>();
            if(!bookends.PlaySequence(id,completed:ReturnToChooser))return false;
            playing=true;overlay?.SetActive(false);return true;
        }
        private readonly Queue<string> finaleArchiveQueue=new();
        private static bool IsFinaleEmphasis(string id) => new[]{"trust","evidence","infrastructure"}.Any(family=>id=="seq.campaign.epilogue."+family+"_emphasis.high"||id=="seq.campaign.epilogue."+family+"_emphasis.low");
        private void PlayRecordedFinaleEmphasis()
        {
            finaleArchiveQueue.Clear();
            int emphasis=new Game.Runtime.CampaignMissionProgressStore(Game.Runtime.SaveService.CreateDefault()).ReadCommandNodeEmphasis();
            string[] families={"trust","evidence","infrastructure"};
            for(int i=0;i<3;i++)finaleArchiveQueue.Enqueue("seq.campaign.epilogue."+families[i]+"_emphasis."+((emphasis&(1<<i))!=0?"high":"low"));
            if(!PlayRegistered(finaleArchiveQueue.Dequeue(),null))finaleArchiveQueue.Clear();
        }
        private bool PlayRegistered(string id, string stateId)
        {
            int requiredMission = id switch
            {
                "seq.ch04.close.protocol_fragment_04" => CampaignMissionSequence.IndexOf(CampaignMissionSequence.ArmorBreak),
                "seq.ch05.open.citywide_command" or "seq.ch05.m01.debrief" => CampaignMissionSequence.IndexOf(CampaignMissionSequence.CitywideAlert),
                "seq.ch05.m02.debrief" => CampaignMissionSequence.IndexOf(CampaignMissionSequence.TrustUnderFire),
                "seq.ch05.close.protocol_fragment_05" or "seq.campaign.epilogue.canonical" or "seq.campaign.postscript.recovery_watch" => CampaignMissionSequence.IndexOf(CampaignMissionSequence.CommandNode),
                _ => IsFinaleEmphasis(id)?CampaignMissionSequence.IndexOf(CampaignMissionSequence.CommandNode):-1
            };
            if (requiredMission < 0 || !UiShellRuntimeGateway.TryReadCampaignOperations(out var model) || !IsMissionEarned(model.CompletedMissionMask, requiredMission)) return false;
            var config = bootstrap?.CampaignMissionNarrativeConfigs?.FirstOrDefault(s=>s!=null && s.SequenceId==id)
                ?? supplementalSequences?.FirstOrDefault(s=>s!=null && s.SequenceId==id);
            if (config == null || narrative == null || bootstrap == null) return false;
            var persian = UiShellRuntimeGateway.Localization.IsRightToLeft ? bootstrap.FirstLaunchPersianLocale : null;
            var legacy = persian != null ? new FirstLaunchNarrativeLocaleTextCompositionSystemHelper(FallbackGameTextResolver.Instance,persian) : (IGameTextResolver)FallbackGameTextResolver.Instance;
            var resolver = new FirstLaunchNarrativeCompositionSystemHelper.SharedLocaleCompositionSystemHelper(legacy);
            if (!player.Initialize(config,bootstrap.FirstLaunchSpeakerCatalog,bootstrap.FirstLaunchPunctuationProfile,narrative,resolver,SettingsService.Load(),persian,storyOnly:true)) return false;
            player.HandoffRequested-=EndPlayback; player.HandoffRequested+=EndPlayback;
            if (!(string.IsNullOrEmpty(stateId) ? player.Start() : player.StartAt(stateId))) return false;
            playing=true; overlay?.SetActive(false);
            narrative.PlaybackControlsView.BindSkip(ReturnToChooser);
            narrative.PlaybackControlsView.BindTransport(()=>{if(player.IsPaused)player.Resume();else player.Pause();},()=>player.SetSubtitlesEnabled(!player.SubtitlesEnabled));
            return true;
        }
        private void EndPlayback(Game.Narrative.Contracts.NarrativeHandoffResult _)
        {
            if(finaleArchiveQueue.Count>0)
            {
                player.Cancel();
                if(PlayRegistered(finaleArchiveQueue.Dequeue(),null))return;
            }
            ReturnToChooser();
        }
        private void Update()
        {
            if(!IsOpen)return;
            if(UiShellRuntimeGateway.TryReadShellState(out var shell) && shell.ActiveRoute!=UIRoute.MainMenu){Close();return;}
            if(Keyboard.current?.escapeKey.wasPressedThisFrame==true){if(playing)ReturnToChooser();else Close();return;}
            if(playing && player.IsRunning)
            {
                player.Tick(Time.unscaledDeltaTime);
                if(!playing) return;
                narrative.SetSkipState(true,true,Local("ui.home.archive.close","CLOSE"));
                narrative.PlaybackControlsView.ApplyTransport(player.CurrentStateIndex+1,Math.Max(1,player.StateCount-1),player.IsPaused,player.SubtitlesEnabled);
            }
            else if(playing && bookends!=null && !bookends.IsOpen)ReturnToChooser();
            if(!playing && locale!=GameLocalization.CurrentLocaleCode)BuildChooser();
        }
        private void ReturnToChooser()
        {
            finaleArchiveQueue.Clear();
            player.HandoffRequested-=EndPlayback;player.Cancel();bookends?.Close();playing=false;
            if(overlay!=null)BuildChooser();
        }
        public void Close()
        {
            finaleArchiveQueue.Clear();
            player.HandoffRequested-=EndPlayback;player.Cancel();bookends?.Close();playing=false;
            if(overlay!=null)Destroy(overlay);overlay=null;
        }
        private static string Local(string key,string fallback)=>UiShellRuntimeGateway.Localization.Get(key,fallback);
        private static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max,Vector2 size,Vector2 position)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=min;rect.anchorMax=max;rect.sizeDelta=size;rect.anchoredPosition=position;return rect;
        }
        private TMP_Text Text(string name,Transform parent,string value,float size,Vector2 bounds,Vector2 position)
        {
            var text=Rect(name,parent,new Vector2(.5f,.5f),new Vector2(.5f,.5f),bounds,position).gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.fontSize=size;text.color=Color.white;text.alignment=TextAlignmentOptions.Center;text.enableAutoSizing=true;text.fontSizeMin=20;text.fontSizeMax=size;text.raycastTarget=false;
            UiLocalizedText.Set(text,value);return text;
        }
        private Button Button(string name,Transform parent,string label,Vector2 size,Vector2 position,Action action)
        {
            var rect=Rect(name,parent,new Vector2(.5f,.5f),new Vector2(.5f,.5f),size,position);
            var fill=rect.gameObject.AddComponent<V3GradientGraphic>();fill.Configure(new Color32(6,105,172,255),new Color32(4,38,89,255),new Color32(0,185,236,255),2);fill.raycastTarget=true;
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=fill;button.onClick.AddListener(()=>action());
            Text("Label",rect,label,25,size-Vector2.one*12,Vector2.zero);return button;
        }
    }
}
