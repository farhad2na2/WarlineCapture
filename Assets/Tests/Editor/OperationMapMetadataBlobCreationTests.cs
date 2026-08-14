using System.Reflection;
using Game.Components;
using Game.Configs;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class OperationMapMetadataBlobCreationTests
{
    private const string HashA = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
    private const string HashB = "123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0";
    private const string HashC = "23456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef01";

    [Test]
    public void ValidDefinition_CreatesExactPersistentMetadataBlob()
    {
        OperationMapDefinition definition = CreateValidDefinition();
        BlobAssetReference<OperationMapBlob> blob = default;
        try
        {
            Assert.That(definition.TryCreatePersistentMetadataBlob(out blob, out string error), Is.True, error);
            Assert.That(blob.IsCreated, Is.True);

            ref OperationMapBlob metadata = ref blob.Value;
            Assert.That(metadata.OperationMapId.ToString(), Is.EqualTo("opmap.skirmish.desert_base_01"));
            Assert.That(metadata.SourceOperationMapId.ToString(),
                Is.EqualTo("opmap.skirmish.desert_base_01"));
            Assert.That(metadata.SourceIdentityHash.ToString(), Is.EqualTo(HashA));
            Assert.That(metadata.SourceContentHash.ToString(), Is.EqualTo(HashB));
            Assert.That(metadata.ContentHash.ToString(), Is.EqualTo(HashB));
            Assert.That(metadata.GeneratedMetadataHash.ToString(), Is.EqualTo(HashC));
            Assert.That(metadata.SchemaVersion, Is.EqualTo(2));
            Assert.That(metadata.ContentVersion, Is.EqualTo(7));
            Assert.That(metadata.Grid.Dimensions, Is.EqualTo(new int2(200, 200)));
            Assert.That(metadata.Grid.CellSize, Is.EqualTo(1f));
            Assert.That(metadata.Surface.SurfaceCount, Is.EqualTo(40000));
            Assert.That(metadata.Surface.MinimumHeight, Is.EqualTo(-10f));
            Assert.That(metadata.Surface.MaximumHeight, Is.EqualTo(50f));
            Assert.That(metadata.Navigation.GridAuthoringLocalId, Is.EqualTo(123));
            Assert.That(metadata.Navigation.StaticGridBlockerCount, Is.EqualTo(1));
            Assert.That(metadata.Navigation.UsesSurfaceMovementMetadata, Is.EqualTo(1));
            Assert.That(metadata.Navigation.SupportsDynamicBlockers, Is.EqualTo(1));
            Assert.That(metadata.Navigation.SupportsDynamicOccupancy, Is.EqualTo(1));
            Assert.That(metadata.PlanningCameraId.ToString(), Is.EqualTo("camera.skirmish.planning"));
            Assert.That(metadata.BattleCameraId.ToString(), Is.EqualTo("camera.skirmish.battle"));
            Assert.That(metadata.Cameras.Length, Is.EqualTo(2));
            Assert.That(metadata.Anchors.Length, Is.EqualTo(2));
            Assert.That(metadata.Minimap.Id.ToString(), Is.EqualTo("minimap.skirmish.projection"));
            Assert.That(metadata.Minimap.ProjectionSize, Is.EqualTo(new float2(200f, 100f)));

            OperationMapCameraBlob camera = metadata.Cameras[1];
            Quaternion expectedRotation = Quaternion.Euler(45f, 90f, 0f);
            Assert.That(math.distance(camera.Rotation.value, new float4(
                expectedRotation.x,
                expectedRotation.y,
                expectedRotation.z,
                expectedRotation.w)), Is.LessThan(0.0001f));
            Assert.That(camera.ClampToCameraBounds, Is.EqualTo(1));

            OperationMapAnchorBlob anchor = metadata.Anchors[1];
            Assert.That(anchor.Kind, Is.EqualTo(OperationMapAnchorKind.Runway));
            Assert.That(anchor.FactionId, Is.EqualTo(1));
            Assert.That(anchor.LaneIndex, Is.EqualTo(2));
        }
        finally
        {
            if (blob.IsCreated)
                blob.Dispose();
            Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void RepeatedCreation_ProducesEquivalentOrderedMetadata()
    {
        OperationMapDefinition definition = CreateValidDefinition();
        BlobAssetReference<OperationMapBlob> first = default;
        BlobAssetReference<OperationMapBlob> second = default;
        try
        {
            Assert.That(definition.TryCreatePersistentMetadataBlob(out first, out string firstError), Is.True, firstError);
            Assert.That(definition.TryCreatePersistentMetadataBlob(out second, out string secondError), Is.True, secondError);

            ref OperationMapBlob firstValue = ref first.Value;
            ref OperationMapBlob secondValue = ref second.Value;
            Assert.That(secondValue.OperationMapId, Is.EqualTo(firstValue.OperationMapId));
            Assert.That(secondValue.GeneratedMetadataHash, Is.EqualTo(firstValue.GeneratedMetadataHash));
            Assert.That(secondValue.Cameras.Length, Is.EqualTo(firstValue.Cameras.Length));
            Assert.That(secondValue.Anchors.Length, Is.EqualTo(firstValue.Anchors.Length));
            for (int index = 0; index < firstValue.Cameras.Length; index++)
            {
                Assert.That(secondValue.Cameras[index].Id, Is.EqualTo(firstValue.Cameras[index].Id));
                Assert.That(secondValue.Cameras[index].Position, Is.EqualTo(firstValue.Cameras[index].Position));
                Assert.That(secondValue.Cameras[index].Rotation, Is.EqualTo(firstValue.Cameras[index].Rotation));
            }
            for (int index = 0; index < firstValue.Anchors.Length; index++)
            {
                Assert.That(secondValue.Anchors[index].Id, Is.EqualTo(firstValue.Anchors[index].Id));
                Assert.That(secondValue.Anchors[index].Position, Is.EqualTo(firstValue.Anchors[index].Position));
                Assert.That(secondValue.Anchors[index].Rotation, Is.EqualTo(firstValue.Anchors[index].Rotation));
            }
        }
        finally
        {
            if (first.IsCreated)
                first.Dispose();
            if (second.IsCreated)
                second.Dispose();
            Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void InvalidDefinition_FailsWithoutCreatingBlob()
    {
        OperationMapDefinition definition = CreateValidDefinition();
        try
        {
            Set(definition, "contentHash", "invalid");

            Assert.That(definition.TryCreatePersistentMetadataBlob(out BlobAssetReference<OperationMapBlob> blob, out string error), Is.False);
            Assert.That(blob.IsCreated, Is.False);
            StringAssert.Contains("content hash", error);
        }
        finally
        {
            Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void MetadataBlob_DoesNotContainHeavyMapPayloadFields()
    {
        string[] forbiddenTokens =
        {
            "Blocker",
            "Occupancy",
            "Mesh",
            "Texture",
            "Manifest",
            "Renderer",
            "Scene"
        };

        foreach (FieldInfo field in typeof(OperationMapBlob).GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            foreach (string token in forbiddenTokens)
                StringAssert.DoesNotContain(token, field.Name, $"OperationMapBlob.{field.Name}");
        }
    }

    private static OperationMapDefinition CreateValidDefinition()
    {
        OperationMapDefinition definition = ScriptableObject.CreateInstance<OperationMapDefinition>();
        Set(definition, "operationMapId", "opmap.skirmish.desert_base_01");
        Set(definition, "schemaVersion", 2);
        Set(definition, "contentVersion", 7);
        Set(definition, "sourceIdentityHash", HashA);
        Set(definition, "contentHash", HashB);
        Set(definition, "generatedMetadataHash", HashC);
        Set(definition, "bounds", new OperationMapBoundsConfig(
            new Vector3(-100f, -10f, -100f),
            new Vector3(100f, 50f, 100f),
            new Vector3(-90f, -5f, -90f),
            new Vector3(90f, 40f, 90f),
            new Vector3(-80f, 10f, -80f),
            new Vector3(80f, 40f, 80f)));
        Set(definition, "gridMetadata", new OperationMapGridMetadataConfig(
            HashA.Substring(0, 32), HashA, new Vector3(-100f, 0f, -100f), new Vector2Int(200, 200), 1f, 0));
        Set(definition, "surfaceMetadata", new OperationMapSurfaceMetadataConfig(
            HashB.Substring(0, 32), HashB, HashC.Substring(0, 32), 40000, 3, 1, -10f, 50f));
        Set(definition, "navigationMetadata", new OperationMapNavigationMetadataConfig(
            HashA.Substring(0, 32), 123, 1, true, true, true));
        Set(definition, "cameras", new[]
        {
            new OperationMapCameraConfig(
                "camera.skirmish.planning",
                new Vector3(0f, 30f, 0f),
                new Vector3(60f, 0f, 0f),
                true,
                60f,
                30f,
                true),
            new OperationMapCameraConfig(
                "camera.skirmish.battle",
                new Vector3(20f, 25f, 30f),
                new Vector3(45f, 90f, 0f),
                false,
                55f,
                20f,
                true)
        });
        Set(definition, "planningCameraId", "camera.skirmish.planning");
        Set(definition, "battleCameraId", "camera.skirmish.battle");
        Set(definition, "minimap", new OperationMapMinimapConfig(
            "minimap.skirmish.projection",
            new Vector3(-100f, 0f, -50f),
            new Vector2(200f, 100f),
            0f));
        Set(definition, "anchors", new[]
        {
            new OperationMapAnchorConfig(
                "anchor.skirmish.objective.alpha",
                OperationMapAnchorKind.Objective,
                new Vector3(20f, 0f, 30f),
                Vector3.zero,
                5f),
            new OperationMapAnchorConfig(
                "anchor.skirmish.runway.faction_1",
                OperationMapAnchorKind.Runway,
                new Vector3(-20f, 0f, -30f),
                new Vector3(0f, 90f, 0f),
                20f,
                1,
                2)
        });
        return definition;
    }

    private static void Set<T>(OperationMapDefinition definition, string fieldName, T value)
    {
        FieldInfo field = typeof(OperationMapDefinition).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(definition, value);
    }
}
