using TUNING;
using UnityEngine;

namespace ONIAnywhereDoor
{
    /// <summary>
    /// 1x2 的任意门。SimCellOccupier 负责阻挡气体/液体/固体物质，
    /// AnywhereDoor 负责把进入格子的复制人瞬间传送到网络中另一扇门。
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

            // 不可损毁：不会被高温熔化、不会损坏、不会淹没或掩埋。
            def.Invincible = true;
            def.Overheatable = false;
            def.Breakable = false;
            def.Floodable = false;
            def.Entombable = false;
            def.Repairable = false;
            def.Replaceable = false;
            def.CanMove = false;
            def.BaseTimeUntilRepair = -1f;

            // 不交换热量和物质。
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
            // 不需要 Storage/Conduit；物质隔离由 DoPostConfigureComplete 中的 SimCellOccupier 处理。
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            // 阻挡气体、液体和固体物质，但不替换单元格元素，也不参与热交换。
            // doReplaceElement=false 让格子保持非固体；Nav 层的通行由 AnywhereDoor 单独打开。
            SimCellOccupier barrier = go.AddOrGet<SimCellOccupier>();
            barrier.doReplaceElement = false;
            barrier.setGasImpermeable = true;
            barrier.setLiquidImpermeable = true;
            barrier.setTransparent = true;

            go.AddOrGet<AnywhereDoor>();

            go.AddOrGet<AnywhereDoorDupeButton>();
            go.AddOrGet<AnywhereDoorCritterButton>();

            // 寻路层的传送连接；由 AnywhereDoor 自动组成最近邻网络。
            go.AddOrGet<NavTeleporter>();

            Prioritizable.AddRef(go);

            KBatchedAnimController anim = go.GetComponent<KBatchedAnimController>();
            if (anim != null)
            {
                anim.initialAnim = "idle";
            }

            // 门不需要“启用/禁用”开关，通过侧边栏的复制人/小动物选项控制通行。
            UnityEngine.Object.DestroyImmediate(go.GetComponent<BuildingEnabledButton>());
        }
    }
}
