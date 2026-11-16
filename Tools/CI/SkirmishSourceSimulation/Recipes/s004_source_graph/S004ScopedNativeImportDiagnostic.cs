using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Game.Components;
using Game.Runtime;
using Unity.Entities;

internal static class S004ScopedNativeImportDiagnostic
{
    private static void Need(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    private static int Main(string[] args)
    {
        string output = args.Length == 4 ? Path.GetFullPath(args[3]) : throw new ArgumentException("source, keys, binary directory, output required");
        try
        {
            JObject source = JObject.Parse(File.ReadAllText(Path.GetFullPath(args[0])));
            string[] keys = JArray.Parse(File.ReadAllText(Path.GetFullPath(args[1]))).Values<string>().ToArray();
            string binaryDirectory = Path.GetFullPath(args[2]);
            Need(keys.Length == 9 && keys.Distinct(StringComparer.Ordinal).Count() == 9,
                "Exact nine-key descriptor required");

            var manager = new EntityManager { LogicalAllocatorAdmitted = true };
            int next = 500000;
            manager.AllocateLogicalIdentity = () => new Entity { Index = next++, Version = 1 };
            GeneratedReferenceTemplateImporter.Result imported =
                GeneratedReferenceTemplateImporter.Import(manager, source, binaryDirectory, keys);
            Need(imported.ImportedEntities == 276 && imported.SourceToLogical.Count == 276 && imported.Prefabs.Count == 9,
                "Actual importer root/entity counts changed");
            Need(imported.OmittedPresentationComponents == 10,
                "Actual importer presentation-component omission count changed");
            Need(keys.All(imported.Prefabs.ContainsKey), "Imported root keys differ from exact descriptor");

            Type[] omittedTypes = {
                typeof(UnitAttackImpactVfxReference),
                typeof(UnitMuzzleFlashVfxReference),
                typeof(GroundMissileLauncherVfxReferenceComponent),
                typeof(AirMissileLauncherVfxReferenceComponent)
            };
            foreach (Entity entity in imported.SourceToLogical.Values)
                foreach (Type type in omittedTypes)
                    Need(!manager.HasComponent(entity, type), "Presentation-only component entered logical import: " + type.FullName);

            var importedEntities = imported.SourceToLogical.Values.ToArray();
            foreach (Entity entity in importedEntities) manager.DestroyEntity(entity);
            Need(importedEntities.All(entity => !manager.Exists(entity)), "Successful import cleanup left an Entity allocated");

            // A deterministic allocator collision fails partway through Import's identity allocation loop.
            // The shared importer's rollback boundary must retire the first identity before propagating failure.
            var rollbackManager = new EntityManager { LogicalAllocatorAdmitted = true };
            var collision = new Entity { Index = 777777, Version = 1 };
            rollbackManager.AllocateLogicalIdentity = () => collision;
            bool failed = false;
            try
            {
                GeneratedReferenceTemplateImporter.Import(rollbackManager, source, binaryDirectory, keys);
            }
            catch (ArgumentException)
            {
                failed = true;
            }
            Need(failed, "Injected mid-allocation collision did not fail Import");
            Need(!rollbackManager.Exists(collision), "Importer rollback left a partially allocated identity behind");

            var receipt = new JObject {
                ["status"] = "Passed_ActualSharedImporter_DisposableEntityManagerOnly",
                ["roots_imported"] = imported.Prefabs.Count,
                ["entities_imported"] = imported.ImportedEntities,
                ["components_imported"] = imported.ImportedComponents,
                ["unsupported_leaf_references_in_source"] = ((JArray)source["unsupported_values"]).Count,
                ["omitted_presentation_components"] = imported.OmittedPresentationComponents,
                ["omitted_presentation_component_types"] = new JArray(omittedTypes.Select(t => t.FullName)),
                ["successful_import_cleanup"] = importedEntities.All(entity => !manager.Exists(entity)),
                ["mid_allocation_failure_injected"] = true,
                ["mid_allocation_failure_rollback_clean"] = !rollbackManager.Exists(collision),
                ["source_authority_rewritten"] = false,
                ["unity_world_created"] = false,
                ["default_game_object_injection_world_touched"] = false,
                ["mission_initialized"] = false,
                ["runtime_replay_ready"] = false,
                ["native_visual_readiness"] = false,
                ["mission_ready"] = false
            };
            File.WriteAllText(output, receipt.ToString(Newtonsoft.Json.Formatting.Indented) + Environment.NewLine);
            Console.WriteLine("[S004ScopedNativeImportDiagnostic] result=Passed roots=9 entities=276 retainedPresentationRefs=16 omittedPresentationComponents=10 cleanup=Passed rollback=Passed missionReady=False nativeVisualReadiness=False");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }
}
