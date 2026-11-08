using TUNING;
using UnityEngine;

namespace ONIAnywhereDoor
{
    /// <summary>
    /// 1x2 Anywhere Door. SimCellOccupier blocks gas/liquid/solid matter, while
    /// AnywhereDoor instantly teleports duplicants entering a cell to another
    /// door in the network.
    /// </summary>
    public sealed class AnywhereDoorConfig : IBuildingConfig
    {
        public const string ID = "ONIAnywhereDoor";

        private const string ANIM = "oni_anywhere_door_kanim";

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                ID,
                1,
                2,
                ANIM,
                30,
                30f,
                new float[] { 200f },
                new string[] { "BuildableRaw" },
                1600f,
                BuildLocationRule.OnFloor,
                TUNING.BUILDINGS.DECOR.NONE,
                TUNING.NOISE_POLLUTION.NONE);

            def.ObjectLayer = ObjectLayer.Building;
            def.SceneLayer = Grid.SceneLayer.Building;
            def.ForegroundLayer = Grid.SceneLayer.InteriorWall;
            def.ContinuouslyCheckFoundation = true;

            // Indestructible: never melts, breaks, floods, or becomes entombed.
            def.Invincible = true;
            def.Overheatable = false;
            def.Breakable = false;
            def.Floodable = false;
            def.Entombable = false;
            def.Repairable = false;
            def.Replaceable = false;
            def.CanMove = false;
            def.BaseTimeUntilRepair = -1f;

            // No heat or matter exchange.
            def.ThermalConductivity = 0f;
            def.UseStructureTemperature = false;
            def.ExhaustKilowattsWhenActive = 0f;
            def.SelfHeatKilowattsWhenActive = 0f;
            def.EnergyConsumptionWhenActive = 0f;

            def.AudioCategory = "Metal";
            def.AudioSize = "small";
            def.PermittedRotations = PermittedRotations.Unrotatable;
            def.DefaultAnimState = "idle";
            def.ShowInBuildMenu = true;
            def.AddSearchTerms(global::STRINGS.SEARCH_TERMS.TRANSPORT);

            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            // No Storage or Conduit is needed; SimCellOccupier in
            // DoPostConfigureComplete handles matter isolation.
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            // Block gas, liquid, and solid matter without replacing the cell
            // element or participating in heat exchange. doReplaceElement=false
            // keeps the cell non-solid; AnywhereDoor separately opens the Nav layer.
            SimCellOccupier barrier = go.AddOrGet<SimCellOccupier>();
            barrier.doReplaceElement = false;
            barrier.setGasImpermeable = true;
            barrier.setLiquidImpermeable = true;
            barrier.setTransparent = true;

            go.AddOrGet<AnywhereDoor>();

            go.AddOrGet<AnywhereDoorDupeButton>();
            go.AddOrGet<AnywhereDoorCritterButton>();

            // Nav-layer teleport link; AnywhereDoor builds the nearest-neighbour network automatically.
            go.AddOrGet<NavTeleporter>();

            Prioritizable.AddRef(go);

            KBatchedAnimController anim = go.GetComponent<KBatchedAnimController>();
            if (anim != null)
            {
                anim.initialAnim = "idle";
            }

            // The door needs no enable/disable switch; the duplicant/critter
            // action buttons in the function area control passage.
            UnityEngine.Object.DestroyImmediate(go.GetComponent<BuildingEnabledButton>());
        }
    }
}
