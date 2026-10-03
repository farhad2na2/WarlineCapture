using System;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static class CH05M05CommandNodeIntegration
    {
        public static void Build()=>BuildCore();
        public static void BuildCore()
        {
            CH05M05CommandNodeConfigBuilder.Build();
            FinishPrepared();
            CH05M05CommandNodeMapValidation.Run();
            Debug.Log("[CommandNodeCore] result=Passed independentMap=Passed finale=Canonical audit=Guaranteed voices=ExcludedByRequest captions=2Locales");
        }
        public static void FinishPrepared()
        {
            CH05M05CommandNodeNarrativeBuilder.BuildWithoutVoices();
            CH05M05CommandNodePresentationBuilder.Build();
            M01FirstContactConfigBuilder.RefreshChapterCatalogs();
            CH05M05CommandNodeContentBuilder.RegisterProductionEntries();
            AssetDatabase.SaveAssets();
        }
        public static void CompletePrepared()
        {
            CH05M05CommandNodeConfigBuilder.CompletePreparedConfiguration();
            FinishPrepared();
        }
        public static void Validate()
        {
            CH05M05CommandNodeMapValidation.Run();
            CH05M05CommandNodeRulesValidation.Run();
            CH05M05CommandNodeObjectiveValidation.Run();
            CH05M05CommandNodeNarrativeBuilder.ValidateCaptionedFinale();
            CH05M05CommandNodeRecoveryValidation.Run();
            Debug.Log("[CommandNodeValidation] result=Passed nativeMap=Passed rules=Passed objectives=Passed voices=ExcludedByRequest nativeJourney=SeparateGate");
        }
    }
}
