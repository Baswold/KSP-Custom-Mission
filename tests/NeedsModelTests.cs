using System;
using MegaCrew;
class NeedsModelTests
{
    static void Equal(double a,double b){if(Math.Abs(a-b)>1e-7)throw new Exception("Expected "+b+", got "+a);}
    static void Yes(bool value){if(!value)throw new Exception("Condition failed");}
    static void Main()
    {
        var c=new CrewRecord("test","test");NeedsModel.Tick(c,3600,false);Equal(c.Hunger,100.0/6);Equal(c.Thirst,25);Equal(c.Fatigue,25);
        var split=new CrewRecord("test","test");for(int i=0;i<3600;i++)NeedsModel.Tick(split,1,false);Equal(c.Hunger,split.Hunger);Equal(c.Thirst,split.Thirst);Equal(c.Fatigue,split.Fatigue);
        NeedsModel.Tick(c,21600,false);Equal(c.Hunger,100);Equal(c.Thirst,100);Equal(c.Fatigue,100);
        c=new CrewRecord("test","test");c.Resting=true;c.Fatigue=100;NeedsModel.Tick(c,7200,false);Equal(c.Fatigue,0);Equal(c.Hunger,100.0/3);Equal(c.Thirst,50);
        c.Resting=true;c.Hunger=10;c.Thirst=20;c.Fatigue=100;NeedsModel.Tick(c,7200,true);Equal(c.Hunger,10);Equal(c.Thirst,20);Equal(c.Fatigue,0);Yes(!c.Resting);
        c.Hunger=89.9;c.Thirst=89.9;c.Fatigue=89.9;Yes(NeedsModel.Fit(c));c.Hunger=90;Yes(!NeedsModel.Fit(c));c.Hunger=0;c.Thirst=90;Yes(!NeedsModel.Fit(c));c.Thirst=0;c.Fatigue=90;Yes(!NeedsModel.Fit(c));c.Fatigue=0;c.Resting=true;Yes(!NeedsModel.Fit(c));Yes(!NeedsModel.Fit(null));
        Console.WriteLine("PASS: rates, frame partitioning, upper limits, ordinary sleep, frozen needs and duty thresholds.");
    }
}
