"""Fetch licensed Poly Haven surface maps; verify every byte against vendor metadata."""
import concurrent.futures
import hashlib
import json
from pathlib import Path
import urllib.request

workspace=Path(__file__).resolve().parents[2]
metadata=workspace/'.tools/visual-assets'
output=workspace/'RoxxParkingIndia/Assets/Resources/Environment'

def fetch(job):
    asset,channel,res,entry=job
    suffix={'Diffuse':'diff','nor_gl':'nor_gl','AO':'ao','Rough':'rough'}[channel]
    target=output/f'{asset}_{suffix}_{res}.jpg'
    if target.exists() and hashlib.md5(target.read_bytes()).hexdigest()==entry['md5']:return str(target.name)+' cached'
    request=urllib.request.Request(entry['url'],headers={'User-Agent':'RoxxParkingIndia development'})
    with urllib.request.urlopen(request,timeout=90) as response:data=response.read()
    assert len(data)==entry['size'] and hashlib.md5(data).hexdigest()==entry['md5'],target
    target.write_bytes(data)
    return f'{target.name}: {len(data)} verified bytes'

if __name__=='__main__':
    jobs=[]
    for asset in ['asphalt_02','concrete_floor_02','brick_wall_001','rock_face_03']:
        source=metadata/f'{asset}.json'
        if not source.exists():
            request=urllib.request.Request(f'https://api.polyhaven.com/files/{asset}',headers={'User-Agent':'RoxxParkingIndia development','Accept':'application/json'})
            with urllib.request.urlopen(request,timeout=30) as response:data=response.read()
            source.write_bytes(data)
        data=json.loads(source.read_text())
        for channel,res in [('Diffuse','2k'),('nor_gl','2k'),('AO','1k'),('Rough','1k')]:jobs.append((asset,channel,res,data[channel][res]['jpg']))
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
        for result in pool.map(fetch,jobs):print(result,flush=True)
