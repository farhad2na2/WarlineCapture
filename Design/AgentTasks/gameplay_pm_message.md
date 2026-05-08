# PM Message To Gameplay

Date: 2026-05-08
Priority: P0 temporary Gate 4 runtime rejection follow-up

The user rejected temporary Gate 4 art/runtime behavior. Read:

- `Design/AgentReports/2026-05-08_pm_temporary-art-rejected-ecs-scale-motion.md`
- `Design/AgentTasks/gameplay_current.md`

Your lane owns the runtime side:

- Remove or replace public `SpriteRenderer` unit presentation and SpriteRenderer-era naming. The user saw `M01RuntimeSpriteRenderers`; the public M01 unit presentation must read as ECS atlas entities, not SpriteRenderer proxies.
- Remove `MissionRuntimeSpriteRendererRuntime` as an accepted public M01 unit path. If a bridge object exists, it must be ECS-owned atlas-quad presentation and must not expose SpriteRenderer components/names for unit visuals.
- Replace tiny hand-tuned visual scale with scale-contract/metric-driven sizing. Consume Art/Atlas/Designer scale roles rather than hardcoded readability multipliers.
- Replace the huge green selected marker with small under-each-soldier selected treatment.
- Remove the unclear blue marker unless it has a defined readable purpose and is accepted by Art/Atlas/QA.
- Calibrate M01 rifle squad movement speed from config to realistic infantry run/walk. It must not look like teleporting.
- Prove run animation changes while moving, not just a static move pose.

Required proof:

- Public M01 route capture before approval request.
- Test/assertion that public M01 unit visuals have no active `SpriteRenderer` components and no `MissionRuntimeSpriteRendererRuntime` component.
- Test/assertion that selection marker stays bounded under soldiers.
- Test/assertion or captured evidence that movement duration/speed is realistic and run animation advances during movement.

Write:

`Design/AgentReports/2026-05-08_gameplay_m01-ecs-scale-selection-motion-fix.md`

Do not commit or push.
