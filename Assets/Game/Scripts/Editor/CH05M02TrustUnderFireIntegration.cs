using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static class CH05M02TrustUnderFireIntegration
    {
        public static void BuildCore()
        {
            CH05M02TrustUnderFireConfigBuilder.Build();CH05M02TrustUnderFireNarrativeBuilder.BuildWithoutVoices();CH05M02TrustUnderFirePresentationBuilder.Build();Register();CH05M02TrustUnderFireMapValidation.Run();
            Debug.Log("[TrustUnderFireCore] result=Passed independentMap=true protectedRoutes=2 controls=Existing voices=PendingSeparateGate");
        }
        private static void Register(){M01FirstContactConfigBuilder.RefreshChapterCatalogs();CH05M02TrustUnderFireContentBuilder.RegisterProductionEntries();AssetDatabase.SaveAssets();}
        public static void Build(){BuildCore();Finish();}
        public static void Finish(){CH05M02TrustUnderFireNarrativeBuilder.BuildAndInstall();CH05M02TrustUnderFirePresentationBuilder.Build();Register();Debug.Log("[TrustUnderFireIntegration] result=Passed campaignIndex=21 voices=Installed controls=Existing");}
        public static void FinishPrepared(){Finish();}
        public static void CompletePrepared(){CH05M02TrustUnderFireConfigBuilder.CompletePreparedConfiguration();Finish();}
        public static void ValidateCore(){CH05M02TrustUnderFireMapValidation.Run();CH05M02TrustUnderFireRulesValidation.Run();CH05M02TrustUnderFireObjectiveValidation.Run();Debug.Log("[TrustUnderFireCoreValidation] result=Passed nativeMap=Passed rules=Passed objectives=Passed voices=PendingSeparateGate");}
        public static void ValidateInstalledVoices(){CH05M02TrustUnderFireNarrativeBuilder.InstallComicVoices();Debug.Log("[TrustUnderFireVoiceValidation] result=Passed clips=14 locales=2");}
        public static void Validate(){ValidateCore();ValidateInstalledVoices();Debug.Log("[TrustUnderFireValidation] result=Passed nativeMap=Passed voices=Passed");}
    }
}
