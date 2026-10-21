using System;
using System.IO;
using System.Linq;
using Game.Configs;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class MainMenuModeCardsAuthoring
    {
        public const string OperationsPreviewPath="Assets/Game/Art/UI/MainMenuPanels/Missions/operation.o001.png";
        public const string OperationsIllustrationPath="Assets/Game/Art/UI/MainMenuPanels/Missions/operation.o001-illustration.png";
        public const string CompletionPanoramaPath="Assets/Game/Art/UI/V3Shared/MainMenuPlates/CampaignCompletion/home-campaign-complete-panorama-v01.png";
        private static TMP_FontAsset Font=>AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset");
        public static void BuildAndReview()
        {
            try
            {
                if(!File.Exists(OperationsPreviewPath))SkirmishLibraryPreviewCapture.CaptureOperationsHomePreview();
                MainMenuV3PrefabBuilder.Build();
                MainMenuModeCardsValidation.Run();
            }
            catch(Exception error){Debug.LogException(error);MissionEditorValidationExit.Complete(false);}
        }
        public static void BuildReviewAndInput()
        {
            try
            {
                RebuildExisting();
                if(!MainMenuModeCardsValidation.Validate()){MissionEditorValidationExit.Complete(false);return;}
                MainMenuModeCardsInputReview.Run();
            }
            catch(Exception error){Debug.LogException(error);MissionEditorValidationExit.Complete(false);}
        }
        public static void RebuildExisting()
        {
            BuildSharedArtworkCatalog();
            const string path="Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try{Apply(root);PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();
        }
        public static void Apply(GameObject root)
        {
            BuildSharedArtworkCatalog();
            var left=root.transform.Find("LeftContent");
            var existing=left.GetComponent<MainMenuModeCardsView>();if(existing!=null)UnityEngine.Object.DestroyImmediate(existing);
            foreach(string name in new[]{"CompleteMissionCount","CompletionThumbnail","CompletionEmblem","CompletionPanorama"})
            {var old=left.Find("Card_Campaign/"+name);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);}
            var operations=BuildCard(left.Find("Card_Operations"),MainMenuMode.Operations);
            var skirmish=BuildCard(left.Find("Card_Skirmish"),MainMenuMode.Skirmish);
            var hero=left.Find("Card_Campaign");
            var count=Label(hero,"CompleteMissionCount",28,Color.white);
            var panorama=Rect(hero,"CompletionPanorama");panorama.SetAsFirstSibling();
            panorama.gameObject.AddComponent<RawImage>().raycastTarget=false;
            var artwork=panorama.gameObject.AddComponent<MissionAtlasArtworkView>();
            ConfigurePanoramaImporter();
            var ending=AssetDatabase.LoadAssetAtPath<Texture2D>(CompletionPanoramaPath);
            if(ending==null)throw new InvalidOperationException("Dedicated Campaign ending panorama required.");
            artwork.Set(null,ending);
            var emblem=Rect(hero,"CompletionEmblem");
            emblem.gameObject.AddComponent<MissionAtlasStatusGraphic>().Configure(MissionAtlasStatusGraphic.Kind.Medal,true,2);
            var controller=left.gameObject.AddComponent<MainMenuModeCardsView>();
            controller.Configure(hero.GetComponent<MainMenuCampaignCardView>(),operations,skirmish,count,artwork,emblem);
            controller.ApplyLayout();
        }
        private static void ConfigurePanoramaImporter()
        {
            if(AssetImporter.GetAtPath(CompletionPanoramaPath) is not TextureImporter importer)
                throw new InvalidOperationException("Missing ending panorama importer.");
            if(!importer.mipmapEnabled && importer.textureCompression==TextureImporterCompression.CompressedHQ &&
                importer.GetPlatformTextureSettings("Android").overridden)return;
            importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;
            importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.CompressedHQ;
            foreach(string platform in new[]{"Android","iPhone"})
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name=platform,overridden=true,maxTextureSize=2048,format=TextureImporterFormat.ASTC_4x4,compressionQuality=100});
            importer.SaveAndReimport();
        }
        private static void BuildSharedArtworkCatalog()
        {
            const string path="Assets/Game/Resources/UI/MissionAtlasMissionArtwork.asset";
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(SkirmishSetupPrefabBuilder.PrefabPath).GetComponentInChildren<QuickCustomScreenView>(true);
            var data=new SerializedObject(source).FindProperty("approvedMissionIllustrations");
            var entries=new System.Collections.Generic.List<MissionAtlasMissionArtworkCatalog.Entry>();
            for(int i=0;i<data.arraySize;i++)
                if(data.GetArrayElementAtIndex(i).objectReferenceValue is Texture2D texture)
                    entries.Add(new MissionAtlasMissionArtworkCatalog.Entry{MissionId="S"+(i+1).ToString("000"),Texture=texture});
            if(AssetImporter.GetAtPath(OperationsIllustrationPath) is TextureImporter importer)
            {
                if(importer.mipmapEnabled || importer.textureCompression!=TextureImporterCompression.CompressedHQ ||
                    !importer.GetPlatformTextureSettings("Android").overridden)
                {
                    importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;
                    importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.CompressedHQ;
                    foreach(string platform in new[]{"Android","iPhone"})
                        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name=platform,overridden=true,maxTextureSize=2048,format=TextureImporterFormat.ASTC_4x4,compressionQuality=100});
                    importer.SaveAndReimport();
                }
            }
            var operation=AssetDatabase.LoadAssetAtPath<Texture2D>(OperationsIllustrationPath);
            if(operation==null)throw new InvalidOperationException("Actual Operations mission thumbnail required.");
            entries.Add(new MissionAtlasMissionArtworkCatalog.Entry{MissionId="operation.o001",Texture=operation});
            var catalog=AssetDatabase.LoadAssetAtPath<MissionAtlasMissionArtworkCatalog>(path);
            if(catalog==null){catalog=ScriptableObject.CreateInstance<MissionAtlasMissionArtworkCatalog>();AssetDatabase.CreateAsset(catalog,path);}
            catalog.Configure(entries.ToArray());EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);
        }
        private static MainMenuModeCardView BuildCard(Transform root,MainMenuMode mode)
        {
            var oldView=root.GetComponent<MainMenuModeCardView>();if(oldView!=null)UnityEngine.Object.DestroyImmediate(oldView);
            var oldFace=root.GetComponent<V3GradientGraphic>();if(oldFace!=null)UnityEngine.Object.DestroyImmediate(oldFace);
            foreach(Transform child in root.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            var previous=root.GetComponent<MainMenuDisclosureView>();if(previous!=null)UnityEngine.Object.DestroyImmediate(previous);
            Color accent=mode==MainMenuMode.Operations?new Color32(25,210,114,255):new Color32(255,112,24,255);
            var face=root.gameObject.AddComponent<V3GradientGraphic>();face.Configure(new Color32(7,26,35,255),new Color32(2,12,20,255),accent,3);face.raycastTarget=false;
            var art=Rect(root,"NextMissionThumbnail");art.gameObject.AddComponent<RawImage>().raycastTarget=false;
            var artwork=art.gameObject.AddComponent<MissionAtlasArtworkView>();
            var titleShade=Rect(root,"ModeTitleShade").gameObject.AddComponent<V3GradientGraphic>();
            titleShade.Configure(new Color(0.01f,.03f,.05f,.88f),new Color(0.01f,.03f,.05f,0),Color.clear,0);titleShade.raycastTarget=false;
            var shade=Rect(root,"InformationBand").gameObject.AddComponent<V3GradientGraphic>();shade.raycastTarget=false;
            var iconCell=Rect(root,"ModeEmblemCell");
            iconCell.gameObject.AddComponent<V3GradientGraphic>().Configure(accent*.35f,new Color32(3,25,22,255),accent,2);
            var icon=Rect(iconCell,"ModeEmblem").gameObject.AddComponent<Image>();icon.raycastTarget=false;icon.preserveAspect=true;
            string path="Assets/Game/Art/UI/V3Shared/Sprites/MainMenuIcons/"+
                (mode==MainMenuMode.Operations?"SCN02_Icon_OperationsCompass_V3.png":"SCN02_Icon_SkirmishBlades_V3.png");
            icon.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            var title=Label(root,"ModeTitle",40,Color.white);
            var next=Label(root,"NextPlayableMission",25,accent);
            var progress=Label(root,"UniqueMissionProgress",27,Color.white);
            var track=Rect(root,"ProgressTrack");track.gameObject.AddComponent<V3GradientGraphic>().Configure(new Color32(18,45,58,255),new Color32(10,29,39,255),new Color32(55,83,98,255),1);
            var fill=Rect(track,"ProgressFill");fill.gameObject.AddComponent<V3GradientGraphic>().Configure(accent,accent*.65f,Color.clear,0);
            var medals=Rect(root,"DifficultyMedals");
            var counts=new TMP_Text[4];
            for(int i=0;i<4;i++)
            {
                var slot=Rect(medals,"Tier"+i);var star=Rect(slot,"Star").gameObject.AddComponent<MissionAtlasStatusGraphic>();
                star.Configure(MissionAtlasStatusGraphic.Kind.Medal,true,i);star.SetStandaloneStar(true);
                counts[i]=Label(slot,"Count",24,Color.white);
            }
            var rank=Label(root,"ModeRank",24,new Color32(235,194,111,255));
            var detail=Label(root,"MedalEligibility",21,new Color32(160,191,207,255));
            var hotspot=Rect(root,"Hotspot");
            var actionFace=hotspot.gameObject.AddComponent<V3GradientGraphic>();
            actionFace.SetMilitaryMaterial(MilitaryUiMaterialKind.BallisticWeave);
            var button=hotspot.gameObject.AddComponent<Button>();button.targetGraphic=actionFace;
            var colors=button.colors;colors.highlightedColor=new Color(1.15f,1.15f,1.15f);colors.pressedColor=new Color(.7f,.7f,.7f);button.colors=colors;
            var label=Label(hotspot,"Label",30,Color.white);label.alignment=TextAlignmentOptions.Center;Fill(label.rectTransform,6);
            var view=root.gameObject.AddComponent<MainMenuModeCardView>();
            view.Configure(mode,artwork,AssetDatabase.LoadAssetAtPath<Sprite>(OperationsPreviewPath),title,next,progress,label,rank,detail,counts,fill,track,iconCell,icon.rectTransform,medals,button,shade,actionFace);
            return view;
        }
        private static RectTransform Rect(Transform parent,string name)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);return r;}
        private static TMP_Text Label(Transform parent,string name,float size,Color color)
        {
            var r=Rect(parent,name);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=Font;t.fontSize=size;t.color=color;t.raycastTarget=false;t.alignment=TextAlignmentOptions.MidlineLeft;
            t.textWrappingMode=TextWrappingModes.NoWrap;t.overflowMode=TextOverflowModes.Ellipsis;t.margin=new Vector4(4,0,4,0);return t;
        }
        private static void Fill(RectTransform r,float margin)
        {r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=Vector2.one*margin;r.offsetMax=-Vector2.one*margin;}
    }
}
