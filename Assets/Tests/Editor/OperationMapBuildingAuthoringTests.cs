using Game.Authoring;
using Game.Components;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class OperationMapBuildingAuthoringTests
{
    [Test]
    public void TryValidate_RequiresStableIdentityAndDefinition()
    {
        var owner = new GameObject("BuildingOwner");
        var intactVisual = new GameObject("IntactVisual");
        var definitionOwner = new GameObject("BuildingDefinition");
        try
        {
            intactVisual.transform.SetParent(owner.transform, false);
            var authoring = owner.AddComponent<OperationMapBuildingAuthoring>();
            var definition = definitionOwner.AddComponent<BuildingDefinitionAuthoring>();
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty("operationMapId").stringValue = "opmap.skirmish.building_test_01";
            serialized.FindProperty("sourceGlobalObjectId").stringValue =
                "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-123-0";
            serialized.FindProperty("placementIndex").intValue = 4;
            serialized.FindProperty("factionId").intValue = 2;
            serialized.FindProperty("originCell").vector2IntValue = new Vector2Int(12, 18);
            serialized.FindProperty("definition").objectReferenceValue = definition;
            serialized.FindProperty("intactVisualRoot").objectReferenceValue = intactVisual;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(authoring.TryValidate(out string error), Is.True, error);
            Assert.That(authoring.PlacementIndex, Is.EqualTo(4));
            Assert.That(authoring.FactionId, Is.EqualTo(2));
            Assert.That(authoring.OriginCell, Is.EqualTo(new Vector2Int(12, 18)));
            Assert.That(
                authoring.BlockerPolicy,
                Is.EqualTo(OperationMapBuildingBlockerPolicy.RubbleRemainsBlocked));
        }
        finally
        {
            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(definitionOwner);
        }
    }

    [Test]
    public void TryValidate_RejectsUnimplementedBlockerPolicy()
    {
        var owner = new GameObject("BuildingOwner");
        var intactVisual = new GameObject("IntactVisual");
        var definitionOwner = new GameObject("BuildingDefinition");
        try
        {
            intactVisual.transform.SetParent(owner.transform, false);
            var authoring = owner.AddComponent<OperationMapBuildingAuthoring>();
            var definition = definitionOwner.AddComponent<BuildingDefinitionAuthoring>();
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty("operationMapId").stringValue = "opmap.skirmish.building_test_01";
            serialized.FindProperty("sourceGlobalObjectId").stringValue =
                "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-123-0";
            serialized.FindProperty("placementIndex").intValue = 4;
            serialized.FindProperty("blockerPolicy").enumValueIndex =
                (int)OperationMapBuildingBlockerPolicy.Unknown;
            serialized.FindProperty("definition").objectReferenceValue = definition;
            serialized.FindProperty("intactVisualRoot").objectReferenceValue = intactVisual;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(authoring.TryValidate(out string error), Is.False);
            StringAssert.Contains("blocker policy", error);
        }
        finally
        {
            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(definitionOwner);
        }
    }

    [Test]
    public void TryValidate_RejectsRendererOutsideDeclaredVisualStates()
    {
        using var fixture = new BuildingAuthoringFixture();
        new GameObject("OrphanRenderer", typeof(MeshRenderer))
            .transform.SetParent(fixture.Owner.transform, false);

        Assert.That(fixture.Authoring.TryValidate(out string error), Is.False);
        StringAssert.Contains("exactly one declared building visual state", error);
    }

    [Test]
    public void TryValidate_RejectsIndependentRenderOnlyIdentityInsideVisualState()
    {
        using var fixture = new BuildingAuthoringFixture();
        GameObject prop = new("IndependentProp", typeof(MeshRenderer));
        prop.transform.SetParent(fixture.IntactVisual.transform, false);
        OperationMapEntityPresentationIdentityAuthoring identity =
            prop.AddComponent<OperationMapEntityPresentationIdentityAuthoring>();
        identity.ConfigureForEditor(
            "opmap.skirmish.building_test_01",
            "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-123-0",
            OperationMapEntityPresentationRole.RenderOnly,
            OperationMapEntityPresentationIdentityAuthoring.NoPlacementIndex);

        Assert.That(fixture.Authoring.TryValidate(out string error), Is.False);
        StringAssert.Contains("independent or mismatched presentation ownership", error);
    }

    private sealed class BuildingAuthoringFixture : System.IDisposable
    {
        public BuildingAuthoringFixture()
        {
            Owner = new GameObject("BuildingOwner");
            IntactVisual = new GameObject("IntactVisual");
            IntactVisual.transform.SetParent(Owner.transform, false);
            DefinitionOwner = new GameObject("BuildingDefinition");
            Authoring = Owner.AddComponent<OperationMapBuildingAuthoring>();
            BuildingDefinitionAuthoring definition =
                DefinitionOwner.AddComponent<BuildingDefinitionAuthoring>();
            var serialized = new SerializedObject(Authoring);
            serialized.FindProperty("operationMapId").stringValue = "opmap.skirmish.building_test_01";
            serialized.FindProperty("sourceGlobalObjectId").stringValue =
                "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-123-0";
            serialized.FindProperty("placementIndex").intValue = 4;
            serialized.FindProperty("definition").objectReferenceValue = definition;
            serialized.FindProperty("intactVisualRoot").objectReferenceValue = IntactVisual;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public GameObject Owner { get; }
        public GameObject IntactVisual { get; }
        public GameObject DefinitionOwner { get; }
        public OperationMapBuildingAuthoring Authoring { get; }

        public void Dispose()
        {
            Object.DestroyImmediate(Owner);
            Object.DestroyImmediate(DefinitionOwner);
        }
    }
}
