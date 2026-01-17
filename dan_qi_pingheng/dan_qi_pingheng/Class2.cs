using Bag;
using HarmonyLib;
using script.ExchangeMeeting.Logic;
using script.ExchangeMeeting.Logic.Interface;
using script.ExchangeMeeting.UI.Ctr;
using script.ExchangeMeeting.UI.Interface;
using script.ItemSource;
using script.ItemSource.Interface;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace zjr_mcs
{
    internal class xiantiandaoti
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(RoundManager), "SetHuaShen")]
        public static void RoundManager_SetHuaShen_Postfix(RoundManager __instance)
        {
            KBEngine.Avatar player = Tools.instance.getPlayer();
            if (player.SelectTianFuID.ToList().Contains(303))
                player.spell.addDBuff(10014);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(RoundManager), "setJieDan")]
        public static void RoundManager_setJieDan_Postfix(RoundManager __instance)
        {
            KBEngine.Avatar player = Tools.instance.getPlayer();
            if (player.SelectTianFuID.ToList().Contains(303))
                player.spell.addDBuff(10014);
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(RoundManager), "SetJieYing")]
        public static void RoundManager_SetJieYing_Postfix(RoundManager __instance)
        {
            KBEngine.Avatar player = Tools.instance.getPlayer();
            if (player.SelectTianFuID.ToList().Contains(303))
                player.spell.addDBuff(10014);
        }
    }
}
