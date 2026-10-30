# Routine story: The Burn Brief

**Category:** Routine flight operations and crew care. This is not the campaign emergency.

**Location and crew:** Hermes command station and quarters. Hamish and Basil are awake on the human watch; Ada and Vikram are in their actual DeepFreeze pods. Implementation target: read the active vessel, orbit, node, resources and crew state from KSP. This is a proposed scene; the pure math/coverage helpers do not make it playable yet.

**Starting problem:** Mission Control returns a routine trajectory update after the documented communication delay. The suggested burn is usable, but its alignment, execution and verification window must fit the current watch and both reviewers' available intervals. One reviewer is tired and wants to accept quickly so the crew can return to the observation log; the other asks for a second look at the predicted periapsis and propellant margin.

**Sequence:**

1. Read current orbit, node UT and delta-v, vessel fuel/EC, crew fatigue and the mission-control message/request age.
2. Compare the full pre-alignment + burn + post-verification interval with the assigned awake watch and each reviewer's awake/fit interval. The planner never moves burn UT or pauses the shift clock to make it fit.
3. If the plan is valid and both reviewers are fit, Hamish verifies the target/orbit and Basil verifies the vector/propellant budget. Each records approval against the same unchanged plan revision. In single-player this is rehearsal only; only authenticated distinct clients can establish two-human approval in LMP.
4. If timing, communications, identity, freshness or fit checks fail, hold the node, record why, rest/reassess or request a revised plan. Routine work continues while awaiting a new message; the crew may complete the observation log or check supplies.
5. Execute only after current node, approvals and interval checks still match. Observe the actual post-burn orbit and fuel change, then record the result for handover.

**Social choice:** Hamish can ask to take the observation task while Basil rechecks the arithmetic, or they can pause together for a short discussion about why rushing is tempting. Both paths preserve the same safety gates; they change the debrief tone and workload assignment, not orbital truth.

**Success evidence:** actual KSP node and plan revision match both approvals; the full finite operation interval is covered; communication and reviewer status are current; the resulting orbit/fuel are observed and logged. A rejected or expired plan has a clear recovery path. No synthetic failure or extra emergency is introduced.


**Dialogue draft:**

Basil: “The node is a point on the clock. We are not. Show me the whole burn.”

Hamish: “And the time either side of it. If we only budget the engine, we have
forgotten the people steering it.”

If they defer, the handover note records a decision rather than a failure:
“Plan held for a new window. Orbit unchanged. No propellant expended.” The game
must verify those facts instead of granting success from the dialogue alone.

**Implementation gaps:** communications request lifecycle, live node prediction,
measured engine performance, fit/watch adapter, two authenticated clients,
actual burn/post-burn observation and saved dialogue outcomes. No automatic burn,
animated handoff or completed multiplayer feature is implied by this draft.
