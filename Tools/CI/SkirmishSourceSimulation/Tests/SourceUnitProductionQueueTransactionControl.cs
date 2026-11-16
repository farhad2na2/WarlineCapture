extern alias ActualCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Simulation;
using Game.Skirmish.Contracts;
using Game.Tactical.Contracts;
using Unity.Entities;
using Unity.Mathematics;
using Newtonsoft.Json.Linq;
using ActualVector3 = ActualCore::UnityEngine.Vector3;

// Explicitly isolated source-host fixture. It exercises the real shared queue
// transaction and SkirmishProductionService, not a mission run or outcome.
internal static class SourceUnitProductionQueueTransactionControl
{
    struct PresentationFreeTransportView : ISharedProductionTransportView<string>
    {
        internal readonly List<string> Calls;
        internal readonly bool Present, HasTransform;
        internal readonly int Mode, Phase;
        internal PresentationFreeTransportView(List<string> calls, bool present, bool hasTransform, int mode, int phase)
        { Calls = calls; Present = present; HasTransform = hasTransform; Mode = mode; Phase = phase; }
        public SharedProductionTransportRead Read(string building) => new SharedProductionTransportRead(Present, HasTransform, Mode, Phase);
        public void Spin(string building, float delta) => Calls.Add("spin");
        public void Arrival(string building, float now) => Calls.Add("arrival");
        public void Delivery(string building, float now, ref uint random) => Calls.Add("delivery");
        public void Departure(string building, float now) => Calls.Add("departure");
        public void Publish() => Calls.Add("publish");
    }

    sealed class CurrentAuthoredRifleProduction
    {
        internal readonly SourceUnitPrefab Rifle;
        internal readonly SourceUnitPrefab Transport;
        internal readonly BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata RifleMetadata;
        internal readonly BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata TransportMetadata;
        internal readonly string DescriptorSha256;
        internal readonly int RecipeQuantity;

        CurrentAuthoredRifleProduction(SourceUnitPrefab rifle, SourceUnitPrefab transport,
            BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata rifleMetadata,
            BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata transportMetadata,
            string descriptorSha256, int recipeQuantity)
        { Rifle = rifle; Transport = transport; RifleMetadata = rifleMetadata; TransportMetadata = transportMetadata; DescriptorSha256 = descriptorSha256; RecipeQuantity = recipeQuantity; }

        internal static CurrentAuthoredRifleProduction LoadCurrent(string path, string expectedSha256)
        {
            string root = FindRepositoryRoot();
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path) ||
                !System.Text.RegularExpressions.Regex.IsMatch(expectedSha256 ?? string.Empty, "^[0-9a-f]{64}$"))
                throw new InvalidDataException("Explicit absolute current descriptor path and SHA-256 required");
            path = Path.GetFullPath(path);
            if (Sha256File(path) != expectedSha256)
                throw new InvalidDataException("Explicit authored descriptor SHA-256 mismatch");
            byte[] bytes = File.ReadAllBytes(path);
            string descriptorSha = Sha256(bytes);
            JObject document = JObject.Parse(Encoding.UTF8.GetString(bytes));
            Validate(document, root);
            return Decode(document, descriptorSha);
        }

        static string FindRepositoryRoot()
        {
            for (DirectoryInfo directory = new DirectoryInfo(Environment.CurrentDirectory); directory != null; directory = directory.Parent)
                if (Directory.Exists(Path.Combine(directory.FullName, "Assets", "Game", "Scripts")) &&
                    File.Exists(Path.Combine(directory.FullName, "Tools", "CI", "SkirmishSourceSimulation", "Recipes", "generate_barrack_rifle_source_descriptor.py")))
                    return directory.FullName;
            throw new DirectoryNotFoundException("WarlineCapture repository root not found from fixture working directory");
        }

        static string Sha256(byte[] bytes)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
        }

        static string Sha256File(string path) => Sha256(File.ReadAllBytes(path));

        static void Need(bool condition, string message)
        {
            if (!condition) throw new InvalidDataException(message);
        }

        static string Text(JToken token, string path) => token?.Type == JTokenType.String
            ? (string)token : throw new InvalidDataException("Missing source descriptor text: " + path);
        static int Int(JToken token, string path) => token?.Type == JTokenType.Integer
            ? checked((int)token) : throw new InvalidDataException("Missing source descriptor integer: " + path);
        static long Long(JToken token, string path) => token?.Type == JTokenType.Integer
            ? (long)token : throw new InvalidDataException("Missing source descriptor long: " + path);
        static float Float(JToken token, string path) => token?.Type == JTokenType.Float || token?.Type == JTokenType.Integer
            ? (float)token : throw new InvalidDataException("Missing source descriptor number: " + path);
        static bool Bool(JToken token, string path) => token?.Type == JTokenType.Boolean
            ? (bool)token : throw new InvalidDataException("Missing source descriptor bool: " + path);

        static string NormalizePath(string value)
        {
            Need(!string.IsNullOrWhiteSpace(value) && !Path.IsPathRooted(value) && !value.Contains("\\") &&
                 Array.TrueForAll(value.Split('/'), part => part.Length > 0 && part != "." && part != ".."),
                 "Unsafe descriptor source path");
            return value;
        }

        static void VerifySourcePins(JObject document, string root)
        {
            JObject pins = document["source_pins"] as JObject ?? throw new InvalidDataException("Descriptor source pins missing");
            // The current descriptor generator emits 46 exact asset/meta/source pins, including the separately authored transport identity and its pinned variant base. Keep this exact count bounded; every entry below is path-safe and SHA-256 verified.
            Need(pins.Count == 46, "Exact 46-path current descriptor source-pin set required");
            foreach (JProperty pin in pins.Properties())
            {
                string relative = NormalizePath(pin.Name);
                string expected = Text(pin.Value, "source_pins." + pin.Name);
                Need(expected.Length == 64 && expected.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f'), "Invalid source hash");
                string path = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
                Need(path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.Ordinal), "Pinned path escaped repository");
                for (DirectoryInfo parent = new FileInfo(path).Directory; parent != null && parent.FullName.StartsWith(root, StringComparison.Ordinal); parent = parent.Parent)
                    Need((File.GetAttributes(parent.FullName) & FileAttributes.ReparsePoint) == 0, "Reparse point in pinned source path");
                Need(File.Exists(path) && Sha256File(path) == expected, "Current source pin changed: " + relative);
            }
            foreach (string required in new[]
            {
                "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config.asset",
                "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config.asset.meta",
                "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab",
                "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab.meta",
                "Assets/Game/Prefabs/Characters/Unit.prefab",
                "Assets/Game/Prefabs/Characters/Unit.prefab.meta",
                "Assets/Game/Prefabs/Vehicles/Unit_Veh.prefab",
                "Assets/Game/Prefabs/Vehicles/Unit_Veh.prefab.meta",
                "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Helicopter_Transport_Config.asset",
                "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Helicopter_Transport_Config.asset.meta",
                "Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Transport.prefab",
                "Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Transport.prefab.meta",
                "Assets/Game/Scripts/Composition/BuildingProductionUnitMetadataPrefabSystemHelper.cs",
                "Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.TransportMetadata.cs",
                "Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs"
            })
                Need(pins[required] != null, "Required original resolver source pin missing: " + required);
        }

        static string MetaGuid(string path)
        {
            string text = File.ReadAllText(path);
            var matches = System.Text.RegularExpressions.Regex.Matches(text, "^guid: ([0-9a-f]{32})$", System.Text.RegularExpressions.RegexOptions.Multiline);
            Need(matches.Count == 1, "Expected one GUID in pinned asset meta: " + path);
            return matches[0].Groups[1].Value;
        }

        static void Validate(JObject document, string root)
        {
            Need(Int(document["schema_version"], "schema_version") == 1 &&
                 Text(document["kind"], "kind") == "BarrackRifleCurrentAuthoredSourceFacts" &&
                 Text(document["status"], "status") == "PassedSourceOnlyDescriptor" &&
                 Text(document["authority"], "authority") == "Current checked-in Unity text assets and pinned source files only",
                 "Unsupported current authored descriptor qualification");
            VerifySourcePins(document, root);
            JObject facts = document["facts"] as JObject ?? throw new InvalidDataException("Descriptor facts missing");
            JObject rifle = facts["unit"] as JObject ?? throw new InvalidDataException("Rifle facts missing");
            JObject riflePrefab = rifle["prefab"] as JObject ?? throw new InvalidDataException("Rifle prefab identity missing");
            JObject rifleTransport = rifle["transport"] as JObject ?? throw new InvalidDataException("Rifle transport reference missing");
            JObject recipe = ((JArray)facts["producer"]["recipes"])[0] as JObject ?? throw new InvalidDataException("Barrack rifle recipe missing");
            JObject recipePrefab = recipe["prefab"] as JObject ?? throw new InvalidDataException("Barrack recipe prefab missing");
            JObject transport = facts["transport_unit"] as JObject ?? throw new InvalidDataException("Transport facts missing");
            JObject transportPrefab = transport["prefab"] as JObject ?? throw new InvalidDataException("Transport prefab identity missing");
            JObject transportConfig = transport["config"] as JObject ?? throw new InvalidDataException("Transport config identity missing");
            JArray rifleFootprint = rifle["footprint_cells"] as JArray ?? throw new InvalidDataException("Rifle footprint pair missing");
            JArray transportFootprint = transport["footprint_cells"] as JArray ?? throw new InvalidDataException("Transport footprint pair missing");
            Need(rifleFootprint.Count == 2 && transportFootprint.Count == 2,
                "Exact two-axis source footprint pairs required");
            Need(Text(rifle["key"], "unit.key") == "Unit_Chr_Soldier_Male_02_Alt_04" &&
                 Text(riflePrefab["path"], "unit.prefab.path") == "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab" &&
                 Text(riflePrefab["guid"], "unit.prefab.guid") == MetaGuid(Path.Combine(root, "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab.meta")) &&
                 Int(recipe["index"], "producer.recipes[0].index") == 0 &&
                 Text(recipe["unit_key"], "producer.recipes[0].unit_key") == Text(rifle["key"], "unit.key") &&
                 Text(recipePrefab["guid"], "recipe.prefab.guid") == Text(riflePrefab["guid"], "unit.prefab.guid") &&
                 Long(recipePrefab["local_id"], "recipe.prefab.local_id") == Long(riflePrefab["local_id"], "unit.prefab.local_id") &&
                 Int(recipe["quantity"], "producer.recipes[0].quantity") == 4,
                 "Barrack recipe and Rifle authored asset identity disagree");
            Need(Text(rifleTransport["path"], "unit.transport.path") == "Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Transport.prefab" &&
                 Text(rifleTransport["guid"], "unit.transport.guid") == MetaGuid(Path.Combine(root, "Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Transport.prefab.meta")) &&
                 Long(rifleTransport["local_id"], "unit.transport.local_id") == 5616646512338707422L &&
                 Text(transportPrefab["path"], "transport_unit.prefab.path") == Text(rifleTransport["path"], "unit.transport.path") &&
                 Text(transportPrefab["guid"], "transport_unit.prefab.guid") == Text(rifleTransport["guid"], "unit.transport.guid") &&
                 Long(transportPrefab["local_id"], "transport_unit.prefab.local_id") == Long(rifleTransport["local_id"], "unit.transport.local_id") &&
                 Text(rifleTransport["config_path"], "unit.transport.config_path") == Text(transportConfig["path"], "transport_unit.config.path") &&
                 Text(rifleTransport["config_guid"], "unit.transport.config_guid") == Text(transportConfig["guid"], "transport_unit.config.guid") &&
                 Text(transportConfig["guid"], "transport_unit.config.guid") == MetaGuid(Path.Combine(root, Text(transportConfig["path"], "transport_unit.config.path") + ".meta")),
                 "Rifle transport GUID/fileID/config provenance mismatch");

            string rifleConfigPath = Path.Combine(root, "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config.asset");
            string rifleConfig = File.ReadAllText(rifleConfigPath);
            Need(System.Text.RegularExpressions.Regex.IsMatch(rifleConfig,
                "^\\s*productionTransportPrefab: \\{fileID: 5616646512338707422, guid: " + Text(rifleTransport["guid"], "unit.transport.guid") + ", type: 3\\}$",
                System.Text.RegularExpressions.RegexOptions.Multiline), "Rifle config transport GUID/fileID binding changed");
            string transportPrefabPath = Path.Combine(root, Text(transportPrefab["path"], "transport_unit.prefab"));
            string transportPrefabText = File.ReadAllText(transportPrefabPath);
            Need(System.Text.RegularExpressions.Regex.IsMatch(transportPrefabText,
                "propertyPath: config\\s*\\n\\s+value:\\s*\\n\\s+objectReference: \\{fileID: 11400000, guid: " + Text(transportConfig["guid"], "transport_unit.config.guid") + ", type: 2\\}",
                System.Text.RegularExpressions.RegexOptions.Multiline), "Transport prefab/config binding changed");

            Need(Float(rifle["production_duration_seconds"], "unit.production_duration_seconds") == 5f &&
                 Bool(rifle["is_air_unit"], "unit.is_air_unit") == false &&
                 Float(rifle["transport_arrival_seconds"], "unit.transport_arrival_seconds") == 5f &&
                 Float(rifle["transport_hold_for_next_ready_seconds"], "unit.transport_hold_for_next_ready_seconds") == 4f &&
                 Int(rifle["transport_max_concurrent"], "unit.transport_max_concurrent") == 1 &&
                 Bool(rifle["transport_requires_airport_runway"], "unit.transport_requires_airport_runway") == false &&
                 Bool(rifle["transport_uses_runway_landing"], "unit.transport_uses_runway_landing") == false &&
                 Int(rifleFootprint[0], "unit.footprint_cells.x") == 1 &&
                 Int(rifleFootprint[1], "unit.footprint_cells.y") == 1 &&
                 Float(transport["production_duration_seconds"], "transport.production_duration_seconds") == 5f &&
                 Bool(transport["is_air_unit"], "transport.is_air_unit") &&
                 transport["production_transport_prefab"]?.Type == JTokenType.Null &&
                 Float(transport["production_transport_arrival_seconds"], "transport.arrival") == 5f &&
                 Float(transport["production_transport_hold_for_next_ready_seconds"], "transport.hold") == 4f &&
                 Int(transport["production_transport_max_concurrent"], "transport.max_concurrent") == 2 &&
                 Bool(transport["production_transport_requires_airport_runway"], "transport.requires_runway") == false &&
                 Bool(transport["production_transport_uses_runway_landing"], "transport.uses_runway_landing") == false &&
                 Int(transportFootprint[0], "transport.footprint_cells.x") == 1 &&
                 Int(transportFootprint[1], "transport.footprint_cells.y") == 1 &&
                 Bool(transport["auto_calculate_footprint"], "transport.auto_calculate_footprint") == false,
                 "Original UnitGridAuthoring metadata facts changed");
            JObject scope = document["scope"] as JObject ?? throw new InvalidDataException("Descriptor scope missing");
            Need(Bool(scope["native_factory_invoked"], "scope.native_factory_invoked") == false &&
                 Bool(scope["runtime_admission"], "scope.runtime_admission") == false &&
                 Bool(scope["mission_ready"], "scope.mission_ready") == false,
                 "Source-only descriptor cannot claim native admission or mission readiness");
        }

        static CurrentAuthoredRifleProduction Decode(JObject document, string descriptorSha)
        {
            JObject rifle = (JObject)document["facts"]["unit"];
            JObject transport = (JObject)document["facts"]["transport_unit"];
            SourceUnitPrefab riflePrefab = SourceUnitPrefab.FromAssetIdentity(
                (string)rifle["key"], (string)rifle["prefab"]["path"], (string)rifle["prefab"]["guid"], (long)rifle["prefab"]["local_id"]);
            SourceUnitPrefab transportPrefab = SourceUnitPrefab.FromAssetIdentity(
                (string)transport["key"], (string)transport["prefab"]["path"], (string)transport["prefab"]["guid"], (long)transport["prefab"]["local_id"]);
            var rifleMetadata = new BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata(
                (float)rifle["production_duration_seconds"], transportPrefab, (bool)rifle["is_air_unit"],
                (float)rifle["transport_arrival_seconds"], (float)rifle["transport_hold_for_next_ready_seconds"],
                (int)rifle["transport_max_concurrent"], (bool)rifle["transport_requires_airport_runway"],
                (bool)rifle["transport_uses_runway_landing"], new ActualCore::UnityEngine.Vector2Int((int)rifle["footprint_cells"][0], (int)rifle["footprint_cells"][1]));
            var transportMetadata = new BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata(
                (float)transport["production_duration_seconds"], null, (bool)transport["is_air_unit"],
                (float)transport["production_transport_arrival_seconds"], (float)transport["production_transport_hold_for_next_ready_seconds"],
                (int)transport["production_transport_max_concurrent"], (bool)transport["production_transport_requires_airport_runway"],
                (bool)transport["production_transport_uses_runway_landing"], new ActualCore::UnityEngine.Vector2Int((int)transport["footprint_cells"][0], (int)transport["footprint_cells"][1]));
            return new CurrentAuthoredRifleProduction(riflePrefab, transportPrefab, rifleMetadata, transportMetadata,
                descriptorSha, (int)document["facts"]["producer"]["recipes"][0]["quantity"]);
        }

        internal static void CheckNegativeControls(string path)
        {
            string root = FindRepositoryRoot();
            JObject baseline = JObject.Parse(File.ReadAllText(path));
            var wrongLocalId = (JObject)baseline.DeepClone();
            wrongLocalId["facts"]["unit"]["transport"]["local_id"] = 5616646512338707423L;
            ExpectRejected(wrongLocalId, root, "wrong transport fileID");
            var wrongTransportMaximum = (JObject)baseline.DeepClone();
            wrongTransportMaximum["facts"]["transport_unit"]["production_transport_max_concurrent"] = 1;
            ExpectRejected(wrongTransportMaximum, root, "wrong transport maxConcurrent");
            var wrongPin = (JObject)baseline.DeepClone();
            wrongPin["source_pins"]["Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Helicopter_Transport_Config.asset"] = new string('0', 64);
            ExpectRejected(wrongPin, root, "changed transport config source hash");
        }

        static void ExpectRejected(JObject document, string root, string label)
        {
            bool rejected = false;
            try { Validate(document, root); }
            catch (InvalidDataException) { rejected = true; }
            Require(rejected, "Focused source metadata control must reject " + label);
        }
    }

    sealed class Fixture
    {
        internal readonly EntityManager Em;
        internal readonly Entity Session;
        internal readonly Entity ProducerEntity;
        internal readonly RuntimeBuildingEntity Building;
        internal readonly SourceUnitPrefab Unit;
        internal readonly SkirmishRoleOverlay Role;
        internal readonly SkirmishResolvedSetup Setup;
        internal readonly BuildingProductionQueueCompositionSystemHelper Production;
        internal readonly BuildingProductionQueueCompositionSystemHelper.QueueContext Context;
        internal readonly SourceProductionDeliveryOwner Owner;
        internal int ProductionMetadataResolverCalls;
        internal SourceUnitPrefab LastResolvedMetadataPrefab;
        internal BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata LastResolvedMetadata;

        internal Fixture(int materials) : this(materials, null) { }

        internal Fixture(int materials, CurrentAuthoredRifleProduction authored)
        {
            int nextEntityIndex = 1;
            Em = new EntityManager
            {
                LogicalAllocatorAdmitted = true,
                AllocateLogicalIdentity = () => new Entity { Index = nextEntityIndex++, Version = 1 }
            };
            var overlays = SkirmishRoleOverlayCatalog.CreateS002GroundSlice();
            Role = Array.Find(overlays, value => value.RoleKind == SkirmishRoleKind.Rifle);
            Require(Role.RoleKind == SkirmishRoleKind.Rifle, "Actual compiled rifle overlay required");
            Setup = new SkirmishResolvedSetup
            {
                CatalogId = "S002-UNIT-QUEUE-FIXTURE",
                ArmyProfileId = SkirmishArmyProfileId.GroundManeuver,
                Readiness = SkirmishReadinessStage.FullArsenal,
                RoleOverlays = overlays,
                MaterialsEach = materials,
                MaterialsCapacityEach = Math.Max(materials, 1),
                InfantryCapEach = 64,
                GroundCapEach = 64,
                TacticalAirCapEach = 64,
                SupplyCapEach = 256
            };
            string unitKey = SkirmishRoleCatalogConfig.RuntimePrefabKey(Role.RoleKind);
            if (authored != null)
            {
                Require(authored.Rifle.name == unitKey && authored.RecipeQuantity == Role.SquadMembers,
                    "Current authored Rifle identity and Barrack recipe must match compiled role quantity");
                Unit = authored.Rifle;
            }
            else Unit = new SourceUnitPrefab(unitKey, Em.CreateEntity());
            Session = Em.CreateEntity();
            var sessionId = new Unity.Collections.FixedString64Bytes("unit-queue-fixture");
            Em.AddComponentData(Session, new SkirmishExpandedSessionComponent
            {
                SessionId = sessionId,
                Phase = SkirmishSessionPhase.Playing,
                SpawnComplete = 1
            });
            Em.AddComponentObject(Session, new SkirmishResolvedSetupRecord { Setup = Setup });
            var bindings = new Dictionary<string, SkirmishRoleOverlay>(StringComparer.Ordinal)
            {
                [unitKey] = Role
            };
            var catalog = new SkirmishProductionCatalog(Setup, bindings, true);
            Em.AddComponentObject(Session, new SkirmishProductionCatalogRecord { Catalog = catalog });
            Em.AddComponentData(Session, new SkirmishEconomyStockComponent
            {
                FactionId = 1,
                Materials = materials,
                MaterialsCapacity = Math.Max(materials, 1),
                Fuel = 100,
                FuelCapacity = 100
            });
            Em.AddComponentData(Session, new SkirmishCapacityComponent
            {
                FactionId = 1,
                InfantryCap = 64,
                GroundCap = 64,
                AirCap = 64,
                SupplyCap = 256
            });
            Em.AddBuffer<SkirmishProductionReservation>(Session);

            ProducerEntity = Em.CreateEntity();
            Em.AddComponentData(ProducerEntity, new SkirmishAttemptOwnedComponent
            {
                SessionId = sessionId,
                StableObjectId = new Unity.Collections.FixedString64Bytes("fixture.barracks"),
                FactionId = 1,
                IsStructure = 1
            });
            Em.AddComponentData(ProducerEntity, new SkirmishStructureIdentityComponent
            {
                StructureId = new Unity.Collections.FixedString64Bytes(SkirmishStructureIds.Barracks),
                Producer = SkirmishProducerKind.Barracks
            });
            Em.AddComponentData(ProducerEntity, new UnitHealth { Current = 100, Max = 100 });

            var producerPrefab = new SourceUnitPrefab(
                SkirmishProductionCatalog.ProducerPrefabKey(Role.Producer), Em.CreateEntity());
            var definition = new RuntimeDefinition
            {
                SourceMetadataComplete = true,
                Prefab = producerPrefab,
                ProductionSlots = new List<RuntimeDefinition.ProductionSlotDefinition>
                {
                    new RuntimeDefinition.ProductionSlotDefinition { SpawnUnitPrefab = Unit, Quantity = Role.SquadMembers }
                }
            };
            Building = new RuntimeBuildingEntity
            {
                Id = 1001,
                CombatEntity = ProducerEntity,
                HasOwnerFaction = true,
                OwnerFactionId = 1,
                InitialProducerCollectionsKnown = true,
                PendingProductions = new List<SourcePendingProduction>(),
                Definition = definition
            };

            Production = new BuildingProductionQueueCompositionSystemHelper();
            Production.ConfigureUnitProductionMetadataResolver((SourceUnitPrefab prefab,
                out BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata metadata) =>
            {
                if (authored == null && ReferenceEquals(prefab, Unit))
                {
                    metadata = new BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata(
                        30f, null, false, 0f, 0f, 1, false, false, new ActualCore::UnityEngine.Vector2Int(1, 1));
                    return true;
                }
                if (authored != null && ReferenceEquals(prefab, authored.Rifle)) { metadata = authored.RifleMetadata; ProductionMetadataResolverCalls++; LastResolvedMetadataPrefab = prefab; LastResolvedMetadata = metadata; return true; }
                if (authored != null && ReferenceEquals(prefab, authored.Transport)) { metadata = authored.TransportMetadata; ProductionMetadataResolverCalls++; LastResolvedMetadataPrefab = prefab; LastResolvedMetadata = metadata; return true; }
                metadata = default;
                return false;
            });
            Production.ConfigureProductionRecipes(new[]
            {
                new BuildingProductionRecipe { UnitPrefab = Unit, ProducerPrefab = producerPrefab, Quantity = Role.SquadMembers }
            });
            Context = new BuildingProductionQueueCompositionSystemHelper.QueueContext(
                null, null, new BuildingProductionSlotUtilitySystemHelper(), null, null, null);

            Entity grid = Em.CreateEntity();
            Em.AddComponentData(grid, new GridConfig { Width = 1, Height = 1, CellSize = 1f });
            var walkable = Em.AddBuffer<GridWalkable>(grid);
            walkable.Add(new GridWalkable { Value = 1 });
            Em.AddComponentData(grid, new DynamicBlockerComponent());
            Em.AddComponentData(grid, new DynamicOccupancyComponent());
            var buildings = new Dictionary<int, RuntimeBuildingEntity> { [Building.Id] = Building };
            var roots = new Dictionary<int, SourceProducerRootFrame>
            {
                [Building.Id] = new SourceProducerRootFrame(float3.zero, quaternion.identity, 1f)
            };
            var world = new SourceProductionGroundWorld(Em, grid, Entity.Null, buildings, roots);
            Owner = new SourceProductionDeliveryOwner(world, Production, Context,
                new Dictionary<int, SourceProductionDeliveryRecipe[]>
                {
                    [Building.Id] = new[] { new SourceProductionDeliveryRecipe(0, Unit, Role.SquadMembers, default) }
                }, 4, 1);
        }
    }

    sealed class ThrowAfterMutatingList : IList<BuildingProductionQueueCompositionSystemHelper.IPendingProduction>
    {
        readonly List<BuildingProductionQueueCompositionSystemHelper.IPendingProduction> rows = new();
        readonly BuildingProductionQueueCompositionSystemHelper.IPendingProduction prior;
        internal ThrowAfterMutatingList(BuildingProductionQueueCompositionSystemHelper.IPendingProduction existing)
        {
            prior = existing;
            rows.Add(existing);
        }
        public int Count => rows.Count;
        public bool IsReadOnly => false;
        public BuildingProductionQueueCompositionSystemHelper.IPendingProduction this[int index] { get => rows[index]; set => rows[index] = value; }
        public void Add(BuildingProductionQueueCompositionSystemHelper.IPendingProduction item)
        {
            rows.Add(item);
            // Inject a post-reservation enqueue fault after corrupting an already
            // queued timeline entry. The real transaction must restore both.
            prior.StartedAt = 123f;
            prior.ReadyAt = 456f;
            throw new InvalidOperationException("fixture enqueue failure after mutation");
        }
        public void Clear() => rows.Clear();
        public bool Contains(BuildingProductionQueueCompositionSystemHelper.IPendingProduction item) => rows.Contains(item);
        public void CopyTo(BuildingProductionQueueCompositionSystemHelper.IPendingProduction[] array, int arrayIndex) => rows.CopyTo(array, arrayIndex);
        public IEnumerator<BuildingProductionQueueCompositionSystemHelper.IPendingProduction> GetEnumerator() => rows.GetEnumerator();
        public int IndexOf(BuildingProductionQueueCompositionSystemHelper.IPendingProduction item) => rows.IndexOf(item);
        public void Insert(int index, BuildingProductionQueueCompositionSystemHelper.IPendingProduction item) => rows.Insert(index, item);
        public bool Remove(BuildingProductionQueueCompositionSystemHelper.IPendingProduction item) => rows.Remove(item);
        public void RemoveAt(int index) => rows.RemoveAt(index);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    sealed class SemanticProducer { internal int Id; }

    sealed class SemanticOwnerFixtureAdapter : SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.IAdapter<int, SemanticProducer, SourcePendingProduction>
    {
        internal readonly List<string> Calls = new();
        internal SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure Evaluation;
        internal bool HasProducer = true, QueueCapacity = true, ProducerLookupAccepted = true, MissionReceipt, QueueAccepted = true;
        internal bool HasOperationMapProducer, OperationQueueAccepted = true;
        internal SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.QueueOutcome QueueResult =
            SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.QueueOutcome.ProducerQueueFull;
        internal SemanticProducer Producer = new() { Id = 4001 };
        internal bool ProducerIdentityPreserved = true;
        internal bool UseOriginalResourceOwner;
        internal int CreditsCost = 3, MaterialsCost = 5;
        internal FactionEconomy Economy;
        internal FactionTacticalMaterialsComponent Materials;
        public SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure Evaluate(int prefab, int price, out string required)
        { Calls.Add("evaluate"); required = string.Empty; return Evaluation; }
        public bool IsConfiguredSpawnable(int prefab) { Calls.Add("configured"); return false; }
        public bool BeginConfiguredPlacement(int prefab) { Calls.Add("placement"); return false; }
        public bool HasGlobalQueueCapacity() { Calls.Add("global_capacity"); return true; }
        public bool TryResolveCosts(int prefab, int fallback, out int credits, out int materials)
        { Calls.Add("resolve_costs"); credits = CreditsCost; materials = MaterialsCost; return true; }
        public bool TryFindProducer(int prefab, bool requireQueueCapacity, out int buildingId, out int productionIndex, out string displayName)
        {
            Calls.Add(requireQueueCapacity ? "producer_capacity" : "producer_any");
            buildingId = HasProducer && (!requireQueueCapacity || QueueCapacity) ? Producer.Id : 0;
            productionIndex = buildingId == 0 ? -1 : 2;
            displayName = buildingId == 0 ? string.Empty : "Fixture producer";
            return buildingId != 0;
        }
        public bool TryGetRequiredProducerDisplayName(int prefab, out string displayName)
        { Calls.Add("required_name"); displayName = "Required producer"; return true; }
        public bool TryFindOperationMapProducer(int prefab) { Calls.Add("operation_map_probe"); return HasOperationMapProducer; }
        public bool HasGlobalQueueCapacityAfterOperationMapFailure() { Calls.Add("global_capacity_after_operation_failure"); return true; }
        public SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure Spend(int credits, int materials)
        {
            Calls.Add("spend");
            if (UseOriginalResourceOwner && FactionConstructionResourceUtilitySystemHelper.TrySpend(
                    ref Economy, ref Materials, credits, materials) != FactionConstructionResourceMutationResult.Applied)
                return SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.InsufficientCredits;
            return SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.None;
        }
        public void Restore(int credits, int materials)
        {
            Calls.Add("restore");
            if (UseOriginalResourceOwner && FactionConstructionResourceUtilitySystemHelper.TryRollback(
                    ref Economy, ref Materials, credits, materials) != FactionConstructionResourceMutationResult.Applied)
                throw new InvalidOperationException("Original fixture resource rollback failed");
        }
        public bool TryQueueOperationMap(int prefab, float now) { Calls.Add("operation_map_queue"); return OperationQueueAccepted; }
        public bool IsMissionReceipt() { Calls.Add("mission_receipt"); return MissionReceipt; }
        public bool TryGetProducer(int buildingId, out SemanticProducer producer)
        { Calls.Add("producer_lookup"); producer = Producer; return ProducerLookupAccepted && buildingId == Producer.Id; }
        public void FocusProducer(SemanticProducer producer, int prefab)
        { Calls.Add("focus"); ProducerIdentityPreserved &= ReferenceEquals(Producer, producer); }
        public SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.QueueOutcome TryQueueUnit(SemanticProducer producer, int productionIndex, int frameCount, float now,
            out SourcePendingProduction pending)
        { Calls.Add("queue"); ProducerIdentityPreserved &= ReferenceEquals(Producer, producer); pending = new SourcePendingProduction(); return QueueAccepted ? SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.QueueOutcome.Queued : QueueResult; }
        public void AdjustAcceptedPending(SemanticProducer producer, SourcePendingProduction pending, int credits, int materials)
        { Calls.Add("adjust_pending"); ProducerIdentityPreserved &= ReferenceEquals(Producer, producer); }
        public void RecordUnitOrdered(int prefab) => Calls.Add("record_ordered");
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    static int Materials(Fixture f) => SkirmishMaterialsService.Read(f.Em, f.Session, 1);

    static bool SelectFixtureProducer(
        SourceCampAvailabilityKernel.UnitProducerCandidate[] candidates,
        bool requireQueueCapacity,
        out SourceCampAvailabilityKernel.UnitProducerCandidate selected)
    {
        for (int pass = 0; pass < 2; pass++)
        {
            foreach (var candidate in candidates)
                if (SourceCampAvailabilityKernel.SelectUnitProducerCandidate(
                        candidate, pass, requireQueueCapacity, out selected))
                    return true;
        }

        selected = default;
        return false;
    }

    static void CheckSharedProducerSelection()
    {
        // Isolated typed facts exercise the exact extracted selector used by
        // both native request evaluation and SourceCampAvailabilityKernel.
        var candidates = new[]
        {
            new SourceCampAvailabilityKernel.UnitProducerCandidate
                { Pass = 1, BuildingId = 30, ProductionIndex = 2, DisplayName = "neutral", HasQueueCapacity = true },
            new SourceCampAvailabilityKernel.UnitProducerCandidate
                { Pass = 0, BuildingId = 10, ProductionIndex = 0, DisplayName = "player-full", HasQueueCapacity = false },
            new SourceCampAvailabilityKernel.UnitProducerCandidate
                { Pass = 0, BuildingId = 20, ProductionIndex = 1, DisplayName = "player-open", HasQueueCapacity = true }
        };

        Require(SelectFixtureProducer(candidates, false, out var first) &&
                first.BuildingId == 10 && first.ProductionIndex == 0 && first.DisplayName == "player-full",
            "Original first player producer must win when queue capacity is not required");
        Require(SelectFixtureProducer(candidates, true, out var open) &&
                open.BuildingId == 20 && open.ProductionIndex == 1 && open.DisplayName == "player-open",
            "Queue-required selection must skip full player producer before neutral fallback");
        Require(!SelectFixtureProducer(Array.Empty<SourceCampAvailabilityKernel.UnitProducerCandidate>(), true, out var missing) &&
                missing.Equals(default(SourceCampAvailabilityKernel.UnitProducerCandidate)),
            "Missing producer must fail with original cleared outputs");
    }

    static void CheckSharedLanePolicyUnityAlias()
    {
        CheckLanePolicy(false, default, 0, 2, new float3(0f, 0f, -6f));
        CheckLanePolicy(false, default, 1, 2, new float3(0f, 0f, 6f));
        CheckLanePolicy(false, default, 0, 0, float3.zero);
        CheckLanePolicy(true, new ActualVector3(0f, 1f, 0f), 1, 2, new float3(0f, 0f, 6f));
        CheckLanePolicy(true, new ActualVector3(3f, 4f, 4f), 1, 2, new float3(3.6f, 0f, 4.8f));
        CheckLanePolicy(true, new ActualVector3(0f, 0f, 0.005f), 0, 2, new float3(0f, 0f, -6f));
        CheckLanePolicy(true, new ActualVector3(float.NaN, 0f, 0f), 1, 2, new float3(0f, 0f, 6f));
        CheckLanePolicy(true, new ActualVector3(float.PositiveInfinity, 0f, 0f), 1, 2, float3.zero);
    }

    static void CheckPresentationFreeDropTimingPolicy()
    {
        // This fixture exercises only the common clock. It intentionally has
        // no transport prefab, PoseGraph, DropStart/DropEnd, or actor state.
        var regular = SharedProductionDeliveryKernel.BeginDropTiming(10f, 2f);
        CheckDropProgress(regular, 9f, 0f, "pre-start time must clamp to zero");
        CheckDropProgress(regular, 10f, 0f, "drop start is zero progress");
        CheckDropProgress(regular, 11f, 0.5f, "half duration is half progress");
        CheckDropProgress(regular, 12f, 1f, "duration boundary is complete");
        CheckDropProgress(regular, 13f, 1f, "post-duration time remains complete");

        // The original transport path floors the denominator at .01 seconds.
        var zero = SharedProductionDeliveryKernel.BeginDropTiming(20f, 0f);
        CheckDropProgress(zero, 20f, 0f, "zero duration starts at zero progress");
        CheckDropProgress(zero, 20.005f, 0.5f, "zero duration uses the .01-second floor");
        CheckDropProgress(zero, 20.01f, 1f, "zero duration completes at the .01-second floor");
        var negative = SharedProductionDeliveryKernel.BeginDropTiming(30f, -1f);
        CheckDropProgress(negative, 30.01f, 1f, "negative duration preserves the .01-second floor");

        var helicopter = new SharedProductionDeliveryKernel.Transport { Phase = 1, NextDropReadyAt = 40f };
        SharedProductionDeliveryKernel.BeginHelicopterDropTiming(ref helicopter, 40f);
        Require(helicopter.HasDrop && helicopter.DropStartedAt == 40f && helicopter.DropDuration == 2f &&
                helicopter.DropStart.Equals(float3.zero) && helicopter.DropEnd.Equals(float3.zero) && helicopter.DropPosition.Equals(float3.zero),
            "Timing-only helicopter admission must not invent drop positions");
        Require(math.abs(SharedProductionDeliveryKernel.AdvanceDropTiming(ref helicopter, 41f) - 0.5f) < 0.00001f &&
                SharedProductionDeliveryKernel.AdvanceDropTiming(ref helicopter, 42f) == 1f,
            "Timing-only helicopter progress must use the same shared two-second drop clock");
        SharedProductionDeliveryKernel.FinishDrop(ref helicopter, 42f, true);
        Require(!helicopter.HasDrop && helicopter.Delivered == 1 && helicopter.NextDropReadyAt == 42f,
            "Timing-only completion must retain shared delivery count and next-drop state");

        var phase = SharedProductionDeliveryKernel.HelicopterPhaseTiming(50f, 5f, 4f);
        phase.Phase = 1;
        phase.HoverEnteredAt = 55f;
        phase.NextDropReadyAt = 55f;
        var timingOnlyDrop = default(SharedProductionDeliveryKernel.DropTiming);
        bool hasTimingOnlyDrop = false;
        int timingOnlyDelivered = 0;
        SharedProductionDeliveryKernel.BeginHelicopterDropTiming(ref phase, ref timingOnlyDrop, ref hasTimingOnlyDrop, 55f);
        Require(hasTimingOnlyDrop && timingOnlyDrop.StartedAt == 55f && timingOnlyDrop.Duration == 2f &&
                phase.Phase == 1 && phase.ArrivalSeconds == 5f && phase.DepartureSeconds == 5f &&
                phase.HoldSeconds == 4f && phase.NextDropReadyAt == 55f,
            "Presentation-free transport state must contain phase/drop clocks only, with authored timings");
        Require(math.abs(SharedProductionDeliveryKernel.AdvanceDropTiming(timingOnlyDrop, hasTimingOnlyDrop, 56f) - 0.5f) < 0.00001f,
            "Presentation-free active drop must use the shared DropTiming progress policy");
        SharedProductionDeliveryKernel.FinishDrop(ref phase, ref hasTimingOnlyDrop, ref timingOnlyDelivered, 57f, true);
        Require(!hasTimingOnlyDrop && timingOnlyDelivered == 1 && phase.NextDropReadyAt == 57f,
            "Presentation-free drop completion must use shared delivered/next-ready bookkeeping");

        var invalidAdmission = new SharedProductionDeliveryKernel.Transport { Phase = 0, NextDropReadyAt = 0f };
        bool invalidOrderRejected = false;
        try { SharedProductionDeliveryKernel.BeginHelicopterDropTiming(ref invalidAdmission, 0f); }
        catch (InvalidOperationException error) when (error.Message == "Source drop admission/order") { invalidOrderRejected = true; }
        Require(invalidOrderRejected, "Timing-only drop must preserve shared phase admission guard");
    }

    static void CheckPresentationFreeTransportDispatch()
    {
        var calls = new List<string>();
        var view = new PresentationFreeTransportView(calls, true, false, 1, 0);
        uint random = 19;
        SharedProductionTransportDispatchKernel.Step< string, PresentationFreeTransportView>(ref view, "producer", 1f, 0.1f, ref random);
        Require(calls.Count == 0 && random == 19,
            "Normal dispatch must retain its transform admission gate for the native owner");
        SharedProductionTransportDispatchKernel.StepWithoutPresentation<string, PresentationFreeTransportView>(ref view, "producer", 1f, 0.1f, ref random);
        Require(string.Join(",", calls) == "arrival,publish" && random == 19,
            "Scoped presentation-free dispatch must share the arrival branch and publication order without a transform");
        calls.Clear();
        view = new PresentationFreeTransportView(calls, true, false, 1, 1);
        SharedProductionTransportDispatchKernel.StepWithoutPresentation<string, PresentationFreeTransportView>(ref view, "producer", 2f, 0.1f, ref random);
        Require(string.Join(",", calls) == "delivery,publish" && random == 19,
            "Scoped presentation-free dispatch must preserve delivery phase ordering");
        calls.Clear();
        view = new PresentationFreeTransportView(calls, true, false, 1, 2);
        SharedProductionTransportDispatchKernel.StepWithoutPresentation<string, PresentationFreeTransportView>(ref view, "producer", 3f, 0.1f, ref random);
        Require(string.Join(",", calls) == "departure,publish" && random == 19,
            "Scoped presentation-free dispatch must preserve departure ordering");
        calls.Clear();
        view = new PresentationFreeTransportView(calls, false, false, 1, 0);
        SharedProductionTransportDispatchKernel.StepWithoutPresentation<string, PresentationFreeTransportView>(ref view, "producer", 4f, 0.1f, ref random);
        Require(calls.Count == 0, "Presentation-free dispatch must still reject absent transports");
    }

    static void CheckPresentationFreePhaseTimingPolicy()
    {
        var timing = SharedProductionDeliveryKernel.HelicopterPhaseTiming(10f, 2f, 4f);
        var partial = SharedProductionDeliveryKernel.AdvancePhase(ref timing, 11f);
        Require(partial.EvaluatedPhase == 0 && partial.Transition == SharedProductionDeliveryKernel.Transition.None &&
                math.abs(partial.Progress - 0.5f) < 0.00001f && timing.Phase == 0 && timing.PhaseStartedAt == 10f,
            "Shared phase clock must remain in arrival before the exact duration boundary");
        var arrived = SharedProductionDeliveryKernel.AdvancePhase(ref timing, 12f);
        Require(arrived.EvaluatedPhase == 0 && arrived.Transition == SharedProductionDeliveryKernel.Transition.Arrived &&
                arrived.Progress == 1f && timing.Phase == 1 && timing.PhaseStartedAt == 12f &&
                timing.HoverEnteredAt == 12f && timing.NextDropReadyAt == 12f,
            "Arrival boundary must update phase, hover clock, and next-drop clock together");
        var delivery = SharedProductionDeliveryKernel.AdvancePhase(ref timing, 12f);
        Require(delivery.EvaluatedPhase == 1 && delivery.Transition == SharedProductionDeliveryKernel.Transition.Delivery &&
                timing.Phase == 1 && timing.PhaseStartedAt == 12f,
            "Arrival update must precede delivery callback on the following phase step");

        var state = SharedProductionDeliveryKernel.Helicopter(10f, 2f, 4f,
            new float3(0f, 0f, 0f), new float3(10f, 4f, -2f), new float3(20f, 2f, 4f),
            quaternion.identity, quaternion.RotateY(math.radians(90f)), quaternion.RotateY(math.radians(180f)));
        var geometryTransition = SharedProductionDeliveryKernel.Advance(ref state, 11f);
        Require(geometryTransition == partial.Transition && state.Phase == 0 &&
                math.distance(state.Position, new float3(5f, 2f, -1f)) < 0.00001f &&
                math.abs(math.dot(state.Rotation, math.slerp(quaternion.identity, quaternion.RotateY(math.radians(90f)), 0.5f))) > 0.99999f,
            "Existing Advance must apply the shared phase progress to its original position/rotation path");
        geometryTransition = SharedProductionDeliveryKernel.Advance(ref state, 12f);
        Require(geometryTransition == SharedProductionDeliveryKernel.Transition.Arrived && state.Phase == 1 &&
                state.Position.Equals(ReferenceTransportLerp(state.Entry, state.Hover, 1f)) &&
                state.Rotation.Equals(math.slerp(state.EntryRotation, state.HoverRotation, 1f)),
            "Existing Advance must preserve exact arrival-side position and rotation effects");
        SharedProductionDeliveryKernel.DepartIfNoSoonPending(ref state, 16f, false);
        Require(state.Phase == 2 && state.PhaseStartedAt == 16f,
            "Departure must begin only after the original hover hold expires");
        var departing = SharedProductionDeliveryKernel.Advance(ref state, 18f);
        Require(departing == SharedProductionDeliveryKernel.Transition.None && state.Phase == 2 &&
                math.distance(state.Position, new float3(15f, 3f, 1f)) < 0.00001f,
            "Departure must retain the shared half-duration interpolation before completion");
        var departed = SharedProductionDeliveryKernel.Advance(ref state, 20f);
        Require(departed == SharedProductionDeliveryKernel.Transition.Departed && state.Phase == 2 &&
                state.Position.Equals(ReferenceTransportLerp(state.Hover, state.Exit, 1f)) &&
                state.Rotation.Equals(math.slerp(state.HoverRotation, state.ExitRotation, 1f)),
            "Departure boundary must preserve final route pose and transition ordering");

        timing = new SharedProductionDeliveryKernel.PhaseTiming(77, 0f, 1f, 1f, 1f, 0f, 0f);
        bool invalidRejected = false;
        try { SharedProductionDeliveryKernel.AdvancePhase(ref timing, 0f); }
        catch (InvalidOperationException error) when (error.Message == "Unknown source transport phase") { invalidRejected = true; }
        Require(invalidRejected, "Unknown phase must remain a hard failure in the shared timing policy");
    }

    static float3 ReferenceTransportLerp(float3 a, float3 b, float t)
    {
        t = math.clamp(t, 0f, 1f);
        return new float3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
    }

    static void CheckDropProgress(SharedProductionDeliveryKernel.DropTiming timing,
        float now, float expected, string message)
    {
        float actual = SharedProductionDeliveryKernel.DropProgress(timing, now);
        Require(math.isfinite(actual) && math.abs(actual - expected) <= 0.0001f, message);
    }

    static void CheckLanePolicy(bool hasCamera, ActualVector3 cameraForward, int lane, int maxConcurrent, float3 expected)
    {
        ActualVector3 actual = SharedProductionDeliveryKernel.ProductionTransportLaneOffset(
            hasCamera, cameraForward, lane, maxConcurrent);
        Require(math.abs(actual.x - expected.x) < 0.00001f &&
                math.abs(actual.y - expected.y) < 0.00001f &&
                math.abs(actual.z - expected.z) < 0.00001f,
            "Shared lane policy must preserve the Unity Vector3 lane offset and edge behavior through ActualCore alias");
    }

    static void CheckSharedRequestOrderingAndRollback()
    {
        var denied = new SemanticOwnerFixtureAdapter
        {
            Evaluation = SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.InsufficientMaterials
        };
        var deniedResult = SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Execute<int, SemanticProducer, SourcePendingProduction>(
            denied, 1, 5, false, 7, 12f, out _);
        Require(deniedResult == SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.InsufficientMaterials &&
                string.Join(",", denied.Calls) == "evaluate",
            "Common owner must stop after the original first failure without spending, selecting, or queueing");

        var rejectedQueue = new SemanticOwnerFixtureAdapter { QueueAccepted = false };
        var queueFailure = SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Execute<int, SemanticProducer, SourcePendingProduction>(
            rejectedQueue, 1, 5, true, 7, 12f, out _);
        Require(queueFailure == SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.ProducerQueueFull &&
                string.Join(",", rejectedQueue.Calls) ==
                "evaluate,configured,global_capacity,resolve_costs,producer_capacity,mission_receipt,spend,producer_lookup,focus,queue,restore",
            "Common owner must focus before queue and refund after queue rejection in native order");

        var missingProducer = new SemanticOwnerFixtureAdapter { ProducerLookupAccepted = false };
        var producerFailure = SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Execute<int, SemanticProducer, SourcePendingProduction>(
            missingProducer, 1, 5, true, 7, 12f, out _);
        Require(producerFailure == SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.InvalidSelection &&
                string.Join(",", missingProducer.Calls) ==
                "evaluate,configured,global_capacity,resolve_costs,producer_capacity,mission_receipt,spend,producer_lookup,restore",
            "Common owner must refund before focus/queue when the adapter cannot resolve the selected producer object");

        var opMap = new SemanticOwnerFixtureAdapter
        {
            HasProducer = false,
            HasOperationMapProducer = true,
            OperationQueueAccepted = false
        };
        var opFailure = SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Execute<int, SemanticProducer, SourcePendingProduction>(
            opMap, 1, 5, false, 7, 12f, out _);
        Require(opFailure == SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.InvalidSelection &&
                string.Join(",", opMap.Calls) ==
                "evaluate,configured,global_capacity,resolve_costs,producer_capacity,producer_any,operation_map_probe,spend,operation_map_queue,restore,global_capacity_after_operation_failure",
            "Common owner must preserve operation-map fallback spend/queue/refund and postfailure capacity ordering");

        var accepted = new SemanticOwnerFixtureAdapter();
        var acceptedResult = SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Execute<int, SemanticProducer, SourcePendingProduction>(
            accepted, 1, 5, false, 7, 12f, out _);
        Require(acceptedResult == SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.None &&
                accepted.ProducerIdentityPreserved &&
                string.Join(",", accepted.Calls) ==
                "evaluate,configured,global_capacity,resolve_costs,producer_capacity,mission_receipt,spend,producer_lookup,queue,adjust_pending,record_ordered",
            "Common owner must pass the adapter-owned producer object through the same success and payment sequence");
    }

    static void CheckLegacyResourceOwnerTransaction()
    {
        SemanticOwnerFixtureAdapter NewFixture(byte policy, int credits, int materials, bool accepted) => new()
        {
            UseOriginalResourceOwner = true, CreditsCost = 10000, MaterialsCost = 20, QueueAccepted = accepted,
            Economy = new FactionEconomy { FactionId = 1, Money = credits, MaterialsOnlyConstruction = policy },
            Materials = new FactionTacticalMaterialsComponent { FactionId = 1, Current = materials, Capacity = 220 }
        };
        var rejected = NewFixture(MissionConstructionCostPolicy.MaterialsOnly, 0, 220, false);
        var outcome = SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Execute<int, SemanticProducer, SourcePendingProduction>(
            rejected, 1, 20, false, 0, 0, out _);
        Require(outcome == SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.ProducerQueueFull &&
                rejected.Economy.Money == 0 && rejected.Materials.Current == 220 && rejected.Calls.Contains("restore"),
            "Original materials-only spend must be restored after the semantic queue rejects");

        var accepted = NewFixture(MissionConstructionCostPolicy.MaterialsOnly, 0, 220, true);
        outcome = SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Execute<int, SemanticProducer, SourcePendingProduction>(
            accepted, 1, 20, false, 0, 0, out _);
        Require(outcome == SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.None &&
                accepted.Economy.Money == 0 && accepted.Materials.Current == 200 && !accepted.Calls.Contains("restore"),
            "Original materials-only spend must occur once after an accepted semantic request");

        var legacy = NewFixture(MissionConstructionCostPolicy.Legacy, 0, 220, true);
        outcome = SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Execute<int, SemanticProducer, SourcePendingProduction>(
            legacy, 1, 20, false, 0, 0, out _);
        Require(outcome == SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.InsufficientCredits &&
                legacy.Economy.Money == 0 && legacy.Materials.Current == 220 && !legacy.Calls.Contains("queue"),
            "Legacy policy must not discard the authored credit component or queue an unpaid request");
    }

    static void CheckSemanticFailureBoundary()
    {
        foreach (BuildingUiCommandSystemHelper.CampRequestFailure nativeFailure in
                 Enum.GetValues(typeof(BuildingUiCommandSystemHelper.CampRequestFailure)))
        {
            var semantic = SourceCampUiAdapter.ToSemanticFailure(nativeFailure);
            Require(SourceCampUiAdapter.ToCampFailure(semantic) == nativeFailure,
                $"Source adapter must preserve each native failure through the semantic contract: {nativeFailure}");
        }
        Require(SourceCampUiAdapter.ToSemanticFailure((BuildingUiCommandSystemHelper.CampRequestFailure)255) ==
                    SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.InvalidSelection &&
                SourceCampUiAdapter.ToCampFailure((SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure)255) ==
                    BuildingUiCommandSystemHelper.CampRequestFailure.InvalidSelection,
            "Unknown boundary values must retain the original InvalidSelection fallback");

        foreach (var outcome in new[]
                 {
                     SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.QueueOutcome.ProducerQueueFull,
                     SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.QueueOutcome.GlobalQueueFull,
                     SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.QueueOutcome.Rejected
                 })
        {
            var adapter = new SemanticOwnerFixtureAdapter { QueueAccepted = false, QueueResult = outcome };
            var failure = SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Execute<int, SemanticProducer, SourcePendingProduction>(
                adapter, 1, 5, false, 7, 12f, out _);
            var expected = outcome switch
            {
                SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.QueueOutcome.ProducerQueueFull => SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.ProducerQueueFull,
                SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.QueueOutcome.GlobalQueueFull => SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.GlobalQueueFull,
                _ => SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure.InvalidSelection
            };
            Require(failure == expected && adapter.Calls[adapter.Calls.Count - 1] == "restore",
                $"Semantic queue outcome must preserve queue mapping and paid rollback: {outcome}");
        }
    }

    static void CheckPresentationFreeFixtureOption()
    {
        var ordinary = new JObject { ["mission"] = "S002" };
        Require(!SourceSimulationProgram.UsePresentationFreeKnownNullTransportCamera(ordinary),
            "Absent fixture option must leave the normal camera path selected");

        var admitted = new JObject
        {
            ["mission"] = "S002",
            ["semantic_command_control_test"] = new JObject { ["kind"] = "PaidRifleToMove" },
            ["presentation_free_delivery_fixture"] = new JObject
            {
                ["kind"] = "S002SingleRifleKnownNullTransportCamera",
                ["world_camera_is_null"] = true
            }
        };
        Require(SourceSimulationProgram.UsePresentationFreeKnownNullTransportCamera(admitted),
            "Only the explicit isolated S002 paid-rifle null-camera fixture may select that branch");

        foreach (JToken malformed in new JToken[] { JValue.CreateNull(), new JValue(1), new JValue("absent"), new JArray(), new JObject() })
        {
            var invalid = (JObject)admitted.DeepClone();
            invalid["presentation_free_delivery_fixture"] = malformed;
            bool rejected = false;
            try { SourceSimulationProgram.UsePresentationFreeKnownNullTransportCamera(invalid); }
            catch (InvalidDataException) { rejected = true; }
            Require(rejected, "Present null/non-object/unknown fixture values must fail closed");
        }

        var wrongMission = (JObject)admitted.DeepClone();
        wrongMission["mission"] = "S026";
        bool wrongMissionRejected = false;
        try { SourceSimulationProgram.UsePresentationFreeKnownNullTransportCamera(wrongMission); }
        catch (NotSupportedException) { wrongMissionRejected = true; }
        Require(wrongMissionRejected, "The fixture must remain scoped to S002");
    }

    static void CheckAuthoredMetadataAndDeliveryGate(CurrentAuthoredRifleProduction authored)
    {
        var f = new Fixture(1000, authored);
        var duration = f.Production.ResolveProductionDurationSeconds(f.Unit);
        var settings = f.Production.ResolveProductionTransportSettings(f.Unit, null, null, null);
        Require(duration == 5f && settings.TransportPrefab == authored.Transport &&
                settings.ArrivalSeconds == 5f && settings.HoldForNextReadySeconds == 4f &&
                settings.MaxConcurrent == 2 && !settings.RequiresAirportRunway &&
                settings.Mode == BuildingProductionQueueCompositionSystemHelper.ProductionTransportMode.Helicopter,
            "Original queue metadata owner must resolve current authored Rifle and helicopter settings");
        Require(f.ProductionMetadataResolverCalls >= 2 && ReferenceEquals(f.LastResolvedMetadataPrefab, authored.Transport) &&
                f.LastResolvedMetadata.ProductionDurationSeconds == 5f && f.LastResolvedMetadata.IsAirUnit &&
                f.LastResolvedMetadata.ProductionTransportMaxConcurrent == 2,
            "Original queue owner must read both exact Rifle and transport metadata through the typed resolver");
        int before = Materials(f);
        bool deliveryRejected = false;
        try { f.Owner.TryQueue(f.Building.Id, 0, 10f, out _); }
        catch (NotSupportedException error) when (error.Message.Contains("Source transport mode/geometry not admitted"))
        { deliveryRejected = true; }
        Require(deliveryRejected && f.Building.PendingProductions.Count == 0 && Materials(f) == before &&
                f.Em.GetBuffer<SkirmishProductionReservation>(f.Session, true).Length == 0,
            "Actual delivery owner must reject the unadmitted authored helicopter before payment/queue mutation");
    }

    static void CheckSyntheticSuccess()
    {
        var f = new Fixture(1000);
        int before = Materials(f);
        Require(f.Owner.TryQueue(f.Building.Id, 0, 10f, out var pending), "Synthetic source-owner fixture should admit its no-transport control");
        Require(pending != null && pending.ReceiptOwner == f.Session && pending.ReceiptId != 0 &&
                pending.ReceiptAttempt.Equals(f.Em.GetComponentData<SkirmishExpandedSessionComponent>(f.Session).SessionId),
            "Synthetic accepted pending DTO must retain its actual reservation receipt");
        Require(f.Building.PendingProductions.Count == 1 && f.Building.PendingProductions[0] == pending &&
                Materials(f) == before - f.Role.MaterialsCost,
            "Synthetic queue control must insert the same pending DTO and debit materials once");
        var receipts = f.Em.GetBuffer<SkirmishProductionReservation>(f.Session, true);
        Require(receipts.Length == 1 && receipts[0].ReservationId == pending.ReceiptId &&
                receipts[0].ProducerRuntimeId == f.Building.Id && receipts[0].Phase == SkirmishReservationPhase.Reserved,
            "Synthetic queue control must retain its real source reservation receipt");
    }

    static void CheckSyntheticDenial()
    {
        var f = new Fixture(0);
        Require(!f.Owner.TryQueue(f.Building.Id, 0, 10f, out var pending) && pending == null,
            "Synthetic insufficient-materials request must be denied before pending insertion");
        Require(f.Building.PendingProductions.Count == 0 && Materials(f) == 0 &&
                f.Em.GetBuffer<SkirmishProductionReservation>(f.Session, true).Length == 0,
            "Synthetic rejected request must leave queue, materials, and receipts unchanged");
    }

    static void CheckRollback()
    {
        var f = new Fixture(1000);
        var prior = new SourcePendingProduction { StartedAt = 2f, ReadyAt = 9f };
        IList<BuildingProductionQueueCompositionSystemHelper.IPendingProduction> queue = new ThrowAfterMutatingList(prior);
        int before = Materials(f);
        int released = 0;
        bool injected = false;
        try
        {
            f.Production.TryQueuePlayerUnitFromBuildingShared<BuildingProductionQueueCompositionSystemHelper.IPendingProduction>(
                f.Context, f.Building, 0, f.Unit, f.Em, 10f, queue,
                () => new SourcePendingProduction(), _ => released++, (_, _) => f.Role.SquadMembers,
                out _);
        }
        catch (InvalidOperationException error) when (error.Message == "fixture enqueue failure after mutation")
        {
            injected = true;
        }
        Require(injected, "Failure fixture must reach the injected post-reservation enqueue failure");
        Require(queue.Count == 1 && ReferenceEquals(queue[0], prior) && prior.StartedAt == 2f && prior.ReadyAt == 9f,
            "Rollback must remove the new pending entry and restore preexisting timeline state");
        Require(released == 1 && Materials(f) == before,
            "Rollback must return the pending DTO and refund the actual materials reservation");
        var receipts = f.Em.GetBuffer<SkirmishProductionReservation>(f.Session, true);
        Require(receipts.Length == 1 && receipts[0].Phase == SkirmishReservationPhase.Cancelled &&
                receipts[0].ProducerRuntimeId == f.Building.Id,
            "Rollback must preserve the actual cancelled receipt and producer binding");
    }

    static void Main(string[] args)
    {
        if (args == null || args.Length != 2)
            throw new InvalidDataException("Explicit current authored descriptor absolute path and SHA-256 arguments required");
        var authored = CurrentAuthoredRifleProduction.LoadCurrent(args[0], args[1]);
        CurrentAuthoredRifleProduction.CheckNegativeControls(args[0]);
        CheckSharedProducerSelection();
        CheckSharedLanePolicyUnityAlias();
        CheckPresentationFreeDropTimingPolicy();
        CheckPresentationFreeTransportDispatch();
        CheckPresentationFreePhaseTimingPolicy();
        CheckSharedRequestOrderingAndRollback();
        CheckLegacyResourceOwnerTransaction();
        CheckSemanticFailureBoundary();
        CheckPresentationFreeFixtureOption();
        CheckSyntheticSuccess();
        CheckSyntheticDenial();
        CheckAuthoredMetadataAndDeliveryGate(authored);
        CheckRollback();
        Console.WriteLine("[SourceUnitProductionQueueTransactionControl] result=Passed scope=IsolatedFixture controls=shared-drop-clock-presentation-free-finite-time-edges-plus-synthetic-queue-success-denial-rollback-and-authored-rifle-transport-metadata metadata=actual-original-resolver actual_delivery=preflight-rejection-before-payment actual_paid_queue=false template_available=false delivery_spawned=false aria_invoked=false mission_admission=false descriptor_sha256=" + authored.DescriptorSha256);
    }
}
