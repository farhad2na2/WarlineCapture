# POP-12 Resource Logistics Exchange Target Lock Prompt V01

Use case: ui-mockup

Asset type: full target-lock reference mockup for Unity Canvas match popup, landscape mobile RTS.

Primary request: Generate a polished WarlineCapture RESOURCE EXCHANGE popup target mockup aligned with the approved bright premium military command art direction. It must look like a sibling of the current Build Popup/Build Drawer: dark charcoal brushed-metal panels, thin gold beveled borders, olive/gold accents, clear Oxanium-like typography, dense but readable tactical layout, and separated UI parts that can be rebuilt in Unity.

Scene/backdrop: dimmed 3D RTS match HUD and operation-map background. The background should suggest a Middle East operation map with base logistics, trucks, and a runway/transport plane presence, but it must be subdued behind the modal. Do not make the background a standalone hero scene.

Layout: 16:9 landscape. Centered modal popup occupying most of the screen but with safe margins around it. Same overall proportions and visual language as the Build Popup target: left catalog grid, right detail and queue column, top header, bottom instruction rail.

Header: left title icon suggests logistics exchange, title text reads RESOURCE EXCHANGE, top-right close button with X.

Tabs: two large tabs under header: EXPORT selected in gold, IMPORT default dark. Tabs must be clean, aligned, and not touching.

Left recipe grid: two rows of large recipe cards. Show at least five route cards:
- EXPORT OIL
- EXPORT MATERIALS
- EXPORT FUEL
- IMPORT MATERIALS
- IMPORT FUEL

Cards: each card has a separate image well, title, input resource amount, output preview amount, duration, and availability/status row. One card should be selected with a gold border and a small separate checkmark badge. One card may be disabled/locked with a separate lock badge and clear disabled reason, but the lock must not be baked into the background frame.

Right details panel: selected card is EXPORT OIL. Show title EXPORT OIL, role LOGISTICS ROUTE, rate line "100 OIL -> 46 CREDITS", amount stepper with minus and plus buttons around "100", input cost row, output preview row, duration row, requirements row, and a large gold CONFIRM EXCHANGE button. Details panel should feel equivalent to the Build Popup selected item panel, not a store checkout.

Queue panel: directly below details on the right. Header reads EXCHANGE QUEUE with capacity "2/3" and a separate information icon. Include three rows:
- EXPORT OIL with progress bar around 65%, ETA 00:11, rush icon/button, cancel X
- IMPORT FUEL queued, ETA 00:30, drag/reorder handle or order number
- EXPORT MATERIALS complete or ready-to-clear state with completion/check badge
Include bottom buttons RUSH ALL and CLEAR COMPLETED. Rush All should use Rush Ticket visual language, not Credits.

Bottom instruction rail: "SELECT ROUTE, SET AMOUNT, THEN CONFIRM EXCHANGE." Include a small separate info icon at the start.

Visual language: premium stylized military RTS UI, dark brushed metal, warm gold highlights, muted olive secondary accents, subtle blue information accents only for data/progress, crisp beveled lines, large readable text, consistent spacing, no one-note blue/purple palette, no oversized decorative cards, no marketing hero layout.

Text to render clearly: RESOURCE EXCHANGE, EXPORT, IMPORT, EXPORT OIL, EXPORT MATERIALS, EXPORT FUEL, IMPORT MATERIALS, IMPORT FUEL, LOGISTICS ROUTE, RATE, AMOUNT, INPUT COST, OUTPUT, DURATION, REQUIREMENTS, CONFIRM EXCHANGE, EXCHANGE QUEUE, RUSH ALL, CLEAR COMPLETED, SELECT ROUTE, SET AMOUNT, THEN CONFIRM EXCHANGE.

Layering constraints for future extraction: no text baked into reusable frames; no icons baked into panel/card backgrounds; no progress fills baked into queue row backgrounds; no lock/check/warning badges baked into card frames; button icons, resource icons, progress bars, and badges should be visually separable.

Composition/framing: all popup corners fully visible, no clipped cards, no stretched borders, no touching card frames, no tiny unreadable text, no random placeholder labels, no extra currencies beyond Credits, Materials, Oil, Fuel, and Rush Tickets.

Avoid: old green/blue UI direction, flat web dashboard styling, store/shop language, fantasy coins for every resource, transparent background, green-screen background, layer sheet, watermark, screenshot crop, illegible pseudo text, duplicate close buttons, baked progress or icons in background art.
