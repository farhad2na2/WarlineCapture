# SCN-05 Saga Map High-End Layer Prompt

Use case: ui-mockup
Asset type: AAA mobile RTS Saga Campaign map target plus separated implementation layer atlas.

Primary request:
Create a high-end WarlineCapture Saga Campaign landscape UI target at 1672 x 941, matching the existing dark graphite military HUD chrome, cyan edge highlights, amber/gold selected states, and premium 2D isometric city-map art direction.

Reference:
Use `reference/SCN-05_SagaMap_Landscape_Target.png` for quality and layout only. Do not preserve Chapter 03, Shattered Harbor, 3-x nodes, or old star counts.

Canonical content:
- Header: SAGA CAMPAIGN
- Chapter dropdown: Chapter 01
- Chapter title dropdown: First Response
- Star counter: 0 / 15
- Difficulty: NORMAL
- Chapter rewards: 0 / 9
- Mission nodes:
  - 1-1 First Contact, selected/next
  - 1-2 Establish The Base, locked
  - 1-3 Radar Warning, locked
  - 1-4 Airlift, locked
  - 1-5 Breach Assault, locked

Layer atlas request:
Create a separate clean layer atlas on flat chroma key. The atlas must separate:
- full shell frame and fill
- map viewport content art without nodes or labels
- back button background and back chevron icon
- chapter dropdown frame, title dropdown frame, difficulty dropdown frame
- chapter rewards button frame
- star counter frame
- route line segment
- mission node selected frame
- mission node normal/locked frame
- mission node completed frame
- lock icon, star icon, node portrait marker icon

Layer rules:
- No node number, mission name, star count, lock text, chapter title, or reward count may be baked into reusable sprites.
- Map art must not contain route lines or mission node markers.
- Node frames must be 9-slice compatible.
