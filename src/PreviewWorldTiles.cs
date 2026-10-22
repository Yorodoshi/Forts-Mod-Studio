using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Drawing.Imaging;using System.Linq;
namespace FortsStudio {
 public partial class Preview {
  // Tiles are addressed in projected world pixels. Following a projectile only changes
  // their destination rectangles; the scene is painted again only for newly exposed tiles.
  const int WorldTileSize=512,WorldTileBleed=2;
  const long WorldTileMinimumBudget=48L*1024*1024,WorldTileMaximumBudget=256L*1024*1024;
  struct WorldTileKey:IEquatable<WorldTileKey> {
   public int X,Y;public bool Objects;
   public bool Equals(WorldTileKey other){return X==other.X&&Y==other.Y&&Objects==other.Objects;}
   public override bool Equals(object other){return other is WorldTileKey&&Equals((WorldTileKey)other);}
   public override int GetHashCode(){unchecked{return (X*397^Y)*397^(Objects?1:0);}}
  }
  class WorldTile {public Bitmap Image;public LinkedListNode<WorldTileKey> Node;}
  readonly Dictionary<WorldTileKey,WorldTile> worldTiles=new Dictionary<WorldTileKey,WorldTile>();
  readonly LinkedList<WorldTileKey> worldTileLru=new LinkedList<WorldTileKey>();
  readonly HashSet<WorldTileKey> worldTileFramePins=new HashSet<WorldTileKey>();
  readonly List<RectangleF> geometryCoverage=new List<RectangleF>(),objectCoverage=new List<RectangleF>();
  readonly Dictionary<Entity,RectangleF> objectLocalBounds=new Dictionary<Entity,RectangleF>();
  RectangleF geometryWorldBounds,objectWorldBounds;
  long worldTileBytes,worldTileBudget=WorldTileMinimumBudget;bool worldTileSettingsReady,paintingWorldTile,objectsWereMoving,worldTileAdmissionBlocked;
  float worldTileZoom,worldTileUnit,worldTileDpiX,worldTileDpiY;string worldTileGame,geometryBackground;
  bool geometryGround,objectBoundsOption;double objectAim;int objectProgress;Entity objectSelected;
  int ScenePaintWidth {get{return paintingWorldTile?WorldTileSize+2*WorldTileBleed:Width;}}
  int ScenePaintHeight {get{return paintingWorldTile?WorldTileSize+2*WorldTileBleed:Height;}}

  void RemoveWorldTile(WorldTileKey key){WorldTile tile;if(!worldTiles.TryGetValue(key,out tile))return;worldTiles.Remove(key);worldTileLru.Remove(tile.Node);worldTileFramePins.Remove(key);worldTileBytes-=(long)tile.Image.Width*tile.Image.Height*4;tile.Image.Dispose();}
  void ClearWorldTiles(bool? objects){worldTileAdmissionBlocked=false;if(!objects.HasValue){foreach(var tile in worldTiles.Values)tile.Image.Dispose();worldTiles.Clear();worldTileLru.Clear();worldTileFramePins.Clear();worldTileBytes=0;return;}var remove=new List<WorldTileKey>();foreach(var key in worldTiles.Keys)if(key.Objects==objects.Value)remove.Add(key);foreach(var key in remove)RemoveWorldTile(key);}
  void DiscardWorldTiles(){ClearWorldTiles(null);worldTileSettingsReady=false;geometryCoverage.Clear();objectCoverage.Clear();objectLocalBounds.Clear();geometryWorldBounds=objectWorldBounds=RectangleF.Empty;objectsWereMoving=false;}
  void EnsureWorldTileSettings(Graphics target){
   if(worldTileSettingsReady&&worldTileZoom==zoom&&worldTileUnit==Unit&&worldTileDpiX==target.DpiX&&worldTileDpiY==target.DpiY&&worldTileGame==Project.GameRoot)return;
   ClearWorldTiles(null);staticSceneDirty=true;objectSceneDirty=true;worldTileSettingsReady=true;worldTileZoom=zoom;worldTileUnit=Unit;worldTileDpiX=target.DpiX;worldTileDpiY=target.DpiY;worldTileGame=Project.GameRoot;
  }
  void DrawStaticScene(Graphics target){
   EnsureWorldTileSettings(target);var options=Project.Preview;
   if(geometryGround!=options.RangeGrounded||geometryBackground!=options.Background)staticSceneDirty=true;
   if(staticSceneDirty){ClearWorldTiles(false);BuildGeometryCoverage();geometryGround=options.RangeGrounded;geometryBackground=options.Background;staticSceneDirty=false;}
   bool objectsMoving=PrepareObjectWorldCache();BeginWorldTileFrame(objectsMoving);DrawViewportBackground(target);DrawWorldTiles(target,false,geometryCoverage,geometryWorldBounds);
  }
  bool PrepareObjectWorldCache(){
   var options=Project.Preview;
   if(objectSceneDirty||objectSelected!=Selected||objectAim!=options.AimDegrees||objectBoundsOption!=options.Bounds||objectProgress!=BuildProgress){
    ClearWorldTiles(true);BuildObjectCoverage();objectSelected=Selected;objectAim=options.AimDegrees;objectBoundsOption=options.Bounds;objectProgress=BuildProgress;objectSceneDirty=false;
   }
   bool moving=hasAnimatedModels||sim-lastShot<.4;if(moving!=objectsWereMoving){ClearWorldTiles(true);objectsWereMoving=moving;}return moving;
  }
  void DrawObjects(Graphics target){
   EnsureWorldTileSettings(target);if(PrepareObjectWorldCache()){PaintObjects(target);return;}
   DrawWorldTiles(target,true,objectCoverage,objectWorldBounds);
  }
  void BeginWorldTileFrame(bool objectsMoving){
   int size=WorldTileSize+2*WorldTileBleed;long tileBytes=(long)size*size*4;
   long columns=((long)Width+WorldTileSize-1)/WorldTileSize+1,rows=((long)Height+WorldTileSize-1)/WorldTileSize+1;
   int layers=(geometryCoverage.Count>0?1:0)+(!objectsMoving&&objectCoverage.Count>0?1:0);
   worldTileBudget=(long)Math.Min(WorldTileMaximumBudget,Math.Max(WorldTileMinimumBudget,columns*(double)rows*layers*tileBytes));
   worldTileFramePins.Clear();worldTileAdmissionBlocked=false;var viewport=new RectangleF(-(int)Math.Round(camera.X),-(int)Math.Round(camera.Y),Width,Height);
   // Pin both layers before either is painted. A large geometry layer must not
   // evict object tiles that will be used later in this same frame.
   foreach(var key in worldTiles.Keys)if((!key.Objects||!objectsMoving)&&new RectangleF((float)key.X*WorldTileSize,(float)key.Y*WorldTileSize,WorldTileSize,WorldTileSize).IntersectsWith(viewport))worldTileFramePins.Add(key);
   MakeWorldTileRoom(0);
  }
  bool MakeWorldTileRoom(long bytes){
   if(worldTileBytes+bytes<=worldTileBudget)return true;if(worldTileAdmissionBlocked)return false;
   while(worldTileBytes+bytes>worldTileBudget){var candidate=worldTileLru.First;while(candidate!=null&&worldTileFramePins.Contains(candidate.Value))candidate=candidate.Next;if(candidate==null){worldTileAdmissionBlocked=true;return false;}RemoveWorldTile(candidate.Value);}return true;
  }
  static bool FiniteRect(RectangleF bounds){return !Single.IsNaN(bounds.Left)&&!Single.IsInfinity(bounds.Left)&&!Single.IsNaN(bounds.Top)&&!Single.IsInfinity(bounds.Top)&&!Single.IsNaN(bounds.Right)&&!Single.IsInfinity(bounds.Right)&&!Single.IsNaN(bounds.Bottom)&&!Single.IsInfinity(bounds.Bottom);}
  static void AddCoverage(List<RectangleF> coverage,ref RectangleF total,RectangleF bounds){if(bounds.Width<=0||bounds.Height<=0||!FiniteRect(bounds))return;total=coverage.Count==0?bounds:RectangleF.Union(total,bounds);coverage.Add(bounds);}
  PointF ProjectedWorld(PointF point){float cell=Unit*zoom;return new PointF(point.X*cell,point.Y*cell);}
  static RectangleF LineBounds(PointF a,PointF b,float margin){return RectangleF.FromLTRB(Math.Min(a.X,b.X)-margin,Math.Min(a.Y,b.Y)-margin,Math.Max(a.X,b.X)+margin,Math.Max(a.Y,b.Y)+margin);}
  void BuildGeometryCoverage(){
   geometryCoverage.Clear();geometryWorldBounds=RectangleF.Empty;
   foreach(var beam in sceneBeams){float margin=(float)(beam.Material==null?32:MaterialVisuals.WorldHeight(beam.Material))*zoom/2+5;AddCoverage(geometryCoverage,ref geometryWorldBounds,LineBounds(ProjectedWorld(new PointF(beam.X1,beam.Y1)),ProjectedWorld(new PointF(beam.X2,beam.Y2)),margin));}
   BuildCladding();foreach(var polygon in claddingFaces){if(polygon.Length==0)continue;var p=ProjectedWorld(polygon[0]);var bounds=new RectangleF(p.X,p.Y,0,0);foreach(var point in polygon){p=ProjectedWorld(point);bounds=RectangleF.FromLTRB(Math.Min(bounds.Left,p.X),Math.Min(bounds.Top,p.Y),Math.Max(bounds.Right,p.X),Math.Max(bounds.Bottom,p.Y));}bounds.Inflate(2,2);AddCoverage(geometryCoverage,ref geometryWorldBounds,bounds);}
   var radii=new Dictionary<int,SizeF>();foreach(var node in sceneJoints){SizeF radius;if(!radii.TryGetValue(node.Team,out radius)){var image=Assets.Image("materials/node_team"+node.Team+".tga",Project.GameRoot);radius=image==null?new SizeF(8*zoom+2,8*zoom+2):new SizeF(image.Width*.25f*zoom+2,image.Height*.25f*zoom+2);radii.Add(node.Team,radius);}var pt=ProjectedWorld(node.Point);AddCoverage(geometryCoverage,ref geometryWorldBounds,new RectangleF(pt.X-radius.Width,pt.Y-radius.Height,2*radius.Width,2*radius.Height));}
   if(Project.Preview.RangeGrounded)foreach(var ground in groundBounds){if(!ground.Any)continue;var p=ProjectedWorld(new PointF(ground.Left,ground.Y));var q=ProjectedWorld(new PointF(ground.Right,ground.Y));float margin=Math.Max(2,5*zoom)/2+2;AddCoverage(geometryCoverage,ref geometryWorldBounds,RectangleF.FromLTRB(p.X-15*zoom-margin,p.Y-margin,q.X+15*zoom+margin,p.Y+55*zoom+margin));}
  }
  void BuildObjectCoverage(){
   objectCoverage.Clear();objectLocalBounds.Clear();objectWorldBounds=RectangleF.Empty;bool selectedPlaced=false;
   foreach(var entry in sceneObjects){var item=entry.Item;var entity=entry.Entity;if(Selected!=null&&item.Entity==Selected.Key)selectedPlaced=true;var bounds=ObjectBounds(entity,Selected==entity,item.Flipped);var p=ProjectedWorld(new PointF(item.X,item.Y));bounds.Offset(p);AddCoverage(objectCoverage,ref objectWorldBounds,bounds);}
   if(Selected==null||selectedPlaced)return;var origin=ProjectedWorld(new PointF(4,4));RectangleF selectedBounds;
   if(Selected.Kind=="devices"||Selected.Kind=="weapons")selectedBounds=ObjectBounds(Selected,true,false);
   else if(Selected.Kind=="projectiles"){var image=Assets.Body(Selected,Project.GameRoot);if(image==null)return;var size=ProjectileImageSize(Selected,image);selectedBounds=new RectangleF(0,-size.Height/2,size.Width,size.Height);selectedBounds.Inflate(2,2);}
   else return;selectedBounds.Offset(origin);AddCoverage(objectCoverage,ref objectWorldBounds,selectedBounds);
  }
  RectangleF ObjectBounds(Entity entity,bool selected,bool flip){
   RectangleF bounds;if(!objectLocalBounds.TryGetValue(entity,out bounds)){bounds=MeasureObjectBounds(entity);objectLocalBounds.Add(entity,bounds);}
   if(selected&&Project.Preview.Bounds){float w=(float)entity.V("SelectionWidth",90)*zoom,h=(float)entity.V("SelectionHeight",60)*zoom,x=(float)entity.V("SelectionOffsetX",0)*zoom-w/2,y=(float)entity.V("SelectionOffsetY",-40)*zoom-h/2;var frame=RectangleF.FromLTRB(x-2,y-30,Math.Max(x+w+2,x+Math.Max(140,w)),y+h+2);bounds=bounds.IsEmpty?frame:RectangleF.Union(bounds,frame);}
   if(flip)bounds=new RectangleF(-bounds.Right,bounds.Y,bounds.Width,bounds.Height);return bounds;
  }
  RectangleF MeasureObjectBounds(Entity entity){
   float scale=(float)Math.Abs(entity.V("Scale",1))*zoom;RectangleF bounds=RectangleF.Empty;
   if(entity.Kind=="devices"&&(entity.BuildStages.Count>0||entity.Texture.Length>0)){
    var first=entity.BuildStages.OrderBy(x=>x.Progress).FirstOrDefault();var image=Assets.Image(first!=null&&first.Image.Length>0?first.Image:entity.Texture,Project.GameRoot)??Assets.Body(entity,Project.GameRoot);
    if(image!=null){var size=Raster.ModelSize(image,entity.V("Scale",1));bounds=new RectangleF(-size.Width*zoom*.25f,-size.Height*zoom*.5f,size.Width*zoom*.5f,size.Height*zoom*.5f);}
   }else if(entity.Art!=null&&entity.Art.Enabled&&Assets.Image(entity.Art.BaseImage,Project.GameRoot)!=null&&Assets.Image(entity.Art.HeadImage,Project.GameRoot)!=null){
    var art=entity.Art;bounds=new RectangleF((float)-art.BaseWidth*scale/2,(float)-art.BaseHeight*scale,(float)art.BaseWidth*scale,(float)art.BaseHeight*scale);
    if(BuildProgress>=50){double angle=(entity.Kind=="weapons"?-Project.Preview.AimDegrees:0)*Math.PI/180,c=Math.Cos(angle),s=Math.Sin(angle);float left=Single.MaxValue,top=Single.MaxValue,right=Single.MinValue,bottom=Single.MinValue;
     // Include the full recoil travel so the same bounds are valid during a shot.
     foreach(double recoil in new[]{0.0,Math.Max(0,art.RecoilDistance)})foreach(double x in new[]{-art.PivotX*art.HeadWidth-recoil,(1-art.PivotX)*art.HeadWidth-recoil})foreach(double y in new[]{-art.PivotY*art.HeadHeight,(1-art.PivotY)*art.HeadHeight}){float px=(float)(art.MountX+x*c-y*s)*scale,py=(float)(-art.MountY+x*s+y*c)*scale;left=Math.Min(left,px);right=Math.Max(right,px);top=Math.Min(top,py);bottom=Math.Max(bottom,py);}bounds=RectangleF.Union(bounds,RectangleF.FromLTRB(left,top,right,bottom));float bearing=6*scale;bounds=RectangleF.Union(bounds,new RectangleF((float)art.MountX*scale-bearing,(float)-art.MountY*scale-bearing,2*bearing,2*bearing));}
   }else if(!NativeModels.TryAnimationBounds(entity,Project.GameRoot,(float)entity.V("Scale",1)*zoom,entity.Kind=="weapons"?Project.Preview.AimDegrees:0,BuildProgress,out bounds)){
    var image=Assets.Body(entity,Project.GameRoot);if(image!=null)bounds=new RectangleF(-image.Width*scale/2,-image.Height*scale,image.Width*scale,image.Height*scale);
   }
   if(!bounds.IsEmpty)bounds.Inflate(3,3);return bounds;
  }
  bool VisibleObject(Entity entity,PointF pos,bool selected,bool flip){var bounds=ObjectBounds(entity,selected,flip);bounds.Offset(pos);return bounds.Right>=0&&bounds.Left<=ScenePaintWidth&&bounds.Bottom>=0&&bounds.Top<=ScenePaintHeight;}
  static bool CoveredTile(List<RectangleF> coverage,RectangleF tile){foreach(var bounds in coverage)if(bounds.IntersectsWith(tile))return true;return false;}
  void DrawWorldTiles(Graphics target,bool objects,List<RectangleF> coverage,RectangleF bounds){
   if(coverage.Count==0)return;int cameraX=(int)Math.Round(camera.X),cameraY=(int)Math.Round(camera.Y);var viewport=new RectangleF(-cameraX,-cameraY,Width,Height);if(!bounds.IntersectsWith(viewport))return;var visible=RectangleF.Intersect(viewport,bounds);
   int firstX=(int)Math.Floor(visible.Left/WorldTileSize),lastX=(int)Math.Ceiling(visible.Right/WorldTileSize)-1,firstY=(int)Math.Floor(visible.Top/WorldTileSize),lastY=(int)Math.Ceiling(visible.Bottom/WorldTileSize)-1;
   var interpolation=target.InterpolationMode;var pixelOffset=target.PixelOffsetMode;
   try{target.InterpolationMode=InterpolationMode.NearestNeighbor;target.PixelOffsetMode=PixelOffsetMode.Half;
    for(int y=firstY;y<=lastY;y++)for(int x=firstX;x<=lastX;x++){
     var projected=new RectangleF((float)x*WorldTileSize,(float)y*WorldTileSize,WorldTileSize,WorldTileSize);var padded=projected;padded.Inflate(WorldTileBleed,WorldTileBleed);if(!CoveredTile(coverage,padded))continue;
     var key=new WorldTileKey{X=x,Y=y,Objects=objects};WorldTile tile;if(!worldTiles.TryGetValue(key,out tile))tile=PaintWorldTile(key);else{worldTileLru.Remove(tile.Node);worldTileLru.AddLast(tile.Node);}
     try{var destination=new Rectangle(cameraX+x*WorldTileSize,cameraY+y*WorldTileSize,WorldTileSize,WorldTileSize);target.DrawImage(tile.Image,destination,WorldTileBleed,WorldTileBleed,WorldTileSize,WorldTileSize,GraphicsUnit.Pixel);}finally{if(tile.Node==null)tile.Image.Dispose();}
    }
   }finally{target.InterpolationMode=interpolation;target.PixelOffsetMode=pixelOffset;}
  }
  WorldTile PaintWorldTile(WorldTileKey key){
   int size=WorldTileSize+2*WorldTileBleed;long bytes=(long)size*size*4;bool retain=MakeWorldTileRoom(bytes);
   var image=new Bitmap(size,size,PixelFormat.Format32bppPArgb);var savedCamera=camera;bool savedPainting=paintingWorldTile;
   try{image.SetResolution(worldTileDpiX,worldTileDpiY);camera=new PointF(WorldTileBleed-(float)key.X*WorldTileSize,WorldTileBleed-(float)key.Y*WorldTileSize);paintingWorldTile=true;
    using(var g=Graphics.FromImage(image)){g.Clear(Color.Transparent);g.SetClip(new Rectangle(0,0,size,size));g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;
     if(key.Objects)PaintObjects(g);else{Ground(g);foreach(var beam in sceneBeams)if(beam.Behind)Beam(g,beam.Segment,beam.Material);Cladding(g);foreach(var beam in sceneBeams)if(!beam.Behind)Beam(g,beam.Segment,beam.Material);Joints(g);}
    }
   }catch{image.Dispose();throw;}finally{camera=savedCamera;paintingWorldTile=savedPainting;}
   var tile=new WorldTile{Image=image};if(retain){tile.Node=worldTileLru.AddLast(key);worldTiles.Add(key,tile);worldTileFramePins.Add(key);worldTileBytes+=bytes;}return tile;
  }
 }
}
