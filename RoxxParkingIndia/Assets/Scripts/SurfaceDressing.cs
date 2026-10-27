using UnityEngine;
using UnityEngine.Rendering;

namespace RoxxParking
{
    // Cosmetic surface layers have no colliders and do not alter authored clearance.
    public static class SurfaceDressing
    {
        private static Material wear;
        public static void Create(Transform root,CourseDefinition c)
        {
            if(wear==null)
            {
                wear=new Material(Resources.Load<Shader>("RoadWear")){name="Irregular oil and tyre wear"};
                wear.SetColor("_Tint",new Color(.67f,.65f,.61f,.58f));
            }
            var random=new System.Random((int)(c.mapWidth*97+c.mapLength*31+c.spawnX*13));
            int count=c.district==4?32:22;
            for(int i=0;i<count;i++)
            {
                float x=((float)random.NextDouble()-.5f)*(c.mapWidth-3),z=((float)random.NextDouble()-.5f)*(c.mapLength-4);
                GameObject patch=GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.Destroy(patch.GetComponent<Collider>());
                patch.name="Oil / tyre scuff";patch.transform.SetParent(root,false);
                patch.transform.localPosition=new Vector3(x,.023f,z);
                patch.transform.localRotation=Quaternion.Euler(90,0,(float)random.NextDouble()*360);
                patch.transform.localScale=new Vector3(1.2f+(float)random.NextDouble()*3,2+(float)random.NextDouble()*5,1);
                Renderer renderer=patch.GetComponent<Renderer>();renderer.sharedMaterial=wear;
                renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            }
            // Shallow resurfaced rectangles break the uniform repeating ground without blocking tyres.
            for(int i=0;i<4;i++)
            {
                Vector3 p=new Vector3((i%2==0?-1:1)*c.mapWidth*.34f,.012f,(i-1.5f)*Mathf.Min(c.mapLength*.17f,32));
                Material asphalt=EnvironmentArt.Surface("Repair asphalt "+i,"asphalt_02",new Color(.53f+i*.025f,.55f+i*.025f,.55f+i*.025f),Vector2.one,.72f);
                WorldBuilder.Box("Asphalt repair patch",root,p,new Vector3(3.1f,.016f,4.4f+i*.5f),asphalt);
            }
        }

        public static void JerseyBarrier(Transform root,float width,float length,Material material)
        {
            // Sloping shoulders and a narrow crown replace flat toy-like divider cubes.
            Vector2[] profile={new Vector2(-width/2,0),new Vector2(-width/2,.12f),new Vector2(-width*.23f,.38f),new Vector2(-width*.18f,.55f),new Vector2(width*.18f,.55f),new Vector2(width*.23f,.38f),new Vector2(width/2,.12f),new Vector2(width/2,0)};
            var vertices=new Vector3[16];var uv=new Vector2[16];var triangles=new int[84];
            for(int end=0;end<2;end++)for(int k=0;k<8;k++){int n=end*8+k;vertices[n]=new Vector3(profile[k].x,profile[k].y,(end-.5f)*length);uv[n]=new Vector2(profile[k].y/.8f,end*length/3);}
            int at=0;
            for(int k=0;k<8;k++){int j=(k+1)%8;foreach(int n in new[]{k,j,k+8,j,j+8,k+8})triangles[at++]=n;}
            for(int k=1;k<7;k++){triangles[at++]=0;triangles[at++]=k+1;triangles[at++]=k;triangles[at++]=8;triangles[at++]=k+8;triangles[at++]=k+9;}
            for(int t=0;t<triangles.Length;t+=3){int swap=triangles[t+1];triangles[t+1]=triangles[t+2];triangles[t+2]=swap;}
            var mesh=new Mesh{name="Precast sloping divider",vertices=vertices,uv=uv,triangles=triangles};mesh.RecalculateNormals();mesh.RecalculateTangents();
            root.GetComponentInParent<CourseMeshLifetime>()?.Own(mesh);
            var obj=new GameObject("Precast jersey barrier",typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(root,false);
            obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<MeshRenderer>().sharedMaterial=material;
        }
    }
}
