# Selected Skirmish A — 120-battle navigation iteration

The owner selected **Skirmish A** and **Campaign B**, then requested further iteration before implementation. This packet refines only the selected A atlas/drawer. Campaign B remains the selected reference unchanged. No runtime menu edits were made.

## Why the drawer shows six

The five battlefield groups each contain24 catalog entries. Four objective families divide those24 into six each. The right drawer shows the selected family, while the other families and all five battlefields remain directly selectable.

**5 battlefields ×4 families ×6 missions =120 catalog entries.** Catalog capacity and current playable availability are separate; future entries remain coming soon.

| Battlefield | Catalog range | Per-family groups |
| --- | --- | --- |
| Desert Base | S001–S024 | BA01–06, FC07–12, BT13–18, CE19–24 |
| City Crossroads | S025–S048 | Same four local groups of six |
| Mountain Pass | S049–S072 | Same four local groups of six |
| Industrial Basin | S073–S096 | Same four local groups of six |
| Airfield Plains | S097–S120 | Same four local groups of six |

## Same A screen in four navigation states

1. [Desert Base → Base Assault → S001–S006](01-desert-base-assault.png). Clear breadcrumb120 →24 →6; four family ranges visible.
2. [Desert Base → Frontline Control → S007–S012](02-desert-frontline.png). Only the selected family and six rows change.
3. [Industrial Basin → Base Assault → S073–S078](03-industrial-base-assault.png). Another battlefield selects another24-entry range.
4. [Airfield Plains → Convoy Escort → S115–S120](04-airfield-convoy-last-six.png). The last family reaches the final catalog entryS120 through the same drawer.

The IDs/ranges describe catalog navigation; overlays do not alter physical gameplay maps or mission bindings. Generated artwork/status examples illustrate design only. Actual ready states, mission copy and earned medals will come from the existing native catalog and ledger.

## Selected Campaign B

![Campaign B — mission sectors](../B-campaign-mission-sectors.png)

## Selected A with clearer catalog scope

![Skirmish A — same atlas and mission drawer](01-desert-base-assault.png)

[Exact prompts and PNG hashes](prompts-and-manifest.json). Earlier D/E layouts are not selected and are not being implemented. Continue visual iteration on A/B before proceeding with native menu changes. PR70 remains draft and unmerged.
