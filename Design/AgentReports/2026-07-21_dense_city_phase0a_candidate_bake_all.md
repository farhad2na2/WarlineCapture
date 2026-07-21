# Phase 0A Candidate Bake All

Result: `CandidateBakeAllPassedPendingVisualAndRuntimeAcceptance`
Production cutover: `0`
Rollback applied: `0`

| Stage | Result | Milliseconds | Failure |
|---|---|---:|---|
| preflight-isolation | Passed | 4494 |  |
| candidate-population | Passed | 5079 |  |
| candidate-presentation-identities | Passed | 9100 |  |
| candidate-authoring-readiness | Passed | 1575 |  |
| candidate-entity-bake | Passed | 8063 |  |
| shared-art-budget | Passed | 1332 |  |
| candidate-binding-layout | Passed | 6776 |  |
| candidate-bake-budget | Passed | 4 |  |
| postflight-isolation | Passed | 5504 |  |

Visual parity, Editor lifecycle acceptance, Android acceptance, and production cutover remain separate gates.
