using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Game.Configs
{
    /// <summary>
    /// ARIA queue request file. An already open Editor (Tools/CI/invoke_unity_macos.sh --reuse) cannot receive process
    /// environment variables, so run_aria_queue.py --reuse writes this file at a fixed project-relative path and the
    /// harness reads it in RunAriaQueueAndExit. The environment path (WARLINE_ARIA_QUEUE and friends) keeps working and
    /// wins when WARLINE_ARIA_QUEUE is set. The harness consumes the file (renames it) so a stale request is never replayed.
    /// </summary>
    [Serializable]
    public sealed class SkirmishAriaQueueRequest
    {
        public const string RelativePath = "Temp/aria-queue/request.json";
        public const int SupportedVersion = 1;
        public const double MaxAgeSeconds = 900d;
        public const double MaxFutureSkewSeconds = 60d;
        public const string Marker = "[SkirmishAriaQueue]";

        public int version;
        public string requestId;
        /// <summary>Absolute project root the request was written for; a request for another checkout is refused.</summary>
        public string project;
        /// <summary>Absolute path of the queue file (one "catalog,seed,locale" job per line).</summary>
        public string queue;
        /// <summary>Same as WARLINE_MENU_JOURNEY=1.</summary>
        public bool menuJourney;
        /// <summary>Same as WARLINE_VALIDATION_LIBRARY=1.</summary>
        public bool validationLibrary;
        /// <summary>UTC, round-trip ISO 8601 ("2026-11-17T12:00:00Z").</summary>
        public string createdUtc;

        public static string PathFor(string projectRoot) =>
            Path.Combine(projectRoot, RelativePath.Replace('/', Path.DirectorySeparatorChar));

        /// <summary>Null on success. Never throws on malformed text.</summary>
        public static string Parse(string json, out SkirmishAriaQueueRequest request)
        {
            request = null;
            if (string.IsNullOrWhiteSpace(json)) return "requestEmpty";
            // JsonUtility accepts truncated text and fills what it can; a half-written file must be refused whole.
            string trimmed = json.Trim();
            if (!trimmed.StartsWith("{", StringComparison.Ordinal) || !trimmed.EndsWith("}", StringComparison.Ordinal))
                return "requestUnparseable";
            try { request = JsonUtility.FromJson<SkirmishAriaQueueRequest>(json); }
            catch (Exception) { request = null; }
            return request == null ? "requestUnparseable" : null;
        }

        /// <summary>Null when the request may drive this project's Editor now.</summary>
        public static string Validate(SkirmishAriaQueueRequest request, string projectRoot, DateTime utcNow, Func<string, bool> fileExists)
        {
            if (request == null) return "requestMissing";
            if (request.version != SupportedVersion) return "requestVersionUnsupported:" + request.version;
            if (string.IsNullOrWhiteSpace(request.requestId)) return "requestIdMissing";
            if (!SameDirectory(request.project, projectRoot))
                return "requestProjectMismatch:request=" + (request.project ?? "") + ",editor=" + projectRoot;
            if (string.IsNullOrWhiteSpace(request.queue) || !Path.IsPathRooted(request.queue)) return "requestQueueNotAbsolute";
            if (fileExists == null || !fileExists(request.queue)) return "requestQueueMissing:" + request.queue;
            if (!DateTime.TryParse(request.createdUtc, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime created))
                return "requestCreatedUtcInvalid";
            double age = (utcNow - created).TotalSeconds;
            if (age > MaxAgeSeconds) return "requestStale:ageSeconds=" + ((long)age).ToString(CultureInfo.InvariantCulture);
            if (age < -MaxFutureSkewSeconds) return "requestFromFuture:ageSeconds=" + ((long)age).ToString(CultureInfo.InvariantCulture);
            return null;
        }

        /// <summary>Where the harness moves a request it has read, so a stale request is never replayed.</summary>
        public static string ConsumedPath(string requestPath, string requestId)
        {
            string safe = string.IsNullOrEmpty(requestId) ? "unknown" : requestId;
            foreach (char invalid in Path.GetInvalidFileNameChars()) safe = safe.Replace(invalid, '_');
            return requestPath + ".consumed-" + safe;
        }

        public static string FormatAccepted(SkirmishAriaQueueRequest request) =>
            Marker + " request=File id=" + request.requestId + " menuJourney=" + (request.menuJourney ? 1 : 0) +
            " validationLibrary=" + (request.validationLibrary ? 1 : 0) + " queue=" + request.queue;

        public static string FormatRefused(string reason) => Marker + " request=Refused reason=" + reason;

        private static bool SameDirectory(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return false;
            return string.Equals(Normalize(a), Normalize(b), StringComparison.Ordinal);
        }

        private static string Normalize(string path) =>
            path.Replace('\\', '/').TrimEnd('/');
    }
}
