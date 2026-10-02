using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static class CH05M01CitywideAlertIntegration
    {
        public static void Build()
        {
            CH05M01CitywideAlertConfigBuilder.Build();
            Finish();
        }
        public static void Finish()
        {
            CH05M01CitywideAlertNarrativeBuilder.BuildAndInstall();
            CH05M01CitywideAlertPresentationBuilder.Build();
            M01FirstContactConfigBuilder.RefreshChapterCatalogs();
            CH05M01CitywideAlertContentBuilder.RegisterProductionEntries();
            AssetDatabase.SaveAssets();
            Debug.Log("[CitywideAlertIntegration] result=Passed map=independent-bounded-urban controls=existing campaignIndex=20");
        }
        public static void Validate()
        {
            CH05M01CitywideAlertMapValidation.Run();CH05M01CitywideAlertRulesValidation.Run();CH05M01CitywideAlertObjectiveValidation.Run();CH05M01CitywideAlertNarrativeBuilder.InstallComicVoices();
            Debug.Log("[CitywideAlertValidation] result=Passed nativeMap=Passed rules=Passed objectives=Passed voices=Passed");
        }
        public static void RepairMinimap(){CH05M01CitywideAlertConfigBuilder.RepairMinimap();Finish();}
        public static void FinishPrepared(){Finish();}
        public static void CompletePrepared()
        {
            CH05M01CitywideAlertConfigBuilder.CompletePreparedConfiguration();
            Finish();
        }
    }
}
