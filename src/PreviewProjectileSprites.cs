using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;

namespace FortsStudio {
 public partial class Preview {
  const int ProjectileAngleCount=360;
  const int ProjectileSpritePadding=2;
  const int ProjectileAtlasMaximumCell=256;
  const int ProjectilePreparedMaximumDimension=1024;
  const int ProjectileMaximumVariants=24;
  const long ProjectileSpriteBudget=32L*1024*1024;
  const long ProjectileAtlasPageBudget=1024L*1024;

  // Keys do not keep imported PNGs, or Raster.Tight's derived sources, alive.
  // The LRU owns only the much smaller prepared pixels and disposes them itself.
  ConditionalWeakTable<Bitmap,ProjectileSpriteSource> projectileSpriteSources=new ConditionalWeakTable<Bitmap,ProjectileSpriteSource>();
  readonly LinkedList<ProjectileSpriteVariant> projectileSpriteVariants=new LinkedList<ProjectileSpriteVariant>();
  readonly LinkedList<ProjectileSpritePage> projectileSpritePages=new LinkedList<ProjectileSpritePage>();
  long projectileSpriteBytes;

  sealed class ProjectileSpriteSource {
   internal readonly WeakReference Original;
   internal readonly Dictionary<SizeF,ProjectileSpriteVariant> Variants=new Dictionary<SizeF,ProjectileSpriteVariant>();
   internal ProjectileSpriteSource(Bitmap source){Original=new WeakReference(source);}
  }
  sealed class ProjectileSpriteVariant {
   internal ProjectileSpriteSource Owner;
   internal SizeF PreparedSize;
   internal Bitmap Prepared;
   internal int CellSize,CellsPerPage,Columns;
   internal LinkedListNode<ProjectileSpriteVariant> Node;
   internal readonly Dictionary<int,ProjectileSpritePage> Pages=new Dictionary<int,ProjectileSpritePage>();
  }
  sealed class ProjectileSpritePage {
   internal ProjectileSpriteVariant Owner;
   internal Bitmap Pixels;
   internal int Group,Ready;
   internal LinkedListNode<ProjectileSpritePage> Node;
  }

  void DrawProjectileSprite(Graphics g,Entity projectile,PointF screen,PointF velocity){
   if(projectile==null||Project==null||!ProjectileFinite(screen.X)||!ProjectileFinite(screen.Y))return;
   var source=Assets.Body(projectile,Project.GameRoot);
   if(source==null){if(VisibleLine(screen,screen,3))g.FillEllipse(Brushes.Wheat,screen.X-2,screen.Y-2,4,4);return;}
   // Keep the existing custom-image/native-image world-size rules. Reject or
   // cull before allocating prepared pixels or generating an angle cell.
   var size=ProjectileImageSize(projectile,source);
   if(!ProjectileFinite(size.Width)||!ProjectileFinite(size.Height)||size.Width<=0||size.Height<=0)return;
   double radius=Math.Sqrt((double)size.Width*size.Width+(double)size.Height*size.Height)*.5+4;
   if(screen.X+radius<0||screen.X-radius>ScenePaintWidth||screen.Y+radius<0||screen.Y-radius>ScenePaintHeight)return;
   float angle=ProjectileFinite(velocity.X)&&ProjectileFinite(velocity.Y)?(float)(Math.Atan2(velocity.Y,velocity.X)*180/Math.PI):0;
   var variant=PreparedProjectileSprite(source,size);
   if(variant==null)return;
   if(variant.CellSize>ProjectileAtlasMaximumCell){DrawLargeProjectileSprite(g,variant,size,screen,angle);return;}
   int bucket=(int)Math.Round(angle*ProjectileAngleCount/360.0);
   bucket=(bucket%ProjectileAngleCount+ProjectileAngleCount)%ProjectileAngleCount;
   var page=ProjectileAnglePage(variant,bucket);
   if(page==null){DrawLargeProjectileSprite(g,variant,size,screen,angle);return;}
   int cell=bucket%variant.CellsPerPage;
   var rect=new Rectangle(cell%variant.Columns*variant.CellSize,cell/variant.Columns*variant.CellSize,variant.CellSize,variant.CellSize);
   var destination=new Rectangle((int)Math.Round(screen.X-variant.CellSize*.5),(int)Math.Round(screen.Y-variant.CellSize*.5),variant.CellSize,variant.CellSize);
   // Each frame copies a small, already rotated cell at exactly 1:1. Neither
   // the original PNG nor the presized source is filtered in this hot path.
   var state=g.Save();
   try{g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;g.DrawImage(page.Pixels,destination,rect.X,rect.Y,rect.Width,rect.Height,GraphicsUnit.Pixel);}
   finally{g.Restore(state);}
  }

  static bool ProjectileFinite(float value){return !Single.IsNaN(value)&&!Single.IsInfinity(value);}

  ProjectileSpriteVariant PreparedProjectileSprite(Bitmap source,SizeF requested){
   float factor=Math.Min(1,ProjectilePreparedMaximumDimension/Math.Max(requested.Width,requested.Height));
   // Very large sprites use a bounded source and the ordinary transformed
   // draw path. They can never request a giant bitmap or an atlas page.
   var preparedSize=factor<1?new SizeF(Math.Max(1,requested.Width*factor),Math.Max(1,requested.Height*factor)):requested;
   ProjectileSpriteSource owner;
   if(!projectileSpriteSources.TryGetValue(source,out owner)){owner=new ProjectileSpriteSource(source);projectileSpriteSources.Add(source,owner);}
   ProjectileSpriteVariant found;
   if(owner.Variants.TryGetValue(preparedSize,out found)){TouchProjectileVariant(found);return found;}
   PruneDeadProjectileSprites();
   while(projectileSpriteVariants.Count>=ProjectileMaximumVariants)DiscardProjectileVariant(projectileSpriteVariants.First.Value);
   int width=(int)Math.Ceiling(preparedSize.Width)+ProjectileSpritePadding*2;
   int height=(int)Math.Ceiling(preparedSize.Height)+ProjectileSpritePadding*2;
   if(!ReserveProjectileSpriteBytes((long)width*height*4,null))return null;
   Bitmap pixels=new Bitmap(width,height,PixelFormat.Format32bppPArgb);
   try{
    using(var paint=Graphics.FromImage(pixels))using(var attributes=new ImageAttributes()){
     paint.CompositingMode=CompositingMode.SourceCopy;paint.InterpolationMode=InterpolationMode.HighQualityBicubic;paint.PixelOffsetMode=PixelOffsetMode.HighQuality;
     attributes.SetWrapMode(WrapMode.TileFlipXY);
     var target=new RectangleF((width-preparedSize.Width)*.5f,(height-preparedSize.Height)*.5f,preparedSize.Width,preparedSize.Height);
     paint.DrawImage(source,new[]{new PointF(target.Left,target.Top),new PointF(target.Right,target.Top),new PointF(target.Left,target.Bottom)},new RectangleF(0,0,source.Width,source.Height),GraphicsUnit.Pixel,attributes);
    }
   }catch{pixels.Dispose();throw;}
   found=new ProjectileSpriteVariant{Owner=owner,PreparedSize=preparedSize,Prepared=pixels,CellSize=(int)Math.Ceiling(Math.Sqrt((double)width*width+(double)height*height))+2};
   int cells=16;
   while(cells>1&&(long)found.CellSize*found.CellSize*cells*4>ProjectileAtlasPageBudget)cells/=2;
   found.CellsPerPage=cells;found.Columns=Math.Min(4,cells);
   found.Node=projectileSpriteVariants.AddLast(found);owner.Variants.Add(preparedSize,found);
   projectileSpriteBytes+=(long)width*height*4;
   return found;
  }

  ProjectileSpritePage ProjectileAnglePage(ProjectileSpriteVariant variant,int bucket){
   int group=bucket/variant.CellsPerPage,cell=bucket%variant.CellsPerPage;
   ProjectileSpritePage page;
   if(!variant.Pages.TryGetValue(group,out page)){
    int width=variant.Columns*variant.CellSize,height=(variant.CellsPerPage/variant.Columns)*variant.CellSize;
    long bytes=(long)width*height*4;
    if(!ReserveProjectileSpriteBytes(bytes,variant))return null;
    page=new ProjectileSpritePage{Owner=variant,Group=group,Pixels=new Bitmap(width,height,PixelFormat.Format32bppPArgb)};
    page.Node=projectileSpritePages.AddLast(page);variant.Pages.Add(group,page);projectileSpriteBytes+=bytes;
   }else{projectileSpritePages.Remove(page.Node);projectileSpritePages.AddLast(page.Node);}
   if((page.Ready&(1<<cell))==0){
    var rect=new Rectangle(cell%variant.Columns*variant.CellSize,cell/variant.Columns*variant.CellSize,variant.CellSize,variant.CellSize);
    using(var paint=Graphics.FromImage(page.Pixels)){
     paint.SetClip(rect);paint.CompositingMode=CompositingMode.SourceCopy;paint.InterpolationMode=InterpolationMode.HighQualityBicubic;paint.PixelOffsetMode=PixelOffsetMode.HighQuality;
     paint.TranslateTransform(rect.X+rect.Width*.5f,rect.Y+rect.Height*.5f);paint.RotateTransform(bucket*360f/ProjectileAngleCount);
     var image=variant.Prepared;
     paint.DrawImage(image,new RectangleF(-image.Width*.5f,-image.Height*.5f,image.Width,image.Height),new RectangleF(0,0,image.Width,image.Height),GraphicsUnit.Pixel);
    }
    page.Ready|=1<<cell;
   }
   return page;
  }

  void DrawLargeProjectileSprite(Graphics g,ProjectileSpriteVariant variant,SizeF requested,PointF screen,float angle){
   var image=variant.Prepared;
   float width=image.Width*(requested.Width/variant.PreparedSize.Width),height=image.Height*(requested.Height/variant.PreparedSize.Height);
   if(!ProjectileFinite(width)||!ProjectileFinite(height))return;
   var state=g.Save();
   try{
    g.TranslateTransform(screen.X,screen.Y);g.RotateTransform(angle);g.InterpolationMode=InterpolationMode.HighQualityBilinear;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
    g.DrawImage(image,new RectangleF(-width*.5f,-height*.5f,width,height),new RectangleF(0,0,image.Width,image.Height),GraphicsUnit.Pixel);
   }finally{g.Restore(state);}
  }

  void TouchProjectileVariant(ProjectileSpriteVariant variant){projectileSpriteVariants.Remove(variant.Node);projectileSpriteVariants.AddLast(variant.Node);}
  void PruneDeadProjectileSprites(){
   var node=projectileSpriteVariants.First;
   while(node!=null){var next=node.Next;if(!node.Value.Owner.Original.IsAlive)DiscardProjectileVariant(node.Value);node=next;}
  }
  bool ReserveProjectileSpriteBytes(long extra,ProjectileSpriteVariant keep){
   if(extra>ProjectileSpriteBudget)return false;
   while(projectileSpriteBytes+extra>ProjectileSpriteBudget&&projectileSpritePages.First!=null)DiscardProjectilePage(projectileSpritePages.First.Value);
   while(projectileSpriteBytes+extra>ProjectileSpriteBudget){
    var node=projectileSpriteVariants.First;
    while(node!=null&&System.Object.ReferenceEquals(node.Value,keep))node=node.Next;
    if(node==null)return false;DiscardProjectileVariant(node.Value);
   }
   return true;
  }
  void DiscardProjectilePage(ProjectileSpritePage page){
   projectileSpritePages.Remove(page.Node);page.Owner.Pages.Remove(page.Group);
   projectileSpriteBytes-=(long)page.Pixels.Width*page.Pixels.Height*4;page.Pixels.Dispose();
  }
  void DiscardProjectileVariant(ProjectileSpriteVariant variant){
   // Removing a page updates this dictionary, so detach its entries first.
   foreach(var page in new List<ProjectileSpritePage>(variant.Pages.Values))DiscardProjectilePage(page);
   projectileSpriteVariants.Remove(variant.Node);variant.Owner.Variants.Remove(variant.PreparedSize);
   projectileSpriteBytes-=(long)variant.Prepared.Width*variant.Prepared.Height*4;variant.Prepared.Dispose();
  }
  void ClearProjectileSprites(){
   while(projectileSpriteVariants.First!=null)DiscardProjectileVariant(projectileSpriteVariants.First.Value);
   projectileSpriteSources=new ConditionalWeakTable<Bitmap,ProjectileSpriteSource>();projectileSpriteBytes=0;
  }
 }
}
