Status: advisory
Topic: M01 hostile patrol readability cannot rely only on tint
Docs reviewed:
- `Design/M01_FirstContact_Production_Contract.md`
- `Design/Art_Asset_Requirements_Register.md`
- `Design/Art_Asset_Requirements_Register.csv`
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
Finding:
- The M01 contract allows `unit.enemy.patrol_01` to reuse an enemy-tinted infantry variant if approved, but the visual gate requires the enemy patrol to read as hostile without relying only on color.
- The art register has rows for the attack marker and destroyed VFX, but this audit did not find a dedicated M01 hostile unit visual variant row or an explicit approval checklist for silhouette/pose/marker/readability differences between `unit.player.rifle_squad_01` and `unit.enemy.patrol_01`.
Why it matters:
- Gameplay can technically pass by tinting the friendly infantry sprite, while QA/HCI or the user can still reject the result as non-AAA or inaccessible because the hostile target is only color-differentiated.
- This creates a likely cross-lane mismatch: Gameplay may treat the temporary hostile tint as acceptable implementation evidence, Art may still consider the hostile unit missing, and QA may block manual HCI/balance because target identity is ambiguous at tactical zoom.
Recommended fix:
- Add an explicit M01 hostile patrol visual approval row or sub-row in the asset register, tied to `unit.enemy.patrol_01`.
- The acceptance rule should require at least one non-color cue: hostile marker/chevron, different silhouette/stance/loadout, outline treatment, readable team indicator, or approved hostile variant sprite. The cue must be visible in the close tactical capture and must not depend on color alone.
- Keep temporary tint allowed only for technical renderer validation, not final QA/HCI pass or art completion.
Affected lanes:
- Gameplay: can keep the current tint for renderer/capture validation, but should not claim final hostile visual approval from tint alone.
- QA/HCI: should explicitly check non-color hostile readability during M01 smoke.
- UI/VFX: may need attack/hostile marker support if Art does not create a distinct hostile sprite immediately.
- Support/FTUE: attack-step `Show Me`/`Do It` should target the runtime enemy id and not compensate for poor visual identity with tutorial text alone.
Needs user decision:
- No immediate product decision needed for the current capture-fix task.
- User/art direction decision will be needed before final M01 visual approval: approve a distinct hostile variant, approve a non-color marker treatment, or approve both.
Next task update needed:
- Not urgent for the current Gameplay capture framing fix.
- Before QA/HCI final smoke, update the relevant lane task or asset register so `unit.enemy.patrol_01` has an explicit hostile readability acceptance rule.
