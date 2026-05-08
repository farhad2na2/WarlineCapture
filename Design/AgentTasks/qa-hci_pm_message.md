# PM Message For QA/HCI

Date: 2026-05-08

The user rejected the selected-readability pass and called out that repeated feedback was missed. QA/HCI must treat this as a validation-process failure.

Do not pass another M01 selected-readability or Gate 4 review by checking only a proxy such as "no SpriteRenderer." The user-visible requirement is stronger: public M01 visible units/buildings must not be scene/runtime GameObject renderer wrappers. They must be ECS entity visuals.

Prepare and then run a rejection matrix covering:

- MeshRenderer/MeshFilter/SpriteRenderer wrapper rejection,
- huge green target marker,
- wrong/missing idle and run animation,
- crouched/sitting movement frames,
- foot/top artifact,
- scale/aspect and vertical squash,
- hard-to-select soldiers,
- yellow placeholder selected marker,
- unexplained red flashing enemy/object,
- M01 infantry-only scope.

Expected report:

`Design/AgentReports/2026-05-08_qa-hci_user-feedback-regression-gate.md`
