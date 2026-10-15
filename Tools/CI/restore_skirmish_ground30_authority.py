"""Verify or explicitly restore one immutable ignored authoring input from main's archive."""
import argparse
import hashlib
from pathlib import Path
import zipfile

ROOT=Path(__file__).resolve().parents[2]
ARCHIVE='Design/AgentReports/SkirmishExpansion/ProductionAdapter/shared-production-native-20261015/run09/checked-native-shared-controls-and-placement.zip'
ARCHIVE_SHA='68512e7e880d2f955b62e2ee4b4f2af5b670029cbbec567f2405e96b5c16e197'
MEMBER='Logs/SkirmishProduction/ground-source-fit-20261015-11.json'
AUTHORITY_SHA='18b520ff6cb45a25f645502cba1787b27375cb52a20843195dd501e3b7029f73'
AUTHORITY_BYTES=2340364

def digest(raw):return hashlib.sha256(raw).hexdigest()

def verify_or_restore(root,do_restore=False):
    root=Path(root).resolve();archive=root/ARCHIVE;target=root/MEMBER
    if digest(archive.read_bytes())!=ARCHIVE_SHA:raise ValueError('Tracked authority archive bytes changed')
    with zipfile.ZipFile(archive) as source:
        matches=[row for row in source.infolist() if row.filename==MEMBER]
        if len(matches)!=1 or matches[0].file_size!=AUTHORITY_BYTES:raise ValueError('Exact single bounded authority member required')
        raw=source.read(matches[0])
    if len(raw)!=AUTHORITY_BYTES or digest(raw)!=AUTHORITY_SHA:raise ValueError('Archived authority member differs from frozen envelope pin')
    if target.exists():
        if not target.is_file() or digest(target.read_bytes())!=AUTHORITY_SHA:raise ValueError('Existing authority differs; never overwrite it')
        return 'VerifiedExistingAuthority'
    if not do_restore:return 'VerifiedArchive_RestoreRequired'
    target.parent.mkdir(parents=True,exist_ok=True)
    # Never extract archive paths generally. Only this literal target is writable,
    # only with exclusive create, after full archive/member byte verification.
    with target.open('xb') as destination:destination.write(raw)
    return 'RestoredExactAuthority'

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--restore',action='store_true')
    args=parser.parse_args();result=verify_or_restore(ROOT,args.restore)
    print('[SkirmishGround30Authority] result='+result+' sha256='+AUTHORITY_SHA+' sourceFitCertified=0')
