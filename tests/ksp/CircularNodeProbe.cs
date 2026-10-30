// Disposable QA only. Adds and removes one temporary node in an isolated save.
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using MegaCrew.Navigation;
[KSPAddon(KSPAddon.Startup.MainMenu,false)]
public sealed class CircularNodeProbeStart:MonoBehaviour
{
    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(5);
        string root=KSPUtil.ApplicationRootPath,marker=Path.Combine(root,"CircularNodeQA.enable");
        if(!File.Exists(marker))yield break;
        File.Move(marker,Path.Combine(root,"CircularNodeQA.consumed"));
        try {
            const string name="MegaCrew Navigation QA";
            var game=GamePersistence.LoadGame("persistent",name,true,false);
            if(game==null)throw new Exception("Missing disposable QA save");
            HighLogic.SaveFolder=name;HighLogic.CurrentGame=game;
            Debug.Log("[CircularNodeQA] Loading isolated practice vessel");
            FlightDriver.StartAndFocusVessel(game,0);
        }catch(Exception e){Debug.LogError("[CircularNodeQA] Setup failed: "+e);}
    }
}
[KSPAddon(KSPAddon.Startup.Flight,false)]
public sealed class CircularNodeProbeFlight:MonoBehaviour
{
    IEnumerator Start()
    {
        if(HighLogic.SaveFolder!="MegaCrew Navigation QA")yield break;
        yield return new WaitForSecondsRealtime(10);
        var vessel=FlightGlobals.ActiveVessel;
        if(vessel==null||vessel.patchedConicSolver==null){Debug.LogError("[CircularNodeQA] Missing vessel or solver");yield break;}
        var solver=vessel.patchedConicSolver;
        if(solver.maneuverNodes.Count!=0){Debug.LogError("[CircularNodeQA] Refusing to alter existing nodes");yield break;}
        ManeuverNode node=null;
        try {
            double radius=vessel.orbit.semiMajorAxis,body=vessel.mainBody.Radius,mu=vessel.mainBody.gravParameter;
            double target=80000, ut=Planetarium.GetUniversalTime()+120;
            if(Math.Abs(vessel.orbit.eccentricity)>1e-5)throw new Exception("Fixture orbit not circular");
            double dv=MissionControlMath.CircularApoapsisBurn(mu,body,radius,target);
            node=solver.AddManeuverNode(ut);
            if(node==null)throw new Exception("Node creation failed");
            node.DeltaV=new global::Vector3d(0,0,dv);solver.UpdateFlightPlan();
            Debug.Log("[CircularNodeQA] Requested dv="+dv+" UT="+ut);
        }catch(Exception e){Debug.LogError("[CircularNodeQA] Setup failed: "+e);if(node!=null)node.RemoveSelf();yield break;}
        yield return new WaitForSecondsRealtime(3);
        try {
            if(node.nextPatch==null||!node.nextPatch.activePatch)throw new Exception("No active next patch");
            double actual=node.nextPatch.PeA,error=actual-80000;
            Debug.Log("[CircularNodeQA] RESULT predictedPe="+actual+" error="+error+" reference="+node.nextPatch.referenceBody.bodyName);
        }catch(Exception e){Debug.LogError("[CircularNodeQA] Prediction failed: "+e);}
        finally {node.RemoveSelf();solver.UpdateFlightPlan();Debug.Log("[CircularNodeQA] CLEANUP remaining="+solver.maneuverNodes.Count);}
    }
}
