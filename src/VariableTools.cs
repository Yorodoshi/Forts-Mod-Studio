using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace FortsStudio {
 public static class VariableTools {
  public const int MaximumListLength=4096;
  public static readonly string[] ContextNames={"speed","hp","damage","repeat_index"};
  public static string TypeLabel(string type){return type=="int"?"Целочисленное":type=="list"?"Список":"Дробное";}
  public static bool IsTarget(Block b){return b.Type=="variable"||b.Type=="set_var"||b.Type=="add_var"||b.Type=="multiply_var"||b.Type=="edit_var"||b.Kind=="loop"&&b.Type=="while";}
  public static bool UsesNumber(Block b){return b.Kind=="delay"||b.Kind=="loop"||new[]{"variable","hp","chance","set_var","add_var","multiply_var","edit_var","fire","damage","convert_projectile","spawn_projectile","set_projectile_hp","alpha"}.Contains(b.Type);}
  public static bool UsesValue(Block b){return UsesNumber(b)&&(b.Type!="edit_var"||b.Operation!="clear"&&b.Operation!="remove_item");}
  public static bool UsesIndex(Block b){return b.Type=="edit_var"&&(b.Operation=="set_item"||b.Operation=="remove_item");}
  public static string ExpressionFor(Block b){if(!String.IsNullOrWhiteSpace(b.Expression))return b.Expression;return UsesNumber(b)?b.Text??"":"";}
  public static IEnumerable<Block> Blocks(IEnumerable<Block> blocks){foreach(var b in blocks){yield return b;foreach(var child in Blocks(b.Children))yield return child;foreach(var child in Blocks(b.Otherwise))yield return child;}}
  public static void ValidateName(string name){
   if(!Regex.IsMatch(name??"",@"^[A-Za-z_][A-Za-z0-9_]{0,39}$"))throw new ArgumentException("Имя переменной: 1–40 латинских букв, цифр или _, первая — буква или _." );
   if(ContextNames.Contains(name)||name=="pi"||name=="e"||name=="tau")throw new ArgumentException("Это имя зарезервировано: "+name);
  }
  public static object Coerce(string type,object value){
   var list=value as List<double>;
   if(type=="list"){
    if(list==null)throw new ArgumentException("Ожидается список чисел, например [1, 2, 3].");
    if(list.Count>MaximumListLength)throw new ArgumentException("Максимум 4096 элементов списка.");
    foreach(double item in list)Finite(item);
    return new List<double>(list);
   }
   if(type!="int"&&type!="float")throw new ArgumentException("Неизвестный тип переменной: "+type);
   if(list!=null||value==null)throw new ArgumentException("Ожидается число, а не список.");
   double number=Convert.ToDouble(value,CultureInfo.InvariantCulture);Finite(number);
   if(type=="int"){
    number=Math.Truncate(number);
    if(number<Int32.MinValue||number>Int32.MaxValue)throw new ArgumentException("Целочисленное значение выходит за диапазон Int32.");
   }
   return number;
  }
  static void Finite(double number){if(Double.IsNaN(number)||Double.IsInfinity(number))throw new ArgumentException("Результат должен быть конечным числом.");}
  public static string FormatValue(object value){var list=value as List<double>;return list==null?Convert.ToDouble(value,CultureInfo.InvariantCulture).ToString("G12",CultureInfo.InvariantCulture):"["+String.Join(", ",list.Select(n=>n.ToString("G12",CultureInfo.InvariantCulture)))+"]";}
  public static Dictionary<string,object> ResolveInitial(Project project){return new InitialResolver(project).Resolve();}
  public static Dictionary<string,object> ResolveInitial(Project project,string name){return new InitialResolver(project).Resolve(name);}
  public static void RequireFormulaType(Project project,FormulaNode node,bool expectsList){
   bool actual=ListResult(project,node,new HashSet<string>(StringComparer.Ordinal));
   if(actual!=expectsList)throw new ArgumentException(expectsList?"Формула должна возвращать список чисел.":"Формула должна возвращать число, а не список.");
  }
  static bool ListResult(Project project,FormulaNode node,HashSet<string> bound){
   if(node.Kind=="n")return false;
   if(node.Kind=="v"){
    if(bound.Contains(node.Text)||ContextNames.Contains(node.Text))return false;
    var variable=project.Variables.FirstOrDefault(v=>v!=null&&v.Name==node.Text);
    if(variable==null)throw new ArgumentException("Не объявлена переменная: "+node.Text);
    return variable.Type=="list";
   }
   if(node.Kind=="l"){foreach(var a in node.Args)RequireNumber(project,a,bound);return true;}
   if(node.Kind=="u"||node.Kind=="b"){foreach(var a in node.Args)RequireNumber(project,a,bound);return false;}
   if(node.IsCalculus){
    var inner=new HashSet<string>(bound,StringComparer.Ordinal);inner.Add(node.Args[1].Text);
    RequireNumber(project,node.Args[0],inner);for(int i=2;i<node.Args.Length;i++)RequireNumber(project,node.Args[i],bound);return false;
   }
   bool consumesList=node.Text=="len"||node.Text=="sum"||node.Text=="mean"||node.Text=="at"||(node.Text=="min"||node.Text=="max")&&node.Args.Length==1;
   if(consumesList){
    if(!ListResult(project,node.Args[0],bound))throw new ArgumentException("Функция требует список чисел: "+node.Text);
    for(int i=1;i<node.Args.Length;i++)RequireNumber(project,node.Args[i],bound);return false;
   }
   foreach(var a in node.Args)RequireNumber(project,a,bound);
   return node.Text=="range";
  }
  static void RequireNumber(Project project,FormulaNode node,HashSet<string> bound){if(ListResult(project,node,bound))throw new ArgumentException("Здесь требуется число, а не список.");}
  sealed class InitialResolver {
   readonly Dictionary<string,Variable> definitions=new Dictionary<string,Variable>(StringComparer.Ordinal);
   readonly Dictionary<string,object> values=new Dictionary<string,object>(StringComparer.Ordinal);
   readonly HashSet<string> visiting=new HashSet<string>(StringComparer.Ordinal);
   public InitialResolver(Project project){
    if(project.Variables.Count>256)throw new ArgumentException("Максимум 256 переменных в проекте.");
    foreach(var v in project.Variables){
     if(v==null)throw new ArgumentException("Пустое объявление переменной.");
     ValidateName(v.Name);
     if(definitions.ContainsKey(v.Name))throw new ArgumentException("Повторное имя переменной: "+v.Name);
     if(v.Type!="int"&&v.Type!="float"&&v.Type!="list")throw new ArgumentException("Неизвестный тип переменной: "+v.Name);
     if(v.Scope!="object"&&v.Scope!="global")throw new ArgumentException("Неверная область переменной "+v.Name);
     definitions.Add(v.Name,v);
    }
   }
   public Dictionary<string,object> Resolve(){foreach(string name in definitions.Keys)Value(name);return values;}
   public Dictionary<string,object> Resolve(string name){Value(name);return values;}
   object Value(string name){
    object value;if(values.TryGetValue(name,out value))return value;
    Variable definition;
    if(!definitions.TryGetValue(name,out definition))throw new ArgumentException("Не объявлена переменная: "+name);
    if(!visiting.Add(name))throw new ArgumentException("Циклическая зависимость начальных значений: "+name);
    try {
     value=String.IsNullOrWhiteSpace(definition.Expression)?(definition.Type=="list"?(object)new List<double>():definition.Initial):Formula.Evaluate(definition.Expression,Value);
     value=Coerce(definition.Type,value);values.Add(name,value);return value;
    }catch(Exception ex){throw new ArgumentException(name+": "+ex.Message,ex);}finally{visiting.Remove(name);}
   }
  }
  static bool Mentions(string expression,string name){if(String.IsNullOrWhiteSpace(expression))return false;try{return Formula.Parse(expression).References.Contains(name);}catch(ArgumentException){}catch(FormatException){}return Regex.IsMatch(expression,@"(?<![A-Za-z0-9_])\$?"+Regex.Escape(name)+@"(?![A-Za-z0-9_])");}
  public static List<string> References(Project project,string name){
   var result=new List<string>();
   foreach(var v in project.Variables)if(v.Name!=name&&Mentions(v.Expression,name))result.Add(v.Name);
   foreach(var c in project.Chains)foreach(var b in Blocks(c.Blocks))if(IsTarget(b)&&b.Value==name||UsesValue(b)&&Mentions(ExpressionFor(b),name)||UsesIndex(b)&&Mentions(b.IndexExpression,name))result.Add(c.Name+" · "+b.Type);
   return result.Distinct().ToList();
  }
  public static void Rename(Project project,Variable variable,string name){
   ValidateName(name);
   if(project.Variables.Any(v=>v!=variable&&v.Name==name))throw new ArgumentException("Повторное имя переменной: "+name);
   string old=variable.Name;if(old==name)return;
   var updates=new List<Action>();
   foreach(var v in project.Variables){var target=v;string expression=Rewrite(v.Expression,old,name);updates.Add(()=>target.Expression=expression);}
   foreach(var c in project.Chains)foreach(var b in Blocks(c.Blocks)){
    var target=b;bool formulaActive=UsesValue(b)&&!String.IsNullOrWhiteSpace(b.Expression);string expression=formulaActive?Rewrite(b.Expression,old,name):b.Expression,index=UsesIndex(b)?Rewrite(b.IndexExpression,old,name):b.IndexExpression,legacy=UsesValue(b)&&!formulaActive?Rewrite(b.Text,old,name):b.Text;
    updates.Add(()=>{target.Expression=expression;target.IndexExpression=index;target.Text=legacy;if(IsTarget(target)&&target.Value==old)target.Value=name;});
   }
   foreach(var apply in updates)apply();variable.Name=name;
  }
  static string Rewrite(string expression,string old,string name){return String.IsNullOrWhiteSpace(expression)||!Mentions(expression,old)?expression??"":Formula.RenameReferences(expression,old,name);}
 }
}
