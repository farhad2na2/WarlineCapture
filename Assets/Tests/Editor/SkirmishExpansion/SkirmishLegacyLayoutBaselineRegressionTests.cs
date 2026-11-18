using System;
using System.IO;
using System.Security.Cryptography;
using Game.Configs;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using UnityEngine;
namespace Game.Tests.Editor
{
    public sealed class SkirmishLegacyLayoutBaselineRegressionTests
    {
        [Test] public void CapturedLegacyLayoutsAndAllTwelveSetupHashesRemainExact()
        {
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string artifact = Path.Combine(project, SkirmishLegacyLayoutBaselineContract.ArtifactRepositoryPath);
            using (var hash = SHA256.Create())
                Assert.AreEqual(SkirmishLegacyLayoutBaselineContract.ArtifactSha256,
                    BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(artifact))).Replace("-", "").ToLowerInvariant(), "Immutable capture artifact changed.");
            // The hashes below are compared directly, so the Editor that captured them (CapturedUnityVersion, provenance
            // only) need not be the running one. Guard instead that the run uses the project's pinned Editor
            // (ProjectSettings/ProjectVersion.txt), not a stray install (the 6000.5.2f1 constant failed on 6000.5.8f1).
            Assert.AreEqual(ProjectEditorVersion(project), Application.unityVersion, "Run with the Editor pinned in ProjectSettings/ProjectVersion.txt.");
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoad(project, out var matrix, out string error), error);
            var catalog = SkirmishExpansionCatalogConfig.Load(); Assert.IsNotNull(catalog);
            var rows = SkirmishLegacyLayoutBaselineContract.ReadRows(); Assert.AreEqual(12, rows.Length);
            foreach (var expected in rows)
            {
                Assert.IsTrue(catalog.TryGetDefinition(expected.CatalogId, out var definition));
                Assert.AreEqual(expected.LayoutSha256, SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(definition.MapLayout));
                Assert.IsTrue(Enum.TryParse(expected.Size, out SkirmishSizeId size));
                Assert.IsTrue(SkirmishSetupCompiler.TryCompile(definition, SkirmishDifficultyId.Regular, size, expected.Seed,
                    new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds }, matrix,
                    out var setup, out var reasons), string.Join(";", reasons));
                Assert.AreEqual(expected.SetupHash, setup.SetupHash, expected.CatalogId + "/" + expected.Size);
                Assert.AreEqual(expected.PolicylessHash, SkirmishSetupCompiler.ComputePolicylessHash(setup, expected.Seed), expected.CatalogId + "/" + expected.Size + "/policyless");
            }
        }
        public static string ProjectEditorVersion(string project)
        {
            foreach (string line in File.ReadAllLines(Path.Combine(project, "ProjectSettings", "ProjectVersion.txt")))
                if (line.StartsWith("m_EditorVersion:", StringComparison.Ordinal))
                    return line.Substring("m_EditorVersion:".Length).Trim();
            throw new InvalidDataException("ProjectSettings/ProjectVersion.txt has no m_EditorVersion line.");
        }
        public static void RunFocusedValidation()
        {
            try { new SkirmishLegacyLayoutBaselineRegressionTests().CapturedLegacyLayoutsAndAllTwelveSetupHashesRemainExact();
                Debug.Log("[SkirmishLegacyLayoutBaselineRegressionTests] result=Passed catalogs=4 configurations=12 nativeGameplayCertified=0"); }
            catch(Exception error) { Debug.LogError("[SkirmishLegacyLayoutBaselineRegressionTests] result=Failed\n" + error); throw; }
        }
    }
}
