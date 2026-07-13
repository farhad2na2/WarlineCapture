# SCN-06 Mission Briefing Command-Base Target Lock V01

## Status

Approved implementation target for the Canvas Mission Briefing screen.

## References

- Primary style continuity: the production SCN-05 Campaign Operations screen.
- Information architecture: the archived SCN-06 Mission Briefing target.
- Mission art: `scn05_blackout_relay_preview_v01.png` from SCN-05.

## Layout Contract

- Preserve the installed shared header; SCN-06 owns only the body overlay.
- Use the SCN-05 graphite, brass, amber, olive, and restrained cyan palette.
- Use Oxanium Bold for display hierarchy and Oxanium Medium for supporting copy.
- Keep a 7/5 overview-to-intel column split below the title row.
- Left: mission image, mission identity, situation briefing, and five-node chapter progress.
- Right: primary objectives, tactical conditions, and enemy intel.
- Bottom: three reward modules and a large Deploy Operation command.
- All text remains live TMP text. Art, icons, and nine-sliced chrome remain separate assets.

## Interaction Contract

- Back returns through route history to Campaign Operations.
- Campaign Mission 01 opens this screen.
- Deploy Operation remains disabled until a Campaign mission-launch contract exists.
- No UI-local mission session, progression, rewards, or launch payload may be invented.

## Responsive Contract

- Validate at 1920x1080 and 2400x1080.
- No text or icon may touch panel borders or overlap adjacent content.
- The shared header height, font family, button state language, image PPU, and nine-slice treatment must match SCN-05.
