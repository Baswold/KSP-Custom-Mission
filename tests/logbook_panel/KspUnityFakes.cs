using System;
using System.Collections.Generic;
using MegaCrew.Logbook;

namespace UnityEngine
{
public class MonoBehaviour { }
public class GUISkin { public GUIStyle label=new GUIStyle();public GUIStyle box=new GUIStyle(); }
public class GUIStyle { public bool wordWrap;public GUIStyle(){}public GUIStyle(GUIStyle other){wordWrap=other.wordWrap;} }
public struct Vector2 { public float x,y;public Vector2(float a,float b){x=a;y=b;} }
public struct Rect { public float x,y,width,height;public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;}public bool Contains(Vector2 p){return p.x>=x&&p.y>=y&&p.x<=x+width&&p.y<=y+height;} }
public sealed class GUILayoutOption { }
public enum KeyCode { L,LeftControl,RightControl,LeftAlt,RightAlt }
public static class Mathf { public static float Max(float a,float b){return Math.Max(a,b);}public static float Min(float a,float b){return Math.Min(a,b);}public static float Clamp(float v,float a,float b){return Math.Max(a,Math.Min(b,v));} }
public static class Screen { public static int width=1280,height=800; }
public static class Input { public static Vector2 mousePosition;public static bool GetKeyDown(KeyCode k){return false;}public static bool GetKey(KeyCode k){return false;} }
public static class GUI
{
    public static GUISkin skin=new GUISkin();public static bool enabled=true;public static string FocusedName="";public static int FocusCalls;
    public static bool Button(Rect r,string t){return false;}
    public static Rect Window(int id,Rect r,Action<int> draw,string title){draw(id);return r;}
    public static void DragWindow(Rect r){}public static void FocusControl(string n){FocusCalls++;FocusedName=n??"";}public static string GetNameOfFocusedControl(){return FocusedName;}public static void SetNextControlName(string n){}
}
public static class GUILayout
{
    public static GUILayoutOption Height(float n){return new GUILayoutOption();}public static GUILayoutOption Width(float n){return new GUILayoutOption();}
    public static GUILayoutOption ExpandWidth(bool b){return new GUILayoutOption();}public static GUILayoutOption MinWidth(float n){return new GUILayoutOption();}
    public static void BeginHorizontal(params GUILayoutOption[] o){}public static void EndHorizontal(){}
    public static void BeginVertical(GUIStyle s,params GUILayoutOption[] o){}public static void EndVertical(){}
    public static void BeginArea(Rect r){}public static void EndArea(){}
    public static void Label(string s,params GUILayoutOption[] o){}public static void Label(string s,GUIStyle style){}
    public static bool Button(string s,params GUILayoutOption[] o){return false;}
    public static string TextArea(string s,params GUILayoutOption[] o){return s;}
    public static string TextArea(string s,int maxLength,params GUILayoutOption[] o){return s.Length>maxLength?s.Substring(0,maxLength):s;}
    public static Vector2 BeginScrollView(Vector2 p,params GUILayoutOption[] o){return p;}
    public static Vector2 BeginScrollView(Vector2 p,bool h,bool v,params GUILayoutOption[] o){return p;}
    public static void EndScrollView(){}public static void Space(float n){}
}
}

public enum GameScenes { TRACKSTATION=6,SPACECENTER=5,FLIGHT=7 }
public sealed class KSPAddon:Attribute { public enum Startup { Flight }public KSPAddon(Startup s,bool once){} }
public abstract class ScenarioModule { public virtual void OnLoad(ConfigNode n){}public virtual void OnSave(ConfigNode n){} }
public sealed class ConfigNode
{
    public readonly string name;public readonly ValueList values=new ValueList();public readonly NodeList nodes=new NodeList();
    public ConfigNode():this(""){}public ConfigNode(string n){name=n;}
    public int CountValues{get{return values.Count;}}public int CountNodes{get{return nodes.Count;}}
    public void AddValue(string k,object v){values.Add(new Value(k,Convert.ToString(v,System.Globalization.CultureInfo.InvariantCulture)));}
    public string GetValue(string k){return values.GetValue(k);}public ConfigNode AddNode(string n){var c=new ConfigNode(n);nodes.Add(c);return c;}
    public ConfigNode[] GetNodes(string n){var a=new List<ConfigNode>();foreach(var c in nodes)if(c.name==n)a.Add(c);return a.ToArray();}
    public void ClearData(){values.Clear();nodes.Clear();}public ConfigNode CreateCopy(){var c=new ConfigNode(name);CopyTo(c);return c;}
    public void CopyTo(ConfigNode target){foreach(Value v in values)target.values.Add(new Value(v.name,v.value));foreach(ConfigNode c in nodes)c.CopyTo(target.AddNode(c.name));}
    public sealed class Value{public readonly string name,value;public Value(string n,string v){name=n;value=v;}}
    public sealed class ValueList:List<Value>{public int CountByName(string n){int x=0;foreach(Value v in this)if(v.name==n)x++;return x;}public string GetValue(string n){foreach(Value v in this)if(v.name==n)return v.value;return null;}}
    public sealed class NodeList:List<ConfigNode>{}
}
public sealed class ProtoScenarioModule
{
    public string moduleName;public object moduleRef;public ConfigNode config;public int LoadCalls;
    public void Load(ScenarioRunner runner){LoadCalls++;var m=(MissionLogScenario)Activator.CreateInstance(typeof(MissionLogScenario));m.OnLoad(config);moduleRef=m;}
}
public sealed class ScenarioRunner { public static ScenarioRunner Instance=new ScenarioRunner(); }
public sealed class Game
{
    public List<ProtoScenarioModule> scenarios=new List<ProtoScenarioModule>();public int AddCalls;public bool ThrowOnAdd;
    public ProtoScenarioModule AddProtoScenarioModule(Type t,params GameScenes[] scenes)
    {
        AddCalls++;if(ThrowOnAdd)throw new InvalidOperationException("synthetic registration failure");
        var n=new ConfigNode("SCENARIO");n.AddValue("name",t.Name);n.AddValue("scene",String.Join(",",Array.ConvertAll(scenes,x=>((int)x).ToString())));
        var p=new ProtoScenarioModule{moduleName=t.Name,config=n};scenarios.Add(p);return p;
    }
}
public static class HighLogic
{
    public static GameScenes LoadedScene=GameScenes.FLIGHT;public static bool LoadedSceneIsFlight=true;
    public static Game CurrentGame;public static string SaveFolder="MegaCrewTest";public static UnityEngine.GUISkin Skin=new UnityEngine.GUISkin();
}
public static class FlightGlobals { public static bool ready=true;public static Vessel ActiveVessel; }
public static class FlightDriver { public static bool Pause; }
public static class TimeWarp { public static int CurrentRateIndex;public static float CurrentRate=1f; }
public static class Planetarium { public static double UniversalTime=100;public static double GetUniversalTime(){return UniversalTime;} }
public static class InputLockManager
{
    public static readonly HashSet<string> Locks=new HashSet<string>();public static readonly List<string> Removed=new List<string>();
    public static void SetControlLock(ControlTypes c,string id){Locks.Add(id);}public static void RemoveControlLock(string id){Removed.Add(id);Locks.Remove(id);}
}
[Flags] public enum ControlTypes { ALL_SHIP_CONTROLS=1 }
public sealed class CelestialBody { public string bodyName; }
public sealed class Orbit { public double PeA=100000,ApA=120000,period=5000; }
public sealed class ProtoCrewMember { public string name; }
public sealed class Vessel
{
    public Guid id=Guid.NewGuid();public string vesselName="Hermes";public CelestialBody mainBody=new CelestialBody{bodyName="Kerbin"};
    public bool isEVA,loaded=true,packed;public Orbit orbit=new Orbit();public List<Part> parts=new List<Part>();public List<ProtoCrewMember> crew=new List<ProtoCrewMember>();
    public List<ProtoCrewMember> GetVesselCrew(){return crew;}
}
public class PartModule { }
public sealed class Part { public List<PartResource> Resources=new List<PartResource>();public List<PartModule> Modules=new List<PartModule>(); }
public sealed class PartResource { public string resourceName;public double amount,maxAmount;public bool flowState; }
public sealed class ScienceData { public string subjectID,title;public double dataAmount; }
public interface IScienceDataContainer { ScienceData[] GetData(); }

namespace MegaCrew
{
    public static class CryoBridge
    {
        public static readonly Dictionary<string,bool?> Frozen=new Dictionary<string,bool?>();
        public static bool TryFrozen(string name,out bool value){bool? x;if(!Frozen.TryGetValue(name,out x)||!x.HasValue){value=false;return false;}value=x.Value;return true;}
    }
    public static class MissionConsole
    {
        public static bool Allowed=true;public static int Calls;
        public static bool CanPerformDuty(string name,Guid vesselId,out string reason)
        {Calls++;reason=Allowed?"":"Crew member is not fit for duty.";return Allowed&&name=="Basil Kerman"&&vesselId!=Guid.Empty;}
    }
}
