# Phase 0A Candidate Bake All

Result: `CandidateBakeAllPassedPendingVisualAndRuntimeAcceptance`
Production cutover: `0`
Rollback applied: `0`

| Stage | Result | Milliseconds | Failure |
|---|---|---:|---|
| preflight-isolation | Passed | 5578 |  |
| candidate-population | Passed | 6938 |  |
| candidate-authoring-readiness | Passed | 1414 |  |
| candidate-entity-bake | Passed | 8958 |  |
| shared-art-budget | Passed | 4839 |  |
| candidate-binding-layout | Passed | 24849 |  |
| candidate-bake-budget | Passed | 21 |  |
| postflight-isolation | Passed | 12503 |  |

Visual parity, Editor lifecycle acceptance, Android acceptance, and production cutover remain separate gates.
