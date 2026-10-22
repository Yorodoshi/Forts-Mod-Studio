using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace FortsStudio {
 // DevIL is supplied by the user's Forts installation. Bind only the library
 // selected through the local installation setting, never a DLL beside a project.
 internal static class GameImageDecoder {
  const uint LoadLibrarySearchDllLoadDir=0x00000100;
  const uint LoadLibrarySearchSystem32=0x00000800;
  static readonly object sync=new object();
  static readonly HashSet<string> failedRoots=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  static Decoder decoder;

  [DllImport("kernel32.dll",EntryPoint="LoadLibraryExW",ExactSpelling=true,CharSet=CharSet.Unicode,SetLastError=true)]
  static extern IntPtr LoadLibraryEx(string file,IntPtr reserved,uint flags);
  [DllImport("kernel32.dll",ExactSpelling=true,CharSet=CharSet.Ansi,SetLastError=true)]
  static extern IntPtr GetProcAddress(IntPtr module,string name);
  [DllImport("kernel32.dll",EntryPoint="GetModuleFileNameW",ExactSpelling=true,CharSet=CharSet.Unicode,SetLastError=true)]
  static extern uint GetModuleFileName(IntPtr module,StringBuilder file,int size);
  [DllImport("kernel32.dll",ExactSpelling=true,SetLastError=true)]
  [return:MarshalAs(UnmanagedType.Bool)] static extern bool FreeLibrary(IntPtr module);

  [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void Init();
  [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void GenImages(uint count,out uint image);
  [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void BindImage(uint image);
  [UnmanagedFunctionPointer(CallingConvention.StdCall,CharSet=CharSet.Ansi)] delegate byte LoadImage([MarshalAs(UnmanagedType.LPStr)]string file);
  [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate byte ConvertImage(uint format,uint type);
  [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int GetInteger(uint mode);
  [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate IntPtr GetData();
  [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void DeleteImages(uint count,ref uint image);
  [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate byte Enable(uint mode);
  [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void OriginFunc(uint mode);

  sealed class Decoder {
   // The successful module is intentionally retained until process exit. Its
   // delegates must never outlive it, and DevIL keeps process-wide image state.
   internal IntPtr Module;
   internal Init Initialize;
   internal GenImages Generate;
   internal BindImage Bind;
   internal LoadImage Load;
   internal ConvertImage Convert;
   internal GetInteger Integer;
   internal GetData Data;
   internal DeleteImages Delete;
   internal Enable SetEnabled;
   internal OriginFunc SetOrigin;
  }

  static T Bind<T>(IntPtr module,string name) where T:class {
   IntPtr address=GetProcAddress(module,name);
   if(address==IntPtr.Zero)throw new EntryPointNotFoundException(name);
   return (T)(object)Marshal.GetDelegateForFunctionPointer(address,typeof(T));
  }

  static Decoder Open(string root){
   string library=Path.GetFullPath(Path.Combine(root,"DevIL.dll"));
   if(!File.Exists(library)||(File.GetAttributes(library)&FileAttributes.ReparsePoint)!=0)throw new IOException("The game image decoder was not found in the selected installation.");
   // These flags exclude the application directory, current directory and PATH.
   // A system without restricted DLL search support must fail closed.
   IntPtr module=LoadLibraryEx(library,IntPtr.Zero,LoadLibrarySearchDllLoadDir|LoadLibrarySearchSystem32);
   if(module==IntPtr.Zero)throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
   try{
    var actual=new StringBuilder(32768);
    uint length=GetModuleFileName(module,actual,actual.Capacity);
    if(length==0||length>=actual.Capacity||!String.Equals(Path.GetFullPath(actual.ToString()),library,StringComparison.OrdinalIgnoreCase))throw new IOException("The image decoder path did not match the selected installation.");
    var result=new Decoder{
     Module=module,Initialize=Bind<Init>(module,"ilInit"),Generate=Bind<GenImages>(module,"ilGenImages"),
     Bind=Bind<BindImage>(module,"ilBindImage"),Load=Bind<LoadImage>(module,"ilLoadImage"),
     Convert=Bind<ConvertImage>(module,"ilConvertImage"),Integer=Bind<GetInteger>(module,"ilGetInteger"),
     Data=Bind<GetData>(module,"ilGetData"),Delete=Bind<DeleteImages>(module,"ilDeleteImages"),
     SetEnabled=Bind<Enable>(module,"ilEnable"),SetOrigin=Bind<OriginFunc>(module,"ilOriginFunc")
    };
    result.Initialize();result.SetEnabled(0x0600);result.SetOrigin(0x0602);
    return result;
   }catch{FreeLibrary(module);throw;}
  }

  internal static Bitmap Decode(string file,string game){
   try{
    string root=GameInstallation.RequireRoot(game);
    string image=Path.GetFullPath(file);
    if(image.Length<3||!Char.IsLetter(image[0])||image[1]!=':'||(image[2]!='\\'&&image[2]!='/')||new DriveInfo(Path.GetPathRoot(image)).DriveType==DriveType.Network)return null;
    lock(sync){
     if(decoder==null){
      if(failedRoots.Contains(root))return null;
      try{decoder=Open(root);}catch{failedRoots.Add(root);return null;}
     }
     // Once trusted and loaded, the same format decoder may read images from
     // another trusted installation. Switching projects cannot load another DLL.
     return ReadImage(decoder,image);
    }
   }catch{return null;}
  }

  static Bitmap ReadImage(Decoder api,string file){
   uint id=0;
   api.Generate(1,out id);
   if(id==0)return null;
   Bitmap result=null;
   try{
    api.Bind(id);
    if(api.Load(file)==0||api.Convert(0x80E1,0x1401)==0)return null;
    int width=api.Integer(0x0DE4),height=api.Integer(0x0DE5);
    long pixels=(long)width*height;
    if(width<=0||height<=0||pixels>64000000)return null;
    int byteCount=checked((int)(pixels*4));
    int rowBytes=checked(width*4);
    IntPtr data=api.Data();
    if(data==IntPtr.Zero)return null;
    byte[] buffer=new byte[byteCount];
    Marshal.Copy(data,buffer,0,buffer.Length);
    result=new Bitmap(width,height,PixelFormat.Format32bppArgb);
    BitmapData bits=result.LockBits(new Rectangle(0,0,width,height),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
    try{for(int y=0;y<height;y++)Marshal.Copy(buffer,checked(y*rowBytes),IntPtr.Add(bits.Scan0,checked(y*bits.Stride)),rowBytes);}
    finally{result.UnlockBits(bits);}
   }catch{if(result!=null)result.Dispose();throw;}
   finally{try{api.Delete(1,ref id);}catch{if(result!=null)result.Dispose();throw;}}
   return result;
  }
 }
}
