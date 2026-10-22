using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;

namespace FortsStudio {
 public static class TechnologyRules {
  public static bool IsTemplate(string key){return key=="workshop"||key=="upgrade"||key=="factory";}
  public static bool IsTechnology(Entity e){return e.Kind=="devices"&&e.IsTechnology;}
  public static IEnumerable<Choice> Choices(Project project,Entity owner){
   foreach(var e in project.Entities.Where(x=>IsTechnology(x)&&x!=owner))
    yield return new Choice(e.Key,e.NameRu+" · свой проект ("+e.Key+")");
   foreach(var t in Catalog.Templates.Where(x=>x.Kind=="devices"&&IsTemplate(x.Key)))
    yield return new Choice(t.Key,t.Name+" · Forts ("+t.Key+")");
  }
  public static List<Entity> Users(Project p,Entity technology){return p.Entities.Where(x=>x!=technology&&x.RequireTechnology&&x.RequiredTechnology==technology.Key).ToList();}
  public static string Name(Project p,string key){var e=p.Entities.FirstOrDefault(x=>x.Key==key);if(e!=null)return e.NameRu;var t=Catalog.Templates.FirstOrDefault(x=>x.Kind=="devices"&&IsTemplate(x.Key)&&x.Key==key);return t==null?key:t.Name;}
  public static void Validate(Project p,List<string> errors){
   var technologies=p.Entities.Where(x=>IsTechnology(x)&&!String.IsNullOrEmpty(x.Key)).GroupBy(x=>x.Key).ToDictionary(x=>x.Key,x=>x.First());
   foreach(var e in p.Entities){
    if(e.IsTechnology&&e.Kind!="devices")errors.Add(e.NameRu+": технология должна быть сооружением.");
    if(!e.RequireTechnology)continue;
    if(e.Kind=="projectiles"){errors.Add(e.NameRu+": технология задаётся орудию, а не его снаряду.");continue;}
    if(String.IsNullOrEmpty(e.RequiredTechnology)){errors.Add(e.NameRu+": включено требование технологии, но технология не выбрана.");continue;}
    if(e.RequiredTechnology==e.Key){errors.Add(e.NameRu+": объект не может требовать собственную постройку.");continue;}
    if(!technologies.ContainsKey(e.RequiredTechnology)&&!IsTemplate(e.RequiredTechnology))errors.Add(e.NameRu+": не найдена технология «"+e.RequiredTechnology+"». Выберите технологию во вкладке «Основное».");
    if(e.Extras.Any(x=>x.Key=="Prerequisite"||x.Key=="PrerequisiteAlt"))errors.Add(e.NameRu+": удалите дополнительное поле Prerequisite / PrerequisiteAlt — требование уже задано выбором технологии.");
   }
   // Follow the single-parent graph without recursion. Reject an unreachable
   // technology chain rather than exporting items the player can never unlock.
   var reported=new HashSet<string>();
   foreach(var start in technologies.Values){
    var path=new List<string>();var indices=new Dictionary<string,int>();Entity current=start;
    while(current!=null){
     int index;
     if(indices.TryGetValue(current.Key,out index)){
      var cycle=path.Skip(index).ToList();string identity=String.Join("|",cycle.OrderBy(x=>x));
      if(cycle.Count>1&&reported.Add(identity))errors.Add("Цикл технологий: "+String.Join(" → ",cycle.Concat(new[]{current.Key}).Select(x=>Name(p,x)))+". Ни одно из этих зданий нельзя будет построить первым.");
      break;
     }
     indices[current.Key]=path.Count;path.Add(current.Key);
     if(!current.RequireTechnology||!technologies.TryGetValue(current.RequiredTechnology??"",out current))break;
    }
   }
  }
 }

 public partial class Studio {
  void DrawTechnologyRequirement(FlowLayoutPanel f,Entity e){
   Header(f,"Открытие через технологию");
   var required=new UiCheckBox{Text="Требуется построенная технология",Checked=e.RequireTechnology,AutoSize=true,ForeColor=Theme.Text,Margin=new Padding(0,4,0,8)};
   f.Controls.Add(required);
   var choose=Theme.Combo();
   foreach(var item in TechnologyRules.Choices(project,e))choose.Items.Add(item);
   var selectedChoice=choose.Items.Cast<Choice>().FirstOrDefault(x=>x.Key==e.RequiredTechnology);
   if(selectedChoice==null&&!String.IsNullOrEmpty(e.RequiredTechnology)){
    selectedChoice=new Choice(e.RequiredTechnology,"Не найдена технология · "+e.RequiredTechnology);choose.Items.Add(selectedChoice);
   }
   choose.SelectedItem=selectedChoice;
   if(choose.SelectedIndex<0&&choose.Items.Count>0)choose.SelectedIndex=0;
   choose.Enabled=e.RequireTechnology;f.Controls.Add(choose);
   var summary=Theme.Label("",9);summary.ForeColor=Theme.Accent;f.Controls.Add(summary);
   Action update=()=>summary.Text=e.RequireTechnology?"Открывается после: "+TechnologyRules.Name(project,e.RequiredTechnology)+". Условие проверяет сама Forts.":"Доступно с начала матча при наличии ресурсов.";
   required.CheckedChanged+=(s,a)=>{e.RequireTechnology=required.Checked;choose.Enabled=required.Checked;if(required.Checked&&choose.SelectedItem!=null)e.RequiredTechnology=((Choice)choose.SelectedItem).Key;update();Mark();};
   choose.SelectedIndexChanged+=(s,a)=>{if(choose.SelectedItem==null)return;e.RequiredTechnology=((Choice)choose.SelectedItem).Key;update();Mark();};
   update();
   f.Controls.Add(Theme.Label("Выберите своё технологическое здание или стандартную технологию Forts. Нужно завершить его строительство. Это условие наличия здания, а не одноразовое исследование навсегда.",9));
   if(e.IsTechnology){
    Header(f,"Что открывает эта технология");
    var users=TechnologyRules.Users(project,e);
    if(users.Count==0)f.Controls.Add(Theme.Label("Пока ничего. Откройте материал, сооружение или орудие, поставьте галочку «Требуется построенная технология» и выберите это здание.",9));
    foreach(var user in users){var target=user;f.Controls.Add(Theme.Button((target.IsTechnology?"Технология":names[target.Kind])+": "+target.NameRu,()=>{Switch(TechnologyRules.IsTechnology(target)?"technologies":target.Kind,target);}));}
    f.Controls.Add(Theme.Label("У самой технологии также может быть предшественник. Так можно собрать цепочку: мастерская → новая технология → сооружения и материалы.",9));
   }
  }
  void RemoveEntity(Entity e){
   var users=TechnologyRules.Users(project,e).Concat(project.Entities.Where(x=>x!=e&&(x.ClusterEnabled&&x.ClusterProjectile==e.Key||x.HybridAA&&x.HybridNearProjectile==e.Key||x.Ammo.Contains(e.Key)))).Distinct().ToList();
   if(users.Count>0){UiDialogs.Show("Этот объект используется здесь:\n\n"+String.Join("\n",users.Select(x=>"• "+x.NameRu))+"\n\nСначала измените соответствующую технологию или боеприпас у этих объектов.","Объект используется",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
   if(UiDialogs.Show("Удалить «"+e.NameRu+"» из проекта?","Удаление объекта",MessageBoxButtons.OKCancel)!=DialogResult.OK)return;
   project.Entities.Remove(e);foreach(var w in project.Entities){w.Ammo.Remove(e.Key);if(!w.RequireTechnology&&w.RequiredTechnology==e.Key)w.RequiredTechnology="";}Switch(kind);Mark();
  }
 }
}
