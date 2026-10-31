using System;
using System.Collections.Generic;
using MegaCrew.Logbook;

class MissionLogTests
{
    static int checks;
    static void Yes(bool value,string why){checks++;if(!value)throw new Exception(why);}
    static void Eq(string a,string b,string why){Yes(a==b,why+" ("+a+" != "+b+")");}
    static MissionLogEntry Entry(double ut=10,string note="routine")
    {return new MissionLogEntry(Guid.NewGuid(),ut,Guid.NewGuid(),"Hermes", "Kerbin","Basil Kerman","Note","Recorded cabin status",note);}
    static void RejectAppend(MissionLogBook book,MissionLogEntry entry){string reason;Yes(!book.TryAppend(entry,out reason)&&reason.Length>0,"Invalid append must return a reason");}
    static string Snapshot(ConfigNode n)
    {
        string s="{"+n.name+"}";
        foreach(ConfigNode.Value v in n.values)s+="V"+v.name.Length+":"+v.name+"="+v.value.Length+":"+v.value;
        foreach(ConfigNode child in n.nodes)s+="N"+Snapshot(child);
        return s;
    }
    static ConfigNode Save(MissionLogScenario scenario)
    {var n=new ConfigNode("SCENARIO");scenario.OnSave(n);return n;}
    static MissionLogScenario Load(ConfigNode n){var s=new MissionLogScenario();s.OnLoad(n);return s;}
    static void Main()
    {
        var book=new MissionLogBook();var first=Entry(10,"line one\n{brace}= value");string reason;
        Yes(book.TryAppend(first,out reason),"valid entry append");
        Yes(book.Entries.Count==1&&object.ReferenceEquals(book.Entries[0],first),"entries exposed read-only in append order");
        bool collectionReadOnly=false;try{((IList<MissionLogEntry>)book.Entries).Add(Entry(10));}catch(NotSupportedException){collectionReadOnly=true;}
        Yes(collectionReadOnly&&book.Entries.Count==1,"read-only entries collection cannot be externally mutated");
        RejectAppend(book,first);RejectAppend(book,Entry(9));
        RejectAppend(book,new MissionLogEntry(Guid.Empty,11,Guid.NewGuid(),"Hermes","Kerbin","Basil","Note","ok","ok"));
        RejectAppend(book,new MissionLogEntry(Guid.NewGuid(),double.NaN,Guid.NewGuid(),"Hermes","Kerbin","Basil","Note","ok","ok"));
        RejectAppend(book,new MissionLogEntry(Guid.NewGuid(),11,Guid.Empty,"Hermes","Kerbin","Basil","Note","ok","ok"));
        RejectAppend(book,new MissionLogEntry(Guid.NewGuid(),11,Guid.NewGuid(),new string('x',257),"Kerbin","Basil","Note","ok","ok"));
        RejectAppend(book,new MissionLogEntry(Guid.NewGuid(),11,Guid.NewGuid(),"Hermes","Kerbin","Basil","Unknown","ok","ok"));
        RejectAppend(book,new MissionLogEntry(Guid.NewGuid(),11,Guid.NewGuid(),"Hermes","Kerbin","Basil","Note",new string('x',8193),"ok"));
        RejectAppend(book,new MissionLogEntry(Guid.NewGuid(),11,Guid.NewGuid(),"Hermes","Kerbin","Basil","Note","ok","bad\0note"));
        Yes(book.TryAppend(new MissionLogEntry(Guid.NewGuid(),10,Guid.NewGuid(),"Hermes","Kerbin","Basil","Orbit","Snapshot with no note",""),out reason),"empty optional note is allowed");

        var scenario=new MissionLogScenario();var native=new ConfigNode("SCENARIO");native.AddValue("name","MissionLogScenario");native.AddValue("scene","7,5,6");scenario.OnLoad(native);
        Yes(scenario.LoadError==""&&scenario.Book.Entries.Count==0,"empty native scenario accepted");
        Yes(scenario.TryAppend(first,out reason),"scenario append delegates to model");
        var saved=Save(scenario);Yes(saved.GetValue("name")=="MissionLogScenario"&&saved.GetValue("scene")=="7,5,6","native name/scene metadata retained");
        var loaded=Load(saved);Yes(loaded.LoadError==""&&loaded.Book.Entries.Count==1,"schema round-trip");
        var actual=loaded.Book.Entries[0];
        Yes(actual.Id==first.Id&&actual.VesselId==first.VesselId&&actual.UT==10,"identity and invariant UT round-trip");
        Eq(actual.Note,first.Note,"base64 text round-trips multiline braces and equals");Eq(actual.Observation,first.Observation,"observation round-trip");
        Yes(Snapshot(saved)==Snapshot(Save(loaded)),"canonical persistence round-trip is stable");

        var invalidCases=new List<ConfigNode>();
        var unknown=saved.CreateCopy();unknown.AddValue("futureField","keep");invalidCases.Add(unknown);
        var future=saved.CreateCopy();future.values.SetValue("schema","99",false);invalidCases.Add(future);
        var extraNode=saved.CreateCopy();extraNode.AddNode("FUTURE").AddValue("x","y");invalidCases.Add(extraNode);
        var duplicateField=saved.CreateCopy();duplicateField.AddValue("schema",1);invalidCases.Add(duplicateField);
        var duplicateEntryField=saved.CreateCopy();duplicateEntryField.GetNodes("ENTRY")[0].AddValue("note","again");invalidCases.Add(duplicateEntryField);
        var badBase64=saved.CreateCopy();badBase64.GetNodes("ENTRY")[0].values.SetValue("note","%%%",false);invalidCases.Add(badBase64);
        var badUtf8=saved.CreateCopy();badUtf8.GetNodes("ENTRY")[0].values.SetValue("note","/w==",false);invalidCases.Add(badUtf8);
        var unknownEntry=saved.CreateCopy();unknownEntry.GetNodes("ENTRY")[0].AddValue("future","keep");invalidCases.Add(unknownEntry);
        var oversizedNumber=saved.CreateCopy();oversizedNumber.GetNodes("ENTRY")[0].values.SetValue("ut",new string('9',10000),false);invalidCases.Add(oversizedNumber);
        var duplicateId=saved.CreateCopy();var duplicateChild=duplicateId.AddNode("ENTRY");saved.GetNodes("ENTRY")[0].CopyTo(duplicateChild);invalidCases.Add(duplicateId);
        var decreasing=saved.CreateCopy();var later=Entry(20);var orderedScenario=new MissionLogScenario();orderedScenario.OnLoad(native);Yes(orderedScenario.TryAppend(first,out reason)&&orderedScenario.TryAppend(later,out reason),"prepare ordered entries");var two=Save(orderedScenario);two.GetNodes("ENTRY")[1].values.SetValue("ut","5",false);invalidCases.Add(two);
        foreach(var bad in invalidCases)
        {
            string before=Snapshot(bad);var rejected=Load(bad);
            Yes(rejected.LoadError.Length>0&&rejected.Book.Entries.Count==0,"malformed/future data rejected");
            RejectScenarioAppend(rejected,Entry(30));
            var preserved=Save(rejected);Yes(Snapshot(preserved)==before,"rejected node preserved exactly on save");
        }

        var overlong=new ConfigNode("SCENARIO");overlong.AddValue("schema",1);var overEntry=overlong.AddNode("ENTRY");
        foreach(string key in new[]{"id","vesselId","ut","vesselName","bodyName","author","kind","observation","note"})overEntry.AddValue(key,key=="observation"?new string('A',MissionLogBook.MaxEncodedChars(MissionLogBook.MaxObservationChars)+1):"x");
        var over=Load(overlong);Yes(over.LoadError.Length>0,"encoded input rejected by raw length before base64 decode");Yes(Snapshot(Save(over))==Snapshot(overlong),"oversized raw data preserved");

        var full=new MissionLogBook();for(int i=0;i<MissionLogBook.MaxEntries;i++)Yes(full.TryAppend(Entry(i),out reason),"append under cap");
        Yes(full.Entries.Count==MissionLogBook.MaxEntries,"cap reached");RejectAppend(full,Entry(MissionLogBook.MaxEntries));
        Yes(full.Entries.Count==MissionLogBook.MaxEntries&&full.Entries[0].UT==0,"capacity rejection never evicts existing data");
        var cappedScenario=new MissionLogScenario();cappedScenario.OnLoad(native);
        for(int i=0;i<MissionLogBook.MaxEntries;i++)Yes(cappedScenario.TryAppend(Entry(i),out reason),"scenario append under cap");
        var overCap=Save(cappedScenario);var extra=overCap.AddNode("ENTRY");overCap.GetNodes("ENTRY")[0].CopyTo(extra);
        var rejectedOverCap=Load(overCap);Yes(rejectedOverCap.LoadError.Length>0&&rejectedOverCap.Book.Entries.Count==0,"over-cap loaded scenario rejected without truncation");
        Yes(Snapshot(Save(rejectedOverCap))==Snapshot(overCap),"over-cap raw scenario remains intact");
        var duplicateLoaded=Save(loaded);var duplicateLoadedNode=duplicateLoaded.AddNode("ENTRY");saved.GetNodes("ENTRY")[0].CopyTo(duplicateLoadedNode);
        var duplicateScenario=Load(duplicateLoaded);Yes(duplicateScenario.LoadError.Length>0,"duplicate IDs in loaded entries rejected");
        Console.WriteLine("PASS: "+checks+" mission-log assertions; bounded immutable append, duplicate/cap/UT order, native scenario metadata, canonical UTF-8 base64 round-trip, strict malformed/future rejection and exact raw-node preservation.");
    }
    static void RejectScenarioAppend(MissionLogScenario scenario,MissionLogEntry entry)
    {string reason;Yes(!scenario.TryAppend(entry,out reason)&&reason.Length>0,"rejected load refuses mutation");}
}

public class ScenarioModule{public virtual void OnLoad(ConfigNode n){}public virtual void OnSave(ConfigNode n){}}
public sealed class ConfigNode
{
    public readonly string name;public readonly ValueList values=new ValueList();public readonly NodeList nodes=new NodeList();
    public ConfigNode():this(""){}public ConfigNode(string nodeName){name=nodeName;}
    public int CountValues{get{return values.Count;}}public int CountNodes{get{return nodes.Count;}}
    public void AddValue(string key,object value){values.Add(new Value(key,Convert.ToString(value,System.Globalization.CultureInfo.InvariantCulture)));}
    public string GetValue(string key){return values.GetValue(key);}
    public ConfigNode AddNode(string nodeName){var n=new ConfigNode(nodeName);nodes.Add(n);return n;}
    public ConfigNode[] GetNodes(string nodeName){var found=new List<ConfigNode>();foreach(var n in nodes)if(n.name==nodeName)found.Add(n);return found.ToArray();}
    public void ClearData(){values.Clear();nodes.Clear();}
    public ConfigNode CreateCopy(){var n=new ConfigNode(name);CopyTo(n);return n;}
    public void CopyTo(ConfigNode target){foreach(Value v in values)target.values.Add(new Value(v.name,v.value));foreach(ConfigNode child in nodes)child.CopyTo(target.AddNode(child.name));}
    public sealed class Value{public readonly string name,value;public Value(string n,string v){name=n;value=v;}}
    public sealed class ValueList:List<Value>{public int CountByName(string n){int c=0;foreach(Value v in this)if(v.name==n)c++;return c;}public string GetValue(string n){foreach(Value v in this)if(v.name==n)return v.value;return null;}public bool SetValue(string n,string value,bool create){for(int i=0;i<Count;i++)if(this[i].name==n){this[i]=new Value(n,value);return true;}if(create){Add(new Value(n,value));return true;}return false;}}
    public sealed class NodeList:List<ConfigNode>{}
}
