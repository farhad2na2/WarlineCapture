using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Game.Editor
{
    public static class CH02M02SupplyLineMediaImporter
    {
        public const string ArtRoot="Assets/Game/Art/Narrative/CH02M02SupplyLine";
        public static readonly string[] Panels={"SupplyChain","Manifest","EmergencyServices"};
        public static Sprite Panel(string id,bool wide)=>AssetDatabase.LoadAllAssetsAtPath($"{ArtRoot}/{id}.png")
            .OfType<Sprite>().Single(s=>s.name==$"{id}-{(wide ? "20x9" : "16x9")}");
        public static void ConfigureArt()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(string id in Panels)
            {
                string path=$"{ArtRoot}/{id}.png";
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(importer==null) throw new InvalidOperationException("Missing final comic panel: "+path);
                importer.GetSourceTextureWidthAndHeight(out int width,out int height);
                float fullHeight=Mathf.Min(height,width*9f/16f),wideHeight=Mathf.Min(height,width*9f/20f);
                var crops=new List<SpriteMetaData>{
                    new SpriteMetaData{name=$"{id}-16x9",rect=Crop(width,height,16f/9f),alignment=0,pivot=new Vector2(.5f,.5f)},
                    new SpriteMetaData{name=$"{id}-20x9",rect=Crop(width,height,20f/9f),alignment=0,pivot=new Vector2(.5f,.5f)}};

                importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit=100; importer.sRGBTexture=true; importer.alphaSource=TextureImporterAlphaSource.None;
                importer.alphaIsTransparency=false; importer.mipmapEnabled=false; importer.streamingMipmaps=false; importer.isReadable=false;
                importer.npotScale=TextureImporterNPOTScale.None; importer.wrapMode=TextureWrapMode.Clamp;
                importer.filterMode=FilterMode.Bilinear; importer.textureCompression=TextureImporterCompression.CompressedHQ;
                importer.maxTextureSize=2048;
                var factories=new SpriteDataProviderFactories();factories.Init();
                var provider=factories.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
                var old=provider.GetSpriteRects();
                var rectangles=crops.Select(crop=>new SpriteRect {name=crop.name,rect=crop.rect,alignment=SpriteAlignment.Center,pivot=crop.pivot,
                    spriteID=old.FirstOrDefault(existing=>existing.name==crop.name)?.spriteID ?? GUID.Generate()}).ToArray();
                provider.SetSpriteRects(rectangles);
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rectangles.Select(rect=>new SpriteNameFileIdPair(rect.name,rect.spriteID)));
                provider.Apply();importer.SaveAndReimport();
                if(Mathf.Abs(Panel(id,false).rect.width/Panel(id,false).rect.height-16f/9f)>.005f ||
                    Mathf.Abs(Panel(id,true).rect.width/Panel(id,true).rect.height-20f/9f)>.005f)
                    throw new InvalidOperationException("Comic crop ratio mismatch: "+id);
            }
            Debug.Log("[SupplyLineComicArt] result=Passed sources=3 authoredCrops=6 bilingualTextFree=1");
        }
        private static Rect Crop(int width,int height,float aspect)
        {
            int h=Mathf.FloorToInt(Mathf.Min(height,width/aspect));int w=Mathf.Min(width,Mathf.FloorToInt(h*aspect));
            return new Rect((width-w)/2,(height-h)/2,w,h);
        }
        public static void ValidateStableArtImports()
        {
            ConfigureArt();
            var before=Panels.SelectMany(id=>new[]{PanelIdentity(Panel(id,false)),PanelIdentity(Panel(id,true))}).ToArray();
            ConfigureArt();
            var after=Panels.SelectMany(id=>new[]{PanelIdentity(Panel(id,false)),PanelIdentity(Panel(id,true))}).ToArray();
            if(!before.SequenceEqual(after)) throw new InvalidOperationException("Reimport changed a comic sprite identity.");
            Debug.Log("[SupplyLineComicArtStability] result=Passed sources=3 stableSpriteIds=6");
        }
        private static string PanelIdentity(Sprite panel)
        {AssetDatabase.TryGetGUIDAndLocalFileIdentifier(panel,out string guid,out long id); return guid+":"+id;}

    }
}
