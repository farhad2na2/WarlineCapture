"""Invoke the existing selected-map owner for the refreshed diagnostic epoch."""
import hashlib
import importlib.util
from pathlib import Path


def generate(repo, output):
    owner = Path(repo) / 'Tools/CI/SkirmishSourceSimulation/Recipes/s002_source_graph/generate_s002_selected_map_graph_attestation.py'
    before = hashlib.sha256(owner.read_bytes()).hexdigest()
    spec = importlib.util.spec_from_file_location('s002_refreshed_selected_map_owner', owner)
    if spec is None or spec.loader is None:
        raise ValueError('Original selected-map owner unavailable')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    result = module.generate(Path(repo), Path(output), refreshed=True)
    if hashlib.sha256(owner.read_bytes()).hexdigest() != before:
        raise ValueError('Selected-map owner changed during refreshed derivation')
    return result
