using System;

namespace Game.Editor
{
    internal readonly struct StaticMapPresentationBakeInput
    {
        internal StaticMapPresentationBakeInput(
            string operationMapId,
            string sourceScenePath,
            string sourceMapRootPath,
            string outputRoot,
            string manifestPath,
            string integrityPath,
            float chunkSize)
        {
            OperationMapId = operationMapId;
            SourceScenePath = sourceScenePath;
            SourceMapRootPath = sourceMapRootPath;
            OutputRoot = outputRoot;
            ManifestPath = manifestPath;
            IntegrityPath = integrityPath;
            ChunkSize = chunkSize;
        }

        internal string OperationMapId { get; }
        internal string SourceScenePath { get; }
        internal string SourceMapRootPath { get; }
        internal string OutputRoot { get; }
        internal string SceneOutputFolder => $"{OutputRoot}/Scenes";
        internal string ManifestPath { get; }
        internal string IntegrityPath { get; }
        internal float ChunkSize { get; }

        internal bool TryValidate(out string error)
        {
            if (string.IsNullOrWhiteSpace(OperationMapId) || OperationMapId.Length > 64)
                return Fail("Operation-map id must contain 1 to 64 characters.", out error);
            if (!IsAssetFilePath(SourceScenePath, ".unity"))
                return Fail("Source scene must be a normalized project-relative Unity scene path.", out error);
            if (!IsHierarchyPath(SourceMapRootPath))
                return Fail("Source map root must be a normalized hierarchy path.", out error);
            if (!IsAssetFolderPath(OutputRoot))
                return Fail("Output root must be a normalized project-relative asset folder.", out error);
            if (!IsOwnedOutputFile(ManifestPath, OutputRoot, ".asset"))
                return Fail("Manifest must be an asset owned by the output root.", out error);
            if (!IsOwnedOutputFile(IntegrityPath, OutputRoot, ".json"))
                return Fail("Integrity ledger must be JSON owned by the output root.", out error);
            if (string.Equals(ManifestPath, IntegrityPath, StringComparison.Ordinal))
                return Fail("Manifest and integrity ledger paths must be distinct.", out error);
            if (!float.IsFinite(ChunkSize) || ChunkSize <= 0f)
                return Fail("Chunk size must be finite and greater than zero.", out error);

            error = null;
            return true;
        }

        private static bool IsAssetFilePath(string path, string extension)
        {
            return IsNormalizedProjectPath(path) &&
                   path.StartsWith("Assets/", StringComparison.Ordinal) &&
                   path.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAssetFolderPath(string path)
        {
            return IsNormalizedProjectPath(path) &&
                   path.StartsWith("Assets/", StringComparison.Ordinal) &&
                   !path.EndsWith("/", StringComparison.Ordinal) &&
                   path.IndexOf('.', path.LastIndexOf('/') + 1) < 0;
        }

        private static bool IsOwnedOutputFile(string path, string outputRoot, string extension)
        {
            return IsAssetFilePath(path, extension) &&
                   path.StartsWith(outputRoot + "/", StringComparison.Ordinal);
        }

        private static bool IsHierarchyPath(string path)
        {
            return !string.IsNullOrWhiteSpace(path) &&
                   !path.StartsWith("/", StringComparison.Ordinal) &&
                   !path.EndsWith("/", StringComparison.Ordinal) &&
                   path.IndexOf('\\') < 0 &&
                   path.IndexOf("//", StringComparison.Ordinal) < 0;
        }

        private static bool IsNormalizedProjectPath(string path)
        {
            return !string.IsNullOrWhiteSpace(path) &&
                   path.IndexOf('\\') < 0 &&
                   path.IndexOf("//", StringComparison.Ordinal) < 0 &&
                   path.IndexOf("/../", StringComparison.Ordinal) < 0 &&
                   !path.EndsWith("/..", StringComparison.Ordinal) &&
                   !path.StartsWith("../", StringComparison.Ordinal);
        }

        private static bool Fail(string message, out string error)
        {
            error = message;
            return false;
        }
    }
}
