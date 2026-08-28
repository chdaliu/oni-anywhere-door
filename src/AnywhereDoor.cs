using System.Collections.Generic;
using KSerialization;
using UnityEngine;

namespace ONIAnywhereDoor
{
    /// <summary>
    /// 任意门的核心逻辑。
    ///
    /// 同一世界中的门按“最近邻”顺序组成一个环状传送网络：
    /// A → B → C → ... → A。每个门只需要一条到下一扇门的 NavTeleporter 链接，
    /// 复制人自动寻路时会把传送网络当作道路使用；路径查找会选择最近的入口和路线。
    ///
    /// InstantTeleportPatch 会把 Teleport 过渡立即完成，因此传送没有动画、等待或排队。
    ///
    /// SimCellOccupier 提供气体/液体/固体不可渗透属性；本组件把 Nav 层重新设为非固体，
    /// 因此只有生物可以穿过，物质不能通过，也不参与热交换。
    /// </summary>
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class AnywhereDoor : KMonoBehaviour, ISim200ms
    {
        public static readonly List<AnywhereDoor> AllDoors = new List<AnywhereDoor>();

        [Serialize]
        public bool allowDuplicants = true;

        [Serialize]
        public bool allowCritters = true;

        private NavTeleporter navTeleporter;
        private Building building;
        private int[] placementCells;
        private float linkRefreshTimer;
        private float passabilityRefreshTimer;
        private bool registered;

        public bool AllowDuplicants
        {
            get { return allowDuplicants; }
        }

        public bool AllowCritters
        {
            get { return allowCritters; }
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();

            building = GetComponent<Building>();
            navTeleporter = GetComponent<NavTeleporter>();

            if (!AllDoors.Contains(this))
            {
                AllDoors.Add(this);
            }
            registered = true;

            // BuildingComplete.OnSpawn 可能在本组件之后才设置建筑位置，
            // 因此延迟一点再读取 PlacementCells，避免缓存无效坐标。
            passabilityRefreshTimer = 0.5f;

            // 新门加入后让现有门尽快重建最近邻网络；存档载入时也会在 0.5 秒内完成。
            linkRefreshTimer = 0.5f;
            for (int i = 0; i < AllDoors.Count; i++)
            {
                AnywhereDoor other = AllDoors[i];
                if (other != null && other != this)
                {
                    other.linkRefreshTimer = 0.05f;
                }
            }
        }

        protected override void OnCleanUp()
        {
            if (registered)
            {
                registered = false;
                AllDoors.Remove(this);

                for (int i = 0; i < AllDoors.Count; i++)
                {
                    AnywhereDoor other = AllDoors[i];
                    if (other != null)
                    {
                        other.linkRefreshTimer = 0.05f;
                    }
                }
            }

            ClearPassability();

            if (navTeleporter != null)
            {
                navTeleporter.SetTarget(null);
            }

            base.OnCleanUp();
        }

        public void Sim200ms(float dt)
        {
            if (passabilityRefreshTimer > 0f)
            {
                passabilityRefreshTimer -= dt;
                if (passabilityRefreshTimer <= 0f)
                {
                    ApplyPassability();
                }
            }

            if (linkRefreshTimer > 0f)
            {
                linkRefreshTimer -= dt;
                if (linkRefreshTimer <= 0f)
                {
                    RefreshAllLinks();
                }
            }
        }

        public void SetAllowDuplicants(bool value)
        {
            if (allowDuplicants == value)
            {
                return;
            }

            allowDuplicants = value;
            ApplyPassability();
            RefreshAllLinks();
            RefreshUserMenu();
        }

        public void SetAllowCritters(bool value)
        {
            if (allowCritters == value)
            {
                return;
            }

            allowCritters = value;
            ApplyPassability();
            RefreshUserMenu();
        }

        public bool ContainsCell(int cell)
        {
            EnsurePlacementCells();
            if (placementCells == null)
            {
                return false;
            }

            for (int i = 0; i < placementCells.Length; i++)
            {
                if (placementCells[i] == cell)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsDoorCell(int cell)
        {
            for (int i = 0; i < AllDoors.Count; i++)
            {
                AnywhereDoor door = AllDoors[i];
                if (door != null && door.registered && door.ContainsCell(cell))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 每个世界单独组网。门先按坐标排序，再按最近邻顺序串成一个环：
        /// 每扇门链接到离它最近的下一扇门，最后一扇回到第一扇。
        /// 这样复制人寻路时可以从最近的入口进入网络，并沿较短的链路前进。
        /// </summary>
        public static void RefreshAllLinks()
        {
            Dictionary<int, List<AnywhereDoor>> groups = new Dictionary<int, List<AnywhereDoor>>();

            for (int i = 0; i < AllDoors.Count; i++)
            {
                AnywhereDoor door = AllDoors[i];
                if (door == null || !door.registered)
                {
                    continue;
                }

                int world = GetWorldId(door);
                if (world < 0)
                {
                    door.SetLinkTarget(null);
                    continue;
                }

                List<AnywhereDoor> group;
                if (!groups.TryGetValue(world, out group))
                {
                    group = new List<AnywhereDoor>();
                    groups[world] = group;
                }
                group.Add(door);
            }

            foreach (KeyValuePair<int, List<AnywhereDoor>> pair in groups)
            {
                List<AnywhereDoor> group = pair.Value;
                group.Sort(CompareDoors);

                List<AnywhereDoor> allowed = new List<AnywhereDoor>();
                for (int i = 0; i < group.Count; i++)
                {
                    AnywhereDoor door = group[i];
                    if (door.allowDuplicants && door.navTeleporter != null)
                    {
                        allowed.Add(door);
                    }
                    else
                    {
                        door.SetLinkTarget(null);
                    }
                }

                if (allowed.Count <= 1)
                {
                    for (int i = 0; i < allowed.Count; i++)
                    {
                        allowed[i].SetLinkTarget(null);
                    }
                    continue;
                }

                List<AnywhereDoor> ring = BuildNearestRing(allowed);
                for (int i = 0; i < ring.Count; i++)
                {
                    ring[i].SetLinkTarget(ring[(i + 1) % ring.Count]);
                }
            }
        }

        private static List<AnywhereDoor> BuildNearestRing(List<AnywhereDoor> doors)
        {
            List<AnywhereDoor> remaining = new List<AnywhereDoor>(doors);
            List<AnywhereDoor> ring = new List<AnywhereDoor>(doors.Count);

            AnywhereDoor current = remaining[0];
            remaining.RemoveAt(0);
            ring.Add(current);

            while (remaining.Count > 0)
            {
                AnywhereDoor nearest = null;
                int bestDistance = int.MaxValue;

                for (int i = 0; i < remaining.Count; i++)
                {
                    int distance = CellDistance(current, remaining[i]);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        nearest = remaining[i];
                    }
                }

                ring.Add(nearest);
                remaining.Remove(nearest);
                current = nearest;
            }

            return ring;
        }

        private void SetLinkTarget(AnywhereDoor target)
        {
            if (navTeleporter == null)
            {
                return;
            }

            navTeleporter.SetTarget(target != null ? target.navTeleporter : null);
        }

        private void ApplyPassability()
        {
            EnsurePlacementCells();
            if (placementCells == null)
            {
                return;
            }

            for (int i = 0; i < placementCells.Length; i++)
            {
                int cell = placementCells[i];
                if (!Grid.IsValidCell(cell))
                {
                    continue;
                }

                // SimCellOccupier 会把格子设为 C# 固体；这里改回非固体，
                // 只保留 Sim 层的气体/液体/固体不可渗透属性。
                Grid.SetSolid(cell, false, CellEventLogger.Instance.SimCellOccupierForceSolid);
                Game.Instance.SetDupePassableSolid(cell, passable: true, solid: false);

                // 物理通行只由这两个标志控制：复制人与小动物可以分别放行。
                Grid.DupeImpassable[cell] = !allowDuplicants;
                Grid.CritterImpassable[cell] = !allowCritters;

                // 不通过门所在格交换热量（0f = 完全隔热）。
                SimMessages.SetInsulation(cell, 0f);

                if (Pathfinding.Instance != null)
                {
                    Pathfinding.Instance.AddDirtyNavGridCell(cell);
                }
            }
        }

        private void ClearPassability()
        {
            EnsurePlacementCells();
            if (placementCells == null)
            {
                return;
            }

            for (int i = 0; i < placementCells.Length; i++)
            {
                int cell = placementCells[i];
                if (!Grid.IsValidCell(cell))
                {
                    continue;
                }

                Element element = Grid.Element[cell];
                bool solid = element != null && element.IsSolid;
                Grid.SetSolid(cell, solid, CellEventLogger.Instance.SimCellOccupierDestroy);
                Game.Instance.SetDupePassableSolid(cell, passable: false, solid);

                Grid.DupeImpassable[cell] = false;
                Grid.CritterImpassable[cell] = false;
                SimMessages.SetInsulation(cell, 1f);

                // SimCellOccupier 会把 RenderedByWorld 设为 false，拆除时恢复。
                // Substance.renderedByWorld 对 Mod 程序集不可见，等价条件为 Element.IsSolid。
                if (element != null)
                {
                    Grid.RenderedByWorld[cell] = element.IsSolid;
                }

                if (Pathfinding.Instance != null)
                {
                    Pathfinding.Instance.AddDirtyNavGridCell(cell);
                }
            }
        }

        private void EnsurePlacementCells()
        {
            if (placementCells != null)
            {
                return;
            }

            if (building == null)
            {
                building = GetComponent<Building>();
            }
            if (building != null)
            {
                building.RefreshCells();
                placementCells = building.PlacementCells;
            }
        }

        private void RefreshUserMenu()
        {
            if (Game.Instance != null && Game.Instance.userMenu != null)
            {
                Game.Instance.userMenu.Refresh(gameObject);
            }
        }

        private static int GetWorldId(AnywhereDoor door)
        {
            if (door == null)
            {
                return -1;
            }

            int cell = Grid.PosToCell(door);
            return Grid.IsValidCell(cell) ? Grid.WorldIdx[cell] : -1;
        }

        private static int CellDistance(AnywhereDoor a, AnywhereDoor b)
        {
            int cellA = Grid.PosToCell(a);
            int cellB = Grid.PosToCell(b);

            int xA;
            int yA;
            int xB;
            int yB;
            Grid.CellToXY(cellA, out xA, out yA);
            Grid.CellToXY(cellB, out xB, out yB);

            return Mathf.Abs(xA - xB) + Mathf.Abs(yA - yB);
        }

        private static int CompareDoors(AnywhereDoor a, AnywhereDoor b)
        {
            int cellA = Grid.PosToCell(a);
            int cellB = Grid.PosToCell(b);

            int worldA = Grid.IsValidCell(cellA) ? Grid.WorldIdx[cellA] : int.MaxValue;
            int worldB = Grid.IsValidCell(cellB) ? Grid.WorldIdx[cellB] : int.MaxValue;
            if (worldA != worldB)
            {
                return worldA.CompareTo(worldB);
            }

            int xA;
            int yA;
            int xB;
            int yB;
            Grid.CellToXY(cellA, out xA, out yA);
            Grid.CellToXY(cellB, out xB, out yB);

            if (yA != yB)
            {
                return yA.CompareTo(yB);
            }
            return xA.CompareTo(xB);
        }
    }
}
