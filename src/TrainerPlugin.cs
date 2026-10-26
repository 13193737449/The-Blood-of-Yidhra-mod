using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace YidhraTrainer
{
    [BepInPlugin(PluginId, "Yidhra Trainer", Version)]
    [BepInProcess("The Blood of Yidhra.exe")]
    public sealed class TrainerPlugin : BaseUnityPlugin
    {
        public const string PluginId = "local.yidhra.trainer";
        public const string Version = "1.1.0";
        private Harmony harmony;
        private ConfigEntry<bool> health, stamina, ammo, items, panel, damage, kill, recoil, spread;
        private ConfigEntry<int> multiplier;
        private ConfigEntry<KeyCode> healthKey, staminaKey, ammoKey, itemsKey, panelKey, damageKey, killKey, recoilKey, spreadKey;
        private bool ready;
        private Rect window;
        private GUIStyle labelStyle, toggleStyle, buttonStyle, windowStyle, fieldStyle;
        private Font font;
        private string failure, goldInput = "100000", goldMessage = "";
        private int tab;
        private static readonly int[] Multipliers = new int[] { 2, 5, 10, 20, 50, 100 };

        private void Awake()
        {
            health = Feature("InfiniteHealth", "无限生命：仅玩家，保留受击反馈。");
            stamina = Feature("InfiniteStamina", "无限体力。");
            ammo = Feature("InfiniteAmmo", "无限弹药，无需换弹；关闭恢复真实弹匣。");
            items = Feature("InfiniteItems", "急救包与手雷不减；仍需拥有足够数量。");
            damage = Feature("DamageBoost", "玩家对敌人伤害倍率。");
            kill = Feature("OneHitKill", "玩家正伤害命中敌人时秒杀，优先于伤害倍率。");
            recoil = Feature("NoRecoil", "仅取消玩家开火镜头震动，保留剧情震动。");
            spread = Feature("NoSpread", "独立取消玩家子弹散射。");
            multiplier = Config.Bind("Combat", "DamageMultiplier", 5, new ConfigDescription("伤害倍率1至100。", new AcceptableValueRange<int>(1, 100)));
            panel = Config.Bind("Interface", "ShowPanel", true, "显示中文面板，不暂停游戏。");
            healthKey = Hotkey("Health", KeyCode.F1); staminaKey = Hotkey("Stamina", KeyCode.F2);
            ammoKey = Hotkey("Ammo", KeyCode.F3); itemsKey = Hotkey("Items", KeyCode.F4);
            damageKey = Hotkey("Damage", KeyCode.F5); recoilKey = Hotkey("Recoil", KeyCode.F6);
            killKey = Hotkey("OneHitKill", KeyCode.F7); spreadKey = Hotkey("NoSpread", KeyCode.F9);
            panelKey = Hotkey("Panel", KeyCode.F8);
            window = new Rect(Math.Max(10, Screen.width - 470), 90, 450, 480);
            try
            {
                UnicodePathCompatibility.Install();
                harmony = new Harmony(PluginId);
                int count = PatchInstaller.Install(harmony);
                ready = true;
                SyncState();
                Logger.LogInfo("Yidhra Trainer ready v" + Version + "; " + count + " patches installed. F8 panel.");
            }
            catch (Exception ex)
            {
                TrainerState.DisableAll();
                failure = "补丁加载失败，请检查 BepInEx 日志。";
                Logger.LogError("Yidhra Trainer failed: " + ex);
                if (ex.Data.Contains("RollbackFailure")) Logger.LogError("Trainer rollback failed: " + ex.Data["RollbackFailure"]);
            }
        }

        private ConfigEntry<bool> Feature(string name, string description) { return Config.Bind("Features", name, false, description); }
        private ConfigEntry<KeyCode> Hotkey(string name, KeyCode key) { return Config.Bind("Hotkeys", name, key, "修改器快捷键，可自定义。"); }
        private void SyncState()
        {
            TrainerState.InfiniteHealth = ready && health.Value;
            TrainerState.InfiniteStamina = ready && stamina.Value;
            TrainerState.InfiniteAmmo = ready && ammo.Value;
            TrainerState.InfiniteItems = ready && items.Value;
            TrainerState.DamageBoost = ready && damage.Value;
            TrainerState.OneHitKill = ready && kill.Value;
            TrainerState.NoRecoil = ready && recoil.Value;
            TrainerState.NoSpread = ready && spread.Value;
            TrainerState.DamageMultiplier = multiplier.Value;
        }
        private void Update()
        {
            if (Input.GetKeyDown(panelKey.Value)) Set(panel, !panel.Value);
            if (!ready) return;
            Key(health, healthKey); Key(stamina, staminaKey); Key(ammo, ammoKey); Key(items, itemsKey);
            Key(damage, damageKey); Key(kill, killKey); Key(recoil, recoilKey); Key(spread, spreadKey);
        }
        private void Key(ConfigEntry<bool> value, ConfigEntry<KeyCode> key) { if (Input.GetKeyDown(key.Value)) Set(value, !value.Value); }
        private void Set(ConfigEntry<bool> setting, bool value)
        {
            if (setting.Value == value) return;
            setting.Value = value; SyncState(); Config.Save();
            Logger.LogInfo(setting.Definition.Key + " = " + value);
        }
        private void InitStyles()
        {
            font = Font.CreateDynamicFontFromOSFont(new string[] { "Microsoft YaHei", "SimHei", "Arial" }, 18);
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
            toggleStyle = new GUIStyle(GUI.skin.toggle) { fontSize = 18 };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 16 };
            fieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = 18 };
            windowStyle = new GUIStyle(GUI.skin.window) { fontSize = 18 };
            if (font != null) labelStyle.font = toggleStyle.font = buttonStyle.font = fieldStyle.font = windowStyle.font = font;
        }
        private void OnGUI()
        {
            if (panel == null) return;
            if (labelStyle == null) InitStyles();
            if (!panel.Value)
            {
                GUI.Label(new Rect(15, Math.Max(0, Screen.height - 35), 470, 30), "伊德海拉修改器  |  " + panelKey.Value + " 打开面板", labelStyle);
                return;
            }
            window.width = Math.Min(450, Math.Max(280, Screen.width - 10));
            window.x = Mathf.Clamp(window.x, 0, Math.Max(0, Screen.width - window.width));
            window.y = Mathf.Clamp(window.y, 0, Math.Max(0, Screen.height - window.height));
            window = GUI.Window(153683, window, DrawWindow, "伊德海拉之血 · 修改器 " + Version, windowStyle);
        }
        private void DrawWindow(int id)
        {
            bool oldEnabled = GUI.enabled;
            try
            {
                GUI.Label(new Rect(18, 32, window.width - 36, 28), ready ? "独立开关 · 设置自动保存" : failure, labelStyle);
                GUI.enabled = oldEnabled && ready;
                string[] names = new string[] { "资源", "战斗", "金币" };
                float width = (window.width - 44) / 3;
                for (int i = 0; i < 3; i++)
                    if (GUI.Button(new Rect(18 + i * (width + 4), 68, width, 32), (tab == i ? "● " : "") + names[i], buttonStyle)) tab = i;
                if (tab == 0) DrawResources();
                else if (tab == 1) DrawCombat();
                else DrawGold();
                if (GUI.Button(new Rect(18, 396, 160, 32), "全部关闭", buttonStyle))
                {
                    Set(health, false); Set(stamina, false); Set(ammo, false); Set(items, false);
                    Set(damage, false); Set(kill, false); Set(recoil, false); Set(spread, false);
                }
                GUI.enabled = oldEnabled;
                if (GUI.Button(new Rect(window.width - 178, 396, 160, 32), "隐藏 [" + panelKey.Value + "]", buttonStyle)) Set(panel, false);
                GUI.Label(new Rect(18, 438, window.width - 36, 30), "面板不暂停游戏；金币应用后不会自动撤销。", labelStyle);
            }
            finally { GUI.enabled = oldEnabled; }
            GUI.DragWindow(new Rect(0, 0, window.width, 30));
        }
        private void DrawResources()
        {
            Toggle(health, healthKey, "无限生命", 115); Toggle(stamina, staminaKey, "无限体力", 160);
            Toggle(ammo, ammoKey, "无限弹药 / 无需换弹", 205); Toggle(items, itemsKey, "急救包、手雷不消耗", 250);
            GUI.Label(new Rect(18, 305, window.width - 36, 66), "道具需先持有。关闭无限弹药后恢复原有真实弹匣及备用数量。", labelStyle);
        }
        private void DrawCombat()
        {
            Toggle(damage, damageKey, "伤害提升 ×" + multiplier.Value, 110);
            float width = (window.width - 56) / 6;
            for (int i = 0; i < Multipliers.Length; i++)
                if (GUI.Button(new Rect(18 + i * (width + 4), 150, width, 30), Multipliers[i] + "×", buttonStyle))
                { multiplier.Value = Multipliers[i]; SyncState(); Config.Save(); }
            Toggle(kill, killKey, "秒杀（优先于伤害倍率）", 200);
            Toggle(recoil, recoilKey, "无后座力（开火镜头不震动）", 245);
            Toggle(spread, spreadKey, "子弹不散射（独立开关）", 290);
            GUI.Label(new Rect(18, 337, window.width - 36, 50), "仅增强玩家对敌人的攻击；保留剧情无敌和正常死亡流程。", labelStyle);
        }
        private DataManager ActiveData()
        {
            var data = Managers.Datamanager;
            if (data == null) return null;
            var player = data.GetPlayer();
            return player != null && player.Life > 0 ? data : null;
        }
        private void DrawGold()
        {
            DataManager data = ready ? ActiveData() : null;
            int balance;
            bool available = GoldOperations.TryGetBalance(data, out balance);
            GUI.Label(new Rect(18, 115, window.width - 36, 30), available ? "当前金币：" + balance : "进入关卡并载入角色后可修改金币", labelStyle);
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && available;
            if (GUI.Button(new Rect(18, 160, (window.width - 44) / 2, 34), "+ 10,000", buttonStyle)) GoldOperations.TryAdd(data, 10000, out goldMessage);
            if (GUI.Button(new Rect(window.width / 2 + 4, 160, (window.width - 44) / 2, 34), "+ 100,000", buttonStyle)) GoldOperations.TryAdd(data, 100000, out goldMessage);
            GUI.Label(new Rect(18, 216, 105, 30), "目标余额：", labelStyle);
            goldInput = GUI.TextField(new Rect(123, 212, Math.Max(70, window.width - 245), 32), goldInput, 10, fieldStyle);
            if (GUI.Button(new Rect(window.width - 112, 212, 94, 32), "应用", buttonStyle))
            {
                int value;
                if (!int.TryParse(goldInput, out value)) goldMessage = "请输入0至999999999的整数。";
                else GoldOperations.TrySet(data, value, out goldMessage);
            }
            GUI.enabled = enabled;
            GUI.Label(new Rect(18, 263, window.width - 36, 62), goldMessage, labelStyle);
            GUI.Label(new Rect(18, 330, window.width - 36, 58), "修改真实共享余额；正常保存游戏后会写入存档。全部关闭不会恢复旧余额。", labelStyle);
        }
        private void Toggle(ConfigEntry<bool> entry, ConfigEntry<KeyCode> key, string text, float y)
        {
            bool value = GUI.Toggle(new Rect(22, y, window.width - 44, 30), entry.Value, "[" + key.Value + "] " + text, toggleStyle);
            if (value != entry.Value) Set(entry, value);
        }
        private void OnDestroy()
        {
            ready = false; TrainerState.DisableAll();
            try { if (harmony != null) harmony.UnpatchSelf(); }
            catch (Exception ex) { Logger.LogError("Trainer cleanup failed: " + ex.Message); }
            finally { UnicodePathCompatibility.Restore(); }
            if (font != null) UnityEngine.Object.Destroy(font);
        }
    }
}
