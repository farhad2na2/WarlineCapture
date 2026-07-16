using System.Reflection;
using Game.Components;
using Game.Configs;
using NUnit.Framework;
using UnityEngine;

public sealed class OperationMapContractValidationTests
{
    private const string Hash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [Test]
    public void ValidDefinitionsAndResolvedScenariosPass()
    {
        OperationMapDefinition map = CreateValidDefinition("opmap.skirmish.desert_base_01");
        ScenarioSetupConfig scenario = CreateScenario(
            "scenario.skirmish.desert_patrol",
            map.OperationMapId);

        Assert.That(
            OperationMapContractValidation.TryValidate(
                new[] { map },
                new[] { scenario },
                new[] { EvidenceFor(map) },
                out string error),
            Is.True,
            error);
    }

    [Test]
    public void MissingCollectionsAndEntriesFailClosed()
    {
        OperationMapDefinition map = CreateValidDefinition("opmap.skirmish.desert_base_01");

        Assert.That(OperationMapContractValidation.TryValidate(null, System.Array.Empty<ScenarioSetupConfig>(), System.Array.Empty<OperationMapContractEvidence>(), out _), Is.False);
        Assert.That(OperationMapContractValidation.TryValidate(System.Array.Empty<OperationMapDefinition>(), System.Array.Empty<ScenarioSetupConfig>(), System.Array.Empty<OperationMapContractEvidence>(), out _), Is.False);
        Assert.That(OperationMapContractValidation.TryValidate(new[] { map }, null, new[] { EvidenceFor(map) }, out _), Is.False);
        Assert.That(OperationMapContractValidation.TryValidate(new[] { map }, System.Array.Empty<ScenarioSetupConfig>(), null, out _), Is.False);
        Assert.That(OperationMapContractValidation.TryValidate(new OperationMapDefinition[] { null }, System.Array.Empty<ScenarioSetupConfig>(), new[] { EvidenceFor(map) }, out _), Is.False);
        Assert.That(OperationMapContractValidation.TryValidate(new[] { map }, new ScenarioSetupConfig[] { null }, new[] { EvidenceFor(map) }, out _), Is.False);
    }

    [Test]
    public void DuplicateMapAndScenarioIdsFailOrdinalUniqueness()
    {
        OperationMapDefinition first = CreateValidDefinition("opmap.skirmish.desert_base_01");
        OperationMapDefinition duplicate = CreateValidDefinition("opmap.skirmish.desert_base_01");
        Assert.That(
            OperationMapContractValidation.TryValidate(
                new[] { first, duplicate },
                System.Array.Empty<ScenarioSetupConfig>(),
                new[]
                {
                    EvidenceFor(first),
                    EvidenceFor(CreateValidDefinition("opmap.skirmish.evidence_only"))
                },
                out string mapError),
            Is.False);
        StringAssert.Contains("Duplicate operation-map id", mapError);

        OperationMapDefinition secondMap = CreateValidDefinition("opmap.skirmish.desert_base_02");
        ScenarioSetupConfig firstScenario = CreateScenario("scenario.skirmish.desert_patrol", first.OperationMapId);
        ScenarioSetupConfig duplicateScenario = CreateScenario("scenario.skirmish.desert_patrol", secondMap.OperationMapId);
        Assert.That(
            OperationMapContractValidation.TryValidate(
                new[] { first, secondMap },
                new[] { firstScenario, duplicateScenario },
                new[] { EvidenceFor(first), EvidenceFor(secondMap) },
                out string scenarioError),
            Is.False);
        StringAssert.Contains("Duplicate scenario id", scenarioError);
    }

    [Test]
    public void UnresolvedScenarioMapLinkFails()
    {
        OperationMapDefinition map = CreateValidDefinition("opmap.skirmish.desert_base_01");
        ScenarioSetupConfig scenario = CreateScenario(
            "scenario.skirmish.desert_patrol",
            "opmap.skirmish.missing_map");

        Assert.That(
            OperationMapContractValidation.TryValidate(
                new[] { map },
                new[] { scenario },
                new[] { EvidenceFor(map) },
                out string error),
            Is.False);
        StringAssert.Contains("unresolved operation-map id", error);
    }

    [Test]
    public void InvalidMetadataAndIdentityFailBeforePublication()
    {
        OperationMapDefinition map = CreateValidDefinition("opmap.skirmish.desert_base_01");
        OperationMapContractEvidence validEvidence = EvidenceFor(map);
        Set(map, "contentHash", "stale");
        Assert.That(
            OperationMapContractValidation.TryValidate(
                new[] { map },
                System.Array.Empty<ScenarioSetupConfig>(),
                new[] { validEvidence },
                out string mapError),
            Is.False);
        StringAssert.Contains("content hash", mapError);

        map = CreateValidDefinition("opmap.skirmish.desert_base_01");
        ScenarioSetupConfig invalidScenario = CreateScenario("invalid", map.OperationMapId);
        Assert.That(
            OperationMapContractValidation.TryValidate(
                new[] { map },
                new[] { invalidScenario },
                new[] { EvidenceFor(map) },
                out string scenarioError),
            Is.False);
        StringAssert.Contains("Invalid scenario id", scenarioError);
    }

    [Test]
    public void StaleHashEvidenceFailsEvenWhenBothHashesAreWellFormed()
    {
        OperationMapDefinition map = CreateValidDefinition("opmap.skirmish.desert_base_01");
        OperationMapContractEvidence stale = new(
            map.OperationMapId,
            map.SchemaVersion,
            map.ContentVersion,
            map.SourceIdentityHash,
            "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789",
            map.GeneratedMetadataHash);

        Assert.That(
            OperationMapContractValidation.TryValidate(
                new[] { map },
                System.Array.Empty<ScenarioSetupConfig>(),
                new[] { stale },
                out string error),
            Is.False);
        StringAssert.Contains("stale version or hash evidence", error);
    }

    [Test]
    public void ValidContractValidationAllocatesZeroBytesAfterWarmup()
    {
        OperationMapDefinition[] maps =
            { CreateValidDefinition("opmap.skirmish.desert_base_01") };
        ScenarioSetupConfig[] scenarios =
            { CreateScenario("scenario.skirmish.desert_patrol", maps[0].OperationMapId) };
        OperationMapContractEvidence[] evidence = { EvidenceFor(maps[0]) };
        Assert.That(OperationMapContractValidation.TryValidate(maps, scenarios, evidence, out _), Is.True);

        long before = System.GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < 128; index++)
            OperationMapContractValidation.TryValidate(maps, scenarios, evidence, out _);
        long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.Zero);
    }

    private static OperationMapDefinition CreateValidDefinition(string operationMapId)
    {
        OperationMapDefinition definition = ScriptableObject.CreateInstance<OperationMapDefinition>();
        Set(definition, "operationMapId", operationMapId);
        Set(definition, "schemaVersion", 1);
        Set(definition, "contentVersion", 1);
        Set(definition, "sourceIdentityHash", Hash);
        Set(definition, "contentHash", Hash);
        Set(definition, "generatedMetadataHash", Hash);
        Set(definition, "bounds", new OperationMapBoundsConfig(
            new Vector3(-100f, -10f, -100f),
            new Vector3(100f, 50f, 100f),
            new Vector3(-90f, -5f, -90f),
            new Vector3(90f, 40f, 90f),
            new Vector3(-80f, 10f, -80f),
            new Vector3(80f, 40f, 80f)));
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
                Vector3.zero,
                Vector3.zero,
                4f)
        });
        return definition;
    }

    private static ScenarioSetupConfig CreateScenario(string scenarioId, string operationMapId)
    {
        ScenarioSetupConfig scenario = ScriptableObject.CreateInstance<ScenarioSetupConfig>();
        Set(scenario, "scenarioId", scenarioId);
        Set(scenario, "operationMapId", operationMapId);
        return scenario;
    }

    private static OperationMapContractEvidence EvidenceFor(OperationMapDefinition map)
    {
        return new OperationMapContractEvidence(
            map.OperationMapId,
            map.SchemaVersion,
            map.ContentVersion,
            map.SourceIdentityHash,
            map.ContentHash,
            map.GeneratedMetadataHash);
    }

    private static void Set<T>(object target, string fieldName, T value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(target, value);
    }
}
