using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FortsStudio {
 // A small expression language. Its Lua export is an AST literal, never executable input.
 public static class Formula {
  public const int MaxLength=4096, MaxDepth=32, MaxListLength=4096, MaxWork=100000;
  public static FormulaNode Parse(string text) { return new FormulaParser(text).Parse(); }
  public static object Evaluate(string text,Func<string,object> resolve) { return Parse(text).Evaluate(resolve); }
  public static string RenameReferences(string text,string oldName,string newName) {
   if(!ValidName(oldName)||!ValidName(newName)) throw new ArgumentException("Invalid variable name.");
   FormulaNode node=Parse(text);var names=new HashSet<string>(StringComparer.Ordinal);CollectNames(node,names);names.Add(newName);
   string result=Rename(node,oldName,newName,new HashSet<string>(StringComparer.Ordinal),names).ToExpression();
   if(result.Length>MaxLength)throw new FormatException("Renamed formula exceeds 4096 characters.");
   return result;
  }
  internal static bool ValidName(string s) {
   if(String.IsNullOrEmpty(s)||!(Char.IsLetter(s[0])||s[0]=='_')) return false;
   for(int i=1;i<s.Length;i++) if(!(Char.IsLetterOrDigit(s[i])||s[i]=='_')) return false;
   return true;
  }
  static void CollectNames(FormulaNode n,HashSet<string> names) { if(n.Kind=="v")names.Add(n.Text);foreach(var a in n.Args)CollectNames(a,names); }
  static FormulaNode Rename(FormulaNode n,string oldName,string newName,HashSet<string> bound,HashSet<string> names) {
   if(n.Kind=="v")return n.Text==oldName&&!bound.Contains(oldName)?FormulaNode.Variable(newName):n;
   var args=new List<FormulaNode>(n.Args);
   if(n.IsCalculus) {
    string binder=args[1].Text;
    var inner=new HashSet<string>(bound,StringComparer.Ordinal);inner.Add(binder);
    var free=new List<string>();args[0].CollectReferences(inner,free,new HashSet<string>(StringComparer.Ordinal));
    if(binder==newName&&free.Contains(oldName)) {
     int suffix=1;string fresh;do{fresh="fms_local_"+(suffix++).ToString(CultureInfo.InvariantCulture);}while(names.Contains(fresh));names.Add(fresh);
     args[0]=RenameBound(args[0],binder,fresh);binder=fresh;args[1]=FormulaNode.Variable(fresh);
    }
    inner=new HashSet<string>(bound,StringComparer.Ordinal);inner.Add(binder);
    args[0]=Rename(args[0],oldName,newName,inner,names);
    for(int i=2;i<args.Count;i++)args[i]=Rename(args[i],oldName,newName,bound,names);
   } else for(int i=0;i<args.Count;i++)args[i]=Rename(args[i],oldName,newName,bound,names);
   return new FormulaNode(n.Kind,n.Text,n.Number,args.ToArray());
  }
  // Rename one lexical binding while leaving bindings that shadow it intact.
  static FormulaNode RenameBound(FormulaNode n,string oldName,string newName) {
   if(n.Kind=="v")return n.Text==oldName?FormulaNode.Variable(newName):n;
   var args=new List<FormulaNode>(n.Args);
   for(int i=0;i<args.Count;i++) {
    if(n.IsCalculus&&(i==1||(i==0&&args[1].Text==oldName)))continue;
    args[i]=RenameBound(args[i],oldName,newName);
   }
   return new FormulaNode(n.Kind,n.Text,n.Number,args.ToArray());
  }
 }

 public sealed class FormulaNode {
  internal readonly string Kind,Text;internal readonly double Number;internal readonly FormulaNode[] Args;internal readonly int Depth;
  internal bool IsCalculus { get{return Kind=="f"&&(Text=="diff"||Text=="integrate");} }
  internal FormulaNode(string kind,string text,double number,params FormulaNode[] args) {
   Kind=kind;Text=text;Number=number;Args=args??new FormulaNode[0];Depth=1;
   foreach(var a in Args)Depth=Math.Max(Depth,a.Depth+1);
   if(Depth>Formula.MaxDepth)throw new FormatException("Formula nesting exceeds 32 levels.");
  }
  internal static FormulaNode Variable(string name) {return new FormulaNode("v",name,0);}
  public IEnumerable<string> References {
   get {var result=new List<string>();CollectReferences(new HashSet<string>(StringComparer.Ordinal),result,new HashSet<string>(StringComparer.Ordinal));return result.AsReadOnly();}
  }
  internal void CollectReferences(HashSet<string> bound,List<string> result,HashSet<string> seen) {
   if(Kind=="v") {if(!bound.Contains(Text)&&seen.Add(Text))result.Add(Text);return;}
   for(int i=0;i<Args.Length;i++) {
    if(IsCalculus&&i==1)continue;
    if(IsCalculus&&i==0){var inner=new HashSet<string>(bound,StringComparer.Ordinal);inner.Add(Args[1].Text);Args[i].CollectReferences(inner,result,seen);}
    else Args[i].CollectReferences(bound,result,seen);
   }
  }
  public object Evaluate(Func<string,object> resolve) {return Eval(new FormulaContext(resolve),null);}
  internal object Eval(FormulaContext c,FormulaBinding binding) {
   c.Tick();
   if(Kind=="n")return Number;
   if(Kind=="v") {
    for(var b=binding;b!=null;b=b.Parent)if(b.Name==Text)return b.Value;
    if(c.Resolve==null)throw new InvalidOperationException("Unknown variable: "+Text);
    return c.Normalize(c.Resolve(Text),Text);
   }
   if(Kind=="l") {var list=new List<double>();foreach(var a in Args)list.Add(c.Num(a.Eval(c,binding)));return list;}
   if(Kind=="u") {double x=c.Num(Args[0].Eval(c,binding));return c.Finite(Text=="-"?-x:x);}
   if(Kind=="b") {
    double a=c.Num(Args[0].Eval(c,binding)),b=c.Num(Args[1].Eval(c,binding)),r;
    if(Text=="+")r=a+b;else if(Text=="-")r=a-b;else if(Text=="*")r=a*b;
    else if(Text=="/"){if(b==0)throw new InvalidOperationException("Division by zero.");r=a/b;}
    else if(Text=="%") {if(b==0)throw new InvalidOperationException("Division by zero.");r=a-c.Finite(Math.Floor(c.Finite(a/b)))*b;}
    else r=Math.Pow(a,b);
    return c.Finite(r);
   }
   if(IsCalculus) {
    double p=c.Num(Args[2].Eval(c,binding));
    if(Text=="diff") {
     double h=1e-5*Math.Max(1,Math.Abs(p));
     double right=c.Num(Args[0].Eval(c,new FormulaBinding(Args[1].Text,c.Finite(p+h),binding)));
     double left=c.Num(Args[0].Eval(c,new FormulaBinding(Args[1].Text,c.Finite(p-h),binding)));
     return c.Finite((right-left)/(2*h));
    }
    double q=c.Num(Args[3].Eval(c,binding));int steps=64;
    if(Args.Length==5) {double s=c.Num(Args[4].Eval(c,binding));if(s!=Math.Floor(s)||s<2||s>256||s%2!=0)throw new InvalidOperationException("Integral steps must be an even integer from 2 to 256.");steps=(int)s;}
    double width=c.Finite(q-p),step=c.Finite(width/steps),total=0;
    for(int i=0;i<=steps;i++) {
     double x=i==steps?q:c.Finite(p+i*step),y=c.Num(Args[0].Eval(c,new FormulaBinding(Args[1].Text,x,binding)));
     total=c.Finite(total+(i==0||i==steps?1:(i%2==0?2:4))*y);
    }
    return c.Finite(total*step/3);
   }
   var values=new object[Args.Length];for(int i=0;i<Args.Length;i++)values[i]=Args[i].Eval(c,binding);
   return Call(c,Text,values);
  }
  static object Call(FormulaContext c,string name,object[] v) {
   if(name=="len"||name=="sum"||name=="mean"||(name=="min"||name=="max")&&v.Length==1) {
    List<double> list=c.List(v[0]);if(name=="len")return (double)list.Count;
    if(list.Count==0&&name!="sum")throw new InvalidOperationException(name+" requires a nonempty list.");
    double aggregate=name=="min"?Double.PositiveInfinity:name=="max"?Double.NegativeInfinity:0;
    foreach(double item in list){c.Tick();if(name=="min")aggregate=Math.Min(aggregate,item);else if(name=="max")aggregate=Math.Max(aggregate,item);else aggregate=c.Finite(aggregate+item);}
    if(name=="mean")aggregate/=list.Count;return c.Finite(aggregate);
   }
   if(name=="at") {List<double> list=c.List(v[0]);double index=c.Num(v[1]);if(index!=Math.Floor(index)||index<1||index>list.Count)throw new InvalidOperationException("List index must be an integer from 1 to its length.");return list[(int)index-1];}
   var a=new double[v.Length];for(int i=0;i<v.Length;i++)a[i]=c.Num(v[i]);
   double x=a[0],y=a.Length>1?a[1]:0,r;
   if(name=="range") {
    double step=a.Length==3?a[2]:1;if(step==0)throw new InvalidOperationException("Range step cannot be zero.");
    var list=new List<double>();if(step>0&&y<x||step<0&&y>x)return list;
    double count=Math.Floor(c.Finite(c.Finite(y-x)/step))+1;if(count>Formula.MaxListLength)throw new InvalidOperationException("List exceeds 4096 items.");
    for(int i=0;i<(int)count;i++){c.Tick();list.Add(c.Finite(x+i*step));}return list;
   }
   if(name=="min"||name=="max") {r=x;for(int i=1;i<a.Length;i++){c.Tick();r=name=="min"?Math.Min(r,a[i]):Math.Max(r,a[i]);}return r;}
   if(name=="abs")r=Math.Abs(x);else if(name=="sqrt")r=Math.Sqrt(x);else if(name=="cbrt")r=Math.Sign(x)*Math.Pow(Math.Abs(x),1.0/3);
   else if(name=="exp")r=Math.Exp(x);else if(name=="ln"||name=="log") {if(x<=0||a.Length==2&&(y<=0||y==1))throw new InvalidOperationException("Invalid logarithm domain.");r=a.Length==2?Math.Log(x)/Math.Log(y):Math.Log(x);}
   else if(name=="log10")r=Math.Log10(x);else if(name=="pow")r=Math.Pow(x,y);
   else if(name=="floor")r=Math.Floor(x);else if(name=="ceil")r=Math.Ceiling(x);else if(name=="round")r=x>=0?Math.Floor(x+.5):Math.Ceiling(x-.5);else if(name=="sign")r=Math.Sign(x);
   else if(name=="clamp") {if(y>a[2])throw new InvalidOperationException("Clamp minimum exceeds maximum.");r=Math.Max(y,Math.Min(a[2],x));}
   else if(name=="lerp")r=x+(y-x)*a[2];
   else if(name=="sin")r=Math.Sin(x);else if(name=="cos")r=Math.Cos(x);else if(name=="tan")r=Math.Tan(x);
   else if(name=="asin")r=Math.Asin(x);else if(name=="acos")r=Math.Acos(x);else if(name=="atan")r=Math.Atan(x);else if(name=="atan2")r=x==0&&y==0?0:Math.Atan2(x==0?0:x,y);
   else if(name=="sinh")r=(c.Finite(Math.Exp(x))-c.Finite(Math.Exp(-x)))/2;else if(name=="cosh")r=(c.Finite(Math.Exp(x))+c.Finite(Math.Exp(-x)))/2;
   else if(name=="tanh") {double z=Math.Exp(-2*Math.Abs(x));r=(x<0?-1:1)*(1-z)/(1+z);}
   else if(name=="deg")r=x*180/Math.PI;else if(name=="rad")r=x*Math.PI/180;
   else throw new InvalidOperationException("Unknown function: "+name);
   return c.Finite(r);
  }
  public string ToLua() {
   var s=new StringBuilder("{k=");s.Append(Quote(Kind));
   if(Kind=="n")s.Append(",v=").Append(Number.ToString("R",CultureInfo.InvariantCulture));
   else if(Text!=null)s.Append(",v=").Append(Quote(Text));
   if(Args.Length>0){s.Append(",a={");for(int i=0;i<Args.Length;i++){if(i>0)s.Append(',');s.Append(Args[i].ToLua());}s.Append('}');}return s.Append('}').ToString();
  }
  internal string ToExpression() {
   if(Kind=="n")return Number.ToString("R",CultureInfo.InvariantCulture);if(Kind=="v")return "$"+Text;
   if(Kind=="u")return Text+Args[0].ToExpression();
   if(Kind=="b"){
    string left=Args[0].ToExpression();if(Text=="^"&&Args[0].Kind=="u")left="("+left+")";
    return "("+left+Text+Args[1].ToExpression()+")";
   }
   var s=new StringBuilder(Kind=="l"?"[":Text+"(");for(int i=0;i<Args.Length;i++){if(i>0)s.Append(',');s.Append(Args[i].ToExpression());}return s.Append(Kind=="l"?"]":")").ToString();
  }
  static string Quote(string s) {return "'"+s.Replace("\\","\\\\").Replace("'","\\'").Replace("\r","\\r").Replace("\n","\\n")+"'";}
 }

 sealed class FormulaBinding {
  internal readonly string Name;internal readonly double Value;internal readonly FormulaBinding Parent;
  internal FormulaBinding(string name,double value,FormulaBinding parent){Name=name;Value=value;Parent=parent;}
 }
 sealed class FormulaContext {
  internal readonly Func<string,object> Resolve;int work;
  internal FormulaContext(Func<string,object> resolve){Resolve=resolve;}
  internal void Tick(){if(++work>Formula.MaxWork)throw new InvalidOperationException("Formula work limit exceeded (100000).");}
  internal double Finite(double n){if(Double.IsNaN(n)||Double.IsInfinity(n))throw new InvalidOperationException("Formula result is not finite or is outside a function domain.");return n;}
  internal double Num(object o){if(!(o is double))throw new InvalidOperationException("A numeric value is required; a list cannot be used here.");return Finite((double)o);}
  internal List<double> List(object o){var list=o as List<double>;if(list==null)throw new InvalidOperationException("A list is required.");return list;}
  internal object Normalize(object o,string name) {
   if(o==null)throw new InvalidOperationException("Unknown variable: "+name);
   var source=o as IList<double>;if(source!=null){if(source.Count>Formula.MaxListLength)throw new InvalidOperationException("List exceeds 4096 items.");var copy=new List<double>(source.Count);foreach(double n in source){Tick();copy.Add(Finite(n));}return copy;}
   if(o is double||o is float||o is int||o is long||o is short||o is byte||o is decimal||o is uint||o is ulong||o is ushort||o is sbyte)return Finite(Convert.ToDouble(o,CultureInfo.InvariantCulture));
   throw new InvalidOperationException("Variable "+name+" must contain a number or numeric list.");
  }
 }

 sealed class FormulaParser {
  readonly string text;int pos,nesting;
  internal FormulaParser(string input){if(String.IsNullOrWhiteSpace(input))throw new FormatException("Formula is empty.");if(input.Length>Formula.MaxLength)throw new FormatException("Formula exceeds 4096 characters.");text=input;}
  internal FormulaNode Parse(){FormulaNode n=Expression();Space();if(pos!=text.Length)Error("Unexpected character");return n;}
  void Space(){while(pos<text.Length&&Char.IsWhiteSpace(text[pos]))pos++;}
  bool Take(char c){Space();if(pos<text.Length&&text[pos]==c){pos++;return true;}return false;}
  void Need(char c){if(!Take(c))Error("Expected '"+c+"'");}
  void Error(string message){throw new FormatException(message+" at position "+(pos+1).ToString(CultureInfo.InvariantCulture)+".");}
  FormulaNode Expression(){FormulaNode n=Product();while(true){if(Take('+'))n=new FormulaNode("b","+",0,n,Product());else if(Take('-'))n=new FormulaNode("b","-",0,n,Product());else return n;}}
  FormulaNode Product(){FormulaNode n=Unary();while(true){if(Take('*'))n=new FormulaNode("b","*",0,n,Unary());else if(Take('/'))n=new FormulaNode("b","/",0,n,Unary());else if(Take('%'))n=new FormulaNode("b","%",0,n,Unary());else return n;}}
  FormulaNode Unary(){if(++nesting>Formula.MaxDepth)Error("Formula nesting exceeds 32 levels");try{if(Take('+'))return new FormulaNode("u","+",0,Unary());if(Take('-'))return new FormulaNode("u","-",0,Unary());return Power();}finally{nesting--;}}
  FormulaNode Power(){FormulaNode n=Primary();if(Take('^'))n=new FormulaNode("b","^",0,n,Unary());return n;}
  FormulaNode Primary() {
   Space();if(pos==text.Length){Error("Expected a value");return null;}
   if(Take('(')){FormulaNode n=Expression();Need(')');return n;}
   if(Take('[')){var items=new List<FormulaNode>();if(!Take(']')){do{items.Add(Expression());if(items.Count>Formula.MaxListLength)Error("List exceeds 4096 items");}while(Take(','));Need(']');}return new FormulaNode("l",null,0,items.ToArray());}
   Space();if(Char.IsDigit(text[pos])||text[pos]=='.')return Number();
   bool explicitVariable=Take('$');Space();int start=pos;
   if(pos>=text.Length||!(Char.IsLetter(text[pos])||text[pos]=='_')){Error("Expected a number, variable or function");return null;}
   pos++;while(pos<text.Length&&(Char.IsLetterOrDigit(text[pos])||text[pos]=='_'))pos++;
   string name=text.Substring(start,pos-start);
   if(!explicitVariable&&Take('(')) {
    var args=new List<FormulaNode>();if(!Take(')')){do{args.Add(Expression());}while(Take(','));Need(')');}
    ValidateFunction(name,args);return new FormulaNode("f",name,0,args.ToArray());
   }
   if(!explicitVariable&&(name=="pi"||name=="e"||name=="tau"))return new FormulaNode("n",null,name=="e"?Math.E:name=="tau"?2*Math.PI:Math.PI);
   return FormulaNode.Variable(name);
  }
  FormulaNode Number() {
   int start=pos;bool digits=false;while(pos<text.Length&&Char.IsDigit(text[pos])){pos++;digits=true;}
   if(pos<text.Length&&text[pos]=='.'){pos++;while(pos<text.Length&&Char.IsDigit(text[pos])){pos++;digits=true;}}
   if(!digits)Error("Invalid number");
   if(pos<text.Length&&(text[pos]=='e'||text[pos]=='E')){pos++;if(pos<text.Length&&(text[pos]=='+'||text[pos]=='-'))pos++;int exponent=pos;while(pos<text.Length&&Char.IsDigit(text[pos]))pos++;if(pos==exponent)Error("Invalid exponent");}
   double value;if(!Double.TryParse(text.Substring(start,pos-start),NumberStyles.Float,CultureInfo.InvariantCulture,out value)||Double.IsNaN(value)||Double.IsInfinity(value))Error("Number must be finite");
   return new FormulaNode("n",null,value);
  }
  void ValidateFunction(string name,List<FormulaNode> args) {
   int lo=1,hi=1;
   if(name=="diff"){lo=hi=3;}else if(name=="integrate"){lo=4;hi=5;}
   else if(name=="at"||name=="pow"||name=="atan2")lo=hi=2;
   else if(name=="clamp"||name=="lerp")lo=hi=3;
   else if(name=="log"){lo=1;hi=2;}else if(name=="range"){lo=2;hi=3;}else if(name=="min"||name=="max"){hi=Formula.MaxListLength;}
   else if(Array.IndexOf(new[]{"abs","sqrt","cbrt","exp","ln","log10","floor","ceil","round","sign","sin","cos","tan","asin","acos","atan","sinh","cosh","tanh","deg","rad","len","sum","mean"},name)<0)Error("Unknown function '"+name+"'");
   if(args.Count<lo||args.Count>hi)Error("Wrong number of arguments for '"+name+"'");
   if((name=="diff"||name=="integrate")&&args[1].Kind!="v")Error("Calculus second argument must be a local variable name (for example x)");
  }
 }
}
