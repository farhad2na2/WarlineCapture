"""Resolve an archived evidence hash without treating the working tree as that capture."""
from functools import lru_cache
import hashlib
from pathlib import Path
import subprocess


@lru_cache(maxsize=None)
def _ancestor_hashes(root: str, path: str) -> frozenset[str]:
    commits = subprocess.run(["git", "rev-list", "HEAD", "--", path], cwd=root,
        check=True, capture_output=True, text=True).stdout.splitlines()
    hashes = set()
    for commit in commits:
        result = subprocess.run(["git", "show", f"{commit}:{path}"], cwd=root, capture_output=True)
        if result.returncode == 0:
            hashes.add(hashlib.sha256(result.stdout).hexdigest())
            # Windows captures hash checked-out CRLF bytes; Git stores the same text
            # as LF. Preserve the recorded digest and resolve either checkout form.
            if b"\x00" not in result.stdout:
                lf = result.stdout.replace(b"\r\n", b"\n")
                hashes.add(hashlib.sha256(lf).hexdigest())
                hashes.add(hashlib.sha256(lf.replace(b"\n", b"\r\n")).hexdigest())
    return frozenset(hashes)


def historical_hash_resolves(root: Path, path: str, expected: str) -> bool:
    # Captured artifacts may have been superseded, but their exact bytes must
    # remain reachable in this branch's history. This grants no current QA credit.
    return expected in _ancestor_hashes(str(root), path)
