// Source tooling only. This adapter invokes the existing Editor staging scope
// and original launch resolver; it does not publish a catalog entry.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Editor;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Unity.Entities;

internal sealed class SourceStagedLaunch : IDisposable
{
    private IDisposable availability;
    internal Entity Session { get; private set; }
    internal SkirmishResolvedSetup Setup { get; private set; }
    internal SkirmishLaunchPayload Payload { get; private set; }

    // The materialized source catalog is never mutated. The scope receives an
    // isolated managed catalog containing its exact authored fields plus the
    // already compiled candidate definition.
    internal static SkirmishExpansionCatalogConfig CreateToolCatalog(
        SkirmishExpansionCatalogConfig original, SkirmishScenarioDefinitionConfig definition)
    {
        if (original == null || definition == null || original.Definitions.Any(x => x != null && x.CatalogId == definition.CatalogId))
            throw new ArgumentException("A new definition and an unchanged source catalog are required");
        var copy = UnityEngine.ScriptableObject.CreateInstance<SkirmishExpansionCatalogConfig>();
        var fields = typeof(SkirmishExpansionCatalogConfig).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        var definitions = fields.SingleOrDefault(x => x.Name == "definitions" && x.FieldType == typeof(SkirmishScenarioDefinitionConfig[]));
        if (definitions == null) throw new InvalidOperationException("Original authored definition field required");
        foreach (var field in fields) field.SetValue(copy, field.GetValue(original));
        definitions.SetValue(copy, original.Definitions.Concat(new[] { definition }).ToArray());
        if (!copy.TryGetDefinition(definition.CatalogId, out var actual) || !ReferenceEquals(actual, definition) ||
            original.Definitions.Any(x => x != null && x.CatalogId == definition.CatalogId))
            throw new InvalidOperationException("Isolated authored catalog identity changed");
        return copy;
    }

    internal static SourceStagedLaunch Open(EntityManager em, SkirmishExpansionCatalogConfig authoredCatalog,
        SkirmishScenarioDefinitionConfig definition, IReadOnlyList<SkirmishSetupMatrixRow> matrix,
        SkirmishContentManifest manifest, SkirmishResolvedSetup expected, int toolIndex)
    {
        if (em == null || authoredCatalog == null || definition == null || expected == null ||
            definition.CatalogId != expected.CatalogId || definition.DefinitionId != expected.DefinitionId ||
            definition.ContentVersion != expected.ContentVersion || expected.SetupHash != SkirmishSetupCompiler.ComputeHash(expected, expected.Seed))
            throw new ArgumentException("Actual authored definition and compiled setup identity required");
        var result = new SourceStagedLaunch();
        try
        {
            result.availability = SkirmishGenericMissionAvailabilityScope.Open(authoredCatalog, expected.CatalogId, toolIndex);
            if (!SkirmishExpandedLaunchResolver.TryCompileAndQueue(em, expected.CatalogId, expected.DifficultyId,
                expected.SizeId, expected.Seed, authoredCatalog.ToAuthoredSet(), matrix, manifest,
                out var setup, out var payload, out var reasons, null, definition.ContentVersion))
                throw new InvalidOperationException("Original staged resolver rejected: " + string.Join(";", reasons));
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            if (sessions.CalculateEntityCount() != 1) throw new InvalidOperationException("Exactly one original queued session required");
            var entity = sessions.GetSingletonEntity();
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(entity);
            var record = em.GetComponentObject<SkirmishLaunchDefinitionRecord>(entity);
            if (setup.SetupHash != expected.SetupHash || setup.LayoutBindingFingerprint != expected.LayoutBindingFingerprint ||
                payload == null || state.Phase != SkirmishSessionPhase.Queued || state.SetupHash != expected.SetupHash ||
                record == null || !ReferenceEquals(record.Definition, definition) || !record.Matches(setup, state) ||
                !SkirmishMissionLogicalBindingProvider.TryRead(em, entity, out var binding) ||
                binding.Fingerprint != setup.MissionLogicalBindingFingerprint)
                throw new InvalidOperationException("Original queued session differs from compiled authored input");
            result.Session = entity; result.Setup = setup; result.Payload = payload;
            Console.Error.WriteLine("[SourceStagedLaunch] entity=" + entity.Index + ":" + entity.Version +
                " session=" + state.SessionId + " catalog=" + setup.CatalogId + " definition=" + setup.DefinitionId +
                " version=" + setup.ContentVersion + " setup_hash=" + setup.SetupHash +
                " phase=" + state.Phase + " scope=source_tooling mission_ready=0");
            return result;
        }
        catch { result.Dispose(); throw; }
    }

    public void Dispose() { availability?.Dispose(); availability = null; }
}
