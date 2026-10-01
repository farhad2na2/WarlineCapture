using System;
using System.Linq;
using Game.Composition;
using Game.Configs;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Focused imported-asset and current caption checks; does not establish audible acceptance.</summary>
    public static class MissingMissionComicVoiceValidation
    {
        [MenuItem("Game/Campaign/Validate Refreshed Comic Voices")]
        public static void Validate()
        {
            var locale = AssetDatabase.LoadAssetAtPath<NarrativeLocaleConfig>(M02EstablishBaseNarrativeLocaleBuilder.PersianLocalePath)
                ?? throw new InvalidOperationException("Persian narrative locale missing");
            var localized = new SerializedObject(locale);
            var selection = new FirstLaunchNarrativePortraitVoiceSelectionPresentationSystemHelper();
            selection.SetLocale(locale);
            int count = 0;
            foreach (var copy in M04AirliftCopyCatalog.Brief.Where(c => c.Id is "m04-brief-01" or "m04-brief-02"))
            {
                Check(M04AirliftNarrativeBuilder.Path, M04AirliftMediaImporter.VoiceRoot, copy.Id, copy.Key, copy.English, copy.Persian, localized, selection);
                count++;
            }
            foreach (var copy in CH02M02SupplyLineCopy.Brief.Concat(CH02M02SupplyLineCopy.Comms).Concat(CH02M02SupplyLineCopy.Debrief))
            {
                Check(CH02M02SupplyLineNarrativeBuilder.Path, CH02M02SupplyLineNarrativeBuilder.VoiceRoot, copy.Id, copy.Key, copy.English, copy.Persian, localized, selection);
                count++;
            }
            foreach (var copy in CH04M03SplitFrontCopy.Brief.Concat(CH04M03SplitFrontCopy.Comms).Concat(CH04M03SplitFrontCopy.Debrief))
            {
                Check(CH04M03SplitFrontNarrativeBuilder.Path, CH04M03SplitFrontNarrativeBuilder.VoiceRoot, copy.Id, copy.Key, copy.English, copy.Persian, localized, selection);
                count++;
            }
            if (count != 17) throw new InvalidOperationException("Expected seventeen refreshed bilingual lines");
            Debug.Log("[MissingMissionComicVoiceBindings] result=Passed lines=17 clips=34 locales=2 canonicalCaptions=Exact uniqueBindings=1 voiceWindows=Passed audibleAcceptance=Pending");
        }

        private static void Check(string narrativePath,string voiceRoot,string id,string key,string english,string persian,
            SerializedObject locale,FirstLaunchNarrativePortraitVoiceSelectionPresentationSystemHelper selection)
        {
            var found = AssetDatabase.LoadAllAssetsAtPath(narrativePath).OfType<NarrativeSequenceConfig>()
                .SelectMany(sequence => sequence.States.SelectMany(state => state.Lines.Select(line => (state,line))))
                .Where(pair => pair.line.LineId == id).ToArray();
            if (found.Length != 1) throw new InvalidOperationException("Expected one EN narrative binding: " + id);
            var pair = found[0];
            var lineData = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(narrativePath).OfType<NarrativeSequenceConfig>()
                .Single(sequence => sequence.States.Any(state => state.Lines.Any(line => line.LineId == id))));
            int captions = 0;
            var states = lineData.FindProperty("states");
            for (int s = 0; s < states.arraySize; s++)
            {
                var lines = states.GetArrayElementAtIndex(s).FindPropertyRelative("lines");
                for (int i = 0; i < lines.arraySize; i++)
                {
                    var line = lines.GetArrayElementAtIndex(i);
                    if (line.FindPropertyRelative("lineId").stringValue != id) continue;
                    if (line.FindPropertyRelative("englishFallback").stringValue != english || line.FindPropertyRelative("textKey").stringValue != key)
                        throw new InvalidOperationException("EN canonical caption mismatch: " + id);
                    captions++;
                }
            }
            if (captions != 1) throw new InvalidOperationException("EN caption count mismatch: " + id);
            int faVoiceCount = 0, faTextCount = 0;
            var voices = locale.FindProperty("voices");
            for (int i = 0; i < voices.arraySize; i++)
            {
                var voice = voices.GetArrayElementAtIndex(i);
                if (voice.FindPropertyRelative("lineId").stringValue != id) continue;
                if (AssetDatabase.GetAssetPath(voice.FindPropertyRelative("voiceClip").objectReferenceValue) != voiceRoot + "/fa/" + id + ".wav")
                    throw new InvalidOperationException("FA voice path mismatch: " + id);
                faVoiceCount++;
            }
            var text = locale.FindProperty("text");
            for (int i = 0; i < text.arraySize; i++)
            {
                var entry = text.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("key").stringValue != key) continue;
                if (entry.FindPropertyRelative("value").stringValue != persian) throw new InvalidOperationException("FA canonical caption mismatch: " + id);
                faTextCount++;
            }
            if (faVoiceCount != 1 || faTextCount != 1) throw new InvalidOperationException("Duplicate/missing FA binding or caption: " + id);
            var en = AssetDatabase.LoadAssetAtPath<AudioClip>(voiceRoot + "/en/" + id + ".wav");
            var fa = AssetDatabase.LoadAssetAtPath<AudioClip>(voiceRoot + "/fa/" + id + ".wav");
            if (en == null || fa == null || en == fa || en.channels != 1 || fa.channels != 1 || en.length < .25f || fa.length < .25f
                || pair.line.VoiceClip != en || selection.ResolveVoiceClip(pair.line) != fa)
                throw new InvalidOperationException("Missing/mismatched imported voice: " + id);
            float longest = Mathf.Max(en.length,fa.length);
            if (pair.line.DeadlineSeconds < longest || pair.state.DurationSeconds < longest || pair.line.DeadlineSeconds > pair.state.DurationSeconds)
                throw new InvalidOperationException("Comic caption/state window cuts off voice: " + id);
        }
    }
}
