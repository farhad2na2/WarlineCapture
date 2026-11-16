#!/usr/bin/env bash
set -euo pipefail
repo="$(cd "$(dirname "$0")/../../../../.." && pwd)"
recipe="$repo/Tools/CI/SkirmishSourceSimulation/Experiments/bloom_prefilter"
out="${1:-/Volumes/T7/WarlineBloomPrefilter-$(date +%Y%m%d-%H%M%S)}"
mkdir -p "$out"
python3 "$recipe/generate.py" --out "$out/generated"
python3 "$recipe/prepare_manifest.py" --manifest "$repo/Tools/CI/SkirmishSourceSimulation/source_manifest.plan.json" --output "$out/source_manifest.bloom-experimental.plan.json"
rtk proxy dotnet restore "$recipe/FocusedHarness.csproj" -p:NuGetAudit=false -p:RestoreIgnoreFailedSources=true -p:BaseIntermediateOutputPath="$out/obj/" >"$out/restore.log" 2>&1
for variant in BaselineMap BloomMap; do
  echo "=== $variant ==="
  CANDIDATE_DIR="$out/generated/$variant" rtk proxy dotnet run --no-restore --project "$recipe/FocusedHarness.csproj" --configuration Release \
    -p:CandidateDir="$out/generated/$variant" \
    -p:NuGetAudit=false -p:RestoreIgnoreFailedSources=true \
    -p:BaseIntermediateOutputPath="$out/obj/" \
    -p:OutputPath="$out/$variant/bin/" \
    2>&1 | tee "$out/$variant.log"
done
echo "OUTPUT_DIR=$out"
