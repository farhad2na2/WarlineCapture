# Campaign chapter tab repair

- Fixed the artwork clip extending over the right frame: four-pixel inset on every edge, with stretched shade inside the clip. Saved via live Unity PrefabUtility; no YAML editing.
- Selected tabs use warm amber shading at 82% opacity; available tabs use blue-green shading at 86%; locked tabs use 91% shading. Existing text colors and progress markers remain.
- Updated runtime appearance and both existing builders so regeneration preserves the repair.
- Native Game view inspected at 1920x1080 in Persian and English. Complete selected chapter frame and all five stronger tab backgrounds are visible. Returned the unsaved locale to Persian.
- Live inspection passed all five chapter insets (4,4)/(-4,-4) and shade opacity >= 0.8. Runtime compilation passed without compiler errors.
- This is a focused UI repair. Human visual acceptance and device checks remain pending; no new mission gameplay acceptance run was performed.
