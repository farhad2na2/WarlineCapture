using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class DenseCityCanalPresentationMaterialTests
{
    private const string CandidateWaterMaterialPath =
        "Assets/Game/GeneratedOperationMaps/DenseCity/" +
        "opmap.skirmish.desert_base_01/Candidate/SharedMaterials/" +
        "Synty_Generic_Basic_7ebfd405eb8711d4bb584f620ef5043a.mat";

    public static void RunFocusedValidation()
    {
        var suite = new DenseCityCanalPresentationMaterialTests();
        suite.Override_IsLimitedToTheCanonicalCanalWaterSource();
        suite.CandidateWaterMaterial_IsClearlyBlueAndRetainsTransparentPolicy();
        Debug.Log("[DenseCityCanalPresentationMaterialValidation] result=Passed tests=2");
    }

    [Test]
    public void Override_IsLimitedToTheCanonicalCanalWaterSource()
    {
        Assert.That(
            DenseCityCandidateAuthoringTransaction.TryGetCandidatePresentationColorOverride(
                DenseCityCandidateAuthoringTransaction.CanalWaterSourceMaterialPath,
                out Color color),
            Is.True);
        AssertColor(
            color,
            DenseCityCandidateAuthoringTransaction.CandidateCanalWaterBaseColor);
        Assert.That(
            DenseCityCandidateAuthoringTransaction.TryGetCandidatePresentationColorOverride(
                "Assets/Other/Material.mat",
                out _),
            Is.False);
    }

    [Test]
    public void CandidateWaterMaterial_IsClearlyBlueAndRetainsTransparentPolicy()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(CandidateWaterMaterialPath);
        Assert.That(material, Is.Not.Null, CandidateWaterMaterialPath);
        Assert.That(material.HasProperty("_BaseColor"), Is.True);

        Color color = material.GetColor("_BaseColor");
        AssertColor(
            color,
            DenseCityCandidateAuthoringTransaction.CandidateCanalWaterBaseColor);
        Assert.That(color.b, Is.GreaterThanOrEqualTo(0.5f));
        Assert.That(color.b, Is.GreaterThan(color.g * 2f));
        Assert.That(color.b, Is.GreaterThan(color.r * 8f));
        Assert.That(material.GetFloat("_Surface"), Is.EqualTo(1f));
        Assert.That(material.renderQueue, Is.EqualTo(3000));
        Assert.That(material.GetTag("RenderType", false), Is.EqualTo("Transparent"));
    }

    private static void AssertColor(Color actual, Color expected)
    {
        Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.0001f));
        Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.0001f));
        Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.0001f));
        Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.0001f));
    }
}
