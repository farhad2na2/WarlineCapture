using System;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Entities;

namespace Game.Runtime
{
    // Source-host launch and actual initialization owners. This creates no
    // native UI input, physical scene, or player-readiness evidence.
    internal static class SourceExpandedSessionConstruction
    {
        internal static Entity Create(EntityManager em, SkirmishResolvedSetup setup,
            SkirmishScenarioDefinitionConfig definition, string attempt)
        {
            if (em == null || setup == null || definition == null || String.IsNullOrWhiteSpace(attempt) ||
                definition.CatalogId != setup.CatalogId || definition.DefinitionId != setup.DefinitionId ||
                definition.ContentVersion != setup.ContentVersion || definition.OperationMapId != setup.OperationMapId ||
                definition.MapLayoutId != setup.LayoutId || setup.SetupHash != SkirmishSetupCompiler.ComputeHash(setup, setup.Seed))
                throw new InvalidOperationException("Actual compiled source setup and attempt required");
            var payload = new SkirmishLaunchPayload { SessionId = attempt, CatalogId = setup.CatalogId, DefinitionId = setup.DefinitionId,
                ContentVersion = setup.ContentVersion, AllConfigHashes = setup.SetupHash.ToString("X8", System.Globalization.CultureInfo.InvariantCulture),
                MapId = setup.OperationMapId, LayoutId = setup.LayoutId, DifficultyId = setup.DifficultyId, SizeId = setup.SizeId, Seed = setup.Seed };
            if (!SkirmishExpandedLaunchProjection.TryQueue(em, payload, setup, null, definition))
                throw new InvalidOperationException("Original source launch projection rejected the compiled setup");
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishResolvedSetupRecord));
            if (sessions.CalculateEntityCount() != 1) throw new InvalidOperationException("Unique original source session required");
            var entity = sessions.GetSingletonEntity();
            if (!ReferenceEquals(em.GetComponentObject<SkirmishResolvedSetupRecord>(entity).Setup, setup))
                throw new InvalidOperationException("Original source session does not retain the compiled setup");
            var owner = new SkirmishSessionInitializationSystem();
            var state = new SystemState { EntityManager = em, Enabled = true };
            state.Begin(); owner.OnCreate(ref state); state.End();
            state.Begin();
            try
            {
                if (!state.Enabled || !state.RequiredQueriesPresent) throw new InvalidOperationException("Original session owner is not eligible to update");
                owner.OnUpdate(ref state);
            }
            finally { state.End(); }
            var session = em.GetComponentData<SkirmishExpandedSessionComponent>(entity);
            if (session.InitializationComplete != 1 || session.Phase != SkirmishSessionPhase.Spawning ||
                !ReferenceEquals(em.GetComponentObject<SkirmishResolvedSetupRecord>(entity).Setup, setup))
                throw new InvalidOperationException("Original session owner did not initialize the actual source setup");
            return entity;
        }
    }
}
