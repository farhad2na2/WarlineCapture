using System;
using System.Linq;
using Game.Configs;
using Newtonsoft.Json.Linq;

namespace Game.Runtime
{
    // Read-only source-candidate inspection. No view, binding, geometry or admission is created.
    internal static class SourceS006LogicalBindingProbe
    {
        internal static JObject Probe(SkirmishScenarioDefinitionConfig definition,
            SkirmishResolvedSetup setup, SkirmishMissionViewProfile existingView)
        {
            if (definition == null || setup == null || definition.CatalogId != "S006" ||
                setup.CatalogId != "S006" || setup.DefinitionId != definition.DefinitionId ||
                definition.CampaignMap == null || definition.MapLayout == null)
                throw new InvalidOperationException("Exact compiled S006 candidate identity required");

            bool required = SkirmishMissionLogicalBindingCompiler.RequiresBinding(setup);
            bool accepted = SkirmishMissionLogicalBindingCompiler.TryCompile(definition, setup,
                out var compiled, out string error);
            var roles = setup.Forces.Where(x => x.Quantity > 0).Select(x => x.RoleKind)
                .Concat(setup.RoleOverlays.Select(x => x.RoleKind)).Distinct().OrderBy(x => (int)x);
            return new JObject {
                ["source_only"] = true,
                ["binding_required"] = required,
                ["definition_has_binding"] = definition.MissionLogicalBinding != null,
                ["compiler_accepted"] = accepted,
                ["compiler_error"] = error,
                ["compiled_binding_returned"] = compiled != null,
                ["source_operation_map_id"] = definition.CampaignMap.OperationMapId,
                ["layout_id"] = setup.LayoutId,
                ["required_framing_roles"] = new JArray(roles.Select(x => x.ToString())),
                ["existing_view_catalog_id"] = existingView?.CatalogId,
                ["existing_view_definition_id"] = existingView?.DefinitionId,
                ["existing_view_valid"] = existingView != null && existingView.IsValid,
                ["existing_view_matches_candidate"] = existingView != null && existingView.Matches(setup, definition),
                ["runtime_admission"] = false,
                ["source_fit_certified"] = false,
                ["mission_ready"] = false
            };
        }
    }
}
