"""Generate the ordinary source-host ARIA bridge for the shared S003 air opening."""
import hashlib
from pathlib import Path

HOST='Tools/CI/SkirmishSourceSimulation/Adapters/493_GeneratedS002UnifiedAria.s003.cs'
BRIDGE='Tools/CI/SkirmishSourceSimulation/Adapters/SourceS003AirOpeningController.cs'
OUTPUT='SourceS003SharedAirOpeningWorld.cs'


def generate(repo, output):
    repo=Path(repo).resolve(strict=True);paths=[repo/HOST,repo/BRIDGE];raw=[p.read_bytes() for p in paths];source=raw[0].decode()
    def once(old,new):
        nonlocal source
        if source.count(old)!=1:raise ValueError('Exact source host seam required: '+old[:100])
        source=source.replace(old,new,1)
    once('private SourceAriaS002Controller sourceS002Aria;', 'private SourceS003AirOpeningController sourceS002Aria;')
    once('sourceS002Aria=new SourceAriaS002Controller(Entities,Session,Army,WorldId,actualPaid,context.SourcePresetLoadIndex,context,sourceNumericCamera,S003PlayerPlacement);',
         'if(Setup.CatalogId!="S003"||match.ScenarioIndex!=5)throw new InvalidOperationException("Exact S003 source air-opening diagnostic required");sourceS002Aria=new SourceS003AirOpeningController(Entities,Session,Army,actualPaid,WorldId,context);')
    if any(p.read_bytes()!=data for p,data in zip(paths,raw)):raise ValueError('Source changed during generation')
    dest=Path(output);dest.mkdir(parents=True,exist_ok=False);data=source.encode();(dest/OUTPUT).write_bytes(data)
    return {'source_pins':{str(p):hashlib.sha256(data).hexdigest() for p,data in zip(paths,raw)},'outputs':{OUTPUT:hashlib.sha256(data).hexdigest()},
            'experimental_only':True,'scope':'Same ordinary source host with actual shared air-opening policy and paid owner; partial planner, no native input/readiness'}
