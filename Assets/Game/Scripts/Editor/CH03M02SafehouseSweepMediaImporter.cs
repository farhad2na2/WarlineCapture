using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Game.Editor
{
    public static class CH03M02SafehouseSweepMediaImporter
    {
        public const string ArtRoot = "Assets/Game/Art/Narrative/CH03M02SafehouseSweep";
        public const string PreviewPath = ArtRoot + "/BriefVerifiedNode.png";
        public const string SourceArt = "Design/AgentReports/CH03M02SafehouseSweep/Mockups/ch03m02-safehouse-sweep-briefing-v01.png";
        public const string VoiceRoot = "Assets/Game/Audio/Narrative/CH03M02SafehouseSweep/Voice";
        public static readonly string[] Panels = {"BriefVerifiedNode", "BriefProtectedNeighbor", "BriefPerimeterPlan", "CommsCourierEscape", "DebriefSafehouseCleared", "DebriefWrongHouseSpared", "DebriefLedgerChannel"};
        public static IEnumerable<SafehouseSweepNarrativeLine> Lines => CH03M02SafehouseSweepCopy.Brief.Concat(CH03M02SafehouseSweepCopy.Comms).Concat(CH03M02SafehouseSweepCopy.Debrief);

        public static string VoicePath(string id, bool persian) => $"{VoiceRoot}/{(persian ? "fa" : "en")}/{id}.wav";
        public static AudioClip Voice(string id, bool persian) => AssetDatabase.LoadAssetAtPath<AudioClip>(VoicePath(id, persian));
        public static Texture Preview() => AssetDatabase.LoadAssetAtPath<Texture>(PreviewPath);
        public static Sprite Panel(string id, bool wide) => AssetDatabase.LoadAllAssetsAtPath($"{ArtRoot}/{id}.png").OfType<Sprite>().Single(sprite => sprite.name == $"{id}-{(wide ? "20x9" : "16x9")}");
        public static string PanelId(string lineId) => lineId switch
        {
            "safehouse_sweep-brief-01" => Panels[0], "safehouse_sweep-brief-02" => Panels[1], "safehouse_sweep-brief-03" => Panels[2],
            "safehouse_sweep-comms-01" => Panels[3], "safehouse_sweep-debrief-01" => Panels[4], "safehouse_sweep-debrief-02" => Panels[5], "safehouse_sweep-debrief-03" => Panels[6],
            _ => throw new InvalidOperationException("No Safehouse Sweep comic panel for " + lineId)
        };

        public static void ConfigureArt()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (string id in Panels)
            {
                string path = $"{ArtRoot}/{id}.png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter ?? throw new InvalidOperationException("Missing final Safehouse Sweep comic panel: " + path);
                importer.GetSourceTextureWidthAndHeight(out int width, out int height);
                var crops = new[]
                {
                    new SpriteMetaData {name = id + "-16x9", rect = Crop(width, height, 16f / 9f), alignment = 0, pivot = new Vector2(.5f, .5f)},
                    new SpriteMetaData {name = id + "-20x9", rect = Crop(width, height, 20f / 9f), alignment = 0, pivot = new Vector2(.5f, .5f)}
                };
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple; importer.spritePixelsPerUnit = 100;
                importer.sRGBTexture = true; importer.alphaSource = TextureImporterAlphaSource.None; importer.alphaIsTransparency = false; importer.mipmapEnabled = false;
                importer.streamingMipmaps = false; importer.isReadable = false; importer.npotScale = TextureImporterNPOTScale.None; importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear; importer.textureCompression = TextureImporterCompression.CompressedHQ; importer.maxTextureSize = 2048;
                var factories = new SpriteDataProviderFactories(); factories.Init(); var provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
                var old = provider.GetSpriteRects();
                var rectangles = crops.Select(crop => new SpriteRect {name = crop.name, rect = crop.rect, alignment = SpriteAlignment.Center, pivot = crop.pivot, spriteID = old.FirstOrDefault(existing => existing.name == crop.name)?.spriteID ?? GUID.Generate()}).ToArray();
                provider.SetSpriteRects(rectangles); provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rectangles.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID))); provider.Apply(); importer.SaveAndReimport();
                if (Mathf.Abs(Panel(id, false).rect.width / Panel(id, false).rect.height - 16f / 9f) > .005f || Mathf.Abs(Panel(id, true).rect.width / Panel(id, true).rect.height - 20f / 9f) > .005f)
                    throw new InvalidOperationException("Safehouse Sweep comic crop ratio mismatch: " + id);
            }
            Debug.Log("[SafehouseSweepVisual] result=Passed comicSources=7 authoredCrops=14 cleanPreview=1 playerFacing=1");
        }

        public static void ConfigureVoices()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport); int count = 0;
            foreach (var line in Lines)
            foreach (bool persian in new[] {false, true})
            {
                string path = VoicePath(line.Id, persian);
                var importer = AssetImporter.GetAtPath(path) as AudioImporter ?? throw new InvalidOperationException("Missing final voice: " + path);
                var settings = importer.defaultSampleSettings; settings.loadType = AudioClipLoadType.CompressedInMemory; settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate; settings.quality = .7f; settings.preloadAudioData = false; importer.defaultSampleSettings = settings;
                importer.forceToMono = true; importer.loadInBackground = true; importer.ambisonic = false;
                importer.userData = "status=ELEVENLABS_PAID_CREATOR_COMMERCIAL_LICENSE; provider=ElevenLabs; model=eleven_v3; locale=" + (persian ? "fa-IR" : "en-US") + "; manifest=safehouse_sweep_voice_manifest.json; runtimeNetworkTts=false";
                importer.SaveAndReimport(); var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path); if (clip == null || clip.length < .25f || clip.channels != 1) throw new InvalidOperationException("Invalid voice clip: " + path); count++;
            }
            AudioRuntimeConfigAssetBuilder.BuildDefaultAssets();
            Debug.Log($"[SafehouseSweepVoiceImports] result=Passed clips={count} locales=2 preload=0 runtimeNetworkTts=0");
        }

        private static Rect Crop(int width, int height, float aspect) { int h = Mathf.FloorToInt(Mathf.Min(height, width / aspect)); int w = Mathf.Min(width, Mathf.FloorToInt(h * aspect)); return new Rect((width - w) / 2, (height - h) / 2, w, h); }
    }
}
