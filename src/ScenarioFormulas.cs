using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FortsStudio {
 public static partial class Exporter {
  static string FormulaNumber(double value){return value.ToString("R",CultureInfo.InvariantCulture);}
  static string FormulaValue(object value){var list=value as List<double>;return list==null?FormulaNumber(Convert.ToDouble(value,CultureInfo.InvariantCulture)):"{ "+String.Join(", ",list.Select(FormulaNumber))+" }";}
  static bool EditHasValue(Block b){return b.Type!="edit_var"||b.Operation!="clear"&&b.Operation!="remove_item";}
  static string FormulaRuntime(Project p){
   var initial=VariableTools.ResolveInitial(p);
   var s=new StringBuilder("dofile('scripts/forts.lua')\nlocal fms_path = path\nlocal fms_folder = "+Q(p.Folder)+"\nlocal fms_default_weapon = "+Q(p.Entities.Where(e=>e.Kind=="weapons").Select(e=>e.Key).FirstOrDefault()??"cannon")+"\nlocal fms_blocks = {\n");
   foreach(var c in p.Chains.Where(c=>c.Enabled&&c.Event!="native_impact"))foreach(var b in AllBlocks(c.Blocks)){
    s.Append("["+Q(b.Id)+"] = { kind = "+Q(b.Kind)+", type = "+Q(b.Type)+", value = "+Q(b.Value)+", text = "+Q(b.Text)+", op = "+Q(b.Operator)+", tag = "+Q(b.Tag)+", operation = "+Q(b.Operation??"set")+", number = "+FormulaNumber(b.Number)+", interval = "+N(b.Number2)+", children = "+Ids(b.Children)+", otherwise = "+Ids(b.Otherwise));
    if(VariableTools.UsesNumber(b)&&EditHasValue(b)){
     s.Append(", numeric = true");string expression=VariableTools.ExpressionFor(b);
     if(!String.IsNullOrWhiteSpace(expression))s.Append(", formula = "+Formula.Parse(expression).ToLua());
    }
    if(b.Type=="edit_var"&&(b.Operation=="set_item"||b.Operation=="remove_item"))s.Append(", indexFormula = "+Formula.Parse(String.IsNullOrWhiteSpace(b.IndexExpression)?"1":b.IndexExpression).ToLua());
    if(b.Type=="sound"||b.Type=="loop_sound"){var clip=p.Sounds.FirstOrDefault(x=>x.Id==b.Value);if(clip!=null)s.Append(", sound = fms_path .. "+Q("/effects/block_"+b.Id+".lua")+", duration = "+N(clip.Duration));}
    s.Append(" },\n");
   }
   s.Append("}\nlocal fms_rules = {\n");
   foreach(var c in p.Chains.Where(c=>c.Enabled&&c.Event!="native_impact"))s.Append("{ event = "+Q(c.Event)+", source = "+Q(c.Source)+", interval = "+N(c.Interval)+", body = "+Ids(c.Blocks)+" },\n");
   s.Append("}\nlocal fms_variables = {\n");
   foreach(var v in p.Variables)s.Append("["+Q(v.Name)+"] = { scope = "+Q(v.Scope)+", type = "+Q(v.Type)+", initial = "+FormulaValue(initial[v.Name])+" },\n");
   s.Append("}\nlocal fms_entity_audio = {\n");
   foreach(var e in p.Entities){if(e.WorkAudio.Data.Length>0)s.Append("["+Q(e.Key)+"] = { effect = fms_path .. "+Q("/effects/"+e.Key+"_work.lua")+", duration = "+N(e.WorkAudio.Duration)+", slot = 'work' },\n");if(e.FlightAudio.Data.Length>0)s.Append("["+Q(e.Key)+"] = { effect = fms_path .. "+Q("/effects/"+e.Key+"_flight.lua")+", duration = "+N(e.FlightAudio.Duration)+", slot = 'flight' },\n");}
   s.Append("}\n");
   foreach(string resource in new[]{"FortsStudio.formula_runtime.lua","FortsStudio.runtime.lua"})using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)){
    if(stream==null)throw new Exception("Не найден встроенный исполнитель сценариев.");
    using(var reader=new StreamReader(stream))s.Append(reader.ReadToEnd()).Append('\n');
   }
   return s.ToString();
  }
  static void ValidateVariablesAndFormulas(Project p,List<string> errors){
   try{VariableTools.ResolveInitial(p);}catch(Exception ex){errors.Add("Начальные значения переменных: "+UiText.FormulaError(ex.Message));}
   foreach(var c in p.Chains)foreach(var b in AllBlocks(c.Blocks)){
    var variable=VariableTools.IsTarget(b)?p.Variables.FirstOrDefault(v=>v!=null&&v.Name==b.Value):null;
    if(VariableTools.IsTarget(b)&&variable==null)errors.Add(c.Name+": не объявлена переменная "+b.Value);
    bool isList=variable!=null&&variable.Type=="list";
    if((b.Type=="variable"||b.Kind=="loop"&&b.Type=="while")&&isList&&b.Operator!="=="&&b.Operator!="~=")errors.Add(c.Name+": списки можно сравнивать только через == или ~=.");
    if(isList&&(b.Type=="add_var"||b.Type=="multiply_var"))errors.Add(c.Name+": для списка используйте действие «Редактировать переменную».");
    if(b.Type=="edit_var"){
     string[] allowed=isList?new[]{"set","append","set_item","remove_item","clear"}:new[]{"set","add","multiply"};
     if(!allowed.Contains(b.Operation))errors.Add(c.Name+": операция не соответствует типу переменной.");
    }
    if(c.Event=="native_impact"&&(VariableTools.IsTarget(b)||VariableTools.UsesValue(b)&&!String.IsNullOrWhiteSpace(b.Expression)))errors.Add(c.Name+": формулы и переменные требуют обычного события, а не штатных осколков.");
    if(VariableTools.UsesNumber(b)&&EditHasValue(b)){
     string expression=VariableTools.ExpressionFor(b);
     bool listValue=isList&&(b.Type=="set_var"||b.Type=="variable"||b.Kind=="loop"&&b.Type=="while"||b.Type=="edit_var"&&b.Operation=="set");
     if(listValue&&String.IsNullOrWhiteSpace(expression))errors.Add(c.Name+": задайте формулу списка, например [1, 2, 3].");
     CheckFormula(p,expression,c.Name,listValue,errors);
    }
    if(b.Type=="edit_var"&&(b.Operation=="set_item"||b.Operation=="remove_item"))CheckFormula(p,String.IsNullOrWhiteSpace(b.IndexExpression)?"1":b.IndexExpression,c.Name,false,errors,true);
   }
  }
  static void CheckFormula(Project p,string expression,string context,bool expectsList,List<string> errors,bool isIndex=false){
   if(String.IsNullOrWhiteSpace(expression))return;
   try{
    var node=Formula.Parse(expression);var references=node.References.ToArray();
    foreach(string name in references)if(!VariableTools.ContextNames.Contains(name)&&!p.Variables.Any(v=>v!=null&&v.Name==name))throw new ArgumentException("Не объявлена переменная: "+name);
    VariableTools.RequireFormulaType(p,node,expectsList);
    // Only constant expressions can be evaluated ahead of a match. Event values
    // and variables may change before a particular action is executed.
    if(references.Length==0){object result=node.Evaluate(name=>{throw new ArgumentException(name);});if(expectsList)VariableTools.Coerce("list",result);else{double number=(double)VariableTools.Coerce("float",result);if(isIndex&&(number<1||number>4096||number!=Math.Floor(number)))throw new ArgumentException("Индекс должен быть целым числом от 1 до 4096.");}}
   }catch(Exception ex){errors.Add(context+": формула: "+UiText.FormulaError(ex.Message));}
  }
 }
}
