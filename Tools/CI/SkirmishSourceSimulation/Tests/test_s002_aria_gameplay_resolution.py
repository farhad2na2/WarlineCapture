#!/usr/bin/env python3
"""Focused guards for presentation-free role selection and original paid ownership."""
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
ADAPTERS = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters"
ARIA = ADAPTERS / "476_SourceAriaPaidRecruitment.cs"
AVAILABILITY = ADAPTERS / "451_SourceCampAvailability.cs"
PRIMARY = ADAPTERS / "453_SourceCampPrimary.cs"
SCOPE = ADAPTERS / "454_SourceCampReadScope.cs"
COSTS = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/availability/templates/UnitCostReads.SourceMethods.cs.txt"
NATIVE_METADATA = ROOT / "Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs"
CATALOG_BINDING = ADAPTERS / "473_GeneratedS002CatalogAvailabilityBinding.cs"
IDENTITY = ADAPTERS / "454_SourceCampReadScope.cs"
PAID_CONTEXT = ADAPTERS / "530_GeneratedS002PaidContext.cs"


class SourceAriaGameplayResolutionControls(unittest.TestCase):
    def test_aria_role_resolution_has_no_ui_catalog_or_public_facts_dependency(self):
        source = ARIA.read_text()
        for forbidden in ("SourceCatalogRows", "BuildDrawerCatalogItem", "CatalogPublicFacts", "sourceFacts", ".Read(category"):
            self.assertNotIn(forbidden, source)
        self.assertIn("availability.TryResolveRoleUnit(role", source)
        self.assertIn("availability.ReadUnitSemantic(prefab,materials)", source)
        self.assertIn("availability.RequestUnitSemantic(offer.Prefab,offer.MaterialsFallback,delivery.TryQueue", source)
        context = PAID_CONTEXT.read_text()
        self.assertIn("new SourceAriaPaidRecruitment(Entities,Session,availability,ActualDeliveryOwner)", context)

    def test_role_resolution_uses_closed_gameplay_identity_domain_and_exact_admission(self):
        source = AVAILABILITY.read_text()
        resolver = source[source.index("internal bool TryResolveRoleUnit("):source.index("internal SourceCampAvailabilityResult ReadUnitSemantic(")]
        self.assertIn("map.Configured", resolver)
        self.assertNotIn("CatalogPrefabStore", resolver)
        self.assertIn("catalog.TryRole(identity.Name", resolver)
        self.assertIn("map.Configured", resolver)
        self.assertIn("identity.Prefab", resolver)
        self.assertNotIn("ConfiguredDefinitionsByPrefab", resolver)
        self.assertIn("costs.TryGetConfiguredUnitRequestMetadata(command,out var metadata)", resolver)
        self.assertIn("if(!metadata.CanRequest)continue", resolver)
        self.assertIn("Ambiguous source role catalog identity", resolver)
        self.assertIn("SourceCampReadScope(em,null,null,map)", resolver)

    def test_recipe_override_and_costs_use_original_unit_metadata_semantics(self):
        source = COSTS.read_text()
        binding = CATALOG_BINDING.read_text()
        identity = IDENTITY.read_text()
        native = NATIVE_METADATA.read_text()
        self.assertIn("TryGetConfiguredUnitRequestMetadata(SourceUnitPrefab prefab", source)
        self.assertIn("return TryGetUnitDefinitionMetadata(prefab,out metadata)", source)
        self.assertIn("SourceUnitGameplayRequestMetadata", source)
        self.assertIn("gameplayMetadata.TryGet(prefab,out var authoring)", source)
        self.assertNotIn("store.TryGetUnit", source)
        decoder = binding[binding.index("internal static SourceUnitGameplayRequestMetadataStore DecodeGameplayRequestMetadata"):binding.index("internal static List<LogicalCatalogPrefab> DecodeConfiguredUnits")]
        for field in ("CanRequest", "MaterialsCost", "Price"):
            self.assertIn(f'fields["{field}"]', decoder)
        for presentation in ("ConfiguredDisplayName", "ConfiguredDescription", "PortraitSprite", "CatalogUnitAuthoringValues"):
            self.assertNotIn(presentation, decoder)
        self.assertIn("SourceCampGameplayIdentityMap", identity)
        self.assertIn("expectedOrder.Count!=sourceBindings.Count", identity)
        self.assertIn("SourceGameplayPrefabBinding", identity)
        gameplay_identity = identity[identity.index("internal sealed class SourceGameplayPrefabBinding"):identity.index("internal sealed class SourceCampReadScope")]
        self.assertIn("SourceIdentity", gameplay_identity)
        self.assertIn("SourceGameplayPrefabBinding", gameplay_identity)
        self.assertNotIn("CatalogPrefabStore", gameplay_identity)
        self.assertNotIn("LogicalCatalogPrefab", gameplay_identity[:gameplay_identity.index("internal SourceUnitPrefab Command(")])
        self.assertIn("RequireSameConfiguredOrder", gameplay_identity)
        self.assertNotIn("em.Exists(x.Command.Template)", gameplay_identity)
        policy = identity[identity.index("internal static class SourceCampCatalogPolicy"):identity.index("internal static class SourceCampCatalogTokenAllows")]
        self.assertIn("catalog.TryRole(identity.Name,out _)", policy)
        self.assertNotIn("CatalogPrefabStore", policy)
        self.assertIn("var allowed=units.Where(p=>record.TryRole(p.name,out _)).ToArray()", binding)
        self.assertNotIn("completeSourceResolvedGetters", binding[binding.index("internal readonly struct SourceUnitGameplayRequestMetadata"):binding.index("// Same-world")])
        # Existing full-world identity map remains strict for consumers that require a template.
        strict = identity[identity.index("internal sealed class SourceCampIdentityMap"):identity.index("internal sealed class SourceCampGameplayIdentityMap")]
        self.assertIn("em.Exists(x.Command.Template)", strict)
        # The extracted native helper applies configured recipe costs and enables requestability.
        self.assertIn("metadata.Price = recipe.MaterialsCost", native)
        self.assertIn("metadata.CreditsCost = recipe.CreditsCost", native)
        self.assertIn("metadata.CanRequest = true", native)
        self.assertIn("materialsFallback=metadata.Price", AVAILABILITY.read_text())

    def test_direct_request_keeps_original_shared_owner_legality_payment_and_receipt_path(self):
        primary = PRIMARY.read_text()
        direct = primary[primary.index("internal SourceCampPrimaryResult RequestUnitSemantic(\n            SourceUnitPrefab prefab"):primary.index("internal SourceCampPrimaryResult RequestUnitSemantic(\n            BuildDrawerCatalogItem selected")]
        self.assertIn("UnitProductionSemanticRequestOwner.Execute<SourceUnitPrefab, RuntimeBuildingEntity, SourcePendingProduction>", direct)
        self.assertIn("new SourceCampReadScope(em, null, null, map)", direct)
        self.assertIn("delivery.TryQueue", ARIA.read_text())
        self.assertIn("adapter.Pending.ReceiptId == 0", direct)
        self.assertIn("adapter.Pending.ReceiptOwner != session", direct)
        adapter = primary[primary.index("public SourceCampAvailabilityKernel.UnitProductionSemanticRequestOwner.Failure Evaluate("):primary.index("public bool IsConfiguredSpawnable(")]
        self.assertIn("EvaluateUnitFailureInCurrentScope(directPrefab, price", adapter)
        direct_branch = adapter[:adapter.index("var result = owner.Read(selected")]
        self.assertIn("if (selected.Prefab == null)", direct_branch)
        self.assertIn("EvaluateUnitFailureInCurrentScope(directPrefab, price", direct_branch)
        self.assertIn("IsMissionReceipt()", primary)
        self.assertIn("Source mission queue payment belongs to actual shared reserve owner", primary)

    def test_gameplay_only_scope_fails_closed_for_ui_read_facts(self):
        source = SCOPE.read_text()
        self.assertIn("CatalogMetadataReadScope(em,store,facts,record)", source)
        rows = (ADAPTERS / "432_SourceCatalogRows.cs").read_text()
        self.assertIn("Facts??throw new NotSupportedException(\"Presentation/read-model facts are absent from gameplay-only scope\")", rows)
        self.assertIn("Facts=facts;Catalog=catalog", rows)


if __name__ == "__main__":
    unittest.main()
