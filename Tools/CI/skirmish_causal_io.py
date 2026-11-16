"""Bounded regular-file and duplicate-key JSON readers used by causal admission."""
import hashlib,json,os,stat
from pathlib import Path
def need(x,m):
 if not x: raise ValueError(m)
def sha(b): return hashlib.sha256(b).hexdigest()
def read(path,limit):
 p=Path(path).absolute()
 for node in [p,*p.parents]:
  s=node.lstat();need(not stat.S_ISLNK(s.st_mode),'Symlink/ancestor rejected')
 s=p.lstat();need(stat.S_ISREG(s.st_mode) and 0<s.st_size<=limit,'Positive bounded regular file required before open')
 fd=os.open(p,os.O_RDONLY|os.O_NOFOLLOW)
 try:
  f=os.fstat(fd);need(stat.S_ISREG(f.st_mode) and (f.st_dev,f.st_ino,f.st_size)==(s.st_dev,s.st_ino,s.st_size),'Descriptor changed')
  buf=bytearray(s.st_size);at=0
  while at<len(buf):
   b=os.read(fd,len(buf)-at);need(b,'Truncated');buf[at:at+len(b)]=b;at+=len(b)
  need(not os.read(fd,1),'Grew during read');after=os.fstat(fd);need((after.st_size,after.st_mtime_ns,after.st_ctime_ns)==(f.st_size,f.st_mtime_ns,f.st_ctime_ns),'Changed during read')
 finally: os.close(fd)
 after=p.lstat();need((after.st_dev,after.st_ino,after.st_size,after.st_mtime_ns,after.st_ctime_ns)==(s.st_dev,s.st_ino,s.st_size,s.st_mtime_ns,s.st_ctime_ns),'Path changed')
 return bytes(buf)
def pairs(rows):
 d={}
 for k,v in rows: need(k not in d,'Duplicate JSON key');d[k]=v
 return d
def parse(b): return json.loads(b,object_pairs_hook=pairs,parse_constant=lambda s:(_ for _ in ()).throw(ValueError('Nonfinite JSON')))
