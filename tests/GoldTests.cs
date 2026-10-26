using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using YidhraTrainer;

internal static class GoldTests
{
    private static string managedPath;
    private static int passed;
    private static int failed;

    private static int Main(string[] args)
    {
        managedPath = args[0];
        AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs e) {
            string path = Path.Combine(managedPath, new AssemblyName(e.Name).Name + ".dll");
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        };
        try { Run(); }
        catch (Exception error) { Console.WriteLine("FATAL: " + error); return 2; }
        Console.WriteLine("RESULT: " + passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run()
    {
        Test("reads real shared balance, ignoring current shadow balance", ReadShared);
        Test("sets shared balance only, preserving every other item", SetShared);
        Test("adds once when current inventory is the shared inventory", AddSameInventory);
        Test("adds to shared inventory without touching another current inventory", AddDifferentInventory);
        Test("real purchases consume updated shared balance", RealPurchase);
        Test("missing Coin starts at zero", MissingCoin);
        Test("zero and maximum balances accepted", Boundaries);
        Test("negative and over-limit set rejected without mutation", InvalidSet);
        Test("negative additions, over-limit additions and integer overflow rejected", InvalidAdd);
        Test("uninitialized manager and inventories fail safely", Uninitialized);
        Test("invalid existing balance rejected and explicit set can repair it", InvalidExisting);
    }

    private static void Test(string name, Action action)
    {
        try { action(); passed++; Console.WriteLine("PASS: " + name); }
        catch (Exception e) { failed++; Console.WriteLine("FAIL: " + name + " :: " + e); }
    }

    private static void Equal<T>(T expected, T actual, string description)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception(description + ": expected " + expected + ", actual " + actual);
    }

    private static T Bare<T>() { return (T)FormatterServices.GetUninitializedObject(typeof(T)); }

    private static void Field(object instance, string name, object value)
    {
        FieldInfo field = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field == null) throw new MissingFieldException(instance.GetType().FullName, name);
        field.SetValue(instance, value);
    }

    private static InventoryController Inventory(int coin)
    {
        InventoryController inventory = Bare<InventoryController>();
        Field(inventory, "PropsInventory", new Dictionary<string, int> {
            {"Coin", coin}, {"AidKit", 3}, {"Grenade", 4}, {"QuestKey", 2}, {"Bullet", 88} });
        return inventory;
    }

    private static DataManager Manager(InventoryController shared, InventoryController current)
    {
        DataManager data = Bare<DataManager>();
        Field(data, "PlayersInventory", new List<InventoryController> {shared});
        Field(data, "Inventory", current);
        return data;
    }

    private static void ReadShared()
    {
        DataManager data = Manager(Inventory(50), Inventory(999));
        int balance;
        Equal(true, GoldOperations.TryGetBalance(data, out balance), "read succeeds");
        Equal(50, balance, "shared balance returned");
        Equal(data.GetPropsCount("Coin"), balance, "matches real DataManager getter");
    }

    private static void SetShared()
    {
        InventoryController shared = Inventory(50), current = Inventory(999);
        DataManager data = Manager(shared, current);
        Dictionary<string, int> snapshot = new Dictionary<string, int>(shared.GetPropsList());
        string message;
        Equal(true, GoldOperations.TrySet(data, 777, out message), "set succeeds");
        Equal(777, data.GetPropsCount("Coin"), "real getter updated");
        Equal(999, current.GetPropsCount("Coin"), "shadow unchanged");
        Equal(snapshot.Count, shared.GetPropsList().Count, "dictionary entry count unchanged");
        foreach (KeyValuePair<string, int> item in snapshot)
            if (item.Key != "Coin") Equal(item.Value, shared.GetPropsCount(item.Key), "preserved " + item.Key);
        Equal(true, !string.IsNullOrEmpty(message), "feedback present");
    }

    private static void AddSameInventory()
    {
        InventoryController inventory = Inventory(50);
        DataManager data = Manager(inventory, inventory);
        string message;
        Equal(true, GoldOperations.TryAdd(data, 10, out message), "add succeeds");
        Equal(60, data.GetPropsCount("Coin"), "added exactly once");
    }

    private static void AddDifferentInventory()
    {
        InventoryController shared = Inventory(50), current = Inventory(999);
        DataManager data = Manager(shared, current);
        string message;
        Equal(true, GoldOperations.TryAdd(data, 10, out message), "add succeeds");
        Equal(60, shared.GetPropsCount("Coin"), "shared incremented");
        Equal(999, current.GetPropsCount("Coin"), "shadow unchanged");
        Equal(3, shared.GetPropsCount("AidKit"), "consumable unchanged");
        Equal(88, current.GetPropsCount("Bullet"), "current ammunition unchanged");
    }

    private static void RealPurchase()
    {
        InventoryController current = Inventory(999);
        DataManager data = Manager(Inventory(1), current);
        string message;
        Equal(true, GoldOperations.TrySet(data, 100, out message), "set purchasing balance");
        Equal(true, data.UseProp("Coin", 80), "real game purchase succeeds");
        Equal(20, data.GetPropsCount("Coin"), "real game purchase consumes balance");
        Equal(false, data.UseProp("Coin", 21), "insufficient real purchase fails");
        Equal(20, data.GetPropsCount("Coin"), "failed purchase retains balance");
        Equal(999, current.GetPropsCount("Coin"), "purchase shadow unchanged");
    }

    private static void MissingCoin()
    {
        InventoryController inventory = Inventory(0);
        inventory.GetPropsList().Remove("Coin");
        DataManager data = Manager(inventory, null);
        int balance;
        string message;
        Equal(true, GoldOperations.TryGetBalance(data, out balance), "missing coin readable");
        Equal(0, balance, "missing balance zero");
        Equal(true, GoldOperations.TryAdd(data, 7, out message), "add creates coin");
        Equal(7, data.GetPropsCount("Coin"), "coin created");
    }

    private static void Boundaries()
    {
        DataManager data = Manager(Inventory(1), null);
        string message;
        Equal(true, GoldOperations.TrySet(data, 0, out message), "zero valid");
        Equal(0, data.GetPropsCount("Coin"), "zero set");
        Equal(true, GoldOperations.TrySet(data, 999999999, out message), "maximum valid");
        Equal(true, GoldOperations.TryAdd(data, 0, out message), "zero addition valid");
        Equal(999999999, data.GetPropsCount("Coin"), "maximum preserved");
    }

    private static void InvalidSet()
    {
        DataManager data = Manager(Inventory(50), null);
        string message;
        foreach (int amount in new[] {-1, int.MinValue, 1000000000, int.MaxValue})
        {
            Equal(false, GoldOperations.TrySet(data, amount, out message), "reject " + amount);
            Equal(50, data.GetPropsCount("Coin"), "invalid set preserves balance");
            Equal(true, !string.IsNullOrEmpty(message), "rejection feedback");
        }
    }

    private static void InvalidAdd()
    {
        DataManager data = Manager(Inventory(999999999), null);
        string message;
        foreach (int delta in new[] {-1, int.MinValue, 1, int.MaxValue})
        {
            Equal(false, GoldOperations.TryAdd(data, delta, out message), "reject delta " + delta);
            Equal(999999999, data.GetPropsCount("Coin"), "invalid add preserves balance");
        }
    }

    private static void AssertUnavailable(DataManager data)
    {
        int balance;
        string message;
        Equal(false, GoldOperations.TryGetBalance(data, out balance), "unavailable read");
        Equal(0, balance, "failed read initializes output");
        Equal(false, GoldOperations.TrySet(data, 10, out message), "unavailable set");
        Equal(true, !string.IsNullOrEmpty(message), "set feedback");
        Equal(false, GoldOperations.TryAdd(data, 10, out message), "unavailable add");
    }

    private static void Uninitialized()
    {
        AssertUnavailable(null);
        DataManager data = Bare<DataManager>();
        AssertUnavailable(data);
        Field(data, "PlayersInventory", new List<InventoryController>());
        AssertUnavailable(data);
        Field(data, "PlayersInventory", new List<InventoryController> {null});
        AssertUnavailable(data);
        AssertUnavailable(Manager(Bare<InventoryController>(), Inventory(50)));
    }

    private static void InvalidExisting()
    {
        string message;
        int balance;
        foreach (int invalid in new[] {-5, int.MaxValue})
        {
            DataManager data = Manager(Inventory(invalid), null);
            Equal(false, GoldOperations.TryGetBalance(data, out balance), "invalid stored balance rejected");
            Equal(false, GoldOperations.TryAdd(data, 1, out message), "invalid stored balance cannot add");
            Equal(invalid, data.GetPropsCount("Coin"), "rejection retains data");
            Equal(true, GoldOperations.TrySet(data, 50, out message), "explicit valid set repairs balance");
            Equal(50, data.GetPropsCount("Coin"), "repaired balance");
        }
    }
}
