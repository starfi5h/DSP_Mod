using System;

namespace SphereEditorTools
{
    class Stringpool
    {
        public static string Tool;
        public static string Display_options;
        public static string Star;
        public static string Swarm;

        public static string Mirror_symmetry;
        public static string None;
        public static string Equatorial;
        public static string Antipodal;
        public static string[] MirrorModes;
        public static string Rotation_symmetry;

        public static string Layer_copying;
        public static string Copy;
        public static string Paste;
        public static string Clear;
        public static string Paste1;
        public static string Paste2;

        public static string LAYER;
        public static string[] DisplayMode;
        public static string SymmetricTool;
        public static string Rotation;
        public static string Mirror;


        public static void Set()
        {
            if (Localization.language == Language.zhCN)
            {
                LAYER = "层级";
                SymmetricTool = "[对称工具]";
                Rotation = "旋转";
                Mirror = "镜像";

                Tool = "工具";
                Display_options = "显示选项";
                Star = "恒星";
                Swarm = "太阳帆";

                Mirror_symmetry = "镜像对称";
                None = "无";
                Equatorial = "赤道对称";
                Antipodal = "对跖点";
                Rotation_symmetry = "旋转对称";

                Layer_copying = "复制层级";
                Copy = "复制";
                Paste = "粘贴";
                Clear = "重设";
                Paste1 = "粘贴 - 固定网格";
                Paste2 = "粘贴 - 自由位置";
            }
            else
            {
                LAYER = "LAYER".Translate();
                SymmetricTool = "[Symmetry]".Translate();
                Rotation = "Rotation".Translate();
                Mirror = "Mirror".Translate();

                Tool = "Tool".Translate();
                Display_options = "Display options".Translate();
                Star = "Star".Translate();
                Swarm = "Swarm".Translate();

                Mirror_symmetry = "Mirror symmetry".Translate();
                None = "None".Translate();
                Equatorial = "Equatorial".Translate();
                Antipodal = "Antipodal".Translate();
                Rotation_symmetry = "Rotation symmetry".Translate();

                Layer_copying = "Copying Layer".Translate();
                Copy = "Copy".Translate();
                Paste = "Paste".Translate();
                Clear = "Clear".Translate();
                Paste1 = "Paste - snap".Translate();
                Paste2 = "Paste - free".Translate();
            }
            MirrorModes = new String[] { None, Equatorial, Antipodal };
            DisplayMode = new String[]
            {
                Display_options + " : " + Star + "," + Swarm,
                Display_options + " : " + Star,
                Display_options + " : " + Swarm,
                Display_options + " : " + None,
            };
        }
    }
}
