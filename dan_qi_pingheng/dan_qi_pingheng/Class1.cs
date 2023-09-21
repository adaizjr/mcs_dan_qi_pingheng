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
            // 使用Debug.Log()方法来将文本输出到控制台
            Debug.Log("Hello,mcs_pingheng!");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            Harmony.CreateAndPatchAll(typeof(pinghengBepInExMod));
        }

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
                for (int j = 0; j < listSum; j++)
                {
                    attaker.spell.addDBuff(15);
                }
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

                            UIPopTip.Inst.Pop("超阶吃药，毒性大增" + tmp_dandu.ToString(), PopTipIconType.感悟);
                        }
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
                    if (num < 100 && num >= 0)
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
            if (list.Contains(620))
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
            if (i2 == 3 && list.Contains(612))
            {
                isJiXu = true;
            }
            if (i2 == 4 && list.Contains(613))
            {
                isJiXu = true;
            }
            if (i2 == 5 && list.Contains(614))
            {
                isJiXu = true;
            }
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
                    if (num < 100 && num >= 0)
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
                UIPopTip.Inst.Pop("超阶物品，无法使用", PopTipIconType.感悟);
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
                UIPopTip.Inst.Pop("超阶物品，无法使用", PopTipIconType.感悟);
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
                UIPopTip.Inst.Pop("超阶物品，无法使用", PopTipIconType.感悟);
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
                int num = 37;
                foreach (SkillItem skillItem in __instance.hasJieDanSkillList)
                {
                    if (__instance.level >= 10)
                        num += (int)jsonData.instance.JieDanBiao[skillItem.itemId.ToString()]["EXP"].n * 11 / 2;
                    else
                        num += (int)jsonData.instance.JieDanBiao[skillItem.itemId.ToString()]["EXP"].n * 9 / 2;
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
                UIPopTip.Inst.Pop("超阶物品，无法使用", PopTipIconType.感悟);
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
                int i2 = (int)(Math.Min(UIBiGuanXiuLianPanel.GetBiguanSpeed(), ShuangXiuLianHuaSuDu.DataDict[i].speed) * biGuanTime / jiazhi);
                if (jiazhi > 1)
                {
                    int tmp_shengyu = (int)(Math.Min(UIBiGuanXiuLianPanel.GetBiguanSpeed(), ShuangXiuLianHuaSuDu.DataDict[i].speed) * biGuanTime - jiazhi * i2);
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
            Debug.Log("hello jesha");
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
            else if (npcBigLevel >= 4)
            {
                float num2 = .14f;
                num += (int)(num2 * (float)num);
            }
            __instance.npcSetField.AddNpcExp(npcId, (int)((float)num * times));
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
            if (npcData["isImportant"].b)
            {
                if (npcBigLevel == 4 && npcData.HasField("HuaShengTime"))
                {
                    num = 1296 * 4;
                }
                if (npcBigLevel == 3 && npcData.HasField("YuanYingTime"))
                {
                    num = 1080 * 3;
                }
            }
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
            else if (npcBigLevel >= 4)
            {
                float num2 = .14f;
                num += (int)(num2 * (float)num);
            }
            NpcJieSuanManager.inst.npcSetField.AddNpcExp(npcId, num);
            return false;
        }
        public static float myget_jindan_xishu(int npcBigLevel, int tmp_jindanlv)
        {
            return (npcBigLevel >= 4 ? .11f : .09f) * tmp_jindanlv - .63f;
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
}
