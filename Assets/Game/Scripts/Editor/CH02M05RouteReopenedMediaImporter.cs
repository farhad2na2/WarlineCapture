using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Configs;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class CH02M05RouteReopenedMediaImporter
    {
        public const string ArtPath="Assets/Game/Art/Narrative/CH02M05RouteReopened/RouteReopened.png";
        public const string SourceArt="Design/AgentReports/CH02M05RouteReopened/Mockups/ch02m05-route-reopened-briefing-v01.png";
        public const string VoiceRoot="Assets/Game/Audio/Narrative/CH02M05RouteReopened/Voice";
        public static IEnumerable<RouteReopenedNarrativeLine> Lines=>CH02M05RouteReopenedCopy.Brief.Concat(CH02M05RouteReopenedCopy.Comms).Concat(CH02M05RouteReopenedCopy.Debrief);
        public static string VoicePath(string id,bool persian)=>$"{VoiceRoot}/{(persian?"fa":"en")}/{id}.wav";
        public static AudioClip Voice(string id,bool persian)=>AssetDatabase.LoadAssetAtPath<AudioClip>(VoicePath(id,persian));
        public static Sprite Panel()=>AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath);
        public static Texture Preview()=>AssetDatabase.LoadAssetAtPath<Texture>(ArtPath);
        public static void ConfigureArt()
        {
            if(!File.Exists(SourceArt))throw new InvalidOperationException("Approved Route Reopened visual is missing: "+SourceArt);
            Directory.CreateDirectory(Path.GetDirectoryName(ArtPath));
            if(!File.Exists(ArtPath) || File.GetLastWriteTimeUtc(SourceArt)>File.GetLastWriteTimeUtc(ArtPath))File.Copy(SourceArt,ArtPath,true);
            AssetDatabase.ImportAsset(ArtPath,ImportAssetOptions.ForceSynchronousImport);
            var importer=AssetImporter.GetAtPath(ArtPath) as TextureImporter??throw new InvalidOperationException("Route Reopened visual could not be imported.");
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.isReadable=false;importer.maxTextureSize=2048;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
            if(Panel()==null)throw new InvalidOperationException("Route Reopened sprite missing after import.");
            Debug.Log("[RouteReopenedVisual] result=Passed approvedMockup=1 playerFacing=1");
        }
        public static void ConfigureVoices()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);int count=0;
            foreach(var line in Lines)foreach(bool persian in new[]{false,true})
            {
                string path=VoicePath(line.Id,persian);var importer=AssetImporter.GetAtPath(path) as AudioImporter??throw new InvalidOperationException("Missing final voice: "+path);
                var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.CompressedInMemory;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;settings.quality=.7f;settings.preloadAudioData=false;importer.defaultSampleSettings=settings;
                importer.forceToMono=true;importer.loadInBackground=true;importer.ambisonic=false;importer.userData="status=ELEVENLABS_PAID_CREATOR_COMMERCIAL_LICENSE; provider=ElevenLabs; model=eleven_v3; locale="+(persian?"fa-IR":"en-US")+"; manifest=route_reopened_voice_manifest.json; runtimeNetworkTts=false";importer.SaveAndReimport();
                var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);if(clip==null||clip.length<.25f||clip.channels!=1)throw new InvalidOperationException("Invalid voice clip: "+path);count++;
            }
            AudioRuntimeConfigAssetBuilder.BuildDefaultAssets();Debug.Log($"[RouteReopenedVoiceImports] result=Passed clips={count} locales=2 preload=0 runtimeNetworkTts=0");
        }
    }
}
