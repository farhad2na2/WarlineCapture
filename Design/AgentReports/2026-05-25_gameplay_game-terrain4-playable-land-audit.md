# Game_Terrain4 Playable Land Footprint Audit

Date: 2026-05-25

Step: 1/2/3 - audit current island footprint, verify the larger-island foundation rebuild, and confirm remapped dressing bounds.

Conclusion: the rebuilt island foundation covers the 2024x2024 gameplay image with green/dirt playable terrain, and generated dressing has been remapped to that playable-land contract.

Target map footprint:
- `MapTarget2024`: count 0, width 2023, depth 2023, x [-1011.5, 1011.5], z [-1011.5, 1011.5]

Current foundation footprint:
- `FoundationOverall`: count 22474, width 3082.198, depth 2975.169, x [-1533.103, 1549.095], z [-1549.377, 1425.792]
- Shortfall versus target: west 0, east 0, south 0, north 0

Current foundation categories:
- `GreenPlayableGround`: count 17953, width 2930.813, depth 2831.412, x [-1441.291, 1489.521], z [-1475.949, 1355.464]
- `InnerBeachBlend`: count 3021, width 2916.812, depth 2875.924, x [-1452.585, 1464.227], z [-1496.299, 1379.625]
- `OuterBeachCoast`: count 1500, width 3082.197, depth 2975.17, x [-1533.103, 1549.094], z [-1549.377, 1425.793]
- Beach prefab centers inside 2024 gameplay map: 0 / 4521

Generated mask/dressing footprint:
- `GeneratedDressingOverall`: count 862, width 1976.028, depth 2011.301, x [-984.918, 991.11], z [-1007.716, 1003.584]
- `Generated_Mountains`: count 55, width 1965.266, depth 2011.301, x [-974.156, 991.11], z [-1007.716, 1003.584]
- `Generated_Rocks`: count 807, width 1968.355, depth 1988.146, x [-984.918, 983.437], z [-993.5, 994.646]

Design implication for step 4:
- Regenerate `Game_Terrain4` from the larger island foundation plus the remapped mask dressing contract whenever the map art changes.
- Keep beach/coast prefabs as visual border content outside the gameplay contract.
- Keep generated dressing/reserve checks tied to the 2024 playable map footprint, not the full foundation/beach footprint.

Data: `Design/AgentReports/Data/GeneratedScenes/GameTerrain4_PlayableLandAudit/game_terrain4_playable_land_audit.json`
