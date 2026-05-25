# Game_Terrain4 Playable Land Footprint Audit

Date: 2026-05-25

Step: 1/2/3 - audit current island footprint, verify the larger-island foundation rebuild, and confirm remapped dressing bounds.

Conclusion: the rebuilt island foundation covers the 2024x2024 gameplay image with green/dirt playable terrain, and generated dressing has been remapped to that playable-land contract.

Target map footprint:
- `MapTarget2024`: count 0, width 2023, depth 2023, x [-1011.5, 1011.5], z [-1011.5, 1011.5]

Current foundation footprint:
- `FoundationOverall`: count 17027, width 2358.803, depth 2386.351, x [-1167.993, 1190.81], z [-1196.339, 1190.012]
- Shortfall versus target: west 0, east 0, south 0, north 0

Current foundation categories:
- `GreenPlayableGround`: count 13247, width 2358.803, depth 2386.351, x [-1167.993, 1190.81], z [-1196.339, 1190.012]
- `InnerBeachBlend`: count 2520, width 2234.204, depth 2250.516, x [-1119.305, 1114.899], z [-1128.69, 1121.825]
- `OuterBeachCoast`: count 1260, width 2298.364, depth 2287.06, x [-1148.417, 1149.947], z [-1142.297, 1144.763]

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
