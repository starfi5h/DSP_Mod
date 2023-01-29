using System.Collections.Generic;
using UnityEngine;

namespace AlterTickrate
{
    public class UIwarningTip
    {
        static UIButtonTip tip = null;

        public static void Create(UIWarningItemEntry itemEntry, int signalId)
        {
            if (tip != null)
                GameObject.Destroy(tip.gameObject);

            int detailId = itemEntry.signalId;
            string title = detailId < 20000 ? LDB.ItemName(detailId) : LDB.RecipeName(detailId - 20000);
            string text = GetText(signalId, detailId);
            Transform parent = itemEntry.transform;
            tip = UIButtonTip.Create(true, title, text, 2, new Vector2(0, -10), 0, parent, "", "");
        }

        public static void Destory()
        {
            if (tip != null)
                GameObject.Destroy(tip.gameObject);
            tip = null;
        }

        private static string GetText(int signalId, int detailId)
        {
            Dictionary<int, int> map = new();
            var ws = GameMain.data.warningSystem;
            if (signalId > 0)
            {
                for (int i = 1; i < ws.warningCursor; i++)
                {
                    ref var data = ref ws.warningPool[i];
                    if (data.id == i && data.state > 0 && data.signalId == signalId && data.detailId == detailId)
                    {
                        if (map.ContainsKey(data.astroId))
                            ++map[data.astroId];
                        else
                            map[data.astroId] = 1;
                    }
                }
            }
            string text = "";
            foreach (var pair in map)
            {
                var planet = GameMain.galaxy.PlanetById(pair.Key);
                if (planet != null)
                    text += planet.displayName + " (" + pair.Value + ")\n";
            }
            return text;
        }
    }
}
