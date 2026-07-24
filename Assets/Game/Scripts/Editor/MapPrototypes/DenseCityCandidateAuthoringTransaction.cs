using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Authoring;
using Game.Configs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    internal static class DenseCityCandidateAuthoringTransaction
    {
        internal const string CandidateMapScenePath =
            "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/" +
            "opmap_skirmish_desert_base_01_dense_city_authoring_candidate.unity";
        internal const string CandidateEntityScenePath =
            "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/" +
            "opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity";

        private const string SourceMapScenePath =
            "Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity";
        private const string SourceEntityScenePath =
            "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/" +
            "opmap_skirmish_desert_base_01_entity_presentation_candidate.unity";
        private const string ConfigPath =
            "Assets/Game/Configs/OperationMaps/Skirmish/" +
            "SkirmishDesertBase_MapWideCity_Config.asset";
        private const string GeneratorSchema = "dense-city-v1";
        private const int GeneratorSchemaVersion = 1;

        [MenuItem("Game/Maps/Skirmish Desert Base/Create Dense City Candidate Hierarchy")]
        public static void CreateCandidateHierarchy()
        {
            RuntimeCitySpawnerSystemConfig config =
                AssetDatabase.LoadAssetAtPath<RuntimeCitySpawnerSystemConfig>(ConfigPath);
            if (config == null)
                throw new InvalidOperationException($"Dense-city config is missing: '{ConfigPath}'.");

            int seed = unchecked((int)config.RandomSeed);
            string generationHash = ComputeGenerationHash(
                SourceMapScenePath,
                SourceEntityScenePath,
                ConfigPath,
                GeneratorSchema,
                GeneratorSchemaVersion,
                seed);
            string generationId =
                $"dense-city:{OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId}:" +
                generationHash.Substring(0, 16);

            if (!TryCreate(
                    SourceMapScenePath,
                    SourceEntityScenePath,
                    CandidateMapScenePath,
                    CandidateEntityScenePath,
                    generationId,
                    GeneratorSchema,
                    GeneratorSchemaVersion,
                    seed,
                    generationHash,
                    out string error))
            {
                throw new InvalidOperationException(
                    $"Dense-city candidate hierarchy transaction rejected: {error}");
            }

            Debug.Log(
                $"[DenseCityCandidateAuthoringTransaction] result=Created " +
                $"generationId={generationId} generationHash={generationHash} " +
                $"mapCandidate={CandidateMapScenePath} entityCandidate={CandidateEntityScenePath}");
        }

        internal static bool TryCreate(
            string sourceMapScenePath,
            string sourceEntityScenePath,
            string candidateMapScenePath,
            string candidateEntityScenePath,
            string generationId,
            string generatorSchema,
            int generatorSchemaVersion,
            int deterministicSeed,
            string deterministicGenerationHash,
            out string error)
        {
            error = null;
            Scene candidateMapScene = default;
            Scene candidateEntityScene = default;
            bool mapCandidateCreated = false;
            bool entityCandidateCreated = false;

            try
            {
                if (!IsDistinctScenePair(
                        sourceMapScenePath,
                        sourceEntityScenePath,
                        candidateMapScenePath,
                        candidateEntityScenePath))
                {
                    error = "Dense-city candidate transaction requires four distinct scene asset paths.";
                    return false;
                }
                if (!File.Exists(ToPhysicalPath(sourceMapScenePath)) ||
                    !File.Exists(ToPhysicalPath(sourceEntityScenePath)))
                {
                    error = "Dense-city candidate source scene is missing.";
                    return false;
                }
                if (!OperationMapHashRules.IsValidSha256(deterministicGenerationHash))
                {
                    error = "Dense-city candidate generation hash is invalid.";
                    return false;
                }
                if (generatorSchemaVersion <= 0 ||
                    string.IsNullOrWhiteSpace(generationId) ||
                    string.IsNullOrWhiteSpace(generatorSchema))
                {
                    error = "Dense-city candidate generation identity is invalid.";
                    return false;
                }
                if (AssetExists(candidateMapScenePath) ||
                    AssetExists(candidateEntityScenePath))
                {
                    error = "Dense-city candidate scene already exists.";
                    return false;
                }

                string sourceMapHash = ComputeFileHash(sourceMapScenePath);
                string sourceEntityHash = ComputeFileHash(sourceEntityScenePath);
                EnsureAssetFolder(Path.GetDirectoryName(candidateMapScenePath)?.Replace('\\', '/'));
                EnsureAssetFolder(Path.GetDirectoryName(candidateEntityScenePath)?.Replace('\\', '/'));

                if (!AssetDatabase.CopyAsset(sourceMapScenePath, candidateMapScenePath))
                    throw new InvalidOperationException("Dense-city map candidate copy failed.");
                mapCandidateCreated = true;
                if (!AssetDatabase.CopyAsset(sourceEntityScenePath, candidateEntityScenePath))
                    throw new InvalidOperationException("Dense-city entity candidate copy failed.");
                entityCandidateCreated = true;
                AssetDatabase.ImportAsset(
                    candidateMapScenePath,
                    ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.ImportAsset(
                    candidateEntityScenePath,
                    ImportAssetOptions.ForceSynchronousImport);

                RequireIndependentGuid(sourceMapScenePath, candidateMapScenePath);
                RequireIndependentGuid(sourceEntityScenePath, candidateEntityScenePath);
                candidateMapScene =
                    EditorSceneManager.OpenScene(candidateMapScenePath, OpenSceneMode.Additive);
                candidateEntityScene =
                    EditorSceneManager.OpenScene(candidateEntityScenePath, OpenSceneMode.Additive);

                RuntimeCityRAndDEditModeBuilder.ReplaceDenseCitySemanticHierarchy(
                    candidateMapScene,
                    candidateEntityScene,
                    generationId,
                    generatorSchema,
                    generatorSchemaVersion,
                    deterministicSeed,
                    deterministicGenerationHash);
                if (!DenseCitySemanticHierarchyBuilder.TryValidate(
                        candidateMapScene,
                        candidateEntityScene,
                        generationId,
                        out error))
                {
                    throw new InvalidOperationException(error);
                }
                if (!DenseCityBakeReadinessValidator.TryResolveGenerationState(
                        candidateMapScene,
                        candidateEntityScene,
                        out bool generated,
                        out string resolvedGenerationId,
                        out error) ||
                    !generated ||
                    !string.Equals(
                        generationId,
                        resolvedGenerationId,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        error ?? "Dense-city candidate generation state did not resolve.");
                }

                if (!EditorSceneManager.SaveScene(
                        candidateMapScene,
                        candidateMapScenePath,
                        false) ||
                    !EditorSceneManager.SaveScene(
                        candidateEntityScene,
                        candidateEntityScenePath,
                        false))
                {
                    throw new InvalidOperationException("Dense-city candidate scene save failed.");
                }
                EditorSceneManager.CloseScene(candidateEntityScene, true);
                candidateEntityScene = default;
                EditorSceneManager.CloseScene(candidateMapScene, true);
                candidateMapScene = default;

                if (!string.Equals(
                        sourceMapHash,
                        ComputeFileHash(sourceMapScenePath),
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        sourceEntityHash,
                        ComputeFileHash(sourceEntityScenePath),
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Dense-city candidate transaction changed a protected source scene.");
                }

                AssetDatabase.SaveAssets();
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                if (candidateEntityScene.IsValid() && candidateEntityScene.isLoaded)
                    EditorSceneManager.CloseScene(candidateEntityScene, true);
                if (candidateMapScene.IsValid() && candidateMapScene.isLoaded)
                    EditorSceneManager.CloseScene(candidateMapScene, true);
                if (entityCandidateCreated)
                    AssetDatabase.DeleteAsset(candidateEntityScenePath);
                if (mapCandidateCreated)
                    AssetDatabase.DeleteAsset(candidateMapScenePath);
                return false;
            }
        }

        internal static string ComputeGenerationHash(
            string sourceMapScenePath,
            string sourceEntityScenePath,
            string configPath,
            string generatorSchema,
            int generatorSchemaVersion,
            int deterministicSeed)
        {
            string input =
                ComputeFileHash(sourceMapScenePath) + "\n" +
                ComputeFileHash(sourceEntityScenePath) + "\n" +
                ComputeFileHash(configPath) + "\n" +
                generatorSchema + "\n" +
                generatorSchemaVersion.ToString(CultureInfo.InvariantCulture) + "\n" +
                deterministicSeed.ToString(CultureInfo.InvariantCulture);
            using SHA256 sha = SHA256.Create();
            return ToLowerHex(sha.ComputeHash(Encoding.UTF8.GetBytes(input)));
        }

        private static bool IsDistinctScenePair(params string[] paths)
        {
            var unique = new System.Collections.Generic.HashSet<string>(
                StringComparer.Ordinal);
            for (int index = 0; index < paths.Length; index++)
            {
                string path = paths[index];
                if (string.IsNullOrWhiteSpace(path) ||
                    !path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) ||
                    !unique.Add(path))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool AssetExists(string assetPath) =>
            AssetDatabase.LoadAssetAtPath<SceneAsset>(assetPath) != null ||
            File.Exists(ToPhysicalPath(assetPath));

        private static void RequireIndependentGuid(string sourcePath, string candidatePath)
        {
            string sourceGuid = AssetDatabase.AssetPathToGUID(sourcePath);
            string candidateGuid = AssetDatabase.AssetPathToGUID(candidatePath);
            if (string.IsNullOrEmpty(sourceGuid) ||
                string.IsNullOrEmpty(candidateGuid) ||
                string.Equals(sourceGuid, candidateGuid, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Dense-city candidate did not receive an independent GUID: '{candidatePath}'.");
            }
        }

        private static string ComputeFileHash(string assetPath)
        {
            string physicalPath = ToPhysicalPath(assetPath);
            if (!File.Exists(physicalPath))
                throw new FileNotFoundException("Dense-city transaction input is missing.", assetPath);
            using SHA256 sha = SHA256.Create();
            using FileStream stream = File.OpenRead(physicalPath);
            return ToLowerHex(sha.ComputeHash(stream));
        }

        private static string ToLowerHex(byte[] bytes) =>
            BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();

        private static string ToPhysicalPath(string assetPath) =>
            Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(Application.dataPath) ?? string.Empty,
                assetPath));

        private static void EnsureAssetFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureAssetFolder(parent);
            string name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
                throw new InvalidOperationException($"Invalid asset folder path: '{path}'.");
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
