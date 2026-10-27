"""Full car-footprint clearance, progression and low-speed bicycle path checks.

This verifies authored geometry, not WheelCollider handling or gold target times.
"""
import heapq
import json
import math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
courses=json.loads((ROOT/'Assets/Resources/courses.json').read_text())['courses']

def axes(yaw):
    a=math.radians(yaw)
    return ((math.cos(a),-math.sin(a)),(math.sin(a),math.cos(a)))

def overlaps(a,b):
    ax,az,aw,al,ayaw=a;bx,bz,bw,bl,byaw=b
    aa,ba=axes(ayaw),axes(byaw)
    for dx,dz in aa+ba:
        distance=abs((bx-ax)*dx+(bz-az)*dz)
        ra=sum(abs(v[0]*dx+v[1]*dz)*s/2 for v,s in zip(aa,(aw,al)))
        rb=sum(abs(v[0]*dx+v[1]*dz)*s/2 for v,s in zip(ba,(bw,bl)))
        if distance>=ra+rb:return False
    return True

def valid(pose,c,obstacles):
    x,z,yaw=pose;ca,sa=abs(math.cos(math.radians(yaw))),abs(math.sin(math.radians(yaw)))
    if abs(x)+ca*1.02+sa*2.29>c['mapWidth']/2-.08:return False
    if abs(z)+sa*1.02+ca*2.29>c['mapLength']/2-.08:return False
    return not any(overlaps((x,z,2.04,4.58,yaw),b) for b in obstacles if abs(x-b[0])<max(b[2],b[3])/2+3 and abs(z-b[1])<max(b[2],b[3])/2+3)

def parked(pose,c):
    x,z,yaw=pose;a=math.radians(c['bayYaw']);dx=x-c['bayX'];dz=z-c['bayZ']
    lx=dx*math.cos(a)-dz*math.sin(a);lz=dx*math.sin(a)+dz*math.cos(a)
    angle=abs((yaw-c['bayYaw']+180)%360-180)
    if not c['reversePark']:angle=min(angle,180-angle)
    if angle>12:return False
    ca,sa=math.cos(math.radians(angle)),math.sin(math.radians(angle))
    return abs(lx)+.98*ca+2.25*sa<=c['bayWidth']/2 and abs(lz)+2.25*ca+.98*sa<=c['bayLength']/2

def solve(start,target,c,obstacles,final=False,start_gear=1):
    """Hybrid A*: 4.2m minimum turn radius, both gears, 0.3m swept footprint samples."""
    queue=[];seen={};nodes=[]
    tx,tz=target['x'],target['z']
    def key(p,g):return(round(p[0]/.35),round(p[1]/.35),round(p[2]/5)%72,g)
    def heuristic(p):
        distance=math.hypot(p[0]-tx,p[1]-tz)
        if (final or target['gear']<0 or target.get('align')) and distance<12:
            angle=abs((p[2]-target.get('yaw',c['bayYaw'])+180)%360-180)
            if final and not c['reversePark']:angle=min(angle,180-angle)
            distance+=angle/25
        return distance
    k=key(start,start_gear);nodes.append((start,start_gear,None));seen[k]=0
    heapq.heappush(queue,(heuristic(start),0,0,k));expanded=0
    while queue:
        _,node,cost,k=heapq.heappop(queue)
        if cost>seen[k]+.001:continue
        pose,current_gear,_=nodes[node];x,z,yaw=pose;expanded+=1
        reached=parked(pose,c) if final else math.hypot(x-tx,z-tz)<target['radius']*.7
        heading_ok=not (target['gear']<0 or target.get('align')) or abs((yaw-target.get('yaw',c['bayYaw'])+180)%360-180)<=20
        if reached and heading_ok and (not target['gear'] or k[3]==target['gear']):
            path=[];gears=[]
            while node is not None:
                p,g,node=nodes[node];path.append(p);gears.append(g)
            return list(reversed(path)),list(reversed(gears)),expanded
        if expanded>180000:raise AssertionError((c['name'],'search budget exhausted',target))
        for gear in ((-1,) if c.get('reverseOnly') else (1,-1)):
            for curvature in (-1/4.2,-.5/4.2,0,.5/4.2,1/4.2):
                step=.9;delta=gear*step*curvature;heading=math.radians(yaw);samples=[]
                for part in (1/3,2/3,1):
                    a=heading+delta*part/2
                    samples.append((x+gear*step*part*math.sin(a),z+gear*step*part*math.cos(a),(yaw+math.degrees(delta)*part)%360))
                if not all(valid(p,c,obstacles) for p in samples):continue
                p=samples[-1];nk=key(p,gear)
                nc=cost+step*(1 if gear==1 else 1.12)+(1 if gear!=k[3] else 0)+(.04 if curvature else 0)
                if nc>=seen.get(nk,float('inf')):continue
                seen[nk]=nc;new_node=len(nodes);nodes.append((p,gear,node))
                heapq.heappush(queue,(nc+heuristic(p)*1.25,new_node,nc,nk))
    raise AssertionError((c['name'],'no drivable path',target))

def main():
    assert len(courses)==24 and len({c['name'] for c in courses})==24
    assert [c['difficulty'] for c in courses[:12]]==sorted(c['difficulty'] for c in courses[:12])
    assert {c['district'] for c in courses}=={0,1,2,3,4}
    report=[]
    for c in courses:
        assert all(0 <= p.get("minKph",0) <= 100 for p in c["route"]), c["name"]
        obstacles=[(b['x'],b['z'],b['width'],b['length'],b['yaw']) for b in c['barriers']]
        for prefix in ('spawn','bay'):
            assert valid(tuple(c[prefix+k] for k in ('X','Z','Yaw')),c,obstacles),(c['name'],prefix,'blocked')
        assert c['bayWidth']>1.96 and c['bayLength']>4.5
        assert not c['reversePark'] or any(p['gear']==-1 for p in c['route'])
        pose=tuple(c['spawn'+k] for k in ('X','Z','Yaw'));distance=0;expansions=0;changes=0;lastgear=-1 if c.get('reverseOnly') else 1
        targets=c['route']+[dict(x=c['bayX'],z=c['bayZ'],gear=-1 if c['reversePark'] else 0,radius=.3)]
        for i,t in enumerate(targets):
            path,gears,n=solve(pose,t,c,obstacles,final=i==len(targets)-1,start_gear=lastgear)
            distance+=sum(math.hypot(a[0]-b[0],a[1]-b[1]) for a,b in zip(path,path[1:]));expansions+=n
            for g in gears:changes+=g!=lastgear;lastgear=g
            pose=path[-1]
        row=dict(name=c['name'],district=c['district'],difficulty=c['difficulty'],bayWidth=c['bayWidth'],routePoints=len(c['route']),reverse=c['reversePark'],feasiblePathMetres=round(distance,1),gearChanges=changes,searchStates=expansions)
        report.append(row);print('PASS',json.dumps(row),flush=True)
    out=ROOT/'Builds/Checks';out.mkdir(parents=True,exist_ok=True)
    (out/'course-validation.json').write_text(json.dumps(report,indent=2))
    print('PASS: 12 campaign courses and 12 expert trials; swept-footprint paths with heading/reverse-only restrictions. Gate timing and the 90 km/h speed sensor are checked in Unity, not this static path search.')

if __name__=='__main__':main()
