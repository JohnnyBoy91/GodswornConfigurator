using System.IO;
using System.Text.Json;

//WIP due to IL2CPP problems

namespace JCGodSwornConfigurator
{
    internal class HandleWaveManager
    {
        //private Plugin.ModManager modManager => Plugin.ModManager.Instance;
        //public void Initialize(string mapName)
        //{
        //    WaveManagerBlueprint waveManagerBlueprint = new WaveManagerBlueprint();
        //    waveManagerBlueprint.mapKey = mapName;
        //    foreach (var waveOption in modManager.waveManager.WavesOptions)
        //    {
        //        WaveManagerBlueprint.WaveOptionsConfig waveOptionsConfig = new WaveManagerBlueprint.WaveOptionsConfig();
        //        waveOptionsConfig.unitCountMultiplier = waveOption.TimesAmount;
        //        waveOptionsConfig.waveName = waveOption.WaveName.Value;
        //        waveOptionsConfig.spawnTimeSeconds = waveOption.SpawnTime;
        //        bool haveRevealData = false;
        //        foreach (var wave in waveOption.Waves)
        //        {
        //            foreach (var waveEvent in wave.Events)
        //            {
        //                if (!haveRevealData)
        //                {
        //                    //waveOptionsConfig.revealInVision = waveEvent.RevealUnitsMinimap;
        //                    //waveOptionsConfig.revealOnMiniMap = waveEvent.RevealUnitsVision;
        //                    haveRevealData = true;
        //                }
        //                //foreach (var unitGroup in waveEvent.SpawnSets)
        //                //{
        //                //    WaveManagerBlueprint.WaveEventConfig waveEventConfig = new WaveManagerBlueprint.WaveEventConfig();
        //                //    waveEventConfig.difficulty = unitGroup.Difficulty;
        //                //    WaveManagerBlueprint.SpawnUnitSetConfig spawnUnitSetConfig = new WaveManagerBlueprint.SpawnUnitSetConfig();
        //                //    foreach (var unit in unitGroup.Units)
        //                //    {
        //                //        spawnUnitSetConfig.units.Add(unit.name);
        //                //    }
        //                //    waveEventConfig.spawnUnitSets.Add(spawnUnitSetConfig);
        //                //    waveOptionsConfig.waves.Add(waveEventConfig);
        //                //}
        //                for (int i = 0; i < 4; i++)
        //                {
        //                    WaveManagerBlueprint.WaveEventConfig waveEventConfig = new WaveManagerBlueprint.WaveEventConfig();
        //                    waveEventConfig.difficulty = (Difficulty)i;
        //                    WaveManagerBlueprint.SpawnUnitSetConfig spawnUnitSetConfig = new WaveManagerBlueprint.SpawnUnitSetConfig();
        //                    for (int j = 0; j < 6; j++)
        //                    {
        //                        spawnUnitSetConfig.units.Add("Skybull");
        //                    }
        //                    waveEventConfig.spawnUnitSets.Add(spawnUnitSetConfig);
        //                    waveOptionsConfig.waves.Add(waveEventConfig);
        //                }
        //            }
        //        }
        //        waveManagerBlueprint.waveOptions.Add(waveOptionsConfig);
        //    }

        //    //write defaults
        //    //var options = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };
        //    //string jsonString = JsonSerializer.Serialize(waveManagerBlueprint, options);
        //    //Utilities.WriteConfig(modManager.modRootPath + Plugin.ModManager.generatedConfigFolderPath + "WaveManagerConfig.json", jsonString);
        //}
    }
}
