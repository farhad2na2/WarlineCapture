using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static class CH04M05ArmorBreakIntegration
    {
        public static void Build()
        {
            CH04M05ArmorBreakConfigBuilder.Build();
            Finish();
        }
        public static void Finish()
        {
            CH04M05ArmorBreakNarrativeBuilder.BuildAndInstall();
            CH04M05ArmorBreakPresentationBuilder.Build();
            M01FirstContactConfigBuilder.RefreshChapterCatalogs();
            CH04M05ArmorBreakContentBuilder.RegisterProductionEntries();
            AssetDatabase.SaveAssets();
            Debug.Log("[ArmorBreakIntegration] result=Passed map=independent-bounded-frontier controls=existing campaignIndex=19");
        }
        public static void CompletePrepared()
        {
            CH04M05ArmorBreakConfigBuilder.CompletePreparedConfiguration();
            Finish();
        }
    }
}
