using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RoxxParking
{
    public sealed class RoxxGame : MonoBehaviour
    {
        public const int CampaignCourseCount=12;
        public static readonly Color[] PaintColors = { new Color(0.86f, 0.27f, 0.08f), new Color(0.85f, 0.85f, 0.79f), new Color(0.21f, 0.32f, 0.24f), new Color(0.18f, 0.20f, 0.21f),new Color(.06f,.16f,.29f),new Color(.57f,.43f,.22f) };
        public VehicleController Car { get; private set; }
        public int UnlockedLevel { get; private set; }
        public float ParkingProgress => parkingHold / ParkingRules.HoldSeconds;
        public int CourseCount => courses.Length;
        public int MaximumTrialStars => (CourseCount-CampaignCourseCount)*3;
        public bool HasWon => state == GameState.Won;
        public bool IsPreview { get; private set; }
        public bool IsTrial => currentLevel>=CampaignCourseCount;
        public bool IsNewBest { get; private set; }
        public float PreviousBest { get; private set; }
        public int NewTrialStars { get; private set; }
        public string NewPaint { get; private set; }
        public int TrialStars { get { int total=0;for(int i=CampaignCourseCount;i<CourseCount;i++)total+=Mathf.Clamp(BestMedal(i),0,3);return total; } }
        public bool IsPaintUnlocked(int index) => ParkingRules.PaintUnlocked(index,TrialStars);
        public string GateHint { get { foreach(var gate in gates)if(gate.NearApproach(Car.transform.position))return gate.Status;return ""; } }
        public CourseDefinition CurrentCourse => courses[currentLevel];
        public int RouteIndex { get; private set; }
        public int RouteCount => CurrentCourse.route.Length;
        public float Elapsed { get; private set; }
        public int EarnedMedal { get; private set; }
        public float BestTime(int index) => PlayerPrefs.GetFloat("bestTime." + courses[index].name, 0);
        public int BestMedal(int index) => PlayerPrefs.GetInt("medal." + courses[index].name, 0);
        public Vector3 ObjectivePosition => RouteIndex < RouteCount ? new Vector3(CurrentCourse.route[RouteIndex].x,0,CurrentCourse.route[RouteIndex].z) : bay.position;
        public string ObjectiveText => RouteIndex < RouteCount ? "ROUTE " + (RouteIndex+1) + "/" + RouteCount + (CurrentCourse.route[RouteIndex].minKph>0 ? " / "+CurrentCourse.route[RouteIndex].minKph.ToString("0")+"+ KM/H THROUGH SENSOR" : CurrentCourse.route[RouteIndex].gear < 0 ? " / REVERSE / NOSE ALONG ARROW" : CurrentCourse.route[RouteIndex].align ? " / DRIVE / NOSE ALONG ARROW" : " / FOLLOW AMBER POINT") : CurrentCourse.reversePark ? "REVERSE INTO BAY / NOSE ALONG ARROW" : "PARK FULLY INSIDE THE BAY";
        public bool ThrottleHeld, BrakeHeld, LeftHeld, RightHeld;
        public float WheelInput;
        private CourseDefinition[] courses;
        private GameHud hud;
        private OrbitCamera orbit;
        public float CameraSensitivity => orbit.Sensitivity;
        public bool SteeringOnRight { get; private set; }
        public void SetCameraSensitivity(float value) => orbit.SetSensitivity(value);
        public void SetSteeringSide(bool right)
        {
            ClearInput(); Car.SetInput(0,0,false);
            SteeringOnRight=right;
            PlayerPrefs.SetInt("steeringOnRight",right?1:0);
            hud.ApplyControlLayout();
        }
        private VisualFinish graphics;
        public bool HighGraphics => graphics.ContactShading;
        public void ToggleGraphics()
        {
            bool high=!HighGraphics;graphics.SetQuality(high);
            QualitySettings.shadows=high?ShadowQuality.All:ShadowQuality.HardOnly;
            QualitySettings.shadowDistance=high?40:25;
            QualitySettings.shadowResolution=high?ShadowResolution.High:ShadowResolution.Medium;
            hud.ShowPause();
        }
        private Transform courseRoot, bay;
        private int currentLevel;
        private float parkingHold;
        private Transform[] routeMarkers;
        private TimedGate[] gates=new TimedGate[0];
        private AudioSource feedback;
        private AudioClip gateSound, finishSound;
        private float gateFlash;
        private bool buttons;
        private enum GameState { Menu, Driving, Paused, Won, Failed }
        private GameState state;

        private void Awake()
        {
            Application.targetFrameRate = 30;
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 2;
            QualitySettings.shadowDistance = 35;
            QualitySettings.shadowCascades = 2;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
#if UNITY_WEBGL && !UNITY_EDITOR
            QualitySettings.shadows=ShadowQuality.All;
            QualitySettings.shadowDistance=40;
            QualitySettings.shadowCascades=1;
            QualitySettings.antiAliasing=0;
#endif
            Time.fixedDeltaTime = 0.02f;
            Time.timeScale = 1;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            TextAsset data = Resources.Load<TextAsset>("courses");
            if (data == null) throw new InvalidOperationException("Missing Resources/courses.json");
            courses = JsonUtility.FromJson<CourseList>(data.text).courses;
            if (courses == null || courses.Length == 0) throw new InvalidOperationException("No training courses configured");
            UnlockedLevel = Mathf.Clamp(PlayerPrefs.GetInt("unlockedLevel", 0), 0, CampaignCourseCount - 1);
            EnvironmentArt.Lighting();
            GameObject sun = new GameObject("Sun"); Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.32f; light.color = new Color(1, 0.96f, 0.89f);
            light.shadows = LightShadows.Soft; light.shadowBias=.025f;light.shadowNormalBias=.18f;
            sun.transform.rotation = Quaternion.Euler(38, -36, 0); RenderSettings.sun = light;
            GameObject cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); cam.tag = "MainCamera";
            Camera camera = cam.GetComponent<Camera>(); camera.backgroundColor = RenderSettings.fogColor;
            camera.clearFlags = CameraClearFlags.Skybox; camera.fieldOfView = 53; camera.farClipPlane = 230;camera.nearClipPlane=.12f;
            camera.allowHDR=SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf);
            graphics=cam.AddComponent<VisualFinish>();
            orbit = cam.AddComponent<OrbitCamera>();
            if (EventSystem.current == null) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            feedback = gameObject.AddComponent<AudioSource>(); feedback.playOnAwake = false; feedback.volume = .18f;
            gateSound = Tone("Route cleared", .12f, 660);
            finishSound = Tone("Course complete", .8f, 440);
            int paint=PlayerPrefs.GetInt("paint",0);if(!IsPaintUnlocked(paint))paint=0;
            Car = WorldBuilder.CreateCar(PaintColors[paint]);
            Car.Crashed += Crash;
            orbit.target = Car.transform;
            SteeringOnRight=PlayerPrefs.GetInt("steeringOnRight",0)==1;
            hud = new GameHud(this);
            LoadCourse(0);
            Menu();
        }

        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.R) && (state==GameState.Failed || state==GameState.Driving))Restart();
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (state == GameState.Driving) Pause(); else if (state == GameState.Paused) Resume(); else Menu();
            }
            if (state == GameState.Driving)
            {
                float steer = buttons ? (RightHeld ? 1 : 0) - (LeftHeld ? 1 : 0) : WheelInput;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) steer = -1;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) steer = 1;
                bool forward = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
                bool reverse = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
                if(CurrentCourse.reverseOnly)Car.Gear=-1;
                else if (forward) Car.Gear = 1; else if (reverse) Car.Gear = -1;
                if (Input.GetKeyDown(KeyCode.G)) ToggleGear();
                Car.SetInput(steer, ThrottleHeld || forward || reverse ? 1 : 0, BrakeHeld || Input.GetKey(KeyCode.Space));
                if (Car.transform.position.y < -3) Crash();
            }
            hud.Update();
            gateFlash = Mathf.Max(0, gateFlash - Time.deltaTime);
        }

        public bool RouteJustCleared => gateFlash > 0;

        private void FixedUpdate()
        {
            if (state != GameState.Driving) return;
            CourseDefinition course = courses[currentLevel];
            Elapsed += Time.fixedDeltaTime;
            if (RouteIndex < RouteCount)
            {
                RoutePoint point = course.route[RouteIndex];
                Vector3 position = Car.transform.position;
                float forwardSpeed = Vector3.Dot(Car.GetComponent<Rigidbody>().linearVelocity, Car.transform.forward);
                if ((!(point.align || point.gear<0) || Mathf.Abs(Mathf.DeltaAngle(point.yaw,Car.transform.eulerAngles.y)) <= 30)
                    && ParkingRules.MinimumSpeedReached(Car.Speed,point.minKph)
                    && ParkingRules.RouteReached(position.x-point.x,position.z-point.z,point.radius,point.gear,Car.Gear,forwardSpeed))
                {
                    routeMarkers[RouteIndex].gameObject.SetActive(false); RouteIndex++; gateFlash = 1.4f;
                    if (RouteIndex < RouteCount) routeMarkers[RouteIndex].gameObject.SetActive(true);
                    feedback.PlayOneShot(gateSound);
                }
            }
            Vector3 local = bay.InverseTransformPoint(Car.transform.position);
            float angle = Mathf.DeltaAngle(bay.eulerAngles.y, Car.transform.eulerAngles.y);
            // Wheels-inside-bay rule: all four wheels inside the bay at once,
            // while stopped, completes the park after the 1.6 s hold below.
            // No perfect-alignment heading is required for a normal bay. For a
            // reverse bay the car must still be in R and roughly facing the bay
            // direction (30 deg, not the old 12 deg), otherwise driving in
            // forward and switching to R at the end would also win.
            float[] wheelX = null, wheelZ = null;
            if (Car.wheels != null && Car.wheels.Length >= 4)
            {
                wheelX = new float[Car.wheels.Length]; wheelZ = new float[Car.wheels.Length];
                for (int i = 0; i < Car.wheels.Length; i++)
                {
                    Vector3 wheelLocal = bay.InverseTransformPoint(Car.wheels[i].transform.position);
                    wheelX[i] = wheelLocal.x; wheelZ[i] = wheelLocal.z;
                }
            }
            bool fits = RouteIndex == RouteCount
                && (!course.reversePark || (Car.Gear == -1 && Mathf.Abs(angle) <= ParkingRules.ReverseBayHeadingTolerance))
                && Mathf.Abs(local.y) < 2 && Vector3.Dot(Car.transform.up, Vector3.up) > 0.95f
                && ParkingRules.WheelsInsideBay(wheelX, wheelZ, course.bayWidth, course.bayLength, Car.Speed);
            parkingHold = ParkingRules.Accumulate(parkingHold, fits, Time.fixedDeltaTime);
            if (parkingHold < ParkingRules.HoldSeconds) return;
            state = GameState.Won; Car.SetDriving(false); ClearInput();
            EarnedMedal = ParkingRules.Medal(Elapsed,course.goldTime);
            PreviousBest=BestTime(currentLevel);IsNewBest=!IsPreview && (PreviousBest<=0 || Elapsed<PreviousBest);
            int starsBefore=TrialStars;
            feedback.PlayOneShot(finishSound);
            if(!IsPreview)
            {
                string timeKey = "bestTime." + course.name, medalKey = "medal." + course.name;
                if (PlayerPrefs.GetFloat(timeKey, float.MaxValue) > Elapsed) PlayerPrefs.SetFloat(timeKey,Elapsed);
                PlayerPrefs.SetInt(medalKey,Mathf.Max(PlayerPrefs.GetInt(medalKey,0),EarnedMedal));
                if(!IsTrial)UnlockedLevel = Mathf.Max(UnlockedLevel, Mathf.Min(currentLevel + 1, CampaignCourseCount - 1));
                PlayerPrefs.SetInt("unlockedLevel", UnlockedLevel); PlayerPrefs.Save();
            }
            NewTrialStars=Mathf.Max(0,TrialStars-starsBefore);
            NewPaint=starsBefore<6&&TrialStars>=6?"MIDNIGHT BLUE":starsBefore<15&&TrialStars>=15?"DESERT GOLD":"";
            hud.ShowResult(true, currentLevel == (IsTrial?courses.Length:CampaignCourseCount) - 1);
        }

        private void LoadCourse(int index)
        {
            bool rebuild=courseRoot==null || currentLevel!=index;
            if (rebuild && courseRoot != null) { courseRoot.gameObject.SetActive(false); Destroy(courseRoot.gameObject); }
            currentLevel = index; parkingHold = 0; RouteIndex = 0; Elapsed = 0; EarnedMedal = 0; gateFlash = 0;
            IsNewBest=false;PreviousBest=0;NewTrialStars=0;NewPaint="";
            CourseDefinition course = courses[index];
            if(rebuild)
            {
            courseRoot = WorldBuilder.CreateCourse(course, out bay);
            gates=courseRoot.GetComponentsInChildren<TimedGate>();
            routeMarkers = new Transform[course.route.Length];
            for (int i=0;i<routeMarkers.Length;i++)
            {
                RoutePoint point = course.route[i];
                Transform marker = new GameObject("Route point " + (i+1)).transform;
                marker.SetParent(courseRoot,false); marker.position=new Vector3(point.x,.04f,point.z);
                Material amber = WorldBuilder.Material("Route amber",new Color(.98f,.65f,.12f),.05f);
                // Broken ground ring keeps the road surface visible and the next destination readable.
                for(int segment=0;segment<12;segment++)
                {
                    float a=segment*Mathf.PI/6;
                    GameObject dash=WorldBuilder.Box("Route ring",marker,new Vector3(Mathf.Sin(a)*point.radius,0,Mathf.Cos(a)*point.radius),new Vector3(.55f,.025f,.10f),amber);
                    dash.transform.localRotation=Quaternion.Euler(0,segment*30,0);
                }
                if(point.gear<0 || point.align)
                {
                    marker.rotation=Quaternion.Euler(0,point.yaw,0);
                    WorldBuilder.Box("Reverse heading",marker,Vector3.zero,new Vector3(.1f,.025f,1.4f),amber);
                    for(int side=-1;side<=1;side+=2)
                    {
                        GameObject tip=WorldBuilder.Box("Heading arrow",marker,new Vector3(side*.2f,0,.45f),new Vector3(.10f,.025f,.6f),amber);
                        tip.transform.localRotation=Quaternion.Euler(0,-side*45,0);
                    }
                }
                routeMarkers[i]=marker;marker.gameObject.SetActive(i==0);
            }
            }
            else
            {
                for(int i=0;i<routeMarkers.Length;i++)routeMarkers[i].gameObject.SetActive(i==0);
                foreach(var gate in gates)gate.ResetCycle();
            }
            Rigidbody rb = Car.GetComponent<Rigidbody>();
            rb.position = new Vector3(course.spawnX, 0.62f, course.spawnZ);
            rb.rotation = Quaternion.Euler(0, course.spawnYaw, 0);
            rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
            Car.Gear = course.reverseOnly?-1:1; ClearInput(); orbit.ResetView(true);
            Physics.SyncTransforms();
        }

        public string CourseName(int index) => courses[index].name;
        public CourseDefinition CourseAt(int index) => courses[index];
        public void StartLevel(int index, bool preview = false)
        {
            if (index < 0 || index >= courses.Length || (preview ? index != 4 : index < CampaignCourseCount && index > UnlockedLevel)) return;
            IsPreview=preview;
            Time.timeScale = 1; Car.SetDriving(false); LoadCourse(index);
            state = GameState.Driving; Car.SetDriving(true); orbit.ControlsEnabled = true; orbit.Showcase = false;
            hud.ShowDriving(index, courses[index].name);
        }
        public void Restart() => StartLevel(currentLevel,IsPreview);
        public void NextLevel() => StartLevel(Mathf.Min(currentLevel + 1, courses.Length - 1));
        public void Menu()
        {
            PlayerPrefs.Save();
            IsPreview=false;
            Time.timeScale = 1; state = GameState.Menu; Car.SetDriving(false); ClearInput();
            orbit.ControlsEnabled = false; orbit.Showcase = true; hud.ShowMenu();
        }
        public void Pause()
        {
            if (state != GameState.Driving) return;
            state = GameState.Paused; ClearInput(); Car.SetInput(0, 0, true);
            Time.timeScale = 0; orbit.ControlsEnabled = false; hud.ShowPause();
        }
        public void Resume()
        {
            if (state != GameState.Paused) return;
            PlayerPrefs.Save();
            ClearInput(); Time.timeScale = 1; state = GameState.Driving; orbit.ControlsEnabled = true;
            hud.ShowDriving(currentLevel, courses[currentLevel].name);
        }
        private void Crash()
        {
            if (state != GameState.Driving) return;
            state = GameState.Failed; Car.SetDriving(false); ClearInput(); hud.ShowResult(false, false);
        }
        public void ToggleGear() { if (state == GameState.Driving && !CurrentCourse.reverseOnly) Car.Gear *= -1; }
        public void ToggleInputMode() { ClearInput(); buttons = !buttons; hud.SetInputMode(buttons); }
        public void ResetCamera() => orbit.ResetView();
        public void SetPaint(int index)
        {
            if (!IsPaintUnlocked(index)) return;
            WorldBuilder.Material("SUV paint", PaintColors[index]).color = PaintColors[index];
            PlayerPrefs.SetInt("paint", index); PlayerPrefs.Save();
        }
        private void ClearInput() { WheelInput = 0; ThrottleHeld = BrakeHeld = LeftHeld = RightHeld = false; }
        private static AudioClip Tone(string name,float duration,float frequency)
        {
            const int rate=22050; float[] samples=new float[Mathf.CeilToInt(duration*rate)];
            for(int i=0;i<samples.Length;i++)
            {
                float t=(float)i/rate, envelope=Mathf.Sin(Mathf.PI*i/samples.Length);
                samples[i]=envelope*(Mathf.Sin(t*frequency*2*Mathf.PI)+.35f*Mathf.Sin(t*frequency*2.5f*Mathf.PI))*.4f;
            }
            AudioClip clip=AudioClip.Create(name,samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
        private void OnApplicationFocus(bool focus) { if (!focus && state == GameState.Driving) Pause(); }
        private void OnApplicationPause(bool paused) { if (paused && state == GameState.Driving) Pause(); }
        private void OnDestroy() { Time.timeScale = 1; Screen.sleepTimeout = SleepTimeout.SystemSetting; Destroy(gateSound);Destroy(finishSound); }
    }
}
