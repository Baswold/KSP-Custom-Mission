# Finch lander and Hermes external cradle

Development candidates for the single-player foundation, following the priority
in docs/ROADMAP.md. These craft files do not replace any save or the existing
Hermes trainer. Neither craft includes a launch booster.

## Delivered hardware

- `Ships/VAB/Hermes Finch Vacuum Lander.craft`: 17 stock parts, two-seat Mk2 lander
  cabin, full Rockomax X200-8 LF/O tank, Terrier, four LT-2 legs, four RCS blocks,
  40 units of cabin monopropellant, four fixed solar panels, supplemental battery
  and standard Clamp-O-Tron docking port. The cabin's Lander variant is selected.
- `Ships/VAB/Hermes Finch Docking Cradle.craft`: existing Hermes walkthrough
  assembly with one external side port and the Finch attached port-to-port.
  This is an open cradle, not a shielded cargo bay. The interior route is retained.

Use `tools/build_landers.py --ksp-root '/path/to/KSP'` to regenerate from local
stock part definitions. No stock assets are redistributed. The generator uses
stock node positions and serializes resources; prefab defaults supply modules.
The cradle uses existing SSPX/DeepFreeze dependencies from the trainer.

## Mission envelope and provisional budget

The target is a sortie from a mothership **already in low Mun or Minmus orbit**,
landing and returning to rendezvous. The 150-km Kerbin trainer is a deployment
practice site, not a lunar transfer solution. No atmospheric entry, Duna landing,
Kerbin return or ascent from Kerbin is qualified by this design.

The ideal design calculation includes two 0.09375-t crew, full cabin monopropellant
and four tonnes of LF/O. It estimates 7.3425 t wet mass, about 2,663 m/s ideal vacuum
delta-v at 345 s Isp, and initial Mun TWR about 5.0. These are design calculations;
actual KSP mass/module behavior and flight losses need separate validation.

A provisional Mun sortie allocation is 650 m/s descent, 650 m/s ascent,
200 m/s rendezvous/correction and 300 m/s retained reserve: 1,800 m/s total.
This is a planning allocation, not a proven trajectory or guarantee. Hovering,
terrain, landing site and inclination can exceed it. Low Minmus orbit needs less
propulsion but still needs a tested ascent/rendezvous plan. Preserve monopropellant
for docking and keep adequate battery reserves through eclipse.

## Storage and deployment procedure to qualify

1. Load the cradle in the editor. Keep the centrifuge stopped. Check full geometric
   clearance around the cabin hatch, docking rings, legs, arrays and radiators.
2. In a disposable orbit fixture, verify the two docking nodes are connected and
   the lander engine is off. Transfer two crew by stock crew transfer if available;
   otherwise use an EVA transfer. No new FreeIVA passage is claimed through the port.
3. Select the lander port's control reference, separate using its docking-node
   release, and translate clear with RCS. Confirm the carrier remains intact.
4. Keep the engine off until at least 50 m clear and orient away from the carrier
   before firing. This distance is a proposed test procedure, not measured plume
   clearance. No automatic undocking burn is installed.
5. Deploy legs, demonstrate a surface touchdown without engine/terrain contact,
   verify both crew can EVA/reboard, then ascend with the planned reserve.
6. Return with engines off near Hermes and redock the same standard ports. Check
   state/crew/resources across save/reload, then retransfer crew.

## Evidence and outstanding gates

Nine Python tests pass, including craft graph connectivity/acyclicity, unique IDs,
finite transforms, normalized rotations, opposed/coincident docking faces, four
legs/RCS blocks and explicit fuel resources. These checks are structural and do
not prove collider clearance, leg deployment, flight stability or docking behavior.
`docs/Finch-Design.json` records the design figures and runtime status.

Native KSP loading is checked with a temporary disposable probe, separate from the
production mod. Results will be recorded here after observation. Flight load,
storage/release clearance, landing, EVA access, re-ascent, redocking and reload
persistence must all pass before these candidates can be called usable landers.

### Loader-probe history

The first native loader attempt rejected both crafts because generated stock part
names containing underscores were not encoded as KSP's period-separated names.
The generator and a regression test now enforce this format. A second attempt
produced no result while using Unity's game-time wait at the main menu. The test
probe now uses a real-time wait and explicit begin markers, and is being rerun.
The temporary `tests/ksp/LanderLoaderProbe.cs` is test-only and must not be placed
in a normal installation: it instantiates and destroys both craft in the main menu.
It is not included in the production MegaCrew DLL.

### Native loader result

The corrected files were loaded with `ShipConstruction.LoadShip` in the separate
KSP 1.12.5 test copy. The Finch loader returned 17 parts, two seats, one docking
module and one engine; the loader reported 6.967 t with resources and no crew.
The cradle returned 35 parts, 32 total seats, two docking modules and two engines.
The Finch's loaded root was the Mk2 lander cabin. The probe did not report a root
for the cradle, so its actual in-game attachment hierarchy needs investigation;
the Python graph test is not sufficient proof of this. KSP logged a module-index
mismatch before finding the cabin's variant module, plus the existing SSPX
adapter module mismatch. The test ran at the main menu, outside a real editor
or flight scene, and generated unrelated main-menu module-start exceptions.

The measured loaded mass is not the fully crewed design estimate; resolve the
remaining difference against the stock editor and module/crew mass before relying
on the ideal delta-v figure. Loading does not establish docking-node state, collider
clearance, working legs, staging, EVA access, propulsion or landing/recovery.
