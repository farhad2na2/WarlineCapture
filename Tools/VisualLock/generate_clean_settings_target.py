#!/usr/bin/env python3
"""Document the SCN-04 Settings visual-lock target.

The canonical Settings target is an AI-generated landscape mockup saved at:
Design/VisualLock/SCN-04_SettingsAccessibility/SCN-04_SettingsAccessibility_Landscape_Target.png

This helper intentionally does not regenerate or overwrite that PNG. Earlier
versions of this script upscaled the portrait source mockup, which no longer
matches the accepted visual-lock workflow for Settings.
"""

from __future__ import annotations

from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
TARGET = ROOT / "Design" / "VisualLock" / "SCN-04_SettingsAccessibility" / "SCN-04_SettingsAccessibility_Landscape_Target.png"
NOTES = ROOT / "Design" / "VisualLock" / "SCN-04_SettingsAccessibility" / "SCN-04_SettingsAccessibility_CleanLandscape_Notes.md"


def main() -> None:
    print(f"Canonical target: {TARGET}")
    print(f"Generation notes: {NOTES}")
    print("This helper does not overwrite the target image.")


if __name__ == "__main__":
    main()
