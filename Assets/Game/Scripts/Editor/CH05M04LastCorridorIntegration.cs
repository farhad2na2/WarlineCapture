using System;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static class CH05M04LastCorridorIntegration
    {
        public static void Build()=>BuildCore();
        public static void BuildCore()
        {
            CH05M04LastCorridorConfigBuilder.Build();
            FinishPrepared();
            CH05M04LastCorridorMapValidation.Run();
            Debug.Log("[LastCorridorCore] result=Passed independentMap=Passed deliveryCategories=5 routes=2SameArtery voices=ExcludedByRequest captions=2Locales");
        }
        public static void FinishPrepared()
        {
            CH05M04LastCorridorNarrativeBuilder.BuildWithoutVoices();
            CH05M04LastCorridorPresentationBuilder.Build();
            M01FirstContactConfigBuilder.RefreshChapterCatalogs();
            CH05M04LastCorridorContentBuilder.RegisterProductionEntries();
            AssetDatabase.SaveAssets();
        }
        public static void CompletePrepared()
        {
            CH05M04LastCorridorConfigBuilder.CompletePreparedConfiguration();
            FinishPrepared();
        }
        public static void Validate()
        {
            CH05M04LastCorridorMapValidation.Run();
            CH05M04LastCorridorRulesValidation.Run();
            CH05M04LastCorridorObjectiveValidation.Run();
            Debug.Log("[LastCorridorValidation] result=Passed nativeMap=Passed rules=Passed objectives=Passed voices=ExcludedByRequest nativeJourney=SeparateGate");
        }
    }
}
