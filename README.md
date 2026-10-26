# 伊德海拉之血修改器 · Yidhra Trainer

适用于《伊德海拉之血》（The Blood of Yidhra）1.0 的 BepInEx 5 插件，当前插件版本 **1.1.0**。提供中文面板，各项功能可独立开关，快捷键可自定义。

## 功能

| 快捷键 | 功能 |
| --- | --- |
| F1 | 无限生命 |
| F2 | 无限体力 |
| F3 | 无限弹药，无需换弹 |
| F4 | 急救包、手雷使用不减，使用前仍需持有 |
| F5 | 伤害提升，面板可选 2 / 5 / 10 / 20 / 50 / 100 倍 |
| F6 | 无后座力：关闭玩家开火镜头震动，保留剧情震动 |
| F7 | 秒杀，玩家正伤害命中敌人时生效 |
| F9 | 子弹不散射，与无后座力独立 |
| F8 | 显示 / 隐藏中文面板 |
| 面板金币页 | 查看余额、增加金币或设置指定余额 |

功能开关默认关闭；面板提供“全部关闭”。金币是一次性修改，关闭开关不会恢复原余额。面板不暂停游戏。

## 环境要求

- Windows x64，Unity Mono 版游戏。
- 游戏目录已安装兼容的 **BepInEx 5**。当前验证环境为 BepInEx 5.4.23.5、HarmonyX 2.9.0。
- 本仓库不包含游戏本体、游戏程序集或 BepInEx；依赖由使用者自行准备。
- 其他游戏版本及 IL2CPP 版本未验证。

## 下载与安装

1. 在仓库页面选择 **Code → Download ZIP** 并解压，或克隆本仓库。
2. 正常退出游戏。将 `dist/YidhraTrainer.dll` 复制到游戏目录的 `BepInEx/plugins/YidhraTrainer/` 下；文件夹不存在时可自行创建。
3. 启动游戏，按 **F8** 打开中文面板。

也可在解压后的项目目录运行安装脚本，显式指定自己的游戏目录：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Install-Trainer.ps1 -GamePath 'D:\Games\The Blood of Yidhra'
```

双击 `安装修改器.cmd` 使用默认相邻目录布局：项目文件夹和名为 `The Blood of Yidhra` 的游戏文件夹位于同一父目录。其他布局请使用上面的 `-GamePath` 命令。

快捷键配置位于游戏目录 `BepInEx/config/local.yidhra.trainer.cfg`，关闭游戏后可编辑。重要进度建议先备份存档；金币等修改可能通过游戏正常保存写入存档。

## 卸载

退出游戏，删除 `BepInEx/plugins/YidhraTrainer/YidhraTrainer.dll`，或运行：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Install-Trainer.ps1 -GamePath 'D:\Games\The Blood of Yidhra' -Uninstall
```

卸载保留配置、存档及其他插件。

## 从源码编译

需要 Windows 的 .NET Framework 4.x C# 编译器，以及本机游戏和 BepInEx 的程序集。脚本会检查依赖，使用 `src/` 下的源码生成 `dist/YidhraTrainer.dll`。

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Build.ps1 -GamePath 'D:\Games\The Blood of Yidhra'
```

## 测试与验证范围

在项目根目录依次运行，将示例游戏路径替换为自己的路径：

```powershell
$gamePath = 'D:\Games\The Blood of Yidhra'
.\tests\Run-Tests.ps1 -GamePath $gamePath
.\tests\Run-GoldTests.ps1 -GamePath $gamePath
.\tests\Run-CombatTests.ps1 -GamePath $gamePath
.\tests\Run-UnicodePathTests.ps1 -GamePath $gamePath
.\tests\Run-PatchSmokeTests.ps1 -GamePath $gamePath
.\tests\Run-MonoTests.ps1 -GamePath $gamePath
.\tests\Run-InstallTests.ps1
```

先运行资源行为测试以创建 `tests/bin/`，然后编译其他测试。Harmony 整合测试使用游戏自带的 Mono；普通 .NET 测试不能代替 Unity 场景验证。测试产物和日志由脚本在本地生成，不纳入源码仓库。

1.1.0 已有资源、战斗、金币、中文路径兼容、17 处补丁注册及真实 Unity 启动的验证记录。完整游玩、各类敌人和剧情阶段仍需实际游戏验收，不保证其他版本适配。可在游戏的 `BepInEx/LogOutput.log` 中检查：

```text
Yidhra Trainer ready v1.1.0; 17 patches installed
```

## 项目结构

- `src/`：插件源码。
- `dist/YidhraTrainer.dll`：编译好的插件。
- `Build.ps1`：构建脚本。
- `Install-Trainer.ps1`、安装 / 卸载 `.cmd`：安装和卸载入口。
- `tests/`：测试源码与运行脚本。
- `使用说明.md`：详细功能与版本说明。

## 许可证

本项目代码使用 [MIT License](LICENSE)。游戏及第三方依赖的权利和许可证归各自权利人，本项目许可证不改变它们的授权范围。
