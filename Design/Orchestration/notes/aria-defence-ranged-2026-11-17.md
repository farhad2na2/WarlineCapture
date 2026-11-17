# ARIA home defence vs a ranged raid (missions/aria-defence-ranged) — 2026-11-17

## Trigger
Lane 14, S002 155923 en, Defeat at match 95.78 s.
- An enemy picket tank (group 16) holds at (949, 345), about 66 units from our HQ at (1001, 303), from t=0.
- It is ordered to Attack our base at 66.89 s and deals ~140 HP per 5 s.
- ARIA's touches are identical to lane 12's winning run through action 32. In lane 12 the tank is gone by 56.5 s.
- Home defence saw `threat=False threatGround=False` for the whole raid (base damage only; nothing presented within the 120-unit home radius):
  - round 1 sent squad slot 0 to column-side home ground, away from the tank;
  - round 2 (base critical) spent about 11 s on Home plus card-by-card selection (deselecting and reselecting slot 0), and the base fell before an order.

## Change (public view only, ordinary controls)
1. **Ranged attacker** (SkirmishWatch): during base damage, the nearest presented enemy minimap marker within 130 units of home becomes the target (`AriaPublicHomeThreatPolicy.PreferAttacker`).
   - A fog-visible unit is preferred over a last-seen ghost marker.
   - Only markers that are not already a presented home threat count.
   - A visible unit gets a direct attack tap (-20002). Either kind gets clear ground 18 units from the marker toward home (-20006).
   - No hidden state: the minimap presented contacts only (positions and last-seen flags already shown to the player).
2. **DefenseTarget(view, round)** with base damage:
   - order of preference: attacker unit, then ground beside the attacker;
   - from round 2, far-side home ground (35 units away from the column);
   - then the old chain (column-side home ground, threat ground, threat).
   - A presented minimap home contact keeps its original targets.
3. **Anti-armour defender:** the roster page now exposes `AntiArmorMask` (presented role rocketeer, tank or siege). The single-defender round and the sweep's first card prefer it.
4. **Fast escalation (S002 proportionate defence only; the generic planner is unchanged):**
   - no Home tap when the defence ground is already on screen; it stays available as the one re-centre;
   - no deselecting in the all-group sweep when every selected card is an assault card;
   - order first, then add: the already-selected squads (or one anti-armour card) get Attack + ground immediately, then the rest of the page is selected and ordered (`RecoveryPageOrdered`, bit 16);
   - bounded as before: one re-centre, one retap, give-up/disarm, then a 4 s retry. Yield rules are unchanged (home defence still pre-empts purchases).
5. **Trace:** the `event=home_defense` line now adds `attacker=0|1|2 attackerDistance attackerTap attackerGround farGround antiArmorMask selectedMask`.

## Tests
- New: `AriaHomeDefenseRangedTests` (6 tests). Covers:
  - attacker preference;
  - target order;
  - the anti-armour first defender;
  - escalation's first order within 2 touches with no deselect or Home tap, and the whole page within 8;
  - nothing selected → card, Attack, ground;
  - the generic sweep unchanged.
- `AriaHomeDefenseBaseDamageTests`: three tests now start with the camera away from home (no ground on screen), which is the real lane-14 state.
- All 48 standalone policy tests pass. The Unity compile has not run.

## Expectation and limits
- If the tank is completely fogged (no visible or ghost marker within 130 units), round 1 is unchanged. Round 2 then tries the far side of home before the column side.
- Lane 14 showed the tank as a public "slipping hostile" at 54–59 s, so a ghost marker should exist at 67–79 s (LastSeenExpireSeconds=20). This is not proven: watch `attacker=` on the home_defense lines.
