from pathlib import Path
import types
def generate(repo,output):
 p=Path(__file__).with_name('generate_current.py');m=types.ModuleType('selected_map_current');m.__file__=str(p);exec(compile(p.read_bytes(),str(p),'exec'),m.__dict__);return m.generate_sources(repo,output)
