#!/usr/bin/env python3
"""Read-only package checks and an opt-in, stock-only macOS test copy."""
import argparse
import json
from pathlib import Path
import re
import shutil
import sys

PACKAGE_FILES = (
    'dependencies.json', 'Source/MegaCrew.cs', 'Source/Practice.cs',
    'GameData/MegaCrew/Plugins/MegaCrew.dll',
    'saves/MegaCrew Practice/persistent.sfs',
    'Ships/VAB/Hermes Walkthrough v2.craft',
)
# Deliberate allowlist: no installed mods, settings, logs, saves or server data.
STOCK_ITEMS = ('KSP.app', 'GameData/Squad', 'buildID.txt', 'Physics.cfg',
               'LegalNotice.txt', 'readme.txt')


def package_errors(root):
    errors = []
    for name in PACKAGE_FILES:
        path = root / name
        if not path.is_file() or path.stat().st_size == 0:
            errors.append('Missing or empty package file: ' + name)
    manifest = root / 'dependencies.json'
    if manifest.is_file():
        try:
            data = json.loads(manifest.read_text())
            if not isinstance(data, (dict, list)) or not data:
                errors.append('dependencies.json must contain a nonempty object or array')
        except (ValueError, OSError) as exc:
            errors.append('Cannot parse dependencies.json: ' + str(exc))
    return errors


def stock_errors(source):
    errors = []
    for name in STOCK_ITEMS:
        if not (source / name).exists():
            errors.append('Missing stock item: ' + name)
    build = source / 'buildID.txt'
    if build.is_file() and not re.search(r'^build id\s*=\s*03190\s*$',
                                        build.read_text(), re.MULTILINE):
        errors.append('Expected KSP build 03190')
    return errors


def prepare_copy(source, destination):
    source, destination = source.resolve(), destination.resolve()
    if source == destination or source in destination.parents or destination in source.parents:
        raise ValueError('Source and destination must be separate, non-nested directories')
    errors = stock_errors(source)
    if errors:
        raise ValueError('; '.join(errors))
    # mkdir without exist_ok prevents replacing any existing installation/save.
    destination.mkdir(parents=True, exist_ok=False)
    try:
        for name in STOCK_ITEMS:
            src, dst = source / name, destination / name
            dst.parent.mkdir(parents=True, exist_ok=True)
            if src.is_dir():
                shutil.copytree(src, dst, symlinks=True)
            else:
                shutil.copy2(src, dst)
        (destination / 'saves').mkdir()
        (destination / 'Ships' / 'VAB').mkdir(parents=True)
        (destination / 'MEGACREW-STOCK-COPY.txt').write_text(
            'Stock-only test copy prepared. Mods and mission are not installed.\n'
            'No startup or gameplay validation has been performed.\n')
    except Exception:
        # Preserve partial output for inspection; never recursively delete a user path.
        print('Copy incomplete; inspect the destination before retrying.', file=sys.stderr)
        raise


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--repo', type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument('--stock', type=Path, help='Existing KSP directory; read only')
    parser.add_argument('--prepare-copy', type=Path, metavar='NEW_DIRECTORY',
                        help='Copy only stock items into a new directory (requires --stock)')
    args = parser.parse_args()
    if args.prepare_copy and not args.stock:
        parser.error('--prepare-copy requires --stock')
    errors = package_errors(args.repo)
    if args.stock:
        errors.extend(stock_errors(args.stock))
    if args.prepare_copy:
        try:
            prepare_copy(args.stock, args.prepare_copy)
            print('Prepared stock-only copy. Dependencies and mission still need installation.')
        except (OSError, ValueError) as exc:
            errors.append('Copy failed: ' + str(exc))
    for error in errors:
        print('FAIL: ' + error)
    if errors:
        print('Preflight incomplete. Do not claim startup or mission validation.')
        return 1
    print('Package files present. Dependency hashes, runtime loading and gameplay remain unverified.')
    return 0


if __name__ == '__main__':
    sys.exit(main())
