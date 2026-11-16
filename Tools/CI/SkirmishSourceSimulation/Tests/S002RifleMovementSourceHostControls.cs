using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Game.Runtime;
using Game.Components;
using Unity.Entities;

internal static class S002RifleMovementSourceHostControls
{
    private const string RifleKey = "Unit_Chr_Soldier_Male_02_Alt_04";

    private static int Main(string[] args)
    {
        try { return Run(args); }
        catch (Exception error)
        {
            Console.Error.WriteLine("[S002RifleMovementSourceHostControls] result=Failed " + error);
            return 1;
        }
    }

    private static int Run(string[] args)
    {
        Need(args.Length == 9, "expected S002 capture/receipt, S003 capture/receipt, legacy descriptor, source root, output directory, control receipt, current projection descriptor");
        string s002Path = Path.GetFullPath(args[0]);
        string s002ReceiptPath = Path.GetFullPath(args[1]);
        string s003Path = Path.GetFullPath(args[2]);
        string s003ReceiptPath = Path.GetFullPath(args[3]);
        string descriptorPath = Path.GetFullPath(args[4]);
        string sourceRoot = Path.GetFullPath(args[5]);
        string outputDirectory = Path.GetFullPath(args[6]);
        string outputPath = Path.GetFullPath(args[7]);
        string currentProjectionDescriptorPath = Path.GetFullPath(args[8]);

        JObject s002 = Read(s002Path);
        JObject s002Receipt = Read(s002ReceiptPath);
        JObject s003 = Read(s003Path);
        JObject s003Receipt = Read(s003ReceiptPath);
        JObject descriptor = new JObject { ["path"] = descriptorPath, ["sha256"] = Sha(descriptorPath) };

        Need((string)s002Receipt["kind"] == "S002FocusedPrefabCaptureReceipt" &&
            (string)s002Receipt["validation_status"] == "Passed" &&
            (string)s002Receipt["capture_sha256"] == Sha(s002Path) &&
            (int?)s002Receipt["prefab_count"] == 12 && (int?)s002Receipt["entity_count"] == 352 &&
            (bool?)s002Receipt["mission_ready"] == false &&
            s002["kind"]?.Value<string>() == "AuthoredSkirmishPrefabReferenceData" &&
            s002["opening_prefabs"]?[RifleKey] != null,
            "Retained S002 capture receipt and Alt04 historical root witness required");
        Need((string)s003Receipt["kind"] == "S003FocusedPrefabCaptureReceipt" &&
            (string)s003Receipt["validation_status"] == "PassedDiagnosticSourceOnly" &&
            (string)s003Receipt["capture_sha256"] == Sha(s003Path) &&
            (int?)s003Receipt["root_count"] == 19 && (int?)s003Receipt["entity_count"] == 647 &&
            (bool?)s003Receipt["mission_ready"] == false &&
            s003["kind"]?.Value<string>() == "AuthoredSkirmishPrefabReferenceData" &&
            s003["opening_prefabs"]?[RifleKey] != null,
            "Retained S003 capture receipt and Alt04 historical root witness required");

        SourceUnitMoveAuthoringSource s002Source = LoadProjection(descriptor, s002, sourceRoot);
        SourceUnitMoveAuthoringSource s003Source = LoadProjection(descriptor, s003, sourceRoot);
        JObject currentProjectionDescriptor = new JObject { ["path"] = currentProjectionDescriptorPath,
            ["sha256"] = Sha(currentProjectionDescriptorPath) };
        var projectionReads = new List<string>();
        SourceUnitMoveAuthoringSource currentSource = SourceUnitMoveAuthoringSource.LoadCurrentProjection(
            currentProjectionDescriptor, RifleKey, sourceRoot, (path, digest, maximum) => {
                projectionReads.Add(Path.GetFullPath(path));
                return AutonomousWorld.PinnedFile(path, digest, maximum);
            });
        string forbiddenPrefab = Path.GetFullPath(Path.Combine(sourceRoot,
            "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab"));
        Need(SameProjection(currentSource.Projection, s002Source.Projection) &&
            Match(currentSource.Combat, s002Source.Combat) && Match(currentSource.Health, s002Source.Health) &&
            Match(currentSource.Attack, s002Source.Attack) && !projectionReads.Contains(forbiddenPrefab),
            "Current projection-only API must match shared helper outputs without reading prefab/capture sources");
        bool sourcePinRejected = RejectCurrentProjectionMutation(currentProjectionDescriptorPath, outputDirectory,
            currentProjectionDescriptor, "source-pin", sourceRoot);
        bool wrongConfigRejected = RejectCurrentProjectionMutation(currentProjectionDescriptorPath, outputDirectory,
            currentProjectionDescriptor, "wrong-guid", sourceRoot);
        bool valuePayloadRejected = RejectCurrentProjectionMutation(currentProjectionDescriptorPath, outputDirectory,
            currentProjectionDescriptor, "value-payload", sourceRoot);
        bool getterDriftRejected = RejectCurrentProjectionMutation(currentProjectionDescriptorPath, outputDirectory,
            currentProjectionDescriptor, "getter-drift", sourceRoot);
        Need(sourcePinRejected && wrongConfigRejected && valuePayloadRejected && getterDriftRejected,
            "Current projection-only descriptor must reject pin, config, value payload, and getter drift mutations");
        Need(SameProjection(s002Source.Projection, s003Source.Projection),
            "S002 and S003 current pinned config projections differ");
        Need(Match(s002Source.Attack, s003Source.Attack),
            "S002 and S003 current pinned UnitAttack projections differ");

        string[] s002Keys = ((JArray)s002["requested_prefab_keys"]).Values<string>().ToArray();
        string[] s003Keys = ((JArray)s003["requested_prefab_keys"]).Values<string>().ToArray();
        string s002Binary = Path.GetDirectoryName(s002Path);
        string s003Binary = Path.GetDirectoryName(s003Path);
        JObject s002Preflight = GeneratedReferenceTemplateImporter.Preflight(s002, s002Binary, s002Keys);
        JObject s003Preflight = GeneratedReferenceTemplateImporter.Preflight(s003, s003Binary, s003Keys);
        Need((bool?)s002Preflight["world_allocated"] == false && (int?)s002Preflight["entity_identities_allocated"] == 0 &&
            (bool?)s003Preflight["world_allocated"] == false && (int?)s003Preflight["entity_identities_allocated"] == 0,
            "Historical witness preflight must allocate no world identities");

        bool s002MismatchRejected = RejectMismatchBeforeAllocation(s002, s002Binary, s002Keys, s002Source.Projection, 810000);
        bool s003MismatchRejected = RejectMismatchBeforeAllocation(s003, s003Binary, s003Keys, s003Source.Projection, 820000);
        bool s002CombatMismatchRejected = RejectCombatMismatchBeforeAllocation(s002, s002Binary, s002Keys, s002Source, 830000);
        bool s003CombatMismatchRejected = RejectCombatMismatchBeforeAllocation(s003, s003Binary, s003Keys, s003Source, 840000);
        bool s002HealthMismatchRejected = RejectHealthMismatchBeforeAllocation(s002, s002Binary, s002Keys, s002Source, 845000);
        bool s003HealthMismatchRejected = RejectHealthMismatchBeforeAllocation(s003, s003Binary, s003Keys, s003Source, 846000);
        bool s002MissingHealthRejected = RejectMissingProjectedComponentBeforeAllocation(s002, s002Binary, s002Keys, s002Source, true, 850000);
        bool s003MissingCombatRejected = RejectMissingProjectedComponentBeforeAllocation(s003, s003Binary, s003Keys, s003Source, false, 860000);
        bool s002AttackMismatchesRejected = RejectAttackMismatchesBeforeAllocation(s002, s002Binary, s002Keys, s002Source, 870000);
        bool s003AttackMismatchesRejected = RejectAttackMismatchesBeforeAllocation(s003, s003Binary, s003Keys, s003Source, 880000);
        bool s002MissingAttackRejected = RejectMissingAttackBeforeAllocation(s002, s002Binary, s002Keys, s002Source, 890000);
        bool s003MissingAttackRejected = RejectMissingAttackBeforeAllocation(s003, s003Binary, s003Keys, s003Source, 895000);
        Need(s002MismatchRejected && s003MismatchRejected && s002CombatMismatchRejected && s003CombatMismatchRejected &&
            s002HealthMismatchRejected && s003HealthMismatchRejected && s002MissingHealthRejected && s003MissingCombatRejected &&
            s002AttackMismatchesRejected && s003AttackMismatchesRejected && s002MissingAttackRejected && s003MissingAttackRejected,
            "Both profile imports must reject movement/combat/health/UnitAttack mismatch or missing component before allocation");

        ImportCheck s002Import = ImportAndClean(s002, s002Binary, s002Keys, s002Source, 900000);
        ImportCheck s003Import = ImportAndClean(s003, s003Binary, s003Keys, s003Source, 950000);
        Need(s002Import.Roots == 12 && s002Import.Entities == 352 &&
            s003Import.Roots == 19 && s003Import.Entities == 647 &&
            s002Import.MovementMatches && s003Import.MovementMatches &&
            s002Import.CombatHealthMatches && s003Import.CombatHealthMatches &&
            s002Import.AttackMatches && s003Import.AttackMatches &&
            s002Import.EntitiesRemoved && s003Import.EntitiesRemoved,
            "Historical witness imports must match targeted movement/combat/health/UnitAttack slices and clean up");

        JObject report = new JObject {
            ["kind"] = "S002RifleMovementSourceHostControl",
            ["status"] = "Passed_TargetedHistoricalMovementCombatHealthAndUnitAttackSlicesOnly",
            ["source_profile"] = "S002",
            ["compiled_s002_bindings"] = new JArray(
                "source_manifest.plan.json", "Adapters/305_GeneratedReferenceTemplateImporter.cs",
                "Adapters/GeneratedReferenceTemplateImporter.UnitMoveProjection.cs", "UnitCombatHealthAuthoringProjection.cs",
                "UnitAttackAuthoringProjection.cs",
                "Adapters/307_GeneratedS002StartingWorld.lifetime.cs",
                "Adapters/513_FreshSourceStorageLifetimeAuthority.cs"),
            ["current_config_descriptor_sha256"] = Sha(descriptorPath),
            ["current_source_only_projection"] = new JObject {
                ["descriptor_sha256"] = Sha(currentProjectionDescriptorPath),
                ["projection_matches_shared_legacy_loader"] = true,
                ["prefab_or_capture_sources_read"] = false,
                ["source_pin_mismatch_rejected"] = sourcePinRejected,
                ["wrong_config_guid_rejected"] = wrongConfigRejected,
                ["caller_value_payload_rejected"] = valuePayloadRejected,
                ["getter_drift_rejected"] = getterDriftRejected,
                ["entity_allocation_or_importer_used"] = false },
            ["current_projection"] = Describe(s002Source.Projection),
            ["current_unit_attack_projection"] = Describe(s002Source.Attack),
            ["s002_historical_witness"] = Witness(s002Path, s002ReceiptPath, s002Preflight, s002Import),
            ["s003_historical_witness"] = Witness(s003Path, s003ReceiptPath, s003Preflight, s003Import),
            ["mismatch_rejected_before_allocation"] = new JObject {
                ["movement_s002"] = s002MismatchRejected, ["movement_s003"] = s003MismatchRejected,
                ["combat_s002"] = s002CombatMismatchRejected, ["combat_s003"] = s003CombatMismatchRejected,
                ["health_s002"] = s002HealthMismatchRejected, ["health_s003"] = s003HealthMismatchRejected,
                ["missing_health_s002"] = s002MissingHealthRejected, ["missing_combat_s003"] = s003MissingCombatRejected,
                ["unit_attack_all_value_fields_s002"] = s002AttackMismatchesRejected,
                ["unit_attack_all_value_fields_s003"] = s003AttackMismatchesRejected,
                ["missing_unit_attack_s002"] = s002MissingAttackRejected,
                ["missing_unit_attack_s003"] = s003MissingAttackRejected },
            ["projected_component_match"] = new JObject {
                ["s002_unit_combat_health"] = s002Import.CombatHealthMatches,
                ["s003_unit_combat_health"] = s003Import.CombatHealthMatches,
                ["s002_unit_attack_values"] = s002Import.AttackMatches,
                ["s003_unit_attack_values"] = s003Import.AttackMatches },
            ["current_selected_asset_pins_verified"] = new JArray(
                "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config.asset",
                "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config.asset.meta",
                "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab"),
            ["full_capture_pin_audit_performed"] = false,
            ["unprojected_components"] = "All components other than UnitMove, UnitFootprint, UnitMovementBehavior, UnitVehicleMovement, UnitVehicleKinematics, UnitCombat, UnitHealth, and UnitAttack remain historical capture passthrough; VFX references are untouched.",
            ["unity_world_created"] = false,
            ["mission_manager_created"] = false,
            ["mission_ready"] = false,
            ["scope"] = "Five Alt04 movement components plus UnitCombat, UnitHealth, and all UnitAttack value fields only. S002 and S003 captures are retained historical witnesses; projected component mismatches reject before allocation; VFX references are untouched; no whole-capture current qualification or admission claim."
        };
        File.WriteAllText(outputPath, report.ToString(Newtonsoft.Json.Formatting.Indented) + Environment.NewLine);
        Console.WriteLine("[S002RifleMovementSourceHostControls] result=Passed s002=12 s003=19 movementSlice=1 combatHealthSlice=1 unitAttack=1 allValueMismatchesRejected=1 missingRejected=1 cleanup=1 world=0 mission=0 readiness=0");
        return 0;
    }

    private static SourceUnitMoveAuthoringSource LoadProjection(JObject descriptor, JObject capture, string sourceRoot) =>
        SourceUnitMoveAuthoringSource.Load(descriptor, capture, RifleKey, sourceRoot,
            (path, digest, maximum) => AutonomousWorld.PinnedFile(path, digest, maximum));

    private static bool RejectCurrentProjectionMutation(string descriptorPath, string outputDirectory,
        JObject descriptorReference, string mutation, string sourceRoot)
    {
        JObject input = Read(descriptorPath);
        string authoringRel = "Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs";
        string configRel = "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config.asset";
        string overrideAuthoring = null;
        if (mutation == "source-pin") input["source_pins"][configRel] = new string('0', 64);
        else if (mutation == "wrong-guid") input["config_guid"] = new string('0', 32);
        else if (mutation == "value-payload") input["values_bits"] = new JObject { ["speed"] = "00000000" };
        else if (mutation == "getter-drift")
        {
            string path = Path.Combine(sourceRoot, authoringRel);
            overrideAuthoring = File.ReadAllText(path).Replace(
                "Mathf.Max(0f, config != null ? config.AttackRange : attackRange)",
                "Mathf.Max(1f, config != null ? config.AttackRange : attackRange)");
            input["source_pins"][authoringRel] = Digest(System.Text.Encoding.UTF8.GetBytes(overrideAuthoring));
        }
        else throw new ArgumentOutOfRangeException(nameof(mutation));

        string pathOut = Path.Combine(outputDirectory, "negative-current-projection-" + mutation + ".json");
        File.WriteAllText(pathOut, input.ToString(Newtonsoft.Json.Formatting.Indented) + Environment.NewLine);
        JObject reference = new JObject { ["path"] = pathOut, ["sha256"] = Sha(pathOut) };
        Action attempt = () => {
            SourceUnitMoveAuthoringSource.LoadCurrentProjection(reference, RifleKey, sourceRoot,
                (path, digest, maximum) => {
                    if (overrideAuthoring != null && Path.GetFullPath(path) == Path.GetFullPath(Path.Combine(sourceRoot, authoringRel)))
                    {
                        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(overrideAuthoring);
                        if (bytes.Length > maximum || Digest(bytes) != digest) throw new InvalidDataException("Pinned fixture bytes mismatch");
                        return bytes;
                    }
                    return AutonomousWorld.PinnedFile(path, digest, maximum);
                });
        };
        switch (mutation)
        {
            case "source-pin":
                return ThrowsExact<InvalidOperationException>(attempt,
                    "Source input hash changed: " + Path.GetFullPath(Path.Combine(sourceRoot, configRel)));
            case "wrong-guid":
                return ThrowsExact<InvalidDataException>(attempt, "Rifle prefab does not bind the pinned config GUID");
            case "value-payload":
                return ThrowsExact<InvalidDataException>(attempt, "Exact single-rifle UnitMove source descriptor required");
            case "getter-drift":
                return ThrowsExact<InvalidDataException>(attempt, "Current UnitAttack getter semantics changed: ConfiguredAttackRange");
            default:
                throw new ArgumentOutOfRangeException(nameof(mutation));
        }
    }

    private static bool ThrowsExact<TException>(Action action, string expectedMessage) where TException : Exception
    {
        try { action(); }
        catch (TException error)
        { return error.GetType() == typeof(TException) && error.Message == expectedMessage; }
        return false;
    }

    private static string Digest(byte[] bytes) => BitConverter.ToString(System.Security.Cryptography.SHA256.Create().ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();

    private static bool RejectMismatchBeforeAllocation(JObject source, string binary, string[] keys,
        UnitMoveAuthoringProjection expected, int firstIndex)
    {
        int allocations = 0;
        var manager = new EntityManager { LogicalAllocatorAdmitted = true };
        manager.AllocateLogicalIdentity = () => { allocations++; return new Entity { Index = firstIndex + allocations, Version = 1 }; };
        UnitMoveAuthoringProjection wrong = UnitMoveAuthoringProjection.FromAuthoredMovementWithoutModelBounds(
            expected.Speed + 1f, expected.WalkSpeed, expected.RoadSpeedMultiplier, expected.ArriveDistance,
            expected.Footprint.Size.x, expected.Footprint.Size.y,
            expected.Footprint.Size.x == 1 && expected.Footprint.Size.y == 1,
            expected.MovementBehavior.UsesVehicleMotion != 0,
            expected.MovementBehavior.AllowIdleWander != 0);
        bool rejected = false;
        try { GeneratedReferenceTemplateImporter.Import(manager, source, binary, keys, RifleKey, wrong); }
        catch (InvalidOperationException) { rejected = true; }
        return rejected && allocations == 0;
    }

    private sealed class ImportCheck
    {
        internal bool MovementMatches;
        internal bool CombatHealthMatches;
        internal bool AttackMatches;
        internal bool EntitiesRemoved;
        internal int Roots;
        internal int Entities;
        internal int Components;
    }

    private static ImportCheck ImportAndClean(JObject source, string binary, string[] keys,
        SourceUnitMoveAuthoringSource authoring, int firstIndex)
    {
        var manager = new EntityManager { LogicalAllocatorAdmitted = true };
        int next = firstIndex;
        manager.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        GeneratedReferenceTemplateImporter.Result result = GeneratedReferenceTemplateImporter.Import(
            manager, source, binary, keys, RifleKey, authoring.Projection, authoring.Combat, authoring.Health, authoring.Attack);
        Entity rifle = result.Prefabs[RifleKey];
        bool movementMatches = Match(manager.GetComponentData<UnitMove>(rifle), authoring.Projection) &&
            Match(manager.GetComponentData<UnitFootprint>(rifle), authoring.Projection) &&
            Match(manager.GetComponentData<UnitMovementBehavior>(rifle), authoring.Projection) &&
            Match(manager.GetComponentData<UnitVehicleMovement>(rifle), authoring.Projection) &&
            Match(manager.GetComponentData<UnitVehicleKinematics>(rifle), authoring.Projection);
        UnitCombat combat = manager.GetComponentData<UnitCombat>(rifle);
        UnitHealth health = manager.GetComponentData<UnitHealth>(rifle);
        bool combatHealthMatches = combat.AggroRangeCells == authoring.Combat.AggroRangeCells &&
            Bits(combat.ChaseBreakDistance) == Bits(authoring.Combat.ChaseBreakDistance) &&
            combat.CanAttack == authoring.Combat.CanAttack && combat.AutoEngage == authoring.Combat.AutoEngage &&
            health.Current == authoring.Health.Current && health.Max == authoring.Health.Max;
        bool attackMatches = Match(manager.GetComponentData<UnitAttack>(rifle), authoring.Attack);
        Entity[] entities = result.SourceToLogical.Values.ToArray();
        foreach (Entity entity in entities) manager.DestroyEntity(entity);
        return new ImportCheck { MovementMatches = movementMatches, CombatHealthMatches = combatHealthMatches,
            AttackMatches = attackMatches, EntitiesRemoved = entities.All(entity => !manager.Exists(entity)),
            Roots = result.Prefabs.Count, Entities = result.ImportedEntities, Components = result.ImportedComponents };
    }

    private static bool RejectAttackMismatchesBeforeAllocation(JObject source, string binary, string[] keys,
        SourceUnitMoveAuthoringSource authoring, int firstIndex)
    {
        string[] floatFields = { "Range", "CooldownSeconds", "TraceWidth", "TraceScrollSpeed", "TraceDashDensity", "TraceVisibleSeconds" };
        foreach (string field in floatFields)
            if (!RejectMutatedAttackBeforeAllocation(source, binary, keys, authoring, firstIndex++, field, null)) return false;
        foreach (string channel in new[] { "x", "y", "z", "w" })
            if (!RejectMutatedAttackBeforeAllocation(source, binary, keys, authoring, firstIndex++, "TraceColor", channel)) return false;
        foreach (string field in new[] { "Damage", "TracerEveryNthShot" })
            if (!RejectMutatedAttackBeforeAllocation(source, binary, keys, authoring, firstIndex++, field, null)) return false;
        return true;
    }

    private static bool RejectMutatedAttackBeforeAllocation(JObject source, string binary, string[] keys,
        SourceUnitMoveAuthoringSource authoring, int firstIndex, string field, string channel)
    {
        JObject mutated = (JObject)source.DeepClone();
        string sourceId = (string)mutated["opening_prefabs"][RifleKey];
        JObject row = ((JArray)mutated["entities"]).OfType<JObject>().Single(entity => (string)entity["id"] == sourceId);
        JObject fields = (JObject)row["components"]["Game.Components.UnitAttack"]["value"]["fields"];
        string qualified = "Game.Components.UnitAttack::" + field;
        JObject scalar = (JObject)fields[qualified];
        if (field == "TraceColor")
        {
            JObject colorFields = (JObject)scalar["fields"];
            scalar = (JObject)colorFields["Unity.Mathematics.float4::" + channel];
        }
        if ((string)scalar["codec"] == "f32-bits-v1")
        {
            uint bits = Convert.ToUInt32((string)scalar["bits"], 16) ^ 1u;
            scalar["bits"] = bits.ToString("x8", System.Globalization.CultureInfo.InvariantCulture);
        }
        else
        {
            long value = long.Parse((string)scalar["value"], System.Globalization.CultureInfo.InvariantCulture);
            scalar["value"] = (value + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        int allocations = 0;
        var manager = new EntityManager { LogicalAllocatorAdmitted = true };
        manager.AllocateLogicalIdentity = () => { allocations++; return new Entity { Index = firstIndex + allocations, Version = 1 }; };
        bool rejected = false;
        try { GeneratedReferenceTemplateImporter.Import(manager, mutated, binary, keys, RifleKey,
            authoring.Projection, authoring.Combat, authoring.Health, authoring.Attack); }
        catch (InvalidOperationException) { rejected = true; }
        return rejected && allocations == 0;
    }

    private static bool RejectMissingAttackBeforeAllocation(JObject source, string binary, string[] keys,
        SourceUnitMoveAuthoringSource authoring, int firstIndex)
    {
        JObject missing = (JObject)source.DeepClone();
        string sourceId = (string)missing["opening_prefabs"][RifleKey];
        JObject row = ((JArray)missing["entities"]).OfType<JObject>().Single(entity => (string)entity["id"] == sourceId);
        ((JObject)row["components"]).Remove("Game.Components.UnitAttack");
        int allocations = 0;
        var manager = new EntityManager { LogicalAllocatorAdmitted = true };
        manager.AllocateLogicalIdentity = () => { allocations++; return new Entity { Index = firstIndex + allocations, Version = 1 }; };
        bool rejected = false;
        try { GeneratedReferenceTemplateImporter.Import(manager, missing, binary, keys, RifleKey,
            authoring.Projection, authoring.Combat, authoring.Health, authoring.Attack); }
        catch (InvalidOperationException) { rejected = true; }
        return rejected && allocations == 0;
    }

    private static bool Match(UnitAttack actual, UnitAttack expected) =>
        Bits(actual.Range) == Bits(expected.Range) && Bits(actual.CooldownSeconds) == Bits(expected.CooldownSeconds) &&
        actual.Damage == expected.Damage && Bits(actual.TraceColor.x) == Bits(expected.TraceColor.x) &&
        Bits(actual.TraceColor.y) == Bits(expected.TraceColor.y) && Bits(actual.TraceColor.z) == Bits(expected.TraceColor.z) &&
        Bits(actual.TraceColor.w) == Bits(expected.TraceColor.w) && Bits(actual.TraceWidth) == Bits(expected.TraceWidth) &&
        Bits(actual.TraceScrollSpeed) == Bits(expected.TraceScrollSpeed) && Bits(actual.TraceDashDensity) == Bits(expected.TraceDashDensity) &&
        Bits(actual.TraceVisibleSeconds) == Bits(expected.TraceVisibleSeconds) &&
        actual.TracerEveryNthShot == expected.TracerEveryNthShot;

    private static bool Match(UnitCombat actual, UnitCombat expected) =>
        actual.AggroRangeCells == expected.AggroRangeCells &&
        Bits(actual.ChaseBreakDistance) == Bits(expected.ChaseBreakDistance) &&
        actual.CanAttack == expected.CanAttack && actual.AutoEngage == expected.AutoEngage;

    private static bool Match(UnitHealth actual, UnitHealth expected) =>
        actual.Current == expected.Current && actual.Max == expected.Max;

    private static bool RejectCombatMismatchBeforeAllocation(JObject source, string binary, string[] keys,
        SourceUnitMoveAuthoringSource authoring, int firstIndex)
    {
        int allocations = 0;
        var manager = new EntityManager { LogicalAllocatorAdmitted = true };
        manager.AllocateLogicalIdentity = () => { allocations++; return new Entity { Index = firstIndex + allocations, Version = 1 }; };
        UnitCombat wrong = authoring.Combat;
        wrong.AggroRangeCells++;
        bool rejected = false;
        try { GeneratedReferenceTemplateImporter.Import(manager, source, binary, keys, RifleKey,
            authoring.Projection, wrong, authoring.Health); }
        catch (InvalidOperationException) { rejected = true; }
        return rejected && allocations == 0;
    }

    private static bool RejectHealthMismatchBeforeAllocation(JObject source, string binary, string[] keys,
        SourceUnitMoveAuthoringSource authoring, int firstIndex)
    {
        int allocations = 0;
        var manager = new EntityManager { LogicalAllocatorAdmitted = true };
        manager.AllocateLogicalIdentity = () => { allocations++; return new Entity { Index = firstIndex + allocations, Version = 1 }; };
        UnitHealth wrong = authoring.Health;
        wrong.Max++;
        bool rejected = false;
        try { GeneratedReferenceTemplateImporter.Import(manager, source, binary, keys, RifleKey,
            authoring.Projection, authoring.Combat, wrong); }
        catch (InvalidOperationException) { rejected = true; }
        return rejected && allocations == 0;
    }

    private static bool RejectMissingProjectedComponentBeforeAllocation(JObject source, string binary, string[] keys,
        SourceUnitMoveAuthoringSource authoring, bool removeHealth, int firstIndex)
    {
        JObject missing = (JObject)source.DeepClone();
        string sourceId = (string)missing["opening_prefabs"][RifleKey];
        JObject row = ((JArray)missing["entities"]).OfType<JObject>().Single(entity => (string)entity["id"] == sourceId);
        string component = removeHealth ? "Game.Components.UnitHealth" : "Game.Components.UnitCombat";
        JObject components = (JObject)row["components"];
        components.Remove(component);
        int allocations = 0;
        var manager = new EntityManager { LogicalAllocatorAdmitted = true };
        manager.AllocateLogicalIdentity = () => { allocations++; return new Entity { Index = firstIndex + allocations, Version = 1 }; };
        bool rejected = false;
        try { GeneratedReferenceTemplateImporter.Import(manager, missing, binary, keys, RifleKey,
            authoring.Projection, authoring.Combat, authoring.Health); }
        catch (InvalidOperationException) { rejected = true; }
        return rejected && allocations == 0;
    }

    private static bool Match(UnitMove value, UnitMoveAuthoringProjection expected) =>
        Bits(value.Speed) == Bits(expected.Speed) && Bits(value.WalkSpeed) == Bits(expected.WalkSpeed) &&
        Bits(value.RoadSpeedMultiplier) == Bits(expected.RoadSpeedMultiplier) && Bits(value.ArriveDistance) == Bits(expected.ArriveDistance);
    private static bool Match(UnitFootprint value, UnitMoveAuthoringProjection expected) => value.Size.Equals(expected.Footprint.Size);
    private static bool Match(UnitMovementBehavior value, UnitMoveAuthoringProjection expected) =>
        value.AllowIdleWander == expected.MovementBehavior.AllowIdleWander && value.UsesVehicleMotion == expected.MovementBehavior.UsesVehicleMotion;
    private static bool Match(UnitVehicleMovement value, UnitMoveAuthoringProjection expected) =>
        Bits(value.TurnSpeedDegrees) == Bits(expected.VehicleMovement.TurnSpeedDegrees) &&
        Bits(value.Acceleration) == Bits(expected.VehicleMovement.Acceleration) &&
        Bits(value.Braking) == Bits(expected.VehicleMovement.Braking) &&
        Bits(value.RearPivotOffset) == Bits(expected.VehicleMovement.RearPivotOffset);
    private static bool Match(UnitVehicleKinematics value, UnitMoveAuthoringProjection expected) =>
        Bits(value.CurrentSpeed) == Bits(expected.VehicleKinematics.CurrentSpeed) &&
        Bits(value.StallSeconds) == Bits(expected.VehicleKinematics.StallSeconds);

    private static bool SameProjection(UnitMoveAuthoringProjection a, UnitMoveAuthoringProjection b) =>
        Match(new UnitMove { Speed = a.Speed, WalkSpeed = a.WalkSpeed, RoadSpeedMultiplier = a.RoadSpeedMultiplier, ArriveDistance = a.ArriveDistance }, b) &&
        Match(a.Footprint, b) && Match(a.MovementBehavior, b) && Match(a.VehicleMovement, b) && Match(a.VehicleKinematics, b);

    private static JObject Describe(UnitMoveAuthoringProjection value) => new JObject {
        ["speed_bits"] = Bits(value.Speed), ["walk_speed_bits"] = Bits(value.WalkSpeed),
        ["road_speed_multiplier_bits"] = Bits(value.RoadSpeedMultiplier), ["arrive_distance_bits"] = Bits(value.ArriveDistance),
        ["footprint_x"] = value.Footprint.Size.x, ["footprint_y"] = value.Footprint.Size.y,
        ["allow_idle_wander"] = value.MovementBehavior.AllowIdleWander, ["uses_vehicle_motion"] = value.MovementBehavior.UsesVehicleMotion,
        ["turn_speed_bits"] = Bits(value.VehicleMovement.TurnSpeedDegrees), ["acceleration_bits"] = Bits(value.VehicleMovement.Acceleration),
        ["braking_bits"] = Bits(value.VehicleMovement.Braking), ["rear_pivot_offset_bits"] = Bits(value.VehicleMovement.RearPivotOffset),
        ["current_speed_bits"] = Bits(value.VehicleKinematics.CurrentSpeed), ["stall_seconds_bits"] = Bits(value.VehicleKinematics.StallSeconds)
    };

    private static JObject Describe(UnitAttack value) => new JObject {
        ["range_bits"] = Bits(value.Range), ["cooldown_seconds_bits"] = Bits(value.CooldownSeconds),
        ["damage"] = value.Damage, ["trace_color_bits"] = new JObject {
            ["r"] = Bits(value.TraceColor.x), ["g"] = Bits(value.TraceColor.y),
            ["b"] = Bits(value.TraceColor.z), ["a"] = Bits(value.TraceColor.w) },
        ["trace_width_bits"] = Bits(value.TraceWidth), ["trace_scroll_speed_bits"] = Bits(value.TraceScrollSpeed),
        ["trace_dash_density_bits"] = Bits(value.TraceDashDensity),
        ["trace_visible_seconds_bits"] = Bits(value.TraceVisibleSeconds),
        ["tracer_every_nth_shot"] = value.TracerEveryNthShot
    };

    private static JObject Witness(string path, string receiptPath, JObject preflight, ImportCheck import) => new JObject {
        ["capture_path"] = path, ["capture_sha256"] = Sha(path),
        ["receipt_path"] = receiptPath, ["receipt_sha256"] = Sha(receiptPath),
        ["source_only_preflight"] = preflight,
        ["historical_import_roots"] = import.Roots, ["historical_import_entities"] = import.Entities,
        ["decoded_component_count"] = import.Components, ["movement_slice_match"] = import.MovementMatches,
        ["disposable_entities_removed"] = import.EntitiesRemoved
    };

    private static JObject Read(string path)
    {
        using var text = new StringReader(File.ReadAllText(path));
        using var reader = new Newtonsoft.Json.JsonTextReader(text) { DateParseHandling = Newtonsoft.Json.DateParseHandling.None };
        JObject value = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
        if (reader.Read()) throw new InvalidDataException("Exactly one complete JSON document required");
        return value;
    }
    private static string Sha(string path) => BitConverter.ToString(System.Security.Cryptography.SHA256.Create().ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    private static string Bits(float value) => unchecked((uint)BitConverter.ToInt32(BitConverter.GetBytes(value), 0)).ToString("x8");
    private static void Need(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
}
