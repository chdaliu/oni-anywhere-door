using HarmonyLib;

namespace ONIAnywhereDoor
{
    /// <summary>
    /// 让任意门相关的 Teleport 过渡在下一帧立即完成，跳过传送动画的等待。
    /// 只对“起点/终点在任意门格子上”的 Teleport 过渡生效，不影响原版其他传送器。
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

            // TransitionDriver.UpdateTransition 在下一 tick 会直接到达目标格并推进路径。
            IsComplete(__instance) = true;
        }
    }
}
