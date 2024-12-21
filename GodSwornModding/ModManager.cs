using System;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using BepInEx.Configuration;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace JCGodSwornConfigurator
{
    [BepInPlugin("JCGodSwornConfigurator", "GodSwornConfigurator", "1.0.0")]
    public class Plugin : BasePlugin
    {
        public GameObject GodSwornMainModObject;
        public override void Load()
        {
            // Plugin startup logic
            Log.LogInfo("Plugin JCGodSwornConfigurator is loaded!");
            ClassInjector.RegisterTypeInIl2Cpp<ModManager>();
            if (GodSwornMainModObject == null)
            {
                GodSwornMainModObject = new GameObject("GodSwornConfiguratorMaster");
                GameObject.DontDestroyOnLoad(GodSwornMainModObject);
                GodSwornMainModObject.hideFlags = HideFlags.HideAndDontSave;
                GodSwornMainModObject.AddComponent<ModManager>();
            }
            else
            {
                GodSwornMainModObject.AddComponent<ModManager>();
            }
            GodSwornMainModObject.GetComponent<ModManager>().plugin = this;
        }

        public class ModManager : MonoBehaviour
        {
            public string configPath;
            public string modRootPath;
            public string[] lines;

            public bool DisableModMasterSwitch;

            public Plugin plugin;

            public GameManager gameManager;
            public DataManager dataManager;
            public DamageManager damageManager;

            //main menu initialization
            private bool initialized;
            //initialization part 2 because some managers & values exist in game scene only
            private bool initializedInGame;
            //wait before modding datamanager
            private int waitFrames = 120;

            private string listDelimiter = ", ";
            private string wordDelimiter = "_";
            private string keyDelimiter = ":";

            internal void Update()
            {
                //Bepinex is angry if I don't wait here, probably some time between inject and scene load? Can't use unity event either here for some reason
                if (waitFrames > 0)
                {
                    waitFrames--;
                    return;
                }

                if (!initialized && DataManager.Instance != null)
                {
                    dataManager = DataManager.Instance;
                    ReadModConfig();
                    InitSetup();
                }

                if (DataManager.Instance.gameMgr != null && !initializedInGame)
                {
                    //test damage modifiers
                    //DataManager.Instance.gameMgr.DmgMgr.Data.ComparisonSheet[6].defType[0].Precentage = 900f;
                    //DataManager.Instance.gameMgr.DmgMgr.Data.ComparisonSheet[6].defType[1].Precentage = 600f;

                    gameManager = DataManager.Instance.gameMgr;
                    damageManager = gameManager.DmgMgr;

                    //WriteDefaultDamageTypeModifierConfig();

                    Il2CppReferenceArray<DMGData.TypesCompared> comparisonSheet = damageManager.Data.ComparisonSheet;

                    for (int i = 0; i < comparisonSheet.Length; i++)
                    {
                        foreach (var defType in comparisonSheet[i].defType)
                        {
                            string searchKey = new StringBuilder(i.ToString()).Append(wordDelimiter).Append(comparisonSheet[i].dmgType).Append(wordDelimiter).Append(defType.DefenseType.ToString()).ToString();
                            defType.Precentage = GetFloatByKey(defType.Precentage, searchKey);
                        }
                    }

                    InGameSetup();
                    plugin.Log.LogInfo("In-Game Init Complete");
                    initializedInGame = true;
                }


                //hotkey to force reloading the text file values again
                if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyUp(KeyCode.F11))
                {
                    plugin.Log.LogInfo("Reloading mod config");
                    ReadModConfig();
                    InitSetup();
                    //DataManager.Instance.FactionsOptions.Factions[0].Construction[0].CostData.resources[0].resource.
                    //DataManager.Instance.FactionsOptions.Factions[0].DefaultUpgrades[0].AddActions.AddItem(DataManager.Instance.FactionsOptions.Factions[1].DefaultUpgrades[1].AddActions[2]);
                    //DataManager.Instance.FactionsOptions.Factions[0].DefaultUpgrades[0].AddActions.AddItem(DataManager.Instance.FactionsOptions.Factions[1].DefaultUpgrades[1].AddActions[4]);
                }
            }

            private void InitSetup()
            {
                if (DisableModMasterSwitch)
                {
                    initialized = true;
                    return;
                }
                int intValue = 0;
                float floatValue = 0;
                bool boolValue = false;

                #region FactionStuff

                //Faction Stuff
                /*
                 * 0 Faction_Baltic_Saule
                 * 1 Faction_Baltic_Meness
                 * 2 Faction_Baltic_Michael
                 * 3 Faction_Baltic_Ausrine
                 * 4 Faction_Baltic_Bishops
                 */

                for (int i = 0; i < dataManager.FactionsOptions.Factions.Count; i++)
                {
                    string factionName = dataManager.FactionsOptions.Factions[i].name;
                    //Starting Resources
                    if (int.TryParse(GetValue(factionName + "_StartingWorshipperAmount"), out intValue))
                    {
                        dataManager.FactionsOptions.Factions[i].StartingWorshipperAmount = intValue;
                    }
                    if (int.TryParse(GetValue(factionName + "_StartFood"), out intValue))
                    {
                        dataManager.FactionsOptions.Factions[i].StartFood.amount = intValue;
                    }
                    if (int.TryParse(GetValue(factionName + "_StartWood"), out intValue))
                    {
                        dataManager.FactionsOptions.Factions[i].StartWood.amount = intValue;
                    }
                    if (int.TryParse(GetValue(factionName + "_StartFaith"), out intValue))
                    {
                        dataManager.FactionsOptions.Factions[i].StartFaith.amount = intValue;
                    }
                    if (int.TryParse(GetValue(factionName + "_StartWealth"), out intValue))
                    {
                        dataManager.FactionsOptions.Factions[i].StartWealth.amount = intValue;
                    }

                    //Resource Base Incomes
                    if (int.TryParse(GetValue(factionName + "_BaseIncomeFood"), out intValue))
                    {
                        dataManager.FactionsOptions.Factions[i].StartFood.Increase = intValue;
                    }
                    if (int.TryParse(GetValue(factionName + "_BaseIncomeWood"), out intValue))
                    {
                        dataManager.FactionsOptions.Factions[i].StartWood.Increase = intValue;
                    }
                    if (int.TryParse(GetValue(factionName + "_BaseIncomeFaith"), out intValue))
                    {
                        dataManager.FactionsOptions.Factions[i].StartFaith.Increase = intValue;
                    }
                    if (int.TryParse(GetValue(factionName + "_BaseIncomeWealth"), out intValue))
                    {
                        dataManager.FactionsOptions.Factions[i].StartWealth.Increase = intValue;
                    }

                    //Resource Max Caps
                    if (int.TryParse(GetValue(factionName + "_MaxFoodCap"), out intValue))
                    {
                        dataManager.FactionsOptions.Factions[i].StartFood.Maximum = intValue;
                    }
                    if (int.TryParse(GetValue(factionName + "_MaxWoodCap"), out intValue))
                    {
                        dataManager.FactionsOptions.Factions[i].StartWood.Maximum = intValue;
                    }
                    if (int.TryParse(GetValue(factionName + "_MaxFaithCap"), out intValue))
                    {
                        dataManager.FactionsOptions.Factions[i].StartFaith.Maximum = intValue;
                    }
                    if (int.TryParse(GetValue(factionName + "_MaxWealthCap"), out intValue))
                    {
                        dataManager.FactionsOptions.Factions[i].StartWealth.Maximum = intValue;
                    }

                    //
                    //if (int.TryParse(GetValue(DataManager.Instance.FactionsOptions.Factions[i].name + "_MaxWealthCap"), out intValue))
                    //{
                    //    DataManager.Instance.FactionsOptions.Factions[i]. = intValue;
                    //}
                }

                //saule marauders
                if (bool.TryParse(GetValue("AddMarauderToSauleWarcamp"), out boolValue) && boolValue == true)
                {
                    dataManager.FactionsOptions.Factions[0].DefaultUpgrades[0].AddActions = dataManager.FactionsOptions.Factions[1].DefaultUpgrades[1].AddActions;
                }

                //for (int i = 0; i < DataManager.Instance.FactionsOptions.Factions.Count; i++)
                //{
                //    if (int.TryParse(GetValue(DataManager.Instance.FactionsOptions.Factions[i].name + "_" + "StartWood"), out intValue))
                //    {
                //        DataManager.Instance.FactionsOptions.Factions[i].start.amount = intValue;
                //    }
                //}

                #endregion

                //if (int.TryParse(GetValue("StartingWorshipperAmount"), out intValue))
                //{
                //    for (int i = 0; i < DataManager.Instance.FactionsOptions.Factions.Count; i++)
                //    {
                //        DataManager.Instance.FactionsOptions.Factions[i].StartingWorshipperAmount = intValue;
                //    }
                //}

                #region GlobalVariables

                //worshipper data
                dataManager.MinimumWorshipperSpawnTime = GetIntByKey(dataManager.MinimumWorshipperSpawnTime, nameof(dataManager.MinimumWorshipperSpawnTime));
                dataManager.MaximumWorshipperSpawnTime = GetIntByKey(dataManager.MaximumWorshipperSpawnTime, nameof(dataManager.MaximumWorshipperSpawnTime));
                dataManager.StartingSpawnTime = GetIntByKey(dataManager.StartingSpawnTime, nameof(dataManager.StartingSpawnTime));
                dataManager.WorsipperSpawnTimeIncrease = GetFloatByKey(dataManager.WorsipperSpawnTimeIncrease, nameof(dataManager.WorsipperSpawnTimeIncrease));
                dataManager.EmptyHouseSpawnIncrease = GetFloatByKey(dataManager.EmptyHouseSpawnIncrease, nameof(dataManager.EmptyHouseSpawnIncrease));
                dataManager.PopulationSpawnTimeIncrease = GetFloatByKey(dataManager.PopulationSpawnTimeIncrease, nameof(dataManager.PopulationSpawnTimeIncrease));

                //xp and levels
                dataManager.maxLevel = GetIntByKey(dataManager.maxLevel, nameof(dataManager.maxLevel));
                dataManager.LevelOneXPRequirement = GetIntByKey(dataManager.LevelOneXPRequirement, nameof(dataManager.LevelOneXPRequirement));
                dataManager.LevelOneXPRequirementUnit = GetIntByKey(dataManager.LevelOneXPRequirementUnit, nameof(dataManager.LevelOneXPRequirementUnit));
                dataManager.FlatIncreasePerLvl = GetIntByKey(dataManager.FlatIncreasePerLvl, nameof(dataManager.FlatIncreasePerLvl));
                dataManager.FlatIncreasePerLvlUnit = GetIntByKey(dataManager.FlatIncreasePerLvlUnit, nameof (dataManager.FlatIncreasePerLvlUnit));

                //respawn
                dataManager.IncreaseRespawnTime = GetIntByKey(dataManager.IncreaseRespawnTime, nameof(dataManager.IncreaseRespawnTime));
                dataManager.LevelSpawnTimeIncrease = GetFloatByKey(dataManager.LevelSpawnTimeIncrease, nameof(dataManager.LevelSpawnTimeIncrease));
                dataManager.StartRespawnTime = GetIntByKey(dataManager.StartRespawnTime, nameof(dataManager.StartRespawnTime));

                //misc
                dataManager.StrenghtScaling = GetFloatByKey(dataManager.StrenghtScaling, nameof (dataManager.StrenghtScaling));
                dataManager.ChaseDistance = GetFloatByKey(dataManager.ChaseDistance, nameof (dataManager.ChaseDistance));

                #endregion

                //nurse
                //DataManager.Instance.FactionsOptions.Factions[2].Construction[6].CreationData.Creation[0].GetComponent<Building>().BData.Actions[0].CostData.resources[0].amount = 17;
                //DataManager.Instance.FactionsOptions.Factions[2].Construction[6].CreationData.Creation[0].GetComponent<Building>().BData.Actions[0].CreationData.Creation[0].GetComponent<Unit>().DataUnit.Speed = 17;
                //DataManager.Instance.FactionsOptions.Factions[2].Construction[6].CreationData.Creation[0].GetComponent<Building>().BData.Actions[0].CreationData.Creation[0].GetComponent<Unit>().DataUnit.DefualtMaxHealth = 600;
                plugin.Log.LogInfo("Finished main menu mod setup");
                initialized = true;
            }

            private void InGameSetup()
            {
                if (DisableModMasterSwitch)
                {
                    initializedInGame = true;
                    return;
                }
                gameManager.CorpseUnitTime = GetFloatByKey(gameManager.CorpseUnitTime, nameof (gameManager.CorpseUnitTime));
                plugin.Log.LogInfo("Finished in-game mod setup");
                initializedInGame = true;
            }

            #region Utilities

            private float GetFloatByKey(float originalFloat, string key)
            {
                if (float.TryParse(GetValue(key), out float outVal))
                {
                    return outVal;
                }
                else
                {
                    plugin.Log.LogInfo("Failed to parse Float: " + key);
                    return originalFloat;
                }
            }

            private int GetIntByKey(int originalInt, string key)
            {
                if (int.TryParse(GetValue(key), out int outVal))
                {
                    return outVal;
                }
                else
                {
                    plugin.Log.LogInfo("Failed to parse Int: " + key);
                    return originalInt;
                }
            }

            private bool GetBoolByKey(bool originalBool, string key)
            {
                if (bool.TryParse(GetValue(key), out bool boolVal))
                {
                    return boolVal;
                }
                else
                {
                    plugin.Log.LogInfo("Failed to parse Int: " + key);
                    return originalBool;
                }
            }

            private void WriteDefaultDamageTypeModifierConfig()
            {
                Il2CppReferenceArray<DMGData.TypesCompared> comparisonSheet = damageManager.Data.ComparisonSheet;
                List<string> damageTypeLines = new List<string>();
                for (int i = 0; i < comparisonSheet.Length; i++)
                {
                    plugin.Log.LogInfo(new StringBuilder(i.ToString()).Append(listDelimiter).Append(comparisonSheet[i].dmgType).ToString());
                    foreach (var defType in comparisonSheet[i].defType)
                    {
                        plugin.Log.LogInfo(new StringBuilder(defType.DefenseType.ToString()).Append(listDelimiter).Append(defType.Precentage).ToString());
                        damageTypeLines.Add(new StringBuilder(i.ToString()).Append(wordDelimiter).Append(comparisonSheet[i].dmgType).Append(wordDelimiter).Append(defType.DefenseType.ToString()).Append(keyDelimiter).Append(defType.Precentage).ToString());
                    }
                }
                WriteConfig(modRootPath + "DamageTypesConfig.txt", damageTypeLines);
            }

            #endregion

            //Read config text file from mod folder
            public void ReadModConfig()
            {
                configPath = Directory.GetCurrentDirectory() + @"\BepInEx\plugins\GodswornConfigurator\config.txt";
                modRootPath = Directory.GetCurrentDirectory() + @"\BepInEx\plugins\GodswornConfigurator\";
                plugin.Log.LogInfo(configPath);
                lines = null;
                StreamReader reader = new StreamReader(configPath, true);
                lines = reader.ReadToEnd().Split('\n');
                reader.Close();
                //master disable switch field
                if (bool.TryParse(GetValue("DisableThisMod"), out bool boolVal))
                {
                    DisableModMasterSwitch = boolVal;
                }
            }

            public void WriteConfig(string fileName, List<string> text)
            {
                StreamWriter writer = new StreamWriter(fileName, true);
                for (int i = 0; i < text.Count; i++)
                {
                    writer.Write('\n' + text[i]);
                }
                writer.Close();
            }

            //Retrieve value from file
            public string GetValue(string key)
            {
                foreach (string line in lines)
                {
                    //ignore commented lines
                    if (!line.Contains("//"))
                    {
                        if (line.Split(':')[0].Contains(key))
                        {
                            string value = line.Split(':')[1];
                            return value;
                        }
                    }
                }
                return null;
            }

        }

    }

}