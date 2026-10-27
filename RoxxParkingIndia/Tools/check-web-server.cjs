'use strict';
const assert=require('node:assert/strict');
const fs=require('node:fs');
const os=require('node:os');
const path=require('node:path');
const http=require('node:http');
const zlib=require('node:zlib');
const {createServer}=require('./serve-web.cjs');

(async()=>{
  const root=fs.mkdtempSync(path.join(os.tmpdir(),'roxx-web-check-'));
  fs.mkdirSync(path.join(root,'Build'));
  fs.writeFileSync(path.join(root,'index.html'),'<title>Roxx</title>');
  const wasm=Buffer.from([0,97,115,109,1,0,0,0]);
  fs.writeFileSync(path.join(root,'Build/game.wasm.gz'),zlib.gzipSync(wasm));
  fs.writeFileSync(path.join(root,'Build/game.js.gz'),zlib.gzipSync('window.fixture=true;'));
  const apk=Buffer.from('APK download fixture');
  fs.writeFileSync(path.join(root,'phone.apk'),apk);
  const server=createServer(root,path.join(root,'phone.apk'));
  await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
  const request=(url,method='GET')=>new Promise((resolve,reject)=>{
    http.request({host:'127.0.0.1',port:server.address().port,path:url,method},res=>{
      const chunks=[];res.on('data',chunk=>chunks.push(chunk));res.on('end',()=>resolve({code:res.statusCode,headers:res.headers,body:Buffer.concat(chunks)}));
    }).on('error',reject).end();
  });
  try{
    const page=await request('/');assert.equal(page.code,200);assert.match(page.body.toString(),/Roxx/);
    const file=await request('/Build/game.wasm.gz');assert.equal(file.headers['content-type'],'application/wasm');assert.equal(file.headers['content-encoding'],'gzip');assert.deepEqual(zlib.gunzipSync(file.body),wasm);
    const head=await request('/Build/game.wasm.gz','HEAD');assert.equal(head.body.length,0);assert.equal(head.headers['content-length'],String(file.body.length));
    assert.equal((await request('/Build/game.js.gz')).headers['content-type'],'application/javascript');
    const download=await request('/download/RoxxParkingIndia-0.6.1.apk');
    assert.equal(download.code,200);assert.deepEqual(download.body,apk);
    assert.equal(download.headers['content-type'],'application/vnd.android.package-archive');
    assert.equal(download.headers['content-disposition'],'attachment; filename="RoxxParkingIndia-0.6.1.apk"');
    const apkHead=await request('/download/RoxxParkingIndia-0.6.1.apk','HEAD');
    assert.equal(apkHead.body.length,0);assert.equal(apkHead.headers['content-length'],String(apk.length));
    for(const url of ['/../PROJECT.md','/Build/%2e%2e/index.html','/Build/%5c..%5csecret','/.git/config','/Build/game.wasm.gz:other'])assert.equal((await request(url)).code,403,url);
    for(const url of ['/Build/','/Tools/serve-web.cjs','/Build/missing','/phone.apk','/download/other.apk','/download/'])assert.equal((await request(url)).code,404,url);
    assert.equal((await request('/%XY')).code,400);
    assert.equal((await request('/','POST')).code,405);
    assert.equal(JSON.parse((await request('/health')).body).game,'Roxx Parking India');
    console.log('PASS: gzip/wasm and APK MIME, download bytes/disposition/HEAD, health, traversal and selected-file isolation.');
  }finally{
    await new Promise(resolve=>server.close(resolve));
    assert.ok(path.resolve(root).startsWith(path.join(path.resolve(os.tmpdir()),'roxx-web-check-')));
    fs.rmSync(root,{recursive:true});
  }
})().catch(error=>{console.error(error);process.exitCode=1;});
