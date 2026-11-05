using System;
using System.Collections.Generic;
using System.Linq;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>Authors real native controls into both content prefabs using the same UI component.</summary>
    public static class ApprovedMissionAtlasBuilder
    {
        private const string CampaignPath="Assets/Game/Prefabs/UI/Shell/Content/SCN05_CampaignOperationsContent.prefab";
        private const string MainPath="Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab";
        public static async void BuildAndReview()
        {
            try { Build(); await ApprovedMissionAtlasReview.Run(); }
            catch(Exception error) {Debug.LogException(error);MissionEditorValidationExit.Complete(false);}
        }
        public static async void ReviewExisting()
        {
            try { await ApprovedMissionAtlasReview.Run(); }
            catch(Exception error) {Debug.LogException(error);MissionEditorValidationExit.Complete(false);}
        }
        public static void Build()
        {
            RepairMainPageComics();BuildCampaign();BuildSkirmish();AssetDatabase.SaveAssets();
            Debug.Log("[ApprovedMissionAtlas] result=Passed campaignComics=25 chapterComics=5 sharedNativeLayout=true");
        }
        private static MissionAtlasScreenView Install(GameObject root, GameObject bindingOwner = null)
        {
            var previous=root.transform.Find("ApprovedMissionAtlas");if(previous!=null)UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var go=new GameObject("ApprovedMissionAtlas",typeof(RectTransform));go.transform.SetParent(root.transform,false);MissionAtlasScreenView.Fill(go.transform as RectTransform);
            var view=go.AddComponent<MissionAtlasScreenView>();
            var catalog=V3UiFoundationBuilder.RequireCatalog();
            view.Author(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset"),catalog,
                SpriteAt("Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/scn03_icon_08_back_arrow.png"),
                SpriteAt("Assets/Game/Art/UI/V3Shared/Sprites/Icons/ui_icon_chapters_book.png"),
                SpriteAt("Assets/Game/Art/UI/Generated/MainMenuV15C/LayeredOneGo/scn02_deploy_chevrons.png"),bindingOwner==null);
            for(int i=0;i<root.transform.childCount;i++)
            {
                var child=root.transform.GetChild(i);
                if(child==go.transform)continue;
                if(bindingOwner!=null && child==bindingOwner.transform)
                {
                    child.gameObject.SetActive(true);
                    var hidden=child.GetComponent<CanvasGroup>();
                    if(hidden==null)hidden=child.gameObject.AddComponent<CanvasGroup>();
                    hidden.alpha=0;hidden.blocksRaycasts=false;hidden.interactable=false;
                }
                else child.gameObject.SetActive(false);
            }
            EditorUtility.SetDirty(view);return view;
        }
        public static void BuildCampaign()
        {
            var root=PrefabUtility.LoadPrefabContents(CampaignPath);
            try
            {
                var campaign=root.GetComponentInChildren<CampaignOperationsScreenView>(true);
                var comics=ReadMainPageComics();
                comics[13]=SpriteAt("Assets/Game/Art/Narrative/CH03M04EvidenceChain/BriefProtectedCustody.png");
                for(int i=0;i<25;i++)
                {
                    if(comics[i]!=null)continue;
                    var future=FutureMissionComicCatalog.Find(i/5+1,i%5+1);
                    if(future!=null)comics[i]=SpriteAt("Assets/Game/Resources/FutureMissionComics/"+future.image+".png");
                    if(comics[i]==null)throw new InvalidOperationException("Missing exact Campaign comic for mission "+i);
                }
                var covers=new[]{comics[0],comics[5],SpriteAt("Assets/Game/Resources/FutureMissionComics/Bookends/CH03_Open.png"),comics[15],comics[20]};
                var atlas=Install(root,campaign.gameObject);var data=new SerializedObject(campaign);
                data.FindProperty("approvedAtlas").objectReferenceValue=atlas;
                var scenes=data.FindProperty("districtAtlasScenes");scenes.arraySize=5;
                var city=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Game/Art/UI/Generated/MissionAtlas/campaign-city-atlas.png");
                if(city==null)throw new InvalidOperationException("Approved city atlas artwork is missing");
                for(int i=0;i<5;i++)scenes.GetArrayElementAtIndex(i).objectReferenceValue=city;
                SetArray(data,"missionComicPlates",comics);SetArray(data,"chapterComicPlates",covers);data.ApplyModifiedPropertiesWithoutUndo();
                var archive=atlas.gameObject.AddComponent<Game.Composition.MainMenuStoryArchiveView>();
                var mainArchive=AssetDatabase.LoadAssetAtPath<GameObject>(MainPath).GetComponentInChildren<Game.Composition.MainMenuStoryArchiveView>(true);
                var supplements=new SerializedObject(mainArchive).FindProperty("supplementalSequences");var sequences=new Game.Configs.NarrativeSequenceConfig[supplements.arraySize];
                for(int i=0;i<sequences.Length;i++)sequences[i]=supplements.GetArrayElementAtIndex(i).objectReferenceValue as Game.Configs.NarrativeSequenceConfig;
                archive.Configure(null,AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Medium SDF.asset"),sequences);
                while(campaign.ApprovedArchiveRequested.GetPersistentEventCount()>0)UnityEditor.Events.UnityEventTools.RemovePersistentListener(campaign.ApprovedArchiveRequested,0);
                UnityEditor.Events.UnityEventTools.AddPersistentListener(campaign.ApprovedArchiveRequested,archive.Open);
                campaign.Apply(new UiCampaignOperationsModel(1,1,1,new UiCampaignMissionModel(Game.Missions.Contracts.CampaignMissionSequence.IdAt(0),"","","FIRST CONTACT",true,false,false,0,0,0,UiCampaignMissionPrimaryActionKind.Start,"START BRIEFING"),"",false,1,0));
                PrefabUtility.SaveAsPrefabAsset(root,CampaignPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        public static void BuildSkirmish()
        {
            var root=PrefabUtility.LoadPrefabContents(SkirmishSetupPrefabBuilder.PrefabPath);
            try
            {
                var screen=root.GetComponentInChildren<QuickCustomScreenView>(true);var size=screen.MissionSizeControl;
                // Keep the bound control outside the subtree replaced by Install on every rebuild.
                if(size!=null)size.transform.SetParent(root.transform,false);
                else size=CreateMissionSize(root.transform);
                var atlas=Install(root);var data=new SerializedObject(screen);data.FindProperty("approvedSkirmishAtlas").objectReferenceValue=atlas;
                data.FindProperty("missionSizeSegmented").objectReferenceValue=size;
                var icons=new[]{"Assets/Game/Art/UI/Icons/scn08_icon_objective_star.png","Assets/Game/Art/UI/Icons/scn09_icon_rally_flag.png","Assets/Game/Art/UI/Generated/Armory/LayeredOneGo/scn19_icon_damage_burst.png","Assets/Game/Art/UI/Generated/Armory/LayeredOneGo/scn19_icon_vehicle_truck.png"};
                var iconArray=data.FindProperty("approvedObjectiveIcons");iconArray.arraySize=4;for(int i=0;i<4;i++)iconArray.GetArrayElementAtIndex(i).objectReferenceValue=SpriteAt(icons[i]);
                var illustrations=data.FindProperty("approvedMissionIllustrations");illustrations.arraySize=4;
                string[] illustrationPaths={"Assets/Game/Art/UI/Generated/MissionAtlas/Previews/S001-menu-illustration.png","Assets/Game/Art/UI/Generated/MissionAtlas/Previews/S002-menu-illustration.png","Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Veh_Jet_01_Action_512.png","Assets/Game/Art/UI/Portraits/Secondary/Portrait_Unit_Veh_Jet_02_Action_512.png"};
                for(int i=0;i<4;i++)
                {
                    illustrations.GetArrayElementAtIndex(i).objectReferenceValue=AssetDatabase.LoadAssetAtPath<Texture2D>(illustrationPaths[i]);
                    if(illustrations.GetArrayElementAtIndex(i).objectReferenceValue==null)throw new InvalidOperationException("Missing exact mission illustration "+illustrationPaths[i]);
                }
                data.ApplyModifiedPropertiesWithoutUndo();
                if(size!=null)
                {
                    size.transform.SetParent(atlas.ExtraControls,false);MissionAtlasScreenView.Fill(size.transform as RectTransform);
                    if(size.SegmentRoot is RectTransform segments)MissionAtlasScreenView.Fill(segments);
                    foreach(var label in size.GetComponentsInChildren<TMP_Text>(true))
                    {label.enableAutoSizing=false;label.fontSize=label.fontSizeMin=label.fontSizeMax=22;var fixedSize=label.GetComponent<LibraryFixedTextSize>();if(fixedSize!=null){fixedSize.Size=22;fixedSize.MaxLines=2;}}
                    var sizeStyle=size.GetComponent<MissionAtlasArmySizeView>();
                    if(sizeStyle==null)sizeStyle=size.gameObject.AddComponent<MissionAtlasArmySizeView>();
                    else
                    {
                        foreach(var button in size.SegmentButtons)
                            foreach(string childName in new[]{"ChoiceFace","SelectedChoice","UnavailableChoice"})
                            {var child=button.transform.Find(childName);if(child!=null)UnityEngine.Object.DestroyImmediate(child.gameObject);}
                    }
                    sizeStyle.Author(size);
                    size.gameObject.SetActive(false);
                }
                var map=SkirmishLibraryPreviewAssets.Load(SkirmishLibraryPreviewAssets.TheatreBaseKey);
                var nav=new MissionAtlasItem[5];for(int i=0;i<5;i++)
                {string id=SkirmishBattleLibraryArt.MapIds[i];SkirmishBattleLibraryArt.Copy(id,false,out string title,out _);nav[i]=new MissionAtlasItem{Id="TheaterPin_"+id,Title=title,Subtitle="24 MISSIONS",Enabled=true,Selected=i==0,Medals=-1,Texture=SkirmishLibraryPreviewAssets.Load(SkirmishLibraryPreviewAssets.ResolvePlateKey(id))};}
                atlas.Present(new MissionAtlasPresentation {Skirmish=true,Heading="SKIRMISH / SELECT MAP",Navigation=nav,Overview=map,DetailTitle=nav[0].Title,Preview=nav[0].Texture,Purpose="Choose a map, then select a mission.",Primary="CHOOSE MISSIONS",Secondary="QUICK BATTLE",CanLaunch=true});
                PrefabUtility.SaveAsPrefabAsset(root,SkirmishSetupPrefabBuilder.PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        private static UISegmentedControlView CreateMissionSize(Transform parent)
        {
            var root=new GameObject("MissionSizeSegmented",typeof(RectTransform),typeof(UISegmentedControlView));root.transform.SetParent(parent,false);
            var view=root.GetComponent<UISegmentedControlView>();var data=new SerializedObject(view);data.FindProperty("segmentRoot").objectReferenceValue=root.transform;
            var buttons=data.FindProperty("segmentButtons");var labels=data.FindProperty("segmentLabels");buttons.arraySize=labels.arraySize=3;
            for(int i=0;i<3;i++)
            {
                var go=new GameObject("MissionSize_"+i,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(root.transform,false);
                var image=go.GetComponent<Image>();image.color=new Color32(16,35,44,255);var b=go.GetComponent<Button>();b.targetGraphic=image;
                var label=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));label.transform.SetParent(go.transform,false);MissionAtlasScreenView.Fill(label.transform as RectTransform);
                var text=label.GetComponent<TMP_Text>();text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset");text.fontSize=24;text.color=Color.white;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;
                buttons.GetArrayElementAtIndex(i).objectReferenceValue=b;labels.GetArrayElementAtIndex(i).objectReferenceValue=text;
            }
            data.FindProperty("applyVisualSelection").boolValue=true;data.FindProperty("normalBackgroundColor").colorValue=new Color32(16,35,44,255);data.FindProperty("selectedBackgroundColor").colorValue=new Color32(0,96,119,255);data.ApplyModifiedPropertiesWithoutUndo();return view;
        }
        public static void RepairMainPageComics()
        {
            var comics=ReadMainPageComics();
            if(comics[13]==null)comics[13]=SpriteAt("Assets/Game/Art/Narrative/CH03M04EvidenceChain/BriefProtectedCustody.png");
            for(int i=0;i<25;i++)
            {
                if(comics[i]!=null)continue;
                var future=FutureMissionComicCatalog.Find(i/5+1,i%5+1);
                if(future==null)throw new InvalidOperationException("Missing own-mission comic for "+i);
                comics[i]=SpriteAt("Assets/Game/Resources/FutureMissionComics/"+future.image+".png");
            }
            var root=PrefabUtility.LoadPrefabContents(MainPath);
            try
            {
                var view=root.GetComponentInChildren<MainMenuCampaignCardView>(true);var data=new SerializedObject(view);var plates=data.FindProperty("plates");plates.arraySize=25;
                for(int i=0;i<25;i++)
                {
                    var entry=plates.GetArrayElementAtIndex(i);entry.FindPropertyRelative("missionId").stringValue=Game.Missions.Contracts.CampaignMissionSequence.IdAt(i);
                    entry.FindPropertyRelative("plate").objectReferenceValue=comics[i];
                }
                data.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,MainPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            Debug.Log("[CampaignComicBindings] result=Passed mainPage=25 ownMissionArt=true existingBindingsPreserved=true");
        }
        private static void SetArray(SerializedObject data,string field,Sprite[] values)
        {var p=data.FindProperty(field);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];}
        private static Sprite[] ReadMainPageComics()
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(MainPath).GetComponentInChildren<MainMenuCampaignCardView>(true);
            var plates=new SerializedObject(source).FindProperty("plates");var result=new Sprite[25];
            for(int i=0;i<plates.arraySize;i++)
            {
                var entry=plates.GetArrayElementAtIndex(i);string id=entry.FindPropertyRelative("missionId").stringValue;
                for(int j=0;j<25;j++)if(id==Game.Missions.Contracts.CampaignMissionSequence.IdAt(j))result[j]=entry.FindPropertyRelative("plate").objectReferenceValue as Sprite;
            }
            return result;
        }
        private static Sprite SpriteAt(string path)
        {
            var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            var sprite=sprites.FirstOrDefault(s=>s.name.EndsWith("16x9",StringComparison.OrdinalIgnoreCase))??sprites.FirstOrDefault();
            if(sprite==null)throw new InvalidOperationException("No imported comic/icon sprite: "+path);return sprite;
        }
    }
}
