using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Collections.Generic;
using HarmonyLib;
using YidhraTrainer;

internal static class PatchSmokeTests
{
    static string game, root;
    static int Main(string[] args)
    {
        game = args[0]; root = args[1];
        AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs e) {
            string name = new AssemblyName(e.Name).Name + ".dll";
            foreach (string dir in new string[] {Path.Combine(game, "BepInEx\\core"), Path.Combine(game, "The Blood of Yidhra_Data\\Managed"), Path.Combine(root, "dist")}) {
                string file = Path.Combine(dir, name);
                if (File.Exists(file)) return Assembly.LoadFrom(file);
            }
            return null;
        };
        try { Run(); Console.WriteLine("PASS: real Harmony registration, resource writes and unpatch restore"); return 0; }
        catch(Exception e) {Console.WriteLine("FAIL: " + e); return 1;}
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Run()
    {
        UnicodePathCompatibility.Install();
        Harmony harmony = new Harmony("local.yidhra.trainer.smoketest");
        try {
            int count = PatchInstaller.Install(harmony);
            Check(count == 17, "all seventeen patches registered");
            TrainerState.InfiniteHealth = true;
            TrainerState.InfiniteStamina = true;
            TrainerState.InfiniteItems = true;
            PlayerController player = (PlayerController)FormatterServices.GetUninitializedObject(typeof(PlayerController));
            player.MaxLife = 120; player.MaxEnergy = 90;
            typeof(BaseObject).GetProperty("Life").GetSetMethod(true).Invoke(player, new object[] {-100f});
            Check(player.Life == 120, "real setter intercepts lethal damage");
            typeof(PlayerController).GetProperty("Energy").GetSetMethod(true).Invoke(player, new object[] {0f});
            Check(player.Energy == 90, "real stamina setter intercepts depletion");
            BaseObject enemy = (BaseObject)FormatterServices.GetUninitializedObject(typeof(BaseObject));
            enemy.MaxLife = 200;
            typeof(BaseObject).GetProperty("Life").GetSetMethod(true).Invoke(enemy, new object[] {7f});
            Check(enemy.Life == 7, "real enemy setter unaffected");
            InventoryController inventory = (InventoryController)FormatterServices.GetUninitializedObject(typeof(InventoryController));
            Dictionary<string,int> props = new Dictionary<string,int>{{"AidKit",2},{"Coin",10}};
            typeof(InventoryController).GetField("PropsInventory",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(inventory,props);
            Check(inventory.UseProp("AidKit",1) && inventory.GetPropsCount("AidKit") == 2, "actual patched item method preserves quantity");
            Check(inventory.UseProp("Coin",3) && inventory.GetPropsCount("Coin") == 7, "actual patched item method keeps coin consumption");
            Type gunType = null;
            foreach (Type candidate in typeof(BaseGun).Assembly.GetTypes())
                if (candidate != typeof(BaseGun) && typeof(BaseGun).IsAssignableFrom(candidate) && !candidate.IsAbstract) {gunType=candidate;break;}
            BaseGun gun = (BaseGun)FormatterServices.GetUninitializedObject(gunType);
            typeof(BaseGun).GetField("<ClipCapacity>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(gun,12);
            typeof(BaseGun).GetField("<BulletType>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(gun,"TestBullet");
            EquipmentSlot slot = (EquipmentSlot)0;
            Dictionary<EquipmentSlot,int> magazines = new Dictionary<EquipmentSlot,int>{{slot,0}};
            typeof(InventoryController).GetField("<SeletedItem>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(inventory,slot);
            typeof(InventoryController).GetField("MagazineBulletList",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(inventory,magazines);
            typeof(InventoryController).GetField("WeaponEquipmentsDic",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(inventory,new Dictionary<EquipmentSlot,BaseWeapon>{{slot,gun}});
            props["TestBullet"]=30;
            TrainerState.InfiniteAmmo=true;
            Check((int)Invoke(inventory,"GetSelectedWeaponBulletNumber") == 12,"real postfix makes empty gun fireable");
            Invoke(inventory,"ShootBullet");
            Invoke(inventory,"ReloadBullet",12);
            Check(magazines[slot]==0 && props["TestBullet"]==30,"real patched shooting/reload preserves both inventories");
            // The patched player Reload must skip before touching uninitialized Unity animation state.
            Invoke(player,"Reload");
            TrainerState.InfiniteAmmo=false;
            Check((int)Invoke(inventory,"GetSelectedWeaponBulletNumber")==0,"ammo toggle off restores real magazine immediately");
            magazines[slot]=4;
            Invoke(inventory,"ShootBullet");
            Invoke(inventory,"ReloadBullet",12);
            Check(magazines[slot]==12 && props["TestBullet"]==21,"ammo toggle off restores actual shooting and reloading");
            TrainerState.InfiniteHealth=false; TrainerState.InfiniteStamina=false;
            typeof(BaseObject).GetProperty("Life").GetSetMethod(true).Invoke(player,new object[]{7f});
            typeof(PlayerController).GetProperty("Energy").GetSetMethod(true).Invoke(player,new object[]{3f});
            Check(player.Life==7 && player.Energy==3,"health and stamina toggle off restores actual setter behavior");
            // Exercise the same combat handlers through real Harmony argument/state injection.
            harmony.Patch(typeof(ProbeEnemy).GetMethod("SettlementDamage"),prefix:new HarmonyMethod(typeof(CombatPatches).GetMethod("DamagePrefix")));
            var enemyProbe=(ProbeEnemy)FormatterServices.GetUninitializedObject(typeof(ProbeEnemy));
            typeof(BaseObject).GetField("life",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(enemyProbe,101f);
            var hit=(DamageInfo)FormatterServices.GetUninitializedObject(typeof(DamageInfo));
            typeof(DamageInfo).GetField("<Source>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(hit,player);
            typeof(DamageInfo).GetField("<Damage>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(hit,7);
            TrainerState.DamageBoost=true;TrainerState.DamageMultiplier=5;
            typeof(ProbeEnemy).GetMethod("SettlementDamage").Invoke(enemyProbe,new object[]{hit});
            Check(enemyProbe.LastDamage==35 && hit.Damage==7,"real Harmony damage injection preserves original record");
            TrainerState.OneHitKill=true;
            typeof(ProbeEnemy).GetMethod("SettlementDamage").Invoke(enemyProbe,new object[]{hit});
            Check(enemyProbe.LastDamage==101,"real Harmony one-hit priority");
            harmony.Patch(typeof(ShotProbe).GetMethod("Fire"),prefix:new HarmonyMethod(typeof(CombatPatches).GetMethod("ShotPrefix")),finalizer:new HarmonyMethod(typeof(CombatPatches).GetMethod("ShotFinalizer")));
            TrainerState.NoRecoil=true;
            var shot=new ShotProbe();
            try {typeof(ShotProbe).GetMethod("Fire").Invoke(shot,new object[]{player});}catch(TargetInvocationException ex){Check(ex.InnerException is InvalidOperationException,"real finalizer retains original exception");}
            Check(shot.Blocked && CombatPatches.ShakePrefix(),"real finalizer restores shot scope after exception");
            harmony.Patch(typeof(BulletProbe).GetMethod("Create"),prefix:new HarmonyMethod(typeof(CombatPatches).GetMethod("BulletPrefix")));
            var parameters=new Dictionary<string,object>{{"user",player},{"offsetangle",5f}};
            var bulletProbe=new BulletProbe();TrainerState.NoSpread=true;
            typeof(BulletProbe).GetMethod("Create").Invoke(bulletProbe,new object[]{parameters});
            Check(bulletProbe.Angle==0 && (float)parameters["offsetangle"]==5,"real no-spread injection clones parameters");
            harmony.UnpatchSelf();
            Check(inventory.UseProp("AidKit",1) && inventory.GetPropsCount("AidKit") == 1, "unpatch restores actual item consumption");
            Console.WriteLine("Installed patches: " + count);
        } finally {harmony.UnpatchSelf(); TrainerState.DisableAll(); UnicodePathCompatibility.Restore();}
    }
    public class ProbeEnemy:BaseMoster {public int LastDamage;public override void SettlementDamage(DamageInfo info){LastDamage=info.Damage;}}
    public class ShotProbe {public bool Blocked;public void Fire(BaseCharacter actor){Blocked=!CombatPatches.ShakePrefix();throw new InvalidOperationException("probe");}}
    public class BulletProbe {public float Angle;public void Create(Dictionary<string,object> args){Angle=(float)args["offsetangle"];}}
    static object Invoke(object target,string method,params object[] args) {return target.GetType().GetMethod(method).Invoke(target,args);}
    static void Check(bool ok,string message) {if(!ok)throw new Exception(message); Console.WriteLine("PASS: "+message);}
}



