#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Multi-job ARIA queue: several catalog/seed/locale jobs back-to-back in ONE Editor
    /// session. Each job goes through the unchanged single-run launch path
    /// (RunFocusedAriaAndExit -> LaunchCatalog -> Tick -> Finish -> FinishEvidence), so
    /// outcomes come only from the real match state at normal speed. The source
    /// fingerprint is computed once at queue start and once at queue end; a mismatch
    /// fails the whole queue. Queue position lives in SessionState and survives the
    /// Play Mode domain reloads between jobs.
    /// </summary>
    public static partial class SkirmishS002AriaRunHarness
    {
        private const string AriaQueueActiveKey = "Warline.AriaQueue.Active";
        private const string AriaQueuePathKey = "Warline.AriaQueue.Path";
        private const string AriaQueueJobsKey = "Warline.AriaQueue.Jobs";
        private const string AriaQueueIndexKey = "Warline.AriaQueue.Index";
        private const string AriaQueueRecordedKey = "Warline.AriaQueue.Recorded";
        private const string AriaQueueWinsKey = "Warline.AriaQueue.Wins";
        private const string AriaQueueSourceHashKey = "Warline.AriaQueue.SourceHash";
        private const string AriaQueuePendingKey = "Warline.AriaQueue.DispatchPending";
        private const string AriaQueueInFlightKey = "Warline.AriaQueue.InFlight";
        private const string AriaQueueJobStartKey = "Warline.AriaQueue.JobStartTicks";
        private const string AriaQueueJobRecordedKey = "Warline.AriaQueue.JobRecorded";
        private const string AriaQueueJobCountedKey = "Warline.AriaQueue.JobCounted";
        private const string AriaQueueJobResultKey = "Warline.AriaQueue.JobResult";
        private const string AriaQueueLog = "[SkirmishAriaQueue]";
        private const int AriaQueueMaximumJobs = 1200;
        // Tick already aborts a playing job after WallClockBudgetSeconds. This only
        // catches a job that left Play Mode without any Finish/quit request.
        private const double AriaQueueJobWatchdogSeconds = WallClockBudgetSeconds + 600d;

        private static readonly string[] AriaQueueRefusedDiagnostics =
        {
            "WARLINE_SKIRMISH_STARTUP_ONLY", "WARLINE_SKIRMISH_INPUT_DIAGNOSTIC", "WARLINE_S004_MANUAL_PROBE",
            "WARLINE_S005_PAID_BUILDING_CLEARANCE_ONLY", "WARLINE_S005_NATIVE_RECOVERY_ONLY"
        };

        private sealed class GenericAriaCensusCache
        {
            public string CatalogId, ArtifactId;
            public int Seed;
            public SkirmishAcceptanceCensus Census;
        }
        private static GenericAriaCensusCache genericAriaCensus;

        // ---- Generic expansion-catalog selection (any non-coded catalog ID) ----

        /// <summary>Catalog IDs outside the coded S002-S005 branches.</summary>
        private static bool IsGenericAriaCatalog(string catalog) =>
            SkirmishMissionDispatch.Canonical(catalog) &&
            catalog != SkirmishAcceptanceCensusCapture.CatalogId &&
            catalog != SkirmishAcceptanceCensusCapture.S003CatalogId &&
            catalog != SkirmishAcceptanceCensusCapture.S004CatalogId &&
            catalog != "S005";

        private static bool TryResolveGenericAriaDispatch(string catalog,
            out SkirmishScenarioDefinitionConfig definition, out SkirmishBattleCatalogEntry entry, out string error)
        {
            definition = null;
            entry = default;
            var authored = SkirmishExpansionCatalogConfig.Load();
            if (authored == null || !authored.TryGetDefinition(catalog, out definition) || definition == null)
            {
                error = "Catalog " + catalog + " does not resolve to a definition in the expansion catalog.";
                return false;
            }
            // The harness queues through the published library index; counterparts
            // with legacy indices <= 7 (S001/S025/S073) launch a different path.
            if (!SkirmishMissionDispatch.TryGetGenericById(SkirmishBattleCatalogConfig.Load(), catalog, out entry) ||
                entry.PlayableScenarioIndex <= 7 || !SkirmishMissionDispatch.MatchesDefinition(entry, definition))
            {
                error = "Catalog " + catalog + " has no playable generic library dispatch matching definition " +
                    definition.DefinitionId + " v" + definition.ContentVersion.ToString(CultureInfo.InvariantCulture) + ".";
                return false;
            }
            error = null;
            return true;
        }

        private static bool TryCreateGenericAriaPayload(string catalog, int selectedSeed, string selectedLocale,
            out SkirmishAriaAcceptancePayload payload, out string error)
        {
            payload = default;
            if (!TryResolveGenericAriaDispatch(catalog, out var definition, out _, out error))
                return false;
            return SkirmishAriaAcceptancePayload.TryCreate(catalog, definition.DefinitionId, definition.ContentVersion,
                SkirmishSizeId.Standard, SkirmishDifficultyId.Regular, selectedSeed, selectedLocale, out payload, out error);
        }

        private static int GenericAriaScenarioIndex(string catalog)
        {
            if (!TryResolveGenericAriaDispatch(catalog, out _, out var entry, out string error))
                throw new InvalidOperationException(error);
            return entry.PlayableScenarioIndex;
        }

        // Census for the row comes from the actual launched native setup (captured
        // while the session is live at stage 0), never from another catalog's census.
        private static bool TryReadGenericAriaCensus(out SkirmishAcceptanceCensus census)
        {
            census = null;
            string artifact = SessionState.GetString(Key + ".artifactId", "");
            World world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
            {
                EntityManager em = world.EntityManager;
                using var sessions = em.CreateEntityQuery(typeof(SkirmishResolvedSetupRecord));
                if (sessions.CalculateEntityCount() == 1)
                {
                    var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(sessions.GetSingletonEntity()).Setup;
                    if (setup != null && setup.CatalogId == catalogId && setup.Seed == seed)
                        genericAriaCensus = new GenericAriaCensusCache
                        {
                            CatalogId = catalogId, Seed = seed, ArtifactId = artifact,
                            Census = SkirmishAcceptanceCensusCapture.FromSetup(setup)
                        };
                }
            }
            var cached = genericAriaCensus;
            if (cached == null || cached.CatalogId != catalogId || cached.Seed != seed || cached.ArtifactId != artifact)
                return false;
            census = cached.Census;
            census.CodeHash = SessionState.GetString(CandidateHashKey, "");
            return true;
        }

        // Same id shape and three-slot rule as SkirmishS002AriaRunLog.TryNextAriaRunId,
        // without the coded first-visit seed whitelist (generic catalogs have none).
        private static bool TryNextGenericAriaRunId(string csvText, string catalog, int selectedSeed,
            string selectedLocale, out string runId, out string error)
        {
            runId = null;
            if (selectedSeed <= 0)
            {
                error = "Seed must be a positive integer.";
                return false;
            }
            if (!string.Equals(selectedLocale, GameLocalization.EnglishLocaleCode, StringComparison.Ordinal) &&
                !string.Equals(selectedLocale, GameLocalization.PersianLocaleCode, StringComparison.Ordinal))
            {
                error = "Use catalog locale en or fa-IR.";
                return false;
            }
            string token = string.Equals(selectedLocale, GameLocalization.EnglishLocaleCode, StringComparison.Ordinal) ? "en" : "fa";
            string prefix = string.Format(CultureInfo.InvariantCulture, "{0}-aria-rs-{1}-{2}-", catalog, selectedSeed, token);
            string[] lines = string.IsNullOrEmpty(csvText)
                ? Array.Empty<string>()
                : csvText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                string candidate = prefix + attempt.ToString(CultureInfo.InvariantCulture);
                bool used = false;
                foreach (string line in lines)
                    if (line.StartsWith(candidate + ",", StringComparison.Ordinal) || line == candidate)
                    {
                        used = true;
                        break;
                    }
                if (!used)
                {
                    runId = candidate;
                    error = null;
                    return true;
                }
            }
            error = "All three ARIA slots for this seed and locale already have rows.";
            return false;
        }

        // ---- Queue entry point ----

        /// <summary>
        /// Wrapper entry: WARLINE_ARIA_QUEUE names a text file, one job per line
        /// "catalog,seed,locale" (blank lines, '#' comments and a "catalog,seed,locale"
        /// header are ignored). Runs every job in this Editor session, then exits once:
        /// 0 when every job appended a runs.csv row, 2 otherwise. Do not pass -quit.
        /// </summary>
        public static void RunAriaQueueAndExit()
        {
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
                    productionBatch != null || productionCanary != null || codedSizeCameraActive ||
                    SessionState.GetBool(Key, false) || SessionState.GetBool(QuitPendingKey, false) ||
                    SessionState.GetBool(AriaQueueActiveKey, false))
                    throw new InvalidOperationException("ARIA queue requires an idle Editor with no other owned run.");
                foreach (string variable in AriaQueueRefusedDiagnostics)
                    if (Environment.GetEnvironmentVariable(variable) == "1")
                        throw new InvalidOperationException("ARIA queue refuses diagnostic mode " + variable + "=1 (no ARIA rows).");
                string path = Environment.GetEnvironmentVariable("WARLINE_ARIA_QUEUE");
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    throw new FileNotFoundException("WARLINE_ARIA_QUEUE must name an existing queue file.", path);
                path = Path.GetFullPath(path);
                List<string> jobs = ParseAriaQueue(File.ReadAllLines(path));
                if (jobs.Count < 1 || jobs.Count > AriaQueueMaximumJobs)
                    throw new InvalidDataException("ARIA queue requires 1.." + AriaQueueMaximumJobs + " jobs; found " + jobs.Count + ".");

                Debug.Log("[SkirmishCandidateFingerprint] phase=Start owner=AriaQueue");
                var timer = System.Diagnostics.Stopwatch.StartNew();
                string sourceHash = SkirmishCandidateSourceHash.Compute(Path.GetFullPath(Path.Combine(Application.dataPath, "..")));
                timer.Stop();
                if (string.IsNullOrEmpty(sourceHash) || sourceHash.Length != 64)
                    throw new InvalidDataException("ARIA queue source fingerprint unavailable.");
                Debug.Log("[SkirmishCandidateFingerprint] phase=Completed owner=AriaQueue elapsedMilliseconds=" +
                    timer.Elapsed.TotalMilliseconds.ToString("R", CultureInfo.InvariantCulture) + " sha256=" + sourceHash);

                SessionState.SetString(AriaQueuePathKey, path);
                SessionState.SetString(AriaQueueJobsKey, string.Join("\n", jobs));
                SessionState.SetInt(AriaQueueIndexKey, 0);
                SessionState.SetInt(AriaQueueRecordedKey, 0);
                SessionState.SetInt(AriaQueueWinsKey, 0);
                SessionState.SetString(AriaQueueSourceHashKey, sourceHash);
                SessionState.SetBool(AriaQueueInFlightKey, false);
                SessionState.SetBool(AriaQueuePendingKey, true);
                SessionState.SetBool(AriaQueueActiveKey, true);
                Environment.SetEnvironmentVariable("WARLINE_ARIA_QUIT", "1");
                AppendAriaQueueResult("job,catalog,seed,locale,result,recorded,counted_win,seconds", true);
                Debug.Log(AriaQueueLog + " result=Started jobs=" + jobs.Count + " queue=" + path + " sha256=" + sourceHash);
                HookAriaQueueUpdate();
                // Dispatch now, inside -executeMethod, exactly like the single-run entry.
                DispatchAriaQueueJob();
            }
            catch (Exception error)
            {
                Debug.LogError(AriaQueueLog + " result=Failed reason=startup error=" + error);
                SessionState.SetBool(AriaQueueActiveKey, false);
                if (!EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.Exit(2);
            }
        }

        private static List<string> ParseAriaQueue(string[] lines)
        {
            var jobs = new List<string>();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
                string[] fields = line.Split(',');
                if (fields.Length != 3)
                    throw new InvalidDataException("Queue line " + (i + 1) + " must be catalog,seed,locale: " + line);
                string catalog = fields[0].Trim(), seedText = fields[1].Trim(), localeText = fields[2].Trim();
                if (string.Equals(catalog, "catalog", StringComparison.OrdinalIgnoreCase)) continue;
                if (!int.TryParse(seedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedSeed))
                    throw new InvalidDataException("Queue line " + (i + 1) + " has a non-integer seed: " + line);
                jobs.Add(catalog + "," + parsedSeed.ToString(CultureInfo.InvariantCulture) + "," + localeText);
            }
            return jobs;
        }

        private static string[] AriaQueueJobs()
        {
            string text = SessionState.GetString(AriaQueueJobsKey, "");
            return string.IsNullOrEmpty(text) ? Array.Empty<string>() : text.Split('\n');
        }

        [InitializeOnLoadMethod]
        private static void ResumeAriaQueueAfterDomainReload()
        {
            if (SessionState.GetBool(AriaQueueActiveKey, false)) HookAriaQueueUpdate();
        }

        private static void HookAriaQueueUpdate()
        {
            EditorApplication.update -= AriaQueueUpdate;
            EditorApplication.update += AriaQueueUpdate;
        }

        private static string AriaQueueSourceHashOrNull()
        {
            if (!SessionState.GetBool(AriaQueueActiveKey, false)) return null;
            string hash = SessionState.GetString(AriaQueueSourceHashKey, "");
            return hash.Length == 64 ? hash : null;
        }

        private static void AriaQueueUpdate()
        {
            if (!SessionState.GetBool(AriaQueueActiveKey, false))
            {
                EditorApplication.update -= AriaQueueUpdate;
                return;
            }
            bool idle = !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling &&
                !EditorApplication.isUpdating && !SessionState.GetBool(QuitPendingKey, false);
            if (SessionState.GetBool(AriaQueuePendingKey, false))
            {
                if (idle && !SessionState.GetBool(Key, false)) DispatchAriaQueueJob();
                return;
            }
            // Watchdog: a job that left Play Mode without any Finish/quit request.
            if (idle && SessionState.GetBool(AriaQueueInFlightKey, false) &&
                AriaQueueJobSeconds() > AriaQueueJobWatchdogSeconds)
            {
                Debug.LogError(AriaQueueLog + " watchdog=1 reason=jobLeftPlayWithoutFinish");
                EditorApplication.update -= Tick;
                SessionState.SetBool(Key, false);
                CompleteAriaQueueJob();
            }
        }

        private static double AriaQueueJobSeconds()
        {
            return long.TryParse(SessionState.GetString(AriaQueueJobStartKey, ""), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out long ticks)
                ? (DateTime.UtcNow.Ticks - ticks) / (double)TimeSpan.TicksPerSecond
                : 0d;
        }

        private static void DispatchAriaQueueJob()
        {
            SessionState.SetBool(AriaQueuePendingKey, false);
            string[] jobs = AriaQueueJobs();
            int index = SessionState.GetInt(AriaQueueIndexKey, 0);
            if (index >= jobs.Length)
            {
                CompleteAriaQueue();
                return;
            }
            SessionState.SetString(AriaQueueJobStartKey, DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture));
            SessionState.SetBool(AriaQueueJobRecordedKey, false);
            SessionState.SetBool(AriaQueueJobCountedKey, false);
            SessionState.SetString(AriaQueueJobResultKey, SkirmishS002AriaRunLog.ResultAbort);
            SessionState.SetBool(AriaQueueInFlightKey, true);
            string[] fields = jobs[index].Split(',');
            Debug.Log(AriaQueueLog + " dispatch job=" + (index + 1) + "/" + jobs.Length + " catalog=" + fields[0] +
                " seed=" + fields[1] + " locale=" + fields[2]);
            try
            {
                int jobSeed = int.Parse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture);
                // Validate before touching Editor state; an unsupported job is an Abort, not a queue stop.
                if (!TryCreateSelectedPayload(fields[0], jobSeed, fields[2], out _, out string payloadError))
                    throw new InvalidOperationException(payloadError ?? "Payload rejected.");
                Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_CATALOG", fields[0]);
                Environment.SetEnvironmentVariable("WARLINE_S002_SEED", fields[1]);
                Environment.SetEnvironmentVariable("WARLINE_S002_LOCALE", fields[2]);
                RunFocusedAriaAndExit();
            }
            catch (Exception error)
            {
                Debug.LogError(AriaQueueLog + " job=" + (index + 1) + "/" + jobs.Length + " launchFailed=1 error=" + error.Message);
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.update -= Tick;
                    SessionState.SetBool(Key, false);
                    CompleteAriaQueueJob();
                }
                // If Play already started, the ordinary Tick/Finish path owns the abort.
            }
        }

        // Called from FinishEvidence after the row append attempt.
        private static void ObserveAriaQueueJobRow(bool appended, bool counted, string row)
        {
            if (!SessionState.GetBool(AriaQueueActiveKey, false)) return;
            SessionState.SetBool(AriaQueueJobRecordedKey, appended);
            SessionState.SetBool(AriaQueueJobCountedKey, appended && counted);
            string result = appended ? RowResult(row) : string.Empty;
            SessionState.SetString(AriaQueueJobResultKey,
                string.IsNullOrEmpty(result) ? SkirmishS002AriaRunLog.ResultAbort : result);
        }

        // Called from FinishQuitAfterPlayMode after the owned Play cleanup; consumes the
        // per-job quit request and schedules the next job instead of exiting.
        private static bool TryContinueAriaQueue(int jobExitCode)
        {
            if (!SessionState.GetBool(AriaQueueActiveKey, false)) return false;
            Debug.Log(AriaQueueLog + " jobExitCode=" + jobExitCode);
            // Play has ended and the job's quit request was consumed. A Finish that failed
            // before clearing its run latch must not stall the next dispatch.
            EditorApplication.update -= Tick;
            SessionState.SetBool(Key, false);
            CompleteAriaQueueJob();
            return true;
        }

        private static void CompleteAriaQueueJob()
        {
            string[] jobs = AriaQueueJobs();
            int index = SessionState.GetInt(AriaQueueIndexKey, 0);
            string[] fields = index < jobs.Length ? jobs[index].Split(',') : new[] { "?", "?", "?" };
            bool recorded = SessionState.GetBool(AriaQueueJobRecordedKey, false);
            bool counted = SessionState.GetBool(AriaQueueJobCountedKey, false);
            string result = recorded ? SessionState.GetString(AriaQueueJobResultKey, SkirmishS002AriaRunLog.ResultAbort)
                : SkirmishS002AriaRunLog.ResultAbort;
            string seconds = AriaQueueJobSeconds().ToString("0.#", CultureInfo.InvariantCulture);
            if (recorded) SessionState.SetInt(AriaQueueRecordedKey, SessionState.GetInt(AriaQueueRecordedKey, 0) + 1);
            if (counted) SessionState.SetInt(AriaQueueWinsKey, SessionState.GetInt(AriaQueueWinsKey, 0) + 1);
            Debug.Log(AriaQueueLog + " job=" + (index + 1) + "/" + jobs.Length + " catalog=" + fields[0] +
                " seed=" + fields[1] + " locale=" + fields[2] + " result=" + result + " seconds=" + seconds +
                " recorded=" + (recorded ? 1 : 0) + " countedWin=" + (counted ? 1 : 0));
            AppendAriaQueueResult(string.Join(",", (index + 1).ToString(CultureInfo.InvariantCulture), fields[0], fields[1],
                fields[2], result, recorded ? "1" : "0", counted ? "1" : "0", seconds), false);
            SessionState.SetBool(AriaQueueInFlightKey, false);
            SessionState.SetInt(AriaQueueIndexKey, index + 1);
            SessionState.SetBool(AriaQueuePendingKey, true);
            HookAriaQueueUpdate();
        }

        private static void CompleteAriaQueue()
        {
            string[] jobs = AriaQueueJobs();
            int recorded = SessionState.GetInt(AriaQueueRecordedKey, 0);
            int wins = SessionState.GetInt(AriaQueueWinsKey, 0);
            string startHash = SessionState.GetString(AriaQueueSourceHashKey, "");
            // Clear ownership first: the final quit must reach the ordinary exit owner.
            SessionState.SetBool(AriaQueueActiveKey, false);
            SessionState.SetBool(AriaQueuePendingKey, false);
            SessionState.SetBool(AriaQueueInFlightKey, false);
            EditorApplication.update -= AriaQueueUpdate;
            string endHash = string.Empty;
            try
            {
                Debug.Log("[SkirmishCandidateFingerprint] phase=Start owner=AriaQueueEnd");
                var timer = System.Diagnostics.Stopwatch.StartNew();
                endHash = SkirmishCandidateSourceHash.Compute(Path.GetFullPath(Path.Combine(Application.dataPath, "..")));
                timer.Stop();
                Debug.Log("[SkirmishCandidateFingerprint] phase=Completed owner=AriaQueueEnd elapsedMilliseconds=" +
                    timer.Elapsed.TotalMilliseconds.ToString("R", CultureInfo.InvariantCulture) + " sha256=" + endHash);
            }
            catch (Exception error)
            {
                Debug.LogError("[SkirmishSourceFingerprint] result=Failed owner=AriaQueueEnd error=" + error);
            }
            bool sourceUnchanged = startHash.Length == 64 && endHash == startHash;
            int exitCode = sourceUnchanged && recorded == jobs.Length ? 0 : 2;
            if (sourceUnchanged)
                Debug.Log(AriaQueueLog + " result=Completed jobs=" + jobs.Length + " recorded=" + recorded + " wins=" + wins +
                    " sourceUnchanged=1 exitCode=" + exitCode);
            else
                Debug.LogError(AriaQueueLog + " result=Failed reason=candidateSourceChangedDuringQueue jobs=" + jobs.Length +
                    " recorded=" + recorded + " wins=" + wins + " sourceUnchanged=0 exitCode=" + exitCode);
            Environment.SetEnvironmentVariable("WARLINE_ARIA_QUIT", "1");
            RequestQuitIfAsked(exitCode);
        }

        private static void AppendAriaQueueResult(string line, bool header)
        {
            string path = SessionState.GetString(AriaQueuePathKey, "");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                string output = path + ".results.csv";
                if (header && File.Exists(output)) return;
                File.AppendAllText(output, line + "\n", new UTF8Encoding(false));
            }
            catch (Exception error)
            {
                Debug.LogWarning(AriaQueueLog + " resultsFile=Unavailable error=" + error.Message);
            }
        }
    }
}
#endif
