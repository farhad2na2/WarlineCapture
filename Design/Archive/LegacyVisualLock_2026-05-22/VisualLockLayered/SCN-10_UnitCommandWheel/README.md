# SCN-10 Unit Command / Command Wheel Layer Pack

Status: RouteOverlayImplemented. This pack supports the hidden `CommandWheelCanvas` overlay inside `Screen_MatchOverlay`. The layers are authored as separate frames, fills, icons, and content sprites so Unity can compose an interactive Canvas without baked text or icon artifacts.

Validation rules:
- Keep `CommandWheelCanvas` inactive by default.
- Open from the HUD `SpecialButton`; close from the scrim or close button.
- Use separate icon images for Stop, Move, Attack, Extract, Rope Drop, and Patrol.
- Use animated button transitions for all wheel command segments.
