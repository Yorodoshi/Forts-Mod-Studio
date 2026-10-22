using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace FortsStudio {
 // One owner-drawn list serves every project category. The native ListBox keeps
 // scrolling and keyboard navigation; each paint is strictly confined to one row.
 public sealed class ProjectObjectList : ListBox {
  Font titleFont,subtitleFont;
  int rowPadding=10,titleHeight,subtitleHeight;
  public ProjectObjectList(){
   DrawMode=DrawMode.OwnerDrawFixed;BorderStyle=BorderStyle.None;
   IntegralHeight=false;SelectionMode=SelectionMode.One;HorizontalScrollbar=false;
   BackColor=Theme.Card;ForeColor=Theme.Text;AccessibleName="Объекты проекта";
   ResizeRedraw=true;UpdateMetrics();
  }
  protected override void OnFontChanged(EventArgs e){base.OnFontChanged(e);UpdateMetrics();}
  protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);UpdateMetrics();}
  void UpdateMetrics(){
   if(titleFont!=null)titleFont.Dispose();if(subtitleFont!=null)subtitleFont.Dispose();
   titleFont=new Font(Font.FontFamily,Font.Size,FontStyle.Bold);
   subtitleFont=new Font(Font.FontFamily,Math.Max(7,Font.Size*.82f),FontStyle.Regular);
   float scale=1;titleHeight=titleFont.Height;subtitleHeight=subtitleFont.Height;
   if(IsHandleCreated)using(var g=CreateGraphics()){scale=g.DpiY/96;titleHeight=(int)Math.Ceiling(titleFont.GetHeight(g));subtitleHeight=(int)Math.Ceiling(subtitleFont.GetHeight(g));}
   rowPadding=Math.Max(8,(int)Math.Round(10*scale));
   ItemHeight=Math.Min(255,Math.Max(52,titleHeight+subtitleHeight+rowPadding*2+4));
   Invalidate();
  }
  public static string Title(object item){
   var e=item as Entity;if(e!=null)return UiText.EntityName(e)??UiText.Get("Без названия");
   var c=item as Chain;if(c!=null)return String.IsNullOrWhiteSpace(c.Name)?"Без названия":c.Name;
   return Convert.ToString(item);
  }
  protected override void OnDrawItem(DrawItemEventArgs e){
   if(e.Index<0||e.Index>=Items.Count)return;
   var saved=e.Graphics.Save();try{
    e.Graphics.SetClip(e.Bounds,CombineMode.Intersect);
    bool active=(e.State&DrawItemState.Selected)!=0;
    Color background=active?Color.FromArgb(39,68,69):BackColor;
    using(var brush=new SolidBrush(background))e.Graphics.FillRectangle(brush,e.Bounds);
    if(active)using(var marker=new SolidBrush(Theme.Accent))e.Graphics.FillRectangle(marker,e.Bounds.X,e.Bounds.Y,3,e.Bounds.Height);
    object item=Items[e.Index];var entity=item as Entity;var chain=item as Chain;
    string subtitle=entity!=null?entity.Key:chain!=null?(chain.Enabled?"Сценарий включён":"Сценарий выключен"):"";
    int width=Math.Max(1,e.Bounds.Width-rowPadding*2);
    var title=new Rectangle(e.Bounds.X+rowPadding,e.Bounds.Y+rowPadding,width,titleHeight+2);
    var detail=new Rectangle(title.X,title.Bottom+3,width,subtitleHeight+2);
    const TextFormatFlags flags=TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix|TextFormatFlags.PreserveGraphicsClipping;
    TextRenderer.DrawText(e.Graphics,Title(item),titleFont,title,ForeColor,flags);
    TextRenderer.DrawText(e.Graphics,UiText.Get(subtitle??""),subtitleFont,detail,Theme.Muted,flags);
    if(Focused&&(e.State&DrawItemState.Focus)!=0)ControlPaint.DrawFocusRectangle(e.Graphics,Rectangle.Inflate(e.Bounds,-4,-3),ForeColor,background);
   }finally{e.Graphics.Restore(saved);}
  }
  protected override void Dispose(bool disposing){if(disposing){if(titleFont!=null)titleFont.Dispose();if(subtitleFont!=null)subtitleFont.Dispose();titleFont=null;subtitleFont=null;}base.Dispose(disposing);}
 }

 public partial class Studio {
  sealed class NavigationState {public object Item;public int Index,Top;}
  readonly Dictionary<string,NavigationState> navigationState=new Dictionary<string,NavigationState>();
  readonly Dictionary<string,Button> categoryButtons=new Dictionary<string,Button>();
  Label navigationCaption,navigationEmpty;bool objectViewActive;

  Control CreateNavigation(){
   var navigation=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=5,BackColor=Theme.Card,Margin=new Padding(0,0,12,0)};
   navigation.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
   navigation.RowStyles.Add(new RowStyle(SizeType.AutoSize));
   navigation.RowStyles.Add(new RowStyle(SizeType.AutoSize));
   navigation.RowStyles.Add(new RowStyle(SizeType.AutoSize));
   navigation.RowStyles.Add(new RowStyle(SizeType.Percent,100));
   navigation.RowStyles.Add(new RowStyle(SizeType.AutoSize));
   var brand=Theme.Label("F / STUDIO 2.7",12,true);brand.Dock=DockStyle.Fill;brand.Margin=Padding.Empty;brand.Padding=new Padding(12,16,6,15);navigation.Controls.Add(brand,0,0);
   var categories=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Margin=Padding.Empty,Padding=new Padding(8,0,8,8)};
   categories.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
   foreach(var pair in names.Concat(new[]{new KeyValuePair<string,string>("actions","Сценарии"),new KeyValuePair<string,string>("project","Проект и установка")})){
    string category=pair.Key;var button=Theme.Button(pair.Value,()=>Switch(category));button.Dock=DockStyle.Fill;button.Margin=new Padding(0,2,0,2);button.AutoEllipsis=true;
    int row=categories.RowCount++;categories.RowStyles.Add(new RowStyle(SizeType.AutoSize));categories.Controls.Add(button,0,row);categoryButtons[category]=button;
   }
   navigation.Controls.Add(categories,0,1);
   navigationCaption=Theme.Label("",9,true);navigationCaption.Dock=DockStyle.Fill;navigationCaption.Margin=Padding.Empty;navigationCaption.Padding=new Padding(12,8,8,9);navigationCaption.ForeColor=Theme.Muted;navigation.Controls.Add(navigationCaption,0,2);
   var listHost=new Panel{Dock=DockStyle.Fill,Margin=Padding.Empty,BackColor=Theme.Card};
   objects.Dock=DockStyle.Fill;objects.Margin=Padding.Empty;objects.Font=Font;
   objects.SelectedIndexChanged+=(s,e)=>{if(!changing)PresentSelectedObject();};
   objects.MouseClick+=(s,e)=>{if(!changing&&!objectViewActive&&objects.SelectedItem!=null)PresentSelectedObject();};
   objects.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Enter&&objects.SelectedItem!=null){PresentSelectedObject();e.Handled=true;}};
   listHost.Controls.Add(objects);
   navigationEmpty=Theme.Label("Пока ничего не добавлено",9);navigationEmpty.AutoSize=false;navigationEmpty.Dock=DockStyle.Fill;navigationEmpty.Padding=new Padding(12,16,10,0);navigationEmpty.ForeColor=Theme.Muted;listHost.Controls.Add(navigationEmpty);navigationEmpty.BringToFront();
   navigation.Controls.Add(listHost,0,3);
   var add=Theme.Button("＋ Добавить",Add,true);add.Dock=DockStyle.Fill;add.Margin=new Padding(8,8,8,8);navigation.Controls.Add(add,0,4);
   return navigation;
  }
  IEnumerable<object> CategoryObjects(string category){
   if(category=="actions")return project.Chains.Cast<object>();
   if(!names.ContainsKey(category))return Enumerable.Empty<object>();
   return project.Entities.Where(e=>category=="technologies"?TechnologyRules.IsTechnology(e):e.Kind==category&&(category!="devices"||!TechnologyRules.IsTechnology(e))).Cast<object>();
  }
  void RememberNavigation(){
   if(kind==null||objects.Items.Count==0)return;
   navigationState[kind]=new NavigationState{Item=objects.SelectedItem,Index=Math.Max(0,objects.SelectedIndex),Top=objects.TopIndex};
  }
  void ResetNavigation(){navigationState.Clear();changing=true;try{objects.Items.Clear();}finally{changing=false;}}
  void Switch(string category,object preferred=null){
   RememberNavigation();NavigationState remembered;navigationState.TryGetValue(category,out remembered);
   var items=CategoryObjects(category).ToArray();
   int index=preferred==null?-1:Array.FindIndex(items,x=>Object.ReferenceEquals(x,preferred));
   bool explicitSelection=index>=0;
   if(index<0&&remembered!=null)index=Array.FindIndex(items,x=>Object.ReferenceEquals(x,remembered.Item));
   if(index<0&&items.Length>0)index=Math.Min(items.Length-1,remembered==null?0:remembered.Index);
   changing=true;objects.BeginUpdate();try{
    kind=category;selected=null;chain=null;insertion=null;preview.Selected=null;objectViewActive=false;
    objects.Items.Clear();objects.Items.AddRange(items);objects.SelectedIndex=index;
    if(!explicitSelection&&remembered!=null&&items.Length>0)objects.TopIndex=Math.Min(items.Length-1,remembered.Top);
   }finally{objects.EndUpdate();changing=false;}
   ScenarioLayout(category=="actions");UpdateNavigationCaption();PresentSelectedObject();
  }
  void UpdateNavigationCaption(){
   if(navigationCaption==null)return;
   string caption;names.TryGetValue(kind,out caption);if(caption==null)caption=kind=="actions"?"Сценарии":"Проект";
   navigationCaption.Text=kind=="project"?caption:caption+" · "+objects.Items.Count;
   navigationEmpty.Text=kind=="project"?"Настройки всего проекта":"Пока ничего не добавлено";
   navigationEmpty.Visible=objects.Items.Count==0;objects.Visible=objects.Items.Count>0;
   foreach(var pair in categoryButtons){bool on=pair.Key==kind;pair.Value.BackColor=on?Color.FromArgb(39,68,69):Theme.Input;pair.Value.ForeColor=on?Theme.Accent:Theme.Text;}
  }
  void PresentSelectedObject(){
   selected=objects.SelectedItem as Entity;chain=objects.SelectedItem as Chain;insertion=null;preview.Selected=selected;
   if(selected!=null){ShowEntity();objectViewActive=true;}
   else if(chain!=null){ShowChain();objectViewActive=true;}
   else if(kind=="project")ShowProject();else if(kind=="actions")ShowActionsEmpty();else ShowEmpty();
  }
  void UpdateObjectCaption(object item){
   objects.Invalidate();
   if(!Object.ReferenceEquals(item,objects.SelectedItem)||!objectViewActive)return;
   var label=editor.Controls.Find("editorTitle",true).FirstOrDefault() as Label;if(label!=null)label.Text=ProjectObjectList.Title(item);
  }
  // Detached controls otherwise leave handles and timers alive across tab changes.
  // The preview is a shared control and must be removed before disposing its host.
  void ClearUi(Control host){
   if(host==right){preview.AimChanged=null;if(right.Contains(preview)&&preview.Parent!=null)preview.Parent.Controls.Remove(preview);}
   var children=host.Controls.Cast<Control>().ToArray();host.Controls.Clear();foreach(var child in children)child.Dispose();
  }
 }
}
