using Game.UI.Runtime;

namespace Game.Composition
{
    internal sealed partial class MenuBootstrapCompositionSystemHelper
    {
        private void MarkMatchHudReady()
        {
            firstLaunchNarrative.MarkMatchHudReady();
            performanceDiagnosticsSystem.MarkMatchReady();
        }

        private static void ApplyStartupRuntimeSettings()
        {
            SettingsService.ApplyRuntime(
                AndroidPerformanceRuntimeSettings.Resolve(SettingsService.Load()));
        }
    }
}
