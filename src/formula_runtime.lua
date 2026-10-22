-- Formula AST interpreter. No loadstring, raw Lua expressions, or access to globals.
-- Keep numeric semantics and limits in sync with Formula.cs.
local function fms_formula_eval(root, resolve)
 local work=0
 local function tick() work=work+1; if work>100000 then error('Formula work limit exceeded (100000).') end end
 local function finite(x)
  if type(x)~='number' or x~=x or x==math.huge or x==-math.huge then error('Formula result is not finite or is outside a function domain.') end
  return x
 end
 local function num(x) if type(x)~='number' then error('A numeric value is required; a list cannot be used here.') end return finite(x) end
 local function list(x) if type(x)~='table' then error('A list is required.') end return x end
 local function normalized(x,name)
  if type(x)=='number' then return finite(x) end
  if type(x)=='table' then
   if #x>4096 then error('List exceeds 4096 items.') end
   local copy={};for i=1,#x do tick();copy[i]=num(x[i]) end;return copy
  end
  error('Variable '..name..' must contain a number or numeric list.')
 end
 local function atan2(y,x)
  if x>0 then return math.atan(y/x) end
  if x<0 then return math.atan(y/x)+(y>=0 and math.pi or -math.pi) end
  if y>0 then return math.pi/2 elseif y<0 then return -math.pi/2 end
  return 0
 end
 local ev
 local function call(name,v)
  if name=='len' or name=='sum' or name=='mean' or ((name=='min' or name=='max') and #v==1) then
   local a=list(v[1]);if name=='len' then return #a end
   if #a==0 and name~='sum' then error(name..' requires a nonempty list.') end
   local r=name=='min' and math.huge or (name=='max' and -math.huge or 0)
   for i=1,#a do tick();local n=num(a[i]);if name=='min' then r=math.min(r,n) elseif name=='max' then r=math.max(r,n) else r=finite(r+n) end end
   if name=='mean' then r=r/#a end;return finite(r)
  end
  if name=='at' then local a=list(v[1]);local i=num(v[2]);if i~=math.floor(i) or i<1 or i>#a then error('List index must be an integer from 1 to its length.') end;return a[i] end
  for i=1,#v do v[i]=num(v[i]) end
  local x,y=v[1],v[2];local r
  if name=='range' then
   local step=v[3] or 1;if step==0 then error('Range step cannot be zero.') end
   local a={};if (step>0 and y<x) or (step<0 and y>x) then return a end
   local count=math.floor(finite(finite(y-x)/step))+1;if count>4096 then error('List exceeds 4096 items.') end
   for i=0,count-1 do tick();a[i+1]=finite(x+i*step) end;return a
  end
  if name=='min' or name=='max' then r=x;for i=2,#v do tick();if name=='min' then r=math.min(r,v[i]) else r=math.max(r,v[i]) end end;return r end
  if name=='abs' then r=math.abs(x)
  elseif name=='sqrt' then r=math.sqrt(x)
  elseif name=='cbrt' then r=(x<0 and -1 or (x>0 and 1 or 0))*math.abs(x)^(1/3)
  elseif name=='exp' then r=math.exp(x)
  elseif name=='ln' or name=='log' then
   if x<=0 or (y and (y<=0 or y==1)) then error('Invalid logarithm domain.') end
   r=y and math.log(x)/math.log(y) or math.log(x)
  elseif name=='log10' then r=math.log(x)/math.log(10)
  elseif name=='pow' then r=x^y
  elseif name=='floor' then r=math.floor(x)
  elseif name=='ceil' then r=math.ceil(x)
  elseif name=='round' then r=x>=0 and math.floor(x+.5) or math.ceil(x-.5)
  elseif name=='sign' then r=x<0 and -1 or (x>0 and 1 or 0)
  elseif name=='clamp' then if y>v[3] then error('Clamp minimum exceeds maximum.') end;r=math.max(y,math.min(v[3],x))
  elseif name=='lerp' then r=x+(y-x)*v[3]
  elseif name=='sin' then r=math.sin(x)
  elseif name=='cos' then r=math.cos(x)
  elseif name=='tan' then r=math.tan(x)
  elseif name=='asin' then r=math.asin(x)
  elseif name=='acos' then r=math.acos(x)
  elseif name=='atan' then r=math.atan(x)
  elseif name=='atan2' then r=atan2(x,y)
  elseif name=='sinh' then r=(finite(math.exp(x))-finite(math.exp(-x)))/2
  elseif name=='cosh' then r=(finite(math.exp(x))+finite(math.exp(-x)))/2
  elseif name=='tanh' then local z=math.exp(-2*math.abs(x));r=(x<0 and -1 or 1)*(1-z)/(1+z)
  elseif name=='deg' then r=x*180/math.pi
  elseif name=='rad' then r=x*math.pi/180
  else error('Unknown function: '..name) end
  return finite(r)
 end
 ev=function(n,binding,depth)
  tick();if depth>32 then error('Formula nesting exceeds 32 levels.') end
  local k,v,a=n.k,n.v,n.a or {}
  if k=='n' then return finite(v) end
  if k=='v' then local b=binding;while b do if b.name==v then return b.value end;b=b.parent end
   if not resolve then error('Unknown variable: '..v) end;return normalized(resolve(v),v)
  end
  if k=='l' then if #a>4096 then error('List exceeds 4096 items.') end;local result={};for i=1,#a do result[i]=num(ev(a[i],binding,depth+1)) end;return result end
  if k=='u' then local x=num(ev(a[1],binding,depth+1));return finite(v=='-' and -x or x) end
  if k=='b' then
   local x,y=num(ev(a[1],binding,depth+1)),num(ev(a[2],binding,depth+1));local r
   if v=='+' then r=x+y elseif v=='-' then r=x-y elseif v=='*' then r=x*y
   elseif v=='/' then if y==0 then error('Division by zero.') end;r=x/y
   elseif v=='%' then if y==0 then error('Division by zero.') end;r=x-finite(math.floor(finite(x/y)))*y
   elseif v=='^' then r=x^y else error('Unknown formula operator.') end
   return finite(r)
  end
  if k~='f' then error('Unknown formula node.') end
  if v=='diff' or v=='integrate' then
   local p=num(ev(a[3],binding,depth+1));local name=a[2].v
   local function sample(x) return num(ev(a[1],{name=name,value=finite(x),parent=binding},depth+1)) end
   if v=='diff' then local h=1e-5*math.max(1,math.abs(p));return finite((sample(p+h)-sample(p-h))/(2*h)) end
   local q=num(ev(a[4],binding,depth+1));local steps=64
   if a[5] then steps=num(ev(a[5],binding,depth+1));if steps~=math.floor(steps) or steps<2 or steps>256 or steps%2~=0 then error('Integral steps must be an even integer from 2 to 256.') end end
   local width=finite(q-p);local step=finite(width/steps);local total=0
   for i=0,steps do local x=i==steps and q or finite(p+i*step);local weight=(i==0 or i==steps) and 1 or (i%2==0 and 2 or 4);total=finite(total+weight*sample(x)) end
   return finite(total*step/3)
  end
  local values={};for i=1,#a do values[i]=ev(a[i],binding,depth+1) end;return call(v,values)
 end
 return ev(root,nil,1)
end
