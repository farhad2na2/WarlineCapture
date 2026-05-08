Gate: Gameplay M01 sprite-renderer capture update
Status: needs fixes
Reason:
- The updated capture is improved: unit/decor sprites no longer appear as map-fragment rectangles, and the player squad is visible on the tactical ground.
- The capture is still not accepted as final close tactical evidence because the hostile patrol is clipped at the far right edge of the frame. QA/HCI cannot assess hostile readability, unit grounding, shadow direction, or combat spacing from a partially off-screen enemy.
- The Gameplay lane also updated `Design/AgentReports/2026-05-07_gameplay_m01-sprite-atlas-renderer.md` in place instead of writing the requested capture-fix report path. Keep the content, but the next completion should use the requested handoff file so PM can track the iteration cleanly.
Validation accepted:
- Capture no longer shows the previous map-fragment rectangle failure.
- Reported texture-backed quad logging covers player, hostile, and command/decor proxy texture sizes.
- Reported scene-search check says no banned runtime scene lookup calls were found in the touched gameplay runtime files.
Validation still needed:
- Reframe or reposition the capture so player squad, hostile patrol, command/decor proxy, and relevant tactical ground are fully visible inside the frame.
- The hostile patrol must not be clipped by the capture edge.
- Rerun the focused capture builder after framing is corrected.
- Submit the corrected handoff as `Design/AgentReports/2026-05-07_gameplay_m01-sprite-atlas-renderer-capture-fix.md`.
Cross-lane notices:
- QA/HCI remains blocked from using the current capture as final visual evidence.
- UI and Support/FTUE remain unaffected and can stay waiting.
- Final art approval remains separate because current unit/building/tactical-map PNGs are AI-generated and still `exists_needs_review`.
Next gate/task:
- Gameplay should continue the capture-fix task, preserving the improved texture-backed rendering path, and only report complete once the full player/enemy/command composition is visible and inspectable.
