using System;

namespace RoxxParking
{
    public enum GatePhase { Closed, Opening, Open, Closing }

    // Shared by the moving gate and deterministic timing/occupancy checks.
    public sealed class GateCycle
    {
        public GatePhase Phase { get; private set; }
        public float Openness { get; private set; }
        private readonly float closedSeconds,openSeconds;
        private float elapsed;
        public float SecondsRemaining => Math.Max(0,(Phase==GatePhase.Closed?closedSeconds:openSeconds)-elapsed);
        public GateCycle(float closedSeconds,float openSeconds)
        {
            if(float.IsNaN(closedSeconds)||float.IsInfinity(closedSeconds)||closedSeconds<=0 || float.IsNaN(openSeconds)||float.IsInfinity(openSeconds)||openSeconds<=0)
                throw new ArgumentOutOfRangeException("Gate durations must be finite and positive");
            this.closedSeconds=closedSeconds;this.openSeconds=openSeconds;
        }
        public void Tick(float dt,bool occupied)
        {
            if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<=0)return;
            switch(Phase)
            {
                case GatePhase.Closed:
                    elapsed+=dt;if(elapsed>=closedSeconds){Phase=GatePhase.Opening;elapsed=0;}break;
                case GatePhase.Opening:
                    Openness=Math.Min(1,Openness+dt/1.2f);if(Openness>=1){Phase=GatePhase.Open;elapsed=0;}break;
                case GatePhase.Open:
                    elapsed+=dt;if(elapsed>=openSeconds&&!occupied){Phase=GatePhase.Closing;elapsed=0;}break;
                case GatePhase.Closing:
                    if(occupied){Phase=GatePhase.Opening;break;}
                    Openness=Math.Max(0,Openness-dt/1.2f);if(Openness<=0){Phase=GatePhase.Closed;elapsed=0;}break;
            }
        }
    }

    // Pure rules shared by gameplay and the small executable self-check.
    public static class ParkingRules
    {
        public static bool PaintUnlocked(int index,int trialStars) => index>=0 && index<6 && (index<4 || trialStars>=(index==4?6:15));
        public const float HoldSeconds = 1.6f;
        public const float MaximumSpeed = 0.35f;
        public const float MaximumAngle = 12f;
        public const float CarHalfWidth = 0.98f;
        public const float CarHalfLength = 2.25f;

        public static bool MinimumSpeedReached(float metresPerSecond, float minimumKph)
            => Finite(metresPerSecond) && Finite(minimumKph) && minimumKph >= 0 && Math.Abs(metresPerSecond) * 3.6f >= minimumKph;

        public static bool RouteReached(float dx, float dz, float radius, int requiredGear, int gear, float forwardSpeed)
        {
            if (!Finite(dx) || !Finite(dz) || !Finite(radius) || !Finite(forwardSpeed) || radius <= 0) return false;
            return dx * dx + dz * dz <= radius * radius
                && (requiredGear == 0 || (gear == requiredGear && forwardSpeed * requiredGear > 0.05f));
        }

        public static int Medal(float elapsed, float goldTime)
        {
            if (!Finite(elapsed) || !Finite(goldTime) || elapsed < 0 || goldTime <= 0) return 0;
            return elapsed <= goldTime ? 3 : elapsed <= goldTime * 1.5f ? 2 : 1;
        }

        public static bool Fits(float localX, float localZ, float headingDegrees,
            float bayWidth, float bayLength, float speed)
        {
            if (!Finite(localX) || !Finite(localZ) || !Finite(headingDegrees) || !Finite(speed)
                || !Finite(bayWidth) || !Finite(bayLength) || bayWidth <= 0 || bayLength <= 0)
                return false;
            float angle = Math.Abs(headingDegrees % 180f);
            angle = Math.Min(angle, 180f - angle);
            if (angle > MaximumAngle || Math.Abs(speed) > MaximumSpeed) return false;
            double radians = angle * Math.PI / 180.0;
            double extentX = CarHalfWidth * Math.Cos(radians) + CarHalfLength * Math.Sin(radians);
            double extentZ = CarHalfLength * Math.Cos(radians) + CarHalfWidth * Math.Sin(radians);
            return Math.Abs(localX) + extentX <= bayWidth * 0.5
                && Math.Abs(localZ) + extentZ <= bayLength * 0.5;
        }

        // New parking rule: mission completes when all four wheels are inside
        // the bay at the same time, while the car is effectively stopped. This
        // replaces the old "perfect alignment" rule for the final park: Fits()
        // above required the whole rotated body inside the bay and a heading
        // within 12 degrees. WheelsInsideBay has no heading check of its own;
        // RoxxGame still applies a heading tolerance for reverse bays (see
        // ReverseBayHeadingTolerance), because without it a car driven forward
        // into the bay and then switched to R would also win a reverse park.
        // Speed is still capped so driving through the bay cannot win: the
        // 1.6 s hold in RoxxGame only accumulates while this stays true.
        public const float ReverseBayHeadingTolerance = 30f;

        public static bool WheelsInsideBay(float[] wheelX, float[] wheelZ, float bayWidth, float bayLength, float speed)
        {
            if (wheelX == null || wheelZ == null || wheelX.Length != wheelZ.Length || wheelX.Length < 4
                || !Finite(speed) || Math.Abs(speed) > MaximumSpeed
                || !Finite(bayWidth) || !Finite(bayLength) || bayWidth <= 0 || bayLength <= 0)
                return false;
            for (int i = 0; i < wheelX.Length; i++)
            {
                if (!Finite(wheelX[i]) || !Finite(wheelZ[i])) return false;
                if (Math.Abs(wheelX[i]) > bayWidth * 0.5 || Math.Abs(wheelZ[i]) > bayLength * 0.5) return false;
            }
            return true;
        }

        public static float Accumulate(float current, bool eligible, float deltaTime)
        {
            if (!eligible || !Finite(current) || !Finite(deltaTime) || deltaTime < 0) return 0;
            return Math.Min(HoldSeconds, Math.Max(0, current) + deltaTime);
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
