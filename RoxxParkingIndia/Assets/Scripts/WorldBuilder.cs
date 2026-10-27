using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoxxParking
{
    [Serializable] public sealed class CourseList { public CourseDefinition[] courses; }
    [Serializable] public sealed class CourseDefinition
    {
        public string name;
        public int district, difficulty;
        public string briefing;
        public float mapWidth = 34, mapLength = 48, goldTime = 50;
        public bool reversePark, reverseOnly;
        public GateDefinition[] gates;
        public RoutePoint[] route;
        public float spawnX, spawnZ, spawnYaw, bayX, bayZ, bayYaw;
        public float bayWidth = 3.2f, bayLength = 6.2f;
        public BarrierDefinition[] barriers;
    }
    [Serializable] public sealed class BarrierDefinition { public float x, z, width, length, yaw; public string kind; }
    [Serializable] public sealed class RoutePoint { public float x, z, yaw, minKph, radius = 2.5f; public int gear; public bool align; }
    [Serializable] public sealed class GateDefinition { public float x,z,width=4.2f,closedSeconds=4,openSeconds=5; }

    public static class WorldBuilder
    {
        private static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        public static readonly Color Saffron = new Color(1f, 0.44f, 0.15f);
        public static readonly Color Emerald = new Color(0.12f, 0.72f, 0.52f);

        public static Material Material(string name, Color color, float gloss = 0)
        {
            if (materials.TryGetValue(name, out Material material)) return material;
            Material template = Resources.Load<Material>("PrototypeSurface");
            material = template != null ? new Material(template) : new Material(Shader.Find("Standard"));
            material.name = name; material.color = color;
            material.SetFloat("_Glossiness", gloss);
            materials[name] = material;
            return material;
        }

        public static GameObject Shape(string name, Transform parent, PrimitiveType shape,
            Vector3 position, Vector3 size, Material material, bool collider = false)
        {
            GameObject obj = GameObject.CreatePrimitive(shape);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = size;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) UnityEngine.Object.Destroy(obj.GetComponent<Collider>());
            return obj;
        }

        public static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size,
            Material material, bool collider = false)
        {
            GameObject obj=Shape(name,parent,PrimitiveType.Cube,position,size,material,collider);
            if(material.mainTexture!=null)
            {
                // Keep scanned grain/brick size consistent on narrow curbs and long walls as well as the road.
                MeshFilter filter=obj.GetComponent<MeshFilter>();Mesh mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);
                Vector3[] vertices=mesh.vertices,normals=mesh.normals;Vector2[] uv=new Vector2[vertices.Length];
                Vector2 tiling=material.mainTextureScale;
                for(int i=0;i<uv.Length;i++)
                {
                    Vector3 p=Vector3.Scale(vertices[i],size),n=normals[i];
                    Vector2 plane=Mathf.Abs(n.y)>.5f?new Vector2(p.x,p.z):Mathf.Abs(n.x)>.5f?new Vector2(p.z,p.y):new Vector2(p.x,p.y);
                    uv[i]=new Vector2(plane.x/(3*Mathf.Max(.001f,tiling.x)),plane.y/(3*Mathf.Max(.001f,tiling.y)));
                }
                mesh.name="Metre-scaled "+name;mesh.uv=uv;mesh.RecalculateTangents();filter.sharedMesh=mesh;
                parent.GetComponentInParent<CourseMeshLifetime>()?.Own(mesh);
            }
            return obj;
        }

        public static Transform CreateCourse(CourseDefinition definition, out Transform bay)
        {
            Transform root = new GameObject("Course - " + definition.name).transform;
            CourseMeshLifetime meshes=root.gameObject.AddComponent<CourseMeshLifetime>();
            DistrictArt.Create(root, definition);
            SurfaceDressing.Create(root,definition);
            for (int i=0; i<definition.barriers.Length; i++) DistrictArt.Obstacle(root, definition.barriers[i], i);
            float w=definition.mapWidth, l=definition.mapLength;
            Boundary(root,new Vector3(-w/2-.25f,.25f,0),new Vector3(.5f,.5f,l+1));
            Boundary(root,new Vector3(w/2+.25f,.25f,0),new Vector3(.5f,.5f,l+1));
            Boundary(root,new Vector3(0,.25f,-l/2-.25f),new Vector3(w,.5f,.5f));
            Boundary(root,new Vector3(0,.25f,l/2+.25f),new Vector3(w,.5f,.5f));
            bay = new GameObject("Parking bay").transform;
            bay.SetParent(root, false);
            bay.position = new Vector3(definition.bayX, 0.025f, definition.bayZ);
            bay.rotation = Quaternion.Euler(0, definition.bayYaw, 0);
            Material green = Material("Parking green", Emerald);
            Box("Bay fill", bay, Vector3.zero, new Vector3(definition.bayWidth, 0.01f, definition.bayLength),
                EnvironmentArt.Surface("Bay fill", "asphalt_02", new Color(0.55f, 0.76f, 0.64f), new Vector2(definition.bayWidth / 3, definition.bayLength / 3)));
            for (int sign = -1; sign <= 1; sign += 2)
            {
                Box("Bay side", bay, new Vector3(sign * definition.bayWidth / 2, 0.015f, 0), new Vector3(0.12f, 0.02f, definition.bayLength), green);
                Box("Bay end", bay, new Vector3(0, 0.015f, sign * definition.bayLength / 2), new Vector3(definition.bayWidth, 0.02f, 0.12f), green);
            }
            if(definition.reversePark)
            {
                Material arrow=Material("Bay arrow",new Color(.85f,.87f,.78f));
                Box("Required nose direction",bay,new Vector3(0,.025f,0),new Vector3(.09f,.02f,1.3f),arrow);
                for(int side=-1;side<=1;side+=2)
                {
                    GameObject tip=Box("Bay arrowhead",bay,new Vector3(side*.2f,.025f,.4f),new Vector3(.08f,.02f,.6f),arrow);
                    tip.transform.localRotation=Quaternion.Euler(0,-side*45,0);
                }
            }
            Transform signPost = new GameObject("Parking sign").transform;
            signPost.SetParent(bay, false);
            signPost.localPosition = new Vector3(definition.bayWidth / 2 + 0.7f, 0, definition.bayLength / 2);
            Box("Post", signPost, Vector3.up, new Vector3(0.07f, 2, 0.07f), Material("Metal", new Color(0.45f, 0.48f, 0.47f)));
            Box("P sign", signPost, new Vector3(0, 2.1f, 0), new Vector3(0.9f, 0.9f, 0.09f), green);
            Material signWhite = Material("Sign lettering", Color.white);
            Box("P stem", signPost, new Vector3(-0.2f, 2.1f, -0.065f), new Vector3(0.1f, 0.59f, 0.025f), signWhite);
            Box("P top", signPost, new Vector3(0, 2.35f, -0.065f), new Vector3(0.42f, 0.1f, 0.025f), signWhite);
            Box("P middle", signPost, new Vector3(0, 2.1f, -0.065f), new Vector3(0.42f, 0.1f, 0.025f), signWhite);
            Box("P right", signPost, new Vector3(0.2f, 2.23f, -0.065f), new Vector3(0.1f, 0.3f, 0.025f), signWhite);
            StaticBatchingUtility.Combine(root.gameObject);
            foreach(MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
                if(filter.GetComponent<Renderer>().isPartOfStaticBatch && filter.sharedMesh.name.Contains("Combined")) meshes.Own(filter.sharedMesh);
            // Moving beams must be created after static batching.
            if(definition.gates!=null) foreach(GateDefinition gate in definition.gates) TimedGate.Create(root,gate);
            return root;
        }

        private static void Boundary(Transform root, Vector3 position, Vector3 size)
        {
            GameObject wall = Box("Boundary", root, position, size,
                EnvironmentArt.Surface("Boundary", "concrete_floor_02", new Color(0.78f, 0.76f, 0.68f), new Vector2(2, 1)), true);
            wall.layer = 8; wall.AddComponent<CourseObstacle>();
        }

        public static VehicleController CreateCar(Color color)
        {
            GameObject car = new GameObject("Roxx-inspired five-door SUV - prototype");
            Rigidbody rb = car.AddComponent<Rigidbody>();
            rb.mass = 1850; rb.linearDamping = 0.08f; rb.angularDamping = 1.8f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.centerOfMass = new Vector3(0, 0.24f, 0.1f);
            BoxCollider chassis = car.AddComponent<BoxCollider>();
            chassis.center = new Vector3(0, 0.53f, 0); chassis.size = new Vector3(1.88f, 1.05f, 4.28f);
            GameObject supplied = Resources.Load<GameObject>("Thar/TharRoxx");
            if (supplied != null) return AttachThar(car, supplied, color);
            Transform model = new GameObject("Body").transform; model.SetParent(car.transform, false);
            model.localPosition = new Vector3(0, -0.32f, 0);
            Material body = Material("SUV paint", color, 0.4f);
            Material black = Material("Rubber", new Color(0.065f, 0.075f, 0.08f));
            Material glass = Material("Tinted glass", new Color(0.10f, 0.21f, 0.24f), 0.65f);
            Material silver = Material("Alloy", new Color(0.56f, 0.61f, 0.63f), 0.65f);
            Box("Chassis", model, new Vector3(0, 0.65f, 0), new Vector3(1.8f, 0.55f, 4.12f), body);
            Box("Bonnet", model, new Vector3(0, 0.96f, 1.36f), new Vector3(1.77f, 0.3f, 1.32f), body);
            Box("Cabin", model, new Vector3(0, 1.20f, -0.45f), new Vector3(1.72f, 0.7f, 2.6f), body);
            Box("Black roof", model, new Vector3(0, 1.59f, -0.47f), new Vector3(1.79f, 0.15f, 2.75f), black);
            Box("Windscreen", model, new Vector3(0, 1.29f, 0.89f), new Vector3(1.49f, 0.46f, 0.035f), glass);
            Box("Rear glass", model, new Vector3(0, 1.28f, -1.78f), new Vector3(1.45f, 0.43f, 0.035f), glass);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int door = 0; door < 2; door++)
                {
                    float z = 0.24f - door * 1.08f;
                    Box("Side window", model, new Vector3(side * 0.872f, 1.30f, z), new Vector3(0.025f, 0.4f, 0.91f), glass);
                    Box("Door handle", model, new Vector3(side * 0.918f, 0.98f, z - 0.31f), new Vector3(0.05f, 0.045f, 0.17f), black);
                    Box("Door seam", model, new Vector3(side * 0.908f, 0.79f, z - 0.49f), new Vector3(0.015f, 0.31f, 0.018f), black);
                }
                Box("Rear quarter glass", model, new Vector3(side * 0.874f, 1.3f, -1.55f), new Vector3(0.03f, 0.38f, 0.24f), glass);
                Box("Mirror", model, new Vector3(side * 1.03f, 1.15f, 0.84f), new Vector3(0.27f, 0.18f, 0.23f), black);
                Box("Running board", model, new Vector3(side * 0.98f, 0.39f, -0.05f), new Vector3(0.19f, 0.13f, 2.25f), black);
                foreach (float z in new[] { -1.42f, 1.40f })
                    Box("Wheel arch cap", model, new Vector3(side * 0.91f, 0.62f, z), new Vector3(0.21f, 0.11f, 0.95f), black);
                Box("Tail light", model, new Vector3(side * 0.67f, 0.87f, -2.086f), new Vector3(0.26f, 0.31f, 0.045f), Material("Tail red", new Color(0.72f, 0.08f, 0.05f)));
                GameObject lamp = Shape("Round headlamp", model, PrimitiveType.Cylinder, new Vector3(side * 0.61f, 0.99f, 2.08f),
                    new Vector3(0.31f, 0.025f, 0.31f), Material("Headlights", new Color(1, 0.93f, 0.68f)));
                lamp.transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
            Box("Front bumper", model, new Vector3(0, 0.47f, 2.13f), new Vector3(1.91f, 0.23f, 0.18f), black);
            Box("Rear bumper", model, new Vector3(0, 0.47f, -2.13f), new Vector3(1.91f, 0.23f, 0.18f), black);
            Box("Grille", model, new Vector3(0, 0.93f, 2.08f), new Vector3(0.92f, 0.39f, 0.06f), black);
            for (int i = -2; i <= 2; i++) Box("Grille slot", model, new Vector3(i * 0.17f, 0.95f, 2.12f), new Vector3(0.035f, 0.29f, 0.03f), silver);
            GameObject spare = Shape("Rear spare wheel", model, PrimitiveType.Cylinder, new Vector3(0, 1.03f, -2.27f), new Vector3(0.73f, 0.14f, 0.73f), black);
            spare.transform.localRotation = Quaternion.Euler(90, 0, 0);
            VehicleController controller = car.AddComponent<VehicleController>();
            controller.wheels = new WheelCollider[4]; controller.wheelVisuals = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                WheelCollider wc = CreateWheel(car.transform, new Vector3(side * 0.88f, 0, i < 2 ? 1.4f : -1.42f), i, 0.39f);
                controller.wheels[i] = wc;
                Transform visual = new GameObject("Wheel visual " + i).transform; visual.SetParent(car.transform, false);
                GameObject tyre = Shape("All-terrain tyre", visual, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.78f, 0.15f, 0.78f), black);
                tyre.transform.localRotation = Quaternion.Euler(0, 0, 90);
                GameObject hub = Shape("Alloy wheel", visual, PrimitiveType.Cylinder, new Vector3(side * 0.158f, 0, 0), new Vector3(0.47f, 0.012f, 0.47f), silver);
                hub.transform.localRotation = Quaternion.Euler(0, 0, 90);
                controller.wheelVisuals[i] = visual;
            }
            return controller;
        }

        private static VehicleController AttachThar(GameObject car, GameObject prefab, Color color)
        {
            car.name = "Mahindra Thar Roxx";
            GameObject visual = UnityEngine.Object.Instantiate(prefab, car.transform, false);
            visual.name = "Thar Roxx model";
            Material paint = Material("SUV paint", color, 0.8f); paint.SetFloat("_Metallic", 0.35f);
            Material suppliedPaint = Resources.Load<Material>("Thar/Thar_paint");
            foreach (MeshRenderer renderer in visual.GetComponentsInChildren<MeshRenderer>())
            {
                Material[] surfaces = renderer.sharedMaterials;
                for (int m = 0; m < surfaces.Length; m++) if (surfaces[m] == suppliedPaint) surfaces[m] = paint;
                renderer.sharedMaterials = surfaces;
            }
            var controller = car.AddComponent<VehicleController>();
            controller.wheels = new WheelCollider[4]; controller.wheelVisuals = new Transform[4]; controller.wheelHubs = new Transform[4];
            string[] corners = { "FL", "FR", "RL", "RR" };
            for (int i = 0; i < 4; i++)
            {
                Transform wheel = visual.transform.Find("Wheel" + corners[i]);
                Transform hub = visual.transform.Find("Hub" + corners[i]);
                if (wheel == null || hub == null) throw new InvalidOperationException("Incomplete Thar wheel rig: " + corners[i]);
                controller.wheels[i] = CreateWheel(car.transform, wheel.localPosition, i, 0.4f);
                controller.wheelVisuals[i] = wheel; controller.wheelHubs[i] = hub;
            }
            car.GetComponent<BoxCollider>().size = new Vector3(1.76f, 1.05f, 4.28f);
            return controller;
        }

        private static WheelCollider CreateWheel(Transform car, Vector3 position, int index, float radius)
        {
            GameObject wheel = new GameObject("Wheel physics " + index); wheel.transform.SetParent(car, false);
            wheel.transform.localPosition = position;
            WheelCollider wc = wheel.AddComponent<WheelCollider>(); wc.radius = radius;
            wc.mass = 28; wc.suspensionDistance = 0.2f; wc.wheelDampingRate = 0.6f;
            JointSpring spring = wc.suspensionSpring; spring.spring = 42000; spring.damper = 5200; spring.targetPosition = 0.5f;
            wc.suspensionSpring = spring;
            WheelFrictionCurve friction = wc.sidewaysFriction; friction.stiffness = 1.8f; wc.sidewaysFriction = friction;
            wc.ConfigureVehicleSubsteps(5, 5, 8);
            return wc;
        }
    }
}
