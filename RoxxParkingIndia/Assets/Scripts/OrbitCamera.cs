using UnityEngine;
using UnityEngine.EventSystems;

namespace RoxxParking
{
    public sealed class OrbitCamera : MonoBehaviour
    {
        public Transform target;
        public bool ControlsEnabled = true;
        public bool Showcase;
        public float Sensitivity { get; private set; } = 1;
        private void Awake() => Sensitivity = ValidSensitivity(PlayerPrefs.GetFloat("cameraSensitivity", 1));
        private static float ValidSensitivity(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 1 : Mathf.Clamp(value, .25f, 2);
        public void SetSensitivity(float value)
        {
            Sensitivity = ValidSensitivity(value);
            PlayerPrefs.SetFloat("cameraSensitivity", Sensitivity);
        }
        private float yaw = 28;
        private float pitch = 27;
        private float distance = 8.6f;
        private int dragFinger = -1;
        private bool mouseDragging;
        private Vector2 lastMouse;
        private Vector3 smoothFocus, focusVelocity;
        private float heading, headingVelocity, orbitYaw, yawVelocity, orbitPitch, pitchVelocity;
        private float cameraDistance, distanceVelocity;
        private bool initialized;
        private Transform trackedTarget;
        private Rigidbody trackedBody;
        private float fovVelocity;

        public void ResetView(bool snap = false)
        {
            yaw = 28; pitch = 27; distance = 8.6f;
            dragFinger = -1; mouseDragging = false;
            if (snap) initialized = false;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            if(trackedTarget!=target){trackedTarget=target;trackedBody=target.GetComponent<Rigidbody>();}
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0) return;
            if (ControlsEnabled)
            {
                if (Input.touchCount == 0)
                {
                    Vector2 mouse = Input.mousePosition;
                    if (Input.GetMouseButtonDown(1) && !OverUI()) { mouseDragging = true; lastMouse = mouse; }
                    if (mouseDragging && Input.GetMouseButton(1)) { Drag(mouse - lastMouse); lastMouse = mouse; }
                    if (!Input.GetMouseButton(1)) mouseDragging = false;
                    if (!OverUI()) distance = Mathf.Clamp(distance - Input.mouseScrollDelta.y, 5.5f, 13);
                }
                else mouseDragging = false;
                bool trackedFingerFound = false;
                foreach (Touch touch in Input.touches)
                {
                    if (touch.phase == TouchPhase.Began && dragFinger < 0
                        && touch.position.y > Screen.height * 0.35f
                        && !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId)))
                        dragFinger = touch.fingerId;
                    if (touch.fingerId != dragFinger) continue;
                    trackedFingerFound = true;
                    if (touch.phase == TouchPhase.Moved) Drag(touch.deltaPosition);
                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) dragFinger = -1;
                }
                if (!trackedFingerFound) dragFinger = -1;
            }
            else { dragFinger = -1; mouseDragging = false; }

            float fast=Showcase || trackedBody==null?0:Mathf.InverseLerp(8,VehicleController.ForwardTopSpeed,trackedBody.linearVelocity.magnitude);
            float desiredDistance=distance+fast*2.4f;
            Camera lens=GetComponent<Camera>();
            lens.fieldOfView=Mathf.SmoothDamp(lens.fieldOfView,53+fast*10,ref fovVelocity,.6f,Mathf.Infinity,dt);
            Vector3 focus = target.position + Vector3.up * 0.9f + target.forward*(fast*1.6f);
            if (Showcase) focus -= target.right * 2.3f;
            if (!initialized)
            {
                smoothFocus = focus; focusVelocity = Vector3.zero;
                heading = target.eulerAngles.y; orbitYaw = yaw; orbitPitch = pitch; cameraDistance = distance;
                headingVelocity = yawVelocity = pitchVelocity = distanceVelocity = 0; initialized = true;
            }
            smoothFocus = Vector3.SmoothDamp(smoothFocus, focus, ref focusVelocity, 0.1f, Mathf.Infinity, dt);
            heading = Mathf.SmoothDampAngle(heading, target.eulerAngles.y, ref headingVelocity, 0.24f, Mathf.Infinity, dt);
            orbitYaw = Mathf.SmoothDampAngle(orbitYaw, yaw, ref yawVelocity, 0.08f, Mathf.Infinity, dt);
            orbitPitch = Mathf.SmoothDamp(orbitPitch, pitch, ref pitchVelocity, 0.08f, Mathf.Infinity, dt);
            Vector3 direction = -(Quaternion.Euler(orbitPitch, heading + orbitYaw, 0) * Vector3.forward);
            float clearance = Mathf.Max(desiredDistance, cameraDistance);
            if (Physics.SphereCast(smoothFocus, 0.25f, direction, out RaycastHit hit, clearance,
                1 << 8, QueryTriggerInteraction.Ignore)) clearance = Mathf.Max(0.35f, hit.distance - 0.1f);
            // Pull in immediately at walls; damp only the return to avoid smoothing through an obstacle.
            cameraDistance = Mathf.SmoothDamp(cameraDistance, Mathf.Min(desiredDistance, clearance), ref distanceVelocity, 0.3f, Mathf.Infinity, dt);
            if (cameraDistance > clearance) { cameraDistance = clearance; distanceVelocity = 0; }
            transform.position = smoothFocus + direction * cameraDistance;
            transform.LookAt(smoothFocus);
        }

        private void Drag(Vector2 pixels)
        {
            float scale = 150f * Sensitivity / Mathf.Max(1, Screen.height);
            yaw = Mathf.Repeat(yaw + pixels.x * scale + 180, 360) - 180;
            pitch = Mathf.Clamp(pitch - pixels.y * scale, 15, 65);
        }

        private static bool OverUI() => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
