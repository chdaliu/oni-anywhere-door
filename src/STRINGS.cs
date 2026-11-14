namespace ONIAnywhereDoor
{
    /// <summary>
    /// Building and UI text. Initialize() selects Chinese or English from the
    /// language chosen in the game, so no external translation files are
    /// required. It runs when the mod loads, before the Mods screen is shown.
    /// </summary>
    public static class STRINGS
    {
        private static bool initialized;

        public static class BUILDINGS
        {
            public static class PREFABS
            {
                public static class ONIANYWHEREDOOR
                {
                    public static LocString NAME;
                    public static LocString DESC;
                    public static LocString EFFECT;
                    public static LocString FLAVOR;
                }
            }
        }

        /// <summary>
        /// Title and description shown in the Mods screen. These replace the
        /// bilingual text from mod.yaml at runtime so the entry and its tooltip
        /// follow the selected game language instead of showing both languages.
        /// </summary>
        public static class MOD_INFO
        {
            public static LocString TITLE;
            public static LocString DESCRIPTION;
        }

        public static class UI
        {
            public static class ONIANYWHEREDOOR
            {
                public static LocString TITLE;
                public static LocString CURRENT_STATE;
                public static LocString DUPE;
                public static LocString DUPE_ALLOWED;
                public static LocString DUPE_FORBIDDEN;
                public static LocString DUPE_TOOLTIP;
                public static LocString CRITTER;
                public static LocString CRITTER_ALLOWED;
                public static LocString CRITTER_FORBIDDEN;
                public static LocString CRITTER_TOOLTIP;
            }
        }

        /// <summary>Pick and assign the strings for the current game language.</summary>
        public static void Initialize()
        {
            if (initialized)
            {
                return;
            }
            initialized = true;

            bool zh = UseChinese();
            MOD_INFO.TITLE = L(zh, "任意门", "Anywhere Door");
            MOD_INFO.DESCRIPTION = L(zh,
                "添加 1x2 的任意门。所有建成的任意门在同一世界中自动组成最近邻传送网络，" +
                "复制人自动寻路时会自动选择最近的入口使用网络，传送瞬间完成，" +
                "没有动画、等待或排队；不需要选择目的地。任意门阻挡气体、液体和固体物质，" +
                "只有复制人与小动物可以通过；可在建筑详情面板下方的功能区分别允许或禁止" +
                "复制人与小动物通过（小动物默认禁止）。需要研究“除污”，" +
                "并使用矿物原料与金属矿石或精炼金属建造；不可损毁，不交换热量。",
                "Adds a 1x2 Anywhere Door. Completed doors in the same world automatically " +
                "form a nearest-neighbour teleport network that duplicant pathfinding uses " +
                "automatically, picking the closest entrance; travel is instant with no " +
                "animation, waiting, or queue, and no destination needs to be chosen. " +
                "Doors block gas, liquids, and solids; only duplicants and critters may pass, " +
                "and their passage can be allowed or blocked independently from the action " +
                "area below the building details panel (critters are blocked by default). " +
                "Unlocked by Decontamination research and built from raw mineral plus " +
                "metal ore or refined metal; indestructible, with no heat exchange.");

            BUILDINGS.PREFABS.ONIANYWHEREDOOR.NAME = L(zh, "任意门", "Anywhere Door");
            BUILDINGS.PREFABS.ONIANYWHEREDOOR.DESC = L(zh,
                "一扇由矿石雕成的门，门内是一片缓慢旋转的星海。\n\n" +
                "所有建成的任意门在同一世界中自动组成最近邻传送网络。" +
                "复制人自动寻路时会把网络当作道路，自动选择最近的入口进入，" +
                "传送瞬间完成，没有动画、等待或排队；不需要手动选择目的地。\n\n" +
                "任意门会阻挡气体、液体和固体物质通过，只有复制人与小动物可以穿过。" +
                "它不会与周围交换热量，不会被高温熔化、不会损坏，也无法被淹没或掩埋；" +
                "只能通过拆除指令移除。",
                "A door carved from mineral, holding a slowly turning sea of stars.\n\n" +
                "All completed Anywhere Doors in the same world automatically form a " +
                "nearest-neighbour teleport network. Duplicant pathfinding treats the " +
                "network as a route and picks the closest entrance; travel is instant, " +
                "with no animation, waiting, or queue, and no destination needs to be chosen.\n\n" +
                "Anywhere Doors block gas, liquids, and solids; only duplicants and critters " +
                "may pass. They do not exchange heat, cannot melt, break, flood, or become " +
                "entombed, and can only be removed with the deconstruct command.");
            BUILDINGS.PREFABS.ONIANYWHEREDOOR.EFFECT = L(zh,
                "同世界最近邻网络\n自动寻路使用\n瞬间传送/无排队\n" +
                "阻挡气体/液体/固体物质\n只有生物可以通过\n" +
                "可允许或禁止复制人通过\n可允许或禁止小动物通过\n" +
                "矿石建造\n不可损毁\n无热量交换",
                "Nearest-neighbour network per world\nUsed by pathfinding\n" +
                "Instant travel / no queue\nBlocks gas / liquid / solid matter\n" +
                "Only creatures may pass\nCan allow or block duplicant passage\n" +
                "Can allow or block critter passage\nBuilt from mineral\n" +
                "Indestructible\nNo heat exchange");
            BUILDINGS.PREFABS.ONIANYWHEREDOOR.FLAVOR = L(zh,
                "\"门后不是另一个房间，而是另一扇门。\"",
                "\"Behind the door is not another room, but another door.\"");

            UI.ONIANYWHEREDOOR.TITLE = L(zh, "任意门", "Anywhere Door");
            UI.ONIANYWHEREDOOR.CURRENT_STATE = L(zh, "当前：{0}", "Now: {0}");

            UI.ONIANYWHEREDOOR.DUPE = L(zh, "复制人：{0}", "Dupes: {0}");
            UI.ONIANYWHEREDOOR.DUPE_ALLOWED = L(zh, "允许", "On");
            UI.ONIANYWHEREDOOR.DUPE_FORBIDDEN = L(zh, "禁止", "Off");
            UI.ONIANYWHEREDOOR.DUPE_TOOLTIP = L(zh,
                "允许时复制人可以走进任意门并使用传送网络。\n" +
                "禁止时这扇门会挡住复制人，传送连接也会断开。",
                "On: duplicants can enter the door and use the teleport network.\n" +
                "Off: the door stops duplicants and its teleport links are disconnected.");

            UI.ONIANYWHEREDOOR.CRITTER = L(zh, "小动物：{0}", "Critters: {0}");
            UI.ONIANYWHEREDOOR.CRITTER_ALLOWED = L(zh, "允许", "On");
            UI.ONIANYWHEREDOOR.CRITTER_FORBIDDEN = L(zh, "禁止", "Off");
            UI.ONIANYWHEREDOOR.CRITTER_TOOLTIP = L(zh,
                "允许时小动物可以穿过任意门所在的位置。\n" +
                "禁止时任意门会挡住小动物。小动物只物理通行，不会被传送。\n" +
                "新建的任意门默认禁止小动物通过。",
                "On: critters can physically pass through the door's cells.\n" +
                "Off: the door stops critters. Critters only walk through; " +
                "they are not teleported.\n" +
                "New Anywhere Doors block critters by default.");
        }

        /// <summary>Pick the localized string for the current game language.</summary>
        private static bool UseChinese()
        {
            try
            {
                string code = Localization.GetCurrentLanguageCode();
                return !string.IsNullOrEmpty(code) && code.StartsWith("zh");
            }
            catch
            {
                return false;
            }
        }

        private static LocString L(bool chinese, string zh, string en)
        {
            return new LocString(chinese ? zh : en);
        }
    }
}
