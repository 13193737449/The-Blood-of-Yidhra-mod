using System;
using System.Collections.Generic;
using System.Reflection;
namespace YidhraTrainer
{
    public static class CombatPatches
    {
        private static readonly MethodInfo Clone = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo DamageField = typeof(DamageInfo).GetField("<Damage>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
        [ThreadStatic] private static int playerShotDepth;

        public static void DamagePrefix(BaseObject __instance, ref DamageInfo __0)
        {
            if ((!TrainerState.DamageBoost && !TrainerState.OneHitKill) || !(__instance is BaseMoster) ||
                __0 == null || !(__0.Source is PlayerController) || __0.Damage <= 0) return;
            double damage = __0.Damage;
            if (TrainerState.OneHitKill)
                damage = Math.Max(damage, Math.Ceiling(__instance.Life));
            else
                damage *= Math.Max(1, Math.Min(100, TrainerState.DamageMultiplier));
            int value = (int)Math.Min(int.MaxValue, damage);
            if (value == __0.Damage) return;
            // Preserve all damage metadata without re-running the weapon-dependent constructor.
            DamageInfo copy = (DamageInfo)Clone.Invoke(__0, null);
            DamageField.SetValue(copy, value);
            __0 = copy;
        }

        public static void BulletPrefix(ref Dictionary<string, object> __0)
        {
            object user;
            if (!TrainerState.NoSpread || __0 == null || !__0.ContainsKey("offsetangle") ||
                !__0.TryGetValue("user", out user) || !(user is PlayerController)) return;
            var copy = new Dictionary<string, object>(__0);
            copy["offsetangle"] = 0f;
            __0 = copy;
        }

        public static void ShotPrefix(BaseCharacter __0, out bool __state)
        {
            __state = __0 is PlayerController;
            if (__state) playerShotDepth++;
        }

        public static Exception ShotFinalizer(bool __state, Exception __exception)
        {
            if (__state) playerShotDepth = Math.Max(0, playerShotDepth - 1);
            return __exception;
        }

        public static bool ShakePrefix()
        {
            return !TrainerState.NoRecoil || playerShotDepth == 0;
        }
    }
}

