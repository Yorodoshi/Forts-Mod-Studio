using System;using System.Drawing;using System.Windows.Forms;
namespace FortsStudio {
 public partial class Studio {
  void MaterialWidth(FlowLayoutPanel f,Entity e){
   var extrusion=new UiCheckBox{Text="Вытягивать целую секцию, как дерево",AutoSize=true,ForeColor=Theme.Text,Checked=e.Flags.ContainsKey("FullExtrusion")?e.Flags["FullExtrusion"]:e.Template!="armour"};extrusion.CheckedChanged+=(s,a)=>{e.Flags["FullExtrusion"]=extrusion.Checked;Mark();};f.Controls.Add(extrusion);f.Controls.Add(Theme.Label("При протягивании от балки Forts строит полную секцию. Выключите, чтобы материал вытягивался отдельной балкой, как броня. Можно менять независимо от прочности, массы и текстуры.",9));Header(f,"Размер балки в игре");
   var hint=Theme.Label("",9);hint.ForeColor=Theme.Accent;
   Action update=()=>hint.Text="Текстура: "+MaterialVisuals.PixelHeight(e)+" px → "+References.Format(MaterialVisuals.WorldHeight(e))+" ед. в Forts. Штатный шаблон: "+References.Format(MaterialVisuals.NativeHeight(e,project.GameRoot))+" ед. Прозрачные поля изображения входят в размер.";
   var row=new FlowLayoutPanel{Width=490,AutoSize=true,WrapContents=false};var n=new NumericUpDown{DecimalPlaces=1,Increment=.5M,Minimum=1,Maximum=256,Value=(decimal)Math.Max(1,Math.Min(256,MaterialVisuals.WorldHeight(e))),Width=110,BackColor=Theme.Input,ForeColor=Theme.Text};var slider=new TrackBar{Minimum=2,Maximum=512,Value=MaterialVisuals.PixelHeight(e),Width=330,TickStyle=TickStyle.None,BackColor=Theme.Card};bool sync=false;
   n.ValueChanged+=(s,a)=>{if(sync)return;sync=true;e.Thickness=(double)n.Value;slider.Value=MaterialVisuals.PixelHeight(e);update();Mark();sync=false;};slider.ValueChanged+=(s,a)=>{if(sync)return;sync=true;e.Thickness=slider.Value*.5;n.Value=(decimal)e.Thickness;update();Mark();sync=false;};row.Controls.Add(slider);row.Controls.Add(n);f.Controls.Add(row);f.Controls.Add(hint);f.Controls.Add(Theme.Label("Высота спрайта, игровые единицы; шаг 0,5. Масса не меняет изображение. Встроенный просмотр и экспорт используют один и тот же спрайт, включая прозрачность и повреждения.",9));f.Controls.Add(Theme.Button("Размер штатного шаблона",()=>{e.Thickness=MaterialVisuals.NativeHeight(e,project.GameRoot);Mark();ShowEntityTab(e,"Основное");}));update();
  }
 }
}
