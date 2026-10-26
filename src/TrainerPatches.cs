using System;

namespace YidhraTrainer
{
    // Parameters use Harmony's argument-index convention, independent of game parameter names.
    public static class TrainerPatches
    {
        public static void LifePrefix(BaseObject __instance, ref float __0)
        {
            if (TrainerState.InfiniteHealth && __instance is PlayerController && __instance.MaxLife > 0)
                __0 = __instance.MaxLife;
        }

        public static void EnergyPrefix(PlayerController __instance, ref float __0)
        {
            if (TrainerState.InfiniteStamina && __instance.MaxEnergy > 0)
                __0 = __instance.MaxEnergy;
        }

        public static void AmmoCountPostfix(InventoryController __instance, ref int __result)
        {
            if (!TrainerState.InfiniteAmmo) return;
            BaseWeapon weapon;
            if (!__instance.GetWeaponEquipments().TryGetValue(__instance.SeletedItem, out weapon)) return;
            BaseGun gun = weapon as BaseGun;
            if (gun != null)
                __result = Math.Max(__result, Math.Max(1, gun.ClipCapacity));
        }

        public static bool ShootPrefix()
        {
            return !TrainerState.InfiniteAmmo;
        }

        public static bool ReloadPrefix()
        {
            return !TrainerState.InfiniteAmmo;
        }

        public static bool UsePropPrefix(InventoryController __instance, string __0, int __1, ref bool __result)
        {
            // Coin purchases/upgrades and quest items retain their normal consumption.
            if (!TrainerState.InfiniteItems || (__0 != "AidKit" && __0 != "Grenade") || __1 <= 0)
                return true;
            if (__instance.GetPropsCount(__0) < __1) return true;
            __result = true;
            return false;
        }
    }
}
