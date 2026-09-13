# M3 / M4 mobile audit fixes — 13 September 2026

Scope: all 27 findings in `M03_M04_Mobile_Design_Audit_2026-09-13.md`. Editor validation only. Captures remain under `/private/tmp/warline-mobile-fixes-20260913/`, outside Design.

## Delivery checklist

Gameplay and UI fixes are implemented. English/Farsi Editor acceptance is recorded below. Voice delivery (A23, plus two updated M3 lessons) remains incomplete until the prepared paid-service upload is approved; this report is not a complete audio sign-off.

| Finding | Change | Verification |
|---|---|---|
| A01 — Stop lesson undermines the defense | Stop has an explicit Restore Hold follow-up; QA uses the visible ARIA button. | Passed: fresh M3 full guidance, victory, results and campaign return |
| A02 — Camera shows empty road while the selected squad is behind the HUD | A completed squad move recenters the camera on the actual selected survivors. | Passed: fresh M3 full guidance, victory, results and campaign return |
| A03 — ARIA can go blank before the main encounter | Guidance continues through all twelve steps; combat lessons await real milestones. | Passed: fresh M3 full guidance, victory, results and campaign return |
| A04 — M4's playable camera is much too distant | M4 returns to a close command camera after its opening tour. | Passed: close M4 opening/playable view, captured at 16:9 and 20:9 |
| A05 — M4 Show Me does not show where to go | Lessons 6 and 11 frame landing and departure destinations. | Passed: actual Show Me at lessons 6 and 11 frames landing and departure |
| A06 — “Marked” landing/exit areas are not readable on the battlefield | World rings and localized labels show the real landing/departure radii. | Passed: visible, localized landing/departure rings at actual radii |
| A07 — Build balances are authoring placeholders | Build reads current materials, oil, fuel and credits. | Passed: visible Build balances are 100/100 materials, 0 oil, 10,000 fuel and 50,000 credits; no spending on open |
| A08 — One accidental tap can destroy a required unit | Mission actors reject self-destruction; the HUD disables that action. | Passed: protected APC self-destruct rejected during real replay; normal enemy combat can still cause defeat |
| A09 — M3's movement acceptance is more specific than the lesson | Positioning completion follows the selected squad reaching a valid destination. | Passed: real squad positioning accepted in M3 guided victory |
| A10 — M4's mandatory escort-survival rule is under-explained | Escort losses affect the bonus star, not rescue success. | Passed: escort-loss rule and rescue/transfer integration checks |
| A11 — M3 purchase price and budget are incomplete | Cards and details show the authoritative credit price as well as materials. | Passed: visible Tower 50 materials / 22,000 credits; Barrier 15 / 6,000; rifle 20 / 10,000; real purchase acceptance recorded below |
| A12 — Optional defense begins on the wrong purchase | Opening Build requires an explicit item selection, with a selection prompt. | Passed: Build opens with no purchase selected and a localized selection instruction |
| A13 — Farsi still has English in the M3 branch | Additional buildings and soldier descriptions use the shared localization catalog; localized ECS strings honor UTF-8 capacity. | Passed: visible Farsi building cards, labels and rifle catalog; first-activation atlas defect fixed and recaptured |
| A14 — Contact pressure silently skips teaching | The convoy waits through preparation; optional lessons require an explicit skip. | Passed: fresh M3 full guidance, victory, results and campaign return |
| A15 — Casualties do not produce trustworthy selection feedback | Dead units cannot remain focused; solid-color health bars reflect health, and casualty feedback is explicit. | Passed: dead-focus and health bar regressions; visible M3 losses/result |
| A16 — M3 finale reuses opening instructions | The finale has victory copy and frames surviving actors. | Passed: M3 finale centers survivors, victory copy, stale warning absent |
| A17 — Radar and building decisions lack a demonstrated payoff | Guide and debrief explain radar/defense tradeoffs while preserving the no-spend route. | Passed: no-spend/no-ping 3-star victory and localized tactical debrief |
| A18 — M4's minimap overstates the useful rescue roster | M4 minimap filtering uses the active mission roster. | Passed: live M4 roster/minimap captures |
| A19 — Jet shortcut is enabled but does nothing useful | Squad-category availability follows live APC/helicopter/escort units; empty Jet is disabled. | Passed: M4 class mask and visible disabled Jet at both landscape ratios |
| A20 — M4's mission panel spends space inefficiently | Compact metrics replace the large central action panel; large navigation controls live under ARIA. | Passed: M4 en/fa HUD captures at 16:9 and 20:9 |
| A21 — Selection lesson advances with only one specialist | The selection lesson requires all four specialists. | Passed: real projection stays at lesson 4 with one selected, advances with four |
| A22 — Unload is a gap in contextual assistance | Do It opens the passenger drawer; unloading is a separate visible action. | Passed: ARIA opens the visible passenger drawer with all four still aboard; actual Exit All unloads them before normal helicopter boarding |
| A23 — M4 tutorial has no ARIA narration path | M4 narration uses its own event IDs; audio generation awaits explicit payload approval. | Awaiting approval of the 42-clip ElevenLabs payload |
| A24 — Instruction copy includes implementation commentary | M4 instructions and the M3 final instruction are shorter; shared keys remain the source of translated copy. | Passed: all 96 M3/M4 tutorial presentations (2 languages × 2 text sizes × 24 lessons) |
| A25 — Rescue pacing can bypass the threat entirely | The patrol gets a visible warning tied to complete APC boarding, with the authored two-minute fallback. | Passed: real boarding-triggered patrol; four enemies defeated during rescue; idle replay/retry fails through normal combat |
| A26 — Extraction finale loses its hero vehicle | The helicopter continues outbound through the normal movement queue; the finale follows it. | Passed: real outbound aircraft movement and a close finale with rescue-specific Farsi copy; final camera capture confirms the stale panel and world rings are hidden |
| A27 — M4 results do not make the quality of the rescue clear | Results show specialists rescued and earned/missed rescue, escort and time stars. | Passed: EN/FA victory and defeat at 16:9 and 20:9; 4/4 rescued, star criteria, receipt, three saved unlocks and Campaign return |

## Accepted Editor validation

- `/private/tmp/warline-fix-m3-final.log`: full guided defense victory at **195.642 combat seconds**. All 12 lessons; actual Stop and Restore Hold checked on all eight rifles; no optional purchases or pings; all seven hostiles stopped; one rifle loss, no civilian losses and an undamaged post; three stars. Three debrief panels, visible English/Farsi results, actual Continue to Campaign, prior actor cleanup, persisted M4 availability and first-clear rewards.
- `/private/tmp/warline-fix-m4-recovery-final.log`: full rescue plus guide and recovery acceptance. All 12 guide topics and 57 classes in both languages; 16:9 and 20:9 HUD; guide pause/resume. Actual APC boarding, driving, opening the passenger drawer, unloading, helicopter boarding, 20-second clear landing and normal airborne departure. All four specialists rescued, all four hostiles defeated, no escort losses. Visible bilingual results, persisted three unit unlocks and actual Campaign return. Replay rejects protected self-destruct; idle replay and retry lose through normal combat at 4x simulation; retry has a clean roster and no duplicate rewards. No health, position, outcome or completion-fact injection.
- `/private/tmp/warline-fix-architecture-acceptance.log`: **139/139 architecture checks passed**, nine fixtures. No baseline or source-growth ceiling relaxed.
- `/private/tmp/warline-fix-reinforcement-preflight.log`: all **96 tutorial layouts** (M3/M4 × 12 × English/Farsi × standard/large text), first-open passenger lifecycle, specialist identity/compact health, one-versus-four specialist selection, M3 optional reinforcement catalog and zero additional hot-path snapshots passed.
- `/private/tmp/warline-fix-drawers-verified.log`: actual M3 tutorial buttons open both optional drawers. Farsi titles, category tabs, resource captions, prices and instructions render correctly. The Soldiers tab has the configured rifle, not an empty catalog. Opening either drawer leaves 50,000 credits and 100 materials unchanged; the no-purchase path still advances through all first nine lessons. This short capture run is not presented as a separate full victory.
- `/private/tmp/warline-fix-checks4.log`: earlier M3 acquisition/persistence, M4's 14 regression suites and 138/139 architecture tests passed; the remaining array snapshot and long lesson title were fixed and subsequently passed. Failed startup/compile attempts and wrapper timeouts are excluded from accepted evidence.
- Final combined regression run: `/private/tmp/warline-fix-comprehensive-final.log` — **Passed**. All eight grouped validation entry points completed, including five new regressions, the 96 combined tutorial layouts, M3’s 11 suites, M4’s 14 suites, M3 interaction/acquisition/persistence, and 139/139 architecture checks. The final cinematic visibility adjustment was validated separately below.
- Final architecture rerun: `/private/tmp/warline-fix-final-architecture.log` — **139/139 passed** after the last cinematic change.
- Final M4 cinematic cleanup capture: `/private/tmp/warline-fix-m4-cinematic-final.log` — **Passed**. A fresh real rescue completes at 74.022 combat seconds, with four rescued and no losses. The departing helicopter stays visible; the stale patrol panel, world rings and legacy Step 1/1/access labels are absent. Debrief, bilingual result, saved unlocks and Campaign return pass again.

- `/private/tmp/warline-fix-one-click-final.log`: all seven M3 interaction suites passed after the one-action change.
- `/private/tmp/warline-fix-paid-rifle-acceptance.log`: **Passed**. The actual Soldiers tab, rifle card and Recruit button produce four live rifles. The displayed 10,000 credits / 20 materials match the real remaining budget of 40,000 / 80. No production/economy facts were injected. The paid branch uses its own assertions; the separate no-spend journey remains unchanged.
- `/private/tmp/warline-fix-build-suite-final.log`: **28/28 Build popup tests passed**, including catalog filtering, all categories, mobile touch targets, credit detail, selection persistence, pointer blocking, placement/production routing, cancellation and popup lifecycle. Tests explicitly set and restore their expected English locale; bilingual rendering is checked separately through actual captures.

- `/private/tmp/warline-fix-queue-capture.log`: paid recruitment repeated successfully with a settled Farsi queue capture; caption, translated unit name, credit balance and 80/100 materials are readable. The probe waits for the actual Skip button to be presented before clicking it.

## Additional defects caught during implementation

1. Passenger presentation caching ignored the drawer's open state. Opening/closing now applies the cached model immediately, and first activation no longer cancels an explicit open request in Awake.
2. M4's generic civilian/soldier labels obscured the rescue role. The four passengers now read **Specialist / Rescue passenger** from shared translation keys. Compact health is numeric (`55/55`), avoiding a clipped localized prefix.
3. Static Farsi Build labels could sample the previous Latin font atlas during first activation. Localized bindings schedule a single mesh refresh before rendering after activation/font changes; no continuous font rebuild is added.
4. M3's optional reinforcement was omitted because the catalog only looked for a mandatory ProduceUnit objective. Its existing configured rifle is now projected through the defense definition and becomes available when the supplied barracks is ready. This preserves optionality and transaction ownership.
5. Cinematic suspension left legacy Step 1/1/access labels under ARIA. These are hidden during the camera sequence; M4 has separate opening and successful rescue captions.
6. M4's final camera could retain the previous patrol strip for a presentation refresh. Mission controls and world rings are hidden during the camera sequence; the cleared route uses its correct status.

7. A metadata refresh could clear the player’s selected purchase and disable Recruit. The catalog now restores a still-valid selection after refresh. A regression selects a different card, rebinds metadata, and checks that its detail and action remain selected.
8. M3’s optional reinforcement Do It uses the shared one-action path; Show Me can navigate to the rifle tab. It never performs the entire purchase sequence in one click.

## Architecture and delivery

- Mission state, movement, combat, timing, prices and protection remain owned by ECS. UI reads explicit presentation models and issues normal commands.
- Cohesive existing methods were moved into partial files to keep frozen source-size ceilings intact. No architecture guard was weakened.
- Canonical prefab/config rebuilds use `MissionMobileAuditFixBuilder`. Eleven generated assets and metadata for seventeen new source files were copied back and hash-verified against the accepted QA copy. Only intended generated assets and source metadata are copied back; temporary probes, cache-only narrative reordering, logs and evidence images are excluded.
- Unity Hub and the user's Editor were not terminated. Isolated Editor QA used the checked macOS GUI-licensing wrapper. No Android/device QA was performed.

## Outstanding voice delivery

The runtime M4 narration path and event IDs are implemented. **38 M4 clips** (7 narrative lines and 12 tutorial lessons in two languages) and **4 updated M3 clips** (lessons 7 and 12 in two languages) still require generation/import and delivery verification.

The prepared, user-visible payload is `M03_M04_Voice_Update_Payload_2026-09-13.json`. M4 dry-run validation passes: 7 narrative + 12 tutorial lines, two locales, 5,074 characters. No audio was generated or uploaded during this task.

Automatic approval review rejected the paid ElevenLabs upload because “fix all” did not explicitly authorize sending these proprietary scripts to `api.elevenlabs.io`. The approval question is pending. The prior M3 approval covered a different prepared payload and does not authorize this one. Until approval and audio validation, A23 remains open and the changed M3 text/voice alignment is not signed off.


## Practical limits

This verifies the exercised Editor journeys and regressions; it does not establish every possible strategy, human-rated fun, or device performance. No physical mobile touch/audio/performance claim is made. Evidence images remain outside Design. The asset SHA-256 manifest is `/private/tmp/warline-mobile-fixes-20260913/delivered-assets-sha256.json`.
