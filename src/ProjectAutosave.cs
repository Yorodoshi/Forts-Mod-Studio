using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FortsStudio {
 // Schedule, Flush and Dispose belong to the UI thread. Only detached snapshots
 // enter the background queue; the editor's mutable project never does.
 public sealed class ProjectAutosave : IDisposable {
  sealed class ReferenceComparer : IEqualityComparer<object> {
   public new bool Equals(object a,object b){return Object.ReferenceEquals(a,b);}
   public int GetHashCode(object value){return RuntimeHelpers.GetHashCode(value);}
  }
  sealed class SaveResult {public long Revision;public Exception Error;}

  static readonly Dictionary<Type,FieldInfo[]> snapshotFields=new Dictionary<Type,FieldInfo[]>();
  readonly string path;
  readonly Action<long,Exception> completed;
  readonly SynchronizationContext completionContext;
  readonly int ownerThread;
  readonly System.Windows.Forms.Timer debounce;
  Task tail;
  Project pendingProject;
  long revision;
  volatile bool disposed;

  public ProjectAutosave(string path,Action<long,Exception> completed){
   if(String.IsNullOrWhiteSpace(path))throw new ArgumentException("An autosave path is required.","path");
   this.path=Path.GetFullPath(path);this.completed=completed;
   ownerThread=Thread.CurrentThread.ManagedThreadId;
   completionContext=SynchronizationContext.Current??new WindowsFormsSynchronizationContext();
   var ready=new TaskCompletionSource<bool>();ready.SetResult(true);tail=ready.Task;
   debounce=new System.Windows.Forms.Timer{Interval=1500};debounce.Tick+=DebounceTick;
  }

  // The returned revision lets the editor ignore a completion for older edits.
  public long Schedule(Project project){
   VerifyUsable();if(project==null)throw new ArgumentNullException("project");
   pendingProject=project;revision++;debounce.Stop();debounce.Start();return revision;
  }

  void DebounceTick(object sender,EventArgs args){
   debounce.Stop();if(disposed||pendingProject==null)return;
   long current=revision;
   try{var snapshot=Snapshot(pendingProject);pendingProject=null;Enqueue(snapshot,current);}
   catch(Exception error){Notify(current,error);}
  }

  // Closing the editor can wait for disk work safely: workers only Post UI
  // notifications and never wait for the UI thread to handle them.
  public long Flush(Project project){
   VerifyUsable();if(project==null)throw new ArgumentNullException("project");
   debounce.Stop();pendingProject=null;long current=++revision;
   var write=Enqueue(Snapshot(project),current);write.Wait();var result=write.Result;
   if(result.Error!=null)throw new IOException("Не удалось сохранить автокопию: "+result.Error.Message,result.Error);
   return current;
  }

  Task<SaveResult> Enqueue(Project snapshot,long current){
   var write=tail.ContinueWith(previous=>Write(snapshot,current),CancellationToken.None,TaskContinuationOptions.None,TaskScheduler.Default);
   tail=write;return write;
  }

  SaveResult Write(Project snapshot,long current){
   var result=new SaveResult{Revision=current};string temporary=null;
   try{
    string directory=Path.GetDirectoryName(path);Directory.CreateDirectory(directory);
    temporary=Path.Combine(directory,Path.GetFileName(path)+"."+Guid.NewGuid().ToString("N")+".tmp");
    Storage.Save(snapshot,temporary);
    // The temporary file shares the destination volume. Replacement keeps the
    // previous complete autosave available until the new file is complete.
    if(File.Exists(path))File.Replace(temporary,path,null);else File.Move(temporary,path);
    temporary=null;
   }catch(Exception error){result.Error=error;}
   finally{if(temporary!=null)try{File.Delete(temporary);}catch{}}
   Notify(result.Revision,result.Error);return result;
  }

  void Notify(long current,Exception error){
   if(disposed||completed==null)return;
   try{completionContext.Post(unused=>{if(!disposed)completed(current,error);},null);}
   catch{} // A closed UI dispatcher must not turn a completed save into a failure.
  }

  static Project Snapshot(Project project){return (Project)Clone(project,new Dictionary<object,object>(new ReferenceComparer()));}
  static object Clone(object source,Dictionary<object,object> seen){
   if(source==null)return null;Type type=source.GetType();
   // Strings include embedded image/audio data and are immutable. Reusing
   // their references keeps the UI snapshot independent of payload size.
   if(source is string||type.IsValueType)return source;
   object existing;if(seen.TryGetValue(source,out existing))return existing;
   var dictionary=source as IDictionary;
   if(dictionary!=null){var copy=(IDictionary)Activator.CreateInstance(type);seen.Add(source,copy);foreach(DictionaryEntry item in dictionary)copy.Add(Clone(item.Key,seen),Clone(item.Value,seen));return copy;}
   var array=source as Array;
   if(array!=null){if(array.Rank!=1)throw new SerializationException("Project snapshots require one-dimensional arrays.");var copy=(Array)array.Clone();seen.Add(source,copy);for(int i=array.GetLowerBound(0);i<=array.GetUpperBound(0);i++)copy.SetValue(Clone(array.GetValue(i),seen),i);return copy;}
   var list=source as IList;
   if(list!=null){var copy=(IList)Activator.CreateInstance(type);seen.Add(source,copy);foreach(object item in list)copy.Add(Clone(item,seen));return copy;}
   // Project data is expressed in public fields. Skipping initializers avoids
   // constructing temporary lists, audio IDs and defaults before copying them.
   object result=FormatterServices.GetUninitializedObject(type);seen.Add(source,result);
   FieldInfo[] fields;
   lock(snapshotFields){if(!snapshotFields.TryGetValue(type,out fields)){fields=type.GetFields(BindingFlags.Instance|BindingFlags.Public);snapshotFields.Add(type,fields);}}
   foreach(var field in fields)field.SetValue(result,Clone(field.GetValue(source),seen));return result;
  }

  void VerifyUsable(){
   if(disposed)throw new ObjectDisposedException("ProjectAutosave");
   if(Thread.CurrentThread.ManagedThreadId!=ownerThread)throw new InvalidOperationException("Project autosave snapshots must be requested on the UI thread.");
  }
  public void Dispose(){
   if(disposed)return;VerifyUsable();disposed=true;debounce.Stop();debounce.Tick-=DebounceTick;debounce.Dispose();pendingProject=null;
  }
 }
}
