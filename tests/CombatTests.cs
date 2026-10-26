using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using YidhraTrainer;
internal static class CombatTests
{
 static string managed; static int count;
 static int Main(string[] args){managed=args[0];AppDomain.CurrentDomain.AssemblyResolve+=delegate(object s,ResolveEventArgs e){string p=System.IO.Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return System.IO.File.Exists(p)?Assembly.LoadFrom(p):null;};try{Run();Console.WriteLine("PASS: "+count+" combat assertions");return 0;}catch(Exception e){Console.WriteLine("FAIL: "+e);return 1;}}
 static T Bare<T>(){return (T)FormatterServices.GetUninitializedObject(typeof(T));}
 static void Field(object x,Type t,string name,object value){t.GetField(name,BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance).SetValue(x,value);}
 static void Check(bool value,string message){if(!value)throw new Exception(message);count++;}
 static DamageInfo Info(BaseObject source,int damage){var d=Bare<DamageInfo>();Field(d,typeof(DamageInfo),"<Source>k__BackingField",source);Field(d,typeof(DamageInfo),"<Damage>k__BackingField",damage);d.HeadShotRate=0.37f;return d;}
 [MethodImpl(MethodImplOptions.NoInlining)]static void Run(){
  var player=Bare<PlayerController>();var enemy=Bare<BaseMoster>();Field(enemy,typeof(BaseObject),"life",100.5f);
  var original=Info(player,7);var d=original;
  CombatPatches.DamagePrefix(enemy,ref d);Check(d.Damage==7,"disabled damage");
  TrainerState.DamageBoost=true;TrainerState.DamageMultiplier=5;CombatPatches.DamagePrefix(enemy,ref d);
  Check(d.Damage==35 && original.Damage==7 && !object.ReferenceEquals(original,d),"boost clones and preserves input");Check(d.HeadShotRate==original.HeadShotRate && object.ReferenceEquals(d.Source,player),"damage metadata preserved");
  d=Info(enemy,8);CombatPatches.DamagePrefix(player,ref d);Check(d.Damage==8,"enemy damage unchanged");
  d=Info(player,8);CombatPatches.DamagePrefix(Bare<BaseObject>(),ref d);Check(d.Damage==8,"environment unchanged");
  d=Info(null,9);CombatPatches.DamagePrefix(enemy,ref d);Check(d.Damage==9,"unattributed damage unchanged");
  d=Info(player,0);CombatPatches.DamagePrefix(enemy,ref d);Check(d.Damage==0,"blocked damage stays zero");
  d=Info(player,int.MaxValue);CombatPatches.DamagePrefix(enemy,ref d);Check(d.Damage==int.MaxValue,"boost saturates without overflow");
  TrainerState.OneHitKill=true;d=Info(player,7);CombatPatches.DamagePrefix(enemy,ref d);Check(d.Damage==101,"kill takes priority over multiplier and rounds life up");
  TrainerState.DisableAll();d=Info(player,7);CombatPatches.DamagePrefix(enemy,ref d);Check(d.Damage==7,"disable all restores damage");
  var input=new Dictionary<string,object>{{"user",player},{"offsetangle",4.5f},{"other",123}};var data=input;
  CombatPatches.BulletPrefix(ref data);Check(object.ReferenceEquals(data,input),"spread disabled leaves dictionary");
  TrainerState.NoSpread=true;CombatPatches.BulletPrefix(ref data);Check((float)data["offsetangle"]==0 && (float)input["offsetangle"]==4.5f && (int)data["other"]==123,"no spread copies only angle");
  data=new Dictionary<string,object>{{"user",enemy},{"offsetangle",3f}};CombatPatches.BulletPrefix(ref data);Check((float)data["offsetangle"]==3,"enemy spread retained");
  data=new Dictionary<string,object>{{"user",player}};CombatPatches.BulletPrefix(ref data);Check(!data.ContainsKey("offsetangle"),"non-spread weapons unchanged");
  data=null;CombatPatches.BulletPrefix(ref data);Check(data==null,"null bullet parameters safe");
  TrainerState.NoRecoil=true;Check(CombatPatches.ShakePrefix(),"environment shake allowed");
  bool outer,inner;CombatPatches.ShotPrefix(player,out outer);Check(!CombatPatches.ShakePrefix(),"shot shake suppressed");
  CombatPatches.ShotPrefix(player,out inner);CombatPatches.ShotFinalizer(inner,null);Check(!CombatPatches.ShakePrefix(),"nested shot scope retained");
  var error=new InvalidOperationException("original");Check(object.ReferenceEquals(error,CombatPatches.ShotFinalizer(outer,error)),"original exception preserved");Check(CombatPatches.ShakePrefix(),"exception cleans shot context");
  CombatPatches.ShotPrefix(enemy,out outer);Check(CombatPatches.ShakePrefix(),"enemy shot does not suppress environment");CombatPatches.ShotFinalizer(outer,null);
  TrainerState.NoRecoil=false;CombatPatches.ShotPrefix(player,out outer);Check(CombatPatches.ShakePrefix(),"recoil disabled restores shake");CombatPatches.ShotFinalizer(outer,null);
  TrainerState.DisableAll();Check(!TrainerState.NoSpread&&!TrainerState.OneHitKill&&!TrainerState.DamageBoost&&!TrainerState.NoRecoil,"all new toggles disabled");
 }
}
