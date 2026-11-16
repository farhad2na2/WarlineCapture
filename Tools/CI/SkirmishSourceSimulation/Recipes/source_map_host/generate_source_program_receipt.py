"""Generate the source-map host with its installed-map receipt and clearance handoff."""
from importlib.util import module_from_spec, spec_from_file_location
from pathlib import Path


def generate(repo, output):
    helper = Path(repo) / 'Tools/CI/SkirmishSourceSimulation/Recipes/source_map_host/generate.py'
    spec = spec_from_file_location('source_map_host_generator', helper)
    module = module_from_spec(spec)
    spec.loader.exec_module(module)
    receipt = module.generate_program_output(repo, output, Path(__file__))
    target = Path(output) / 'SourceSimulationProgram.cs'
    text = target.read_text()
    text = module.once(text, '  if(projected!=null){if(!deferredClearance',
                       '  if(preparedSourceMap!=null)clearance=preparedSourceMap.Clearance;\n  else if(projected!=null){if(!deferredClearance')
    text = module.once(text, 'result["source_delivery_events"]=JArray.FromObject(world.ActualDeliveryOwner.DeliveryEvents);',
                       module.live_owner_result_handoff())
    target.write_text(text)
    receipt['outputs'][target.name] = module.digest(target.read_bytes())
    return receipt
