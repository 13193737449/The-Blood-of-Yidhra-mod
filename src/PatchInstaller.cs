using System;
using System.Reflection;
using HarmonyLib;

namespace YidhraTrainer
{
    public static class PatchInstaller
    {
        public const int PatchCount = 17;
        // Shared by the plugin and the real-Harmony smoke test.
        public static int Install(Harmony harmony)
        {
            try
            {
                Patch(harmony, typeof(BaseObject), "set_Life", new Type[] { typeof(float) }, "LifePrefix", false);
                Patch(harmony, typeof(PlayerController), "set_Energy", new Type[] { typeof(float) }, "EnergyPrefix", false);
                Patch(harmony, typeof(InventoryController), "GetSelectedWeaponBulletNumber", Type.EmptyTypes, "AmmoCountPostfix", true);
                Patch(harmony, typeof(InventoryController), "ShootBullet", Type.EmptyTypes, "ShootPrefix", false);
                Patch(harmony, typeof(InventoryController), "ReloadBullet", new Type[] { typeof(int) }, "ReloadPrefix", false);
                Patch(harmony, typeof(PlayerController), "Reload", Type.EmptyTypes, "ReloadPrefix", false);
                Patch(harmony, typeof(InventoryController), "UseProp", new Type[] { typeof(string), typeof(int) }, "UsePropPrefix", false);
                Patch(harmony, typeof(BaseObject), "SettlementDamage", new Type[] { typeof(DamageInfo) }, "DamagePrefix", false, typeof(CombatPatches));
                Patch(harmony, typeof(Bullet), "Create", new Type[] { typeof(System.Collections.Generic.Dictionary<string, object>) }, "BulletPrefix", false, typeof(CombatPatches));
                Patch(harmony, typeof(GameCamera), "Shake", new Type[] { typeof(float), typeof(int) }, "ShakePrefix", false, typeof(CombatPatches));
                // These are all seven weapon implementations that call the discrete camera-shake overload.
                foreach (Type gun in new Type[] { typeof(GrenadeLauncher), typeof(HandGun), typeof(MachineGun), typeof(Rifle), typeof(ShotGun), typeof(Sniper), typeof(Submachine) })
                {
                    MethodInfo target = gun.GetMethod("OnAttackEvent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                    if (target == null) throw new MissingMethodException(gun.FullName, "OnAttackEvent");
                    harmony.Patch(target,
                        prefix: new HarmonyMethod(typeof(CombatPatches).GetMethod("ShotPrefix")),
                        finalizer: new HarmonyMethod(typeof(CombatPatches).GetMethod("ShotFinalizer")));
                }
                return PatchCount;
            }
            catch (Exception original)
            {
                try { harmony.UnpatchSelf(); }
                catch (Exception cleanup) { original.Data["RollbackFailure"] = cleanup.Message; }
                throw;
            }
        }

        private static void Patch(Harmony harmony, Type type, string methodName, Type[] parameters, string handlerName, bool postfix, Type handlerType = null)
        {
            MethodInfo target = AccessTools.Method(type, methodName, parameters);
            MethodInfo handler = (handlerType ?? typeof(TrainerPatches)).GetMethod(handlerName, BindingFlags.Public | BindingFlags.Static);
            if (target == null || handler == null) throw new MissingMethodException(type.FullName, methodName);
            HarmonyMethod patch = new HarmonyMethod(handler);
            if (postfix) harmony.Patch(target, postfix: patch);
            else harmony.Patch(target, prefix: patch);
        }
    }
}


