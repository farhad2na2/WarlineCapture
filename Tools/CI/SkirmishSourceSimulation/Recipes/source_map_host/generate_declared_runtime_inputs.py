from importlib.util import module_from_spec, spec_from_file_location
from pathlib import Path

def generate(repo, output):
    helper=Path(repo)/'Tools/CI/SkirmishSourceSimulation/Recipes/source_map_host/generate.py'
    spec=spec_from_file_location('source_map_host_generator',helper)
    module=module_from_spec(spec);spec.loader.exec_module(module)
    return module.generate_declared_inputs_output(repo,output,Path(__file__))
