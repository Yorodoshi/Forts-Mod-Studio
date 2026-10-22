using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Drawing.Imaging;using System.IO;using System.Linq;using System.Runtime.CompilerServices;using System.Text.RegularExpressions;
namespace FortsStudio {
 // Forts renders a texture pixel as half a world unit. Export and preview share the same bitmap.
 public static class MaterialVisuals {
  class BeamEntry {public string Source;public int Height;public bool Join;public Bitmap Image;}
  class MaterialEntry {public string Game,Template,Texture;public string[] Frames;public Dictionary<int,BeamEntry> Beams=new Dictionary<int,BeamEntry>();}
  static ConditionalWeakTable<Entity,MaterialEntry> cache=new ConditionalWeakTable<Entity,MaterialEntry>();
  static Dictionary<string,Dictionary<string,string[]>> sources=new Dictionary<string,Dictionary<string,string[]>>(StringComparer.Ordinal);
  public static int PixelHeight(Entity e){return Math.Max(2,Math.Min(512,(int)Math.Round(e.Thickness*2,MidpointRounding.AwayFromZero)));}
  public static double WorldHeight(Entity e){return PixelHeight(e)*.5;}
  static MaterialEntry Entry(Entity e){MaterialEntry entry;if(!cache.TryGetValue(e,out entry)){entry=new MaterialEntry();cache.Add(e,entry);}return entry;}
  public static string[] Frames(Entity e,string game){return Frames(e,game,Entry(e));}
  static string[] Frames(Entity e,string game,MaterialEntry entry){
   game=game??"";
   if(entry.Frames!=null&&Object.ReferenceEquals(entry.Texture,e.Texture)&&entry.Game==game&&entry.Template==e.Template)return entry.Frames;
   string[] found;
   if(e.Texture.Length>0)found=new[]{e.Texture};
   else{
    Dictionary<string,string[]> templates;
    if(!sources.TryGetValue(game,out templates)){templates=new Dictionary<string,string[]>(StringComparer.Ordinal);sources.Add(game,templates);}
    if(!templates.TryGetValue(e.Template,out found)){
     try{string src=File.ReadAllText(Path.Combine(game,"data","materials","building_materials.lua"));string sprite=Catalog.Get(e).Sprite;var m=Regex.Match(src,"Name\\s*=\\s*\""+Regex.Escape(sprite)+"\"");if(m.Success){string table=Catalog.Balanced(src,src.LastIndexOf('{',m.Index));found=Regex.Matches(table,"texture\\s*=\\s*\"([^\"]+)\"").Cast<Match>().Select(x=>x.Groups[1].Value).ToArray();}}catch{}
     if(found==null||found.Length==0)found=new[]{Catalog.Get(e).Texture};
     templates[e.Template]=found;
    }
   }
   entry.Beams.Clear();entry.Game=game;entry.Template=e.Template;entry.Texture=e.Texture;entry.Frames=found;return found;
  }
  public static Bitmap Beam(Entity e,string game,int frame=0){
   var entry=Entry(e);var frames=Frames(e,game,entry);
   frame=Math.Max(0,Math.Min(frame,frames.Length-1));string source=frames[frame];
   int h=PixelHeight(e);
   bool join=e.Texture.Length>0&&e.SeamlessTexture;
   BeamEntry old;
   if(entry.Beams.TryGetValue(frame,out old)&&Object.ReferenceEquals(old.Source,source)&&old.Height==h&&old.Join==join)return old.Image;
   var im=Assets.Image(source,game);if(im==null){entry.Beams[frame]=new BeamEntry{Source=source,Height=h,Join=join};return null;}
   int w=Math.Max(8,Math.Min(4096,(int)Math.Round(im.Width*(double)h/im.Height)));
   var result=Raster.Resize(im,w,h);
   if(join)Raster.JoinHorizontalEdges(result);
   // A caller may retain the previous bitmap. Let its normal lifetime end
   // after replacing the cache entry instead of disposing it underneath them.
   entry.Beams[frame]=new BeamEntry{Source=source,Height=h,Join=join,Image=result};return result;
  }
  public static double NativeHeight(Entity e,string game){var im=Assets.Image(Catalog.Get(e).Texture,game);return im==null?32:im.Height*.5;}
 }
}
