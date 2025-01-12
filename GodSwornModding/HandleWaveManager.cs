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

        private static WaveManagerBlueprint currentWaveManagerBlueprint;
        private static string currentMapName;
        private static WaveManagers waveManager;

        public static class TreidenData
        {
            public static bool init = false;
            public static int playerID = 0;
            public static int playerTeam = 0;
        }

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
                    Plugin.ModManager.Log(__instance.name + ": " + __instance.RandomizedReinforcements[0].transform.position.ToString());
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
                Plugin.ModManager.Log("WaveEventInjected " + __instance.WaveMgr.gameObject.name);
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
                    if (__instance.WaveMgr.gameObject.name.Contains("Baltic A"))
                    {
                        //activeWave = true;
                        unitName = "Werewolf";
                    }
                    if (__instance.WaveMgr.gameObject.name.Contains("Baltic B"))
                    {
                        unitName = "Tribesman";
                    }
                    if (__instance.WaveMgr.gameObject.name.Contains("Baltic C"))
                    {
                        unitName = "Witch";
                    }
                    if (__instance.WaveMgr.gameObject.name.Contains("Baltic D"))
                    {
                        activeWave = true;
                        //unitName = "Marauder";
                    }
                    if (__instance.WaveMgr.gameObject.name.Contains("Baltic E"))
                    {
                        unitName = "Leshi";
                    }
                    if (__instance.WaveMgr.gameObject.name.Contains("Baltic Support"))
                    {
                        unitName = "Spigana";
                    }

                    if (__instance.WaveMgr.gameObject.name.Contains("Order A"))
                    {
                        //activeWave = true;
                        unitName = "Militant";
                    }
                    if (__instance.WaveMgr.gameObject.name.Contains("Order B"))
                    {
                        unitName = "Footman";
                    }
                    if (__instance.WaveMgr.gameObject.name.Contains("Order C"))
                    {
                        unitName = "Marksman";
                    }
                    if (__instance.WaveMgr.gameObject.name.Contains("Order D"))
                    {
                        activeWave = true;
                        unitName = "Militant";
                    }
                    if (__instance.WaveMgr.gameObject.name.Contains("Order E"))
                    {
                        unitName = "Tracker";
                    }
                    if (__instance.WaveMgr.gameObject.name.Contains("Order Support"))
                    {
                        unitName = "Knight";
                    }

                    foreach (var spawnSet in __instance.SpawnSets)
                    {
                        spawnSet.Units.Clear();
                        int unitQuantity = __instance.ParticipantID == 6 ? 20 : 15;
                        float variance = 0f;
                        unitQuantity = new System.Random().Next((int)(unitQuantity - (variance * unitQuantity)), (int)(unitQuantity + (variance * unitQuantity)));
                        
                        if (__instance.ParticipantID == 6 && activeWave)
                        {
                            foreach (var item in modManager.treidenCommanderModData.commanderDatas[0].unitBuildDatas)
                            {
                                Plugin.ModManager.Log(item.name + ": " + item.quantityOwned);
                                if (item.quantityOwned > 0)
                                {
                                    for (int i = 0; i < item.quantityOwned; i++)
                                    {
                                        Plugin.ModManager.Log(item.name);
                                        spawnSet.Units.Add(Plugin.ModManager.Instance.unitDataList.Where(x => x.name == item.name).First());
                                    }
                                }
                            }
                        }
                        else if (__instance.ParticipantID == 7 && activeWave)
                        {
                            if (!activeWave) unitQuantity = 0;
                            for (int i = 0; i < unitQuantity; i++)
                            {
                                spawnSet.Units.Add(Plugin.ModManager.Instance.unitDataList.Where(x => x.name == unitName).First());
                            }
                        }

                        if (!activeWave)
                        {
                        }
                        //for (int i = 0; i < unitQuantity; i++)
                        //{
                        //    spawnSet.Units.Add(Plugin.ModManager.Instance.unitDataList.Where(x => x.name == unitName).First());
                        //}
                    }
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
                            participant.Wealth.amount = 3000;
                            participant.Wealth.Increase = 20;
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
                for (int i = 0; i < waveManager.WavesOptions.Count; i++)
                {
                    waveManager.WavesOptions[i].SpawnTime = 30;
                }
                if (waveManager.WavesRepeats.Count > 1)
                {
                    waveManager.WavesRepeats.RemoveRange(1, waveManager.WavesRepeats.Count - 1);
                }
                for (int i = 0; i < waveManager.WavesRepeats.Count; i++)
                {
                    waveManager.WavesRepeats[i].SpawnTime = 30;
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
