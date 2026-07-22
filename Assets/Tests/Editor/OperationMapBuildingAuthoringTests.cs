using Game.Authoring;
using Game.Components;
using Game.Configs;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class OperationMapBuildingAuthoringTests
{
    private const string GeneratedStableId =
        "densecity.0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    public static void RunGeneratedIdentityValidation()
    {
        var authoringTests = new OperationMapBuildingAuthoringTests();
        var recordTests = new DenseCityGenerationRecordsTests();
        authoringTests.TryValidate_RequiresStableIdentityAndDefinition();
        authoringTests.TryValidate_AcceptsGeneratedStableIdentityWithoutGlobalObjectId();
        authoringTests.ConfigureGeneratedForEditor_AssignsCompleteValidatedOwnership();
        authoringTests.TryValidate_RejectsMixedAuthoredAndGeneratedIdentities();
        recordTests.RecordIdentity_CreatesDeterministicBoundedGeneratedStableId();
        Debug.Log("[OperationMapGeneratedBuildingIdentityValidation] result=Passed tests=5");
    }

    [Test]
    public void ConfigureGeneratedForEditor_AssignsCompleteValidatedOwnership()
    {
        var owner = new GameObject("GeneratedBuildingOwner");
        var intactVisual = new GameObject("IntactVisual");
        var destroyedVisual = new GameObject("DestroyedVisual");
        var definitionOwner = new GameObject("BuildingDefinition");
        try
        {
            intactVisual.transform.SetParent(owner.transform, false);
            destroyedVisual.transform.SetParent(owner.transform, false);
            var authoring = owner.AddComponent<OperationMapBuildingAuthoring>();
            BuildingDefinitionAuthoring definition =
                definitionOwner.AddComponent<BuildingDefinitionAuthoring>();

            authoring.ConfigureGeneratedForEditor(
                "opmap.skirmish.building_test_01",
                GeneratedStableId,
                17,
                3,
                new Vector2Int(23, 41),
                definition,
                intactVisual,
                destroyedVisual);

            Assert.That(authoring.TryValidate(out string error), Is.True, error);
            Assert.That(authoring.StableId, Is.EqualTo(GeneratedStableId));
            Assert.That(authoring.SourceGlobalObjectId, Is.Empty);
            Assert.That(authoring.PlacementIndex, Is.EqualTo(17));
            Assert.That(authoring.FactionId, Is.EqualTo(3));
            Assert.That(authoring.OriginCell, Is.EqualTo(new Vector2Int(23, 41)));
            Assert.That(authoring.Definition, Is.SameAs(definition));
            Assert.That(authoring.IntactVisualRoot, Is.SameAs(intactVisual));
            Assert.That(authoring.DestroyedVisualRoot, Is.SameAs(destroyedVisual));
        }
        finally
        {
            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(definitionOwner);
        }
    }

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
            Assert.That(authoring.StableId, Is.EqualTo(authoring.SourceGlobalObjectId));
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
    public void TryValidate_RejectsMixedAuthoredAndGeneratedIdentities()
    {
        using var fixture = new BuildingAuthoringFixture();
        var serialized = new SerializedObject(fixture.Authoring);
        serialized.FindProperty("stableId").stringValue = GeneratedStableId;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(fixture.Authoring.TryValidate(out string error), Is.False);
        StringAssert.Contains("Exactly one", error);
    }

    [Test]
    public void TryValidate_AcceptsGeneratedStableIdentityWithoutGlobalObjectId()
    {
        var owner = new GameObject("GeneratedBuildingOwner");
        var intactVisual = new GameObject("IntactVisual");
        var definitionOwner = new GameObject("BuildingDefinition");
        try
        {
            intactVisual.transform.SetParent(owner.transform, false);
            var authoring = owner.AddComponent<OperationMapBuildingAuthoring>();
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty("operationMapId").stringValue = "opmap.skirmish.building_test_01";
            serialized.FindProperty("stableId").stringValue = GeneratedStableId;
            serialized.FindProperty("sourceGlobalObjectId").stringValue = string.Empty;
            serialized.FindProperty("placementIndex").intValue = 9;
            serialized.FindProperty("definition").objectReferenceValue =
                definitionOwner.AddComponent<BuildingDefinitionAuthoring>();
            serialized.FindProperty("intactVisualRoot").objectReferenceValue = intactVisual;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(authoring.TryValidate(out string error), Is.True, error);
            Assert.That(authoring.StableId, Is.EqualTo(GeneratedStableId));
            Assert.That(authoring.SourceGlobalObjectId, Is.Empty);
            Assert.That(OperationMapIdentityRules.IsValidGeneratedStableId(authoring.StableId), Is.True);
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
