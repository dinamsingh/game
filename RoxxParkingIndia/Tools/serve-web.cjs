'use strict';
const http=require('node:http');
const fs=require('node:fs');
const path=require('node:path');

function createServer(buildRoot,apkPath=null){
  const root=fs.realpathSync(buildRoot);
  const apk=apkPath?fs.realpathSync(apkPath):null;
  return http.createServer((req,res)=>{
    const reject=code=>{res.writeHead(code,{'Content-Type':'text/plain','Cache-Control':'no-store'});res.end(http.STATUS_CODES[code]);};
    if(req.method!=='GET'&&req.method!=='HEAD')return reject(405);
    let name;
    try{name=decodeURIComponent(req.url.split('?')[0]);}catch{return reject(400);}
    if(name.length>2048||name.includes('\\')||name.includes(':')||name.includes('\0')||name.split('/').some(p=>p.startsWith('.')))return reject(403);
    if(name==='/health'){
      res.writeHead(200,{'Content-Type':'application/json','Cache-Control':'no-store'});
      return res.end(req.method==='HEAD'?undefined:JSON.stringify({game:'Roxx Parking India',version:'0.6.1'}));
    }
    if(name==='/download/RoxxParkingIndia-0.6.1.apk'&&apk)return sendFile(apk,true);
    if(name==='/')name='/index.html';
    if(name!=='/index.html'&&!name.startsWith('/Build/')&&!name.startsWith('/StreamingAssets/'))return reject(404);
    const target=path.resolve(root,'.'+name);
    if(!target.startsWith(root+path.sep))return reject(403);
    fs.realpath(target,(error,real)=>{
      if(error)return reject(404);
      if(!real.startsWith(root+path.sep))return reject(403);
      sendFile(real);
    });
    function sendFile(real,download=false){
      fs.stat(real,(error,stat)=>{
        if(error||!stat.isFile())return reject(404);
        const compressed=!download&&name.endsWith('.gz');
        const ext=download?'.apk':path.extname(compressed?name.slice(0,-3):name);
        const type={'.apk':'application/vnd.android.package-archive','.html':'text/html; charset=utf-8','.js':'application/javascript','.wasm':'application/wasm','.json':'application/json','.png':'image/png','.jpg':'image/jpeg','.svg':'image/svg+xml'}[ext]||'application/octet-stream';
        const headers={'Content-Type':type,'Content-Length':stat.size,'X-Content-Type-Options':'nosniff','Cache-Control':download||ext==='.html'?'no-store':'no-cache'};
        if(download)headers['Content-Disposition']='attachment; filename="RoxxParkingIndia-0.6.1.apk"';
        if(compressed)headers['Content-Encoding']='gzip';
        res.writeHead(200,headers);
        if(req.method==='HEAD')return res.end();
        const stream=fs.createReadStream(real);
        stream.on('error',()=>res.destroy());
        res.on('close',()=>stream.destroy());
        stream.pipe(res);
      });
    }
  });
}
module.exports={createServer};
if(require.main===module){
  const root=path.join(__dirname,'../Builds/Web-0.6.1');
  if(!fs.existsSync(path.join(root,'index.html')))throw new Error('Browser build missing. Run Invoke-Unity.ps1 -Action Web first.');
  const host=process.argv[2]||'127.0.0.1';
  if(!/^\d{1,3}(\.\d{1,3}){3}$/.test(host))throw new Error('An IPv4 address is required.');
  const apk=path.join(__dirname,'../Builds/Android/RoxxParkingIndia-0.6.1.apk');
  const server=createServer(root,fs.existsSync(apk)?apk:null);
  server.requestTimeout=30000;server.headersTimeout=10000;
  server.on('error',error=>{console.error(error.message);process.exitCode=1;});
  server.listen(8877,host,()=>console.log(`Roxx Parking India: http://${host}:8877/ (browser build and the selected APK only)`));
}
