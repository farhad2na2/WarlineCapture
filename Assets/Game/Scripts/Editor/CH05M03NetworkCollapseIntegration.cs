using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static class CH05M03NetworkCollapseIntegration
    {
        public static void BuildCore()
        {
            CH05M03NetworkCollapseConfigBuilder.Build();
            CH05M03NetworkCollapseNarrativeBuilder.BuildWithoutVoices();
            CH05M03NetworkCollapsePresentationBuilder.Build();
            Register();
            CH05M03NetworkCollapseMapValidation.Run();
            Debug.Log("[NetworkCollapseCore] result=Passed independentMap=true nodes=3 controls=Existing voices=NotRequested");
        }
        public static void FinishPrepared()
        {
            CH05M03NetworkCollapseConfigBuilder.SetSupplyReadiness();
            CH05M03NetworkCollapseNarrativeBuilder.BuildWithoutVoices();
            CH05M03NetworkCollapsePresentationBuilder.Build();
            Register();
            ValidateCore();
        }
        private static void Register()
        {
            M01FirstContactConfigBuilder.RefreshChapterCatalogs();
            CH05M03NetworkCollapseContentBuilder.RegisterProductionEntries();
            AssetDatabase.SaveAssets();
        }
        public static void ValidateCore()
        {
            CH05M03NetworkCollapseMapValidation.Run();
            CH05M03NetworkCollapseRulesValidation.Run();
            CH05M03NetworkCollapseObjectiveValidation.Run();
            Debug.Log("[NetworkCollapseCoreValidation] result=Passed nativeMap=Passed rules=Passed objectives=Passed voices=NotRequested");
        }
    }
}
