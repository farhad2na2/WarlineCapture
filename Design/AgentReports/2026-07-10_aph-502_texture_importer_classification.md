# APH-502 Texture Importer Classification Inventory

- Task: `APH-502`
- Status: `incomplete`
- Final buckets accepted: `false`
- Inventory date: `2026-07-10`
- Current semantic revision: `bc0287616ac225de524d836cd8409c4fd0d49eb0`
- Scope: Git-tracked `.meta` files whose current worktree YAML contains `TextureImporter:`
- Tracked worktree clean: `false`
- Import settings changed: none
- Unity run: none

## Corrected Result

This inventory has two independent views. The first is current-revision semantic classification from current tracked importer YAML and path conventions. The second is current-revision inclusion evidence status. Historical evidence never changes the current semantic bucket and is not accepted as current inclusion evidence.

**The seven presented final buckets are not accepted.** They comprise the six chosen semantic categories plus the `excluded/unreferenced` evidence-status bucket. They remain a provisional inventory until current-revision Unity evidence is produced from a fully clean tracked worktree. The report and JSON are explicitly `incomplete` until that condition is met.

### Chosen Current Semantic Bucket

| Mutually exclusive chosen category | Count |
|---|---:|
| UI | 2,880 |
| World albedo | 401 |
| World normal/mask | 37 |
| VFX | 33 |
| Impostor/atlas | 39 |
| Generated source/reference | 74 |
| **Total tracked texture importers** | **3,464** |

The chosen buckets close exactly: `2,880 + 401 + 37 + 33 + 39 + 74 = 3,464`.

### Current Inclusion Evidence Status

| Evidence status | Count | Exact meaning |
|---|---:|---|
| Accepted current inclusion | 0 | Positive inclusion evidence generated for the same revision as the analyzed importer metadata |
| `excluded/unreferenced` | 3,464 | **No accepted inclusion evidence** for the analyzed revision |

`excluded/unreferenced` is retained only as the required output label. It does **not** mean proven excluded, unreferenced, unreachable, unused, or safe to remove/change. Current inclusion is unknown for all 3,464 importers because all available evidence is from another revision or is dirty.

## Revision Gate

The analyzer obtains current `HEAD` and requires a fully clean tracked worktree using the conservative equivalent of:

```sh
git status --porcelain=v1 -z --untracked-files=no
```

Any staged or unstaged tracked-file change rejects all evidence, including unrelated changes, because dependency roots, scenes, settings, code-driven loading, build configuration, and generated evidence can affect inclusion. Untracked files are intentionally ignored because the inventory and script themselves are untracked deliverables and tracked-revision evidence cannot include untracked content.

Positive evidence paths are accepted only when all applicable conditions hold:

1. Evidence status is `complete`.
2. Content residency `baselineCommit` or BuildReport `exactCommit` exactly equals current `HEAD`.
3. A BuildReport has `dirty: false`.
4. The entire tracked worktree is clean; no staged or unstaged tracked file differs from `HEAD`.

Evidence that fails any condition is labeled historical/rejected and contributes zero paths to current evidence status.

Final seven-bucket acceptance is stricter: it requires both a same-revision clean content-residency inventory and a same-revision clean BuildReport that explicitly sets `allIncludedTexturePathsExported: true`. A top-100 BuildReport can contribute positive paths but can never complete APH-502.

The current cleanliness gate is failed by tracked modifications including:

- `Design/AgentReports/architecture_performance_android_apk_build_report.json`
- `Design/AgentReports/architecture_performance_android_apk_build_report.md`
- `Design/Architecture/architecture_performance_hardening_implementation_tracker.md`

Therefore current evidence remains rejected independently of the evidence revision mismatches.

| Evidence file | Evidence revision | Current disposition |
|---|---|---|
| `architecture_performance_content_residency_baseline.json` | `7084805d771142706f340e9f2e52a68570bcb72b` | Historical only; revision mismatch |
| `architecture_performance_android_aab_build_report.json` | `a527e151e9e43a491ba30f4c19a0320dc54faf5c` | Historical only; revision mismatch |
| `architecture_performance_android_apk_build_report.json` | `4c05a2da10bf5117ca592cf8daac05459ab3b74c` | Rejected; revision mismatch and `dirty: true` |

The old residency and BuildReport paths are reported only as evidence metadata. They are not joined to current importer paths and no historical inclusion claim is projected onto current assets.

## Semantic Candidates And Precedence

Semantic candidates are computed for every importer before a chosen bucket is selected, regardless of evidence status. Each JSON row exposes `semanticCandidates`, `semanticAmbiguity`, `chosenSemanticCategory`, `evidenceStatus`, and `evidenceMeaning`.

Candidate membership is intentionally overlapping:

| Semantic candidate | Membership count |
|---|---:|
| UI | 2,976 |
| World albedo | 401 |
| World normal/mask | 46 |
| VFX | 33 |
| Impostor/atlas | 39 |
| Generated source/reference | 74 |

Candidate rules are case-insensitive:

- **Generated source/reference:** `/Generated/` plus a `/Reference(s)/` or `/Source(s)/` segment, or a `reference`/`source` filename token.
- **Impostor/atlas:** `impostor` in the path, an `atlas` filename token, or `/Atlases/`.
- **VFX:** `/Effects/`, `/FX/`, `/VFX/`, or a VFX/particle/muzzle-flash/smoke/glow filename token.
- **World normal/mask:** YAML `textureType: 1`, or a normal/mask/metallic/roughness/occlusion/specular filename token.
- **UI:** nonzero YAML `spriteMode`, YAML `textureType: 8`, or UI/GUI/Interface/Fonts path convention.
- **World albedo:** explicit fallback only when no other semantic candidate exists.

Chosen-bucket precedence is generated source/reference, impostor/atlas, VFX, UI, world normal/mask, then world albedo. UI precedes normal/mask because all seven UI+normal/mask overlaps have `textureType: 8` and `spriteMode: 1`; their `normal`, `mask`, or gas-mask filename tokens are UI concepts rather than normal-map importer evidence. The overlap remains visible.

## Ambiguity And Unclassified Sets

- Ambiguous semantic set: **105 importers**.
- Unclassified semantic set: **empty (0)**.

| Overlapping candidates | Count | Chosen bucket |
|---|---:|---|
| UI + generated source/reference | 74 | Generated source/reference |
| UI + VFX | 22 | VFX |
| UI + world normal/mask | 7 | UI |
| Impostor/atlas + world normal/mask | 2 | Impostor/atlas |

The exact 105 sorted paths are emitted by the default analyzer output and the JSON `ambiguities` array. The sets are also bounded as follows:

- UI + generated source/reference: all 72 files under `Assets/Game/Art/UI/Portraits/Generated/References/` plus the two files under `Assets/Game/Art/UI/Generated/SplashLoading/TargetLockV04Imagegen/Source/`.
- UI + VFX: `Assets/Piloto Studio/Textures/MusicalNotes_Glow.png`; the 19 files under `Assets/Synty/InterfaceMilitaryCombatHUD/Sprites/FX/`; and the two `ICON_SM_Wep_Grenade_Smoke_01_*` files under `Sprites/Icons_Resources/`.
- UI + world normal/mask: `scn02_mode_card_thumbnail_mask_frame.png`; the generated and non-generated `scn08_command_button_normal_frame.png` and `scn08_squad_card_normal_frame.png`; `scn09_build_card_frame_normal.png`; and `ICON_SM_Chr_Attach_Gas_Mask_01_Military.png`.
- Impostor/atlas + world normal/mask: `Bullet_Decal_Atlas_01_Normals.png` and `Bullet_Decal_Atlas_N.png`.

Every one of these 105 rows currently also has evidence status `excluded/unreferenced`, meaning only no accepted current-revision inclusion evidence. The semantic candidates remain visible despite that evidence status.

## Reproduction

Run from the repository root:

```sh
python3 Tools/CI/aph502_texture_importer_classification.py
python3 Tools/CI/aph502_texture_importer_classification.py --json > /tmp/aph502_texture_importers.json
```

The script uses the Python standard library and read-only Git commands. It writes nothing, changes no importer, and does not invoke Unity. JSON output contains all 3,464 sorted rows and the complete ambiguity and unclassified sets.

## Exact Corrections From Review

1. Removed the invalid union of current importer metadata with residency/BuildReport paths from older commits.
2. Added exact-revision and full tracked-worktree cleanliness gates; stale evidence is historical only and dirty BuildReports are rejected.
3. Split mutually exclusive current semantic choice from current inclusion evidence status.
4. Compute all semantic candidates before precedence for every importer, including all 3,464 rows with no accepted evidence.
5. Exposed 105 actual overlaps, including all 74 generated+UI paths and all excluded/no-evidence rows' semantic candidates.
6. Defined `excluded/unreferenced` solely as `no accepted inclusion evidence`; removed claims that those assets are actually excluded, unreferenced, or outside current dependency roots.
7. Added machine-readable/report `status: incomplete` and `finalBucketsAccepted: false`; none of the seven presented buckets is accepted without current clean Unity evidence.

## Residual Limitations And Unity Follow-Up

- Current build inclusion remains unknown until content residency or a complete BuildReport is generated at `bc0287616ac225de524d836cd8409c4fd0d49eb0` with a clean worktree. If `HEAD` changes, evidence must instead match the new revision.
- The current unrelated tracked modifications force evidence rejection. They must be resolved by their owner; this task does not modify or revert them.
- Path/importer semantics cannot prove material slot use. For disputed world albedo, normal/mask, VFX, or UI/VFX overlaps, inspect reverse dependencies and the referencing Material shader property in Unity.
- The available APH-500 JSON files report only the largest 100 included assets. A revision-consistent detailed BuildReport must export every `BuildReport.packedAssets[].contents[].sourceAssetPath` whose object type includes `UnityEngine.Texture2D`; a top-100 list cannot establish absence.
- The historical residency file has 637 unique `Texture2D` paths while its summary says 639. Resolve that generator discrepancy before accepting a regenerated baseline.
- Serialized dependency inventories do not cover every code-driven load. Audit `Resources.Load`, Addressables catalogs/keys, asset bundles, and direct GUID/path lookup, and add any such roots to the residency inventory.
- Generated source/reference is a path convention, not proof of build exclusion. Its 74 paths require the same revision-consistent evidence as every other semantic category.

No import settings should be changed from this inventory alone.
