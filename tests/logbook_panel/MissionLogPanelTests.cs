using System;
using System.Collections.Generic;
using System.Reflection;
using MegaCrew.Logbook;

sealed class RecordingScienceContainer : PartModule, IScienceDataContainer
{
    public ScienceData[] Data=new ScienceData[0];public Action OnRead;public int Reads,MutatingCalls;
    public ScienceData[] GetData(){Reads++;if(OnRead!=null)OnRead();return Data;}
    public void DumpData(ScienceData data){MutatingCalls++;}public void ReturnData(ScienceData[] data){MutatingCalls++;}
    public void ReviewData(){MutatingCalls++;}public void TransmitData(ScienceData[] data,bool callback){MutatingCalls++;}
    public void RunExperiment(){MutatingCalls++;}public void ResetExperiment(){MutatingCalls++;}
}

static class MissionLogPanelTests
{
    static int checks;
    static void Yes(bool value,string message){checks++;if(!value)throw new Exception(message);}
    static void Equal(string actual,string expected,string message){Yes(actual==expected,message+"; got "+actual);}
    static void Set(object target,string name,object value){target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);}
    static object Invoke(object target,string name,params object[] args)
    {
        try{return target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,args);}
        catch(TargetInvocationException ex){throw ex.InnerException;}
    }
    static void Capture(MissionLogPanel panel,string kind){Invoke(panel,"Capture",kind);}
    static MissionLogScenario Scenario(Game game)
    {return (MissionLogScenario)game.scenarios.Find(p=>p.moduleName==typeof(MissionLogScenario).Name).moduleRef;}

    static void Reset()
    {
        HighLogic.LoadedScene=GameScenes.FLIGHT;HighLogic.LoadedSceneIsFlight=true;HighLogic.CurrentGame=new Game();HighLogic.SaveFolder="MegaCrew QA";
        FlightGlobals.ready=true;FlightGlobals.ActiveVessel=new Vessel();FlightDriver.Pause=false;TimeWarp.CurrentRateIndex=0;TimeWarp.CurrentRate=1f;Planetarium.UniversalTime=100;
        FlightGlobals.ActiveVessel.crew.Add(new ProtoCrewMember{name="Basil Kerman"});
        FlightGlobals.ActiveVessel.crew.Add(new ProtoCrewMember{name="Hamish Kerman"});
        MegaCrew.CryoBridge.Frozen.Clear();MegaCrew.CryoBridge.Frozen["Basil Kerman"]=false;MegaCrew.CryoBridge.Frozen["Hamish Kerman"]=false;
        MegaCrew.MissionConsole.Allowed=true;MegaCrew.MissionConsole.Calls=0;ScenarioRunner.Instance=new ScenarioRunner();
        UnityEngine.GUI.FocusedName="";UnityEngine.GUI.FocusCalls=0;InputLockManager.Locks.Clear();InputLockManager.Removed.Clear();
    }
    static MissionLogPanel NewPanel(string author="Basil Kerman")
    {var p=new MissionLogPanel();Invoke(p,"Update");Set(p,"selectedAuthor",author);return p;}
    static ProtoScenarioModule Pending(Game game)
    {var p=new ProtoScenarioModule{moduleName=typeof(MissionLogScenario).Name,config=new ConfigNode("SCENARIO")};p.config.AddValue("name",p.moduleName);p.config.AddValue("scene","7,5,6");game.scenarios.Add(p);return p;}

    static void Main()
    {
        Reset();var panel=NewPanel("");Set(panel,"visible",true);Invoke(panel,"OnGUI");
        Yes(HighLogic.CurrentGame.AddCalls==0&&HighLogic.CurrentGame.scenarios.Count==0,"opening/drawing the panel does not create persistent scenario state");

        Reset();panel=NewPanel();FlightGlobals.ActiveVessel=null;Capture(panel,"Orbit");
        Yes(HighLogic.CurrentGame.AddCalls==0,"missing active vessel refuses before scenario creation");
        var invalid=new Action[] {
            delegate{FlightDriver.Pause=true;},delegate{TimeWarp.CurrentRateIndex=1;},
            delegate{FlightGlobals.ActiveVessel.isEVA=true;},delegate{FlightGlobals.ActiveVessel.packed=true;},
            delegate{FlightGlobals.ActiveVessel.loaded=false;},delegate{FlightGlobals.ready=false;},
            delegate{HighLogic.LoadedScene=GameScenes.SPACECENTER;},delegate{HighLogic.SaveFolder="Practice";}
        };
        foreach(Action invalidate in invalid)
        {
            Reset();panel=NewPanel();invalidate();Capture(panel,"Orbit");
            Yes(HighLogic.CurrentGame.AddCalls==0&&HighLogic.CurrentGame.scenarios.Count==0,"paused/warp/EVA/packed/unloaded/non-flight/non-MegaCrew context refuses before persistence");
        }

        Reset();panel=NewPanel();Set(panel,"noteText",new string('n',MissionLogBook.MaxNoteChars+1));Capture(panel,"Orbit");
        Yes(HighLogic.CurrentGame.AddCalls==0,"oversized note is rejected before adding a scenario");
        Reset();panel=NewPanel("Ada Kerman");FlightGlobals.ActiveVessel.crew.Add(new ProtoCrewMember{name="Ada Kerman"});
        MegaCrew.CryoBridge.Frozen.Remove("Ada Kerman");
        object[] authorArgs={FlightGlobals.ActiveVessel,null};var authors=(List<string>)Invoke(panel,"AwakeAuthors",authorArgs);
        Yes(!authors.Contains("Ada Kerman"),"unknown CryoBridge awake state does not offer an author label");
        MegaCrew.MissionConsole.Allowed=false;Capture(panel,"Orbit");
        Yes(HighLogic.CurrentGame.AddCalls==0,"live readiness denial for unknown/unfit selected crew refuses before persistence");

        Reset();panel=NewPanel();Capture(panel,"Orbit");
        Yes(HighLogic.CurrentGame.AddCalls==1&&HighLogic.CurrentGame.scenarios.Count==1,"first valid capture creates one native scenario");
        Yes(HighLogic.CurrentGame.scenarios[0].LoadCalls==1,"new scenario goes through one native ProtoScenarioModule.Load registration");
        Yes(Scenario(HighLogic.CurrentGame).Book.Entries.Count==1,"valid capture appends its actual orbit observation");
        Equal(Scenario(HighLogic.CurrentGame).Book.Entries[0].Kind,"Orbit","capture kind recorded");
        Capture(panel,"Orbit");
        Yes(HighLogic.CurrentGame.AddCalls==1&&HighLogic.CurrentGame.scenarios.Count==1&&Scenario(HighLogic.CurrentGame).Book.Entries.Count==2&&HighLogic.CurrentGame.scenarios[0].LoadCalls==1,"subsequent capture reuses the loaded scenario without duplicate registration");

        Reset();panel=NewPanel();var pending=Pending(HighLogic.CurrentGame);ScenarioRunner.Instance=null;Capture(panel,"Orbit");
        Yes(HighLogic.CurrentGame.AddCalls==0&&HighLogic.CurrentGame.scenarios.Count==1&&pending.moduleRef==null,"pending native module is not duplicated or appended before registration");

        Reset();panel=NewPanel();Pending(HighLogic.CurrentGame);Pending(HighLogic.CurrentGame);Capture(panel,"Orbit");
        Yes(HighLogic.CurrentGame.AddCalls==0&&HighLogic.CurrentGame.scenarios.Count==2,"duplicate scenario matches deny without adding another module");
        Reset();panel=NewPanel();HighLogic.CurrentGame.scenarios.Add(new ProtoScenarioModule{moduleName=typeof(MissionLogScenario).Name,moduleRef=new object()});Capture(panel,"Orbit");
        Yes(HighLogic.CurrentGame.AddCalls==0&&HighLogic.CurrentGame.scenarios.Count==1,"wrong scenario moduleRef type denies without duplicate registration");

        Reset();panel=NewPanel();var bad=new MissionLogScenario();var badNode=new ConfigNode("SCENARIO");badNode.AddValue("name",typeof(MissionLogScenario).Name);badNode.AddValue("scene","7,5,6");badNode.AddValue("schema",99);bad.OnLoad(badNode);
        HighLogic.CurrentGame.scenarios.Add(new ProtoScenarioModule{moduleName=typeof(MissionLogScenario).Name,moduleRef=bad});Capture(panel,"Orbit");
        Yes(bad.LoadError.Length>0&&bad.Book.Entries.Count==0&&HighLogic.CurrentGame.AddCalls==0,"bad schema remains read-only at adapter boundary");

        Reset();panel=NewPanel();HighLogic.CurrentGame.ThrowOnAdd=true;Capture(panel,"Orbit");
        Yes(HighLogic.CurrentGame.AddCalls==1&&HighLogic.CurrentGame.scenarios.Count==0,"scenario registration exception does not leave a created module or append");
        Yes(((string)panel.GetType().GetField("status",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(panel)).Contains("registration failed"),"registration exception is reported to the panel");

        Reset();panel=NewPanel();var sciencePart1=new Part();var sciencePart2=new Part();var dataA=new ScienceData{subjectID="subject/log",title="Surface notes",dataAmount=2};
        var first=new RecordingScienceContainer{Data=new[]{dataA}};var second=new RecordingScienceContainer{Data=new[]{dataA,new ScienceData{subjectID="subject/log",title="Duplicate card",dataAmount=3}}};
        sciencePart1.Modules.Add(first);sciencePart2.Modules.Add(second);FlightGlobals.ActiveVessel.parts.Add(sciencePart1);FlightGlobals.ActiveVessel.parts.Add(sciencePart2);
        Capture(panel,"RetainedScience");var scienceText=Scenario(HighLogic.CurrentGame).Book.Entries[0].Observation;
        Yes(first.Reads==1&&second.Reads==1,"science inventory calls GetData exactly once per container");
        Yes(first.MutatingCalls==0&&second.MutatingCalls==0,"capture never calls science dump/return/review/transmit operations");
        Yes(scienceText.Contains("2 distinct stored cards")&&scienceText.Contains("1 exact subject IDs")&&scienceText.Contains("5 total retained data units"),"reference duplicate is counted once while distinct same-subject cards aggregate");
        Yes(dataA.dataAmount==2&&second.Data[1].dataAmount==3,"science cards remain unchanged");

        Reset();panel=NewPanel();var resourcesPart=new Part();
        resourcesPart.Resources.Add(new PartResource{resourceName="Food",amount=3,maxAmount=4,flowState=true});
        resourcesPart.Resources.Add(new PartResource{resourceName="Food",amount=7,maxAmount=12,flowState=false});
        resourcesPart.Resources.Add(new PartResource{resourceName="ElectricCharge",amount=5,maxAmount=10,flowState=false});
        FlightGlobals.ActiveVessel.parts.Add(resourcesPart);Capture(panel,"Supplies");
        var supply=Scenario(HighLogic.CurrentGame).Book.Entries[0].Observation;
        Yes(supply.Contains("Food: all 10 / 16; flow enabled 3 / 4"),"supply snapshot distinguishes all stores from flow-enabled stores");
        Yes(supply.Contains("ElectricCharge: all 5 / 10; flow enabled 0 / 0"),"non-flow resource remains in all-store totals");
        Yes(resourcesPart.Resources[0].amount==3&&resourcesPart.Resources[1].amount==7&&resourcesPart.Resources[2].amount==5,"resource capture is observational and does not change amounts");

        Reset();panel=NewPanel();var changing=new RecordingScienceContainer{Data=new[]{new ScienceData{subjectID="subject/log",title="status",dataAmount=1}}};
        changing.OnRead=delegate{FlightDriver.Pause=true;};var changingPart=new Part();changingPart.Modules.Add(changing);FlightGlobals.ActiveVessel.parts.Add(changingPart);
        Capture(panel,"RetainedScience");
        Yes(changing.Reads==1&&HighLogic.CurrentGame.AddCalls==0&&HighLogic.CurrentGame.scenarios.Count==0,"pause change during GetData invalidates capture before scenario creation");

        CheckReadInvalidation(delegate{HighLogic.CurrentGame=new Game();},"game change");
        CheckReadInvalidation(delegate{HighLogic.SaveFolder="MegaCrewDifferent";},"save change");
        CheckReadInvalidation(delegate{FlightGlobals.ActiveVessel=new Vessel();},"active vessel change");
        CheckReadInvalidation(delegate{FlightGlobals.ActiveVessel.mainBody=new CelestialBody{bodyName="Mun"};},"body change");
        CheckReadInvalidation(delegate{Planetarium.UniversalTime=99;},"backward UT change");

        Reset();panel=NewPanel();Set(panel,"visible",true);Set(panel,"noteFocused",true);Set(panel,"clearOwnedFocusPending",true);
        UnityEngine.GUI.FocusedName="MegaCrewMissionLogNote";Invoke(panel,"OnGUI");
        Yes(UnityEngine.GUI.FocusCalls==1&&UnityEngine.GUI.FocusedName==""&&!((bool)panel.GetType().GetField("noteFocused",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(panel)),"pending cleanup clears the hidden panel's own note focus");
        Reset();panel=NewPanel();Set(panel,"noteFocused",true);Set(panel,"clearOwnedFocusPending",true);UnityEngine.GUI.FocusedName="UnrelatedField";Invoke(panel,"OnGUI");
        Yes(UnityEngine.GUI.FocusCalls==0&&UnityEngine.GUI.FocusedName=="UnrelatedField","pending cleanup preserves unrelated field focus");

        Reset();panel=NewPanel();InputLockManager.SetControlLock(ControlTypes.ALL_SHIP_CONTROLS,"FreeIVA.ExternalLock");
        InputLockManager.SetControlLock(ControlTypes.ALL_SHIP_CONTROLS,"MegaCrew.MissionLogPanel");Invoke(panel,"OnApplicationFocus",false);
        Yes(InputLockManager.Locks.Contains("FreeIVA.ExternalLock")&&!InputLockManager.Locks.Contains("MegaCrew.MissionLogPanel"),"application blur releases only this panel's input lock");

        Console.WriteLine("PASS: "+checks+" mission-log panel boundary assertions; persistence lifecycle, context/readiness, native pending/duplicate handling, science read-only aggregation, resource flow totals, and capture revalidation. Fake GUI tests compile/dispatch only; no visual/runtime behavior is claimed.");
    }
    static void CheckReadInvalidation(Action change,string label)
    {
        Reset();var panel=NewPanel();var game=HighLogic.CurrentGame;
        var container=new RecordingScienceContainer{Data=new[]{new ScienceData{subjectID="subject/log",title="sample",dataAmount=1}}};
        container.OnRead=change;var part=new Part();part.Modules.Add(container);FlightGlobals.ActiveVessel.parts.Add(part);
        Capture(panel,"RetainedScience");
        Yes(container.Reads==1&&game.AddCalls==0&&game.scenarios.Count==0,"GetData "+label+" invalidates capture before persistence");
    }
}
