// Test-only one-shot KSP orbital fixture. Never install in a user's normal game.
using System;
using System.Collections;
using System.IO;
using UnityEngine;
[KSPAddon(KSPAddon.Startup.MainMenu, false)]
public sealed class FinchOrbitStart : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(5);
        string root=KSPUtil.ApplicationRootPath;
        string marker=Path.Combine(root,"FinchOrbitQA.enable");
        if(!File.Exists(marker)) yield break;
        File.Move(marker,Path.Combine(root,"FinchOrbitQA.consumed"));
        try {
            const string folder="MegaCrew Finch Orbit QA";
            var game=GamePersistence.LoadGame("persistent",folder,true,false);
            if(game==null)throw new Exception("Disposable save could not load");
            HighLogic.SaveFolder=folder;HighLogic.CurrentGame=game;
            var path=Path.Combine(root,"Ships","VAB","Hermes Finch Docking Cradle.craft");
            var craft=ShipConstruction.LoadShip(path);
            if(craft==null||craft.parts.Count!=35)throw new Exception("Unexpected craft load");
            uint launch=game.launchID++;
            for(int i=0;i<craft.parts.Count;i++){var p=craft.parts[i];p.flightID=2700000000u+(uint)i;p.missionID=54321;p.launchID=launch;p.flagURL="Squad/Flags/default";p.temperature=293;}
            var dummyProto=new ProtoVessel(new ConfigNode(),null);
            var dummyObject=new GameObject("FinchOrbitDummy");var dummy=dummyObject.AddComponent<Vessel>();dummy.parts=craft.parts;dummyProto.vesselRef=dummy;
            foreach(var p in craft.parts)dummyProto.protoPartSnapshots.Add(new ProtoPartSnapshot(p,dummyProto));
            foreach(var snapshot in dummyProto.protoPartSnapshots)snapshot.storePartRefs();
            var nodes=new ConfigNode[dummyProto.protoPartSnapshots.Count];
            for(int i=0;i<nodes.Length;i++){nodes[i]=new ConfigNode("PART");dummyProto.protoPartSnapshots[i].Save(nodes[i]);}
            var orbit=new Orbit(0,0,0,750000,0,0,100000,FlightGlobals.GetHomeBody());
            var node=ProtoVessel.CreateVesselNode("Finch Orbit QA",VesselType.Station,orbit,0,nodes);
            var proto=new ProtoVessel(node,game);
            game.flightState.protoVessels.Clear();game.flightState.protoVessels.Add(proto);
            Debug.Log("[FinchOrbitQA] Starting isolated orbit fixture with "+nodes.Length+" parts");
            Destroy(dummyObject);
            FlightDriver.StartAndFocusVessel(game,0);
        } catch(Exception e) { Debug.LogError("[FinchOrbitQA] Fixture failed: "+e); }
    }
}
[KSPAddon(KSPAddon.Startup.Flight, false)]
public sealed class FinchOrbitCheck : MonoBehaviour
{
    IEnumerator Start()
    {
        if(HighLogic.SaveFolder!="MegaCrew Finch Orbit QA")yield break;
        yield return new WaitForSecondsRealtime(8);
        var v=FlightGlobals.ActiveVessel;
        if(v==null){Debug.LogError("[FinchOrbitQA] No active vessel");yield break;}
        Debug.Log("[FinchOrbitQA] Vessel="+v.vesselName+" parts="+v.parts.Count+" situation="+v.situation+" Pe="+v.orbit.PeA+" Ap="+v.orbit.ApA);
        foreach(var p in v.parts)foreach(PartModule m in p.Modules)if(m.moduleName=="ModuleDockingNode"){
            var node=m as ModuleDockingNode;var decouple=m.Events["Decouple"];var undock=m.Events["Undock"];
            Debug.Log("[FinchOrbitQA] DOCK id="+p.flightID+" state="+node.state+" decouple="+(decouple!=null&&decouple.active)+" undock="+(undock!=null&&undock.active));
        }
        yield return new WaitForSecondsRealtime(20);
        v=FlightGlobals.ActiveVessel;
        if(v!=null)Debug.Log("[FinchOrbitQA] AFTER20 parts="+v.parts.Count+" situation="+v.situation);
    }
}
