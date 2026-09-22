#if UNITY_EDITOR
using Game.Configs;
using UnityEditor;

namespace Game.Editor
{
    /// <summary>
    /// S002 menus and executeMethod entry points for the shared expanded ARIA watch.
    /// Stuck-simulation abort uses <see cref="SkirmishExpandedAriaFailFast"/>.
    /// Appends a runs.csv row only after the match finishes or the harness aborts. Never stamps Victory.
    /// </summary>
    [InitializeOnLoad]
    public static class SkirmishS002AriaRunHarness
    {
        private static readonly SkirmishExpandedAriaWatchDriver Driver = new SkirmishExpandedAriaWatchDriver(CreateProfile());

        static SkirmishS002AriaRunHarness()
        {
            Driver.ArmIfSessionPending();
        }

        [MenuItem("Tools/Warline/Skirmish/Launch S002 ARIA Watch 104731 en")]
        public static void Launch104731En() => Launch(104731, GameLocalization.EnglishLocaleCode);

        [MenuItem("Tools/Warline/Skirmish/Launch S002 ARIA Watch 104731 fa-IR")]
        public static void Launch104731Fa() => Launch(104731, GameLocalization.PersianLocaleCode);

        [MenuItem("Tools/Warline/Skirmish/Launch S002 ARIA Watch 130365 en")]
        public static void Launch130365En() => Launch(130365, GameLocalization.EnglishLocaleCode);

        [MenuItem("Tools/Warline/Skirmish/Launch S002 ARIA Watch 130365 fa-IR")]
        public static void Launch130365Fa() => Launch(130365, GameLocalization.PersianLocaleCode);

        [MenuItem("Tools/Warline/Skirmish/Launch S002 ARIA Watch 155923 en")]
        public static void Launch155923En() => Launch(155923, GameLocalization.EnglishLocaleCode);

        [MenuItem("Tools/Warline/Skirmish/Launch S002 ARIA Watch 155923 fa-IR")]
        public static void Launch155923Fa() => Launch(155923, GameLocalization.PersianLocaleCode);

        public static void LaunchFromEnvironment() => Driver.LaunchFromEnvironment();

        public static void Launch(int requestedSeed, string requestedLocale) => Driver.Launch(requestedSeed, requestedLocale);

        public static string EvidenceDirectory() => Driver.EvidenceDirectory();

        private static SkirmishExpandedAriaWatchProfile CreateProfile()
        {
            return new SkirmishExpandedAriaWatchProfile
            {
                CatalogId = SkirmishAcceptanceCensusCapture.CatalogId,
                ActiveKey = "Warline.S002.AriaRun",
                SeedKey = "Warline.S002.AriaSeed",
                LocaleKey = "Warline.S002.AriaLocale",
                StartedKey = "Warline.S002.AriaStarted",
                NormalKey = "Warline.S002.AriaNormal",
                PlayingSinceKey = "Warline.S002.AriaPlayingSince",
                EnvironmentSeedVariable = "WARLINE_S002_SEED",
                EnvironmentLocaleVariable = "WARLINE_S002_LOCALE",
                LogTag = "[SkirmishS002AriaRun]",
                DefaultSeed = SkirmishAcceptanceCensusCapture.FirstVisitSeed,
                EvidenceDirectoryRelative = SkirmishAcceptanceScaffold.RelativeReportEvidenceDirectory,
                RunsPathRelative = SkirmishAcceptanceScaffold.RelativeRunsPath,
                LiveTraceFormat = "s002-aria-live-{0}-{1}.jsonl",
                InvalidSeedError = SkirmishS002AriaRunLog.InvalidSeedError,
                IsFirstVisitSeed = SkirmishAcceptanceCensusCapture.IsRegularStandardAriaSeed,
                TryCreatePayload = SkirmishAriaAcceptancePayload.TryCreateFirstVisitS002
            };
        }
    }
}
#endif
