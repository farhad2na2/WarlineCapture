#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Game.Configs;
using Game.UI.Runtime;
using Game.UI.Contracts;
using RTLTMPro;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Game.Editor
{
    public static class SupportPopupPrefabBuilder
    {
        public const string PopupPath="Assets/Game/Prefabs/UI/Shell/Popups/SupportPopup.prefab";
        public const string TargetingPath="Assets/Game/Prefabs/UI/Shell/SupportTargeting.prefab";
        public const string ArtworkFolder="Assets/Game/Art/UI/Support";
        private static readonly string[] ArtworkNames={"support-smoke-wide-v1.png","support-strike-wide-v1.png","support-paratroopers-wide-v1.png","support-supply-wide-v1.png"};
        private static readonly Color Dark=new Color32(12,23,29,250),Line=new Color32(107,126,137,255),Cyan=new Color32(0,178,222,255),Green=new Color32(42,174,55,255);
        private static TMP_FontAsset font;
        [MenuItem("Game/Support/Build Popup And Input")]
        public static void Build()
        {
            SupportAbilityCatalogBuilder.Build();
            ImportArtwork();
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset");
            var root=new GameObject("SupportPopup",typeof(RectTransform),typeof(CanvasGroup));
            try
            {
                Stretch(root.GetComponent<RectTransform>());
                var scrim=root.AddComponent<Image>();scrim.color=new Color(0,0,0,.65f);scrim.raycastTarget=true;
                var frame=Rect(root.transform,"SupportFrame",0,0,1672,941);
                frame.gameObject.AddComponent<MainMenuV3SectionLayoutView>().Configure(new Vector2(1672,941),MainMenuV3SectionAlignment.Center);
                Panel(frame,"Outer",112,16,1448,909,Dark,Line);
                var header=Panel(frame,"Header",128,30,1416,82,Dark,Line);
                Art(header,"Parachute",12,8,64,64,RequireSprite("Assets/Game/Art/UI/Icons/scn08_icon_support_parachute.png"));
                Text(header,"Title",86,4,405,70,"support.title",54);
                var resources=Text(header,"Resources",595,4,430,72,"support.fuel.available",30);
                var stop=Button(header,"Stop",1050,8,260,64,"support.stop_aria",Cyan,24);
                var close=Button(header,"Close",1330,8,72,64,"support.close",Line,20);
                var mission=Text(Panel(frame,"Context",128,122,1416,50,Dark,Line),"Mission",18,4,1380,42,"support.optional",23);
                var cards=new SupportAbilityCardView[4];var icons=new Sprite[4];
                for(int i=0;i<4;i++)
                {
                    float x=128+(i%2)*500,y=184+(i/2)*307;
                    var card=Panel(frame,"SupportCard"+(i+1),x,y,488,293,Dark,Line);
                    var button=card.gameObject.AddComponent<Button>();button.targetGraphic=card.GetComponent<V3GradientGraphic>();
                    icons[i]=RequireSprite(ArtworkFolder+"/"+ArtworkNames[i]);
                    CoverArt(card,"Artwork",4,43,480,190,icons[i]);
                    Text(card,"Name",18,3,452,38,"support.name."+(i+1),29);
                    var status=Text(card,"Status",18,234,452,54,"support.card.status",19);
                    cards[i]=card.gameObject.AddComponent<SupportAbilityCardView>();cards[i].Configure((byte)(i+1),button,status,card.GetComponent<V3GradientGraphic>());
                }
                var detail=Panel(frame,"Detail",1128,184,416,600,Dark,Line);
                var title=Text(detail,"Title",18,4,380,55,"support.name.1",30);
                var preview=CoverArt(detail,"Artwork",12,70,392,218,icons[0]);
                var description=Text(detail,"Description",18,301,380,157,"support.description.1",23);
                var requirements=Text(detail,"Requirements",18,475,380,110,"support.detail.cost",24);
                Text(Panel(frame,"Instruction",128,798,972,106,Dark,Line),"Instruction",16,10,940,86,"support.instruction",23);
                var begin=Button(frame,"BeginTargeting",1112,798,432,106,"support.choose.area",Green,32);
                var view=root.AddComponent<SupportPopupView>();view.Configure(cards,icons,preview,title,description,requirements,resources,mission,
                    begin.GetComponentInChildren<TMP_Text>(),close,begin,stop);
                UIPopupMotionView.Ensure(root);
                PrefabUtility.SaveAsPrefabAsset(root,PopupPath);
            }
            finally {UnityEngine.Object.DestroyImmediate(root);}
            BuildTargeting();Register();InstallCopy();AssetDatabase.SaveAssets();
            Debug.Log("[SupportPopupPrefabBuilder] result=Passed cards=4 artwork=wide-cover aria=HeaderStop popup=fullscreen");
        }
        private static void BuildTargeting()
        {
            var root=new GameObject("SupportTargeting",typeof(RectTransform));Stretch(root.GetComponent<RectTransform>());
            try
            {
                var bar=Panel(root.transform,"PreviewBar",0,0,1500,180,Dark,Line);
                bar.gameObject.AddComponent<MainMenuV3SectionLayoutView>().Configure(new Vector2(1672,180),MainMenuV3SectionAlignment.BottomCenter);
                var status=Text(bar,"Status",20,12,1460,60,"support.target.instruction",23);
                var confirm=Button(bar,"Confirm",20,88,210,72,"support.confirm",Green,25);
                var cancel=Button(bar,"Cancel",244,88,180,72,"support.cancel",Line,24);
                var propose=Button(bar,"Propose",438,88,215,72,"support.propose",Cyan,24);
                var approve=Button(bar,"Approve",667,88,230,72,"support.approve",Green,24);
                var decline=Button(bar,"Decline",911,88,230,72,"support.decline",Line,24);
                var show=Button(bar,"ShowTarget",1155,88,325,72,"support.show",Cyan,24);
                var footprintObject=new GameObject("SupportFootprint");footprintObject.transform.SetParent(root.transform,false);
                var footprint=footprintObject.AddComponent<LineRenderer>();footprint.positionCount=65;footprint.useWorldSpace=true;
                footprint.startWidth=footprint.endWidth=.12f;footprint.loop=false;footprint.enabled=false;
                var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Configs/Support/SupportFootprint.mat");
                if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(material,"Assets/Game/Configs/Support/SupportFootprint.mat");}
                footprint.sharedMaterial=material;
                var flightLayout=Rect(root.transform,"FlightStatusLayout",0,0,1672,941);
                flightLayout.gameObject.AddComponent<MainMenuV3SectionLayoutView>().Configure(new Vector2(1672,941),MainMenuV3SectionAlignment.BottomCenter);
                var banner=Panel(flightLayout,"FlightStatus",0,0,1000,60,Dark,Cyan);
                banner.anchorMin=banner.anchorMax=new Vector2(.5f,0);banner.pivot=new Vector2(.5f,0);banner.anchoredPosition=new Vector2(0,194);
                banner.gameObject.AddComponent<CanvasGroup>().blocksRaycasts=false;
                var flightStatus=Text(banner,"Message",14,4,972,52,"support.optional",22);flightStatus.raycastTarget=false;
                banner.gameObject.SetActive(false);
                root.AddComponent<SupportTargetingInputUiSystemHelper>().Configure(bar.gameObject,status,confirm,cancel,propose,approve,decline,show,footprint,flightStatus);
                PrefabUtility.SaveAsPrefabAsset(root,TargetingPath);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        private static void Register()
        {
            var popup=AssetDatabase.LoadAssetAtPath<GameObject>(PopupPath);var targeting=AssetDatabase.LoadAssetAtPath<GameObject>(TargetingPath);
            int registered=0;
            foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Game/Prefabs/UI"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(asset.GetComponentInChildren<UIShellContentView>(true)==null)continue;
                var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach(var view in root.GetComponentsInChildren<UIShellContentView>(true))
                    {var serialized=new SerializedObject(view);serialized.FindProperty("supportPopupPrefab").objectReferenceValue=popup;serialized.ApplyModifiedPropertiesWithoutUndo();registered++;}
                    if(root.GetComponentInChildren<SupportTargetingInputUiSystemHelper>(true)==null)PrefabUtility.InstantiatePrefab(targeting,root.transform);
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            // The shipped shell is authored directly in Menu; update only its Support reference/input child.
            const string scenePath="Assets/Game/Scenes/Menu.unity";
            if(File.Exists(scenePath))
            {
                var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
                bool wasOpen=scene.IsValid() && scene.isLoaded;
                if(!wasOpen)scene=EditorSceneManager.OpenScene(scenePath,UnityEditor.SceneManagement.OpenSceneMode.Additive);
                try
                {
                    foreach(var root in scene.GetRootGameObjects()) foreach(var view in root.GetComponentsInChildren<UIShellContentView>(true))
                    {
                        var serialized=new SerializedObject(view);serialized.FindProperty("supportPopupPrefab").objectReferenceValue=popup;serialized.ApplyModifiedPropertiesWithoutUndo();registered++;
                        if(view.GetComponentInChildren<SupportTargetingInputUiSystemHelper>(true)==null)PrefabUtility.InstantiatePrefab(targeting,view.transform);
                    }
                    EditorSceneManager.SaveScene(scene);
                }
                finally{if(!wasOpen)EditorSceneManager.CloseScene(scene,true);}
            }
            if(registered==0)throw new InvalidOperationException("No shipped UIShellContentView received Support references.");
            Debug.Log("[SupportPopupRegistration] result=Passed shells="+registered);
        }
        private static void InstallCopy()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
            var tables=new List<GameLocaleTable>();
            foreach(var locale in catalog.Locales)
            {
                var records=new List<GameLocalizedStringRecord>();
                var managedKeys=new HashSet<string>(StringComparer.Ordinal);
                foreach(var entry in SupportUiCopyCatalog.Entries)managedKeys.Add(entry.Key);
                foreach(var record in locale.Entries)if(!managedKeys.Contains(record.Key))records.Add(record);
                foreach(var entry in SupportUiCopyCatalog.Entries)records.Add(new GameLocalizedStringRecord(entry.Key,locale.LocaleCode=="fa-IR"?entry.Persian:entry.English));
                tables.Add(new GameLocaleTable(locale.LocaleCode,locale.DisplayName,locale.ShortLabel,locale.RightToLeft,locale.FontAsset,records));
            }
            catalog.Configure(catalog.SourceLocaleCode,tables);EditorUtility.SetDirty(catalog);
        }
        private static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();
            r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
        }
        private static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        private static RectTransform Panel(Transform parent,string name,float x,float y,float w,float h,Color fill,Color line)
        {var r=Rect(parent,name,x,y,w,h);r.gameObject.AddComponent<V3GradientGraphic>().Configure(fill,new Color32(2,8,12,255),line,2);return r;}
        private static TMP_Text Text(Transform parent,string name,float x,float y,float w,float h,string key,float size)
        {
            var r=Rect(parent,name,x,y,w,h);var text=r.gameObject.AddComponent<RTLTextMeshPro>();text.font=font;text.fontSize=size;
            text.enableAutoSizing=true;text.fontSizeMin=size*.8f;text.fontSizeMax=size;text.color=Color.white;text.alignment=TextAlignmentOptions.MidlineLeft;
            text.raycastTarget=false;string fallback="";foreach(var entry in SupportUiCopyCatalog.Entries)if(entry.Key==key){fallback=entry.English;break;}
            text.text=fallback;var binding=r.gameObject.AddComponent<V3LocalizedTextBindingView>();binding.Configure(key,fallback,false);return text;
        }
        private static Image Art(Transform parent,string name,float x,float y,float w,float h,Sprite sprite)
        {var r=Rect(parent,name,x,y,w,h);r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x+w*.5f,-y-h*.5f);
         var image=r.gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;return image;}
        private static Image CoverArt(Transform parent,string name,float x,float y,float w,float h,Sprite sprite)
        {
            var viewport=Rect(parent,name,x,y,w,h);viewport.gameObject.AddComponent<RectMask2D>();
            var image=Art(viewport,"Image",0,0,w,h,sprite);image.preserveAspect=false;
            var rect=image.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;
            var fitter=image.gameObject.AddComponent<AspectRatioFitter>();fitter.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio=sprite.rect.width/sprite.rect.height;return image;
        }
        private static Button Button(Transform parent,string name,float x,float y,float w,float h,string key,Color border,float size)
        {
            var r=Panel(parent,name,x,y,w,h,new Color(border.r*.25f,border.g*.25f,border.b*.25f,1),border);
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<V3GradientGraphic>();
            var text=Text(r,"Label",6,4,w-12,h-8,key,size);text.alignment=TextAlignmentOptions.Center;return b;
        }
        private static Sprite RequireSprite(string path)
        {var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(sprite==null)throw new FileNotFoundException(path);return sprite;}
        private static void ImportArtwork()
        {
            foreach(var name in ArtworkNames)
            {
                string path=ArtworkFolder+"/"+name;
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(importer==null)throw new FileNotFoundException(path);
                if(importer.textureType==TextureImporterType.Sprite&&importer.spriteImportMode==SpriteImportMode.Single&&
                    !importer.mipmapEnabled&&importer.wrapMode==TextureWrapMode.Clamp&&importer.maxTextureSize==2048)continue;
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=2048;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
                settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.alphaIsTransparency=false;
                importer.SaveAndReimport();
            }
        }
    }
}
#endif
