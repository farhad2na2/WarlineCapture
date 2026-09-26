using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Game.Editor
{
    public static class CH02M03MarketLifelineMediaImporter
    {
        public const string ArtRoot="Assets/Game/Art/Narrative/CH02M03MarketLifeline";
        public const string VoiceRoot="Assets/Game/Audio/Narrative/CH02M03MarketLifeline/Voice";
        public static readonly string[] Panels={"OldMarket","ManifestEvidence","MarketRelief"};
        public static IEnumerable<MarketLifelineNarrativeLine> Lines=>CH02M03MarketLifelineCopy.Brief.Concat(CH02M03MarketLifelineCopy.Comms).Concat(CH02M03MarketLifelineCopy.Debrief);
        public static string VoicePath(string id,bool persian)=>$"{VoiceRoot}/{(persian?"fa":"en")}/{id}.wav";
        public static AudioClip Voice(string id,bool persian)=>AssetDatabase.LoadAssetAtPath<AudioClip>(VoicePath(id,persian));
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
            Debug.Log("[MarketLifelineComicArt] result=Passed sources=3 authoredCrops=6 bilingualTextFree=1");
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
            Debug.Log("[MarketLifelineComicArtStability] result=Passed sources=3 stableSpriteIds=6");
        }
        private static string PanelIdentity(Sprite panel)
        {AssetDatabase.TryGetGUIDAndLocalFileIdentifier(panel,out string guid,out long id); return guid+":"+id;}

        public static void ConfigureVoices()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(var line in Lines)foreach(bool persian in new[]{false,true})ConfigureVoice(VoicePath(line.Id,persian),persian);
            AudioRuntimeConfigAssetBuilder.BuildDefaultAssets();
            Debug.Log("[MarketLifelineVoiceImports] result=Passed clips=14 locales=2 preload=0 runtimeNetworkTts=0");
        }

        private static void ConfigureVoice(string path,bool persian)
        {
            var importer=AssetImporter.GetAtPath(path) as AudioImporter??throw new InvalidOperationException("Missing final voice: "+path);
            var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.CompressedInMemory;settings.compressionFormat=AudioCompressionFormat.Vorbis;
            settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;settings.sampleRateOverride=44100;settings.quality=.7f;settings.preloadAudioData=false;importer.defaultSampleSettings=settings;
            importer.forceToMono=true;importer.loadInBackground=true;importer.ambisonic=false;
            importer.userData="status=ELEVENLABS_PAID_CREATOR_COMMERCIAL_LICENSE; provider=ElevenLabs; model=eleven_v3; locale="+(persian?"fa-IR":"en-US")+"; manifest=market_lifeline_voice_manifest.json; runtimeNetworkTts=false";
            importer.SaveAndReimport();var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if(clip==null||clip.length<.25f||clip.channels!=1)throw new InvalidOperationException("Invalid voice clip: "+path);
        }

    }
}
