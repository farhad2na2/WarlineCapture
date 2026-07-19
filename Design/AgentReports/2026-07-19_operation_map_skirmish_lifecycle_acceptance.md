# Operation Map Skirmish Lifecycle Acceptance

## Scope

Accept the preserved-current-map Skirmish probe and sequential reload requirement using the existing production Menu -> Match -> Menu flow.

## Evidence

- `CurrentOperationMapScenarioSetupTests`: `1 / 1` passed, confirming the preserved map id and both typed deployment anchors.
- `Aph805MenuMatchMenuLifecyclePlayModeTests`: `2 / 2` passed in Editor PlayMode.
- The production route loads `opmap_skirmish_desert_base_01_runtime` through local Addressables.
- The active ECS map publishes `opmap.skirmish.desert_base_01`, `scenario.skirmish.desert_base_standard`, and mission `skirmish`.
- Teardown reaches `AddressablesUnloadComplete`, removes the operation-map ECS root, releases the loaded scene, and preserves the shared World.
- A second complete Match lifecycle succeeds after the first teardown.
- The validation log contains zero compiler errors, exceptions, or invalid Addressables operation handles.

## Result

The Phase 9 preserved-current-map Skirmish probe and sequential reload item is accepted. No new runtime path, scene, map asset, or scenario identity was introduced.
