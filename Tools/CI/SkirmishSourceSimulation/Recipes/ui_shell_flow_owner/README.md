# UI shell flow source-host fixture

Run `python3 Tools/CI/SkirmishSourceSimulation/Recipes/ui_shell_flow_owner/run_source_host_fixture.py <new-output-directory>`.

The runner extracts the original UI shell contract declarations and compiles the current `UiShellFlowSystem` source files against the checked-in `fixture/` ECS host. It rejects an existing output directory and writes a receipt, source pins, compile log, and run log into the requested directory.

The controlled fixture checks three receipt cases: no receipt leaves the transition running; a mismatched sequence is ignored; and an explicitly supplied matching receipt advances the owner state. The host never completes a transition automatically. Gateway registration and audio/music enqueue are recorded boundaries; no UI gateway registration, audio queue mutation, or playback is claimed.

This is a source-host behavior fixture only. It does not execute Unity, gameplay, a mission, the native UI, or a player readiness flow.
