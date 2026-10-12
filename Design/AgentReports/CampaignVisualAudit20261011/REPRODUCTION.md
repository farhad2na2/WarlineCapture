# Evidence and reproduction notes

The audit checkout is `/Volumes/T7/Projects/WarlineCapture-CampaignVisualAudit`, branch `codex/campaign-visual-audit-20261011`. `workspace.json` records the immutable baseline and Library copy source. The copied Library is an independent APFS copy-on-write clone, not a shared symlink. Editor Pipeline port 7803 isolates this Editor from the main and earlier T7 Editors.

## What changed

- Two audit-only Editor helpers under `Assets/Game/Scripts/Editor/` and their meta files.
- Local Editor Pipeline port configuration for the separate Editor.
- This report, native screenshots, source inventory, receipts, logs and reproduction scripts.
- No gameplay, environment scene, art material, prefab or mission configuration change is part of the audit.

The capture helper's asset-save cycle could serialize two unrelated runtime values into `SCN08_MatchHudContent.prefab`. Every such diff was preserved under `validation/*-hud-side-effect.diff`, then only that audit-owned change was restored. Final git status must contain no modification of this prefab. No changes in the main checkout or earlier T7 checkout were restored or overwritten.

## Native capture procedure

Keep Unity Hub open and signed in. Use the checked repository wrapper; do not use batchmode or invoke Unity directly. The audit Editor remains open on this checkout. Compile helpers through Unity CLI `recompile`, then verify `recompile_status` completed without errors, with `--project-path` pinned to this checkout.

Export the shipping mission inventory via the wrapper's executeMethod `Game.Editor.CampaignVisualAudit20261011.ExportInventory`. Set `request.json` to a new unused run identifier, for example:

```json
{"index":0,"run":"03","warmupSeconds":8,"supplemental":true}
```

Then run from the audit checkout:

```sh
rtk proxy Tools/CI/invoke_unity_macos.sh --reuse \
  --project /Volumes/T7/Projects/WarlineCapture-CampaignVisualAudit \
  --timeout 420 --log /private/tmp/warline-audit-unique-run.log -- \
  -executeMethod Game.Editor.CampaignVisualAudit20261011.CaptureMission
```

A valid image run requires wrapper exit 0, `[CampaignVisualAudit] result=Passed`, a matching active mission ID, native Metal graphics and all expected files. Four standard world views plus a HUD screenshot are written. Supplemental views add to the marker's world-view count. Inspect the actual HUD and `missionOutcome`: a Passed capture can still show a failed mission, as Steel Push does.

The helper creates an isolated unlocked English save profile under the run folder. It dispatches the existing menu's Select/Deploy and narrative Skip actions programmatically. It does not complete objectives, replace forces, issue troop orders or inject victory. Standard objective inspection views use role positions and may focus on hostile groups; they are not an exhaustive tour of every objective. Supplemental destination views use authored anchors.

Never reuse an evidence directory. Preserve failed logs and receipts, then use a different run ID. `reimportSource:true` requests a force synchronous import; it does not relax the packed entity readiness predicate. All four failed Chapter 5 maps still failed after that retry.

## Source previews

`Game.Editor.CampaignSourceVisualAudit20261011.CaptureBlockedSources` opens authored sources only for Chapter 5 missions without a successful runtime capture. It uses the existing native source-rendering utility with authored lighting, verifies unchanged source hashes, and writes `source-previews/`. It does not include runtime-spawned mission facilities or units. A missing object in these images is not evidence that it would be missing in a successful runtime mission.

`CaptureRunwayEnds` separately renders actual runway mesh endpoints in `source-runway-previews/`. The mesh asset's serialized name is `RunwaySurface`; the earlier supplemental scanner looked for the creation-time name `Campaign_RunwaySurface`, so its first two airport runs captured story destinations but no runway endpoints. The separate endpoint pass corrects this evidence gap without changing the maps. Its images are labeled source previews, not runtime screenshots. The raw capture counter says runways=5, meaning five mesh instances across three sources: DenseCity has one, CityEdgeAirfield and GroundedSignal each have two adjoining modules. Ten captured mesh endpoints include internal joins; do not infer five independent airstrips or place thresholds at all ten endpoints.

Source rendering retains detail lighting. The diagnostic source overview suppresses fog/post processing because it exceeds normal gameplay altitude. Runtime overviews retain the native rendering configuration and can therefore be fogged. Neither is a shipping camera limit test.

## Instrumentation limitations and corrections

- V1 captures (initial missions 1–20) do not record outcome or camera settling; actual images were reviewed. Steel Push's result screen overrides its capture pass marker.
- V1's `hour:0` is an unused receipt field, not an actual day/night reading.
- `environment.txt` lists disabled Light components too. Use V2 `lightStates` or the preserved native probes for enabled-light claims.
- One read-only light probe timed out during import and another ran after Play Mode ended. Both are preserved; neither supports a lighting conclusion.
- The source-preview helper initially failed compilation because `ReadOnlySpan` could not be passed directly to LINQ. The helper was corrected to enumerate `ToArray()`. The successful compile receipt and repair note are retained. Production code was unchanged.
- The supplemental runner inherited a misleading `reimport=True` progress/result label. Its actual request omits `reimportSource` and therefore does not reimport. Preserve the original runner output; the final annotation explains this metadata error. Tail runtime-load retries genuinely used reimport.
- The first runway-end wrapper dispatch timed out during assembly reload and never acquired validation ownership. The same checked wrapper was retried after Pipeline became reachable; no Editor was restarted or licensing reset.
- The runs use the PC quality profile on this Mac. No mobile frame-time, memory or player acceptance has been measured.

## Review artifacts

`findings.json` contains all 25 reviewed entries, exact map/source assets and full authored anchors. `FINDINGS.md` presents recommendations per mission. `IMPLEMENTATION_PLAN.md` organizes the work and acceptance gates. `gallery.html` is a standalone local gallery with chapter, evidence and text filters; open it locally or serve this folder on localhost. All image links are relative so the folder can move as a unit.

`evidence-manifest.json` records image/receipt hashes. `instrumentation/` stores the helper versions, orchestration/contact-sheet scripts and review-note inputs. The report generator uses the original `/private/tmp` review-input filenames; restore the saved `review-inputs/` there to reproduce it, or update its input paths. Native capture scripts are historical run descriptions and refuse to overwrite their run directories; assign fresh run IDs before reusing them.

Visual review, automated wrapper/compile evidence, full normal-input completion and device acceptance are separate gates. Only the first two are in scope for this audit; the latter two remain pending.
