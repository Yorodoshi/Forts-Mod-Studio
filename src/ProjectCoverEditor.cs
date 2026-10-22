using System;using System.Drawing;using System.Windows.Forms;
namespace FortsStudio {
 public partial class Studio {
  void DrawProjectCover(FlowLayoutPanel f){
   Header(f,"Обложка мода");f.Controls.Add(Theme.Label("Изображение списка модификаций и Steam Workshop. Сохраняется внутри проекта и применяется при каждом экспорте.",9));
   if(!String.IsNullOrEmpty(project.CoverImage)){var image=Assets.Image(project.CoverImage,project.GameRoot);if(image!=null)f.Controls.Add(new PictureBox{Image=Raster.Fit(image,320,180),Width=320,Height=180,SizeMode=PictureBoxSizeMode.Normal});}
   f.Controls.Add(Theme.Button("Выбрать обложку",()=>Safe(()=>{using(var d=new OpenFileDialog{Filter="Изображения|*.png;*.jpg;*.jpeg;*.bmp"})if(UiDialogs.Show(d,this)==DialogResult.OK){project.CoverImage=Assets.Import(d.FileName,true);Mark();ShowProject();}})));
   if(!String.IsNullOrEmpty(project.CoverImage))f.Controls.Add(Theme.Button("Автоматическая обложка",()=>{project.CoverImage="";Mark();ShowProject();}));
  }
 }
}
