using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FortsStudio {
 public static partial class Exporter {
  static void HybridNumber(List<string> errors,Entity e,string name,double value,double min,double max) {
   if(Double.IsNaN(value)||Double.IsInfinity(value)||value<min||value>max)
    errors.Add(e.Key+": "+name+" — число от "+N(min)+" до "+N(max)+".");
  }
  public static void ValidateHybrid(Project p,List<string> errors) {
   var available=new HashSet<string>(Catalog.Templates.Where(t=>t.Kind=="projectiles").Select(t=>t.Key));
   foreach(var projectile in p.Entities.Where(e=>e.Kind=="projectiles"))available.Add(projectile.Key);
   foreach(var e in p.Entities.Where(e=>e.HybridAA)) {
    if(e.Kind!="weapons"){errors.Add(e.Key+": два режима ПВО доступны только орудию.");continue;}
    HybridNumber(errors,e,"Ближняя граница ПВО",e.HybridNearRange,.001,1000000);
    HybridNumber(errors,e,"Дальность обнаружения ПВО",e.HybridFarRange,.001,1000000);
    if(e.HybridNearRange>=e.HybridFarRange)errors.Add(e.Key+": ближняя граница ПВО должна быть меньше дальности обнаружения.");
    HybridNumber(errors,e,"Запас переключения ПВО",e.HybridHysteresis,0,1000000);
    HybridNumber(errors,e,"Период поиска целей",e.HybridScanPeriod,.025,1);
    HybridNumber(errors,e,"Снарядов в ближней очереди",e.HybridNearRounds,1,256);
    if(!Double.IsNaN(e.HybridNearRounds)&&!Double.IsInfinity(e.HybridNearRounds)&&e.HybridNearRounds!=Math.Floor(e.HybridNearRounds))errors.Add(e.Key+": количество снарядов ближней очереди должно быть целым.");
    HybridNumber(errors,e,"Интервал ближних выстрелов",e.HybridNearPeriod,.001,60);
    HybridNumber(errors,e,"Ближняя перезарядка",e.HybridNearReload,0,3600);
    HybridNumber(errors,e,"Ближний разброс",e.HybridNearSpread,0,10);
    HybridNumber(errors,e,"Ближняя задержка выстрела",e.HybridNearDelay,0,60);
    HybridNumber(errors,e,"Ближняя скорость снаряда",e.HybridNearSpeed,.001,100000);
    HybridNumber(errors,e,"Металл ближней очереди",e.HybridNearMetal,0,1000000);
    HybridNumber(errors,e,"Энергия ближней очереди",e.HybridNearEnergy,0,1000000);
    HybridNumber(errors,e,"Минимальная дальняя скорость снаряда",e.V("MinFireSpeed",8000),.001,100000);
    HybridNumber(errors,e,"Максимальная дальняя скорость снаряда",e.V("MaxFireSpeed",8000.1),.001,100000);
    if(e.Ammo.Count!=1)errors.Add(e.Key+": выберите ровно один снаряд дальнего режима ПВО; ближний снаряд выбирается отдельно.");
    foreach(string projectile in e.Ammo)if(!available.Contains(projectile))errors.Add(e.Key+": отсутствует снаряд дальнего ПВО "+projectile+".");
    if(String.IsNullOrWhiteSpace(e.HybridNearProjectile)||!available.Contains(e.HybridNearProjectile))errors.Add(e.Key+": отсутствует снаряд ближнего ПВО "+e.HybridNearProjectile+".");
    foreach(var projectile in p.Entities.Where(x=>x.Kind=="projectiles"&&x.FiniteAmmo&&(e.Ammo.Contains(x.Key)||e.HybridNearProjectile==x.Key)))
     errors.Add(e.Key+": двухрежимное ПВО использует обычные снаряды; расходуемый боеприпас "+projectile.Key+" нужно отключить или выбрать другой тип.");
   }
  }
  // Native ammunition modes keep their ordinary engine reload, costs, collisions and projectiles.
  // Reference: EarthWork Games' High Seas modding guide and FortsAPI.html.
  public static string HybridWeaponScript(Entity e) {
   if(e.Kind!="weapons"||!e.HybridAA)return "";
   string far=e.Ammo.Count>0?e.Ammo[0]:"flak";
   string near=String.IsNullOrEmpty(e.HybridNearProjectile)?"minigun":e.HybridNearProjectile;
   var s=new StringBuilder("\n-- FMS: two native firing modes. Projectile definitions are not modified.\n");
   s.Append(Sprite("fms_"+e.Key+"_hybrid_far",AssetPath(e.Key,"-icon.tga")));
   s.Append(Sprite("fms_"+e.Key+"_hybrid_near",AssetPath(e.Key,"-icon.tga")));
   s.Append("dlc2_Ammunition = {\n");
   s.Append(" { Tag = 'fms_hybrid_far', StringId = 'Weapon.fms_hybrid_far', Sprite = "+Q("fms_"+e.Key+"_hybrid_far")+", Projectile = "+Q(far)+" },\n");
   s.Append(" { Tag = 'fms_hybrid_near', StringId = 'Weapon.fms_hybrid_near', Sprite = "+Q("fms_"+e.Key+"_hybrid_near")+", Projectile = "+Q(near));
   s.Append(", RoundsEachBurst = "+N(Math.Max(1,Math.Round(e.HybridNearRounds)))+", ProjectilesEachRound = 1, RoundPeriod = "+N(e.HybridNearPeriod)+", ReloadTime = "+N(e.HybridNearReload));
   s.Append(", MinFireSpeed = "+N(e.HybridNearSpeed)+", MaxFireSpeed = "+N(e.HybridNearSpeed+.1)+", MetalFireCost = "+N(e.HybridNearMetal)+", EnergyFireCost = "+N(e.HybridNearEnergy));
   s.Append(", FireStdDev = "+N(e.HybridNearSpread)+", FireStdDevAuto = "+N(e.HybridNearSpread*.5)+", FireDelay = "+N(e.HybridNearDelay));
   // Keep a user supplied shot sound in both modes. Otherwise use the actual vanilla minigun shot effect.
   s.Append(", TriggerProjectileAgeAction = false, RetriggerFireEffect = false, CutOffEffects = true, FireEffect = "+(String.IsNullOrEmpty(e.Sound)?Q("effects/fire_minigun.lua"):"FireEffect")+", FireEndEffect = '' },\n}\n");
   return s.ToString();
  }

  // Called after the ordinary localisation files have been written.
  public static void ExportHybridAi(Project p,string root) {
   var weapons=p.Entities.Where(e=>e.Kind=="weapons"&&e.HybridAA).ToList();if(weapons.Count==0)return;
   var s=new StringBuilder("-- FMS owns automatic targeting of these new weapon types.\n");
   foreach(var e in weapons){string k="["+Q(e.Key)+"]";
    s.Append("data.AntiAirFireProbability"+k+" = 0\nAntiAirFireProbabilityHumanAssist"+k+" = 0\ndata.OffensiveFireProbability"+k+" = 0\ndata.FireDuringRebuildProbability"+k+" = 0\n");
   }
   Write(root,"ai/ai.lua",s.ToString());
   foreach(string lang in new[]{"russian","english"}){
    bool ru=lang=="russian";string file=Path.Combine(root,"mods","language-"+lang,"weapons","strings.lua");Directory.CreateDirectory(Path.GetDirectoryName(file));
    string body="\nWeapon['fms_hybrid_far'] = L"+Q(ru?"Дальний режим · зенитный залп":"Long range · flak burst")+"\nWeapon['fms_hybrid_near'] = L"+Q(ru?"Ближний режим · скоростная очередь":"Close range · rapid burst")+"\n";
    File.AppendAllText(file,body,Encoding.Unicode);
   }
  }

  public static string HybridRuntime(Project p) {
   var weapons=p.Entities.Where(e=>e.Kind=="weapons"&&e.HybridAA).ToList();if(weapons.Count==0)return "";
   var s=new StringBuilder("\nlocal fms_hybrid_config = {\n");
   foreach(var e in weapons){
    s.Append("["+Q(e.Key)+"] = { automatic = "+(e.HybridAutomatic?"true":"false")+", near = "+N(e.HybridNearRange)+", far = "+N(e.HybridFarRange)+", hysteresis = "+N(e.HybridHysteresis)+", period = "+N(e.HybridScanPeriod));
    s.Append(", nearSpeed = "+N(e.HybridNearSpeed)+", farSpeed = "+N((e.V("MinFireSpeed",8000)+e.V("MaxFireSpeed",8000.1))*.5));
    s.Append(", nearProjectile = "+Q(e.HybridNearProjectile)+", farProjectile = "+Q(e.Ammo.Count>0?e.Ammo[0]:"flak"));
    s.Append(", nearRounds = "+N(Math.Max(1,Math.Round(e.HybridNearRounds)))+", nearPeriod = "+N(e.HybridNearPeriod)+", nearDelay = "+N(e.HybridNearDelay)+", farRounds = "+N(e.V("RoundsEachBurst",4))+", farPeriod = "+N(e.V("RoundPeriod",.25))+", delay = "+N(e.V("FireDelay",0))+" },\n");
   }
   s.Append("}\n");s.Append(HybridController);return s.ToString();
  }

  const string HybridController=@"
-- The entire controller runs on simulation frames, identically on every peer.
-- Only serialisable state lives in data; no local wall clock, input or UI state is used.
local function fms_hybrid_state()
 data.FMSHybrid = data.FMSHybrid or { next = {}, modes = {} }
 return data.FMSHybrid
end
local function fms_hybrid_distance(a,b)
 local dx=a.x-b.x;local dy=a.y-b.y;return math.sqrt(dx*dx+dy*dy)
end
local function fms_hybrid_projectiles()
 local sides={ [1]={}, [2]={} }
 for side=1,2 do
  for i=0,ProjectileCount(side)-1 do
   local id=GetProjectileId(side,i)
   if id>0 and NodeExists(id) then
    local team=NodeTeam(id);local name=GetNodeProjectileSaveName(id)
    if team%MAX_SIDES==side and GetProjectileTypeCanBeShotDown(name,team) then
     local kind=GetNodeProjectileType(id)
     if kind~=PROJECTILE_TYPE_MISSILE or IsMissileAttacking(id) then
      table.insert(sides[side],{id=id,pos=NodePosition(id),vel=NodeVelocity(id),gravity=GetProjectileGravity(id)})
     end
    end
   end
  end
 end
 return sides
end
local function fms_hybrid_intercept(origin,target,cfg,near)
 local speed=math.max(1,near and cfg.nearSpeed or cfg.farSpeed)
 -- Intercept the first round. Leading by half a 13-round burst can put the
 -- solution behind our own weapon when a projectile is already close.
 local delay=near and cfg.nearDelay or cfg.delay
 local gx=0;local gy=target.gravity
 local rx=target.pos.x-origin.x+target.vel.x*delay
 local ry=target.pos.y-origin.y+target.vel.y*delay+.5*gy*delay*delay
 local vx=target.vel.x;local vy=target.vel.y+gy*delay
 local a=vx*vx+vy*vy-speed*speed
 local b=2*(rx*vx+ry*vy);local c=rx*rx+ry*ry
 local t=math.sqrt(c)/speed
 if math.abs(a)<.0001 then
  if b<-.0001 then t=-c/b end
 else
  local discriminant=b*b-4*a*c
  if discriminant>=0 then
   local root=math.sqrt(discriminant)
   local first=(-b-root)/(2*a);local second=(-b+root)/(2*a)
   if first>0 and second>0 then t=math.min(first,second)
   elseif first>0 then t=first elseif second>0 then t=second end
  end
 end
 -- Refine gravity with Newton steps instead of the old oscillating fixed-point
 -- iteration, which was unstable for missiles faster than our AA projectile.
 for iteration=1,4 do
  local x=rx+vx*t;local y=ry+vy*t+.5*gy*t*t
  local distance=math.sqrt(x*x+y*y)
  if distance>.001 then
   local derivative=(x*vx+y*(vy+gy*t))/distance-speed
   if math.abs(derivative)>.001 then
    local nextTime=t-(distance-speed*t)/derivative
    if nextTime>0 and nextTime<30 then t=nextTime end
   end
  end
 end
 return Vec3(origin.x+rx+vx*t,origin.y+ry+vy*t+.5*gy*t*t,0),t
end
local function fms_hybrid_power(id,flight,near)
 if near then return 1 end
 -- Flak's timer is ammunition power, not cursor distance. Use the engine's
 -- current minimum/maximum fuse (including commander and other mod changes).
 local first=GetFuseDelay(id,0);local last=GetFuseDelay(id,1)
 if last>first then return math.max(0,math.min(1,(flight-first)/(last-first))) end
 return 1
end
function FMS_HybridUpdate(frame)
 local state=fms_hybrid_state()
 if state.unavailable then return end
 if not state.apiChecked then
  for _,name in ipairs({'SetWeaponSelectedAmmo','GetWeaponSelectedAmmo','GetProjectileTypeCanBeShotDown','ProjectileCount','GetProjectileId','GetProjectileGravity','IsWeaponReadyToFire','GetWeaponBarrelPosition','GetDeviceForward','FireWeaponWithPower','GetFuseDelay','ReserveWeaponAim'}) do
   if not _G[name] then Log('FMS: hybrid AA requires the native High Seas ammunition API; missing '..name);state.unavailable=true;return end
  end
  state.apiChecked=true
  Log('FMS: hybrid AA controller active')
 end
 local targets=nil;local present={}
 for index=0,GetTotalDeviceCount()-1 do
  local id=GetTotalDeviceId(index);local cfg=fms_hybrid_config[GetDeviceType(id)]
  if cfg and cfg.automatic then
   present[id]=true
   if frame>=(state.next[id] or 0) then
    state.next[id]=frame+math.max(1,math.ceil(cfg.period*30))
    if IsDeviceFullyBuilt(id) and IsDeviceAvailable(id) and not IsDummy(id) then
     -- Use the engine's readiness predicate, as the stock anti-air AI does.
     -- Round counts describe a burst; they are not a ready-to-fire flag.
     if IsWeaponReadyToFire(id) then
      local side=GetDeviceTeamIdActual(id)%MAX_SIDES;local enemy=side==1 and 2 or 1
      if side==1 or side==2 then
       if not targets then targets=fms_hybrid_projectiles() end
       local origin=GetWeaponBarrelPosition(id);local candidates={}
       local forward=GetDeviceForward(id)
       for _,projectile in ipairs(targets[enemy]) do
        local d=fms_hybrid_distance(origin,projectile.pos)
        local dx=projectile.pos.x-origin.x;local dy=projectile.pos.y-origin.y
        if d<=cfg.far and dx*forward.x+dy*forward.y>0 then
         table.insert(candidates,{projectile=projectile,distance=d})
        end
       end
       table.sort(candidates,function(a,b) if a.distance==b.distance then return a.projectile.id<b.projectile.id else return a.distance<b.distance end end)
       for candidateIndex,candidate in ipairs(candidates) do
        -- Bound obstruction checks under very heavy projectile loads.
        if candidateIndex>32 then break end
        local near=candidate.distance<cfg.near
        if state.modes[id]=='fms_hybrid_near' and candidate.distance<cfg.near+cfg.hysteresis then near=true end
        local mode=near and 'fms_hybrid_near' or 'fms_hybrid_far'
        local projectile=near and cfg.nearProjectile or cfg.farProjectile
        -- Resolve by the unique tag, without assuming whether the engine inserts
        -- a default ammunition entry before dlc2_Ammunition.
        if state.modes[id]~=mode or GetWeaponSelectedAmmo(id)~=projectile then SetWeaponSelectedAmmo(id,mode) end
        local point,flight=fms_hybrid_intercept(origin,candidate.projectile,cfg,near)
        -- A fast, very close projectile can pass us during spool-up. Keep the
        -- aim in front, like the stock AA's close-threat correction.
        if (point.x-origin.x)*forward.x+(point.y-origin.y)*forward.y<=0 then
         point=candidate.projectile.pos
         flight=candidate.distance/math.max(1,near and cfg.nearSpeed or cfg.farSpeed)
        end
        local power=fms_hybrid_power(id,flight,near)
        local flags=FIREFLAG_TERRAINBLOCKS+FIREFLAG_EXTRACLEARANCE
        local result=FireWeaponWithPower(id,point,0,FIREWEAPON_STDDEVTEST_DEFAULT,flags+FIREFLAG_IGNOREFASTDOORS+FIREFLAG_TEST,power)
        if result==FIRE_SUCCESS or result==FIRE_DOOR then
         state.modes[id]=mode
         -- This reserves the aim for one scan only; FireWeapon still enforces resources,
         -- doors, firing angle, engine reload, water, disruption and friendly clearance.
         ReserveWeaponAim(id,math.max(.1,cfg.period*1.5))
         local fired=FireWeaponWithPower(id,point,0,FIREWEAPON_STDDEVTEST_DEFAULT,flags,power)
         if fired==FIRE_SUCCESS and not state.firstShot then
          state.firstShot=true;Log('FMS: hybrid AA automatic fire started')
         end
         break
        end
       end
      end
     end
    end
   end
  end
 end
 for id in pairs(state.next) do if not present[id] then state.next[id]=nil;state.modes[id]=nil end end
end
local fms_hybrid_previous_load=Load
function Load(...)
 if fms_hybrid_previous_load then fms_hybrid_previous_load(...) end
 fms_hybrid_state()
 Log('FMS: hybrid AA 2.5.1 loaded')
end
local fms_hybrid_previous_update=Update
function Update(frame,...)
 if fms_hybrid_previous_update then fms_hybrid_previous_update(frame,...) end
 FMS_HybridUpdate(frame)
end
local fms_hybrid_previous_restart=OnRestart
function OnRestart(...)
 if fms_hybrid_previous_restart then fms_hybrid_previous_restart(...) end
 data.FMSHybrid={next={},modes={}}
end
";
 }
}
