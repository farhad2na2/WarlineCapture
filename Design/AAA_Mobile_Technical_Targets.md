# WarlineCapture AAA Mobile Technical Targets

Date: 2026-05-21

## Purpose

This document turns the AAA mobile promise into concrete validation targets for 3D operation-map gameplay, UI readability, and performance. Use it with `Architecture/performance_regression_contract.md`, `3D_SingleMap_Gameplay_Direction.md`, and `LargeScale_Grid_Movement_Design.md`.

These are design targets, not final certified platform requirements. Tighten them only after real Android baselines exist.

## Target Device Tiers

| Tier | Role | Target |
|---|---|---|
| Baseline Android | Minimum supported experience. | Stable 30 FPS target, readable UI, reduced VFX/LOD, no thermal collapse during short sessions. |
| Recommended Android | Intended quality bar. | Stable 30 FPS with higher unit counts, richer VFX, and full command-base UI. |
| High-end Android | Showcase capture and marketing target. | 60 FPS target where feasible, richer shadows/VFX, larger battles for promotional captures. |

## Frame And Session Budgets

| Scenario | Baseline Target | Recommended Target |
|---|---:|---:|
| Boot to Main Menu | p95 frame under 33 ms after warmup | p95 under 25 ms after warmup |
| Main Menu idle | Stable 30 FPS | Stable 60 FPS where supported |
| M01 select/move/attack | p95 under 33 ms | p95 under 25 ms |
| M01 result flow | p95 under 33 ms | p95 under 25 ms |
| Operation-map steady state | p95 under 33 ms for 10 minutes | p95 under 25 ms for 10 minutes |
| Production-scale stress | No unbounded stalls; p99 documented | p95 under 33 ms with tuned LOD/VFX |

## 3D Operation-Map Scale Targets

| Stage | Friendly Units | Hostile Units | Civilians | Vehicles/Air | Notes |
|---|---:|---:|---:|---:|---|
| M01 | 1 squad | 1 patrol | 0-6 readable context civilians | 0 | Teaching slice, no crowd pressure. |
| Chapter 1 | 1-3 squads | 1-4 groups | 5-20 | 0-4 | Prove readability before larger fights. |
| Operations Week 1 | 1-4 squads | 2-6 groups | 10-30 | 0-6 | District consequence and warnings visible. |
| Production Scale | 4-8 squads | 6-12 groups | 20-60 | 4-12 | Requires LOD, marker pooling, and device-tier fallback. |

## UI And Marker Budgets

- Minimum interactive touch target: 80 px at 1920x1080 reference.
- Required capture aspects: 16:9 and 20:9 for every accepted gameplay-facing screen.
- World markers should be pooled and capped by priority.
- High-priority markers: objective, selected squad, attack target, move destination, civilian-risk warning, threat alert.
- Low-priority markers should fade or collapse when marker density would hide units.
- Critical warnings must not be color-only; pair icon, motion, text, and audio.

## Readability Targets

- Selected unit/squad must be identifiable without zooming into a single soldier.
- Civilian and hostile silhouettes must not be confused at the default battle camera.
- Move and attack markers must remain readable without covering the selected unit.
- Minimap viewport, objective jump, and threat jump must point to the same operation-map metadata anchors.
- UI text must remain readable on 16:9 and 20:9 Android landscape captures.

## Validation Evidence

Every milestone that claims AAA mobile readiness should provide:

- device tier used
- build type
- scenario path
- frame p95/p99/max after warmup
- GC allocation after warmup
- unit/building/projectile/marker/UI object counts
- 16:9 and 20:9 captures for user-facing UI
- thermal/session notes for 10-minute gameplay checks when available

Absence of visible stutter or FreezeDetect logs is not enough.
