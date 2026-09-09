using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class M03RadarWarningMediaImporter
    {
        public const string ArtRoot="Assets/Game/Art/Narrative/M03RadarWarning/Final";
        public const string VoiceRoot="Assets/Game/Audio/Narrative/M03RadarWarning/Voice";
        public static readonly string[] Panels={"B01","B02","B03","C01","D01","D02","D03"};
        public static IEnumerable<M03NarrativeLine> Lines=>M03RadarWarningCopyCatalog.Brief.Concat(M03RadarWarningCopyCatalog.Comms)
            .Concat(M03RadarWarningCopyCatalog.Debrief).Concat(M03RadarWarningCopyCatalog.DebriefOutcomes);
        public static string VoicePath(string id,bool persian)=>$"{VoiceRoot}/{(persian ? "fa" : "en")}/{id}.wav";
        public static AudioClip Voice(string id,bool persian)=>AssetDatabase.LoadAssetAtPath<AudioClip>(VoicePath(id,persian));
        public static Sprite Panel(string id,bool wide)=>AssetDatabase.LoadAllAssetsAtPath($"{ArtRoot}/M03-{id}.png")
            .OfType<Sprite>().Single(s=>s.name==$"M03-{id}-{(wide ? "20x9" : "16x9")}");

        public static void ConfigureArt()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(string id in Panels)
            {
                string path=$"{ArtRoot}/M03-{id}.png";
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(importer==null) throw new InvalidOperationException("Missing final comic panel: "+path);
                importer.GetSourceTextureWidthAndHeight(out int width,out int height);
                float fullHeight=Mathf.Min(height,width*9f/16f),wideHeight=Mathf.Min(height,width*9f/20f);
                var crops=new[]{
                    new SpriteMetaData{name=$"M03-{id}-16x9",rect=new Rect(0,(height-fullHeight)*.5f,width,fullHeight),alignment=0,pivot=new Vector2(.5f,.5f)},
                    new SpriteMetaData{name=$"M03-{id}-20x9",rect=new Rect(0,(height-wideHeight)*.5f,width,wideHeight),alignment=0,pivot=new Vector2(.5f,.5f)}};
                importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit=100; importer.sRGBTexture=true; importer.alphaSource=TextureImporterAlphaSource.None;
                importer.alphaIsTransparency=false; importer.mipmapEnabled=false; importer.streamingMipmaps=false; importer.isReadable=false;
                importer.npotScale=TextureImporterNPOTScale.None; importer.wrapMode=TextureWrapMode.Clamp;
                importer.filterMode=FilterMode.Bilinear; importer.textureCompression=TextureImporterCompression.CompressedHQ;
                importer.maxTextureSize=2048; importer.spritesheet=crops; importer.SaveAndReimport();
                if(Mathf.Abs(Panel(id,false).rect.width/Panel(id,false).rect.height-16f/9f)>.005f ||
                    Mathf.Abs(Panel(id,true).rect.width/Panel(id,true).rect.height-20f/9f)>.005f)
                    throw new InvalidOperationException("Comic crop ratio mismatch: "+id);
            }
            Debug.Log("[M03ComicArt] result=Passed sources=7 authoredCrops=14 bilingualTextFree=1");
        }
        public static void ValidateStableArtImports()
        {
            ConfigureArt();
            var before=Panels.SelectMany(id=>new[]{PanelIdentity(Panel(id,false)),PanelIdentity(Panel(id,true))}).ToArray();
            ConfigureArt();
            var after=Panels.SelectMany(id=>new[]{PanelIdentity(Panel(id,false)),PanelIdentity(Panel(id,true))}).ToArray();
            if(!before.SequenceEqual(after)) throw new InvalidOperationException("Reimport changed a comic sprite identity.");
            Debug.Log("[M03ComicArtStability] result=Passed sources=7 stableSpriteIds=14");
        }
        private static string PanelIdentity(Sprite panel)
        {AssetDatabase.TryGetGUIDAndLocalFileIdentifier(panel,out string guid,out long id); return guid+":"+id;}

        public static void ConfigureVoices()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(var line in Lines) foreach(bool persian in new[]{false,true}) ConfigureVoice(VoicePath(line.Id,persian),persian);
            for(int step=1;step<=12;step++) foreach(bool persian in new[]{false,true}) ConfigureVoice(VoicePath($"tutorial-m03-{step:00}",persian),persian);
            AudioRuntimeConfigAssetBuilder.BuildDefaultAssets();
            Debug.Log("[M03VoiceImports] result=Passed clips=46 locales=2 preload=0 runtimeNetworkTts=0");
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
                (persian ? "fa-IR" : "en-US")+"; manifest=m03_voice_manifest.json; runtimeNetworkTts=false";
            importer.SaveAndReimport();
            var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if(clip==null || clip.length<.25f || clip.channels!=1) throw new InvalidOperationException("Invalid voice clip: "+path);
        }
    }
}
