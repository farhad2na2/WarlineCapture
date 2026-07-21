# Phase 0A Candidate Bake All

Result: `CandidateBakeAllPassedPendingVisualAndRuntimeAcceptance`
Production cutover: `0`
Rollback applied: `0`

| Stage | Result | Milliseconds | Failure |
|---|---|---:|---|
| preflight-isolation | Passed | 2703 |  |
| candidate-population | Passed | 3600 |  |
| candidate-presentation-identities | Passed | 4237 |  |
| candidate-authoring-readiness | Passed | 1046 |  |
| candidate-entity-bake | Passed | 202617 |  |
| shared-art-budget | Passed | 1124 |  |
| candidate-binding-layout | Passed | 5522 |  |
| candidate-bake-budget | Passed | 4 |  |
| postflight-isolation | Passed | 4494 |  |

Visual parity, Editor lifecycle acceptance, Android acceptance, and production cutover remain separate gates.
