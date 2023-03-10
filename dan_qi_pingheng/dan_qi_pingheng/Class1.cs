using Bag;
using BepInEx;
using HarmonyLib;
using JSONClass;
using KBEngine;
using LianQi;
using script.NewLianDan.LianDan;
using System;
using System.Reflection;
using UnityEngine;

namespace dan_qi_pingheng
{
    [BepInPlugin("plugins.zjr.mcsmod", "这是我的第一个BepIn插件", "1.0.0.0")]
    public class MyFirstBepInExMod : BaseUnityPlugin
    {// 在插件启动时会直接调用Awake()方法
        void Awake()
        {
            // 使用Debug.Log()方法来将文本输出到控制台
            Debug.Log("Hello, world!");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(GUIPackage.item), "gongneng")]
    class DanduPatch
    {
        public static void Postfix(GUIPackage.item __instance)
        {
            int type = _ItemJsonData.DataDict[__instance.itemID].type;
            if (type == 5 && !jsonData.instance.ItemJsonData[string.Concat(__instance.itemID)]["seid"].ToList().Contains(31))
            {
                KBEngine.Avatar player = PlayerEx.Player;
                int level = (int)player.level;
                level = (level + 5) / 3;
                KBEngine.Avatar avatar = (KBEngine.Avatar)KBEngineApp.app.player();
                if (__instance.quality > level + 1)
                {
                    avatar.AddDandu((__instance.quality - level - 1) * 30);
                }
            }
        }
    }
    [HarmonyPatch(typeof(GUIPackage.item), "GetJiaoYiPrice", new Type[] { typeof(int), typeof(bool), typeof(bool) })]
    class GUIItemJiaoyiPatch
    {
        public static void Postfix(GUIPackage.item __instance, ref int __result, ref int npcid, ref bool isPlayer, ref bool zongjia)
        {
            if (npcid > 0 && !zongjia)
            {
                int num = jsonData.instance.GetMonstarInterestingItem(npcid, __instance.itemID, __instance.Seid);
                float tmp_baseprice = __instance.itemPrice;
                if (__instance.Seid != null && __instance.Seid.HasField("Money"))
                {
                    tmp_baseprice = __instance.Seid["Money"].I;
                }
                if (__instance.Seid != null && __instance.Seid.HasField("NaiJiu"))
                {
                    tmp_baseprice = tmp_baseprice * GUIPackage.ItemCellEX.getItemNaiJiuPrice(__instance);
                }
                if (isPlayer)
                {
                    if (num < 80 && num >= 0)
                    {
                        float jiaCheng = __result / (tmp_baseprice * .5f);
                        float newjiaCheng = jiaCheng - num * .01f + .8f;
                        if (jsonData.instance.ItemJsonData[string.Concat(__instance.itemID)]["seid"].ToList().Contains(7) && num <= 80)
                            newjiaCheng += .2f;
                        __result = (int)(tmp_baseprice * .5f * newjiaCheng);
                    }
                }
                //else if (num == 100)
                //{
                //    float jiaCheng = __result / tmp_baseprice;
                //    float newjiaCheng = jiaCheng - num * .01f;
                //    __result = (int)(tmp_baseprice * newjiaCheng);
                //}
            }
        }
    }

    [HarmonyPatch(typeof(Bag.BaseItem), "GetJiaoYiPrice", new Type[] { typeof(int), typeof(bool), typeof(bool) })]
    class BaseItemJiaoyiPatch
    {
        public static void Postfix(Bag.BaseItem __instance, ref int __result, ref int npcid, ref bool isPlayer, ref bool zongjia)
        {
            if (npcid > 0 && !zongjia)
            {
                int num = jsonData.instance.GetMonstarInterestingItem(npcid, __instance.Id, __instance.Seid);
                float tmp_baseprice = __instance.BasePrice;
                if (__instance.Seid != null && __instance.Seid.HasField("Money"))
                {
                    tmp_baseprice = __instance.Seid["Money"].I;
                }
                if (__instance.Seid != null && __instance.Seid.HasField("NaiJiu"))
                {
                    tmp_baseprice = tmp_baseprice * get_naijiu_xishu(__instance);
                }

                if (isPlayer)
                {
                    if (num < 80 && num >= 0)
                    {
                        float jiaCheng = __result / (tmp_baseprice * .5f);
                        float newjiaCheng = jiaCheng - num * .01f + .8f;
                        if (jsonData.instance.ItemJsonData[string.Concat(__instance.Id)]["seid"].ToList().Contains(7) && num <= 80)
                            newjiaCheng += .2f;
                        __result = (int)(tmp_baseprice * .5f * newjiaCheng);
                    }
                }
                //else if (num == 100)
                //{
                //    float jiaCheng = __result / tmp_baseprice;
                //    float newjiaCheng = jiaCheng - num * .01f;
                //    __result = (int)(tmp_baseprice * newjiaCheng);
                //}
            }
        }
        static float get_naijiu_xishu(Bag.BaseItem __instance)
        {
            _ItemJsonData itemJsonData = _ItemJsonData.DataDict[__instance.Id];
            float result;
            if (itemJsonData.type == 14 || itemJsonData.type == 9)
            {
                float num = 100f;
                if (itemJsonData.type == 14)
                {
                    num = (float)jsonData.instance.LingZhouPinJie[itemJsonData.quality.ToString()]["Naijiu"];
                }
                result = __instance.Seid["NaiJiu"].n / num;
            }
            else
            {
                result = 1f;
            }
            return result;
        }
    }

    [HarmonyPatch(typeof(LianDanPanel), "PutDanLu", new Type[] { typeof(DanLuSlot) })]
    class DanLuPatch
    {
        public static bool Prefix(ref DanLuSlot dragSlot)
        {
            KBEngine.Avatar player = PlayerEx.Player;
            int level = (int)player.level;
            level = (level + 5) / 3;
            return level + 1 >= dragSlot.Item.GetImgQuality();
        }
    }
    [HarmonyPatch(typeof(LianDanPanel), "PutCaoYao", new Type[] { typeof(LianDanSlot) })]
    class CaoyaoPatch
    {
        public static bool Prefix(ref LianDanSlot dragSlot)
        {
            KBEngine.Avatar player = PlayerEx.Player;
            int level = (int)player.level;
            level = (level + 5) / 3;
            return level + 1 >= dragSlot.Item.GetImgQuality();
        }
    }
    [HarmonyPatch(typeof(LianQiTotalManager), "PutItem", new Type[] { typeof(LianQiSlot) })]
    class DuanzaoPatch
    {
        public static bool Prefix(ref LianQiSlot dragSlot)
        {
            KBEngine.Avatar player = PlayerEx.Player;
            int level = (int)player.level;
            level = (level + 5) / 3;
            return level + 1 >= dragSlot.Item.GetImgQuality();
        }
    }

    [HarmonyPatch(typeof(Avatar), "AddZiZhiSpeed", new Type[] { typeof(float) })]
    class ZizhiPatch
    {
        public static bool Prefix(Avatar __instance, ref float __result, ref float speed)
        {
            if (__instance.ZiZhi > 85)
                __result = Math.Max(2, (__instance.ZiZhi - 85) * .01f + .85f) * speed;
            else
                __result = Math.Max(-.4f, (__instance.ZiZhi - 15) / 56f - .4f) * speed;
            return false;
        }
    }
}
