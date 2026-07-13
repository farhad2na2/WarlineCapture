using System;
using System.Collections.Generic;
using Game.Runtime;
using Game.UI.Runtime;

namespace Game.Composition
{
    internal static class AndroidPerformanceRuntimeSettings
    {
        internal static UISettingsModel Resolve(UISettingsModel settings)
        {
            return Resolve(settings, Environment.GetCommandLineArgs());
        }

        internal static UISettingsModel Resolve(
            UISettingsModel settings,
            IReadOnlyList<string> commandLineArguments)
        {
            if (!AndroidPerformanceRecorder.TryGetRequestedReleaseFrameRate(commandLineArguments, out _))
                return settings;

            settings.Graphics.Quality = UIGraphicsQuality.High;
            settings.Graphics.FrameRateMode = UIFrameRateMode.Sixty;
            return settings;
        }
    }
}
