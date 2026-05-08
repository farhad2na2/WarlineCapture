# POP-08 Intel Reveal Target And Layers Prompt

Use case: ui-mockup
Asset type: WarlineCapture landscape mobile game popup UI target plus separated Unity Canvas layer PNGs.
Primary request: Create a high-end POP-08 Intel Reveal popup in the accepted WarlineCapture premium military RTS HUD style, then export the exact reusable visual layers separately.

Scene/backdrop: Dimmed operation command desk and city map, blue scanning light, holographic intel archive panels, dark tactical atmosphere.

UI layout: Centered modal popup. Header text is rendered in Unity as `INTEL REVEALED` with a separate document/magnifier icon. Three reusable evidence cards display `Supply Ledger`, `Cargo Manifest`, and `Radio Intercept`; card frames, content thumbnails, confidence chips, and inspect buttons are separate layers. Bottom notice text is rendered in Unity as `New intel available in Intel Archive`. Bottom buttons are separate secondary and primary button backgrounds, with Unity TMP labels `CLOSE` and `VIEW INTEL`.

Layer export rules:

- Do not bake UI text into reusable frames, buttons, chips, or icons.
- Export transparent-corner frames suitable for Unity 9-slice import.
- Export content thumbnails separately from their frames.
- Export button backgrounds separately from button labels.
- Export icons as clean transparent PNGs.
- Keep the target and layer set aligned to a 1672 x 941 landscape reference.
