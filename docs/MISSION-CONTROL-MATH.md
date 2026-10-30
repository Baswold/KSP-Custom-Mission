# Mission Control navigation kernel

Basil requested an onboard computer for planetary intercepts, atmospheric
periapsis targeting and rendezvous, with reliable node creation and realistic
communications delay. These features are not implemented yet. This first kernel
contains only two independently tested calculations:

- Round-trip light time for a specified outbound and inbound path length, in
  metres, using 299,792,458 m/s. A static direct path has delay 2d/c. Relay path,
  changing geometry, CommNet connection and asynchronous request handling must
  be integrated before treating this as mission-control authorization.
- Ideal signed tangential impulse from a *circular* orbit at its current radius
  to a lower target periapsis, assuming one central body and an instantaneous
  burn. Invalid inputs and periapses at/above the initial orbit are rejected.
  This is a math primitive, not a live orbit detector, node creator or flight plan.

Tests independently reconstruct the target orbit's apsides from energy and
angular momentum. Run with local .NET paths:

```sh
python3 tests/navigation/run.py --runtime /path/to/shared/Microsoft.NETCore.App/VERSION --csc /path/to/sdk/VERSION/Roslyn/bincore/csc.dll
```

Before enabling node creation, require a finite, current orbit and explicit
assumption checks, independently propagate the proposed maneuver using KSP's
patched-conic solver, compare predicted periapsis to a defined tolerance, and
refuse creation if the prediction cannot be verified. Never silently replace an
existing node. Explicitly coordinate plan revision, burn time and the two human
approvals with the upstream shift system; both approvals must be invalidated by
any plan change. Single-player name checks are a rehearsal, not authenticated
multiplayer approval. Include connection loss, delayed responses, vessel change,
SOI transitions, stale plans and save/reload in integration testing.

A planet intercept needs multi-body patched-conic search and independent
propagation, not a circular two-body shortcut. Docking rendezvous needs relative
position and velocity at encounter, collision constraints and approach staging.
Those remain open work. No automatic burns or time warp are introduced here.

## Two-body Lambert prototype

`Lambert.SolveShortWay` computes a zero-revolution, short-way transfer between
3D position vectors in one central-body frame. It returns departure and arrival
velocities; it rejects collinear geometry, invalid inputs and cases where it
cannot bracket a well-conditioned solution. It does not select a launch window,
solve long-way or multi-revolution transfers, model sphere-of-influence crossings
or check collisions. A small internal time residual is not an independent proof
that the transfer is operationally safe.

The numerical tests cover a circular quarter-orbit analytic benchmark and a
noncircular transfer at 300, 1200 and 3000 seconds, verified with independent
20,000-step Runge–Kutta propagation. These are finite examples, not a full
accuracy guarantee. Before use for node creation, add broader geometry/time
coverage and compare the candidate against KSP's actual patched-conic trajectory.

An independent Luna subagent supplied a deterministic randomized propagation
regression: 2,000 short-way cases with nondimensional radii 0.8–1.2 and flight
times from about 0.2 to 10 characteristic periods. Independent fixed-step RK4
propagation reported no position or velocity relative error above 1e-5; the
worst observed was about 1.2e-10. This does not cover SOI transitions,
near-collinear cases or extreme radius ratios and does not validate KSP frames.

The `NodeFrame` helper converts a world delta-v into radial-plus, normal-minus
and prograde components using an orthonormal velocity-based basis. A Luna
subagent checked the signs against [MechJeb's source](https://github.com/MuMech/MechJeb2/blob/dev/MechJeb2/OrbitExtensions.cs)
and tested circular and eccentric vector round-trips. The radial-plus vector
is perpendicular to prograde and is not generally the direct radial vector in
an eccentric orbit. The inputs must share a right-handed inertial frame; KSP's
Orbit coordinate conversions and `ManeuverNode.GetBurnVector` still require
runtime verification before this can safely write nodes.

## Burn-approval guard

`BurnApprovals` is a pure workflow guard for a future trusted identity adapter.
It requires the assigned Hamish and Basil roles to resolve to distinct authenticated
principals, enforces the request's round-trip delay and expiry, clears approvals
when a plan is revised or connection is lost, and checks the live plan fingerprint
again at readiness. The adapter must supply a canonical fingerprint covering the
vessel, node, burn time, vector and target, and must bind role to authenticated
session identity. Calling code must not use a KSP crew name as proof of a human
identity. This class does not authenticate, communicate, persist, create nodes or
execute a burn. It is not connected to the shift system or multiplayer yet.
Single-player rehearsal requires a separately identified rehearsal path and must
never be presented as two authenticated people.

Following Luna review, the guard rejects reused request IDs for the current
session, enforces a trusted configured minimum delay and expires at the exact
window boundary. `PlanFingerprint` hashes vessel, node, body, target, frame,
burn time and all three components. The caller must recompute it from the live
node, observe every mutation and include any additional mission-specific
constraints. IDs and identity canonicalization remain the trusted adapter's
responsibility; the current guard is in-memory and is not a replay-proof
persistent protocol across process restarts.
