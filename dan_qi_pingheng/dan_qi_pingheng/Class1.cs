using Bag;
using BepInEx;
using HarmonyLib;
using JSONClass;
using KBEngine;
using LianQi;
using script.NewLianDan.LianDan;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace zjr_mcs
{
    [BepInPlugin("plugins.zjr.mcs_pingheng", "zjr平衡插件", "1.0.0.0")]
    public class pinghengBepInExMod : BaseUnityPlugin
    {// 在插件启动时会直接调用Awake()方法
        void Awake()
        {
            //instance = this;
            Debug.Log("Hello,mcs_pingheng!");
            noDanyao = base.Config.Bind<bool>("nandu", "不吃药", false, "不吃药，默认关闭");
            Debug.Log("nodanyao:" + noDanyao.Value.ToString());
            falidai = base.Config.Bind<bool>("nandu", "法力贷", false, "法力贷，默认关闭");
            Debug.Log("falidai:" + falidai.Value.ToString());

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            Debug.Log("GetExecutingAssembly");
            Harmony.CreateAndPatchAll(typeof(pinghengBepInExMod));
            Debug.Log("pinghengBepInExMod");
        }
        //public static pinghengBepInExMod instance;
        public static BepInEx.Configuration.ConfigEntry<bool> noDanyao;
        public static BepInEx.Configuration.ConfigEntry<bool> falidai;

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(Tab.WuDaoSlot), "Study")]
        //public static bool WuDaoSlot_Study_Prefix(Tab.WuDaoSlot __instance)
        //{
        //    if (__instance.State == 2)
        //    {
        //        KBEngine.Avatar player = Tools.instance.getPlayer();
        //        JSONObject jsonobject = jsonData.instance.WuDaoJson[__instance.Id.ToString()];
        //        bool tmp_flag = jsonobject["Lv"].I <= (player.level + 5) / 3;
        //        if (!tmp_flag)
        //            UIPopTip.Inst.Pop("境界过低，需要" + ToolsEx.ToBigLevelName(jsonobject["Lv"].I - 1), PopTipIconType.叹号);
        //        return tmp_flag;
        //    }
        //    return true;
        //}

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GUIPackage.Skill), "realizeSeid39")]
        public static bool Skill_realizeSeid39_Prefix(GUIPackage.Skill __instance, int seid, List<int> damage, KBEngine.Avatar attaker, KBEngine.Avatar receiver, int type)
        {
            int listSum = RoundManager.instance.getListSum(attaker.crystal);
            RoundManager.instance.removeCard(attaker, listSum);

            int tmp_id = __instance.getSeidJson(seid)["skillid"].I;
            if (tmp_id <= 5060 && tmp_id >= 5056)
            {
                //attaker.recvDamage(attaker, attaker, __instance.skill_ID, -listSum * __instance.getSeidJson(seid)["value1"].I, type);
                //for (int j = 0; j < listSum; j++)
                //{
                //    attaker.spell.addDBuff(15);
                //}
                damage[0] = damage[0] + listSum * (__instance.getSeidJson(seid)["value1"].I + listSum);
            }
            else
                damage[0] = damage[0] + listSum * __instance.getSeidJson(seid)["value1"].I;

            return false;
        }
    }

    [HarmonyPatch(typeof(GUIPackage.item), "gongneng")]
    class DanduPatch
    {
        public static bool Prefix(GUIPackage.item __instance)
        {
            if (_ItemJsonData.DataDict.ContainsKey(__instance.itemID))
            {
                int type = _ItemJsonData.DataDict[__instance.itemID].type;
                if (pinghengBepInExMod.noDanyao.Value)
                {
                    if (type == 5 && __instance.itemID != 5317)
                    {
                        string msg = "禁止吃药";
                        UIPopTip.Inst.Pop(msg, PopTipIconType.叹号);
                        return false;
                    }
                    return true;
                }

                if (type == 5 && !jsonData.instance.ItemJsonData[string.Concat(__instance.itemID)]["seid"].ToList().Contains(31)
                    && !jsonData.instance.ItemJsonData[string.Concat(__instance.itemID)]["seid"].ToList().Contains(16))
                {
                    int itemCanUseNum = GUIPackage.item.GetItemCanUseNum(__instance.itemID);
                    if (itemCanUseNum > 0 && Tools.getJsonobject(Tools.instance.getPlayer().NaiYaoXin, string.Concat(__instance.itemID)) < itemCanUseNum)
                    {
                        KBEngine.Avatar player = PlayerEx.Player;
                        int level = (int)player.level;
                        level = (level + 5) / 3;
                        KBEngine.Avatar avatar = (KBEngine.Avatar)KBEngineApp.app.player();
                        if (__instance.quality > level + 1)
                        {
                            int tmp_dandu = (__instance.quality - level - 1) * 30;
                            avatar.AddDandu(tmp_dandu);

                            UIPopTip.Inst.Pop("超阶吃药，毒性大增" + tmp_dandu.ToString(), PopTipIconType.叹号);
                        }
                    }
                }
                if (__instance.itemID == 6307)
                {
                    if (Tools.getJsonobject(Tools.instance.getPlayer().NaiYaoXin, string.Concat(__instance.itemID)) >= 20)
                    {
                        string msg = "血菩提20/20，无法服用";
                        UIPopTip.Inst.Pop(msg, PopTipIconType.叹号);
                        return false;
                    }
                    else
                    {
                        __instance.AddNaiYaoXin();
                        string msg = "血菩提" + Tools.getJsonobject(Tools.instance.getPlayer().NaiYaoXin, string.Concat(__instance.itemID)).ToString() + "/20";
                        UIPopTip.Inst.Pop(msg, PopTipIconType.叹号);
                    }
                }
            }
            return true;
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
                    if (num < 100 && num >= 0 && tmp_baseprice > 0)
                    {
                        float jiaCheng = Mathf.RoundToInt(__result / (tmp_baseprice * .5f) * 100) / 100f;
                        float newjiaCheng = Mathf.Min(jiaCheng * 2, jiaCheng - num * .01f + 1);
                        __result = Mathf.RoundToInt(tmp_baseprice * .5f * newjiaCheng);
                    }
                }
                //else if (jsonData.instance.AvatarJsonData[npcid.ToString()]["gudingjiage"].I != 1)
                //{
                //    if (num == 100)
                //    {
                //        float num3 = (float)jsonData.instance.getSellPercent(npcid, __instance.itemID) / 100f;
                //        float jiaCheng = __result / (tmp_baseprice) / num3;
                //        float newjiaCheng = jiaCheng - .95f;
                //        __result = Mathf.RoundToInt(tmp_baseprice * newjiaCheng * num3);
                //    }
                //}
            }
        }
    }

    [HarmonyPatch(typeof(GUIPackage.item), "CalcNPCZhuangTai")]
    class GUIItemCalcNPCZhuangTaiPatch
    {
        public static void Postfix(GUIPackage.item __instance, ref int npcid, ref bool isJiXu, ref bool isLaJi)
        {
            new_zhuangtai(__instance.itemID, ref npcid, ref isJiXu, ref isLaJi);
        }
        public static void new_zhuangtai(int itemid, ref int npcid, ref bool isJiXu, ref bool isLaJi)
        {
            isJiXu = false;
            isLaJi = false;
            _ItemJsonData itemJsonData = _ItemJsonData.DataDict[itemid];
            List<int> list = new List<int>();
            if (itemJsonData.ItemFlag.Count > 0)
            {
                foreach (int item in itemJsonData.ItemFlag)
                {
                    list.Add(item);
                }
            }
            if (list.Contains(50))
            {
                isLaJi = true;
            }
            JSONObject jsonobject = npcid.NPCJson();
            if (!jsonobject.HasField("Status"))
            {
                return;
            }
            int i = jsonobject["Status"]["StatusId"].I;
            int i2 = (jsonobject["Level"].I + 2) / 3;
            if (list.Contains(620) || list.Contains(621))
            {
                int danYaoCanUseNum = NpcJieSuanManager.inst.npcUseItem.GetDanYaoCanUseNum(npcid, itemid);
                if (danYaoCanUseNum > 0)
                {
                    isJiXu = true;
                }
            }
            if (i2 == 1 && list.Contains(610))
            {
                isJiXu = true;
            }
            if (i2 == 2 && list.Contains(611))
            {
                isJiXu = true;
            }
            if (i2 == 2 && itemid >= 3901 && itemid <= 3906)
            {
                isJiXu = true;
            }
            if (i2 == 3 && list.Contains(612))
            {
                isJiXu = true;
            }
            if (i2 == 3 && itemid >= 3912 && itemid <= 3915)
            {
                isJiXu = true;
            }
            if (i2 == 4 && list.Contains(613))
            {
                isJiXu = true;
            }
            if (i2 == 4 && itemid >= 3920 && itemid <= 3924)
            {
                isJiXu = true;
            }
            if (i2 == 5 && list.Contains(614))
            {
                isJiXu = true;
            }
            //if (itemid == 5231)
            //{
            //    isJiXu = false;
            //}
        }
    }
    [HarmonyPatch(typeof(Bag.BaseItem), "CalcNPCZhuangTai")]
    class BaseItemCalcNPCZhuangTaiPatch
    {
        public static void Postfix(Bag.BaseItem __instance, ref int npcid, ref bool isJiXu, ref bool isLaJi)
        {
            GUIItemCalcNPCZhuangTaiPatch.new_zhuangtai(__instance.Id, ref npcid, ref isJiXu, ref isLaJi);
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
                    if (num < 100 && num >= 0 && tmp_baseprice > 0)
                    {
                        float jiaCheng = Mathf.RoundToInt(__result / (tmp_baseprice * .5f) * 100) / 100f;
                        float newjiaCheng = Mathf.Min(jiaCheng * 2, jiaCheng - num * .01f + 1);
                        __result = Mathf.RoundToInt(tmp_baseprice * .5f * newjiaCheng);
                    }
                }
                //else if (jsonData.instance.AvatarJsonData[npcid.ToString()]["gudingjiage"].I != 1)
                //{
                //    if (num == 100)
                //    {
                //        float num3 = (float)jsonData.instance.getSellPercent(npcid, __instance.Id) / 100f;
                //        float jiaCheng = __result / (tmp_baseprice) / num3;
                //        float newjiaCheng = jiaCheng - .95f;
                //        __result = Mathf.RoundToInt(tmp_baseprice * newjiaCheng * num3);
                //    }
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
            if (level + 1 < dragSlot.Item.GetImgQuality())
            {
                UIPopTip.Inst.Pop("超阶物品，无法使用", PopTipIconType.叹号);
            }
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
            if (level + 1 < dragSlot.Item.GetImgQuality())
            {
                UIPopTip.Inst.Pop("超阶物品，无法使用", PopTipIconType.叹号);
            }
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
            if (level + 1 < dragSlot.Item.GetImgQuality())
            {
                UIPopTip.Inst.Pop("超阶物品，无法使用", PopTipIconType.叹号);
            }
            return level + 1 >= dragSlot.Item.GetImgQuality();
        }
    }

    [HarmonyPatch(typeof(Avatar), "AddZiZhiSpeed", new Type[] { typeof(float) })]
    class ZizhiPatch
    {
        public static bool Prefix(Avatar __instance, ref float __result, ref float speed)
        {
            __result = npcxiulianPatch.get_zizhi_xishu(__instance.ZiZhi) * speed;
            return false;
        }
    }
    [HarmonyPatch(typeof(Avatar), "getJieDanSkillAddExp")]
    class jiedanPatch
    {
        public static bool Prefix(Avatar __instance, ref float __result)
        {
            __result = my_jindanxishu(__instance);
            return false;
        }
        static float my_jindanxishu(Avatar __instance)
        {
            if (__instance.level <= 6)
            {
                return 1;
            }
            else
            {
                int num = 1;
                foreach (SkillItem skillItem in __instance.hasJieDanSkillList)
                {
                    if (__instance.level >= 10)
                        num += (int)jsonData.instance.JieDanBiao[skillItem.itemId.ToString()]["EXP"].n * 15 / 2;
                    else
                        num += (int)jsonData.instance.JieDanBiao[skillItem.itemId.ToString()]["EXP"].n * 13 / 2;
                }
                return num * .01f;
            }
        }
    }

    [HarmonyPatch(typeof(Tab.TabWuPingPanel), "AddEquip", new Type[] { typeof(int), typeof(EquipItem) })]
    class EquipPatch
    {
        public static bool Prefix(Tab.TabWuPingPanel __instance, ref int index, ref EquipItem equipItem)
        {
            if (equipItem.Id == 1018)
                return true;
            KBEngine.Avatar player = PlayerEx.Player;
            int level = (int)player.level;
            level = (level + 5) / 3;
            if (level + 1 < equipItem.GetImgQuality())
            {
                UIPopTip.Inst.Pop("超阶物品，无法使用", PopTipIconType.叹号);
            }
            return level + 1 >= equipItem.GetImgQuality();
        }
    }

    [HarmonyPatch(typeof(UIBiGuanXiuLianPanel), "CalcShuangXiu", new Type[] { typeof(int) })]
    class shuangxiuPatch
    {
        public static bool Prefix(UIBiGuanXiuLianPanel __instance, ref int biGuanTime)
        {
            KBEngine.Avatar player = PlayerEx.Player;
            if (player.ShuangXiuData.HasField("JingYuan"))
            {
                JSONObject jsonobject = player.ShuangXiuData["JingYuan"];
                ShuangXiuMiShu shuangXiuMiShu = ShuangXiuMiShu.DataDict[jsonobject["Skill"].I];
                int i = jsonobject["PinJie"].I;
                int npcid = 0;
                bool flag = false;
                if (jsonobject.HasField("DaoLvID"))
                {
                    npcid = jsonobject["DaoLvID"].I;
                    flag = !NPCEx.IsDeath(npcid);
                }
                int jiazhi = ShuangXiuJingYuanJiaZhi.DataDict[shuangXiuMiShu.ningliantype].jiazhi;
                float tmp_sudu = pinghengBepInExMod.noDanyao.Value ? ShuangXiuLianHuaSuDu.DataDict[i].speed : Math.Min(UIBiGuanXiuLianPanel.GetBiguanSpeed(), ShuangXiuLianHuaSuDu.DataDict[i].speed);
                int i2 = (int)(tmp_sudu * biGuanTime / jiazhi);
                if (jiazhi > 1)
                {
                    int tmp_shengyu = (int)(tmp_sudu * biGuanTime - jiazhi * i2);
                    if (tmp_shengyu > 0)
                    {
                        int num = jsonobject["Count"].I;
                        if (num > jiazhi)
                        {
                            player.ShuangXiuData["JingYuan"].SetField("Count", tmp_shengyu + 1);
                        }
                        else
                        {
                            if (num + tmp_shengyu >= jiazhi)
                            {
                                i2++;
                            }
                            player.ShuangXiuData["JingYuan"].SetField("Count", (tmp_shengyu + num) % jiazhi + 1);
                        }
                    }
                }
                if (i2 > 0)
                {
                    if (shuangXiuMiShu.ningliantype == 1)
                    {
                        player.addEXP(i2);
                        if (flag)
                        {
                            NPCEx.AddJsonInt(npcid, "exp", i2);
                        }
                    }
                    else if (shuangXiuMiShu.ningliantype == 2)
                    {
                        player.xinjin += i2;
                    }
                    else if (shuangXiuMiShu.ningliantype == 3)
                    {
                        player.addShenShi(i2);
                        if (flag)
                        {
                            NPCEx.AddJsonInt(npcid, "shengShi", i2);
                        }
                    }
                    else if (shuangXiuMiShu.ningliantype == 4)
                    {
                        player._HP_Max += i2;
                        if (flag)
                        {
                            NPCEx.AddJsonInt(npcid, "HP", i2);
                        }
                    }
                }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(NpcJieSuanManager), "GetJieShaNpcList", new Type[] { typeof(int) })]
    class jieshaPatch
    {
        public static bool Prefix(NpcJieSuanManager __instance, ref List<int> __result, ref int index)
        {
            Debug.LogWarning("hello jesha");
            List<int> list = new List<int>();
            if (__instance.npcMap.bigMapNPCDictionary.ContainsKey(index) && __instance.npcMap.bigMapNPCDictionary[index].Count > 0)
            {
                foreach (int num in __instance.npcMap.bigMapNPCDictionary[index])
                {
                    bool b_xie = jsonData.instance.AvatarJsonData[num.ToString()]["XingGe"].I >= 10;
                    int tmp_level = jsonData.instance.AvatarJsonData[num.ToString()]["Level"].I;
                    if (jsonData.instance.AvatarRandomJsonData[num.ToString()]["HaoGanDu"].I < 40)
                    {
                        if ((__instance.GetNpcBigLevel(num) >= Tools.instance.getPlayer().getLevelType() && __instance.GetNpcData(num)["ActionId"].I == 34)
                        || (tmp_level > Tools.instance.getPlayer().level && b_xie))
                            list.Add(num);
                    }
                }
            }
            __result = list;
            return false;
        }
    }

    [HarmonyPatch(typeof(NpcJieSuanManager), "GuDingAddExp", new Type[] { typeof(int), typeof(float) })]
    class npcxiulianPatch
    {
        public static bool Prefix(NpcJieSuanManager __instance, ref int npcId, ref float times)
        {
            JSONObject npcData = __instance.GetNpcData(npcId);
            npcData.SetField("isTanChaUnlock", true);
            int num = get_xiulian_sudu(npcId);
            int zizhi = npcData["ziZhi"].I;
            float tmp_zizhi = get_zizhi_xishu(zizhi);
            num = (int)(num * (1 + tmp_zizhi));
            int npcBigLevel = NpcJieSuanManager.inst.GetNpcBigLevel(npcId);
            if (npcData.HasField("JinDanData"))
            {
                int tmp_jindanlv = npcData["JinDanData"]["JinDanLv"].I;
                float num2 = npcbiguanxiulianPatch.myget_jindan_xishu(npcBigLevel, tmp_jindanlv);
                num += (int)(num2 * (float)num);
            }
            else if (npcBigLevel >= 3)
            {
                float num2 = npcbiguanxiulianPatch.myget_jindan_xishu(npcBigLevel, 7);
                num += (int)(num2 * (float)num);
            }
            if (times == 1f)
                __instance.npcSetField.AddNpcExp(npcId, (int)((float)num * times));
            else
            {
                int dongfu = 100;
                switch (npcBigLevel)
                {
                    case 3:
                        dongfu = 150;
                        break;
                    case 4:
                        dongfu = 200;
                        break;
                    case 5:
                        dongfu = 210;
                        break;
                }
                if (PlayerEx.IsDaoLv(npcId))
                {
                    dongfu = 210;
                }
                num = (int)(num * (2 + tmp_zizhi * 3 + dongfu * 1.2f / 100) / (1 + tmp_zizhi) / 2.6f);
                __instance.npcSetField.AddNpcExp(npcId, (int)((float)num * times));
            }
            return false;
        }
        public static int get_xiulian_sudu(int npcId)
        {
            JSONObject npcData = NpcJieSuanManager.inst.GetNpcData(npcId);
            int npcBigLevel = NpcJieSuanManager.inst.GetNpcBigLevel(npcId);
            int num = 432 * 2;
            switch (npcBigLevel)
            {
                case 2:
                    num = 432 * 3;
                    break;
                case 3:
                    num = 864 * 3;
                    break;
                case 4:
                    num = 1080 * 4;
                    break;
            }
            //if (npcData["isImportant"].b)
            //{
            //    if (npcBigLevel == 3 && npcData.HasField("YuanYingTime"))
            //    {
            //        num = 1080 * 3;
            //    }
            //}
            if (npcBigLevel >= 5)
                num = 1296 * 5;
            return num;
        }
        public static float get_zizhi_xishu(int zizhi)
        {
            float tmp_zizhi = -.76f;
            {
                if (zizhi > 80)
                    tmp_zizhi = zizhi * .01f;
                else
                    tmp_zizhi = .24f * Mathf.Pow(1.02887f, zizhi - 10) - 1;
            }
            return tmp_zizhi;
        }
    }

    [HarmonyPatch(typeof(NPCXiuLian), "NpcBiGuan", new Type[] { typeof(int) })]
    class npcbiguanxiulianPatch
    {
        public static bool Prefix(NPCXiuLian __instance, ref int npcId)
        {
            NpcJieSuanManager.inst.npcUseItem.autoUseItem(npcId);
            JSONObject jsonobject = jsonData.instance.AvatarJsonData[npcId.ToString()];
            int npcBigLevel = NpcJieSuanManager.inst.GetNpcBigLevel(npcId);
            int dongfu = 100;
            switch (npcBigLevel)
            {
                case 3:
                    dongfu = 150;
                    break;
                case 4:
                    dongfu = 200;
                    break;
                case 5:
                    dongfu = 210;
                    break;
            }
            if (PlayerEx.IsDaoLv(npcId))
            {
                dongfu = 210;
                NpcJieSuanManager.inst.npcMap.AddNpcToThreeScene(npcId, 101);
            }
            JSONObject npcData = NpcJieSuanManager.inst.GetNpcData(npcId);
            int num = npcxiulianPatch.get_xiulian_sudu(npcId);
            int zizhi = npcData["ziZhi"].I;
            float tmp_zizhi = npcxiulianPatch.get_zizhi_xishu(zizhi);
            num = (int)(num * (tmp_zizhi + dongfu * 1.2f / 100));
            if (npcData.HasField("JinDanData"))
            {
                int tmp_jindanlv = npcData["JinDanData"]["JinDanLv"].I;
                float num2 = npcbiguanxiulianPatch.myget_jindan_xishu(npcBigLevel, tmp_jindanlv);
                num += (int)(num2 * (float)num);
            }
            else if (npcBigLevel >= 3)
            {
                float num2 = npcbiguanxiulianPatch.myget_jindan_xishu(npcBigLevel, 7);
                num += (int)(num2 * (float)num);
            }
            NpcJieSuanManager.inst.npcSetField.AddNpcExp(npcId, num);
            return false;
        }
        public static float myget_jindan_xishu(int npcBigLevel, int tmp_jindanlv)
        {
            return (npcBigLevel >= 4 ? .15f : .13f) * tmp_jindanlv - .99f;
        }
    }

    [HarmonyPatch(typeof(UIJianLingQingJiaoPanel), "Start")]
    class JianlingPatch
    {
        public static bool Prefix(UIJianLingQingJiaoPanel __instance)
        {
            for (int i = 0; i < __instance.QingJiaoSkills.Count; i++)
            {
                JianLingQingJiao qingJiao = JianLingQingJiao.DataList[i];
                if (qingJiao.JiYi == 70)
                    qingJiao.JiYi = 69;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(ShowXiaoGuoManager), "getTotalCiTiao")]
    class lianqi_sxgm_Patch
    {
        public static bool Prefix(ShowXiaoGuoManager __instance)
        {
            my_getTotalCiTiao(__instance);
            return false;
        }
        static void my_getTotalCiTiao(ShowXiaoGuoManager __instance)
        {
            List<PutMaterialCell> caiLiaoCells = LianQiTotalManager.inst.putMaterialPageManager.lianQiPageManager.putCaiLiaoCell.caiLiaoCells;
            __instance.entryDictionary = new Dictionary<int, int>();
            Dictionary<int, int> dictionary = new Dictionary<int, int>();
            for (int i = 0; i < caiLiaoCells.Count; i++)
            {
                if (caiLiaoCells[i].shuXingTypeID != 0)
                {
                    if (caiLiaoCells[i].shuXingTypeID >= 49 && caiLiaoCells[i].shuXingTypeID <= 56)
                    {
                        if (__instance.entryDictionary.ContainsKey(49))
                        {
                            Dictionary<int, int> dictionary2 = __instance.entryDictionary;
                            dictionary2[49] = dictionary2[49] + caiLiaoCells[i].lingLi;
                            dictionary2 = dictionary;
                            dictionary2[49] = dictionary2[49] + 1;
                        }
                        else
                        {
                            __instance.entryDictionary.Add(49, caiLiaoCells[i].lingLi);
                            dictionary.Add(49, 1);
                        }
                    }
                    else if (caiLiaoCells[i].shuXingTypeID >= 1 && caiLiaoCells[i].shuXingTypeID <= 8)
                    {
                        if (__instance.entryDictionary.ContainsKey(1))
                        {
                            Dictionary<int, int> dictionary2 = __instance.entryDictionary;
                            dictionary2[1] = dictionary2[1] + caiLiaoCells[i].lingLi;
                            dictionary2 = dictionary;
                            dictionary2[1] = dictionary2[1] + 1;
                        }
                        else
                        {
                            __instance.entryDictionary.Add(1, caiLiaoCells[i].lingLi);
                            dictionary.Add(1, 1);
                        }
                    }
                    else if (__instance.entryDictionary.ContainsKey(caiLiaoCells[i].shuXingTypeID))
                    {
                        Dictionary<int, int> dictionary2 = __instance.entryDictionary;
                        int shuXingTypeID = caiLiaoCells[i].shuXingTypeID;
                        dictionary2[shuXingTypeID] += caiLiaoCells[i].lingLi;
                        dictionary2 = dictionary;
                        shuXingTypeID = caiLiaoCells[i].shuXingTypeID;
                        dictionary2[shuXingTypeID]++;
                    }
                    else
                    {
                        __instance.entryDictionary.Add(caiLiaoCells[i].shuXingTypeID, caiLiaoCells[i].lingLi);
                        dictionary.Add(caiLiaoCells[i].shuXingTypeID, 1);
                    }
                }
            }
            if (__instance.entryDictionary.Keys.Count == 0)
            {
                return;
            }
            float wuWeiBaiFenBi = LianQiTotalManager.inst.putMaterialPageManager.wuWeiManager.getWuWeiBaiFenBi();
            int selectLingWenID = LianQiTotalManager.inst.putMaterialPageManager.lingWenManager.getSelectLingWenID();
            int num = -1;
            float num2 = -1f;
            KBEngine.Avatar player = Tools.instance.getPlayer();
            int wuDaoLevelByType = player.wuDaoMag.getWuDaoLevelByType(22);
            bool flag = false;
            if (player.checkHasStudyWuDaoSkillByID(2241))
            {
                flag = true;
            }
            if (selectLingWenID > 0)
            {
                num = jsonData.instance.LianQiLingWenBiao[selectLingWenID.ToString()]["value3"].I;
                num2 = jsonData.instance.LianQiLingWenBiao[selectLingWenID.ToString()]["value4"].n;
            }
            JSONObject lianQiHeCheng = jsonData.instance.LianQiHeCheng;
            Dictionary<int, int> dictionary3 = new Dictionary<int, int>();
            foreach (int num3 in __instance.entryDictionary.Keys)
            {
                int num4 = __instance.entryDictionary[num3];
                num4 = (int)((float)num4 * wuWeiBaiFenBi);
                if (num3 == 49)
                {
                    if (num4 >= jsonData.instance.LianQiDuoDuanShangHaiBiao["1"]["cast"].I)
                    {
                        dictionary3.Add(num3, num4);
                    }
                }
                else
                {
                    int i2 = lianQiHeCheng[num3.ToString()]["cast"].I;
                    if (num4 >= i2)
                    {
                        dictionary3.Add(num3, num4);
                    }
                }
            }
            if (dictionary3.Keys.Count == 0)
            {
                __instance.entryDictionary = dictionary3;
                return;
            }
            Dictionary<int, int> dictionary4 = new Dictionary<int, int>();
            int num5 = 0;
            foreach (int key in dictionary3.Keys)
            {
                num5 += dictionary[key];
            }
            if (selectLingWenID > 0 && num == 2)
            {
                num2 /= (float)num5;
            }
            foreach (int key2 in dictionary3.Keys)
            {
                int num6 = dictionary3[key2];
                if (selectLingWenID > 0)
                {
                    if (num == 1)
                    {
                        num6 = (int)((float)num6 * num2);
                    }
                    else
                    {
                        int num7 = (int)(num2 * (float)dictionary[key2]);
                        num6 += num7;
                    }
                }
                num6 = (int)(num6 * (1 + .1f * wuDaoLevelByType));
                if (flag)
                {
                    num6 = (int)((float)num6 * 1.5f);
                }
                int i3 = lianQiHeCheng[key2.ToString()]["cast"].I;
                int value = num6 / i3;
                dictionary4.Add(key2, value);
            }
            __instance.entryDictionary = dictionary4;
        }
    }

    [HarmonyPatch(typeof(ZhongLingLiManager), "getTotalZongLingLi")]
    class lianqi_zllm_Patch
    {
        public static void Postfix(ZhongLingLiManager __instance, ref float __result)
        {
            KBEngine.Avatar player = Tools.instance.getPlayer();
            int wuDaoLevelByType = player.wuDaoMag.getWuDaoLevelByType(22);
            __result = __result * (1 + .1f * wuDaoLevelByType);
        }
    }

    [HarmonyPatch(typeof(KillSystem.RewardOrder_RandomNpcFactory), "GetNpcId")]
    class rornf_Patch
    {
        public static bool Prefix(KillSystem.RewardOrder_RandomNpcFactory __instance, ref int __result, ref int id)
        {
            __result = my_GetNpcId(__instance, id);
            return false;
        }
        static int my_GetNpcId(KillSystem.RewardOrder_RandomNpcFactory __instance, int id)
        {
            List<int> liuPai = KillRandomNpcData.DataDict[id].LiuPai;
            List<int> level = KillRandomNpcData.DataDict[id].Level;
            List<int> xingGe = KillRandomNpcData.DataDict[id].XingGe;
            int num = liuPai[UnityEngine.Random.Range(0, liuPai.Count)];
            int num2 = level[level.Count - 1];
            int num3 = xingGe[UnityEngine.Random.Range(0, xingGe.Count)];
            //int num4 = 0;
            //int num5 = 0;
            foreach (JSONObject jsonobject in jsonData.instance.AvatarJsonData.list)
            {
                if (jsonobject["id"].I >= 20000
                    && (!jsonobject.HasField("isImportant") || !jsonobject["isImportant"].b)
                    && !KillSystem.KillManager.Inst.RewardOrderModels.ContainsKey(jsonobject["id"].I)
                    && level.Contains(jsonobject["Level"].I)
                    && liuPai.Contains(jsonobject["LiuPai"].I)
                    && xingGe.Contains(jsonobject["XingGe"].I))
                {
                    if (jsonobject["Level"].I == num2)
                        return jsonobject["id"].I;
                    //if (jsonobject["Level"].I > num4)
                    //{
                    //    num4 = jsonobject["Level"].I;
                    //    num5 = jsonobject["id"].I;
                    //}
                }
            }
            //if (num5 > 0)
            //    return num5;
            return FactoryManager.inst.npcFactory.CreateNpc(num, num2, num3);
        }
    }

    [HarmonyPatch(typeof(EndlessSeaMag), "SetCanSeeMonstar")]
    class EndlessSeaMag_SetCanSeeMonstar_Patch
    {
        public static void Postfix(EndlessSeaMag __instance)
        {
            //foreach (SeaAvatarObjBase seaAvatarObjBase in __instance.MonstarList)
            //{
            //    seaAvatarObjBase.ShowMonstarObj();
            //}
            foreach (var tmp in AllMapManage.instance.mapIndex)
            {
                MapSeaCompent mapSeaCompent = tmp.Value as MapSeaCompent;
                //if (mapSeaCompent.NodeHasIsLand())
                //{
                //    EndlessSeaMag.AddSeeIsland(tmp.Key);
                //}
                if (mapSeaCompent.WhetherHasJiZhi)
                {
                    EndlessSeaMag.AddSeeIsland(tmp.Key);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Avatar), "addEXP")]
    class falidai_Patch
    {
        static bool canexp = true;
        public static bool Prefix(Avatar __instance, ref int num)
        {
            if (pinghengBepInExMod.falidai.Value)
            {
                canexp = true;
                int biglevel = (__instance.level + 2) / 3;
                if (biglevel >= 5)
                {
                    return true;
                }
                if (num <= 0)
                {
                    return true;
                }
                else
                {
                    if ((int)(__instance.money) >= num * (5 - biglevel))
                    {
                        return true;
                    }
                }
                __instance.money += (ulong)num;
                UIPopTip.Inst.Pop("没钱，借了" + num.ToString() + "法力贷", PopTipIconType.叹号);
                canexp = false;
                return false;
            }
            return true;
        }
        public static void Postfix(Avatar __instance, ref int num)
        {
            if (pinghengBepInExMod.falidai.Value)
            {
                int biglevel = (__instance.level + 2) / 3;
                if (num > 0 && __instance.level < 13 && canexp)
                {
                    if ((int)(__instance.money) >= num * (5 - biglevel))
                    {
                        __instance.money -= (ulong)(num * (5 - biglevel));
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(UIBiGuanXiuLianPanel), "GetBiguanSpeed", new Type[] { typeof(bool), typeof(int), typeof(string) })]
    class ubgxlp_Patch
    {
        public static bool Prefix(UIBiGuanXiuLianPanel __instance, ref float __result, ref bool log, ref int biGuanType, ref string sceneName)
        {
            __result = my_GetBiguanSpeed(log, biGuanType, sceneName);
            return false;
        }
        static float my_GetBiguanSpeed(bool log = false, int biGuanType = 1, string sceneName = "")
        {
            KBEngine.Avatar player = PlayerEx.Player;
            int staticID = player.getStaticID();
            if (staticID != 0)
            {
                float num = jsonData.instance.XinJinGuanLianJsonData[player.getXinJinGuanlianType().ToString()]["speed"].n / 100f;
                string text = SceneEx.NowSceneName;
                if (!string.IsNullOrWhiteSpace(sceneName))
                {
                    text = sceneName;
                }
                float num2;
                if (text == "S101")
                {
                    DongFuData dongFuData = new DongFuData(DongFuManager.NowDongFuID);
                    dongFuData.Load();
                    float xiuliansudu = (float)DFLingYanLevel.DataDict[dongFuData.LingYanLevel].xiuliansudu;
                    int xiuliansudu2 = DFZhenYanLevel.DataDict[dongFuData.JuLingZhenLevel].xiuliansudu;
                    num2 = xiuliansudu + (float)xiuliansudu2;
                    num2 /= 100f;
                }
                else
                {
                    num2 = jsonData.instance.BiguanJsonData[biGuanType.ToString()]["speed"].n / 100f;
                }
                float n = jsonData.instance.StaticSkillJsonData[staticID.ToString()]["Skill_Speed"].n;
                float num3 = player.AddZiZhiSpeed(n);
                float jieDanSkillAddExp = player.getJieDanSkillAddExp();
                float num4 = (n * num2 * num + num3 * (num2 * num < 1 ? num2 * num : 1)) * jieDanSkillAddExp;
                float num5 = 0f;
                if (player.TianFuID.HasField(string.Concat(12)))
                {
                    num5 = player.TianFuID["12"].n / 100f;
                    num4 += num4 * num5;
                }
                if (log)
                {
                    Debug.Log(string.Format("闭关修炼速度:心境速度{0} 地脉速度(使用的场景{1}){2} 功法速度{3} 资质速度{4} 金丹加成{5} 天赋加成{6}", new object[]
                    {
                    num,
                    text,
                    num2,
                    n,
                    num3,
                    jieDanSkillAddExp,
                    num5
                    }) + string.Format("\n闭关速度结算:((({0}*{1}*{2})+{3})*{4})*(1+{5})={6}", new object[]
                    {
                    n,
                    num2,
                    num,
                    num3,
                    jieDanSkillAddExp,
                    num5,
                    num4
                    }));
                }
                return num4;
            }
            return 0f;
        }
    }
}
