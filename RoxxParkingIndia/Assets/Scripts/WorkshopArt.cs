using UnityEngine;
using UnityEngine.Rendering;

namespace RoxxParking
{
    // Original service-yard kit. Drivable obstacles remain defined in courses.json.
    public static class WorkshopArt
    {
        private static Material Metal => EnvironmentArt.Surface("Workshop worn enamel", "rusty_painted_metal", new Color(.64f,.70f,.68f), Vector2.one, .67f);
        private static Material Wood => EnvironmentArt.Surface("Shipping timber", "wooden_rough_planks", new Color(.79f,.71f,.60f), Vector2.one);
        private static Material Dark => WorldBuilder.Material("Workshop black steel", new Color(.105f,.13f,.14f), .32f);
        private static Material Cream => WorldBuilder.Material("Workshop ivory enamel", new Color(.78f,.76f,.65f), .15f);
        private static GameObject Box(string n, Transform t, Vector3 p, Vector3 s, Material m) => WorldBuilder.Box(n,t,p,s,m);
        private static Transform Group(string n,Transform t,Vector3 p,float yaw=0)
        {
            Transform g=new GameObject(n).transform;g.SetParent(t,false);g.localPosition=p;g.localRotation=Quaternion.Euler(0,yaw,0);return g;
        }

        private static bool Prop(string id,Transform root,Vector3 p,Vector3 size,float yaw=0)
        {
            GameObject prefab=Resources.Load<GameObject>("Workshop/"+id);
            if(prefab==null)return false;
            Transform obj=Object.Instantiate(prefab,root,false).transform;obj.localPosition=p;
            obj.localRotation=Quaternion.Euler(0,yaw,0);obj.localScale=size;return true;
        }

        public static void Create(Transform root,CourseDefinition c)
        {
            float w=c.mapWidth,l=c.mapLength;
            Material slab=EnvironmentArt.Surface("Service yard concrete","concrete_floor_02",new Color(.79f,.78f,.71f),Vector2.one);
            WorldBuilder.Box("Service yard slab",root,new Vector3(0,-.12f,0),new Vector3(w,.24f,l),slab,true);
            Box("Neighbourhood ground",root,new Vector3(0,-.35f,0),new Vector3(200,.4f,200),WorldBuilder.Material("Workshop outskirts soil",new Color(.33f,.32f,.28f)));
            Material bitumen=EnvironmentArt.Surface("Delivery lane asphalt","asphalt_02",new Color(.73f,.74f,.71f),Vector2.one);
            Box("Resurfaced entrance",root,new Vector3(-w*.28f,.006f,-l*.32f),new Vector3(6,.012f,l*.36f),bitumen);
            // Asymmetry, open bays and separate roof lines make the yard read as one working place.
            ServiceBlock(root,new Vector3(-3,0,l/2+5.4f),180,24,10);
            ServiceBlock(root,new Vector3(-w/2-5.6f,0,3),90,19,9);
            Office(root,new Vector3(w/2+4.1f,0,-l*.24f),-90);
            Canopy(root,new Vector3(w/2+2.4f,0,l*.22f),-90);
            DistrictArt.Fence(root,new Vector3(0,0,-l/2-1.2f),w+6,90);
            DistrictArt.Fence(root,new Vector3(w/2+1.3f,0,l*.2f),l*.55f,0);
            for(int side=-1;side<=1;side+=2)
            {
                Box("Edge drainage channel",root,new Vector3(side*(w/2-.28f),.007f,0),new Vector3(.4f,.012f,l),Dark);
                for(float z=-l/2+.4f;z<l/2;z+=.38f)
                    Box("Drain crossbar",root,new Vector3(side*(w/2-.28f),.019f,z),new Vector3(.43f,.018f,.055f),Metal);
                for(float z=-l/2+1;z<l/2;z+=2)
                    Box("Chamfered yard kerb",root,new Vector3(side*(w/2+.16f),.12f,z),new Vector3(.3f,.24f,1.94f),slab);
            }
            for(int i=0;i<4;i++)
            {
                Crate(root,new Vector3(w/2+2.2f,0,l/2-2.5f-i*1.5f),1.2f,1.2f,1.1f,i*7);
                if(i<2) Crate(root,new Vector3(w/2+2.2f,1.1f,l/2-2.5f-i*1.5f),1.05f,1.1f,.85f,-5);
            }
            for(int i=0;i<3;i++) Drum(root,new Vector3(-w/2-1.1f,0,-l/2+3+i*.72f));
            EnvironmentArt.Tree(root,new Vector3(w/2+8,0,l/2+8),22);
            EnvironmentArt.Tree(root,new Vector3(-w/2-12,0,-l/2-7),26);
            EnvironmentArt.Building(root,new Vector3(w/2+20,0,l/2+22),3,2,-1);
            EnvironmentArt.Building(root,new Vector3(-w/2-18,0,l/2+24),4,3,1);
            for(int i=0;i<3;i++) EnvironmentArt.Building(root,new Vector3(-20+i*20,0,l/2+32),3+i%2,i+4,-1);
            EnvironmentArt.StreetLight(root,new Vector3(w/2+1,0,-l/2+3),-1);
            // Low-contrast expansion joints, never raised collision geometry.
            for(float z=-l/2+5;z<l/2;z+=6)
                Box("Concrete expansion joint",root,new Vector3(0,.009f,z),new Vector3(w,.008f,.025f),WorldBuilder.Material("Joint sealant",new Color(.30f,.31f,.29f)));
        }

        private static void ServiceBlock(Transform root,Vector3 p,float yaw,float width,float depth)
        {
            Transform b=Group("Masonry motor workshop",root,p,yaw);
            Material brick=EnvironmentArt.Surface("Service block aged brick","brick_wall_001",new Color(.77f,.72f,.61f),Vector2.one);
            Material render=EnvironmentArt.Surface("Workshop concrete lintels","concrete_floor_02",new Color(.74f,.73f,.65f),Vector2.one);
            Box("Back wall",b,new Vector3(0,2.6f,-depth/2),new Vector3(width,5.2f,.3f),brick);
            foreach(int side in new[]{-1,1}) Box("End wall",b,new Vector3(side*width/2,2.6f,0),new Vector3(.3f,5.2f,depth),brick);
            Box("Workshop floor",b,new Vector3(0,.035f,0),new Vector3(width,.07f,depth),render);
            int bays=Mathf.FloorToInt(width/6);float spacing=width/bays;
            for(int i=0;i<=bays;i++)
                Box("Brick pier",b,new Vector3(-width/2+i*spacing,2.6f,depth/2),new Vector3(.7f,5.2f,.6f),brick);
            Box("Front brick fascia",b,new Vector3(0,4.85f,depth/2),new Vector3(width,.7f,.6f),brick);
            Box("Precast lintel",b,new Vector3(0,4.36f,depth/2+.08f),new Vector3(width,.24f,.78f),render);
            Material shutter=EnvironmentArt.Surface("Aged workshop shutters","painted_metal_shutter",new Color(.67f,.72f,.70f),Vector2.one,.7f);
            for(int i=0;i<bays;i++)
            {
                float x=-width/2+spacing*(i+.5f);
                bool open=i==1;
                float h=open?.75f:3.9f;
                Box(open?"Raised shutter":"Closed roller shutter",b,new Vector3(x,4.15f-h/2,depth/2+.07f),new Vector3(spacing-.8f,h,.12f),shutter);
                if(open)
                {
                    Workbench(b,new Vector3(x,0,-depth/2+.8f));
                    Prop("industrial_storage_cart",b,new Vector3(x+1.7f,0,depth/2-1.5f),new Vector3(.72f,1.1f,1.3f),-20);
                    for(int k=0;k<3;k++) Crate(b,new Vector3(x-1.6f+k*1.3f,0,-1),1.1f,.9f,.85f,k*2);
                    Box("Bay wall tool board",b,new Vector3(x,2.1f,-depth/2+.22f),new Vector3(3.4f,1.2f,.12f),Metal);
                    for(int k=0;k<7;k++) Box("Hanging tool",b,new Vector3(x-1.3f+k*.43f,2.1f,-depth/2+.31f),new Vector3(.05f,.57f,.04f),Cream);
                }
                else Box("Shutter pull handle",b,new Vector3(x,.85f,depth/2+.17f),new Vector3(.5f,.05f,.08f),Dark);
                Sign(b,new Vector3(x,4.86f,depth/2+.34f),"SERVICE  /  0"+(i+1),2.9f,.4f);
                Box("Door rain hood",b,new Vector3(x,4.48f,depth/2+.5f),new Vector3(spacing-.4f,.1f,1.2f),Metal);
            }
            // Two sloped roof planes with an actual ridge, eaves and exposed steel supports.
            foreach(int side in new[]{-1,1})
            {
                GameObject roof=Box("Pitched sheet roof",b,new Vector3(0,5.75f,side*depth*.26f),new Vector3(width+1,.11f,depth*.57f),Metal);
                roof.transform.localRotation=Quaternion.Euler(side*13,0,0);
                Box("Eaves gutter",b,new Vector3(0,5.12f,side*(depth/2+.45f)),new Vector3(width+.9f,.16f,.18f),Dark);
                Box("Downpipe",b,new Vector3(side*(width/2-.12f),2.5f,depth/2+.48f),new Vector3(.1f,5,.1f),Metal);
            }
            for(float x=-width/2+1;x<width/2;x+=3)
                Box("Exposed roof purlin",b,new Vector3(x,5.05f,0),new Vector3(.1f,.17f,depth),Dark);
            Box("Roof ridge cap",b,new Vector3(0,6.3f,0),new Vector3(width+1,.12f,.35f),Metal);
            Sign(b,new Vector3(0,6.65f,depth/2),"ROXX  MOTOR  WORKS",width*.7f,1);
        }

        private static void Office(Transform root,Vector3 p,float yaw)
        {
            Transform b=Group("Parts office",root,p,yaw);
            Material render=EnvironmentArt.Surface("Office weathered render","concrete_floor_02",new Color(.67f,.64f,.53f),Vector2.one);
            Box("Office walls",b,new Vector3(0,1.8f,0),new Vector3(10,3.6f,6),render);
            Box("Office parapet",b,new Vector3(0,3.7f,0),new Vector3(10.5f,.25f,6.4f),Cream);
            Box("Office door",b,new Vector3(-3,1.15f,3.04f),new Vector3(1.15f,2.3f,.1f),Metal);
            Material glass=WorldBuilder.Material("Workshop window glass",new Color(.16f,.25f,.28f),.85f);
            for(int i=0;i<3;i++)
            {
                float x=-.8f+i*1.75f;
                Box("Office window surround",b,new Vector3(x,2,3.06f),new Vector3(1.5f,1.5f,.15f),Cream);
                Box("Office glazing",b,new Vector3(x,2,3.15f),new Vector3(1.27f,1.24f,.02f),glass);
                Box("Window mullion",b,new Vector3(x,2,3.18f),new Vector3(.045f,1.3f,.04f),Dark);
            }
            Sign(b,new Vector3(0,3.13f,3.11f),"PARTS  /  RECEPTION",5.5f,.5f);
            WorldBuilder.Shape("Rooftop water tank",b,PrimitiveType.Cylinder,new Vector3(2,4.25f,-1),new Vector3(1.6f,.55f,1.6f),Dark);
            Box("Air conditioner",b,new Vector3(4.3f,1.1f,3.25f),new Vector3(1,.65f,.5f),Cream);
            for(int i=0;i<7;i++) Box("Condenser louvre",b,new Vector3(4.3f,.86f+i*.075f,3.51f),new Vector3(.85f,.025f,.015f),Dark);
        }

        private static void Canopy(Transform root,Vector3 p,float yaw)
        {
            Transform b=Group("Goods loading canopy",root,p,yaw);
            foreach(float x in new[]{-5f,5f}) foreach(float z in new[]{-1.3f,2.3f})
            {
                Box("Canopy steel column",b,new Vector3(x,2, z),new Vector3(.16f,4,.16f),Metal);
                Box("Column concrete foot",b,new Vector3(x,.2f,z),new Vector3(.5f,.4f,.5f),Cream);
            }
            Box("Canopy roof",b,new Vector3(0,4.15f,.5f),new Vector3(11,.16f,5.2f),Metal);
            for(int i=0;i<6;i++) Box("Canopy roof rib",b,new Vector3(-5+i*2,4.24f,.5f),new Vector3(.05f,.05f,5.2f),Dark);
            Sign(b,new Vector3(0,3.7f,3.14f),"GOODS IN  /  KEEP CLEAR",7,.55f);
        }

        public static void Crate(Transform root,Vector3 p,float width,float length,float height,float yaw=0)
        {
            if(Prop("wooden_crate_01",root,p,new Vector3(width,height,length),yaw))return;
            Transform b=Group("Framed timber shipping crate",root,p,yaw);
            Box("Timber panels",b,Vector3.up*height/2,new Vector3(width,height,length),Wood);
            Material frame=EnvironmentArt.Surface("Crate hardwood framing","wooden_rough_planks",new Color(.54f,.46f,.34f),Vector2.one);
            foreach(int side in new[]{-1,1})
            {
                foreach(float y in new[]{.1f,height-.1f})
                {
                    Box("Crate end rail",b,new Vector3(0,y,side*(length/2+.035f)),new Vector3(width+.08f,.14f,.07f),frame);
                    Box("Crate side rail",b,new Vector3(side*(width/2+.035f),y,0),new Vector3(.07f,.14f,length),frame);
                }
                foreach(int end in new[]{-1,1})
                    Box("Corner timber batten",b,new Vector3(side*(width/2-.07f),height/2,end*(length/2+.07f)),new Vector3(.14f,height,.07f),frame);
                GameObject brace=Box("Diagonal crate brace",b,new Vector3(0,height/2,side*(length/2+.08f)),new Vector3(.12f,Mathf.Sqrt(width*width+height*height)*.85f,.06f),frame);
                brace.transform.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(width,height)*Mathf.Rad2Deg);
            }
            Box("Shipping label",b,new Vector3(width*.18f,height*.62f,-length/2-.12f),new Vector3(Mathf.Min(.35f,width*.25f),.22f,.012f),Cream);
        }

        public static void Drum(Transform root,Vector3 p)
        {
            if(Prop("barrel_03",root,p,new Vector3(.59f,.89f,.59f)))return;
            Transform b=Group("Used steel oil drum",root,p);
            WorldBuilder.Shape("Drum body",b,PrimitiveType.Cylinder,Vector3.up*.44f,new Vector3(.57f,.44f,.57f),Metal);
            foreach(float y in new[]{.05f,.24f,.64f,.87f})
                WorldBuilder.Shape("Rolled drum seam",b,PrimitiveType.Cylinder,Vector3.up*y,new Vector3(.59f,.014f,.59f),Dark);
            WorldBuilder.Shape("Drum filler",b,PrimitiveType.Cylinder,new Vector3(.13f,.89f,0),new Vector3(.07f,.015f,.07f),Cream);
        }

        private static void Workbench(Transform root,Vector3 p)
        {
            Transform b=Group("Service bench and drawers",root,p);
            Box("Tool cabinet",b,new Vector3(0,.45f,0),new Vector3(3,.9f,.65f),Metal);
            Box("Bench timber top",b,new Vector3(0,.95f,0),new Vector3(3.2f,.12f,.8f),Wood);
            for(int i=0;i<4;i++)
            {
                Box("Drawer seam",b,new Vector3(0,.16f+i*.19f,.34f),new Vector3(2.9f,.015f,.015f),Dark);
                Box("Drawer pull",b,new Vector3(0,.23f+i*.19f,.38f),new Vector3(.5f,.035f,.04f),Cream);
            }
        }

        public static void Sign(Transform root,Vector3 p,string text,float width,float height)
        {
            Box("Enamel sign backing",root,p,new Vector3(width,height,.06f),Dark);
            Transform t=Group(text,root,p+Vector3.forward*.04f);
            t.localRotation=Quaternion.Euler(0,180,0);
            var label=t.gameObject.AddComponent<TextMesh>();label.text=text;label.anchor=TextAnchor.MiddleCenter;
            label.alignment=TextAlignment.Center;label.fontSize=64;label.color=new Color(.87f,.82f,.65f);
            label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.font.RequestCharactersInTexture(text,label.fontSize,FontStyle.Normal);
            float advance=0;
            foreach(char ch in text) if(label.font.GetCharacterInfo(ch,out CharacterInfo glyph,label.fontSize,FontStyle.Normal)) advance+=glyph.advance;
            // TextMesh uses one tenth of a world unit per font pixel at characterSize 1.
            label.characterSize=10*Mathf.Min(width*.9f/Mathf.Max(1,advance),height*.75f/label.fontSize);
            var renderer=t.GetComponent<MeshRenderer>();renderer.sharedMaterial=label.font.material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
    }
}
