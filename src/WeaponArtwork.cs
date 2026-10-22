using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Drawing.Imaging;using System.Linq;using System.Runtime.CompilerServices;using System.Text;using System.Windows.Forms;
namespace FortsStudio {
 public class WeaponArt {
  public bool Enabled=false;
  public string BaseImage="",HeadImage="";
  public double BaseWidth=180,BaseHeight=55,HeadWidth=220,HeadHeight=95;
  public double PivotX=.25,PivotY=.55,MountX=0,MountY=70,MuzzleX=.98,MuzzleY=.5,RecoilDistance=8;
 }
 public static class WeaponArtwork {
  class PreparedImage {public int Width,Height;public Bitmap Image;}
  class PreparedVariants {public PreparedImage First,Second;}
  static ConditionalWeakTable<Bitmap,PreparedVariants> preparedImages=new ConditionalWeakTable<Bitmap,PreparedVariants>();
  // Resample artwork once to the exported pixel density. Painting a small
  // weapon then samples this bitmap instead of filtering its full import.
  static Bitmap Prepared(Bitmap source,double width,double height,double size){
   if(source==null)return null;
   int w=Math.Max(2,(int)Math.Round(width*2*size)),h=Math.Max(2,(int)Math.Round(height*2*size));
   if(source.Width==w&&source.Height==h)return source;
   PreparedVariants variants;if(!preparedImages.TryGetValue(source,out variants)){variants=new PreparedVariants();preparedImages.Add(source,variants);}
   var found=variants.First;if(found!=null&&found.Width==w&&found.Height==h)return found.Image;
   found=variants.Second;if(found!=null&&found.Width==w&&found.Height==h){variants.Second=variants.First;variants.First=found;return found.Image;}
   var image=Raster.Resize(source,w,h);variants.Second=variants.First;variants.First=new PreparedImage{Width=w,Height=h,Image=image};return image;
  }
  public static bool Draw(Graphics g,Entity e,string game,PointF pos,float scale,double angle,int progress,double time,double recoil){
   var art=e.Art;if(art==null||!art.Enabled)return false;
   double size=e.V("Scale",1);
   var bottom=Prepared(Raster.Tight(Assets.Image(art.BaseImage,game)),art.BaseWidth,art.BaseHeight,size);var head=Prepared(Raster.Tight(Assets.Image(art.HeadImage,game)),art.HeadWidth,art.HeadHeight,size);
   if(bottom==null||head==null)return false;
   var state=g.Save();g.TranslateTransform(pos.X,pos.Y);g.ScaleTransform(scale,scale);
   g.DrawImage(bottom,(float)-art.BaseWidth/2,(float)-art.BaseHeight,(float)art.BaseWidth,(float)art.BaseHeight);
   if(progress>=50){g.TranslateTransform((float)art.MountX,(float)-art.MountY);g.RotateTransform((float)-angle);float d=12;var bearing=g.Save();using(var clip=new GraphicsPath()){clip.AddEllipse(-d/2,-d/2,d,d);g.SetClip(clip,CombineMode.Intersect);g.DrawImage(head,new RectangleF(-d/2,-d/2,d,d),new RectangleF((float)(art.PivotX*head.Width-d*head.Width/art.HeadWidth/2),(float)(art.PivotY*head.Height-d*head.Height/art.HeadHeight/2),(float)(d*head.Width/art.HeadWidth),(float)(d*head.Height/art.HeadHeight)),GraphicsUnit.Pixel);}g.Restore(bearing);g.DrawImage(head,(float)(-art.PivotX*art.HeadWidth-recoil*art.RecoilDistance),(float)(-art.PivotY*art.HeadHeight),(float)art.HeadWidth,(float)art.HeadHeight);}
   g.Restore(state);return true;
  }
 }
 public static partial class Exporter {
  static string WeaponArtworkScript(Project p,string root,Entity e){
   var a=e.Art;if(a==null||!a.Enabled)return "";
   string baseFile=AssetPath(e.Key,"-base.tga"),headFile=AssetPath(e.Key,"-head.tga"),pivotFile=AssetPath(e.Key,"-pivot.tga");
   var bottom=Raster.Tight(Assets.Image(a.BaseImage,p.GameRoot));var head=Raster.Tight(Assets.Image(a.HeadImage,p.GameRoot));
   if(bottom==null||head==null)throw new Exception(e.NameRu+": загрузите основание и поворотную часть орудия.");
   double size=e.V("Scale",1);int headWidth=Math.Max(2,(int)Math.Round(a.HeadWidth*2*size)),headHeight=Math.Max(2,(int)Math.Round(a.HeadHeight*2*size));
   using(var image=Raster.Resize(bottom,Math.Max(2,(int)Math.Round(a.BaseWidth*2*size)),Math.Max(2,(int)Math.Round(a.BaseHeight*2*size))))Bytes(root,baseFile,Assets.Tga(image,0));
   using(var image=Raster.Resize(head,headWidth,headHeight)){
    Bytes(root,headFile,Assets.Tga(image,0));
    // Keep a small stationary bearing at the actual pivot. Besides supplying
    // dimensions to Head, this leaves the bearing in place during barrel recoil.
    using(var pivot=new Bitmap(headWidth,headHeight,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(pivot)){
     float diameter=(float)Math.Min(Math.Min(headWidth,headHeight),24*size);var destination=new RectangleF((headWidth-diameter)/2,(headHeight-diameter)/2,diameter,diameter);
     using(var clip=new GraphicsPath()){clip.AddEllipse(destination);g.SetClip(clip);g.DrawImage(image,destination,new RectangleF((float)(a.PivotX*headWidth-diameter/2),(float)(a.PivotY*headHeight-diameter/2),diameter,diameter),GraphicsUnit.Pixel);}
     Bytes(root,pivotFile,Assets.Tga(pivot,0));
    }
   }
   // A model parent needs its own sprite dimensions in the native renderer.
   // A dimensioned pivot bearing keeps rotation at the mounting point; Barrel
   // supplies the artwork and engine recoil. Its child pivots use that same size.
   string prefix="fms_art_"+e.Key;var b=new StringBuilder("Scale = 1\n");b.Append(Sprite(prefix+"_base",baseFile));b.Append(Sprite(prefix+"_head",headFile));b.Append(Sprite(prefix+"_pivot",pivotFile));
   b.Append("Root = { Name = 'FMSBase', Angle = 0, Pivot = { 0, -0.5 }, PivotOffset = { 0, 0 }, Sprite = "+Q(prefix+"_base")+", UserData = 0, ChildrenInFront = {\n");
   b.Append("{ Name = 'Head', Angle = 0, Pivot = { "+N(a.MountX/a.BaseWidth)+", "+N(.5-a.MountY/a.BaseHeight)+" }, PivotOffset = { 0, 0 }, Sprite = "+Q(prefix+"_pivot")+", UserData = 50, ChildrenInFront = {\n");
   b.Append("{ Name = 'Barrel', Angle = 0, Pivot = { "+N(.5-a.PivotX)+", "+N(.5-a.PivotY)+" }, PivotOffset = { 0, 0 }, Sprite = "+Q(prefix+"_head")+", UserData = 50, ChildrenInFront = {\n");
   b.Append("{ Name = 'Hardpoint0', Angle = 90, Pivot = { "+N(a.MuzzleX-.5)+", "+N(a.MuzzleY-.5)+" }, PivotOffset = { 0, 0 } },\n");
   b.Append("{ Name = 'LaserSight', Angle = 90, Pivot = { "+N(a.MuzzleX-.5)+", "+N(a.MuzzleY-.5)+" }, PivotOffset = { 0, 0 } },\n");
   b.Append("{ Name = 'ReloadEmitter', Angle = 90, Pivot = { "+N(a.MuzzleX-.5)+", "+N(a.MuzzleY-.5)+" }, PivotOffset = { 0, 0 } },\n");
   b.Append("{ Name = 'Chamber', Angle = 0, Pivot = { "+N(a.PivotX-.5)+", "+N(a.PivotY-.5)+" }, PivotOffset = { 0, 0 } }\n} } } }, { Name = 'Icon', Pivot = { 0, 0.5 } } } }\n");
   b.Append("BarrelRecoilLimit = "+N(-a.RecoilDistance/a.HeadWidth)+"\nBarrelRecoilSpeed = -2\nBarrelReturnForce = 0.5\n");
   return b.ToString();
  }
 }
 public partial class Studio {
  void DrawWeaponArtwork(FlowLayoutPanel f,Entity e){
   if(e.Art==null)e.Art=new WeaponArt();var a=e.Art;Header(f,"Послойная модель орудия");
   var enabled=new UiCheckBox{Text="Своё основание и поворотная часть",Checked=a.Enabled,AutoSize=true,ForeColor=Theme.Text};enabled.CheckedChanged+=(s,v)=>{a.Enabled=enabled.Checked;Mark();ShowEntityTab(e,"Ресурсы");};f.Controls.Add(enabled);
   f.Controls.Add(Theme.Label("Основание стоит на платформе; верхняя часть поворачивается при наведении и двигается при отдаче. Ствол на исходном изображении должен смотреть строго вправо. Размеры — в игровых единицах.",9));
   foreach(string layer in new[]{"Основание","Поворотная часть"}){string part=layer;f.Controls.Add(Theme.Button(part+" · загрузить PNG",()=>Safe(()=>{using(var dialog=new OpenFileDialog{Filter="Изображения|*.png;*.jpg;*.bmp"})if(UiDialogs.Show(dialog,this)==DialogResult.OK){string image=Assets.Import(dialog.FileName,true);var bitmap=Assets.Image(image,project.GameRoot);if(part=="Основание"){a.BaseImage=image;a.BaseHeight=a.BaseWidth*bitmap.Height/bitmap.Width;}else{a.HeadImage=image;a.HeadHeight=a.HeadWidth*bitmap.Height/bitmap.Width;}a.Enabled=a.BaseImage.Length>0&&a.HeadImage.Length>0;Mark();ShowEntityTab(e,"Ресурсы");}})));}
   if(!a.Enabled)return;
   Number(f,"Ширина основания","Полная ширина изображения основания.",a.BaseWidth,2,700,v=>a.BaseWidth=v,"BaseWidth");
   Number(f,"Высота основания","Высота изображения от платформы до верхнего края.",a.BaseHeight,2,500,v=>a.BaseHeight=v,"BaseHeight");
   Number(f,"Ширина поворотной части","Размер изображения целиком, включая прозрачные поля.",a.HeadWidth,2,900,v=>a.HeadWidth=v,"HeadWidth");
   Number(f,"Высота поворотной части","Размер изображения целиком, включая прозрачные поля.",a.HeadHeight,2,600,v=>a.HeadHeight=v,"HeadHeight");
   Number(f,"Ось вращения X, %","Где находится шарнир внутри изображения верхней части: 0% — левый край.",a.PivotX*100,0,100,v=>a.PivotX=v/100,"PivotX");
   Number(f,"Ось вращения Y, %","0% — верхний край; 100% — нижний край.",a.PivotY*100,0,100,v=>a.PivotY=v/100,"PivotY");
   Number(f,"Высота шарнира","Расстояние от платформы до оси вращения.",a.MountY,0,600,v=>a.MountY=v,"MountY");
   Number(f,"Сдвиг шарнира X","Положение шарнира относительно середины основания.",a.MountX,-600,600,v=>a.MountX=v,"MountX");
   Number(f,"Выход снаряда X, %","Дульный срез внутри изображения верхней части.",a.MuzzleX*100,0,100,v=>a.MuzzleX=v/100,"MuzzleX");
   Number(f,"Выход снаряда Y, %","Вертикальное положение дульного среза.",a.MuzzleY*100,0,100,v=>a.MuzzleY=v/100,"MuzzleY");
   Number(f,"Ход отдачи","Насколько изображение откатывается назад после выстрела.",a.RecoilDistance,0,80,v=>a.RecoilDistance=v,"RecoilDistance");
  }
 }
}
