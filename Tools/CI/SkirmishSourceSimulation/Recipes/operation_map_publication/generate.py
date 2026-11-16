from pathlib import Path
import hashlib,types
def generate(repo,output):
 root=Path(__file__).parent;before={str(p):hashlib.sha256(p.read_bytes()).hexdigest()for p in root.glob('*.py')};proof={}
 for name in ['regenerate_root','regenerate_publisher','regenerate_full_read_models']:
  p=root/(name+'.py');m=types.ModuleType(name);m.__file__=str(p);exec(compile(p.read_bytes(),str(p),'exec'),m.__dict__);proof[name]=m.generate(Path(repo),Path(output))
 if before!={p:hashlib.sha256(Path(p).read_bytes()).hexdigest()for p in before}:raise ValueError('Publication recipe/helper drift')
 return proof
