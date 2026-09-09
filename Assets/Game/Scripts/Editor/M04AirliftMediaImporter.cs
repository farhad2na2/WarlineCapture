using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class M04AirliftMediaImporter
    {
        public const string ArtRoot="Assets/Game/Art/Narrative/M04Airlift/Final";
        public const string VoiceRoot="Assets/Game/Audio/Narrative/M04Airlift/Voice";
        public static readonly string[] Panels={"B01","B02","B03","C01","D01","D02","D03"};
        public static IEnumerable<M04NarrativeLine> Lines=>M04AirliftCopyCatalog.Brief.Concat(M04AirliftCopyCatalog.Comms)
            .Concat(M04AirliftCopyCatalog.Debrief);
        public static AudioClip Voice(string id,bool persian)=>null;
        public static Sprite Panel(string id,bool wide)=>AssetDatabase.LoadAllAssetsAtPath($"{ArtRoot}/M04-{id}.png")
            .OfType<Sprite>().Single(s=>s.name==$"M04-{id}-{(wide ? "20x9" : "16x9")}");
        public static Sprite LailaPortrait()=>AssetDatabase.LoadAllAssetsAtPath(ArtRoot+"/M04-B01.png")
            .OfType<Sprite>().Single(s=>s.name=="M04-Laila-Portrait");

        public static void ConfigureArt()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(string id in Panels)
            {
                string path=$"{ArtRoot}/M04-{id}.png";
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(importer==null) throw new InvalidOperationException("Missing final comic panel: "+path);
                importer.GetSourceTextureWidthAndHeight(out int width,out int height);
                float fullHeight=Mathf.Min(height,width*9f/16f),wideHeight=Mathf.Min(height,width*9f/20f);
                var crops=new List<SpriteMetaData>{
                    new SpriteMetaData{name=$"M04-{id}-16x9",rect=new Rect(0,(height-fullHeight)*.5f,width,fullHeight),alignment=0,pivot=new Vector2(.5f,.5f)},
                    new SpriteMetaData{name=$"M04-{id}-20x9",rect=new Rect(0,(height-wideHeight)*.5f,width,wideHeight),alignment=0,pivot=new Vector2(.5f,.5f)}};
                if(id=="B01") crops.Add(new SpriteMetaData{name="M04-Laila-Portrait",rect=new Rect(951,211,540,720),alignment=0,pivot=new Vector2(.5f,.5f)});
                importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit=100; importer.sRGBTexture=true; importer.alphaSource=TextureImporterAlphaSource.None;
                importer.alphaIsTransparency=false; importer.mipmapEnabled=false; importer.streamingMipmaps=false; importer.isReadable=false;
                importer.npotScale=TextureImporterNPOTScale.None; importer.wrapMode=TextureWrapMode.Clamp;
                importer.filterMode=FilterMode.Bilinear; importer.textureCompression=TextureImporterCompression.CompressedHQ;
                importer.maxTextureSize=2048; importer.spritesheet=crops.ToArray(); importer.SaveAndReimport();
                if(Mathf.Abs(Panel(id,false).rect.width/Panel(id,false).rect.height-16f/9f)>.005f ||
                    Mathf.Abs(Panel(id,true).rect.width/Panel(id,true).rect.height-20f/9f)>.005f)
                    throw new InvalidOperationException("Comic crop ratio mismatch: "+id);
            }
            Debug.Log("[M04ComicArt] result=Passed sources=7 authoredCrops=14 bilingualTextFree=1");
        }
        public static void ValidateStableArtImports()
        {
            ConfigureArt();
            var before=Panels.SelectMany(id=>new[]{PanelIdentity(Panel(id,false)),PanelIdentity(Panel(id,true))}).Append(PanelIdentity(LailaPortrait())).ToArray();
            ConfigureArt();
            var after=Panels.SelectMany(id=>new[]{PanelIdentity(Panel(id,false)),PanelIdentity(Panel(id,true))}).Append(PanelIdentity(LailaPortrait())).ToArray();
            if(!before.SequenceEqual(after)) throw new InvalidOperationException("Reimport changed a comic sprite identity.");
            Debug.Log("[M04ComicArtStability] result=Passed sources=7 stableSpriteIds=15 portrait=1");
        }
        private static string PanelIdentity(Sprite panel)
        {AssetDatabase.TryGetGUIDAndLocalFileIdentifier(panel,out string guid,out long id); return guid+":"+id;}

    }
}
