# Isolated navigation candidate builder — 0.3.6 profile

This temporary profile is based on builder commit `3ea374b`. It reconstructs a
candidate from the pristine published `Hermes-Practice-v0.3.0.zip` in upstream
commit `f426997` and applies exactly one cumulative patch:

- `Hermes-Campaign-v0.3.6-source.patch` from `f426997`, SHA-256
  `8756f2dfb46e3e915f1185fd440e79cd84f6d6a5cbd291d7583863d1a0777f05`.

The 0.3.6 patch includes the cumulative 0.3.5 campaign and epoch work. Apply it
once to the pristine 0.3.0 ZIP. Do not stack it with 0.3.5, the epoch planner,
0.3.4, or the superseded console/supply/QA supplements. The builder verifies
the ZIP and patch hashes before creating output, preserves both artifacts under
`provenance/`, records the single patch sequence in `manifest.json`, and overlays
tracked navigation C# files from `418229a`.

The output must be a new directory outside the checkout. It is a review/build
artifact, not an installer. No game, save, server, or network access is used.

## Prepare without KSP assemblies

```sh
python3 tools/build_navigation_candidate.py \
  --repo /path/to/KSP-Custom-Mission \
  --upstream-ref f426997 \
  --navigation-ref 418229a \
  --output /tmp/megacrew-candidate \
  --prepare-only
```

## Compile against KSP 1.12.5

```sh
python3 tools/build_navigation_candidate.py \
  --repo /path/to/KSP-Custom-Mission \
  --upstream-ref f426997 \
  --navigation-ref 418229a \
  --output /tmp/megacrew-candidate \
  --managed /path/to/KSP_Data/Managed \
  --csc /path/to/dotnet/sdk/VERSION/Roslyn/bincore/csc.dll
```

## Focused tests

```sh
python3 -m unittest discover -s tests -p test_navigation_candidate.py -v
```

The original safety suite and dependent two-patch sequence fixture are retained.
