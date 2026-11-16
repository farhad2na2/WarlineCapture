"""Extract the current original Skirmish surface-placement owner unchanged."""
import hashlib
import re
from pathlib import Path

SOURCE="Assets/Game/Scripts/Systems/BuildingPlacementAdapterCompositionSystemHelper.cs"
SIGNATURE="internal static bool IsSkirmishFootprintLevel(MapSurfaceComponent surface, RectInt footprint)"
NAMES=("SkirmishMaxFoundationHeightDelta","SkirmishMaxBuildingSlopeDegrees","SkirmishMaxFoundationElevation")

def extract(text):
    if text.count(SIGNATURE)!=1: raise ValueError("Exact original surface owner required")
    start=text.index(SIGNATURE);opening=text.index("{",start);depth=0;end=None
    for i in range(opening,len(text)):
        depth+=(text[i]=="{")-(text[i]=="}")
        if depth==0:end=i+1;break
    if end is None:raise ValueError("Unclosed original surface owner")
    constants=[]
    for name in NAMES:
        rows=re.findall(r"^\s*private const float "+re.escape(name)+r"\s*=\s*([0-9]+(?:\.[0-9]+)?f);\s*$",text,re.MULTILINE)
        if len(rows)!=1 or float(rows[0][:-1])<=0:raise ValueError("Exact positive original surface threshold required: "+name)
        constants.append("private const float "+name+" = "+rows[0]+";")
    return constants,text[start:end]

def generate(repo,output):
    repo=Path(repo);out=Path(output);out.mkdir(parents=True,exist_ok=True);source=repo/SOURCE;before=source.read_bytes();constants,body=extract(before.decode("utf-8"))
    generated="using Unity.Mathematics;using Game.Components;namespace Game.Runtime {internal static class SourceSkirmishSurfacePlacementOwner {\n"+"\n".join(constants)+"\n"+body+"\n}}\n"
    (out/"SourceSkirmishSurfacePlacementOwner.cs").write_text(generated)
    if source.read_bytes()!=before:raise ValueError("Original source changed while extracting")
    return {"source_inputs":{str(source):hashlib.sha256(before).hexdigest()},"scope":"Original footprint-level source body and thresholds only; no scene or readiness claim"}
