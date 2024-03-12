using HarmonyLib;

namespace DeliverySlotsTweaks
{
    public class UIBuildMenuPatch
    {
        static bool Initialized = false;

        [HarmonyPostfix, HarmonyPatch(typeof(UIBuildMenu), nameof(UIBuildMenu._OnRegEvent))]
        public static void AddEvents(UIBuildMenu __instance)
        {
            if (Initialized) return;

            Initialized = true;
            for (int i = 0; i < __instance.childButtons.Length; i++)
            {
                var button = __instance.childButtons[i];
                if (button == null) continue;

                button.data = i;
                button.onRightClick += OnChildButtonRightClick;
            }
        }

        static void OnChildButtonRightClick(int index)
        {
            var window = UIRoot.instance.uiGame.buildMenu;
            if (UIBuildMenu.protos[window.currentCategory, index] == null)
            {
                return;
            }

            // UIGame.FocusOnReplicate
            int itemId = UIBuildMenu.protos[window.currentCategory, index].ID;
            var itemProto = LDB.items.Select(itemId);
            if (itemProto != null && itemProto.maincraft != null)
            {
                TryReplicateItem(itemProto);
            }
        }

        static void TryReplicateItem(ItemProto itemProto)
        {
            var recipe = itemProto.maincraft;
            if (recipe == null) return;

            var id = recipe.ID;
            var num = 1;
            if (!recipe.Handcraft)
            {
                UIRealtimeTip.Popup("该配方".Translate() + recipe.madeFromString + "生产".Translate(), true, 0);
                return;
            }
            if (!GameMain.history.RecipeUnlocked(id))
            {
                UIRealtimeTip.Popup("配方未解锁".Translate(), true, 0);
                return;
            }
            var mechaForge = GameMain.mainPlayer.mecha.forge;
            var predictNum = mechaForge.PredictTaskCount(id, 99);
            if (num > predictNum)
            {
                num = predictNum;
            }
            if (num == 0)
            {
                UIRealtimeTip.Popup("材料不足".Translate(), true, 0);
                return;
            }
            if (mechaForge.AddTask(id, num) == null)
            {
                UIRealtimeTip.Popup("材料不足".Translate(), true, 0);
                return;
            }
            GameMain.history.RegFeatureKey(1000104);
            var resultCount = 0;
            if (recipe.ResultCounts != null && recipe.ResultCounts.Length > 0)
            {
                resultCount = recipe.ResultCounts[0];
            }
            UIRealtimeTip.Popup(string.Format("Crafting {0} [{1}]".Translate(), itemProto.name, resultCount), false, 0);
        }
    }
}
