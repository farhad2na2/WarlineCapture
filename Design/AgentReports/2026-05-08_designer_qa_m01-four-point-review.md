Lane:
Designer / QA

Task:
Review the current M01 build/capture evidence against the four AAA design points: player fantasy, first 10 minutes, readable scale, and cohesive presentation.

Scope:
- Reviewed current public-launch captures in `Design/AgentReports/Captures/2026-05-08_m01-public-launch/`.
- Reviewed route/safe-area capture evidence in `Design/AgentReports/Captures/2026-05-08_m01-route-safe-area/` and `Design/AgentReports/Captures/2026-05-08_m01-safe-area-profile-matrix/`.
- Reviewed latest M01 gameplay, QA/HCI, PM, and art-readiness reports dated 2026-05-08.
- This is a Designer/QA evidence review. It does not change runtime behavior, HUD implementation, mission balance, or art assets.

Overall decision:
M01 is directionally aligned with the intended AAA mobile tactical design, but it is not ready for final design signoff yet. The current evidence supports "continue to focused Gate 4 QA/HCI" rather than "approve M01 as shippable."

The core gameplay and existing UI visual targets should remain unchanged. The needed work is a narrow M01 polish and proof pass: updated premise-facing copy, lower first-screen noise, stronger first-action readability, final public captures from the refreshed QA workspace, and a PM/user decision on temporary infantry art.

Four-point review:

| Point | Current status | Designer/QA read |
| --- | --- | --- |
| Player fantasy | Partial pass | The mission is now compatible with "field commander preparing/executing a targeted operation," but the captured UI still reads more like a generic RTS combat screen than a precision command operation in a civilian district. |
| First 10 minutes | Partial pass / blocked for final proof | The automated public golden path now reaches result, and the opening lethal-fire issue is fixed. Final HCI proof is still blocked until the QA workspace is refreshed and the first-control screen is reviewed interactively. |
| Readable scale | Partial pass | The 16:9 and 20:9 captures show the tactical map, HUD, minimap, squad, and hostile patrol, but the first-control composition still needs clearer unit/selection/enemy readability proof at public camera scale. |
| Cohesive presentation | Partial pass | The HUD direction, command bar, result popup, and tactical feedback language are close to the target. Temporary art, noisy threat feed/copy, selected-panel naming, and final atlas readiness keep it below AAA finish. |

Point 1: Player fantasy

What works:
- The current M01 scope still fits the updated command fantasy: one rifle squad, one hostile patrol, a clear objective, move-to-cover teaching, attack, and result.
- The public route proves the player can act as a field commander through selection, movement, attack, objective completion, and result popup.
- The "prepare and strike with precision" premise does not require changing the completed M01 mechanics or current HUD visual target.

What is not aligned enough yet:
- The captures still use generic or system-like language in places. Examples include `Rifleman Male IV`, generic threat feed entries, and combat alerts that do not clearly frame the operation as targeted action against a hostile faction embedded in a civilian district.
- The first visible gameplay state does not yet sell "preparing to attack" strongly enough. It shows a combat-ready HUD, but not enough intent: confirm contact, move to cover, identify hostile patrol, then strike.
- The civilian-district constraint is present in the design docs, but not strongly legible in the M01 moment-to-moment UI/copy.

Designer/QA recommendation:
- Keep the gameplay unchanged.
- Update M01-facing copy labels to frame the objective as a targeted operation:
  - selected squad label should be operational, not asset-like.
  - objective/support prompt should point to confirm contact, move to cover, neutralize hostile patrol.
  - threat feed should avoid unrelated or higher-scale alerts in the first teaching window unless they are part of M01.

Point 2: First 10 minutes

What works:
- Gameplay reports the public Campaign route now covers Saga Map -> Briefing -> Loadout -> Deploy -> select -> move -> attack -> neutralize -> result popup.
- The opening-control protection fixes the previous critical failure where the hostile patrol could kill the squad before the player understood selection/movement.
- M01 stays at the correct onboarding scope: select, move, attack, objective, result. No vehicles, base, transport, build, or extra player unit type are introduced.

What blocks final signoff:
- QA/HCI accepted the Gameplay handoff only. It explicitly did not produce final Gate 4 closeout.
- The independent QA workspace was stale for the latest handoff, so final HCI review still needs a refreshed run.
- The first-control screen remains visually busy for a first-time mobile player. The player needs to instantly know:
  - what is mine
  - what is hostile
  - where to move first
  - why this action matters

Designer/QA recommendation:
- Do not add new FTUE steps.
- Make the first-control moment quieter and more deliberate:
  - prioritize squad selection state, move target, objective, and one assistant recommendation.
  - suppress or delay nonessential threat feed noise during the first few seconds.
  - validate by observing whether a new player can identify and execute the first move without reading the whole HUD.

Point 3: Readable scale

What works:
- The route/safe-area capture matrix proves the intended HUD regions can fit across 16:9 and 20:9 profiles.
- The map, minimap, objective tracker, command bar, selected panel, assistant entry, feedback states, and result popup have a coherent placement system.
- Gameplay now asserts four distinct soldier renderers under one squad entity, visible selected marker, tactical projectile scale, and atlas-backed runtime presentation.

What is still weak:
- The current public-launch captures need a fresh post-fix review proving the four-soldier squad reads clearly at actual first-control camera scale.
- In the public 16:9 capture, hostile contact is edge-biased and does not yet have strong enough first-read priority for a new player.
- Selection visibility depends on the current marker/cyan presentation, which needs final scale/position proof around the squad formation.
- Final or milestone infantry art is not approved; `FinalAtlasArtReady = 0` remains a known readability risk.

Designer/QA recommendation:
- Keep large-scale grid movement as the long-term design promise, not an M01 scope expansion.
- For M01, prove readable scale with a fresh capture set:
  - match start
  - squad selected
  - move feedback
  - attack feedback
  - result popup
  - 16:9 and 20:9
  - current atlas/marker/VFX state after the latest gameplay changes

Point 4: Cohesive presentation

What works:
- The HUD is recognizably tactical and mobile-oriented: objective top-left, command bar bottom, minimap bottom-right, assistant surface, result modal.
- The result popup is one of the strongest current presentation states. It clearly confirms mission completion and supports the campaign loop.
- The route/safe-area capture sheets show that the UI system has a consistent visual language.

What is not AAA enough yet:
- Some visible labels still feel like internal asset/debug naming rather than shipped game language.
- The threat feed can overstate the scale of the encounter and distract from M01's first teaching beat.
- Temporary infantry art, missing enemy variant clarity, missing final VFX art, and `FinalAtlasArtReady = 0` prevent final cohesion approval.
- The captured screen does not yet strongly communicate the updated premise: a deliberate targeted operation in a populated city space.

Designer/QA recommendation:
- Do a copy/noise pass before final HCI, not a HUD redesign.
- PM/user must decide whether the current soldier sheet is acceptable as temporary Gate 4 art. If accepted, QA/HCI can review it as milestone art with known limitations. If rejected, M01 should not seek final visual signoff until Art provides milestone player/enemy infantry and VFX assets.

Concrete findings:

1. P0 - Final design signoff cannot pass without refreshed Gate 4 HCI evidence.
   - Evidence: QA/HCI accepted the gameplay handoff but explicitly did not produce final closeout; QA workspace state was stale for the latest handoff.
   - Owner: QA/HCI, after PM acceptance and workspace refresh.

2. P1 - First-control screen is still too noisy for AAA first-time readability.
   - Evidence: public-launch captures show full HUD, threat feed, objective, command tray, minimap, assistant entry, unit panel, and combat context all competing at once.
   - Owner: UI + Support/FTUE for copy/noise sequencing if final HCI confirms the issue.

3. P1 - M01 player fantasy copy is not fully aligned with the new offensive command premise.
   - Evidence: current captured labels and alerts do not consistently say "targeted operation against hostile faction in civilian district."
   - Owner: Designer + UI + Support/FTUE copy pass.

4. P1 - Public readability of four-soldier squad, hostile patrol, selected marker, and attack feedback needs final visual proof.
   - Evidence: Gameplay tests assert the runtime behavior, but Art/Atlas and QA/HCI still require public captures proving actual player-facing readability at scale.
   - Owner: Gameplay + Art/Atlas + QA/HCI.

5. P1 - Art readiness is still an explicit design risk.
   - Evidence: `FinalAtlasArtReady = 0`, no final enemy-tinted patrol variant approval, and final VFX assets are not approved.
   - Owner: PM/user decision, then Art/Atlas or Gameplay depending on approval.

No required changes to existing completed gameplay/UI targets:
- Do not change M01 scope.
- Do not add vehicles, bases, transport, city-scale control, or large-scale grid movement to M01.
- Do not redesign the HUD layout.
- Do not rewrite FTUE structure.
- Do not change the current visual target direction.

Required before M01 can be treated as AAA-ready for this slice:
1. PM accepts the latest Gameplay handoff or assigns fixes.
2. QA workspace is refreshed with the latest M01 gameplay state.
3. QA/HCI runs final Gate 4 focused HCI with public-route captures.
4. PM/user approves or rejects temporary infantry art for Gate 4.
5. Designer/UI/Support do a narrow M01 copy/noise pass if final HCI confirms first-control overload.
6. Fresh captures prove first-control readability at 16:9 and 20:9.

Designer verdict:
M01 is on the right design track. It now supports the updated fantasy without requiring gameplay or HUD redesign. Against AAA mobile standards, the slice is not yet presentation-complete: the first-control moment must become more readable, the operation premise must show through the copy, and final captures must prove the current squad/enemy/marker/art state in the active build.
