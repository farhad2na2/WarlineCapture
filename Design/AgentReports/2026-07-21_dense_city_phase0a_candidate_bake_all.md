# Phase 0A Candidate Bake All

Result: `CandidateBakeAllPassedPendingVisualAndRuntimeAcceptance`
Production cutover: `0`
Rollback applied: `0`

| Stage | Result | Milliseconds | Failure |
|---|---|---:|---|
| preflight-isolation | Passed | 2707 |  |
| candidate-population | Passed | 3822 |  |
| candidate-presentation-identities | Passed | 4020 |  |
| candidate-source-transform-parity | Passed | 237505 |  |
| candidate-authoring-readiness | Passed | 1866 |  |
| candidate-entity-bake | Passed | 284402 |  |
| shared-art-budget | Passed | 1122 |  |
| candidate-binding-layout | Passed | 5209 |  |
| candidate-bake-budget | Passed | 2 |  |
| postflight-isolation | Passed | 4385 |  |

Visual parity, Editor lifecycle acceptance, Android acceptance, and production cutover remain separate gates.
