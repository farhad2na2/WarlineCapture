using System;

namespace Game.Operations.Contracts
{
    /// <summary>
    /// Admin lock: Operations Unity work stays on a shadow worktree.
    /// The shared Windows checkout is reserved for Programmer 1 / other tracks.
    /// </summary>
    public static class OperationsShadowProject
    {
        public const string SharedWindowsCheckout = @"D:\Projects\WarlineCapture";
        public const string ShadowWindowsCheckout = @"D:\Projects\WarlineCapture-Operations";
        public const string PreferredBranchPrefix = "cursor/operations-";

        public static string NormalizeProjectPath(string projectPath)
        {
            if (string.IsNullOrWhiteSpace(projectPath))
                return string.Empty;

            string unified = Environment.ExpandEnvironmentVariables(projectPath.Trim())
                .Replace('/', '\\');
            if (LooksLikeWindowsDrivePath(unified))
                return char.ToUpperInvariant(unified[0]) + unified.Substring(1).TrimEnd('\\');

            return unified.TrimEnd('\\');
        }

        public static bool IsSharedWindowsCheckout(string projectPath)
        {
            string normalized = NormalizeProjectPath(projectPath);
            return IsSameOrDescendant(normalized, SharedWindowsCheckout);
        }

        public static bool IsShadowWindowsCheckout(string projectPath)
        {
            string normalized = NormalizeProjectPath(projectPath);
            return IsSameOrDescendant(normalized, ShadowWindowsCheckout);
        }

        public static bool TryRejectSharedWindowsCheckout(string projectPath, out string error)
        {
            if (IsSharedWindowsCheckout(projectPath))
            {
                error = "Operations must not open or lock " + SharedWindowsCheckout +
                        ". Use the shadow worktree " + ShadowWindowsCheckout + ".";
                return true;
            }

            error = null;
            return false;
        }

        public static bool IsApprovedWindowsValidationPath(string projectPath) =>
            IsShadowWindowsCheckout(projectPath) && !IsSharedWindowsCheckout(projectPath);

        private static bool LooksLikeWindowsDrivePath(string path) =>
            path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':';

        private static bool IsSameOrDescendant(string candidate, string root)
        {
            if (candidate.Length == 0)
                return false;

            string expected = NormalizeProjectPath(root);
            if (string.Equals(candidate, expected, StringComparison.OrdinalIgnoreCase))
                return true;

            string prefix = expected + "\\";
            return candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }
    }
}
