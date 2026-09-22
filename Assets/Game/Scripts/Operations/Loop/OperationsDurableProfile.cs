using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Game.Operations.Loop
{
    /// <summary>
    /// Disk copy of the Package 3 strategic profile, loop journal, and checkpoint blobs.
    /// Opening the directory restores the last committed hub or mission.
    /// <see cref="OperationsShippingEnvelope"/> copies this directory into the shipping
    /// profile field <c>operationsEnvelope</c>.
    /// </summary>
    public static class OperationsDurableProfile
    {
        /// <summary>
        /// Directory sentinel written to ready.txt. It is not a player-ready certification.
        /// </summary>
        public const string ReadyMarker = "o001-player-ready";

        const string ReadyFile = "ready.txt";
        const string StrategicFile = "strategic.json";
        const string CampaignFile = "campaign.b64";
        const string QuickFile = "quick.b64";
        const string LoopFile = "loop.txt";
        const string BlobDirectory = "blobs";

        public static bool Exists(string directory) =>
            !string.IsNullOrEmpty(directory) && File.Exists(Path.Combine(directory, ReadyFile));

        public static void Write(string directory, OperationsLoopSession session)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentException("Profile directory is required.", nameof(directory));
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            Directory.CreateDirectory(directory);
            string readyPath = Path.Combine(directory, ReadyFile);
            if (File.Exists(readyPath))
                File.Delete(readyPath);

            var encoding = new UTF8Encoding(false);
            File.WriteAllText(Path.Combine(directory, StrategicFile), session.CommittedJson ?? string.Empty, encoding);
            File.WriteAllText(Path.Combine(directory, CampaignFile), Convert.ToBase64String(session.CampaignEnvelope ?? Array.Empty<byte>()), encoding);
            File.WriteAllText(Path.Combine(directory, QuickFile), Convert.ToBase64String(session.QuickGameEnvelope ?? Array.Empty<byte>()), encoding);
            File.WriteAllText(Path.Combine(directory, LoopFile), WriteDocument(session.CopyDocument()), encoding);

            var blobs = new Dictionary<string, string>(StringComparer.Ordinal);
            session.CopyCheckpointBlobs(blobs);
            string blobDirectory = Path.Combine(directory, BlobDirectory);
            Directory.CreateDirectory(blobDirectory);
            var keep = new HashSet<string>(blobs.Keys, StringComparer.Ordinal);
            foreach (string existing in Directory.GetFiles(blobDirectory))
            {
                if (!keep.Contains(Path.GetFileName(existing)))
                    File.Delete(existing);
            }

            foreach (KeyValuePair<string, string> pair in blobs)
                File.WriteAllText(Path.Combine(blobDirectory, pair.Key), pair.Value ?? string.Empty, encoding);

            File.WriteAllText(readyPath, ReadyMarker + "\n", encoding);
        }

        public static OperationsLoopSession Read(string directory)
        {
            if (!Exists(directory))
                throw new InvalidOperationException("profile_missing");
            string ready = File.ReadAllText(Path.Combine(directory, ReadyFile)).Trim();
            if (!string.Equals(ready, ReadyMarker, StringComparison.Ordinal))
                throw new InvalidOperationException("profile_marker");

            var encoding = new UTF8Encoding(false);
            string json = File.ReadAllText(Path.Combine(directory, StrategicFile), encoding);
            byte[] campaign = Convert.FromBase64String(File.ReadAllText(Path.Combine(directory, CampaignFile), encoding).Trim());
            byte[] quick = Convert.FromBase64String(File.ReadAllText(Path.Combine(directory, QuickFile), encoding).Trim());
            OperationsLoopDocument document = ReadDocument(File.ReadAllText(Path.Combine(directory, LoopFile), encoding));
            var blobs = new Dictionary<string, string>(StringComparer.Ordinal);
            string blobDirectory = Path.Combine(directory, BlobDirectory);
            if (Directory.Exists(blobDirectory))
            {
                foreach (string file in Directory.GetFiles(blobDirectory))
                    blobs[Path.GetFileName(file)] = File.ReadAllText(file, encoding);
            }

            return OperationsLoopSession.Open(json, campaign, quick, document, blobs);
        }

        static string WriteDocument(OperationsLoopDocument document)
        {
            var builder = new StringBuilder();
            builder.Append("schema=1\n");
            Line(builder, "phase", ((int)document.Phase).ToString(CultureInfo.InvariantCulture));
            Line(builder, "slot", ((int)document.Slot).ToString(CultureInfo.InvariantCulture));
            Line(builder, "runId", document.RunId);
            Line(builder, "sessionId", document.SessionId);
            Line(builder, "offerId", document.OfferId);
            Line(builder, "missionId", document.MissionId);
            Line(builder, "districtId", document.DistrictId);
            Line(builder, "mapId", document.MapId);
            Line(builder, "scenarioId", document.ScenarioId);
            Line(builder, "transactionId", document.TransactionId);
            Line(builder, "snapshotHash", document.SnapshotHash);
            Line(builder, "contentHash", document.ContentHash);
            Line(builder, "seed", document.Seed.ToString(CultureInfo.InvariantCulture));
            Line(builder, "attemptOrdinal", document.AttemptOrdinal.ToString(CultureInfo.InvariantCulture));
            Line(builder, "runRevision", document.RunRevision.ToString(CultureInfo.InvariantCulture));
            Line(builder, "definitionVersion", document.DefinitionVersion.ToString(CultureInfo.InvariantCulture));
            Line(builder, "restartCount", document.RestartCount.ToString(CultureInfo.InvariantCulture));
            Line(builder, "checkpointSequence", document.CheckpointSequence.ToString(CultureInfo.InvariantCulture));
            Line(builder, "practice", document.Practice ? "1" : "0");
            Line(builder, "returnAcknowledged", document.ReturnAcknowledged ? "1" : "0");
            Line(builder, "refundEligible", document.RefundEligible ? "1" : "0");
            Line(builder, "publishedCheckpointId", document.PublishedCheckpointId);
            Line(builder, "resultText", document.ResultText);
            Line(builder, "resultHash", document.ResultHash);
            Line(builder, "beforeMetrics", document.BeforeMetrics);
            Line(builder, "afterMetrics", document.AfterMetrics);
            Line(builder, "history", document.History);
            Line(builder, "creditsAtLaunch", document.CreditsAtLaunch.ToString(CultureInfo.InvariantCulture));
            Line(builder, "xpAtLaunch", document.XpAtLaunch.ToString(CultureInfo.InvariantCulture));
            Line(builder, "actionPointsAtLaunch", document.ActionPointsAtLaunch.ToString(CultureInfo.InvariantCulture));
            Line(builder, "invokeSharedSceneView", document.InvokeSharedSceneView ? "1" : "0");
            return builder.ToString();
        }

        static void Line(StringBuilder builder, string key, string value)
        {
            if ((value ?? string.Empty).IndexOf('\n') >= 0 || (value ?? string.Empty).IndexOf('\r') >= 0)
                throw new InvalidOperationException("profile_newline:" + key);
            builder.Append(key).Append('=').Append(value ?? string.Empty).Append('\n');
        }

        static OperationsLoopDocument ReadDocument(string text)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            string[] lines = (text ?? string.Empty).Split('\n');
            for (int index = 0; index < lines.Length; index++)
            {
                string line = lines[index].TrimEnd('\r');
                if (line.Length == 0)
                    continue;
                int split = line.IndexOf('=');
                if (split <= 0)
                    throw new InvalidOperationException("profile_line");
                values[line.Substring(0, split)] = line.Substring(split + 1);
            }

            if (!values.TryGetValue("schema", out string schema) || schema != "1")
                throw new InvalidOperationException("profile_schema");

            return new OperationsLoopDocument
            {
                Phase = (OperationsLoopPhase)RequireInt(values, "phase"),
                Slot = (OperationsMatchMode)RequireInt(values, "slot"),
                RunId = Require(values, "runId"),
                SessionId = Require(values, "sessionId"),
                OfferId = Require(values, "offerId"),
                MissionId = Require(values, "missionId"),
                DistrictId = Require(values, "districtId"),
                MapId = Require(values, "mapId"),
                ScenarioId = Require(values, "scenarioId"),
                TransactionId = Require(values, "transactionId"),
                SnapshotHash = Require(values, "snapshotHash"),
                ContentHash = Require(values, "contentHash"),
                Seed = RequireInt(values, "seed"),
                AttemptOrdinal = RequireInt(values, "attemptOrdinal"),
                RunRevision = RequireInt(values, "runRevision"),
                DefinitionVersion = RequireInt(values, "definitionVersion"),
                RestartCount = RequireInt(values, "restartCount"),
                CheckpointSequence = RequireInt(values, "checkpointSequence"),
                Practice = RequireBool(values, "practice"),
                ReturnAcknowledged = RequireBool(values, "returnAcknowledged"),
                RefundEligible = RequireBool(values, "refundEligible"),
                PublishedCheckpointId = Require(values, "publishedCheckpointId"),
                ResultText = Require(values, "resultText"),
                ResultHash = Require(values, "resultHash"),
                BeforeMetrics = Require(values, "beforeMetrics"),
                AfterMetrics = Require(values, "afterMetrics"),
                History = Require(values, "history"),
                CreditsAtLaunch = RequireInt(values, "creditsAtLaunch"),
                XpAtLaunch = RequireInt(values, "xpAtLaunch"),
                ActionPointsAtLaunch = RequireInt(values, "actionPointsAtLaunch"),
                InvokeSharedSceneView = OptionalBool(values, "invokeSharedSceneView")
            };
        }

        static string Require(Dictionary<string, string> values, string key)
        {
            if (!values.TryGetValue(key, out string value))
                throw new InvalidOperationException("profile_field:" + key);
            return value;
        }

        static int RequireInt(Dictionary<string, string> values, string key)
        {
            if (!int.TryParse(Require(values, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out int number))
                throw new InvalidOperationException("profile_int:" + key);
            return number;
        }

        static bool RequireBool(Dictionary<string, string> values, string key)
        {
            string value = Require(values, key);
            if (value == "1")
                return true;
            if (value == "0")
                return false;
            throw new InvalidOperationException("profile_bool:" + key);
        }

        static bool OptionalBool(Dictionary<string, string> values, string key)
        {
            if (!values.TryGetValue(key, out string value) || value.Length == 0)
                return false;
            if (value == "1")
                return true;
            if (value == "0")
                return false;
            throw new InvalidOperationException("profile_bool:" + key);
        }
    }
}
