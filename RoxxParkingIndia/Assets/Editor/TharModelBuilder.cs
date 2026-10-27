using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class TharImportSettings : AssetPostprocessor
{
    private void OnPreprocessModel()
    {
        if (assetPath != TharModelBuilder.SourcePath) return;
        var importer = (ModelImporter)assetImporter;
        importer.importCameras = false; importer.importLights = false; importer.importAnimation = false;
        importer.isReadable = true; importer.meshCompression = ModelImporterMeshCompression.Low;
        importer.optimizeMeshPolygons = true; importer.optimizeMeshVertices = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
    }
}

public static class TharModelBuilder
{
    public const string SourcePath = "Assets/Art/TharRoxx/TharRoxx.dae";
    private const string OutputRoot = "Assets/Resources/Thar";

    public static void EnsureBuilt()
    {
        if (!File.Exists(OutputRoot + "/TharRoxx.prefab")) Prepare();
    }

    public static void Prepare()
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
        if (source == null) throw new InvalidOperationException("Thar DAE model did not import");
        GameObject model = UnityEngine.Object.Instantiate(source);
        try
        {
            var meshes = model.GetComponentsInChildren<MeshFilter>();
            var renderers = model.GetComponentsInChildren<MeshRenderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            var report = new Report
            {
                center = bounds.center, size = bounds.size,
                triangles = meshes.Sum(m => m.sharedMesh.triangles.Length / 3),
                materials = renderers.SelectMany(r => r.sharedMaterials).Select(m => m.name).Distinct().ToArray(),
                parts = meshes.Select(m => new Part { path = AnimationUtility.CalculateTransformPath(m.transform, model.transform),
                    center = m.GetComponent<Renderer>().bounds.center, size = m.GetComponent<Renderer>().bounds.size,
                    triangles = m.sharedMesh.triangles.Length / 3 }).ToArray()
            };
            Directory.CreateDirectory("Builds/Checks");
            File.WriteAllText("Builds/Checks/thar-import.json", JsonUtility.ToJson(report, true));
            Debug.Log($"THAR_IMPORT: {meshes.Length} meshes, {report.triangles} triangles, size {bounds.size}");
            Bake(model, bounds);
        }
        finally { UnityEngine.Object.DestroyImmediate(model); }
    }

    private static void Bake(GameObject source, Bounds bounds)
    {
        Directory.CreateDirectory(OutputRoot);
        var sourceMeshes = source.GetComponentsInChildren<MeshFilter>();
        string prefix = "instance_0/Mahindra_-_Thar_Roxx/";
        // The supplied SketchUp model faces +X; wheel groups identify its four complete brake assemblies.
        string[] groups = { "group_21", "group_70", "group_64", "group_41" };
        string[] labels = { "FL", "FR", "RL", "RR" };
        var assemblies = groups.Select(g => source.transform.Find(prefix + g)).ToArray();
        if (assemblies.Any(t => t == null)) throw new InvalidDataException("Supplied Thar wheel hierarchy changed");
        Bounds[] wheelBounds = assemblies.Select(t => BoundsOf(t.GetComponentsInChildren<MeshRenderer>())).ToArray();
        float wheelY = wheelBounds.Average(b => b.center.y);
        float scale = Mathf.Min(4.5f / bounds.size.x, 1.96f / bounds.size.z);
        Matrix4x4 normalize = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, -90, 0), Vector3.one * scale)
            * Matrix4x4.Translate(new Vector3(-bounds.center.x, -wheelY, -bounds.center.z));
        var materials = new Dictionary<string, Material>();
        var mirrored = new Dictionary<Mesh, Mesh>();
        GameObject output = new GameObject("Thar Roxx supplied model");
        try
        {
            Transform body = new GameObject("Body").transform; body.SetParent(output.transform, false);
            Combine(sourceMeshes.Where(m => !assemblies.Any(a => m.transform.IsChildOf(a))).ToArray(), body,
                normalize, materials, mirrored, "Body");
            for (int i = 0; i < 4; i++)
            {
                Vector3 position = normalize.MultiplyPoint3x4(wheelBounds[i].center); position.y = 0;
                Transform spin = new GameObject("Wheel" + labels[i]).transform; spin.SetParent(output.transform, false); spin.localPosition = position;
                Transform hub = new GameObject("Hub" + labels[i]).transform; hub.SetParent(output.transform, false); hub.localPosition = position;
                MeshFilter[] all = assemblies[i].GetComponentsInChildren<MeshFilter>();
                bool Rotates(MeshFilter m) => m.GetComponentsInParent<Transform>().Any(t => t.name == "Wheel");
                Matrix4x4 pivot = Matrix4x4.Translate(-position) * normalize;
                Combine(all.Where(Rotates).ToArray(), spin, pivot, materials, mirrored, "Wheel" + labels[i]);
                Combine(all.Where(m => !Rotates(m)).ToArray(), hub, pivot, materials, mirrored, "Hub" + labels[i]);
            }
            PrefabUtility.SaveAsPrefabAsset(output, OutputRoot + "/TharRoxx.prefab");
            AssetDatabase.SaveAssets();
            int count = output.GetComponentsInChildren<MeshRenderer>().Length;
            File.WriteAllText("Builds/Checks/thar-optimized.json", JsonUtility.ToJson(new OptimizedReport
            { renderers = count, triangles = output.GetComponentsInChildren<MeshFilter>().Sum(m => m.sharedMesh.triangles.Length / 3),
                size = BoundsOf(output.GetComponentsInChildren<MeshRenderer>()).size, wheelRadius = wheelBounds[0].size.y * scale / 2 }, true));
            Debug.Log("THAR_BAKED: " + count + " material-combined renderers; four independent wheels and brake hubs.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(output);
            foreach (Mesh mesh in mirrored.Values) UnityEngine.Object.DestroyImmediate(mesh);
        }
    }

    private static Bounds BoundsOf(MeshRenderer[] renderers)
    {
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    private static void Combine(MeshFilter[] parts, Transform parent, Matrix4x4 normalize,
        Dictionary<string, Material> materials, Dictionary<Mesh, Mesh> mirrored, string label)
    {
        var batches = new Dictionary<Material, List<CombineInstance>>();
        foreach (MeshFilter part in parts)
        {
            Matrix4x4 matrix = normalize * part.transform.localToWorldMatrix;
            Mesh mesh = part.sharedMesh;
            // Mirrored SketchUp instances need reversed winding after their scale is baked into vertices.
            if (matrix.determinant < 0)
            {
                if (!mirrored.TryGetValue(mesh, out Mesh reversed))
                {
                    reversed = UnityEngine.Object.Instantiate(mesh);
                    for (int s = 0; s < mesh.subMeshCount; s++)
                    {
                        int[] indices = mesh.GetTriangles(s);
                        for (int j = 0; j < indices.Length; j += 3) { int a = indices[j]; indices[j] = indices[j + 2]; indices[j + 2] = a; }
                        reversed.SetTriangles(indices, s);
                    }
                    mirrored.Add(mesh, reversed);
                }
                mesh = reversed;
            }
            Material[] original = part.GetComponent<MeshRenderer>().sharedMaterials;
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                Material material = Surface(original[Mathf.Min(s, original.Length - 1)], materials);
                if (!batches.TryGetValue(material, out var batch)) batches.Add(material, batch = new List<CombineInstance>());
                batch.Add(new CombineInstance { mesh = mesh, subMeshIndex = s, transform = matrix });
            }
        }
        foreach (var batch in batches)
        {
            string path = OutputRoot + "/" + label + "_" + batch.Key.name.Replace(' ', '_') + ".asset";
            Mesh mesh = new Mesh { name = label + " " + batch.Key.name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.CombineMeshes(batch.Value.ToArray(), true, true); mesh.RecalculateBounds();
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) { EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); mesh = existing; }
            else AssetDatabase.CreateAsset(mesh, path);
            var obj = new GameObject(batch.Key.name, typeof(MeshFilter), typeof(MeshRenderer)); obj.transform.SetParent(parent, false);
            obj.GetComponent<MeshFilter>().sharedMesh = mesh; obj.GetComponent<MeshRenderer>().sharedMaterial = batch.Key;
        }
    }

    private static Material Surface(Material source, Dictionary<string, Material> cache)
    {
        string name = source.name;
        string category = name == "Carpaint_Flakes_White" ? "Thar paint"
            : name.Contains("Glass") ? (name.Contains("Mirror") ? "Thar mirror" : "Thar glass")
            : name == "Roof_Color" ? "Thar roof"
            : name == "Color_M07" || name == "Color_M08" ? "Thar rubber"
            : name == "Rim_Ring_Color" ? "Thar dark alloy"
            : name == "Color_B06" ? "Thar tail lights" : name == "Signal_Color" ? "Thar indicators"
            : name == "material" || name == "Color_M00" ? "Thar white" : "Thar alloy";
        if (cache.TryGetValue(category, out Material material)) return material;
        string path = OutputRoot + "/" + category.Replace(' ', '_') + ".mat";
        material = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool create = material == null;
        if (create) material = new Material(Shader.Find("Standard"));
        material.name = category;
        Color color = source.color; color.a = 1;
        float metal = 0, smooth = 0.35f;
        switch (category)
        {
            case "Thar paint": color = new Color(0.84f, 0.83f, 0.78f); metal = 0.35f; smooth = 0.8f; break;
            case "Thar roof": color = new Color(0.035f, 0.038f, 0.04f); smooth = 0.6f; break;
            case "Thar rubber": color = new Color(0.035f, 0.038f, 0.04f); smooth = 0.22f; break;
            case "Thar glass": color = new Color(0.045f, 0.075f, 0.09f); metal = 0.3f; smooth = 0.94f; break;
            case "Thar mirror": color = new Color(0.8f, 0.83f, 0.85f); metal = 1; smooth = 0.97f; break;
            case "Thar dark alloy": color = new Color(0.14f, 0.15f, 0.16f); metal = 0.8f; smooth = 0.7f; break;
            case "Thar alloy": color = new Color(0.58f, 0.6f, 0.62f); metal = 0.8f; smooth = 0.65f; break;
            case "Thar tail lights": color = new Color(0.65f, 0.015f, 0.01f); smooth = 0.75f; break;
            case "Thar indicators": color = new Color(1, 0.42f, 0.04f); smooth = 0.75f; break;
            case "Thar white": color = new Color(0.85f, 0.86f, 0.82f); smooth = 0.6f; break;
        }
        material.color = color; material.SetFloat("_Metallic", metal); material.SetFloat("_Glossiness", smooth);
        if (category == "Thar tail lights") { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * 0.3f); }
        if (create) AssetDatabase.CreateAsset(material, path); else EditorUtility.SetDirty(material);
        cache.Add(category, material); return material;
    }

    [Serializable] private sealed class Report
    {
        public Vector3 center, size;
        public int triangles;
        public string[] materials;
        public Part[] parts;
    }
    [Serializable] private sealed class Part { public string path; public Vector3 center, size; public int triangles; }
    [Serializable] private sealed class OptimizedReport { public int renderers, triangles; public Vector3 size; public float wheelRadius; }
}
