using System;
using UnityEngine;

namespace RoxxParking
{
    public sealed class VehicleController : MonoBehaviour
    {
        public const float ForwardTopSpeed = 100f / 3.6f;
        public const float ReverseTopSpeed = 18f / 3.6f;
        // Stability / braking tuning. ForwardTopSpeed is intentionally unchanged:
        // the Century sprint course has a 90+ km/h sensor, so the fix must make
        // high speed safe to steer and quick to stop, not cap the car below it.
        // Wheelbase is ~2.82 m (front axle +1.40 m, rear axle -1.42 m).
        private const float Wheelbase = 2.82f;
        // Lateral acceleration the steering is allowed to demand. Above this the
        // tall SUV used to grip, load the outside tyres and roll instead of turn.
        private const float MaxLateralAcceleration = 8f;
        // Strong, arcade-style brake decel so a held brake stops the car promptly
        // instead of the old ~4.6 s / ~64 m stop from 100 km/h. At crawling speed
        // a held brake now stops the car outright instead of letting it creep.
        private const float BrakeDeceleration = 22f;
        private const float BrakeTorque = 9000f;
        public WheelCollider[] wheels;
        public Transform[] wheelVisuals;
        public Transform[] wheelHubs;
        public Transform steeringVisual;
        public Action Crashed;
        public bool DrivingEnabled { get; private set; }
        public int Gear { get; set; } = 1;
        public float Speed => body == null ? 0 : body.linearVelocity.magnitude;
        public float Steering { get; private set; }
        public float Throttle { get; private set; }
        public bool Braking { get; private set; }
        public float WheelSpeed { get; private set; }
        private Rigidbody body;
        private float inputSteering;
        private float inputThrottle;
        private bool inputBrake;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.maxLinearVelocity = ForwardTopSpeed;
            body.linearDamping = .015f;
        }

        public void SetInput(float steering, float throttle, bool brake)
        {
            inputSteering = Mathf.Clamp(steering, -1, 1);
            inputThrottle = Mathf.Clamp01(throttle);
            inputBrake = brake;
        }

        public void SetDriving(bool enabled)
        {
            DrivingEnabled = enabled;
            SetInput(0, 0, false);
            if (!enabled && body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        private void FixedUpdate()
        {
            if (wheels == null) return;
            float steerTarget = DrivingEnabled ? inputSteering : 0;
            // Slow the steering response itself at speed: a sudden full lock at
            // 60-100 km/h was a big part of the rollover, not just the final angle.
            float steerRate = Mathf.Lerp(3.5f, 1.4f, Mathf.InverseLerp(8f, ForwardTopSpeed, Speed));
            Steering = Mathf.MoveTowards(Steering, steerTarget, Time.fixedDeltaTime * steerRate);
            Throttle = DrivingEnabled ? inputThrottle : 0;
            Braking = !DrivingEnabled || inputBrake;
            float forwardSpeed = Vector3.Dot(body.linearVelocity, transform.forward);
            bool changingDirection = forwardSpeed * Gear < -0.5f;
            float limit = Gear < 0 && !changingDirection ? ReverseTopSpeed : ForwardTopSpeed;
            body.maxLinearVelocity = limit;
            float motor = Speed >= limit ? 0 : Throttle * Gear * 680f * Mathf.Lerp(1f, .65f, Speed / ForwardTopSpeed);
            float brake = Braking || changingDirection ? BrakeTorque : Throttle < 0.01f ? 110f : 0;
            float steeringLock = Mathf.Lerp(34f, 4f, Mathf.InverseLerp(8f, ForwardTopSpeed, Speed));
            // Cap the steering angle so lateral acceleration stays near
            // MaxLateralAcceleration: steer = atan(a * wheelbase / v^2). At parking
            // speeds (below ~21 km/h) this cap is above 34 deg, so slow parking
            // turns and the 4.2 m minimum turning radius are untouched. At 60 km/h
            // it drops to ~4.6 deg and at 100 km/h to ~1.7 deg, so the car must be
            // slowed before a real turn instead of rolling through it.
            if (Speed > 0.5f)
            {
                float lateralLock = Mathf.Atan(MaxLateralAcceleration * Wheelbase / (Speed * Speed)) * Mathf.Rad2Deg;
                steeringLock = Mathf.Min(steeringLock, Mathf.Max(lateralLock, 1.5f));
            }
            for (int i = 0; i < wheels.Length; i++)
            {
                wheels[i].steerAngle = i < 2 ? Steering * steeringLock : 0;
                wheels[i].motorTorque = brake > 500 ? 0 : motor;
                wheels[i].brakeTorque = brake;
            }
            if (Braking)
            {
                // WheelCollider brake torque alone is capped by tyre friction, so
                // add a direct, predictable stop. This is what makes the brake
                // feel immediate rather than a long coast-down.
                float currentSpeed = body.linearVelocity.magnitude;
                if (currentSpeed <= 0.5f)
                    body.linearVelocity = Vector3.zero;
                else
                    body.linearVelocity -= body.linearVelocity.normalized * Mathf.Min(currentSpeed, BrakeDeceleration * Time.fixedDeltaTime);
            }
            // Anti-roll: damp rotation around the forward (roll) axis only, so a
            // lateral jolt cannot build into a roll. Yaw (turning) is untouched.
            Vector3 localAngular = transform.InverseTransformDirection(body.angularVelocity);
            localAngular.z *= 1f - Mathf.Min(0.5f, 8f * Time.fixedDeltaTime);
            body.angularVelocity = transform.TransformDirection(localAngular);
            WheelSpeed = wheels.Length == 0 ? 0 : wheels[0].rpm;
        }

        private void LateUpdate()
        {
            if (wheels == null || wheelVisuals == null) return;
            for (int i = 0; i < Mathf.Min(wheels.Length, wheelVisuals.Length); i++)
            {
                wheels[i].GetWorldPose(out Vector3 position, out Quaternion rotation);
                wheelVisuals[i].SetPositionAndRotation(position, rotation);
                if (wheelHubs != null && i < wheelHubs.Length && wheelHubs[i] != null)
                    wheelHubs[i].SetPositionAndRotation(position, transform.rotation * Quaternion.Euler(0, wheels[i].steerAngle, 0));
            }
            if (steeringVisual != null)
                steeringVisual.localRotation = Quaternion.Euler(0, 0, -Steering * 120);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (DrivingEnabled && collision.collider.GetComponentInParent<CourseObstacle>() != null)
                Crashed?.Invoke();
        }
    }

    public sealed class CourseObstacle : MonoBehaviour { }
}
