namespace YidhraTrainer
{
    public static class TrainerState
    {
        public static bool InfiniteHealth;
        public static bool InfiniteStamina;
        public static bool InfiniteAmmo;
        public static bool InfiniteItems;
        public static bool DamageBoost, OneHitKill, NoRecoil, NoSpread;
        public static int DamageMultiplier = 5;

        public static void DisableAll()
        {
            InfiniteHealth = InfiniteStamina = InfiniteAmmo = InfiniteItems = false;
            DamageBoost = OneHitKill = NoRecoil = NoSpread = false;
        }
    }
}

