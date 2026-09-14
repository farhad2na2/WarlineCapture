using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static partial class M05BreachAssaultEditorProbe
    {
        private const string VoiceAudit = "Warline.M05.VoiceAudit";
        private static readonly HashSet<string> PlayedVoices = new();
        private static double nextVoiceSample;
        public static void RunVoicedEnglish()
        {
            ValidateVoiceCatalog();
            SessionState.SetBool(VoiceAudit,true);PlayedVoices.Clear();nextVoiceSample=0;
            RunGuidedEnglish();
        }
        public static void ValidateVoiceCatalog()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<AudioEventCatalogConfig>("Assets/Game/Audio/Events/AudioEventCatalogConfig.asset");
            foreach(bool fa in new[]{false,true})
            {
                foreach(var line in M05BreachAssaultMediaImporter.Lines)
                    RequireVoice(M05BreachAssaultMediaImporter.Voice(line.Id,fa));
                for(int step=1;step<=8;step++)
                {
                    string id=$"vo.aria.tutorial.m05.{step:00}."+(fa?"fa":"en");
                    var entry=catalog.Events.Single(e=>e.EventId==id);
                    var clip=M05BreachAssaultMediaImporter.Voice($"tutorial-m05-{step:00}",fa);
                    RequireVoice(clip);
                    if(entry.BusId!="Voice" || entry.Clips.Count!=1 || entry.Clips[0].Clip!=clip || entry.Clips[0].Weight<=0)
                        throw new InvalidOperationException("Invalid M5 tutorial audio route: "+id);
                }
            }
            Debug.Log("[M05VoiceCatalog] result=Passed clips=30 tutorialRoutes=16 locales=2");
        }
        private static void RequireVoice(AudioClip clip)
        {
            if(clip==null || clip.length<.25f || clip.channels!=1 || clip.preloadAudioData)
                throw new InvalidOperationException("Missing or invalid M5 voice: "+clip);
        }
        private static void SampleVoices()
        {
            if(!SessionState.GetBool(VoiceAudit,false) || EditorApplication.timeSinceStartup<nextVoiceSample)return;
            nextVoiceSample=EditorApplication.timeSinceStartup+.1;
            foreach(var source in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude))
            {
                if(!source.isPlaying || source.clip==null || source.timeSamples<=0 || source.volume<=0 || source.mute)continue;
                string path=AssetDatabase.GetAssetPath(source.clip);
                if(!path.StartsWith(M05BreachAssaultMediaImporter.VoiceRoot+"/",StringComparison.Ordinal))continue;
                string language=GameLocalization.CurrentLocaleCode.StartsWith("fa",StringComparison.Ordinal)?"fa":"en";
                if(!path.Contains("/"+language+"/"))throw new InvalidOperationException("Wrong-language M5 playback: "+path);
                if(PlayedVoices.Add(path))Debug.Log("[M05VoicePlayback] "+path+" advancingSamples="+source.timeSamples);
            }
        }
        private static void ValidatePlayedVoices()
        {
            if(!SessionState.GetBool(VoiceAudit,false))return;
            foreach(bool fa in new[]{false,true})
            {
                foreach(var line in M05BreachAssaultMediaImporter.Lines)
                    if(!PlayedVoices.Contains(M05BreachAssaultMediaImporter.VoicePath(line.Id,fa)))
                        throw new InvalidOperationException("Story voice never played: "+line.Id+" fa="+fa);
                string prefix=M05BreachAssaultMediaImporter.VoiceRoot+"/"+(fa?"fa":"en")+"/tutorial-";
                if(!PlayedVoices.Any(path=>path.StartsWith(prefix,StringComparison.Ordinal)))
                    throw new InvalidOperationException("Tutorial narration never played: "+prefix);
            }
            Debug.Log("[M05VoicePlayback] result=Passed storyClips=14 tutorialBothLocales=1 uniqueClips="+PlayedVoices.Count);
            SessionState.SetBool(VoiceAudit,false);
        }
    }
}
