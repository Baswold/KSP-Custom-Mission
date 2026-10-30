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
