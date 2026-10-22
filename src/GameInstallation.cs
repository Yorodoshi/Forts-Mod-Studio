using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FortsStudio {
 // Projects contain data, but cannot silently approve executable code locations.
 public static class GameInstallation {
  static readonly object gate=new object();
  static readonly HashSet<string> approved=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  static readonly Dictionary<string,string> resolved=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
  static readonly string settings=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FortsModStudio2","approved-game-folders.txt");
  public static void Initialize(string discovered){
   lock(gate){
    try{if(File.Exists(settings))foreach(string value in File.ReadAllLines(settings)){string root;if(TryValidate(value,out root))approved.Add(root);}}catch(IOException){}catch(UnauthorizedAccessException){}
    string found;if(TryValidate(discovered,out found))approved.Add(found);
   }
  }
  static bool TryValidate(string value,out string root){
   root=null;if(String.IsNullOrWhiteSpace(value)||!Path.IsPathRooted(value)||value.StartsWith(@"\\",StringComparison.Ordinal)||value.StartsWith("//",StringComparison.Ordinal))return false;
   try{string full=Path.GetFullPath(value).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);if(full.Length<3||full[1]!=':')return false;
    var drive=new DriveInfo(Path.GetPathRoot(full));if(drive.DriveType!=DriveType.Fixed&&drive.DriveType!=DriveType.Removable)return false;
    if(!File.Exists(Path.Combine(full,"Forts.exe"))||!Directory.Exists(Path.Combine(full,"data")))return false;root=full;return true;
   }catch(ArgumentException){return false;}catch(IOException){return false;}catch(UnauthorizedAccessException){return false;}catch(NotSupportedException){return false;}
  }
  public static bool TrustFromUser(string folder){
   string root;if(!TryValidate(folder,out root))return false;
   lock(gate){approved.Add(root);resolved[folder]=root;
    try{Directory.CreateDirectory(Path.GetDirectoryName(settings));File.WriteAllLines(settings,approved.OrderBy(s=>s,StringComparer.OrdinalIgnoreCase),new UTF8Encoding(false));}catch(IOException){}catch(UnauthorizedAccessException){}
   }return true;
  }
  public static string RequireRoot(string folder){
   lock(gate){string root;if(folder!=null&&resolved.TryGetValue(folder,out root))return root;
    if(!TryValidate(folder,out root)||!approved.Contains(root))throw new InvalidOperationException("Выберите установленную Forts через «Проект и установка» → «Выбрать папку игры». Путь из импортированного проекта не разрешает запуск программ.");
    resolved[folder]=root;return root;
   }
  }
  public static string SelectForProject(string candidate){
   try{return RequireRoot(candidate);}catch(InvalidOperationException){lock(gate){return approved.OrderBy(s=>s,StringComparer.OrdinalIgnoreCase).FirstOrDefault()??"";}}
  }
 }
}
