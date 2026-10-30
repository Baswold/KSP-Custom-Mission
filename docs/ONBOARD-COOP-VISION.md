# Onboard cooperative mission: proposed integration target

Basil's requested experience is a crew inhabiting one ship together: walk to a
station to operate it, perform useful work in distinct roles, go outside for
repairs, and use cryosleep to bridge long passages. Routine operation should not
require map view, external part menus or leaving the ship's perspective.

This is a proposal for coordination, not a claim about implemented features. The
current README describes a single-player orbital trainer with external part-menu
steps, manually signed-off traversal and disabled time warp. Hami's Codex retains
ownership of the initial source and practice implementation. Agree integration
boundaries in issue #1 before changing those systems.

## First shared mission: restore a cooling loop before the next burn

1. The cockpit reports a slowly worsening cooling fault with a clear warning and
   time to respond. The pilot holds a safe attitude and postpones the burn.
2. The engineer uses an interior panel to identify the faulty loop, isolate it and
   route power to the healthy circuit. A diagnostic identifies whether an EVA is
   actually needed; faults should not all reduce to the same repair button.
3. The scientist suspends a heat-sensitive experiment, protects the collected data
   and monitors temperatures. This buys repair time at the cost of delaying the
   experiment; science is a meaningful mission responsibility.
4. For an external fault, one crewmember carries a finite repair kit to the failed
   component while another monitors power and provides the checklist inside.
5. The engineer pressure-tests the repair. The scientist verifies temperatures,
   and the pilot resumes the burn using cockpit navigation information.
6. After a successful systems check, the crew selects a wake event and agrees to
   cryosleep. Everyone returns to the same ship state before the next operation.

Provide a quiet teaching version first. Later variation can include a stuck array,
sensor disagreement or a leaking coolant branch. Give faults readable causes,
useful warning time and recovery options. Avoid random instant losses and endless
maintenance chores. Alternate calm travel, preparation, discovery and occasional
multi-role emergencies. Support two players by allowing station handover; do not
require three simultaneous button presses.

## Technical gates before this can be called co-op

| Area | Acceptance target |
| --- | --- |
| Shared crew | Two clients see distinct crewmembers in the same vessel and consistent occupied seats/interior positions |
| Station ownership | One pilot authority; explicit transfer; engineer/science commands cannot fight over flight input |
| Shared systems | One authoritative fault/resource/objective state, versioned messages and duplicate-command protection |
| Join/reconnect | A reconnecting client receives a complete snapshot, including fault stage, resources and crew state |
| Onboard operation | Deploy equipment, diagnose systems, run science and navigate from interior controls; document any remaining external-menu dependency |
| Repairs | Both clients observe identical failure and repair outcomes; spare parts consumed exactly once |
| Cryosleep | Agreed wake target, consent from participating players, safe-flight checks and a defined disconnect/abort policy |
| Time progression | Explicitly choose and test an LMP-compatible shared clock/warp policy; freezing a Kerbal alone does not accelerate travel |
| Save/reload | Authority, pending repairs, crew and wake schedule survive reload consistently |

Begin with a shared engineering switch and one deterministic fault before adding
random events. Prove a command travels from the non-pilot client to the authority
and returns as the same state on both machines. Then connect actual effects,
interior controls and finally cryosleep progression. A single-player console must
not be presented as network synchronization.

## Candidate work boundaries to agree

- Hami's Codex: finish uploading and validating the existing trainer package.
- Basil's Codex: Mac setup/preflight and evidence reporting (this contribution).
- Next agreed implementation: define command/state contracts for one engineering
  action, with an explicit owner for the LMP adapter and a two-client test plan.
- Later: station UI, FreeIVA presence/interaction, science activities, faults/EVA
  repair and shared cryosleep. Confirm upstream APIs and licenses before adapting
  or redistributing any mod; keep required versions pinned.
