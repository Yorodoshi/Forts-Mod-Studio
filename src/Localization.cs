using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace FortsStudio {
 // UI language belongs to the application, never to the mod project.
 static partial class UiText {
  sealed class Caption {public string Source;public bool Applying;}
  static readonly Dictionary<string,string> translations=new Dictionary<string,string>(StringComparer.Ordinal);
  static readonly Dictionary<string,string> folded=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
  static readonly Dictionary<string,string> fragments=new Dictionary<string,string>(StringComparer.Ordinal);
  static readonly ConditionalWeakTable<Control,Caption> captions=new ConditionalWeakTable<Control,Caption>();
  static readonly List<WeakReference> controls=new List<WeakReference>();
  static readonly string settingsPath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FortsModStudio2","interface-language.txt");
  static readonly Regex fragmentPattern;
  static string language="ru";
  public static bool English {get{return language=="en";}}
  static UiText(){
   AddCoreTranslations(translations);AddEditorTranslations(translations);AddPreviewTranslations(translations);AddFormulaTranslations(translations);
   translations["1 клетка = {0} ед.  ·  {1}%  ·  Балок: {2}  ·  Объектов: {3}"]="1 cell = {0} units  ·  {1}%  ·  Beams: {2}  ·  Objects: {3}";
   translations["Ближний режим · скоростная очередь"]="Close mode · rapid burst";
   translations["Дальний режим · зенитный залп"]="Long-range mode · anti-air burst";
   foreach(var pair in translations)if(!folded.ContainsKey(pair.Key))folded.Add(pair.Key,pair.Value);
   var keys=translations.Keys.Where(k=>k.Length>1&&HasRussian(k)).OrderByDescending(k=>k.Length).Select(k=>(Char.IsLetterOrDigit(k[0])?@"(?<![\p{L}\p{N}_])":"")+Regex.Escape(k)+(Char.IsLetterOrDigit(k[k.Length-1])?@"(?![\p{L}\p{N}_])":""));
   fragmentPattern=new Regex(String.Join("|",keys),RegexOptions.CultureInvariant|RegexOptions.IgnoreCase|RegexOptions.Compiled);
   try{if(File.Exists(settingsPath)&&File.ReadAllText(settingsPath).Trim()=="en")language="en";}catch(IOException){}catch(UnauthorizedAccessException){}
  }
  static bool HasRussian(string text){foreach(char c in text)if(c>='\u0400'&&c<='\u052f')return true;return false;}
  static string Exact(string source){string found;if(translations.TryGetValue(source,out found))return found;if(!folded.TryGetValue(source,out found))return null;return source==source.ToUpperInvariant()?found.ToUpperInvariant():found;}
  public static string Get(string source){
   if(!English||String.IsNullOrEmpty(source)||!HasRussian(source))return source??"";
   string found=Exact(source);if(found!=null)return found;
   if(source.StartsWith("Перезарядка: ",StringComparison.Ordinal)&&source.EndsWith(" с",StringComparison.Ordinal))return "Reloading: "+source.Substring(13,source.Length-15)+" s";
   if(fragments.TryGetValue(source,out found))return found;
   found=fragmentPattern.Replace(source,m=>Exact(m.Value)??m.Value);
   if(fragments.Count>=2048)fragments.Clear();fragments[source]=found;return found;
  }
  public static string EntityName(Entity entity){if(entity==null)return "";return English&&!String.IsNullOrWhiteSpace(entity.NameEn)?entity.NameEn:!String.IsNullOrWhiteSpace(entity.NameRu)?entity.NameRu:!String.IsNullOrWhiteSpace(entity.NameEn)?entity.NameEn:entity.Key;}
  public static T Track<T>(T control) where T:Control {
   Caption caption;if(captions.TryGetValue(control,out caption))return control;
   caption=new Caption{Source=control.Text};captions.Add(control,caption);controls.Add(new WeakReference(control));control.TextChanged+=CaptionChanged;Apply(control,caption);return control;
  }
  static void CaptionChanged(object sender,EventArgs args){var control=(Control)sender;Caption caption;if(!captions.TryGetValue(control,out caption)||caption.Applying)return;caption.Source=control.Text;Apply(control,caption);}
  static void Apply(Control control,Caption caption){if(control.IsDisposed)return;string value=Get(caption.Source);if(control.Text==value)return;caption.Applying=true;try{control.Text=value;}finally{caption.Applying=false;}}
  public static void KeepLiteral(Control control,string source){Caption caption;if(captions.TryGetValue(control,out caption)){control.TextChanged-=CaptionChanged;captions.Remove(control);}control.Text=source;}
  public static void RegisterCombo(UiComboBox combo){controls.Add(new WeakReference(combo));}
  public static void SetLanguage(string value){
   value=value=="en"?"en":"ru";if(language==value)return;language=value;fragments.Clear();
   try{Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));File.WriteAllText(settingsPath,language,new UTF8Encoding(false));}catch(IOException){}catch(UnauthorizedAccessException){}
   for(int i=controls.Count-1;i>=0;i--){var control=controls[i].Target as Control;if(control==null||control.IsDisposed){controls.RemoveAt(i);continue;}Caption caption;if(captions.TryGetValue(control,out caption))Apply(control,caption);var combo=control as UiComboBox;if(combo!=null)combo.RefreshLanguage();control.Invalidate();}
  }
 }
 sealed class UiLabel:Label {public UiLabel(){UiText.Track(this);}}
 sealed class UiButton:Button {public UiButton(){UiText.Track(this);}}
 sealed class UiCheckBox:CheckBox {public UiCheckBox(){UiText.Track(this);}}
 sealed class UiForm:Form {public UiForm(){UiText.Track(this);}}
 sealed class UiReadOnlyTextBox:TextBox {
  public UiReadOnlyTextBox(){ReadOnlyChanged+=(s,e)=>{if(ReadOnly)UiText.Track(this);};}
 }
 sealed class UiComboBox:ComboBox {
  bool refreshingLanguage;
  public UiComboBox(){FormattingEnabled=true;Format+=(s,e)=>{if(e.ListItem is Entity)e.Value=UiText.EntityName((Entity)e.ListItem);else if(e.ListItem is Choice)e.Value=UiText.Get(((Choice)e.ListItem).Label);else if(e.ListItem is Template)e.Value=UiText.Get(((Template)e.ListItem).Name);else if(e.ListItem is string)e.Value=UiText.Get((string)e.ListItem);};UiText.RegisterCombo(this);}
  protected override void OnSelectedIndexChanged(EventArgs e){if(!refreshingLanguage)base.OnSelectedIndexChanged(e);}
  protected override void OnSelectedValueChanged(EventArgs e){if(!refreshingLanguage)base.OnSelectedValueChanged(e);}
  public void RefreshLanguage(){refreshingLanguage=true;try{RefreshItems();Invalidate();}finally{refreshingLanguage=false;}}
 }
 static class UiDialogs {
  public static DialogResult Show(string text){return MessageBox.Show(UiText.Get(text));}
  public static DialogResult Show(string text,string caption){return MessageBox.Show(UiText.Get(text),UiText.Get(caption));}
  public static DialogResult Show(string text,string caption,MessageBoxButtons buttons){return MessageBox.Show(UiText.Get(text),UiText.Get(caption),buttons);}
  public static DialogResult Show(string text,string caption,MessageBoxButtons buttons,MessageBoxIcon icon){return MessageBox.Show(UiText.Get(text),UiText.Get(caption),buttons,icon);}
  public static DialogResult Show(CommonDialog dialog,IWin32Window owner){var file=dialog as FileDialog;if(file!=null){file.Title=UiText.Get(file.Title);var parts=file.Filter.Split('|');for(int i=0;i<parts.Length;i+=2)parts[i]=UiText.Get(parts[i]);file.Filter=String.Join("|",parts);}var folder=dialog as FolderBrowserDialog;if(folder!=null)folder.Description=UiText.Get(folder.Description);return dialog.ShowDialog(owner);}
 }
 public partial class Studio {
  void AddLanguageSelector(FlowLayoutPanel toolbar){
   var label=new UiLabel{Text="Язык / Language",AutoSize=true,ForeColor=Theme.Muted,Margin=new Padding(12,10,4,3)};
   var languages=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=110,BackColor=Theme.Input,ForeColor=Theme.Text,Margin=new Padding(3,5,3,3)};
   languages.Items.AddRange(new[]{"Русский","English"});languages.SelectedIndex=UiText.English?1:0;
   languages.SelectedIndexChanged+=(s,e)=>{SuspendLayout();try{UiText.SetLanguage(languages.SelectedIndex==1?"en":"ru");objects.AccessibleName=UiText.Get("Объекты проекта");objects.Invalidate();if(selected!=null)UpdateObjectCaption(selected);preview.SceneChanged();preview.Invalidate();}finally{ResumeLayout(true);}};
   toolbar.Controls.Add(label);toolbar.Controls.Add(languages);toolbar.WrapContents=true;toolbar.AutoSize=true;
  }
 }
}
