using System;
using System.Collections;
using System.Collections.Generic;

[assembly: System.Reflection.AssemblyVersion("0.0.0.0")]

namespace DF
{
    public sealed class FrozenInfo { public Guid vesselID; public uint partID; }
    public sealed class DeepFreezeState
    {
        public IDictionary FrozenKerbals { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);
    }
    public static class DeepFreeze
    {
        public static bool APIReady = true;
        public static DeepFreezeState Instance = new DeepFreezeState();
    }
}

public sealed class DeepFreezerFake : PartModule
{
    public bool DFIIsFreezeActive { get; set; }
    public bool DFIIsThawActive { get; set; }
    public bool DFIcrewXferTOActive { get; set; }
    public bool DFIcrewXferFROMActive { get; set; }
    public int FreezeCalls { get; private set; }
    public void beginFreezeKerbal(ProtoCrewMember member) { FreezeCalls++; }
    public void beginThawKerbal(string name) { }
}
