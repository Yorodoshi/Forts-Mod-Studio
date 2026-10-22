using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace FortsStudio {
 public class Project {
  public int Format=2; public string Id=Guid.NewGuid().ToString("N"), Folder="fms_workshop", NameRu="Моя мастерская",NameEn="My workshop",DescriptionRu="",DescriptionEn="",GameRoot="";
  public List<Entity> Entities=new List<Entity>(); public List<Chain> Chains=new List<Chain>(); public List<Segment> Scene=new List<Segment>(); public List<PlacedItem> Placed=new List<PlacedItem>();
  public List<Variable> Variables=new List<Variable>(); public List<AudioClip> Sounds=new List<AudioClip>();
  public PreviewOptions Preview=new PreviewOptions();
  public string LastInstall="";
  public string CoverImage="";
  public static Project Demo(string game) { var p=new Project();p.Id="b06dbb0f24084d259d14dfb258cadb20";p.GameRoot=game;p.Folder="fms_studio_demo";p.NameRu="FMS 2.5 — Мастерская";p.NameEn="FMS 2.5 - Workshop";
   var m=Catalog.Create("materials","bracing",game); m.Key="fms_reinforced_wood";m.NameRu="Усиленная балка";m.NameEn="Reinforced beam";m.DescriptionRu="Деревянная балка: прочность 650. Создано в Forts Mod Studio 2.";m.DescriptionEn="Reinforced timber: 650 hit points.";m.Values["HitPoints"]=650;p.Entities.Add(m);
   var d=Catalog.Create("devices","battery",game);d.Key="fms_power_bank";d.NameRu="Энергоблок";d.NameEn="Power bank";d.Values["EnergyStorageCapacity"]=5000;p.Entities.Add(d);
   var pr=Catalog.Create("projectiles","cannon",game);pr.Key="fms_shell";pr.NameRu="Тяжёлый снаряд";pr.NameEn="Heavy shell";p.Entities.Add(pr);
   var w=Catalog.Create("weapons","cannon",game);w.Key="fms_test_cannon";w.NameRu="Пушка мастерской";w.NameEn="Workshop cannon";w.Values["ReloadTime"]=3;w.Values["BuildTimeComplete"]=1;w.Values["MetalCost"]=50;w.Values["EnergyCost"]=50;w.Ammo.Add(pr.Key);p.Entities.Add(w);
   p.Scene.Add(new Segment{X1=2,Y1=7,X2=5,Y2=7,Material=m.Key});p.Scene.Add(new Segment{X1=2,Y1=7,X2=2,Y2=4,Material=m.Key});p.Scene.Add(new Segment{X1=2,Y1=4,X2=5,Y2=4,Material=m.Key});p.Scene.Add(new Segment{X1=5,Y1=4,X2=5,Y2=7,Material=m.Key});p.Scene.Add(new Segment{X1=2,Y1=7,X2=5,Y2=4,Material=m.Key});
   return p;
  }
 }
 public class Entity {
  public string Kind="materials",Key="",NameRu="",NameEn="",DescriptionRu="",DescriptionEn="",Template="bracing";
  public Dictionary<string,double> Values=new Dictionary<string,double>(); public Dictionary<string,bool> Flags=new Dictionary<string,bool>();
  public List<Extra> Extras=new List<Extra>(); public List<string> Ammo=new List<string>();
  public bool IsTechnology=false,RequireTechnology=false; public string RequiredTechnology="";
  public string Texture="",Icon="",Sound="",SoundName=""; public bool SeamlessTexture=true; public bool FiniteAmmo=false;public double AmmoMetal=25,AmmoEnergy=50,AmmoBuildTime=5;
  public double SoundVolume=1; public bool SoundEachShot=false; public string CompareTemplate="";
  public AudioClip DestroyAudio=new AudioClip(),WorkAudio=new AudioClip(),FlightAudio=new AudioClip();
  public List<BuildStage> BuildStages=new List<BuildStage>();
  public WeaponArt Art=new WeaponArt();
  public bool HybridAA=false,HybridAutomatic=true;
  public string HybridNearProjectile="minigun";
  public double HybridNearRange=1800,HybridFarRange=14000,HybridHysteresis=250,HybridScanPeriod=.1;
  public double HybridNearRounds=13,HybridNearPeriod=.08,HybridNearReload=7,HybridNearSpeed=9000,HybridNearMetal=20,HybridNearEnergy=300;
  public double HybridNearSpread=.02,HybridNearDelay=.45;
  public bool ClusterEnabled=false,ClusterAtApex=true;public string ClusterProjectile="";public int ClusterCount=12;
  public double ClusterDelay=3,ClusterMinimumAge=.15,ClusterEjectionSpeed=350,ClusterSpread=.25;
  public double ProjectileVisualScale=1,ProjectileImageWidth=20;
  public double Thickness=12; public override string ToString(){return UiText.EntityName(this);}
  public double V(string key,double fallback){double n;return Values.TryGetValue(key,out n)?n:fallback;}
 }
 public class Extra {public string Key="Custom", Value="0",Scope="script";}
 public class Segment {public float X1,Y1,X2,Y2;public string Material="";public int Team=1;}
 public class PlacedItem {public float X,Y;public string Entity="";public bool Flipped=false;}
 public class PreviewOptions {public double GridUnits=50,AimDegrees=0,Power=1,TimeScale=.25;public int BuildTeam=1;public bool Grid=true,Bounds=true,Trajectory=true,RangeGrounded=true,FollowProjectile=false;public string Ammo="",Background="desert";}
 public class AudioClip {public string Id=Guid.NewGuid().ToString("N"),Name="",FileName="",Data="";public double Volume=1,Duration=2;public override string ToString(){return UiText.Get(Name);}}
 public class BuildStage {public int Progress=0;public string Image="",Name="";}
 public class Variable {public string Name="counter",Scope="object";public double Initial=0;public override string ToString(){return UiText.Get(Name);}}
 public class Block { public string Id=Guid.NewGuid().ToString("N"),Kind="condition",Type="enemy",Value="",Text="",Operator=">=",Scope="object",Tag="scenario";public double Number=100,Number2=.1;public List<Block> Children=new List<Block>(),Otherwise=new List<Block>(); }
 public class Chain {public string Name="Новая цепочка",Event="device_hit",Source="";public bool Enabled=true;public double Interval=1;public List<Block> Blocks=new List<Block>();public override string ToString(){return UiText.Get(Name);}}
 public class Param {
  public string Key,Label,Help,Scope;public double Min,Max,Default; public bool IsBool;
  public Param(string key,string label,string help,string scope,double value,double max,bool negative=false){Key=key;Label=label;Help=help;Scope=scope;Default=value;Max=max;Min=negative?-max:0;}
 }
 public class Template {
  public string Kind,Key,Name,Script,Texture,Sprite,ListSource;public Template(string kind,string key,string name,string script,string texture,string sprite,string listSource=""){Kind=kind;Key=key;Name=name;Script=script;Texture=texture;Sprite=sprite;ListSource=listSource;}
  public override string ToString(){return UiText.Get(Name);}
 }
 public static class Catalog {
  public static List<Template> Templates=new List<Template>{
   new Template("materials","bracing","Дерево","","materials/wood_panel.dds","bracing"),new Template("materials","armour","Броня","","materials/armour.dds","armour"),new Template("materials","backbracing","Фоновое дерево","","materials/wood_panel_bg.dds","bracing-bg"),
   new Template("devices","battery","Аккумулятор","devices/battery.lua","devices/battery/battery01.dds","battery_detail"),new Template("devices","store","Хранилище металла","devices/metal_store.lua","devices/metal_store/head.dds","store-head"),new Template("devices","mine","Шахта","devices/mine.lua","devices/mine/Mine01.dds","mine"),new Template("devices","turbine","Ветрогенератор","devices/windturbine.lua","devices/windturbine/head.dds","turbine-head"),
   new Template("devices","workshop","Мастерская","devices/workshop.lua","devices/tier2workshop/base.dds","workshop-base"),new Template("devices","upgrade","Центр модернизации","devices/upgradecentre.lua","devices/upgradecentre/base.dds","upgradecentre-base"),new Template("devices","factory","Завод","devices/factory.lua","devices/tier3factory/base.dds","factory-base"),
   new Template("weapons","cannon","Пушка","weapons/cannon.lua","weapons/cannon/head.dds","cannon-head"),new Template("weapons","machinegun","Пулемёт","weapons/machinegun.lua","weapons/machinegun/head.dds","machinegun-head"),new Template("weapons","mortar","Миномёт","weapons/mortar.lua","weapons/mortar/head.dds","mortar-head"),
   new Template("weapons","minigun","Скорострельный пулемёт","weapons/minigun.lua","weapons/minigun/head.dds","minigun-head"),new Template("weapons","flak","Зенитная пушка","mods/weapon_pack/weapons/flak.lua","mods/weapon_pack/weapons/flak/head.dds","flak-head","mods/weapon_pack/weapons/weapon_list.lua"),
   new Template("projectiles","cannon","Пушечный снаряд","","weapons/media/bullet.tga",""),new Template("projectiles","machinegun","Пуля","","weapons/media/bullet.tga",""),new Template("projectiles","mortar2","Мина","","objects/projectiles/mortar/mortar.dds",""),new Template("projectiles","missile","Управляемая ракета","","weapons/media/bullet.tga",""),new Template("projectiles","laser","Луч","","weapons/media/beam.tga",""),new Template("projectiles","flak","Штатный зенитный снаряд","","weapons/media/bullet.tga","","mods/weapon_pack/weapons/projectile_list.lua"),new Template("projectiles","minigun","Штатная пуля минигана","","weapons/media/bullet.tga","")
  };
  static List<Param> list=new List<Param>();
  static void P(string k,string n,string h,string s,double d,double max,bool neg=false){list.Add(new Param(k,n,h,s,d,max,neg));}
  static Catalog(){
   P("Mass","Масса","Физическая масса. Не влияет на толщину изображения.","material",.25,10000,true);
   P("Stiffness","Жёсткость","Сопротивление балки деформации. Больше — меньше прогиб.","material",200000,5000000);
   P("MaxCompression","Предел сжатия","Отношение текущей длины к исходной, при котором балка ломается при сжатии.","material",.9,5);
   P("MaxExpansion","Предел растяжения","Отношение текущей длины к исходной, при котором балка ломается при растяжении.","material",1.1,5);
   P("MinLength","Минимальная длина","Минимальная длина нового сегмента в игровых единицах.","material",37.5,5000);
   P("MaxLength","Максимальная длина","Максимальная длина строящегося сегмента.","material",150,5000);
   P("MaxLinkLength","Длина существующей балки","Предел длины соединения в физике Forts.","material",218,10000);
   P("AirDrag","Сопротивление воздуха","Сила сопротивления движению балки.","material",6.4,1000);
   P("AbsorptionMomentumThreshold","Поглощение импульса","Порог импульса снаряда для поглощения материалом.","material",30,100000);
   P("ReflectionMomentumThreshold","Отражение импульса","Порог импульса для отражения снаряда.","material",80,100000);
   P("PenetrationMomentumThreshold","Пробитие импульсом","Порог, после которого снаряд может пробить материал.","material",1000,1000000);
   P("RicochetVariationFactor","Разброс рикошета","Вариация направления отражённого снаряда.","material",0,10);
   P("SpeedLossFactor","Сохранение скорости","Множитель скорости после взаимодействия с материалом.","material",1,5);
   foreach(var x in new[]{"MetalBuildCost","MetalRepairCost","MetalReclaim","EnergyBuildCost","EnergyRepairCost","EnergyReclaim","EnergyRunCost"})P(x,Labels[x],Helps[x],"material",0,100000);
   P("BuildTime","Время постройки","Время строительства балки, секунды.","material",5,3600);P("ScrapTime","Время разбора","Время демонтажа балки, секунды.","material",3,3600);
   P("HitPoints","Прочность","Количество урона до разрушения.","all",150,1000000);
   foreach(var x in new[]{"MetalCost","EnergyCost","MetalRepairCost","EnergyRepairCost","BuildTimeComplete","ScrapPeriod","MaxUpAngle"})P(x,Labels[x],Helps[x],"list",x=="MaxUpAngle"?20:10,x=="MaxUpAngle"?180:100000);
   P("Mass","Масса сооружения","Физическая масса сооружения.","device",40,100000,true);P("WeaponMass","Масса орудия","Физическая масса орудия.","weapon",120,100000,true);
   P("Scale","Масштаб спрайта","Масштаб модели. Свои изображения сооружений и слои оружия уменьшаются при экспорте; размер в игровых единицах равен половине размера текстуры в пикселях.","script",1,10);
   P("SelectionWidth","Ширина рамки выбора","Ширина области, по которой объект можно выбрать мышью. Физическая геометрия зависит также от спрайта и модели.","script",95,2000);
   P("SelectionHeight","Высота рамки выбора","Высота области выделения. Оранжевая рамка предпросмотра отражает это значение.","script",60,2000);
   P("SelectionOffsetX","Смещение рамки X","Смещение области выбора относительно точки крепления.","meta",0,2000,true);P("SelectionOffsetY","Смещение рамки Y","Вертикальное смещение области выбора. Отрицательное значение — вверх.","meta",-40,2000,true);
   foreach(var x in new[]{"MetalProductionRate","EnergyProductionRate","MetalStorageCapacity","EnergyStorageCapacity","DeviceSplashDamage","DeviceSplashDamageMaxRadius","DeviceSplashDamageDelay","StructureSplashDamage","StructureSplashDamageMaxRadius","IncendiaryRadius"})P(x,Labels[x],Helps[x],"script",0,100000,x.EndsWith("ProductionRate"));
   P("BarrelLength","Длина ствола","Расстояние от узла оружия до точки выхода снаряда.","weapon",100,2000);P("ReloadTime","Перезарядка","Интервал между залпами, секунды.","weapon",26,3600);
   P("RoundsEachBurst","Снарядов в залпе","Количество снарядов в одном залпе.","weapon",1,256);P("RoundPeriod","Интервал снарядов","Пауза между снарядами в залпе, секунды.","weapon",.1,60);
   P("ProjectilesEachRound","Одновременно за выстрел","Количество снарядов, создаваемых одновременно при каждом выстреле залпа.","weapon",1,256);
   P("MinFireSpeed","Минимальная скорость","Нижняя граница скорости запуска снаряда.","weapon",6000,100000);P("MaxFireSpeed","Максимальная скорость","Верхняя граница скорости запуска снаряда.","weapon",6000.1,100000);
   P("MinFireAngle","Минимальный угол","Нижняя граница угла наведения в градусах. Знак нужен для направления вниз.","weapon",-20,180,true);P("MaxFireAngle","Максимальный угол","Верхняя граница угла наведения, градусы.","weapon",30,180);
   P("MinFireRadius","Ближняя граница наведения","Минимальное расстояние курсора для наведения.","weapon",600,10000);P("MaxFireRadius","Дальняя граница наведения","Максимальное расстояние курсора для наведения.","weapon",1200,20000);
   P("FireStdDev","Разброс","Стандартное отклонение направления выстрела.","weapon",.01,10);P("Recoil","Отдача","Импульс, передаваемый форту при выстреле.","weapon",600000,10000000);
   P("MetalFireCost","Металл за выстрел","Металл, расходуемый на один выстрел.","weapon",50,100000);P("EnergyFireCost","Энергия за выстрел","Энергия, расходуемая на один выстрел.","weapon",2000,1000000);
   P("ProjectileMass","Масса снаряда","Масса влияет на импульс и взаимодействие с материалами.","projectile",16,10000,true);P("ProjectileDrag","Сопротивление снаряда","Сопротивление воздуха; снижает скорость полёта.","projectile",0,1000);
   P("ProjectileDamage","Прямой урон","Урон в точке попадания снаряда.","projectile",700,1000000);P("ProjectileSplashDamage","Урон по площади","Дополнительный урон вокруг точки взрыва.","projectile",30,1000000);P("ProjectileSplashDamageMaxRadius","Радиус взрыва","Максимальный радиус урона по площади.","projectile",200,10000);
   P("ProjectileSplashMaxForce","Сила взрыва","Максимальная сила ударной волны.","projectile",10000,10000000);P("ProjectileThickness","Толщина снаряда","Толщина снаряда при столкновении.","projectile",10,2000);P("MaxAge","Время жизни","Время существования снаряда до удаления, секунды.","projectile",30,3600);P("Impact","Импульс попадания","Механический импульс попадания.","projectile",20000,10000000);
   P("WeaponDamageBonus","Бонус урона орудиям","Дополнительный урон при попадании в оружие.","projectile",0,1000000);P("DeviceDamageBonus","Бонус урона сооружениям","Дополнительный урон при попадании в устройство.","projectile",0,1000000);P("AntiAirDamage","Урон снарядам","Урон по другим снарядам при перехвате.","projectile",0,100000);
   P("AntiAirHitpoints","Прочность против перехвата","Количество урона ПВО, которое выдерживает снаряд. Повреждённый режим настраивается отдельным типом снаряда.","projectile",1,100000);
   P("ProjectileShootDownRadius","Радиус перехвата","Радиус обнаружения попадания противоснарядных средств в этот снаряд.","projectile",12,1000);
  }
  static Dictionary<string,string> Labels=new Dictionary<string,string>{{"MetalBuildCost","Металл на постройку"},{"MetalRepairCost","Металл на ремонт"},{"MetalReclaim","Возврат металла"},{"EnergyBuildCost","Энергия на постройку"},{"EnergyRepairCost","Энергия на ремонт"},{"EnergyReclaim","Возврат энергии"},{"EnergyRunCost","Расход энергии"},{"MetalCost","Цена: металл"},{"EnergyCost","Цена: энергия"},{"BuildTimeComplete","Время постройки"},{"ScrapPeriod","Время разбора"},{"MaxUpAngle","Наклон площадки"},{"MetalProductionRate","Выработка металла/с"},{"EnergyProductionRate","Выработка энергии/с"},{"MetalStorageCapacity","Запас металла"},{"EnergyStorageCapacity","Запас энергии"},{"DeviceSplashDamage","Взрыв: урон устройствам"},{"DeviceSplashDamageMaxRadius","Взрыв: радиус по устройствам"},{"DeviceSplashDamageDelay","Задержка взрыва"},{"StructureSplashDamage","Взрыв: урон балкам"},{"StructureSplashDamageMaxRadius","Взрыв: радиус по балкам"},{"IncendiaryRadius","Радиус поджога"}};
  static Dictionary<string,string> Helps=new Dictionary<string,string>{{"MetalBuildCost","Стоимость балки в металле, зависит от длины."},{"MetalRepairCost","Расход металла при восстановлении прочности."},{"MetalReclaim","Возвращаемый металл при разборе, зависит от длины."},{"EnergyBuildCost","Стоимость строительства в энергии, зависит от длины."},{"EnergyRepairCost","Расход энергии на восстановление прочности."},{"EnergyReclaim","Энергия, возвращаемая при разборе."},{"EnergyRunCost","Постоянный расход энергии материалом."},{"MetalCost","Металл на создание объекта."},{"EnergyCost","Энергия на создание объекта."},{"BuildTimeComplete","Длительность строительства в секундах."},{"ScrapPeriod","Время демонтажа в секундах."},{"MaxUpAngle","Максимальный наклон платформы, на которой можно построить объект."},{"MetalProductionRate","Металл в секунду. Отрицательное значение расходует металл."},{"EnergyProductionRate","Энергия в секунду. Отрицательное значение расходует энергию."},{"MetalStorageCapacity","Дополнительная вместимость хранилища металла."},{"EnergyStorageCapacity","Дополнительная вместимость хранилища энергии."},{"DeviceSplashDamage","Урон соседним устройствам при разрушении объекта."},{"DeviceSplashDamageMaxRadius","Максимальный радиус урона устройствам при разрушении."},{"DeviceSplashDamageDelay","Пауза между разрушением и взрывом, секунды."},{"StructureSplashDamage","Урон балкам при разрушении объекта."},{"StructureSplashDamageMaxRadius","Максимальный радиус урона балкам при разрушении."},{"IncendiaryRadius","Радиус воспламенения при разрушении."}};
  public static IEnumerable<Param> Parameters(string kind){return list.Where(p=>(p.Scope=="all"&&kind!="projectiles")||(kind=="materials"?p.Scope=="material":kind=="projectiles"?p.Scope=="projectile":p.Scope=="list"||p.Scope=="script"||p.Scope=="meta"||p.Scope==(kind=="weapons"?"weapon":"device")));}
  public static Template Get(Entity e){return Templates.FirstOrDefault(t=>t.Kind==e.Kind&&t.Key==e.Template)??Templates.First(t=>t.Kind==e.Kind);}
  public static string Table(string kind){return kind=="materials"?"Materials":kind=="devices"?"Devices":kind=="weapons"?"Weapons":"Projectiles";}
  public static string ListPath(string kind){return kind=="materials"?"materials/building_materials.lua":kind=="devices"?"devices/device_list.lua":kind=="weapons"?"weapons/weapon_list.lua":"weapons/projectile_list.lua";}
  public static string TemplateListPath(Template t){return String.IsNullOrEmpty(t.ListSource)?ListPath(t.Kind):t.ListSource;}
  public static Entity Create(string kind,string template,string game){var e=new Entity{Kind=kind,Template=template,Key="fms_"+template+"_"+DateTime.Now.ToString("HHmmss"),NameRu="Новый объект",NameEn="New object"};Fill(e,game);if(kind=="materials")e.Thickness=MaterialVisuals.NativeHeight(e,game);if(kind=="devices"&&TechnologyRules.IsTemplate(template)){e.IsTechnology=true;e.NameRu="Новая технология";e.NameEn="New technology";if(template=="factory"){e.RequireTechnology=true;e.RequiredTechnology="workshop";}}return e;}
  public static void Fill(Entity e,string game){e.Values.Clear();e.Flags.Clear();var t=Get(e);string script="",entry="";
   if(Directory.Exists(Path.Combine(game,"data"))){try{var source=File.ReadAllText(Path.Combine(game,"data",TemplateListPath(t)));entry=LuaTable(source,t.Key);if(e.Kind=="materials"){string parent=t.Key=="armour"?"Armor":"Bracing";var match=Regex.Match(source,@"(?m)^"+parent+@"\s*=\s*\{");if(match.Success)entry+=Balanced(source,source.IndexOf('{',match.Index));}if(t.Script.Length>0)script=File.ReadAllText(Path.Combine(game,"data",t.Script));}catch{}}
   foreach(var p in Parameters(e.Kind)){var text=p.Scope=="list"||e.Kind=="materials"||e.Kind=="projectiles"?entry:script;var m=Regex.Match(text,@"(?m)^\s*"+p.Key+@"\s*=\s*(-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)");double v=p.Default;if(m.Success)Double.TryParse(m.Groups[1].Value,NumberStyles.Float,CultureInfo.InvariantCulture,out v);e.Values[p.Key]=v;}
   var offset=Regex.Match(script,@"SelectionOffset\s*=\s*\{\s*(-?[\d.]+)\s*,\s*(-?[\d.]+)");if(offset.Success){e.Values["SelectionOffsetX"]=Double.Parse(offset.Groups[1].Value,CultureInfo.InvariantCulture);e.Values["SelectionOffsetY"]=Double.Parse(offset.Groups[2].Value,CultureInfo.InvariantCulture);}
   string[] flags=e.Kind=="materials"?new[]{"SupportsDevices","ReflectsBeams","CatchesFire","IsBehindDevices","AttachesCladding","FullExtrusion"}:e.Kind=="projectiles"?new[]{"ExplodeOnTouch","PassesThroughMaterials","ProjectileIncendiary","DestroyShields","DeflectedByShields"}:new[]{"CanFlip","IgnitePlatformOnDestruct"};
   foreach(string f in flags){var m=Regex.Match(entry+"\n"+script,@"\b"+f+@"\s*=\s*(true|false)");e.Flags[f]=m.Success?m.Groups[1].Value=="true":f=="SupportsDevices"||f=="CatchesFire"||f=="FullExtrusion"&&t.Key!="armour";}
  }
  public static string LuaTable(string src,string key){var m=Regex.Match(src,@"\bSaveName\s*=\s*"""+Regex.Escape(key)+@"""");if(!m.Success)return "";int a=src.LastIndexOf('{',m.Index);return a<0?"":Balanced(src,a);}
  public static string Balanced(string s,int start){int depth=0;bool quote=false;char q='\0';for(int i=start;i<s.Length;i++){char c=s[i];if(quote){if(c=='\\'){i++;continue;}if(c==q)quote=false;continue;}if(c=='"'||c=='\''){quote=true;q=c;continue;}if(c=='{')depth++;if(c=='}'&&--depth==0)return s.Substring(start,i-start+1);}return "";}
  public static string FindGame(){var paths=new List<string>{@"C:\Program Files (x86)\Steam\steamapps\common\Forts"};try{var steam=Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");if(steam!=null){string p=Convert.ToString(steam.GetValue("SteamPath"));paths.Add(Path.Combine(p,"steamapps","common","Forts"));string f=Path.Combine(p,"steamapps","libraryfolders.vdf");if(File.Exists(f))foreach(Match m in Regex.Matches(File.ReadAllText(f),"\"path\"\\s+\"([^\"]+)\""))paths.Add(Path.Combine(m.Groups[1].Value.Replace("\\\\","\\"),"steamapps","common","Forts"));}}catch{}return paths.FirstOrDefault(p=>File.Exists(Path.Combine(p,"Forts.exe")))??"";}
 }
 public static class Storage {
  public static JavaScriptSerializer Json(){return new JavaScriptSerializer{MaxJsonLength=100000000,RecursionLimit=128};}
  public static void Save(Project p,string path){Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));File.WriteAllText(path,Json().Serialize(p),new UTF8Encoding(false));}
  public static Project Load(string path){string text=File.ReadAllText(path);var obj=Json().Deserialize<Dictionary<string,object>>(text);if(obj.ContainsKey("Format")){var p=Json().Deserialize<Project>(text);foreach(var e in p.Entities)if(e.Kind=="materials"&&!e.Flags.ContainsKey("FullExtrusion"))e.Flags["FullExtrusion"]=e.Template!="armour";return p;}return Legacy(obj);}
  static Project Legacy(Dictionary<string,object> root){var p=new Project();p.GameRoot=Catalog.FindGame();var map=new Dictionary<string,string>{{"mass","Mass"},{"weaponMass","WeaponMass"},{"hitPoints","HitPoints"},{"metalCost","MetalCost"},{"energyCost","EnergyCost"},{"metalBuildCost","MetalBuildCost"},{"energyBuildCost","EnergyBuildCost"},{"metalRepairCost","MetalRepairCost"},{"energyRepairCost","EnergyRepairCost"},{"energyProductionRate","EnergyProductionRate"},{"metalProductionRate","MetalProductionRate"},{"energyStorageCapacity","EnergyStorageCapacity"},{"metalStorageCapacity","MetalStorageCapacity"},{"reloadTime","ReloadTime"},{"barrelLength","BarrelLength"},{"projectileCount","RoundsEachBurst"},{"roundPeriod","RoundPeriod"},{"minFireSpeed","MinFireSpeed"},{"maxFireSpeed","MaxFireSpeed"},{"minFireAngle","MinFireAngle"},{"maxFireAngle","MaxFireAngle"},{"minFireRadius","MinFireRadius"},{"maxFireRadius","MaxFireRadius"},{"energyFireCost","EnergyFireCost"},{"metalFireCost","MetalFireCost"},{"projectileMass","ProjectileMass"},{"projectileDrag","ProjectileDrag"},{"projectileThickness","ProjectileThickness"},{"damage","ProjectileDamage"},{"splashDamage","ProjectileSplashDamage"},{"splashRadius","ProjectileSplashDamageMaxRadius"},{"maxLife","MaxAge"},{"selectWidth","SelectionWidth"},{"selectHeight","SelectionHeight"},{"maxLength","MaxLength"},{"maxLinkLength","MaxLinkLength"},{"stiffness","Stiffness"},{"airDrag","AirDrag"},{"maxCompression","MaxCompression"},{"maxExpansion","MaxExpansion"}};
   if(root.ContainsKey("mod")){var mod=(Dictionary<string,object>)root["mod"];p.Folder=S(mod,"slug","fms_imported");p.NameRu=S(mod,"nameRu",S(mod,"name","Импортированный мод"));p.NameEn=S(mod,"nameEn",p.NameRu);p.DescriptionRu=S(mod,"descriptionRu",S(mod,"description",""));p.DescriptionEn=S(mod,"descriptionEn","");}
   foreach(string kind in new[]{"materials","devices","weapons","projectiles"}){if(!root.ContainsKey(kind))continue;foreach(var item in (System.Collections.IEnumerable)root[kind]){var d=(Dictionary<string,object>)item;var e=Catalog.Create(kind,kind=="materials"?"bracing":kind=="devices"?"battery":"cannon",p.GameRoot);e.Key=S(d,"saveName",e.Key);e.NameRu=S(d,"nameRu",S(d,"name",e.Key));e.NameEn=S(d,"nameEn",e.Key);e.DescriptionRu=S(d,"descriptionRu","");e.DescriptionEn=S(d,"descriptionEn","");foreach(var kv in map){double n;if(d.ContainsKey(kv.Key)&&Double.TryParse(Convert.ToString(d[kv.Key],CultureInfo.InvariantCulture),NumberStyles.Float,CultureInfo.InvariantCulture,out n))e.Values[kv.Value]=n;}
    double time;if(d.ContainsKey("buildTime")&&Double.TryParse(Convert.ToString(d["buildTime"],CultureInfo.InvariantCulture),NumberStyles.Float,CultureInfo.InvariantCulture,out time))e.Values[kind=="materials"?"BuildTime":"BuildTimeComplete"]=time;
    if(d.ContainsKey("asset")&&d["asset"] is Dictionary<string,object>)e.Texture=S((Dictionary<string,object>)d["asset"],"data","");if(d.ContainsKey("icon")&&d["icon"] is Dictionary<string,object>)e.Icon=S((Dictionary<string,object>)d["icon"],"data","");if(d.ContainsKey("sound")&&d["sound"] is Dictionary<string,object>){var a=(Dictionary<string,object>)d["sound"];e.Sound=S(a,"data","");e.SoundName=S(a,"name","sound.wav");}
    if(d.ContainsKey("ammoTypes")){foreach(var a in (System.Collections.IEnumerable)d["ammoTypes"])e.Ammo.Add(Convert.ToString(a));}else if(d.ContainsKey("projectile"))e.Ammo.Add(S(d,"projectile","cannon"));e.FiniteAmmo=d.ContainsKey("finiteAmmo")&&Convert.ToBoolean(d["finiteAmmo"]);p.Entities.Add(e);
   }}return p;
  }
  static string S(Dictionary<string,object> d,string k,string fallback){return d.ContainsKey(k)&&d[k]!=null?Convert.ToString(d[k]):fallback;}
 }
}
