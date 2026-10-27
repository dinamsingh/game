using System;
using UnityEngine;

namespace RoxxParking
{
    public sealed class VehicleController : MonoBehaviour
    {
        public const float ForwardTopSpeed = 100f / 3.6f;
        public const float ReverseTopSpeed = 18f / 3.6f;
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
            Steering = Mathf.MoveTowards(Steering, steerTarget, Time.fixedDeltaTime * 3.5f);
            Throttle = DrivingEnabled ? inputThrottle : 0;
            Braking = !DrivingEnabled || inputBrake;
            float forwardSpeed = Vector3.Dot(body.linearVelocity, transform.forward);
            bool changingDirection = forwardSpeed * Gear < -0.5f;
            float limit = Gear < 0 && !changingDirection ? ReverseTopSpeed : ForwardTopSpeed;
            body.maxLinearVelocity = limit;
            float motor = Speed >= limit ? 0 : Throttle * Gear * 680f * Mathf.Lerp(1f, .65f, Speed / ForwardTopSpeed);
            float brake = Braking || changingDirection ? 3600f : Throttle < 0.01f ? 110f : 0;
            float steeringLock = Mathf.Lerp(34f, 4f, Mathf.InverseLerp(8f, ForwardTopSpeed, Speed));
            for (int i = 0; i < wheels.Length; i++)
            {
                wheels[i].steerAngle = i < 2 ? Steering * steeringLock : 0;
                wheels[i].motorTorque = brake > 500 ? 0 : motor;
                wheels[i].brakeTorque = brake;
            }
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
