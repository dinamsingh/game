using UnityEngine;

namespace RoxxParking
{
    // Original, metre-scaled scenery; all collision geometry stays inside the course definition.
    public static class DistrictArt
    {
        public static readonly string[] Names = { "CITY / DRIVING SCHOOL", "OLD TOWN / WORKSHOPS", "DOCKS / FREIGHT TERMINAL", "HILLS / RIDGE OUTPOST", "PROVING GROUND / CENTURY SPRINT" };
        private static Material Steel => WorldBuilder.Material("Galvanised steel", new Color(.32f,.36f,.37f), .45f);
        private static Material Paint => WorldBuilder.Material("Worn road paint", new Color(.78f,.76f,.63f));
        private static Material Concrete => EnvironmentArt.Surface("Poured concrete", "concrete_floor_02", new Color(.68f,.67f,.61f), new Vector2(2,1));
        private static GameObject Box(string name, Transform root, Vector3 p, Vector3 s, Material m) => WorldBuilder.Box(name,root,p,s,m);

        public static void Create(Transform root, CourseDefinition c)
        {
            if(c.district==4){ProvingGround(root,c);return;}
            if(c.district==1) { WorkshopArt.Create(root,c);return; }
            float w=c.mapWidth, l=c.mapLength;
            Material ground=EnvironmentArt.Surface("District asphalt", "asphalt_02", new Color(.64f,.66f,.67f), new Vector2(w/3,l/3));
            WorldBuilder.Box("Road surface",root,new Vector3(0,-.12f,0),new Vector3(w,.24f,l),ground,true);
            Material earth=EnvironmentArt.Surface("Dusty outskirts","concrete_floor_02",new Color(.44f,.43f,.35f),new Vector2(80,80));
            Box("Surroundings",root,new Vector3(0,-.42f,0),new Vector3(260,.5f,260),earth);
            for(int side=-1;side<=1;side+=2)
            {
                float x=side*(w/2+2);
                Box("Paved verge",root,new Vector3(x,-.02f,0),new Vector3(3.8f,.22f,l),Concrete);
                for(float z=-l/2+1;z<l/2;z+=2)
                    Box("Kerbstone",root,new Vector3(side*(w/2+.12f),.1f,z),new Vector3(.24f,.2f,1.96f),Concrete);
                for(float z=-l/2+5;z<l/2;z+=16)
                {
                    EnvironmentArt.StreetLight(root,new Vector3(side*(w/2+1.3f),0,z),-side);
                    if(c.district==0 || c.district==3) EnvironmentArt.Tree(root,new Vector3(side*(w/2+4.4f),0,z+3),(int)(z+l));
                    if(c.district==0) Bench(root,new Vector3(side*(w/2+2),0,z+6),-side*90);
                }
            }
            if(c.district==0) City(root,w,l);
            else if(c.district==2) Docks(root,w,l);
            else Ridge(root,w,l);
            // Road wear, expansion joints and drainage remain flat and never obstruct a tyre.
            for(int side=-1;side<=1;side+=2)
                for(float z=-l/2+4;z<l/2;z+=12)
                {
                    Box("Drain recess",root,new Vector3(side*(w/2-.35f),.008f,z),new Vector3(.48f,.012f,.9f),Steel);
                    for(int slit=0;slit<7;slit++) Box("Drain slots",root,new Vector3(side*(w/2-.35f),.018f,z-.36f+slit*.12f),new Vector3(.42f,.01f,.035f),WorldBuilder.Material("Drain shadow",new Color(.055f,.065f,.065f)));
                }
        }

        private static void City(Transform root,float w,float l)
        {
            for(int side=-1;side<=1;side+=2)
            {
                Fence(root,new Vector3(side*(w/2+3.6f),0,0),l,0);
                for(int i=0;i<4;i++) EnvironmentArt.Building(root,new Vector3(side*(w/2+14),0,-l/2+7+i*14),3+i%2,i,-side);
            }
            for(int end=-1;end<=1;end+=2)
                for(int i=-1;i<=1;i++)
                {
                    Transform block=EnvironmentArt.Building(root,new Vector3(i*15,0,end*(l/2+13)),3+(i+1)%2,i+6,end);
                    block.localRotation=Quaternion.Euler(0,90,0);
                }
            for(int row=-1;row<=1;row+=2)
                for(float z=-l/2+2;z<l/2-2;z+=5.5f)
                    Box("School parking stripe",root,new Vector3(row*(w/2-3),.015f,z),new Vector3(4,.018f,.08f),Paint);
        }

        private static void ProvingGround(Transform root,CourseDefinition c)
        {
            float w=c.mapWidth,l=c.mapLength;
            WorldBuilder.Box("Test track tarmac",root,new Vector3(0,-.12f,0),new Vector3(w,.24f,l),EnvironmentArt.Surface("Test track asphalt","asphalt_02",new Color(.79f,.8f,.8f),Vector2.one,.75f),true);
            Box("Gravel shoulders",root,new Vector3(0,-.4f,0),new Vector3(230,.5f,l+140),EnvironmentArt.Surface("Track verge gravel","rock_face_03",new Color(.58f,.54f,.43f),Vector2.one));
            for(float z=-l/2+4;z<l/2;z+=8)
            {
                foreach(float x in new[]{-3.5f,3.5f})Box("Runway lane dash",root,new Vector3(x,.016f,z),new Vector3(.14f,.012f,3),Paint);
                foreach(int side in new[]{-1,1})
                {
                    float x=side*(w/2+1);
                    Box("Galvanised crash rail",root,new Vector3(x,.65f,z),new Vector3(.12f,.32f,8),Steel);
                    Box("Rail lower fold",root,new Vector3(x,.47f,z),new Vector3(.2f,.04f,8),Steel);
                    Box("Rail post",root,new Vector3(x, .4f,z),new Vector3(.12f,.8f,.14f),Steel);
                }
            }
            foreach(float z in new[]{-140f,-30f,160f})
            {
                Transform gantry=new GameObject("Track sign gantry").transform;gantry.SetParent(root,false);gantry.localPosition=new Vector3(0,0,z);gantry.localRotation=Quaternion.Euler(0,180,0);
                foreach(int side in new[]{-1,1})Box("Gantry upright",gantry,new Vector3(side*12,2.8f,0),new Vector3(.18f,5.6f,.18f),Steel);
                Box("Gantry crossbeam",gantry,new Vector3(0,5.6f,0),new Vector3(24,.25f,.25f),Steel);
                WorkshopArt.Sign(gantry,new Vector3(0,5.6f,.2f),z< -50?"PROVING GROUND / 100 KM/H":z<0?"90+ KM/H / SPEED SENSOR":"BRAKE / PRECISION STOP",10,1);
                if(z>0)for(int stripe=0;stripe<5;stripe++)Box("Braking distance marker",root,new Vector3(0,.02f,z+stripe*8),new Vector3(8,.015f,.3f+stripe*.07f),Paint);
            }
            for(int side=-1;side<=1;side+=2)for(int i=0;i<4;i++)
            {
                Mountain(root,new Vector3(side*(w/2+77+i%2*25),-5,-240+i*160),30+i*7,65);
                EnvironmentArt.Tree(root,new Vector3(side*(w/2+8),0,-260+i*160),i+side+30);
            }
            Warehouse(root,new Vector3(0,0,l/2+14),180,24,12);
        }

        private static void Docks(Transform root,float w,float l)
        {
            for(int side=-1;side<=1;side+=2)
            {
                Fence(root,new Vector3(side*(w/2+4),0,0),l+6,0);
                for(int i=0;i<5;i++)
                {
                    Vector3 p=new Vector3(side*(w/2+9),0,-l/2+5+i*13);
                    Container(root,p,0,2.44f,12.2f,i);
                    if(i%2==0) Container(root,p+Vector3.up*2.6f,0,2.44f,12.2f,i+1);
                }
            }
            Warehouse(root,new Vector3(0,0,l/2+14),180,38,16);
            for(int i=-1;i<=1;i+=2)
            {
                Box("Gantry leg",root,new Vector3(i*(w/2+5),9,-l/2-10),new Vector3(1.2f,18,1.5f),Steel);
                Box("Gantry bracing",root,new Vector3(i*(w/2+5),4,-l/2-10),new Vector3(4,.7f,3),Steel);
            }
            Box("Gantry beam",root,new Vector3(0,17.5f,-l/2-10),new Vector3(w+13,1.4f,2),Steel);
            for(float z=-l/2+3;z<l/2;z+=6) Box("Freight lane dash",root,new Vector3(0,.015f,z),new Vector3(.12f,.016f,2.5f),Paint);
        }

        private static void Ridge(Transform root,float w,float l)
        {
            for(int side=-1;side<=1;side+=2)
            {
                Fence(root,new Vector3(side*(w/2+2),0,0),l,0);
                for(int i=0;i<3;i++) Mountain(root,new Vector3(side*(w/2+40+i*18),-5,-l/2+i*42),25+i*9,35+i*7);
            }
            Warehouse(root,new Vector3(-w/2-9,0,10),90,18,12);
            Warehouse(root,new Vector3(0,0,l/2+9),180,16,10);
            for(float z=-l/2+2;z<l/2;z+=4)
            {
                Box("Ridge centre line",root,new Vector3(0,.018f,z),new Vector3(.11f,.018f,2),Paint);
                foreach(float x in new[]{-w/2+1,w/2-1}) Box("Road edge",root,new Vector3(x,.016f,z),new Vector3(.11f,.014f,4),Paint);
            }
        }

        private static void Mountain(Transform root,Vector3 p,float height,float radius)
        {
            const int cells=22;
            var vertices=new Vector3[(cells+1)*(cells+1)];var uv=new Vector2[vertices.Length];var triangles=new int[cells*cells*6];
            for(int z=0;z<=cells;z++) for(int x=0;x<=cells;x++)
            {
                float nx=(float)x/cells*2-1,nz=(float)z/cells*2-1;
                float envelope=Mathf.Pow(Mathf.Clamp01(1-nx*nx-nz*nz),.7f);
                float noise=.42f+.65f*Mathf.PerlinNoise(nx*2.2f+p.x*.03f+17,nz*2.2f+p.z*.03f+23)+.15f*Mathf.PerlinNoise(nx*8+7,nz*8+9);
                int index=z*(cells+1)+x;vertices[index]=new Vector3(nx*radius,envelope*noise*height,nz*radius);uv[index]=new Vector2(nx*radius/7,nz*radius/7);
                if(x==cells || z==cells)continue;
                int t=(z*cells+x)*6;triangles[t]=index;triangles[t+1]=index+cells+1;triangles[t+2]=index+1;
                triangles[t+3]=index+1;triangles[t+4]=index+cells+1;triangles[t+5]=index+cells+2;
            }
            var mesh=new Mesh{name="Distant ridge",vertices=vertices,triangles=triangles,uv=uv}; mesh.RecalculateNormals();
            root.GetComponentInParent<CourseMeshLifetime>()?.Own(mesh);
            var obj=new GameObject("Distant rocky ridge",typeof(MeshFilter),typeof(MeshRenderer)); obj.transform.SetParent(root,false);obj.transform.localPosition=p;
            mesh.RecalculateTangents();
            obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<MeshRenderer>().sharedMaterial=EnvironmentArt.Surface("Ridge rock","rock_face_03",new Color(.88f,.88f,.84f),Vector2.one);
        }

        private static void Warehouse(Transform root,Vector3 p,float yaw,float width,float depth)
        {
            Transform building=new GameObject("Brick workshop").transform; building.SetParent(root,false);building.localPosition=p;building.localRotation=Quaternion.Euler(0,yaw,0);
            Material brick=EnvironmentArt.Surface("Workshop brick "+width,"brick_wall_001",new Color(.79f,.77f,.70f),new Vector2(width/3,2));
            Box("Masonry",building,new Vector3(0,3,0),new Vector3(width,6,depth),brick);
            Box("Roof flashing",building,new Vector3(0,6.1f,0),new Vector3(width+.5f,.2f,depth+.5f),Steel);
            for(float x=-width/2+3;x<width/2-1;x+=6)
            {
                Box("Loading door surround",building,new Vector3(x,2.15f,depth/2+.06f),new Vector3(4.9f,4.5f,.18f),Concrete);
                Box("Roller shutter",building,new Vector3(x,2.1f,depth/2+.17f),new Vector3(4.45f,4.1f,.06f),Steel);
                for(float y=.15f;y<4.15f;y+=.18f) Box("Shutter slat",building,new Vector3(x,y,depth/2+.22f),new Vector3(4.45f,.035f,.045f),WorldBuilder.Material("Shutter edge",new Color(.21f,.23f,.23f),.3f));
                Box("Door handle",building,new Vector3(x,.8f,depth/2+.27f),new Vector3(.5f,.06f,.08f),Steel);
                Box("Lintel",building,new Vector3(x,4.6f,depth/2+.18f),new Vector3(5.1f,.22f,.35f),Concrete);
            }
            for(int side=-1;side<=1;side+=2)
                Box("Rainwater pipe",building,new Vector3(side*(width/2-.2f),2.9f,depth/2+.2f),new Vector3(.11f,5.8f,.11f),Steel);
        }

        public static void Container(Transform root,Vector3 p,float yaw,float width,float length,int variant)
        {
            Transform obj=new GameObject("Weathered freight container").transform;obj.SetParent(root,false);obj.localPosition=p;obj.localRotation=Quaternion.Euler(0,yaw,0);
            Color[] colours={new Color(.20f,.29f,.32f),new Color(.43f,.24f,.16f),new Color(.36f,.39f,.28f)};
            Material metal=EnvironmentArt.Surface("Freight paint "+variant%3,"rusty_painted_metal",colours[variant%3]*1.55f,Vector2.one,.65f);
            Box("Container shell",obj,new Vector3(0,1.3f,0),new Vector3(width,2.6f,length),metal);
            for(int side=-1;side<=1;side+=2)
            {
                for(float z=-length/2+.12f;z<length/2;z+=.28f) Box("Pressed steel rib",obj,new Vector3(side*(width/2+.025f),1.3f,z),new Vector3(.055f,2.45f,.06f),metal);
                Box("Container top rail",obj,new Vector3(side*width/2,2.56f,0),new Vector3(.10f,.12f,length),Steel);
                for(int door=-1;door<=1;door+=2) Box("Door locking bar",obj,new Vector3(door*width*.23f,1.3f,side*(length/2+.04f)),new Vector3(.035f,2.4f,.045f),Steel);
            }
            Box("Container ID plate",obj,new Vector3(-width/2-.06f,1.75f,length*.25f),new Vector3(.018f,.24f,1.1f),Paint);
        }

        private static void Bench(Transform root,Vector3 p,float yaw)
        {
            Transform obj=new GameObject("Timber bench").transform;obj.SetParent(root,false);obj.localPosition=p;obj.localRotation=Quaternion.Euler(0,yaw,0);
            Material wood=WorldBuilder.Material("Oiled timber",new Color(.28f,.19f,.11f),.15f);
            for(int i=0;i<4;i++)
            {
                Box("Seat slat",obj,new Vector3(0,.48f,-.22f+i*.14f),new Vector3(1.9f,.055f,.115f),wood);
                Box("Back slat",obj,new Vector3(0,.68f+i*.12f,.28f),new Vector3(1.9f,.09f,.055f),wood);
            }
            foreach(float x in new[]{-.7f,.7f}) Box("Bench leg",obj,new Vector3(x,.25f,0),new Vector3(.08f,.5f,.46f),Steel);
        }

        public static void Fence(Transform root,Vector3 p,float length,float yaw)
        {
            Transform obj=new GameObject("Wire perimeter fence").transform;obj.SetParent(root,false);obj.localPosition=p;obj.localRotation=Quaternion.Euler(0,yaw,0);
            for(float z=-length/2;z<=length/2;z+=3) Box("Fence post",obj,new Vector3(0,1.1f,z),new Vector3(.065f,2.2f,.065f),Steel);
            foreach(float y in new[]{.25f,2.05f}) Box("Fence rail",obj,new Vector3(0,y,0),new Vector3(.045f,.045f,length),Steel);
            // Crossed thin wires read as chain link without an opaque checkerboard texture.
            for(float z=-length/2+.8f;z<length/2-.8f;z+=.42f)
                foreach(float angle in new[]{-42f,42f})
                {
                    GameObject wire=Box("Diagonal fence wire",obj,new Vector3(0,1.15f,z),new Vector3(.009f,2.38f,.009f),Steel);
                    wire.transform.localRotation=Quaternion.Euler(angle,0,0);
                }
        }

        private static void Barrel(Transform root,Vector3 p)
        {
            Material paint=WorldBuilder.Material("Used oil drum",new Color(.24f,.29f,.28f),.25f);
            WorldBuilder.Shape("Oil drum",root,PrimitiveType.Cylinder,p+Vector3.up*.44f,new Vector3(.58f,.44f,.58f),paint);
            foreach(float y in new[]{.12f,.72f}) WorldBuilder.Shape("Drum hoop",root,PrimitiveType.Cylinder,p+Vector3.up*y,new Vector3(.60f,.018f,.60f),Steel);
        }

        public static void Obstacle(Transform root,BarrierDefinition b,int index)
        {
            Transform obj=new GameObject("Obstacle - "+(b.kind??"concrete")).transform;obj.SetParent(root,false);
            obj.localPosition=new Vector3(b.x,0,b.z);obj.localRotation=Quaternion.Euler(0,b.yaw,0);
            obj.gameObject.layer=8;obj.gameObject.AddComponent<CourseObstacle>();
            float height=b.kind=="warehouse"?6f:b.kind=="post"?3.6f:b.kind=="container"?2.6f:b.kind=="crate"?1.2f:.55f;
            BoxCollider collision=obj.gameObject.AddComponent<BoxCollider>();collision.center=Vector3.up*height/2;collision.size=new Vector3(b.width,height,b.length);
            if(b.kind=="post")
            {
                Box("Gate steel upright",obj,Vector3.up*height/2,new Vector3(b.width,height,b.length),Steel);
                Box("Upright warning band",obj,Vector3.up*.85f,new Vector3(b.width+.01f,.22f,b.length+.01f),Paint);
            }
            else if(b.kind=="warehouse")Warehouse(obj,Vector3.zero,0,b.width,b.length);
            else if(b.kind=="container")
            {
                int count=Mathf.CeilToInt(b.length/12.2f);float length=b.length/count;
                for(int i=0;i<count;i++)Container(obj,new Vector3(0,0,-b.length/2+length*(i+.5f)),0,b.width,length-.06f,index+i);
            }
            else if(b.kind=="crate")
            {
                int columns=Mathf.Max(1,Mathf.CeilToInt(b.width/1.5f)),rows=Mathf.Max(1,Mathf.CeilToInt(b.length/1.8f));
                float width=b.width/columns,length=b.length/rows;
                for(int x=0;x<columns;x++) for(int z=0;z<rows;z++)
                {
                    Vector3 p=new Vector3(-b.width/2+width*(x+.5f),0,-b.length/2+length*(z+.5f));
                    WorkshopArt.Crate(obj,p,width-.04f,length-.04f,1.2f);
                }
            }
            else
            {
                for(float z=-b.length/2;z<b.length/2;z+=2.5f)
                {
                    float segment=Mathf.Min(2.48f,b.length/2-z);
                    if(b.width<=1.2f)
                    {
                        Transform part=new GameObject("Precast segment").transform;part.SetParent(obj,false);part.localPosition=new Vector3(0,0,z+segment/2);
                        SurfaceDressing.JerseyBarrier(part,b.width,segment,Concrete);
                    }
                    else Box("Concrete divider",obj,new Vector3(0,.27f,z+segment/2),new Vector3(b.width,.54f,segment),Concrete);
                    Box("Divider reflector",obj,new Vector3(0,.548f,z+segment/2),new Vector3(Mathf.Min(.12f,b.width),.02f,.16f),Paint);
                }
            }
        }
    }
}
