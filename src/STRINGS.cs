namespace ONIAnywhereDoor
{
    /// <summary>
    /// 建筑与侧边栏文本。中文直接写在 LocString 默认值里，
    /// 这样即使没有额外翻译文件，中文客户端也能正常显示。
    /// </summary>
    public static class STRINGS
    {
        public static class BUILDINGS
        {
            public static class PREFABS
            {
                public static class ONIANYWHEREDOOR
                {
                    public static LocString NAME = "任意门";
                    public static LocString DESC =
                        "一扇由矿石雕成的门，门内是一片缓慢旋转的星海。\n\n" +
                        "所有建成的任意门在同一世界中自动组成最近邻传送网络。" +
                        "复制人自动寻路时会把网络当作道路，自动选择最近的入口进入，" +
                        "传送瞬间完成，没有动画、等待或排队；不需要手动选择目的地。\n\n" +
                        "任意门会阻挡气体、液体和固体物质通过，只有复制人与小动物可以穿过。" +
                        "它不会与周围交换热量，不会被高温熔化、不会损坏，也无法被淹没或掩埋；只能通过拆除指令移除。";
                    public static LocString EFFECT =
                        "同世界最近邻网络\n自动寻路使用\n瞬间传送/无排队\n" +
                        "阻挡气体/液体/固体物质\n只有生物可以通过\n" +
                        "可允许或禁止复制人通过\n可允许或禁止小动物通过\n" +
                        "矿石建造\n不可损毁\n无热量交换";
                    public static LocString FLAVOR = "\"门后不是另一个房间，而是另一扇门。\"";
                }
            }
        }

        public static class UI
        {
            public static class ONIANYWHEREDOOR
            {
                public static LocString TITLE = "任意门";

                public static LocString DUPE = "复制人通过：{0}";
                public static LocString DUPE_ALLOWED = "允许";
                public static LocString DUPE_FORBIDDEN = "禁止";
                public static LocString DUPE_TOOLTIP =
                    "允许时复制人可以走进任意门并使用传送网络。\n" +
                    "禁止时这扇门会挡住复制人，传送连接也会断开。";

                public static LocString CRITTER = "小动物通过：{0}";
                public static LocString CRITTER_ALLOWED = "允许";
                public static LocString CRITTER_FORBIDDEN = "禁止";
                public static LocString CRITTER_TOOLTIP =
                    "允许时小动物可以穿过任意门所在的位置。\n" +
                    "禁止时任意门会挡住小动物。小动物只物理通行，不会被传送。";
            }
        }
    }
}
