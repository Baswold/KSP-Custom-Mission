# One-shot real-KSP cryopod QA — pending runtime execution

**Integration status:** this historical full freeze/thaw probe still uses the
direct flight-start shortcut. Actual QA found that shortcut skips KSP's native
scenario initialization and can leave DeepFreeze unready. Do not use a readiness
timeout from this launcher to diagnose a pod defect. Its native load/reload
path needs the same correction documented in
[the direct refusal QA guide](CRYO-REFUSAL-QA.md) before this full procedure is
qualified. The completed direct refusal observation is separate evidence and
does not complete the freeze/thaw or manual UI gates.

This probe belongs only in a fresh, marked `KSP-Hermes-QA` copy. Never put its
DLL in the play copy. The helper refuses existing/nested destinations and does
not copy personal saves, logs or server configuration. On macOS it uses APFS
copy-on-write cloning for KSP.app and GameData. Only the exact published 0.3.0
archive provides the pristine test save; original saves are never read/copied.

1. Stage the hash-pinned provider using `tools/fix_background_cryo.py`, retaining
   its original. No runtime pass is implied by this staged candidate.
2. Prepare a fresh directory named KSP-Hermes-QA:

```
python3 tools/prepare_cryo_qa.py --source-root /path/to/separate/MegaCrew --qa-root /path/to/KSP-Hermes-QA --pristine-archive Hermes-Practice-v0.3.0.zip --provider-candidate /path/to/staged/BackgroundResources.dll
python3 tools/build_cryo_qa.py --ksp-root /path/to/KSP-Hermes-QA --csc /path/to/sdk/Roslyn/bincore/csc.dll
```

3. With no other KSP running and when desktop testing is permitted, launch the
   QA copy's KSP.app. The marked test starts once at the main menu. The probe
   validates root/save/hash boundaries and uses actual KSP crew transfer and
   DeepFreeze freeze/thaw calls. It never fabricates frozen dictionary entries,
   sets UT, or injects EC. The source save starts with the original trainer,
   actual battery/Glykerol supplies and stowed solar arrays.
4. It observes the genuinely unloaded vessel in the tracking station for 45 real
   seconds at 1x, recording actual UT, battery delta and pod ledger timestamps.
   It then saves/reloads, verifies frozen persistence, deploys real solar arrays
   and waits for real recharge before thawing Ada. Eclipse/recharge can extend
   the run to about 40 minutes. Timeout/failure is recorded, never treated as a
   pass. The addon closes only the explicitly marked disposable KSP app.
5. Read `HermesCryoQA-result.cfg` in the QA root and focused `[HermesCryoQA]` log
   lines. The one-shot marker moves enable -> running -> done. An incomplete
   marker/result needs inspection; do not blindly rerun or replace the copy.

The probe is compiled as GameData/HermesQA/HermesCryoQA.dll, separate from the
normal plugin. `probe_completed=true` means the single-pod procedure finished;
`flight_qualification=false` remains explicit. Corrected net EC draw, genuine
freeze/reload/thaw and all recorded amounts still require review. No multiple-pod,
settings-off, depletion/recharge failure-policy, high-warp or thermal pass is
claimed. Compare original/patched providers only in separately fresh QA copies.

Current host evidence: probe compiles against the actual KSP 1.12.5, DeepFreeze,
BackgroundResources and MegaCrew APIs; preparation/guard tests pass. The marked
QA copy and patched provider are prepared. Launch was blocked by the locked Mac;
no runtime report exists yet. Play-copy provider and original saves are unchanged.

## Guard/report follow-up

Apply Hermes-Cryo-QA-guards.patch after Hermes-Cryo-QA-probe.patch. The probe
now refuses old result/running/done markers, writes incomplete/nonqualified
state before game actions, records progress stages, and aborts on save context,
active-vessel, pause or rate changes. Both flight loads await loaded/unpacked
state. The one-shot done marker means the attempt ended, including failures.

Read the report without launching or modifying KSP:

```
python3 tools/inspect_cryo_qa.py /path/to/KSP-Hermes-QA
```

Exit 0 means internally consistent single-pod procedural evidence, never
flight qualification. Exit 2 means incomplete (including no report). Exit 1
means failed or invalid. The reader rejects duplicate keys, contradictory
markers, missing actual-state evidence, nonfinite intervals and inconsistent
battery deltas. It does not substitute for review of raw measurements/logs or
validate high-rate/thermal/failure-policy behavior. Its unit-test complete
fixtures are synthetic and are not live gameplay evidence. Current host output
is incomplete: no runtime report exists. The 20 Python checks and updated probe
compile pass; no live run is claimed.

The report reader accepts the bare top-level values emitted by ConfigNode.Save
(as confirmed in an existing host-generated mission CFG), and an explicit
HERMES_CRYO_QA wrapper. Both forms share duplicate/nesting checks.
