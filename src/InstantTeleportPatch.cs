using HarmonyLib;

namespace ONIAnywhereDoor
{
    /// <summary>
    /// Finish Teleport transitions involving an Anywhere Door on the next frame,
    /// skipping the travel-animation wait. Only transitions whose start or end
    /// cell belongs to an Anywhere Door are affected; vanilla teleporters are untouched.
    /// </summary>
    [HarmonyPatch(typeof(TransitionDriver), "BeginTransition",
        new[] { typeof(Navigator), typeof(Navigator.ActiveTransition) })]
    public static class InstantTeleportPatch
    {
        private static readonly AccessTools.FieldRef<TransitionDriver, bool> IsComplete =
            AccessTools.FieldRefAccess<TransitionDriver, bool>("isComplete");

        public static void Postfix(
            TransitionDriver __instance,
            Navigator navigator,
            Navigator.ActiveTransition transition)
        {
            if (transition == null ||
                (transition.start != NavType.Teleport && transition.end != NavType.Teleport))
            {
                return;
            }

            if (!AnywhereDoor.IsDoorCell(Grid.PosToCell(navigator)))
            {
                return;
            }

            // TransitionDriver.UpdateTransition will reach the target cell on
            // the next tick and advance the path.
            IsComplete(__instance) = true;
        }
    }
}
