using System.Collections.Generic;
using System.Reflection;

namespace YidhraTrainer
{
    public static class GoldOperations
    {
        private const int MaximumBalance = 999999999;
        private static readonly FieldInfo PlayersInventoryField = typeof(DataManager).GetField(
            "PlayersInventory", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static bool TryGetBalance(DataManager data, out int balance)
        {
            balance = 0;
            Dictionary<string, int> props;
            if (!TryGetSharedProps(data, out props)) return false;
            int current;
            props.TryGetValue("Coin", out current);
            if (current < 0 || current > MaximumBalance) return false;
            balance = current;
            return true;
        }

        public static bool TrySet(DataManager data, int amount, out string message)
        {
            if (amount < 0 || amount > MaximumBalance)
            {
                message = "金币数量须为 0 至 999999999。";
                return false;
            }
            Dictionary<string, int> props;
            if (!TryGetSharedProps(data, out props))
            {
                message = "共享背包尚未初始化，无法修改金币。";
                return false;
            }
            // Coin is shared through PlayersInventory[0]. DataManager.AddProp also
            // updates the current inventory and can apply an addition twice.
            props["Coin"] = amount;
            message = "金币已设为 " + amount + "。";
            return true;
        }

        public static bool TryAdd(DataManager data, int delta, out string message)
        {
            if (delta < 0)
            {
                message = "增加的金币数量不能为负数。";
                return false;
            }
            Dictionary<string, int> props;
            if (!TryGetSharedProps(data, out props))
            {
                message = "共享背包尚未初始化，无法修改金币。";
                return false;
            }
            int current;
            props.TryGetValue("Coin", out current);
            if (current < 0 || current > MaximumBalance)
            {
                message = "当前金币数量超出有效范围，请先设置有效数量。";
                return false;
            }
            long next = (long)current + delta;
            if (next > MaximumBalance)
            {
                message = "增加后金币将超过上限 999999999。";
                return false;
            }
            props["Coin"] = (int)next;
            message = "金币已增加 " + delta + "，当前 " + next + "。";
            return true;
        }

        private static bool TryGetSharedProps(DataManager data, out Dictionary<string, int> props)
        {
            props = null;
            if (data == null || PlayersInventoryField == null) return false;
            List<InventoryController> inventories = PlayersInventoryField.GetValue(data) as List<InventoryController>;
            if (inventories == null || inventories.Count == 0 || inventories[0] == null) return false;
            props = inventories[0].GetPropsList();
            return props != null;
        }
    }
}
