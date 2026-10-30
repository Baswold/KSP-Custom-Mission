# MegaCrew isolated integration builder

This stdlib-only tool reconstructs a reviewable source tree from fixed Git
objects. It reads the published `Hermes-Practice-v0.3.0.zip` and cumulative
`Hermes-Campaign-v0.3.4-source.patch` from an explicit upstream Git ref, checks
their recorded SHA-256 values, extracts the archive safely at the output root,
applies the campaign patch once, and overlays tracked `Source/Navigation/*.cs`
files from an explicit navigation ref. It compiles C# 7.3 sources only into the
new output directory when requested. No install, game save outside the pinned
published archive, Luna server, network download, or checkout write is used.

The parent of `--output` must exist and the output path must be new. If a build
fails after output creation, the incomplete output directory is removed. The
manifest includes resolved commit IDs, input hashes, navigation file hashes,
builder/compiler inputs, every output file hash, and a content digest for the output tree
that excludes `manifest.json` itself.

## Prepare the source tree without a KSP installation

```sh
python3 tools/build_navigation_candidate.py \
  --repo /path/to/KSP-Custom-Mission \
  --upstream-ref 9574934 \
  --navigation-ref 11f757e \
  --output /tmp/megacrew-candidate \
  --prepare-only
```

## Compile using the user's own KSP assemblies

```sh
python3 tools/build_navigation_candidate.py \
  --repo /path/to/KSP-Custom-Mission \
  --upstream-ref 9574934 \
  --navigation-ref 11f757e \
  --output /tmp/megacrew-candidate \
  --managed /path/to/KSP_Data/Managed \
  --csc /path/to/dotnet/sdk/VERSION/Roslyn/bincore/csc.dll
```

The output contains the extracted pristine practice package and its pre-existing
0.3.0 DLL until compilation replaces that DLL inside the newly created output.
It is a review artifact, not an installer. Do not copy it into a game until its
separate runtime and acceptance gates have passed.

## Tests

```sh
python3 -m unittest discover -s tests -p test_navigation_candidate.py -v
```

Tests cover bad hashes, traversal/absolute/Windows/backslash paths, symlinks,
case-colliding members, extraction root layout, output/path overwrite refusal,
and preservation of existing files.
