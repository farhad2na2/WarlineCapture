using System;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    // Scoped source-host authority for the original actor projection. This is
    // not native player-session, scene, or mission-readiness evidence.
    internal sealed class SourceS004OpeningContext : IDisposable
    {
        [ThreadStatic] static SourceS004OpeningContext active;
        readonly EntityManager manager;
        readonly Entity session;
        readonly SkirmishResolvedSetup setup;
        readonly JObject frozenSetup;
        readonly string sourceRoot;
        bool disposed;
        private SourceS004OpeningContext(EntityManager manager, Entity session, SkirmishResolvedSetup setup, string sourceRoot)
        { this.manager = manager; this.session = session; this.setup = setup; this.sourceRoot = sourceRoot; frozenSetup = JObject.FromObject(setup); }

        internal static SourceS004OpeningContext Bind(EntityManager em, Entity session, SkirmishResolvedSetup setup,
            string setupPath, string setupSha, string sourceRoot)
        {
            if (active != null || em == null || setup == null || setup.CatalogId != "S004" || !System.IO.Path.IsPathRooted(sourceRoot ?? "") || !em.Exists(session) ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session) || !em.HasComponent<SkirmishResolvedSetupRecord>(session) ||
                !em.HasComponent<SkirmishSharedBuildingsReady>(session) ||
                !ReferenceEquals(em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup, setup) ||
                em.GetComponentData<SkirmishExpandedSessionComponent>(session).Phase != SkirmishSessionPhase.Spawning ||
                SkirmishMissionLogicalBindingCompiler.RequiresBinding(setup))
                throw new InvalidOperationException("Actual S004 source session and completed starting buildings required");
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishResolvedSetupRecord));
            if (sessions.CalculateEntityCount() != 1 || sessions.GetSingletonEntity() != session)
                throw new InvalidOperationException("Unique S004 source session required");
            SourceS004CompiledSetupInput.RequireExact(setup, setupPath, setupSha);
            return active = new SourceS004OpeningContext(em, session, setup, System.IO.Path.GetFullPath(sourceRoot));
        }
        internal static void Require(EntityManager em, FixedString64Bytes sessionId, SkirmishResolvedForceEntry force,
            int forceIndex, int member, int perMemberSupply, string prefabKey, SkirmishResolvedSetup setup,
            SkirmishTypedSpawnConstraint typedPlacement)
        {
            var bound = active;
            if (bound == null || bound.disposed || !ReferenceEquals(bound.manager, em) || !ReferenceEquals(bound.setup, setup) ||
                !em.Exists(bound.session) || !em.HasComponent<SkirmishSharedBuildingsReady>(bound.session) ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(bound.session) ||
                !em.HasComponent<SkirmishResolvedSetupRecord>(bound.session) ||
                !ReferenceEquals(em.GetComponentObject<SkirmishResolvedSetupRecord>(bound.session).Setup, setup) ||
                !em.GetComponentData<SkirmishExpandedSessionComponent>(bound.session).SessionId.Equals(sessionId) ||
                em.GetComponentData<SkirmishExpandedSessionComponent>(bound.session).Phase != SkirmishSessionPhase.Spawning ||
                !JToken.DeepEquals(bound.frozenSetup, JObject.FromObject(setup)) || typedPlacement != null ||
                setup.Forces == null || forceIndex < 0 || forceIndex >= setup.Forces.Length ||
                !JToken.DeepEquals(JObject.FromObject(force), JObject.FromObject(setup.Forces[forceIndex])) ||
                force.Quantity <= 0 || member < 0 || member >= force.Quantity ||
                perMemberSupply != force.SupplyCost / force.Quantity || prefabKey != force.RuntimePrefabKey)
                throw new InvalidOperationException("Exact active same-world S004 opening member required");
        }
        internal static bool RequireSourceStartupExecution(EntityManager em, Entity session, SkirmishResolvedSetup setup)
        {
            var bound = active;
            if (bound == null || bound.disposed || !ReferenceEquals(bound.manager, em) || bound.session != session ||
                !ReferenceEquals(bound.setup, setup) || !em.Exists(session) ||
                !em.HasComponent<SkirmishSharedBuildingsReady>(session) ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session) ||
                !em.HasComponent<SkirmishResolvedSetupRecord>(session) ||
                !ReferenceEquals(em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup, setup) ||
                em.GetComponentData<SkirmishExpandedSessionComponent>(session).Phase != SkirmishSessionPhase.Spawning ||
                !JToken.DeepEquals(bound.frozenSetup, JObject.FromObject(setup)))
                throw new InvalidOperationException("Active same-world S004 source startup scope required");
            return true;
        }
        internal static string RequireSourceOpeningView(EntityManager em, Entity session, SkirmishResolvedSetup setup)
        {
            var bound = active;
            if (bound == null || bound.disposed || !ReferenceEquals(bound.manager, em) || bound.session != session ||
                !ReferenceEquals(bound.setup, setup) || !em.Exists(session) ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session) ||
                !em.HasComponent<SkirmishResolvedSetupRecord>(session) ||
                !ReferenceEquals(em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup, setup) ||
                em.GetComponentData<SkirmishExpandedSessionComponent>(session).Phase != SkirmishSessionPhase.Playing ||
                em.GetComponentData<SkirmishExpandedSessionComponent>(session).SpawnComplete != 1 ||
                !JToken.DeepEquals(bound.frozenSetup, JObject.FromObject(setup)))
                throw new InvalidOperationException("Actual S004 source spawn transition required before view selection");
            return bound.sourceRoot;
        }
        public void Dispose()
        {
            if (disposed) return;
            if (!ReferenceEquals(active, this)) throw new InvalidOperationException("S004 opening scope changed");
            disposed = true; active = null;
        }
    }
}
