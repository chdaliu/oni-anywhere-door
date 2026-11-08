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
    /// The base game scans all loaded assemblies for IBuildingConfig, so the
    /// building itself needs no manual registration. This patch registers the
    /// localization keys and the build-menu entry.
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
            STRINGS.Initialize();
            LocString.CreateLocStringKeys(typeof(STRINGS), null);

            ModUtil.AddBuildingToPlanScreen("Base", AnywhereDoorConfig.ID, "doors");
            TUNING.BUILDINGS.PLANSUBCATEGORYSORTING[AnywhereDoorConfig.ID] = "doors";
        }
    }
}
