using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphereEditorTools
{
    class TranslateString
    {
        public static string Layer =>
            Localization.language == Language.zhCN ? "层级" : "LAYER".Translate();

        public static string HideMode(int mode)
        {
            if (Localization.language == Language.zhCN)
            {
                switch (mode)
                {
                    case 0: return "正常显示";
                    case 1: return "隐藏太阳帆";
                    case 2: return "隐藏太阳帆&恒星";
                    default: return "";
                }
            }
            else
            {
                switch (mode)
                {
                    case 0: return "Normal".Translate();
                    case 1: return "Hide swarm".Translate();
                    case 2: return "Hide swarm & star".Translate();
                    default: return "";
                }
            }
        }

        public static string SymmetricTool =>
            Localization.language == Language.zhCN ? "[对称工具]" : "[Symmetry]".Translate();

        public static string Rotation =>
            Localization.language == Language.zhCN ? "旋转" : "Rotation".Translate();
        public static string Mirror =>
            Localization.language == Language.zhCN ? "镜像" : "Mirror".Translate();

    }
}
