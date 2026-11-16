"""Timing experiment only: cannot detect drift between repeated group reads.

Never use this helper for admission or qualification; the original verifier
checks repeated pins and can detect mutations during a single audit.
"""
from pathlib import Path
import hashlib

def verify_groups(root, groups, read_bytes=None):
    root=Path(root).resolve(strict=True)
    read_bytes=read_bytes or (lambda p:p.read_bytes())
    cache={};result={}
    for group,pins in groups.items():
        normalized={}
        for name,expected in pins.items():
            path=Path(name);path=path if path.is_absolute() else root/path
            if path.is_symlink() or not path.is_file():raise ValueError("Source pin missing or changed: "+name)
            key=str(path.resolve(strict=True))
            if key in cache:
                if cache[key]!=expected:raise ValueError("Conflicting normalized source pins: "+name)
            else:
                if hashlib.sha256(read_bytes(path)).hexdigest()!=expected:raise ValueError("Source pin missing or changed: "+name)
                cache[key]=expected
            if key in normalized and normalized[key]!=expected:raise ValueError("Conflicting normalized source pins: "+name)
            normalized[key]=expected
        result[group]=normalized
    return result
