#if UNITY_EDITOR
using Game.Configs;
using UnityEditor;

namespace Game.Editor
{
    /// <summary>
    /// S005 menus and executeMethod entry points for the shared expanded ARIA watch.
    /// First visit is Regular Standard. English launchers are required; fa-IR uses the same driver.
    /// Appends a runs.csv row only after the match finishes or the harness aborts. Never stamps Victory.
    /// Combined Arms economy, spawn, and scenario assets stay on the S005 mission lane.
    /// </summary>
    [InitializeOnLoad]
    public static class SkirmishS005AriaRunHarness
    {
        public const string InvalidSeedError = "First-visit ARIA seed must be 104734, 130368 or 155926.";

        private static readonly SkirmishExpandedAriaWatchDriver Driver = new SkirmishExpandedAriaWatchDriver(CreateProfile());

        static SkirmishS005AriaRunHarness()
        {
            Driver.ArmIfSessionPending();
        }

        [MenuItem("Tools/Warline/Skirmish/Launch S005 ARIA Watch 104734 en")]
        public static void Launch104734En() => Launch(SkirmishS005FirstVisit.SeedA, GameLocalization.EnglishLocaleCode);

        [MenuItem("Tools/Warline/Skirmish/Launch S005 ARIA Watch 104734 fa-IR")]
        public static void Launch104734Fa() => Launch(SkirmishS005FirstVisit.SeedA, GameLocalization.PersianLocaleCode);

        [MenuItem("Tools/Warline/Skirmish/Launch S005 ARIA Watch 130368 en")]
        public static void Launch130368En() => Launch(SkirmishS005FirstVisit.SeedB, GameLocalization.EnglishLocaleCode);

        [MenuItem("Tools/Warline/Skirmish/Launch S005 ARIA Watch 130368 fa-IR")]
        public static void Launch130368Fa() => Launch(SkirmishS005FirstVisit.SeedB, GameLocalization.PersianLocaleCode);

        [MenuItem("Tools/Warline/Skirmish/Launch S005 ARIA Watch 155926 en")]
        public static void Launch155926En() => Launch(SkirmishS005FirstVisit.SeedC, GameLocalization.EnglishLocaleCode);

        [MenuItem("Tools/Warline/Skirmish/Launch S005 ARIA Watch 155926 fa-IR")]
        public static void Launch155926Fa() => Launch(SkirmishS005FirstVisit.SeedC, GameLocalization.PersianLocaleCode);

        public static void LaunchFromEnvironment() => Driver.LaunchFromEnvironment();

        public static void Launch(int requestedSeed, string requestedLocale) => Driver.Launch(requestedSeed, requestedLocale);

        public static string DescribeLaunch(int requestedSeed, string requestedLocale) =>
            Driver.DescribeLaunch(requestedSeed, requestedLocale);

        public static string EvidenceDirectory() => Driver.EvidenceDirectory();

        private static SkirmishExpandedAriaWatchProfile CreateProfile()
        {
            return new SkirmishExpandedAriaWatchProfile
            {
                CatalogId = SkirmishAcceptanceCensusCapture.S005CatalogId,
                ActiveKey = "Warline.S005.AriaRun",
                SeedKey = "Warline.S005.AriaSeed",
                LocaleKey = "Warline.S005.AriaLocale",
                StartedKey = "Warline.S005.AriaStarted",
                NormalKey = "Warline.S005.AriaNormal",
                PlayingSinceKey = "Warline.S005.AriaPlayingSince",
                EnvironmentSeedVariable = "WARLINE_S005_SEED",
                EnvironmentLocaleVariable = "WARLINE_S005_LOCALE",
                LogTag = "[SkirmishS005AriaRun]",
                DefaultSeed = SkirmishS005FirstVisit.SeedA,
                EvidenceDirectoryRelative = SkirmishAcceptanceScaffold.S005RelativeReportEvidenceDirectory,
                RunsPathRelative = SkirmishAcceptanceScaffold.S005RelativeRunsPath,
                LiveTraceFormat = "s005-aria-live-{0}-{1}.jsonl",
                InvalidSeedError = SkirmishS005AriaRunHarness.InvalidSeedError,
                IsFirstVisitSeed = SkirmishAcceptanceCensusCapture.IsS005RegularStandardSeed,
                TryCreatePayload = SkirmishAriaAcceptancePayload.TryCreateFirstVisitS005
            };
        }
    }
}
#endif
