# WarlineCapture Architecture & Performance Audit

**Date:** 2026-06-19
**Scope:** `Assets/Game/Scripts`

This document serves as a professional, high-level audit of the `Assets/Game/Scripts` folder, focusing on modern Unity DOTS/ECS practices, architectural integrity, and performance.

***

## 1. Architectural Integrity & Patterns (SOLID & Code Quality)

**✅ Finding:** Excellent Dependency Discipline.
A thorough scan of the codebase reveals **zero** instances of `FindObjectOfType`, `FindAnyObjectByType`, or lazy `public static Instance` singletons in the runtime codebase.
*   **Impact:** The codebase is highly modular, testable, and relies on explicit composition and dependency injection instead of global state. This prevents race conditions and hidden dependencies.
*   **Solution:** Continue strictly enforcing the existing `Architecture/gameplay_solid_ecs_contract.md`.

***

## 2. Modern Unity Systems (ECS / DOTS vs. GameObjects)

**⚠️ Finding:** Over-reliance on `SystemBase` instead of `ISystem`.
A search across the `Systems/` directory reveals over 150 usages of `public partial class [Name] : SystemBase` and **exactly 0** usages of `ISystem`.
*   **Impact:** `SystemBase` is a managed class. This incurs managed memory overhead and prevents the Burst compiler from fully compiling the system execution itself (even if it compiles the `IJobEntity` jobs inside). It also creates unnecessary GC tracking pressure compared to unmanaged structs.
*   **Solution:** Incrementally migrate stateless systems from `class [Name] : SystemBase` to `struct [Name] : ISystem` and utilize `ref SystemState` for updates. This unlocks the maximum performance benefits of the Burst compiler.

**⚠️ Finding:** Hybrid Debt in Components.
Classes such as `MapAuthoredBuildingVisualComponent` and `ModularIsoTileMetadata` inside the `Assets/Game/Scripts/Components/` namespace are implemented as `MonoBehaviour`.
*   **Impact:** This blurs the strict DOTS line between authoring and runtime data. Having systems query or cross the managed/unmanaged boundary to read `MonoBehaviour` data causes significant performance degradation.
*   **Solution:** Transition these to `IComponentData` (using Unity's Managed Components feature if object references are strictly necessary), and strictly confine `MonoBehaviour` usage to the `Authorings/` namespace.

***

## 3. Performance & Memory Management (CPU/GPU/GC)

**✅ Finding:** Clean Runtime Hot Paths (No LINQ).
A search for `using System.Linq;` shows that it is entirely constrained to `Editor/` and `Authorings/` namespaces. There is absolutely no LINQ usage in runtime `Systems/`.
*   **Impact:** Excellent CPU performance with zero per-frame LINQ allocation overhead or boxing/unboxing penalties during gameplay.
*   **Solution:** Maintain this strict boundary.

**⚠️ Finding:** Managed `Update()` usage in VFX.
`Update()` methods are correctly constrained away from core gameplay logic, but are still present in individual visual scripts like `MissileTrailVfxView.cs` and `UnitAttackImpactVfxView.cs`.
*   **Impact:** Having many individual MonoBehaviours running their own `Update()` method causes a high number of native-to-managed boundary crossings every frame, which scales poorly in an RTS with hundreds of missiles and impacts.
*   **Solution:** Move VFX visual updates into a single centralized `Manager` that iterates over an array, or better yet, use an ECS system to update transforms in bulk via `IJobParallelForTransform`.

***

## 4. UI & Canvas Optimization

**⚠️ Finding:** Granular UI Views vs. Canvas Rebuilds.
The project clearly separates UI logic into `Assets/Game/Scripts/UI/Components` with highly granular views (`UIActionButtonView`, `ResourceFlyoutView`, etc.). 
*   **Impact:** While excellent for separation of concerns, heavily dynamic elements like `ResourceFlyoutView` (which move every frame) sharing the same Canvas as static elements (like buttons) will cause the entire Canvas to rebuild every frame, spiking CPU usage.
*   **Solution:** Ensure that dynamic, animating UI elements (like flyouts and damage numbers) are placed on a separate, dedicated `Canvas` component from static HUD elements.

***

## Summary Statement
The project exhibits incredibly strong architectural foundations, particularly regarding dependency injection, lack of singletons, and runtime memory cleanliness (no LINQ). The primary vector for performance optimization moving forward is fully transitioning `SystemBase` classes to Burst-compiled `ISystem` structs and minimizing individual MonoBehaviour `Update()` overhead for visual effects.
