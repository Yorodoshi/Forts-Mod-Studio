using System;using System.Diagnostics;using System.Drawing;
namespace FortsStudio {
 public partial class Preview {
  public bool FrameDetails {get;set;}
  double prepareMilliseconds,sceneMilliseconds,objectMilliseconds,projectileMilliseconds,overlayMilliseconds,simulationMilliseconds;
  static double PaintStage(ref long started){long now=Stopwatch.GetTimestamp();double elapsed=(now-started)*1000.0/Stopwatch.Frequency;started=now;return elapsed;}
  void DrawFrameDetails(Graphics g){
   if(!FrameDetails)return;
   string text=String.Format(System.Globalization.CultureInfo.CurrentCulture,
    UiText.Get("Подготовка: {0:0.00} мс\nСцена и фон: {1:0.00} мс\nОбъекты: {2:0.00} мс\nСнаряды и траектория: {3:0.00} мс\nПодписи: {4:0.00} мс\nРасчёт движения: {5:0.00} мс\nСнарядов: {6} · плиток: {7}\nКэш мира: {8:0.0} МБ\nКэш снарядов: {9:0.0} МБ · объектов: {10:0.0} МБ"),
    prepareMilliseconds,sceneMilliseconds,objectMilliseconds,projectileMilliseconds,overlayMilliseconds,simulationMilliseconds,shots.Count,worldTiles.Count,worldTileBytes/1048576.0,projectileSpriteBytes/1048576.0,objectSpriteBytes/1048576.0);
   var size=g.MeasureString(text,performanceFont);float width=Math.Min(size.Width+16,Math.Max(1,Width-24));var rect=new RectangleF(Math.Max(12,Width-width-12),104,width,size.Height+12);
   using(var shade=new SolidBrush(Color.FromArgb(230,17,25,32)))g.FillRectangle(shade,rect);
   g.DrawString(text,performanceFont,Brushes.LightGray,new RectangleF(rect.X+8,rect.Y+6,Math.Max(1,rect.Width-16),size.Height));
  }
 }
}
