using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FortsStudio {
 public static class Raster {
  static ConditionalWeakTable<Bitmap,Bitmap> tightImages=new ConditionalWeakTable<Bitmap,Bitmap>();
  // Transparent padding is an import/layout concern. Cache the visible source
  // rectangle so full-size reference canvases do not make gun feet float.
  public static Bitmap Tight(Bitmap source){
   if(source==null)return null;Bitmap found;if(tightImages.TryGetValue(source,out found))return found;
   int left=source.Width,top=source.Height,right=-1,bottom=-1;
   var bits=source.LockBits(new Rectangle(0,0,source.Width,source.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
   try{var row=new byte[source.Width*4];for(int y=0;y<source.Height;y++){Marshal.Copy(IntPtr.Add(bits.Scan0,y*bits.Stride),row,0,row.Length);for(int x=0;x<source.Width;x++)if(row[x*4+3]>8){left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);}}}finally{source.UnlockBits(bits);}
   found=right<left?Resize(source,source.Width,source.Height):source.Clone(new Rectangle(left,top,right-left+1,bottom-top+1),PixelFormat.Format32bppArgb);
   tightImages.Add(source,found);return found;
  }
  public static Bitmap Fit(Bitmap source,int width,int height){
   var result=new Bitmap(width,height,PixelFormat.Format32bppArgb);double scale=Math.Min((double)width/source.Width,(double)height/source.Height);
   using(var image=Resize(source,Math.Max(1,(int)Math.Round(source.Width*scale)),Math.Max(1,(int)Math.Round(source.Height*scale))))using(var g=Graphics.FromImage(result)){g.CompositingMode=CompositingMode.SourceCopy;g.DrawImageUnscaled(image,(width-image.Width)/2,(height-image.Height)/2);}
   return result;
  }
  public static Size ModelSize(Bitmap source,double scale){double factor=Math.Min(1,4096.0/Math.Max(source.Width*scale,source.Height*scale));return new Size(Math.Max(2,(int)Math.Round(source.Width*scale*factor)),Math.Max(2,(int)Math.Round(source.Height*scale*factor)));}
  // Sampling outside a bitmap must use its edge colours, not transparent black.
  // An unchanged size is a pixel copy, including the alpha channel.
  public static Bitmap Resize(Bitmap source,int width,int height){
   if(source.Width==width&&source.Height==height)
    return source.Clone(new Rectangle(0,0,width,height),PixelFormat.Format32bppArgb);
   var result=new Bitmap(width,height,PixelFormat.Format32bppArgb);
   using(var g=Graphics.FromImage(result))using(var attributes=new ImageAttributes()){
    g.CompositingMode=CompositingMode.SourceCopy;
    g.InterpolationMode=InterpolationMode.HighQualityBicubic;
    g.PixelOffsetMode=PixelOffsetMode.HighQuality;
    attributes.SetWrapMode(WrapMode.TileFlipXY);
    g.DrawImage(source,new Rectangle(0,0,width,height),0,0,source.Width,source.Height,GraphicsUnit.Pixel,attributes);
   }
   return result;
  }

  // Match only the two horizontal edge bands. Interior details and the vertical
  // colour layout stay intact. Blend premultiplied colours to preserve cutouts.
  public static void JoinHorizontalEdges(Bitmap image){
   int width=image.Width,height=image.Height;
   if(width<2)return;
   int band=Math.Min(width/2,Math.Max(2,Math.Min(32,(int)Math.Round(width*.08))));
   var bits=image.LockBits(new Rectangle(0,0,width,height),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
   try{
    var row=new byte[width*4];
    for(int y=0;y<height;y++){
     IntPtr address=IntPtr.Add(bits.Scan0,y*bits.Stride);
     Marshal.Copy(address,row,0,row.Length);
     for(int d=0;d<band;d++){
      double t=band==1?1:1-(double)d/(band-1),mix=.5*t*t*(3-2*t);
      int left=d*4,right=(width-1-d)*4;
      double a=row[left+3],b=row[right+3],la=a*(1-mix)+b*mix,ra=b*(1-mix)+a*mix;
      for(int c=0;c<3;c++){
       double l=row[left+c]*a,r=row[right+c]*b;
       row[left+c]=Byte(la>0?(l*(1-mix)+r*mix)/la:0);
       row[right+c]=Byte(ra>0?(r*(1-mix)+l*mix)/ra:0);
      }
      row[left+3]=Byte(la);row[right+3]=Byte(ra);
     }
     Marshal.Copy(row,0,address,row.Length);
    }
   }finally{image.UnlockBits(bits);}
  }
  static byte Byte(double value){return (byte)Math.Max(0,Math.Min(255,(int)Math.Round(value)));}

  // One brush covers the whole beam. Drawing separate scaled tile rectangles
  // introduces transparent edges and fractional gaps, especially when rotated.
  public static void DrawStrip(Graphics g,Bitmap image,RectangleF bounds){
   if(bounds.Width<=0||bounds.Height<=0)return;
   // Mirror vertically so filtering cannot mix the top and bottom edge colours.
   using(var brush=new TextureBrush(image,WrapMode.TileFlipY))using(var transform=new Matrix()){
    float scale=bounds.Height/image.Height;
    transform.Scale(scale,scale);
    transform.Translate(bounds.X,bounds.Y,MatrixOrder.Append);
    brush.Transform=transform;
    g.FillRectangle(brush,bounds);
   }
  }
 }
}
