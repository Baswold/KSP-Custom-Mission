// QA-only assembly. NEVER include in the normal MegaCrew plugin.
using System;
using System.Collections;
using System.IO;
using System.Globalization;
using System.Security.Cryptography;
using UnityEngine;
using MegaCrew;

[KSPAddon(KSPAddon.Startup.MainMenu,false)]
public sealed class HermesCryoProbe : MonoBehaviour
{
    const string SaveName="MegaCrew Background QA";
    string root;ConfigNode report=new ConfigNode("HERMES_CRYO_QA");bool scoped,fixtureLoaded;Guid intendedVessel;
    IEnumerator Start()
    {
        root=KSPUtil.ApplicationRootPath;
        var scope=Path.Combine(root,"HermesCryoQA.scope");var marker=Path.Combine(root,"HermesCryoQA.enable");
        if(new DirectoryInfo(root).Name!="KSP-Hermes-QA"||!File.Exists(scope)||File.ReadAllText(scope).Trim()!="MEGACREW_DISPOSABLE_CRYO_QA_V1"||!File.Exists(marker))yield break;
        foreach(var suffix in new[]{"running","done"})
            if(File.Exists(Path.Combine(root,"HermesCryoQA."+suffix)))yield break;
        if(File.Exists(Path.Combine(root,"HermesCryoQA-result.cfg")))yield break;
        scoped=true;DontDestroyOnLoad(this);
        File.Move(marker,Path.Combine(root,"HermesCryoQA.running"));
        var steps=Run();
        while(true)
        {
            object next=null;bool more=false;Exception failure=null;
            try{ValidateContext();more=steps.MoveNext();if(more)next=steps.Current;}catch(Exception e){failure=e;}
            if(failure!=null){Record("failure",failure.GetType().Name+": "+failure.Message);Finish(false);yield break;}
            if(!more){Finish(true);yield break;}
            yield return next;
        }
    }
    void ValidateContext()
    {
        if(!fixtureLoaded)return;
        if(HighLogic.SaveFolder!=SaveName)throw new Exception("QA save context changed; test aborted before next action");
        if(TimeWarp.CurrentRateIndex!=0)throw new Exception("QA requires uninterrupted actual 1x progression");
        if(HighLogic.LoadedSceneIsFlight&&FlightDriver.Pause)throw new Exception("QA paused; observation is inconclusive");
        if(HighLogic.LoadedSceneIsFlight&&FlightGlobals.ready&&FlightGlobals.ActiveVessel!=null&&FlightGlobals.ActiveVessel.id!=intendedVessel)
            throw new Exception("QA active vessel changed; test aborted before next action");
    }
    void Record(string key,object value)
    {
        string text=value is IFormattable?((IFormattable)value).ToString(null,CultureInfo.InvariantCulture):value.ToString();
        report.SetValue(key,text,true);report.Save(Path.Combine(root,"HermesCryoQA-result.cfg"));
        Debug.Log("[HermesCryoQA] "+key+"="+value);
    }
    void Finish(bool complete)
    {
        Record("run_state",complete?"completed":"failed");
        Record("probe_completed",complete);Record("flight_qualification",false);
        var marker=Path.Combine(root,"HermesCryoQA.running");
        if(File.Exists(marker))File.Move(marker,Path.Combine(root,"HermesCryoQA.done"));
        // Only this explicitly scoped disposable app is ever closed by the probe.
        if(scoped)Application.Quit();
    }
    static double Charge(Vessel v)
    {double amount=0;foreach(var p in v.parts)foreach(PartResource r in p.Resources)if(r.resourceName=="ElectricCharge"&&r.flowState)amount+=r.amount;return amount;}
    static bool Frozen(Guid vessel)
    {DF.KerbalInfo info;return DF.DeepFreeze.Instance!=null&&DF.DeepFreeze.Instance.FrozenKerbals.TryGetValue("Ada Kerman",out info)&&info.vesselID==vessel;}
    IEnumerator Run()
    {
        Record("schema",1);Record("probe_completed",false);Record("flight_qualification",false);Record("run_state","running");Record("stage","initializing");
        yield return new WaitForSecondsRealtime(5);
        var variant=File.ReadAllText(Path.Combine(root,"HermesCryoQA.variant")).Trim();Record("variant",variant);if(variant!="original"&&variant!="patched")throw new Exception("Unknown QA variant");
        var path=Path.Combine(root,"GameData/REPOSoftTech/BackgroundResources/BackgroundResources.dll");
        string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();
        Record("provider_sha256",hash);
        string expected=variant=="patched"?"d2c1ab678a0891246dc7b16e66e180ced7e8ad55db8bb9971cc742224dfee45a":"ed956b9ec2cc5f414586a52a9086423e4c9125fa72097bce2721ea41eb075bc9";
        if(hash!=expected)throw new Exception("Provider hash mismatch");
        var game=GamePersistence.LoadGame("persistent",SaveName,true,false);
        if(game==null||game.flightState.protoVessels.Count!=1)throw new Exception("Expected one pristine QA vessel");
        intendedVessel=game.flightState.protoVessels[0].vesselID;
        HighLogic.SaveFolder=SaveName;HighLogic.CurrentGame=game;fixtureLoaded=true;Record("stage","loading_flight");
        FlightDriver.StartAndFocusVessel(game,0);
        double deadline=Time.realtimeSinceStartup+180;
        while(!HighLogic.LoadedSceneIsFlight||!FlightGlobals.ready||FlightGlobals.ActiveVessel==null||!FlightGlobals.ActiveVessel.loaded||FlightGlobals.ActiveVessel.packed)
        {if(Time.realtimeSinceStartup>deadline)throw new Exception("Flight initialization timeout");yield return new WaitForSecondsRealtime(1);}
        yield return new WaitForSecondsRealtime(12);
        var v=FlightGlobals.ActiveVessel;Guid vesselId=v.id;
        if(v.parts.Count!=17||v.GetCrewCount()!=4)throw new Exception("Pristine vessel/crew mismatch");
        if(TimeWarp.CurrentRateIndex!=0)throw new Exception("Expected actual 1x progression");
        Record("initial_parts",v.parts.Count);Record("initial_awake_crew",v.GetCrewCount());Record("initial_ec",Charge(v));
        Part freezer=null,origin=null;ProtoCrewMember ada=null;
        foreach(var part in v.parts)
        {
            foreach(PartModule m in part.Modules)
                if(m.moduleName=="DeepFreezer")freezer=part;
            foreach(var person in part.protoModuleCrew)if(person.name=="Ada Kerman"){ada=person;origin=part;}
        }
        if(freezer==null||origin==null||ada==null||Charge(v)<3300)throw new Exception("Missing pod/crew or insufficient freeze reserve");
        uint podId=freezer.flightID;Record("stage","transferring_crew");
        // Actual stock part crew transfer, followed by the real DeepFreeze API.
        origin.RemoveCrewmember(ada);freezer.AddCrewmember(ada);
        GameEvents.onCrewTransferred.Fire(new GameEvents.HostedFromToAction<ProtoCrewMember,Part>(ada,origin,freezer));
        Vessel.CrewWasModified(origin.vessel,freezer.vessel);v.DespawnCrew();
        yield return new WaitForSecondsRealtime(1);v.SpawnCrew();
        DF.DeepFreezer actualPod=null;foreach(PartModule m in freezer.Modules)if(m is DF.DeepFreezer)actualPod=(DF.DeepFreezer)m;
        if(actualPod==null)throw new Exception("Actual pod module unavailable");
        deadline=Time.realtimeSinceStartup+90;
        while(!DF.DeepFreeze.APIReady||actualPod.DFIcrewXferTOActive||actualPod.DFIcrewXferFROMActive)
        {if(Time.realtimeSinceStartup>deadline)throw new Exception("Stock transfer/pod readiness timeout");yield return new WaitForSecondsRealtime(1);}
        yield return new WaitForSecondsRealtime(2);
        Record("stage","freezing_crew");
        if(!freezer.protoModuleCrew.Contains(ada)||!CryoBridge.Freeze("Ada Kerman"))throw new Exception("Real freeze request rejected");
        deadline=Time.realtimeSinceStartup+240;
        while(!Frozen(vesselId)){if(Time.realtimeSinceStartup>deadline)throw new Exception("Actual freeze timeout");yield return new WaitForSecondsRealtime(1);}
        foreach(var part in v.parts)foreach(PartModule m in part.Modules)
        {
            var panel=m as ModuleDeployableSolarPanel;
            if(panel!=null&&panel.deployState.ToString()!="RETRACTED")throw new Exception("Expected stowed solar panels for unloaded power observation");
            var generator=m as ModuleGenerator;
            if(generator!=null&&generator.generatorIsActive)throw new Exception("Unexpected active generator in power fixture");
        }
        Record("actual_frozen_before_unload",true);Record("ec_after_freeze",Charge(v));
        GamePersistence.SaveGame("persistent",SaveName,SaveMode.OVERWRITE);
        Record("stage","unloading_vessel");
        HighLogic.LoadScene(GameScenes.TRACKSTATION);
        yield return new WaitForSecondsRealtime(12);
        v=FlightGlobals.Vessels.Find(x=>x.id==vesselId);
        if(v==null||v.loaded)throw new Exception("Expected the QA vessel to be genuinely unloaded");
        if(!BackgroundResources.UnloadedResources.InterestedVessels.Contains(v.protoVessel))throw new Exception("Provider did not register unloaded vessel");
        DF.PartInfo pod;
        if(!DF.DeepFreeze.Instance.KnownFreezerParts.TryGetValue(podId,out pod))throw new Exception("Missing real pod ledger");
        double before,capacity;BackgroundResources.UnloadedResourceProcessing.GetResourceTotals(v.protoVessel,"ElectricCharge",out before,out capacity);
        Record("stage","observing_unloaded_power");
        double startUT=Planetarium.GetUniversalTime(),last=pod.timeLastElectricity;
        Record("unloaded_before_ec",before);Record("unloaded_before_ut",startUT);Record("unloaded_before_pod_stamp",last);
        yield return new WaitForSecondsRealtime(45);
        double after;BackgroundResources.UnloadedResourceProcessing.GetResourceTotals(v.protoVessel,"ElectricCharge",out after,out capacity);
        Record("unloaded_after_ec",after);Record("unloaded_elapsed_ut",Planetarium.GetUniversalTime()-startUT);
        Record("pod_accounted_seconds",pod.timeLastElectricity-last);Record("pod_expected_ec",(pod.timeLastElectricity-last)*pod.frznChargeRequired/60*pod.numFrznCrew);
        Record("observed_ec_delta",after-before);
        if(variant=="patched"&&after>=before)throw new Exception("Corrected provider did not produce net EC draw; investigate actual load/generation");
        Record("actual_frozen_after_unload",Frozen(vesselId));
        if(!Frozen(vesselId))throw new Exception("Frozen crew lost during unloaded interval");
        GamePersistence.SaveGame("persistent",SaveName,SaveMode.OVERWRITE);
        Record("stage","reloading_frozen_crew");
        var restored=GamePersistence.LoadGame("persistent",SaveName,true,false);
        HighLogic.CurrentGame=restored;HighLogic.SaveFolder=SaveName;FlightDriver.StartAndFocusVessel(restored,0);
        deadline=Time.realtimeSinceStartup+180;
        while(!HighLogic.LoadedSceneIsFlight||FlightGlobals.ActiveVessel==null||!FlightGlobals.ready||!FlightGlobals.ActiveVessel.loaded||FlightGlobals.ActiveVessel.packed)
        {if(Time.realtimeSinceStartup>deadline)throw new Exception("Reload flight timeout");yield return new WaitForSecondsRealtime(1);}
        yield return new WaitForSecondsRealtime(12);v=FlightGlobals.ActiveVessel;
        Record("frozen_after_reload",Frozen(vesselId));Record("awake_after_reload",v.GetCrewCount());
        if(!Frozen(vesselId)||v.GetCrewCount()!=3)throw new Exception("Freeze persistence failed");
        Record("stage","recharging_from_solar");
        // Refill through actual solar generation; never inject EC or skip UT.
        foreach(var part in v.parts)foreach(PartModule m in part.Modules)
            if(m.moduleName=="ModuleDeployableSolarPanel"&&m.Events["Extend"]!=null&&m.Events["Extend"].active)m.Events["Extend"].Invoke();
        deadline=Time.realtimeSinceStartup+2400;
        while(Charge(v)<3300){if(Time.realtimeSinceStartup>deadline)throw new Exception("Solar recharge timeout");yield return new WaitForSecondsRealtime(5);}
        Record("stage","thawing_crew");
        if(!CryoBridge.Thaw("Ada Kerman"))throw new Exception("Real thaw request rejected");
        deadline=Time.realtimeSinceStartup+240;
        while(Frozen(vesselId)){if(Time.realtimeSinceStartup>deadline)throw new Exception("Actual thaw timeout");yield return new WaitForSecondsRealtime(1);}
        Record("actual_thaw_complete",true);Record("final_awake_crew",v.GetCrewCount());
        if(v.GetCrewCount()!=4)throw new Exception("Crew count did not recover after thaw");
        GamePersistence.SaveGame("persistent",SaveName,SaveMode.OVERWRITE);
        Record("stage","procedure_finished");
        Record("note","Single-pod 1x probe only; multiple pods, depletion/settings, high warp and thermal behavior still unqualified.");
    }
}
