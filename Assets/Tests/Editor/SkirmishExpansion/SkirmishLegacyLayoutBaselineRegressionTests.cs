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
            Assert.AreEqual(SkirmishLegacyLayoutBaselineContract.CapturedUnityVersion, Application.unityVersion);
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
        public static void RunFocusedValidation()
        {
            try { new SkirmishLegacyLayoutBaselineRegressionTests().CapturedLegacyLayoutsAndAllTwelveSetupHashesRemainExact();
                Debug.Log("[SkirmishLegacyLayoutBaselineRegressionTests] result=Passed catalogs=4 configurations=12 nativeGameplayCertified=0"); }
            catch(Exception error) { Debug.LogError("[SkirmishLegacyLayoutBaselineRegressionTests] result=Failed\n" + error); throw; }
        }
    }
}
