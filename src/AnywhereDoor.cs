using System.Collections.Generic;
using KSerialization;
using UnityEngine;

namespace ONIAnywhereDoor
{
    /// <summary>
    /// Core logic for the Anywhere Door.
    ///
    /// Doors in the same world form a ring-shaped network in nearest-neighbour
    /// order: A -> B -> C -> ... -> A. Each door needs only one NavTeleporter
    /// link to the next door. Duplicant pathfinding treats the network as a
    /// walkable route and picks the closest entrance.
    ///
    /// InstantTeleportPatch finishes Teleport transitions immediately, so there
    /// is no travel animation, waiting, or queue.
    ///
    /// SimCellOccupier supplies gas/liquid/solid impermeability. This component
    /// restores the Nav layer to non-solid, so only creatures can pass; matter
    /// cannot, and the cells do not exchange heat. The sim flags and insulation
    /// are also re-asserted periodically: reloading or replacing the cell element
    /// can reset the sim-side insulation byte, which would silently reopen the
    /// cells to heat conduction.
    /// </summary>
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class AnywhereDoor : KMonoBehaviour, ISim200ms
    {
        // GasImpermeable | LiquidImpermeable | SolidImpermeable: no gas, liquid,
        // or falling solid may enter the door cells.
        private const byte SealedCellProperties =
            (byte)((int)Sim.Cell.Properties.GasImpermeable |
                   (int)Sim.Cell.Properties.LiquidImpermeable |
                   (int)Sim.Cell.Properties.SolidImpermeable);

        // The sim stores insulation as a byte where 0 is perfect insulation.
        // SimMessages.SetInsulation(1f) restores the default conductivity.
        private const float SealedInsulation = 0f;
        private const float DefaultInsulation = 1f;

        public static readonly List<AnywhereDoor> AllDoors = new List<AnywhereDoor>();

        [Serialize]
        public bool allowDuplicants = true;

        [Serialize]
        public bool allowCritters = false;

        private NavTeleporter navTeleporter;
        private Building building;
        private int[] placementCells;
        private float linkRefreshTimer;
        private float passabilityRefreshTimer;
        private float sealRefreshTimer;
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

            // BuildingComplete.OnSpawn may assign the building position after
            // this component spawns, so delay reading PlacementCells to avoid
            // caching invalid coordinates.
            passabilityRefreshTimer = 0.5f;
            sealRefreshTimer = 0.5f;

            // Rebuild the nearest-neighbour network soon after a door is added;
            // this also completes within 0.5 s when a save is loaded.
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

            // Re-assert the seal at a low frequency. This catches cell element
            // changes that reset the sim insulation byte, and any flag clears.
            sealRefreshTimer -= dt;
            if (sealRefreshTimer <= 0f)
            {
                sealRefreshTimer = 1f;
                MaintainSeal();
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
        /// Build one network per world. Doors are sorted by position and then
        /// chained into a ring in nearest-neighbour order: each door links to
        /// the closest next door, and the last one links back to the first.
        /// Duplicants can enter at the closest door and follow short hops.
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

                // SimCellOccupier marks the cell solid in C#; restore it to
                // non-solid while keeping its Sim-layer impermeability.
                Grid.SetSolid(cell, false, CellEventLogger.Instance.SimCellOccupierForceSolid);
                Game.Instance.SetDupePassableSolid(cell, passable: true, solid: false);

                // Physical passage is controlled by these two flags, so
                // duplicants and critters can be allowed independently.
                Grid.DupeImpassable[cell] = !allowDuplicants;
                Grid.CritterImpassable[cell] = !allowCritters;

                SealCell(cell);

                if (Pathfinding.Instance != null)
                {
                    Pathfinding.Instance.AddDirtyNavGridCell(cell);
                }
            }

            sealRefreshTimer = 1f;
        }

        /// <summary>
        /// Make a cell absolutely impermeable to gas, liquid, and solid matter,
        /// and stop heat conduction across it, without making the cell solid in
        /// the Nav layer (teleportation and walking depend on the Nav layer only).
        /// </summary>
        private static void SealCell(int cell)
        {
            // GasImpermeable/LiquidImpermeable are checked directly by the sim
            // when it considers moving a gas/liquid packet into this cell.
            // SolidImpermeable does the same for falling solids.
            SimMessages.SetCellProperties(cell, SealedCellProperties);

            // The sim's conduction formula multiplies both neighbours' squared
            // insulation bytes, so 0 makes the transfer exactly zero. This is
            // separate from the element's own thermal conductivity.
            SimMessages.SetInsulation(cell, SealedInsulation);
        }

        /// <summary>
        /// Re-apply the seal only when the sim-side state drifted. Element
        /// replacement (including loading a save) resets insulation to the
        /// default 1f, and other systems can clear cell flags.
        /// </summary>
        private void MaintainSeal()
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

                bool flagsMissing = (Grid.Properties[cell] & SealedCellProperties) != SealedCellProperties;
                if (flagsMissing)
                {
                    // The properties indexer exposes the sim's byte directly.
                    SimMessages.SetCellProperties(cell, SealedCellProperties);
                }

                if (Grid.Insulation[cell] != 0)
                {
                    SimMessages.SetInsulation(cell, SealedInsulation);
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
                SimMessages.ClearCellProperties(cell, SealedCellProperties);
                SimMessages.SetInsulation(cell, DefaultInsulation);

                // SimCellOccupier clears RenderedByWorld; restore it on removal.
                // Substance.renderedByWorld is not visible to mod assemblies,
                // so the equivalent condition is Element.IsSolid.
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
