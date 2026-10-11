# S005 paused-aircraft fix

Normal paid Strike run1018 revealed rotation drift between native Pause and Save. Native1044 reproduced the defect in UnitAirMovementSystem on the first zero-delta update (quaternion delta0.003006684; position delta0).

The shared aircraft system now returns before state writes when simulation delta is nonpositive. Its positive-delta body is otherwise unchanged. Root changed no maps, materials, prices, capacities or deadlines.

Native1046 passed five isolated cases: banked fixed-wing movement and return, VTOL movement, delivery transit and a queued target. Each preserved exact state for120 paused frames and resumed on positive delta. Compilation completed without errors. Actual wrapper/native exit0 and the required marker are preserved in the full log. The CI monitor's13 tests passed; it retains ownership, timeout and completion checks using one polling process.

Validated working-tree source is8417a1d738f2ea61693becedd3e66ba85c2ba7592ee1c31606d5683b3d2869cb. The full source delta also includes independent HUD material, fallback-font and quality-setting changes, preserved separately from this fix; the source-delta JSON lists them. Older proofs retain their original hashes. Normal native Continue/Pause/Save and remaining mission acceptance are pending. S005 remains InProgress and is not player-ready.
