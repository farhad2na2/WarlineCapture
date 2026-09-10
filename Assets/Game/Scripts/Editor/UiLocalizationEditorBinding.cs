using Game.Composition;
using UnityEditor;

namespace Game.Editor
{
    internal static class UiLocalizationEditorBinding
    {
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            RenderingQueryComposition.Bind();
            UiDiagnosticsComposition.Bind();
            GameTextResolverAdapter.BindRuntimeLocalization();
            CampaignMissionMenuBootstrapRuntime.BindGuideContent();
        }
    }
}
