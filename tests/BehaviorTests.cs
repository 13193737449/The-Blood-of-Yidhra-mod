using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using YidhraTrainer;

internal static class BehaviorTests
{
    private static int passed;
    private static int failed;
    private static string managedPath;

    private static int Main(string[] args)
    {
        managedPath = args[0];
        AppDomain.CurrentDomain.AssemblyResolve += ResolveGameAssembly;
        try { Run(); }
        catch (Exception error) { Console.WriteLine("FATAL: " + error); return 2; }
        Console.WriteLine("RESULT: " + passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }

    private static Assembly ResolveGameAssembly(object sender, ResolveEventArgs args)
    {
        string path = Path.Combine(managedPath, new AssemblyName(args.Name).Name + ".dll");
        return File.Exists(path) ? Assembly.LoadFrom(path) : null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run()
    {
        Test("health only affects player and honors toggle/zero maximum", Health);
        Test("stamina honors toggle and zero maximum", Stamina);
        Test("consumables preserve owned quantities only when enabled", Consumables);
        Test("insufficient, absent, zero and negative requests use original", InvalidConsumables);
        Test("coins, ammunition and quest items consume normally", OrdinaryItems);
        Test("gun display is positive without mutating actual inventories", AmmoDisplay);
        Test("melee display remains unchanged", MeleeDisplay);
        Test("shoot and reload preserve inventory while enabled", InfiniteShooting);
        Test("shoot and reload execute real game logic while disabled", NormalShooting);
    }

    private static void Test(string name, Action action)
    {
        TrainerState.InfiniteHealth = false;
        TrainerState.InfiniteStamina = false;
        TrainerState.InfiniteAmmo = false;
        TrainerState.InfiniteItems = false;
        try { action(); passed++; Console.WriteLine("PASS: " + name); }
        catch (Exception error) { failed++; Console.WriteLine("FAIL: " + name + " :: " + error); }
    }

    private static T Bare<T>() { return (T)FormatterServices.GetUninitializedObject(typeof(T)); }

    private static void Set(object instance, Type owner, string field, object value)
    {
        FieldInfo info = owner.GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (info == null) throw new MissingFieldException(owner.FullName, field);
        info.SetValue(instance, value);
    }

    private static void Equal<T>(T expected, T actual, string description)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception(description + ": expected " + expected + ", actual " + actual);
    }

    private static void Health()
    {
        PlayerController player = Bare<PlayerController>();
        Set(player, typeof(BaseObject), "<MaxLife>k__BackingField", 125);
        float next = -10;
        TrainerPatches.LifePrefix(player, ref next);
        Equal(-10f, next, "disabled damage unchanged");
        TrainerState.InfiniteHealth = true;
        TrainerPatches.LifePrefix(player, ref next);
        Equal(125f, next, "lethal damage replaced with maximum");
        BaseObject enemy = Bare<BaseObject>();
        Set(enemy, typeof(BaseObject), "<MaxLife>k__BackingField", 300);
        next = 2;
        TrainerPatches.LifePrefix(enemy, ref next);
        Equal(2f, next, "enemy damage unchanged");
        Set(player, typeof(BaseObject), "<MaxLife>k__BackingField", 0);
        next = 17;
        TrainerPatches.LifePrefix(player, ref next);
        Equal(17f, next, "uninitialized maximum ignored");
    }

    private static void Stamina()
    {
        PlayerController player = Bare<PlayerController>();
        Set(player, typeof(PlayerController), "<MaxEnergy>k__BackingField", 80);
        float next = 2;
        TrainerPatches.EnergyPrefix(player, ref next);
        Equal(2f, next, "disabled stamina unchanged");
        TrainerState.InfiniteStamina = true;
        TrainerPatches.EnergyPrefix(player, ref next);
        Equal(80f, next, "stamina maximum");
        Set(player, typeof(PlayerController), "<MaxEnergy>k__BackingField", 0);
        next = 12;
        TrainerPatches.EnergyPrefix(player, ref next);
        Equal(12f, next, "uninitialized stamina ignored");
    }

    private static InventoryController Inventory(BaseWeapon weapon, int magazine)
    {
        InventoryController inventory = Bare<InventoryController>();
        EquipmentSlot slot = (EquipmentSlot)0;
        Set(inventory, typeof(InventoryController), "<SeletedItem>k__BackingField", slot);
        Set(inventory, typeof(InventoryController), "WeaponEquipmentsDic", new Dictionary<EquipmentSlot, BaseWeapon> { {slot, weapon} });
        Set(inventory, typeof(InventoryController), "MagazineBulletList", new Dictionary<EquipmentSlot, int> { {slot, magazine} });
        Set(inventory, typeof(InventoryController), "PropsInventory", new Dictionary<string, int> {
            {"Grenade", 3}, {"AidKit", 2}, {"Coin", 20}, {"QuestKey", 2}, {"TestBullet", 30} });
        return inventory;
    }

    private static bool Use(InventoryController inventory, string name, int count)
    {
        bool result = false;
        if (TrainerPatches.UsePropPrefix(inventory, name, count, ref result)) result = inventory.UseProp(name, count);
        return result;
    }

    private static void Consumables()
    {
        InventoryController inventory = Inventory(null, 0);
        TrainerState.InfiniteItems = true;
        Equal(true, Use(inventory, "Grenade", 3), "grenade usage succeeds");
        Equal(3, inventory.GetPropsCount("Grenade"), "grenades preserved");
        Equal(true, Use(inventory, "AidKit", 1), "aid kit usage succeeds");
        Equal(2, inventory.GetPropsCount("AidKit"), "aid kits preserved");
        TrainerState.InfiniteItems = false;
        Equal(true, Use(inventory, "AidKit", 1), "normal usage succeeds");
        Equal(1, inventory.GetPropsCount("AidKit"), "normal usage consumes");
        Equal(true, Use(inventory, "AidKit", 1), "last item usage succeeds");
        Equal(0, inventory.GetPropsCount("AidKit"), "real game removes depleted key");
    }

    private static void InvalidConsumables()
    {
        InventoryController inventory = Inventory(null, 0);
        TrainerState.InfiniteItems = true;
        Equal(false, Use(inventory, "Grenade", 4), "insufficient inventory fails");
        inventory.GetPropsList().Remove("AidKit");
        Equal(false, Use(inventory, "AidKit", 1), "absent inventory fails");
        bool result = false;
        Equal(true, TrainerPatches.UsePropPrefix(inventory, "Grenade", 0, ref result), "zero delegated");
        Equal(true, TrainerPatches.UsePropPrefix(inventory, "Grenade", -1, ref result), "negative delegated");
        Equal(3, inventory.GetPropsCount("Grenade"), "invalid requests do not mutate in prefix");
    }

    private static void OrdinaryItems()
    {
        InventoryController inventory = Inventory(null, 0);
        TrainerState.InfiniteItems = true;
        foreach (string name in new[] {"Coin", "QuestKey", "TestBullet"})
        {
            int original = inventory.GetPropsCount(name);
            Equal(true, Use(inventory, name, 1), name + " usage succeeds");
            Equal(original - 1, inventory.GetPropsCount(name), name + " normal consumption");
        }
    }

    private static BaseGun Gun(int capacity)
    {
        Type gunType = null;
        foreach (Type type in typeof(BaseGun).Assembly.GetTypes())
            if (type != typeof(BaseGun) && typeof(BaseGun).IsAssignableFrom(type) && !type.IsAbstract) { gunType = type; break; }
        if (gunType == null) throw new Exception("No concrete game gun found");
        BaseGun gun = (BaseGun)FormatterServices.GetUninitializedObject(gunType);
        Set(gun, typeof(BaseGun), "<ClipCapacity>k__BackingField", capacity);
        Set(gun, typeof(BaseGun), "<BulletType>k__BackingField", "TestBullet");
        return gun;
    }

    private static void AmmoDisplay()
    {
        InventoryController inventory = Inventory(Gun(12), 0);
        int count = inventory.GetSelectedWeaponBulletNumber();
        TrainerPatches.AmmoCountPostfix(inventory, ref count);
        Equal(0, count, "disabled empty magazine unchanged");
        TrainerState.InfiniteAmmo = true;
        TrainerPatches.AmmoCountPostfix(inventory, ref count);
        Equal(12, count, "capacity displayed for empty gun");
        Equal(0, inventory.GetSelectedWeaponBulletNumber(), "actual empty magazine preserved");
        Equal(30, inventory.GetInventoryBulletNumber("TestBullet"), "reserve preserved");
        inventory = Inventory(Gun(0), 0);
        count = 0;
        TrainerPatches.AmmoCountPostfix(inventory, ref count);
        Equal(1, count, "zero-capacity gun still passes firing gate");
        count = 99;
        TrainerPatches.AmmoCountPostfix(inventory, ref count);
        Equal(99, count, "higher existing count retained");
    }

    private static void MeleeDisplay()
    {
        InventoryController inventory = Inventory(Bare<Hand_01>(), 0);
        TrainerState.InfiniteAmmo = true;
        int count = 0;
        TrainerPatches.AmmoCountPostfix(inventory, ref count);
        Equal(0, count, "melee unchanged");
    }

    private static void InfiniteShooting()
    {
        InventoryController inventory = Inventory(Gun(12), 4);
        TrainerState.InfiniteAmmo = true;
        Equal(false, TrainerPatches.ShootPrefix(), "shoot consumption skipped");
        if (TrainerPatches.ShootPrefix()) inventory.ShootBullet();
        Equal(false, TrainerPatches.ReloadPrefix(), "reload consumption skipped");
        if (TrainerPatches.ReloadPrefix()) inventory.ReloadBullet(12);
        Equal(4, inventory.GetSelectedWeaponBulletNumber(), "magazine preserved");
        Equal(30, inventory.GetInventoryBulletNumber("TestBullet"), "reserve preserved");
    }

    private static void NormalShooting()
    {
        InventoryController inventory = Inventory(Gun(12), 4);
        Equal(true, TrainerPatches.ShootPrefix(), "normal shoot allowed");
        if (TrainerPatches.ShootPrefix()) inventory.ShootBullet();
        Equal(3, inventory.GetSelectedWeaponBulletNumber(), "real game shot consumes one");
        Equal(true, TrainerPatches.ReloadPrefix(), "normal reload allowed");
        if (TrainerPatches.ReloadPrefix()) inventory.ReloadBullet(12);
        Equal(12, inventory.GetSelectedWeaponBulletNumber(), "real game reload fills magazine");
        Equal(21, inventory.GetInventoryBulletNumber("TestBullet"), "real game reload consumes reserve");
    }
}
