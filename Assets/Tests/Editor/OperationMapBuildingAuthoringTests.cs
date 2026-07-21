using Game.Authoring;
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
        }
        finally
        {
            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(definitionOwner);
        }
    }
}
