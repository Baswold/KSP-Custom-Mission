# Isolated combined source audit — 31 October 2026

31 C# sources compile together against installed KSP 1.12.5 assemblies. No DLL
was installed, no KSP game launched, and no existing save or server changed.
This is an assembly/model compatibility checkpoint, not a gameplay release.

Inputs:
- Pristine published 0.3.0 ZIP: 0071eacf3a1b38a63c611fe2a65877ea2f62f6076b7e97d22e9cdf58b412cf40
- Cumulative 0.3.5 patch: 280c77e0128b2201d875343047f8e4ccdff3e723640d9cf87af3bbc9d4d03851
- Additive epoch planner: 7fc77d8a78da8b81c7f48d35445a72e73ddd8e1eec89e72859869ab06b067ab4
- PR 5 source ref: 2e7d8063bef3b8ff96745df6f4251bf557bf25b7
- PR 8 source ref: 418229a76f82fbd774baf16b09586fabcc826334

Reconstruction: extract the ZIP to a new directory, apply the two patches in
that order, then copy only these seven PR 5 additions into Source:
OnboardEngineering.cs, OnboardOperations.cs, OnboardRendezvous.cs,
OnboardScienceObservation.cs, OperationsModel.cs, RendezvousModel.cs,
ScienceObservationModel.cs. Copy all nine PR 8 Source/Navigation/*.cs files.
Compile all Source/**/*.cs recursively with Roslyn, C# 7.3, against the KSP
Data/Managed DLLs. Do not overwrite host-owned files with branch base versions.
Host-owned source hashes were checked against the current canonical source.

Nine test runners pass on this reconstructed tree: needs, campaign (including
supply adapter and scenario persistence), console input, cryo readiness, epoch
planner, operations trends, rendezvous, science observations, navigation. The
navigation runner passes 2,000 bounded propagation cases, 37 estimator
assertions and 27 watch cases; epoch planner passes 20,100 coverage comparisons.
These use pure models and/or recording KSP boundaries, not real game execution.
Separately, all 22 PR 6 fixture/preflight/snapshot checks pass at
3c1b8a94b53bd5073b3e9d72b27f1e0bb215da9f. No lander flight/release is claimed.

Compiled DLL SHA-256: cec2dc36ea72799da90ffaffe6c6a42ab63142a099bafca9f51699e9d3f7c062

Source-list SHA-256: 998d4896cadfa5792826ac811504951be5faf01442b4813e28124700df790ec0

The source-list digest hashes sorted UTF-8 records of repository-relative path,
NUL, source SHA-256, newline. The local combined manifest retains individual
source, compiler and reference hashes. No credentials/personal paths are in
this public audit.

Remaining gates: live cryopod procedure/power/thermal/failure policy; actual UI
and multi-panel input behavior; stock node/burn comparison; loaded/unpacked
lander and release/redock. PR 5 standalone role checks use roster traits; they
do not yet consult the campaign needs/sleep state, unlike the I panel. Wire a
shared duty-readiness check before claiming human-style needs govern all custom
operations. PR 6 observer still needs the previously requested target-GUID
guard across waits. All draft PRs remain unmerged.
