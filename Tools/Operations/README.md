# Operations host tools

- `check_p0.py` — Linux/host schema and ownership checks. No Unity.
- `check_p1.py` — Linux/host city and profile transaction checks. No Unity.
- `Ensure-OperationsShadowWorktree.ps1` — Programmer 2 later: create/refresh `D:\Projects\WarlineCapture-Operations`. The script's default branch is the P0 branch; point the worktree at the P1 branch before P1 Editor validation.
- `Invoke-OperationsP0Validation.ps1` — Programmer 2 later: P0 Unity validation against that shadow project only.
- `Invoke-OperationsP1Validation.ps1` — Programmer 2 later: P1 Unity validation against that shadow project only.

Do not point Unity at `D:\Projects\WarlineCapture`.
