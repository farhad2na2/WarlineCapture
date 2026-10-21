# Polished Campaign and Skirmish map menus

Owner approved the refined map-first Skirmish direction and the Campaign A chapter-first flow with five large comic mission cards. Native menu source is committed as `28648094e5da633499bdb1bb94aefad412fe724b`. PR70 remains draft and unmerged; owner visual review of the implemented screens is pending.

[Current native gallery](REVIEW.md) · [40 final Game view captures](20261020-225027/) · [Capture checksums](20261020-225027/sha256.json).

## Implemented behavior

- Campaign: chapter atlas with each chapter's comic cover, then five large individual mission comic cards on its map. The selected preview uses that mission's exact existing comic. Main-page comic coverage remains25 with the existing bindings preserved.
- Skirmish: choose a map, then six large mission thumbnails in each of four scrolling objective regions. Fixed objective tabs, a pinned current-region heading and the next-region command expose the continuation. Five maps × four regions × six catalog slots provides 120 capacity; actual release and ownership gates remain authoritative.
- Shared Back, navy/steel backgrounds, border weights, typography, blue secondary and green primary commands. Selected territories have restrained cyan tint and thin boundaries; decorative yellow rectangular strokes are removed. The gear is centered inside its own Settings box, with its header position preserved.
- Human mission names lead; smaller catalog IDs and army/start profiles remain visible. Four medal slots represent actual difficulty tiers. Green completion checks are separate from cyan selection. Counts bind existing progress/catalog models; Coming Soon never becomes a false playable claim.
- Six released mission thumbnails were recaptured on existing geometry, with distinct camera angles and a matching selected preview. Campaign uses its existing comics. No playable map geometry, mission definitions, budgets or fixed scene bindings were changed.
- QA fixes include narrow-screen text fitting, status/icon overlap, real mission-size control spacing, locked/coming-soon distinctions, access-to-store reachability, region heading stability and translating full Campaign purpose text before shortening it.

## Current candidate validation

| Gate | Result and scope |
| --- | --- |
| Owner mockup direction | Approved; [Campaign polish](../../SkirmishUIConcepts/2026-10-20-menu-alternatives/campaign-polish-04/README.md) and [Skirmish polish](../../SkirmishUIConcepts/2026-10-20-menu-alternatives/skirmish-polish-03/README.md). |
| Owner native visual review | Pending. Review the current gallery before merging. |
| Native visual/input QA | Review14 passed all four pages at 2400×1080, 1920×1080 and 1440×1080 in EN/fa-IR:24 page/ratio/language cases and 40 captures. Input System touch covers selection, difficulty, next region, swipe/pinned heading, Back, Campaign briefing and return. Every active label is checked for truncation; card ink bounds use actual per-character font metrics. Gear center is asserted within 0.5 px of its box center in every case. Layout fixtures do not count as gameplay. |
| Focused automated tests |35 passed, 0 failed, 0 skipped on the frozen final source. Read-only progress across maps, selection/access/launch/result models, medal ledger and main-menu navigation. [Exact XML](polish-evidence/focused-tests-35.xml). |
| Campaign normal input | Fresh M01 journey passed English and Persian actual victories, settlement and Campaign return, with 25 menu touches and Input System touch for briefing, selection drag, commands, world destinations and result action. Outcomes injected:0. The isolated fixture ensures first-launch state plus M01–M05 availability, with no seeded wins/outcomes. Other missions are not certified. [Source and screenshots](polish-evidence/campaign/source.json). |
| Skirmish normal input | Fresh English and Persian S002 runs passed normal-speed ARIA victory, native result, selected S002 return, earned Regular card medal and disk reload. Both launch/finish fingerprints match the frozen candidate. |
| Cold restart | English and Persian both passed in distinct Editor processes: native touch, actual earned card medal and profileSeeded=0. [EN proof](polish-evidence/skirmish-en/cold-restart.json) · [FA proof](polish-evidence/skirmish-fa/cold-restart.json). |
| Player/device, Windows and clean-checkout acceptance | Pending; these worktree checks do not establish those gates. |

Frozen worktree source/configuration fingerprint: `d7b218f1f1107dab322d44bba172b58a51978a98dcd0c6d4096937521736b596`. The native Skirmish runner compares complete launch and finish fingerprints; original thumbnail PNGs and native screenshots have separate checksum manifests.

Native worktree checks include two preserved local differences outside this menu commit: the existing Match HUD prefab and Arabic narrative font asset. Their [path/hash record](polish-evidence/preserved-local-source.json) makes this qualification explicit. Neither is silently included in the PR. Automatic approval review rejected restoring the unrelated HUD prefab because it could discard uncommitted work; it remains intact. A clean-checkout pass remains a separate pre-merge gate.

## Retained evidence and failures

[Fresh gameplay/test/restart log index](polish-evidence/native-run-index.json) and [full logs](polish-evidence/native-validation-logs.zip) · [Current visual run index](polish-evidence/visual-run-index.json) · [Representative failed/manual-review screenshots](polish-evidence/retained-failed-screens.zip) with [source/reason/hash index](polish-evidence/retained-failed-screens.json) · [All14 visual review logs plus thumbnail capture log](polish-evidence/visual-review-logs.zip) · [Thumbnail scope and checksums](polish-evidence/thumbnail-manifest.json).

Polish review01–12 retain failures for a missing CanvasRenderer, cramped progress/status labels, long words, difficulty labels, metadata line boxes, localized spacing and icon overlap. Review13 passed the layout matrix, then manual screenshot review caught shortened English Campaign copy surviving in Persian. Review14 fixes that order of operations, asserts a Persian purpose and passes the full matrix. No failed run is relabelled as a pass.

The earlier implementation `c160864` and evidence commit `da124ec5f` are historical. Their 34-test pass, source fingerprint `43e104e05cc1437e04b31ef27afa9cddf39ea8a4f248900077c4905c9e0602eb`, earlier victories and cold restarts are not used to certify this polish. Their [run index](evidence/run-index.json) remains preserved. Earlier native review failures and Campaign selection-driver/compile failures remain documented there.

The [earlier broader-suite report](../SkirmishBattleLibrary/2026-10-20/STATUS.md) also remains authoritative for its failed optional run18 and Windows filesystem/junction limitations. The focused 35 tests do not claim a rerun of the whole project suite, and model simulations do not count as native victories.
