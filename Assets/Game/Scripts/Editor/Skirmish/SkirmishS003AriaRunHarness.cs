#if UNITY_EDITOR
using Game.Configs;
using UnityEditor;

namespace Game.Editor
{
    /// <summary>
    /// S003 menus and executeMethod entry points for the shared expanded ARIA watch.
    /// First visit is Regular Standard. English launchers are required; fa-IR uses the same driver.
    /// Stuck-simulation abort uses <see cref="SkirmishExpandedAriaFailFast"/>.
    /// Appends a runs.csv row only after the match finishes or the harness aborts. Never stamps Victory.
    /// </summary>
    [InitializeOnLoad]
    public static class SkirmishS003AriaRunHarness
    {
        public const string InvalidSeedError = "First-visit ARIA seed must be 104732, 130366 or 155924.";

        private static readonly SkirmishExpandedAriaWatchDriver Driver = new SkirmishExpandedAriaWatchDriver(CreateProfile());

        static SkirmishS003AriaRunHarness()
        {
            Driver.ArmIfSessionPending();
        }

        [MenuItem("Tools/Warline/Skirmish/Launch S003 ARIA Watch 104732 en")]
        public static void Launch104732En() => Launch(SkirmishS003FirstVisit.SeedA, GameLocalization.EnglishLocaleCode);

        [MenuItem("Tools/Warline/Skirmish/Launch S003 ARIA Watch 104732 fa-IR")]
        public static void Launch104732Fa() => Launch(SkirmishS003FirstVisit.SeedA, GameLocalization.PersianLocaleCode);

        [MenuItem("Tools/Warline/Skirmish/Launch S003 ARIA Watch 130366 en")]
        public static void Launch130366En() => Launch(SkirmishS003FirstVisit.SeedB, GameLocalization.EnglishLocaleCode);

        [MenuItem("Tools/Warline/Skirmish/Launch S003 ARIA Watch 130366 fa-IR")]
        public static void Launch130366Fa() => Launch(SkirmishS003FirstVisit.SeedB, GameLocalization.PersianLocaleCode);

        [MenuItem("Tools/Warline/Skirmish/Launch S003 ARIA Watch 155924 en")]
        public static void Launch155924En() => Launch(SkirmishS003FirstVisit.SeedC, GameLocalization.EnglishLocaleCode);

        [MenuItem("Tools/Warline/Skirmish/Launch S003 ARIA Watch 155924 fa-IR")]
        public static void Launch155924Fa() => Launch(SkirmishS003FirstVisit.SeedC, GameLocalization.PersianLocaleCode);

        public static void LaunchFromEnvironment() => Driver.LaunchFromEnvironment();

        public static void Launch(int requestedSeed, string requestedLocale) => Driver.Launch(requestedSeed, requestedLocale);

        public static string DescribeLaunch(int requestedSeed, string requestedLocale) =>
            Driver.DescribeLaunch(requestedSeed, requestedLocale);

        public static string EvidenceDirectory() => Driver.EvidenceDirectory();

        private static SkirmishExpandedAriaWatchProfile CreateProfile()
        {
            return new SkirmishExpandedAriaWatchProfile
            {
                CatalogId = SkirmishAcceptanceCensusCapture.S003CatalogId,
                ActiveKey = "Warline.S003.AriaRun",
                SeedKey = "Warline.S003.AriaSeed",
                LocaleKey = "Warline.S003.AriaLocale",
                StartedKey = "Warline.S003.AriaStarted",
                NormalKey = "Warline.S003.AriaNormal",
                PlayingSinceKey = "Warline.S003.AriaPlayingSince",
                EnvironmentSeedVariable = "WARLINE_S003_SEED",
                EnvironmentLocaleVariable = "WARLINE_S003_LOCALE",
                LogTag = "[SkirmishS003AriaRun]",
                DefaultSeed = SkirmishS003FirstVisit.SeedA,
                EvidenceDirectoryRelative = SkirmishAcceptanceScaffold.S003RelativeReportEvidenceDirectory,
                RunsPathRelative = SkirmishAcceptanceScaffold.S003RelativeRunsPath,
                LiveTraceFormat = "s003-aria-live-{0}-{1}.jsonl",
                InvalidSeedError = SkirmishS003AriaRunHarness.InvalidSeedError,
                IsFirstVisitSeed = SkirmishAcceptanceCensusCapture.IsS003RegularStandardSeed,
                TryCreatePayload = SkirmishAriaAcceptancePayload.TryCreateFirstVisitS003
            };
        }
    }
}
#endif
