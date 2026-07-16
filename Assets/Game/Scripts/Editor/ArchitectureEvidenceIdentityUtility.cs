namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;

    internal sealed class ArchitectureEvidenceIdentity
    {
        public ArchitectureEvidenceIdentity(string exactCommit, string environmentIdentitySha256, bool dirty)
        {
            if (!IsLowerHex(exactCommit, 40))
                throw new ArgumentException("Exact commit must be a 40-character lowercase Git identity.", nameof(exactCommit));
            if (!IsLowerHex(environmentIdentitySha256, 64))
                throw new ArgumentException("Environment identity must be a 64-character lowercase SHA-256.", nameof(environmentIdentitySha256));

            ExactCommit = exactCommit;
            EnvironmentIdentitySha256 = environmentIdentitySha256;
            Dirty = dirty;
        }

        public string ExactCommit { get; }
        public string EnvironmentIdentitySha256 { get; }
        public bool Dirty { get; }

        private static bool IsLowerHex(string value, int length)
        {
            return value != null &&
                   value.Length == length &&
                   value.All(character =>
                       character is >= '0' and <= '9' or >= 'a' and <= 'f');
        }
    }

    internal static class ArchitectureEvidenceIdentityUtility
    {
        internal const string EnvironmentIdentityPath =
            "Design/AgentReports/ArchitectureMaturity/entry_environment.json";

        public static ArchitectureEvidenceIdentity ResolveIfAvailable(
            string projectRoot,
            IEnumerable<string> allowedDirtyPaths = null)
        {
            string root = Path.GetFullPath(projectRoot ?? throw new ArgumentNullException(nameof(projectRoot)));
            string gitPath = Path.Combine(root, ".git");
            if (!Directory.Exists(gitPath) && !File.Exists(gitPath))
                return null;

            string environmentPath = Path.Combine(root, EnvironmentIdentityPath);
            if (!File.Exists(environmentPath))
                throw new FileNotFoundException("Architecture environment identity is missing.", environmentPath);

            string exactCommit = RunGit(root, "rev-parse HEAD").Trim().ToLowerInvariant();
            string environmentHash = ComputeSha256(environmentPath);
            HashSet<string> allowed = new(
                (allowedDirtyPaths ?? Array.Empty<string>())
                .Select(NormalizePath),
                StringComparer.Ordinal);
            bool dirty = RunGit(root, "status --porcelain --untracked-files=normal")
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(ParseStatusPath)
                .Any(path => !allowed.Contains(path));
            return new ArchitectureEvidenceIdentity(exactCommit, environmentHash, dirty);
        }

        private static string ComputeSha256(string path)
        {
            using SHA256 sha = SHA256.Create();
            using FileStream stream = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static string ParseStatusPath(string statusLine)
        {
            string path = statusLine.Length > 3 ? statusLine.Substring(3).Trim() : statusLine.Trim();
            int renameSeparator = path.LastIndexOf(" -> ", StringComparison.Ordinal);
            if (renameSeparator >= 0)
                path = path.Substring(renameSeparator + 4);
            return NormalizePath(path.Trim('"'));
        }

        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/').TrimStart('.', '/');
        }

        private static string RunGit(string projectRoot, string arguments)
        {
            using Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = projectRoot,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            if (!process.Start())
                throw new InvalidOperationException("Failed to start Git while resolving architecture evidence identity.");
            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            if (!process.WaitForExit(30000) || process.ExitCode != 0)
                throw new InvalidOperationException($"Git identity command failed: {stderr.Trim()}");
            return stdout;
        }
    }
}
