using HarmonyLib;
using System.IO;
using System.Reflection;
using System.Text.Json;
using UnityEngine;
using System.Linq;

//WIP due to IL2CPP problems

namespace JCGodSwornConfigurator
{
    internal class HandleWaveManager
    {
        private static Plugin.ModManager modManager => Plugin.ModManager.Instance;

        private static WaveManagerBlueprint currentWaveManagerBlueprint;
        private static string currentMapName;
        private static WaveManagers waveManager;

        [HarmonyPatch(typeof(WaveManagers), "Start")]
        static class ModWaveManager
        {
            [HarmonyPriority(100)]
            private static void Postfix(WaveManagers __instance)
            {
                Plugin.ModManager.Log("WaveManagerInjected");
                string mapName = Plugin.ModManager.Instance.dataManager.GetCurrentMap().MapName.key;
                if (mapName == "$Tervete")
                {
                    waveManager = __instance;
                    Initialize(mapName);
                }
                //int k = 20;
                //foreach (var item in __instance.WavesOptions)
                //{
                //    item.SpawnTime = k;
                //    k += 20;
                //}
                //Log(Instance.waveManager.Waves.ToString() + Instance.waveManager.WavesOptions[0].WaveName.Value + ", " + Instance.waveManager.WavesOptions.Count);
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
                            for (int i = 0; i < quantity; i++)
                            {
                                spawnSet.Units.Add(Plugin.ModManager.Instance.unitDataList.Where(x => x.name == waveUnitConfig.unitNameKey).First());
                            }
                        }
                    }
                }
            }
        }

        private static void Initialize(string mapName)
        {
            currentMapName = mapName;
            mapName = mapName.Replace("$", "");
            WaveManagerBlueprint waveManagerBlueprint = (WaveManagerBlueprint)Utilities.ReadJsonConfig<WaveManagerBlueprint>(Utilities.CombineStrings(modManager.modRootPath, mapName, "ScenarioConfig.json"));
            currentWaveManagerBlueprint = waveManagerBlueprint;

            for (int i = 0; i < waveManager.WavesOptions.Count; i++)
            {
                Plugin.ModManager.Log(Utilities.CombineStrings(waveManager.WavesOptions[i].WaveName.key, currentWaveManagerBlueprint.waveConfigs[i].waveName));
                LocalizationManager.localisedEN[waveManager.WavesOptions[i].WaveName.key] = currentWaveManagerBlueprint.waveConfigs[i].waveName;
                waveManager.WavesOptions[i].SpawnTime = currentWaveManagerBlueprint.waveConfigs[i].spawnTimeSeconds;
                //waveManager.WavesOptions[i].WaveName = currentWaveManagerBlueprint.waveConfigs[i].waveName;
            }

            //WriteDefaults(mapName);
        }

        private static void WriteDefaults(string mapName)
        {
            WaveManagerBlueprint waveManagerBlueprint = new WaveManagerBlueprint();
            waveManagerBlueprint.mapKey = mapName;

            for (int i = 0; i < 10; i++)
            {
                WaveManagerBlueprint.WaveConfig waveConfig = new WaveManagerBlueprint.WaveConfig();
                waveConfig.waveName = Utilities.CombineStrings(mapName, i.ToString());
                waveConfig.spawnTimeSeconds = i * 10;
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
