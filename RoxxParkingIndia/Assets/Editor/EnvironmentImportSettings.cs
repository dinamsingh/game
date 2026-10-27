using UnityEditor;
using UnityEngine;

public sealed class EnvironmentImportSettings : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/Environment/")) return;
        var texture = (TextureImporter)assetImporter;
        Apply(texture, assetPath);
    }

    public static void Apply(TextureImporter texture, string path)
    {
        texture.maxTextureSize = path.Contains("_2k") ? 2048 : 1024; texture.mipmapEnabled = true;
        texture.filterMode = FilterMode.Trilinear; texture.anisoLevel = 8;
        texture.textureCompression = TextureImporterCompression.CompressedHQ;
        texture.sRGBTexture = !path.Contains("_nor_gl_") && !path.Contains("_ao_") && !path.Contains("_rough_") && !path.Contains("_surface") && !path.EndsWith(".hdr") && !path.Contains("Opacity");
        if (path.Contains("_nor_gl_")) texture.textureType = TextureImporterType.NormalMap;
        if (path.EndsWith(".hdr"))
        {
            texture.textureShape = TextureImporterShape.TextureCube;
            texture.generateCubemap = TextureImporterGenerateCubemap.AutoCubemap;
            texture.maxTextureSize = 512;
            var settings = new TextureImporterSettings(); texture.ReadTextureSettings(settings);
            settings.cubemapConvolution = TextureImporterCubemapConvolution.Specular;
            texture.SetTextureSettings(settings);
        }
        texture.userData = "RoxxEnvironment2";
    }
}

public static class EnvironmentAssetsBuilder
{
    public static void Prepare()
    {
        foreach(string asset in new[]{"asphalt_02","concrete_floor_02","brick_wall_001","rock_face_03"})
        {
            string folder="Assets/Resources/Environment/",source=folder+asset+"_rough_1k.jpg",packed=folder+asset+"_surface.png";
            if(!System.IO.File.Exists(source)||System.IO.File.Exists(packed))continue;
            var rough=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
            ImageConversion.LoadImage(rough,System.IO.File.ReadAllBytes(source));
            Color32[] pixels=rough.GetPixels32();
            for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(0,0,0,(byte)(255-pixels[i].r));
            rough.SetPixels32(pixels);rough.Apply();
            System.IO.File.WriteAllBytes(packed,rough.EncodeToPNG());Object.DestroyImmediate(rough);
            AssetDatabase.ImportAsset(packed);
        }
        foreach (string file in System.IO.Directory.GetFiles("Assets/Resources/Environment"))
        {
            if (!file.EndsWith(".jpg") && !file.EndsWith(".hdr") && !file.EndsWith(".png")) continue;
            var importer = AssetImporter.GetAtPath(file) as TextureImporter;
            if (importer == null || importer.userData == "RoxxEnvironment2") continue;
            EnvironmentImportSettings.Apply(importer, file); importer.SaveAndReimport();
        }
        const string path = "Assets/Resources/Environment/DaylightSkybox.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) == null)
        {
            Cubemap sky = AssetDatabase.LoadAssetAtPath<Cubemap>("Assets/Resources/Environment/DaylightSky.hdr");
            if (sky == null) throw new System.InvalidOperationException("HDR sky did not import as a cubemap");
            var material = new Material(Shader.Find("Skybox/Cubemap"));
            material.SetTexture("_Tex", sky); material.SetFloat("_Exposure", 0.8f); material.SetFloat("_Rotation", 110);
            AssetDatabase.CreateAsset(material, path);
        }
        const string leavesPath = "Assets/Resources/Environment/Foliage.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(leavesPath) == null)
        {
            var foliage = new Material(Shader.Find("Roxx/Foliage"));
            foliage.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Environment/LeavesColor.jpg"));
            foliage.SetTexture("_OpacityMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Environment/LeavesOpacity.png"));
            foliage.SetColor("_Color", new Color(1.25f, 1.3f, 1.15f));
            AssetDatabase.CreateAsset(foliage, leavesPath);
        }
        const string surfacePath = "Assets/Resources/Environment/TexturedSurface.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(surfacePath) == null)
        {
            // Keep the runtime ground material's normal-map shader variant in stripped players.
            var surface = new Material(Shader.Find("Standard"));
            surface.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Environment/asphalt_02_diff_1k.jpg"));
            surface.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Environment/asphalt_02_nor_gl_1k.jpg"));
            surface.EnableKeyword("_NORMALMAP"); AssetDatabase.CreateAsset(surface, surfacePath);
        }
        Material retained=AssetDatabase.LoadAssetAtPath<Material>(surfacePath);
        retained.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Environment/asphalt_02_surface.png"));
        retained.SetTexture("_OcclusionMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Environment/asphalt_02_ao_1k.jpg"));
        retained.EnableKeyword("_METALLICGLOSSMAP");retained.EnableKeyword("_NORMALMAP");EditorUtility.SetDirty(retained);
    }
}
