using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace MegaCrew.Logbook
{
    /// <summary>Flight-only, local textual logbook backed by the current game's scenario state.</summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public sealed class MissionLogPanel : MonoBehaviour
    {
        const string LockId = "MegaCrew.MissionLogPanel";
        const int WindowId = 719326;
        const int MaxObservationChars = MissionLogBook.MaxObservationChars;

        struct CaptureContext
        {
            public Game Game;
            public string Save;
            public Vessel Vessel;
            public Guid VesselId;
            public CelestialBody Body;
            public double UT;
            public string Author;
        }

        sealed class ScienceReferenceComparer : IEqualityComparer<ScienceData>
        {
            public bool Equals(ScienceData left, ScienceData right)
            {
                return System.Object.ReferenceEquals(left, right);
            }

            public int GetHashCode(ScienceData value)
            {
                return RuntimeHelpers.GetHashCode(value);
            }
        }

        sealed class ResourceTotals
        {
            public double Amount;
            public double Capacity;
            public double FlowAmount;
            public double FlowCapacity;
        }

        static readonly IEqualityComparer<ScienceData> ScienceComparer = new ScienceReferenceComparer();
        static readonly string[] ResourceNames = { "ElectricCharge", "Food", "Water", "Glykerol" };

        Rect window = new Rect(80f, 70f, 620f, 500f);
        Vector2 entriesScroll;
        Vector2 controlsScroll;
        Vector2 authorsScroll;
        bool visible;
        bool appFocused = true;
        bool noteFocused;
        bool clearOwnedFocusPending;
        Guid contextVesselId;
        Game contextGame;
        string contextSave = "";
        string selectedAuthor = "";
        string noteText = "";
        string status = "Open the logbook to review this save or capture a new observation.";

        bool Allowed
        {
            get
            {
                return HighLogic.LoadedScene == GameScenes.FLIGHT && HighLogic.CurrentGame != null &&
                    (HighLogic.SaveFolder ?? "").StartsWith("MegaCrew", StringComparison.OrdinalIgnoreCase);
            }
        }

        void Update()
        {
            if (!Allowed)
            {
                visible = false;
                selectedAuthor = "";
                contextVesselId = Guid.Empty;
                contextGame = null;
                contextSave = "";
                noteText = "";
                if (noteFocused) clearOwnedFocusPending = true;
                ReleaseInputLock();
                return;
            }

            Vessel vessel = FlightGlobals.ActiveVessel;
            string save = HighLogic.SaveFolder ?? "";
            Game game = HighLogic.CurrentGame;
            Guid id = vessel == null ? Guid.Empty : vessel.id;
            if (contextVesselId != id || !System.Object.ReferenceEquals(contextGame, game) || contextSave != save)
            {
                contextVesselId = id;
                contextGame = game;
                contextSave = save;
                selectedAuthor = "";
                noteText = "";
                clearOwnedFocusPending = true;
                status = "Save or active vessel changed. Select an awake author for this vessel.";
            }

            if (!noteFocused && Input.GetKeyDown(KeyCode.L) &&
                (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) &&
                (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)))
            {
                visible = !visible;
                if (!visible)
                {
                    if (noteFocused) clearOwnedFocusPending = true;
                    ReleaseInputLock();
                }
            }
            if (!visible) ReleaseInputLock();
        }

        void OnGUI()
        {
            if (!Allowed)
            {
                ClearOwnedFocusInOnGUI();
                ReleaseInputLock();
                return;
            }

            GUISkin previousSkin = GUI.skin;
            bool previousEnabled = GUI.enabled;
            try
            {
                GUI.skin = HighLogic.Skin;
                GUI.enabled = true;
                Rect button = new Rect(Mathf.Max(4f, Screen.width - 92f), Screen.height - 95f, 88f, 30f);
                if (GUI.Button(button, "LOGBOOK"))
                {
                    visible = !visible;
                    if (!visible) ClosePanel();
                }

                ClearOwnedFocusInOnGUI();

                if (!visible)
                {
                    ReleaseInputLock();
                    return;
                }

                window.width = Mathf.Min(620f, Mathf.Max(1f, Screen.width - 20f));
                window.height = Mathf.Min(560f, Mathf.Max(1f, Screen.height - 20f));
                window.x = Mathf.Clamp(window.x, 0f, Mathf.Max(0f, Screen.width - window.width));
                window.y = Mathf.Clamp(window.y, 0f, Mathf.Max(0f, Screen.height - window.height));
                window = GUI.Window(WindowId, window, DrawWindow, "MEGACREW / MISSION LOGBOOK");
                ClearOwnedFocusInOnGUI();
                if (!visible)
                {
                    ReleaseInputLock();
                    return;
                }

                Vector2 mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                if (appFocused && (window.Contains(mouse) || noteFocused))
                    InputLockManager.SetControlLock(ControlTypes.ALL_SHIP_CONTROLS, LockId);
                else
                    ReleaseInputLock();
            }
            finally
            {
                GUI.skin = previousSkin;
                GUI.enabled = previousEnabled;
            }
        }

        void DrawWindow(int id)
        {
            bool previousEnabled = GUI.enabled;
            try
            {
                ClearOwnedFocusInOnGUI();
                Rect closeRect = new Rect(Mathf.Max(4f, window.width - 76f), 2f, 68f, 20f);
                if (GUI.Button(closeRect, "Close")) ClosePanel();
                if (!visible) return;

                Vessel vessel = FlightGlobals.ActiveVessel;
                CaptureContext context;
                string captureReason;
                bool captureReady = TryGetContext(selectedAuthor, out context, out captureReason);
                bool hasAuthor = !String.IsNullOrWhiteSpace(selectedAuthor);
                float contentWidth = Mathf.Max(1f, window.width - 16f);
                float controlsY = 26f;
                float controlsHeight = Mathf.Min(170f, Mathf.Max(1f, window.height - 150f));
                float entriesY = controlsY + controlsHeight + 5f;
                float entriesHeight = Mathf.Max(1f, window.height - entriesY - 8f);

                GUILayout.BeginArea(new Rect(8f, controlsY, contentWidth, controlsHeight));
                try
                {
                    controlsScroll = GUILayout.BeginScrollView(controlsScroll,
                        GUILayout.Width(contentWidth), GUILayout.Height(controlsHeight));
                    try
                    {
                        GUILayout.Label("Scenario log / local to this KSP save");
                        GUILayout.Label(ContextSummary(vessel), WrapStyle());
                        DrawAuthorPicker(vessel);

                        GUILayout.BeginHorizontal();
                        try
                        {
                            GUI.enabled = captureReady && hasAuthor;
                            if (GUILayout.Button("Capture orbit", GUILayout.Height(32f))) Capture("Orbit");
                            if (GUILayout.Button("Capture supplies", GUILayout.Height(32f))) Capture("Supplies");
                            if (GUILayout.Button("Capture retained science", GUILayout.Height(32f))) Capture("RetainedScience");
                        }
                        finally
                        {
                            GUI.enabled = true;
                            GUILayout.EndHorizontal();
                        }

                        GUILayout.BeginHorizontal();
                        try
                        {
                            GUI.SetNextControlName("MegaCrewMissionLogNote");
                            noteText = GUILayout.TextArea(noteText ?? "", MissionLogBook.MaxNoteChars,
                                GUILayout.Height(42f), GUILayout.ExpandWidth(true));
                            UpdateOwnedFocusFromOnGUI();
                            GUI.enabled = captureReady && hasAuthor && !String.IsNullOrWhiteSpace(noteText);
                            if (GUILayout.Button("Add note", GUILayout.Width(92f), GUILayout.Height(42f))) Capture("Note");
                        }
                        finally
                        {
                            GUI.enabled = true;
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.Label(!captureReady ? captureReason : status, WrapStyle());
                    }
                    finally { GUILayout.EndScrollView(); }
                }
                finally { GUILayout.EndArea(); }

                GUILayout.BeginArea(new Rect(8f, entriesY, contentWidth, entriesHeight));
                try
                {
                    entriesScroll = GUILayout.BeginScrollView(entriesScroll,
                        GUILayout.Width(contentWidth), GUILayout.Height(entriesHeight));
                    try { DrawEntries(); }
                    finally { GUILayout.EndScrollView(); }
                }
                finally { GUILayout.EndArea(); }

                GUI.DragWindow(new Rect(0f, 0f, Mathf.Max(1f, window.width - 80f), 22f));
            }
            finally { GUI.enabled = previousEnabled; }
        }

        void DrawAuthorPicker(Vessel vessel)
        {
            List<string> authors = AwakeAuthors(vessel, out string reason);
            GUILayout.Label("Author (a locally selected awake crew label; not identity verification)");
            if (authors.Count == 0)
            {
                selectedAuthor = "";
                GUILayout.Label(String.IsNullOrEmpty(reason) ? "No verified awake crew are aboard this vessel." : reason);
                return;
            }

            if (!authors.Contains(selectedAuthor)) selectedAuthor = "";
            authorsScroll = GUILayout.BeginScrollView(authorsScroll, true, false, GUILayout.Height(34f));
            try
            {
                GUILayout.BeginHorizontal();
                try
                {
                    foreach (string name in authors)
                    {
                        string label = name == selectedAuthor ? "Selected: " + name : name;
                        if (GUILayout.Button(label, GUILayout.MinWidth(110f), GUILayout.Height(28f)))
                        {
                            if (vessel == FlightGlobals.ActiveVessel && Allowed) selectedAuthor = name;
                            else status = "Author selection refused: the active mission vessel changed.";
                        }
                    }
                }
                finally { GUILayout.EndHorizontal(); }
            }
            finally { GUILayout.EndScrollView(); }
        }

        List<string> AwakeAuthors(Vessel vessel, out string reason)
        {
            var result = new List<string>();
            reason = "";
            if (vessel == null || vessel != FlightGlobals.ActiveVessel || vessel.isEVA || !vessel.loaded || vessel.packed)
            {
                reason = "Author selection requires a loaded, unpacked mission vessel.";
                return result;
            }

            try
            {
                foreach (ProtoCrewMember member in vessel.GetVesselCrew())
                {
                    if (member == null || String.IsNullOrWhiteSpace(member.name)) continue;
                    bool frozen;
                    if (!CryoBridge.TryFrozen(member.name, out frozen)) continue;
                    if (!frozen && !result.Contains(member.name)) result.Add(member.name);
                }
                result.Sort(StringComparer.Ordinal);
            }
            catch (Exception)
            {
                result.Clear();
                reason = "Awake crew could not be verified from the current vessel.";
            }
            return result;
        }

        string ContextSummary(Vessel vessel)
        {
            if (vessel == null || vessel.mainBody == null) return "Active vessel or body unavailable.";
            return (vessel.vesselName ?? "Unnamed vessel") + " | " + vessel.mainBody.bodyName +
                " | " + vessel.id.ToString("D");
        }

        bool TryGetContext(string author, out CaptureContext context, out string reason)
        {
            context = new CaptureContext();
            reason = "";
            if (!Allowed || HighLogic.CurrentGame == null)
                reason = "Open a MegaCrew flight save to use the logbook.";
            else if (!System.Object.ReferenceEquals(contextGame, HighLogic.CurrentGame) ||
                contextSave != (HighLogic.SaveFolder ?? "") || FlightGlobals.ActiveVessel == null ||
                contextVesselId != FlightGlobals.ActiveVessel.id)
                reason = "Save, game, or active vessel changed; wait for the panel to refresh and select an awake author again.";
            else if (!FlightGlobals.ready || FlightGlobals.ActiveVessel == null)
                reason = "Wait for the active vessel to finish loading.";
            else if (FlightDriver.Pause)
                reason = "Captures require an unpaused flight.";
            else if (TimeWarp.CurrentRateIndex != 0 || TimeWarp.CurrentRate != 1f)
                reason = "Captures require 1x time.";
            else if (FlightGlobals.ActiveVessel.isEVA || !FlightGlobals.ActiveVessel.loaded || FlightGlobals.ActiveVessel.packed)
                reason = "Captures require a loaded, unpacked non-EVA vessel.";
            else if (FlightGlobals.ActiveVessel.mainBody == null)
                reason = "Active vessel body is unavailable.";
            else if (String.IsNullOrWhiteSpace(author))
                reason = "Select an awake author before capturing.";
            else
            {
                double now;
                try { now = Planetarium.GetUniversalTime(); }
                catch (Exception) { now = Double.NaN; }
                if (!Finite(now) || now < 0.0) reason = "Current universal time is invalid.";
                else
                {
                    context.Game = HighLogic.CurrentGame;
                    context.Save = HighLogic.SaveFolder ?? "";
                    context.Vessel = FlightGlobals.ActiveVessel;
                    context.VesselId = context.Vessel.id;
                    context.Body = context.Vessel.mainBody;
                    context.UT = now;
                    context.Author = author;
                }
            }
            return reason.Length == 0;
        }

        bool ContextStillCurrent(CaptureContext context, out string reason)
        {
            reason = "";
            if (!Allowed || !System.Object.ReferenceEquals(HighLogic.CurrentGame, context.Game) ||
                (HighLogic.SaveFolder ?? "") != context.Save || FlightGlobals.ActiveVessel != context.Vessel ||
                context.Vessel == null || context.Vessel.id != context.VesselId || context.Vessel.mainBody != context.Body)
                reason = "Save, game, vessel, or body changed during capture.";
            else if (!FlightGlobals.ready || !context.Vessel.loaded || context.Vessel.packed || context.Vessel.isEVA)
                reason = "Active mission vessel is no longer loaded and unpacked.";
            else if (FlightDriver.Pause || TimeWarp.CurrentRateIndex != 0 || TimeWarp.CurrentRate != 1f)
                reason = "Flight paused or left 1x during capture.";
            else if (selectedAuthor != context.Author)
                reason = "Selected author changed during capture.";
            else
            {
                double now;
                try { now = Planetarium.GetUniversalTime(); }
                catch (Exception) { now = Double.NaN; }
                if (!Finite(now) || now < context.UT) reason = "Universal time became invalid or moved backwards during capture.";
            }
            return reason.Length == 0;
        }

        void Capture(string kind)
        {
            CaptureContext context;
            string reason;
            if (!TryGetContext(selectedAuthor, out context, out reason))
            {
                status = "Capture refused: " + reason;
                return;
            }

            string observation;
            try
            {
                if (kind == "Orbit") observation = CaptureOrbit(context);
                else if (kind == "Supplies") observation = CaptureSupplies(context);
                else if (kind == "RetainedScience") observation = CaptureScience(context);
                else if (kind == "Note") observation = "User-authored note.";
                else throw new InvalidOperationException("Unknown capture kind.");
            }
            catch (Exception exception)
            {
                status = "Capture unavailable: " + SafeMessage(exception);
                return;
            }

            if (observation.Length > MissionLogBook.MaxObservationChars ||
                !MissionLogBook.ValidText(observation, MissionLogBook.MaxObservationChars, true))
            {
                status = "Capture refused: complete observation exceeds the logbook text limit or contains invalid text.";
                return;
            }

            string note = noteText ?? "";
            var entry = new MissionLogEntry(Guid.NewGuid(), context.UT, context.VesselId,
                context.Vessel.vesselName ?? "Unnamed vessel", context.Body.bodyName ?? "Unknown body",
                context.Author, kind, observation, note);
            var validation = new MissionLogBook();
            if (!validation.TryAppend(entry, out reason))
            {
                status = "Capture refused: " + reason;
                return;
            }

            if (!ContextStillCurrent(context, out reason))
            {
                status = "Capture refused: " + reason;
                return;
            }
            if (!DutyReadinessBridge.CanPerformDuty(context.Author, context.VesselId, out reason))
            {
                status = "Capture refused: " + reason;
                return;
            }
            if (!ContextStillCurrent(context, out reason))
            {
                status = "Capture refused: " + reason;
                return;
            }

            MissionLogScenario scenario;
            ProtoScenarioModule proto;
            if (!FindScenario(context.Game, out scenario, out proto, out reason))
            {
                status = "Logbook scenario unavailable: " + reason;
                return;
            }

            if (scenario == null && proto == null)
            {
                // First accepted capture creates only this save's scenario data.
                // No scenario is added merely by opening the panel.
                try
                {
                    proto = context.Game.AddProtoScenarioModule(typeof(MissionLogScenario),
                        GameScenes.FLIGHT, GameScenes.SPACECENTER, GameScenes.TRACKSTATION);
                }
                catch (Exception exception)
                {
                    status = "Logbook scenario registration failed; no entry was appended: " + SafeMessage(exception);
                    return;
                }
                if (proto == null)
                {
                    status = "Logbook scenario could not be registered; no entry was appended.";
                    return;
                }
            }

            if (proto != null && proto.moduleRef == null)
            {
                if (!ContextStillCurrent(context, out reason))
                {
                    status = "Logbook scenario is pending; context changed before registration.";
                    return;
                }
                if (!DutyReadinessBridge.CanPerformDuty(context.Author, context.VesselId, out reason))
                {
                    status = "Logbook scenario is pending; capture refused: " + reason;
                    return;
                }
                ScenarioRunner runner = ScenarioRunner.Instance;
                if (runner == null)
                {
                    status = "Logbook scenario is pending native registration. Wait for the Flight scenario runner and retry.";
                    return;
                }
                try { proto.Load(runner); }
                catch (Exception exception)
                {
                    status = "Logbook scenario registration failed; no entry was appended: " + SafeMessage(exception);
                    return;
                }
            }

            if (!FindScenario(context.Game, out scenario, out proto, out reason) || scenario == null)
            {
                status = "Logbook scenario is not ready; retry after native registration. " + reason;
                return;
            }
            if (!String.IsNullOrEmpty(scenario.LoadError))
            {
                status = "Logbook is read-only because saved data failed validation: " + scenario.LoadError;
                return;
            }
            if (!ContextStillCurrent(context, out reason))
            {
                status = "Capture refused: " + reason;
                return;
            }
            if (!DutyReadinessBridge.CanPerformDuty(context.Author, context.VesselId, out reason))
            {
                status = "Capture refused: " + reason;
                return;
            }
            if (!ContextStillCurrent(context, out reason))
            {
                status = "Capture refused: " + reason;
                return;
            }

            try
            {
                if (scenario.TryAppend(entry, out reason))
                {
                    noteText = "";
                    status = "Observation appended to this game's scenario data. Save normally to persist it.";
                }
                else status = "Logbook refused the entry: " + reason;
            }
            catch (Exception exception)
            {
                status = "Logbook append failed: " + SafeMessage(exception);
            }
        }

        bool FindScenario(Game game, out MissionLogScenario scenario, out ProtoScenarioModule selected,
            out string reason)
        {
            scenario = null;
            selected = null;
            reason = "";
            if (game == null || game.scenarios == null)
            {
                reason = "Current game's scenario list is unavailable.";
                return false;
            }

            int matches = 0;
            foreach (ProtoScenarioModule candidate in game.scenarios)
            {
                if (candidate == null || candidate.moduleName != typeof(MissionLogScenario).Name) continue;
                matches++;
                selected = candidate;
            }
            if (matches > 1)
            {
                reason = "Duplicate logbook scenarios found; no append attempted.";
                return false;
            }
            if (matches == 0) return true;
            if (selected.moduleRef == null)
            {
                reason = "Logbook scenario has not completed native registration.";
                return true;
            }
            scenario = selected.moduleRef as MissionLogScenario;
            if (scenario == null)
            {
                reason = "Scenario name matched, but its module reference has the wrong type.";
                return false;
            }
            return true;
        }

        string CaptureOrbit(CaptureContext context)
        {
            Orbit orbit = context.Vessel.orbit;
            if (orbit == null) throw new InvalidOperationException("Active vessel orbit is unavailable.");
            double pe = orbit.PeA;
            double ap = orbit.ApA;
            double period = orbit.period;
            if (!Finite(pe) || !Finite(ap) || !Finite(period) || period <= 0.0)
                throw new InvalidOperationException("Orbit periapsis, apoapsis, or period is not finite.");
            return "Orbit observation at UT " + F(context.UT) + " s: periapsis altitude " + F(pe) +
                " m, apoapsis altitude " + F(ap) + " m, orbital period " + F(period) +
                " s. These are captured KSP orbit values, not a prediction or maneuver assessment.";
        }

        string CaptureSupplies(CaptureContext context)
        {
            var totals = new Dictionary<string, ResourceTotals>(StringComparer.Ordinal);
            foreach (string name in ResourceNames) totals.Add(name, new ResourceTotals());
            if (context.Vessel.parts == null) throw new InvalidOperationException("Vessel part list is unavailable.");
            foreach (Part part in context.Vessel.parts)
            {
                if (part == null || part.Resources == null) throw new InvalidOperationException("Part resource list is unavailable.");
                foreach (PartResource resource in part.Resources)
                {
                    ResourceTotals total;
                    if (resource == null || !totals.TryGetValue(resource.resourceName, out total)) continue;
                    if (!Finite(resource.amount) || !Finite(resource.maxAmount) || resource.amount < 0.0 || resource.maxAmount < 0.0)
                        throw new InvalidOperationException("A resource amount or capacity is invalid.");
                    total.Amount += resource.amount;
                    total.Capacity += resource.maxAmount;
                    if (resource.flowState)
                    {
                        total.FlowAmount += resource.amount;
                        total.FlowCapacity += resource.maxAmount;
                    }
                    if (!Finite(total.Amount) || !Finite(total.Capacity) ||
                        !Finite(total.FlowAmount) || !Finite(total.FlowCapacity))
                        throw new InvalidOperationException("Aggregated resource totals overflowed.");
                }
            }

            var text = new StringBuilder();
            text.Append("Observed vessel resource amounts at UT ").Append(F(context.UT)).Append(" s. ");
            text.Append("Each all-store amount/capacity is separate from flow-enabled amount/capacity; no rate or future depletion is inferred. ");
            foreach (string name in ResourceNames)
            {
                ResourceTotals total = totals[name];
                text.Append(name).Append(": all ").Append(F(total.Amount)).Append(" / ").Append(F(total.Capacity));
                text.Append("; flow enabled ").Append(F(total.FlowAmount)).Append(" / ").Append(F(total.FlowCapacity)).Append(". ");
            }
            return text.ToString();
        }

        string CaptureScience(CaptureContext context)
        {
            if (context.Vessel.parts == null) throw new InvalidOperationException("Vessel part list is unavailable.");
            var seen = new HashSet<ScienceData>(ScienceComparer);
            var observations = new List<ScienceDataObservation>();
            var counts = new SortedDictionary<string, int>(StringComparer.Ordinal);
            var titles = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
            foreach (Part part in context.Vessel.parts)
            {
                if (part == null || part.Modules == null) throw new InvalidOperationException("Vessel module list is unavailable.");
                foreach (PartModule module in part.Modules)
                {
                    var container = module as IScienceDataContainer;
                    if (container == null) continue;
                    ScienceData[] stored = container.GetData();
                    if (stored == null) continue;
                    foreach (ScienceData data in stored)
                    {
                        if (data == null) throw new InvalidOperationException("Science container returned a null data card.");
                        if (!seen.Add(data)) continue;
                        if (String.IsNullOrWhiteSpace(data.subjectID) || !Finite(data.dataAmount) || data.dataAmount < 0.0)
                            throw new InvalidOperationException("Science data subject or amount is invalid.");
                        observations.Add(new ScienceDataObservation(data.subjectID, data.dataAmount));
                        int count;
                        counts.TryGetValue(data.subjectID, out count);
                        counts[data.subjectID] = count + 1;
                        SortedSet<string> subjectTitles;
                        if (!titles.TryGetValue(data.subjectID, out subjectTitles))
                        {
                            subjectTitles = new SortedSet<string>(StringComparer.Ordinal);
                            titles.Add(data.subjectID, subjectTitles);
                        }
                        subjectTitles.Add(String.IsNullOrWhiteSpace(data.title) ? "Untitled report" : data.title);
                    }
                }
            }

            ScienceObservationSnapshot snapshot = ScienceObservationSnapshot.Create(observations);
            var text = new StringBuilder();
            text.Append("Read-only retained science snapshot at UT ").Append(F(context.UT)).Append(" s: ");
            text.Append(seen.Count.ToString(CultureInfo.InvariantCulture)).Append(" distinct stored cards, ");
            text.Append(snapshot.Subjects.Count.ToString(CultureInfo.InvariantCulture)).Append(" exact subject IDs, ");
            text.Append(F(snapshot.TotalDataUnits)).Append(" total retained data units. ");
            text.Append("This does not establish experiment execution, scientific discovery, or science rewards. ");
            foreach (ScienceSubjectAmount subject in snapshot.Subjects)
            {
                text.Append("Subject ").Append(subject.SubjectId).Append(" | ");
                text.Append(counts[subject.SubjectId].ToString(CultureInfo.InvariantCulture)).Append(" stored cards | ");
                text.Append(F(subject.DataUnits)).Append(" data units | titles: ");
                bool first = true;
                foreach (string title in titles[subject.SubjectId])
                {
                    if (!first) text.Append("; ");
                    text.Append(title);
                    first = false;
                }
                text.Append(". ");
                if (text.Length > MaxObservationChars)
                    throw new InvalidOperationException("Complete retained-science observation exceeds the logbook limit; nothing was truncated or appended.");
            }
            return text.ToString();
        }

        void DrawEntries()
        {
            MissionLogScenario scenario;
            ProtoScenarioModule proto;
            string reason;
            if (!FindScenario(HighLogic.CurrentGame, out scenario, out proto, out reason))
            {
                GUILayout.Label(reason);
                return;
            }
            if (scenario == null)
            {
                GUILayout.Label(proto == null ? "No mission log entries yet. The save scenario is created only on the first valid capture." : reason);
                return;
            }
            if (!String.IsNullOrEmpty(scenario.LoadError))
            {
                GUILayout.Label("Logbook is read-only: " + scenario.LoadError);
                return;
            }

            var entries = scenario.Book == null ? null : scenario.Book.Entries;
            if (entries == null || entries.Count == 0)
            {
                GUILayout.Label("No mission log entries yet.");
                return;
            }
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                MissionLogEntry entry = entries[i];
                if (entry == null) continue;
                GUILayout.BeginVertical(GUI.skin.box);
                try
                {
                    GUILayout.Label("UT " + F(entry.UT) + " s | " + entry.Author + " | " + entry.Kind);
                    GUILayout.Label(entry.VesselName + " | " + entry.BodyName + " | vessel " + entry.VesselId.ToString("D"));
                    GUILayout.Label(entry.Observation, WrapStyle());
                    if (!String.IsNullOrEmpty(entry.Note)) GUILayout.Label("Note: " + entry.Note, WrapStyle());
                }
                finally { GUILayout.EndVertical(); }
            }
        }

        GUIStyle WrapStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.wordWrap = true;
            return style;
        }

        static bool Finite(double value) { return !Double.IsNaN(value) && !Double.IsInfinity(value); }
        static string F(double value) { return value.ToString("G8", CultureInfo.InvariantCulture); }
        static string SafeMessage(Exception exception)
        {
            return exception == null || String.IsNullOrWhiteSpace(exception.Message)
                ? "operation failed; no entry was appended." : exception.Message.Trim();
        }

        bool TextFieldFocused()
        {
            string focused = GUI.GetNameOfFocusedControl() ?? "";
            return focused == "MegaCrewMissionLogNote";
        }

        void ClosePanel()
        {
            visible = false;
            clearOwnedFocusPending = true;
            noteFocused = false;
            ReleaseInputLock();
        }

        void ClearOwnedFocusInOnGUI()
        {
            if (!clearOwnedFocusPending) return;
            if (TextFieldFocused()) GUI.FocusControl(null);
            noteFocused = false;
            clearOwnedFocusPending = false;
        }

        void UpdateOwnedFocusFromOnGUI()
        {
            noteFocused = TextFieldFocused();
        }

        void ReleaseInputLock()
        {
            InputLockManager.RemoveControlLock(LockId);
        }

        void OnApplicationFocus(bool focused)
        {
            appFocused = focused;
            if (!focused) ReleaseInputLock();
        }

        void OnDisable() { ReleaseInputLock(); }
        void OnDestroy() { ReleaseInputLock(); }
    }
}
