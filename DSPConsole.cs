using System;
using System.Collections.Generic;
using ABN;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace DSPConsole
{
    [BepInPlugin("com.wisper.dsp.console", "DSP Console extention", "1.0.7")]
    public class DSPConsole : BaseUnityPlugin
    {
        private static ConfigEntry<bool> config_easy_start;
        private static ConfigEntry<int> config_rsp_ratio;
        private static ConfigEntry<int> config_pile_ratio;

        private static ConfigEntry<int> config_prop_ratio_b;
        private static ConfigEntry<int> config_prop_ratio_r;
        private static ConfigEntry<int> config_prop_ratio_y;
        private static ConfigEntry<int> config_prop_ratio_p;
        private static ConfigEntry<int> config_prop_ratio_g;
        private static ConfigEntry<int> config_prop_ratio_w;
        private static bool pile_set_done = false;

        [Obsolete]
        private void Start()
        {
            config_easy_start = Config.Bind<bool>("config", "EasyStart", false, "轻松模式");
            config_rsp_ratio = Config.Bind<int>("config", "ResearchSpeedRatio", 10, "研究速度的倍率");
            config_pile_ratio = Config.Bind<int>("config", "StoragePileRatio", 10, "物品堆叠倍率");
            config_prop_ratio_b = Config.Bind<int>("config", "PropertyRatioElectro", 10, "电磁矩阵元数据统计倍率");
            config_prop_ratio_r = Config.Bind<int>("config", "PropertyRatioEnergy", 10, "能量矩阵元数据统计倍率");
            config_prop_ratio_y = Config.Bind<int>("config", "PropertyRatioStructure", 10, "结构矩阵元数据统计倍率");
            config_prop_ratio_p = Config.Bind<int>("config", "PropertyRatioInformation", 10, "信息矩阵元数据统计倍率");
            config_prop_ratio_g = Config.Bind<int>("config", "PropertyRatioGravity", 10, "引力矩阵元数据统计倍率");
            config_prop_ratio_w = Config.Bind<int>("config", "PropertyRatioUniverse", 10, "宇宙矩阵元数据统计倍率");

            Harmony.CreateAndPatchAll(typeof(DSPConsole), null);
        }
        private void Update()
        {
        }

        //允许成就上传
        [HarmonyPrefix]
        [HarmonyPatch(typeof(AchievementLogic), "active", MethodType.Getter)]
        public static bool MyActive(ref bool __result)
        {
            __result = true;
            return false;
        }

        //屏蔽异常检测
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameAbnormalityData_0925), "TriggerAbnormality")]
        public static bool MyTriggerAbnormality()
        {
            return false;
        }

        //研究速率
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameHistoryData), "UnlockTechFunction")]
        public static void MyUnlockTechFunction(GameHistoryData __instance, int func, ref double value)
        {
            if (func == 22)
            {
                value *= config_rsp_ratio.Value;
            }
        }

        //EasyMode
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameHistoryData), "NotifyTechUnlock")]
        public static void MyNotifyTechUnlock(GameHistoryData __instance, int _techId)
        {
            Debug.Log(String.Format("easy mode={0},techId={1}", config_easy_start.Value, _techId));
            if (config_easy_start.Value && _techId == 1001)
            {
                UnlockTechByID(2208);//机械骨骼Max
                UnlockTechByID(2406);//通讯控制Max
                UnlockTechByID(2605);//无人机引擎Lv5
                UnlockTechByID(2705);//蓝图Max
                UnlockTechByID(2501);//能量回路Lv1
                UnlockTechByID(4104);//宇宙探索Max
                UnlockTechByID(2902);//驱动引擎Lv2
                //UnlockTechByID(1606);//解锁吸尘器
            }
        }

        //研究速率
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameHistoryData), "SetForNewGame")]
        public static void MySetForNewGame(GameHistoryData __instance)
        {
            Debug.Log(String.Format("set research ratio to {0}", config_rsp_ratio.Value));
            __instance.techSpeed *= config_rsp_ratio.Value;
        }

        //储物箱，背包堆叠率
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StorageComponent), "LoadStatic")]
        public static void MyLoadStatic(StorageComponent __instance)
        {
            if (pile_set_done)
            {
                return;
            }
            ItemProto[] data_array = LDB.items.dataArray;
            for (int index = 0; index < data_array.Length; ++index)
            {
                data_array[index].StackSize *= config_pile_ratio.Value;
                Debug.Log(String.Format("set {0} size to {1}", data_array[index].ID, data_array[index].StackSize));
                StorageComponent.itemStackCount[data_array[index].ID] = data_array[index].StackSize;
            }
            pile_set_done = true;
        }

        //元数据转化率
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PropertyLogic), "UpdateProduction")]
        public static bool MyUpdateProduction(PropertyLogic __instance)
        {
            FactoryProductionStat[] factoryStatPool = __instance.gameData.statistics.production.factoryStatPool;
            int factoryCount = __instance.gameData.factoryCount;
            ClusterPropertyData clusterData = __instance.propertySystem.GetClusterData(__instance.gameData.GetClusterSeedKey());
            ClusterPropertyData propertyData = __instance.gameData.history.propertyData;
            foreach (int productId in PropertySystem.productIds)
            {
                int itemProduction1 = propertyData.GetItemProduction(productId);
                int itemProduction2 = clusterData.GetItemProduction(productId);
                long num = 0;
                for (int index = 0; index < factoryCount; ++index)
                {
                    int productIndex = factoryStatPool[index].productIndices[productId];
                    if (productIndex > 0)
                        num += factoryStatPool[index].productPool[productIndex].total[3];
                }
                int config_prop_ratio = 1;
                if (productId == 6001)
                {
                    config_prop_ratio = config_prop_ratio_b.Value;//蓝糖元数据获取率
                }
                else if (productId == 6002)
                {
                    config_prop_ratio = config_prop_ratio_r.Value;//红糖元数据获取率
                }
                else if (productId == 6003)
                {
                    config_prop_ratio = config_prop_ratio_y.Value;//黄糖元数据获取率
                }
                else if (productId == 6004)
                {
                    config_prop_ratio = config_prop_ratio_p.Value;//紫糖元数据获取率
                }
                else if (productId == 6005)
                {
                    config_prop_ratio = config_prop_ratio_g.Value;//绿糖元数据获取率
                }
                else if (productId == 6006)
                {
                    config_prop_ratio = config_prop_ratio_w.Value;//白糖元数据获取率
                }

                int count = (int)((double)num * config_prop_ratio * (double)__instance.gameData.history.minimalPropertyMultiplier / 60 + 0.001);
                if (count > itemProduction1)
                    propertyData.SetItemProduction(productId, count);
                if (count > itemProduction2)
                    clusterData.SetItemProduction(productId, count);
            }
            return false;
        }

        public static bool UnlockTechByID(int techId)
        {
            TechProto techProto = LDB.techs.Select(techId);
            if (techProto == null)
            {
                return false;
            }

            if (GameMain.data.history.TechUnlocked(techId))
            {
                return true;
            }

            Debug.Log(String.Format("will unlock tech {0},level[{1}:{2}]", techId, techProto.Level, techProto.MaxLevel));
            for (int index = 0; index < techProto.PreTechs.Length; ++index)
            {
                Debug.Log(String.Format("unlock pre tech {0}", techProto.PreTechs[index]));
                if (GameMain.data.history.TechUnlocked(techProto.PreTechs[index], techProto.PreTechsMax) == false)
                {
                    if (UnlockTechByID(techProto.PreTechs[index]) == false)
                    {
                        return false;
                    }
                }
            }

            for (int index = 0; index < techProto.PreTechsImplicit.Length; ++index)
            {
                Debug.Log(String.Format("unlock implicit pre tech {0}", techProto.PreTechsImplicit[index]));
                if (GameMain.data.history.TechUnlocked(techProto.PreTechsImplicit[index], techProto.PreTechsMax) == false)
                {
                    if (UnlockTechByID(techProto.PreTechsImplicit[index]) == false)
                    {
                        return false;
                    }
                }
            }

            for (int i = techProto.Level; i <= techProto.MaxLevel; i++)
            {
                GameMain.data.history.UnlockTech(techId);
                Debug.Log(String.Format("unlock tech {0},level[{1}:{2}]", techId, techProto.Level, techProto.MaxLevel));
            }
            return true;
        }
    }
}

/*
             //机甲核心
            GameMain.history.UnlockTech(2101);
            GameMain.history.UnlockTech(2102);
            GameMain.history.UnlockTech(2103);
            GameMain.history.UnlockTech(2104);
            GameMain.history.UnlockTech(2105);

            //机械骨骼
            GameMain.history.UnlockTech(2201);
            GameMain.history.UnlockTech(2202);
            GameMain.history.UnlockTech(2203);
            GameMain.history.UnlockTech(2204);
            GameMain.history.UnlockTech(2205);
            GameMain.history.UnlockTech(2206);
            GameMain.history.UnlockTech(2207);
            GameMain.history.UnlockTech(2208);

            //机舱容量
            GameMain.history.UnlockTech(2301);
            GameMain.history.UnlockTech(2302);
            GameMain.history.UnlockTech(2303);
            GameMain.history.UnlockTech(2304);
            GameMain.history.UnlockTech(2305);
            GameMain.history.UnlockTech(2306);
            GameMain.history.UnlockTech(2307);//w

            //通讯控制
            GameMain.history.UnlockTech(2401);
            GameMain.history.UnlockTech(2402);
            GameMain.history.UnlockTech(2403);
            GameMain.history.UnlockTech(2404);
            GameMain.history.UnlockTech(2405);
            GameMain.history.UnlockTech(2406);

            //能量回路
            GameMain.history.UnlockTech(2501);
            GameMain.history.UnlockTech(2502);
            GameMain.history.UnlockTech(2503);
            GameMain.history.UnlockTech(2504);
            GameMain.history.UnlockTech(2505);

            //无人机引擎
            GameMain.history.UnlockTech(2601);
            GameMain.history.UnlockTech(2602);
            GameMain.history.UnlockTech(2603);
            GameMain.history.UnlockTech(2604);
            GameMain.history.UnlockTech(2605);

            //蓝图
            GameMain.history.UnlockTech(2701);
            GameMain.history.UnlockTech(2702);
            GameMain.history.UnlockTech(2703);
            GameMain.history.UnlockTech(2704);
            GameMain.history.UnlockTech(2705);

            //能量护盾
            GameMain.history.UnlockTech(2801);
            GameMain.history.UnlockTech(2802);
            GameMain.history.UnlockTech(2803);
            GameMain.history.UnlockTech(2804);
            GameMain.history.UnlockTech(2805);

            //驱动引擎
            GameMain.history.UnlockTech(2901);
            GameMain.history.UnlockTech(2902);
            GameMain.history.UnlockTech(2903);
            GameMain.history.UnlockTech(2904);
            GameMain.history.UnlockTech(2905);

            //自动标记重建
            GameMain.history.UnlockTech(2951);
            GameMain.history.UnlockTech(2952);
            GameMain.history.UnlockTech(2953);
            GameMain.history.UnlockTech(2954);
            GameMain.history.UnlockTech(2955);
            GameMain.history.UnlockTech(2956);//w

            //太阳帆寿命
            GameMain.history.UnlockTech(3101);
            GameMain.history.UnlockTech(3102);
            GameMain.history.UnlockTech(3103);
            GameMain.history.UnlockTech(3104);
            GameMain.history.UnlockTech(3105);
            GameMain.history.UnlockTech(3106);

            //射线传输效率
            GameMain.history.UnlockTech(3201);
            GameMain.history.UnlockTech(3202);
            GameMain.history.UnlockTech(3203);
            GameMain.history.UnlockTech(3204);
            GameMain.history.UnlockTech(3205);
            GameMain.history.UnlockTech(3206);
            GameMain.history.UnlockTech(3207);

            //分拣器货物叠加（已弃用）
            //GameMain.history.UnlockTech(3301);
            //GameMain.history.UnlockTech(3302);
            //GameMain.history.UnlockTech(3303);
            //GameMain.history.UnlockTech(3304);
            //GameMain.history.UnlockTech(3305);

            //集装分拣器改良
            GameMain.history.UnlockTech(3311);
            GameMain.history.UnlockTech(3312);
            GameMain.history.UnlockTech(3313);
            GameMain.history.UnlockTech(3314);
            GameMain.history.UnlockTech(3315);
            GameMain.history.UnlockTech(3316);

            //运输船引擎
            GameMain.history.UnlockTech(3401);
            GameMain.history.UnlockTech(3402);
            GameMain.history.UnlockTech(3403);
            GameMain.history.UnlockTech(3404);
            GameMain.history.UnlockTech(3405);
            GameMain.history.UnlockTech(3406);

            //运输机舱扩容
            GameMain.history.UnlockTech(3501);
            GameMain.history.UnlockTech(3502);
            GameMain.history.UnlockTech(3503);
            GameMain.history.UnlockTech(3504);
            GameMain.history.UnlockTech(3505);
            GameMain.history.UnlockTech(3506);
            GameMain.history.UnlockTech(3507);
            GameMain.history.UnlockTech(3508);

            //矿物利用
            GameMain.history.UnlockTech(3601);
            GameMain.history.UnlockTech(3602);
            GameMain.history.UnlockTech(3603);
            GameMain.history.UnlockTech(3604);
            GameMain.history.UnlockTech(3605);

            //垂直建造
            GameMain.history.UnlockTech(3701);
            GameMain.history.UnlockTech(3702);
            GameMain.history.UnlockTech(3703);
            GameMain.history.UnlockTech(3704);
            GameMain.history.UnlockTech(3705);
            GameMain.history.UnlockTech(3706);

            //运输站集装物流
            GameMain.history.UnlockTech(3801);//w
            GameMain.history.UnlockTech(3802);//w
            GameMain.history.UnlockTech(3803);//w

            //研究速度
            GameMain.history.UnlockTech(3901);
            GameMain.history.UnlockTech(3902);
            GameMain.history.UnlockTech(3903);

            //配送范围
            GameMain.history.UnlockTech(4001);
            GameMain.history.UnlockTech(4002);
            GameMain.history.UnlockTech(4003);
            GameMain.history.UnlockTech(4004);
            GameMain.history.UnlockTech(4005);

            //宇宙探索
            GameMain.history.UnlockTech(4101);
            GameMain.history.UnlockTech(4102);
            GameMain.history.UnlockTech(4103);
            GameMain.history.UnlockTech(4104);

            //动能武器伤害
            GameMain.history.UnlockTech(5001);
            GameMain.history.UnlockTech(5002);
            GameMain.history.UnlockTech(5003);
            GameMain.history.UnlockTech(5004);
            GameMain.history.UnlockTech(5005);

            //能量武器伤害
            GameMain.history.UnlockTech(5101);
            GameMain.history.UnlockTech(5102);
            GameMain.history.UnlockTech(5103);
            GameMain.history.UnlockTech(5104);
            GameMain.history.UnlockTech(5105);

            //爆破武器伤害
            GameMain.history.UnlockTech(5201);
            GameMain.history.UnlockTech(5202);
            GameMain.history.UnlockTech(5203);
            GameMain.history.UnlockTech(5204);
            GameMain.history.UnlockTech(5205);

            //战斗无人机伤害
            GameMain.history.UnlockTech(5301);
            GameMain.history.UnlockTech(5302);
            GameMain.history.UnlockTech(5303);
            GameMain.history.UnlockTech(5304);

            //战斗无人机射速
            GameMain.history.UnlockTech(5401);
            GameMain.history.UnlockTech(5402);
            GameMain.history.UnlockTech(5403);
            GameMain.history.UnlockTech(5404);
            GameMain.history.UnlockTech(5405);//w

            //战斗无人机耐久
            GameMain.history.UnlockTech(5601);
            GameMain.history.UnlockTech(5602);
            GameMain.history.UnlockTech(5603);
            GameMain.history.UnlockTech(5604);
            GameMain.history.UnlockTech(5605);//w

            //行星护盾
            GameMain.history.UnlockTech(5701);
            GameMain.history.UnlockTech(5702);
            GameMain.history.UnlockTech(5703);
            GameMain.history.UnlockTech(5704);
            GameMain.history.UnlockTech(5705);

            //地面编队扩容
            GameMain.history.UnlockTech(5801);
            GameMain.history.UnlockTech(5802);
            GameMain.history.UnlockTech(5803);
            GameMain.history.UnlockTech(5804);
            GameMain.history.UnlockTech(5805);
            GameMain.history.UnlockTech(5806);//w
            GameMain.history.UnlockTech(5807);//w

            //太空编队扩容
            GameMain.history.UnlockTech(5901);
            GameMain.history.UnlockTech(5902);
            GameMain.history.UnlockTech(5903);
            GameMain.history.UnlockTech(5904);
            GameMain.history.UnlockTech(5905);
            GameMain.history.UnlockTech(5906);//w
            GameMain.history.UnlockTech(5907);//w

            //结构强化
            GameMain.history.UnlockTech(6001);
            GameMain.history.UnlockTech(6002);
            GameMain.history.UnlockTech(6003);
            GameMain.history.UnlockTech(6004);
            GameMain.history.UnlockTech(6005);

            //电磁武器效果
            GameMain.history.UnlockTech(6101);
            GameMain.history.UnlockTech(6102);
            GameMain.history.UnlockTech(6103);
            GameMain.history.UnlockTech(6104);
            GameMain.history.UnlockTech(6105);
            GameMain.history.UnlockTech(6106);//w

            GameMain.history.UnlockTech(1001);//电磁学
            GameMain.history.UnlockTech(1002);//电磁矩阵
            GameMain.history.UnlockTech(1101);//高效电浆控制
            GameMain.history.UnlockTech(1102);//等离子萃取精炼
            GameMain.history.UnlockTech(1103);//X射线裂解
            GameMain.history.UnlockTech(1104);//重整精炼
            GameMain.history.UnlockTech(1111);//能量矩阵
            GameMain.history.UnlockTech(1112);//氢燃料棒
            GameMain.history.UnlockTech(1113);//推进器
            GameMain.history.UnlockTech(1114);//加力推进器
            GameMain.history.UnlockTech(1120);//流体储存封装
            GameMain.history.UnlockTech(1121);//基础化工
            GameMain.history.UnlockTech(1122);//高分子化工
            GameMain.history.UnlockTech(1123);//高强度晶体
            GameMain.history.UnlockTech(1124);//结构矩阵
            GameMain.history.UnlockTech(1125);//卡西米尔晶体
            GameMain.history.UnlockTech(1126);//高强度玻璃
            GameMain.history.UnlockTech(1131);//应用型超导体
            GameMain.history.UnlockTech(1132);//高强度材料
            GameMain.history.UnlockTech(1133);//粒子可控
            GameMain.history.UnlockTech(1134);//重氢分馏
            GameMain.history.UnlockTech(1141);//波函数干扰
            GameMain.history.UnlockTech(1142);//微型粒子对撞机
            GameMain.history.UnlockTech(1143);//奇异物质
            GameMain.history.UnlockTech(1144);//人造恒星
            GameMain.history.UnlockTech(1145);//可控湮灭反应
            GameMain.history.UnlockTech(1151);//增产剂 Mk.I
            GameMain.history.UnlockTech(1152);//增产剂 Mk.II
            GameMain.history.UnlockTech(1153);//增产剂 Mk.III
            GameMain.history.UnlockTech(1201);//基础制造
            GameMain.history.UnlockTech(1202);//高速制造
            GameMain.history.UnlockTech(1203);//量子打印
            GameMain.history.UnlockTech(1302);//处理器
            GameMain.history.UnlockTech(1303);//量子芯片
            GameMain.history.UnlockTech(1304);//光子聚束采矿
            GameMain.history.UnlockTech(1305);//亚微观量子纠缠
            GameMain.history.UnlockTech(1311);//半导体材料
            GameMain.history.UnlockTech(1312);//信息矩阵
            GameMain.history.UnlockTech(1401);//自动化冶金
            GameMain.history.UnlockTech(1402);//冶炼提纯
            GameMain.history.UnlockTech(1403);//晶体冶炼
            GameMain.history.UnlockTech(1411);//钢材冶炼
            GameMain.history.UnlockTech(1412);//火力发电
            GameMain.history.UnlockTech(1413);//钛矿冶炼
            GameMain.history.UnlockTech(1414);//高强度钛合金
            GameMain.history.UnlockTech(1415);//移山填海工程
            GameMain.history.UnlockTech(1416);//微型核聚变发电
            GameMain.history.UnlockTech(1417);//位面冶金
            GameMain.history.UnlockTech(1501);//太阳能收集
            GameMain.history.UnlockTech(1502);//光子变频
            GameMain.history.UnlockTech(1503);//太阳帆轨道系统
            GameMain.history.UnlockTech(1504);//射线接收站
            GameMain.history.UnlockTech(1505);//行星电离层利用
            GameMain.history.UnlockTech(1506);//狄拉克逆变机制
            GameMain.history.UnlockTech(1507);//宇宙矩阵
            GameMain.history.UnlockTech(1511);//能量储存
            GameMain.history.UnlockTech(1512);//星际电力运输
            GameMain.history.UnlockTech(1513);//地热开采
            GameMain.history.UnlockTech(1521);//高强度轻质结构
            GameMain.history.UnlockTech(1522);//垂直发射井
            GameMain.history.UnlockTech(1523);//戴森球应力系统
            GameMain.history.UnlockTech(1523);//戴森球应力系统
            GameMain.history.UnlockTech(1523);//戴森球应力系统
            GameMain.history.UnlockTech(1523);//戴森球应力系统
            GameMain.history.UnlockTech(1523);//戴森球应力系统
            GameMain.history.UnlockTech(1523);//戴森球应力系统
            GameMain.history.UnlockTech(1601);//基础物流系统
            GameMain.history.UnlockTech(1602);//改良物流系统
            GameMain.history.UnlockTech(1603);//高效物流系统
            GameMain.history.UnlockTech(1604);//行星物流系统
            GameMain.history.UnlockTech(1605);//星际物流系统
            GameMain.history.UnlockTech(1606);//气态行星开采
            GameMain.history.UnlockTech(1607);//集装物流系统
            GameMain.history.UnlockTech(1608);//配送物流系统
            GameMain.history.UnlockTech(1701);//电磁驱动
            GameMain.history.UnlockTech(1702);//磁悬浮
            GameMain.history.UnlockTech(1703);//粒子磁力阱
            GameMain.history.UnlockTech(1704);//引力波折射
            GameMain.history.UnlockTech(1705);//引力矩阵
            GameMain.history.UnlockTech(1711);//超级磁场发生器
            GameMain.history.UnlockTech(1712);//卫星配电系统
            GameMain.history.UnlockTech(1801);//武器系统
            GameMain.history.UnlockTech(1802);//燃烧单元
            GameMain.history.UnlockTech(1803);//爆破单元
            GameMain.history.UnlockTech(1804);//晶石爆破单元
            GameMain.history.UnlockTech(1805);//动力引擎
            GameMain.history.UnlockTech(1806);//导弹防御塔
            GameMain.history.UnlockTech(1807);//聚爆加农炮
            GameMain.history.UnlockTech(1808);//信号塔
            GameMain.history.UnlockTech(1809);//行星防御系统
            GameMain.history.UnlockTech(1810);//干扰塔
            GameMain.history.UnlockTech(1811);//磁化电浆炮
            GameMain.history.UnlockTech(1812);//钛化弹箱
            GameMain.history.UnlockTech(1813);//超合金弹箱
            GameMain.history.UnlockTech(1814);//高爆炮弹组
            GameMain.history.UnlockTech(1815);//超音速导弹组
            GameMain.history.UnlockTech(1816);//晶石炮弹组
            GameMain.history.UnlockTech(1817);//引力导弹组
            GameMain.history.UnlockTech(1818);//反物质胶囊
            GameMain.history.UnlockTech(1819);//原型机
            GameMain.history.UnlockTech(1820);//精准无人机
            GameMain.history.UnlockTech(1821);//攻击无人机
            GameMain.history.UnlockTech(1822);//护卫舰
            GameMain.history.UnlockTech(1823);//驱逐舰
            GameMain.history.UnlockTech(1824);//压制胶囊
            GameMain.history.UnlockTech(1826);//战场分析基站
 */