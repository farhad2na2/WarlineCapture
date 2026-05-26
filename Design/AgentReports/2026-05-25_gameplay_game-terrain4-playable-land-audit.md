# Game_Terrain4 Playable Land Footprint Audit

Date: 2026-05-25

Step: 1/2/3 - audit current island footprint, verify the larger-island foundation rebuild, and confirm remapped dressing bounds.

Conclusion: the rebuilt island foundation covers the 2024x2024 gameplay image with green/dirt playable terrain, and generated dressing has been remapped to that playable-land contract.

Target map footprint:
- `MapTarget2024`: count 0, width 2023, depth 2023, x [-1011.5, 1011.5], z [-1011.5, 1011.5]

Current foundation footprint:
- `FoundationOverall`: count 20440, width 3057.73, depth 2970.207, x [-1519.661, 1538.069], z [-1545.781, 1424.427]
- Shortfall versus target: west 0, east 0, south 0, north 0

Current foundation categories:
- `GreenPlayableGround`: count 16660, width 2694.917, depth 2614.862, x [-1343.393, 1351.523], z [-1326.339, 1288.523]
- `InnerBeachBlend`: count 2520, width 2958.253, depth 2906.406, x [-1472.41, 1485.843], z [-1514.832, 1391.573]
- `OuterBeachCoast`: count 1260, width 3057.73, depth 2970.208, x [-1519.661, 1538.069], z [-1545.781, 1424.428]
- Beach prefab centers inside 2024 gameplay map: 0 / 3780

Generated mask/dressing footprint:
- `GeneratedDressingOverall`: count 3382, width 2053.926, depth 2078.858, x [-1017.907, 1036.019], z [-1069.185, 1009.673]
- `Generated_Bushes`: count 1473, width 1961.523, depth 1975.426, x [-983.338, 978.186], z [-1007.205, 968.221]
- `Generated_Mountains`: count 238, width 2053.926, depth 2078.858, x [-1017.907, 1036.019], z [-1069.185, 1009.673]
- `Generated_Rocks`: count 695, width 1974.481, depth 1990.308, x [-990.807, 983.674], z [-994.816, 995.492]
- `Generated_Trees`: count 976, width 1970.486, depth 1973.777, x [-994.706, 975.78], z [-1003.774, 970.003]

Design implication for step 4:
- Regenerate `Game_Terrain4` from the larger island foundation plus the remapped mask dressing contract whenever the map art changes.
- Keep beach/coast prefabs as visual border content outside the gameplay contract.
- Keep generated dressing/reserve checks tied to the 2024 playable map footprint, not the full foundation/beach footprint.

Data: `Design/AgentReports/Data/GeneratedScenes/GameTerrain4_PlayableLandAudit/game_terrain4_playable_land_audit.json`
