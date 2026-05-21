#!/usr/bin/env python3
from pathlib import Path
import shutil
ROOT = Path(__file__).resolve().parents[3]
PACK = ROOT / 'Design' / 'VisualLockLayered' / 'SCN-07_LoadoutSquadPrep'
DEST = ROOT / 'Assets' / 'Game' / 'Art' / 'UI' / 'Generated' / 'Loadout' / 'LayeredOneGo'
DEST.mkdir(parents=True, exist_ok=True)
for src in (PACK / 'layers').glob('*.png'):
    shutil.copy2(src, DEST / src.name)
print(f'Copied SCN-07 layers to {DEST}')
