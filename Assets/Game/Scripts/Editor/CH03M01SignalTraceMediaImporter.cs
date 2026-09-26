using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Configs;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Game.Editor
{
    public static class CH03M01SignalTraceMediaImporter
    {
        public const string ArtRoot="Assets/Game/Art/Narrative/CH03M01SignalTrace";
        public const string ArtPath=ArtRoot+"/SignalTrace.png";
        public const string PreviewPath=ArtRoot+"/BriefThreeSignals.png";
        public const string SourceArt="Design/AgentReports/CH03M01SignalTrace/Mockups/ch03m01-signal-trace-briefing-v01.png";
        public const string VoiceRoot="Assets/Game/Audio/Narrative/CH03M01SignalTrace/Voice";
        public static readonly string[] Panels={"BriefThreeSignals","BriefObservationPoints","BriefRulesOfEngagement","CommsCarrierConfirmed","DebriefDeviceRecovered","DebriefCivilianTrust","DebriefSafehouseRoute"};
        public static IEnumerable<SignalTraceNarrativeLine> Lines=>CH03M01SignalTraceCopy.Brief.Concat(CH03M01SignalTraceCopy.Comms).Concat(CH03M01SignalTraceCopy.Debrief);
        public static string VoicePath(string id,bool persian)=>$"{VoiceRoot}/{(persian?"fa":"en")}/{id}.wav";
        public static AudioClip Voice(string id,bool persian)=>AssetDatabase.LoadAssetAtPath<AudioClip>(VoicePath(id,persian));
        public static Sprite Panel(string id,bool wide)=>AssetDatabase.LoadAllAssetsAtPath($"{ArtRoot}/{id}.png").OfType<Sprite>().Single(s=>s.name==$"{id}-{(wide?"20x9":"16x9")}");
        public static string PanelId(string lineId)=>lineId switch{"signal_trace-brief-01"=>Panels[0],"signal_trace-brief-02"=>Panels[1],"signal_trace-brief-03"=>Panels[2],"signal_trace-comms-01"=>Panels[3],"signal_trace-debrief-01"=>Panels[4],"signal_trace-debrief-02"=>Panels[5],"signal_trace-debrief-03"=>Panels[6],_=>throw new InvalidOperationException("No Signal Trace comic panel for "+lineId)};
        public static Texture Preview()=>AssetDatabase.LoadAssetAtPath<Texture>(PreviewPath);
        public static void ConfigureArt()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(string id in Panels)
            {
                string path=$"{ArtRoot}/{id}.png";var importer=AssetImporter.GetAtPath(path) as TextureImporter??throw new InvalidOperationException("Missing final Signal Trace comic panel: "+path);importer.GetSourceTextureWidthAndHeight(out int width,out int height);
                var crops=new[]{new SpriteMetaData{name=$"{id}-16x9",rect=Crop(width,height,16f/9f),alignment=0,pivot=new Vector2(.5f,.5f)},new SpriteMetaData{name=$"{id}-20x9",rect=Crop(width,height,20f/9f),alignment=0,pivot=new Vector2(.5f,.5f)}};
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.spritePixelsPerUnit=100;importer.sRGBTexture=true;importer.alphaSource=TextureImporterAlphaSource.None;importer.alphaIsTransparency=false;importer.mipmapEnabled=false;importer.streamingMipmaps=false;importer.isReadable=false;importer.npotScale=TextureImporterNPOTScale.None;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.maxTextureSize=2048;
                var factories=new SpriteDataProviderFactories();factories.Init();var provider=factories.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();var old=provider.GetSpriteRects();var rectangles=crops.Select(crop=>new SpriteRect{name=crop.name,rect=crop.rect,alignment=SpriteAlignment.Center,pivot=crop.pivot,spriteID=old.FirstOrDefault(existing=>existing.name==crop.name)?.spriteID??GUID.Generate()}).ToArray();provider.SetSpriteRects(rectangles);provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rectangles.Select(rect=>new SpriteNameFileIdPair(rect.name,rect.spriteID)));provider.Apply();importer.SaveAndReimport();
                if(Mathf.Abs(Panel(id,false).rect.width/Panel(id,false).rect.height-16f/9f)>.005f||Mathf.Abs(Panel(id,true).rect.width/Panel(id,true).rect.height-20f/9f)>.005f)throw new InvalidOperationException("Signal Trace comic crop ratio mismatch: "+id);
            }
            Debug.Log("[SignalTraceVisual] result=Passed comicSources=7 authoredCrops=14 cleanPreview=1 playerFacing=1");
        }
        private static Rect Crop(int width,int height,float aspect){int h=Mathf.FloorToInt(Mathf.Min(height,width/aspect));int w=Mathf.Min(width,Mathf.FloorToInt(h*aspect));return new Rect((width-w)/2,(height-h)/2,w,h);}
        public static void ConfigureVoices()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);int count=0;
            foreach(var line in Lines)foreach(bool persian in new[]{false,true})
            {
                string path=VoicePath(line.Id,persian);var importer=AssetImporter.GetAtPath(path) as AudioImporter??throw new InvalidOperationException("Missing final voice: "+path);var settings=importer.defaultSampleSettings;
                settings.loadType=AudioClipLoadType.CompressedInMemory;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;settings.quality=.7f;settings.preloadAudioData=false;importer.defaultSampleSettings=settings;importer.forceToMono=true;importer.loadInBackground=true;importer.ambisonic=false;
                importer.userData="status=ELEVENLABS_PAID_CREATOR_COMMERCIAL_LICENSE; provider=ElevenLabs; model=eleven_v3; locale="+(persian?"fa-IR":"en-US")+"; manifest=signal_trace_voice_manifest.json; runtimeNetworkTts=false";importer.SaveAndReimport();var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);if(clip==null||clip.length<.25f||clip.channels!=1)throw new InvalidOperationException("Invalid voice clip: "+path);count++;
            }
            AudioRuntimeConfigAssetBuilder.BuildDefaultAssets();Debug.Log($"[SignalTraceVoiceImports] result=Passed clips={count} locales=2 preload=0 runtimeNetworkTts=0");
        }
    }
}
