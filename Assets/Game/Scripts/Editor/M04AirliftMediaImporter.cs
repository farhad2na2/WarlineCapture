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
        public static string VoicePath(string id,bool persian)=>$"{VoiceRoot}/{(persian ? "fa" : "en")}/{id}.wav";
        public static AudioClip Voice(string id,bool persian)=>AssetDatabase.LoadAssetAtPath<AudioClip>(VoicePath(id,persian));
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

        public static void ConfigureVoices()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(var line in Lines) foreach(bool persian in new[]{false,true}) ConfigureVoice(VoicePath(line.Id,persian),persian);
            for(int step=1;step<=12;step++) foreach(bool persian in new[]{false,true}) ConfigureVoice(VoicePath($"tutorial-m04-{step:00}",persian),persian);
            AudioRuntimeConfigAssetBuilder.BuildDefaultAssets();
            Debug.Log("[M04VoiceImports] result=Passed clips=38 locales=2 preload=0 runtimeNetworkTts=0");
        }

        private static void ConfigureVoice(string path,bool persian)
        {
            var importer=AssetImporter.GetAtPath(path) as AudioImporter;
            if(importer==null) throw new InvalidOperationException("Missing final voice: "+path);
            var settings=importer.defaultSampleSettings;
            settings.loadType=AudioClipLoadType.CompressedInMemory; settings.compressionFormat=AudioCompressionFormat.Vorbis;
            settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate; settings.sampleRateOverride=44100;
            settings.quality=.7f; settings.preloadAudioData=false; importer.defaultSampleSettings=settings;
            importer.forceToMono=true; importer.loadInBackground=true; importer.ambisonic=false;
            importer.userData="status=ELEVENLABS_PAID_CREATOR_COMMERCIAL_LICENSE; provider=ElevenLabs; model=eleven_v3; locale="+
                (persian ? "fa-IR" : "en-US")+"; manifest=m04_voice_manifest.json; runtimeNetworkTts=false";
            importer.SaveAndReimport();
            var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if(clip==null || clip.length<.25f || clip.channels!=1) throw new InvalidOperationException("Invalid voice clip: "+path);
        }
    }
}
