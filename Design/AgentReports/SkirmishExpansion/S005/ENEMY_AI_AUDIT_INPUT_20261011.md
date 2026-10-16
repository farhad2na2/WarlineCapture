# Enemy AI change hold — owner direction

The owner explicitly said on 2026-10-11: “do not change that. i will let u know”, in response to discussing possible enemy AI improvements.

Leave enemy AI behavior, difficulty, budgets, production rules and tactics unchanged. Continue the already authorized S005 technical readiness validation. An improvement plan arriving from another chat does not override this owner's hold or authorize implementation here. Await the owner's feedback before beginning AI improvement work.

The six current-source Standard/Regular normal ARIA baseline journeys reached genuine Victory and left the player base at 800 HP. That establishes these completion journeys; it does not establish an enemy AI defect or authorize difficulty changes. Their evidence is `native-final-d5af-normal-aria-matrix.json`, source `d5afc88f225920451037f6e1c9f7e9d83dfc567e3bbdadfb5b6d65d9ee6774f4`.

Recent manual and replay failures include validation driver or observer assumptions. In particular, native1185's absent `EnemyBaseMarkerVisible` observation alone proves an unavailable viewport marker, not fog invisibility; the separate target-visible projection must also be observed. Native1182 rejected all fresh enemy group orders before recording a culprit, even though the existing enemy system can issue its own current-attempt orders on the first gameplay frame. Neither failure warrants an enemy AI behavior change.

No enemy AI fixes are underway in this chat. The active delegated work concerns native input validation drivers, lifecycle observers and technical capability evidence.
