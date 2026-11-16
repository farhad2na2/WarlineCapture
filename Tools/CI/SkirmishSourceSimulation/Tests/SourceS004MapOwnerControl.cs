using System;
using System.Collections.Generic;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Unity.Entities;

internal static class SourceS004MapOwnerControl
{
    public static int Main(string[] args)
    {
        if (args.Length != 7) throw new ArgumentException("Graph/hash, matrix/hash, root, layout/hash required");
        var input = SourceExpandedAuthoredSetupInput.Prepare(args[0], args[1], args[2], args[3], args[4], "S004");
        var map = input.Assets.ByPath<OperationMapDefinition>("Assets/Game/Resources/SkirmishS004OperationMap.asset");
        var canonical = input.Assets.ByPath<SkirmishBattleCatalogConfig>("Assets/Game/Resources/SkirmishBattleCatalog.asset");
        if (map == null || canonical == null) throw new InvalidOperationException("Pinned source map and canonical catalog required");
        UnityEngine.Resources.BindSourceResources(new Dictionary<string, object>
        {
            [SkirmishExpansionCatalogConfig.ResourceName] = input.Catalog,
            [SkirmishBattleCatalogConfig.ResourceName] = canonical,
            ["SkirmishS004OperationMap"] = map
        });
        using var replay = SourceValidatedLayoutJson.Open(input.Definition.MapLayout, args[5], args[6]);
        var setup = input.Compile(SkirmishDifficultyId.Regular, SkirmishSizeId.Standard, 104733);
        int next = 600000;
        var em = new EntityManager { LogicalAllocatorAdmitted = true };
        em.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        if (SkirmishS004Environment.LoadMapForSession(em) != null)
            throw new InvalidOperationException("Absent S004 session must not select a map");
        var payload = new SkirmishLaunchPayload { CatalogId = setup.CatalogId, DefinitionId = setup.DefinitionId,
            ContentVersion = setup.ContentVersion, AllConfigHashes = setup.SetupHash.ToString("X8", System.Globalization.CultureInfo.InvariantCulture),
            MapId = setup.OperationMapId, LayoutId = setup.LayoutId, DifficultyId = setup.DifficultyId, SizeId = setup.SizeId, Seed = setup.Seed };
        var invalid = new SkirmishLaunchPayload { CatalogId = "S002", DefinitionId = payload.DefinitionId,
            ContentVersion = payload.ContentVersion, AllConfigHashes = payload.AllConfigHashes, MapId = payload.MapId,
            LayoutId = payload.LayoutId, DifficultyId = payload.DifficultyId, SizeId = payload.SizeId, Seed = payload.Seed };
        if (SkirmishExpandedLaunchProjection.TryQueue(em, invalid, setup, null, input.Definition))
            throw new InvalidOperationException("Mismatched launch payload must reject");
        var sessionEntity = SourceExpandedSessionConstruction.Create(em, setup, input.Definition, "source-s004-owner-control");
        bool duplicateRejected = false;
        try { SourceExpandedSessionConstruction.Create(em, setup, input.Definition, "source-s004-owner-control-duplicate"); }
        catch (InvalidOperationException) { duplicateRejected = true; }
        using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishResolvedSetupRecord));
        if (!duplicateRejected || sessions.CalculateEntityCount() != 1 || sessions.GetSingletonEntity() != sessionEntity || !SkirmishS004Environment.IsActive(em) ||
            !ReferenceEquals(SkirmishS004Environment.LoadMapForSession(em), map))
            throw new InvalidOperationException("Original source map owner did not select the actual bound S004 map");
        Console.WriteLine("[SourceS004MapOwnerControl] result=Passed actualLaunchProjection=1 actualSessionOwner=1 actualMapOwner=1 sameGraphMap=1 absentMismatchDuplicateRejected=1 gameplay=0 readiness=0");
        return 0;
    }
}
