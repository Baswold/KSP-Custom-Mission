# Isolated navigation candidate builder

This stdlib-only tool reconstructs a reviewable source tree from fixed Git
objects. By default it reads the pristine published `Hermes-Practice-v0.3.0.zip`
from upstream commit `f749068`, verifies its SHA-256, then applies an ordered,
hash-pinned patch sequence to a fresh extraction:

1. `Hermes-Campaign-v0.3.5-source.patch` from `4e86cc0` (cumulative source;
   replaces the previous 0.3.4 patch and its console/supply/QA supplements).
2. `Hermes-Epoch-Planner.patch` from `591ca74` (additive epoch/watch planner).

The 0.3.5 patch applies to the pristine 0.3.0 package exactly once; do not stack
it on top of 0.3.4 or apply the superseded supplements. The second patch is
applied only after the first. The tool verifies every artifact hash before it
creates the output directory, preserves the ZIP and both patches in
`provenance/`, records the ordered patch refs/hashes in `manifest.json`, and
overlays tracked `Source/Navigation/*.cs` from navigation ref `418229a`. It
compiles C# 7.3 sources only into the new output directory when requested. No
install, game save outside the pinned published archive, server access, network
download, or checkout write is used.

The output parent must exist and the output directory must be new. If a build
fails after output creation, the incomplete output directory is removed. The
manifest includes resolved commit IDs, input hashes, ordered patch metadata,
navigation file hashes, builder/compiler inputs, every output file hash, and a
content digest for the output tree that excludes only the root `manifest.json`.

## Prepare the source tree without a KSP installation

```sh
python3 tools/build_navigation_candidate.py \
  --repo /path/to/KSP-Custom-Mission \
  --upstream-ref f749068 \
  --navigation-ref 418229a \
  --output /tmp/megacrew-candidate \
  --prepare-only
```

## Compile using the user's own KSP assemblies

```sh
python3 tools/build_navigation_candidate.py \
  --repo /path/to/KSP-Custom-Mission \
  --upstream-ref f749068 \
  --navigation-ref 418229a \
  --output /tmp/megacrew-candidate \
  --managed /path/to/KSP_Data/Managed \
  --csc /path/to/dotnet/sdk/VERSION/Roslyn/bincore/csc.dll
```

The output contains the extracted published practice package and its original
DLL until compilation replaces that DLL inside the new output. It is a review
artifact, not an installer. Do not copy it into a game until separate runtime
and acceptance gates pass.

## Tests

```sh
python3 -m unittest discover -s tests -p test_navigation_candidate.py -v
```

Tests cover bad artifact hashes, traversal/absolute/Windows/backslash paths,
symlinks, case-colliding members, extraction-root layout, output/path overwrite
refusal, preservation of existing files, and ordered patch application using a
synthetic two-patch Git fixture.
