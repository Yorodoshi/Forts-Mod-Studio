using System;using System.Drawing;
namespace FortsStudio {
 public partial class Preview {
  PointF ShotOrigin(Entity weapon,PointF origin,double angle){
   double x=weapon.V("BarrelLength",100),y=0;
   if(weapon.Art!=null&&weapon.Art.Enabled){x=(weapon.Art.MuzzleX-weapon.Art.PivotX)*weapon.Art.HeadWidth;y=(weapon.Art.MuzzleY-weapon.Art.PivotY)*weapon.Art.HeadHeight;}
   return new PointF(origin.X+(float)((x*Math.Cos(angle)+y*Math.Sin(angle))/Unit),origin.Y+(float)((-x*Math.Sin(angle)+y*Math.Cos(angle))/Unit));
  }
  SizeF ProjectileImageSize(Entity e,Bitmap image){
   float width=e.Texture.Length>0?(float)(e.ProjectileImageWidth*e.ProjectileVisualScale*zoom):(float)(image.Width*.5*e.ProjectileVisualScale*zoom);
   return new SizeF(width,width*image.Height/image.Width);
  }
  bool SplitPreview(Shot shot){
   var e=shot.Projectile;
   if(!e.ClusterEnabled||shot.Age<e.ClusterMinimumAge||!(e.ClusterAtApex?shot.Velocity.Y>=0:shot.Age>=e.ClusterDelay))return false;
   var child=Resolve(e.ClusterProjectile,"projectiles");if(child==null)return false;
   int count=Math.Max(1,Math.Min(64,e.ClusterCount));double heading=Math.Atan2(shot.Velocity.Y,shot.Velocity.X);
   for(int i=0;i<count&&shots.Count<600;i++){
    double angle=heading+(count==1?0:((double)i/(count-1)*2-1)*e.ClusterSpread*2);
    var velocity=new PointF(shot.Velocity.X+(float)(Math.Cos(angle)*e.ClusterEjectionSpeed/Unit),shot.Velocity.Y+(float)(Math.Sin(angle)*e.ClusterEjectionSpeed/Unit));
    shots.Add(new Shot{Position=shot.Position,Previous=shot.Position,Velocity=velocity,Life=child.V("MaxAge",30),Projectile=child});
   }
   Notice="Эскиз разделения: "+count+" боевых частей. Попадания и точную физику проверяйте в Forts.";
   shots.Remove(shot);return true;
  }
 }
}
