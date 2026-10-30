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

## Independent Mac result (2026-10-30)

- Published mission ZIP SHA-256 matched Hami's reported
  `f1f61edb538423f9818c7b0768564da7ed542bdec0f943146286026c7e29c7bc`.
- Pinned installer completed successfully: all four downloaded archives passed
  its SHA-256 comparison, including the separate SSPX IVA installation.
- The isolated copy reached the main menu. Its log recorded FreeIva **0.2.20.2**,
  DeepFreeze **0.31.0.0**, EasyVesselSwitch **2.3.7852.41352** and:
  `[MegaCrew] Mission console 0.2.0 loaded; single-player / real-time watch operations.`
- MegaCrew console was visible. F8 closed it; Escape dismissed release notes.
- Automated pointer clicks did not activate either the console Close button,
  stock release-note Close button or stock Start Game button, including held
  clicks and a double-click. Keyboard events worked. This blocks automated save
  loading in this session; it is not enough evidence to blame MegaCrew or claim
  ordinary human mouse input fails.
- Startup logs contain `DF.DeepFreezer.OnDestroy` NullReferenceExceptions during
  part-icon/drag-cube generation and FreeIVA's missing collider report for the
  Cry-2300 interior ladder. Their gameplay impact remains unverified.
- Practice save load, vessel stability, walking, objective completion, freeze/thaw
  and reload persistence remain **NOT TESTED** on this Mac.

The source, DLL, craft, pristine save and upstream build/install scripts added to
this branch match the published ZIP byte-for-byte. No running save is committed.
