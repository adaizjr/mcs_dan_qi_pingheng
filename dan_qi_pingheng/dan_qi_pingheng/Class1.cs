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

                    UIPopTip.Inst.Pop("超阶吃药，毒性大增", PopTipIconType.感悟);
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
                    if (num < 100 && num >= 0)
                    {
                        float jiaCheng = __result / (tmp_baseprice * .5f);
                        float newjiaCheng = jiaCheng - num * .01f + 1;
                        //if (jsonData.instance.ItemJsonData[string.Concat(__instance.itemID)]["seid"].ToList().Contains(7) && num <= 80)
                        //    newjiaCheng += .2f;
                        __result = (int)(tmp_baseprice * .5f * newjiaCheng);
                    }
                }
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
                    if (num < 100 && num >= 0)
                    {
                        float jiaCheng = __result / (tmp_baseprice * .5f);
                        float newjiaCheng = jiaCheng - num * .01f + 1;
                        //if (jsonData.instance.ItemJsonData[string.Concat(__instance.Id)]["seid"].ToList().Contains(7) && num <= 80)
                        //    newjiaCheng += .2f;
                        __result = (int)(tmp_baseprice * .5f * newjiaCheng);
                    }
                }
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
            if (__instance.ZiZhi > 85)
                __result = Math.Max(2, (__instance.ZiZhi - 85) * .01f + .85f) * speed;
            else
                __result = Math.Max(-.4f, (__instance.ZiZhi - 15) / 56f - .4f) * speed;
            return false;
        }
    }

    [HarmonyPatch(typeof(jsonData), "Preload")]
    class RandomPatch
    {
        public static void Postfix(jsonData __instance)
        {
            if (__instance.RandomList.Count < 9500)
                for (int i = 0; i < 9500; i++)
                {
                    __instance.RandomList.Add(jsonData.GetRandom());
                }
        }
    }

    [HarmonyPatch(typeof(Tab.TabWuPingPanel), "AddEquip", new Type[] { typeof(int), typeof(EquipItem) })]
    class EquipPatch
    {
        public static bool Prefix(Tab.TabWuPingPanel __instance, ref int index, ref EquipItem equipItem)
        {
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

    [HarmonyPatch(typeof(NPCFactory), "AuToCreateNpcs")]
    class gengduonpcPatch
    {
        static Dictionary<int, List<string>> NpcAuToCreateDictionary = new Dictionary<int, List<string>>();
        public static void Postfix(NPCFactory __instance)
        {
            JSONObject npcCreateData = jsonData.instance.NpcCreateData;
            if (NpcAuToCreateDictionary.Count < 1)
            {
                JSONObject npcleiXingDate = jsonData.instance.NPCLeiXingDate;
                foreach (string text in npcleiXingDate.keys)
                {
                    if (npcleiXingDate[text]["Level"].I == 1 && npcleiXingDate[text]["LiuPai"].I != 34)
                    {
                        int i = npcleiXingDate[text]["Type"].I;
                        if (NpcAuToCreateDictionary.ContainsKey(i))
                        {
                            NpcAuToCreateDictionary[i].Add(text);
                        }
                        else
                        {
                            NpcAuToCreateDictionary.Add(i, new List<string>
                        {
                            text
                        });
                        }
                    }
                }
            }
            foreach (JSONObject jsonobject in npcCreateData.list)
            {
                int j = jsonobject["NumA"].I;
                if (jsonobject["EventValue"].Count > 0 && GlobalValue.Get(jsonobject["EventValue"][0].I, "NPCFactory.AuToCreateNpcs 每10年自动生成NPC") == jsonobject["EventValue"][1].I)
                {
                    j = jsonobject["NumB"].I;
                }
                int i2 = jsonobject["id"].I;
                while (j > 0)
                {
                    string index = NpcAuToCreateDictionary[i2][__instance.getRandom(0, NpcAuToCreateDictionary[i2].Count - 1)];
                    JSONObject npcDate = new JSONObject(jsonData.instance.NPCLeiXingDate[index].ToString(), -2, false, false);
                    __instance.AfterCreateNpc(npcDate, false, 0, false, null, 0);
                    j--;
                }
            }
            Avatar player = Tools.instance.getPlayer();
            player.emailDateMag.AddNewEmail("3", new EmailData(3, 1, true, true, player.worldTimeMag.nowTime));
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
                int num = jsonobject["Count"].I;
                ShuangXiuMiShu shuangXiuMiShu = ShuangXiuMiShu.DataDict[jsonobject["Skill"].I];
                int i = jsonobject["PinJie"].I;
                int npcid = 0;
                bool flag = false;
                if (jsonobject.HasField("DaoLvID"))
                {
                    npcid = jsonobject["DaoLvID"].I;
                    flag = !NPCEx.IsDeath(npcid);
                }
                int tmp_max_jing = player.ShuangXiuData["JingYuan"].I;
                int i2 = (int)(jsonobject["Reward"].I * Mathf.Min(UIBiGuanXiuLianPanel.GetBiguanSpeed(), ShuangXiuLianHuaSuDu.DataDict[i].speed) / tmp_max_jing * biGuanTime);
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
            List<int> list = new List<int>();
            if (__instance.npcMap.bigMapNPCDictionary.ContainsKey(index) && __instance.npcMap.bigMapNPCDictionary[index].Count > 0)
            {
                foreach (int num in __instance.npcMap.bigMapNPCDictionary[index])
                {
                    if (__instance.GetNpcBigLevel(num) <= Tools.instance.getPlayer().getLevelType() + 1 && __instance.GetNpcBigLevel(num) >= Tools.instance.getPlayer().getLevelType() && jsonData.instance.AvatarRandomJsonData[num.ToString()]["HaoGanDu"].I < 50 && __instance.GetNpcData(num)["ActionId"].I == 34)
                    {
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
            if (npcData.HasField("JinDanData"))
            {
                float num2 = npcData["JinDanData"]["JinDanAddSpeed"].f / 100f;
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
                    num = 1080 * 3;
                    break;
                case 4:
                    num = 1080 * 4;
                    break;
            }
            if (npcBigLevel >= 5)
                num = 1296 * 5;
            if (npcBigLevel <= 1 && npcData["MenPai"].I == 5)
            {
                foreach (var tmp in npcData["staticSkills"].list)
                {
                    if (num < jsonData.instance.StaticSkillJsonData[tmp.I.ToString()]["Skill_Speed"].I)
                    {
                        num = jsonData.instance.StaticSkillJsonData[tmp.I.ToString()]["Skill_Speed"].I;
                    }
                }
            }
            return num;
        }
        public static float get_zizhi_xishu(int zizhi)
        {
            float tmp_zizhi = -.4f;
            {
                if (zizhi > 85)
                    tmp_zizhi = Math.Max(2, (zizhi - 85) * .01f + .85f);
                else
                    tmp_zizhi = Math.Max(-.4f, (zizhi - 15) / 56f - .4f);
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
            int dongfu = 100;
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
                float num2 = npcData["JinDanData"]["JinDanAddSpeed"].f / 100f;
                num += (int)(num2 * (float)num);
            }
            NpcJieSuanManager.inst.npcSetField.AddNpcExp(npcId, num);
            return false;
        }
    }
    [HarmonyPatch(typeof(CyEmail), "GetContent", new Type[] { typeof(string), typeof(EmailData) })]
    class mailPatch
    {
        public static bool Prefix(CyEmail __instance, ref string __result, ref string msg, ref EmailData emailData)
        {
            if (emailData.npcId == 3)
            {
                int[] arr_tmp = new int[10];
                int tmp_zong = 0;
                foreach (var tmp in jsonData.instance.AvatarJsonData.list)
                {
                    int tmp_level = tmp["Level"].I;
                    int tmp_big = (tmp_level - 1) / 3;
                    arr_tmp[tmp_big]++;
                    tmp_zong++;
                }

                __result = "总计" + tmp_zong.ToString() + "，练气" + arr_tmp[0].ToString() + "，筑基" + arr_tmp[1].ToString() + "，金丹" + arr_tmp[2].ToString() + "，元婴" + arr_tmp[3].ToString() + "，化神" + arr_tmp[4].ToString();
                return false;
            }
            return true;
        }
    }
}
