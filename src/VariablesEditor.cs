using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace FortsStudio {
 public partial class Studio {
  static bool VariableBlock(Block b){return b.Type=="variable"||b.Type=="set_var"||b.Type=="add_var"||b.Type=="multiply_var"||b.Type=="edit_var"||b.Kind=="loop"&&b.Type=="while";}
  static string CompactVariableText(string value){return value!=null&&value.Length>280?value.Substring(0,277)+"…":value??"";}
  void DrawVariablesPane(FlowLayoutPanel f,int width){
   f.Controls.Add(Theme.Label("Переменные проекта · "+project.Variables.Count,18,true));
   var note=Theme.Label("Все объявленные переменные. Показаны начальные значения проекта; текущие значения внутри запущенной Forts здесь не считываются.",9);note.ForeColor=Theme.Muted;note.MaximumSize=new Size(width,0);f.Controls.Add(note);
   f.Controls.Add(Theme.Button("＋ Переменная",()=>VariableDialog(null),true));
   Dictionary<string,object> values=null;string failure="";
   try{values=VariableTools.ResolveInitial(project);}catch(Exception ex){failure=UiText.FormulaError(ex.Message);}
   if(failure.Length>0){var error=Theme.Label("Ошибка начальных значений: "+failure,9);error.ForeColor=Color.Salmon;error.MaximumSize=new Size(width,0);f.Controls.Add(error);}
   if(project.Variables.Count==0){var empty=Theme.Label("Переменных пока нет. Создайте целочисленное, дробное значение или список чисел.",10);empty.MaximumSize=new Size(width,0);f.Controls.Add(empty);}
   foreach(var item in project.Variables){
    var variable=item;
    var card=new TableLayoutPanel{Width=width,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,ColumnCount=1,BackColor=Theme.Input,Padding=new Padding(12),Margin=new Padding(0,6,0,6)};card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
    var name=Theme.Label(variable.Name,12,true);UiText.KeepLiteral(name,variable.Name);name.Dock=DockStyle.Fill;card.Controls.Add(name);
    var detail=Theme.Label(VariableTools.TypeLabel(variable.Type)+" · "+(variable.Scope=="global"?"Общая на весь матч":"Отдельная у каждого объекта"),9);detail.Dock=DockStyle.Fill;card.Controls.Add(detail);
    object resolved;string value=values!=null&&values.TryGetValue(variable.Name,out resolved)?VariableTools.FormatValue(resolved):"—";
    var initial=Theme.Label("Начальное значение: "+CompactVariableText(value),9);initial.Dock=DockStyle.Fill;initial.ForeColor=Theme.Accent;card.Controls.Add(initial);
    string expression=String.IsNullOrWhiteSpace(variable.Expression)?variable.Type=="list"?"[]":variable.Initial.ToString("R",CultureInfo.InvariantCulture):variable.Expression;
    var formula=Theme.Label("Формула: "+CompactVariableText(expression),9);formula.Dock=DockStyle.Fill;formula.ForeColor=Theme.Muted;card.Controls.Add(formula);
    var buttons=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill,WrapContents=true,Margin=Padding.Empty};buttons.Controls.Add(Theme.Button("Редактировать",()=>VariableDialog(variable)));buttons.Controls.Add(Theme.Button("Удалить",()=>RemoveVariable(variable)));card.Controls.Add(buttons);
    card.SizeChanged+=(s,e)=>{int wrap=Math.Max(100,card.ClientSize.Width-card.Padding.Horizontal-8);foreach(Control control in card.Controls)if(control is Label)((Label)control).MaximumSize=new Size(wrap,0);};
    foreach(Control control in card.Controls)if(control is Label)((Label)control).MaximumSize=new Size(Math.Max(100,width-32),0);f.Controls.Add(card);
   }
  }

  Project VariableProjectCopy(){
   // Variable edits never copy large imported images or alter the open project before Save.
   var copy=new Project();copy.Entities=project.Entities;
   copy.Variables=project.Variables.Select(v=>new Variable{Name=v.Name,Scope=v.Scope,Type=v.Type,Initial=v.Initial,Expression=v.Expression}).ToList();
   copy.Chains=Storage.Json().Deserialize<List<Chain>>(Storage.Json().Serialize(project.Chains));return copy;
  }
  void VariableDialog(Variable original){
   string fresh="counter";int suffix=1;while(project.Variables.Any(v=>v.Name==fresh))fresh="counter"+(suffix++);
   using(var dialog=new UiForm{Text=original==null?"Новая переменная":"Редактирование переменной",ClientSize=new Size(620,690),MinimumSize=new Size(550,570),BackColor=Theme.Card,ForeColor=Theme.Text,Font=Font,StartPosition=FormStartPosition.CenterParent,MinimizeBox=false,MaximizeBox=false}){
    var body=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Padding=Padding.Empty,Margin=Padding.Empty,BackColor=Theme.Card};body.RowStyles.Add(new RowStyle(SizeType.Percent,100));body.RowStyles.Add(new RowStyle(SizeType.AutoSize));dialog.Controls.Add(body);
    var fields=Theme.Flow(true);body.Controls.Add(fields,0,0);
    fields.Controls.Add(Theme.Label(original==null?"Новая переменная":"Редактирование переменной",18,true));
    fields.Controls.Add(Theme.Label("Имя · латиница, цифры и _",9,true));var name=Theme.TextBox(original==null?fresh:original.Name);fields.Controls.Add(name);
    fields.Controls.Add(Theme.Label("Тип значения",9,true));var type=Theme.Combo();type.Items.AddRange(new[]{new Choice("int","Целочисленное"),new Choice("float","Дробное"),new Choice("list","Список")});type.SelectedItem=type.Items.Cast<Choice>().FirstOrDefault(x=>x.Key==(original==null?"float":original.Type));if(type.SelectedIndex<0)type.SelectedIndex=1;fields.Controls.Add(type);
    fields.Controls.Add(Theme.Label("Область",9,true));var scope=Theme.Combo();scope.Items.AddRange(new[]{new Choice("object","Отдельная у каждого объекта"),new Choice("global","Общая на весь матч")});scope.SelectedIndex=original!=null&&original.Scope=="global"?1:0;fields.Controls.Add(scope);
    fields.Controls.Add(Theme.Label("Формула начального значения",9,true));var formula=Theme.TextBox(original==null?"0":String.IsNullOrWhiteSpace(original.Expression)?original.Type=="list"?"[]":original.Initial.ToString("R",CultureInfo.InvariantCulture):original.Expression);formula.Multiline=true;formula.Height=100;formula.ScrollBars=ScrollBars.Vertical;formula.Font=Theme.TextFont(10);fields.Controls.Add(formula);
    var typeHelp=Theme.Label("",9);typeHelp.ForeColor=Theme.Muted;fields.Controls.Add(typeHelp);
    var preview=Theme.Label("",10,true);preview.ForeColor=Theme.Accent;fields.Controls.Add(preview);
    var error=Theme.Label("",9);error.ForeColor=Color.Salmon;fields.Controls.Add(error);
    var reference=Theme.Label("Формула может ссылаться на другие переменные: $counter. Переименование обновляет ссылки во всём проекте. Начальные формулы рассчитываются при подготовке мода.",9);reference.ForeColor=Theme.Muted;fields.Controls.Add(reference);
    var footer=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill,WrapContents=true,Padding=new Padding(16,8,16,12),BackColor=Theme.Card};body.Controls.Add(footer,0,1);
    Func<Project> draft=()=>{
     string key=name.Text.Trim();VariableTools.ValidateName(key);
     if(project.Variables.Any(v=>!Object.ReferenceEquals(v,original)&&v.Name==key))throw new InvalidOperationException("Переменная с таким именем уже существует.");
     var candidate=VariableProjectCopy();Variable item;
     if(original==null){item=new Variable{Name=key};candidate.Variables.Add(item);}else{item=candidate.Variables[project.Variables.IndexOf(original)];if(item.Name!=key)VariableTools.Rename(candidate,item,key);}
     item.Type=((Choice)type.SelectedItem).Key;item.Scope=((Choice)scope.SelectedItem).Key;item.Expression=formula.Text.Trim();return candidate;
    };
    Action update=()=>{
     string key=((Choice)type.SelectedItem).Key;
     typeHelp.Text=key=="int"?"Целочисленное: 32 бита; дробная часть результата отбрасывается к нулю.":key=="list"?"Список чисел: [1, 2, 3] или range(1, 10). Индексы начинаются с 1. Пустая формула задаёт [].":"Дробное: конечное число двойной точности. В формулах используйте точку: 1.5.";
     try{var candidate=draft();string keyName=name.Text.Trim();var values=VariableTools.ResolveInitial(candidate,keyName);preview.Text="Начальное значение: "+CompactVariableText(VariableTools.FormatValue(values[keyName]));error.Text="";}catch(Exception ex){preview.Text="";error.Text=UiText.FormulaError(ex.Message);}
    };
    var save=Theme.Button("Сохранить",()=>{
     try{
      var candidate=draft();VariableTools.ResolveInitial(candidate,name.Text.Trim());var changed=candidate.Variables[original==null?candidate.Variables.Count-1:project.Variables.IndexOf(original)];
      if(original==null)project.Variables.Add(changed);else{if(original.Name!=changed.Name)VariableTools.Rename(project,original,changed.Name);original.Type=changed.Type;original.Scope=changed.Scope;original.Expression=changed.Expression;original.Initial=changed.Initial;}
      Mark();dialog.DialogResult=DialogResult.OK;dialog.Close();
     }catch(Exception ex){error.Text=UiText.FormulaError(ex.Message);preview.Text="";}
    },true);footer.Controls.Add(save);var cancel=Theme.Button("Отмена",()=>{dialog.DialogResult=DialogResult.Cancel;dialog.Close();});cancel.DialogResult=DialogResult.Cancel;footer.Controls.Add(cancel);footer.Controls.Add(Theme.Button("Гайд по формулам",ShowFormulaGuide));dialog.AcceptButton=save;dialog.CancelButton=cancel;
    using(var refresh=new Timer{Interval=250}){
     refresh.Tick+=(s,e)=>{refresh.Stop();update();};Action schedule=()=>{refresh.Stop();refresh.Start();};name.TextChanged+=(s,e)=>schedule();formula.TextChanged+=(s,e)=>schedule();type.SelectedIndexChanged+=(s,e)=>schedule();scope.SelectedIndexChanged+=(s,e)=>schedule();
     Action size=()=>{int width=Math.Max(260,fields.ClientSize.Width-fields.Padding.Horizontal-SystemInformation.VerticalScrollBarWidth-8);foreach(Control control in fields.Controls){if(control is Label)((Label)control).MaximumSize=new Size(width,0);else control.Width=width;}};fields.SizeChanged+=(s,e)=>size();size();update();dialog.ShowDialog(this);
    }
   }
   if(chain!=null)RenderChain();ShowPalette();
  }
  void RemoveVariable(Variable variable){
   var references=VariableTools.References(project,variable.Name);
   if(references.Count>0){UiDialogs.Show("Переменная используется. Сначала измените или удалите эти ссылки:\n\n"+String.Join("\n",references.Take(14))+(references.Count>14?"\n…":""),"Удаление переменной",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
   if(UiDialogs.Show("Удалить переменную "+variable.Name+"?","Удаление переменной",MessageBoxButtons.OKCancel,MessageBoxIcon.Question)!=DialogResult.OK)return;
   project.Variables.Remove(variable);Mark();if(chain!=null)RenderChain();ShowPalette();
  }
  void DrawVariableAction(FlowLayoutPanel card,Block block,int width){
   var variable=project.Variables.FirstOrDefault(v=>v.Name==block.Value);bool list=variable!=null&&variable.Type=="list";
   var choices=list?new[]{new Choice("set","Заменить весь список"),new Choice("append","Добавить элемент в конец"),new Choice("set_item","Изменить элемент по индексу"),new Choice("remove_item","Удалить элемент по индексу"),new Choice("clear","Очистить список")}:new[]{new Choice("set","Задать значение"),new Choice("add","Прибавить значение"),new Choice("multiply","Умножить на значение")};
   if(!choices.Any(x=>x.Key==block.Operation)){var warning=Theme.Label("Операция не подходит типу переменной. Выберите другую операцию.",9);warning.ForeColor=Color.Salmon;card.Controls.Add(warning);}
   SelectBox(card,"Операция",choices,block.Operation,v=>{block.Operation=v;RenderChain();},width);
   if(block.Operation=="set_item"||block.Operation=="remove_item")BlockFormulaField(card,"Индекс элемента · начиная с 1",block.IndexExpression,v=>block.IndexExpression=v,width,"Пусто: индекс 1.");
   if(block.Operation!="clear"&&block.Operation!="remove_item")BlockFormulaField(card,list&&block.Operation=="set"?"Формула списка":"Формула значения",!String.IsNullOrEmpty(block.Expression)?block.Expression:!String.IsNullOrWhiteSpace(block.Text)?block.Text:block.Number.ToString("R",CultureInfo.InvariantCulture),v=>{block.Expression=v;block.Text="";},width,null,list&&block.Operation=="set");
   if(variable==null){var missing=Theme.Label("Выберите существующую переменную.",9);missing.ForeColor=Color.Salmon;card.Controls.Add(missing);}
  }
  void BlockFormulaField(FlowLayoutPanel host,string label,string expression,Action<string> set,int width,string emptyHint=null,bool expectsList=false){
   host.Controls.Add(Theme.Label(label+" · формула",9,true));var input=Theme.TextBox(expression);input.Width=width;input.Font=Theme.TextFont(10);host.Controls.Add(input);
   var result=Theme.Label("",8);result.MaximumSize=new Size(width,0);host.Controls.Add(result);
   Action update=()=>{
    if(String.IsNullOrWhiteSpace(input.Text)){result.Text=emptyHint??"Пусто: используется прежнее числовое значение блока.";result.ForeColor=Theme.Muted;return;}
    try{
     var formula=Formula.Parse(input.Text);VariableTools.RequireFormulaType(project,formula,expectsList);var references=formula.References.ToArray();var unknown=references.FirstOrDefault(x=>!new[]{"speed","hp","damage","repeat_index"}.Contains(x)&&!project.Variables.Any(v=>v.Name==x));if(unknown!=null)throw new InvalidOperationException("Не найдена переменная: "+unknown);
     if(references.Any(x=>new[]{"speed","hp","damage","repeat_index"}.Contains(x))){result.Text="Вычисляется в Forts при выполнении блока.";result.ForeColor=Theme.Accent;return;}
     var values=VariableTools.ResolveInitial(project);var value=formula.Evaluate(key=>values[key]);result.Text="При начальных значениях: "+CompactVariableText(VariableTools.FormatValue(value));result.ForeColor=Theme.Accent;
    }catch(Exception ex){result.Text="Ошибка формулы: "+UiText.FormulaError(ex.Message);result.ForeColor=Color.Salmon;}
   };
   var refresh=new Timer{Interval=250};refresh.Tick+=(s,e)=>{refresh.Stop();update();};input.Disposed+=(s,e)=>refresh.Dispose();input.TextChanged+=(s,e)=>{set(input.Text);Mark();refresh.Stop();refresh.Start();};update();
  }
 }
}
