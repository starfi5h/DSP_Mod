using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DysonOrbitModifier
{
    class Stringpool
    {
        public static string Create;
        public static string Edit;
        public static string Add_orbit;
        public static string Add_layer;
        public static string Edit_orbit;
        public static string Rotation_speed; //new
        public static string Sync; //new
        public static void Set()
        {
            Create = translationCheck("Create", "创建");
            Edit = translationCheck("Edit", "编辑");
            Add_orbit = translationCheck("Add orbit", "新建轨道");
            Add_layer = translationCheck("Add layer", "新建层级");
            Edit_orbit = translationCheck("Edit orbit", "编辑轨道");
            Rotation_speed = translationCheck("Rotation speed", "旋转速度"); //new
            Sync = translationCheck("Sync", "同步"); //new
        }
        static string translationCheck(string enString, string cnString)
        {
            if (cnString.Translate() != cnString)
                return cnString.Translate(); //Translation plugin
            else if (Localization.language == Language.zhCN)
                return cnString;
            else
                return enString; //Default : English
        }
    }
}
