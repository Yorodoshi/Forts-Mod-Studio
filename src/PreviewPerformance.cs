using System;using System.Collections.Generic;using System.Diagnostics;using System.Drawing;using System.Drawing.Drawing2D;using System.Drawing.Imaging;
namespace FortsStudio {
 public partial class Preview {
  // Static geometry is rebuilt after an edit, rather than rediscovered by every animation frame.
  class SceneBeam {
   public Segment Segment;public Entity Material;public float X1,Y1,X2,Y2;public int Team;public string Key,Texture,Template;public double Thickness;public bool Seamless,Behind,Attach;
   public SceneBeam(Segment s,Entity m){Segment=s;Material=m;X1=s.X1;Y1=s.Y1;X2=s.X2;Y2=s.Y2;Team=s.Team;Key=s.Material;Texture=m==null?null:m.Texture;Template=m==null?null:m.Template;Thickness=m==null?0:m.Thickness;Seamless=m!=null&&m.SeamlessTexture;Behind=Flag(m,"IsBehindDevices");Attach=Flag(m,"AttachesCladding");}
   public bool SameTopology(Segment s,Entity m){return X1==s.X1&&Y1==s.Y1&&X2==s.X2&&Y2==s.Y2&&Team==s.Team&&Behind==Flag(m,"IsBehindDevices")&&Attach==Flag(m,"AttachesCladding");}
   public bool SameVisual(Segment s,Entity m){return SameTopology(s,m)&&System.Object.ReferenceEquals(Material,m)&&Key==s.Material&&Texture==(m==null?null:m.Texture)&&Template==(m==null?null:m.Template)&&Thickness==(m==null?0:m.Thickness)&&Seamless==(m!=null&&m.SeamlessTexture);}
  }
  class SceneJoint {public PointF Point;public int Team;}
  class GroundBounds {public float Left,Right,Y;public bool Any;}
  class SceneObject {public PlacedItem Item;public Entity Entity;}
  static bool Flag(Entity e,string key){bool value;return e!=null&&e.Flags.TryGetValue(key,out value)&&value;}
  Dictionary<string,Entity> entitiesByKind=new Dictionary<string,Entity>(),entitiesByKey=new Dictionary<string,Entity>();Entity firstWeapon;
  List<SceneBeam> sceneBeams=new List<SceneBeam>();List<SceneJoint> sceneJoints=new List<SceneJoint>();List<SceneObject> sceneObjects=new List<SceneObject>();GroundBounds[] groundBounds={new GroundBounds(),new GroundBounds()};
  bool entityIndexDirty=true,sceneDataDirty=true,staticSceneDirty=true,claddingDirty=true,hasAnimatedModels=false;
  Bitmap skyLayer,wallLayer;Size backgroundSize;string backgroundGame,backgroundName;bool objectSceneDirty=true;
  readonly Font performanceFont=new Font("Segoe UI",8);long previousPaintTick,windowPaintTick;int paintSamples;double measuredFps,paintMilliseconds;

  public void SceneChanged(){ClearObjectSprites();entityIndexDirty=true;sceneDataDirty=true;objectSceneDirty=true;Invalidate();}
  void DiscardSceneCache(){ClearObjectSprites();DiscardWorldTiles();if(skyLayer!=null){skyLayer.Dispose();skyLayer=null;}if(wallLayer!=null){wallLayer.Dispose();wallLayer=null;}sceneBeams.Clear();sceneJoints.Clear();sceneObjects.Clear();claddingFaces.Clear();groundBounds=new[]{new GroundBounds(),new GroundBounds()};stock.Clear();staticSceneDirty=true;claddingDirty=true;entityIndexDirty=true;sceneDataDirty=true;objectSceneDirty=true;hasAnimatedModels=false;}
  void EnsureProjectState(){if(System.Object.ReferenceEquals(cameraProject,Project))return;cameraProject=Project;undo.Clear();redo.Clear();start=null;DiscardSceneCache();Reset();Fit();}
  void EnsureEntityIndex(){if(!entityIndexDirty)return;entityIndexDirty=false;entitiesByKind.Clear();entitiesByKey.Clear();firstWeapon=null;if(Project==null)return;foreach(var entity in Project.Entities){string key=entity.Key??"",kindKey=entity.Kind+"\0"+key;if(!entitiesByKind.ContainsKey(kindKey))entitiesByKind.Add(kindKey,entity);if(!entitiesByKey.ContainsKey(key))entitiesByKey.Add(key,entity);if(firstWeapon==null&&entity.Kind=="weapons")firstWeapon=entity;}}
  void EnsureSceneData(){if(!sceneDataDirty||Project==null)return;sceneDataDirty=false;EnsureEntityIndex();bool geometryChanged=sceneBeams.Count!=Project.Scene.Count,visualChanged=geometryChanged;for(int i=0;i<Project.Scene.Count;i++){var segment=Project.Scene[i];var material=Resolve(segment.Material);if(i>=sceneBeams.Count){geometryChanged=true;visualChanged=true;continue;}if(!sceneBeams[i].SameTopology(segment,material))geometryChanged=true;if(!sceneBeams[i].SameVisual(segment,material))visualChanged=true;}
   if(visualChanged){sceneBeams.Clear();foreach(var segment in Project.Scene)sceneBeams.Add(new SceneBeam(segment,Resolve(segment.Material)));staticSceneDirty=true;}
   // Even an identical replacement list must render the new segment instances.
   else for(int i=0;i<sceneBeams.Count;i++)sceneBeams[i].Segment=Project.Scene[i];
   if(geometryChanged){claddingDirty=true;sceneJoints.Clear();groundBounds=new[]{new GroundBounds(),new GroundBounds()};var seen=new HashSet<string>();foreach(var beam in sceneBeams){var points=new[]{new PointF(beam.X1,beam.Y1),new PointF(beam.X2,beam.Y2)};foreach(var point in points){if(seen.Add(beam.Team+":"+PointKey(point)))sceneJoints.Add(new SceneJoint{Point=point,Team=beam.Team});if(beam.Team<1||beam.Team>2)continue;var bounds=groundBounds[beam.Team-1];if(!bounds.Any){bounds.Left=bounds.Right=point.X;bounds.Y=point.Y;bounds.Any=true;}else{bounds.Left=Math.Min(bounds.Left,point.X);bounds.Right=Math.Max(bounds.Right,point.X);bounds.Y=Math.Max(bounds.Y,point.Y);}}}}
   sceneObjects.Clear();hasAnimatedModels=false;foreach(var item in Project.Placed){Entity entity;if(!entitiesByKey.TryGetValue(item.Entity??"",out entity))continue;sceneObjects.Add(new SceneObject{Item=item,Entity=entity});if(IsAnimatedObject(entity))hasAnimatedModels=true;}if(Selected!=null&&IsAnimatedObject(Selected))hasAnimatedModels=true;
  }
  bool IsAnimatedObject(Entity entity){if(entity.Kind!="devices"&&entity.Kind!="weapons")return false;if(entity.Kind=="devices"&&(entity.BuildStages.Count>0||entity.Texture.Length>0))return false;if(entity.Art!=null&&entity.Art.Enabled&&Assets.Image(entity.Art.BaseImage,Project.GameRoot)!=null&&Assets.Image(entity.Art.HeadImage,Project.GameRoot)!=null)return false;return NativeModels.IsAnimated(entity,Project.GameRoot);}
  bool AnimationActive(){return !paused&&(shots.Count>0||bursts.Count>0||sim-lastShot<.4||hasAnimatedModels);}
  bool VisibleLine(PointF a,PointF b,float margin){return Math.Max(a.X,b.X)>=-margin&&Math.Min(a.X,b.X)<=ScenePaintWidth+margin&&Math.Max(a.Y,b.Y)>=-margin&&Math.Min(a.Y,b.Y)<=ScenePaintHeight+margin;}
  void DrawPerformance(Graphics g,long started){long now=Stopwatch.GetTimestamp();paintMilliseconds=(now-started)*1000.0/Stopwatch.Frequency;if(previousPaintTick==0||(now-previousPaintTick)/(double)Stopwatch.Frequency>.5){windowPaintTick=now;paintSamples=0;measuredFps=0;}previousPaintTick=now;paintSamples++;double seconds=(now-windowPaintTick)/(double)Stopwatch.Frequency;if(seconds>=.5){measuredFps=(paintSamples-1)/seconds;windowPaintTick=now;paintSamples=1;}string activity=paused?UiText.Get("пауза"):measuredFps>0?measuredFps.ToString("0")+UiText.Get(" кадров/с"):AnimationActive()?UiText.Get("анимация"):UiText.Get("по запросу");string text=UiText.Get("Кадр: ")+paintMilliseconds.ToString("0.0")+UiText.Get(" мс · ")+activity;SizeF size=g.MeasureString(text,performanceFont);float x=Math.Max(12,Width-size.Width-18),y=78;using(var brush=new SolidBrush(Color.FromArgb(215,17,25,32)))g.FillRectangle(brush,x-5,y-3,size.Width+10,size.Height+6);g.DrawString(text,performanceFont,Brushes.LightGray,x,y);DrawFrameDetails(g);}
 }
}
