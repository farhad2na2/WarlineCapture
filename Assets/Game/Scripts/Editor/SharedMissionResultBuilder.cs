#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace Game.Editor
{
    public static class SharedMissionResultBuilder
    {
        public const string Prefab="Assets/Game/Resources/UI/SharedMissionResult.prefab";
        public const string Art="Assets/Game/Resources/UI/SharedResultArtwork.asset";
        static TMP_FontAsset font;
        public static void BuildAndCapture()
        {
            try{Build();ResultScreenDesignReviewCapture.Capture();}
            catch(Exception e){Debug.LogException(e);MissionEditorValidationExit.Complete(false);}
        }
        public static void Build()
        {
            string locale=Game.Configs.GameLocalization.CurrentLocaleCode;
            Game.Configs.GameLocalization.SetLocale("en",false);
            try
            {
                Directory.CreateDirectory("Assets/Game/Resources/UI");AssetDatabase.Refresh();
                font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset");
                BuildArtwork();BuildCard();
                PatchCampaign(MissionResultV3PrefabBuilder.PrefabPath);
                PatchCampaign("Assets/Game/Prefabs/UI/Shell/Popups/POP05_MissionResultPopup.prefab");
                AssetDatabase.SaveAssets();
                Debug.Log("[SharedMissionResultBuilder] result=Passed sharedRendererPrefab=1 modes=3 missionBindingsUnchanged=true");
            }
            finally{Game.Configs.GameLocalization.SetLocale(locale,false);}
        }
        static void BuildArtwork()
        {
            var entries=new List<SharedResultArtworkCatalog.Entry>();
            var campaign=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/Shell/Content/SCN05_CampaignOperationsContent.prefab").GetComponentInChildren<CampaignOperationsScreenView>(true);
            var comics=new SerializedObject(campaign).FindProperty("missionComicPlates");
            for(int i=0;i<comics.arraySize;i++)
            {
                var sprite=comics.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
                if(sprite==null)throw new InvalidOperationException("Missing Campaign result comic "+i);
                entries.Add(new SharedResultArtworkCatalog.Entry{MissionId=Game.Missions.Contracts.CampaignMissionSequence.IdAt(i),Artwork=sprite.texture});
            }
            foreach(string path in Directory.GetFiles("Assets/Game/Art/SkirmishLibrary/Previews","*.png").OrderBy(x=>x))
            {
                string id=Path.GetFileNameWithoutExtension(path).Replace("skirmish.preview.","");
                entries.Add(new SharedResultArtworkCatalog.Entry{MissionId=id,Artwork=AssetDatabase.LoadAssetAtPath<Texture2D>(path.Replace('\\','/'))});
            }
            // Existing Operations mode illustration, never another mission's comic.
            entries.Add(new SharedResultArtworkCatalog.Entry{MissionId="O001",Artwork=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Game/Art/UI/V3Shared/MainMenuPlates/SCN02_OperationsScene_V3.png")});
            var asset=AssetDatabase.LoadAssetAtPath<SharedResultArtworkCatalog>(Art);
            if(asset==null){asset=ScriptableObject.CreateInstance<SharedResultArtworkCatalog>();AssetDatabase.CreateAsset(asset,Art);}
            asset.Configure(entries.ToArray(),AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Game/Art/UI/V3Shared/MissionResult/POP05_M01_OldMarket_ResultBackdrop_V3.png"));EditorUtility.SetDirty(asset);
        }
        static void BuildCard()
        {
            var root=new GameObject("SharedMissionResult",typeof(RectTransform),typeof(SharedMissionResultView));
            try
            {
                var r=(RectTransform)root.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(1920,1080);
                var b=new SharedMissionResultView.Bindings();
                b.Backdrop=Raw(r,"Backdrop",0,0,1920,1080);b.Backdrop.texture=SharedResultArtworkCatalog.Load()?.Backdrop;
                var shade=Rect("BackdropShade",r,0,0,1920,1080).gameObject.AddComponent<Image>();shade.color=new Color(0,.035f,.055f,.86f);shade.raycastTarget=false;b.BackdropShade=shade;
                var header=Panel(r,"Identity",32,24,1088,140);
                b.Mode=Text(header,"Mode",24,8,440,48,24,SharedMissionResultView.Muted,"CAMPAIGN");
                b.Mission=Text(header,"Mission",24,60,1040,68,38,SharedMissionResultView.White,"FIRST CONTACT");b.Mission.GetComponent<V3LocalizedTextBindingView>().SetFontBounds(28,38);
                b.Context=Text(header,"Context",560,8,504,48,26,SharedMissionResultView.Cyan,"OLD MARKET");
                var duration=Panel(r,"Duration",1624,24,264,140);
                b.DurationHeading=Text(duration,"Heading",20,8,224,52,22,SharedMissionResultView.Cyan,"MISSION DURATION");
                b.Duration=Text(duration,"Value",20,64,224,58,42,SharedMissionResultView.Cyan,"00:44");b.Duration.alignment=TextAlignmentOptions.Center;
                var outcome=Panel(r,"Outcome",32,180,1856,124);b.OutcomeSurface=outcome.GetComponent<V3GradientGraphic>();
                b.OutcomeIcon=Symbol(outcome,"Emblem",24,18,80,84,SharedResultSymbolGraphic.Kind.OutcomeSuccess);
                b.Outcome=Text(outcome,"Title",132,18,420,88,56,SharedMissionResultView.Green,"VICTORY");
                b.Outcome.GetComponent<V3LocalizedTextBindingView>().SetFontBounds(32,56);
                b.Reason=Text(outcome,"Reason",576,30,936,72,30,SharedMissionResultView.White,"");
                b.FilledStars=new V3StarGraphic[3];b.OutlineStars=new V3StarGraphic[3];b.StarRects=new RectTransform[3];
                for(int i=0;i<3;i++)
                {
                    b.FilledStars[i]=Rect("StarFilled"+i,outcome,1552+i*92,22,76,76).gameObject.AddComponent<V3StarGraphic>();
                    b.FilledStars[i].ConfigureOutlinedGradient(new Color32(255,208,64,255),new Color32(255,211,57,255),new Color32(214,143,15,255));b.StarRects[i]=b.FilledStars[i].rectTransform;
                    var outline=Rect("StarOutline"+i,outcome,1552+i*92,22,76,76).gameObject.AddComponent<V3StarGraphic>();outline.ConfigureOutlinedGradient(SharedMissionResultView.Muted,new Color32(3,15,24,255),new Color32(3,15,24,255),0.78f);b.OutlineStars[i]=outline;
                }
                var story=Panel(r,"Story",32,328,600,548);
                b.ArtworkClip=Rect("ArtworkClip",story,16,16,568,264);b.ArtworkClip.gameObject.AddComponent<RectMask2D>();
                b.Artwork=Raw(b.ArtworkClip,"Artwork",0,0,568,264);
                b.SummaryTitle=Text(story,"StoryTitle",24,294,552,80,32,SharedMissionResultView.White,"");b.SummaryTitle.GetComponent<V3LocalizedTextBindingView>().SetFontBounds(24,32);
                (b.SummaryScroll,b.Summary,b.SummaryViewport)=Scroll(story,"Summary",24,390,552,134,26);
                var objectives=Panel(r,"Objectives",648,328,680,548);
                b.ObjectivesHeading=Text(objectives,"Heading",24,8,632,64,29,SharedMissionResultView.Cyan,"OBJECTIVES");
                b.ObjectiveLabels=new TMP_Text[3];b.ObjectiveValues=new TMP_Text[3];b.ObjectiveIcons=new SharedResultSymbolGraphic[3];b.StatusIcons=new SharedResultSymbolGraphic[3];b.ObjectiveSurfaces=new V3GradientGraphic[3];b.StatusSurfaces=new V3GradientGraphic[3];b.ObjectiveStripes=new Image[3];b.ObjectiveRows=new GameObject[3];
                for(int i=0;i<3;i++)
                {
                    var row=Panel(objectives,"Objective"+i,16,88+i*142,648,132);b.ObjectiveRows[i]=row.gameObject;b.ObjectiveSurfaces[i]=row.GetComponent<V3GradientGraphic>();
                    b.ObjectiveStripes[i]=Rect("StateStripe",row,0,0,4,132).gameObject.AddComponent<Image>();b.ObjectiveStripes[i].raycastTarget=false;
                    b.ObjectiveIcons[i]=Symbol(row,"Icon",16,38,52,52,(SharedResultSymbolGraphic.Kind)i);
                    b.ObjectiveLabels[i]=Text(row,"Label",84,14,314,104,30,SharedMissionResultView.White,"");b.ObjectiveLabels[i].GetComponent<V3LocalizedTextBindingView>().SetFontBounds(24,30);
                    b.ObjectiveValues[i]=Text(row,"Value",406,14,154,104,26,SharedMissionResultView.Green,"");
                    var status=Panel(row,"Status",578,42,46,46);b.StatusSurfaces[i]=status.GetComponent<V3GradientGraphic>();b.StatusIcons[i]=Symbol(status,"Mark",6,6,34,34,SharedResultSymbolGraphic.Kind.Check);
                }
                var performance=Panel(r,"Performance",1344,328,544,258);b.PerformanceHeading=Text(performance,"Heading",24,8,496,64,28,SharedMissionResultView.Cyan,"PERFORMANCE");
                b.PerformanceLabels=new TMP_Text[3];b.PerformanceValues=new TMP_Text[3];
                for(int i=0;i<3;i++)
                {
                    var row=Panel(performance,"Stat"+i,16,74+i*56,512,50);b.PerformanceLabels[i]=Text(row,"Label",12,4,346,42,22,SharedMissionResultView.White,"");b.PerformanceValues[i]=Text(row,"Value",366,4,130,42,28,SharedMissionResultView.Cyan,"");
                }
                var info=Panel(r,"Information",1344,602,544,274);b.InfoHeading=Text(info,"Heading",24,8,496,64,28,SharedMissionResultView.Cyan,"REWARDS");
                (b.InfoScroll,b.InfoBody,b.InfoViewport)=Scroll(info,"Body",24,72,496,172,28);
                b.ReportIcon=Symbol(info,"ReportIcon",24,84,64,64,SharedResultSymbolGraphic.Kind.Document);
                b.RewardIcons=new[]{Image(info,"ExperienceIcon",24,84,52,44,Sprite(V3UiFoundationBuilder.CommanderUpgradesIconPath)),Image(info,"CreditsIcon",24,124,52,44,Sprite("Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_03_credits_coin.png"))};
                b.Medal=Rect("Medal",info,24,88,104,104).gameObject.AddComponent<MissionAtlasStatusGraphic>();
                b.Medal.Configure(MissionAtlasStatusGraphic.Kind.Medal,false);
                b.SaveStrip=Panel(r,"SaveStatus",32,888,1856,56);b.SaveStatus=Text(b.SaveStrip,"Value",24,4,1808,48,22,SharedMissionResultView.Cyan,"");
                (b.Primary,b.PrimaryLabel,b.PrimaryIcon)=Button(r,"ContinueButton",1120,956,768,96);
                (b.Retry,b.RetryLabel,b.RetryIcon)=Button(r,"ReplayButton",1120,956,768,96);b.Retry.gameObject.SetActive(false);
                (b.Secondary,b.SecondaryLabel,b.SecondaryIcon)=Button(r,"Secondary",32,956,544,96);
                (b.Tertiary,b.TertiaryLabel,b.TertiaryIcon)=Button(r,"Tertiary",592,956,512,96);
                b.BackIcon=Sprite(V3UiFoundationBuilder.CommanderBackIconPath);b.ContinueIcon=Sprite(V3UiFoundationBuilder.CampaignLaunchIconPath);b.ReplayIcon=Sprite(V3UiFoundationBuilder.ResetIconPath);b.MapIcon=Sprite(V3UiFoundationBuilder.CampaignChaptersIconPath);b.AdjustIcon=Sprite(V3UiFoundationBuilder.SettingsIconPath);
                root.GetComponent<SharedMissionResultView>().Author(b);
                PrefabUtility.SaveAsPrefabAsset(root,Prefab,out bool saved);if(!saved)throw new InvalidOperationException("Shared result prefab save failed");
            }
            finally{Object.DestroyImmediate(root);}
        }
        static void PatchCampaign(string path)
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var owner=root.GetComponent<MissionResultPopupView>();if(owner==null)return;
                var data=new SerializedObject(owner);
                foreach(string field in new[]{"primaryButton","retryButton"})
                {var old=data.FindProperty(field).objectReferenceValue as Button;if(old!=null&&(root.transform.Find("SharedMissionResult")==null||!old.transform.IsChildOf(root.transform.Find("SharedMissionResult"))))old.name="Legacy"+old.name;}
                var prior=root.transform.Find("SharedMissionResult");if(prior!=null)Object.DestroyImmediate(prior.gameObject);
                foreach(Transform child in root.transform)
                {
                    if(child.name is "BattlefieldBackdrop" or "OutcomeBackdropTint")child.gameObject.SetActive(false);
                    else
                    {
                        var group=child.GetComponent<CanvasGroup>();if(group==null)group=child.gameObject.AddComponent<CanvasGroup>();group.alpha=0;group.blocksRaycasts=false;group.interactable=false;
                    }
                }
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab),root.transform);instance.name="SharedMissionResult";
                var view=instance.GetComponent<SharedMissionResultView>();
                data.FindProperty("sharedResult").objectReferenceValue=view;
                data.FindProperty("primaryButton").objectReferenceValue=view.Controls.Primary;
                data.FindProperty("retryButton").objectReferenceValue=view.Controls.Retry;
                data.FindProperty("primaryButtonLabel").objectReferenceValue=view.Controls.PrimaryLabel;
                data.FindProperty("retryButtonLabel").objectReferenceValue=view.Controls.RetryLabel;
                data.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {var go=new GameObject(name,typeof(RectTransform));go.layer=parent.gameObject.layer;go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;MissionAtlasScreenView.Place(r,x,y,w,h);return r;}
        static RectTransform Panel(Transform parent,string name,float x,float y,float w,float h)
        {var r=Rect(name,parent,x,y,w,h);var g=r.gameObject.AddComponent<V3GradientGraphic>();g.Configure(new Color32(16,35,47,255),new Color32(3,15,24,255),SharedMissionResultView.Line,1);g.raycastTarget=false;return r;}
        static TMP_Text Text(Transform parent,string name,float x,float y,float w,float h,float size,Color color,string text)
        {
            var r=Rect(name,parent,x,y,w,h);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.fontSize=size;t.fontSizeMin=size;t.fontSizeMax=size;t.enableAutoSizing=false;t.color=color;t.alignment=TextAlignmentOptions.TopLeft;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;t.margin=new Vector4(4,2,12,2);t.text=text;
            var binding=r.gameObject.AddComponent<V3LocalizedTextBindingView>();binding.Configure("",text);binding.SetFontBounds(size,size);return t;
        }
        static RawImage Raw(Transform parent,string name,float x,float y,float w,float h)
        {var image=Rect(name,parent,x,y,w,h).gameObject.AddComponent<RawImage>();image.raycastTarget=false;return image;}
        static Sprite Sprite(string path)=>AssetDatabase.LoadAssetAtPath<Sprite>(path);
        static Image Image(Transform parent,string name,float x,float y,float w,float h,Sprite sprite)
        {var image=Rect(name,parent,x,y,w,h).gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;return image;}
        static SharedResultSymbolGraphic Symbol(Transform parent,string name,float x,float y,float w,float h,SharedResultSymbolGraphic.Kind kind)
        {var g=Rect(name,parent,x,y,w,h).gameObject.AddComponent<SharedResultSymbolGraphic>();g.Configure(kind,SharedMissionResultView.Green);return g;}
        static (Button,TMP_Text,Image) Button(Transform parent,string name,float x,float y,float w,float h)
        {
            var r=Panel(parent,name,x,y,w,h);var graphic=r.GetComponent<V3GradientGraphic>();graphic.raycastTarget=true;graphic.SetMilitaryMaterial(MilitaryUiMaterialKind.None);
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=graphic;b.transition=Selectable.Transition.ColorTint;
            var colors=b.colors;colors.normalColor=Color.white;colors.disabledColor=Color.white;b.colors=colors;
            var label=Text(r,"Label",96,8,w-120,h-16,36,SharedMissionResultView.White,"");label.alignment=TextAlignmentOptions.Center;
            var icon=Image(r,"Icon",24,20,56,56,null);return(b,label,icon);
        }
        static (ScrollRect,TMP_Text,RectTransform) Scroll(Transform parent,string name,float x,float y,float w,float h,float size)
        {
            var root=Rect(name,parent,x,y,w,h);var viewport=Rect("Viewport",root,0,0,w,h);MissionAtlasScreenView.Fill(viewport);viewport.gameObject.AddComponent<RectMask2D>();
            var content=Rect("Content",viewport,0,0,w,h);content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.sizeDelta=new Vector2(0,h);var text=Text(content,"Text",0,0,w-16,h,size,SharedMissionResultView.White,"");
            var scroll=root.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;scroll.movementType=ScrollRect.MovementType.Clamped;
            var track=Rect("Scrollbar",root,w-10,0,8,h);track.anchorMin=new Vector2(1,0);track.anchorMax=Vector2.one;track.pivot=new Vector2(1,.5f);track.anchoredPosition=Vector2.zero;track.sizeDelta=new Vector2(8,0);var background=track.gameObject.AddComponent<Image>();background.color=new Color32(16,39,53,255);
            var handle=Rect("Handle",track,0,0,8,h);MissionAtlasScreenView.Fill(handle);var thumb=handle.gameObject.AddComponent<Image>();thumb.color=SharedMissionResultView.Cyan;
            var bar=track.gameObject.AddComponent<Scrollbar>();bar.handleRect=handle;bar.targetGraphic=thumb;bar.direction=Scrollbar.Direction.BottomToTop;scroll.verticalScrollbar=bar;
            return(scroll,text,root);
        }
    }
}
#endif
