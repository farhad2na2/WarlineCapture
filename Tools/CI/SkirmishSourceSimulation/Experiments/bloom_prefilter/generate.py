#!/usr/bin/env python3
"""Generate baseline and opt-in Bloom candidates from the checked-in shim."""
from pathlib import Path
import argparse
import hashlib

ROOT = Path(__file__).resolve().parents[5]
SOURCE = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/122_OwnedSpatialMultiMap.cs"

def generate(repo: Path, output: Path) -> dict:
    repo = Path(repo).resolve(strict=True)
    source = repo / "Tools/CI/SkirmishSourceSimulation/Adapters/122_OwnedSpatialMultiMap.cs"
    raw = source.read_bytes()
    src = raw.decode("utf-8").replace("\r\n", "\n")
    output = Path(output)
    output.mkdir(parents=True, exist_ok=False)
    (output / "BaselineMap").mkdir()
    (output / "BloomMap").mkdir()
    with (output / "BaselineMap" / "Map.cs").open("w", encoding="utf-8", newline="\n") as stream:
        stream.write(src)
    needle = "internal int Count,Capacity;"
    replacement = "internal int Count,Capacity; internal ulong[] Bloom; internal int BloomMask;"
    assert src.count(needle) == 1
    candidate = src.replace(needle, replacement)
    needle = "owner=new Owner{Capacity=cap};"
    replacement = "owner=new Owner{Capacity=cap,Bloom=new ulong[1024],BloomMask=65535};"
    assert candidate.count(needle) == 1
    candidate = candidate.replace(needle, replacement)
    needle = "public void Add(K key,V value){if(owner==null||owner.Count>=owner.Capacity)throw new InvalidOperationException(\"Spatial map capacity\");"
    replacement = "public void Add(K key,V value){if(owner==null||owner.Count>=owner.Capacity)throw new InvalidOperationException(\"Spatial map capacity\");if(typeof(K)==typeof(int))BloomAdd(owner,(int)(object)key);"
    assert candidate.count(needle) == 1
    candidate = candidate.replace(needle, replacement)
    needle = " public bool TryGetFirstValue(K key,out V value,out NativeParallelMultiHashMapIterator<K> iterator){iterator=new NativeParallelMultiHashMapIterator<K>{Key=key,Index=0};if(SourceProbeCountersEnabled)SourceProbeCalls++;if(owner!=null&&owner.Rows.TryGetValue(key,out var rows)&&rows.Count>0)"
    replacement = " internal static void BloomAdd(Owner o,int key){uint h=unchecked((uint)key);uint a=Mix(h),b=Mix(h^0x9e3779b9u);int x=(int)(a&65535),y=(int)(b&65535);o.Bloom[x>>6]|=1UL<<(x&63);o.Bloom[y>>6]|=1UL<<(y&63);}\n  static uint Mix(uint x){unchecked{x^=x>>16;x*=0x7feb352du;x^=x>>15;x*=0x846ca68bu;return x^(x>>16);}}\n  bool BloomMayContain(K key){if(owner==null||typeof(K)!=typeof(int)||owner.Bloom==null)return true;uint h=unchecked((uint)(int)(object)key);uint a=Mix(h),b=Mix(h^0x9e3779b9u);int x=(int)(a&owner.BloomMask),y=(int)(b&owner.BloomMask);return (owner.Bloom[x>>6]&(1UL<<(x&63)))!=0&&(owner.Bloom[y>>6]&(1UL<<(y&63)))!=0;}\n public bool TryGetFirstValue(K key,out V value,out NativeParallelMultiHashMapIterator<K> iterator){iterator=new NativeParallelMultiHashMapIterator<K>{Key=key,Index=0};if(SourceProbeCountersEnabled)SourceProbeCalls++;if(BloomMayContain(key)&&owner!=null&&owner.Rows.TryGetValue(key,out var rows)&&rows.Count>0)"
    assert candidate.count(needle) == 1
    candidate = candidate.replace(needle, replacement)
    with (output / "BloomMap" / "Map.cs").open("w", encoding="utf-8", newline="\n") as stream:
        stream.write(candidate)
    return {"source_pins": {str(source): hashlib.sha256(raw).hexdigest()}, "experimental_only": True,
            "scope": "Opt-in int-key Bloom prefilter on a generated NativeParallelMultiHashMap copy; no default/game source mutation",
            "outputs": {"BaselineMap/Map.cs": hashlib.sha256(src.encode()).hexdigest(),
                        "BloomMap/Map.cs": hashlib.sha256(candidate.encode()).hexdigest()}}

def main():
    p = argparse.ArgumentParser()
    p.add_argument("--repo", type=Path, default=ROOT)
    p.add_argument("--out", required=True, type=Path)
    args = p.parse_args()
    print(generate(args.repo, args.out))

if __name__ == "__main__": main()
