using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Newtonsoft.Json.Linq;

namespace Game.Runtime
{
    // Normal-profile bridge from original AIProduction requests to the shared
    // production queue and delivery owner. It admits only current registered
    // producer rows and the exact retained authoring/readback metadata.
    internal sealed class SourceS073LegacyProductionRuntime
    {
        const string MetadataRelativePath = "Design/AgentReports/SkirmishProduction/S073/TransportMetadataReadback20261101/s073-unit-production-metadata.json";
        const string MetadataSha = "1372d72063491572eeaa11ffc78a8bbf790f10d1ef9f673fe5627af4447c3e8e";
        const string ReceiptRelativePath = "Design/AgentReports/SkirmishProduction/S073/NativeProductionRecipeObservation20261101/recipe-observation-receipt.json";
        const string ReceiptSha = "43e8e923810ae82d04dbdf0ca18a2e35d5b44122b4337b7b23fdaa53739fbffb";

        readonly EntityManager em;
        readonly Entity boundary;
        readonly Dictionary<int, RuntimeBuildingEntity> buildings;
        readonly SourceFactionProductionDispatcher dispatcher;
        readonly SourceProductionDeliveryOwner delivery;
        readonly SourceCampAvailabilityKernel playerRequestOwner;
        readonly SourceCampAvailabilityKernel.Context playerRequestContext;
        readonly JObject provenance;
        readonly S073NativeConfiguredUnits configuredUnits;
        readonly IReadOnlyList<SourceUnitPrefab> configuredPrefabs;
        readonly IReadOnlyDictionary<SourceUnitPrefab, SourceTransportTemplateData> transportTemplates;
        JObject sourceTransportDiagnosticProvenance;
        internal int RequestProcessingUpdates { get; private set; }
        internal int DeliveryUpdates { get; private set; }
        internal int DeliveredMembers => delivery.DeliveredMembers;

        // Explicit logical source command. This is not a UI press or native input.
        // All legality, payment ordering and queue decisions remain with the
        // original semantic request, resource and production queue owners.
        internal JObject RequestPlayerConfiguredUnit(int index, float now)
        {
            if (!float.IsFinite(now) || now < 0 || index < 0 || index >= configuredPrefabs.Count ||
                !configuredUnits.TryRead(index, out var sourcePrefab, out _, out int price, out bool requestable, out _) ||
                !requestable || !ReferenceEquals(sourcePrefab, configuredPrefabs[index]))
                throw new NotSupportedException("Actual same-world requestable S073 configured unit required");
            if (delivery.Helicopters == null)
                throw new NotSupportedException("Actual source transport owner must be bound before a player request");
            Entity bank = Entity.Null;
            using (var query = em.CreateEntityQuery(typeof(FactionEconomy), typeof(FactionTacticalMaterialsComponent)))
            using (var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp))
                foreach (var candidate in entities)
                {
                    var economy = em.GetComponentData<FactionEconomy>(candidate);
                    var materials = em.GetComponentData<FactionTacticalMaterialsComponent>(candidate);
                    if (!FactionIdentity.IsPlayerControlled(economy.FactionId)) continue;
                    if (bank != Entity.Null || economy.FactionId != materials.FactionId)
                        throw new NotSupportedException("Unique matching player resource bank required");
                    bank = candidate;
                }
            if (bank == Entity.Null) throw new NotSupportedException("Original player resource bank missing");
            var resources = new SourceCampResourceReads(em, bank);
            var economyBefore = em.GetComponentData<FactionEconomy>(bank);
            var materialsBefore = em.GetComponentData<FactionTacticalMaterialsComponent>(bank);
            if (!configuredUnits.TryCosts(sourcePrefab, price, out int authoredCredits, out int authoredMaterials))
                throw new NotSupportedException("Exact configured unit cost binding required");
            var context = new SourceCampAvailabilityKernel.Context
            {
                RuntimeBuildings = buildings, UnitSpawnPrefabs = configuredPrefabs,
                UnitSpawnPrefabsByKey = playerRequestContext.UnitSpawnPrefabsByKey,
                MaxQueuedUnitProductions = playerRequestContext.MaxQueuedUnitProductions,
                ProductionSystem = playerRequestContext.ProductionSystem,
                ProductionQueueContext = playerRequestContext.ProductionQueueContext,
                GetProductionPrefab = playerRequestContext.GetProductionPrefab,
                ResourceMaterials = resources.CurrentMaterials,
                TryGetEntityManager = (out EntityManager manager) => { manager = em; return true; },
                EvaluateConstructionResources = resources.EvaluateConstructionResources,
                TryResolveUnitResourceCosts = configuredUnits.TryCosts,
                RunwaySystem = new SourceCampProvenNoRunwayReads(buildings, true)
            };
            var adapter = new LegacyPlayerRequestAdapter(this, context, bank, now);
            SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure result;
            string required;
            using (new SourceS073LegacyCatalogScope(em, configuredUnits, configuredPrefabs))
                result = SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Execute<SourceUnitPrefab, RuntimeBuildingEntity, SourcePendingProduction>(
                    adapter, sourcePrefab, price, false, 0, now, out required);
            var economyAfter = em.GetComponentData<FactionEconomy>(bank);
            var materialsAfter = em.GetComponentData<FactionTacticalMaterialsComponent>(bank);
            return new JObject {
                ["scope"] = "ExplicitLogicalSourceCommand_NotNativeInput",
                ["unit_index"] = index, ["unit_prefab"] = sourcePrefab.name,
                ["result"] = result.ToString(), ["accepted"] = result == SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.None,
                ["required_building"] = required, ["pending_recorded"] = adapter.Pending != null,
                ["authored_credits_cost"] = authoredCredits, ["authored_materials_cost"] = authoredMaterials,
                ["resource_policy"] = economyBefore.MaterialsOnlyConstruction,
                ["credits_before"] = economyBefore.Money, ["credits_after"] = economyAfter.Money,
                ["materials_before"] = materialsBefore.Current, ["materials_after"] = materialsAfter.Current,
                ["pending_refundable_materials"] = adapter.Pending == null ? (int?)null : adapter.Pending.RefundableMaterials,
                ["pending_paid_quantity"] = adapter.Pending == null ? (int?)null : adapter.Pending.PaidQuantity,
                ["native_input_observed"] = false, ["mission_ready"] = false
            };
        }

        sealed class LegacyPlayerRequestAdapter : SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.IAdapter<SourceUnitPrefab, RuntimeBuildingEntity, SourcePendingProduction>
        {
            readonly SourceS073LegacyProductionRuntime owner;
            readonly SourceCampAvailabilityKernel.Context context;
            readonly Entity bank;
            readonly float now;
            internal SourcePendingProduction Pending;
            internal LegacyPlayerRequestAdapter(SourceS073LegacyProductionRuntime owner, SourceCampAvailabilityKernel.Context context, Entity bank, float now)
            { this.owner = owner; this.context = context; this.bank = bank; this.now = now; }
            public SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure Evaluate(SourceUnitPrefab prefab, int price, out string required) =>
                SourceCampUiAdapter.ToSemanticFailure(owner.playerRequestOwner.GetCampRequestFailure(context, prefab, price, out required));
            public bool IsConfiguredSpawnable(SourceUnitPrefab prefab) => false;
            public bool BeginConfiguredPlacement(SourceUnitPrefab prefab) => throw new NotSupportedException("Legacy unit command cannot place buildings");
            public bool HasGlobalQueueCapacity() => SourceCampAvailabilityKernel.HasGlobalQueueCapacity(context);
            public bool TryResolveCosts(SourceUnitPrefab prefab, int fallback, out int credits, out int materials) =>
                SourceCampAvailabilityKernel.TryResolveUnitResourceCosts(context, prefab, fallback, out credits, out materials);
            public bool TryFindProducer(SourceUnitPrefab prefab, bool capacity, out int id, out int index, out string display) =>
                owner.playerRequestOwner.TryFindFirstFriendlyProducerBuilding(context, prefab, capacity, out id, out index, out display);
            public bool TryGetRequiredProducerDisplayName(SourceUnitPrefab prefab, out string display) =>
                owner.playerRequestOwner.TryGetRequiredProducerDisplayName(context, prefab, out display);
            public bool TryFindOperationMapProducer(SourceUnitPrefab prefab)
            {
                if (owner.playerRequestOwner.TryFindFirstFriendlyOperationMapProducer(context, prefab, out _, out _, out _))
                    throw new NotSupportedException("Source operation-map player production queue is not bound");
                return false;
            }
            public bool HasGlobalQueueCapacityAfterOperationMapFailure() => SourceCampAvailabilityKernel.HasGlobalQueueCapacity(context);
            public SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure Spend(int credits, int materials)
            {
                var economy = owner.em.GetComponentData<FactionEconomy>(bank);
                var stock = owner.em.GetComponentData<FactionTacticalMaterialsComponent>(bank);
                var result = FactionConstructionResourceUtilitySystemHelper.TrySpend(ref economy, ref stock, credits, materials);
                if (result == FactionConstructionResourceMutationResult.Applied)
                { owner.em.SetComponentData(bank, economy); if (materials > 0) owner.em.SetComponentData(bank, stock); }
                return SourceCampUiAdapter.ToSemanticFailure(SourceCampResourceFailure.MapConstructionResourceFailure(result));
            }
            public void Restore(int credits, int materials)
            {
                var economy = owner.em.GetComponentData<FactionEconomy>(bank);
                var stock = owner.em.GetComponentData<FactionTacticalMaterialsComponent>(bank);
                if (FactionConstructionResourceUtilitySystemHelper.TryRollback(ref economy, ref stock, credits, materials) != FactionConstructionResourceMutationResult.Applied)
                    throw new InvalidOperationException("Original player resource rollback failed");
                owner.em.SetComponentData(bank, economy);
                if (materials > 0) owner.em.SetComponentData(bank, stock);
            }
            public bool TryQueueOperationMap(SourceUnitPrefab prefab, float time) => throw new NotSupportedException("Source operation-map queue not bound");
            public bool IsMissionReceipt() => SourceCampNativeProduction.TrySession(owner.em, out _);
            public bool TryGetProducer(int id, out RuntimeBuildingEntity producer) => owner.buildings.TryGetValue(id, out producer) && producer != null;
            public void FocusProducer(RuntimeBuildingEntity producer, SourceUnitPrefab prefab) => throw new NotSupportedException("Logical source command cannot focus native UI");
            public SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.QueueOutcome TryQueueUnit(RuntimeBuildingEntity producer, int index, int frame, float time, out SourcePendingProduction pending)
            {
                bool accepted = owner.delivery.TryQueue(producer.Id, index, now, out pending);
                Pending = pending;
                return accepted ? SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.QueueOutcome.Queued :
                    SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.QueueOutcome.Rejected;
            }
            public void AdjustAcceptedPending(RuntimeBuildingEntity producer, SourcePendingProduction pending, int credits, int materials)
            {
                if (pending == null || !Game.Tactical.Contracts.MissionConstructionCostPolicy.TryResolve(
                    owner.em.GetComponentData<FactionEconomy>(bank).MaterialsOnlyConstruction, credits, materials, out _, out int effective))
                    throw new InvalidOperationException("Actual accepted pending and resource policy required");
                pending.RefundableMaterials = effective;
                pending.PaidQuantity = Math.Max(1, pending.RemainingQuantity);
            }
            public void RecordUnitOrdered(SourceUnitPrefab prefab) { }
        }

        SourceS073LegacyProductionRuntime(EntityManager manager, Entity runtimeBoundary,
            Dictionary<int, RuntimeBuildingEntity> runtimeBuildings,
            SourceFactionProductionDispatcher requestDispatcher, SourceProductionDeliveryOwner deliveryOwner,
            SourceCampAvailabilityKernel requestOwner, SourceCampAvailabilityKernel.Context requestContext,
            JObject sourceProvenance, S073NativeConfiguredUnits unitRegistry, IReadOnlyList<SourceUnitPrefab> unitPrefabs,
            IReadOnlyDictionary<SourceUnitPrefab, SourceTransportTemplateData> authoredTransportTemplates)
        {
            em = manager ?? throw new ArgumentNullException(nameof(manager));
            boundary = runtimeBoundary;
            buildings = runtimeBuildings ?? throw new ArgumentNullException(nameof(runtimeBuildings));
            dispatcher = requestDispatcher ?? throw new ArgumentNullException(nameof(requestDispatcher));
            delivery = deliveryOwner ?? throw new ArgumentNullException(nameof(deliveryOwner));
            playerRequestOwner = requestOwner ?? throw new ArgumentNullException(nameof(requestOwner));
            playerRequestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
            provenance = sourceProvenance == null ? throw new ArgumentNullException(nameof(sourceProvenance)) : (JObject)sourceProvenance.DeepClone();
            configuredUnits = unitRegistry ?? throw new ArgumentNullException(nameof(unitRegistry));
            configuredPrefabs = unitPrefabs ?? throw new ArgumentNullException(nameof(unitPrefabs));
            transportTemplates = authoredTransportTemplates ?? throw new ArgumentNullException(nameof(authoredTransportTemplates));
            if (!em.Exists(boundary) || !em.HasComponent<BuildingRuntimeStateTag>(boundary) ||
                !ReferenceEquals(delivery.World.Em, em) || !ReferenceEquals(delivery.World.Buildings, buildings))
                throw new NotSupportedException("Same-world S073 legacy production owner binding required");
        }

        internal static SourceS073LegacyProductionRuntime Bind(string repositoryRoot, EntityManager em,
            Entity grid, Entity runtimeBoundary, Dictionary<int, RuntimeBuildingEntity> runtimeBuildings,
            S073NativeConfiguredUnits configuredUnits, bool historicalInputDiagnostic = false)
        {
            if (string.IsNullOrWhiteSpace(repositoryRoot) || em == null || !em.Exists(grid) ||
                !em.Exists(runtimeBoundary) || !em.HasComponent<BuildingRuntimeStateTag>(runtimeBoundary) ||
                runtimeBuildings == null || runtimeBuildings.Count != 10 || configuredUnits == null || !configuredUnits.BelongsTo(em))
                throw new NotSupportedException("Exact same-world S073 producer registry, configured unit binding and runtime boundary required");
            using (var matches = em.CreateEntityQuery(typeof(SkirmishMatchState)))
            using (var expanded = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)))
                if (matches.CalculateEntityCount() != 1 || !expanded.IsEmptyIgnoreFilter ||
                    matches.GetSingleton<SkirmishMatchState>().Phase != SkirmishPhase.Playing)
                    throw new NotSupportedException("One live non-expanded S073 legacy session required for production binding");

            JObject metadata, receipt;
            string metadataPath = Path.GetFullPath(Path.Combine(repositoryRoot, MetadataRelativePath));
            string receiptPath = Path.GetFullPath(Path.Combine(repositoryRoot, ReceiptRelativePath));
            metadata = ReadPinnedJson(metadataPath, MetadataSha, 8 * 1024 * 1024);
            receipt = ReadPinnedJson(receiptPath, ReceiptSha, 1024 * 1024);
            Require((string)metadata["kind"] == "S073AuthoredUnitProductionMetadata" && (int?)metadata["schema_version"] == 1 &&
                (string)metadata["authority"] == "OriginalConfiguredAuthoringFactories_NotMissionState" &&
                (string)metadata["mission"] == "S073" && (int?)metadata["seed_context"] == 104729 &&
                (string)metadata["scope"] == "LegacyInitialForcesAndUnitRegistry_AuthoringOnly_NotRuntimeActivation" &&
                (bool?)metadata["full_mission_initialized"] == false && (bool?)metadata["native_visual_readiness"] == false &&
                (bool?)metadata["baked_actor_footprint_substituted"] == false && (metadata["unsupported_values"] as JArray)?.Count == 0,
                "Qualified source-only S073 production metadata envelope required");
            Require(configuredUnits.Count == 3 && ((JArray)metadata["units"])?.Count == 3 &&
                (string)metadata["legacy_active_unit_registry"]?["path"] == "Assets/Game/Configs/Skirmish/UnitRegistry.asset" &&
                (string)metadata["building_placement_config"]?["path"] == "Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset",
                "Exact three-unit S073 active legacy registry selection required");

            JObject sourcePins = metadata["source_pins"] as JObject ?? throw new InvalidDataException("S073 production source pins missing");
            var historicalSourceDifferences = new JArray();
            string[] requiredSources = {
                "Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs",
                "Assets/Game/Scripts/Composition/BuildingProductionUnitMetadataPrefabSystemHelper.cs",
                "Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.TransportMetadata.cs",
                "Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset",
                "Assets/Game/Configs/Skirmish/UnitRegistry.asset",
                "Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Transport.prefab"
            };
            foreach (string relative in requiredSources)
                RequireProductionSource(repositoryRoot, relative, (string)sourcePins[relative], historicalInputDiagnostic,
                    historicalSourceDifferences, "S073 production authoring source changed: ");
            Require(File.ReadAllLines(Path.Combine(repositoryRoot, "Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset"))
                .Count(line => line.Trim() == "maxQueuedUnitProductions: 4") == 1,
                "Pinned S073 production queue limit changed");

            JObject observation = receipt["observation"] as JObject ?? throw new InvalidDataException("S073 native recipe observation missing");
            Require((string)receipt["kind"] == "S073ProductionRecipeStartupObservationReceipt" && (int?)receipt["schema_version"] == 1 &&
                (string)observation["mission"] == "S073" && (int?)observation["seed"] == 104729 &&
                (bool?)observation["legacy_session"] == true && (string)observation["building_placement_config"] == "Construction" &&
                (bool?)observation["recipe_input_is_null"] == true && (int?)observation["recipe_input_count"] == 0 &&
                (observation["recipe_input_rows"] as JArray)?.Count == 0 && (int?)observation["resulting_ground_exit_count"] == 0 &&
                (observation["resulting_ground_exit_prefabs"] as JArray)?.Count == 0 &&
                (uint?)observation["building_spawn_random_state"] == 0x12345678u &&
                (string)observation["random_state_owner"] == "BuildingGameplaySourceCompositionSystemHelper.BuildingSpawnRandomState",
                "Pinned S073 legacy null-recipe/startup-RNG observation required");
            JObject recipePins = receipt["observed_source_pins_sha256"] as JObject ?? throw new InvalidDataException("S073 recipe receipt source pins missing");
            foreach (string relative in new[] { "Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystemHelper.cs", "Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.cs" })
                RequireProductionSource(repositoryRoot, relative, (string)recipePins[relative], historicalInputDiagnostic,
                    historicalSourceDifferences, "S073 recipe owner source changed: ");

            var byKey = new Dictionary<string, SourceUnitPrefab>(StringComparer.Ordinal);
            var prefabs = new List<SourceUnitPrefab>(3);
            var metadataByPrefab = new Dictionary<SourceUnitPrefab, BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata>();
            var canonicalPrefabsBySourceKey = new Dictionary<string, SourceUnitPrefab>(StringComparer.Ordinal);
            var authoredDefinitions = new Dictionary<string, SourceUnitPrefab>(StringComparer.Ordinal);
            SourceUnitPrefab canonicalTransport = null;
            foreach (var building in runtimeBuildings.Values)
                foreach (var slot in building?.Definition?.ProductionSlots ?? new List<RuntimeDefinition.ProductionSlotDefinition>())
                {
                    if (slot?.SpawnUnitPrefab == null) continue;
                    if (authoredDefinitions.TryGetValue(slot.SpawnUnitPrefab.name, out SourceUnitPrefab priorDefinitionPrefab))
                    {
                        if (!SamePrefab(priorDefinitionPrefab, slot.SpawnUnitPrefab))
                            throw new InvalidDataException("Conflicting S073 producer asset identities share source key: " + slot.SpawnUnitPrefab.name);
                    }
                    else authoredDefinitions.Add(slot.SpawnUnitPrefab.name, slot.SpawnUnitPrefab);
                }

            JArray rows = (JArray)metadata["units"];
            for (int i = 0; i < rows.Count; i++)
            {
                JObject row = (JObject)rows[i];
                string key = (string)row["key"];
                Require((int?)row["index"] == i && (bool?)row["has_production_metadata"] == true &&
                    (string)row["prefab"]?["key"] == key && (bool?)((JObject)((JArray)metadata["registered_unit_prefabs"])[i])["also_in_legacy_initial_forces"] == true,
                    "Contiguous exact S073 configured production row required");
                Require(configuredUnits.TryRead(i, out SourceUnitPrefab configured, out _, out _, out _, out _) &&
                    configured != null && configured.name == key && authoredDefinitions.TryGetValue(key, out SourceUnitPrefab definition) &&
                    SamePrefab(configured, definition) && SameAsset(configured, (JObject)row["prefab"]) &&
                    SameAsset(definition, (JObject)row["prefab"]),
                    "Same-run configured and producer source prefab identity required: " + key);
                canonicalPrefabsBySourceKey.Add(key, configured);
                var fields = row["production_metadata"]?["fields"] as JObject ?? throw new InvalidDataException("S073 production fields missing: " + key);
                SourceUnitPrefab transport = null;
                JToken transportField = fields["ProductionTransportPrefab"];
                if ((string)transportField?["codec"] == "source-asset-reference-v1")
                {
                    JObject identity = (JObject)transportField;
                    Require((string)identity["key"] == "Unit_Veh_Helicopter_Transport" &&
                        (string)identity["path"] == "Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Transport.prefab" &&
                        (string)identity["guid"] == "e540888d7cd8a489f9472b3456bf2406" &&
                        (long?)identity["local_id"] == 5616646512338707422L,
                        "Exact S073 transport asset identity required");
                    if (canonicalTransport == null)
                        canonicalTransport = SourceUnitPrefab.FromAssetIdentity((string)identity["key"], (string)identity["path"],
                            (string)identity["guid"], (long)identity["local_id"]);
                    transport = canonicalTransport;
                }
                else Require((string)transportField?["codec"] == "null-v1", "Unsupported S073 transport metadata codec");
                JObject footprint = fields["FootprintCells"]?["fields"] as JObject ?? throw new InvalidDataException("S073 footprint metadata missing");
                var unitMetadata = new BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata(
                    ReadF32(fields["ProductionDurationSeconds"], key + ".duration"), transport,
                    ReadBool(fields["IsAirUnit"], key + ".air"), ReadF32(fields["ProductionTransportArrivalSeconds"], key + ".arrival"),
                    ReadF32(fields["ProductionTransportHoldForNextReadySeconds"], key + ".hold"),
                    ReadInt(fields["ProductionTransportMaxConcurrent"], key + ".concurrent"),
                    ReadBool(fields["ProductionTransportRequiresAirportRunway"], key + ".runway"),
                    ReadBool(fields["ProductionTransportUsesRunwayLanding"], key + ".runway_landing"),
                    new Vector2Int(ReadInt(footprint["UnityEngine.Vector2Int::m_X"], key + ".footprint.x"),
                        ReadInt(footprint["UnityEngine.Vector2Int::m_Y"], key + ".footprint.y")));
                Require(unitMetadata.ProductionDurationSeconds > 0 && unitMetadata.FootprintCells.x > 0 && unitMetadata.FootprintCells.y > 0,
                    "Positive exact S073 production duration/footprint required: " + key);
                prefabs.Add(configured); metadataByPrefab.Add(configured, unitMetadata);
                string normalized = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(key);
                Require(!string.IsNullOrEmpty(normalized) && byKey.TryAdd(normalized, configured), "Unique S073 configured production unit key required");
            }

            JArray transportRows = metadata["transports"] as JArray ?? throw new InvalidDataException("S073 authored transports missing");
            Require(canonicalTransport != null && transportRows.Count == 1 && transportRows[0] is JObject,
                "One exact configured S073 authored transport graph required");
            var authoredTransportTemplates = SourceS073TransportGraphTemplateBuilder.Build((JObject)transportRows[0], canonicalTransport);

            // Unity resolves repeated references to one prefab asset as the same object.
            // The source importer uses separate managers, so restore that identity only
            // after the complete name/path/GUID/local-ID comparison has succeeded.
            var slotBindings = new List<(RuntimeDefinition.ProductionSlotDefinition slot, SourceUnitPrefab prefab)>();
            foreach (var building in runtimeBuildings.Values)
                foreach (var slot in building?.Definition?.ProductionSlots ?? new List<RuntimeDefinition.ProductionSlotDefinition>())
                {
                    if (slot?.SpawnUnitPrefab == null) throw new NotSupportedException("Unknown S073 production slot source");
                    slotBindings.Add((slot, CanonicalizeProductionPrefab(slot.SpawnUnitPrefab, canonicalPrefabsBySourceKey)));
                }
            foreach (var binding in slotBindings) binding.slot.SpawnUnitPrefab = binding.prefab;

            var production = new BuildingProductionQueueCompositionSystemHelper();
            production.ConfigureProductionRecipes(null);
            production.ConfigureUnitProductionMetadataResolver((SourceUnitPrefab prefab,
                out BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata value) => metadataByPrefab.TryGetValue(prefab, out value));
            var queueContext = new BuildingProductionQueueCompositionSystemHelper.QueueContext(
                prefabs, byKey, new BuildingProductionSlotUtilitySystemHelper(), null, null,
                (EntityManager manager, out Entity entity) => { entity = runtimeBoundary; return ReferenceEquals(manager, em) && em.Exists(entity); });
            var recipes = new Dictionary<int, SourceProductionDeliveryRecipe[]>();
            var frames = new Dictionary<int, SourceProducerRootFrame>();
            foreach (var pair in runtimeBuildings.OrderBy(item => item.Key))
            {
                RuntimeBuildingEntity building = pair.Value;
                if (building == null || building.Id != pair.Key || building.Definition == null || !building.Definition.SourceMetadataComplete ||
                    building.CombatEntity == Entity.Null || !em.Exists(building.CombatEntity) || !em.HasComponent<LocalTransform>(building.CombatEntity))
                    throw new NotSupportedException("Actual same-world S073 producer definition/transform required: " + pair.Key);
                LocalTransform transform = em.GetComponentData<LocalTransform>(building.CombatEntity);
                frames.Add(pair.Key, new SourceProducerRootFrame(transform.Position, transform.Rotation, transform.Scale));
                var selected = new List<SourceProductionDeliveryRecipe>();
                var slots = building.Definition.ProductionSlots;
                if (slots != null)
                    for (int index = 0; index < slots.Count; index++)
                    {
                        var slot = slots[index];
                        if (slot == null || slot.SpawnUnitPrefab == null) throw new NotSupportedException("Unknown S073 production slot source");
                        SourceUnitPrefab recipePrefab = CanonicalizeProductionPrefab(slot.SpawnUnitPrefab, canonicalPrefabsBySourceKey);
                        if (metadataByPrefab.TryGetValue(recipePrefab, out var unit))
                            selected.Add(new SourceProductionDeliveryRecipe(index, recipePrefab, Math.Max(1, slot.Quantity), unit));
                    }
                recipes.Add(pair.Key, selected.ToArray());
            }
            var ground = new SourceProductionGroundWorld(em, grid, runtimeBoundary, runtimeBuildings, frames);
            var delivery = new SourceProductionDeliveryOwner(ground, production, queueContext, recipes, 4,
                (uint)observation["building_spawn_random_state"]);
            var requestContext = new SourceCampAvailabilityKernel.Context
            {
                RuntimeBuildings = runtimeBuildings, UnitSpawnPrefabs = prefabs, UnitSpawnPrefabsByKey = byKey,
                MaxQueuedUnitProductions = 4, ProductionSystem = production, ProductionQueueContext = queueContext,
                GetProductionPrefab = (RuntimeDefinition definition, int index) => CanonicalizeProductionPrefab(
                    SourceCampDefinitionReads.GetProductionPrefab(definition, index), canonicalPrefabsBySourceKey),
                TryGetConfiguredUnitReadModel = (int index, out SourceUnitPrefab prefab, out string display,
                    out int price, out bool canRequest, out bool vehicle) =>
                {
                    prefab = null; display = string.Empty; price = 0; canRequest = false; vehicle = false;
                    if (!configuredUnits.TryRead(index, out var actual, out display, out price, out canRequest, out vehicle) ||
                        index < 0 || index >= prefabs.Count || !SamePrefab(prefabs[index], actual)) return false;
                    prefab = prefabs[index]; return true;
                },
                CountPendingProductionsForFaction = (byte faction, string unitId) => CountPending(runtimeBuildings, faction, unitId),
                CountRuntimeProducedUnitsForFaction = (byte faction, string unitId) => CountProduced(runtimeBuildings, faction, unitId)
            };
            var kernel = new SourceCampAvailabilityKernel();
            var dispatcher = new SourceFactionProductionDispatcher(em, runtimeBoundary, delivery, kernel, requestContext);
            dispatcher.BindLegacyS073RequestOwner();
            return new SourceS073LegacyProductionRuntime(em, runtimeBoundary, runtimeBuildings, dispatcher, delivery,
                kernel, requestContext,
                new JObject { ["unit_metadata_path"] = metadataPath, ["unit_metadata_sha256"] = MetadataSha,
                    ["recipe_receipt_path"] = receiptPath, ["recipe_receipt_sha256"] = ReceiptSha,
                    ["historical_production_input_diagnostic"] = historicalInputDiagnostic,
                    ["historical_source_differences"] = historicalSourceDifferences,
                    ["current_production_metadata_equivalence_proven"] = false,
                    ["native_transport_delivery_bound"] = false, ["native_readiness"] = false }, configuredUnits, prefabs, authoredTransportTemplates);
        }

        // Exact known source epochs only. Historical observations remain historical;
        // these digests do not assert current native production or prefab parity.
        internal static void RequireProductionSource(string root, string relative, string captured, bool historical,
            JArray differences, string failurePrefix)
        {
            string actual = HashFile(Path.Combine(root, relative));
            if (captured == actual) return;
            string expectedCaptured = null, expectedCurrent = null;
            switch (relative)
            {
                case "Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs":
                    expectedCaptured = "cf5f9decee267a41f823e37c6dd0573debfadf2a4c5e140d84af475d1b998f4b";
                    expectedCurrent = "589fcf6cb8e229b6177170eb7efea8173601ed9caef4e5eaddde63a02a319a92"; break;
                case "Assets/Game/Scripts/Composition/BuildingProductionUnitMetadataPrefabSystemHelper.cs":
                    expectedCaptured = "3751802f03ef8d9373bc39483cb878820805c0f2b73d3e79b658604509d6dd5a";
                    expectedCurrent = "18a0bedab759457e1e4534c87e98ade7c9387e5e0c8f409200f047c694838205"; break;
                case "Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.cs":
                    expectedCaptured = "2d753d7dc2d870624e68b480e8d69804de8d05828875b9a2681f076783aeff91";
                    expectedCurrent = "314d322040a6e40f251f7a18e63fd233a22912d6dd11fa66e65431eb884b4466"; break;
            }
            Require(historical && differences != null && captured == expectedCaptured && actual == expectedCurrent,
                failurePrefix + relative);
            differences.Add(new JObject { ["path"] = relative, ["captured_sha256"] = captured,
                ["current_sha256"] = actual, ["current_equivalence_proven"] = false });
        }

        internal void Step(float now, float delta)
        {
            if (!float.IsFinite(now) || !float.IsFinite(delta) || delta <= 0) throw new ArgumentOutOfRangeException(nameof(now));
            using (new SourceS073LegacyCatalogScope(em, configuredUnits, configuredPrefabs))
                dispatcher.ProcessLegacyS073Requests(now);
            RequestProcessingUpdates++;
            delivery.Advance(now, delta);
            DeliveryUpdates++;
        }

        internal JObject BindSourceHostTransportDiagnostic(string repositoryRoot,
            SourceS073HostAbsentPresentationCameraCapability cameraAbsence,
            GeneratedReferenceTemplateImporter.Result imported)
        {
            if (cameraAbsence == null || !cameraAbsence.IsValidForSourceHost)
                throw new NotSupportedException("Explicit source-host absent-by-construction camera capability required");
            if (delivery.Helicopters != null)
                throw new InvalidOperationException("Source-host transport diagnostic cannot replace an existing delivery owner");

            RuntimeActorBirthContext.BindS073SourceHostNoNativeBirthRecordProfile(em, imported);

            var projection = S073SourceClearanceProjectionAdapter.Load(repositoryRoot, delivery.World.Grid);
            JObject parity = SourceS073CapturedClearanceAdmission.RequireSevenCaseCapturedProjectionParity(repositoryRoot);
            delivery.Helicopters = new SourceHelicopterDelivery(delivery, transportTemplates,
                cameraKnown: true, hasCamera: false, actualCameraRight: float3.zero,
                actualCameraForward: float3.zero, reserveActualStaticBounds: projection.ReserveCallback,
                useLiveS073ProducerDropPose: true);
            sourceTransportDiagnosticProvenance = new JObject
            {
                ["kind"] = "S073SourceHostTransportDiagnosticBinding",
                ["scope"] = "SourceOnlyDiagnosticSimulation_OriginalDeliveryOwnerWithPinnedCapturedStaticProjection",
                ["camera_absence"] = cameraAbsence.Provenance(),
                ["static_projection_parity"] = parity,
                ["source_transport_delivery_bound"] = true,
                ["native_transport_delivery_bound"] = false,
                ["native_state_copied"] = false,
                ["native_delivery_observed"] = false,
                ["drop_origin"] = new JObject
                {
                    ["kind"] = "S073LiveProducerPoseDiagnosticApproximation",
                    ["scope"] = "OptInSourceHostTransportDropOriginOnly",
                    ["producer_frame"] = "SameEntityManagerRegisteredCombatEntityLiveLocalTransformAtUse",
                    ["slot_source"] = "ReservedProductionSlotIndex_ProductionSpawnLocalPositions",
                    ["missing_frame"] = "FailClosed",
                    ["native_parity"] = false,
                    ["native_source_changed"] = false,
                    ["default_null_instance_branch_changed"] = false
                },
                ["source_host_birth_profile"] = new JObject
                {
                    ["status"] = "Passed",
                    ["scope"] = "ExactLiveS073SourceHostEntityManagerOnly_NotNativeWorldCompleteness",
                    ["imported_prefab_roots"] = imported.Prefabs.Count,
                    ["imported_prefab_entities"] = imported.ImportedEntities,
                    ["imported_prefab_components"] = imported.ImportedComponents,
                    ["live_source_host_entity_count"] = RuntimeActorBirthContext.S073SourceHostLiveEntityCount(em),
                    ["live_source_host_prefab_count"] = RuntimeActorBirthContext.S073SourceHostLivePrefabCount(em),
                    ["inventory_verification"] = "AllQueryFlagsMatchedIndependentEntityManagerStorage",
                    ["live_record_lookup"] = "EntityManager.HasComponentNamed(RuntimeActorBirthRecordFullName)",
                    ["initial_record_count"] = 0,
                    ["future_actor_checks"] = "Per-adoption live lookup"
                },
                ["source_delivery_is_simulated"] = true,
                ["provider_parity_established"] = false,
                ["current_producer_equivalence_established"] = false,
                ["mission_ready"] = false,
                ["mission_outcome_claimed"] = false
            };
            return (JObject)sourceTransportDiagnosticProvenance.DeepClone();
        }

        internal JObject Snapshot()
        {
            if (!em.Exists(boundary) || !em.HasBuffer<BuildingFactionUnitProductionRequest>(boundary))
                throw new NotSupportedException("Same-world original production request buffer required");
            var requests = em.GetBuffer<BuildingFactionUnitProductionRequest>(boundary, true);
            bool sourceTerminalObserved = false;
            string sourcePhase = "Unknown", sourceOutcome = "Unknown";
            using (var matches = em.CreateEntityQuery(typeof(SkirmishMatchState)))
            {
                if (matches.CalculateEntityCount() == 1)
                {
                    SkirmishMatchState match = matches.GetSingleton<SkirmishMatchState>();
                    sourcePhase = match.Phase.ToString();
                    sourceOutcome = match.Outcome.ToString();
                    sourceTerminalObserved = match.Phase == SkirmishPhase.Finished && match.Outcome != SkirmishOutcome.None;
                }
            }
            int pending = 0, succeeded = 0, failed = 0;
            var rows = new JArray();
            foreach (var request in requests)
            {
                pending += request.Status == BuildingFactionUnitProductionRequest.Pending ? 1 : 0;
                succeeded += request.Status == BuildingFactionUnitProductionRequest.Succeeded ? 1 : 0;
                failed += request.Status == BuildingFactionUnitProductionRequest.Failed ? 1 : 0;
                rows.Add(new JObject { ["request_id"] = request.RequestId, ["faction_id"] = request.FactionId,
                    ["unit_id"] = request.UnitId.ToString(), ["status"] = request.Status, ["result_code"] = request.ResultCode,
                    ["producer"] = request.ProducerDisplayName.ToString(), ["unit"] = request.UnitDisplayName.ToString(),
                    ["queue_count"] = request.QueueCount, ["produced_count"] = request.ProducedCount });
            }
            return new JObject { ["kind"] = "S073SameWorldOriginalProductionRequestAndDeliveryOwner",
                ["scope"] = sourceTransportDiagnosticProvenance == null
                    ? "OriginalAIProductionRequests_SharedQueueAndGroundDeliveryOwner_TransportDeliveryFailClosed"
                    : "OriginalAIProductionRequests_SharedQueueAndGroundDeliveryOwner_SourceTransportDiagnosticSimulation",
                ["registered_buildings"] = buildings.Count, ["request_processing_updates"] = RequestProcessingUpdates,
                ["production_spawnpoint_rows"] = em.HasBuffer<BuildingFactionProductionSpawnPointReadModel>(boundary)
                    ? em.GetBuffer<BuildingFactionProductionSpawnPointReadModel>(boundary, true).Length : 0,
                ["delivery_updates"] = DeliveryUpdates, ["delivered_members"] = DeliveredMembers,
                ["authored_transport_template_count"] = transportTemplates.Count,
                ["transport_delivery_bound"] = false,
                ["native_transport_delivery_bound"] = false,
                ["source_transport_delivery_bound"] = delivery.Helicopters != null,
                ["source_transport_diagnostic_provenance"] = sourceTransportDiagnosticProvenance == null
                    ? null : sourceTransportDiagnosticProvenance.DeepClone(),
                ["source_host_terminal_observed"] = sourceTerminalObserved,
                ["source_host_terminal_phase"] = sourcePhase,
                ["source_host_terminal_outcome"] = sourceOutcome,
                ["source_host_terminal_validation"] = "ObservedSourceStateOnly_NotNativeValidation",
                ["request_counts"] = new JObject { ["pending"] = pending, ["succeeded"] = succeeded, ["failed"] = failed },
                ["requests"] = rows,
                ["source_delivery_events"] = new JArray(delivery.DeliveryEvents.Select(value => value.DeepClone())),
                ["source_delivery_events_scope"] = "ObservedOriginalSourceOwnerEvents_NotNativeDeliveryEvidence",
                ["source_input_provenance"] = provenance,
                ["mission_ready"] = false, ["mission_outcome_claimed"] = false };
        }

        static int CountPending(Dictionary<int, RuntimeBuildingEntity> buildings, byte faction, string id) => buildings.Values.Sum(building =>
            building.HasOwnerFaction && building.OwnerFactionId == faction
                ? building.PendingProductions?.Count(item => item.Prefab != null && BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(item.Prefab.name) == id) ?? 0 : 0);
        static int CountProduced(Dictionary<int, RuntimeBuildingEntity> buildings, byte faction, string id) => buildings.Values.Sum(building =>
            building.HasOwnerFaction && building.OwnerFactionId == faction && building.ProducedUnits != null && building.ProducedUnitPrefabs != null
                ? building.ProducedUnits.Count(entity => building.ProducedUnitPrefabs.TryGetValue(entity, out var prefab) &&
                    BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(prefab.name) == id) : 0);
        static bool SamePrefab(SourceUnitPrefab a, SourceUnitPrefab b) => a != null && b != null && a.name == b.name &&
            a.SourceAssetPath == b.SourceAssetPath && a.SourceAssetGuid == b.SourceAssetGuid && a.SourceAssetLocalId == b.SourceAssetLocalId;
        static SourceUnitPrefab CanonicalizeProductionPrefab(SourceUnitPrefab sourcePrefab,
            IReadOnlyDictionary<string, SourceUnitPrefab> canonicalPrefabsBySourceKey)
        {
            if (sourcePrefab == null || !canonicalPrefabsBySourceKey.TryGetValue(sourcePrefab.name, out SourceUnitPrefab canonical))
                return sourcePrefab;
            if (!SamePrefab(sourcePrefab, canonical))
                throw new InvalidDataException("S073 production source key resolves to conflicting asset identity: " + sourcePrefab.name);
            return canonical;
        }
        static bool SameAsset(SourceUnitPrefab prefab, JObject identity) => identity != null && prefab.SourceAssetPath == (string)identity["path"] &&
            prefab.SourceAssetGuid == (string)identity["guid"] && prefab.SourceAssetLocalId == (long?)identity["local_id"];
        static string HashFile(string path) { using (var sha = SHA256.Create()) return string.Concat(sha.ComputeHash(File.ReadAllBytes(path)).Select(b => b.ToString("x2", CultureInfo.InvariantCulture))); }
        static JObject ReadPinnedJson(string path, string expectedSha, int maxBytes)
        {
            byte[] bytes = File.ReadAllBytes(path);
            using (var sha = SHA256.Create()) Require(bytes.Length > 0 && bytes.Length <= maxBytes &&
                string.Concat(sha.ComputeHash(bytes).Select(b => b.ToString("x2", CultureInfo.InvariantCulture))) == expectedSha,
                "Pinned S073 production input changed: " + path);
            return JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes), new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
        }
        static float ReadF32(JToken value, string label)
        {
            Require((string)value?["codec"] == "f32-bits-v1" && ((string)value["bits"])?.Length == 8, "Exact f32 metadata required: " + label);
            float result = BitConverter.ToSingle(BitConverter.GetBytes(uint.Parse((string)value["bits"], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture)), 0);
            Require(!float.IsNaN(result) && !float.IsInfinity(result), "Finite f32 metadata required: " + label); return result;
        }
        static bool ReadBool(JToken value, string label) { Require((string)value?["codec"] == "bool-v1" && value["value"]?.Type == JTokenType.Boolean, "Exact bool metadata required: " + label); return (bool)value["value"]; }
        static int ReadInt(JToken value, string label)
        {
            if ((string)value?["codec"] != "integer-v1" ||
                !int.TryParse((string)value["value"], NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                throw new InvalidDataException("Exact integer metadata required: " + label);
            return result;
        }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
    }

    internal sealed partial class SourceFactionProductionDispatcher
    {
        internal SourceFactionProductionDispatcher(EntityManager manager, Entity runtimeBoundary,
            SourceProductionDeliveryOwner owner, SourceCampAvailabilityKernel requestOwner,
            SourceCampAvailabilityKernel.Context requestContext)
        {
            em = manager; boundary = runtimeBoundary; this.owner = owner; availability = null;
            requestSystem = requestOwner; context = requestContext;
            if (!em.Exists(boundary) || !em.HasComponent<BuildingRuntimeStateTag>(boundary))
                throw new NotSupportedException("Actual S073 runtime production request boundary required");
        }
        internal void BindLegacyS073RequestOwner() { owner.Production.BindSourceFactionQueueOwner(owner); }
        internal void ProcessLegacyS073Requests(float now)
        {
            if (Poisoned || owner.Poisoned) throw new InvalidOperationException("S073 production owner poisoned");
            if (!float.IsFinite(now)) throw new ArgumentException("Finite S073 production clock required");
            try { ProcessProductionRequests(requestSystem, context, readModels, context, em, boundary, now); }
            catch { Poisoned = true; owner.Poisoned = true; throw; }
        }
    }
}
