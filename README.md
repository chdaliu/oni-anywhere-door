[中文](#chinese) | [English](#english)

<a id="chinese"></a>

# 任意门（ONI-AnywhereDoor）

《缺氧》Mod：添加一扇 1x2 的 **任意门**。同一世界中建成的任意门会自动组成最近邻传送网络，复制人自动寻路时会自动使用它。

## 特性

- **任意门网络**：所有建成的任意门属于同一世界内的连接网络。
- **自动寻路**：复制人寻路会把任意门网络当作道路，自动选择最近的入口进入。
- **瞬间传送**：传送过渡立即完成，没有动画、等待或排队。
- **无需目的地**：不需要选择目的地，任意门自动组网。
- **通行开关**（位于建筑详情面板下方的功能区）
  - 复制人通过：允许 / 禁止（默认允许）。
  - 小动物通过：允许 / 禁止（默认禁止，小动物只会物理穿过门所在位置，不会被传送）。
- **阻挡物质**：气体、液体和固体物质不能通过；只有复制人与小动物可以穿过。
- **矿石与金属建造**：同时需要 200 kg `BuildableRaw`（矿物原料）和 200 kg 金属矿石或精炼金属。
- **研究解锁**：与机械气闸相同，需先研究“除污”（`DirectedAirStreams`）。
- **不可损毁**：不会过热、损坏、淹没、掩埋；只能通过拆除指令移除。
- **不交换热量**：不参与结构温度模拟，门所在格也不参与热交换。
- **中英文界面**：根据游戏语言自动显示中文或英文。

## 使用

1. 研究“除污”后，在“基地 → 门”分类中建造任意门（1x2，放在地面上）。
2. 建造至少两扇门，它们会自动组成最近邻网络。
3. 正常给复制人安排任务，自动寻路会在合适时选择最近的任意门入口并使用网络。
4. 用详情面板下方功能区的“复制人”“小动物”按钮调整这扇门的行为。

## 构建

需要 .NET SDK（`dotnet`）和 Python 3 + Pillow（仅用于重新生成动画）。

```bash
./build.sh
```

构建完成后会自动部署到：

```text
~/Library/Application Support/unity.Klei.Oxygen Not Included/mods/Local/ONI-AnywhereDoor
~/Library/Application Support/unity.Klei.Oxygen Not Included/mods/Steam/ONI-AnywhereDoor
```

## 文件结构

```text
src/
  AnywhereDoorConfig.cs     建筑定义
  AnywhereDoor.cs           自动组网、通行与传送逻辑
  AnywhereDoorButtons.cs    功能区通行按钮
  AnywhereDoorMod.cs        Mod 入口与建造菜单注册
  STRINGS.cs                中英文字符串
  InstantTeleportPatch.cs   瞬间传送补丁
anim/assets/oni_anywhere_door/
  *_0.png / *_build.bytes / *_anim.bytes
tools/generate_door_anim.py 动画与图标生成脚本
```

## 备注

- 任意门只在同一世界内组网，避免跨世界传送带来的寻路/存档复杂度。
- 网络按最近邻顺序连接；任意门之间可以通过多跳互通。
- 小动物只获得物理通行权限，不会被传送。
- 旧存档中已经建好的门会保留原来的通行开关状态。

<a id="english"></a>

# Anywhere Door (ONI-AnywhereDoor)

An **Oxygen Not Included** mod that adds a 1x2 **Anywhere Door**. Completed doors in the same world automatically form a nearest-neighbour teleport network that duplicant pathfinding uses automatically.

## Features

- **Door network**: every completed door belongs to one network per world.
- **Automatic pathfinding**: duplicants treat the network as a route and pick the closest entrance.
- **Instant travel**: teleport transitions finish immediately, with no animation, waiting, or queue.
- **No destination needed**: doors connect automatically.
- **Passage toggles** (in the action area below the building details panel)
  - Duplicants: allowed / blocked (allowed by default).
  - Critters: allowed / blocked (blocked by default; critters only walk through and are never teleported).
- **Blocks matter**: gas, liquids, and solids cannot pass; only duplicants and critters can.
- **Mineral and metal construction**: requires both 200 kg of `BuildableRaw` (raw mineral) and 200 kg of metal ore or refined metal.
- **Research unlock**: same as the Mechanized Airlock; research **Decontamination** (`DirectedAirStreams`) first.
- **Indestructible**: cannot overheat, break, flood, or become entombed; only deconstruction removes it.
- **No heat exchange**: excluded from structure temperature simulation, including its cells.
- **Chinese and English UI**: text follows the current game language.

## Usage

1. Research **Decontamination**, then build the door under **Base → Doors** (1x2, on the floor).
2. Build at least two doors; they form a nearest-neighbour network automatically.
3. Assign tasks normally. Pathfinding will use the closest door entrance when useful.
4. Use the **Duplicants** and **Critters** buttons in the action area below the details panel to change passage.

## Build

Requires the .NET SDK (`dotnet`) and Python 3 + Pillow (only for regenerating the animation).

```bash
./build.sh
```

The build is deployed to:

```text
~/Library/Application Support/unity.Klei.Oxygen Not Included/mods/Local/ONI-AnywhereDoor
~/Library/Application Support/unity.Klei.Oxygen Not Included/mods/Steam/ONI-AnywhereDoor
```

## Project layout

```text
src/
  AnywhereDoorConfig.cs     Building definition
  AnywhereDoor.cs           Networking, passage, and teleport logic
  AnywhereDoorButtons.cs    Action-area passage buttons
  AnywhereDoorMod.cs        Mod entry and build-menu registration
  STRINGS.cs                Chinese/English strings
  InstantTeleportPatch.cs   Instant-teleport patch
anim/assets/oni_anywhere_door/
  *_0.png / *_build.bytes / *_anim.bytes
tools/generate_door_anim.py Animation and icon generator
```

## Notes

- Doors only network within the same world, avoiding cross-world pathfinding and save complexity.
- The network is ordered by nearest neighbours; doors can connect through multiple hops.
- Critters only receive physical passage and are never teleported.
- Doors already present in older saves keep their saved passage settings.
