extern alias ActualCore;
using Bounds = ActualCore::UnityEngine.Bounds;
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    internal sealed partial class AutonomousWorld
    {
        internal SourceProductionGroundWorld ActualProductionWorld { get; private set; }
        internal SourceProductionDeliveryOwner ActualDeliveryOwner { get; private set; }
        internal BuildingProductionQueueCompositionSystemHelper ActualProduction { get; private set; }
        internal BuildingProductionQueueCompositionSystemHelper.QueueContext ActualProductionContext;
        internal IReadOnlyDictionary<SourceUnitPrefab, BuildingProductionRecipe> ActualRecipes { get; private set; }
        internal IReadOnlyDictionary<SourceUnitPrefab, RuntimeDefinition> ActualDefinitionsByPrefab { get; private set; }
        internal IReadOnlyList<SourceUnitPrefab> ActualConfiguredUnits { get; private set; }
        internal IReadOnlyDictionary<string, SourceUnitPrefab> ActualUnitsByKey { get; private set; }

        internal static void RequireActualStartupDomain(SkirmishResolvedSetup setup,int buildings,int actors){if(setup==null||setup.Structures==null||setup.Forces==null||buildings!=setup.Structures.Length||actors!=setup.Forces.Sum(force=>force.Quantity))throw new InvalidOperationException("Actual compiled structures and force totals must be constructed exactly");}

        internal void PrepareActualProductionInputs(int actualGlobalQueueLimit, uint actualConstructorRandomState,
            BuildingProductionQueueCompositionSystemHelper.TryGetUnitProductionMetadataDelegate metadataResolver)
        {
            if (ActualDeliveryOwner != null || ActualUnitMetadata == null || SourceDefinitions == null ||
                Setup==null)
                throw new InvalidOperationException("Fresh complete compiled startup and metadata required once");
            RequireActualStartupDomain(Setup,Buildings.Count,OpeningActors.Count);
            var catalog = Entities.GetComponentObject<SkirmishProductionCatalogRecord>(Session).Catalog;
            var units = ActualUnitMetadata.Ordered.Select(row => row.Prefab).ToArray();
            var byKey = new Dictionary<string, SourceUnitPrefab>(StringComparer.Ordinal);
            var definitions = new Dictionary<SourceUnitPrefab, RuntimeDefinition>();
            foreach (var definition in SourceDefinitions.Definitions)
            {
                if (!definition.SourceMetadataComplete || definition.Prefab == null || definition.IsWall || definition.HasRunway)
                    throw new NotSupportedException("Actual ordinary source ground definitions only");
                definitions.Add(definition.Prefab, definition);
            }
            foreach (var unit in units)
            {
                ActualUnitMetadata.RequireDeliverable(unit);
                byKey.Add(unit.name.Trim().ToLowerInvariant(), unit);
            }
            var recipes = new Dictionary<SourceUnitPrefab, BuildingProductionRecipe>();
            foreach (var row in ActualUnitMetadata.Ordered)
            {
                if (!catalog.TryRole(row.Prefab.name, out var role))
                    throw new NotSupportedException("Actual configured unit has no retained role overlay");
                string producerKey = GeneratedProductionRecipeSource.ProducerPrefabKey(role.Producer);
                if (!SourceDefinitions.Assets.TryGetValue(producerKey, out var producer) || !definitions.ContainsKey(producer))
                    throw new NotSupportedException("Source role producer definition is missing");
                recipes.Add(row.Prefab, new BuildingProductionRecipe
                {
                    ProducerPrefab = producer, UnitPrefab = row.Prefab,
                    Quantity = Math.Max(1, role.SquadMembers), MaterialsCost = role.MaterialsCost,
                    CreditsCost = 0, UseProducerGroundExit = GeneratedProductionRecipeSource.UsesGroundExit(role)
                });
            }
            MetadataCompatibility?.RequireProductionFacts(Setup,catalog,ActualUnitMetadata,SourceDefinitions,recipes);
            var roots = new Dictionary<int, SourceProducerRootFrame>();
            var instances = new Dictionary<int, RuntimeInstance>();
            var grid = Entities.GetComponentData<GridConfig>(GridEntity);
            foreach (var pair in Buildings)
            {
                var b = pair.Value;
                if (b == null || b.IsDestroyed || b.Instance != null || !b.InitialProducerCollectionsKnown ||
                    b.Definition == null || b.Definition.IsWall || b.Definition.HasRunway)
                    throw new NotSupportedException("Fresh ordinary numeric producer root domain required");
                Vector3 position = new GeneratedSourceBuildingCenter().GetFootprintCenter(
                    b.OriginCell, b.Definition.FootprintCells, grid, generatedBuildPlane);
                roots.Add(b.Id, new SourceProducerRootFrame(new float3(position.x, position.y, position.z), quaternion.identity, 1f));
                instances.Add(b.Id, new RuntimeInstance { transform = new RuntimeTransform { position = position } });
            }
            var production = new BuildingProductionQueueCompositionSystemHelper();
            production.ConfigureUnitProductionMetadataResolver(metadataResolver ?? throw new ArgumentNullException(nameof(metadataResolver)));
            production.ConfigureProductionRecipes(recipes.Values.ToArray());
            BuildingProductionQueueCompositionSystemHelper.TryGetPrefabLocalBoundsDelegate bounds =
                (SourceUnitPrefab prefab, out Bounds value) =>
                { value = default; throw new NotSupportedException("Unadmitted source prefab local-bounds read: " + prefab?.name); };
            var context = new BuildingProductionQueueCompositionSystemHelper.QueueContext(units, byKey,
                new BuildingProductionSlotUtilitySystemHelper(), bounds, null,
                (EntityManager em, out Entity entity) => { entity = Boundary; return em == Entities && em.Exists(entity); });
            var ownerRecipes = new Dictionary<int, SourceProductionDeliveryRecipe[]>();
            foreach (var pair in Buildings)
            {
                var slots = pair.Value.Definition.ProductionSlots;
                var selected = new List<SourceProductionDeliveryRecipe>();
                if (slots != null) for (int index = 0; index < slots.Count; index++)
                {
                    var slot = slots[index];
                    if (slot == null || slot.SpawnUnitPrefab == null) throw new NotSupportedException("Unknown actual production slot");
                    // Retain the authored slot in its definition. The original immutable
                    // mission catalog rejects non-configured fallback units before payment;
                    // no unknown metadata or template is fabricated for an unreachable action.
                    if (!catalog.TryRole(slot.SpawnUnitPrefab.name, out _))
                    {
                        if (SkirmishNativeProduction.CanQueue(Entities, Session, pair.Value, slot.SpawnUnitPrefab))
                            throw new InvalidOperationException("Source catalog rejection proof changed");
                        continue;
                    }
                    GeneratedUnitProductionMetadata.Row row;
                    try { row = ActualUnitMetadata.RequireDeliverable(slot.SpawnUnitPrefab); }
                    catch (NotSupportedException error) { throw new NotSupportedException("Actual definition slot has no configured/template metadata: building="+pair.Key+" definition="+pair.Value.Definition.Prefab.name+" unit="+slot.SpawnUnitPrefab.name, error); }
                    if (!recipes.TryGetValue(row.Prefab, out var recipe) || slot.Quantity != recipe.Quantity)
                        throw new InvalidOperationException("Source overlay recipe/definition mismatch: building="+pair.Key+" definition="+pair.Value.Definition.Prefab.name+" unit="+row.Prefab.name+" slotQuantity="+slot.Quantity+" recipeQuantity="+recipe?.Quantity+" recipeProducer="+recipe?.ProducerPrefab?.name+" sameProducer="+ReferenceEquals(recipe?.ProducerPrefab,pair.Value.Definition.Prefab));
                    selected.Add(new SourceProductionDeliveryRecipe(index, row.Prefab, slot.Quantity, row.Production));
                }
                ownerRecipes.Add(pair.Key, selected.ToArray());
            }
            var spatial = new SourceProductionGroundWorld(Entities, GridEntity, Boundary, Buildings, roots);
            var owner = new SourceProductionDeliveryOwner(spatial, production, context, ownerRecipes,
                actualGlobalQueueLimit, actualConstructorRandomState);
            if (S026InactiveWaterDiagnostic) { owner.DiagnosticSession = Session; owner.DiagnosticWorldId = WorldId; owner.DiagnosticTick = () => Tick; }
            // Numeric wrapper root data from the original PositionBuildingObject policy;
            // no visual object/readiness or renderer hierarchy is claimed.
            foreach (var pair in Buildings) pair.Value.Instance = instances[pair.Key];
            ActualConfiguredUnits = units; ActualUnitsByKey = byKey; ActualDefinitionsByPrefab = definitions;
            ActualRecipes = recipes; ActualProductionContext = context; ActualProduction = production;
            ActualProductionWorld = spatial; ActualDeliveryOwner = owner;
        }
    }
}
