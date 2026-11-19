# Windows dev results — W26 step 1: can a Skirmish row host Armor Break? (2026-11-19)

Branch `missions/win-w26-mp-bt-ab` (from integration `491c7b76d`). The Windows dev lead did this task. Read-only code and asset read; no Unity run, no draft, no shared code.

**Outcome: stopped after step 1.** Armor Break needs no new code and no new scene. But no catalog row runs on Armor Break today, and the dump and measurement tools can only run through such a row. Creating that first host needs a decision from you; options in section 3.

## 1. What already works for Armor Break (no shared C# blocks it)
- **Authoring.** `SkirmishMissionAuthoringEntry.Author` takes any `OperationMapDefinition` path given as `campaign_source`. It checks the metadata, local references and the scene GUID only.
- **No source allow-list** in:
  - the setup compiler;
  - the engineering view (camera) profile factory, which works relative to the source;
  - `SkirmishMissionLogicalBindingCompiler`;
  - `SkirmishExpandedLaunchProjection`;
  - `GroundMissionContextFactory.TryResolveApproachKind`, where a map that isn't DB/CC/IB gets `AuthoredLogicalGround`.
  - The hard-coded `opmap.*` checks in the Skirmish runtime cover desert_base, desert_base_01_s005 and cityedgeairfield only.
- **Physical package.** Armor Break (`Assets/Game/GeneratedOperationMaps/CH04M05ArmorBreak/`) has the same pieces as Citywide Alert: Definition, Grid, Surface, Minimap, UnitFixtureRegistry, RuntimeBinding and PreparedEntities scenes. Both are in `OperationMapCatalog_Chapter01`. Camera bounds x 740–2020, z 260–780 (1280×520 m; grid 1280×520 cells).

## 2. What blocks starting
- **No host row.** No registered catalog row uses Armor Break.
  - `Assets/Game/Configs/SkirmishExpansion/StagedS049/` is bound to Armor Break, but it is not in `SkirmishExpansionCatalog.asset`.
  - Every MP binding (S049, S050, S061, S067, …) uses `opmap.ch05.citywide_alert`.
- **The tools need that host.**
  - The layout dump (`RunCatalogLayoutDumpAndExit`) and the area measurement (`RunCatalogAreaMeasurementAndExit`) both launch a registered row through `WARLINE_SKIRMISH_CATALOG`.
  - The measurement refuses a host on another map: "Host map … differs from the draft's campaign_source … host a mission on the draft's map" (`SkirmishS002AriaRunHarness.AreaMeasurement.cs:158-159`). So S050 cannot measure an Armor Break draft.
- **BT and CE can't bootstrap themselves.** Their drafts need a measurement artifact before they can be authored (`SkirmishMissionAuthoringEntry.cs:731-736`). So S061 or S067 cannot become the first Armor Break host. A Base Assault draft needs no measurement (`:694-695`), and the dump runs even if that session hits BlockedSpawn (per the dump code).
- **Python tooling.**
  - `Tools/Missions/test_drafts.py:10-15` maps MP to Citywide Alert only, so an Armor Break MP draft fails that test.
  - `pad_preflight.py` has no Armor Break entry. Passing `--dump <path>` works around that, so no edit is needed.
  - `check_draft.py` and `layout_dump_to_draft.py` work with any source.

## 3. Ways to get a first Armor Break host (your decision)
1. **Re-source a Mountain Pass Base Assault row to Armor Break**, e.g. S049. Data only. This contradicts the W26 entry ("The BA/FC Mountain Pass rows stay on S050"), and it would replace S049's current Citywide evidence. Rows S051–S054 have drafts but no Scenario assets yet.
2. **Shared tooling change (needs the owner's OK in the Windows session).** Let the dump and measurement use the draft's `campaign_source` without a registered host on that map, or register the staged S049 definition as a non-shipping host.
3. **Stop at the source-fit gate.** Then decide the MP BT/CE rows another way.

Whichever is chosen, `test_drafts.py`'s MP source entry also needs a small change or an accepted failure. That file is shared tooling.

## 4. Fit risk (known before any run)
- `Design/AgentReports/SkirmishExpansion/MacSupport/s050-existing-map-fit-review-20261023/proposal.md:9` records that Armor Break's largest clear authored-grid rectangle is **669×56 cells**, at world XZ [1343,616 – 2012,672). Mountain Pass moved to Citywide Alert for that reason.
- So Armor Break may also fail the W26 step 2 test (two defended corridors that must be crossed, and an endpoint past them inside the W23c tap margins). Only a dump will show it.
- Note that Armor Break's camera bounds differ from Citywide's, so the W23c margins must be recomputed (`codex\notes\w23c_edge_margins.py`).
- The dump of a 1280×520 grid will take longer; its hard timeout is 1650 s.

## Open
- Host decision (section 3). Until then, nothing was authored and `missions/win-w26-mp-ce-ab` was not created.
