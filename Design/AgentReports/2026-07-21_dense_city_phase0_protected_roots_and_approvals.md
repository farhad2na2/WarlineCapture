# Dense City Phase 0 Protected Roots And Approvals

Date: 2026-07-21
Tracker: `Design/Architecture/dense_city_editor_bake_hybrid_runtime_implementation_tracker.md`
Revision context: post-clear baseline commit `d4ad9cc38` plus refreshed Addressables evidence

## 1. Protected Root Confirmation

Exact authoring-scene candidates captured by `OperationMapPhase0BaselineProbe` schema v2:

| Role intent | Hierarchy path | GlobalObjectId | Present |
|---|---|---|---|
| Grading archive / handmade archive | `DenseCity_GradingArchive[13]` | `GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-902583272-0` | Yes |
| Archive mountains | `DenseCity_GradingArchive[13]/Mountains[1]` | `GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-1836082762-0` | Yes |
| Buildings | `Map[5]/Buildings[18]` | `GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-2444809882377260586-0` | Yes |
| Mountains | `Map[5]/Mountains[4]` | `GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-9027429371825681282-0` | Yes |
| Resource areas | `Map[5]/ResourceAreas[25]` | `GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-2752690170442537164-0` | Yes |
| Roads | `Map[5]/Roads[16]` | `GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-8740025226467099862-0` | Yes |
| Runways | `Map[5]/Runways[24]` | `GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-7060769374196877377-0` | Yes |
| Vehicles | `Map[5]/Vehicles[20]` | `GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-5294699240646147300-0` | Yes |
| Persistent overrides | `AuthoredCityOverrides` | n/a | **Absent; create in Phase 1** |

Generation must preserve the eight present GlobalObjectIds above. It must not create, delete, rename, reparent, or mutate them. `AuthoredCityOverrides` remains a required future authored root and is not disposable generated content.

## 2. Approval Request A: Generated Hierarchy And Semantic Ownership

Approve tracker sections 5 and 6 as normative:

- Authoring hierarchy names and roles under `Generated_GiantDenseMiddleEasternCity_MapBakeSource` and `Generated_GiantDenseMiddleEasternCity_EntityPresentation`
- Existing-map migration destination roots under `AuthoredOperationMapEntityPresentation`
- Semantic generation table mapping each generated feature to presentation ownership, surface/blocker output, and gameplay treatment

Decision: **Approved as written by project owner on 2026-07-21.**

## 3. Approval Request B: Outside-Grid Policy

Tracker default: city content outside the current gameplay grid is presentation-only unless explicitly approved otherwise.

Decision: **Approved default presentation-only by project owner on 2026-07-21.**

## 4. Still Open Before Phase 0 Exit

- Android current-revision device baseline

No owner design decision remains open in this report.
