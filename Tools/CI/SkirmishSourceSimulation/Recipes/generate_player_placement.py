"""Verify adapted player construction owners against current original source."""
from pathlib import Path
import hashlib
FILES=("ResourceWrites.cs","Reservation.cs","PlacementTransaction.cs","PlacementBindings.cs","PlacementSession.cs")
def body(text,signature,occurrence=0):
 start=-1
 for _ in range(occurrence+1): start=text.index(signature,start+1)
 begin=text.index("{",start);depth=0
 for index in range(begin,len(text)):
  depth+=(text[index]=="{")-(text[index]=="}")
  if depth==0:return "".join(text[begin:index+1].split())
 raise ValueError("Unclosed source owner method")
def generate(repo,output):
 repo=Path(repo);out=Path(output);out.mkdir(parents=True,exist_ok=True);pins={}
 def read(rel):
  p=repo/rel;b=p.read_bytes();pins[str(p)]=hashlib.sha256(b).hexdigest();return b.decode().replace("\r\n","\n")
 base="Tools/CI/SkirmishSourceSimulation/Adapters/PlayerPlacement/";values={n:read(base+n) for n in FILES}
 native=read("Assets/Game/Scripts/Systems/BuildingPlacementConstructionTransaction.cs")
 kernel=read("Assets/Game/Scripts/Simulation/ConstructionTransactionKernel.cs")
 for a,b in (("BuildingPlacementLifecycleCompositionSystemHelper","SourceHostPlacementLifecycle"),("BuildingPlacementConstructionTransaction","SourceHostPlacementTransaction"),("BuildingConstructionResourceTransactionSystemHelper","SourcePlayerConstructionReservation"),("BuildingPlacementCommitCompositionSystemHelper","SourceHostPlacementCommit")):native=native.replace(a,b)
 if native!=values["PlacementTransaction.cs"]:raise ValueError("Original transaction transformation changed")
 values["PlacementTransaction.cs"] += "\n" + kernel
 source=read("Assets/Game/Scripts/Systems/BuildingConstructionResourceTransactionSystemHelper.cs")
 for name in ("TryReserve(","TryFinalize(","TryRollback(","Settle(","ObservePlacement("):
  if body(source,name)!=body(values["Reservation.cs"],name):raise ValueError("Original reservation method changed: "+name)
 source=read("Assets/Game/Scripts/Systems/BuildingPlacementSessionCompositionSystemHelper.cs").replace("BuildingPlacementLifecycleCompositionSystemHelper","SourceHostPlacementLifecycle")
 if body(source,"public bool ConfirmBuildingPlacement(",2)!=body(values["PlacementSession.cs"],"public bool ConfirmBuildingPlacement("):raise ValueError("Original session confirm changed")
 source=read("Assets/Game/Scripts/Systems/RuntimeFactionResourceSystemHelper.cs")
 for name in ("public FactionConstructionResourceMutationResult TrySpendConstructionResources(","public FactionConstructionResourceMutationResult TryRestoreConstructionResources("):
  if body(source,name)!=body(values["ResourceWrites.cs"],name):raise ValueError("Original resource method changed: "+name)
 for name,value in values.items():(out/name).write_text(value)
 return {"source_inputs":pins,"native_preview_bound":False,"player_birth_bound":False}
