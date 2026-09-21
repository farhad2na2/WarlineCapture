# Planning artifact generator

`generate_handoff.py` uses only the Python standard library. It reads the existing stable catalog, declared setup rules and source config inventory, then generates the twenty battle packets/index and four CSV companions. It never loads Unity, edits Assets, or marks gameplay accepted.

```sh
rtk proxy python3 Design/Roadmap/Skirmish_Expansion/Tools/generate_handoff.py --write
rtk proxy python3 Design/Roadmap/Skirmish_Expansion/Tools/generate_handoff.py --check
```

Owning inputs: [SCENARIO_CATALOG](../SCENARIO_CATALOG.csv) for stable identity; [MATCH_SETUP](../MATCH_SETUP.md) for force/cost/cap defaults; [objective rules](../OBJECTIVE_IMPLEMENTATION.md), [roster/economy](../ROSTER_AND_ECONOMY_IMPLEMENTATION.md), [map contract](../MAP_IMPLEMENTATION.md) and generator tables for their derived packet values. Update the owning prose and corresponding generator table together after a reviewed design/tuning change. Never patch one generated packet to hide a shared-rule inconsistency. `--check` rebuilds in memory and reports drift without writing.

Outputs: `IMPLEMENTATION_MANIFEST.csv` (120 definitions), `WORK_QUEUE_004_120.csv` (117 remaining after prototype mappings), `INITIAL_SETUP_MATRIX.csv` (360 scenario/size vectors), `ROSTER_SOURCE_AUDIT.csv` (74 observed source configurations), and `Scenarios/*.md` (20 packets plus index). CSVs are design artifacts; the Unity catalog builder must not treat them as accepted runtime publication data.

The inventory intentionally fails if source counts drift from the audited 51 UnitGrid / 23 BuildingDefinition configs. Re-audit additions/removals, update dispositions and the expected count, then regenerate; do not delete real assets to make the count pass. A source hash change likewise prompts review and regeneration. All generated evidence fields remain Planned/Pending; future runtime evidence belongs in versioned publication assets and per-entry AgentReports, not in hand-edited generated status cells that would be overwritten.

The validator checks catalog combinations, stable work ordering, force caps, expected output equality, local Markdown targets and per-entry packet anchors. Passing it proves documentation consistency only. It does not prove source compilation, real movement, gameplay balance, ARIA victories or device performance.
