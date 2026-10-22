using System;using System.IO;using System.Drawing;using System.Drawing.Drawing2D;using System.Drawing.Imaging;using System.Runtime.InteropServices;using System.Runtime.CompilerServices;using System.Collections.Generic;using System.Linq;using System.Text.RegularExpressions;
namespace FortsStudio {
 public static class Assets {
  class ImageEntry {public Bitmap Image;public ImageEntry(Bitmap image){Image=image;}}
  // Embedded strings can be several megabytes long. Identity lookup avoids
  // hashing all of that data on every paint and releases unused project data.
  static ConditionalWeakTable<string,ImageEntry> embeddedImages=new ConditionalWeakTable<string,ImageEntry>();
  static Dictionary<string,Dictionary<string,Bitmap>> fileImages=new Dictionary<string,Dictionary<string,Bitmap>>(StringComparer.Ordinal);
  static bool native=false,attempt=false;
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern IntPtr LoadLibrary(string path);
  [DllImport("DevIL.dll",CallingConvention=CallingConvention.StdCall)]static extern void ilInit();
  [DllImport("DevIL.dll",CallingConvention=CallingConvention.StdCall)]static extern void ilGenImages(uint n,out uint images);
  [DllImport("DevIL.dll",CallingConvention=CallingConvention.StdCall)]static extern void ilBindImage(uint image);
  [DllImport("DevIL.dll",CallingConvention=CallingConvention.StdCall,CharSet=CharSet.Ansi)]static extern byte ilLoadImage(string name);
  [DllImport("DevIL.dll",CallingConvention=CallingConvention.StdCall)]static extern byte ilConvertImage(uint format,uint type);
  [DllImport("DevIL.dll",CallingConvention=CallingConvention.StdCall)]static extern int ilGetInteger(uint mode);
  [DllImport("DevIL.dll",CallingConvention=CallingConvention.StdCall)]static extern IntPtr ilGetData();
  [DllImport("DevIL.dll",CallingConvention=CallingConvention.StdCall)]static extern void ilDeleteImages(uint n,ref uint images);
  [DllImport("DevIL.dll",CallingConvention=CallingConvention.StdCall)]static extern byte ilEnable(uint mode);
  [DllImport("DevIL.dll",CallingConvention=CallingConvention.StdCall)]static extern void ilOriginFunc(uint mode);
  public static void Resolve(string game){foreach(var t in Catalog.Templates){try{string list=File.ReadAllText(Path.Combine(game,"data",Catalog.TemplateListPath(t)));string entry=Catalog.LuaTable(list,t.Key);var m=Regex.Match(entry,"FileName\\s*=\\s*\"([^\"]+)\"");if(m.Success)t.Script=m.Groups[1].Value;
    if(t.Kind=="materials"){m=Regex.Match(entry,"Sprite\\s*=\\s*\"([^\"]+)\"");if(m.Success)t.Sprite=m.Groups[1].Value;}
    string src=t.Script.Length>0?File.ReadAllText(Path.Combine(game,"data",t.Script)):list;
    int rootAt=src.IndexOf("Root =");if(rootAt>=0){string tree=src.Substring(rootAt);if(t.Kind=="devices"&&TechnologyRules.IsTemplate(t.Key)){var rootSprite=Regex.Match(tree,"Sprite\\s*=\\s*\"([^\"]+)\"");if(rootSprite.Success)t.Sprite=rootSprite.Groups[1].Value;}else{var head=Regex.Match(tree,"Name\\s*=\\s*\"Head\"");if(head.Success){int open=tree.LastIndexOf('{',head.Index);var node=Catalog.Balanced(tree,open);var sprite=Regex.Match(node,"Sprite\\s*=\\s*\"([^\"]+)\"");if(sprite.Success)t.Sprite=sprite.Groups[1].Value;}}}
    var sprites=Regex.Matches(src,"Name\\s*=\\s*\"([^\"]+)\"");string chosen="";foreach(Match s in sprites){string name=s.Groups[1].Value;if(name==t.Sprite||chosen==""&&t.Kind!="materials"){int a=src.LastIndexOf('{',s.Index);var body=a>=0?Catalog.Balanced(src,a):"";var tex=Regex.Match(body,"texture\\s*=\\s*\"([^\"]+)\"");if(tex.Success){if(chosen==""||name==t.Sprite){t.Texture=tex.Groups[1].Value;chosen=name;}if(name==t.Sprite)break;}}}if(chosen.Length>0&&t.Kind!="materials")t.Sprite=chosen;
   }catch{}}
  }
  public static byte[] DataBytes(string s){if(s.StartsWith("data:")){int i=s.IndexOf(',');return Convert.FromBase64String(s.Substring(i+1));}return File.ReadAllBytes(s);}
  public static string Import(string path,bool image){if(!image)return "data:audio/"+Path.GetExtension(path).TrimStart('.')+";base64,"+Convert.ToBase64String(File.ReadAllBytes(path));using(var b=new Bitmap(path)){double sc=Math.Min(1,2048.0/Math.Max(b.Width,b.Height));using(var target=Raster.Resize(b,Math.Max(1,(int)(b.Width*sc)),Math.Max(1,(int)(b.Height*sc)))){using(var ms=new MemoryStream()){target.Save(ms,ImageFormat.Png);return "data:image/png;base64,"+Convert.ToBase64String(ms.ToArray());}}}}
  public static Bitmap Image(string source,string game){
   if(String.IsNullOrEmpty(source))return null;
   bool embedded=source.StartsWith("data:",StringComparison.Ordinal);game=game??"";
   ImageEntry entry;Dictionary<string,Bitmap> files=null;Bitmap old;
   if(embedded){if(embeddedImages.TryGetValue(source,out entry))return entry.Image;}
   else{if(!fileImages.TryGetValue(game,out files)){files=new Dictionary<string,Bitmap>(StringComparer.Ordinal);fileImages.Add(game,files);}if(files.TryGetValue(source,out old))return old;}
   Bitmap b=null;
   try{if(embedded){using(var ms=new MemoryStream(DataBytes(source)))using(var im=new Bitmap(ms))b=new Bitmap(im);}else{string path=Path.IsPathRooted(source)?source:Path.Combine(game,"data",source);if(!File.Exists(path)){var stem=Path.ChangeExtension(path,null);path=new[]{stem+".tga",stem+".png",stem+".dds",stem}.FirstOrDefault(File.Exists)??path;}try{using(var im=new Bitmap(path))b=new Bitmap(im);}catch{b=NativeImage(path,game);}}}catch{}
   if(embedded)embeddedImages.Add(source,new ImageEntry(b));else files[source]=b;
   return b;
  }
  static Bitmap NativeImage(string file,string game){try{if(!attempt){attempt=true;native=LoadLibrary(Path.Combine(game,"DevIL.dll"))!=IntPtr.Zero;if(native){ilInit();ilEnable(0x0600);ilOriginFunc(0x0602);}}if(!native)return null;uint id;ilGenImages(1,out id);try{ilBindImage(id);if(ilLoadImage(file)==0||ilConvertImage(0x80E1,0x1401)==0)return null;int w=ilGetInteger(0x0DE4),h=ilGetInteger(0x0DE5);if(w<=0||h<=0||w*h>64000000)return null;byte[] pixels=new byte[w*h*4];Marshal.Copy(ilGetData(),pixels,0,pixels.Length);var bmp=new Bitmap(w,h,PixelFormat.Format32bppArgb);var bits=bmp.LockBits(new Rectangle(0,0,w,h),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);for(int y=0;y<h;y++)Marshal.Copy(pixels,y*w*4,IntPtr.Add(bits.Scan0,y*bits.Stride),w*4);bmp.UnlockBits(bits);return bmp;}finally{ilDeleteImages(1,ref id);}}catch{return null;}}
  public static Bitmap Body(Entity e,string game){var image=Image(e.Texture.Length>0?e.Texture:Catalog.Get(e).Texture,game);return e.Kind=="projectiles"&&e.Texture.Length>0?Raster.Tight(image):image;}
  // A square artwork thumbnail. HUD cards add their own price area below it.
  public static Bitmap Icon(Entity e,string game){
   var b=new Bitmap(128,128,PixelFormat.Format32bppArgb);
   using(var g=Graphics.FromImage(b)){
    g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;
    using(var br=new LinearGradientBrush(new Rectangle(0,0,128,128),Color.FromArgb(87,116,186),Color.FromArgb(31,48,92),90))g.FillRectangle(br,0,0,128,128);
    var custom=Image(e.Icon,game);var im=custom??Body(e,game);
    if(im!=null){
     if(custom==null&&e.Kind=="materials"){
      // A recognisable close-up, independent of the in-world beam thickness.
      var save=g.Save();g.TranslateTransform(64,64);g.RotateTransform(41);
      using(var outline=new SolidBrush(Color.FromArgb(23,25,31)))g.FillRectangle(outline,-118,-30,236,60);
      // Sample the original artwork rather than enlarging the thin in-game TGA.
      using(var beam=Raster.Resize(im,im.Width,im.Height)){if(e.Texture.Length>0&&e.SeamlessTexture)Raster.JoinHorizontalEdges(beam);Raster.DrawStrip(g,beam,new RectangleF(-118,-27,236,54));}g.Restore(save);
     }else{
      using(var fitted=Raster.Fit(Raster.Tight(im),118,118))g.DrawImageUnscaled(fitted,5,5);
     }
    }else using(var font=new Font("Segoe UI",22,FontStyle.Bold))g.DrawString("FMS",font,Brushes.White,18,45);
    using(var pen=new Pen(Color.FromArgb(205,219,233),3))g.DrawRectangle(pen,1.5f,1.5f,125,125);
   }return b;
  }
  // Stock Forts uses a 128x256 texture cropped at bottom=.664 (170px).
  // The upper 128px contain art; the next 42px remain clear for engine prices.
  public static Bitmap HudCard(Entity e,string game,int state){
   var card=new Bitmap(128,256,PixelFormat.Format32bppArgb);
   using(var art=Icon(e,game))using(var g=Graphics.FromImage(card)){
    if(state==3){using(var attributes=new ImageAttributes()){
     attributes.SetColorMatrix(new ColorMatrix(new float[][]{
      new float[]{.299f,.299f,.299f,0,0},new float[]{.587f,.587f,.587f,0,0},new float[]{.114f,.114f,.114f,0,0},new float[]{0,0,0,1,0},new float[]{0,0,0,0,1}}));
     g.DrawImage(art,new Rectangle(0,0,128,128),0,0,128,128,GraphicsUnit.Pixel,attributes);
    }}else g.DrawImageUnscaled(art,0,0);
    Color panel=state==1||state==2?Color.FromArgb(245,245,229):state==3?Color.FromArgb(150,156,150):Color.FromArgb(201,206,192);
    using(var brush=new SolidBrush(panel))g.FillRectangle(brush,0,128,128,42);
    Color border=state==1||state==2?Color.White:Color.FromArgb(211,220,224);
    using(var pen=new Pen(border,2))g.DrawRectangle(pen,1,1,126,168);
   }return card;
  }
  public static byte[] Tga(Bitmap source,int height){int h=height>0?Math.Max(2,Math.Min(512,height)):source.Height;int w=height>0?Math.Max(8,(int)(source.Width*(double)h/source.Height)):source.Width;w=Math.Min(4096,w);using(var b=Raster.Resize(source,w,h)){using(var ms=new MemoryStream())using(var bw=new BinaryWriter(ms)){byte[] header=new byte[18];header[2]=2;header[12]=(byte)w;header[13]=(byte)(w>>8);header[14]=(byte)h;header[15]=(byte)(h>>8);header[16]=32;header[17]=0x28;bw.Write(header);var bits=b.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);try{var row=new byte[w*4];for(int y=0;y<h;y++){Marshal.Copy(IntPtr.Add(bits.Scan0,y*bits.Stride),row,0,row.Length);bw.Write(row);}}finally{b.UnlockBits(bits);}return ms.ToArray();}}}
 }
}
