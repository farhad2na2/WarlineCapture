#if UNITY_EDITOR
using Game.Configs;
using UnityEditor;

namespace Game.Editor
{
    /// <summary>
    /// S004 menus and executeMethod entry points for the shared expanded ARIA watch.
    /// First visit is Regular Standard. English launchers are required; fa-IR uses the same driver.
    /// Appends a runs.csv row only after the match finishes or the harness aborts. Never stamps Victory.
    /// </summary>
    [InitializeOnLoad]
    public static class SkirmishS004AriaRunHarness
    {
        public const string InvalidSeedError = "First-visit ARIA seed must be 104733, 130367 or 155925.";

        private static readonly SkirmishExpandedAriaWatchDriver Driver = new SkirmishExpandedAriaWatchDriver(CreateProfile());

        static SkirmishS004AriaRunHarness()
        {
            Driver.ArmIfSessionPending();
        }

        [MenuItem("Tools/Warline/Skirmish/Launch S004 ARIA Watch 104733 en")]
        public static void Launch104733En() => Launch(SkirmishS004FirstVisit.SeedA, GameLocalization.EnglishLocaleCode);

        [MenuItem("Tools/Warline/Skirmish/Launch S004 ARIA Watch 104733 fa-IR")]
        public static void Launch104733Fa() => Launch(SkirmishS004FirstVisit.SeedA, GameLocalization.PersianLocaleCode);

        [MenuItem("Tools/Warline/Skirmish/Launch S004 ARIA Watch 130367 en")]
        public static void Launch130367En() => Launch(SkirmishS004FirstVisit.SeedB, GameLocalization.EnglishLocaleCode);

        [MenuItem("Tools/Warline/Skirmish/Launch S004 ARIA Watch 130367 fa-IR")]
        public static void Launch130367Fa() => Launch(SkirmishS004FirstVisit.SeedB, GameLocalization.PersianLocaleCode);

        [MenuItem("Tools/Warline/Skirmish/Launch S004 ARIA Watch 155925 en")]
        public static void Launch155925En() => Launch(SkirmishS004FirstVisit.SeedC, GameLocalization.EnglishLocaleCode);

        [MenuItem("Tools/Warline/Skirmish/Launch S004 ARIA Watch 155925 fa-IR")]
        public static void Launch155925Fa() => Launch(SkirmishS004FirstVisit.SeedC, GameLocalization.PersianLocaleCode);

        public static void LaunchFromEnvironment() => Driver.LaunchFromEnvironment();

        public static void Launch(int requestedSeed, string requestedLocale) => Driver.Launch(requestedSeed, requestedLocale);

        public static string DescribeLaunch(int requestedSeed, string requestedLocale) =>
            Driver.DescribeLaunch(requestedSeed, requestedLocale);

        public static string EvidenceDirectory() => Driver.EvidenceDirectory();

        private static SkirmishExpandedAriaWatchProfile CreateProfile()
        {
            return new SkirmishExpandedAriaWatchProfile
            {
                CatalogId = SkirmishAcceptanceCensusCapture.S004CatalogId,
                ActiveKey = "Warline.S004.AriaRun",
                SeedKey = "Warline.S004.AriaSeed",
                LocaleKey = "Warline.S004.AriaLocale",
                StartedKey = "Warline.S004.AriaStarted",
                NormalKey = "Warline.S004.AriaNormal",
                PlayingSinceKey = "Warline.S004.AriaPlayingSince",
                EnvironmentSeedVariable = "WARLINE_S004_SEED",
                EnvironmentLocaleVariable = "WARLINE_S004_LOCALE",
                LogTag = "[SkirmishS004AriaRun]",
                DefaultSeed = SkirmishS004FirstVisit.SeedA,
                EvidenceDirectoryRelative = SkirmishAcceptanceScaffold.S004RelativeReportEvidenceDirectory,
                RunsPathRelative = SkirmishAcceptanceScaffold.S004RelativeRunsPath,
                LiveTraceFormat = "s004-aria-live-{0}-{1}.jsonl",
                InvalidSeedError = SkirmishS004AriaRunHarness.InvalidSeedError,
                IsFirstVisitSeed = SkirmishAcceptanceCensusCapture.IsS004RegularStandardSeed,
                TryCreatePayload = SkirmishAriaAcceptancePayload.TryCreateFirstVisitS004
            };
        }
    }
}
#endif
