# Implementation status

Updated 2026-09-19. The guided campaign prototype is implemented and undergoing validation; full release certification remains open.

- A1: virtual touchscreen, exclusive contact ownership and cancellation; 14 isolated input cases plus cleanup assertions passed. Basic live M2 handback/restart passed; broader interruption coverage remains open.
- A2/A3: visible-guidance observations, ECS decision loop, confirmed Watch/Stop controls and cyan finger. Ten decision cases passed. Minimap permanently docks left per owner request.
- A4: M1–M5 completed through the shipping touch driver in both English and Farsi (10 baseline wins). Broader varied-start/recovery certification is still open.
- A5: Skirmish touch-only development preview implemented. Ten planner cases pass; first live pilot exposed selection/readiness defects, corrected in source but rerun blocked by Mac lock. Full match, tactical recovery and economy/construction coverage remain open.
- A6/A7: Operations discovery, cross-mode/device/performance and player acceptance remain open.

See [implementation evidence and remaining work](../../AgentReports/AriaWatchPlay/implementation-status.md).

This prototype follows existing visible campaign guidance. It does not yet establish independent tactical competence, tutorial-disabled support or readiness in all modes.

### 2026-09-19 continued live audit

Skirmish paid recruitment and squad selection are verified through the real touch path. A base-only assault lost and is not accepted. Added visible minimap-contact threat priority, tactical-map focus/close handling, shorter reinforcement retry, and non-preemption of an in-progress combat sequence. A live focus/close oscillation was corrected with persistent focus completion. Regression checks: 18 Skirmish + 10 campaign + 14 input = 42 passed. Latest live rerun stopped before Start because the Mac locked. S01 remains preview, not certified; no Skirmish victory is claimed. Next test is full normal-speed EN then FA with these fixes, followed by adverse starts, economy/construction and recovery cases.
