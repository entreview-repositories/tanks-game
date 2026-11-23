import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const require=createRequire(new URL('../../frontend/package.json',import.meta.url));
const {HubConnectionBuilder}=require('@microsoft/signalr');
const base='http://localhost:5174';
async function req(path,method='GET',body,token){const res=await fetch(base+'/api'+path,{method,headers:{'Content-Type':'application/json',...(token?{Authorization:`Bearer ${token}`}:{})},body:body?JSON.stringify(body):undefined});const text=await res.text();let data;try{data=JSON.parse(text);}catch{data=text;}return {status:res.status,data};}
const stamp=Date.now();
const users=await Promise.all([0,1,2,3,4].map(i=>req('/auth/register','POST',{username:`test${stamp}_${i}`,email:`test${stamp}_${i}@example.com`,password:'StudyDemo123'})));
for(const u of users)assert.equal(u.status,200,JSON.stringify(u));
const owner=users[0].data;
const room=await req('/room','POST',{roomName:'Review match',mapName:'desert'},owner.token);assert.equal(room.status,201);
const roomId=room.data.id;
const joined=await Promise.all(users.slice(1).map(u=>req(`/room/${roomId}/join`,'PUT',{},u.data.token)));assert.equal(joined.filter(r=>r.status===200).length,3,JSON.stringify(joined));
const guest=users[1].data;
const connections=[];
try{
 for(const user of [owner,guest]){
 const c=new HubConnectionBuilder().withUrl(base+'/gamehub',{accessTokenFactory:()=>user.token}).build();await c.start();connections.push(c);await c.invoke('JoinGame',c.connectionId,'spoofed',String(roomId),0,0);
 }
 const [first,second]=connections;
 await assert.rejects(second.invoke('StartGame','desert'));
 const chat=new Promise(resolve=>second.on('ReceiveChatMessage',(name,text)=>resolve({name,text})));
 await first.invoke('SendChatMessage','spoofed','integration test');assert.equal((await chat).name,owner.username);
 const start=new Promise(resolve=>second.on('GameStarted',resolve));await first.invoke('StartGame','desert');assert.equal(await start,'desert');
 await assert.rejects(first.invoke('ReportCollision','other-room',25));
 const moved=new Promise(resolve=>second.on('ReceivePlayerMove',(id,data)=>resolve(id)));await first.invoke('SendPlayerMove','spoofed',{position:{x:40,y:0},direction:'right'});assert.equal(await moved,first.connectionId);
 await req('/auth/logout','POST',{},owner.token);assert.equal((await req('/room','POST',{roomName:'revoked',mapName:'desert'},owner.token)).status,401);
 console.log('TANKS_SYSTEM_OK: concurrent capacity, authenticated SignalR, owner start, chat, movement, collision isolation, logout revocation');
}finally{for(const c of connections) await c.stop();}
