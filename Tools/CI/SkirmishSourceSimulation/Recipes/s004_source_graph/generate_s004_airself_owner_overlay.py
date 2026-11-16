#!/usr/bin/env python3
"""Create a fail-closed, opt-in AirSelf diagnostic owner overlay."""
import argparse
import hashlib
import json
from pathlib import Path


def replace_once(text, old, new):
    if text.count(old) != 1:
        raise ValueError("Source owner anchor changed: " + old[:100])
    return text.replace(old, new)


def generate(build, output):
    generated = build / "source/generated"
    queue = generated / "467_SourceProductionDeliveryOwner.cs"
    helicopter = generated / "466_SourceHelicopterDelivery.cs"
    inputs = {str(p): hashlib.sha256(p.read_bytes()).hexdigest() for p in (queue, helicopter)}
    q = queue.read_text()
    q = replace_once(q, "internal SourceHelicopterDelivery Helicopters;", "internal SourceHelicopterDelivery Helicopters;internal SourceS004AirSelfDelivery AirSelf;")
    q = replace_once(q,
        'if(settings.TransportPrefab!=null&&(settings.Mode!=BuildingProductionQueueCompositionSystemHelper.ProductionTransportMode.Helicopter||Helicopters==null||!Helicopters.Admits(settings.TransportPrefab)))throw new NotSupportedException("Source transport mode/geometry not admitted; no payment performed");',
        'if(settings.TransportPrefab!=null&&!((settings.Mode==BuildingProductionQueueCompositionSystemHelper.ProductionTransportMode.Helicopter&&Helicopters!=null&&Helicopters.Admits(settings.TransportPrefab))||(settings.Mode==BuildingProductionQueueCompositionSystemHelper.ProductionTransportMode.AirSelf&&AirSelf!=null&&AirSelf.Admits(settings.TransportPrefab))))throw new NotSupportedException("Source transport mode/geometry not admitted; no payment performed");')
    q = replace_once(q, 'Helicopters?.Advance(now,delta);return Helicopters?.AnyActive??false;',
        'Helicopters?.Advance(now,delta);AirSelf?.Advance(now,delta);return (Helicopters?.AnyActive??false)||(AirSelf?.AnyActive??false);')
    q = replace_once(q, 'return Helicopters?.AnyActive??false;}catch{Poisoned=true;throw;}}',
        'return (Helicopters?.AnyActive??false)||(AirSelf?.AnyActive??false);}catch{Poisoned=true;throw;}}')
    q = replace_once(q, 'public bool HasActiveTransport(RuntimeBuildingEntity b)=>owner.Helicopters?.HasActive(b.Id)??false;',
        'public bool HasActiveTransport(RuntimeBuildingEntity b)=>(owner.Helicopters?.HasActive(b.Id)??false)||(owner.AirSelf?.HasActive(b.Id)??false);')
    q = replace_once(q, 'public bool EnsureTransport(RuntimeBuildingEntity b,SourcePendingProduction p,float now,ref uint random)=>owner.Helicopters.TryEnsure(b,p,now);',
        'public bool EnsureTransport(RuntimeBuildingEntity b,SourcePendingProduction p,float now,ref uint random)=>p.TransportMode==BuildingProductionQueueCompositionSystemHelper.ProductionTransportMode.AirSelf?owner.AirSelf.TryEnsure(b,p,now,ref random):(owner.AirSelf?.AnyActive??false)?false:owner.Helicopters.TryEnsure(b,p,now);')
    h = helicopter.read_text()
    h = replace_once(h, 'internal bool AnyActive=>active.Count>0;', 'internal int ActiveCount=>active.Count;internal bool AnyActive=>active.Count>0;')
    h = replace_once(h, 'void PublishDeliveryCount(){SourcePublishCount(active.Count);}', 'void PublishDeliveryCount(){SourcePublishCount(active.Count+(owner.AirSelf?.ActiveCount??0));}')
    output.mkdir(parents=True, exist_ok=False)
    result = {}
    for name, text in ((queue.name, q), (helicopter.name, h)):
        path = output / name
        path.write_text(text)
        result[name] = hashlib.sha256(path.read_bytes()).hexdigest()
    receipt = {"status": "Generated", "kind": "S004_AIRSELF_SINGLE_TRANSPORT_SOURCE_DIAGNOSTIC_OVERLAY",
        "source_sha256": inputs, "outputs_sha256": result,
        "mixed_transport_concurrency_supported": False, "native_parity": False, "mission_ready": False}
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    return receipt


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--build", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    print(json.dumps(generate(args.build.resolve(strict=True), args.output.resolve())))
