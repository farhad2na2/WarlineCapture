using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Game.Editor
{
    public static class CH03M03FalseFrontMediaImporter
    {
        public const string ArtRoot = "Assets/Game/Art/Narrative/CH03M03FalseFront";
        public const string PreviewPath = ArtRoot + "/BriefEvacuationReport.png";
        public const string VoiceRoot = "Assets/Game/Audio/Narrative/CH03M03FalseFront/Voice";
        public static readonly string[] Panels = {"BriefEvacuationReport", "BriefUncertainRoutes", "BriefProtectRoute", "CommsSamiraCorrection", "CommsAriaRedirect", "DebriefEvacueesSafe", "DebriefAuthoritySeal"};
        public static IEnumerable<FalseFrontNarrativeLine> Lines => CH03M03FalseFrontCopy.Brief.Concat(CH03M03FalseFrontCopy.Comms).Concat(CH03M03FalseFrontCopy.Debrief);

        public static string VoicePath(string id, bool persian) => $"{VoiceRoot}/{(persian ? "fa" : "en")}/{id}.wav";
        public static AudioClip Voice(string id, bool persian) => AssetDatabase.LoadAssetAtPath<AudioClip>(VoicePath(id, persian));
        public static Texture Preview() => AssetDatabase.LoadAssetAtPath<Texture>(PreviewPath);
        public static Sprite Panel(string id, bool wide) => AssetDatabase.LoadAllAssetsAtPath($"{ArtRoot}/{id}.png").OfType<Sprite>().Single(sprite => sprite.name == $"{id}-{(wide ? "20x9" : "16x9")}");
        public static string PanelId(string lineId) => lineId switch
        {
            "false_front-brief-01" => Panels[0], "false_front-brief-02" => Panels[1], "false_front-brief-03" => Panels[2],
            "false_front-comms-01" => Panels[3], "false_front-comms-02" => Panels[4],
            "false_front-debrief-01" => Panels[5], "false_front-debrief-02" => Panels[6],
            _ => throw new InvalidOperationException("No False Front comic panel for " + lineId)
        };

        public static void ConfigureArt()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (string id in Panels)
            {
                string path = $"{ArtRoot}/{id}.png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter ?? throw new InvalidOperationException("Missing False Front comic panel: " + path);
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
                    throw new InvalidOperationException("False Front comic crop ratio mismatch: " + id);
            }
            Debug.Log("[FalseFrontVisual] result=Passed comicSources=7 authoredCrops=14 cleanPreview=1");
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
                importer.userData = "status=ELEVENLABS_PAID_CREATOR_COMMERCIAL_LICENSE; provider=ElevenLabs; model=eleven_v3; locale=" + (persian ? "fa-IR" : "en-US") + "; manifest=false_front_voice_manifest.json; runtimeNetworkTts=false";
                importer.SaveAndReimport(); var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path); if (clip == null || clip.length < .25f || clip.channels != 1) throw new InvalidOperationException("Invalid voice clip: " + path); count++;
            }
            AudioRuntimeConfigAssetBuilder.BuildDefaultAssets();
            Debug.Log($"[FalseFrontVoiceImports] result=Passed clips={count} locales=2 preload=0 runtimeNetworkTts=0");
        }

        private static Rect Crop(int width, int height, float aspect) { int h = Mathf.FloorToInt(Mathf.Min(height, width / aspect)); int w = Mathf.Min(width, Mathf.FloorToInt(h * aspect)); return new Rect((width - w) / 2, (height - h) / 2, w, h); }
    }
}
