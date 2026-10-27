using System;
using RoxxParking;

public static class ParkingRulesSelfCheck
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    public static int Main()
    {
        Check(ParkingRules.Fits(0, 0, 0, 3.2f, 6.2f, 0), "Centered car should qualify");
        Check(ParkingRules.Fits(0, 0, 180, 3.2f, 6.2f, 0), "Reverse-facing parking is allowed");
        Check(ParkingRules.Fits(0, 0, -180, 3.2f, 6.2f, 0), "Negative heading wrap is equivalent");
        Check(!ParkingRules.Fits(0.8f, 0, 0, 3.2f, 6.2f, 0), "Car center inside is insufficient when body crosses edge");
        Check(!ParkingRules.Fits(0, 1, 0, 3.2f, 6.2f, 0), "Rear/front overhang must fit");
        Check(!ParkingRules.Fits(0, 0, 20, 3.2f, 6.2f, 0), "Misalignment must not win");
        Check(!ParkingRules.Fits(0, 0, 0, 3.2f, 6.2f, 0.5f), "Moving through bay must not win");
        Check(!ParkingRules.Fits(0, 0, 0, 3.2f, 6.2f, -0.5f), "Reverse speed has the same threshold");
        Check(!ParkingRules.Fits(0, 0, 0, -3.2f, 6.2f, 0), "Invalid bay must not qualify");
        Check(!ParkingRules.Fits(float.NaN, 0, 0, 3.2f, 6.2f, 0), "Invalid positions must fail closed");
        Check(!ParkingRules.Fits(0, 0, float.PositiveInfinity, 3.2f, 6.2f, 0), "Invalid headings must fail closed");
        float hold = 0;
        for (int i = 0; i < 81; i++) hold = ParkingRules.Accumulate(hold, true, 0.02f);
        Check(hold == ParkingRules.HoldSeconds, "Continuous eligible hold must complete");
        Check(ParkingRules.Accumulate(1.5f, false, 0.02f) == 0, "Leaving bay must reset hold");
        Check(ParkingRules.Accumulate(1, true, float.NaN) == 0, "Invalid time must not complete");
        Check(ParkingRules.RouteReached(1,1,2,0,1,0),"Inside an ordinary route point qualifies");
        Check(!ParkingRules.RouteReached(3,0,2,0,1,1),"Outside route point must not qualify");
        Check(!ParkingRules.RouteReached(0,0,2,-1,1,-1),"Wrong selected gear cannot clear reverse point");
        Check(!ParkingRules.RouteReached(0,0,2,-1,-1,0),"Changing gear while stationary is insufficient");
        Check(!ParkingRules.RouteReached(0,0,2,-1,-1,1),"Forward rolling cannot clear reverse point");
        Check(ParkingRules.RouteReached(0,0,2,-1,-1,-.2f),"Moving backward clears reverse point");
        Check(!ParkingRules.RouteReached(float.NaN,0,2,0,1,1),"Invalid route position fails closed");
        Check(ParkingRules.Medal(30,30)==3 && ParkingRules.Medal(45,30)==2 && ParkingRules.Medal(46,30)==1,"Medal boundaries");
        Check(ParkingRules.Medal(float.NaN,30)==0,"Invalid time cannot award medal");
        Check(ParkingRules.MinimumSpeedReached(25,90) && !ParkingRules.MinimumSpeedReached(24.9f,90),"Speed checkpoint must measure actual metres per second as km/h");
        Check(!ParkingRules.MinimumSpeedReached(float.NaN,90) && !ParkingRules.MinimumSpeedReached(25,-1),"Invalid speed checkpoints fail closed");
        var gate=new GateCycle(2,3);
        Check(gate.Phase==GatePhase.Closed && gate.Openness==0,"Gate starts closed");
        gate.Tick(2,false);gate.Tick(1.3f,false);
        Check(gate.Phase==GatePhase.Open && gate.Openness==1,"Gate opens after red interval");
        gate.Tick(5,true);Check(gate.Phase==GatePhase.Open,"Occupied crossing must stay open");
        gate.Tick(.02f,false);gate.Tick(.3f,false);float before=gate.Openness;
        gate.Tick(.02f,true);gate.Tick(.1f,true);
        Check(gate.Phase==GatePhase.Opening && gate.Openness>=before,"Closing beam reopens for occupied crossing");
        before=gate.Openness;gate.Tick(0,false);gate.Tick(float.NaN,false);
        Check(gate.Openness==before,"Paused/invalid tick cannot advance gate");
        Check(ParkingRules.PaintUnlocked(3,0)&&!ParkingRules.PaintUnlocked(4,5)&&ParkingRules.PaintUnlocked(4,6),"First trial paint threshold");
        Check(!ParkingRules.PaintUnlocked(5,14)&&ParkingRules.PaintUnlocked(5,15)&&!ParkingRules.PaintUnlocked(6,18),"Second paint threshold and invalid index");
        Console.WriteLine("PASS: parking footprint, heading, speed and continuous hold rules.");
        return 0;
    }
}
