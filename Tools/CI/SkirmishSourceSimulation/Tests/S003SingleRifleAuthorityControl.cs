using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Unity.Entities;

// Separate control for the S003 source-only one-Rifle recipe capability.
// It constructs a current setup with the original compiler and never admits a
// factory, runtime world, prefab bake, or mission.
internal static class S003SingleRifleAuthorityControl
{
    const string GraphSha = "34194993cb3430293923140addf3d19d6222e86a675fb82a2fe06b83f84eaf9f";

    static string Sha(string path)
    {
        using (var sha = SHA256.Create())
        using (var stream = File.OpenRead(path))
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
    }

    static void Need(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    static bool Reject(Action action)
    {
        try { action(); return false; }
        catch (InvalidDataException) { return true; }
        catch (NotSupportedException) { return true; }
        catch (ArgumentException) { return true; }
    }

    internal static SkirmishResolvedSetup CompileCurrentStandardSetup(string root)
    {
        string graphPath = Path.Combine(root, "T Logs/SkirmishProduction/s003-source-graph-extensions-20261103-T26/grid-surface/source-config-graph-with-grid-surface-nodes.json");
        Need(Sha(graphPath) == GraphSha, "Exact frozen T26 S003 source graph required");
        JObject graph = JObject.Parse(File.ReadAllText(graphPath));
        JObject sourcePins = graph["source_pins"] as JObject;
        Need(sourcePins != null && sourcePins.Count > 0, "Pinned T26 authored source graph closure required");
        foreach (JProperty pin in sourcePins.Properties())
        {
            string path = (string)pin.Name;
            Need(Path.IsPathRooted(path) && File.Exists(path) && Sha(path) == (string)pin.Value,
                "Current authored S003 setup source pin changed: " + path);
        }
        var materializer = new SourceConfigMaterializer(graph);
        var catalog = materializer.ByPath<SkirmishExpansionCatalogConfig>((string)graph["catalog_path"]);
        var definition = materializer.ByPath<SkirmishScenarioDefinitionConfig>((string)graph["definition_path"]);
        UnityEngine.Resources.BindSourceResources(new Dictionary<string, object>
            { [SkirmishExpansionCatalogConfig.ResourceName] = catalog });
        string matrixPath = (string)graph["setup_matrix"]["path"];
        string matrixText = File.ReadAllText(matrixPath);
        Need(SkirmishSetupMatrixTable.TryLoadFromText(matrixText,
            out List<SkirmishSetupMatrixRow> rows, out string matrixError),
            "Current pinned original setup matrix must parse: " + matrixError);
        var manifest = new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds };
        Need(SkirmishSetupCompiler.TryCompile(definition, SkirmishDifficultyId.Regular,
            SkirmishSizeId.Standard, 104732, manifest, rows,
            out SkirmishResolvedSetup setup, out List<SkirmishCompileReason> reasons),
            "Original compiler rejected current S003 Standard setup: " + string.Join(",", reasons));
        Need(setup.CatalogId == "S003" && setup.DefinitionId == "skirmish.s003" &&
            setup.Seed == 104732 && setup.SizeId == SkirmishSizeId.Standard &&
            setup.SetupHash == 2394754492u, "Actual original S003 setup object differs from frozen T6 target");
        return setup;
    }

    static RuntimeDefinition Definition(SourceUnitPrefab barrack, SourceUnitPrefab rifle, int quantity = 4)
    {
        return new RuntimeDefinition {
            SourceMetadataComplete = false,
            Prefab = barrack,
            ProductionSlots = new List<RuntimeDefinition.ProductionSlotDefinition> {
                new RuntimeDefinition.ProductionSlotDefinition { SpawnUnitPrefab = rifle, Quantity = quantity }
            }
        };
    }

    static SourceProducerRecipeAuthority Read(string root, RuntimeDefinition definition,
        SourceUnitPrefab barrack, SourceUnitPrefab rifle, SourceUnitPrefab transport,
        SkirmishResolvedSetup setup) => SourceProducerRecipeAuthority.ReadExplicitS003SingleRifleFixture(
            root, definition, barrack, rifle, transport, setup);

    public static int Main(string[] args)
    {
        if (args == null || args.Length != 2) throw new ArgumentException("Repository root and output directory required");
        string root = Path.GetFullPath(args[0]);
        string output = Path.GetFullPath(args[1]);
        Directory.CreateDirectory(output);

        var setup = CompileCurrentStandardSetup(root);
        const string barrackKey = "Building_Barrack";
        const string barrackPath = "Assets/Game/Prefabs/Buildings/Building_Barrack.prefab";
        const string barrackGuid = "683195b1e21c94df7add42ab26cd2a33";
        const long barrackLocalId = 6603657636351658259L;
        const string rifleKey = "Unit_Chr_Soldier_Male_02_Alt_04";
        const string riflePath = "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab";
        const string rifleGuid = "ff73c8b7cbf844778916da330e9cd3e3";
        const long rifleLocalId = 3282182154612916099L;
        const string transportKey = "Unit_Veh_Helicopter_Transport";
        const string transportPath = "Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Transport.prefab";
        const string transportGuid = "e540888d7cd8a489f9472b3456bf2406";
        const long transportLocalId = 5616646512338707422L;

        var barrack = SourceUnitPrefab.FromAssetIdentity(barrackKey, barrackPath, barrackGuid, barrackLocalId);
        var rifle = SourceUnitPrefab.FromAssetIdentity(rifleKey, riflePath, rifleGuid, rifleLocalId);
        var transport = SourceUnitPrefab.FromAssetIdentity(transportKey, transportPath, transportGuid, transportLocalId);
        var definition = Definition(barrack, rifle);
        var authority = Read(root, definition, barrack, rifle, transport, setup);
        var fixtureScope = new SourceCampSingleProducerFixtureScope(77, authority);
        bool exactConfiguredUnitAccepted = fixtureScope.HasExactConfiguredUnitIndex(
            new[] { rifle }, new Dictionary<string, SourceUnitPrefab>(StringComparer.Ordinal) { [rifleKey.ToLowerInvariant()] = rifle });
        bool duplicateConfiguredUnitRejected = !fixtureScope.HasExactConfiguredUnitIndex(
            new[] { rifle, rifle }, new Dictionary<string, SourceUnitPrefab>(StringComparer.Ordinal) { [rifleKey.ToLowerInvariant()] = rifle });
        bool wrongConfiguredUnitKeyRejected = !fixtureScope.HasExactConfiguredUnitIndex(
            new[] { rifle }, new Dictionary<string, SourceUnitPrefab>(StringComparer.Ordinal) { ["wrong_rifle"] = rifle });
        Need(authority.Scope == SourceProducerRecipeAuthorityScope.ExplicitS003SingleRifleSourceFixture &&
            !definition.SourceMetadataComplete && exactConfiguredUnitAccepted && duplicateConfiguredUnitRejected &&
            wrongConfiguredUnitKeyRejected && authority.Matches(definition, 0, rifle, 4) &&
            authority.MatchesTransport(transport), "Pinned S003 authority did not bind the exact partial source definition");

        bool wrongSeedRejected = Reject(() => { int previous = setup.Seed; setup.Seed++; try { Read(root, definition, barrack, rifle, transport, setup); } finally { setup.Seed = previous; } });
        bool wrongHashRejected = Reject(() => { uint previous = setup.SetupHash; setup.SetupHash ^= 1; try { Read(root, definition, barrack, rifle, transport, setup); } finally { setup.SetupHash = previous; } });
        bool wrongOverlayRejected = Reject(() => { var previous = setup.RoleOverlays; setup.RoleOverlays = previous.Where(r => r.RoleId != "role.rifle").ToArray(); try { Read(root, definition, barrack, rifle, transport, setup); } finally { setup.RoleOverlays = previous; } });
        bool wrongProducerRejected = Reject(() => Read(root, Definition(
            SourceUnitPrefab.FromAssetIdentity("Wrong_Barrack", barrackPath, barrackGuid, barrackLocalId), rifle), barrack, rifle, transport, setup));
        bool wrongRifleRejected = Reject(() => Read(root, Definition(barrack,
            SourceUnitPrefab.FromAssetIdentity("Wrong_Rifle", riflePath, rifleGuid, rifleLocalId)), barrack, rifle, transport, setup));
        bool wrongQuantityRejected = Reject(() => Read(root, Definition(barrack, rifle, 3), barrack, rifle, transport, setup));
        bool duplicateSlotRejected = Reject(() => { var x = Definition(barrack, rifle); x.ProductionSlots.Add(new RuntimeDefinition.ProductionSlotDefinition { SpawnUnitPrefab = rifle, Quantity = 4 }); Read(root, x, barrack, rifle, transport, setup); });
        bool fullMetadataShortcutRejected = Reject(() => { var x = Definition(barrack, rifle); x.SourceMetadataComplete = true; Read(root, x, barrack, rifle, transport, setup); });
        bool wrongTransportRejected = Reject(() => Read(root, definition, barrack, rifle,
            SourceUnitPrefab.FromAssetIdentity("Wrong_Transport", transportPath, transportGuid, transportLocalId), setup));
        bool nonNullTemplateRejected = Reject(() => Read(root, definition, barrack, rifle,
            new SourceUnitPrefab(transportKey, new Entity { Index = 991, Version = 1 }, transportPath, transportGuid, transportLocalId), setup));

        Need(wrongSeedRejected && wrongHashRejected && wrongOverlayRejected && wrongProducerRejected &&
            wrongRifleRejected && wrongQuantityRejected && duplicateSlotRejected && fullMetadataShortcutRejected &&
            wrongTransportRejected && nonNullTemplateRejected,
            "At least one S003 authority fail-closed control failed");

        var report = new JObject {
            ["kind"] = "S003SingleRifleAuthorityControl",
            ["status"] = "Passed_SourceOnlySingleRifleAuthority",
            ["actual_original_setup_compiled"] = true,
            ["setup_catalog_id"] = setup.CatalogId,
            ["setup_definition_id"] = setup.DefinitionId,
            ["setup_size_id"] = setup.SizeId.ToString(),
            ["setup_seed"] = setup.Seed,
            ["setup_hash"] = setup.SetupHash,
            ["authority_scope"] = authority.Scope.ToString(),
            ["source_metadata_complete"] = false,
            ["identity_only_templates"] = true,
            ["exact_one_row_configured_identity_accepted"] = exactConfiguredUnitAccepted,
            ["duplicate_configured_identity_rejected"] = duplicateConfiguredUnitRejected,
            ["wrong_configured_key_rejected"] = wrongConfiguredUnitKeyRejected,
            ["wrong_seed_rejected"] = wrongSeedRejected,
            ["wrong_setup_hash_rejected"] = wrongHashRejected,
            ["wrong_role_overlay_rejected"] = wrongOverlayRejected,
            ["wrong_producer_identity_rejected"] = wrongProducerRejected,
            ["wrong_rifle_identity_rejected"] = wrongRifleRejected,
            ["wrong_quantity_rejected"] = wrongQuantityRejected,
            ["duplicate_recipe_slot_rejected"] = duplicateSlotRejected,
            ["full_metadata_shortcut_rejected"] = fullMetadataShortcutRejected,
            ["wrong_transport_identity_rejected"] = wrongTransportRejected,
            ["non_null_transport_template_rejected"] = nonNullTemplateRejected,
            ["factory_closure"] = false,
            ["provider_membership"] = false,
            ["runtime_admission"] = false,
            ["mission_world_created"] = false,
            ["mission_readiness"] = false,
            ["scope"] = "Separate S003 source-only one-Rifle recipe authority control. Uses the current original S003 Standard setup compiler and pinned T6/T17 source evidence; generic Barrack identity does not establish S003 provider membership. No factory closure, prefab bake, runtime admission, mission world, or readiness claim."
        };
        File.WriteAllText(Path.Combine(output, "control.json"), report.ToString(Newtonsoft.Json.Formatting.Indented) + Environment.NewLine);
        Console.WriteLine("[S003SingleRifleAuthorityControl] result=Passed scope=SourceOnlyOneRifle mutants=10 factory=0 membership=0 admission=0 world=0 readiness=0");
        return 0;
    }
}
