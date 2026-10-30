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
        string marker=Path.Combine(root,"FinchSpawn4QA.enable");
        if(!File.Exists(marker)) yield break;
        File.Move(marker,Path.Combine(root,"FinchSpawn4QA.consumed"));
        try {
            const string folder="MegaCrew Finch Spawn4 QA";
            var game=GamePersistence.LoadGame("persistent",folder,true,false);
            if(game==null)throw new Exception("Disposable save could not load");
            HighLogic.SaveFolder=folder;HighLogic.CurrentGame=game;
            Debug.Log("[FinchOrbitQA] Loading unmodified practice vessel from isolated QA save");
            FlightDriver.StartAndFocusVessel(game,0);
        } catch(Exception e) { Debug.LogError("[FinchOrbitQA] Fixture failed: "+e); }
    }
}
[KSPAddon(KSPAddon.Startup.Flight, false)]
public sealed class FinchOrbitCheck : MonoBehaviour
{
    static bool attempted;
    IEnumerator Start()
    {
        if(HighLogic.SaveFolder!="MegaCrew Finch Spawn4 QA")yield break;
        if(attempted){yield return StartCoroutine(Observe());yield break;}
        attempted=true;
        yield return new WaitForSecondsRealtime(12);
        if(FlightGlobals.ActiveVessel==null){Debug.LogError("[FinchOrbitQA] Practice vessel failed to load");yield break;}
        Debug.Log("[FinchOrbitQA] Practice vessel loaded: "+FlightGlobals.ActiveVessel.vesselName);
        // Spawn far from the practice vessel, never on top of the original station.
        try {
            var game=HighLogic.CurrentGame;var path=Path.Combine(KSPUtil.ApplicationRootPath,"Ships","VAB","Hermes Finch Docking Cradle.craft");
            var craft=ShipConstruction.LoadShip(path);if(craft==null||craft.parts.Count!=35)throw new Exception("Unexpected craft load");
            uint launch=game.launchID++;
            for(int i=0;i<craft.parts.Count;i++){var p=craft.parts[i];p.flightID=2700000000u+(uint)i;p.missionID=54321;p.launchID=launch;p.flagURL="Squad/Flags/default";p.temperature=293;}
            var dummyProto=new ProtoVessel(new ConfigNode(),null);
            var dummyObject=new GameObject("FinchOrbitDummy");var dummy=dummyObject.AddComponent<Vessel>();dummy.parts=craft.parts;dummyProto.vesselRef=dummy;
            foreach(var p in craft.parts)dummyProto.protoPartSnapshots.Add(new ProtoPartSnapshot(p,dummyProto));
            foreach(var snapshot in dummyProto.protoPartSnapshots)snapshot.storePartRefs();
            var nodes=new ConfigNode[dummyProto.protoPartSnapshots.Count];
            for(int i=0;i<nodes.Length;i++){nodes[i]=new ConfigNode("PART");dummyProto.protoPartSnapshots[i].Save(nodes[i]);}
            // ShipConstruct snapshots are editor-state: KSP has not yet initialized
            // the two mated ports. Reproduce the stock launched "PreAttached"
            // state in this test-only fixture, then verify actual flight behavior.
            foreach(int idx in new[]{17,21})foreach(var module in nodes[idx].GetNodes("MODULE"))
                if(module.GetValue("name")=="ModuleDockingNode")module.SetValue("state","PreAttached",true);
            var orbit=new Orbit(0,0,900000,0,0,0,Planetarium.GetUniversalTime(),FlightGlobals.GetHomeBody());
            var node=ProtoVessel.CreateVesselNode("Finch Orbit QA",VesselType.Station,orbit,0,nodes);
            var proto=game.AddVessel(node);Destroy(dummyObject);
            if(proto==null||proto.vesselRef==null)throw new Exception("AddVessel returned no vessel");
            Debug.Log("[FinchOrbitQA] Spawned disposable vessel id="+proto.vesselRef.id);
            bool switched=FlightGlobals.SetActiveVessel(proto.vesselRef);
            Debug.Log("[FinchOrbitQA] Switch accepted="+switched);
        } catch(Exception e){Debug.LogError("[FinchOrbitQA] Spawn failed: "+e);yield break;}
        yield break;
    }
    IEnumerator Observe()
    {
        yield return new WaitForSecondsRealtime(12);
        var v=FlightGlobals.ActiveVessel;
        if(v==null||v.vesselName!="Finch Orbit QA"){Debug.LogError("[FinchOrbitQA] Target not active");yield break;}
        float deadline=Time.realtimeSinceStartup+30;
        while((!v.loaded||v.packed)&&Time.realtimeSinceStartup<deadline){yield return new WaitForSecondsRealtime(1);v=FlightGlobals.ActiveVessel;if(v==null)yield break;}
        int snapshots=v.protoVessel==null?-1:v.protoVessel.protoPartSnapshots.Count;
        Debug.Log("[FinchOrbitQA] LOAD_GATE loaded="+v.loaded+" packed="+v.packed+" liveParts="+v.parts.Count+" snapshotParts="+snapshots);
        if(!v.loaded||v.packed){Debug.LogError("[FinchOrbitQA] INCONCLUSIVE: vessel never became loaded and unpacked; do not infer part loss or docking from partial live lists");yield break;}
        Debug.Log("[FinchOrbitQA] Vessel="+v.vesselName+" parts="+v.parts.Count+" situation="+v.situation+" Pe="+v.orbit.PeA+" Ap="+v.orbit.ApA);
        foreach(var other in FlightGlobals.Vessels)if(other!=null)Debug.Log("[FinchOrbitQA] WORLD "+other.vesselName+" loaded="+other.loaded+" parts="+(other.loaded?other.parts.Count:other.protoVessel.protoPartSnapshots.Count));
        foreach(var p in v.parts)foreach(PartModule m in p.Modules)if(m.moduleName=="ModuleDockingNode"){
            var node=m as ModuleDockingNode;var decouple=m.Events["Decouple"];var undock=m.Events["Undock"];
            Debug.Log("[FinchOrbitQA] DOCK id="+p.flightID+" state="+node.state+" decouple="+(decouple!=null&&decouple.active)+" undock="+(undock!=null&&undock.active));
        }
        yield return new WaitForSecondsRealtime(20);
        v=FlightGlobals.ActiveVessel;
        if(v!=null)Debug.Log("[FinchOrbitQA] AFTER20 parts="+v.parts.Count+" situation="+v.situation);
    }
}
