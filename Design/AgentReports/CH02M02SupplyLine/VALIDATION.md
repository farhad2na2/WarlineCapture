# Supply Line validation record

Mission `saga.ch02.m02.supply_line`, scenario `scenario.ch02.m02.supply_line`, map `opmap.ch02.supply_yard_01`, seed 2002001. macOS Unity 6000.5.2f1. Validation used repository GUI-licensing wrappers in isolated clones. The main Editor and unrelated work were preserved.

## Passed

- `input-watch-05.log`: clean ordinary-input campaign entry, uninterrupted Watch ARIA victory at 303,555 simulation milliseconds, alternate-lane Move order, real Oil/refinery/Fuel hauling, 42 stored Fuel, 20 protected civilian barrels, 20-second hold, mandatory debrief and result return. Wrapper exit 0 and `[SupplyLineInput] result=Passed`.
- All eight rifle soldiers, both haulers and all three buildings survived. Six finite attackers were defeated. No test wrote unit health, resources, enemy positions, route-recovery facts or victory state during this run. Only an isolated test profile was seeded to make the mission available.
- `input-manual-02.log`: uninterrupted manual victory at 263,787 simulation milliseconds using normal Select/Move controls and the reserve button, with Watch ARIA off throughout. Public campaign entry, mandatory debrief and result return passed; wrapper exit 0.
- The same log records all 54 focused regressions passing: objective publication, invalid-chain rejection, first-clear/replay/duplicate rewards across save reload, actual runtime squad/hauler/building/deadline/integrity failures, paused clock/hold, hauling, resource composition and civilian Fuel protection against vehicle consumption. Thirteen additional mission-rule groups also passed.
- `input-watch-04.log` is retained as earlier regression evidence only; its later UI run was interrupted and is **not** victory evidence.
- Demo 2 warehouse, container, loaded pallet and medical crate remain linked vendor instances inside project-owned wrappers; depot geometry has one gameplay owner. Source/output identities and dependency hashes are in `environment_manifest.txt`.
- English and Persian reserve action and stock/protection counters visually inspected in the running Editor (captures included). Three original captioned narrative backgrounds have authored 16:9 and 20:9 crops; EN/FA text is installed. No runtime network TTS.

## Final integration

`final-regression-04.log`: exact final candidate compiled and passed all 54 regressions plus 13 mission-rule groups; wrapper exit 0. Installed 148 changed files after checking all 204 manifest paths against their pre-install SHA-256 hashes. No concurrent conflicts occurred. `installed-files.json` records before/after hashes; backups are under `/private/tmp/warline-supply-line/main-backup`. No vendor assets were modified. The main Editor was left in its existing Match Play session; relaunch the campaign from Menu to load the new content. The final navigation refinement preserves the validated hauler detour behavior while retaining the existing smaller automatic Skirmish vehicle search budget.

## Limits

This is the authored functional-chain implementation permitted by the campaign plan. Free construction is disabled for this mission. Captioned narrative is installed; newly recorded character voices and supported mobile-device performance acceptance are not claimed. Desktop Editor gameplay does not certify D2-V5 device budgets. Full retry/lifecycle stress coverage, all device aspect ratios and economy approval remain separate acceptance work. The existing generic vehicle status label “ASSIGNED” remains English in the Persian HUD.

## Earlier failures and corrections

The mechanics prototype found a blocked refinery placement, shared-map runtime-id ambiguity, a too-small automatic vehicle search area, and a missing objective publication case. Subsequent UI runs found a hidden reserve action and an ARIA wait marker being treated as a tap target. These were fixed and the clean Watch run above supersedes those failed attempts. Full earlier logs remain under `/private/tmp/warline-supply-line/`; the earlier scripted victory is mechanics-only evidence.

The first final-regression invocation used an incorrect namespace; a second attempt encountered an already-open isolated project. Neither is acceptance evidence. The successful final wrapper run above used the correct entry point in a closed isolated project.

Source diff verification passed with Unity-generated empty YAML values excluded from end-of-line whitespace checking (`git -c core.whitespace=-blank-at-eol diff --check`). Plain `git diff --check` flags seven serializer-produced empty values in the HUD prefab; these were retained as authored by Unity.
