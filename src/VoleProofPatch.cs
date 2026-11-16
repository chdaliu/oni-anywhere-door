using HarmonyLib;

namespace ONIAnywhereDoor
{
    /// <summary>
    /// Shove Voles (code name "Mole") are the only critters that navigate the
    /// DiggerNavGrid and can tunnel through solid cells. SimCellOccupier briefly
    /// marks the door cells solid when a door spawns or a save loads; until
    /// AnywhereDoor restores them, the DiggerNavGrid classifies the cells as
    /// ordinary diggable terrain. These patches make every Anywhere Door cell
    /// undiggable so a vole can never treat a door as rock, even during that
    /// window. Normal critter passage is
    /// unaffected: when critters are allowed they still walk through the door.
    /// </summary>
    [HarmonyPatch(typeof(GameNavGrids.SolidValidator),
        nameof(GameNavGrids.SolidValidator.IsDiggable))]
    public static class VoleDiggerNavPatch
    {
        public static void Postfix(int cell, ref bool __result)
        {
            if (AnywhereDoor.IsDoorCell(cell))
            {
                __result = false;
            }
        }
    }

    /// <summary>
    /// Prevent a vole directly above a door from starting a burrow into it.
    /// CanBurrowInto already rejects occupied building cells, but the explicit
    /// door check keeps the guarantee even if another system clears that layer.
    /// </summary>
    [HarmonyPatch(typeof(BurrowMonitor.Instance),
        nameof(BurrowMonitor.Instance.CanBurrowInto))]
    public static class VoleBurrowPatch
    {
        public static void Postfix(int cell, ref bool __result)
        {
            if (AnywhereDoor.IsDoorCell(cell))
            {
                __result = false;
            }
        }
    }

    /// <summary>
    /// The space-meteor tunnel behaviour uses a separate dig-cell search; keep
    /// it from picking a door cell as a hiding spot as well.
    /// </summary>
    [HarmonyPatch(typeof(DiggerMonitor.Instance), "IsValidDigCell")]
    public static class VoleSpaceTunnelPatch
    {
        public static void Postfix(int cell, ref bool __result)
        {
            if (AnywhereDoor.IsDoorCell(cell))
            {
                __result = false;
            }
        }
    }
}
