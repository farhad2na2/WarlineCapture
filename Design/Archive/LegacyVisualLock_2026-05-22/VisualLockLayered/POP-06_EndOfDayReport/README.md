# POP-06_EndOfDayReport Layer Pack

Status: `GeneratedForCanvasImplementation`

This pack provides separated, reusable PNG layers for the POP-06 End of Day Report popup. The reference target remains the canonical visual target; Unity implementation must compose the popup from these individual layers with TMP text, separate Image icons, and real Buttons.

Required gate files:

- `reference/POP-06_EndOfDayReport_Landscape_Target.png`
- `layers/` separated PNG layers
- `layer_manifest.json` with Unity destination mappings
- `generated_one_go/layers_contact_sheet.png`

Implementation rules:

- Modal, section, row, meter, and button frames are separate from TMP text and icons.
- Transparent-corner frames must be imported as sprites and used as sliced images where appropriate.
- Do not use target crops as interactive controls.
- Do not bake button text or meter labels into sprite layers.
- Preserve 16:9 and 20:9 readability in Unity captures before marking the popup complete.
