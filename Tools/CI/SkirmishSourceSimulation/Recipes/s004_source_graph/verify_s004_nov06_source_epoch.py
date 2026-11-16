#!/usr/bin/env python3
"""Independently reconstruct the November 6 diagnostic inputs."""
import argparse
import json
from pathlib import Path

import build_s004_nov06_source_epoch as epoch
import verify_s004_merged_source_epoch as original


def verify(bundle, digest, output):
    prior = original.epoch
    try:
        original.epoch = epoch
        return original.verify(bundle, digest, output)
    finally:
        original.epoch = prior


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--bundle', type=Path, required=True)
    parser.add_argument('--sha256', required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--receipt', type=Path, required=True)
    args = parser.parse_args()
    result = verify(args.bundle, args.sha256, args.output)
    with args.receipt.open('x') as stream:
        json.dump(result, stream, indent=2)
        stream.write('\n')
    print(json.dumps(result))


if __name__ == '__main__':
    main()
