using System;
using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DenseCityPresentationHierarchyContextTests
{
    private const string TempRoot = "Assets/Tests/Editor/DenseCityPresentationHierarchyContextTemp";
    private const string MapScenePath = TempRoot + "/map.unity";
    private const string EntityScenePath = TempRoot + "/entity.unity";
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
    public void ResolveIndependentParent_RoutesEveryClosedCategoryToItsExplicitBucket()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            var roots = DenseCitySemanticHierarchyBuilder.Create(
                mapScene,
                entityScene,
                "dense-city:test:42",
                "dense-city-v1",
                1,
                42,
                Hash);
            DenseCityPresentationHierarchyContext context =
                DenseCityPresentationHierarchyContext.Create(roots.EntityPresentationSource);

            Assert.That(
                context.ResolveIndependentParent(
                    DenseCityPresentationCategory.GameplayBuildingIntact,
                    GeneratedCityBuildingRole.House).name,
                Is.EqualTo("Buildings"));
            Assert.That(
                context.ResolveIndependentParent(
                    DenseCityPresentationCategory.GameplayBuildingDestroyed,
                    GeneratedCityBuildingRole.Civic).name,
                Is.EqualTo("CivicAndMarket"));
            Assert.That(
                context.ResolveIndependentParent(DenseCityPresentationCategory.Infrastructure).name,
                Is.EqualTo("Infrastructure"));
            Assert.That(
                context.ResolveIndependentParent(DenseCityPresentationCategory.Vegetation).name,
                Is.EqualTo("Vegetation"));
            Assert.That(
                context.ResolveIndependentParent(DenseCityPresentationCategory.Prop).name,
                Is.EqualTo("Props"));
            Assert.That(
                context.ResolveIndependentParent(DenseCityPresentationCategory.Horizon).name,
                Is.EqualTo("Horizon"));
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    [Test]
    public void GeneratedFeatureKinds_RouteToTheirExplicitPresentationOwner()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            var roots = DenseCitySemanticHierarchyBuilder.Create(
                mapScene,
                entityScene,
                "dense-city:test:42",
                "dense-city-v1",
                1,
                42,
                Hash);
            DenseCityPresentationHierarchyContext context =
                DenseCityPresentationHierarchyContext.Create(roots.EntityPresentationSource);
            var expected = new[]
            {
                ("building-intact", DenseCityPresentationCategory.GameplayBuildingIntact,
                    GeneratedCityBuildingRole.House, "Buildings"),
                ("building-destroyed", DenseCityPresentationCategory.GameplayBuildingDestroyed,
                    GeneratedCityBuildingRole.House, "Buildings"),
                ("civic-building-intact", DenseCityPresentationCategory.GameplayBuildingIntact,
                    GeneratedCityBuildingRole.Civic, "CivicAndMarket"),
                ("civic-building-destroyed", DenseCityPresentationCategory.GameplayBuildingDestroyed,
                    GeneratedCityBuildingRole.Civic, "CivicAndMarket"),
                ("road-visual", DenseCityPresentationCategory.Infrastructure,
                    GeneratedCityBuildingRole.None, "Infrastructure"),
                ("road-terrain-patch-visual", DenseCityPresentationCategory.Infrastructure,
                    GeneratedCityBuildingRole.None, "Infrastructure"),
                ("canal-bed-visual", DenseCityPresentationCategory.Infrastructure,
                    GeneratedCityBuildingRole.None, "Infrastructure"),
                ("canal-water-visual", DenseCityPresentationCategory.Infrastructure,
                    GeneratedCityBuildingRole.None, "Infrastructure"),
                ("canal-bridge-visual", DenseCityPresentationCategory.Infrastructure,
                    GeneratedCityBuildingRole.None, "Infrastructure"),
                ("canal-bank-base-visual", DenseCityPresentationCategory.Infrastructure,
                    GeneratedCityBuildingRole.None, "Infrastructure"),
                ("canal-bank-visual", DenseCityPresentationCategory.Infrastructure,
                    GeneratedCityBuildingRole.None, "Infrastructure"),
                ("canal-park-base-visual", DenseCityPresentationCategory.Infrastructure,
                    GeneratedCityBuildingRole.None, "Infrastructure"),
                ("canal-park-visual", DenseCityPresentationCategory.Infrastructure,
                    GeneratedCityBuildingRole.None, "Infrastructure"),
                ("canal-tree-visual", DenseCityPresentationCategory.Vegetation,
                    GeneratedCityBuildingRole.None, "Vegetation"),
                ("canal-bush-visual", DenseCityPresentationCategory.Vegetation,
                    GeneratedCityBuildingRole.None, "Vegetation"),
                ("canal-light-visual", DenseCityPresentationCategory.Infrastructure,
                    GeneratedCityBuildingRole.None, "Infrastructure"),
                ("civic-road-visual", DenseCityPresentationCategory.Infrastructure,
                    GeneratedCityBuildingRole.None, "Infrastructure"),
                ("civic-road-terrain-patch-visual", DenseCityPresentationCategory.Infrastructure,
                    GeneratedCityBuildingRole.None, "Infrastructure"),
                ("civic-fountain-visual", DenseCityPresentationCategory.Prop,
                    GeneratedCityBuildingRole.None, "Props"),
                ("horizon-mountain-visual", DenseCityPresentationCategory.Horizon,
                    GeneratedCityBuildingRole.None, "Horizon"),
                ("boulevard-median-tree-visual", DenseCityPresentationCategory.Vegetation,
                    GeneratedCityBuildingRole.None, "Vegetation"),
                ("boulevard-median-light-visual", DenseCityPresentationCategory.Infrastructure,
                    GeneratedCityBuildingRole.None, "Infrastructure"),
                ("sidewalk-street-light-visual", DenseCityPresentationCategory.Infrastructure,
                    GeneratedCityBuildingRole.None, "Infrastructure"),
                ("free-ground-grass-visual", DenseCityPresentationCategory.Vegetation,
                    GeneratedCityBuildingRole.None, "Vegetation"),
                ("main-street-bush-visual", DenseCityPresentationCategory.Vegetation,
                    GeneratedCityBuildingRole.None, "Vegetation"),
                ("power-pole-visual", DenseCityPresentationCategory.Infrastructure,
                    GeneratedCityBuildingRole.None, "Infrastructure"),
                ("power-line-visual", DenseCityPresentationCategory.Infrastructure,
                    GeneratedCityBuildingRole.None, "Infrastructure"),
                ("courtyard-wall-visual", DenseCityPresentationCategory.Infrastructure,
                    GeneratedCityBuildingRole.None, "Infrastructure"),
                ("courtyard-pillar-visual", DenseCityPresentationCategory.Infrastructure,
                    GeneratedCityBuildingRole.None, "Infrastructure"),
                ("courtyard-well-visual", DenseCityPresentationCategory.Prop,
                    GeneratedCityBuildingRole.None, "Props"),
                ("courtyard-bush-visual", DenseCityPresentationCategory.Vegetation,
                    GeneratedCityBuildingRole.None, "Vegetation"),
                ("street-prop-visual", DenseCityPresentationCategory.Prop,
                    GeneratedCityBuildingRole.None, "Props"),
                ("urban-tree-visual", DenseCityPresentationCategory.Vegetation,
                    GeneratedCityBuildingRole.None, "Vegetation"),
                ("urban-rock-visual", DenseCityPresentationCategory.Prop,
                    GeneratedCityBuildingRole.None, "Props"),
                ("open-ground-visual", DenseCityPresentationCategory.Infrastructure,
                    GeneratedCityBuildingRole.None, "Infrastructure")
            };

            foreach ((string kind, DenseCityPresentationCategory category,
                         GeneratedCityBuildingRole role, string expectedParent) in expected)
            {
                Assert.That(
                    context.ResolveIndependentParent(category, role).name,
                    Is.EqualTo(expectedParent),
                    $"Dense-city feature '{kind}' has the wrong presentation owner.");
            }

            Transform buildingParent = context.ResolveIndependentParent(
                DenseCityPresentationCategory.GameplayBuildingIntact,
                GeneratedCityBuildingRole.House);
            var building = new GameObject("BuildingAttachmentOwner");
            building.transform.SetParent(buildingParent, false);
            var attachment = new GameObject("BuildingAttachment");
            attachment.transform.SetParent(building.transform, false);
            Assert.That(
                context.RequireAttachmentRoot(
                    DenseCityPresentationCategory.BuildingAttachmentIntact,
                    building.transform,
                    attachment.transform),
                Is.SameAs(attachment.transform),
                "Dense-city feature 'building-attachment-intact' must remain beneath its building owner.");
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    [Test]
    public void ResolveIndependentParent_RejectsBuildingAttachments()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            var roots = DenseCitySemanticHierarchyBuilder.Create(
                mapScene,
                entityScene,
                "dense-city:test:42",
                "dense-city-v1",
                1,
                42,
                Hash);
            DenseCityPresentationHierarchyContext context =
                DenseCityPresentationHierarchyContext.Create(roots.EntityPresentationSource);

            Assert.That(
                () => context.ResolveIndependentParent(
                    DenseCityPresentationCategory.BuildingAttachmentIntact),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("declared building"));
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    [Test]
    public void Create_RejectsNonIdentitySemanticBucket()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            var roots = DenseCitySemanticHierarchyBuilder.Create(
                mapScene,
                entityScene,
                "dense-city:test:42",
                "dense-city-v1",
                1,
                42,
                Hash);
            roots.EntityPresentationSource.transform.Find("RenderOnly/Props").localPosition = Vector3.right;

            Assert.That(
                () => DenseCityPresentationHierarchyContext.Create(roots.EntityPresentationSource),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("identity transforms"));
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    [Test]
    public void RequireIndependentRoot_AcceptsOnlyDirectExplicitCategoryOwnership()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            var roots = DenseCitySemanticHierarchyBuilder.Create(
                mapScene,
                entityScene,
                "dense-city:test:42",
                "dense-city-v1",
                1,
                42,
                Hash);
            DenseCityPresentationHierarchyContext context =
                DenseCityPresentationHierarchyContext.Create(roots.EntityPresentationSource);
            Transform propParent = context.ResolveIndependentParent(DenseCityPresentationCategory.Prop);
            var prop = new GameObject("Prop");
            prop.transform.SetParent(propParent, false);
            var nested = new GameObject("Nested");
            nested.transform.SetParent(prop.transform, false);

            Assert.That(
                context.RequireIndependentRoot(DenseCityPresentationCategory.Prop, prop.transform),
                Is.SameAs(prop.transform));
            Assert.That(
                () => context.RequireIndependentRoot(
                    DenseCityPresentationCategory.Prop,
                    nested.transform),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("directly under"));
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    [Test]
    public void RequireAttachmentRoot_RejectsIndependentRenderOnlyOwnership()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            var roots = DenseCitySemanticHierarchyBuilder.Create(
                mapScene,
                entityScene,
                "dense-city:test:42",
                "dense-city-v1",
                1,
                42,
                Hash);
            DenseCityPresentationHierarchyContext context =
                DenseCityPresentationHierarchyContext.Create(roots.EntityPresentationSource);
            Transform buildingParent = context.ResolveIndependentParent(
                DenseCityPresentationCategory.GameplayBuildingIntact,
                GeneratedCityBuildingRole.House);
            var building = new GameObject("Building");
            building.transform.SetParent(buildingParent, false);
            var attachment = new GameObject("Attachment");
            attachment.transform.SetParent(building.transform, false);

            Assert.That(
                context.RequireAttachmentRoot(
                    DenseCityPresentationCategory.BuildingAttachmentIntact,
                    building.transform,
                    attachment.transform),
                Is.SameAs(attachment.transform));

            attachment.transform.SetParent(
                context.ResolveIndependentParent(DenseCityPresentationCategory.Prop),
                false);
            Assert.That(
                () => context.RequireAttachmentRoot(
                    DenseCityPresentationCategory.BuildingAttachmentIntact,
                    building.transform,
                    attachment.transform),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("declared visual-state owner"));
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

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
