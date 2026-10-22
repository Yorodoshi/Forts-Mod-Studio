-- Serializable, simulation-time scenario interpreter.
local function state()
 data.FMS = data.FMS or { globals={}, objects={}, tasks={}, audio={}, serial=0, replacing={} }
 return data.FMS
end
local function subject(c) return c.subject or (c.node and ('p:'..c.node)) or (c.device and ('d:'..c.device)) or 'world' end
local function vars(c,name)
 local s=state(); local def=fms_variables[name] or {scope='object',initial=0}
 local t=s.globals
 if def.scope~='global' then local k=subject(c); s.objects[k]=s.objects[k] or {}; t=s.objects[k] end
 if t[name]==nil then t[name]=def.initial end
 return t
end
local function numeric(text,fallback,c)
 if not text or text=='' then return fallback or 0 end
 if string.sub(text,1,1)=='$' then
  local n=string.sub(text,2)
  if n=='speed' and c.node and NodeExists(c.node) then local v=NodeVelocity(c.node); return math.sqrt(v.x*v.x+v.y*v.y) end
  if n=='repeat_index' then return c.repeat_index or 0 end
  if n=='hp' and c.node and NodeExists(c.node) then return GetNodeProjectileHitPoints(c.node) end
  if n=='damage' and c.node and NodeExists(c.node) then return GetNodeProjectileDamage(c.node) end
  return vars(c,n)[n]
 end
 return tonumber(text) or fallback or 0
end
local function compare(a,op,b)
 if op=='==' then return a==b elseif op=='~=' then return a~=b elseif op=='<' then return a<b elseif op=='>' then return a>b elseif op=='<=' then return a<=b else return a>=b end
end
local function position(c)
 if c.node and NodeExists(c.node) then return NodePosition(c.node) end
 if c.device and DeviceExists(c.device) then return GetDevicePosition(c.device) end
 return Vec3(c.x or 0,c.y or 0,0)
end
local function alive(c)
 if c.node then return NodeExists(c.node) end
 if c.device then return DeviceExists(c.device) end
 return true
end
local function condition(b,c)
 if b.type=='enemy' then return c.owner and c.target and c.owner%MAX_SIDES~=c.target%MAX_SIDES end
 if b.type=='ally' then return c.owner and c.target and c.owner%MAX_SIDES==c.target%MAX_SIDES end
 if b.type=='material' then return c.material==b.value end
 if b.type=='source' then return c.source==b.value end
 if b.type=='variable' then return compare(vars(c,b.value)[b.value],b.op,numeric(b.text,b.number,c)) end
 if b.type=='alive' then return alive(c) end
 if b.type=='chance' then return GetRandomFloat(0,100,'FMS condition')<numeric(b.text,b.number,c) end
 if b.type=='hp' then return c.node and NodeExists(c.node) and compare(GetNodeProjectileHitPoints(c.node),b.op,numeric(b.text,b.number,c)) end
 return false
end
local function stopAudio(key)
 local s=state(); local a=s.audio[key]; if a and a.handle then CancelEffect(a.handle) end; s.audio[key]=nil
end
function FMS_AudioRepeat(key,token)
 local s=state(); local a=s.audio[key]; if not a or a.token~=token then return end
 if not alive(a.context) then stopAudio(key); return end
 if a.handle then CancelEffect(a.handle) end
 a.handle=SpawnEffect(a.effect,position(a.context))
 ScheduleCall(math.max(.05,a.duration),FMS_AudioRepeat,key,token)
end
function FMS_AudioTick()
 for key,a in pairs(state().audio) do if alive(a.context) then if a.handle then SetEffectPosition(a.handle,position(a.context)) end else stopAudio(key) end end
 ScheduleCall(.1,FMS_AudioTick)
end
local function audio(c,effect,duration,slot)
 local s=state(); local key=subject(c)..':'..slot; stopAudio(key); s.serial=s.serial+1
 s.audio[key]={context=c,effect=effect,duration=duration,token=s.serial}
 FMS_AudioRepeat(key,s.serial)
end
local function entityAudio(c)
 local a=fms_entity_audio[c.source]; if a then audio(c,a.effect,a.duration,a.slot) end
end
local function stopObject(c)
 local prefix=subject(c)..':'
 local keys={}; for k in pairs(state().audio) do if string.sub(k,1,#prefix)==prefix then table.insert(keys,k) end end
 for _,k in ipairs(keys) do stopAudio(k) end
end
local function action(b,c)
 local n=numeric(b.text,b.number,c)
 if b.type=='effect' then if b.value~='' then SpawnEffect(b.value,position(c)) end
 elseif b.type=='sound' then if b.sound then SpawnEffect(b.sound,position(c)) end
 elseif b.type=='loop_sound' then if b.sound then audio(c,b.sound,b.duration,b.tag) end
 elseif b.type=='stop_sound' then stopAudio(subject(c)..':'..b.tag)
 elseif b.type=='set_var' then vars(c,b.value)[b.value]=n
 elseif b.type=='add_var' then local t=vars(c,b.value);t[b.value]=t[b.value]+n
 elseif b.type=='multiply_var' then local t=vars(c,b.value);t[b.value]=t[b.value]*n
 elseif b.type=='fire' then if c.a and c.b then IgniteLink(c.a,c.b,true) else IgniteFire(position(c),n,n,TEAM_ANY) end
 elseif b.type=='damage' then if c.device and DeviceExists(c.device) then ApplyDamageToDevice(c.device,n) end
 elseif b.type=='device' then if c.a and c.b then CreateDevice(c.owner,b.value,c.a,c.b,c.t or .5) end
 elseif b.type=='set_projectile_hp' then if c.node and NodeExists(c.node) then SetProjectileAntiAirHitpoints(c.node,math.max(0,n)) end
 elseif b.type=='alpha' then if c.node and NodeExists(c.node) then SetNodeProjectileAlpha(c.node,math.max(0,math.min(1,n))) end
 elseif b.type=='destroy_projectile' then if c.node and NodeExists(c.node) then DestroyProjectile(c.node) end
 elseif b.type=='convert_projectile' then
  if not c.node or not NodeExists(c.node) then return end
  local old=c.node; local v=NodeVelocity(old);local p=NodePosition(old);local team=NodeTeam(old)
  local life=GetNodeProjectileTimeRemaining(old);local client=GetProjectileClientId(old)
  local target=GetMissileTarget(old)
  local id=dlc2_CreateProjectileEx(b.value,c.weapon or fms_default_weapon,team,p,Vec3(v.x*n,v.y*n,v.z or 0),life,client)
  if id and id>0 then
   state().replacing[old]=true;stopObject(c)
   state().objects['p:'..id]=state().objects[subject(c)]
   DestroyProjectile(old);state().replacing[old]=nil
   c.node=id;c.subject='p:'..id;c.source=b.value
   if target then SetMissileTarget(id,target) end
   entityAudio(c)
  end
 elseif b.type=='spawn_projectile' then
  local p=position(c);local v=c.node and NodeExists(c.node) and NodeVelocity(c.node) or Vec3(1,0,0)
  local len=math.sqrt(v.x*v.x+v.y*v.y);if len<.001 then len=1;v=Vec3(1,0,0) end
  for i=1,math.min(64,math.max(1,math.floor(n))) do dlc2_CreateProjectileEx(b.value,c.weapon or fms_default_weapon,c.owner or 1,p,Vec3(v.x/len*b.interval,v.y/len*b.interval,0),30,0) end
 end
end
local function push(task,ids)
 for _,id in ipairs(ids) do local b=fms_blocks[id];if b and b.kind=='condition' and not condition(b,task.context) then return false end end
 table.insert(task.stack,{ids=ids,index=1});return true
end
function FMS_RunTask(id)
 local s=state();local task=s.tasks[id];if not task then return end
 for budget=1,256 do
  local frame=task.stack[#task.stack]
  if not frame then s.tasks[id]=nil;return end
  if frame.loop then
   local b=fms_blocks[frame.loop];local more=frame.remaining>0
   if b.type=='while' then more=condition({type='variable',value=b.value,op=b.op,number=b.number,text=b.text},task.context) and frame.remaining>0 end
   if not more then table.remove(task.stack) else
    if frame.started then frame.started=false;ScheduleCall(b.interval,FMS_RunTask,id);return end
    frame.remaining=frame.remaining-1;frame.iteration=frame.iteration+1;task.context.repeat_index=frame.iteration;frame.started=true;push(task,b.children)
   end
  else
   local bid=frame.ids[frame.index]
   if not bid then table.remove(task.stack) else
    frame.index=frame.index+1;local b=fms_blocks[bid]
    if b then
     if b.kind=='if' then push(task,condition(b,task.context) and b.children or b.otherwise)
     elseif b.kind=='loop' then table.insert(task.stack,{loop=bid,remaining=b.type=='while' and 10000 or math.min(10000,math.max(0,math.floor(numeric(b.text,b.number,task.context)))),iteration=0})
     elseif b.kind=='delay' then push(task,b.children);ScheduleCall(math.max(0,numeric(b.text,b.number,task.context)),FMS_RunTask,id);return
     elseif b.kind=='group' then push(task,b.children)
     elseif b.kind=='action' then if b.type=='stop' then s.tasks[id]=nil;return else action(b,task.context) end end
    end
   end
  end
 end
 ScheduleCall(.05,FMS_RunTask,id)
end
local function launch(rule,c)
 local s=state();local count=0;for _ in pairs(s.tasks) do count=count+1 end;if count>=1024 then return end
 local copy={};for k,v in pairs(c) do copy[k]=v end
 s.serial=s.serial+1;local id=s.serial;s.tasks[id]={context=copy,stack={}}
 push(s.tasks[id],rule.body);FMS_RunTask(id)
end
local function emit(event,c)
 if c.pos then c.x=c.pos.x;c.y=c.pos.y;c.pos=nil end
 for _,r in ipairs(fms_rules) do if r.event==event and (r.source=='' or r.source==c.source) then launch(r,c) end end
end
function FMS_Timer(index)
 local r=fms_rules[index];if not r then return end
 launch(r,{source='',subject='world'});ScheduleCall(math.max(.05,r.interval),FMS_Timer,index)
end
function Load()
 state();ScheduleCall(.1,FMS_AudioTick)
 for i=0,GetTotalDeviceCount()-1 do local id=GetTotalDeviceId(i);local save=GetDeviceType(id);if fms_entity_audio[save] and IsDeviceFullyBuilt(id) then entityAudio({device=id,source=save,owner=GetDeviceTeamIdActual(id),subject='d:'..id}) end end
 for i,r in ipairs(fms_rules) do if r.event=='timer' then ScheduleCall(math.max(.05,r.interval),FMS_Timer,i) end end
 emit('game_start',{source='',subject='world'})
 Log('FMS_LOADED '..fms_folder)
end
function OnWeaponFired(team,save,weapon,node,from)
 local c={owner=team,source=GetNodeProjectileSaveName(node),weapon=save,node=node,subject='p:'..node}
 entityAudio(c);emit('projectile_spawned',c)
 if not from or from==0 then emit('fire',{owner=team,source=save,weapon=save,device=weapon,subject='d:'..weapon}) end
end
function OnDeviceCreated(team,id,save,a,b,t,upgraded)
 emit('device_created',{owner=team,source=save,device=id,a=a,b=b,t=t,subject='d:'..id})
end
function OnDeviceCompleted(team,id,save)
 local c={owner=team,source=save,device=id,subject='d:'..id};entityAudio(c);emit('device_completed',c)
end
function OnDeviceHit(team,id,save,health,node,owner,pos,reflected)
 emit('device_hit',{owner=owner,target=team,source=GetNodeProjectileSaveName(node),device=id,node=node,pos=pos,subject='p:'..node})
end
function OnLinkHit(a,b,node,owner,source,damage,pos,reflected)
 emit('link_hit',{owner=owner,target=NodeTeam(a),source=source,node=node,a=a,b=b,material=GetLinkMaterialSaveName(a,b),pos=pos,subject='p:'..node})
end
function OnTerrainHit(terrain,damage,node,save,surface,pos,normal,reflected)
 emit('terrain_hit',{owner=NodeTeam(node),source=save,node=node,pos=pos,subject='p:'..node})
end
function OnProjectileCollision(teamA,a,saveA,teamB,b,saveB)
 emit('projectile_hit',{owner=teamA,target=teamB,node=a,source=saveA,subject='p:'..a})
 emit('projectile_hit',{owner=teamB,target=teamA,node=b,source=saveB,subject='p:'..b})
end
function OnProjectileHitBeam(node,save,weapon,team,beam,pos,velocity)
 emit('projectile_hit',{owner=NodeTeam(node),target=team,node=node,source=save,pos=pos,subject='p:'..node})
end
function OnProjectileDestroyed(node,team,save,structure,reason)
 local c={owner=team,source=save,node=node,subject='p:'..node}
 if not state().replacing[node] then emit('projectile_destroyed',c);if reason==DESTROYTYPE_EXPIRED then emit('projectile_expired',c) end end
 stopObject(c)
end
function OnDeviceDestroyed(team,id,save,a,b,t)
 local c={owner=team,source=save,device=id,a=a,b=b,t=t,subject='d:'..id}
 if a and NodeExists(a) then local p=NodePosition(a);c.x=p.x;c.y=p.y end
 emit('destroyed',c);stopObject(c)
end
function OnDeviceDeleted(team,id,save,a,b,t) stopObject({device=id,subject='d:'..id}) end
