using System;
using System.Linq;
using Game.Catalog.Contracts;
using Game.Composition;
using Game.Configs;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Imported assets, exact captions, locale selection and complete voice windows.</summary>
    public static class CampaignFinaleComicVoiceValidation
    {
        public static void ValidatePreparedCopy()
        {
            ValidateExistingChapterVoices();
            CH05M05CommandNodeNarrativeBuilder.ValidateFinale();
            var locale = Locale();
            int count = 0;
            foreach (var mission in new[] {
                (CH05M03NetworkCollapseNarrativeBuilder.Path, CampaignFinaleComicVoiceInstaller.NetworkCollapseCopy()),
                (CH05M04LastCorridorNarrativeBuilder.Path, CampaignFinaleComicVoiceInstaller.LastCorridorCopy()),
                (CH05M05CommandNodeNarrativeBuilder.Path, CampaignFinaleComicVoiceInstaller.CommandNodeCopy()) })
            {
                var lines = AssetDatabase.LoadAllAssetsAtPath(mission.Item1).OfType<NarrativeSequenceConfig>()
                    .SelectMany(s => s.States.SelectMany(state => state.Lines)).ToArray();
                if (lines.Length != mission.Item2.Length) throw new InvalidOperationException("Wrong prepared line count");
                foreach (var copy in mission.Item2)
                {
                    var line = lines.Single(l => l.LineId == copy.Id);
                    if (line.EnglishFallback != copy.English || line.TextKey != copy.Key || line.Speaker != copy.Speaker
                        || locale.Text.Single(t => t.Key == copy.Key).Value != copy.Persian)
                        throw new InvalidOperationException("Prepared canonical caption mismatch: " + copy.Id);
                    count++;
                }
            }
            if (count != 55) throw new InvalidOperationException("Expected 55 missing voiced lines");
            Debug.Log("[CampaignFinaleVoicePreparation] result=Passed canonicalLines=55 expectedClips=126 locales=2 generatedVoices=Pending providerApproval=Pending");
        }

        [MenuItem("Game/Campaign/Validate Finale Comic Voices")]
        public static void Validate()
        {
            Check(CH05M03NetworkCollapseNarrativeBuilder.Path, CH05M03NetworkCollapseNarrativeBuilder.VoiceRoot,
                CampaignFinaleComicVoiceInstaller.NetworkCollapseCopy());
            Check(CH05M04LastCorridorNarrativeBuilder.Path, CH05M04LastCorridorNarrativeBuilder.VoiceRoot,
                CampaignFinaleComicVoiceInstaller.LastCorridorCopy());
            Check(CH05M05CommandNodeNarrativeBuilder.Path, CH05M05CommandNodeNarrativeBuilder.VoiceRoot,
                CampaignFinaleComicVoiceInstaller.CommandNodeCopy());
            CH05M05CommandNodeNarrativeBuilder.ValidateFinale();
            ValidateExistingChapterVoices();
            Debug.Log("[CampaignFinaleComicVoiceValidation] result=Passed lines=55 clips=126 locales=2 canonicalCaptions=Exact uniqueBindings=1 voiceWindows=Passed commanderVariants=3 audibleAcceptance=Pending normalInputJourney=Pending");
        }

        public static void ValidateExistingChapterVoices()
        {
            var locale = Locale();
            int count = 0;
            foreach (string chapter in new[] { "Chapter04", "Chapter05" })
            foreach (string path in System.IO.Directory.GetFiles("Assets/Game/Configs/Narrative/" + chapter, "*.asset"))
            {
                if (chapter == "Chapter05" && !path.Contains("CH05M01_") && !path.Contains("CH05M02_")) continue;
                foreach (var sequence in AssetDatabase.LoadAllAssetsAtPath(path).OfType<NarrativeSequenceConfig>())
                foreach (var state in sequence.States) foreach (var line in state.Lines)
                {
                    var localized = locale.Voices.Where(v => v.LineId == line.LineId).ToArray();
                    if (line.VoiceClip == null || line.VoiceClip.length < .25f || localized.Length != 1
                        || localized[0].VoiceClip == null || localized[0].VoiceClip == line.VoiceClip
                        || localized[0].VoiceClip.length < .25f)
                        throw new InvalidOperationException("Existing campaign voice missing: " + line.LineId);
                    count++;
                }
            }
            if (count != 64) throw new InvalidOperationException("Expected 64 existing voiced lines, got " + count);
            Debug.Log("[CampaignExistingComicVoiceAudit] result=Passed chapters=4,5 existingLines=64 existingClips=128 locales=2");
        }

        private static NarrativeLocaleConfig Locale() => AssetDatabase.LoadAssetAtPath<NarrativeLocaleConfig>(
            M02EstablishBaseNarrativeLocaleBuilder.PersianLocalePath) ?? throw new InvalidOperationException("Persian narrative locale missing");

        private static void Check(string path, string root, CampaignFinaleComicVoiceInstaller.Copy[] copies)
        {
            var locale = Locale();
            var selection = new FirstLaunchNarrativePortraitVoiceSelectionPresentationSystemHelper();
            var pairs = AssetDatabase.LoadAllAssetsAtPath(path).OfType<NarrativeSequenceConfig>()
                .SelectMany(s => s.States.SelectMany(state => state.Lines.Select(line => (state, line)))).ToArray();
            if (pairs.Length != copies.Length) throw new InvalidOperationException("Wrong binding count: " + path);
            foreach (var copy in copies)
            {
                var pair = pairs.Single(p => p.line.LineId == copy.Id);
                var line = pair.line;
                var localized = locale.Voices.Single(v => v.LineId == copy.Id);
                if (line.TextKey != copy.Key || line.EnglishFallback != copy.English || line.Speaker != copy.Speaker
                    || locale.Text.Single(t => t.Key == copy.Key).Value != copy.Persian)
                    throw new InvalidOperationException("Canonical caption mismatch: " + copy.Id);
                foreach (bool fa in new[] { false, true })
                foreach (string variant in copy.Speaker == NarrativeSpeakerId.Commander ? new[] { "", "_female", "_neutral" } : new[] { "" })
                {
                    string clipPath = CampaignFinaleComicVoiceInstaller.VoicePath(root, copy.Id, fa, variant);
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                    AudioClip bound = fa
                        ? variant == "_female" ? localized.FemaleVoiceClip : variant == "_neutral" ? localized.NeutralVoiceClip : localized.VoiceClip
                        : variant == "_female" ? line.FemaleVoiceClip : variant == "_neutral" ? line.NeutralVoiceClip : line.VoiceClip;
                    var importer = AssetImporter.GetAtPath(clipPath) as AudioImporter;
                    if (clip == null || clip != bound || clip.length < .25f || clip.channels != 1 || importer == null
                        || importer.defaultSampleSettings.preloadAudioData || !importer.loadInBackground
                        || importer.defaultSampleSettings.loadType != AudioClipLoadType.CompressedInMemory
                        || importer.defaultSampleSettings.compressionFormat != AudioCompressionFormat.Vorbis)
                        throw new InvalidOperationException("Voice binding/import profile mismatch: " + clipPath);
                    if (line.DeadlineSeconds - line.StartSeconds < clip.length + .49f
                        || pair.state.DurationSeconds < line.DeadlineSeconds)
                        throw new InvalidOperationException("Comic window cuts off voice: " + clipPath);
                    selection.Capture(variant == "_female" ? 0 : variant == "_neutral" ? 6 : 1);
                    selection.SetLocale(fa ? locale : null);
                    if (selection.ResolveVoiceClip(line) != clip)
                        throw new InvalidOperationException("Runtime locale/commander voice selection mismatch: " + clipPath);
                }
            }
        }
    }
}
