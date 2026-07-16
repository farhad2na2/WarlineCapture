# Architecture And Performance Validator Registry

> Rendered from `validator_registry.json`; edit the JSON authority and regenerate.

- Baseline commit: `acf21c7c900a55aa19fdb8a19a5693be38d2c036`
- Baseline tree: `bcaa69b9a83cc5cbee201469eba0d07859212ef9`
- Environment identity: `Design/AgentReports/ArchitectureMaturity/entry_environment.json` (`1750156ad389d4f28a392531d19339a96140da898d5c2dfd1920c38d6486239e`)
- Validators: 24
- Evidence inputs: 7

## Canonical Validators

| ID | Lane | Owner | Responsibilities |
|---|---|---|---|
| `android-build-report` | deferred | `Assets/Game/Scripts/Editor/AndroidBuildReportGenerator.cs::AndroidBuildReportGenerator.GenerateAndWriteReports` | `android-build-provenance-report` |
| `architecture-assembly-boundary` | active | `Assets/Tests/Editor/ScriptArchitectureAlignmentContractTests.cs::ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation` | `assembly-boundary-validation` |
| `architecture-assembly-report` | active | `Assets/Tests/Editor/Aph700AssemblyDependencyReportGeneratorTests.cs::Aph700AssemblyDependencyReportGeneratorTests.CurrentRepositoryReportMatchesAsmdefsAndTrackedArtifacts` | `assembly-dependency-report-parity` |
| `architecture-composition-static` | active | `Assets/Tests/Editor/ScriptArchitectureAlignmentContractTests.cs::ScriptArchitectureAlignmentContractTests.RunBootstrapCompositionGuardrailValidation` | `bootstrap-composition-policy`, `hierarchy-search-policy`, `mutable-static-state-policy`, `runtime-helper-ledger` |
| `architecture-dashboard-freshness` | active | `Tools/CI/architecture_performance_dashboard.py::main --check` | `dashboard-evidence-freshness`, `validator-owner-uniqueness` |
| `architecture-ecs-hotpath` | active | `Assets/Tests/Editor/EcsBurstHotPathArchitectureTests.cs::EcsBurstHotPathArchitectureTests.RunFocusedValidation` | `ecs-burst-hotpath-policy`, `ecs-managed-boundary-classification`, `ecs-type-handle-lifecycle` |
| `architecture-managed-ecs-loops` | active | `Assets/Tests/Editor/NonUiSystemBaseMigrationArchitectureTests.cs::NonUiSystemBaseMigrationArchitectureTests.RunFocusedValidation` | `managed-ecs-exception-inventory`, `runtime-loop-inventory`, `systembase-managed-boundary-policy` |
| `architecture-non-ecs-ownership` | active | `Assets/Tests/Editor/NonEcsSystemConversionArchitectureTests.cs::NonEcsSystemConversionArchitectureTests.RunFocusedValidation` | `direct-mutation-boundaries`, `non-ecs-system-naming`, `state-transition-ownership` |
| `architecture-source-growth` | active | `Assets/Tests/Editor/ProductionSourceGrowthArchitectureTests.cs::ProductionSourceGrowthArchitectureTests.RunFocusedValidation` | `production-source-growth-ratchet` |
| `audio-memory-capture` | deferred | `Assets/Game/Scripts/Editor/AudioMemoryPlaybackCapture.cs::AudioMemoryPlaybackCapture.RunMenu; AudioMemoryPlaybackCapture.RunMatch` | `audio-memory-playback-evidence` |
| `audio-performance` | deferred | `Assets/Tests/Editor/AudioPerformanceValidationTests.cs::AudioPerformanceValidationTests.RunFocusedValidation` | `audio-performance-contract` |
| `behavior-menu-match-menu` | active | `Assets/Tests/PlayMode/Aph805MenuMatchMenuLifecyclePlayModeTests.cs::Aph805MenuMatchMenuLifecyclePlayModeTests` | `menu-match-menu-world-lifecycle` |
| `behavior-placement-production` | active | `Assets/Tests/PlayMode/Aph806BuildingPlacementProductionPlayModeTests.cs::Aph806BuildingPlacementProductionPlayModeTests` | `building-placement-production-flow` |
| `behavior-resource-exchange` | active | `Assets/Tests/PlayMode/Aph807ResourceExchangeFlowPlayModeTests.cs::Aph807ResourceExchangeFlowPlayModeTests` | `resource-exchange-flow` |
| `behavior-selection-move-attack` | active | `Assets/Tests/PlayMode/Aph806SelectionMoveAttackPlayModeTests.cs::Aph806SelectionMoveAttackPlayModeTests` | `selection-move-attack-flow` |
| `behavior-transport` | active | `Assets/Tests/PlayMode/Aph807TransportBoardingFlowPlayModeTests.cs::Aph807TransportBoardingFlowPlayModeTests` | `transport-boarding-disembark-flow` |
| `content-residency-inventory` | deferred | `Assets/Game/Scripts/Editor/ContentResidencyInventoryGenerator.cs::ContentResidencyInventoryGenerator.GenerateAndWriteReports` | `content-residency-inventory` |
| `performance-budget-authority` | active | `Assets/Tests/Editor/PerformanceProductBudgetValidatorTests.cs::PerformanceProductBudgetValidatorTests.RunFocusedValidation` | `performance-budget-schema-ratchet` |
| `performance-editor-match-p95` | active | `Assets/Game/Scripts/Editor/MatchRuntimeShellSmokeValidation.cs::Game.Editor.MatchRuntimeShellSmokeValidation.RunPerformanceRegressionBaseline` | `editor-match-frame-p95`, `editor-match-thread-allocation` |
| `performance-match-gc-steady` | active | `Assets/Game/Scripts/Editor/MatchGcAllocationCallstackCapture.cs::Game.Editor.MatchGcAllocationCallstackCapture.RunSteadyState` | `match-global-gc-steady-state` |
| `release-android-development` | deferred | `Tools/CI/android_development_performance_gate.py::main validate` | `android-development-performance-contract` |
| `release-android-performance` | deferred | `Tools/CI/android_release_performance_gate.py::main validate` | `android-release-performance-contract` |
| `static-map-presentation-parity` | active | `Assets/Tests/Editor/StaticMapPresentationStructuralValidationTests.cs::StaticMapPresentationStructuralValidationTests` | `static-map-generated-structural-parity` |
| `visual-quality-matrix` | deferred | `Assets/Tests/Editor/MobileVisualQualityCaptureMatrixTests.cs::MobileVisualQualityCaptureMatrixTests.RunFocusedValidation` | `mobile-visual-quality-matrix` |

## Evidence Inputs

| ID | Requirement | Lane | Owner | Revision | Environment | Required fields | Path |
|---|---|---|---|---|---|---|---|
| `assembly-dependency-report` | required | active | `architecture-assembly-report` | exact-commit | exact-environment | `summary` | `Design/AgentReports/2026-07-10_aph-700_first_party_assembly_dependencies.json` |
| `audio-match-capture` | advisory | deferred | `audio-memory-capture` | exact-commit | not-required | `snapshots` | `Design/AgentReports/aph-401_audio-memory-playback-match.json` |
| `audio-menu-capture` | advisory | deferred | `audio-memory-capture` | exact-commit | not-required | `snapshots` | `Design/AgentReports/aph-401_audio-memory-playback-menu.json` |
| `build-android-aab` | advisory | deferred | `android-build-report` | exact-commit | not-required | `artifactBytes`, `status` | `Design/AgentReports/architecture_performance_android_aab_build_report.json` |
| `build-android-apk` | advisory | deferred | `android-build-report` | exact-commit | not-required | `artifactBytes`, `status` | `Design/AgentReports/architecture_performance_android_apk_build_report.json` |
| `content-residency-inventory` | advisory | deferred | `content-residency-inventory` | exact-commit | not-required | `status`, `summary` | `Design/AgentReports/architecture_performance_content_residency_baseline.json` |
| `editor-match-performance` | required | active | `performance-editor-match-p95` | exact-commit | exact-environment | `editorP95FrameBudgetMs`, `editorP95FrameBudgetPassed`, `frameCount`, `p95FrameMs` | `Design/AgentReports/performance_regression_match_baseline.json` |

## Enforcement

- Every responsibility has exactly one canonical validator owner.
- Every evidence path has exactly one owner row.
- Required evidence fails closed when missing, malformed, stale, unknown, dirty, environment-mismatched, or commit-mismatched.
- Advisory and release-deferred evidence remains visible but does not block the Core Architecture Lane.
- `--check` exits nonzero while the dashboard gate is rejected.
