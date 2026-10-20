# Skirmish saga menu — implementation and evidence

Branch: `codex/skirmish-saga-menu`. Runtime/UI checkpoint: `ac03176`; native pointer-driver correction: `16beead`. **Merge awaits owner visual review.**

The native 24-stop battlefield road, objective flags, earned ribbons, ownership/store states, theatre route and objective medal trails follow the approved saga direction. Catalog IDs, ledger semantics, access/launch authority and Campaign map sources remain in use. Legacy Grid code is dormant pending the owner's deletion decision.

## Game view sizing and native UI fixes

The shell's 4800×2160 logical canvas stretched a child whose scale was reset to one, halving fixed-size controls. Composition now uses a 1080-point reference height and fits the parent canvas. Native Play checks measure a 100 px footer and 80 px stops at 2400×1080. The obsolete map-clip anchor reset, duplicate theatre detail overlay and old yellow panel/safe-area perimeter strokes are removed. Stop medallions receive native raycasts.

Briefing content is measured after localization, retains EN 20 / FA 22 pt, and gains a native scroll viewport when needed. The picture respects its adaptive row height. Skirmish sizing is released from the generic typography late-frame reset while RTL/arrow handling remains enabled. The native journey rejects actual briefing overflow before launch. Result first-medal text fits one line.

## Theatre density and interactions

The owner chose to retain both limits and continue enlargement. Authored menu coasts now cover **66.1%** of the open map while retaining the **10% aspect**, **12 px painted halo-gap**, **20 px area-centroid**, card-containment, visible-terrain, leader and native glyph gates. Blue City Crossroads, gold Industrial Basin and teal Airfield Plains follow the mockup palette. The coast template is a menu overlay; no runtime packing optimizer or physical Campaign map geometry was added.

The reserved portrait preview holds the selected zoom cue clear of title/progress. The parked-cue predicate recognizes that interior slot and retains all nonselected-coast, pin, leader, map and detail checks, avoiding an old attachment path that moved Persian Desert Base. Region hit testing follows the polygon, preventing overlapping bounding boxes from selecting the wrong coast. The cue is an actual native Button routed through the existing view model; its hint describes the button action. Native journeys tap the visible marker for selection, then the cue; cold-restart navigation also exercises the footer zoom action.

Unchanged layout callbacks preserve existing theatre pointer targets and avoid recursively reseating them; actual geometry, locale or selection changes still rebuild. The cue uses the existing footer zoom handler, including its presentation refresh. A regression checks button instance identity across repeated layout passes.

A bounded cache shares exact raster sprites by geometry/color bits, avoiding repeated coast/shade texture generation and unbounded native texture growth on refresh. Shader inclusion is authored through Unity serialized APIs so both dynamically found theatre shaders remain referenced in GraphicsSettings. Player builds/device rendering are a separate pending gate.

## Separate acceptance records

| Gate | Latest record |
| --- | --- |
| Focused native menu, access, launch, ledger and model tests | Run 26: **95 passed, 0 failed, 0 skipped** |
| ARIA plan/control checks | **141 cases passed** |
| Strict pure geometry | Full 14: **113 seated, 0 blocked**, passed |
| Native visual matrix | Capture 15: **18 passed** — 16 locale/ratio screens plus FA 16:9 DB and EN 20:9 AP selection regressions |
| Live scale/briefing | Final EN run 14 and FA run 15 passed: footer 100 px, stops 80 px; full briefings at EN 20 / FA 22 pt |
| Actual S002 normal-touch ARIA victory, result, return and disk medal | Final EN run 14 and FA run 15 passed on `ceeb2284…`: actual victories, visible result medals, correct return selection, ribbon and fresh disk reload |
| Cold Editor process medal reload without seeding | EN restart 05 and FA restart 06 passed in fresh PIDs on the same hash; `profileSeeded=0`, native touch navigation |
| Owner visual review | **Pending** |
| Real player/device acceptance | **Pending** |
| Windows filesystem guard lane | **Unverified on this macOS host** |

The static capture progress is a labelled demo fixture. Normal-input journeys use isolated validation profiles and seed 104731; actual S002 victories, result medals, returned ribbons and disk reloads establish their own evidence. No injected model settlement or earlier-candidate win establishes final native readiness.

## Preserved failed and superseded evidence

Every failed/interrupted capture and raw log remains locally under `iterations/`, `tests/`, `normal-input/` and uniquely named S002 traces. Public review copies redact private host/licensing fields, preserve validation markers and record raw/public SHA-256 hashes. Reused directories can contain stale `failure.txt` files; authoritative records are per-run logs and manifests.

Density iterations exposed real title/cue overlap, a stale 4:3 fallback and a hidden cue; all were fixed. Arabic line-height failures, the EN 4:3 label width and a legacy Persian shelf override were fixed without reducing the font floor or geometry limits. The old runtime replay's bounding-box-center approximation was replaced by the native area centroid, tightening 36 px to 20 px. Exterior shelves retain their width requirement; the new interior preview is checked separately with its explicit title/footer bounds and native glyph captures.

Native attempt 12 stopped before launch with `menuControlsTimeout`: its bounding-box-center selection point was occupied by the new valid cue action. The driver now taps the visible marker and still requires EventSystem to hit the exact region button. That failed run remains failed; it is not a mission win.

Optional broader run 18 **failed: 131 passed, 28 failed, 2 skipped**. Twenty failures concern Windows filesystem/junction controls or the correctly refused macOS temporary-directory symlink. Eight prefab-free ledger fixtures hit the newer native camera-readiness path; their setup now explicitly creates a model simulation, with a separate test proving that native initialization still refuses the prefab-free world. The focused passing suite includes those model tests and the refusal test. Windows guards were not weakened or relabeled as passing.

Earlier actual wins on `dfce6e94…` and `92a727e2…` are retained as historical evidence, including their cold reloads. They do not substitute for the final candidate's new journeys. Run 25 finished 94/0/0 before a considered stop; no Editor was terminated. One already-failed obsolete pure replay was explicitly interrupted, with a PID/log receipt.

## Source scope

The three aircraft dependency-stamp updates were measured against exact backups: geometry/binding fields, GUIDs and original strict guards were preserved. See [exact dependency-stamp backups and audit](review/evidence/aircraft-dependency-audit.zip); full originals remain in `normal-input/cache-stamp-preparation/`. Tracked handoff recovery scenes remain unchanged in `Assets/_Recovery`. The source audit has **zero .unity scene diffs**. Existing Campaign physical maps are reused. Inherited P1/P2 and CI/line-ending changes are listed in the source-path manifest.

[Review comparisons](review/REVIEW.md) · [Final native captures](captures/final-15/manifest.json)

Final candidate SHA-256: `ceeb2284efe03f150ffa4e58e8fb3d312e91ff4968fd7d49221e20c2b4c9a0c0`. EN artifact `1effdbc6887a40f3a3e5fe9f1261edca`; FA artifact `f70e9d3adcf34eec8f978fb3f4666739`. [Final validation and exact native proof files](review/evidence/final-validation.json) · [Public log archive manifest](review/evidence/log-copy-manifest.json) · [Source paths](review/pr-source-paths.json).
