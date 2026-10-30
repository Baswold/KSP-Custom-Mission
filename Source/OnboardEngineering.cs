using System;
using UnityEngine;

namespace MegaCrew
{
    // Separate addon: no changes to the trainer's objective or medical state.
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public sealed class OnboardEngineering : MonoBehaviour
    {
        bool visible;
        Rect window = new Rect(40, 100, 560, 480);
        Vector2 scroll;
        string notice = "Select an individual array or radiator to operate it.";
        const string LockName = "MegaCrew.OnboardEngineering";

        bool Mission(Vessel vessel)
        {
            return HighLogic.LoadedSceneIsFlight && vessel != null && !vessel.isEVA &&
                (HighLogic.SaveFolder ?? "").StartsWith("MegaCrew", StringComparison.OrdinalIgnoreCase);
        }

        bool Engineer(Vessel vessel)
        {
            if (!Mission(vessel)) return false;
            foreach (var member in vessel.GetVesselCrew())
                if (member.experienceTrait != null && member.experienceTrait.TypeName == "Engineer") return true;
            return false;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.E) &&
                (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) &&
                (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))) visible = !visible;
            if (!Mission(FlightGlobals.ActiveVessel)) visible = false;
            if (!visible) InputLockManager.RemoveControlLock(LockName);
        }

        void OnDestroy() { InputLockManager.RemoveControlLock(LockName); }

        void OnGUI()
        {
            if (!Mission(FlightGlobals.ActiveVessel)) return;
            GUI.skin = HighLogic.Skin;
            if (GUI.Button(new Rect(180, Screen.height - 95, 190, 34), "ENGINEERING / Ctrl+Alt+E")) visible = !visible;
            if (!visible) return;
            window.x = Mathf.Clamp(window.x, 0, Math.Max(0, Screen.width - window.width));
            window.y = Mathf.Clamp(window.y, 0, Math.Max(0, Screen.height - 100));
            window = GUILayout.Window(719305, window, Draw, "ONBOARD ENGINEERING", GUILayout.Width(560));
            var mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            if (window.Contains(mouse)) InputLockManager.SetControlLock(ControlTypes.ALL_SHIP_CONTROLS, LockName);
            else InputLockManager.RemoveControlLock(LockName);
        }

        void Draw(int id)
        {
            if (GUILayout.Button("Close")) visible = false;
            var vessel = FlightGlobals.ActiveVessel;
            if (!Mission(vessel)) return;
            GUILayout.Label(vessel.vesselName + " | power and cooling");
            bool engineer = Engineer(vessel);
            GUILayout.Label(engineer ? "Engineer aboard: controls available." : "An engineer must be aboard to operate this station.");
            double ec = 0, capacity = 0;
            foreach (var part in vessel.parts)
                foreach (PartResource resource in part.Resources)
                    if (resource.resourceName == "ElectricCharge") { ec += resource.amount; capacity += resource.maxAmount; }
            GUILayout.Label("ElectricCharge: " + ec.ToString("F0") + " / " + capacity.ToString("F0"));
            GUILayout.Label("Individual controls operate actual hardware; check clearance before deployment.");
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(290));
            int count = 0;
            foreach (var part in vessel.parts)
                foreach (PartModule module in part.Modules)
                {
                    if (module.moduleName != "ModuleDeployableSolarPanel" && module.moduleName != "ModuleDeployableRadiator") continue;
                    count++;
                    GUILayout.BeginVertical(GUI.skin.box);
                    var state = module.GetType().GetField("deployState");
                    GUILayout.Label(part.partInfo.title + " [" + part.flightID + "] | " +
                        (state == null ? "State unavailable" : Convert.ToString(state.GetValue(module))));
                    DrawCommand(vessel, module, "Extend", "Deploy", engineer);
                    DrawCommand(vessel, module, "Retract", "Stow", engineer);
                    GUILayout.EndVertical();
                }
            if (count == 0) GUILayout.Label("No supported deployable solar arrays or radiators on this vessel.");
            GUILayout.EndScrollView();
            GUILayout.Label(notice);
            GUILayout.Label("Available in IVA. Local single-player controls; shared LMP authority is not implemented.");
            GUI.DragWindow(new Rect(0, 0, 10000, 24));
        }

        void DrawCommand(Vessel vessel, PartModule module, string name, string label, bool engineer)
        {
            var command = module.Events[name];
            GUI.enabled = engineer && command != null && command.active;
            if (GUILayout.Button(label))
            {
                // Revalidate at dispatch; never operate another vessel after switching.
                if (vessel != FlightGlobals.ActiveVessel || !Engineer(vessel) || module.vessel != vessel ||
                    command == null || !command.active) notice = "Command refused: vessel, crew or hardware state changed.";
                else
                {
                    try { command.Invoke(); notice = label + " requested. Verify the hardware state above."; }
                    catch (Exception ex) { notice = "Hardware command failed; see KSP.log."; Debug.LogError("[MegaCrew Engineering] " + ex); }
                }
            }
            GUI.enabled = true;
        }
    }
}
