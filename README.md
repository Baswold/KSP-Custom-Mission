# MegaCrew: Hermes orbital maintenance

Custom cooperative crew gameplay for **Kerbal Space Program 1.12.5 build 03190**. Start with single player; multiplayer is not enabled in this package. Inspired by the ship and engineering work in *The Martian*. Cooperative tasks only: no murders, traitors or sabotage.

## Download the custom mission

The complete source, compiled plugin, generated save/craft and tools are in **Hermes-Practice-v0.3.0.zip** in this repository. Extract it into the repository folder before using the paths below. Third-party dependencies download separately from their official releases.

```sh
python3 -m zipfile -e Hermes-Practice-v0.3.0.zip .
python3 tools/install.py --ksp-root "/path/to/your/separate/clean/KSP" --download-dependencies
```

The installer requires Python 3.9+ and refuses to overwrite an existing practice save or install into an already modded copy. Keep KSP closed during installation. For manual installation, follow the steps below.

## Basil's Codex: start here

1. Read this README and the repository's Issues. Use issues for progress, reproducible bugs and coordination. Do not post passwords, tokens, server configuration or private logs.
2. Make a **separate clean KSP copy**. Do not modify Basil's existing installation, saves, or any Luna server Universe.
3. Install the pinned dependencies below, including SSPX's separate IVA interiors. Add this repository's `GameData/MegaCrew` to the copy's GameData. Never create `GameData/GameData`.
4. Copy `saves/MegaCrew Practice` into the copy's `saves`, only if that save name does not already exist. Copy `Ships/VAB/Hermes Walkthrough v2.craft` into the practice save's `Ships/VAB` if wanted in the editor.
5. Launch that separate copy. In KSP.log verify MegaCrew **0.3.0**, FreeIVA, DeepFreeze and EasyVesselSwitch actually loaded. Folder presence is not proof.
6. Load **MegaCrew Practice**, then fly **Hermes Maintenance Trainer** if the game opens at the space center. Press **F8 → Practice → Begin practice on this vessel**.
7. Report results in the relevant issue. Distinguish source/build checks from actual in-game verification. Do not claim a completed test until observed in game.

**Current validation:** custom DLL compiles against KSP 1.12.5. All 17 ship part identifiers match installed parts. The generated practice save is configured for a circular **150 km Kerbin orbit** with four assigned crew. **The original 0.2 trainer loaded into flight on Hami’s Mac. Version 0.3 interior interactions, pantry persistence and full mission completion still need in-game verification.** Hami is testing the host copy. The older editor craft loaded successfully; that does not prove the new save is flight-qualified.

## Required dependencies

Use official release archives, not source-code ZIPs. `dependencies.json` records the exact download URLs and locally measured archive SHA-256 values.

| Dependency | Version | Installation |
| --- | --- | --- |
| [Station Parts Expansion Redux](https://github.com/post-kerbin-mining-corporation/StationPartsExpansionRedux/releases/tag/2.0.11) | 2.0.11 | GameData: StationPartsExpansionRedux, B9PartSwitch, NearFutureProps and ModuleManager 4.2.3. Also install `Extras/StationPartsExpansionReduxIVAs` into GameData. |
| [FreeIVA](https://github.com/FirstPersonKSP/FreeIva/releases/tag/0.2.20.2) | 0.2.20.2 | GameData/FreeIva. Walkable interiors, including supported SSPX and DeepFreeze rooms. |
| [DeepFreeze](https://github.com/JPLRepo/DeepFreeze/releases/tag/v0.31.0.0) | 0.31.0.0 | GameData/REPOSoftTech and CommunityResourcePack. Keep only the newer ModuleManager 4.2.3; skip bundled 4.2.1. |
| [Easy Vessel Switch](https://github.com/ihsoft/EasyVesselSwitch/releases/tag/2.3) | 2.3 | GameData/EasyVesselSwitch, including its bundled KSPDev Utils DLL. Camera focus and EVA/vessel switching; it does not provide interior walking. |

No DLC is required. Do not install Luna or Harmony for this single-player test. No Tailscale is required. This repository contains custom code, generated craft/save and setup files; it does not redistribute KSP or the third-party mod assets.

## Version 0.3: interior interaction and crew needs

Press **C** to enter IVA, **Y** to unbuckle and **I** to open the station for your current room. F8 closes the large mission console if it covers the view. The station uses FreeIVA's actual current compartment and the currently viewed Kerbal; changing the watch-lead label does not move a Kerbal.

| Room | Available operations |
| --- | --- |
| Quarters / centrifuge habitat | Eat a ration, drink water, sleep or wake |
| Corridor / centrifuge spine | Extend/retract actual solar arrays and radiators |
| Lab | Treat an injured crew member; record orbital observations |
| Cryobay | Request actual DeepFreeze freezing and sequential thaw |
| Command | Orbital, crew and electric-charge readouts |
| Any supported room | Inspect a nearby prop collider under the IVA camera crosshair |

This is **room-based interaction**, not a claim that every decorative object has a physical animation or function. FreeIVA still handles hatches with F. No fake science points, arbitrary part repairs, docking or autopilot commands are added.

Food and Water are real CRP resources stored aboard the habitation module. On its first loaded flight in a MegaCrew save, the new pantry module provisions **84 Food / 42 Water** units. Resources and its one-time provision flag are saved by KSP. It does not refill existing stores on every reload. The existing save is not replaced. Save your game after provisioning/consumption. The new module and this migration need an in-game persistence check.

A ration consumes **0.5 Food** and reduces hunger by 40 points. A drink consumes **0.25 Water** and reduces thirst by 40 points. Sleep requires another awake watch member. Meters advance only in loaded flight at 1x, including when watch tracking is paused; they do not catch up offline. Ordinary sleep is an abstract crew state, not a new bunk animation.

Game rates: hunger fills in 6 hours, thirst and awake fatigue in 4 hours; ordinary rest removes full fatigue over 2 hours. Frozen crew pause food/water needs and recover fatigue. These fictional Kerbal rates are not human medical predictions. At 90% hunger, thirst or fatigue, the custom engineering/science controls become unavailable until addressed. Stock flight controls, treatment and access to food/water remain available; no deaths are simulated. Oxygen, carbon dioxide, waste/recycling and autonomous NPC work are not implemented.

Planet graphics: **EVE Redux 1.11.7.2 + BoulderCo stock-planet clouds 1.2.2.1** are pinned in dependencies.json. Install only this cloud configuration; do not combine with another EVE cloud pack. They preserve stock planet sizes/orbits. Both are installed in Hami's copy; visual rendering and performance on the Mac remain pending a restart/test. Third-party visuals are downloaded separately, not redistributed in the custom archive.

Lander storage/hangar work has been requested from Basil's Codex through PR #2 / issue #1. No deployed or recoverable lander is claimed in this version.

## Practice mission

The fresh save starts Hermes in orbit with **Hamish Kerman** (pilot), **Basil Kerman** (engineer), **Ada Kerman** (relief physician/scientist) and **Vikram Kerman** (relief engineer). All four start awake. Time warp is disabled in saves whose names begin `MegaCrew`. No offline progress is simulated.

1. Verify orbit and four crew. The console checks periapsis above 120 km and apoapsis below 180 km.
2. Extend at least two solar arrays and one radiator through their part menus. The console checks actual deployment state.
3. Observe stable ElectricCharge for 60 seconds. A drop greater than 2% of capacity from the observation's start resets it. This is a charge-trend exercise, not proof of a fault-free power system.
4. Walk command → quarters → lab/medical room → centrifuge spine → corridor → cryobay. Sign off manually after inspection.
5. In Emergency, record a training sprain. In Crew & cryosleep, treat it with another awake, uninjured crewmember available. Keep the watch running during the five-minute recovery.
6. Transfer Ada to the DeepFreeze pod using stock crew transfer. Freeze her through the pod or console; then request thaw and confirm she is awake. Actual DeepFreeze state is monitored; sufficient power and Glykerol are required.
7. Confirm the orbit and four crew, then save and report the result.

Allow roughly 10–15 minutes plus learning time. Power/medical incidents are training exercises, not random physical damage. NPCs do not autonomously walk, fly or repair. Cryosleep uses the real DeepFreeze API, but the NPC watch system is a timed wake queue, not AI crew autonomy.

## Controls and movement

- **I in IVA:** current-room station.
- **F8:** MegaCrew console. Keep watch tracking running for timers and medical recovery.
- **C:** enter IVA; **Y:** unbuckle; **WASD:** move; **Shift/Ctrl:** up/down; **Q/E:** roll; **F:** operate the hatch you are looking at. Walking works in flight, not the editor.
- Optional slower interior controls: back up `GameData/FreeIva/settings.cfg`, then replace it with `docs/FreeIVA-slow-movement.cfg` before starting KSP.
- Easy Vessel Switch: hold **O** and click a part to focus the camera; **Option/Alt-click** another vessel or EVA Kerbal to switch; **F7** changes camera stabilization. See the mod's own README for configurable bindings.

The centrifuge currently behaves poorly on Hami's copy. Keep it stopped for practice; maintenance tasks do not require rotation. The aft tank and propulsion section are not a walking route. This craft is an orbital trainer without a launch booster or qualified crew-return vehicle.

## Development and coordination

`Source/MegaCrew.cs` is the main console and DeepFreeze bridge. `Source/Practice.cs` implements maintenance objectives. `Source/Interior.cs` handles cabin stations; `Source/CrewNeeds.cs` handles needs, resources and pantry provisioning. `GameData/MegaCrew/Plugins/MegaCrew.dll` is the compiled plugin. Save state lives in the selected save's `MegaCrewMission.cfg`; the stock game state remains in `persistent.sfs`.

Keep changes scoped to the separate test copy and use focused GitHub issues/PRs. Avoid simultaneous edits to the same files. Hami's Codex owns the initial practice implementation and host test. Basil's Codex should first verify installation on Basil's Mac and report startup/load evidence, then take an agreed issue.

Future multiplayer requires separate integration and testing: Luna 0.29.2 alone will not synchronize these custom objectives or health records. Do not claim co-op success until both clients share the same session and observe a shared vessel/Kerbal. Never overwrite or migrate the existing server Universe without a separate explicit request.

Known pending checks: generated save load, physical stability, hatch traversal, panel clearance, deployment-state objective detection, full cryosleep handover, and timer persistence after save/reload. Relevant logs should be short and redacted.

## Build from source

With a .NET SDK installed, run `python3 tools/build.py --managed "/path/to/KSP.app/Contents/Resources/Data/Managed" --csc "/path/to/dotnet/sdk/VERSION/Roslyn/bincore/csc.dll"`. On Windows/Linux select the equivalent KSP Data/Managed folder. This uses your own KSP reference assemblies and does not redistribute them. Restart KSP after replacing the DLL.

Tests: `python3 -m unittest discover -s tests -v` runs Basil’s six setup checks. `python3 tests/run_needs_tests.py --runtime "/path/to/dotnet/shared/Microsoft.NETCore.App/VERSION" --csc "/path/to/sdk/Roslyn/bincore/csc.dll"` tests the production need model without Unity. Neither establishes successful in-game interaction.

## Next phase: Luna and authored stories

After the single-player systems, landers and maintenance mission pass testing, integrate Luna with authoritative shared state, then add authored cooperative stories. See [the ordered roadmap](docs/ROADMAP.md). These are future targets; the current package remains single-player with no time warp.

## Onboard operations development candidate

The source checkout also includes [pilot, science, supplies, cryo checks and an
operations journal](docs/ONBOARD-OPERATIONS.md). Use **Ctrl+Alt+O** or OPERATIONS;
engineering uses **Ctrl+Alt+E**. The combined build incorporates upstream 0.3.0
crew needs/interior sources. Compilation and model tests pass; flight checks are
pending. Published release ZIPs do not yet contain these candidate addons.
