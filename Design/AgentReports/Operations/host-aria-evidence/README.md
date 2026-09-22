# Operations ARIA evidence scaffolding (O001–O003)

Recorded with the Operations ARIA evidence harness gate after P4 coding (`28b955f2` / PR #36). **AriaWon / Playable Operations mission ready are not claimed.**

## Gap verified

P4 host marker `[OperationsP4Validation] result=Passed checks=14` proves coding wiring only. `TryPlayVisibleControlWin` is mission-ID scripted and is **not** ACCEPTANCE AriaWon evidence.

Shipping Watch virtual-touch (`AriaPlayCapability`) has no Operations row yet; adding one needs a Game PM shared-UI seam. Until then, the Operations-owned recordable path is:

1. Public loop observation (`TryReadHud`, node TargetIds, `CopyPublicActors` / facts)
2. Generic `OperationsAriaObjectivePlanner` (no mission-ID switch)
3. `OperationsAriaInputSkills.TryPlayUnassistedWin` / evidence harness through Loop visible-control APIs

## Folder layout (ACCEPTANCE)

```text
Design/AgentReports/Operations/<build>/<mission>/<difficulty>/<seed>/
```

This scaffold uses build id `host-aria-evidence`.

| Mission | Canonical Regular seed | EN scaffold | FA scaffold seed |
|---|---|---|---|
| O001 | 1102 | `operation.o001/Regular/1102/result.en.json` | `9102` |
| O002 | 1103 | `operation.o002/Regular/1103/result.en.json` | `9103` |
| O003 | 1104 | `operation.o003/Regular/1104/result.en.json` | `9104` |

Checked-in JSON files are **PendingAriaWon** templates (hashes/captures placeholders). Host harness can overwrite them with `HostUnassistedVictoryRecorded` for local proof; Programmer 2 still must run Windows capture before catalog `aria_win_acceptance` flips.

## Host proof (Linux / no Unity)

```bash
python3 Tools/Operations/check_aria_evidence.py
```

Required marker:

```text
[OperationsAriaEvidenceValidation] result=Passed checks=8
```

## Windows shadow procedure (Programmer 2)

Use **only** `D:\Projects\WarlineCapture-Operations`. Never open `D:\Projects\WarlineCapture`.

1. Refresh the Operations shadow worktree onto this branch tip.
2. Keep Unity Hub open and signed in.
3. Coding gate (optional refresh):

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsP4Validation.ps1
```

Marker: `[OperationsP4Validation] result=Passed checks=14`

4. Evidence harness gate:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Operations/Invoke-OperationsAriaEvidenceValidation.ps1
```

Marker: `[OperationsAriaEvidenceValidation] result=Passed checks=8`

5. For ACCEPTANCE AriaWon: run Play Mode / Watch on the shadow project for O001–O003 Regular EN (canonical seeds), retain screen capture + input trace, fill `code_hash` / `config_hash` / `capture_path`, then update catalog `aria_win_acceptance` / `evidence_path`. FA Regular seeds `9102–9104` if cheap on the same harness. Device baseline is **not** required for the playable claim (Farhad).

## Status

| Mission | Authored | Host unassisted path | AriaWon | Playable claim |
|---|---|---|---|---|
| O001 | Yes | Runnable via harness | Pending | Not claimed |
| O002 | Yes | Runnable via harness | Pending | Not claimed |
| O003 | Yes | Runnable via harness | Pending | Not claimed |
