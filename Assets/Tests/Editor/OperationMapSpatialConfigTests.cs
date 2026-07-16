using System;
using System.Reflection;
using Game.Components;
using Game.Configs;
using NUnit.Framework;
using UnityEngine;

public sealed class OperationMapSpatialConfigTests
{
    private const string ValidSha256 = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    private static readonly OperationMapBoundsConfig ValidBounds = new(
        new Vector3(-100f, -10f, -100f),
        new Vector3(100f, 50f, 100f),
        new Vector3(-90f, -5f, -90f),
        new Vector3(90f, 40f, 90f),
        new Vector3(-80f, 0f, -80f),
        new Vector3(80f, 30f, 80f));

    [Test]
    public void Bounds_RequireFiniteContainedPositiveExtents()
    {
        Assert.That(ValidBounds.TryValidate(out string validError), Is.True, validError);

        OperationMapBoundsConfig inverted = new(
            Vector3.one,
            Vector3.zero,
            Vector3.zero,
            Vector3.one,
            Vector3.zero,
            Vector3.one);
        Assert.That(inverted.TryValidate(out _), Is.False);

        OperationMapBoundsConfig cameraOutsideWorld = new(
            Vector3.zero,
            Vector3.one * 10f,
            Vector3.one,
            Vector3.one * 9f,
            Vector3.zero,
            Vector3.one * 11f);
        Assert.That(cameraOutsideWorld.TryValidate(out _), Is.False);

        OperationMapBoundsConfig fixedCameraAltitude = new(
            Vector3.zero,
            Vector3.one * 10f,
            Vector3.one,
            Vector3.one * 9f,
            new Vector3(1f, 5f, 1f),
            new Vector3(9f, 5f, 9f));
        Assert.That(fixedCameraAltitude.TryValidate(out string fixedAltitudeError), Is.True, fixedAltitudeError);

        OperationMapBoundsConfig nonFinite = new(
            new Vector3(float.NaN, 0f, 0f),
            Vector3.one,
            Vector3.zero,
            Vector3.one,
            Vector3.zero,
            Vector3.one);
        Assert.That(nonFinite.TryValidate(out _), Is.False);
    }

    [TestCase("camera.ch01.m01.planning")]
    [TestCase("camera.skirmish.battle")]
    public void CameraId_AcceptsCanonicalScopedValues(string value) =>
        Assert.That(OperationMapIdentityRules.IsValidCameraId(value), Is.True);

    [TestCase("minimap.ch01.m01.projection")]
    [TestCase("minimap.skirmish.projection")]
    public void MinimapId_AcceptsCanonicalScopedValues(string value) =>
        Assert.That(OperationMapIdentityRules.IsValidMinimapId(value), Is.True);

    [TestCase("anchor.ch01.m01.objective.patrol")]
    [TestCase("anchor.skirmish.spawn.faction_1")]
    public void AnchorId_AcceptsCanonicalScopedValues(string value) =>
        Assert.That(OperationMapIdentityRules.IsValidAnchorId(value), Is.True);

    [TestCase(null)]
    [TestCase("")]
    [TestCase("camera")]
    [TestCase("camera.planning")]
    [TestCase("camera.ch01.bad-id")]
    [TestCase("minimap.ch01._projection")]
    [TestCase("anchor.ch01")]
    [TestCase("anchor/ch01/m01/objective")]
    public void ScopedIds_RejectMalformedValues(string value)
    {
        Assert.That(OperationMapIdentityRules.IsValidCameraId(value), Is.False);
        Assert.That(OperationMapIdentityRules.IsValidMinimapId(value), Is.False);
        Assert.That(OperationMapIdentityRules.IsValidAnchorId(value), Is.False);
    }

    [TestCase("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef")]
    [TestCase("ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")]
    public void Sha256_AcceptsCanonicalLowercaseHex(string value) =>
        Assert.That(OperationMapHashRules.IsValidSha256(value), Is.True);

    [TestCase(null)]
    [TestCase("")]
    [TestCase("0123456789abcdef0123456789abcdef")]
    [TestCase("0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF")]
    [TestCase("g123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef")]
    public void Sha256_RejectsMissingWrongLengthUppercaseOrNonHex(string value) =>
        Assert.That(OperationMapHashRules.IsValidSha256(value), Is.False);

    [Test]
    public void GridAndSurfaceMetadata_RequireStableIdentitiesAndValidExtents()
    {
        string hash128 = ValidSha256.Substring(0, 32);
        OperationMapGridMetadataConfig grid = new(
            hash128, ValidSha256, Vector3.zero, new Vector2Int(2048, 1024), 1f, 0);
        OperationMapSurfaceMetadataConfig surface = new(
            hash128, ValidSha256, hash128, 2097152, 3, 1, -2f, 84f);

        Assert.That(grid.TryValidate(out string gridError), Is.True, gridError);
        Assert.That(surface.TryValidate(out string surfaceError), Is.True, surfaceError);
        Assert.That(new OperationMapGridMetadataConfig(
            hash128, ValidSha256, Vector3.zero, Vector2Int.zero, 1f, 0).TryValidate(out _), Is.False);
        Assert.That(new OperationMapSurfaceMetadataConfig(
            hash128, ValidSha256, hash128, 0, 3, 1, -2f, 84f).TryValidate(out _), Is.False);
        Assert.That(new OperationMapSurfaceMetadataConfig(
            hash128, ValidSha256, hash128, 1, 3, 1, 10f, 9f).TryValidate(out _), Is.False);
    }

    [Test]
    public void SpatialRecords_ValidateWithoutHeavyAssetReferences()
    {
        OperationMapCameraConfig camera = CreateCamera("camera.ch01.m01.planning");
        OperationMapMinimapConfig minimap = new(
            "minimap.ch01.m01.projection",
            Vector3.zero,
            new Vector2(2048f, 1024f),
            0f);
        OperationMapAnchorConfig anchor = new(
            "anchor.ch01.m01.objective.patrol",
            OperationMapAnchorKind.Objective,
            Vector3.zero,
            Vector3.zero,
            4f);

        Assert.That(camera.TryValidate(out string cameraError), Is.True, cameraError);
        Assert.That(minimap.TryValidate(out string minimapError), Is.True, minimapError);
        Assert.That(anchor.TryValidate(out string anchorError), Is.True, anchorError);

        Type[] modelTypes =
        {
            typeof(OperationMapBoundsConfig),
            typeof(OperationMapGridMetadataConfig),
            typeof(OperationMapSurfaceMetadataConfig),
            typeof(OperationMapCameraConfig),
            typeof(OperationMapMinimapConfig),
            typeof(OperationMapAnchorConfig)
        };
        foreach (Type modelType in modelTypes)
        {
            foreach (FieldInfo field in modelType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                Assert.That(typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType), Is.False, $"{modelType.Name}.{field.Name}");
        }
    }

    [Test]
    public void Definition_RequiresResolvableCamerasAndUniqueTypedAnchors()
    {
        OperationMapDefinition map = CreateValidDefinition();
        try
        {
            Assert.That(map.TryValidateMetadata(out string validError), Is.True, validError);

            Set(map, "contentHash", ValidSha256.ToUpperInvariant());
            Assert.That(map.TryValidateMetadata(out string hashError), Is.False);
            StringAssert.Contains("content hash", hashError);
            Set(map, "contentHash", ValidSha256);

            Set(map, "anchors", new[]
            {
                CreateAnchor("anchor.ch01.m01.objective.patrol"),
                CreateAnchor("anchor.ch01.m01.objective.patrol")
            });
            Assert.That(map.TryValidateMetadata(out string duplicateError), Is.False);
            StringAssert.Contains("Duplicate", duplicateError);

            Set(map, "anchors", new[] { CreateAnchor("anchor.ch01.m01.objective.patrol") });
            Set(map, "battleCameraId", "camera.ch01.m01.missing");
            Assert.That(map.TryValidateMetadata(out string cameraError), Is.False);
            StringAssert.Contains("does not resolve", cameraError);

            Set(map, "battleCameraId", "camera.ch01.m01.battle");
            Set(map, "anchors", new[]
            {
                new OperationMapAnchorConfig(
                    "anchor.ch01.m01.objective.outside",
                    OperationMapAnchorKind.Objective,
                    new Vector3(1000f, 0f, 0f),
                    Vector3.zero,
                    4f)
            });
            Assert.That(map.TryValidateMetadata(out string anchorBoundsError), Is.False);
            StringAssert.Contains("inside world bounds", anchorBoundsError);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(map);
        }
    }

    private static OperationMapDefinition CreateValidDefinition()
    {
        OperationMapDefinition map = ScriptableObject.CreateInstance<OperationMapDefinition>();
        Set(map, "operationMapId", "opmap.ch01.district_edge_01");
        Set(map, "schemaVersion", 1);
        Set(map, "contentVersion", 1);
        Set(map, "sourceIdentityHash", ValidSha256);
        Set(map, "contentHash", ValidSha256);
        Set(map, "generatedMetadataHash", ValidSha256);
        Set(map, "bounds", ValidBounds);
        Set(map, "gridMetadata", new OperationMapGridMetadataConfig(
            ValidSha256.Substring(0, 32), ValidSha256, new Vector3(-100f, 0f, -100f), new Vector2Int(200, 200), 1f, 0));
        Set(map, "surfaceMetadata", new OperationMapSurfaceMetadataConfig(
            ValidSha256.Substring(0, 32), ValidSha256, ValidSha256.Substring(0, 32), 40000, 3, 1, -10f, 50f));
        Set(map, "cameras", new[]
        {
            CreateCamera("camera.ch01.m01.planning"),
            CreateCamera("camera.ch01.m01.battle")
        });
        Set(map, "planningCameraId", "camera.ch01.m01.planning");
        Set(map, "battleCameraId", "camera.ch01.m01.battle");
        Set(map, "minimap", new OperationMapMinimapConfig(
            "minimap.ch01.m01.projection",
            Vector3.zero,
            new Vector2(2048f, 1024f),
            0f));
        Set(map, "anchors", new[] { CreateAnchor("anchor.ch01.m01.objective.patrol") });
        return map;
    }

    private static OperationMapCameraConfig CreateCamera(string id) =>
        new(id, new Vector3(0f, 30f, 0f), new Vector3(45f, 0f, 0f), true, 60f, 20f, true);

    private static OperationMapAnchorConfig CreateAnchor(string id) =>
        new(id, OperationMapAnchorKind.Objective, Vector3.zero, Vector3.zero, 4f);

    private static void Set<T>(OperationMapDefinition map, string fieldName, T value)
    {
        FieldInfo field = typeof(OperationMapDefinition).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(map, value);
    }
}
