using System;using System.Collections.Generic;using System.Drawing;using System.Linq;using System.Windows.Forms;
namespace FortsStudio {
 public partial class Studio {
  IEnumerable<Choice> ProjectileChoices(Entity owner=null){foreach(var e in project.Entities.Where(x=>x.Kind=="projectiles"&&x!=owner))yield return new Choice(e.Key,e.NameRu+" · "+e.Key);foreach(var t in Catalog.Templates.Where(x=>x.Kind=="projectiles"))yield return new Choice(t.Key,t.Name+" · Forts");}
  void BehaviorProjectile(FlowLayoutPanel f,string label,string value,Action<string> set,Entity owner=null){f.Controls.Add(Theme.Label(label,9,true));var combo=Theme.Combo();foreach(var choice in ProjectileChoices(owner))combo.Items.Add(choice);combo.SelectedItem=combo.Items.Cast<Choice>().FirstOrDefault(x=>x.Key==value);if(combo.SelectedIndex<0&&!String.IsNullOrEmpty(value)){var missing=new Choice(value,value+" · не найден");combo.Items.Add(missing);combo.SelectedItem=missing;}combo.SelectedIndexChanged+=(s,a)=>{if(combo.SelectedItem!=null){set(((Choice)combo.SelectedItem).Key);Mark();}};f.Controls.Add(combo);}
  void DrawAdvancedBehavior(FlowLayoutPanel f,Entity e){
   if(e.Kind=="projectiles"){
    Header(f,"Кассетный боеприпас");
    var enabled=new UiCheckBox{Text="Разделять снаряд на боевые части",Checked=e.ClusterEnabled,AutoSize=true,ForeColor=Theme.Text};enabled.CheckedChanged+=(s,a)=>{e.ClusterEnabled=enabled.Checked;Mark();ShowEntityTab(e,"Поведение");};f.Controls.Add(enabled);
    f.Controls.Add(Theme.Label("Носитель исчезает при разделении. Новые снаряды получают собственные массу, урон и обычную игровую физику. Выберите отдельную мину без наведения, если нужен свободный баллистический полёт.",9));
    if(!e.ClusterEnabled)return;
    BehaviorProjectile(f,"Тип боевой части",e.ClusterProjectile,v=>e.ClusterProjectile=v,e);
    Number(f,"Количество боевых частей","Сколько отдельных снарядов создаёт движок. Больше частей — больше нагрузка на игру.",e.ClusterCount,1,64,v=>e.ClusterCount=(int)Math.Round(v),"ClusterCount");
    var apex=new UiCheckBox{Text="Разделять у вершины траектории",Checked=e.ClusterAtApex,AutoSize=true,ForeColor=Theme.Text};apex.CheckedChanged+=(s,a)=>{e.ClusterAtApex=apex.Checked;Mark();ShowEntityTab(e,"Поведение");};f.Controls.Add(apex);
    if(!e.ClusterAtApex)Number(f,"Задержка после выстрела, с","Время разделения по таймеру вместо вершины траектории.",e.ClusterDelay,.02,30,v=>e.ClusterDelay=v,"ClusterDelay");
    Number(f,"Минимальное время полёта, с","Защита от немедленного разделения при выстреле вниз.",e.ClusterMinimumAge,0,10,v=>e.ClusterMinimumAge=v,"ClusterMinimumAge");
    Number(f,"Скорость разлёта частей","Небольшой импульс разлёта. Затем действуют гравитация и сопротивление, заданные у боевой части.",e.ClusterEjectionSpeed,0,3000,v=>e.ClusterEjectionSpeed=v,"ClusterEjectionSpeed");
    Number(f,"Разброс разлёта, °","Разброс направлений дочерних снарядов. В игре параметр хранится в радианах.",e.ClusterSpread*180/Math.PI,0,90,v=>e.ClusterSpread=v*Math.PI/180,"ClusterSpread");
   }else if(e.Kind=="weapons"){
    Header(f,"Двухрежимная противовоздушная оборона");
    var enabled=new UiCheckBox{Text="Зенитный залп вдали, пулемётная очередь вблизи",Checked=e.HybridAA,AutoSize=true,ForeColor=Theme.Text,MaximumSize=new Size(480,0)};enabled.CheckedChanged+=(s,a)=>{e.HybridAA=enabled.Checked;Mark();ShowEntityTab(e,"Поведение");};f.Controls.Add(enabled);
    f.Controls.Add(Theme.Label("Дальний боеприпас и его параметры задаются в «Основное» и «Параметры». Ближний режим настраивается здесь. Для двух режимов требуется High Seas. Сами штатные снаряды не изменяются.",9));
    if(!e.HybridAA)return;
    var auto=new UiCheckBox{Text="Автоматически наводиться и стрелять по снарядам",Checked=e.HybridAutomatic,AutoSize=true,ForeColor=Theme.Text,MaximumSize=new Size(480,0)};auto.CheckedChanged+=(s,a)=>{e.HybridAutomatic=auto.Checked;Mark();};f.Controls.Add(auto);
    f.Controls.Add(Theme.Label("Без галочки остаётся ручной выбор двух режимов. Автоматика стреляет, когда орудие не выбрано игроком; учитывает препятствия, ресурсы и перезарядку. Начатая очередь завершается перед сменой режима.",9));
    Number(f,"Граница ближнего режима","Расстояние до вражеского снаряда, игровые единицы. 50 единиц — клетка редактора.",e.HybridNearRange,1,10000,v=>e.HybridNearRange=v,"HybridNearRange");
    Number(f,"Дальность обнаружения","Внешняя граница работы автоматики. Для штатного flak разумный начальный ориентир — 8000.",e.HybridFarRange,1,30000,v=>e.HybridFarRange=v,"HybridFarRange");
    Number(f,"Запас границы переключения","Предотвращает частую смену режима у границы дальности.",e.HybridHysteresis,0,1000,v=>e.HybridHysteresis=v,"HybridHysteresis");
    Number(f,"Интервал поиска цели, с","0,1 с — три игровых кадра. Малые интервалы увеличивают нагрузку.",e.HybridScanPeriod,.025,1,v=>e.HybridScanPeriod=v,"HybridScanPeriod");
    Header(f,"Ближняя очередь");BehaviorProjectile(f,"Боеприпас ближнего режима",e.HybridNearProjectile,v=>e.HybridNearProjectile=v);
    Number(f,"Выстрелов в очереди","У штатного минигана 13.",e.HybridNearRounds,1,256,v=>e.HybridNearRounds=Math.Round(v),"HybridNearRounds");
    Number(f,"Интервал выстрелов, с","У штатного минигана 0,08 с.",e.HybridNearPeriod,.001,1,v=>e.HybridNearPeriod=v,"HybridNearPeriod");
    Number(f,"Перезарядка ближнего режима, с","У штатного минигана 7 с. Смена режима не обнуляет текущую перезарядку.",e.HybridNearReload,0,30,v=>e.HybridNearReload=v,"HybridNearReload");
    Number(f,"Скорость ближних снарядов","У штатного минигана 9000.",e.HybridNearSpeed,1,30000,v=>e.HybridNearSpeed=v,"HybridNearSpeed");
    Number(f,"Металл за ближний выстрел","Стоимость применяется движком через выбранный боеприпас.",e.HybridNearMetal,0,500,v=>e.HybridNearMetal=v,"HybridNearMetal");
    Number(f,"Энергия за ближний выстрел","Штатный ориентир минигана — 300.",e.HybridNearEnergy,0,3000,v=>e.HybridNearEnergy=v,"HybridNearEnergy");
    Number(f,"Разброс ближнего режима","Стандартное отклонение направления; у минигана 0,02.",e.HybridNearSpread,0,.1,v=>e.HybridNearSpread=v,"HybridNearSpread");
    Number(f,"Задержка начала очереди, с","Раскрутка; у штатного минигана 0,45 с.",e.HybridNearDelay,0,2,v=>e.HybridNearDelay=v,"HybridNearDelay");
   }
  }
 }
 public static partial class Exporter {
  static bool Finite(double x,double min,double max){return !Double.IsNaN(x)&&!Double.IsInfinity(x)&&x>=min&&x<=max;}
  static void ValidateArtwork(Project p,List<string> errors){foreach(var e in p.Entities){
   if((e.Kind=="devices"||e.Kind=="weapons")&&!Finite(e.V("Scale",1),.01,10))errors.Add(e.NameRu+": масштаб изображения должен быть 0,01–10.");
   if(!Finite(e.ProjectileVisualScale,.05,20)||!Finite(e.ProjectileImageWidth,1,1024))errors.Add(e.NameRu+": недопустимый размер изображения снаряда.");
   var a=e.Art;if(a==null||!a.Enabled)continue;
   if(e.Kind!="weapons")errors.Add(e.NameRu+": послойная модель доступна орудиям.");
   if(String.IsNullOrEmpty(a.BaseImage)||String.IsNullOrEmpty(a.HeadImage))errors.Add(e.NameRu+": загрузите оба слоя орудия.");
   if(!Finite(a.BaseWidth,2,1024)||!Finite(a.BaseHeight,2,1024)||!Finite(a.HeadWidth,2,1024)||!Finite(a.HeadHeight,2,1024))errors.Add(e.NameRu+": размеры слоёв должны быть 2–1024 ед.");
   if(Math.Max(Math.Max(a.BaseWidth,a.BaseHeight),Math.Max(a.HeadWidth,a.HeadHeight))*e.V("Scale",1)>2048)errors.Add(e.NameRu+": размер слоя с учётом масштаба превышает 2048 игровых единиц (4096 пикселей).");
   if(!Finite(a.PivotX,0,1)||!Finite(a.PivotY,0,1)||!Finite(a.MuzzleX,0,1)||!Finite(a.MuzzleY,0,1)||!Finite(a.MountX,-1024,1024)||!Finite(a.MountY,0,1024)||!Finite(a.RecoilDistance,0,128))errors.Add(e.NameRu+": недопустимые координаты послойной модели.");
  }}
 }
}
