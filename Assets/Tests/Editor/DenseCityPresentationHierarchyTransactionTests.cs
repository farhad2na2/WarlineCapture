using System;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DenseCityPresentationHierarchyTransactionTests
{
    private const string TempRoot = "Assets/Tests/Editor/DenseCityPresentationHierarchyTransactionTemp";
    private const string MapScenePath = TempRoot + "/map.unity";
    private const string EntityScenePath = TempRoot + "/entity.unity";
    private const string PrefabGuid = "0123456789abcdef0123456789abcdef";
    private const string MaterialGuid = "abcdef0123456789abcdef0123456789";
    private const string Hash =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [SetUp]
    public void SetUp()
    {
        AssetDatabase.DeleteAsset(TempRoot);
        EnsureFolder(TempRoot);
    }

    [TearDown]
    public void TearDown()
    {
        AssetDatabase.DeleteAsset(TempRoot);
    }

    [Test]
    public void TryPlaceRenderOnlyPresentation_CreatesUnderSemanticParentAndCommits()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            DenseCityPresentationHierarchyContext hierarchy = CreateHierarchy(mapScene, entityScene);
            using var transactions = new DenseCityGenerationTransactionContext(1, 1, 1);

            Assert.That(
                transactions.TryPlaceRenderOnlyPresentation(
                    0,
                    hierarchy,
                    sequence => CreatePresentation(sequence, Matrix4x4.identity),
                    parent => CreateChild(parent, "Prop", Vector3.zero)),
                Is.True);
            transactions.Seal();

            Assert.That(transactions.Records.Presentations, Has.Count.EqualTo(1));
            Assert.That(
                hierarchy.ResolveIndependentParent(DenseCityPresentationCategory.Prop).childCount,
                Is.EqualTo(1));
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    [Test]
    public void TryPlaceRenderOnlyPresentation_WrongParentRollsBackRecordAndObject()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            DenseCityPresentationHierarchyContext hierarchy = CreateHierarchy(mapScene, entityScene);
            using var transactions = new DenseCityGenerationTransactionContext(1, 1, 1);
            Transform wrongParent = hierarchy.ResolveIndependentParent(
                DenseCityPresentationCategory.Vegetation);

            Assert.That(
                () => transactions.TryPlaceRenderOnlyPresentation(
                    0,
                    hierarchy,
                    sequence => CreatePresentation(sequence, Matrix4x4.identity),
                    _ => CreateChild(wrongParent, "MisplacedProp", Vector3.zero)),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("directly under"));
            transactions.Seal();
            Assert.That(transactions.Records.Presentations, Is.Empty);
            Assert.That(wrongParent.childCount, Is.Zero);
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    [Test]
    public void TryPlaceRenderOnlyPresentation_TransformDriftRollsBackRecordAndObject()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            DenseCityPresentationHierarchyContext hierarchy = CreateHierarchy(mapScene, entityScene);
            using var transactions = new DenseCityGenerationTransactionContext(1, 1, 1);
            Transform propParent = hierarchy.ResolveIndependentParent(DenseCityPresentationCategory.Prop);

            Assert.That(
                () => transactions.TryPlaceRenderOnlyPresentation(
                    0,
                    hierarchy,
                    sequence => CreatePresentation(sequence, Matrix4x4.identity),
                    parent => CreateChild(parent, "DriftedProp", Vector3.right)),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("transform drift"));
            transactions.Seal();
            Assert.That(transactions.Records.Presentations, Is.Empty);
            Assert.That(propParent.childCount, Is.Zero);
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    private static Transform CreateChild(Transform parent, string name, Vector3 localPosition)
    {
        var owner = new GameObject(name);
        owner.transform.SetParent(parent, false);
        owner.transform.localPosition = localPosition;
        return owner.transform;
    }

    private static DenseCityPresentationHierarchyContext CreateHierarchy(
        Scene mapScene,
        Scene entityScene)
    {
        var roots = DenseCitySemanticHierarchyBuilder.Create(
            mapScene,
            entityScene,
            "dense-city:test:42",
            "dense-city-v1",
            1,
            42,
            Hash);
        return DenseCityPresentationHierarchyContext.Create(roots.EntityPresentationSource);
    }

    private static DenseCityPresentationBakeRecord CreatePresentation(
        int sequence,
        Matrix4x4 worldMatrix) =>
        DenseCityRenderOnlyPresentationRecordFactory.Create(
            new DenseCityRenderOnlyPresentationRecordInput(
                "dense-city-v1",
                42,
                0,
                sequence,
                "prop-visual",
                DenseCityPresentationCategory.Prop,
                PrefabGuid,
                123,
                new[] { MaterialGuid },
                worldMatrix,
                true,
                true,
                1));

    private static (Scene MapScene, Scene EntityScene) CreateScenePair()
    {
        Scene mapScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Assert.That(EditorSceneManager.SaveScene(mapScene, MapScenePath), Is.True);
        Scene entityScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        Assert.That(EditorSceneManager.SaveScene(entityScene, EntityScenePath), Is.True);
        return (mapScene, entityScene);
    }

    private static void EnsureFolder(string path)
    {
        string[] segments = path.Split('/');
        string current = segments[0];
        for (int index = 1; index < segments.Length; index++)
        {
            string next = current + "/" + segments[index];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, segments[index]);
            current = next;
        }
    }
}
