using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Game.Runtime;
using Game.Components;
using Unity.Entities;
using Unity.Mathematics;

internal static class S003FocusedTemplateSourceHostControls
{
    [DllImport("libc", EntryPoint = "symlink", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern int CreateSymlink(string target, string linkPath);
    const string CaptureSha = "9b31b6863e80d3bd548cd78631a3385b1c7d0ed007da1785c00f1cb16d7a323e";
    const string KeySha = "38bd886005c62bc121d165e1119a3f8876b456eddaa0ef873a1a5681aa66539b";
    const int ExpectedRoots = 19;
    const int ExpectedEntities = 647;
    const int ExpectedComponents = 3991;
    const int ExpectedUnsupportedLeaves = 32;
    const int ExpectedOmittedComponents = 26;
    static void Need(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
    static string Sha(string path)
    {
        using (var hash = SHA256.Create())
        using (var stream = File.OpenRead(path))
            return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
    }
    static int Main(string[] args)
    {
        if (args.Length != 7 && args.Length != 9) throw new ArgumentException("capture, keys, strict receipt, binary directory, output receipt, UnitMove source input, explicit source repository root, and optional S002 capture/receipt required");
        JObject source = JObject.Parse(File.ReadAllText(Path.GetFullPath(args[0])));
        string[] keys = JArray.Parse(File.ReadAllText(Path.GetFullPath(args[1]))).Values<string>().ToArray();
        JObject finalizer = JObject.Parse(File.ReadAllText(Path.GetFullPath(args[2])));
        string binaryDirectory = Path.GetFullPath(args[3]);
        string output = Path.GetFullPath(args[4]);
        string sourceRepositoryRoot = Path.GetFullPath(args[6]);
        const string rifleKey = "Unit_Chr_Soldier_Male_02_Alt_04";
        // Copy the content-pinned descriptor outside the repository to prove that
        // its location is data, while the host-supplied root owns source resolution.
        string portableDirectory = Directory.Exists("/private/tmp") ? "/private/tmp" : Path.GetTempPath();
        string portablePath = Path.Combine(portableDirectory, "unitmove-source-" + Guid.NewGuid().ToString("N") + ".json");
        File.Copy(Path.GetFullPath(args[5]), portablePath);
        var moveDescriptor = new JObject { ["path"] = portablePath, ["sha256"] = Sha(portablePath) };
        SourceUnitMoveAuthoringSource movementInput = SourceUnitMoveAuthoringSource.Load(
            moveDescriptor, source, rifleKey, sourceRepositoryRoot,
            (path, digest, maxBytes) => AutonomousWorld.PinnedFile(path, digest, maxBytes));
        bool descriptorSymlinkRejected = false;
        string symlinkPath = portablePath + ".symlink";
        if (CreateSymlink(portablePath, symlinkPath) != 0) throw new IOException("Could not create descriptor symlink negative control");
        try
        {
            try
            {
                SourceUnitMoveAuthoringSource.Load(new JObject { ["path"] = symlinkPath, ["sha256"] = Sha(portablePath) },
                    source, rifleKey, sourceRepositoryRoot,
                    (path, digest, maxBytes) => AutonomousWorld.PinnedFile(path, digest, maxBytes));
            }
            catch (InvalidDataException) { descriptorSymlinkRejected = true; }
        }
        finally { File.Delete(symlinkPath); File.Delete(portablePath); }
        Need(descriptorSymlinkRejected, "UnitMove source descriptor symlink must fail closed");
        bool sourceRootSymlinkRejected = false;
        string symlinkRoot = Path.Combine(portableDirectory, "unitmove-repo-" + Guid.NewGuid().ToString("N"));
        if (CreateSymlink(sourceRepositoryRoot, symlinkRoot) != 0) throw new IOException("Could not create source-root symlink negative control");
        try
        {
            SourceUnitMoveAuthoringSource.Load(moveDescriptor, source, rifleKey, symlinkRoot,
                (path, digest, maxBytes) => AutonomousWorld.PinnedFile(path, digest, maxBytes));
        }
        catch (InvalidDataException) { sourceRootSymlinkRejected = true; }
        finally { File.Delete(symlinkRoot); }
        Need(sourceRootSymlinkRejected, "UnitMove source repository root symlink must fail closed");
        bool outsideRootPinRejected = false;
        JObject escapedInput = JObject.Parse(File.ReadAllText(Path.GetFullPath(args[5])));
        JObject escapedPins = (JObject)escapedInput["source_pins"];
        JProperty firstPin = escapedPins.Properties().First();
        JToken firstDigest = firstPin.Value.DeepClone();
        firstPin.Remove();
        escapedPins.Add("../outside-source.asset", firstDigest);
        string escapedPath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[5])), "unitmove-escaped-source.json");
        File.WriteAllText(escapedPath, escapedInput.ToString(Formatting.Indented));
        try
        {
            SourceUnitMoveAuthoringSource.Load(new JObject { ["path"] = escapedPath, ["sha256"] = Sha(escapedPath) },
                source, rifleKey, sourceRepositoryRoot,
                (path, digest, maxBytes) => AutonomousWorld.PinnedFile(path, digest, maxBytes));
        }
        catch (InvalidDataException) { outsideRootPinRejected = true; }
        finally { File.Delete(escapedPath); }
        Need(outsideRootPinRejected, "UnitMove source pin escape must fail closed");
        UnitMoveAuthoringProjection rifleMove = movementInput.Projection;
        CheckProjectionMathBitPatterns();
        Need(Sha(args[0]) == CaptureSha && Sha(args[1]) == KeySha, "Exact current S003 capture and key descriptor bytes required");
        Need((string)finalizer["kind"] == "S003FocusedPrefabCaptureReceipt" &&
            (string)finalizer["validation_status"] == "PassedDiagnosticSourceOnly" &&
            (string)finalizer["capture_sha256"] == CaptureSha && (string)finalizer["key_descriptor_sha256"] == KeySha &&
            (int?)finalizer["root_count"] == ExpectedRoots && (int?)finalizer["entity_count"] == ExpectedEntities &&
            (int?)finalizer["unsupported_count"] == ExpectedUnsupportedLeaves &&
            (bool?)finalizer["utility_world_created"] == true && (bool?)finalizer["utility_world_disposed"] == true &&
            (bool?)finalizer["mission_world_created"] == false && (bool?)finalizer["replay_ready"] == false &&
            (bool?)finalizer["mission_ready"] == false,
            "Exact source-only finalizer receipt required; no readiness authority");
        JObject capturedRoots = source["opening_prefabs"] as JObject;
        Need(keys.Length == ExpectedRoots && keys.Distinct(StringComparer.Ordinal).Count() == ExpectedRoots &&
            keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() == ExpectedRoots &&
            JToken.DeepEquals(source["requested_prefab_keys"], JArray.FromObject(keys)) &&
            (string)source["requested_prefab_keys_sha256"] == KeySha &&
            capturedRoots != null && capturedRoots.Count == ExpectedRoots &&
            JToken.DeepEquals(new JArray(capturedRoots.Properties().Select(property => property.Name)), JArray.FromObject(keys)) &&
            ((JArray)source["entities"]).Count == ExpectedEntities &&
            ((JArray)source["unsupported_values"]).Count == ExpectedUnsupportedLeaves,
            "Exact S003 ordered source capture shape required");
        string rifleSourceId = (string)capturedRoots[rifleKey];
        JObject rifleCaptureRow = ((JArray)source["entities"]).OfType<JObject>()
            .Single(row => (string)row["id"] == rifleSourceId);
        string[] projectedSourceComponentTypes = {
            "Game.Components.UnitMove", "Game.Components.UnitFootprint", "Game.Components.UnitMovementBehavior",
            "Game.Components.UnitVehicleMovement", "Game.Components.UnitVehicleKinematics",
            "Game.Components.UnitCombat", "Game.Components.UnitHealth"
        };
        string[] retainedUnprojectedRifleComponentTypes = ((JObject)rifleCaptureRow["components"]).Properties()
            .Select(property => property.Name)
            .Where(name => !projectedSourceComponentTypes.Contains(name, StringComparer.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal).ToArray();
        Need((bool?)source["utility_world_created"] == true && (bool?)source["utility_world_disposed"] == true &&
            (bool?)source["mission_world_created"] == false && (bool?)source["replay_ready"] == false &&
            (bool?)source["native_context_qualified"] == false && (bool?)source["source_fit_certified"] == false &&
            (bool?)source["mission_ready"] == false && (bool?)source["native_visual_readiness"] == false,
            "Source data scope and false readiness flags required");
        int capturedDisabledCount = ((JArray)source["entities"]).OfType<JObject>()
            .Count(row => ((JObject)row["components"]).Property("Unity.Entities.Disabled") != null);
        Need(capturedDisabledCount == 6, "Exact captured Disabled tag census required");
        Need(!GeneratedReferenceTemplateImporter.BirthRecordIsInPresentationOmissionSet,
            "RuntimeActorBirthRecord must never be treated as an omitted presentation component");

        JObject preflight = GeneratedReferenceTemplateImporter.Preflight(source, binaryDirectory, keys);
        Need((int?)preflight["roots"] == ExpectedRoots && (int?)preflight["entities"] == ExpectedEntities &&
            (int?)preflight["decoded_components"] == ExpectedComponents - ExpectedOmittedComponents &&
            (int?)preflight["omitted_presentation_components"] == ExpectedOmittedComponents &&
            (int?)preflight["unsupported_unconsumed_values"] == ExpectedUnsupportedLeaves &&
            (bool?)preflight["world_allocated"] == false && (int?)preflight["entity_identities_allocated"] == 0,
            "Shared importer preflight facts changed");

        // A source ledger mutation outside the exact VFX UnityObjectRef whitelist must fail before allocation.
        JObject invalid = (JObject)source.DeepClone();
        ((JObject)((JArray)invalid["unsupported_values"])[0])["type"] = "Game.Components.SimulationValue";
        int rejectedAllocations = 0;
        var rejectedManager = new EntityManager { LogicalAllocatorAdmitted = true };
        rejectedManager.AllocateLogicalIdentity = () => { rejectedAllocations++; return new Entity { Index = 700000 + rejectedAllocations, Version = 1 }; };
        bool invalidRejected = false;
        try { GeneratedReferenceTemplateImporter.Import(rejectedManager, invalid, binaryDirectory, keys); }
        catch (InvalidOperationException) { invalidRejected = true; }
        Need(invalidRejected && rejectedAllocations == 0,
            "Non-whitelisted unsupported leaf must reject before allocating logical entities");

        string[] foreignRequiredKeys = keys.Concat(new[] { "Unit_NotInS003Capture" }).ToArray();
        int wrongRootAllocations = 0;
        var wrongRootManager = new EntityManager { LogicalAllocatorAdmitted = true };
        wrongRootManager.AllocateLogicalIdentity = () => { wrongRootAllocations++; return new Entity { Index = 710000 + wrongRootAllocations, Version = 1 }; };
        bool foreignKeyRejected = false;
        try { GeneratedReferenceTemplateImporter.Import(wrongRootManager, source, binaryDirectory, foreignRequiredKeys); }
        catch (InvalidOperationException) { foreignKeyRejected = true; }
        Need(foreignKeyRejected && wrongRootAllocations == 0,
            "Foreign required source key must reject before allocating logical entities");

        JObject inconsistentDisabled = (JObject)source.DeepClone();
        var disabledSourceRow = ((JArray)inconsistentDisabled["entities"]).OfType<JObject>()
            .First(row => ((JObject)row["components"]).Property("Unity.Entities.Disabled") != null);
        disabledSourceRow["entity_enabled"] = true;
        int disabledAllocations = 0;
        var disabledManager = new EntityManager { LogicalAllocatorAdmitted = true };
        disabledManager.AllocateLogicalIdentity = () => { disabledAllocations++; return new Entity { Index = 720000 + disabledAllocations, Version = 1 }; };
        bool inconsistentDisabledRejected = false;
        try { GeneratedReferenceTemplateImporter.Import(disabledManager, inconsistentDisabled, binaryDirectory, keys); }
        catch (InvalidOperationException) { inconsistentDisabledRejected = true; }
        Need(inconsistentDisabledRejected && disabledAllocations == 0,
            "Captured Disabled tag and entity enablement mismatch must reject before allocation");

        int partialAllocations = 0;
        Entity firstPartialIdentity = Entity.Null;
        var partialManager = new EntityManager { LogicalAllocatorAdmitted = true };
        partialManager.AllocateLogicalIdentity = () => {
            partialAllocations++;
            if (partialAllocations == 1) { firstPartialIdentity = new Entity { Index = 730001, Version = 1 }; return firstPartialIdentity; }
            throw new InvalidOperationException("Focused partial-allocation failure");
        };
        bool partialAllocationRejected = false;
        try { GeneratedReferenceTemplateImporter.Import(partialManager, source, binaryDirectory, keys); }
        catch (InvalidOperationException) { partialAllocationRejected = true; }
        Need(partialAllocationRejected && partialAllocations == 2 && firstPartialIdentity != Entity.Null &&
            !partialManager.Exists(firstPartialIdentity),
            "Partial identity allocation failure must roll back earlier template entities");

        var manager = new EntityManager { LogicalAllocatorAdmitted = true };
        int next = 500000;
        manager.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
        int mismatchAllocations = 0;
        var mismatchManager = new EntityManager { LogicalAllocatorAdmitted = true };
        mismatchManager.AllocateLogicalIdentity = () => { mismatchAllocations++; return new Entity { Index = 740000 + mismatchAllocations, Version = 1 }; };
        JObject rawMovementValues = (JObject)JObject.Parse(File.ReadAllText(Path.GetFullPath(args[5])))["movement_values"];
        UnitMoveAuthoringProjection[] wrongMovementProjections = {
            UnitMoveAuthoringProjection.FromAuthoredMovementWithoutModelBounds(rifleMove.Speed + 1f, rifleMove.WalkSpeed, rifleMove.RoadSpeedMultiplier, rifleMove.ArriveDistance,
                (int)rawMovementValues["footprintX"], (int)rawMovementValues["footprintY"], (bool)rawMovementValues["autoCalculateFootprint"],
                (bool)rawMovementValues["usesVehicleMotion"], (bool)rawMovementValues["allowIdleWander"]),
            UnitMoveAuthoringProjection.FromAuthoredMovementWithoutModelBounds(rifleMove.Speed, rifleMove.WalkSpeed, rifleMove.RoadSpeedMultiplier, rifleMove.ArriveDistance,
                (int)rawMovementValues["footprintX"] + 1, (int)rawMovementValues["footprintY"], (bool)rawMovementValues["autoCalculateFootprint"],
                (bool)rawMovementValues["usesVehicleMotion"], (bool)rawMovementValues["allowIdleWander"]),
            UnitMoveAuthoringProjection.FromAuthoredMovementWithoutModelBounds(rifleMove.Speed, rifleMove.WalkSpeed, rifleMove.RoadSpeedMultiplier, rifleMove.ArriveDistance,
                (int)rawMovementValues["footprintX"], (int)rawMovementValues["footprintY"], (bool)rawMovementValues["autoCalculateFootprint"],
                (bool)rawMovementValues["usesVehicleMotion"], !(bool)rawMovementValues["allowIdleWander"]),
            UnitMoveAuthoringProjection.FromAuthoredMovement(rifleMove.Speed, rifleMove.WalkSpeed, rifleMove.RoadSpeedMultiplier, rifleMove.ArriveDistance,
                (int)rawMovementValues["footprintX"], (int)rawMovementValues["footprintY"], (bool)rawMovementValues["autoCalculateFootprint"],
                true, (bool)rawMovementValues["allowIdleWander"], false, 0f, 0f)
        };
        bool mismatchRejected = true;
        foreach (UnitMoveAuthoringProjection wrongMovement in wrongMovementProjections)
        {
            bool thisMismatchRejected = false;
            try { GeneratedReferenceTemplateImporter.Import(mismatchManager, source, binaryDirectory, keys,
                rifleKey, wrongMovement, movementInput.Combat, movementInput.Health); }
            catch (InvalidOperationException) { thisMismatchRejected = true; }
            mismatchRejected &= thisMismatchRejected;
        }
        Need(mismatchRejected && mismatchAllocations == 0,
            "Source-host movement projection mismatches must fail before allocating imported entities");

        UnitCombat wrongCombat = movementInput.Combat;
        wrongCombat.ChaseBreakDistance += 1f;
        bool combatMismatchRejected = false;
        try { GeneratedReferenceTemplateImporter.Import(mismatchManager, source, binaryDirectory, keys,
            rifleKey, rifleMove, wrongCombat, movementInput.Health); }
        catch (InvalidOperationException) { combatMismatchRejected = true; }
        UnitHealth wrongHealth = movementInput.Health;
        wrongHealth.Current++;
        bool healthMismatchRejected = false;
        try { GeneratedReferenceTemplateImporter.Import(mismatchManager, source, binaryDirectory, keys,
            rifleKey, rifleMove, movementInput.Combat, wrongHealth); }
        catch (InvalidOperationException) { healthMismatchRejected = true; }
        Need(combatMismatchRejected && healthMismatchRejected && mismatchAllocations == 0,
            "Source-host UnitCombat and UnitHealth mismatches must reject before allocating imported entities");

        GeneratedReferenceTemplateImporter.Result imported =
            GeneratedReferenceTemplateImporter.Import(manager, source, binaryDirectory, keys, rifleKey,
                rifleMove, movementInput.Combat, movementInput.Health);
        Need(ReferenceEquals(imported.Manager, manager) && imported.Prefabs.Count == ExpectedRoots && imported.ImportedEntities == ExpectedEntities &&
            imported.SourceToLogical.Count == ExpectedEntities && imported.ImportedComponents == ExpectedComponents - ExpectedOmittedComponents &&
            imported.OmittedPresentationComponents == ExpectedOmittedComponents && keys.All(imported.Prefabs.ContainsKey),
            "Actual shared importer result differs from exact S003 source domain");
        Need(imported.SourceToLogical.Values.All(entity => manager.Exists(entity) && entity.Index >= 500000) &&
            imported.SourceToLogical.Values.Select(entity => entity.Index).Distinct().Count() == ExpectedEntities,
            "Importer logical entity identities must be unique and isolated from captured source IDs");
        Entity importedRifle = imported.Prefabs[rifleKey];
        UnitMove importedRifleMove = manager.GetComponentData<UnitMove>(importedRifle);
        Need(SameBits(importedRifleMove.Speed, rifleMove.Speed) && SameBits(importedRifleMove.WalkSpeed, rifleMove.WalkSpeed) &&
            SameBits(importedRifleMove.RoadSpeedMultiplier, rifleMove.RoadSpeedMultiplier) &&
            SameBits(importedRifleMove.ArriveDistance, rifleMove.ArriveDistance),
            "Imported original rifle UnitMove component differs from pinned source projection");
        Need(ImportedMovementMatches(manager, importedRifle, rifleMove),
            "Imported rifle movement components differ from complete shared authoring projection");
        UnitCombat importedRifleCombat = manager.GetComponentData<UnitCombat>(importedRifle);
        UnitHealth importedRifleHealth = manager.GetComponentData<UnitHealth>(importedRifle);
        Need(importedRifleCombat.AggroRangeCells == movementInput.Combat.AggroRangeCells &&
            SameBits(importedRifleCombat.ChaseBreakDistance, movementInput.Combat.ChaseBreakDistance) &&
            importedRifleCombat.CanAttack == movementInput.Combat.CanAttack &&
            importedRifleCombat.AutoEngage == movementInput.Combat.AutoEngage &&
            importedRifleHealth.Current == movementInput.Health.Current && importedRifleHealth.Max == movementInput.Health.Max,
            "Imported rifle UnitCombat/UnitHealth differs from current pinned getter projection");
        CheckOriginalMovementProjection();
        Type[] presentationTypes = {
            typeof(Game.Components.UnitAttackImpactVfxReference),
            typeof(Game.Components.UnitMuzzleFlashVfxReference),
            typeof(Game.Components.GroundMissileLauncherVfxReferenceComponent),
            typeof(Game.Components.AirMissileLauncherVfxReferenceComponent)
        };
        foreach (Entity entity in imported.SourceToLogical.Values)
            foreach (Type type in presentationTypes)
                Need(!manager.HasComponent(entity, type), "Excluded VFX component entered logical source import: " + type.FullName);
        int disabledCount = imported.SourceToLogical.Values.Count(entity => manager.HasComponent<Disabled>(entity));
        Need(disabledCount == capturedDisabledCount,
            "Captured Unity.Entities.Disabled markers must resolve and remain present in the logical import");
        Entity[] importedEntities = imported.SourceToLogical.Values.ToArray();
        foreach (Entity entity in importedEntities) manager.DestroyEntity(entity);
        Need(importedEntities.All(entity => !manager.Exists(entity)), "Successful disposable import cleanup left allocated entities");

        JObject s002Evidence = null;
        if (args.Length == 9)
        {
            string s002CapturePath = Path.GetFullPath(args[7]);
            string s002ReceiptPath = Path.GetFullPath(args[8]);
            JObject s002Source = JObject.Parse(File.ReadAllText(s002CapturePath));
            JObject s002Receipt = JObject.Parse(File.ReadAllText(s002ReceiptPath));
            JObject s002Pins = s002Source["source_pins"] as JObject;
            JObject s002Roots = s002Source["opening_prefabs"] as JObject;
            JArray s002KeysArray = s002Source["requested_prefab_keys"] as JArray;
            JArray s002Scenes = s002Source["source_scenes"] as JArray;
            Need((string)s002Receipt["kind"] == "S002FocusedPrefabCaptureReceipt" &&
                (string)s002Receipt["validation_status"] == "Passed" &&
                (string)s002Receipt["capture_sha256"] == Sha(s002CapturePath) &&
                (int?)s002Receipt["prefab_count"] == 12 && (int?)s002Receipt["entity_count"] == 352 &&
                (int?)s002Receipt["source_pin_count"] == 7967 && (bool?)s002Receipt["mission_ready"] == false &&
                (bool?)s002Receipt["native_visual_readiness"] == false && (bool?)s002Receipt["replay_ready"] == false &&
                s002Scenes != null && s002Scenes.Count == 1 && (string)s002Scenes[0] == "Assets/Game/Scenes/Match/MatchRuntimeSubScene.unity" &&
                s002KeysArray != null && s002KeysArray.Values<string>().Contains(rifleKey, StringComparer.Ordinal) &&
                s002Roots?[rifleKey] != null && s002Pins != null,
                "Exact historical S002 scoped capture/receipt and rifle root required; no mission authority");
            foreach (string rel in new[] {
                "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config.asset",
                "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config.asset.meta",
                "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab" })
                Need(s002Pins[rel] != null && (string)s002Pins[rel] == (string)((JObject)JObject.Parse(File.ReadAllText(Path.GetFullPath(args[5])))["source_pins"])[rel],
                    "S002 selected rifle asset pin differs from shared source descriptor: " + rel);

            string s002RifleId = (string)s002Roots[rifleKey];
            JObject s002RifleCaptureRow = ((JArray)s002Source["entities"]).OfType<JObject>()
                .Single(row => (string)row["id"] == s002RifleId);
            string[] s002RetainedUnprojectedRifleComponentTypes = ((JObject)s002RifleCaptureRow["components"]).Properties()
                .Select(property => property.Name)
                .Where(name => !projectedSourceComponentTypes.Contains(name, StringComparer.Ordinal))
                .OrderBy(name => name, StringComparer.Ordinal).ToArray();

            var s002MoveInput = SourceUnitMoveAuthoringSource.Load(
                new JObject { ["path"] = Path.GetFullPath(args[5]), ["sha256"] = Sha(args[5]) },
                s002Source, rifleKey, sourceRepositoryRoot,
                (path, digest, maxBytes) => AutonomousWorld.PinnedFile(path, digest, maxBytes));
            string[] s002Keys = s002KeysArray.Values<string>().ToArray();
            string s002BinaryDirectory = Path.GetDirectoryName(s002CapturePath);
            JObject s002Preflight = GeneratedReferenceTemplateImporter.Preflight(s002Source, s002BinaryDirectory, s002Keys);
            Need((int?)s002Preflight["roots"] == 12 && (int?)s002Preflight["entities"] == 352 &&
                (bool?)s002Preflight["world_allocated"] == false && (int?)s002Preflight["entity_identities_allocated"] == 0,
                "S002 source-only importer preflight differs from retained scoped capture");
            var s002Manager = new EntityManager { LogicalAllocatorAdmitted = true };
            int s002Next = 900000;
            s002Manager.AllocateLogicalIdentity = () => new Entity { Index = s002Next++, Version = 1 };
            int s002MismatchAllocations = 0;
            var s002MismatchManager = new EntityManager { LogicalAllocatorAdmitted = true };
            s002MismatchManager.AllocateLogicalIdentity = () => { s002MismatchAllocations++; return new Entity { Index = 950000 + s002MismatchAllocations, Version = 1 }; };
            UnitMoveAuthoringProjection wrongS002Move = UnitMoveAuthoringProjection.FromAuthoredMovementWithoutModelBounds(
                s002MoveInput.Projection.Speed + 1f, s002MoveInput.Projection.WalkSpeed,
                s002MoveInput.Projection.RoadSpeedMultiplier, s002MoveInput.Projection.ArriveDistance,
                (int)rawMovementValues["footprintX"], (int)rawMovementValues["footprintY"],
                (bool)rawMovementValues["autoCalculateFootprint"], (bool)rawMovementValues["usesVehicleMotion"],
                (bool)rawMovementValues["allowIdleWander"]);
            bool s002MismatchRejected = false;
            try { GeneratedReferenceTemplateImporter.Import(s002MismatchManager, s002Source, s002BinaryDirectory,
                s002Keys, rifleKey, wrongS002Move, s002MoveInput.Combat, s002MoveInput.Health); }
            catch (InvalidOperationException) { s002MismatchRejected = true; }
            Need(s002MismatchRejected && s002MismatchAllocations == 0,
                "S002 shared UnitMove mismatch must reject before source import allocation");
            var s002Imported = GeneratedReferenceTemplateImporter.Import(
                s002Manager, s002Source, s002BinaryDirectory, s002Keys, rifleKey, s002MoveInput.Projection,
                s002MoveInput.Combat, s002MoveInput.Health);
            UnitMove s002Rifle = s002Manager.GetComponentData<UnitMove>(s002Imported.Prefabs[rifleKey]);
            Need(SameBits(s002Rifle.Speed, rifleMove.Speed) && SameBits(s002Rifle.WalkSpeed, rifleMove.WalkSpeed) &&
                SameBits(s002Rifle.RoadSpeedMultiplier, rifleMove.RoadSpeedMultiplier) && SameBits(s002Rifle.ArriveDistance, rifleMove.ArriveDistance),
                "S002 and S003 same-rifle UnitMove source projection differs");
            Need(ImportedMovementMatches(s002Manager, s002Imported.Prefabs[rifleKey], rifleMove),
                "S002 imported rifle movement components differ from S003 shared projection");
            Entity[] s002Entities = s002Imported.SourceToLogical.Values.ToArray();
            foreach (Entity entity in s002Entities) s002Manager.DestroyEntity(entity);
            Need(s002Entities.All(entity => !s002Manager.Exists(entity)), "S002 diagnostic import cleanup left allocated entities");
            s002Evidence = new JObject {
                ["scope"] = "Selected Alt04 five-component movement-slice comparison only; S002 capture pin drift elsewhere is not admitted or repinned",
                ["capture_sha256"] = Sha(s002CapturePath), ["capture_receipt_sha256"] = Sha(s002ReceiptPath),
                ["historical_capture_roots"] = (int)s002Receipt["prefab_count"], ["historical_capture_entities"] = (int)s002Receipt["entity_count"],
                ["source_pin_count"] = (int)s002Receipt["source_pin_count"], ["target_opening_root"] = rifleKey,
                ["source_import_preflight"] = s002Preflight, ["s002_s003_same_movement_slice_bits"] = true,
                ["retained_unprojected_component_types"] = new JArray(s002RetainedUnprojectedRifleComponentTypes),
                ["retained_unprojected_components_authority"] = "Historical captured data passed through unchanged; no current-source field qualification",
                ["mismatch_rejected_before_allocation"] = s002MismatchRejected && s002MismatchAllocations == 0,
                ["disposable_import_cleanup"] = s002Entities.All(entity => !s002Manager.Exists(entity)),
                ["mission_world_created"] = false, ["mission_ready"] = false
            };
        }

        var report = new JObject {
            ["kind"] = "S003FocusedTemplateSourceHostImportControl",
            ["status"] = "Passed_ActualSharedImporter_DisposableLogicalEntityManagerOnly",
            ["preflight"] = preflight,
            ["preflight_no_identity_allocation"] = true,
            ["unsupported_non_vfx_rejected_before_allocation"] = invalidRejected && rejectedAllocations == 0,
            ["foreign_required_key_rejected_before_allocation"] = foreignKeyRejected && wrongRootAllocations == 0,
            ["inconsistent_disabled_state_rejected_before_allocation"] = inconsistentDisabledRejected && disabledAllocations == 0,
            ["partial_identity_allocation_rollback"] = partialAllocationRejected && !partialManager.Exists(firstPartialIdentity),
            ["roots_imported"] = imported.Prefabs.Count,
            ["entities_imported"] = imported.ImportedEntities,
            ["components_imported"] = imported.ImportedComponents,
            ["unsupported_leaf_references_in_source"] = ((JArray)source["unsupported_values"]).Count,
            ["omitted_presentation_components"] = imported.OmittedPresentationComponents,
            ["manager_bound_to_import"] = ReferenceEquals(imported.Manager, manager),
            ["birth_record_not_in_presentation_omission_set"] = !GeneratedReferenceTemplateImporter.BirthRecordIsInPresentationOmissionSet,
            ["unit_movement_descriptor_portable_outside_repository"] = true,
            ["unit_movement_descriptor_symlink_rejected"] = descriptorSymlinkRejected,
            ["unit_movement_source_root_symlink_rejected"] = sourceRootSymlinkRejected,
            ["unit_movement_source_pin_escape_rejected"] = outsideRootPinRejected,
            ["unit_movement_source_repository_root_explicit"] = Path.IsPathRooted(sourceRepositoryRoot),
            ["s002_same_movement_slice_historical_field_control"] = s002Evidence,
            ["rifle_movement_source_projection"] = new JObject {
                ["root_key"] = rifleKey, ["config_guid"] = movementInput.ConfigGuid,
                ["speed_bits"] = FloatBitsHex(rifleMove.Speed), ["walk_speed_bits"] = FloatBitsHex(rifleMove.WalkSpeed),
                ["road_speed_multiplier_bits"] = FloatBitsHex(rifleMove.RoadSpeedMultiplier), ["arrive_distance_bits"] = FloatBitsHex(rifleMove.ArriveDistance),
                ["footprint_x"] = rifleMove.Footprint.Size.x, ["footprint_y"] = rifleMove.Footprint.Size.y,
                ["allow_idle_wander"] = rifleMove.MovementBehavior.AllowIdleWander,
                ["uses_vehicle_motion"] = rifleMove.MovementBehavior.UsesVehicleMotion,
                ["turn_speed_bits"] = FloatBitsHex(rifleMove.VehicleMovement.TurnSpeedDegrees),
                ["acceleration_bits"] = FloatBitsHex(rifleMove.VehicleMovement.Acceleration),
                ["braking_bits"] = FloatBitsHex(rifleMove.VehicleMovement.Braking),
                ["rear_pivot_offset_bits"] = FloatBitsHex(rifleMove.VehicleMovement.RearPivotOffset),
                ["current_speed_bits"] = FloatBitsHex(rifleMove.VehicleKinematics.CurrentSpeed),
                ["stall_seconds_bits"] = FloatBitsHex(rifleMove.VehicleKinematics.StallSeconds),
                ["source_pin_count"] = movementInput.Pins.Count, ["captured_baked_movement_slice_match"] = true,
                ["captured_baked_combat_health_match"] = true,
                ["unit_combat"] = new JObject {
                    ["aggro_range_cells"] = movementInput.Combat.AggroRangeCells,
                    ["chase_break_distance_bits"] = FloatBitsHex(movementInput.Combat.ChaseBreakDistance),
                    ["can_attack"] = movementInput.Combat.CanAttack,
                    ["auto_engage"] = movementInput.Combat.AutoEngage },
                ["unit_health"] = new JObject {
                    ["current"] = movementInput.Health.Current, ["max"] = movementInput.Health.Max },
                ["projected_component_types"] = new JArray(projectedSourceComponentTypes),
                ["retained_unprojected_component_types"] = new JArray(retainedUnprojectedRifleComponentTypes),
                ["retained_unprojected_components_authority"] = "Historical captured data passed through unchanged; no current-source field qualification",
                ["mismatch_rejected_before_allocation"] = mismatchRejected && mismatchAllocations == 0 &&
                    combatMismatchRejected && healthMismatchRejected
            },
            ["rifle_captured_unprojected_component_types"] = new JArray(retainedUnprojectedRifleComponentTypes),
            ["rifle_captured_unprojected_components_authority"] = "Historical captured data passed through unchanged; no current-source field qualification",
            ["captured_disabled_markers"] = capturedDisabledCount,
            ["disabled_markers_preserved"] = disabledCount == capturedDisabledCount,
            ["excluded_presentation_component_types"] = new JArray(presentationTypes.Select(type => type.FullName)),
            ["successful_import_cleanup"] = importedEntities.All(entity => !manager.Exists(entity)),
            ["source_entity_ids_reused"] = false,
            ["unity_world_created"] = false,
            ["mission_manager_created"] = false,
            ["mission_initialized"] = false,
            ["source_authority_rewritten"] = false,
            ["runtime_replay_ready"] = false,
            ["native_visual_readiness"] = false,
            ["source_fit_certified"] = false,
            ["native_context_qualified"] = false,
            ["mission_ready"] = false,
            ["scope"] = "The existing shared GeneratedReferenceTemplateImporter was preflighted and exercised only inside the source-host logical EntityManager. Five rifle movement components plus UnitCombat and UnitHealth fields were compared with pinned current source input. Other captured component types remain historical passthrough only. The 32 exact VFX UnityObjectRef leaves remain unconsumed and 26 presentation components are omitted. No Unity World, mission manager, gameplay, replay, source-fit, or readiness authority."
        };
        File.WriteAllText(output, report.ToString(Formatting.Indented) + Environment.NewLine);
        Console.WriteLine("[S003FocusedTemplateSourceHostControls] result=Passed roots=19 entities=647 components=3965 " +
            "unsupportedLeaves=32 omittedVfxComponents=26 cleanup=Passed preflightRejected=2 world=0 mission=0 readiness=0");
        return 0;
    }

    private static int FloatBits(float value) => BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
    private static string FloatBitsHex(float value) => unchecked((uint)FloatBits(value)).ToString("x8");
    private static bool SameBits(float left, float right) => FloatBits(left) == FloatBits(right);
    private static bool ImportedMovementMatches(EntityManager manager, Entity entity, UnitMoveAuthoringProjection expected)
    {
        UnitFootprint footprint = manager.GetComponentData<UnitFootprint>(entity);
        UnitMovementBehavior behavior = manager.GetComponentData<UnitMovementBehavior>(entity);
        UnitVehicleMovement vehicle = manager.GetComponentData<UnitVehicleMovement>(entity);
        UnitVehicleKinematics kinematics = manager.GetComponentData<UnitVehicleKinematics>(entity);
        return footprint.Size.Equals(expected.Footprint.Size) &&
            behavior.AllowIdleWander == expected.MovementBehavior.AllowIdleWander &&
            behavior.UsesVehicleMotion == expected.MovementBehavior.UsesVehicleMotion &&
            SameBits(vehicle.TurnSpeedDegrees, expected.VehicleMovement.TurnSpeedDegrees) &&
            SameBits(vehicle.Acceleration, expected.VehicleMovement.Acceleration) &&
            SameBits(vehicle.Braking, expected.VehicleMovement.Braking) &&
            SameBits(vehicle.RearPivotOffset, expected.VehicleMovement.RearPivotOffset) &&
            SameBits(kinematics.CurrentSpeed, expected.VehicleKinematics.CurrentSpeed) &&
            SameBits(kinematics.StallSeconds, expected.VehicleKinematics.StallSeconds);
    }
    private static void CheckOriginalMovementProjection()
    {
        int cases = 0;
        int[] configured = { -1, 1, 2 };
        float[] sizes = { 0f, -0f, 0.35f, 1f, 1.9999999f, 2f, 2.0000002f, 3.6f,
            float.NaN, float.PositiveInfinity, float.NegativeInfinity };
        foreach (bool vehicle in new[] { false, true })
        foreach (bool autoFootprint in new[] { false, true })
        foreach (bool hasBounds in new[] { false, true })
        foreach (int x in configured)
        foreach (int y in configured)
        foreach (float sizeX in sizes)
        foreach (float sizeZ in sizes)
        {
            int2 expectedFootprint = new int2(math.max(1, x), math.max(1, y));
            if (vehicle && expectedFootprint.x <= 1 && expectedFootprint.y <= 1 && hasBounds)
            {
                int2 modelFootprint = new int2(math.max(1, (int)math.ceil(sizeX)), math.max(1, (int)math.ceil(sizeZ)));
                if (autoFootprint) expectedFootprint = modelFootprint;
            }
            float modelLength = hasBounds ? math.max(sizeX, sizeZ) : math.max(expectedFootprint.x, expectedFootprint.y);
            UnitVehicleMovement expectedVehicle = new UnitVehicleMovement {
                TurnSpeedDegrees = vehicle ? 180f : 720f,
                Acceleration = vehicle ? math.max(6f, modelLength * 3f) : 999f,
                Braking = vehicle ? math.max(8f, modelLength * 4f) : 999f,
                RearPivotOffset = vehicle ? math.max(0.35f, modelLength * 0.22f) : 0f
            };
            UnitMoveAuthoringProjection actual = UnitMoveAuthoringProjection.FromAuthoredMovement(
                4.8f, 1.7f, 1.15f, 0.05f, x, y, autoFootprint, vehicle, true, hasBounds, sizeX, sizeZ);
            Need(actual.HasMovementSlice && actual.Footprint.Size.Equals(expectedFootprint) &&
                actual.MovementBehavior.AllowIdleWander == 1 && actual.MovementBehavior.UsesVehicleMotion == (vehicle ? 1 : 0) &&
                SameBits(actual.VehicleMovement.TurnSpeedDegrees, expectedVehicle.TurnSpeedDegrees) &&
                SameBits(actual.VehicleMovement.Acceleration, expectedVehicle.Acceleration) &&
                SameBits(actual.VehicleMovement.Braking, expectedVehicle.Braking) &&
                SameBits(actual.VehicleMovement.RearPivotOffset, expectedVehicle.RearPivotOffset) &&
                SameBits(actual.VehicleKinematics.CurrentSpeed, 0f) && SameBits(actual.VehicleKinematics.StallSeconds, 0f),
                "Shared movement projection differs from original baker formulas at a vehicle/bounds boundary");
            cases++;
        }
        Need(cases == 2 * 2 * 2 * 3 * 3 * 11 * 11, "Original baker movement projection boundary controls incomplete");
        UnitMoveAuthoringProjection rifleWithoutBounds = UnitMoveAuthoringProjection.FromAuthoredMovement(
            4.8f, 1.7f, 1.15f, 0.05f, 1, 1, false, false, true, false, 0f, 0f);
        foreach (float size in sizes)
        {
            UnitMoveAuthoringProjection rifleWithBounds = UnitMoveAuthoringProjection.FromAuthoredMovement(
                4.8f, 1.7f, 1.15f, 0.05f, 1, 1, false, false, true, true, size, size);
            Need(rifleWithBounds.Footprint.Size.Equals(rifleWithoutBounds.Footprint.Size) &&
                SameBits(rifleWithBounds.VehicleMovement.Acceleration, rifleWithoutBounds.VehicleMovement.Acceleration) &&
                SameBits(rifleWithBounds.VehicleMovement.Braking, rifleWithoutBounds.VehicleMovement.Braking) &&
                SameBits(rifleWithBounds.VehicleMovement.RearPivotOffset, rifleWithoutBounds.VehicleMovement.RearPivotOffset),
                "Alt04 nonvehicle outputs unexpectedly depend on model bounds");
        }
    }
    private static void CheckProjectionMathBitPatterns()
    {
        int[] patterns = { unchecked((int)0x7fc12345), unchecked((int)0xffc54321), 0x7f800000, unchecked((int)0xff800000),
            0, unchecked((int)0x80000000), 0x3c23d709, 0x3c23d70a, 0x3c23d70b, 1, unchecked((int)0x80000001),
            0x3f800000, unchecked((int)0xbf800000) };
        int compared = 0;
        foreach (int speedBits in patterns)
        foreach (int walkBits in patterns)
        {
            float speed = BitConverter.ToSingle(BitConverter.GetBytes(speedBits), 0);
            float walk = BitConverter.ToSingle(BitConverter.GetBytes(walkBits), 0);
            float road = BitConverter.ToSingle(BitConverter.GetBytes(speedBits), 0);
            float arrive = BitConverter.ToSingle(BitConverter.GetBytes(walkBits), 0);
            float expected = math.min(math.max(0.01f, walk), math.max(0.01f, speed));
            UnitMoveAuthoringProjection actual = UnitMoveAuthoringProjection.FromAuthoredValues(speed, walk, road, arrive);
            Need(SameBits(actual.Speed, speed) && SameBits(actual.WalkSpeed, expected) &&
                SameBits(actual.RoadSpeedMultiplier, road) && SameBits(actual.ArriveDistance, arrive),
                "Pure helper differs bitwise from Unity Mathematics for source-authored float patterns");
            compared++;
        }
        Need(compared == patterns.Length * patterns.Length, "UnitMove math pattern coverage incomplete");
    }
}
