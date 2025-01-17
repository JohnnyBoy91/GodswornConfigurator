using HarmonyLib;
using System.IO;
using System.Reflection;
using System.Text.Json;
using UnityEngine;
using System.Linq;
using System;
using Il2CppSystem;

namespace JCGodSwornConfigurator
{
    internal class HandleWaveManager
    {
        private static Plugin.ModManager modManager => Plugin.ModManager.Instance;
        //tervete
        private static WaveManagerBlueprint currentWaveManagerBlueprint;
        private static string currentMapName;
        //used for init only
        private static WaveManagers waveManager;

        public static class TreidenData
        {
            public static bool init = false;
            public static int playerID = 0;
            public static int playerTeam = 0;
            public static int waveInterval = 38;
            public static int wavesSpawned = 0;
            public static float lastWaveSpawnTime;
            public static int spawnPairCheck = 0;

            public static void ResetData()
            {
                init = false;
                playerID = 0;
                playerTeam = 0;
                waveInterval = 38;
                wavesSpawned = 0;
                lastWaveSpawnTime = 0;
                spawnPairCheck = 0;

                pickedBuildEarly = false;
                pickedBuildMid = false;
                pickedBuildMidLate = false;
                pickedBuildLate = false;
            }
        }

        #region Harmony Patches
        [HarmonyPatch(typeof(WaveManagers), "Start")]
        static class ModWaveManager
        {
            [HarmonyPriority(100)]
            private static void Postfix(WaveManagers __instance)
            {
                Plugin.ModManager.Log("WaveManagerInjected");
                string mapName = Plugin.ModManager.Instance.dataManager.GetCurrentMap().MapName.key;
                if (mapName == "$Tervete" || mapName == "$GaurdiansOfTreiden")
                {
                    Plugin.ModManager.Log(mapName);
                    //if (mapName == "$GaurdiansOfTreiden") Plugin.ModManager.Log(__instance.name + ": " + __instance.RandomizedReinforcements[0].transform.position.ToString());
                    waveManager = __instance;
                    Initialize(mapName);
                }
            }
        }

        [HarmonyPatch(typeof(WaveEvent), "Init")]
        static class ModWaveEvent
        {
            [HarmonyPriority(100)]
            private static void Prefix(WaveEvent __instance)
            {
                Plugin.ModManager.Log("WaveEventInjected");
                if (currentMapName == "$Tervete")
                {
                    //if(waveManager.currentwave == 1) __instance.ParticipantID = 0;
                    __instance.RevealUnitsMinimap = currentWaveManagerBlueprint.revealUnitsOnMiniMap;
                    foreach (var spawnSet in __instance.SpawnSets)
                    {
                        spawnSet.Units.Clear();
                        foreach (var waveUnitConfig in currentWaveManagerBlueprint.waveConfigs[waveManager.currentwave - 1].spawnUnitGroups)
                        {
                            int quantity = waveUnitConfig.quantity;
                            switch (spawnSet.Difficulty)
                            {
                                case Difficulty.Easy:
                                    quantity = (int)(quantity * currentWaveManagerBlueprint.easyUnitMultiplier);
                                    break;
                                case Difficulty.Normal:
                                    break;
                                case Difficulty.Hard:
                                    quantity = (int)(quantity * currentWaveManagerBlueprint.hardUnitMultiplier);
                                    break;
                                case Difficulty.Insane:
                                    quantity = (int)(quantity * currentWaveManagerBlueprint.insaneUnitMultiplier);
                                    break;
                                default:
                                    break;
                            }

                            float variance = Mathf.Clamp(waveUnitConfig.quantityVarianceFactor, 0, 1f);
                            quantity = new System.Random().Next((int)(quantity - (variance * quantity)), (int)(quantity + (variance * quantity)));

                            for (int i = 0; i < quantity; i++)
                            {
                                spawnSet.Units.Add(Plugin.ModManager.Instance.unitDataList.Where(x => x.name == waveUnitConfig.unitNameKey).First());
                            }
                        }
                    }
                }
                if (currentMapName == "$GaurdiansOfTreiden")
                {
                    int balticArmyID = 6;
                    int orderArmyID = 7;

                    //use baltic 1 & 4
                    //use order 1 & 4

                    bool activeWave = false;
                    string unitName = "Cow";
                    //if (__instance.WaveMgr.gameObject.name.Contains("Baltic A"))
                    //{
                    //    unitName = "Werewolf";
                    //}
                    //if (__instance.WaveMgr.gameObject.name.Contains("Baltic B"))
                    //{
                    //    unitName = "Tribesman";
                    //}
                    //if (__instance.WaveMgr.gameObject.name.Contains("Baltic C"))
                    //{
                    //    unitName = "Witch";
                    //}
                    if (__instance.WaveMgr.gameObject.name.Contains("Baltic D"))
                    {
                        activeWave = true;
                        //unitName = "Marauder";
                    }
                    //if (__instance.WaveMgr.gameObject.name.Contains("Baltic E"))
                    //{
                    //    unitName = "Leshi";
                    //}
                    //if (__instance.WaveMgr.gameObject.name.Contains("Baltic Support"))
                    //{
                    //    unitName = "Spigana";
                    //}

                    //if (__instance.WaveMgr.gameObject.name.Contains("Order A"))
                    //{
                    //    unitName = "Militant";
                    //}
                    //if (__instance.WaveMgr.gameObject.name.Contains("Order B"))
                    //{
                    //    unitName = "Footman";
                    //}
                    //if (__instance.WaveMgr.gameObject.name.Contains("Order C"))
                    //{
                    //    unitName = "Marksman";
                    //}
                    if (__instance.WaveMgr.gameObject.name.Contains("Order D"))
                    {
                        activeWave = true;
                        //unitName = "Militant";
                    }
                    //if (__instance.WaveMgr.gameObject.name.Contains("Order E"))
                    //{
                    //    unitName = "Tracker";
                    //}
                    //if (__instance.WaveMgr.gameObject.name.Contains("Order Support"))
                    //{
                    //    unitName = "Knight";
                    //}

                    //for ui
                    if(__instance.ParticipantID == 6 && activeWave && TreidenData.playerTeam == 1)
                    {
                        modManager.treidenCommanderModData.playerWaveManager = waveManager;
                    }
                    else if(__instance.ParticipantID == 7 && activeWave && TreidenData.playerTeam == 2)
                    {
                        modManager.treidenCommanderModData.playerWaveManager = waveManager;
                    }

                    //hacky janky fix to allow dynamic wave intervals
                    __instance.WaveMgr.currentwave = 0;
                    for (int i = 0; i < __instance.WaveMgr.WavesOptions.Count; i++)
                    {
                        __instance.WaveMgr.WavesOptions[i].SpawnTime = __instance.WaveMgr.TimeSinceStart + TreidenData.waveInterval;
                        if (activeWave) Plugin.ModManager.Log("SpawnTime " + __instance.WaveMgr.gameObject.name + " " + __instance.WaveMgr.WavesOptions[i].SpawnTime);
                    }
                    for (int i = 0; i < __instance.WaveMgr.WavesRepeats.Count; i++)
                    {
                        __instance.WaveMgr.WavesRepeats[i].SpawnTime = __instance.WaveMgr.TimeSinceStart + TreidenData.waveInterval;
                    }

                    if ((__instance.ParticipantID == 6 || __instance.ParticipantID == 7) && activeWave)
                    {
                        //handle race condition
                        TreidenData.spawnPairCheck++;
                        if (TreidenData.spawnPairCheck == 2)
                        {
                            TreidenData.wavesSpawned++;
                            TreidenData.waveInterval += 1;
                            TreidenData.lastWaveSpawnTime = __instance.WaveMgr.TimeSinceStart;
                            TreidenData.spawnPairCheck = 0;
                        }
                    }

                    foreach (var spawnSet in __instance.SpawnSets)
                    {
                        spawnSet.Units.Clear();

                        if (__instance.ParticipantID == 6 && activeWave)
                        {
                            if (TreidenData.playerTeam != 1) ProcessTreidenCommanderAIIncome();
                            foreach (var item in modManager.treidenCommanderModData.commanderDatas[TreidenData.playerTeam == 1 ? 0 : 1].unitBuildDatas)
                            {
                                if (item.quantityOwned > 0)
                                {
                                    for (int i = 0; i < item.quantityOwned; i++)
                                    {
                                        if (item.name == "Cherub")
                                        {
                                            spawnSet.Units.Add(Plugin.ModManager.Instance.unitDataList.Where(x => x.name == "Cherub - Eagle").First());
                                            spawnSet.Units.Add(Plugin.ModManager.Instance.unitDataList.Where(x => x.name == "Cherub - Lion").First());
                                            spawnSet.Units.Add(Plugin.ModManager.Instance.unitDataList.Where(x => x.name == "Cherub - Ox").First());
                                        }
                                        else
                                        {
                                            spawnSet.Units.Add(Plugin.ModManager.Instance.unitDataList.Where(x => x.name == item.name).First());
                                        }
                                    }
                                }
                            }
                        }
                        else if (__instance.ParticipantID == 7 && activeWave)
                        {
                            if (TreidenData.playerTeam != 2) ProcessTreidenCommanderAIIncome();
                            foreach (var item in modManager.treidenCommanderModData.commanderDatas[TreidenData.playerTeam == 2 ? 0 : 1].unitBuildDatas)
                            {
                                if (item.quantityOwned > 0)
                                {
                                    for (int i = 0; i < item.quantityOwned; i++)
                                    {
                                        if (item.name == "Cherub")
                                        {
                                            spawnSet.Units.Add(Plugin.ModManager.Instance.unitDataList.Where(x => x.name == "Cherub - Eagle").First());
                                            spawnSet.Units.Add(Plugin.ModManager.Instance.unitDataList.Where(x => x.name == "Cherub - Lion").First());
                                            spawnSet.Units.Add(Plugin.ModManager.Instance.unitDataList.Where(x => x.name == "Cherub - Ox").First());
                                        }
                                        else
                                        {
                                            spawnSet.Units.Add(Plugin.ModManager.Instance.unitDataList.Where(x => x.name == item.name).First());
                                        }
                                    }
                                }
                            }
                        }

                        //if (!activeWave)
                        //{
                        //}
                        //for (int i = 0; i < unitQuantity; i++)
                        //{
                        //    spawnSet.Units.Add(Plugin.ModManager.Instance.unitDataList.Where(x => x.name == unitName).First());
                        //}
                    }
                }
            }
        }
        #endregion

        private static bool pickedBuildEarly;
        private static bool pickedBuildMid;
        private static bool pickedBuildMidLate;
        private static bool pickedBuildLate;
        private static void ProcessTreidenCommanderAI(TreidenCommanderModData.CommanderData aiCommander, int currentWave)
        {
            if (TreidenData.playerTeam == 1)
            {
                ProcessTreidenOrderAI(aiCommander, currentWave);
            }
            else if (TreidenData.playerTeam == 2)
            {
                ProcessTreidenBalticAI(aiCommander, currentWave);
            }
            
        }

        private static void ProcessTreidenCommanderAIIncome()
        {
            TreidenCommanderModData.CommanderData AICommander = modManager.treidenCommanderModData.commanderDatas[1];
            AICommander.currentGoldAI += AICommander.goldIncome * (TreidenData.waveInterval / 5);
            Plugin.ModManager.Log("AIGoldBank:" + AICommander.currentGoldAI + "AI GoldIncomeTick:" + AICommander.goldIncome + ", " + "WaveIncome:" + AICommander.goldIncome * (TreidenData.waveInterval / 5));
            int currentWave = TreidenData.wavesSpawned;

            if (currentWave % 5 == 0)
            {
                Plugin.ModManager.Log(currentWave + ":" + "5th wave, upgrading AI income");
                AICommander.goldIncome += 2;
            }

            if (AICommander.currentGoldAI > currentWave * 20)
            {
                ProcessTreidenCommanderAI(AICommander, currentWave);
            }
            if (AICommander.currentGoldAI > currentWave * 10)
            {
                ProcessTreidenCommanderAI(AICommander, currentWave);
            }
        }


        private static void ProcessTreidenOrderAI(TreidenCommanderModData.CommanderData aiCommander, int currentWave)
        {
            System.Random rng = new System.Random();
            if (currentWave >= 0 && !pickedBuildEarly)
            {
                pickedBuildEarly = true;
                int randomInt = rng.Next(System.Enum.GetValues(typeof(TreidenCommanderModData.treidenBuildOrderEarly)).Length);
                aiCommander.aiBuildEarly = (TreidenCommanderModData.treidenBuildOrderEarly)randomInt;
                Plugin.ModManager.Log(aiCommander.aiBuildEarly.ToString(), 2);
                switch (aiCommander.aiBuildEarly)
                {
                    case TreidenCommanderModData.treidenBuildOrderEarly.Footmen_Xbow:
                        aiCommander.aiUnitWishList.Where(x => x.name == "Militant").First().quantityOwned += 2;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Footman").First().quantityOwned += 6;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Marksman").First().quantityOwned += 8;
                        break;
                    case TreidenCommanderModData.treidenBuildOrderEarly.Militant_Cherub:
                        aiCommander.aiUnitWishList.Where(x => x.name == "Militant").First().quantityOwned += 12;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Footman").First().quantityOwned += 1;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Cherub").First().quantityOwned += 4;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Marksman").First().quantityOwned += 1;
                        break;
                    case TreidenCommanderModData.treidenBuildOrderEarly.Footmen_Cherub:
                        aiCommander.aiUnitWishList.Where(x => x.name == "Militant").First().quantityOwned += 2;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Footman").First().quantityOwned += 6;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Cherub").First().quantityOwned += 4;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Marksman").First().quantityOwned += 2;
                        break;
                    case TreidenCommanderModData.treidenBuildOrderEarly.Rogue_Bow:
                        TryUpgradeAITechLevel(2, aiCommander);
                        aiCommander.aiUnitWishList.Where(x => x.name == "Rogue").First().quantityOwned += 10;
                        aiCommander.aiUnitWishList.Where(x => x.name == "LongbowMan").First().quantityOwned += 4;
                        break;
                    case TreidenCommanderModData.treidenBuildOrderEarly.Tracker_Cherub:
                        TryUpgradeAITechLevel(2, aiCommander);
                        aiCommander.aiUnitWishList.Where(x => x.name == "Tracker").First().quantityOwned += 8;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Rogue").First().quantityOwned += 2;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Cherub").First().quantityOwned += 3;
                        break;
                    default:
                        break;
                }
            }

            if (currentWave >= 10 && !pickedBuildMid)
            {
                pickedBuildMid = true;
                int randomInt = rng.Next(System.Enum.GetValues(typeof(TreidenCommanderModData.treidenBuildOrderMid)).Length);
                aiCommander.aiBuildMid = (TreidenCommanderModData.treidenBuildOrderMid)randomInt;
                Plugin.ModManager.Log(aiCommander.aiBuildMid.ToString(), 2);
                switch (aiCommander.aiBuildMid)
                {
                    case TreidenCommanderModData.treidenBuildOrderMid.Longbows:
                        TryUpgradeAITechLevel(2, aiCommander);
                        aiCommander.aiUnitWishList.Where(x => x.name == "LongbowMan").First().quantityOwned += 6;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Footman").First().quantityOwned += 4;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Zealot").First().quantityOwned += 2;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Nurse").First().quantityOwned += 1;
                        break;
                    case TreidenCommanderModData.treidenBuildOrderMid.Zealot_Nurse:
                        TryUpgradeAITechLevel(2, aiCommander);
                        aiCommander.aiUnitWishList.Where(x => x.name == "Militant").First().quantityOwned += 4;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Zealot").First().quantityOwned += 8;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Nurse").First().quantityOwned += 2;
                        break;
                    case TreidenCommanderModData.treidenBuildOrderMid.Tracker_Rogue:
                        TryUpgradeAITechLevel(2, aiCommander);
                        aiCommander.aiUnitWishList.Where(x => x.name == "Tracker").First().quantityOwned += 6;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Rogue").First().quantityOwned += 6;
                        aiCommander.aiUnitWishList.Where(x => x.name == "LongbowMan").First().quantityOwned += 1;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Nurse").First().quantityOwned += 1;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Zealot").First().quantityOwned += 2;
                        break;
                    case TreidenCommanderModData.treidenBuildOrderMid.HolyRush:
                        TryUpgradeAITechLevel(3, aiCommander);
                        aiCommander.aiUnitWishList.Where(x => x.name == "Avenging Angel").First().quantityOwned += 3;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Cherub").First().quantityOwned += 2;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Nurse").First().quantityOwned += 1;
                        break;
                    default:
                        break;
                }
            }

            if (currentWave >= 18 && !pickedBuildMidLate)
            {
                pickedBuildMidLate = true;
                int randomInt = rng.Next(System.Enum.GetValues(typeof(TreidenCommanderModData.treidenBuildOrderMidLate)).Length);
                aiCommander.aiBuildMidLate = (TreidenCommanderModData.treidenBuildOrderMidLate)randomInt;
                Plugin.ModManager.Log(aiCommander.aiBuildMidLate.ToString(), 2);
                switch (aiCommander.aiBuildMidLate)
                {
                    case TreidenCommanderModData.treidenBuildOrderMidLate.Artillery:
                        TryUpgradeAITechLevel(3, aiCommander);
                        aiCommander.aiUnitWishList.Where(x => x.name == "LongbowMan").First().quantityOwned += 2;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Cannon").First().quantityOwned += 2;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Catapult").First().quantityOwned += 2;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Nurse").First().quantityOwned += 1;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Footman").First().quantityOwned += 2;
                        break;
                    case TreidenCommanderModData.treidenBuildOrderMidLate.Angels:
                        TryUpgradeAITechLevel(3, aiCommander);
                        aiCommander.aiUnitWishList.Where(x => x.name == "Avenging Angel").First().quantityOwned += 6;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Cherub").First().quantityOwned += 3;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Nurse").First().quantityOwned += 2;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Zealot").First().quantityOwned += 2;
                        break;
                    case TreidenCommanderModData.treidenBuildOrderMidLate.Knights:
                        TryUpgradeAITechLevel(3, aiCommander);
                        aiCommander.aiUnitWishList.Where(x => x.name == "Knight").First().quantityOwned += 10;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Nurse").First().quantityOwned += 2;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Zealot").First().quantityOwned += 4;
                        break;
                    default:
                        break;
                }
            }

            if (currentWave >= 26 && !pickedBuildLate)
            {
                pickedBuildLate = true;
                int randomInt = rng.Next(System.Enum.GetValues(typeof(TreidenCommanderModData.treidenBuildOrderLate)).Length);
                aiCommander.aiBuildLate = (TreidenCommanderModData.treidenBuildOrderLate)randomInt;
                Plugin.ModManager.Log(aiCommander.aiBuildLate.ToString(), 2);
                switch (aiCommander.aiBuildLate)
                {
                    case TreidenCommanderModData.treidenBuildOrderLate.BlackKnight:
                        TryUpgradeAITechLevel(4, aiCommander);
                        aiCommander.aiUnitWishList.Where(x => x.name == "Blackknight").First().quantityOwned += 8;
                        break;
                    case TreidenCommanderModData.treidenBuildOrderLate.Paladin:
                        TryUpgradeAITechLevel(4, aiCommander);
                        aiCommander.aiUnitWishList.Where(x => x.name == "Paladin").First().quantityOwned += 10;
                        break;
                    default:
                        break;
                }
            }

            if (rng.Next(100) < 30) return;
            for (int i = 0; i < aiCommander.unitBuildDatas.Count; i++)
            {
                Plugin.ModManager.Log("AIWants " + aiCommander.aiUnitWishList[i].quantityOwned + aiCommander.unitBuildDatas[i].name);
                if (aiCommander.aiUnitWishList[i].quantityOwned > aiCommander.unitBuildDatas[i].quantityOwned)
                {
                    if (aiCommander.currentGoldAI > aiCommander.unitBuildDatas[i].goldCost)
                    {
                        Plugin.ModManager.Log("AIBought " + aiCommander.unitBuildDatas[i].name);
                        aiCommander.unitBuildDatas[i].quantityOwned++;
                        aiCommander.currentGoldAI -= aiCommander.unitBuildDatas[i].goldCost;
                    }
                }
            }
        }

        private static void ProcessTreidenBalticAI(TreidenCommanderModData.CommanderData aiCommander, int currentWave)
        {
            System.Random rng = new System.Random();
            if (currentWave >= 0 && !pickedBuildEarly)
            {
                pickedBuildEarly = true;
                int randomInt = rng.Next(System.Enum.GetValues(typeof(TreidenCommanderModData.treidenBuildBalticEarly)).Length);
                aiCommander.aiBuildBalticEarly = (TreidenCommanderModData.treidenBuildBalticEarly)randomInt;
                Plugin.ModManager.Log(aiCommander.aiBuildBalticEarly.ToString(), 2);
                switch (aiCommander.aiBuildBalticEarly)
                {
                    case TreidenCommanderModData.treidenBuildBalticEarly.MassTribe:
                        aiCommander.aiUnitWishList.Where(x => x.name == "Tribesman").First().quantityOwned += 12;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Skirmisher").First().quantityOwned += 2;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Marauder").First().quantityOwned += 6;
                        break;
                    case TreidenCommanderModData.treidenBuildBalticEarly.Tribe_Rauder:
                        aiCommander.aiUnitWishList.Where(x => x.name == "Tribesman").First().quantityOwned += 8;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Marauder").First().quantityOwned += 8;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Skirmisher").First().quantityOwned += 2;
                        break;
                    case TreidenCommanderModData.treidenBuildBalticEarly.Wolves:
                        aiCommander.aiUnitWishList.Where(x => x.name == "Werewolf").First().quantityOwned += 12;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Marauder").First().quantityOwned += 12;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Skirmisher").First().quantityOwned += 2;
                        break;
                    case TreidenCommanderModData.treidenBuildBalticEarly.Mix:
                        aiCommander.aiUnitWishList.Where(x => x.name == "Tribesman").First().quantityOwned += 6;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Marauder").First().quantityOwned += 6;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Skirmisher").First().quantityOwned += 6;
                        break;
                    case TreidenCommanderModData.treidenBuildBalticEarly.PukiRush:
                        TryUpgradeAITechLevel(2, aiCommander);
                        aiCommander.aiUnitWishList.Where(x => x.name == "Pukis").First().quantityOwned += 6;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Herbalist").First().quantityOwned += 2;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Skirmisher").First().quantityOwned += 2;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Tribesman").First().quantityOwned += 2;
                        break;
                    default:
                        break;
                }
            }

            if (currentWave >= 10 && !pickedBuildMid)
            {
                pickedBuildMid = true;
                int randomInt = rng.Next(System.Enum.GetValues(typeof(TreidenCommanderModData.treidenBuildBalticMid)).Length);
                aiCommander.aiBuildBalticMid = (TreidenCommanderModData.treidenBuildBalticMid)randomInt;
                Plugin.ModManager.Log(aiCommander.aiBuildBalticMid.ToString(), 2);
                switch (aiCommander.aiBuildBalticMid)
                {
                    case TreidenCommanderModData.treidenBuildBalticMid.Puki_Tribe:
                        TryUpgradeAITechLevel(2, aiCommander);
                        aiCommander.aiUnitWishList.Where(x => x.name == "Pukis").First().quantityOwned += 6;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Tribesman").First().quantityOwned += 4;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Herbalist").First().quantityOwned += 3;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Witch").First().quantityOwned += 2;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Ranger").First().quantityOwned += 2;
                        break;
                    case TreidenCommanderModData.treidenBuildBalticMid.Witches:
                        TryUpgradeAITechLevel(2, aiCommander);
                        aiCommander.aiUnitWishList.Where(x => x.name == "Pukis").First().quantityOwned += 1;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Witch").First().quantityOwned += 8;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Herbalist").First().quantityOwned += 2;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Ranger").First().quantityOwned += 2;
                        break;
                    case TreidenCommanderModData.treidenBuildBalticMid.RangedMix:
                        TryUpgradeAITechLevel(2, aiCommander);
                        aiCommander.aiUnitWishList.Where(x => x.name == "Pukis").First().quantityOwned += 1;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Herbalist").First().quantityOwned += 2;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Witch").First().quantityOwned += 4;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Ranger").First().quantityOwned += 4;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Skirmisher").First().quantityOwned += 2;
                        break;
                    //case TreidenCommanderModData.treidenBuildBalticMid.HolyRush:
                    //    TryUpgradeAITechLevel(3, aiCommander);
                    //    aiCommander.aiUnitWishList.Where(x => x.name == "Avenging Angel").First().quantityOwned += 3;
                    //    aiCommander.aiUnitWishList.Where(x => x.name == "Cherub").First().quantityOwned += 2;
                    //    aiCommander.aiUnitWishList.Where(x => x.name == "Nurse").First().quantityOwned += 1;
                    //    break;
                    default:
                        break;
                }
            }

            if (currentWave >= 18 && !pickedBuildMidLate)
            {
                pickedBuildMidLate = true;
                int randomInt = rng.Next(System.Enum.GetValues(typeof(TreidenCommanderModData.treidenBuildBalticMidLate)).Length);
                aiCommander.aiBuildBalticMidLate = (TreidenCommanderModData.treidenBuildBalticMidLate)randomInt;
                Plugin.ModManager.Log(aiCommander.aiBuildBalticMidLate.ToString(), 2);
                switch (aiCommander.aiBuildBalticMidLate)
                {
                    case TreidenCommanderModData.treidenBuildBalticMidLate.Raiders:
                        TryUpgradeAITechLevel(3, aiCommander);
                        aiCommander.aiUnitWishList.Where(x => x.name == "Raider").First().quantityOwned += 6;
                        aiCommander.aiUnitWishList.Where(x => x.name == "WolfWarrior").First().quantityOwned += 4;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Leshi").First().quantityOwned += 2;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Warrior").First().quantityOwned += 2;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Spigana").First().quantityOwned += 1;
                        break;
                    case TreidenCommanderModData.treidenBuildBalticMidLate.Leshis:
                        TryUpgradeAITechLevel(3, aiCommander);
                        aiCommander.aiUnitWishList.Where(x => x.name == "Leshi").First().quantityOwned += 3;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Spigana").First().quantityOwned += 2;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Herbalist").First().quantityOwned += 2;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Witch").First().quantityOwned += 2;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Ranger").First().quantityOwned += 2;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Warrior").First().quantityOwned += 2;
                        break;
                    case TreidenCommanderModData.treidenBuildBalticMidLate.Spiganas:
                        TryUpgradeAITechLevel(3, aiCommander);
                        aiCommander.aiUnitWishList.Where(x => x.name == "Leshi").First().quantityOwned += 1;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Spigana").First().quantityOwned += 5;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Herbalist").First().quantityOwned += 1;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Witch").First().quantityOwned += 2;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Tribesman").First().quantityOwned += 1;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Ranger").First().quantityOwned += 2;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Warrior").First().quantityOwned += 2;
                        break;
                    case TreidenCommanderModData.treidenBuildBalticMidLate.WolfWarriors:
                        TryUpgradeAITechLevel(3, aiCommander);
                        aiCommander.aiUnitWishList.Where(x => x.name == "WolfWarrior").First().quantityOwned += 8;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Warrior").First().quantityOwned += 2;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Herbalist").First().quantityOwned += 2;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Ranger").First().quantityOwned += 4;
                        break;
                    default:
                        break;
                }
            }

            if (currentWave >= 26 && !pickedBuildLate)
            {
                pickedBuildLate = true;
                int randomInt = rng.Next(System.Enum.GetValues(typeof(TreidenCommanderModData.treidenBuildBalticLate)).Length);
                aiCommander.aiBuildBalticLate = (TreidenCommanderModData.treidenBuildBalticLate)randomInt;
                Plugin.ModManager.Log(aiCommander.aiBuildBalticLate.ToString(), 2);
                switch (aiCommander.aiBuildBalticLate)
                {
                    case TreidenCommanderModData.treidenBuildBalticLate.Bulls:
                        TryUpgradeAITechLevel(4, aiCommander);
                        aiCommander.aiUnitWishList.Where(x => x.name == "Skybull").First().quantityOwned += 10;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Stardaughter - Lunar").First().quantityOwned += 3;
                        break;
                    case TreidenCommanderModData.treidenBuildBalticLate.Lunardaughters:
                        TryUpgradeAITechLevel(4, aiCommander);
                        aiCommander.aiUnitWishList.Where(x => x.name == "Stardaughter - Lunar").First().quantityOwned += 8;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Stardaughter - Solar").First().quantityOwned += 4;
                        break;
                    case TreidenCommanderModData.treidenBuildBalticLate.Solardaughters:
                        TryUpgradeAITechLevel(4, aiCommander);
                        aiCommander.aiUnitWishList.Where(x => x.name == "Stardaughter - Solar").First().quantityOwned += 8;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Stardaughter - Lunar").First().quantityOwned += 4;
                        break;
                    case TreidenCommanderModData.treidenBuildBalticLate.T3Mix:
                        TryUpgradeAITechLevel(4, aiCommander);
                        aiCommander.aiUnitWishList.Where(x => x.name == "Stardaughter - Lunar").First().quantityOwned += 4;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Stardaughter - Solar").First().quantityOwned += 4;
                        aiCommander.aiUnitWishList.Where(x => x.name == "Skybull").First().quantityOwned += 4;
                        break;
                    default:
                        break;
                }
            }

            if (rng.Next(100) < 30) return;
            for (int i = 0; i < aiCommander.unitBuildDatas.Count; i++)
            {
                Plugin.ModManager.Log("AIWants " + aiCommander.aiUnitWishList[i].quantityOwned + aiCommander.unitBuildDatas[i].name);
                if (aiCommander.aiUnitWishList[i].quantityOwned > aiCommander.unitBuildDatas[i].quantityOwned)
                {
                    if (aiCommander.currentGoldAI > aiCommander.unitBuildDatas[i].goldCost)
                    {
                        Plugin.ModManager.Log("AIBought " + aiCommander.unitBuildDatas[i].name);
                        aiCommander.unitBuildDatas[i].quantityOwned++;
                        aiCommander.currentGoldAI -= aiCommander.unitBuildDatas[i].goldCost;
                    }
                }
            }
        }

        private static void TryUpgradeAITechLevel(int targetTechLevel, TreidenCommanderModData.CommanderData aiCommander)
        {
            if (aiCommander.techLevel < targetTechLevel)
            {
                aiCommander.currentGoldAI -= 150 + (100 * aiCommander.techLevel);
                aiCommander.techLevel++;
            }
            if (aiCommander.techLevel < targetTechLevel)
            {
                aiCommander.currentGoldAI -= 150 + (100 * aiCommander.techLevel);
                aiCommander.techLevel++;
            }
            if (aiCommander.techLevel < targetTechLevel)
            {
                aiCommander.currentGoldAI -= 150 + (100 * aiCommander.techLevel);
                aiCommander.techLevel++;
            }
        }

        private static int TreidenDifficultyScaledUnitQuantity(int quantity)
        {
            switch (DataManager.Difficulty)
            {
                case Difficulty.Easy:
                    return (int)(quantity * 0.8f);
                case Difficulty.Normal:
                    return quantity;
                case Difficulty.Hard:
                    return (int)(quantity * 1.15f);
                case Difficulty.Insane:
                    return (int)(quantity * 1.3f);
                default:
                    return quantity;
            }
        }

        [HarmonyPatch(typeof(Destructable), "StartSetup")]
        static class ModDestructableStart
        {
            [HarmonyPriority(100)]
            private static void Postfix(Destructable __instance)
            {
                if (Plugin.ModManager.Instance.treidenCommanderModeEnabled && Plugin.ModManager.Instance.dataManager.GetCurrentMap().MapName.key == "$GaurdiansOfTreiden" && !__instance.gameObject.name.Contains("Pole_Choice"))
                {
                    __instance.AddHealth(-100, __instance, 6);
                }
            }
        }

        [HarmonyPatch(typeof(ParticipantManager), "gameStart")]
        static class ModParticipantManager
        {
            [HarmonyPriority(100)]
            private static void Postfix(ParticipantManager __instance)
            {
                if (Plugin.ModManager.Instance.treidenCommanderModeEnabled && Plugin.ModManager.Instance.dataManager.GetCurrentMap().MapName.key == "$GaurdiansOfTreiden")
                {
                    if (modManager.gameManager == null) modManager.gameManager = __instance.GameMgr;
                    foreach (var participant in modManager.gameManager.ParticipantMgrs)
                    {
                        if (participant.PlayerControl)
                        {
                            Plugin.ModManager.Log("Found Player", 2);
                            TreidenData.playerID = participant.ParticipantID;
                            TreidenData.init = true;
                            modManager.treidenCommanderModData.commanderDatas[0].goldIncome = 20;
                            modManager.treidenCommanderModData.commanderDatas[0].faithIncome = 10;
                            participant.Wealth.amount = 500;
                            participant.Wealth.Increase = 20;
                        }
                        else if (participant.ParticipantID == 6 || participant.ParticipantID == 7)
                        {
                            participant.Food.amount = 100;
                            participant.Wood.amount = 100;
                            participant.Wealth.amount = 100;
                            participant.Faith.amount = 100;
                            participant.Food.Increase = 10;
                            participant.Wood.Increase = 10;
                            participant.Wealth.Increase = 10;
                            participant.Faith.Increase = 10;
                            participant.House.Cap = 50;
                            participant.House.Maximum = 50;
                        }
                    }
                }
            }
        }

        private static void Initialize(string mapName)
        {

            currentMapName = mapName;
            mapName = mapName.Replace("$", "");

            if (!File.Exists(Utilities.CombineStrings(modManager.modRootPath, mapName, "ScenarioConfig.json")))
            {
                Plugin.ModManager.Log(mapName + " Scenario File Not Found, move to mod root folder to enable");
                return;
            }

            if (mapName == "Tervete")
            {
                WaveManagerBlueprint waveManagerBlueprint = (WaveManagerBlueprint)Utilities.ReadJsonConfig<WaveManagerBlueprint>(Utilities.CombineStrings(modManager.modRootPath, mapName, "ScenarioConfig.json"));
                currentWaveManagerBlueprint = waveManagerBlueprint;

                waveManager.CurrentWaveTimeScaler = 1;

                for (int i = 0; i < waveManager.WavesOptions.Count; i++)
                {
                    string newKey = "$" + currentWaveManagerBlueprint.waveConfigs[i].waveName;
                    if (!LocalizationManager.localisedEN.ContainsKey(newKey))
                    {
                        LocalizationManager.localisedEN.Add(newKey, currentWaveManagerBlueprint.waveConfigs[i].waveName);
                    }
                    waveManager.WavesOptions[i].WaveName = newKey;

                    float spawnTime = currentWaveManagerBlueprint.waveConfigs[i].spawnTimeSeconds;
                    float variance = Mathf.Clamp(currentWaveManagerBlueprint.waveConfigs[i].spawnTimeVarianceFactor, 0, 0.5f);
                    spawnTime = new System.Random().Next((int)(spawnTime - (variance * spawnTime)), (int)(spawnTime + (variance * spawnTime)));
                    waveManager.WavesOptions[i].SpawnTime = spawnTime;
                }

                for (int i = 1; i < waveManager.WavesOptions.Count; i++)
                {
                    if (waveManager.WavesOptions[i].SpawnTime < waveManager.WavesOptions[i - 1].SpawnTime)
                    {
                        Plugin.ModManager.Log("Wave set to spawn before prior wave, attempting to correct. Please double check your config settings!", 2);
                        waveManager.WavesOptions[i].SpawnTime = waveManager.WavesOptions[i - 1].SpawnTime + 5;
                    }
                }
            }
            
            if (mapName == "GaurdiansOfTreiden")
            {
                int balticArmyID = 6;
                int orderArmyID = 7;

                //foreach (var participant in modManager.gameManager.ParticipantMgrs)
                //{
                //    if (participant.PlayerControl)
                //    {
                //        participant.Wealth.amount = 500;
                //        participant.Wealth.Increase = 10;
                //    }
                //}
                waveManager.CurrentWaveTimeScaler = 1;
                if (waveManager.WavesRepeats.Count > 1)
                {
                    waveManager.WavesRepeats.RemoveRange(1, waveManager.WavesRepeats.Count - 1);
                }
                for (int i = 0; i < waveManager.WavesOptions.Count; i++)
                {
                    waveManager.WavesOptions[i].SpawnTime = TreidenData.waveInterval;
                }
                for (int i = 0; i < waveManager.WavesRepeats.Count; i++)
                {
                    waveManager.WavesRepeats[i].SpawnTime = TreidenData.waveInterval;
                }
                if (waveManager.gameObject.name.Contains("Order D") || waveManager.gameObject.name.Contains("Baltic D"))
                {
                    waveManager.gameObject.GetComponent<Objective>()?.CompleteConsequences.Clear();
                }
            }

            if (modManager.RetrieveDataMode) WriteDefaults(mapName);
        }

        private static void WriteDefaults(string mapName)
        {
            WaveManagerBlueprint waveManagerBlueprint = new WaveManagerBlueprint();
            waveManagerBlueprint.mapKey = mapName;

            for (int i = 0; i < 10; i++)
            {
                WaveManagerBlueprint.WaveConfig waveConfig = new WaveManagerBlueprint.WaveConfig();
                waveConfig.waveName = Utilities.CombineStrings(mapName, (i + 1).ToString());
                waveConfig.spawnTimeSeconds = (i + 1) * 10;
                for (int j = 0; j < 2; j++)
                {
                    WaveManagerBlueprint.WaveUnitConfig waveUnitConfig = new WaveManagerBlueprint.WaveUnitConfig();
                    waveUnitConfig.quantity = i;
                    waveUnitConfig.unitNameKey = "Werewolf";
                    waveConfig.spawnUnitGroups.Add(waveUnitConfig);
                }
                waveManagerBlueprint.waveConfigs.Add(waveConfig);
            }

            Utilities.WriteJsonConfig(Utilities.CombineStrings(modManager.modRootPath, Plugin.ModManager.generatedConfigFolderPath, "TerveteScenarioConfig.json"), waveManagerBlueprint);
        }
    }
}
