"""Capture complete S005 native-source bytes and require the queue manifest to cover them."""
import argparse
import hashlib
import json
from pathlib import Path
import struct

EXTENSIONS = {".cs", ".asmdef", ".asmref", ".rsp", ".json", ".asset", ".prefab", ".unity",
              ".shader", ".hlsl", ".cginc", ".compute", ".meta"}


HASH_BUFFER_BYTES = 65536


def _file_row(project, path):
    digest = hashlib.sha256()
    with path.open("rb", buffering=0) as stream:
        buffer = bytearray(HASH_BUFFER_BYTES)
        view = memoryview(buffer)
        while (count := stream.readinto(buffer)):
            digest.update(view[:count])
    return dict(path=path.relative_to(project).as_posix(), sha256=digest.hexdigest())


def _hash_paths(project, paths):
    """Sequential, bounded hashing; preserve every input row and its order."""
    return [_file_row(project, path) for path in paths]


def snapshot(project):
    paths = []
    for directory in ("Assets", "Packages", "ProjectSettings"):
        folder = project / directory
        if not folder.is_dir():
            raise ValueError("Required source tree absent: " + directory)
        # Keep predecessor Path.rglob/is_file/suffix semantics exactly. Hashing
        # cannot silently replace this scope with an extension/path cache.
        for path in folder.rglob("*"):
            if path.is_file() and path.suffix.lower() in EXTENSIONS:
                paths.append(path)
    return sorted(_hash_paths(project, paths), key=lambda row: row["path"])


def candidate_hash(manifest):
    """Match Windows native ordinal path order, then BinaryWriter's encoding."""
    result = hashlib.sha256()
    def native_ordinal(row):
        encoded = row["path"].replace("/", "\\").encode("utf-16-le")
        return struct.unpack("<" + "H" * (len(encoded)//2), encoded)
    for row in sorted(manifest, key=native_ordinal):
        encoded = row["path"].encode("utf-8")
        length = len(encoded)
        while length >= 128:
            result.update(bytes([(length & 127) | 128]))
            length >>= 7
        result.update(bytes([length]))
        result.update(encoded)
        result.update(struct.pack("<i", 32))
        result.update(bytes.fromhex(row["sha256"]))
    return result.hexdigest()


def build_snapshot(project):
    folder = project / "Library/ScriptAssemblies"
    files = _hash_paths(project, sorted(folder.glob("*.dll"), key=lambda path:path.as_posix()))
    if not files:
        raise ValueError("Compiled ScriptAssemblies absent; compile/load canary is required before trial.")
    digest = hashlib.sha256(json.dumps(files, sort_keys=True, separators=(",", ":")).encode()).hexdigest()
    return files, digest


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    identity_mode=parser.add_mutually_exclusive_group(required=True)
    identity_mode.add_argument("--job", type=Path)
    identity_mode.add_argument("--capture-only",action="store_true",help="Capture fresh bytes for packet construction; never marks native qualification")
    args = parser.parse_args()
    manifest = snapshot(args.project.resolve())
    builds, build_hash = build_snapshot(args.project.resolve())
    with args.output.open("x", encoding="utf-8") as output:
        json.dump(dict(source_manifest=manifest, candidate_sha256=candidate_hash(manifest),
                       build_manifest=builds, build_sha256=build_hash), output, indent=2)
        output.write("\n")
    if args.capture_only:
        print("[S005WindowsSourceSnapshot] result=CapturedUnqualified files="+str(len(manifest)))
        raise SystemExit(0)
    job = json.loads(args.job.read_text(encoding="utf-8-sig"))
    if sorted(job["source_manifest"], key=lambda row: row["path"]) != manifest:
        raise SystemExit("Complete source bytes differ from pinned job; preserve snapshot and repin.")
    if job["candidate_sha256"] != candidate_hash(manifest):
        raise SystemExit("Exact native binary-manifest candidate hash differs from pinned job.")
    if job["build_sha256"] != build_hash:
        raise SystemExit("Compiled ScriptAssemblies pin differs from pinned job; run compile/load canary.")
    print("[S005WindowsSourceSnapshot] result=Passed files=" + str(len(manifest)))
