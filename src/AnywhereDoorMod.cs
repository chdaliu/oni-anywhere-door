using HarmonyLib;
using KMod;

namespace ONIAnywhereDoor
{
    public sealed class AnywhereDoorMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
        }
    }

    /// <summary>
    /// 原版会扫描所有已加载程序集中的 IBuildingConfig，建筑注册本身无需手动调用。
    /// 这里负责注册字符串和建造菜单入口。
    /// </summary>
    [HarmonyPatch(typeof(GeneratedBuildings), nameof(GeneratedBuildings.LoadGeneratedBuildings))]
    public static class GeneratedBuildingsPatch
    {
        private static bool initialized;

        public static void Prefix()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            LocString.CreateLocStringKeys(typeof(STRINGS), null);

            ModUtil.AddBuildingToPlanScreen("Base", AnywhereDoorConfig.ID, "doors");
            TUNING.BUILDINGS.PLANSUBCATEGORYSORTING[AnywhereDoorConfig.ID] = "doors";
        }
    }
}
