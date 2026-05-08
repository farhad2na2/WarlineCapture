Status: needs fixes
Topic: Manual test found M01 tactical ground/map upside down under soldiers
Docs reviewed:
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
- `Design/WarlineCapture_2D_Isometric_Production_Direction.md`

Finding:
The user manually tested the current public M01 launch view and reported that the world/ground map under the soldiers is upside down. This is a Gameplay-owned blocker because the tactical world under the HUD, map loader output, camera/world orientation, metadata anchors, and ECS-backed world presentation belong to the Gameplay lane.

Why it matters:
The earlier brown/tiny-world blocker was improved visually, but a flipped or upside-down ground plate still invalidates player-facing readiness. It can make roads, blockers, objective anchors, spawn positions, minimap mapping, and tap/camera expectations misleading even if units and HUD are visible. QA/HCI should not ask the user for balance or HCI testing on a map whose visual orientation does not match gameplay metadata.

Recommended fix:
Gameplay should fix the M01 tactical ground/map orientation in the runtime map presentation without changing UI canvas/HUD composition. The revised handoff must prove the ground is not upside down, rotated, or mirrored, and that command squad, hostile patrol, roads, objectives, blockers, camera bounds, and minimap mapping still line up after the fix. The fix must respect the ECS world-source rule: only Canvas UI may be non-ECS GameObjects; visible world presentation must be backed by ECS entity/source metadata.

Affected lanes:
Gameplay, QA/HCI, UI.

Needs user decision:
No.

Next task update needed:
Done for Gameplay and QA/HCI current tasks. Gameplay owns the fix/proof. QA/HCI should verify orientation after the revised Gameplay handoff.
