using System;
using System.Collections.Generic;
using System.Linq;
using Game.Components;
using Unity.Entities;

namespace Game.Runtime
{
    // Source-backed projection of the legacy SkirmishCatalogPolicy branch for
    // the retained S073 diagnostic or its disposable copy. It models only unit registry membership; it does
    // not claim the native preset object or authorize building catalog queries.
    internal sealed class SourceS073LegacyCatalogScope : IDisposable
    {
        [ThreadStatic] internal static SourceS073LegacyCatalogScope Current;
        const int S073IndustrialBasinScenarioIndex = 3;
        const int ConfiguredUnitCount = 3;

        readonly EntityManager copy;
        readonly SourceUnitPrefab[] canonicalUnits;
        readonly string sourcePhase;
        readonly string copyPhase;
        readonly bool retainedWorld;

        // The retained diagnostic world has its own source-backed registry;
        // it must not borrow the disposable-copy fixture's world authority.
        internal SourceS073LegacyCatalogScope(EntityManager world,
            S073NativeConfiguredUnits registry, IReadOnlyList<SourceUnitPrefab> productionUnits)
        {
            if (Current != null) throw new NotSupportedException("Nested legacy catalog scope unsupported");
            if (world == null || registry == null || !registry.BelongsTo(world) ||
                registry.Count != ConfiguredUnitCount || productionUnits == null || productionUnits.Count != ConfiguredUnitCount)
                throw new NotSupportedException("Exact same-world S073 registry required");
            ValidateSession(world, SkirmishPhase.Playing, out copyPhase, "Retained S073 world");
            sourcePhase = copyPhase;
            retainedWorld = true;
            var registered = new SourceUnitPrefab[ConfiguredUnitCount];
            for (int i = 0; i < ConfiguredUnitCount; i++)
                if (!registry.TryRead(i, out registered[i], out _, out _, out _, out _) || registered[i] == null)
                    throw new NotSupportedException("Complete ordered S073 registry row required at " + i);
            canonicalUnits = ValidateProjection(registered, productionUnits);
            copy = world;
            Current = this;
        }

        internal SourceS073LegacyCatalogScope(EntityManager source, EntityManager disposableCopy,
            S073NativeConfiguredUnits sourceRegistry, IReadOnlyList<SourceUnitPrefab> productionUnits)
        {
            if (Current != null) throw new NotSupportedException("Nested legacy catalog scope unsupported");
            if (source == null || disposableCopy == null || ReferenceEquals(source, disposableCopy) ||
                sourceRegistry == null || !sourceRegistry.BelongsTo(source) ||
                sourceRegistry.Count != ConfiguredUnitCount || productionUnits == null ||
                productionUnits.Count != ConfiguredUnitCount)
                throw new NotSupportedException("Pinned three-entry same-source S073 registry and distinct disposable copy required");

            ValidateSession(source, SkirmishPhase.Preparing, out sourcePhase, "Source S073 world");
            ValidateSession(disposableCopy, SkirmishPhase.Playing, out copyPhase, "Disposable S073 copy");

            var registered = new SourceUnitPrefab[ConfiguredUnitCount];
            for (int i = 0; i < ConfiguredUnitCount; i++)
            {
                if (!sourceRegistry.TryRead(i, out registered[i], out _, out _, out _, out _) ||
                    registered[i] == null || productionUnits[i] == null)
                    throw new NotSupportedException("Complete ordered S073 registry row required at " + i);
            }
            canonicalUnits = ValidateProjection(registered, productionUnits);

            copy = disposableCopy;
            Current = this;
        }

        static void ValidateSession(EntityManager em, SkirmishPhase requiredPhase, out string phase, string label)
        {
            using (var matches = em.CreateEntityQuery(typeof(SkirmishMatchState)))
            using (var expanded = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)))
            {
                if (matches.CalculateEntityCount() != 1 || !expanded.IsEmptyIgnoreFilter)
                    throw new NotSupportedException(label + " must contain one legacy match and no expanded session");
                SkirmishMatchState match = matches.GetSingleton<SkirmishMatchState>();
                if (match.ScenarioIndex != S073IndustrialBasinScenarioIndex || match.Phase != requiredPhase)
                    throw new NotSupportedException(label + " has the wrong S073 scenario or population phase");
                phase = match.Phase.ToString();
            }
        }

        static string Identity(SourceUnitPrefab prefab)
        {
            if (prefab == null || string.IsNullOrEmpty(prefab.name) ||
                string.IsNullOrEmpty(prefab.SourceAssetPath) || string.IsNullOrEmpty(prefab.SourceAssetGuid) ||
                prefab.SourceAssetLocalId == 0)
                throw new NotSupportedException("Exact authored S073 prefab asset identity required");
            return prefab.SourceAssetPath + "\n" + prefab.SourceAssetGuid + "\n" +
                prefab.SourceAssetLocalId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        static bool ContainsReference(SourceUnitPrefab[] values, SourceUnitPrefab candidate)
        {
            return candidate != null && values.Any(value => ReferenceEquals(value, candidate));
        }

        static bool SameAssetIdentity(SourceUnitPrefab left, SourceUnitPrefab right)
        {
            return left != null && right != null && left.name == right.name && Identity(left) == Identity(right);
        }

        static SourceUnitPrefab[] ValidateProjection(IReadOnlyList<SourceUnitPrefab> registered,
            IReadOnlyList<SourceUnitPrefab> production)
        {
            if (registered == null || production == null || registered.Count != ConfiguredUnitCount ||
                production.Count != ConfiguredUnitCount)
                throw new NotSupportedException("Exact three-entry S073 registry projection required");
            var canonical = new SourceUnitPrefab[ConfiguredUnitCount];
            var identities = new HashSet<string>(StringComparer.Ordinal);
            var keys = new HashSet<string>(StringComparer.Ordinal);
            var references = new HashSet<SourceUnitPrefab>();
            for (int i = 0; i < ConfiguredUnitCount; i++)
            {
                SourceUnitPrefab registry = registered[i];
                SourceUnitPrefab runtime = production[i];
                if (!SameAssetIdentity(registry, runtime) || !identities.Add(Identity(registry)) ||
                    !keys.Add(registry.name) || !references.Add(runtime))
                    throw new NotSupportedException("Unique exact S073 registry-to-production asset identity required at " + i);
                canonical[i] = runtime;
            }
            return canonical;
        }

        static void RequireUnitOnly(bool building)
        {
            if (building) throw new NotSupportedException("S073 legacy building Spawnables registry was not projected");
        }

        internal static void VerifyProjectionControls()
        {
            var registered = SourceUnitPrefab.FromAssetIdentity("Unit_A", "Assets/Game/Prefabs/Unit_A.prefab",
                "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", 1);
            var canonical = SourceUnitPrefab.FromAssetIdentity("Unit_A", "Assets/Game/Prefabs/Unit_A.prefab",
                "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", 1);
            var foreign = SourceUnitPrefab.FromAssetIdentity("Unit_A", "Assets/Game/Prefabs/Unit_A.prefab",
                "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", 1);
            var registeredTwo = SourceUnitPrefab.FromAssetIdentity("Unit_B", "Assets/Game/Prefabs/Unit_B.prefab",
                "cccccccccccccccccccccccccccccccc", 2);
            var registeredThree = SourceUnitPrefab.FromAssetIdentity("Unit_C", "Assets/Game/Prefabs/Unit_C.prefab",
                "dddddddddddddddddddddddddddddddd", 3);
            var canonicalTwo = SourceUnitPrefab.FromAssetIdentity("Unit_B", "Assets/Game/Prefabs/Unit_B.prefab",
                "cccccccccccccccccccccccccccccccc", 2);
            var canonicalThree = SourceUnitPrefab.FromAssetIdentity("Unit_C", "Assets/Game/Prefabs/Unit_C.prefab",
                "dddddddddddddddddddddddddddddddd", 3);
            SourceUnitPrefab[] projected = ValidateProjection(new[] { registered, registeredTwo, registeredThree },
                new[] { canonical, canonicalTwo, canonicalThree });
            if (!ReferenceEquals(projected[0], canonical) || !SameAssetIdentity(registered, canonical) ||
                !ContainsReference(new[] { canonical }, canonical) ||
                ContainsReference(new[] { canonical }, SourceUnitPrefab.FromAssetIdentity("Unit_A",
                    "Assets/Game/Prefabs/Unit_A.prefab", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", 1)) ||
                SameAssetIdentity(registered, foreign))
                throw new InvalidOperationException("S073 legacy catalog projection identity controls failed");

            bool rejectedDuplicate = false;
            try { ValidateProjection(new[] { registered, registered, registeredThree },
                new[] { canonical, canonical, canonicalThree }); }
            catch (NotSupportedException) { rejectedDuplicate = true; }
            if (!rejectedDuplicate) throw new InvalidOperationException("S073 duplicate registry identity control was not rejected");

            bool rejectedMissing = false;
            try { Identity(new SourceUnitPrefab("Unit_Missing", new Entity { Index = 1, Version = 1 })); }
            catch (NotSupportedException) { rejectedMissing = true; }
            if (!rejectedMissing) throw new InvalidOperationException("S073 missing asset identity control was not rejected");

            bool rejectedBuilding = false;
            try { RequireUnitOnly(true); }
            catch (NotSupportedException) { rejectedBuilding = true; }
            if (!rejectedBuilding) throw new InvalidOperationException("S073 building catalog control was not rejected");
            RequireUnitOnly(false);
        }

        internal static bool Allows(EntityManager em, SourceUnitPrefab prefab, bool building)
        {
            var scope = Current;
            if (scope == null || !ReferenceEquals(scope.copy, em))
                throw new NotSupportedException("Exact bound S073 legacy catalog world required");
            RequireUnitOnly(building);
            if (prefab == null) return false;
            return ContainsReference(scope.canonicalUnits, prefab);
        }

        internal object Evidence => new
        {
            policy = "SkirmishCatalogPolicy.Allows.Legacy.UnitPrefabRegistryConfig.UnitSpawnPrefabs",
            registry_count = ConfiguredUnitCount,
            registry_source = "PinnedS073NativeConfiguredUnits_OrderedAssetIdentityBijection",
            source_phase = sourcePhase,
            bound_world_phase = copyPhase,
            bound_world_scope = retainedWorld ? "RetainedDiagnostic" : "DisposableCopy",
            disposable_copy_phase = retainedWorld ? null : copyPhase,
            source_world_mutated = false,
            building_queries_supported = false,
            native_full_parity_claimed = false
        };

        public void Dispose()
        {
            if (!ReferenceEquals(Current, this)) throw new InvalidOperationException("S073 legacy catalog scope ownership lost");
            Current = null;
        }
    }
}
