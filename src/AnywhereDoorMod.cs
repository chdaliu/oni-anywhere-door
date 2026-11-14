using HarmonyLib;
using KMod;

namespace ONIAnywhereDoor
{
    public sealed class AnywhereDoorMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            // The Mods screen lists this mod before the game world (and therefore
            // GeneratedBuildings) is loaded, so pick the language here and replace
            // the bilingual title/description from mod.yaml with the localized
            // text. Otherwise the entry and its tooltip show both languages.
            STRINGS.Initialize();
            if (mod != null)
            {
                mod.title = STRINGS.MOD_INFO.TITLE.text;
                mod.description = STRINGS.MOD_INFO.DESCRIPTION.text;
            }

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

            // Register the door in the Mechanized Airlock's research node before
            // building defs are processed, so BuildingDef.PostProcess creates the
            // matching TechItem and the build menu gates it behind that research.
            Tech tech = Db.Get().Techs.TryGetTechForTechItem("PressureDoor");
            if (tech != null)
            {
                tech.AddUnlockedItemIDs(AnywhereDoorConfig.ID);
            }

            ModUtil.AddBuildingToPlanScreen("Base", AnywhereDoorConfig.ID, "doors");
            TUNING.BUILDINGS.PLANSUBCATEGORYSORTING[AnywhereDoorConfig.ID] = "doors";
        }
    }
}
