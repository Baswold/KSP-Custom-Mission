using System;
using MegaCrew.Navigation;
class MissionControlMathTests
{
    static void Yes(bool v){if(!v)throw new Exception("assertion failed");}
    static void Near(double a,double b,double tol){if(Math.Abs(a-b)>tol)throw new Exception(a+" != "+b);}
    static void Reject(Action a){try{a();}catch(ArgumentOutOfRangeException){return;}throw new Exception("Expected rejection");}
    static void Main()
    {
        Near(MissionControlMath.RoundTripSeconds(299792458,299792458),2,1e-12);
        Near(MissionControlMath.RoundTripSeconds(0,299792458),1,1e-12);
        Reject(()=>MissionControlMath.RoundTripSeconds(-1,1));
        Reject(()=>MissionControlMath.RoundTripSeconds(double.NaN,1));
        const double mu=3.5316e12, body=600000, r=750000, altitude=30000;
        double delta=MissionControlMath.CircularApoapsisBurn(mu,body,r,altitude);
        Yes(delta<0);
        // Independently reconstruct orbit elements from energy and angular momentum.
        double speed=Math.Sqrt(mu/r)+delta, energy=speed*speed/2-mu/r;
        double a=-mu/(2*energy), h=r*speed, e=Math.Sqrt(1+2*energy*h*h/(mu*mu));
        Near(a*(1-e),body+altitude,1e-6);
        Near(a*(1+e),r,1e-6);
        Reject(()=>MissionControlMath.CircularApoapsisBurn(mu,body,r,150000));
        Reject(()=>MissionControlMath.CircularApoapsisBurn(mu,body,r,-1));
        Reject(()=>MissionControlMath.CircularApoapsisBurn(0,body,r,0));
        Reject(()=>MissionControlMath.CircularApoapsisBurn(mu,body,double.PositiveInfinity,0));
        Console.WriteLine("PASS: light time, invalid inputs and independently reconstructed two-body apsides.");
    }
}
