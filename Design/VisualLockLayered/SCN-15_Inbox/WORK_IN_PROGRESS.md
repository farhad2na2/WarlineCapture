# SCN-15 Command Inbox V3 Work In Progress

Status: canonical target audited and the missing Inbox route implementation is
staged. Unity prefab generation, focused validation, and exact-size Play Mode
comparisons are pending because the macOS login session is locked and no Unity
Pipeline Editor is connected.

## Canonical Target Lock

`reference/SCN-15_InboxV3_Final_Target.png`

No current runtime capture is presented as an iteration. Before this pass the
project had an `Inbox` route enum value but no Inbox prefab, view, route mount,
or valid live capture.

## Staged V3 Source

- `Assets/Game/Scripts/Editor/InboxV3PrefabBuilder.cs`
- `Assets/Game/Scripts/UI/Screens/InboxV3View.cs`
- Inbox content binding in `Assets/Game/Scripts/UI/Shell/UIShellContentView.cs`
- Inbox mounting in
  `Assets/Game/Scripts/UI/Shell/MenuOverlayRoutePresentation.cs`
- `Assets/Tests/Editor/InboxV3PrefabTests.cs`
- exact 1920x1080 and 4800x2160 capture entry points in
  `Assets/Game/Scripts/Editor/CanvasMenuFallbackValidation.cs`

The staged 1672x941 composition implements the target header, five-category
rail, search, newest/oldest sorting, five-message list, selected-message detail,
favorite state, two attachments, Mark Read, Mark All Read, unread-only filter,
and View Intel navigation. Categories, search, selection, read state, badges,
attachment feedback, and detail content are interactive rather than painted
labels.

All visible chrome uses directional procedural gradients and the same 3 px
border width. The detail image uses a masked
`AspectRatioFitter.EnvelopeParent` crop. The message column, search field, and
header title expand at 20:9; right-side resources, Sort, and the detail panel
remain pinned to their edges.

The screen reuses the shared V3 North Bridge/Forward Post, district map, Ranger,
ARIA, operation, resource, Settings, and menu icon art. Envelope/brand sources
are referenced from their existing project locations; no Inbox-local raster
copies were created. Gift, bridge, report, globe, filter, search, download,
favorite, back, and chevron symbols are procedural.

Offline Roslyn audits pass for the full updated UI runtime, isolated Inbox
builder, and focused Inbox tests. This is compile evidence, not a substitute for
Unity generation or visual comparison.

## Pending Commands

Run only through `Tools/CI/invoke_unity_macos.sh` after the login session is
unlocked:

```text
-quit -executeMethod Game.Editor.InboxV3PrefabBuilder.Build
-quit -executeMethod InboxV3PrefabTests.RunFocusedValidation
-executeMethod Game.Editor.CanvasMenuFallbackValidation.CaptureInbox1920x1080
-executeMethod Game.Editor.CanvasMenuFallbackValidation.CaptureInbox4800x2160
```

Do not create an immutable iteration until both exact live captures are posted,
compared against the target, and corrected for every visible mismatch.
