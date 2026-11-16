# S073 original flow owner admission

`SourceS073UiShellFlowBootstrap.Initialize` is an optional adapter for a derived source-host profile that already ran `SourceS073UiShellStateOwnerBootstrap.Initialize` against the retained S073 `EntityManager`. To derive a runner from the owner generator's output, run:

```sh
python3 Tools/CI/SkirmishSourceSimulation/Recipes/s073_ui_shell_flow_owner/generate.py \
  <generated-owner-runner.cs> <new-flow-runner.cs> --receipt <new-receipt.json>
```

The derived runner requires matching `s073_ui_shell_flow_diagnostic: true` and `WARLINE_S073_UI_SHELL_FLOW_DIAGNOSTIC=1` opt-ins, and also requires the existing owner diagnostic opt-ins. The integration runs after the retained host is constructed and after the original owner bootstrap succeeds.

The adapter fails closed unless the owner receipt identifies the original S073 shell owner, confirms the same retained entity manager, and reports the untouched Splash/None default with empty request and presentation state. It then requires exactly one entity with every component and buffer that `UiShellFlowSystem.OnCreate` queries. Only after those checks does it invoke the original `UiShellFlowSystem.OnCreate` on that same entity manager.

The adapter invokes the original `OnCreate` and then exactly one `OnUpdate`, with no injected input. It admits only the original owner's Splash/None, idle, Pending startup state and verifies that the update preserves Splash/Pending and emits no route, loading, popup, presentation, or completion requests. It does not fabricate presentation commands or transition completions, update ARIA state, or claim native UI, player, or readiness behavior. The executable fixture pins the current S073 source-host build receipt, captures the flow gateway/audio/music effect seams in a derived copy of `UiShellFlowSystem.cs`, and adds one source-host-only `World.EntityManager` property required by that compiled S073 adapter. The original shell owner body is unchanged; the flow copy differs only at those four effect calls.

This adapter is deliberately separate from the S073 owner generator and shared CLI/manifests. A consuming derived profile must opt in explicitly and pin this adapter and its source inputs in its own evidence recipe. Run the compile-and-execute fixture with `python3 Tools/CI/SkirmishSourceSimulation/Recipes/s073_ui_shell_flow_owner/run_integration_fixture.py <new-output-directory>`.
