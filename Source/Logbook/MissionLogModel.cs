using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace MegaCrew.Logbook
{
    // Author is a locally selected crew label, not an authenticated identity.
    public sealed class MissionLogEntry
    {
        public Guid Id { get; }
        public double UT { get; }
        public Guid VesselId { get; }
        public string VesselName { get; }
        public string BodyName { get; }
        public string Author { get; }
        public string Kind { get; }
        public string Observation { get; }
        public string Note { get; }

        public MissionLogEntry(Guid id, double ut, Guid vesselId, string vesselName, string bodyName,
            string author, string kind, string observation, string note)
        {
            Id=id;UT=ut;VesselId=vesselId;VesselName=vesselName;BodyName=bodyName;
            Author=author;Kind=kind;Observation=observation;Note=note;
        }
    }

    public sealed class MissionLogBook
    {
        public const int MaxEntries=256;
        public const int MaxNameChars=256;
        public const int MaxShortTextChars=128;
        public const int MaxObservationChars=8192;
        public const int MaxNoteChars=1024;

        static readonly UTF8Encoding StrictUtf8=new UTF8Encoding(false,true);
        readonly List<MissionLogEntry> entries=new List<MissionLogEntry>();
        readonly ReadOnlyCollection<MissionLogEntry> view;
        public ReadOnlyCollection<MissionLogEntry> Entries { get { return view; } }

        public MissionLogBook(){view=entries.AsReadOnly();}

        public bool TryAppend(MissionLogEntry entry,out string reason)
        {
            reason="";
            if(entry==null){reason="A log entry is required.";return false;}
            if(entry.Id==Guid.Empty||entry.VesselId==Guid.Empty){reason="Entry and vessel IDs must be nonempty.";return false;}
            if(!CampaignTiming.Finite(entry.UT)||entry.UT<0){reason="Entry UT must be finite and nonnegative.";return false;}
            if(entries.Count>=MaxEntries){reason="Mission log is full; entries are never evicted.";return false;}
            for(int i=0;i<entries.Count;i++)
            {
                if(entries[i].Id==entry.Id){reason="Entry ID already exists.";return false;}
            }
            if(entries.Count>0&&entry.UT<entries[entries.Count-1].UT)
            {reason="Entry UT cannot precede the last recorded entry.";return false;}
            if(!ValidText(entry.VesselName,MaxNameChars,false)||!ValidText(entry.BodyName,MaxShortTextChars,false)||
                !ValidText(entry.Author,MaxShortTextChars,false)||!ValidKind(entry.Kind)||
                !ValidText(entry.Observation,MaxObservationChars,true)||!ValidText(entry.Note,MaxNoteChars,true,true))
            {reason="Entry text or kind is invalid or exceeds its field limit.";return false;}
            entries.Add(entry);return true;
        }

        public static bool ValidKind(string kind)
        {return kind=="Orbit"||kind=="Supplies"||kind=="RetainedScience"||kind=="Note";}

        public static bool ValidText(string text,int maxChars,bool multiline,bool allowEmpty=false)
        {
            if(text==null||(!allowEmpty&&string.IsNullOrWhiteSpace(text))||text.Length>maxChars)return false;
            try{StrictUtf8.GetByteCount(text);}catch(EncoderFallbackException){return false;}
            for(int i=0;i<text.Length;i++)
            {
                char c=text[i];
                if(char.IsControl(c)&&!(multiline&&(c=='\r'||c=='\n'||c=='\t')))return false;
            }
            return true;
        }

        internal static int MaxEncodedChars(int maxTextChars)
        {return 4*((maxTextChars*4+2)/3);}
    }
}
