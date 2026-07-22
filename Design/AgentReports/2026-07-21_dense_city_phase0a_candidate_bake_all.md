# Phase 0A Candidate Bake All

Result: `CandidateBakeAllPassedPendingVisualAndRuntimeAcceptance`
Production cutover: `0`
Rollback applied: `0`

| Stage | Result | Milliseconds | Failure |
|---|---|---:|---|
| preflight-isolation | Passed | 2558 |  |
| candidate-population | Passed | 3672 |  |
| candidate-presentation-identities | Passed | 4007 |  |
| candidate-source-transform-parity | Passed | 208456 |  |
| candidate-authoring-readiness | Passed | 1511 |  |
| candidate-entity-bake | Passed | 210545 |  |
| shared-art-budget | Passed | 845 |  |
| candidate-binding-layout | Passed | 3260 |  |
| candidate-bake-budget | Passed | 2 |  |
| presentation-budget | Passed | 152 |  |
| postflight-isolation | Passed | 3200 |  |

Visual parity, Editor lifecycle acceptance, Android acceptance, and production cutover remain separate gates.
