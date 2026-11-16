"""Generate an isolated, opt-in sparse acquisition source host from exact current sources."""
from __future__ import annotations

import hashlib
import json
from pathlib import Path

KERNEL = "Assets/Game/Scripts/Systems/TargetAcquisitionKernel.cs"
SYSTEM = "Assets/Game/Scripts/Systems/UnitEngagementSystem.cs"
BRIDGE = "Tools/CI/SkirmishSourceSimulation/Adapters/542_UnitEngagementSystem.TypedHostBridge.cs"
SPATIAL_MAP = "Tools/CI/SkirmishSourceSimulation/Adapters/122_OwnedSpatialMultiMap.cs"
WORKLOAD = "Tools/CI/SkirmishSourceSimulation/Adapters/312_PaidSourceOwnerStep.retry.cs"


def _once(text: str, old: str, new: str, label: str) -> str:
    if text.count(old) != 1:
        raise ValueError(f"Exact sparse acquisition source anchor required: {label}")
    return text.replace(old, new, 1)


def _read(repo: Path, relative: str) -> tuple[str, str]:
    path = repo / relative
    raw = path.read_bytes()
    if not raw or len(raw) > 4 * 1024 * 1024:
        raise ValueError(f"Bounded nonempty current source required: {relative}")
    return raw.decode("utf-8").replace("\r\n", "\n"), hashlib.sha256(raw).hexdigest()


def _transform_kernel(text: str) -> str:
    text = _once(
        text,
        "    public interface IReadOnlyTargetAcquisitionContext : ITargetAcquisitionContext { }",
        "    public interface IReadOnlyTargetAcquisitionContext : ITargetAcquisitionContext { }\n"
        "    // Source-only experimental seam. False means the kernel must retain the original rectangle sweep.\n"
        "    public interface ISparseTargetAcquisitionContext : IReadOnlyTargetAcquisitionContext\n"
        "    { bool TryBeginSparseCells(int centerX,int centerZ,int searchRange,int width,int height,out int key); bool TryNextSparseCell(out int key); }",
        "readonly-context-interface",
    )
    text = _once(
        text,
        "where T:struct,IReadOnlyTargetAcquisitionContext => AcquireCore(ref context,input,true);",
        "where T:struct,ISparseTargetAcquisitionContext => AcquireCore(ref context,input,true);",
        "readonly-entry-constraint",
    )
    text = _once(text, "where T:struct,ITargetAcquisitionContext => AcquireCore(ref context,input,false);",
                 "where T:struct,ISparseTargetAcquisitionContext => AcquireCore(ref context,input,false);", "acquire-entry-constraint")
    text = text.replace("where T:struct,ITargetAcquisitionContext", "where T:struct,ISparseTargetAcquisitionContext")
    start = "            for(int dz=-searchRange;dz<=searchRange;dz++)\n"
    end = "            if(best.IsNull&&context.SourceHasAttackMove())\n"
    if text.count(start) != 1 or text.count(end) != 1:
        raise ValueError("Exact original acquisition cell sweep required")
    a = text.index(start)
    b = text.index(end, a)
    sweep = text[a:b]
    body_start = "                    int key=unchecked(x+z*input.GridWidth);\n"
    body_end = "                    }while(context.NextCell(out candidate));\n"
    if sweep.count(body_start) != 1 or sweep.count(body_end) != 1:
        raise ValueError("Exact original candidate bucket body required")
    cell_body = sweep[sweep.index(body_start) + len(body_start):]
    close = cell_body.index(body_end) + len(body_end)
    candidate_body = cell_body[:close]
    candidate_body = _once(
        candidate_body,
        "                    if(!context.BeginCell(key,out TargetIdentity candidate))continue;\n",
        "                    if(!context.BeginCell(key,out TargetIdentity candidate)){if(!context.TryNextSparseCell(out sparseKey))break;continue;}\n",
        "sparse-empty-key-advance",
    )
    sparse = (
        "            bool sparse=context.TryBeginSparseCells(centerX,centerZ,searchRange,input.GridWidth,input.GridHeight,out int sparseKey);\n"
        "            if(sparse)\n            {\n"
        "                while(sparseKey>=0)\n                {\n"
        "                    int key=sparseKey;\n"
        + candidate_body
        + "                    if(!context.TryNextSparseCell(out sparseKey))break;\n"
        "                }\n"
        "            }\n            else\n            {\n"
        + sweep
        + "            }\n"
    )
    transformed = text[:a] + sparse + text[b:]
    helper = r'''
// Generated only in the disposable source-host experiment. Keys are strictly ascending
// flat map keys, so enumeration matches the original z-major then x-major cell sweep.
namespace Game.Runtime.Targeting
{
    public struct SourceSparseCellIterator
    {
        private int[] keys; private int count,index,width,minX,maxX,minZ,maxZ;
        public static bool IsValid(int[] values,int length,int gridWidth,int gridHeight)
        {
            if(values==null||length<0||length>values.Length||gridWidth<=0||gridHeight<=0||(long)gridWidth*gridHeight>int.MaxValue)return false;
            int previous=-1;for(int i=0;i<length;i++){int key=values[i];if(key<=previous||key<0||key>=(long)gridWidth*gridHeight)return false;previous=key;}return true;
        }
        public bool Begin(int[] values,int length,int centerX,int centerZ,int range,int gridWidth,int gridHeight,out int key)
        {
            key=0;if(!IsValid(values,length,gridWidth,gridHeight)||range<0)return false;
            long left=(long)centerX-range,right=(long)centerX+range,top=(long)centerZ-range,bottom=(long)centerZ+range;
            if(left<int.MinValue||right>int.MaxValue||top<int.MinValue||bottom>int.MaxValue)return false;
            keys=values;count=length;index=0;width=gridWidth;minX=(int)Math.Max(0L,left);maxX=(int)Math.Min((long)gridWidth-1,right);minZ=(int)Math.Max(0L,top);maxZ=(int)Math.Min((long)gridHeight-1,bottom);
            if(FindNext(out key))return true;key=-1;return true;
        }
        public bool Next(out int key)=>FindNext(out key);
        private bool FindNext(out int key)
        {
            key=0;if(keys==null)return false;
            while(index<count){int value=keys[index++],z=value/width,x=value-z*width;if(x>=minX&&x<=maxX&&z>=minZ&&z<=maxZ){key=value;return true;}}return false;
        }
    }
}
'''
    return transformed + "\n" + helper


def _transform_system(text: str) -> str:
    text = _once(
        text,
        "            [ReadOnly] public NativeParallelMultiHashMap<int, Entity> SpatialMap;\n",
        "            [ReadOnly] public NativeParallelMultiHashMap<int, Entity> SpatialMap;\n"
        "            // Only populated by the disposable source-host experiment.\n"
        "            public int[] SourceSparseKeys; public int SourceSparseKeyCount; public bool SourceSparseKeysEnabled;\n",
        "job-sparse-snapshot-fields",
    )
    text = _once(
        text,
        "            private struct NativeContext:IReadOnlyTargetAcquisitionContext\n",
        "            private struct NativeContext:ISparseTargetAcquisitionContext\n",
        "native-context-interface",
    )
    anchor = "                public bool NextCell(out TargetIdentity candidate)\n"
    if text.count(anchor) != 1:
        raise ValueError("Exact source native context cell iterator required")
    methods = (
        "                private SourceSparseCellIterator sparseCells;\n"
        "                public bool TryBeginSparseCells(int centerX,int centerZ,int searchRange,int width,int height,out int key)\n"
        "                {key=0;if(!Job.SourceSparseKeysEnabled){UnitEngagementSystem.RecordSparseFallbackRow();return false;}if(!sparseCells.Begin(Job.SourceSparseKeys,Job.SourceSparseKeyCount,centerX,centerZ,searchRange,width,height,out key)){UnitEngagementSystem.RecordSparseFallbackRow();return false;}UnitEngagementSystem.RecordSparseUsedRow();if(key>=0)UnitEngagementSystem.RecordSparseCellKey();return true;}\n"
        "                public bool TryNextSparseCell(out int key){bool next=sparseCells.Next(out key);if(next&&key>=0)UnitEngagementSystem.RecordSparseCellKey();return next;}\n"
    )
    return _once(text, anchor, methods + anchor, "native-context-sparse-iterator")


def _transform_bridge(text: str) -> str:
    text = _once(text, "using Game.Components;", "using Game.Components;using Game.Runtime.Targeting;using Newtonsoft.Json.Linq;", "bridge-kernel-namespace")
    text = _once(
        text,
        "  internal static int SourceHostAcquisitionSampleId;\n",
        "  internal static int SourceHostAcquisitionSampleId;\n"
        "  internal static readonly bool SourceSparseAcquisitionOptIn=Environment.GetEnvironmentVariable(\"WARLINE_SOURCE_SPARSE_ACQUISITION\")==\"1\";\n"
        "  internal static long SourceSparseSnapshotBuilds,SourceSparseSnapshotValid,SourceSparseSnapshotInvalid,SourceSparseSnapshotKeys,SourceSparseUsedRows,SourceSparseFallbackRows,SourceSparseCellKeysVisited;\n"
        "  static void SourceSparseIncrement(ref long value){if(value<long.MaxValue)value++;}\n"
        "  static void SourceSparseAdd(ref long value,long amount){if(amount<=0)return;value=value>long.MaxValue-amount?long.MaxValue:value+amount;}\n"
        "  internal static void RecordSparseUsedRow()=>SourceSparseIncrement(ref SourceSparseUsedRows);internal static void RecordSparseFallbackRow()=>SourceSparseIncrement(ref SourceSparseFallbackRows);internal static void RecordSparseCellKey()=>SourceSparseIncrement(ref SourceSparseCellKeysVisited);\n"
        "  internal static void RecordSparseSnapshot(int unique,bool valid){SourceSparseIncrement(ref SourceSparseSnapshotBuilds);if(valid){SourceSparseIncrement(ref SourceSparseSnapshotValid);SourceSparseAdd(ref SourceSparseSnapshotKeys,unique);}else SourceSparseIncrement(ref SourceSparseSnapshotInvalid);}\n"
        "  internal static JObject SourceSparseTelemetry=>new JObject{[\"enabled\"]=SourceSparseAcquisitionOptIn,[\"snapshot_builds\"]=SourceSparseSnapshotBuilds,[\"snapshot_valid\"]=SourceSparseSnapshotValid,[\"snapshot_invalid\"]=SourceSparseSnapshotInvalid,[\"snapshot_keys\"]=SourceSparseSnapshotKeys,[\"sparse_acquisition_rows\"]=SourceSparseUsedRows,[\"fallback_acquisition_rows\"]=SourceSparseFallbackRows,[\"sparse_cell_keys_visited\"]=SourceSparseCellKeysVisited};\n",
        "bridge-sparse-telemetry-fields",
    )
    text = _once(
        text,
        "   if(SourceHostWorkloadCountersEnabled){SourceHostAcquireQueryRows=rows.Length;SourceHostSpatialMapEntries=job.SpatialMap.Count();SourceHostSpatialMapOccupiedCells=job.SpatialMap.UniqueKeyCount();SourceHostGridWidth=job.Grid.Width;SourceHostGridHeight=job.Grid.Height;SourceHostBuildingTargetCount=job.BuildingTargets.Length;}\n",
        "   if(SourceHostWorkloadCountersEnabled){SourceHostAcquireQueryRows=rows.Length;SourceHostSpatialMapEntries=job.SpatialMap.Count();SourceHostSpatialMapOccupiedCells=job.SpatialMap.UniqueKeyCount();SourceHostGridWidth=job.Grid.Width;SourceHostGridHeight=job.Grid.Height;SourceHostBuildingTargetCount=job.BuildingTargets.Length;}\n"
        "   if(SourceSparseAcquisitionOptIn){int unique;long buildTicks;job.SourceSparseKeys=job.SpatialMap.BuildSortedUniqueIntKeyIndexForSourceCandidate(out unique,out buildTicks);job.SourceSparseKeyCount=unique;job.SourceSparseKeysEnabled=ValidateSparseSnapshot(job.SourceSparseKeys,unique,job.Grid.Width,job.Grid.Height);RecordSparseSnapshot(unique,job.SourceSparseKeysEnabled);}\n",
        "bridge-opt-in-snapshot",
    )
    anchor = "  // Reflection is used once per exact admitted job header, never per entity/body invocation.\n"
    validator = (
        "  static bool ValidateSparseSnapshot(int[] keys,int count,int width,int height)=>SourceSparseCellIterator.IsValid(keys,count,width,height);\n"
    )
    return _once(text, anchor, validator + anchor, "bridge-snapshot-validation")


def _transform_workload(text: str) -> str:
    return _once(
        text,
        '["path_grid_cells_per_invocation_sum"]=ground.SourcePathGridCellsSum}:new JObject{["enabled"]=false,["path_schedule_trace"]=ground.SourcePathScheduleTrace};',
        '["path_grid_cells_per_invocation_sum"]=ground.SourcePathGridCellsSum,["sparse_acquisition"]=UnitEngagementSystem.SourceSparseTelemetry}:new JObject{["enabled"]=false,["path_schedule_trace"]=ground.SourcePathScheduleTrace,["sparse_acquisition"]=UnitEngagementSystem.SourceSparseTelemetry};',
        "worker-result-sparse-telemetry",
    )


def generate(repo: Path, output: Path) -> dict:
    repo = Path(repo).resolve(strict=True)
    output = Path(output)
    sources = {}
    pins = {}
    for name in (KERNEL, SYSTEM, BRIDGE, SPATIAL_MAP, WORKLOAD):
        sources[name], pins[name] = _read(repo, name)
    if sources[SPATIAL_MAP].count("internal int[] BuildSortedUniqueIntKeyIndexForSourceCandidate(out int uniqueCount,out long elapsedTicks)") != 1:
        raise ValueError("Exact source-only sorted spatial-map snapshot API required")
    generated = {
        "SparseTargetAcquisitionKernel.cs": _transform_kernel(sources[KERNEL]),
        "SparseUnitEngagementSystem.cs": _transform_system(sources[SYSTEM]),
        "SparseUnitEngagementHostBridge.cs": _transform_bridge(sources[BRIDGE]),
        "SparsePaidSourceOwnerStep.cs": _transform_workload(sources[WORKLOAD]),
    }
    output.mkdir(parents=True, exist_ok=False)
    for name, text in generated.items():
        with (output / name).open("w", encoding="utf-8", newline="\n") as stream:
            stream.write(text)
    return {"source_pins": {str(repo / k): v for k, v in pins.items()}, "experimental_only": True,
            "scope": "Sorted occupied-cell snapshot once per source acquisition; default game source remains unchanged",
            "outputs": {k: hashlib.sha256(v.encode()).hexdigest() for k, v in generated.items()}}

