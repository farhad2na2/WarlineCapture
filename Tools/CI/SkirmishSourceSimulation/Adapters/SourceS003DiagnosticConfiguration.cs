using System;
using System.Linq;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Game.Runtime
{
    // One explicit source-only run configuration. It grants no native or player
    // qualification and does not change the captured Regular input identity.
    internal sealed class SourceS003DiagnosticConfiguration : IDisposable
    {
        internal static SourceS003DiagnosticConfiguration Current { get; private set; }
        internal readonly SkirmishDifficultyId SelectedDifficulty;
        internal readonly string Path, Sha256;
        private SkirmishResolvedSetup regular, selected;

        private SourceS003DiagnosticConfiguration(SkirmishDifficultyId difficulty, string path, string sha)
        { SelectedDifficulty = difficulty; Path = path; Sha256 = sha; }

        internal static SourceS003DiagnosticConfiguration Open(JObject initialization)
        {
            string path = Environment.GetEnvironmentVariable("WARLINE_SOURCE_DIAGNOSTIC_CONFIG");
            string hash = Environment.GetEnvironmentVariable("WARLINE_SOURCE_DIAGNOSTIC_CONFIG_SHA256");
            if (String.IsNullOrEmpty(path) && String.IsNullOrEmpty(hash)) return null;
            if (Current != null || String.IsNullOrEmpty(path) || String.IsNullOrEmpty(hash) ||
                !System.IO.Path.IsPathFullyQualified(path) || hash.Length != 64 ||
                hash.Any(c => !(c >= '0' && c <= '9' || c >= 'a' && c <= 'f')))
                throw new InvalidOperationException("One exact source diagnostic configuration required");
            var config = JObject.Parse(System.Text.Encoding.UTF8.GetString(AutonomousWorld.PinnedFile(path, hash, 65536)),
                new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            string[] fields = { "schema_version", "kind", "mission_id", "seed", "size_id", "selected_difficulty",
                "capture_difficulty", "capture_qualification", "native_parity", "runtime_admission", "mission_ready" };
            if (config.Count != fields.Length || config.Properties().Any(p => !fields.Contains(p.Name)) ||
                (int?)config["schema_version"] != 1 || (string)config["kind"] != "S003SourceDiagnosticSelection" ||
                (string)config["mission_id"] != "S003" || (string)initialization?["mission_id"] != "S003" ||
                (string)config["capture_difficulty"] != "Regular" ||
                config["native_parity"]?.Type != JTokenType.Boolean || (bool)config["native_parity"] ||
                config["runtime_admission"]?.Type != JTokenType.Boolean || (bool)config["runtime_admission"] ||
                config["mission_ready"]?.Type != JTokenType.Boolean || (bool)config["mission_ready"] ||
                !JToken.DeepEquals(config["capture_qualification"], initialization["configuration_qualification"]))
                throw new InvalidOperationException("Exact source-only Regular capture binding required");
            var selection = initialization["selection"] as JObject;
            if (selection == null || (int?)selection["difficulty_id"] != (int)SkirmishDifficultyId.Regular ||
                config["seed"]?.Type != JTokenType.Integer || config["size_id"]?.Type != JTokenType.Integer ||
                (int?)config["seed"] != (int?)selection["seed"] || (int?)config["size_id"] != (int?)selection["size_id"] ||
                config["selected_difficulty"]?.Type != JTokenType.String ||
                !Enum.TryParse((string)config["selected_difficulty"], false, out SkirmishDifficultyId difficulty) ||
                difficulty.ToString() != (string)config["selected_difficulty"] ||
                (difficulty != SkirmishDifficultyId.Recruit && difficulty != SkirmishDifficultyId.Veteran && difficulty != SkirmishDifficultyId.Commander))
                throw new InvalidOperationException("Exact non-Regular diagnostic selection required");
            Current = new SourceS003DiagnosticConfiguration(difficulty, path, hash);
            return Current;
        }

        internal void Bind(SkirmishResolvedSetup capturedRegular, SkirmishResolvedSetup actualSelected)
        {
            if (!ReferenceEquals(Current, this) || regular != null || actualSelected == null || actualSelected.DifficultyId != SelectedDifficulty)
                throw new InvalidOperationException("Fresh selected source compiler setup required");
            SourceS003DifficultySetupCompatibility.Require(capturedRegular, actualSelected);
            regular = capturedRegular; selected = actualSelected;
        }

        internal bool IsSelected(SkirmishResolvedSetup value)
        {
            if (!ReferenceEquals(Current, this) || !ReferenceEquals(value, selected) || regular == null) return false;
            try { SourceS003DifficultySetupCompatibility.Require(regular, selected); return true; }
            catch (InvalidOperationException) { return false; }
        }

        internal void RequirePair(SkirmishResolvedSetup capturedRegular, SkirmishResolvedSetup actualSelected)
        {
            if (!ReferenceEquals(capturedRegular, regular) || !IsSelected(actualSelected))
                throw new InvalidOperationException("Same bound source compiler setup pair required");
        }

        public void Dispose()
        {
            try { AutonomousWorld.PinnedFile(Path, Sha256, 65536); }
            finally
            {
                if (ReferenceEquals(Current, this)) Current = null;
                regular = null; selected = null;
            }
        }
    }
}
