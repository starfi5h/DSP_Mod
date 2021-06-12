using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphereEditorTools
{
    class TranslateString
    {
        public static string layer =>
            Localization.language == Language.zhCN ? "层级" : "Layer".Translate();
    }
}
