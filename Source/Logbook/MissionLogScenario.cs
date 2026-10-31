using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MegaCrew.Logbook
{
    // Registered in the current Game only when the first valid log capture is
    // appended. It uses KSP's scenario save, never an external file.
    public sealed class MissionLogScenario : ScenarioModule
    {
        const string EntryNode="ENTRY";
        static readonly string[] EntryKeys={"id","ut","vesselId","vesselName","bodyName","author","kind","observation","note"};
        static readonly UTF8Encoding StrictUtf8=new UTF8Encoding(false,true);
        ConfigNode preservedLoadNode;
        string nativeName,nativeScene;
        public MissionLogBook Book { get; private set; }
        public string LoadError { get; private set; }

        public MissionLogScenario(){Book=new MissionLogBook();LoadError="";}

        public bool TryAppend(MissionLogEntry entry,out string reason)
        {
            if(preservedLoadNode!=null){reason=LoadError.Length==0?"Saved log data is read-only after a rejected load.":LoadError;return false;}
            return Book.TryAppend(entry,out reason);
        }

        void Reject(ConfigNode node,string reason)
        {
            Book=new MissionLogBook();LoadError=reason;preservedLoadNode=node==null?new ConfigNode():node.CreateCopy();
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            Book=new MissionLogBook();LoadError="";preservedLoadNode=null;nativeName=null;nativeScene=null;
            if(node==null){Reject(node,"Missing scenario node; original data preserved.");return;}
            try
            {
                if(!ReadMetadata(node,"name",typeof(MissionLogScenario).Name,out nativeName)||
                    !ReadScene(node,out nativeScene))
                {Reject(node,"Invalid native scenario metadata; original data preserved.");return;}

                int schemaCount=node.values.CountByName("schema");
                int entryCount=node.GetNodes(EntryNode).Length;
                foreach(ConfigNode.Value value in node.values)
                    if(value.name!="name"&&value.name!="scene"&&value.name!="schema")
                    {Reject(node,"Unknown scenario data; original data preserved.");return;}
                foreach(ConfigNode child in node.nodes)
                    if(child.name!=EntryNode){Reject(node,"Unknown scenario node; original data preserved.");return;}

                if(schemaCount==0&&entryCount==0)
                {
                    if(node.CountValues!=(nativeName==null?0:1)+(nativeScene==null?0:1)||node.CountNodes!=0)
                        Reject(node,"Unexpected data in empty log scenario; original data preserved.");
                    return;
                }
                if(schemaCount!=1||node.values.CountByName("schema")!=1||node.GetValue("schema")!="1"||entryCount>MissionLogBook.MaxEntries)
                {Reject(node,"Unsupported or malformed mission log schema; original data preserved.");return;}
                if(node.CountValues!=schemaCount+(nativeName==null?0:1)+(nativeScene==null?0:1))
                {Reject(node,"Duplicate or unknown scenario values; original data preserved.");return;}

                var loaded=new MissionLogBook();
                foreach(ConfigNode child in node.GetNodes(EntryNode))
                {
                    MissionLogEntry entry;
                    string reason;
                    if(!ReadEntry(child,out entry,out reason)||!loaded.TryAppend(entry,out reason))
                    {Reject(node,"Invalid mission log entry ("+reason+"); original data preserved.");return;}
                }
                Book=loaded;
            }
            catch(Exception e)
            {
                Reject(node,"Malformed mission log data ("+e.GetType().Name+"); original data preserved.");
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            if(preservedLoadNode!=null){node.ClearData();preservedLoadNode.CopyTo(node);return;}
            node.ClearData();
            if(nativeName!=null)node.AddValue("name",nativeName);
            if(nativeScene!=null)node.AddValue("scene",nativeScene);
            node.AddValue("schema",1);
            foreach(MissionLogEntry entry in Book.Entries)
            {
                ConfigNode child=node.AddNode(EntryNode);
                child.AddValue("id",entry.Id.ToString("D"));
                child.AddValue("ut",entry.UT.ToString("R",CultureInfo.InvariantCulture));
                child.AddValue("vesselId",entry.VesselId.ToString("D"));
                child.AddValue("vesselName",Encode(entry.VesselName));
                child.AddValue("bodyName",Encode(entry.BodyName));
                child.AddValue("author",Encode(entry.Author));
                child.AddValue("kind",entry.Kind);
                child.AddValue("observation",Encode(entry.Observation));
                child.AddValue("note",Encode(entry.Note));
            }
        }

        static bool ReadMetadata(ConfigNode node,string key,string expected,out string value)
        {
            int count=node.values.CountByName(key);value=null;
            if(count>1)return false;
            if(count==0)return true;
            value=node.GetValue(key);
            return value==expected;
        }

        static bool ReadScene(ConfigNode node,out string value)
        {
            value=null;int count=node.values.CountByName("scene");if(count>1)return false;
            if(count==0)return true;
            value=node.GetValue("scene");
            if(string.IsNullOrEmpty(value)||value.Length>128)return false;
            for(int i=0;i<value.Length;i++)if(!(char.IsDigit(value[i])||value[i]==','||value[i]==' '))return false;
            return true;
        }

        static bool ReadEntry(ConfigNode node,out MissionLogEntry entry,out string reason)
        {
            entry=null;reason="malformed field set";
            if(node.CountNodes!=0||node.CountValues!=EntryKeys.Length)return false;
            foreach(string key in EntryKeys)if(node.values.CountByName(key)!=1)return false;
            foreach(ConfigNode.Value value in node.values)
                if(Array.IndexOf(EntryKeys,value.name)<0)return false;
            Guid id,vesselId;double ut;
            string rawId=node.GetValue("id"),rawVessel=node.GetValue("vesselId"),rawUt=node.GetValue("ut");
            if(rawId==null||rawId.Length!=36||rawVessel==null||rawVessel.Length!=36||rawUt==null||rawUt.Length>32||
                !Guid.TryParseExact(rawId,"D",out id)||id==Guid.Empty||id.ToString("D")!=rawId||
                !Guid.TryParseExact(rawVessel,"D",out vesselId)||vesselId==Guid.Empty||vesselId.ToString("D")!=rawVessel||
                !double.TryParse(rawUt,NumberStyles.Float,CultureInfo.InvariantCulture,out ut)||!CampaignTiming.Finite(ut)||ut<0||
                ut.ToString("R",CultureInfo.InvariantCulture)!=rawUt)
            {reason="invalid ID or UT";return false;}
            string vesselName,bodyName,author,observation,note;
            if(!Decode(node.GetValue("vesselName"),MissionLogBook.MaxNameChars,false,out vesselName)||
                !Decode(node.GetValue("bodyName"),MissionLogBook.MaxShortTextChars,false,out bodyName)||
                !Decode(node.GetValue("author"),MissionLogBook.MaxShortTextChars,false,out author)||
                !Decode(node.GetValue("observation"),MissionLogBook.MaxObservationChars,false,out observation)||
                !Decode(node.GetValue("note"),MissionLogBook.MaxNoteChars,true,out note))
            {reason="invalid or oversized UTF-8 text";return false;}
            string kind=node.GetValue("kind");
            if(kind==null||kind.Length>32){reason="invalid kind";return false;}
            entry=new MissionLogEntry(id,ut,vesselId,vesselName,bodyName,author,kind,observation,note);
            reason="";return true;
        }

        static string Encode(string value){return Convert.ToBase64String(StrictUtf8.GetBytes(value));}
        static bool Decode(string raw,int maxChars,bool allowEmpty,out string text)
        {
            text=null;
            if(raw==null||raw.Length>MissionLogBook.MaxEncodedChars(maxChars))return false;
            try
            {
                byte[] bytes=Convert.FromBase64String(raw);
                text=StrictUtf8.GetString(bytes);
                if(text.Length>maxChars||Convert.ToBase64String(bytes)!=raw)return false;
                bool multiline=maxChars==MissionLogBook.MaxObservationChars||maxChars==MissionLogBook.MaxNoteChars;
                return MissionLogBook.ValidText(text,maxChars,multiline,allowEmpty);
            }
            catch(FormatException){return false;}
            catch(DecoderFallbackException){return false;}
        }
    }
}
