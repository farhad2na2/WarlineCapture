using System.Text;
using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public static class SkirmishStressCensus
    {
        public static SkirmishStressCensusSample Measure(EntityManager em, in SkirmishStressSession session, float elapsedSeconds)
        {
            var sample = new SkirmishStressCensusSample
            {
                Phase = session.ActivePhase,
                RequestedCombat = session.RequestedCombat,
                RequestedAir = session.RequestedAir,
                RequestedSupport = session.RequestedSupport,
                RequestedBuildings = session.RequestedBuildings,
                ElapsedSeconds = elapsedSeconds
            };
            if (em == default)
                return sample;

            using var query = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<Faction>(), ComponentType.ReadOnly<UnitHealth>() },
                None = new[] { ComponentType.ReadOnly<OperationMapBuildingComponent>(), ComponentType.ReadOnly<OperationMapAuthoredVehiclePresentation>() }
            });
            using var entities = query.ToEntityArray(Allocator.Temp);
            int spawnedCombat = 0, spawnedAir = 0, spawnedSupport = 0, spawnedBuildings = 0;
            int aliveCombat = 0, aliveAir = 0, aliveSupport = 0, aliveBuildings = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                byte faction = em.GetComponentData<Faction>(entity).Id;
                if (faction is not (1 or 2)) continue;
                bool alive = em.GetComponentData<UnitHealth>(entity).Current > 0;
                bool building = em.HasComponent<RuntimeBuildingCombatTag>(entity);
                bool support = !building && IsSupport(em, entity);
                bool air = !building && !support && IsAir(em, entity);
                bool combat = !building && !support;
                if (building)
                {
                    spawnedBuildings++;
                    if (alive) aliveBuildings++;
                }
                if (support)
                {
                    spawnedSupport++;
                    if (alive) aliveSupport++;
                }
                if (air)
                {
                    spawnedAir++;
                    if (alive) aliveAir++;
                }
                if (combat)
                {
                    spawnedCombat++;
                    if (alive) aliveCombat++;
                }
            }

            int lostUnits = 0;
            int lostBuildings = 0;
            using var matches = em.CreateEntityQuery(typeof(SkirmishMatchState));
            if (matches.CalculateEntityCount() == 1)
            {
                var match = matches.GetSingleton<SkirmishMatchState>();
                lostUnits = match.PlayerUnitsLost + match.EnemyUnitsLost;
                lostBuildings = match.PlayerBuildingsLost + match.EnemyBuildingsLost;
            }
            int deadCombatInWorld = spawnedCombat - aliveCombat;
            sample.AliveCombat = aliveCombat;
            sample.AliveAir = aliveAir;
            sample.AliveSupport = aliveSupport;
            sample.AliveBuildings = aliveBuildings;
            sample.DestroyedCombat = lostUnits > deadCombatInWorld ? lostUnits : deadCombatInWorld;
            sample.DestroyedAir = spawnedAir - aliveAir;
            sample.DestroyedSupport = spawnedSupport - aliveSupport;
            sample.DestroyedBuildings = lostBuildings > spawnedBuildings - aliveBuildings
                ? lostBuildings
                : spawnedBuildings - aliveBuildings;
            sample.SpawnedCombat = aliveCombat + sample.DestroyedCombat;
            sample.SpawnedAir = spawnedAir;
            sample.SpawnedSupport = spawnedSupport;
            sample.SpawnedBuildings = aliveBuildings + sample.DestroyedBuildings;
            sample.MissingSpawnCombat = session.RequestedCombat - sample.SpawnedCombat;
            sample.LiveProjectiles = CountLiveProjectiles(em);
            return sample;
        }

        public static bool IsSupport(EntityManager em, Entity entity)
        {
            if (em.HasComponent<UnitResourceHauler>(entity)) return true;
            return NameContains(em, entity, "truck");
        }

        public static bool IsAir(EntityManager em, Entity entity)
        {
            if (em.HasComponent<UnitAirMovement>(entity)) return true;
            return NameContains(em, entity, "helicopter") || NameContains(em, entity, "drone") || NameContains(em, entity, "jet");
        }

        public static int CountLiveProjectiles(EntityManager em)
        {
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<UnitAttackTraceComponent>());
            using var entities = query.ToEntityArray(Allocator.Temp);
            int live = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                if (em.GetComponentData<UnitAttackTraceComponent>(entities[i]).TimeRemaining > 0f)
                    live++;
            }
            return live;
        }

        public static string FormatLine(in SkirmishStressCensusSample sample)
        {
            return SkirmishStressRecipe.ReportMarker
                + " phase=" + sample.Phase
                + " requestedCombat=" + sample.RequestedCombat
                + " spawnedCombat=" + sample.SpawnedCombat
                + " aliveCombat=" + sample.AliveCombat
                + " destroyedCombat=" + sample.DestroyedCombat
                + " missingSpawnCombat=" + sample.MissingSpawnCombat
                + " requestedAir=" + sample.RequestedAir
                + " spawnedAir=" + sample.SpawnedAir
                + " aliveAir=" + sample.AliveAir
                + " requestedSupport=" + sample.RequestedSupport
                + " spawnedSupport=" + sample.SpawnedSupport
                + " aliveSupport=" + sample.AliveSupport
                + " requestedBuildings=" + sample.RequestedBuildings
                + " spawnedBuildings=" + sample.SpawnedBuildings
                + " aliveBuildings=" + sample.AliveBuildings
                + " destroyedBuildings=" + sample.DestroyedBuildings
                + " liveProjectiles=" + sample.LiveProjectiles
                + " elapsed=" + sample.ElapsedSeconds.ToString("0.00");
        }

        public static string FormatReport(in SkirmishStressSession session, DynamicBuffer<SkirmishStressCensusSample> samples)
        {
            var text = new StringBuilder();
            text.AppendLine("# E0.3 stress census");
            text.AppendLine();
            text.AppendLine("- Seed: `" + session.Seed + "`");
            text.AppendLine("- Scale target: `" + session.Scale + "` combat units across both sides");
            text.AppendLine("- Layout: `" + session.Layout + "`");
            text.AppendLine("- Mode: `" + (session.Sequence != 0 ? "sequence" : "single-phase") + "`");
            text.AppendLine("- Active phase: `" + session.ActivePhase + "`");
            text.AppendLine();
            text.AppendLine("| Phase | Requested combat | Spawned combat | Alive combat | Destroyed combat | Missing spawn | Spawned air | Alive air | Spawned support | Spawned buildings | Live traces | Elapsed |");
            text.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|");
            for (int i = 0; i < samples.Length; i++)
            {
                var sample = samples[i];
                text.Append("| ").Append(sample.Phase)
                    .Append(" | ").Append(sample.RequestedCombat)
                    .Append(" | ").Append(sample.SpawnedCombat)
                    .Append(" | ").Append(sample.AliveCombat)
                    .Append(" | ").Append(sample.DestroyedCombat)
                    .Append(" | ").Append(sample.MissingSpawnCombat)
                    .Append(" | ").Append(sample.SpawnedAir)
                    .Append(" | ").Append(sample.AliveAir)
                    .Append(" | ").Append(sample.SpawnedSupport)
                    .Append(" | ").Append(sample.SpawnedBuildings)
                    .Append(" | ").Append(sample.LiveProjectiles)
                    .Append(" | ").Append(sample.ElapsedSeconds.ToString("0.00"))
                    .AppendLine(" |");
            }
            text.AppendLine();
            text.AppendLine("Spawned/alive/destroyed columns are measured entity counts. Requested columns are authoring inputs only.");
            return text.ToString();
        }

        private static bool NameContains(EntityManager em, Entity entity, string token)
        {
            if (!em.HasComponent<UnitSourcePrefabKey>(entity)) return false;
            return em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString().ToLowerInvariant().Contains(token);
        }
    }
}
