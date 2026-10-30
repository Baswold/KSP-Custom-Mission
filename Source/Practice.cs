using System;
using UnityEngine;

namespace MegaCrew
{
    public sealed partial class MissionConsole
    {
        int practiceStep;
        bool practiceStarted, sawFrozen;
        string practiceVessel = "";
        double powerSeconds, powerStart, practiceSeconds;
        readonly string[] practiceTasks = {
            "Verify the 150 km parking orbit and four crew.",
            "Restore generation: extend at least two solar arrays and one radiator using their part menus.",
            "Observe a stable battery for 60 seconds in sunlight.",
            "Walk from command through quarters and lab to the cryobay. Inspect hatches and report back.",
            "Medical drill: record a sprain, have a second crew member treat it, and finish recovery.",
            "Transfer Ada to the cryobay, freeze her, then thaw her and verify she is awake.",
            "Final orbit, power and crew check. Debrief complete."
        };
        void ResetPractice(){practiceStep=0;practiceStarted=false;sawFrozen=false;practiceVessel="";powerSeconds=0;powerStart=0;practiceSeconds=0;}
        bool PracticeShip {get{return Ship!=null&&Ship.id.ToString()==practiceVessel;}}
        void Charge(out double amount,out double capacity){amount=0;capacity=0;if(Ship==null)return;foreach(var p in Ship.parts)foreach(PartResource r in p.Resources)if(r.resourceName=="ElectricCharge"){amount+=r.amount;capacity+=r.maxAmount;}}
        bool SafeOrbit(){return Ship!=null&&Ship.mainBody.bodyName=="Kerbin"&&Ship.orbit!=null&&Ship.orbit.PeA>120000&&Ship.orbit.ApA<180000;}
        bool FourCrew(){foreach(var c in crew)if(!Aboard(c.Name)&&!Frozen(c.Name))return false;return true;}
        int Extended(string module){int count=0;if(Ship==null)return 0;foreach(var p in Ship.parts)foreach(PartModule m in p.Modules)if(m.moduleName==module){var f=m.GetType().GetField("deployState");if(f!=null&&Convert.ToString(f.GetValue(m)).Equals("EXTENDED",StringComparison.OrdinalIgnoreCase))count++;}return count;}
        void AdvancePractice(){practiceStep=Math.Min(6,practiceStep+1);powerSeconds=0;Save();ScreenMessages.PostScreenMessage("MegaCrew: maintenance objective updated",4,ScreenMessageStyle.UPPER_CENTER);}
        void UpdatePractice(double dt)
        {
            if(!practiceStarted||!PracticeShip||!HighLogic.LoadedSceneIsFlight||dt<=0||dt>5||TimeWarp.CurrentRateIndex!=0||!running)return;
            practiceSeconds+=dt;
            if(practiceStep==0&&SafeOrbit()&&FourCrew())AdvancePractice();
            else if(practiceStep==1&&Extended("ModuleDeployableSolarPanel")>=2&&Extended("ModuleDeployableRadiator")>=1)AdvancePractice();
            else if(practiceStep==2){double ec,cap;Charge(out ec,out cap);if(powerSeconds==0)powerStart=ec;if(cap<=0||ec<powerStart-cap*0.02){powerSeconds=0;return;}powerSeconds+=dt;if(powerSeconds>=60)AdvancePractice();}
            else if(practiceStep==4){bool treated=false;foreach(var c in crew)if(c.Treated&&c.Injury!="None")checks.Add("practiceMedicalTreated");foreach(var c in crew)if(c.Injury!="None")treated=true;if(checks.Contains("practiceMedicalTreated")&&!treated)AdvancePractice();}
            else if(practiceStep==5){if(Frozen("Ada Kerman"))sawFrozen=true;if(sawFrozen&&!Frozen("Ada Kerman")&&Aboard("Ada Kerman"))AdvancePractice();}
        }
        void DrawPractice()
        {
            GUILayout.Label("ORBITAL MAINTENANCE / 10-15 MINUTE PRACTICE");
            GUILayout.Label("Load the MegaCrew Practice save: Hermes starts in a circular 150 km Kerbin orbit. No launch or time warp is required. Start all four crew awake. Keep the centrifuge stopped if it behaves poorly.");
            DrawTelemetry();
            GUI.enabled=Allowed&&HighLogic.LoadedSceneIsFlight&&Ship!=null;
            if(!practiceStarted&&GUILayout.Button("Begin practice on this vessel")){ResetPractice();practiceStarted=true;practiceVessel=Ship.id.ToString();running=true;lastUt=Planetarium.GetUniversalTime();message="Maintenance watch started. Open the solar/radiator part menus for objective 2.";Save();}
            GUI.enabled=true;
            if(!practiceStarted)return;
            if(!PracticeShip){GUILayout.Label("Return to the practice station to continue tracking.");return;}
            GUILayout.Label("Elapsed watch: "+(practiceSeconds/60).ToString("F1")+" min");
            for(int i=0;i<practiceTasks.Length;i++)GUILayout.Label((i<practiceStep?"DONE | ":i==practiceStep?"NOW  | ":"WAIT | ")+practiceTasks[i]);
            if(practiceStep==2)GUILayout.Label("Battery observation: "+powerSeconds.ToString("F0")+" / 60 s; resets if charge falls more than 2% of total capacity below the interval start. This checks charge trend, not hardware reliability.");
            GUI.enabled=Allowed&&HighLogic.LoadedSceneIsFlight;
            if(practiceStep==3){GUILayout.Label("C enters IVA; Y unbuckles; WASD moves; Shift/Ctrl moves up/down; F operates a hatch you are looking at. This inspection is a manual crew sign-off.");if(GUILayout.Button("Sign off: internal inspection completed"))AdvancePractice();}
            if(practiceStep==4)GUILayout.Label("Open Emergency -> Record minor sprain, then Crew & cryosleep -> Treat. Keep watch tracking running for the five-minute simulated recovery.");
            if(practiceStep==5)GUILayout.Label("Use stock crew transfer to put Ada in the real DeepFreeze pod. Freeze via Crew & cryosleep or the pod controls; then Wake frozen crew now. Power and Glykerol are required. Real pod state is monitored.");
            if(practiceStep==6)GUILayout.Label(SafeOrbit()&&FourCrew()?"PRACTICE COMPLETE: orbit and four-crew manifest verified. Save the game. No automatic repair or interior traversal proof is claimed.":"Recheck orbit and restore all four crew before final sign-off.");
            GUI.enabled=true;
            GUILayout.Label("Automatic evidence: orbit, crew manifest, deployed arrays/radiator, charge interval and DeepFreeze state. Interior inspection and medical diagnosis remain training sign-offs.");
        }
        void SavePractice(ConfigNode n){var p=n.AddNode("PRACTICE");p.AddValue("started",practiceStarted);p.AddValue("step",practiceStep);p.AddValue("vessel",practiceVessel);p.AddValue("seconds",practiceSeconds);p.AddValue("sawFrozen",sawFrozen);}
        void LoadPractice(ConfigNode n){var p=n.GetNode("PRACTICE");if(p==null)return;bool.TryParse(p.GetValue("started"),out practiceStarted);int.TryParse(p.GetValue("step"),out practiceStep);practiceStep=Math.Max(0,Math.Min(6,practiceStep));practiceVessel=p.GetValue("vessel")??"";double.TryParse(p.GetValue("seconds"),out practiceSeconds);bool.TryParse(p.GetValue("sawFrozen"),out sawFrozen);}
    }
}
