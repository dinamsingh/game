#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace RoxxParking
{
    // Only invoked by an explicit --roxx-smoke command; absent from release players.
    public sealed class PrototypeSmokeCheck : MonoBehaviour
    {
        private float started;
        private bool finished;
        private string output;
        private bool hadProgress;
        private int previousProgress;
        private bool cameraPassed;
        private bool hadSensitivity,hadSteeringSide;
        private float oldSensitivity;
        private int oldSteeringSide;
        private readonly Dictionary<string,float> bestTimes=new Dictionary<string,float>();
        private readonly Dictionary<string,int> medals=new Dictionary<string,int>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--roxx-smoke") >= 0)
                new GameObject("Explicit prototype self-check").AddComponent<PrototypeSmokeCheck>();
        }

        private IEnumerator Start()
        {
            started = Time.realtimeSinceStartup;
            hadProgress = PlayerPrefs.HasKey("unlockedLevel");
            previousProgress = PlayerPrefs.GetInt("unlockedLevel", 0);
            hadSensitivity=PlayerPrefs.HasKey("cameraSensitivity");oldSensitivity=PlayerPrefs.GetFloat("cameraSensitivity",1);
            hadSteeringSide=PlayerPrefs.HasKey("steeringOnRight");oldSteeringSide=PlayerPrefs.GetInt("steeringOnRight",0);
            output = Path.GetFullPath("Builds/Checks"); Directory.CreateDirectory(output);
            yield return null;
            RoxxGame game = FindAnyObjectByType<RoxxGame>();
            if (game == null) { Finish(false, "Game did not initialize"); yield break; }
            for(int i=0;i<game.CourseCount;i++)
            {
                string name=game.CourseName(i);
                if(PlayerPrefs.HasKey("bestTime."+name)) bestTimes[name]=PlayerPrefs.GetFloat("bestTime."+name);
                if(PlayerPrefs.HasKey("medal."+name)) medals[name]=PlayerPrefs.GetInt("medal."+name);
            }
            yield return new WaitForSecondsRealtime(1);
            if (game.Car.wheelHubs == null || game.Car.wheelHubs.Length != 4 || game.Car.transform.Find("Thar Roxx model") == null)
            { Finish(false, "Supplied Thar model and wheel hubs were not loaded"); yield break; }
            Renderer[] carRenderers = game.Car.GetComponentsInChildren<Renderer>();
            Bounds carBounds = carRenderers[0].bounds;
            bool paintedBody = false;
            foreach (Renderer renderer in carRenderers)
            {
                carBounds.Encapsulate(renderer.bounds);
                foreach (Material material in renderer.sharedMaterials) if (material.name == "SUV paint") paintedBody = true;
            }
            if (!paintedBody || carRenderers.Length > 60 || carBounds.size.x > 1.98f || carBounds.size.z > 4.51f)
            {
                Capture("failed-import.png");
                Finish(false, $"Imported Thar validation: paint={paintedBody}, renderers={carRenderers.Length}, bounds={carBounds.size}, angles={game.Car.transform.eulerAngles}");
                yield break;
            }
            if (game.Car.wheels[0].transform.localPosition.x >= 0 || game.Car.wheels[1].transform.localPosition.x <= 0
                || game.Car.wheels[0].transform.localPosition.z <= game.Car.wheels[2].transform.localPosition.z)
            { Finish(false, "Thar wheel corner order is incorrect"); yield break; }
            if (RenderSettings.skybox == null || RenderSettings.customReflectionTexture == null)
            { Finish(false, "Environment sky/reflection assets are missing"); yield break; }
            Material foliage = Resources.Load<Material>("Environment/Foliage");
            if (foliage == null || foliage.shader.name != "Roxx/Foliage" || !foliage.shader.isSupported)
            { Finish(false, "Textured foliage shader is missing or unsupported"); yield break; }
            if (!Capture("menu.png")) yield break;
            yield return CheckControls(game);
            if(finished)yield break;
            GameObject.Find("Expert trials").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();yield return null;
            if(!Capture("expert-menu.png"))yield break;
            game.Menu();yield return null;
            yield return new WaitForSecondsRealtime(0.25f);
            game.StartLevel(0);
            yield return new WaitForSeconds(1);
            yield return CheckCamera(game);
            if (!cameraPassed) yield break;
            game.Restart();
            yield return new WaitForSeconds(1);
            float startZ = game.Car.transform.position.z;
            game.ThrottleHeld = true;
            yield return new WaitForSeconds(2);
            game.ThrottleHeld = false;
            if (game.Car.transform.position.z <= startZ + 1.5f)
            { Finish(false, "Vehicle did not move forward under throttle"); yield break; }
            game.BrakeHeld = true;
            yield return new WaitForSeconds(1.5f);
            game.BrakeHeld = false;
            if (game.Car.Speed > 0.8f) { Finish(false, "Brake did not stop vehicle"); yield break; }
            if (!Capture("driving.png")) yield break;
            yield return CheckSpeed(game);
            if(finished)yield break;
            game.StartLevel(0);yield return new WaitForSeconds(.5f);
            OrbitCamera follow = Camera.main.GetComponent<OrbitCamera>();
            Canvas ui = FindAnyObjectByType<Canvas>();
            follow.enabled = false; ui.enabled = false;
            Camera.main.transform.position = game.Car.transform.TransformPoint(new Vector3(-3.5f, 2, 5));
            Camera.main.transform.LookAt(game.Car.transform.position + Vector3.up * 0.8f);
            bool frontCaptured = Capture("vehicle-front.png");
            ui.enabled = true; follow.enabled = true; follow.ResetView(true);
            if (!frontCaptured) yield break;
            yield return new WaitForSecondsRealtime(0.25f);
            game.Car.SetDriving(false);
            Rigidbody body = game.Car.GetComponent<Rigidbody>();
            body.position = new Vector3(0, 0.62f, 13);
            body.rotation = Quaternion.identity;
            Physics.SyncTransforms();
            yield return new WaitForSeconds(1);
            game.Car.SetDriving(true);
            yield return new WaitForSeconds(2.3f);
            if (!game.HasWon) { Finish(false, "Centered stopped vehicle did not complete parking hold"); yield break; }
            if (!Capture("completed.png")) yield break;
            typeof(RoxxGame).GetProperty("UnlockedLevel").SetValue(game,RoxxGame.CampaignCourseCount-1);
            for(int i=1;i<game.CourseCount;i++)
            {
                // Physical acceleration/camera checks above run at real time; rule traversal uses faster virtual time.
                Time.timeScale=3;
                if(i==RoxxGame.CampaignCourseCount)
                    typeof(RoxxGame).GetProperty("UnlockedLevel").SetValue(game,0);
                game.StartLevel(i); yield return new WaitForSeconds(.3f);
                CourseDefinition c=game.CurrentCourse;
                if(c.district==4)foreach(MeshRenderer ridge in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                    if(ridge.gameObject.name=="Distant rocky ridge" && ridge.bounds.min.x<c.mapWidth/2+4 && ridge.bounds.max.x> -c.mapWidth/2-4)
                    {Finish(false,"Backdrop mountain intrudes into the proving ground");yield break;}
                if(c.reverseOnly)
                {
                    game.ToggleGear();
                    if(game.Car.Gear!=-1){Finish(false,"Reverse-only trial allowed drive gear");yield break;}
                }
                if(game.RouteIndex!=0 || game.RouteCount!=c.route.Length)
                {Finish(false,"Route did not reset: "+c.name);yield break;}
                if(i%3==0)
                {
                    // A second view shows each location's layout and materials, not only its spawn.
                    OrbitCamera orbit=Camera.main.GetComponent<OrbitCamera>();orbit.enabled=false;
                    Camera.main.transform.position=new Vector3(-c.mapWidth*.48f,20,-c.mapLength*.34f);
                    Camera.main.transform.LookAt(new Vector3(0,0,8));
                    if(!Capture(i>=12?"trial-"+(i+1)+".png":"district-"+c.district+".png"))yield break;
                    orbit.enabled=true;orbit.ResetView(true);
                }
                if(i>=12)
                {
                    yield return null;yield return null;
                    if(!Capture("trial-driving-"+(i+1)+".png"))yield break;
                }
                if(i==15)
                {
                    yield return CheckGate(game);
                    if(finished)yield break;
                }
                if(i==4)
                {
                    foreach(string id in new[]{"wooden_crate_01","barrel_03","industrial_storage_cart"})
                    {
                        var prop=Resources.Load<GameObject>("Workshop/"+id);
                        if(prop==null || prop.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3>25000)
                        {Finish(false,"Workshop prop absent or over budget: "+id);yield break;}
                    }
                    foreach(string asset in new[]{"wooden_rough_planks","rusty_painted_metal","painted_metal_shutter"})
                        if(Resources.Load<Texture2D>("Environment/"+asset+"_diff_1k")==null || Resources.Load<Texture2D>("Environment/"+asset+"_nor_gl_1k")==null)
                        {Finish(false,"Workshop surface missing: "+asset);yield break;}
                    yield return new WaitForSeconds(.7f);
                    foreach(TextMesh sign in FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
                        if(sign.GetComponent<Renderer>().localBounds.size.y>1.1f)
                        {Finish(false,"Workshop sign exceeds its physical board: "+sign.text);yield break;}
                    if(!Capture("workshop-driving.png"))yield break;
                    OrbitCamera orbit=Camera.main.GetComponent<OrbitCamera>();orbit.enabled=false;
                    Canvas canvas=FindAnyObjectByType<Canvas>();canvas.enabled=false;
                    Camera.main.transform.position=new Vector3(-16,14,-21);Camera.main.transform.LookAt(new Vector3(0,0,4));
                    if(!Capture("workshop-overview.png"))yield break;
                    Camera.main.transform.position=new Vector3(-7,3.2f,c.mapLength/2-5);
                    Camera.main.transform.LookAt(new Vector3(0,1.5f,c.mapLength/2+5));
                    if(!Capture("workshop-service.png"))yield break;
                    canvas.enabled=true;orbit.enabled=true;orbit.ResetView(true);
                }
                Teleport(game,c.bayX,c.bayZ,c.bayYaw,1,0);
                yield return new WaitForSeconds(2);
                if(game.HasWon){Finish(false,"Skipped route incorrectly completed: "+c.name);yield break;}
                for(int j=0;j<c.route.Length;j++)
                {
                    RoutePoint point=c.route[j];
                    if(point.gear<0)
                    {
                        Teleport(game,point.x,point.z,point.yaw,1,.3f);
                        yield return new WaitForSeconds(.1f);
                        if(game.RouteIndex!=j){Finish(false,"Forward cleared reverse gate: "+c.name);yield break;}
                    }
                    int gear=point.gear==0?1:point.gear;
                    if(point.minKph>0)
                    {
                        Teleport(game,point.x,point.z,point.yaw,gear,0);
                        yield return new WaitForSeconds(.12f);
                        if(game.RouteIndex!=j){Finish(false,"Slow car cleared speed sensor");yield break;}
                    }
                    Teleport(game,point.x,point.z,point.yaw,gear,gear*Mathf.Max(.4f,point.minKph/3.6f+.2f));
                    yield return new WaitForSeconds(.12f);
                    if(game.RouteIndex!=j+1){Finish(false,"Route gate failed: "+c.name+" / "+j);yield break;}
                }
                if(c.reversePark && !c.reverseOnly)
                {
                    Teleport(game,c.bayX,c.bayZ,c.bayYaw,1,0);
                    yield return new WaitForSeconds(2);
                    if(game.HasWon){Finish(false,"Forward gear won reverse parking: "+c.name);yield break;}
                    Teleport(game,c.bayX,c.bayZ,c.bayYaw+180,-1,0);
                    yield return new WaitForSeconds(2);
                    if(game.HasWon){Finish(false,"Wrong heading won reverse parking: "+c.name);yield break;}
                }
                Teleport(game,c.bayX,c.bayZ,c.bayYaw,c.reversePark?-1:1,0);
                yield return new WaitForSeconds(2.2f);
                if(!game.HasWon || game.BestTime(i)<=0 || game.BestMedal(i)==0)
                {Finish(false,"Completion/record failed: "+c.name);yield break;}
                if(i>=12 && game.UnlockedLevel!=0){Finish(false,"Expert trial changed campaign unlocks");yield break;}
                Debug.Log("ROXX_COURSE_OK: "+c.name);
            }
            if(game.TrialStars!=game.MaximumTrialStars || !game.IsPaintUnlocked(4) || !game.IsPaintUnlocked(5))
            {Finish(false,"Expert medals did not unlock the earned paints");yield break;}
            if(!Capture("trial-result.png"))yield break;
            game.Menu();yield return null;
            GameObject.Find("Expert trials").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();yield return null;
            if(!Capture("expert-menu-test-earned.png"))yield break;
            var expert=GameObject.Find("Course 12").GetComponent<UnityEngine.UI.Button>();
            if(!expert.interactable){Finish(false,"Expert menu locked behind campaign");yield break;}
            expert.onClick.Invoke();yield return null;
            if(!game.IsTrial){Finish(false,"Expert menu did not start the trial");yield break;}
            float savedBest=game.BestTime(4);int savedMedal=game.BestMedal(4);
            typeof(RoxxGame).GetProperty("UnlockedLevel").SetValue(game,0);
            game.StartLevel(4,true);yield return new WaitForSeconds(.3f);
            if(!game.IsPreview || game.CurrentCourse!=game.CourseAt(4)) {Finish(false,"Locked workshop practice did not open");yield break;}
            foreach(RoutePoint point in game.CurrentCourse.route)
            {
                int g=point.gear<0?-1:1;Teleport(game,point.x,point.z,point.yaw,g,g*.4f);yield return new WaitForSeconds(.15f);
            }
            Teleport(game,game.CurrentCourse.bayX,game.CurrentCourse.bayZ,game.CurrentCourse.bayYaw,-1,0);
            yield return new WaitForSeconds(2.2f);
            if(!game.HasWon || game.UnlockedLevel!=0 || game.BestTime(4)!=savedBest || game.BestMedal(4)!=savedMedal)
            {Finish(false,"Practice completion changed progression or records");yield break;}
            game.Restart();yield return new WaitForSeconds(.3f);
            if(!game.IsPreview || game.RouteIndex!=0){Finish(false,"Practice retry lost preview mode");yield break;}
            yield return new WaitForSecondsRealtime(0.25f);
            Finish(true, "24 course loads/ordered gates/parking/records, reverse-only gear, expert menu without campaign unlocks, earned paints, timed-gate collision/open clearance/occupancy hold/pause, and workshop practice preservation passed. Driving/camera checks passed. Route traversal was teleported, not a human run or retention measurement.");
        }

        private IEnumerator CheckControls(RoxxGame game)
        {
            var orbit=Camera.main.GetComponent<OrbitCamera>();
            var yaw=typeof(OrbitCamera).GetField("yaw",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            var drag=typeof(OrbitCamera).GetMethod("Drag",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            game.SetCameraSensitivity(1);orbit.ResetView(true);float before=(float)yaw.GetValue(orbit);
            drag.Invoke(orbit,new object[]{new Vector2(12,0)});float normal=Mathf.DeltaAngle(before,(float)yaw.GetValue(orbit));
            game.SetCameraSensitivity(2);orbit.ResetView(true);
            drag.Invoke(orbit,new object[]{new Vector2(12,0)});
            if(Mathf.Abs(Mathf.DeltaAngle(before,(float)yaw.GetValue(orbit))-normal*2)>.01f){Finish(false,"Camera sensitivity did not scale drag");yield break;}
            game.SetCameraSensitivity(float.NaN);if(game.CameraSensitivity!=1){Finish(false,"Invalid sensitivity not reset");yield break;}
            game.SetCameraSensitivity(99);if(game.CameraSensitivity!=2){Finish(false,"Sensitivity upper bound failed");yield break;}
            game.SetCameraSensitivity(-1);if(game.CameraSensitivity!=.25f){Finish(false,"Sensitivity lower bound failed");yield break;}
            game.Menu();GameObject.Find("Controls").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();yield return null;
            var slider=GameObject.Find("Camera sensitivity").GetComponent<UnityEngine.UI.Slider>();slider.value=1.5f;
            GameObject.Find("Steering side").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            if(!game.SteeringOnRight)game.SetSteeringSide(true);
            if(!Capture("controls-settings.png"))yield break;
            GameObject.Find("Back").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();yield return null;
            if(Mathf.Abs(PlayerPrefs.GetFloat("cameraSensitivity")-1.5f)>.001f || PlayerPrefs.GetInt("steeringOnRight")!=1)
            {Finish(false,"Controls were not saved");yield break;}
            game.StartLevel(0);yield return null;
            for(int side=0;side<2;side++)
            {
                game.SetSteeringSide(side==1);yield return null;
                RectTransform wheel=GameObject.Find("Steering wheel").GetComponent<RectTransform>();
                RectTransform accel=GameObject.Find("Accelerator").GetComponent<RectTransform>();
                if(wheel.sizeDelta.x!=300 || wheel.anchorMin.x!=(side==1?1:0) || accel.anchorMin.x!=(side==1?0:1))
                {Finish(false,"Large steering/pedal layout failed");yield break;}
                if(!Capture(side==0?"controls-left.png":"controls-right.png"))yield break;
            }
            game.ToggleInputMode();yield return null;
            if(GameObject.Find("Left").GetComponent<RectTransform>().sizeDelta!=new Vector2(145,155))
            {Finish(false,"Large steering buttons missing");yield break;}
            if(!Capture("controls-buttons-right.png"))yield break;
            game.LeftHeld=true;game.SetSteeringSide(false);
            if(game.LeftHeld || game.WheelInput!=0){Finish(false,"Layout change retained held input");yield break;}
            game.ToggleInputMode();game.Pause();yield return null;
            GameObject.Find("Controls").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();yield return null;
            GameObject.Find("Back").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();yield return null;
            if(Time.timeScale!=0){Finish(false,"Control settings resumed paused game");yield break;}
            game.Resume();game.SetCameraSensitivity(oldSensitivity);game.SetSteeringSide(oldSteeringSide==1);game.Menu();
            Debug.Log("ROXX_CONTROLS_OK: sensitivity bounds/2x drag, slider save, both layouts, large wheel/buttons, input release and paused settings.");
        }

        private IEnumerator CheckSpeed(RoxxGame game)
        {
            foreach(string asset in new[]{"asphalt_02","concrete_floor_02","brick_wall_001","rock_face_03"})
                if(Resources.Load<Texture2D>("Environment/"+asset+"_diff_2k")==null || Resources.Load<Texture2D>("Environment/"+asset+"_surface")==null)
                {Finish(false,"Upgraded surface absent: "+asset);yield break;}
            var shader=Resources.Load<Shader>("VisualFinish");
            if(shader==null || !shader.isSupported){Finish(false,"Visual finish shader unsupported");yield break;}
            game.StartLevel(18);yield return new WaitForSeconds(.5f);
            game.Pause();game.ToggleGraphics();
            if(game.HighGraphics){Finish(false,"Balanced graphics toggle failed");yield break;}
            game.ToggleGraphics();game.Resume();
            float start=Time.time,peak=0,startZ=game.Car.transform.position.z;
            game.ThrottleHeld=true;
            while(Time.time-start<22 && game.Car.Speed*3.6f<99.5f)
            {peak=Mathf.Max(peak,game.Car.Speed*3.6f);yield return new WaitForFixedUpdate();}
            float accelerationTime=Time.time-start;
            for(int i=0;i<100;i++){yield return new WaitForFixedUpdate();peak=Mathf.Max(peak,game.Car.Speed*3.6f);}
            if(peak<99 || peak>100.5f || !game.Car.DrivingEnabled)
            {Finish(false,"100 km/h physics check failed, peak="+peak+", distance="+(game.Car.transform.position.z-startZ));yield break;}
            if(!Capture("sprint-100.png"))yield break;
            game.ThrottleHeld=false;game.BrakeHeld=true;
            float brakeZ=game.Car.transform.position.z,brakeStart=Time.time;
            while(game.Car.Speed>.3f && Time.time-brakeStart<6)yield return new WaitForFixedUpdate();
            float stopDistance=game.Car.transform.position.z-brakeZ,stopTime=Time.time-brakeStart;
            game.BrakeHeld=false;
            if(game.Car.Speed>.8f){Finish(false,"High-speed braking failed");yield break;}
            game.Car.Gear=-1;game.ThrottleHeld=true;
            float reverseStart=Time.time,reversePeak=0;
            while(Time.time-reverseStart<4){yield return new WaitForFixedUpdate();reversePeak=Mathf.Max(reversePeak,game.Car.Speed*3.6f);}
            game.ThrottleHeld=false;
            if(reversePeak<17.5f || reversePeak>18.5f){Finish(false,"Reverse speed cap failed: "+reversePeak);yield break;}
            File.WriteAllText(Path.Combine(output,"speed-result.json"),JsonUtility.ToJson(new SpeedResult{peakKph=peak,secondsTo995=accelerationTime,brakingMetres=stopDistance,brakingSeconds=stopTime,reversePeakKph=reversePeak},true));
            Debug.Log("ROXX_SPEED_OK: peak="+peak+", acceleration="+accelerationTime+", brake metres="+stopDistance+", reverse="+reversePeak);
        }
        [Serializable] private sealed class SpeedResult {public float peakKph,secondsTo995,brakingMetres,brakingSeconds,reversePeakKph;}

        private IEnumerator CheckGate(RoxxGame game)
        {
            var gate=FindAnyObjectByType<TimedGate>();
            var car=game.Car.GetComponent<BoxCollider>();
            Vector3 crossing=new Vector3(gate.transform.position.x,.5f,gate.transform.position.z);
            Physics.SyncTransforms();
            if(gate.IsOpen || !Physics.ComputePenetration(car,crossing,Quaternion.identity,gate.Beam,gate.Beam.transform.position,gate.Beam.transform.rotation,out _,out _))
            {Finish(false,"Closed gate did not block the car footprint");yield break;}
            if(!Capture("timed-gate-red.png"))yield break;
            float deadline=Time.realtimeSinceStartup+15;
            while(!gate.IsOpen && Time.realtimeSinceStartup<deadline)yield return null;
            if(!gate.IsOpen || Physics.ComputePenetration(car,crossing,Quaternion.identity,gate.Beam,gate.Beam.transform.position,gate.Beam.transform.rotation,out _,out _))
            {Finish(false,"Open gate did not clear the vehicle");yield break;}
            game.Car.SetDriving(false);game.Car.GetComponent<Rigidbody>().position=crossing;Physics.SyncTransforms();
            yield return new WaitForSeconds(6.5f);
            if(!gate.IsOpen){Finish(false,"Gate closed on an occupied crossing");yield break;}
            if(!Capture("timed-gate-green.png"))yield break;
            float height=gate.Beam.transform.position.y;game.Pause();
            yield return new WaitForSecondsRealtime(.3f);
            if(!gate.IsOpen || Mathf.Abs(gate.Beam.transform.position.y-height)>.01f){Finish(false,"Paused gate kept moving");yield break;}
            game.Resume();
            game.Restart();yield return null;
            var reused=FindAnyObjectByType<TimedGate>();
            if(reused!=gate || gate.Cycle.Phase!=GatePhase.Closed || game.RouteIndex!=0)
            {Finish(false,"Retry rebuilt the course or did not reset the gate and route");yield break;}
        }

        private static void Teleport(RoxxGame game,float x,float z,float yaw,int gear,float velocity)
        {
            game.Car.SetDriving(false);Rigidbody body=game.Car.GetComponent<Rigidbody>();
            body.position=new Vector3(x,.5f,z);body.rotation=Quaternion.Euler(0,yaw,0);
            body.linearVelocity=body.rotation*Vector3.forward*velocity;body.angularVelocity=Vector3.zero;
            game.Car.Gear=gear;Physics.SyncTransforms();game.Car.SetDriving(true);
        }

        private IEnumerator CheckCamera(RoxxGame game)
        {
            Camera camera = Camera.main;
            OrbitCamera orbit = camera == null ? null : camera.GetComponent<OrbitCamera>();
            if (orbit == null) { Finish(false, "Missing follow camera"); yield break; }
            GameObject subject = new GameObject("Isolated camera test target");
            subject.transform.position = new Vector3(70, 4, 70);
            orbit.target = subject.transform; orbit.Showcase = false; orbit.ResetView(true);
            yield return new WaitForSeconds(0.2f);
            subject.transform.rotation = Quaternion.Euler(0, 90, 0);
            float until = Time.realtimeSinceStartup + 1;
            Quaternion previous = camera.transform.rotation;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
                float rate = Quaternion.Angle(previous, camera.transform.rotation) / Mathf.Max(0.001f, Time.unscaledDeltaTime);
                if (rate > 600) { Finish(false, "Camera snapped during a target turn"); yield break; }
                previous = camera.transform.rotation;
            }
            if (Mathf.Abs(Mathf.DeltaAngle(camera.transform.eulerAngles.y, 118)) > 3)
            { Finish(false, "Camera did not settle behind the turned target"); yield break; }
            subject.transform.rotation = Quaternion.Euler(0, 179, 0);
            yield return new WaitForSeconds(0.8f);
            previous = camera.transform.rotation;
            subject.transform.rotation = Quaternion.Euler(0, -179, 0);
            yield return new WaitForSeconds(0.2f);
            if (Quaternion.Angle(previous, camera.transform.rotation) > 6)
            { Finish(false, "Camera took the long path across heading wrap"); yield break; }
            yield return new WaitForSeconds(0.6f);
            Vector3 focus = subject.transform.position + Vector3.up * 0.9f;
            Vector3 direction = (camera.transform.position - focus).normalized;
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Temporary camera obstruction"; wall.layer = 8;
            wall.transform.position = focus + direction * 4; wall.transform.localScale = Vector3.one * 1.5f;
            Physics.SyncTransforms(); yield return null; yield return null;
            Vector3 offset = camera.transform.position - focus;
            if (offset.magnitude > 6 || Physics.SphereCast(focus, 0.22f, offset.normalized, out RaycastHit hit,
                offset.magnitude, 1 << 8, QueryTriggerInteraction.Ignore))
            { Finish(false, "Camera remained behind or inside an obstruction"); yield break; }
            Destroy(wall); yield return new WaitForSeconds(1);
            if (Vector3.Distance(camera.transform.position, focus) < 8)
            { Finish(false, "Camera did not recover distance after an obstruction"); yield break; }
            orbit.target = game.Car.transform; orbit.ResetView(true); Destroy(subject); cameraPassed = true;
        }

        private void Update()
        {
            if (!finished && Time.realtimeSinceStartup - started > 600)
                Finish(false, "Runtime self-check exceeded 600 seconds");
        }

        private bool Capture(string name)
        {
            Camera camera = Camera.main;
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (camera == null || canvas == null) { Finish(false, "Missing camera or UI canvas"); return false; }
            RenderTexture previousTarget = camera.targetTexture, previousActive = RenderTexture.active;
            RenderMode previousMode = canvas.renderMode;
            Camera previousCamera = canvas.worldCamera;
            float previousDistance = canvas.planeDistance;
            RenderTexture target = RenderTexture.GetTemporary(1280, 720, 24, RenderTextureFormat.Default, RenderTextureReadWrite.Default, 2);
            Texture2D pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                // Batch Editor has no rendered Game view; render the same scene and HUD explicitly.
                camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                Canvas.ForceUpdateCanvases(); camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                File.WriteAllBytes(Path.Combine(output, name), pixels.EncodeToPNG());
                return true;
            }
            catch (Exception error) { Finish(false, "Screen capture failed: " + error.Message); return false; }
            finally
            {
                camera.targetTexture = previousTarget; RenderTexture.active = previousActive;
                canvas.renderMode = previousMode; canvas.worldCamera = previousCamera; canvas.planeDistance = previousDistance;
                RenderTexture.ReleaseTemporary(target); Destroy(pixels);
            }
        }

        private void Finish(bool passed, string message)
        {
            if (finished) return;
            finished = true; Time.timeScale = 1;
            if (hadProgress) PlayerPrefs.SetInt("unlockedLevel", previousProgress);
            else PlayerPrefs.DeleteKey("unlockedLevel");
            if(hadSensitivity)PlayerPrefs.SetFloat("cameraSensitivity",oldSensitivity);else PlayerPrefs.DeleteKey("cameraSensitivity");
            if(hadSteeringSide)PlayerPrefs.SetInt("steeringOnRight",oldSteeringSide);else PlayerPrefs.DeleteKey("steeringOnRight");
            RoxxGame game=FindAnyObjectByType<RoxxGame>();
            if(game!=null) for(int i=0;i<game.CourseCount;i++)
            {
                string name=game.CourseName(i);
                if(bestTimes.TryGetValue(name,out float time))PlayerPrefs.SetFloat("bestTime."+name,time);else PlayerPrefs.DeleteKey("bestTime."+name);
                if(medals.TryGetValue(name,out int medal))PlayerPrefs.SetInt("medal."+name,medal);else PlayerPrefs.DeleteKey("medal."+name);
            }
            PlayerPrefs.Save();
            File.WriteAllText(Path.Combine(output, "runtime-result.json"), JsonUtility.ToJson(new Result { passed = passed, message = message }, true));
            Debug.Log((passed ? "ROXX_SMOKE_OK: " : "ROXX_SMOKE_FAILED: ") + message);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(passed ? 0 : 1);
#else
            Application.Quit(passed ? 0 : 1);
#endif
        }
        [Serializable] private sealed class Result { public bool passed; public string message; }
    }
}
#endif
