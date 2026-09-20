# Skirmish Battle Library — P0 Wireframes

Date: 2026-09-20  
Author: Game Design  
Status: Accepted design handoff for post-E0 UI (Programmer 1 / Astra)  
Authority branch: `codex/m03-radar-warning`  
Upstream: `Design/Roadmap/Skirmish_Expansion/PLAN.md`, `BATTLE_CATALOG.md`, `MATCH_SETUP.md`, `DELIVERY.md` (E7/E8), `MAPS.md`

## Purpose

Mobile-first Skirmish battle-library UX that scales to **120** accepted scenarios now and toward **200** later **without** a flat endless list.

Design only. No implementation in this document. Do not invent parallel public modes. Do not divert E0.1 float work.

## Locked product frame

| Rule | Detail |
|---|---|
| One mode | Skirmish only: library → one briefing → Start Battle |
| Catalog identity | `map × objective × army × start` = 5 × 4 × 3 × 2 = **120** |
| Not catalog rows | Difficulty, size, seed (replay options on the same identity) |
| Custom | Labelled override of the same dimensions — not a second catalog |
| Sandbox | Sibling of the library (Full Arsenal = Sandbox only) |
| Operations | Separate later track — out of scope here |
| Readiness | **Playable** vs **Planned** (catalog). Distinct from in-match R1–R3 and device size gates |
| Unlocking | Entire published catalog selectable; do not gate behind 119 wins |
| Locales | EN + FA authored strings required before any Playable slice ships |

### Map collections (5 × 24)

| Code | Collection (EN working name) |
|---|---|
| DB | Desert Base |
| CC | City Crossroads |
| MP | Mountain Pass |
| IB | Industrial Basin |
| AP | Airfield Plains |

Objectives: **BA** Base Assault · **FC** Frontline Control · **BT** Breakthrough · **CE** Convoy Escort  
Armies: **G** Ground Maneuver · **A** Air Mobile · **C** Combined Arms  
Starts: **F** Field Base · **E** Established Base  

IDs **S001–S120** never change when labels localize or cards reorder.

### Ship earlier vs needs E7

| Can ship earlier (with validated entries) | Needs E7 proper |
|---|---|
| Stable IDs, map-collection shell, filters | Five collections at accepted quality |
| Playable vs Planned honesty | Custom snapshots + Sandbox + Full Arsenal |
| One briefing → Start Battle | Completion / replay identity |
| EN/FA strings, old-setup migration | 24→48→72→96→120 slice releases |
| Placeholders that never claim Playable | E8 per-entry acceptance (no template inference) |

---

## Navigation

```text
Main Menu → Skirmish → [A] Library root → [B] Map collection → card → [C] Briefing → Start Battle
```

Back stacks normally. After match: restore **collection + filters + scroll**, not a blank root.

Landscape mobile. EN/FA mirrored layouts (RTL when product FA standard requires it).

---

## Screen A — Library root (map collections)

**Purpose:** Choose a map collection; show honest Playable counts. No flat list of 120.

```text
┌─ Skirmish ──────────────────────────────────┐
│ ← Back          Skirmish         [?] help   │
├─────────────────────────────────────────────┤
│ Recently Played (horizontal, Playable only) │
│ [thumb S###] [thumb] [thumb]                │
├─────────────────────────────────────────────┤
│ Map collections (vertical, five rows)       │
│ ┌─────────────────────────────────────────┐ │
│ │ [map still]  Desert Base                │ │
│ │              24 battles · 6 Playable    │ │
│ │              BA · FC · BT · CE          │ │
│ └─────────────────────────────────────────┘ │
│ … City Crossroads / Mountain Pass /         │
│   Industrial Basin / Airfield Plains        │
├─────────────────────────────────────────────┤
│ [ Sandbox ]  secondary — not a sixth map    │
└─────────────────────────────────────────────┘
```

### States

| State | Behaviour |
|---|---|
| Loading | Skeleton rows |
| Load error | Localized message + Retry |
| 0 Playable | Collection still tappable; subtitle “None Playable yet” + short reason (e.g. CC terrain) |
| Recently Played empty | Hide the row |

### Rules

- No flat list of 120; no tiny 120-tab strip.
- No Operations entry from this screen.
- Sandbox clearly separate from map collections.
- Favourites may live inside collections later (P3); not required for P0.

### Copy keys

Screen title · `{playable}/{total}` · zero-Playable reason · Sandbox label · error/retry.

### P0 acceptance

Players find a named battle via map collection in ≤3 taps; never by scrolling one list of 120.

---

## Screen B — Collection + battle card

**Purpose:** Browse 24 battles on one map; filter without catalog bloat.

```text
┌─ Desert Base ───────────────────────────────┐
│ ← Maps     Desert Base      🔍              │
├─────────────────────────────────────────────┤
│ Filters (chips)                             │
│ Objective: [All][BA][FC][BT][CE]            │
│ Army:      [All][G][A][C]                   │
│ Start:     [All][Field][Established]        │
│ Ready:     [Playable][Planned][All]         │
│ Default Ready chip = Playable               │
├─────────────────────────────────────────────┤
│ Card list (large, ~2 visible per viewport)  │
│ ┌─────────────────────────────────────────┐ │
│ │ [screenshot]                            │ │
│ │ Title EN/FA (authored, not concatenated)│ │
│ │ [obj] BA  ·  Army G  ·  Field           │ │
│ │ ~12–18 min  ·  ● Playable  ·  ★ --/--   │ │
│ └─────────────────────────────────────────┘ │
│ Planned: same layout, badge Planned,        │
│ Start blocked on briefing; no fake ★        │
└─────────────────────────────────────────────┘
```

### Card anatomy

- Thumbnail (approved still; placeholder OK only while Planned)
- Authored EN/FA title
- Objective icon · army · start
- Duration band
- Readiness badge: **Playable** | **Planned**
- Completion only if Playable (per scenario; Regular slot is enough for P0)
- Stable ID in accessibility/debug only — not the primary label

### Empty / error

| Case | UI |
|---|---|
| Filters match nothing | “No Playable Ground battles on this map yet” + [Clear filters] [Show Planned] |
| Search miss | Same pattern with query echoed |
| Catalog load fail | Retry; no blank screen |
| Missing thumbnail | Neutral placeholder; card still opens |

### Sort

BA → FC → BT → CE, then G → A → C, then Field → Established (catalog order).

### Hard rules

- Difficulty / size / seed **never** appear as chips or extra cards.
- Planned cards may open a **read-only** briefing; they cannot Start Battle.

### P0 acceptance

Default list shows Playable only; Planned cannot be mistaken for launchable.

---

## Screen C — Briefing → Start Battle

**Purpose:** One screen to understand the fight and deploy. Kill legacy multi-control SCN-13 setup chains for catalog play.

```text
┌─ Briefing ──────────────────────────────────┐
│ ← Back                    Desert Base       │
├─────────────────────────────────────────────┤
│ [map overview — routes + public objectives] │
│ Title · objective · army · start            │
├─────────────────────────────────────────────┤
│ Win / lose conditions (exact, localized)    │
│ Your start: force · buildings · resources   │
│ Enemy rules + asymmetry (explicit bullets)  │
│ Roster limits · duration · army cap         │
│ Device: Standard available · War locked …   │
│ [Full force list ▾] optional drawer         │
├─ Advanced (collapsed by default) ───────────┤
│ Difficulty  [Recruit|Regular|Veteran|Cmd]   │
│ Size        [Standard|War|Large War]        │
│ Seed        [Random] [value]                │
│ Defaults: Regular + Standard + Random       │
│ Tip: “Recommended size: War” if catalog says│
│     (tip only — does not change default)    │
├─────────────────────────────────────────────┤
│ Playable: [ Start Battle ]     [ Back ]     │
│ Planned:  [ Unavailable — in development ]  │
│           disabled primary + reason         │
└─────────────────────────────────────────────┘
```

### Replay options (not catalog)

| Control | Values | Notes |
|---|---|---|
| Difficulty | Recruit / Regular / Veteran / Commander | Behaviour only; same stats/costs/start packages |
| Size | Standard / War / Large War | Unsupported sizes disabled with device explanation **before** deploy |
| Seed | Random or value | Seed-only keeps scenario identity |

First-session defaults: **Regular + Standard + Random**. CSV “recommended size” is a tip, not the default.

### Custom gate (P0 minimum)

If the player later changes army / start / resources / asymmetry (when those controls exist): banner **“Custom — will not count as scenario completion.”**  
Seed / difficulty / legal size alone = still catalog identity.

### Planned briefing

Visible for roadmap honesty; **Start Battle** disabled with short reason. Never sell Planned as Playable.

### P0 acceptance

Median path from card tap to match load is **one** briefing screen; no mandatory second setup page for catalog play.

---

## EN / FA

- Authored titles and compact descriptions in **both** EN and FA — not string-assembled (`Map · Objective · …`).
- IDs stable across locale.
- Briefing body fully localized; no mixed-language chrome.
- Empty/error/migration toasts localized (acceptance X31: real phones).
- Ship no Playable library slice without both languages.
- Thumbnails: no baked Latin-only labels in the image art.

---

## Empty / error summary

| Surface | Requirement |
|---|---|
| Root load fail | Retry |
| Collection 0 Playable | Honest reason; still browsable |
| Filter/search empty | Clear filters / Show Planned / Back to maps |
| Unsupported size | Start disabled until a supported size is selected |
| Missing briefing asset | Placeholder; Start still allowed if Playable |
| No dead ends | Every empty/error path has a next action |

---

## Migration of old saved setup

Authority: `MATCH_SETUP.md`, `DELIVERY.md` E1, acceptance X01.

| Rule | Detail |
|---|---|
| On open | Migrate old saved setup to the **supported small preset**, or return safely to setup with a clear message |
| Difficulty enums | Explicit table old Easy/Normal/Hard/Brutal → Recruit/Regular/Veteran/Commander — **never silent remap** |
| Legacy small BA | Evolve toward **S001 / S025**; keep old configs as regression/practice **outside** catalog count |
| Old wins | Must **not** credit victories for materially changed scenario IDs |
| Campaign | Progress unchanged |
| Device | Unavailable size never auto-picks an unsupported combo |

Success signal: fresh install, old save, unavailable device size, and replay/change-setup each land only on supported combos; one session; player sees what changed.

---

## Shared readiness vocabulary

| UI term | Means |
|---|---|
| Playable / Planned | Catalog acceptance |
| Recruit → Commander | Replay difficulty (not a new battle) |
| Standard / War / Large War | Device / session size |
| R1 / R2 / R3 | In-match recruitment only — **never on cards** |

---

## P0 checklist for Programmer 1 (post-E0)

1. Five map collections; no flat 120.
2. Planned cannot Start Battle.
3. Median path: one briefing screen to Start.
4. Filters/selection restored after match.
5. Old setup migrates safely; campaign untouched.
6. EN + FA strings before any Playable slice ships.

## Out of scope (this P0)

Sandbox interior chrome · full Custom builder · Operations · favourites polish (P3) · Strongpoint / Siege paths for 200 · monetization · diverting E0.1 float work.

## Related backlog (P1–P3, not this deliverable)

P1 filters/search polish · thumbnail capture contract · P2 slice-release UX (24→120) · replay-vs-bloat copy · P3 favourites / recommend-next · reserve 200-path without inventing modes.

---

End of P0 wireframe handoff.
