using System;

namespace SphereEditorTools
{
    class Stringpool
    {
        public static string Tool;
        public static string Display_options;
        public static string Star;
        public static string Mask;

        public static string SymmetryTool;
        public static string None;
        public static string Equatorial;
        public static string Antipodal;
        public static string[] MirrorModes;

        public static string LAYER;
        public static string[] DisplayMode;
        public static string SymmetricTool;
        public static string Rotation;
        public static string Mirror;

        public static string OrbitTool;
        public static string AnchorMode;
        public static string AngularSpeed;
        public static string Set1;
        public static string Reset;

        public static void Set()
        {
            if (Localization.isZHCN)
            {
                LAYER = "层级";
                SymmetricTool = "[对称工具]";
                Rotation = "旋转";
                Mirror = "镜像";

                Tool = "工具";
                Display_options = "显示选项";
                Star = "恒星";
                Mask = "遮罩";

                SymmetryTool = "对称工具";
                None = "无";
                Equatorial = "赤道对称";
                Antipodal = "对跖点";

                OrbitTool = "轨道工具";
                AnchorMode = "锚定模式";
                AngularSpeed = "角速度";
                Set1 = "设置";
                Reset = "还原";
            }
            else
            {
                LAYER = "LAYER".Translate();
                SymmetricTool = "[Symmetry]".Translate();
                Rotation = "Rotation".Translate();
                Mirror = "Mirror".Translate();

                Tool = "Tool".Translate();
                Display_options = "Display".Translate();
                Star = "Star".Translate();
                Mask = "Mask".Translate();

                SymmetryTool = "Symmetry Tool".Translate();
                None = "None".Translate();
                Equatorial = "Equatorial".Translate();
                Antipodal = "Antipodal".Translate();

                OrbitTool = "Orbit Tool".Translate();
                AnchorMode = "Anchor Mode".Translate();
                AngularSpeed = "Angular Speed".Translate();
                Set1 = "Set".Translate();
                Reset = "Reset".Translate();
            }
            MirrorModes = new String[] { None, Equatorial, Antipodal };
            DisplayMode = new String[]
            {
                Display_options + " : " + Star,
                Display_options + " : " + Star + "," + Mask,
                Display_options + " : " + None,
                Display_options + " : " + Mask,
            };
        }
    }
}
