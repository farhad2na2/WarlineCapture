# Building defense target filtering   2026-10-16

Native N37/02 observations isolated TargetSelection at 474.57 935.09 ms (12 samples, median 618.36 ms), compared with TargetCollection at 3.53 7.21 ms and EffectApplication at 0.0066 0.0129 ms. These shared static profiler scopes are not world-exclusive; the observer has measured overhead. This motivates selection-path optimization without proving an FPS gain.

FindBestTargets rejects self, dead/friendly/neutral candidates and candidates beyond the existing horizontal range before combat-policy and attempt/fog lookups. Every remaining candidate still passes the same AllowsTarget gate. Traversal, nearest-four insertion, range comparison and cached-shot revalidation are preserved.

Two new native regressions cover both observers, exact horizontal range boundary with a height difference, friendly/neutral exclusion, hidden/foreign-attempt/outside-range rejection, and fog loss before a cached shot. Existing nearest-four, policy/domain, live/dead, feedback and allocation controls also passed.

Validation: Unity 6000.5.8f1, Android target, GUI licensing, checked InvokeUnityExecuteMethodValidation.ps1, 600-second timeout. Native native-03 caller/wrapper exit 0; exact marker `[BuildingDefenseAttackSystemValidation] result=Passed tests=18`. The 32-tower/740-candidate control reported zero measured managed allocation. Source/version/wrapper pins matched before/after. Scoped git diff --check passed.

Evidence: `Logs/SkirmishProduction/LowMemoryMode/DefensePredicateOrder-01/native-03/` contains pins, resource preflight, raw logs and receipt. Native-01 (missing test namespace) and native-02 (temporary DynamicBuffer indexer) failed compilation and remain preserved; neither counts as a test pass. Both were repaired before native-03.

Pending: comparable actual S005 performance, normal-input ARIA FindBase/result/return, camera acceptance and all mission readiness gates. This step accepts zero whole missions. Separate source-fit review confirmed S026 route start (970,650) lies inside blocking Ground Staging in all three sizes; preserve complete facilities while revising logical outlets. No physical map changes.
