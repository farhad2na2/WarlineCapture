# Owned validation memory guard

The owner explicitly authorized current and future threshold cancellations on 2026-10-16: "Allow current and future threshold cancellations." This applies only to a root-owned, verified validation Editor and its descendants. Unity Hub and unrelated processes remain protected.

`Tools/CI/watch_owned_unity_memory.py` observes available RAM and commit capacity every two seconds. Below 12 GiB free RAM or 16 GiB free commit, it verifies the Editor executable, PID, parent, creation time, full command identity, project, unique log and executeMethod, then rechecks the descendant tree immediately before cancelling that owned tree. Protected processes and foreign-project Editors cause refusal. The durable production-state authorization is required and rechecked before cancellation.

Validation: `[OwnedUnityMemoryWatcherSelfTest] result=Passed controls=14 noProcessesStopped=1`. The native AdvanceMapFocus-01 runs 01, 04, 06 and 07 recorded successful resource-floor cancellation; native05 recorded natural Editor exit with no cancellation. Evidence remains under `Logs/SkirmishProduction/LowMemoryMode/AdvanceMapFocus-01/native-XX/`, including complete wrapper logs and memory JSONL. Runs 04 and 06 compiled before cancellation. Run07 reached the focused controls, failed its later fixture ground-point assertion, restored its scene setup, and crossed a resource floor during shutdown. None supplies a passing mission or focused-suite acceptance.

Heavy work stays sequential. Check resource headroom and project ownership before each checked-wrapper launch; arm this observer immediately after obtaining the exact owned Editor identity. Cancelled runs remain failed evidence. The observer is an operational safeguard, not proof that a future black screen cannot occur. The prior black-screen cause remains unproved.

Do not use desktop capture/input while its separate recovery hold is active. Preserve native screenshots, failed evidence, unrelated changes, Hub and active unrelated Editors.
