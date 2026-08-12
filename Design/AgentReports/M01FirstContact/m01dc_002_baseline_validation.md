# M01DC-002 Baseline Validation

Capture head: `70c46a8378cf68ec3d1fe177033a071f473e274f`  
Capture tree: `98543d3d1bfa688c11ef66e61fb11c31b830c7c8`  
Unity: `6000.5.2f1`

| Gate | Result | Required marker | Raw log SHA-256 |
|---|---|---|---|
| Production source growth | Passed `17 / 17`; zero `error CS*` rows | `[ProductionSourceGrowthArchitectureValidation] result=Passed tests=17` | `fecc75c110d6b9675d6cd86f4a694f104dea53bcd5176ebbf672bea99c1ab525` |
| Architecture closeout | Passed `23` suites; zero `error CS*` rows | `[ArchitectureHardeningCloseoutValidation] result=Passed suites=23` | `eb63c1c89e74611bf3740cc6f42d4dccd9746176b95752837d3a8ab245dfb710` |

Both gates ran through `Tools/CI/InvokeUnityExecuteMethodValidation.ps1` with explicit logs, pass markers, and timeouts. The repository-documented atomic Windows recovery stopped only the exact verified stale generic licensing client while no Editor owned the project, then immediately launched the checked GUI wrapper so the Editor-bundled version-matched licensing client acquired the pipe. Unity Hub remained open.

The 23-suite gate regenerated seven FirstLaunch/Addressables files as incidental validation output. Those exact files were restored to the capture head after the pass; they are not M01DC-002 outputs. Android was not required for this baseline slice.
