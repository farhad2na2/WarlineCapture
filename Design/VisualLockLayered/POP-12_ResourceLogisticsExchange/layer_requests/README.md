# POP-12 Resource Logistics Exchange Layer Requests

Status: V01 fulfilled by separate green-key one-go source sheets.

Layer generation should use the accepted target PNG under:

`../reference/POP-12_ResourceLogisticsExchange_NewMainMenuArtDirection_TargetLock_V01.png`

V01 generated source sheets:

- `../generated_one_go/source/POP-12_ResourceExchange_Panels_Green_v01.png`
- `../generated_one_go/source/POP-12_ResourceExchange_Icons_Green_v01.png`
- `../generated_one_go/source/POP-12_ResourceExchange_Content_Green_v01.png`

V01 output:

- `../layer_manifest.json`
- `../layers/`
- `../generated_one_go/layers_contact_sheet.png`
- `../validation/pop12_layer_validation.json`

Required layer groups:

- popup outer frame and inner panel frames
- header title strip and close button frame
- Export/Import tab frames, selected and default
- recipe card frames: default, selected, disabled, locked, warning
- card image wells and resource icon slots
- details panel frame, amount stepper frame, plus/minus icons
- confirm button frame and disabled confirm button frame
- queue panel frame, queue row frame, progress bar frame, progress fill
- Rush All and Clear Completed button frames and icons
- match resource icons: Materials, Oil, Fuel; optional Rush Ticket inventory icon. Credits and Command do not appear in the in-match Resource Exchange.
- warning, lock, information, transport plane, truck, timer, cancel, and completion icons

All icons, progress bars, locks, warnings, and text must remain separate from background frames.

Regeneration rule:

- Request new green-key sheets for missing assets.
- Do not crop the target reference into production sprites.
- Keep content thumbnails separate from card frames and runtime labels.
