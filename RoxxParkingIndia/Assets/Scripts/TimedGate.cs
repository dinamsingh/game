using UnityEngine;

namespace RoxxParking
{
    public sealed class TimedGate : MonoBehaviour
    {
        public GateCycle Cycle { get; private set; }
        public BoxCollider Beam { get; private set; }
        public bool IsOpen => Cycle.Phase==GatePhase.Open;
        public string Status => IsOpen?"GREEN SIGNAL / CROSS NOW":Cycle.Phase==GatePhase.Closed?"RED SIGNAL / OPENS IN "+Mathf.CeilToInt(Cycle.SecondsRemaining)+"s":"GATE MOVING / WAIT FOR GREEN";
        private Rigidbody body;
        private Transform car;
        private GameObject red,green;
        private float width;
        private float closedSeconds,openSeconds;
        public bool NearApproach(Vector3 position) => Mathf.Abs(position.x-transform.position.x)<width/2+2 && position.z-transform.position.z>-12 && position.z-transform.position.z<2;

        public static void Create(Transform root,GateDefinition definition)
        {
            var obj=new GameObject("Signal controlled lifting gate");obj.transform.SetParent(root,false);obj.transform.localPosition=new Vector3(definition.x,0,definition.z);
            var gate=obj.AddComponent<TimedGate>();gate.width=definition.width;gate.closedSeconds=definition.closedSeconds;gate.openSeconds=definition.openSeconds;
            gate.Cycle=new GateCycle(gate.closedSeconds,gate.openSeconds);
            // A thin overhead beam should not pull the chase camera into the car.
            var beam=new GameObject("Moving collision beam",typeof(Rigidbody),typeof(BoxCollider),typeof(CourseObstacle));
            beam.transform.SetParent(obj.transform,false);beam.transform.localPosition=Vector3.up*.9f;
            gate.body=beam.GetComponent<Rigidbody>();gate.body.isKinematic=true;gate.body.useGravity=false;gate.body.interpolation=RigidbodyInterpolation.Interpolate;
            gate.Beam=beam.GetComponent<BoxCollider>();gate.Beam.size=new Vector3(gate.width,.4f,.28f);
            var ivory=WorldBuilder.Material("Gate enamel",new Color(.87f,.82f,.64f),.3f);
            var black=WorldBuilder.Material("Gate warning black",new Color(.09f,.10f,.09f));
            WorldBuilder.Box("Lifting crossbeam",beam.transform,Vector3.zero,gate.Beam.size,ivory);
            for(float x=-gate.width/2+.35f;x<gate.width/2-.2f;x+=.65f)
            {
                GameObject stripe=WorldBuilder.Box("Beam warning stripe",beam.transform,new Vector3(x,0,-.15f),new Vector3(.22f,.38f,.015f),black);
                stripe.transform.localRotation=Quaternion.Euler(0,0,-18);
            }
            float side=gate.width/2+.2f;
            WorldBuilder.Box("Signal housing",obj.transform,new Vector3(side,2.6f,-.38f),new Vector3(.45f,.9f,.22f),black);
            var redMat=WorldBuilder.Material("Gate red lamp",new Color(.95f,.10f,.04f),.3f);
            var greenMat=WorldBuilder.Material("Gate green lamp",new Color(.12f,1,.35f),.3f);
            gate.red=WorldBuilder.Shape("Red signal",obj.transform,PrimitiveType.Sphere,new Vector3(side,2.8f,-.53f),Vector3.one*.24f,redMat);
            gate.green=WorldBuilder.Shape("Green signal",obj.transform,PrimitiveType.Sphere,new Vector3(side,2.4f,-.53f),Vector3.one*.24f,greenMat);gate.green.SetActive(false);
            for(int direction=-1;direction<=1;direction+=2)
                for(int i=0;i<5;i++) WorldBuilder.Box("Stop line dash",obj.transform,new Vector3((i-2)*.65f,.025f,direction*4),new Vector3(.45f,.02f,.12f),ivory);
        }

        public void ResetCycle()
        {
            Cycle=new GateCycle(closedSeconds,openSeconds);body.position=transform.position+Vector3.up*.9f;
            red.SetActive(true);green.SetActive(false);
        }

        private void FixedUpdate()
        {
            if(car==null){var game=FindAnyObjectByType<RoxxGame>();if(game!=null)car=game.Car.transform;}
            bool occupied=false;
            if(car!=null)
            {
                Vector3 offset=car.position-transform.position;
                float extentX=Mathf.Abs(car.right.x)*ParkingRules.CarHalfWidth+Mathf.Abs(car.forward.x)*ParkingRules.CarHalfLength;
                float extentZ=Mathf.Abs(car.right.z)*ParkingRules.CarHalfWidth+Mathf.Abs(car.forward.z)*ParkingRules.CarHalfLength;
                occupied=Mathf.Abs(offset.x)<width/2+extentX+.4f && Mathf.Abs(offset.z)<extentZ+.7f;
            }
            Cycle.Tick(Time.fixedDeltaTime,occupied);
            body.MovePosition(transform.position+Vector3.up*Mathf.Lerp(.9f,3.35f,Cycle.Openness));
            red.SetActive(!IsOpen);green.SetActive(IsOpen);
        }
    }
}
