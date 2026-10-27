using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class WorkshopImportSettings : AssetPostprocessor
{
    private void OnPreprocessModel()
    {
        if(!assetPath.StartsWith("Assets/Art/Workshop/"))return;
        var importer=(ModelImporter)assetImporter;
        importer.importCameras=false;importer.importLights=false;importer.importAnimation=false;
        importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.None;
    }
}

public static class WorkshopAssetsBuilder
{
    public static void Prepare()
    {
        const string folder="Assets/Resources/Workshop";Directory.CreateDirectory(folder);
        foreach(string id in new[]{"wooden_crate_01","barrel_03","industrial_storage_cart"})
        {
            string path=folder+"/"+id;
            if(AssetDatabase.LoadAssetAtPath<GameObject>(path+".prefab")!=null)continue;
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Workshop/"+id+".fbx");
            if(source==null)throw new InvalidOperationException("Missing licensed workshop model: "+id);
            GameObject model=UnityEngine.Object.Instantiate(source),baked=new GameObject(id);
            try
            {
                var filters=model.GetComponentsInChildren<MeshFilter>();
                var renderers=model.GetComponentsInChildren<MeshRenderer>();
                if(filters.Length==0 || renderers.Length==0)throw new InvalidOperationException("Empty workshop model: "+id);
                Bounds bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                Vector3 size=bounds.size;
                if(Mathf.Min(size.x,size.y,size.z)<.001f)throw new InvalidOperationException("Degenerate workshop model: "+id);
                var matrix=Matrix4x4.Scale(new Vector3(1/size.x,1/size.y,1/size.z))*Matrix4x4.Translate(-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z));
                var parts=new List<CombineInstance>();
                foreach(var f in filters)for(int s=0;s<f.sharedMesh.subMeshCount;s++)
                    parts.Add(new CombineInstance{mesh=f.sharedMesh,subMeshIndex=s,transform=matrix*f.transform.localToWorldMatrix});
                var mesh=new Mesh{name=id,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
                mesh.CombineMeshes(parts.ToArray(),true,true);mesh.RecalculateBounds();
                int triangles=mesh.triangles.Length/3;
                if(triangles>25000)throw new InvalidOperationException("Workshop prop exceeds per-model triangle budget: "+id+" / "+triangles);
                AssetDatabase.CreateAsset(mesh,path+".asset");
                var material=new Material(Shader.Find("Standard")){name=id};
                material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Environment/"+id+"_diff_1k.jpg");
                material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Environment/"+id+"_nor_gl_1k.jpg"));material.EnableKeyword("_NORMALMAP");
                material.SetTexture("_OcclusionMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Environment/"+id+"_ao_1k.jpg"));
                material.SetFloat("_Glossiness",id=="wooden_crate_01"?.18f:.38f);material.SetFloat("_Metallic",id=="wooden_crate_01"?0:.6f);
                AssetDatabase.CreateAsset(material,path+".mat");
                baked.AddComponent<MeshFilter>().sharedMesh=mesh;baked.AddComponent<MeshRenderer>().sharedMaterial=material;
                PrefabUtility.SaveAsPrefabAsset(baked,path+".prefab");
                Debug.Log("WORKSHOP_PROP_READY: "+id+" / "+triangles+" triangles / normal and AO mapped");
            }
            finally{UnityEngine.Object.DestroyImmediate(model);UnityEngine.Object.DestroyImmediate(baked);}
        }
        AssetDatabase.SaveAssets();
    }
}
