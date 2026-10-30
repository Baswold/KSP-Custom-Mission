# Mac installation handoff

## Observed on 2026-10-30

At upstream commit `49c7385`, only README.md is tracked. The dependency manifest,
custom plugin/source, craft and practice save described there are absent. This
initially blocked installation. Upstream commit `aababb7` subsequently supplied
`Hermes-Practice-v0.2.0.zip` and the manifest. The extracted archive passes the
package preflight. The local stock installation reports build **03190**.
Runtime results are recorded below separately from these file checks.

Coordination: https://github.com/w4mdzzn8my-dotcom/KSP-Custom-Mission/issues/1

## Reproducible preflight

Python 3, standard library only:

```sh
# Extract the upstream package into a separate staging directory first.
python3 -m zipfile -e Hermes-Practice-v0.2.0.zip /path/to/package-staging
python3 tools/mac_preflight.py --repo /path/to/package-staging --stock '/path/to/Kerbal Space Program'
python3 -m unittest discover -s tests -v
```

The preflight reports missing/empty package files and malformed manifest JSON.
It does not yet validate the manifest schema, dependency archive hashes, DLL
version, craft correctness or in-game loading. A passing result is only a file
presence check.

To prepare a separate stock-only test directory:

```sh
python3 tools/mac_preflight.py --repo /path/to/package-staging --stock '/path/to/Kerbal Space Program' \
  --prepare-copy '/path/to/NEW-MegaCrew-Test'
```

This copies the Mac application, stock Squad assets and required root files using
an explicit allowlist. It excludes installed mods, DLC, settings, existing saves,
logs and server data. It refuses existing destinations and nested source/target
paths. It deliberately prepares the stock copy even if the mission package is
missing, but still exits nonzero until preflight passes. A failed copy is left in
place for inspection; select a new destination for a retry.

With the package extracted, use the README's pinned archives and install the separate
SSPX IVA extras. Verify archive hashes against the uploaded manifest before
installing. Do not borrow the user's existing mod collection. Copy the practice
save only into a previously absent save directory.

## Evidence still required

Record each result as PASS, FAIL or NOT TESTED; do not infer gameplay from files.

| Check | Required evidence |
| --- | --- |
| Plugin startup | Fresh test-copy KSP.log entries showing MegaCrew 0.2.0, FreeIVA, DeepFreeze and EasyVesselSwitch loaded; note loader errors |
| Generated save | Hermes at roughly 150 km, four named crew, no missing-part dialog |
| Vessel stability | Loaded ship remains intact; stationary centrifuge; panels deploy without collision |
| Walking | Command to cryobay route, every hatch, camera and controls |
| Objectives | Deployment detection, 60-second charge observation, medical recovery |
| Cryosleep | Ada enters actual frozen state, then thaws with crew roster restored |
| Persistence | Save/reload preserves objective progress and timers without duplication |

Share only short redacted excerpts. A screenshot or observed successful walk is
stronger evidence than a manual objective checkbox. LMP co-op needs its own
multi-client test; none of the above establishes multiplayer synchronization.
