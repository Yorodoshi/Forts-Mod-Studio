using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FortsStudio {
 public static partial class Exporter {
  // The native Age action creates the children, so collisions, gravity, damage,
  // ownership and replay determinism stay in the game's projectile simulation.
  public static string ClusterProjectileDefinition(Entity e) {
   if(e.Kind!="projectiles"||!e.ClusterEnabled)return "";
   return "\n-- FMS: native cluster release; the runtime sets the absolute Age trigger.\n"+
    "item.Effects = item.Effects or {}\n"+
    "item.Effects.Age = { t200 = { Effect = 'effects/mortar_air_burst.lua', Splash = false, Terminate = true, Offset = 0, Projectile = { Count = "+e.ClusterCount+", Type = "+Q(e.ClusterProjectile)+", Speed = "+N(e.ClusterEjectionSpeed)+", StdDev = "+N(e.ClusterSpread)+", AngleOffset = 0 } } }\n";
  }

  public static void ValidateClusters(Project p,List<string> errors) {
   foreach(var e in p.Entities.Where(x=>x.ClusterEnabled)) {
    if(e.Kind!="projectiles") { errors.Add(e.NameRu+": кассетное разделение доступно только снарядам.");continue; }
    if(e.ClusterCount<1||e.ClusterCount>64)errors.Add(e.NameRu+": число боевых частей должно быть 1–64.");
    if(!p.Entities.Any(x=>x.Kind=="projectiles"&&x.Key==e.ClusterProjectile)&&!Catalog.Templates.Any(x=>x.Kind=="projectiles"&&x.Key==e.ClusterProjectile))errors.Add(e.NameRu+": не найдена боевая часть "+e.ClusterProjectile+".");
    if(e.ClusterProjectile==e.Key)errors.Add(e.NameRu+": снаряд не может разделяться на самого себя.");
    if(!ClusterNumber(e.ClusterMinimumAge,0,3600)||!ClusterNumber(e.ClusterDelay,0.02,3600))errors.Add(e.NameRu+": неверное время разделения кассеты.");
    double lifetime=e.V("MaxAge",30);
    if(lifetime<=e.ClusterMinimumAge||!e.ClusterAtApex&&lifetime<=e.ClusterDelay)errors.Add(e.NameRu+": время жизни должно превышать время разделения кассеты.");
    if(!ClusterNumber(e.ClusterEjectionSpeed,0,100000)||!ClusterNumber(e.ClusterSpread,0,6.283185307))errors.Add(e.NameRu+": неверная скорость или разброс боевых частей.");
    // A recursively splitting ammunition chain has unbounded projectile growth.
    var visited=new HashSet<string>();var next=e;
    while(next!=null&&next.ClusterEnabled) {
     if(!visited.Add(next.Key)){errors.Add(e.NameRu+": циклическая цепочка кассетных снарядов.");break;}
     next=p.Entities.FirstOrDefault(x=>x.Kind=="projectiles"&&x.Key==next.ClusterProjectile);
    }
   }
  }
  static bool ClusterNumber(double value,double min,double max) { return !Double.IsNaN(value)&&!Double.IsInfinity(value)&&value>=min&&value<=max; }

  public static string ClusterRuntime(Project p) {
   var entries=p.Entities.Where(e=>e.Kind=="projectiles"&&e.ClusterEnabled).ToList();
   if(entries.Count==0)return "";
   var s=new StringBuilder("\n-- FMS: monitor only active cluster carriers, using simulation time.\nlocal fms_cluster_definitions = {\n");
   foreach(var e in entries)s.Append("["+Q(e.Key)+"] = { apex = "+(e.ClusterAtApex?"true":"false")+", delay = "+N(e.ClusterDelay)+", minAge = "+N(e.ClusterMinimumAge)+" },\n");
   s.Append("}\n");
   s.Append(@"
local function fms_cluster_state()
 data.FMS_Clusters = data.FMS_Clusters or { nodes = {}, epoch = 0 }
 return data.FMS_Clusters
end

local function fms_cluster_release(node)
 -- SetNodeProjectileAgeTrigger takes absolute projectile age, not a delay.
 -- A small positive increment also avoids the special 'clear override' zero.
 SetNodeProjectileAgeTrigger(node, GetNodeAge(node) + 0.001)
end

function FMS_ClusterTick(epoch)
 local state = fms_cluster_state()
 if state.epoch ~= epoch then return end
 local ids = {}
 for node in pairs(state.nodes) do table.insert(ids, node) end
 table.sort(ids)
 for _,node in ipairs(ids) do
  local tracked = state.nodes[node]
  if not NodeExists(node) or GetNodeProjectileSaveName(node) ~= tracked.source then
   state.nodes[node] = nil
  else
   local definition = fms_cluster_definitions[tracked.source]
   if not definition then state.nodes[node] = nil
   elseif GetNodeAge(node) >= definition.minAge then
    -- Forts uses positive Y downwards. The first nonnegative vertical speed
    -- is the first descending simulation sample, at or immediately after apex.
    local velocity = NodeVelocity(node)
    if velocity.y >= 0 then
     state.nodes[node] = nil
     fms_cluster_release(node)
    end
   end
  end
 end
 ScheduleCall(0.025, FMS_ClusterTick, epoch)
end

local fms_cluster_previous_load = Load
function Load(...)
 if fms_cluster_previous_load then fms_cluster_previous_load(...) end
 local state = fms_cluster_state()
 state.epoch = state.epoch + 1
 ScheduleCall(0.025, FMS_ClusterTick, state.epoch)
end

local fms_cluster_previous_fired = OnWeaponFired
function OnWeaponFired(team, saveName, weapon, node, from)
 if fms_cluster_previous_fired then fms_cluster_previous_fired(team, saveName, weapon, node, from) end
 if not NodeExists(node) then return end
 local source = GetNodeProjectileSaveName(node)
 local definition = fms_cluster_definitions[source]
 if not definition then return end
 if definition.apex then
  -- Prevent an inherited weapon fuse from releasing the carrier prematurely.
  SetNodeProjectileAgeTrigger(node, 1000000)
  fms_cluster_state().nodes[node] = { source = source }
 else
  SetNodeProjectileAgeTrigger(node, GetNodeAge(node) + math.max(definition.minAge, definition.delay))
 end
end

local fms_cluster_previous_destroyed = OnProjectileDestroyed
function OnProjectileDestroyed(node, team, saveName, structure, reason)
 fms_cluster_state().nodes[node] = nil
 if fms_cluster_previous_destroyed then fms_cluster_previous_destroyed(node, team, saveName, structure, reason) end
end

local fms_cluster_previous_restart = OnRestart
function OnRestart(...)
 if fms_cluster_previous_restart then fms_cluster_previous_restart(...) end
 local state = fms_cluster_state()
 state.nodes = {}
 state.epoch = state.epoch + 1
 ScheduleCall(0.025, FMS_ClusterTick, state.epoch)
end

");
   return s.ToString();
  }
 }
}
