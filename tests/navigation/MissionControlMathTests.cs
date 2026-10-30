using System;
using MegaCrew.Navigation;
class MissionControlMathTests
{
    static void Yes(bool v){if(!v)throw new Exception("assertion failed");}
    static void Near(double a,double b,double tol){if(Math.Abs(a-b)>tol)throw new Exception(a+" != "+b);}
    static void Reject(Action a){try{a();}catch(ArgumentOutOfRangeException){return;}throw new Exception("Expected rejection");}
    static Vector3d Acc(Vector3d p,double mu){return p*(-mu/(p.Magnitude*p.Magnitude*p.Magnitude));}
    static Vector3d Propagate(Vector3d p,Vector3d v,double time,double mu)
    {
        int steps=20000;double dt=time/steps;
        for(int i=0;i<steps;i++){
            var a1=Acc(p,mu);var k1=v;
            var a2=Acc(p+k1*(dt/2),mu);var k2=v+a1*(dt/2);
            var a3=Acc(p+k2*(dt/2),mu);var k3=v+a2*(dt/2);
            var a4=Acc(p+k3*dt,mu);var k4=v+a3*dt;
            p=p+(k1+k2*2+k3*2+k4)*(dt/6);v=v+(a1+a2*2+a3*2+a4)*(dt/6);
        }return p;
    }
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
        // Quarter-circle Kepler arc: analytic endpoints and velocities.
        double radius=700000, n=Math.Sqrt(mu/(radius*radius*radius)), vc=Math.Sqrt(mu/radius);
        var sol=Lambert.SolveShortWay(new Vector3d(radius,0,0),new Vector3d(0,radius,0),Math.PI/(2*n),mu);
        Near(sol.DepartureVelocity.X,0,1e-5);Near(sol.DepartureVelocity.Y,vc,1e-5);
        Near(sol.ArrivalVelocity.X,-vc,1e-5);Near(sol.ArrivalVelocity.Y,0,1e-5);Near(sol.TimeResidual,0,1e-6);
        var departure=new Vector3d(700000,0,0);var target=new Vector3d(450000,779422.8634059948,0);
        foreach(double duration in new[]{300.0, 1200.0, 3000.0}) {
            var eccentric=Lambert.SolveShortWay(departure,target,duration,mu);
            Yes((Propagate(departure,eccentric.DepartureVelocity,duration,mu)-target).Magnitude<0.1);
        }
        Reject(()=>Lambert.SolveShortWay(new Vector3d(radius,0,0),new Vector3d(radius,0,0),0,mu));
        Console.WriteLine("PASS: light time, periapsis reconstruction, circular Lambert and independent RK4 propagation.");
    }
}
