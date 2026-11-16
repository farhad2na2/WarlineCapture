#!/usr/bin/env python3
"""Extract ordinary airdrop source; reject unbound Support behavior explicitly.

This is a diagnostic projection, not an admitted Unity or Support scheduler.
"""
import argparse
import hashlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
SOURCE = ROOT / "Assets/Game/Scripts/Systems/UnitTransportAirdropSystem.cs"
REPLACEMENTS = {
    "if(support){SupportParatrooperAdapterSystemHelper.BeforeDrop(em,ecb,transport,Entity.Null,grid,_gridQuery.GetSingletonEntity(),out _,SupportRejectionReason.TargetGone);return;}":
        'if(support) throw new NotSupportedException("Support paratrooper owner not admitted");',
    "if(support){SupportParatrooperAdapterSystemHelper.BeforeDrop(em,ecb,transport,passenger,grid,_gridQuery.GetSingletonEntity(),out _,SupportRejectionReason.NotReady);return;}":
        'if(support) throw new NotSupportedException("Support paratrooper owner not admitted");',
    "{if(!SupportParatrooperAdapterSystemHelper.BeforeDrop(em,ecb,transport,passenger,grid,_gridQuery.GetSingletonEntity(),out landingCell))return;passengers=em.GetBuffer<UnitTransportPassengerElement>(transport);}":
        '{throw new NotSupportedException("Support paratrooper owner not admitted");}',
}

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--source-sha256", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    raw = SOURCE.read_bytes()
    if hashlib.sha256(raw).hexdigest() != args.source_sha256:
        raise ValueError("Original airdrop source pin changed")
    text = raw.decode("utf-8")
    for original, replacement in REPLACEMENTS.items():
        if text.count(original) != 1: raise ValueError("Original Support airdrop seam changed")
        text = text.replace(original, replacement)
    if "SupportParatrooperAdapterSystemHelper" in text:
        raise ValueError("Unreviewed Support paratrooper use")
    Path(args.output).write_text(text)
    print("[S004AirdropProjection] result=Generated scope=SupportFailsClosed")

if __name__ == "__main__": main()
