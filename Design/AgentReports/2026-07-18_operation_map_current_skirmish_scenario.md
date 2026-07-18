# Current Operation Map Standard Skirmish Scenario

Date: 2026-07-18
Scope: first scenario-data asset for the one-physical-map rollout
Result: passed

## Change

Added `ScenarioSetup_Skirmish_DesertBaseStandard` with:

- scenario id `scenario.skirmish.desert_base_standard`
- operation-map id `opmap.skirmish.desert_base_01`
- required faction 1 deployment anchor
- required faction 2 deployment anchor

The asset contains identity and typed anchor requirements only. It does not duplicate the operation-map scene, subscene, placements, static-presentation chunks, or generated metadata.

## Validation

- `CurrentOperationMapScenarioSetupTests`: 1/1 passed.
- `OperationMapContractValidationTests`: 10/10 passed.
- Unity compilation produced no C# compiler errors.
- `git diff --check`: passed.
- Unity validation used the documented out-of-sandbox macOS licensing path.

Feature gates, objectives, starting forces/resources, enemy setup, campaign outcomes, and UI identity projection remain separate Phase 9 work.
