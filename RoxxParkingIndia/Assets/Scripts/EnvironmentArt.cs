using UnityEngine;
using UnityEngine.Rendering;

namespace RoxxParking
{
    public static class EnvironmentArt
    {
        public static void Lighting()
        {
            RenderSettings.skybox = Resources.Load<Material>("Environment/DaylightSkybox");
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.63f, 0.73f);
            RenderSettings.ambientEquatorColor = new Color(0.43f, 0.46f, 0.47f);
            RenderSettings.ambientGroundColor = new Color(0.24f, 0.23f, 0.21f);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = Resources.Load<Cubemap>("Environment/DaylightSky");
            RenderSettings.reflectionIntensity = 0.95f;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.72f, 0.78f, 0.82f); RenderSettings.fogDensity = 0.0025f;
        }

        public static Material Surface(string name, string asset, Color tint, Vector2 tiles, float roughness = 0.85f)
        {
            Material material = WorldBuilder.Material(name, tint, 1 - roughness);
            material.mainTexture = Resources.Load<Texture2D>("Environment/" + asset + "_diff_2k") ?? Resources.Load<Texture2D>("Environment/" + asset + "_diff_1k");
            material.mainTextureScale = tiles;
            Texture2D normal = Resources.Load<Texture2D>("Environment/" + asset + "_nor_gl_2k") ?? Resources.Load<Texture2D>("Environment/" + asset + "_nor_gl_1k");
            material.SetTexture("_BumpMap", normal); material.SetTextureScale("_BumpMap", tiles);
            material.SetFloat("_BumpScale", 0.75f); material.EnableKeyword("_NORMALMAP");
            Texture2D ao=Resources.Load<Texture2D>("Environment/"+asset+"_ao_1k");
            if(ao!=null){material.SetTexture("_OcclusionMap",ao);material.SetTextureScale("_OcclusionMap",tiles);material.SetFloat("_OcclusionStrength",.8f);}
            Texture2D surface=Resources.Load<Texture2D>("Environment/"+asset+"_surface");
            if(surface!=null){material.SetTexture("_MetallicGlossMap",surface);material.SetTextureScale("_MetallicGlossMap",tiles);material.SetFloat("_GlossMapScale",.8f);material.EnableKeyword("_METALLICGLOSSMAP");}
            return material;
        }

        public static Transform Building(Transform parent, Vector3 position, int floors, int variation, int facing)
        {
            Transform root = new GameObject("Apartment block " + variation).transform; root.SetParent(parent, false);
            root.localPosition = position;
            float height = floors * 2.8f;
            Color[] tones = { new Color(0.69f, 0.65f, 0.56f), new Color(0.75f, 0.73f, 0.65f), new Color(0.57f, 0.62f, 0.61f), new Color(0.66f, 0.53f, 0.43f) };
            Material plaster = Surface("Plaster " + variation % 4,"concrete_floor_02",tones[variation % 4]*1.2f,Vector2.one);
            Material stone = WorldBuilder.Material("Facade trim", new Color(0.8f, 0.79f, 0.72f));
            Material glass = WorldBuilder.Material("Apartment glass", new Color(0.12f, 0.2f, 0.25f), 0.7f);
            glass.SetFloat("_Metallic", 0.55f);
            Material metal = WorldBuilder.Material("Railings", new Color(0.16f, 0.19f, 0.18f), 0.3f);
            WorldBuilder.Box("Plastered building", root, Vector3.up * height / 2, new Vector3(10, height, 9), plaster);
            for (int floor = 0; floor < floors; floor++)
            {
                float y = floor * 2.8f + 1.5f;
                WorldBuilder.Box("Slab edge", root, new Vector3(0, floor * 2.8f + 0.14f, 0), new Vector3(10.25f, 0.18f, 9.3f), stone);
                for (int w = 0; w < 3; w++)
                {
                    float z = (w - 1) * 2.7f;
                    if(floor==0 && w==1)
                    {
                        Material shutter=Surface("Street shop shutter","painted_metal_shutter",new Color(.74f,.78f,.78f),Vector2.one,.65f);
                        WorldBuilder.Box("Shop doorway surround",root,new Vector3(facing*5.05f,1.2f,z),new Vector3(.16f,2.4f,2.5f),stone);
                        WorldBuilder.Box("Street shop shutter",root,new Vector3(facing*5.16f,1.14f,z),new Vector3(.08f,2.25f,2.25f),shutter);
                        var awning=WorldBuilder.Box("Shop entrance canopy",root,new Vector3(facing*5.7f,2.65f,z),new Vector3(1.5f,.08f,2.8f),metal);
                        awning.transform.localRotation=Quaternion.Euler(0,0,-facing*8);
                        WorldBuilder.Box("Canopy front lip",root,new Vector3(facing*6.4f,2.55f,z),new Vector3(.06f,.2f,2.8f),metal);
                        continue;
                    }
                    WorldBuilder.Box("Window frame", root, new Vector3(facing * 5.04f, y, z), new Vector3(0.12f, 1.75f, 1.9f), stone);
                    WorldBuilder.Box("Window glass", root, new Vector3(facing * 5.12f, y, z), new Vector3(0.03f, 1.48f, 1.62f), glass);
                    WorldBuilder.Box("Mullion", root, new Vector3(facing * 5.15f, y, z), new Vector3(0.04f, 1.5f, 0.06f), metal);
                    if ((w + floor + variation) % 3 != 0) continue;
                    WorldBuilder.Box("Balcony floor", root, new Vector3(facing * 5.5f, y - 0.96f, z), new Vector3(1.2f, 0.14f, 2.3f), stone);
                    WorldBuilder.Box("Balcony top rail", root, new Vector3(facing * 6.05f, y - 0.06f, z), new Vector3(0.07f, 0.06f, 2.3f), metal);
                    for (int bar = 0; bar < 6; bar++)
                        WorldBuilder.Box("Balcony railing", root, new Vector3(facing * 6.05f, y - 0.5f, z - 1.08f + bar * 0.43f), new Vector3(0.045f, 0.86f, 0.045f), metal);
                }
            }
            WorldBuilder.Shape("Facade rainwater pipe",root,PrimitiveType.Cylinder,new Vector3(facing*5.14f,height/2,-4.2f),new Vector3(.085f,height/2,.085f),metal);
            for(int floor=1;floor<floors;floor+=2)
            {
                Vector3 p=new Vector3(facing*5.3f,floor*2.8f+.6f,3.2f);
                WorldBuilder.Box("Exterior AC unit",root,p,new Vector3(.45f,.6f,.86f),stone);
                for(int slit=0;slit<6;slit++)WorldBuilder.Box("AC ventilation grille",root,p+new Vector3(facing*.24f,-.2f+slit*.08f,0),new Vector3(.025f,.025f,.7f),metal);
            }
            WorldBuilder.Box("Roof coping", root, new Vector3(0, height + 0.2f, 0), new Vector3(10.3f, 0.4f, 9.3f), stone);
            WorldBuilder.Shape("Rooftop water tank", root, PrimitiveType.Cylinder, new Vector3(1, height + 0.9f, 1), new Vector3(1.4f, 0.65f, 1.4f),
                WorldBuilder.Material("Water tank", new Color(0.12f, 0.13f, 0.12f)));
            return root;
        }

        public static void Tree(Transform root, Vector3 position, int seed)
        {
            Material bark = WorldBuilder.Material("Bark", new Color(0.8f, 0.78f, 0.72f));
            bark.mainTexture = Resources.Load<Texture2D>("Environment/BarkColor"); bark.mainTextureScale = new Vector2(1, 2);
            WorldBuilder.Shape("Tree trunk", root, PrimitiveType.Cylinder, position + Vector3.up * 1.6f, new Vector3(0.26f, 1.6f, 0.26f), bark);
            for (int i = 0; i < 5; i++)
            {
                float angle = (i * 137.5f + seed * 21) * Mathf.Deg2Rad;
                Vector3 start = position + Vector3.up * (1.8f + i * 0.15f);
                Vector3 end = position + new Vector3(Mathf.Cos(angle), 3.7f, Mathf.Sin(angle));
                GameObject branch = WorldBuilder.Shape("Tree branch", root, PrimitiveType.Cylinder, (start + end) / 2,
                    new Vector3(0.12f, Vector3.Distance(start, end) / 2, 0.12f), bark);
                branch.transform.rotation = Quaternion.FromToRotation(Vector3.up, end - start);
            }
            const int count = 140;
            var vertices = new Vector3[count * 4]; var normals = new Vector3[count * 4];
            var uv = new Vector2[count * 4]; var colours = new Color[count * 4]; var triangles = new int[count * 6];
            for (int i = 0; i < count; i++)
            {
                float height = (i + 0.5f) / count * 2 - 1;
                float angle = i * 2.399963f + seed * 0.7f;
                float ring = Mathf.Sqrt(1 - height * height);
                Vector3 center = position + new Vector3(Mathf.Cos(angle) * ring * 1.75f, 3.6f + height * 1.3f, Mathf.Sin(angle) * ring * 1.75f);
                Quaternion rotation = Quaternion.Euler(Mathf.Sin(i * 7.3f) * 50, angle * Mathf.Rad2Deg, Mathf.Cos(i * 3.7f) * 35);
                float width = 0.65f + i % 4 * 0.07f, length = width * 1.7f;
                Vector3[] corners = { new Vector3(-width/2,-length/2,0), new Vector3(width/2,-length/2,0), new Vector3(width/2,length/2,0), new Vector3(-width/2,length/2,0) };
                Vector2[] sample = { new Vector2(0.51f,0.47f), new Vector2(0.79f,0.47f), new Vector2(0.79f,1), new Vector2(0.51f,1) };
                for (int k = 0; k < 4; k++)
                {
                    int index = i * 4 + k; vertices[index] = center + rotation * corners[k]; normals[index] = rotation * Vector3.back;
                    uv[index] = sample[k]; colours[index] = Color.white * (0.85f + i % 4 * 0.08f);
                }
                int v = i * 4, t = i * 6;
                triangles[t] = v; triangles[t+1] = v+2; triangles[t+2] = v+1; triangles[t+3] = v; triangles[t+4] = v+3; triangles[t+5] = v+2;
            }
            var mesh = new Mesh { name = "Textured branch canopy", vertices = vertices, normals = normals, uv = uv, colors = colours, triangles = triangles };
            mesh.RecalculateBounds();
            root.GetComponentInParent<CourseMeshLifetime>()?.Own(mesh);
            var canopy = new GameObject("Leaf canopy", typeof(MeshFilter), typeof(MeshRenderer)); canopy.transform.SetParent(root, false);
            canopy.GetComponent<MeshFilter>().sharedMesh = mesh; canopy.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load<Material>("Environment/Foliage");
        }

        public static void StreetLight(Transform root, Vector3 position, int facing)
        {
            Material metal = WorldBuilder.Material("Streetlight metal", new Color(0.25f, 0.29f, 0.31f), 0.5f);
            WorldBuilder.Shape("Streetlight pole", root, PrimitiveType.Cylinder, position + Vector3.up * 3.2f, new Vector3(0.11f, 3.2f, 0.11f), metal);
            WorldBuilder.Box("Lamp arm", root, position + new Vector3(facing * 0.7f, 6.35f, 0), new Vector3(1.4f, 0.08f, 0.08f), metal);
            WorldBuilder.Box("LED housing", root, position + new Vector3(facing * 1.4f, 6.3f, 0), new Vector3(0.7f, 0.12f, 0.28f), metal);
        }
    }
}
