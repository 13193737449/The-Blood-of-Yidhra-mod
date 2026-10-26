using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
internal static class MonoHost
{
 [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] static extern IntPtr LoadLibraryW(string path);
 [DllImport("kernel32.dll", CharSet=CharSet.Unicode)] static extern bool SetDllDirectoryW(string path);
 [DllImport("mono-2.0-bdwgc.dll",CallingConvention=CallingConvention.Cdecl)] static extern void mono_set_dirs(IntPtr assemblyDir,IntPtr configDir);
 [DllImport("mono-2.0-bdwgc.dll",CallingConvention=CallingConvention.Cdecl)] static extern IntPtr mono_jit_init_version(IntPtr name,IntPtr version);
 [DllImport("mono-2.0-bdwgc.dll",CallingConvention=CallingConvention.Cdecl)] static extern IntPtr mono_domain_assembly_open(IntPtr domain,IntPtr name);
 [DllImport("mono-2.0-bdwgc.dll",CallingConvention=CallingConvention.Cdecl)] static extern int mono_jit_exec(IntPtr domain,IntPtr assembly,int argc,IntPtr argv);
 [DllImport("mono-2.0-bdwgc.dll",CallingConvention=CallingConvention.Cdecl)] static extern void mono_set_assemblies_path(IntPtr path);
 static IntPtr U(string s){byte[] b=Encoding.UTF8.GetBytes(s+"\0");IntPtr p=Marshal.AllocHGlobal(b.Length);Marshal.Copy(b,0,p,b.Length);return p;}
 static int Main(string[] args)
 {
  string game=args[0],exe=args[1];
  string runtime=Path.Combine(game,"MonoBleedingEdge\\EmbedRuntime");
  SetDllDirectoryW(runtime);
  if(LoadLibraryW(Path.Combine(runtime,"mono-2.0-bdwgc.dll"))==IntPtr.Zero)throw new Exception("LoadLibrary failed: "+Marshal.GetLastWin32Error());
  return Run(game,exe,args);
 }
 [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
 static int Run(string game,string exe,string[] args)
 {
  mono_set_dirs(U(Path.Combine(game,"The Blood of Yidhra_Data\\Managed")),U(Path.Combine(game,"MonoBleedingEdge\\etc")));
  mono_set_assemblies_path(U(Path.Combine(game,"The Blood of Yidhra_Data\\Managed")));
  IntPtr domain=mono_jit_init_version(U("YidhraMonoRegression"),U("v4.0.30319"));
  if(domain==IntPtr.Zero)throw new Exception("Mono init failed");
  IntPtr assembly=mono_domain_assembly_open(domain,U(exe));
  if(assembly==IntPtr.Zero)throw new Exception("Mono assembly open failed");
  int count=args.Length-1;
  IntPtr argv=Marshal.AllocHGlobal(IntPtr.Size*count);
  Marshal.WriteIntPtr(argv,0,U(exe));
  for(int i=2;i<args.Length;i++)Marshal.WriteIntPtr(argv,IntPtr.Size*(i-1),U(args[i]));
  return mono_jit_exec(domain,assembly,count,argv);
 }
}





