#pragma warning disable 618 // Reproduce the legacy Mono exception reported by the actual game.
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using YidhraTrainer;
using Mono.Cecil;
using MonoMod.Utils;
internal static class UnicodePathTests
{
 static string core,dist;
 static int Main(string[] args) {
  core=args[0];dist=args[1];
  AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender,ResolveEventArgs e){string file=new AssemblyName(e.Name).Name+".dll";foreach(string dir in new string[]{core,dist}){string p=Path.Combine(dir,file);if(File.Exists(p))return Assembly.LoadFrom(p);}return null;};
  try {Run();Console.WriteLine("PASS: Unicode assembly-name compatibility regression");return 0;} catch(Exception e){Console.WriteLine("FAIL: "+e.GetType().Name+": "+e.Message);return 1;}
 }
 sealed class BrokenPathAssembly:Assembly {
  public int GetNameCalls;
  public override string FullName {get{return "UnicodeFixture, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null";}}
  public override AssemblyName GetName(){GetNameCalls++;throw new ExecutionEngineException("String conversion error: Illegal byte sequence encounted in the input.");}
  public override AssemblyName GetName(bool copied){return GetName();}
 }
 [MethodImpl(MethodImplOptions.NoInlining)] static void Run(){
  using(ModuleDefinition module=ModuleDefinition.CreateModule("UnicodeRegression",ModuleKind.Dll)) {
   var importer=new MMReflectionImporter(module); var bad=new BrokenPathAssembly();
   bool reproduced=false;try {importer.ImportReference(bad);}catch(ExecutionEngineException){reproduced=true;}
   if(!reproduced)throw new Exception("original importer did not reproduce encoding failure");
   int calls=bad.GetNameCalls;
   MethodInfo seed=typeof(UnicodePathCompatibility).GetMethod("CacheAssembly",BindingFlags.NonPublic|BindingFlags.Static);
   seed.Invoke(null,new object[]{importer,bad});
   var name=importer.ImportReference(bad);
   if(name.Name!="UnicodeFixture"||name.Version.ToString()!="1.2.3.4"||bad.GetNameCalls!=calls)throw new Exception("safe cache failed");
  }
  using(var warmup=new DynamicMethodDefinition("BeforeCompatibility",typeof(void),Type.EmptyTypes)) {}
  var previous=MMReflectionImporter.ProviderNoDefault;
  UnicodePathCompatibility.Install();

  using(var probe=new DynamicMethodDefinition("AfterCompatibility",typeof(void),Type.EmptyTypes)) {
   bool safe=false;
   foreach(var field in typeof(ModuleDefinition).GetFields(BindingFlags.NonPublic|BindingFlags.Instance))
    if(field.FieldType==typeof(IReflectionImporter)) {var instance=field.GetValue(probe.Module);safe=instance!=null && instance.GetType().FullName.Contains("SafeImporter");}
   if(!safe)throw new Exception("Mono cached old readonly provider after DMD warmup");
  }
  using(ModuleDefinition module=ModuleDefinition.CreateModule("SafeProvider",new ModuleParameters{Kind=ModuleKind.Dll,ReflectionImporterProvider=MMReflectionImporter.ProviderNoDefault})){
   module.ImportReference(typeof(UnicodePathTests).GetMethod("Main",BindingFlags.NonPublic|BindingFlags.Static));
  }
  UnicodePathCompatibility.Restore();
  using(var restored=new DynamicMethodDefinition("RestoredFactory",typeof(void),Type.EmptyTypes)) {
   foreach(var field in typeof(ModuleDefinition).GetFields(BindingFlags.NonPublic|BindingFlags.Instance))
    if(field.FieldType==typeof(IReflectionImporter)) {var instance=field.GetValue(restored.Module);if(instance!=null && instance.GetType().FullName.Contains("SafeImporter"))throw new Exception("factory detour not restored");}
  }
  if(!object.ReferenceEquals(previous,typeof(MMReflectionImporter).GetField("ProviderNoDefault").GetValue(null)))throw new Exception("provider not restored");
 }
}



