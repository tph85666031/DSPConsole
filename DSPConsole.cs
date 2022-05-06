using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace DSPConsole
{
    [BepInPlugin("com.wisper.dsp.console", "Console Extension", "1.0.0")]
    public class DSPConsole : BaseUnityPlugin
    {
        private static ConfigEntry<int> config_rsp_ratio;
        private static ConfigEntry<int> config_pile_ratio;

        private static ConfigEntry<int> config_prop_ratio_b;
        private static ConfigEntry<int> config_prop_ratio_r;
        private static ConfigEntry<int> config_prop_ratio_y;
        private static ConfigEntry<int> config_prop_ratio_p;
        private static ConfigEntry<int> config_prop_ratio_g;
        private static ConfigEntry<int> config_prop_ratio_w;
        private static bool pile_set_done = false;
        private static bool console_inited = false;

        [Obsolete]
        private void Start()
        {
            config_rsp_ratio = Config.Bind<int>("config", "ResearchSpeedRatio", 10, "研究速度的倍率");
            config_pile_ratio = Config.Bind<int>("config", "StoragePileRatio", 10, "物品堆叠倍率");
            config_prop_ratio_b = Config.Bind<int>("config", "PropertyRatioElectro", 60, "电磁矩阵元数据统计倍率");
            config_prop_ratio_r = Config.Bind<int>("config", "PropertyRatioEnergy", 60, "能量矩阵元数据统计倍率");
            config_prop_ratio_y = Config.Bind<int>("config", "PropertyRatioStructure", 60, "结构矩阵元数据统计倍率");
            config_prop_ratio_p = Config.Bind<int>("config", "PropertyRatioInformation", 60, "信息矩阵元数据统计倍率");
            config_prop_ratio_g = Config.Bind<int>("config", "PropertyRatioGravity", 60, "引力矩阵元数据统计倍率");
            config_prop_ratio_w = Config.Bind<int>("config", "PropertyRatioUniverse", 60, "宇宙矩阵元数据统计倍率");
            Harmony.CreateAndPatchAll(typeof(DSPConsole), null);
        }

        private void Update()
        {
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameHistoryData), "UnlockTechFunction")]
        public static void MyUnlockTechFunction(GameHistoryData __instance, int func, ref double value)
        {
            if (func == 22)
            {
                value *= config_rsp_ratio.Value;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameHistoryData), "SetForNewGame")]
        public static void MySetForNewGame(GameHistoryData __instance)
        {
            Debug.Log(String.Format("set research ratio to {0}", config_rsp_ratio.Value));
            __instance.techSpeed *= config_rsp_ratio.Value;
        }

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

        private static string ConsoleSetPile(string param)
        {
            if (GameMain.mainPlayer == null)
            {
                return "not in game!";
            }
            int ratio = 1;
            int.TryParse(param, out ratio);
            if (ratio <= 0)
            {
                return "invalid param";
            }
            ItemProto[] data_array = LDB.items.dataArray;
            for (int index = 0; index < data_array.Length; ++index)
            {
                StorageComponent.itemStackCount[data_array[index].ID] = data_array[index].StackSize * ratio;
            }
            config_pile_ratio.Value = ratio;
            return "ok";
        }

        private static string ConsoleSetRsp(string param)
        {
            if (GameMain.mainPlayer == null)
            {
                return "not in game!";
            }
            int ratio = 1;
            int.TryParse(param, out ratio);
            if (ratio <= 0)
            {
                return "invalid param";
            }
            config_rsp_ratio.Value = ratio;
            return "ok";
        }

        private static string ConsoleSetProperty(string param)
        {
            if (GameMain.mainPlayer == null)
            {
                return "not in game!";
            }
            string[] ratios = param.Split(',');
            if (ratios.Length != 6)
            {
                return "invalid param";
            }

            int ratio = 1;
            int.TryParse(ratios[0], out ratio);
            if (ratio <= 0)
            {
                return "invalid param";
            }
            config_prop_ratio_b.Value = ratio;

            int.TryParse(ratios[1], out ratio);
            if (ratio <= 0)
            {
                return "invalid param";
            }
            config_prop_ratio_r.Value = ratio;

            int.TryParse(ratios[2], out ratio);
            if (ratio <= 0)
            {
                return "invalid param";
            }
            config_prop_ratio_y.Value = ratio;

            int.TryParse(ratios[3], out ratio);
            if (ratio <= 0)
            {
                return "invalid param";
            }
            config_prop_ratio_p.Value = ratio;

            int.TryParse(ratios[4], out ratio);
            if (ratio <= 0)
            {
                return "invalid param";
            }
            config_prop_ratio_g.Value = ratio;

            int.TryParse(ratios[5], out ratio);
            if (ratio <= 0)
            {
                return "invalid param";
            }
            config_prop_ratio_w.Value = ratio;

            return "ok";
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(XConsole), "Update")]
        public static void MyUpdate(XConsole __instance)
        {
            if (console_inited == false)
            {
                console_inited = true;

                Debug.Log("Start register pile command ...");
                XConsole.RegisterCommand("pile", new XConsole.DCommandFunc(ConsoleSetPile));

                Debug.Log("Start register rsp command ...");
                XConsole.RegisterCommand("rsp", new XConsole.DCommandFunc(ConsoleSetRsp));

                Debug.Log("Start register property command ...");
                XConsole.RegisterCommand("prop", new XConsole.DCommandFunc(ConsoleSetProperty));
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(XConsole), "UnlockTech")]
        public static bool MyUnlockTech(XConsole __instance, ref string __result, string param)
        {
            if (GameMain.mainPlayer == null)
            {
                return true;
            }

            if (param != "noob" && param != "all")
            {
                return true;
            }

            //机械骨骼
            GameMain.history.UnlockTech(2201);
            GameMain.history.UnlockTech(2202);
            GameMain.history.UnlockTech(2203);
            GameMain.history.UnlockTech(2204);
            GameMain.history.UnlockTech(2205);
            GameMain.history.UnlockTech(2206);
            GameMain.history.UnlockTech(2207);
            GameMain.history.UnlockTech(2208);

            //通讯控制
            GameMain.history.UnlockTech(2401);
            GameMain.history.UnlockTech(2402);
            GameMain.history.UnlockTech(2403);
            GameMain.history.UnlockTech(2404);
            GameMain.history.UnlockTech(2405);
            GameMain.history.UnlockTech(2406);

            //无人机引擎
            GameMain.history.UnlockTech(2601);
            GameMain.history.UnlockTech(2602);
            GameMain.history.UnlockTech(2603);
            GameMain.history.UnlockTech(2604);
            GameMain.history.UnlockTech(2605);

            if (param == "all")
            {
                //机甲核心
                GameMain.history.UnlockTech(2101);
                GameMain.history.UnlockTech(2102);
                GameMain.history.UnlockTech(2103);
                GameMain.history.UnlockTech(2104);
                GameMain.history.UnlockTech(2105);

                //机舱容量
                GameMain.history.UnlockTech(2301);
                GameMain.history.UnlockTech(2302);
                GameMain.history.UnlockTech(2303);
                GameMain.history.UnlockTech(2304);
                GameMain.history.UnlockTech(2305);
                GameMain.history.UnlockTech(2306);

                //能量回路
                GameMain.history.UnlockTech(2501);
                GameMain.history.UnlockTech(2502);
                GameMain.history.UnlockTech(2503);
                GameMain.history.UnlockTech(2504);
                GameMain.history.UnlockTech(2505);

                //
                GameMain.history.UnlockTech(2701);
                GameMain.history.UnlockTech(2702);
                GameMain.history.UnlockTech(2703);
                GameMain.history.UnlockTech(2704);
                GameMain.history.UnlockTech(2705);

                //驱动引擎
                GameMain.history.UnlockTech(2901);
                GameMain.history.UnlockTech(2902);
                GameMain.history.UnlockTech(2903);
                GameMain.history.UnlockTech(2904);
                GameMain.history.UnlockTech(2905);

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

                //分拣货物叠加
                GameMain.history.UnlockTech(3301);
                GameMain.history.UnlockTech(3302);
                GameMain.history.UnlockTech(3303);
                GameMain.history.UnlockTech(3304);
                GameMain.history.UnlockTech(3305);

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
                GameMain.history.UnlockTech(3509);
                GameMain.history.UnlockTech(3510);

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
                GameMain.history.UnlockTech(3801);
                GameMain.history.UnlockTech(3802);
                GameMain.history.UnlockTech(3803);

                //研究速度
                GameMain.history.UnlockTech(3901);
                GameMain.history.UnlockTech(3902);
                GameMain.history.UnlockTech(3903);

                //宇宙探索
                GameMain.history.UnlockTech(4101);
                GameMain.history.UnlockTech(4102);
                GameMain.history.UnlockTech(4103);
                GameMain.history.UnlockTech(4104);

                GameMain.history.UnlockTech(1001);//电磁学
                GameMain.history.UnlockTech(1002);//电磁矩阵
                GameMain.history.UnlockTech(1101);//高效电浆控制
                GameMain.history.UnlockTech(1102);//等离子萃取精炼
                GameMain.history.UnlockTech(1103);//X射线裂解
                GameMain.history.UnlockTech(1111);//能量矩阵
                GameMain.history.UnlockTech(1112);//氢燃料棒
                GameMain.history.UnlockTech(1113);//推进器
                GameMain.history.UnlockTech(1114);//加力推进器
                GameMain.history.UnlockTech(1120);//流体储存封装
                GameMain.history.UnlockTech(1121);//基础化工
                GameMain.history.UnlockTech(1122);//高分子化工
                GameMain.history.UnlockTech(1123);//高强度精纯
                GameMain.history.UnlockTech(1124);//结构矩阵
                GameMain.history.UnlockTech(1125);//卡西米尔精纯
                GameMain.history.UnlockTech(1126);//高强度玻璃
                GameMain.history.UnlockTech(1131);//应用型超导体
                GameMain.history.UnlockTech(1132);//高强度材料
                GameMain.history.UnlockTech(1133);//粒子可控技术
                GameMain.history.UnlockTech(1134);//重氢分馏
                GameMain.history.UnlockTech(1141);//波函数干扰
                GameMain.history.UnlockTech(1142);//微型粒子对撞机
                GameMain.history.UnlockTech(1143);//奇异物质
                GameMain.history.UnlockTech(1144);//人造恒星
                GameMain.history.UnlockTech(1145);//可控湮灭反应
                GameMain.history.UnlockTech(1151);//加速剂Ⅰ
                GameMain.history.UnlockTech(1152);//加速剂Ⅱ
                GameMain.history.UnlockTech(1153);//加速剂Ⅲ
                GameMain.history.UnlockTech(1201);//基础制造工艺制造台Ⅰ
                GameMain.history.UnlockTech(1202);//高级制造工艺制造台Ⅱ
                GameMain.history.UnlockTech(1203);//量子打印技术制造台Ⅲ
                GameMain.history.UnlockTech(1302);//处理器
                GameMain.history.UnlockTech(1303);//量子芯片
                GameMain.history.UnlockTech(1304);//光子聚束采矿技术
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
                GameMain.history.UnlockTech(1417);//位面冶金技术
                GameMain.history.UnlockTech(1501);//太阳能收集
                GameMain.history.UnlockTech(1502);//光子变频
                GameMain.history.UnlockTech(1503);//太阳帆轨道系统
                GameMain.history.UnlockTech(1504);//射线接收站
                GameMain.history.UnlockTech(1505);//行星电离层利用
                GameMain.history.UnlockTech(1506);//狄拉克逆变机制
                //GameMain.history.UnlockTech(1507);//宇宙矩阵
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
                GameMain.history.UnlockTech(1701);//电磁驱动
                GameMain.history.UnlockTech(1702);//磁悬浮技术
                GameMain.history.UnlockTech(1703);//粒子磁力阱
                GameMain.history.UnlockTech(1704);//引力波折射
                GameMain.history.UnlockTech(1705);//引力矩阵
                GameMain.history.UnlockTech(1711);//超级磁场发生器
                GameMain.history.UnlockTech(1712);//卫星配电系统

                //添加物品
                //GameMain.mainPlayer.TryAddItemToPackage(2104, 2, 0,false);//星际物流运输站
                //GameMain.mainPlayer.TryAddItemToPackage(2105, 1, 0,false);//轨道采集器

                //GameMain.mainPlayer.TryAddItemToPackage(5002, 2, 0,false);//星际物流运输船
                //GameMain.mainPlayer.TryAddItemToPackage(5001, 2, 0,false);//物流运输机
            }
            __result = "ok";
            return false;
        }

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
                    config_prop_ratio = config_prop_ratio_b.Value;
                }
                else if (productId == 6002)
                {
                    config_prop_ratio = config_prop_ratio_r.Value;
                }
                else if (productId == 6003)
                {
                    config_prop_ratio = config_prop_ratio_y.Value;
                }
                else if (productId == 6004)
                {
                    config_prop_ratio = config_prop_ratio_p.Value;
                }
                else if (productId == 6005)
                {
                    config_prop_ratio = config_prop_ratio_g.Value;
                }
                else if (productId == 6006)
                {
                    config_prop_ratio = config_prop_ratio_w.Value;
                }

                int count = (int)((double)num * config_prop_ratio * (double)__instance.gameData.gameDesc.propertyMultiplier / 60 + 0.001);
                if (count > itemProduction1)
                    propertyData.SetItemProduction(productId, count);
                if (count > itemProduction2)
                    clusterData.SetItemProduction(productId, count);
            }
            return false;
        }
    }
}
