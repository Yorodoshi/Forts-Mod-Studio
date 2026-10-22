using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Drawing.Imaging;using System.Runtime.CompilerServices;
namespace FortsStudio {
 public partial class Preview {
  const long ObjectSpriteBudget=24L*1024*1024,ObjectSpriteMaximumBytes=4L*1024*1024;
  const int ObjectSpriteMaximumSide=2048;
  struct ObjectSpriteKey:IEquatable<ObjectSpriteKey> {
   public Entity Entity;public bool Selected,Flipped;public int Progress;public double Aim;
   public bool Equals(ObjectSpriteKey other){return System.Object.ReferenceEquals(Entity,other.Entity)&&Selected==other.Selected&&Flipped==other.Flipped&&Progress==other.Progress&&Aim==other.Aim;}
   public override bool Equals(object other){return other is ObjectSpriteKey&&Equals((ObjectSpriteKey)other);}
   public override int GetHashCode(){unchecked{return (((RuntimeHelpers.GetHashCode(Entity)*397^(Selected?1:0))*397^(Flipped?1:0))*397^Progress)*397^Aim.GetHashCode();}}
  }
  class ObjectSprite {public Bitmap Image;public int Left,Top;public LinkedListNode<ObjectSpriteKey> Node;}
  class ObjectSpriteMotion {public bool Animated,Recoil;}
  readonly Dictionary<ObjectSpriteKey,ObjectSprite> objectSprites=new Dictionary<ObjectSpriteKey,ObjectSprite>();
  readonly LinkedList<ObjectSpriteKey> objectSpriteLru=new LinkedList<ObjectSpriteKey>();
  readonly HashSet<ObjectSpriteKey> objectSpriteFramePins=new HashSet<ObjectSpriteKey>();
  readonly Dictionary<Entity,ObjectSpriteMotion> objectSpriteMotion=new Dictionary<Entity,ObjectSpriteMotion>();
  long objectSpriteBytes;bool objectSpriteSettingsReady,objectSpriteAdmissionBlocked;
  float objectSpriteZoom,objectSpriteUnit,objectSpriteDpiX,objectSpriteDpiY;string objectSpriteGame;

  void ClearObjectSprites(){foreach(var sprite in objectSprites.Values)sprite.Image.Dispose();objectSprites.Clear();objectSpriteLru.Clear();objectSpriteFramePins.Clear();objectSpriteMotion.Clear();objectSpriteBytes=0;objectSpriteSettingsReady=false;objectSpriteAdmissionBlocked=false;}
  void EnsureObjectSpriteSettings(Graphics target){
   if(objectSpriteSettingsReady&&objectSpriteZoom==zoom&&objectSpriteUnit==Unit&&objectSpriteDpiX==target.DpiX&&objectSpriteDpiY==target.DpiY&&objectSpriteGame==Project.GameRoot)return;
   ClearObjectSprites();objectSpriteSettingsReady=true;objectSpriteZoom=zoom;objectSpriteUnit=Unit;objectSpriteDpiX=target.DpiX;objectSpriteDpiY=target.DpiY;objectSpriteGame=Project.GameRoot;
  }
  ObjectSpriteKey SceneObjectSpriteKey(Entity entity,bool selected,bool flipped){return new ObjectSpriteKey{Entity=entity,Selected=selected&&Project.Preview.Bounds,Flipped=flipped,Progress=BuildProgress,Aim=entity.Kind=="weapons"?Project.Preview.AimDegrees:0};}
  bool DynamicSceneObject(Entity entity){
   ObjectSpriteMotion motion;if(!objectSpriteMotion.TryGetValue(entity,out motion)){
    motion=new ObjectSpriteMotion();bool stages=entity.Kind=="devices"&&(entity.BuildStages.Count>0||entity.Texture.Length>0);
    if(!stages){var art=entity.Art;bool artwork=art!=null&&art.Enabled&&Assets.Image(art.BaseImage,Project.GameRoot)!=null&&Assets.Image(art.HeadImage,Project.GameRoot)!=null;
     if(artwork)motion.Recoil=true;else{motion.Animated=NativeModels.IsAnimated(entity,Project.GameRoot);motion.Recoil=NativeModels.HasRecoil(entity,Project.GameRoot);}
    }
    objectSpriteMotion.Add(entity,motion);
   }
   return motion.Animated||motion.Recoil&&sim-lastShot<.4;
  }
  // Called once before the ordered PaintObjects loop. Pin both selected/flipped
  // variants that will be used, so an oversized scene keeps a stable cached subset.
  void BeginObjectSpriteFrame(Graphics target){
   if(paintingWorldTile)return;EnsureObjectSpriteSettings(target);objectSpriteFramePins.Clear();objectSpriteAdmissionBlocked=false;bool selectedPlaced=false;
   foreach(var entry in sceneObjects){var item=entry.Item;var entity=entry.Entity;if(Selected!=null&&item.Entity==Selected.Key)selectedPlaced=true;PinObjectSprite(entity,Screen(new PointF(item.X,item.Y)),Selected==entity,item.Flipped);}
   if(Selected!=null&&!selectedPlaced&&(Selected.Kind=="devices"||Selected.Kind=="weapons"))PinObjectSprite(Selected,Screen(new PointF(4,4)),true,false);
  }
  void PinObjectSprite(Entity entity,PointF position,bool selected,bool flip){if(!VisibleObject(entity,position,selected,flip)||DynamicSceneObject(entity))return;var key=SceneObjectSpriteKey(entity,selected,flip);if(objectSprites.ContainsKey(key))objectSpriteFramePins.Add(key);}
  void RemoveObjectSprite(ObjectSpriteKey key){ObjectSprite sprite;if(!objectSprites.TryGetValue(key,out sprite))return;objectSprites.Remove(key);objectSpriteLru.Remove(sprite.Node);objectSpriteFramePins.Remove(key);objectSpriteBytes-=(long)sprite.Image.Width*sprite.Image.Height*4;sprite.Image.Dispose();}
  bool MakeObjectSpriteRoom(long bytes){
   if(objectSpriteBytes+bytes<=ObjectSpriteBudget)return true;if(objectSpriteAdmissionBlocked)return false;
   while(objectSpriteBytes+bytes>ObjectSpriteBudget){var candidate=objectSpriteLru.First;while(candidate!=null&&objectSpriteFramePins.Contains(candidate.Value))candidate=candidate.Next;if(candidate==null){objectSpriteAdmissionBlocked=true;return false;}RemoveObjectSprite(candidate.Value);}return true;
  }
  void DrawSceneObject(Graphics target,Entity entity,PointF position,bool selected,bool flip){
   if(!VisibleObject(entity,position,selected,flip))return;
   if(paintingWorldTile){Object(target,entity,position,selected,flip);return;}
   EnsureObjectSpriteSettings(target);if(DynamicSceneObject(entity)){Object(target,entity,position,selected,flip);return;}
   var key=SceneObjectSpriteKey(entity,selected,flip);ObjectSprite sprite;
   if(objectSprites.TryGetValue(key,out sprite)){objectSpriteLru.Remove(sprite.Node);objectSpriteLru.AddLast(sprite.Node);objectSpriteFramePins.Add(key);}
   else{
    var bounds=ObjectBounds(entity,selected,flip);if(bounds.IsEmpty)return;
    if(!FiniteRect(bounds)){Object(target,entity,position,selected,flip);return;}
    double left=Math.Floor(bounds.Left),top=Math.Floor(bounds.Top),width=Math.Ceiling(bounds.Right)-left,height=Math.Ceiling(bounds.Bottom)-top;
    if(width<=0||height<=0||width>ObjectSpriteMaximumSide||height>ObjectSpriteMaximumSide||width*height*4>ObjectSpriteMaximumBytes||left<Int32.MinValue||left>Int32.MaxValue||top<Int32.MinValue||top>Int32.MaxValue||!MakeObjectSpriteRoom((long)(width*height*4))){Object(target,entity,position,selected,flip);return;}
    var image=new Bitmap((int)width,(int)height,PixelFormat.Format32bppPArgb);
    try{image.SetResolution(target.DpiX,target.DpiY);using(var g=Graphics.FromImage(image)){g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.TranslateTransform(-(float)left,-(float)top);Object(g,entity,PointF.Empty,key.Selected,flip);}}
    catch{image.Dispose();throw;}
    sprite=new ObjectSprite{Image=image,Left=(int)left,Top=(int)top,Node=objectSpriteLru.AddLast(key)};objectSprites.Add(key,sprite);objectSpriteFramePins.Add(key);objectSpriteBytes+=(long)image.Width*image.Height*4;
   }
   target.DrawImageUnscaled(sprite.Image,(int)Math.Round(position.X)+sprite.Left,(int)Math.Round(position.Y)+sprite.Top);
  }
 }
}
