using System;
using System.Collections.Generic;
using System.Linq;
using Game.Catalog.Contracts;
using Game.Configs;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Prevents campaign dialogue from shipping over a blank or live-match backdrop.</summary>
    public sealed class CampaignMissionComicCoverageValidation : IPreprocessBuildWithReport
    {
        public int callbackOrder=>0;
        public void OnPreprocessBuild(BuildReport report)=>ValidateAll();

        [MenuItem("Game/Campaign/Validate Comic Coverage")]
        public static void ValidateAll()
        {
            var failures=new List<string>();int sequences=0,dialogues=0;
            foreach(string guid in AssetDatabase.FindAssets("t:NarrativeSequenceConfig",new[]{"Assets/Game/Configs/Narrative/Chapter02","Assets/Game/Configs/Narrative/Chapter03"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);
                foreach(var sequence in AssetDatabase.LoadAllAssetsAtPath(path).OfType<NarrativeSequenceConfig>().Where(s=>s.SequenceId.StartsWith("seq.ch",StringComparison.Ordinal)))
                {
                    sequences++;
                    foreach(var state in sequence.States.Where(s=>s.Kind==NarrativeStateKind.PanelDialogue))
                    {
                        dialogues++;
                        ValidatePanel(sequence,state,state.Panel16x9,state.Panel16x9Reference,"16x9",failures);
                        ValidatePanel(sequence,state,state.Panel20x9,state.Panel20x9Reference,"20x9",failures);
                    }
                }
            }
            if(failures.Count>0)throw new BuildFailedException("Campaign comic coverage failed:\n"+string.Join("\n",failures));
            Debug.Log($"[CampaignComicCoverage] result=Passed sequences={sequences} dialogueStates={dialogues} aspects=16x9,20x9 shipGate=enabled");
        }

        public static void ValidateMission(string assetPath,string sequencePrefix)
        {
            var failures=new List<string>();int dialogues=0;
            foreach(var sequence in AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<NarrativeSequenceConfig>().Where(s=>s.SequenceId.StartsWith(sequencePrefix,StringComparison.Ordinal)))
                foreach(var state in sequence.States.Where(s=>s.Kind==NarrativeStateKind.PanelDialogue)){dialogues++;ValidatePanel(sequence,state,state.Panel16x9,state.Panel16x9Reference,"16x9",failures);ValidatePanel(sequence,state,state.Panel20x9,state.Panel20x9Reference,"20x9",failures);}
            if(failures.Count>0)throw new InvalidOperationException(string.Join("\n",failures));
            if(dialogues==0)throw new InvalidOperationException("No dialogue states found for "+sequencePrefix);
            Debug.Log($"[CampaignComicCoverage] result=Passed mission={sequencePrefix} dialogueStates={dialogues} aspects=2");
        }

        private static void ValidatePanel(NarrativeSequenceConfig sequence,NarrativeStateRecord state,Sprite direct,UnityEngine.AddressableAssets.AssetReferenceSprite reference,string aspect,List<string> failures)
        {
            if(direct!=null)return;
            if(reference==null||!reference.RuntimeKeyIsValid()||string.IsNullOrEmpty(reference.SubObjectName)){failures.Add($"{sequence.SequenceId}/{state.StateId} missing {aspect} comic panel");return;}
            string path=AssetDatabase.GUIDToAssetPath(reference.AssetGUID);
            if(string.IsNullOrEmpty(path)||!AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Any(sprite=>sprite.name==reference.SubObjectName))failures.Add($"{sequence.SequenceId}/{state.StateId} has unresolved {aspect} comic '{reference.SubObjectName}'");
        }
    }
}
