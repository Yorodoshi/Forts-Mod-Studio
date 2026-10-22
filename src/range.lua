-- Runs only in the project's dedicated sandbox map. All physics is native.
local function range_log(s) Log(range_tag..' '..s) end
local function count(t) local n=0 for _ in pairs(t) do n=n+1 end return n end
local function status(text)
 if not ControlExists("","fms_range_status") then AddTextControl("","fms_range_status",text,ANCHOR_TOP_LEFT,Vec3(30,75,0),false,"Readout") else SetControlText("","fms_range_status",text) end
end
local function reason(code,prefix)
 for k,v in pairs(_G) do if type(k)=='string' and string.sub(k,1,3)==(prefix or 'CS_') and v==code then return k end end
 return tostring(code)
end
local function range_position(index)
 local p=range_nodes[index];local b=data.FMSRange.bases[p.team]
 return Vec3(b.x+(p.x-b.ox)*range_unit,b.y+(p.y-b.oy)*range_unit,0)
end
function FMSRangeStep()
 local r=data.FMSRange;if not r or r.finished then return end
 r.steps=r.steps+1
 for index,l in pairs(r.links) do if not NodeExists(l.a) or not NodeExists(l.b) or not IsNodeLinkedTo(l.a,l.b) then r.finished=true;PauseFire(false);range_log("INCOMPLETE destroyed_link="..index);status("FMS: construction interrupted; a support was destroyed. Restart the range.");return end end
 for index,link in ipairs(range_links) do if not r.links[index] then
  local a=r.nodes[link.a];local b=r.nodes[link.b]
  if a and not NodeExists(a) then r.errors[index]='destroyed parent';a=nil end
  if b and not NodeExists(b) then r.errors[index]='destroyed parent';b=nil end
  if a and b then
   local result=CreateLink(link.team,link.material,a,b)
   -- CS_SUCCESS is negative (-15 in Forts 1.38); it is not a node id.
   if result==CS_SUCCESS or (IsNodeLinkedTo(a,b) and GetLinkMaterialSaveName(a,b)==link.material) then r.links[index]={a=a,b=b};r.errors[index]=nil
   else r.errors[index]=reason(result) end
  elseif a or b then
   local parent=a or b;local target=a and link.b or link.a
   local id=CreateNode(link.team,link.material,parent,range_position(target))
   if id>=0 then r.nodes[target]=id;r.links[index]={a=parent,b=id};r.errors[index]=nil
   else r.errors[index]=reason(id) end
  end
 end end
 for index,obj in ipairs(range_devices) do if not r.devices[index] then
  local a=r.nodes[obj.a];local b=r.nodes[obj.b]
  if a and b and NodeExists(a) and NodeExists(b) and IsNodeLinkedTo(a,b) then
   local id=CreateDevice(obj.team,obj.name,a,b,obj.t)
   if id>=0 then r.devices[index]=id;r.deviceErrors[index]=nil else r.deviceErrors[index]=reason(id,'CD_') end
  end
 end end
 local links=count(r.links);local devices=count(r.devices);local signature=links..'/'..#range_links..' '..devices..'/'..#range_devices
 if signature~=r.last or r.steps%40==0 then range_log('BUILDING links='..links..'/'..#range_links..' devices='..devices..'/'..#range_devices);r.last=signature;status('FMS: assembling '..links..'/'..#range_links..' beams; '..devices..'/'..#range_devices..' devices')
  if r.steps%40==0 then for i,why in pairs(r.errors) do range_log('PENDING link '..i..' '..why) end;for i,why in pairs(r.deviceErrors) do range_log('PENDING device '..i..' '..why) end end
 end
 if links==#range_links and devices==#range_devices then
  r.finished=true;PauseFire(false);status('FMS: range assembled. Aim and fire with the normal game controls.');range_log('READY links='..links..'/'..#range_links..' devices='..devices..'/'..#range_devices)
 elseif r.steps*.25>=range_timeout then
  r.finished=true;PauseFire(false);status('FMS: incomplete scene. See the Studio range report.');range_log('INCOMPLETE links='..links..'/'..#range_links..' devices='..devices..'/'..#range_devices)
  for i,l in ipairs(range_links) do if not r.links[i] then range_log('ERROR link '..i..' '..l.material..' '..(r.errors[i] or 'unreachable node')) end end
  for i,d in ipairs(range_devices) do if not r.devices[i] then range_log('ERROR device '..d.name..' '..(r.deviceErrors[i] or 'platform not ready')) end end
 else ScheduleCall(.25,FMSRangeStep) end
end
function FMSRangeBuild()
 if GetMapFilename()~=range_map or GetGameMode()=='Editor' or data.FMSRange then return end
 data.FMSRange={nodes={},bases={},links={},devices={},errors={},deviceErrors={},steps=0,shots=0,hits=0};local r=data.FMSRange
 SetUnlimitedResources(1,true,true);SetUnlimitedResources(2,true,true);PauseFire(true)
 for team=1,2 do
  local root=nil
  for i,p in ipairs(range_nodes) do if p.team==team and (not root or p.y>range_nodes[root].y or p.y==range_nodes[root].y and ((team==1 and p.x<range_nodes[root].x) or (team==2 and p.x>range_nodes[root].x))) then root=i end end
  if root then
   local anchor=nil;local best=nil
   for i=0,NodeCount(team)-1 do
    local id=GetNodeId(team,i)
    if not IsFoundation(id) then local p=NodePosition(id);local score=team==1 and p.x or -p.x
     if not best or score>best.x+.01 or math.abs(score-best.x)<.01 and p.y<best.y then best={x=score,y=p.y};anchor=id end
    end
   end
   if not anchor then range_log('ERROR no elevated anchor for side '..team);r.finished=true;PauseFire(false);return end
   local p=NodePosition(anchor);r.nodes[root]=anchor;r.bases[team]={x=p.x,y=p.y,ox=range_nodes[root].x,oy=range_nodes[root].y}
   range_log('ANCHOR side='..team..' x='..p.x..' y='..p.y)
   if range_grounded then
    local minx,maxx=p.x,p.x
    for _,point in ipairs(range_nodes) do if point.team==team then local x=p.x+(point.x-range_nodes[root].x)*range_unit;minx=math.min(minx,x);maxx=math.max(maxx,x) end end
    local block=CreateBlock();SetBlockOwner(block,team);SetBlockFlags(block,1,true)
    AddBlockVertex(block,Vec3(minx-15,p.y,0),-2);AddBlockVertex(block,Vec3(maxx+15,p.y,0),-2)
    AddBlockVertex(block,Vec3(maxx+15,p.y+55,0),-2);AddBlockVertex(block,Vec3(minx-15,p.y+55,0),-2)
    SetBlockTexture(block,GetEnvironmentPath()..'/ground/ground1.dds');SetBlockSurface(block,GetSurfaceIndex('foundations'));UpdateGroundTriangles()
    range_log('GROUND side='..team..' block='..block)
   end
  end
 end
 range_log('START expected_links='..#range_links..' expected_devices='..#range_devices)
 FMSRangeStep()
end
local range_old_load=Load
function Load(gameStart)
 if range_old_load then range_old_load(gameStart) end
 if GetMapFilename()==range_map and GetGameMode()~='Editor' then ScheduleCall(.25,FMSRangeBuild) end
end
local range_old_fired=OnWeaponFired
function OnWeaponFired(team,save,weapon,node,from)
 if range_old_fired then range_old_fired(team,save,weapon,node,from) end
 if data.FMSRange and from==0 then data.FMSRange.shots=data.FMSRange.shots+1;local v=NodeVelocity(node);range_log('SHOT '..save..' projectile='..GetNodeProjectileSaveName(node)..' speed='..math.sqrt(v.x*v.x+v.y*v.y)) end
end
local range_old_linkhit=OnLinkHit
function OnLinkHit(a,b,node,owner,save,damage,pos,reflected)
 if range_old_linkhit then range_old_linkhit(a,b,node,owner,save,damage,pos,reflected) end
 if data.FMSRange then data.FMSRange.hits=data.FMSRange.hits+1;range_log('HIT '..save..' material='..GetLinkMaterialSaveName(a,b)..' raw_damage='..damage) end
end
-- Restore the generated scene when the sandbox battle is restarted.
local range_old_restart=OnRestart
function OnRestart()
 if range_old_restart then range_old_restart() end
 if GetMapFilename()==range_map and GetGameMode()~='Editor' then data.FMSRange=nil;ScheduleCall(.25,FMSRangeBuild) end
end
local range_old_cleanup=Cleanup
function Cleanup()
 if data.FMSRange then PauseFire(false) end
 if range_old_cleanup then range_old_cleanup() end
end
