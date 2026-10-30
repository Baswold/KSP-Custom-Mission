using System;
using System.Collections.Generic;
namespace MegaCrew.Navigation
{
    // Pure readiness state. A trusted external adapter must authenticate principals.
    public sealed class BurnApprovals
    {
        readonly Func<string,string,bool> authenticate;
        readonly Dictionary<string,string> approvals=new Dictionary<string,string>();
        string plan,request;double burn,requested,readyAt,expires;bool connected;
        public int Revision {get;private set;}
        public BurnApprovals(Func<string,string,bool> authenticatedRole){authenticate=authenticatedRole??throw new ArgumentNullException("authenticatedRole");}
        static bool Finite(double x){return !double.IsNaN(x)&&!double.IsInfinity(x);}
        void Clear(){approvals.Clear();request=null;}
        public void Connection(bool available){if(connected&&!available)Clear();connected=available;}
        public void Revise(string immutableFingerprint,double burnUT)
        {
            if(string.IsNullOrWhiteSpace(immutableFingerprint)||!Finite(burnUT))throw new ArgumentException("Invalid plan");
            plan=immutableFingerprint;burn=burnUT;Revision++;Clear();
        }
        public void Request(string requestId,double now,double roundTrip,double maxAge)
        {
            if(!connected||plan==null)throw new InvalidOperationException("Plan and connection required");
            if(string.IsNullOrWhiteSpace(requestId)||!Finite(now)||!Finite(roundTrip)||!Finite(maxAge)||roundTrip<0||maxAge<=0||roundTrip>maxAge||now>=burn||!Finite(now+maxAge))throw new ArgumentException("Invalid approval window");
            Clear();request=requestId;requested=now;readyAt=now+roundTrip;expires=now+maxAge;
        }
        public bool Approve(string principal,string role,string requestId,int revision,string fingerprint,double now)
        {
            if(plan!=null && fingerprint!=plan){Clear();return false;}
            if(!connected||request==null||!Finite(now)||now<readyAt||now>expires||now>=burn||requestId!=request||revision!=Revision||fingerprint!=plan||string.IsNullOrWhiteSpace(principal)||(role!="Hamish"&&role!="Basil"))return false;
            try{if(!authenticate(principal,role))return false;}catch{return false;}
            foreach(var item in approvals)if(item.Key!=role&&item.Value==principal)return false;
            approvals[role]=principal;return true;
        }
        public bool Ready(string liveFingerprint,double now)
        {
            string h,b;
            if(plan!=null && liveFingerprint!=plan){Clear();return false;}
            if(!connected||request==null||!Finite(now)||now<readyAt||now>expires||now>=burn||liveFingerprint!=plan||!approvals.TryGetValue("Hamish",out h)||!approvals.TryGetValue("Basil",out b)||h==b)return false;
            try{return authenticate(h,"Hamish")&&authenticate(b,"Basil");}catch{return false;}
        }
    }
}
