using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Linq;
namespace FortsStudio {
 public partial class Preview {
  protected override void OnPaint(System.Windows.Forms.PaintEventArgs e){long started=System.Diagnostics.Stopwatch.GetTimestamp(),stage=started;base.OnPaint(e);if(Width<2||Height<2)return;var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;if(Project==null){Background(g);return;}EnsureProjectState();EnsureSceneData();var opt=Project.Preview;prepareMilliseconds=PaintStage(ref stage);DrawStaticScene(g);sceneMilliseconds=PaintStage(ref stage);
   DrawObjects(g);objectMilliseconds=PaintStage(ref stage);
   if(opt.Trajectory&&Weapon()!=null&&Tool=="Прицел")Trajectory(g);
   using(var trail=new Pen(Color.FromArgb(190,255,237,174),Math.Max(1,2*zoom)))foreach(var shot in shots){var a=Screen(shot.Previous);var b=Screen(shot.Position);if(VisibleLine(a,b,3))g.DrawLine(trail,a,b);DrawProjectileSprite(g,shot.Projectile,b,shot.Velocity);}projectileMilliseconds=PaintStage(ref stage);
   if(start!=null){var a=Screen(start.Value);var b=Screen(cursor);double length=Length(Sub(cursor,start.Value))*Unit;var mat=Resolve(Material);bool valid=mat!=null&&length>=mat.V("MinLength",37.5)&&length<=mat.V("MaxLength",150);using(var pen=new Pen(valid?Color.FromArgb(110,243,198):Color.Salmon,3)){pen.DashStyle=DashStyle.Dash;g.DrawLine(pen,a,b);}TextBox(g,length.ToString("0.#")+" ед.",new RectangleF(b.X+12,b.Y-25,100,24),valid?Color.MediumAquamarine:Color.Salmon);}
   if(Tool=="Балка"||Tool=="Прицел"){var p=Screen(cursor);using(var pen=new Pen(Color.FromArgb(210,249,231,171))){g.DrawEllipse(pen,p.X-7,p.Y-7,14,14);g.DrawLine(pen,p.X-12,p.Y,p.X+12,p.Y);g.DrawLine(pen,p.X,p.Y-12,p.X,p.Y+12);}}
   using(var shade=new SolidBrush(Color.FromArgb(215,17,25,32)))g.FillRectangle(shade,10,10,Width-20,62);g.DrawString(UiText.Get("МАКЕТ / ")+UiText.Get(Tool).ToUpperInvariant(),Theme.TextFont(9,true),Brushes.White,22,20);g.DrawString(UiText.Get(Notice),Theme.TextFont(8),Brushes.LightGray,new RectangleF(22,41,Width-45,30));
   using(var shade=new SolidBrush(Color.FromArgb(220,17,25,32)))g.FillRectangle(shade,10,Height-63,Width-20,53);var f=Theme.TextFont(8);g.DrawString(String.Format(UiText.Get("1 клетка = {0} ед.  ·  {1}%  ·  Балок: {2}  ·  Объектов: {3}"),Unit.ToString("0.#"),(zoom*100).ToString("0"),Project.Scene.Count,Project.Placed.Count),f,Brushes.White,20,Height-54);g.DrawString(UiText.Get("Эскиз: без расчёта деформаций и попаданий. Точная физика — в Forts."),f,Brushes.MediumAquamarine,new RectangleF(20,Height-35,Width-40,25));overlayMilliseconds=PaintStage(ref stage);DrawPerformance(g,started);
  }
  void Background(Graphics g){
   string game=Project==null?"":Project.GameRoot,name=Project==null?"":Project.Preview.Background;
   if(skyLayer==null||backgroundSize!=ClientSize||backgroundGame!=game||backgroundName!=name){
    if(skyLayer!=null)skyLayer.Dispose();if(wallLayer!=null){wallLayer.Dispose();wallLayer=null;}
    skyLayer=new Bitmap(Width,Height,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
    using(var skyGraphics=Graphics.FromImage(skyLayer)){
     skyGraphics.InterpolationMode=InterpolationMode.HighQualityBicubic;
     using(var sky=new LinearGradientBrush(ClientRectangle,Color.FromArgb(199,174,105),Color.FromArgb(148,167,160),90))skyGraphics.FillRectangle(sky,ClientRectangle);
     if(Project!=null){string folder="environment/"+name+"/background/";var image=Assets.Image(folder+"skyColour.dds",game);if(image!=null)skyGraphics.DrawImage(image,ClientRectangle);
      image=Assets.Image(folder+"canyonWall2.dds",game)??Assets.Image(folder+"canyonWall.dds",game);
      if(image!=null){int h=Math.Max(1,(int)Math.Round(Height*.72));int w=Math.Max(1,(int)Math.Round(image.Width*(double)h/image.Height));wallLayer=new Bitmap(w,h,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
       using(var wallGraphics=Graphics.FromImage(wallLayer))using(var attributes=new System.Drawing.Imaging.ImageAttributes()){
        wallGraphics.CompositingMode=CompositingMode.SourceCopy;wallGraphics.InterpolationMode=InterpolationMode.HighQualityBicubic;wallGraphics.PixelOffsetMode=PixelOffsetMode.HighQuality;attributes.SetWrapMode(WrapMode.TileFlipXY);
        wallGraphics.DrawImage(image,new Rectangle(0,0,w,h),0,0,image.Width,image.Height,GraphicsUnit.Pixel,attributes);
       }
      }
     }
    }
    backgroundSize=ClientSize;backgroundGame=game;backgroundName=name;
   }
   // All background pixels are prepared once. Camera tracking uses integer
   // copies instead of filtering a viewport-sized layer on every frame.
   var mode=g.CompositingMode;g.CompositingMode=CompositingMode.SourceCopy;g.DrawImageUnscaled(skyLayer,0,0);g.CompositingMode=mode;
   if(wallLayer!=null){int width=wallLayer.Width,offset=(int)Math.Round(camera.X*.035);int first=offset%width;if(first>0)first-=width;for(int x=first;x<Width;x+=width)g.DrawImageUnscaled(wallLayer,x,Height-wallLayer.Height);}
  }
  void DrawViewportBackground(Graphics g){
   Background(g);if(!Project.Preview.Grid)return;float cell=Unit*zoom;if(cell<4||Single.IsNaN(cell)||Single.IsInfinity(cell))return;
   var smooth=g.SmoothingMode;g.SmoothingMode=SmoothingMode.None;
   try{using(var pen=new Pen(Color.FromArgb(32,255,244,209))){float firstX=(float)Math.Round(camera.X)%cell,firstY=(float)Math.Round(camera.Y)%cell;if(firstX>0)firstX-=cell;if(firstY>0)firstY-=cell;for(float x=firstX;x<Width;x+=cell)g.DrawLine(pen,x,0,x,Height);for(float y=firstY;y<Height;y+=cell)g.DrawLine(pen,0,y,Width,y);}}
   finally{g.SmoothingMode=smooth;}
  }
  void Beam(Graphics g,Segment s,Entity material){var a=Screen(new PointF(s.X1,s.Y1));var b=Screen(new PointF(s.X2,s.Y2));float th=(float)(material==null?32:MaterialVisuals.WorldHeight(material))*zoom;if(!VisibleLine(a,b,th/2+4))return;float dx=b.X-a.X,dy=b.Y-a.Y,len=(float)Math.Sqrt(dx*dx+dy*dy);if(len<.5)return;var state=g.Save();g.TranslateTransform(a.X,a.Y);g.RotateTransform((float)(Math.Atan2(dy,dx)*180/Math.PI));using(var shadow=new SolidBrush(Color.FromArgb(130,20,22,24)))g.FillRectangle(shadow,0,-th/2-1,len,th+3);var im=material==null?null:MaterialVisuals.Beam(material,Project.GameRoot);if(im!=null)Raster.DrawStrip(g,im,new RectangleF(0,-th/2,len,th));else using(var brush=new SolidBrush(Color.SaddleBrown))g.FillRectangle(brush,0,-th/2,len,th);g.Restore(state);}

  void PaintObjects(Graphics g){BeginObjectSpriteFrame(g);bool selectedPlaced=false;foreach(var entry in sceneObjects){var item=entry.Item;var obj=entry.Entity;if(Selected!=null&&item.Entity==Selected.Key)selectedPlaced=true;var position=Screen(new PointF(item.X,item.Y));DrawSceneObject(g,obj,position,Selected==obj,item.Flipped);}if(Selected==null||selectedPlaced)return;PointF origin=Screen(new PointF(4,4));if(Selected.Kind=="devices"||Selected.Kind=="weapons"){DrawSceneObject(g,Selected,origin,true,false);}else if(Selected.Kind=="projectiles"){var img=Assets.Body(Selected,Project.GameRoot);if(img!=null){var size=ProjectileImageSize(Selected,img);DrawProjectileSprite(g,Selected,new PointF(origin.X+size.Width/2,origin.Y),new PointF(1,0));}}}
  void Object(Graphics g,Entity obj,PointF pos,bool selected,bool flip){float scale=(float)obj.V("Scale",1)*zoom;double recoil=Math.Max(0,1-(sim-lastShot)/.25);bool stages=obj.Kind=="devices"&&(obj.BuildStages.Count>0||obj.Texture.Length>0);var state=g.Save();if(flip){g.TranslateTransform(pos.X,0);g.ScaleTransform(-1,1);pos.X=0;}
   if(stages){scale*=.5f;var first=obj.BuildStages.OrderBy(x=>x.Progress).FirstOrDefault();var root=Assets.Image(first!=null&&first.Image.Length>0?first.Image:obj.Texture,Project.GameRoot)??Assets.Body(obj,Project.GameRoot);if(root!=null){var dimensions=Raster.ModelSize(root,obj.V("Scale",1));float w=dimensions.Width*zoom*.5f,h=dimensions.Height*zoom*.5f;if(obj.BuildStages.Count==0)g.DrawImage(root,pos.X-w/2,pos.Y-h,w,h);else foreach(var stage in obj.BuildStages.Where(x=>x.Progress<=BuildProgress).OrderBy(x=>x.Progress)){var im=Assets.Image(stage.Image.Length>0?stage.Image:obj.Texture,Project.GameRoot)??root;float fit=Math.Min(w/im.Width,h/im.Height);float iw=im.Width*fit,ih=im.Height*fit;g.DrawImage(im,pos.X-iw/2,pos.Y-h+(h-ih)/2,iw,ih);}}}
   else if(!WeaponArtwork.Draw(g,obj,Project.GameRoot,pos,scale,obj.Kind=="weapons"?Project.Preview.AimDegrees:0,BuildProgress,sim,recoil)&&!NativeModels.Draw(g,obj,Project.GameRoot,pos,scale,obj.Kind=="weapons"?Project.Preview.AimDegrees:0,BuildProgress,sim,recoil)){var im=Assets.Body(obj,Project.GameRoot);if(im!=null)g.DrawImage(im,pos.X-im.Width*scale/2,pos.Y-im.Height*scale,im.Width*scale,im.Height*scale);}
   if(selected&&Project.Preview.Bounds){float w=(float)obj.V("SelectionWidth",90)*zoom,h=(float)obj.V("SelectionHeight",60)*zoom,x=pos.X+(float)obj.V("SelectionOffsetX",0)*zoom-w/2,y=pos.Y+(float)obj.V("SelectionOffsetY",-40)*zoom-h/2;using(var p=new Pen(Color.FromArgb(255,178,96),1.5f)){p.DashStyle=DashStyle.Dash;g.DrawRectangle(p,x,y,w,h);}TextBox(g,UiText.EntityName(obj),new RectangleF(x,y-28,Math.Max(140,w),23),Color.FromArgb(255,205,140));}g.Restore(state);
  }
  void Trajectory(Graphics g){var w=Weapon();double angle=Project.Preview.AimDegrees*Math.PI/180,speed=w.V("MinFireSpeed",6000)+(w.V("MaxFireSpeed",6000)-w.V("MinFireSpeed",6000))*Project.Preview.Power;var origin=ShotOrigin(w,Origin(),angle);var a=Screen(origin);using(var pen=new Pen(Color.FromArgb(145,255,235,170),1.4f)){pen.DashStyle=DashStyle.Dot;for(double t=.015;t<3;t+=.015){var p=Screen(new PointF(origin.X+(float)(Math.Cos(angle)*speed*t/Unit),origin.Y+(float)((-Math.Sin(angle)*speed*t+981*t*t/2)/Unit)));g.DrawLine(pen,a,p);a=p;if(p.X>Width+50||p.Y>Height+50)break;}}}
  void TextBox(Graphics g,string text,RectangleF rect,Color c){using(var b=new SolidBrush(Color.FromArgb(215,17,25,32)))g.FillRectangle(b,rect);using(var b=new SolidBrush(c))g.DrawString(UiText.Get(text),Theme.TextFont(8),b,rect);}
 }
}
