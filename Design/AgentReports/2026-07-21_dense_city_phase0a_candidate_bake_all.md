# Phase 0A Candidate Bake All

Result: `CandidateBakeAllPassedPendingVisualAndRuntimeAcceptance`
Production cutover: `0`
Rollback applied: `0`

| Stage | Result | Milliseconds | Failure |
|---|---|---:|---|
| preflight-isolation | Passed | 2995 |  |
| candidate-population | Passed | 4072 |  |
| candidate-presentation-identities | Passed | 4418 |  |
| candidate-authoring-readiness | Passed | 1174 |  |
| candidate-entity-bake | Passed | 5728 |  |
| shared-art-budget | Passed | 1362 |  |
| candidate-binding-layout | Passed | 4647 |  |
| candidate-bake-budget | Passed | 3 |  |
| postflight-isolation | Passed | 3971 |  |

Visual parity, Editor lifecycle acceptance, Android acceptance, and production cutover remain separate gates.
