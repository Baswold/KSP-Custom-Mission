# Mission logbook

The Flight-only logbook is available in save folders whose names begin with
`MegaCrew`. Open it with **Ctrl+Alt+L** or the **LOGBOOK** button. It reads
entries from the current game's `MissionLogScenario`; entries from earlier
vessels in that save remain visible.

The panel does not add a scenario when opened. The first accepted capture adds
the logbook scenario to the current game, and the public Flight scenario runner
loads it before the entry is appended. If native registration is unavailable,
the panel reports that state and does not append. The data is persisted only
when the player uses KSP's ordinary save or quicksave.

Before capture, the panel requires a loaded, unpacked, non-EVA active vessel,
an unpaused 1x Flight, and an awake author aboard that vessel. The author is a
locally selected crew label, not an authenticated player identity. The live
crew-duty API is checked again immediately before append. A changed save,
game, vessel, body, author, time state, invalid measurement, or failed scenario
validation refuses the entry.

Each button samples current values at click time:

- **Orbit** stores the current UT, vessel and body identity, periapsis altitude,
  apoapsis altitude, and orbital period.
- **Supplies** stores current and capacity totals for ElectricCharge, Food,
  Water, and Glykerol. It lists flow-enabled stores separately from all stores;
  it does not estimate rates or future depletion.
- **Retained science** reads stored science cards through `GetData`, deduplicates
  repeated references to the same card, and reports card counts, subject IDs,
  titles, and retained data units. This does not establish experiment
  execution, discovery, or science rewards.
- **Add note** stores the author's text with the current vessel and body
  context.

Observation text is limited to 8192 characters and notes to 1024. Oversized or
invalid text is refused without truncation. The logbook retains at most 256
entries and shows newest entries first. It does not autosave, write external
files, or synchronize through Luna Multiplayer. Images and other media are not
attached; future image binding requires a separate validated storage and
reference design.

This panel has compile and model-level qualification only. It has not been
qualified in a running KSP session.
