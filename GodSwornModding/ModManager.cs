using System;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using BepInEx.Configuration;
using UnityEngine;
using System.Collections;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using BepInEx.Unity.IL2CPP.Utils.Collections;

namespace JCGodSwornConfigurator
{
    [BepInPlugin("JCGodSwornConfigurator", "GodSwornConfigurator", "1.0.0")]
    public class Plugin : BasePlugin
    {
        #region Plugin Core
        public GameObject GodSwornMainModObject;
        // Plugin startup logic
        public override void Load()
        {
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
        #endregion

        public class ModManager : MonoBehaviour
        {
            public string configPath;
            public string modRootPath;
            public string[] lines;

            public bool DisableModMasterSwitch;
            public bool ShowUIWidget;

            public Plugin plugin;

            public GameManager gameManager;
            public DataManager dataManager;
            public DamageManager damageManager;

            public List<UnitData> unitDataList = new List<UnitData>();

            //main menu initialization
            private bool initialized;
            //initialization part 2 because some managers & values exist in game scene only
            private bool initializedInGame;
            //wait before modding datamanager
            private int waitFrames = 120;

            private readonly string listDelimiter = ", ";
            private readonly string wordDelimiter = "_";
            private readonly string keyDelimiter = ":";
            private readonly string generatedConfigFolderPath = @"DefaultConfigData\";

            internal void Update()
            {
                //Bepinex is angry if I don't wait here, probably some time between inject and unity/scene load? Can't use unity event either here for some reason
                if (waitFrames > 0)
                {
                    waitFrames--;
                    return;
                }

                if (!initialized && DataManager.Instance != null)
                {
                    dataManager = DataManager.Instance;
                    Initialize();
                    ReadModConfig();
                    MainSetup();
                }

                if (DataManager.Instance.gameMgr != null && !initializedInGame)
                {
                    gameManager = DataManager.Instance.gameMgr;
                    damageManager = gameManager.DmgMgr;

                    //WriteDefaultDamageTypeModifierConfig();   //internal use for creating default config

                    InGameSetup();
                    plugin.Log.LogInfo("In-Game Init Complete");
                    initializedInGame = true;
                }


                //hotkey to force reloading the text file values again
                if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyUp(KeyCode.F10))
                {
                    ShowUIWidget = !ShowUIWidget;
                }
            }

            void OnGUI()
            {
                if (ShowUIWidget)
                {
                    GUI.Box(new Rect(10, 10, 260, 120), "Godsworn Configurator");
                    if (GUI.Button(new Rect(20, 40, 220, 20), "Reload Config Files"))
                    {

                        plugin.Log.LogInfo("Reloading mod config");
                        ReadModConfig();
                        initializedInGame = false;
                        MainSetup();
                    }
                    if (GUI.Button(new Rect(20, 70, 220, 20), "Generate Default Config Values"))
                    {
                        WriteDefaultDataConfigs();
                    }
                }
            }

            private void Initialize()
            {
                modRootPath = Directory.GetCurrentDirectory() + @"\BepInEx\plugins\GodswornConfigurator\";
                configPath = modRootPath + "config.txt";
                if (!Directory.Exists(modRootPath + generatedConfigFolderPath))
                {
                    Directory.CreateDirectory(modRootPath + generatedConfigFolderPath);
                }
            }

            /// <summary>
            /// General mod setup for global config for variables present in main menu
            /// </summary>
            private void MainSetup()
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                if (DisableModMasterSwitch)
                {
                    initialized = true;
                    return;
                }
                bool boolValue;

                #region FactionStuff

                //Faction Stuff
                /*
                 * 0 Faction_Baltic_Saule
                 * 1 Faction_Baltic_Meness
                 * 2 Faction_Baltic_Michael
                 * 3 Faction_Baltic_Ausrine
                 * 4 Faction_Baltic_Bishops
                 */

                var factionsData = dataManager.FactionsOptions.Factions;

                //WriteDefaultFactionDataConfig();  //internal use for creating default config

                unitDataList.Clear();
                List<string> modifiedBuildingsList = new List<string>();

                for (int i = 0; i < 5; i++)
                {
                    string factionName = factionsData[i].name;

                    //Starting Resources
                    factionsData[i].StartingWorshipperAmount = GetIntByKey(factionsData[i].StartingWorshipperAmount, factionName + "_StartingWorshipperAmount");
                    factionsData[i].StartFood.amount = GetIntByKey(factionsData[i].StartFood.amount, factionName + "_StartFood");
                    factionsData[i].StartWood.amount = GetIntByKey(factionsData[i].StartWood.amount, factionName + "_StartWood");
                    factionsData[i].StartFaith.amount = GetIntByKey(factionsData[i].StartFaith.amount, factionName + "_StartFaith");
                    factionsData[i].StartWealth.amount = GetIntByKey(factionsData[i].StartWealth.amount, factionName + "_StartWealth");

                    //Resource Base Incomes
                    factionsData[i].StartFood.Increase = GetIntByKey(factionsData[i].StartFood.Increase, factionName + "_BaseIncomeFood");
                    factionsData[i].StartWood.Increase = GetIntByKey(factionsData[i].StartWood.Increase, factionName + "_BaseIncomeWood");
                    factionsData[i].StartFaith.Increase = GetIntByKey(factionsData[i].StartFaith.Increase, factionName + "_BaseIncomeFaith");
                    factionsData[i].StartWealth.Increase = GetIntByKey(factionsData[i].StartWealth.Increase, factionName + "_BaseIncomeWealth");

                    //Resource Max Caps
                    factionsData[i].StartFood.Maximum = GetIntByKey(factionsData[i].StartFood.Maximum, factionName + "_MaxFoodCap");
                    factionsData[i].StartWood.Maximum = GetIntByKey(factionsData[i].StartWood.Maximum, factionName + "_MaxWoodCap");
                    factionsData[i].StartFaith.Maximum = GetIntByKey(factionsData[i].StartFaith.Maximum, factionName + "_MaxFaithCap");
                    factionsData[i].StartWealth.Maximum = GetIntByKey(factionsData[i].StartWealth.Maximum, factionName + "_MaxWealthCap");

                    //Hero Stats
                    HeroData heroData = factionsData[i].MainHero;
                    heroData.DefualtMaxHealth = GetIntByKey(heroData.DefualtMaxHealth, CombineStrings(factionName, wordDelimiter, nameof(heroData.DefualtMaxHealth)));
                    heroData.DefaultHealthRegen = GetFloatByKey(heroData.DefaultHealthRegen, CombineStrings(factionName, wordDelimiter, nameof(heroData.DefaultHealthRegen)));
                    heroData.Speed = GetFloatByKey(heroData.Speed, CombineStrings(factionName, wordDelimiter, nameof(heroData.Speed)));
                    heroData.Armor = GetIntByKey(heroData.Armor, CombineStrings(factionName, wordDelimiter, nameof(heroData.Armor)));
                    heroData.MagicResistance = GetIntByKey(heroData.MagicResistance, CombineStrings(factionName, wordDelimiter, nameof(heroData.MagicResistance)));
                    heroData.Visionrange = GetIntByKey(heroData.Visionrange, CombineStrings(factionName, wordDelimiter, nameof(heroData.Visionrange)));
                    heroData.XP = GetIntByKey(heroData.XP, CombineStrings(factionName, wordDelimiter, nameof(heroData.XP)));

                    //building data
                    for (int j = 0; j < factionsData[i].Construction.Length; j++)
                    {
                        string buildingName = factionsData[i].Construction[j].name;
                        if (!modifiedBuildingsList.Contains(buildingName))
                        {
                            plugin.Log.LogInfo(buildingName);
                            modifiedBuildingsList.Add(buildingName);
                            //building cost
                            for (int k = 0; k < factionsData[i].Construction[j].CostData.resources.Length; k++)
                            {
                                string resourceName = GetSanitizedResourceName(factionsData[i].Construction[j].CostData.resources[k].resource.name);
                                string searchKey = CombineStrings(buildingName, wordDelimiter, resourceName);
                                factionsData[i].Construction[j].CostData.resources[k].amount = GetIntByKey(factionsData[i].Construction[j].CostData.resources[k].amount, searchKey);
                            }

                            //Get unit data from creation buildings
                            for (int k = 0; k < factionsData[i].Construction[j].CreationData.Creation[0].GetComponent<Building>().BData.Actions.Length; k++)
                            {
                                var actionData = factionsData[i].Construction[j].CreationData.Creation[0].GetComponent<Building>().BData.Actions[k];
                                if (actionData.CreationData != null)
                                {
                                    var unitData = actionData.CreationData.Creation[0].GetComponent<Unit>()?.DataUnit;
                                    if (unitData != null)
                                    {
                                        if (unitData.CreationAbility == null)
                                        {
                                            unitData.CreationAbility = actionData;
                                            plugin.Log.LogInfo(CombineStrings(unitData.name, " missing creation data, patching with building action data"));
                                        }
                                        unitDataList.Add(unitData);
                                    }
                                }
                            }
                        }
                    }

                    //Get unit data from upgrades
                    for (int j = 0; j < factionsData[i].DefaultUpgrades.Count; j++)
                    {
                        for (int k = 0; k < factionsData[i].DefaultUpgrades[j].AddActions.Count; k++)
                        {
                            var actionData = factionsData[i].DefaultUpgrades[j].AddActions[k];
                            if (actionData.CreationData != null)
                            {
                                var unitData = actionData.CreationData.Creation[0].GetComponent<Unit>()?.DataUnit;
                                if (unitData != null && !unitDataList.Contains(unitData))
                                {
                                    unitDataList.Add(unitData);
                                }
                            }
                        }
                    }

                }

                //WriteDefaultUnitDataConfig();

                //process unit mods
                foreach (var unit in unitDataList)
                {
                    plugin.Log.LogInfo(unit.name);
                    string baseSearchKey = CombineStrings("Unit_", unit.name);
                    unit.DefualtMaxHealth = GetIntByKey(unit.DefualtMaxHealth, CombineStrings(baseSearchKey, wordDelimiter, nameof(unit.DefualtMaxHealth)));
                    unit.DefaultHealthRegen = GetFloatByKey(unit.DefaultHealthRegen, CombineStrings(baseSearchKey, wordDelimiter, nameof(unit.DefaultHealthRegen)));
                    unit.Speed = GetFloatByKey(unit.Speed, CombineStrings(baseSearchKey, wordDelimiter, nameof(unit.Speed)));
                    unit.Armor = GetIntByKey(unit.Armor, CombineStrings(baseSearchKey, wordDelimiter, nameof(unit.Armor)));
                    unit.MagicResistance = GetIntByKey(unit.MagicResistance, CombineStrings(baseSearchKey, wordDelimiter, nameof(unit.MagicResistance)));
                    unit.Visionrange = GetIntByKey(unit.Visionrange, CombineStrings(baseSearchKey, wordDelimiter, nameof(unit.Visionrange)));
                    unit.XP = GetIntByKey(unit.XP, CombineStrings(baseSearchKey, wordDelimiter, nameof(unit.XP)));
                    unit.HousingUpkeep = GetIntByKey(unit.HousingUpkeep, CombineStrings(baseSearchKey, wordDelimiter, nameof(unit.HousingUpkeep)));
                    //unit cost data
                    if (unit.CreationAbility != null)
                    {
                        for (int i = 0; i < unit.CreationAbility.CostData.resources.Count; i++)
                        {
                            string resourceName = GetSanitizedResourceName(unit.CreationAbility.CostData.resources[i].resource.name);
                            string searchKey = CombineStrings(baseSearchKey, wordDelimiter, resourceName);
                            unit.CreationAbility.CostData.resources[i].amount = GetIntByKey(unit.CreationAbility.CostData.resources[i].amount, searchKey);
                        }
                    }
                    else
                    {
                        plugin.Log.LogWarning(unit.name + "Missing Creation Data");
                    }
                }

                //saule marauders
                if (bool.TryParse(GetValue("AddMarauderToSauleWarcamp"), out boolValue) && boolValue == true)
                {
                    //factionsData[0].DefaultUpgrades[0].AddActions = factionsData[1].DefaultUpgrades[1].AddActions;
                    ActionData newData1 = factionsData[1].DefaultUpgrades[1].AddActions[2];
                    ActionData newData2 = factionsData[1].DefaultUpgrades[1].AddActions[4];

                    factionsData[0].DefaultUpgrades[0].AddActions = factionsData[0].DefaultUpgrades[0].AddActions.AddItem(newData1).ToArray();
                    factionsData[0].DefaultUpgrades[0].AddActions = factionsData[0].DefaultUpgrades[0].AddActions.AddItem(newData2).ToArray();
                }

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

                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                plugin.Log.LogInfo(CombineStrings("Finished main menu mod setup in ", elapsedMs.ToString(), " ms"));
                initialized = true;
            }

            /// <summary>
            /// Handles setup for config that exists outside of main menu and in game scene only
            /// </summary>
            private void InGameSetup()
            {
                if (DisableModMasterSwitch)
                {
                    initializedInGame = true;
                    return;
                }

                //Damage Type Modifiers
                Il2CppReferenceArray<DMGData.TypesCompared> comparisonSheet = damageManager.Data.ComparisonSheet;

                for (int i = 0; i < comparisonSheet.Length; i++)
                {
                    foreach (var defType in comparisonSheet[i].defType)
                    {
                        
                        string searchKey = CombineStrings(i.ToString(), wordDelimiter, comparisonSheet[i].dmgType.ToString(), wordDelimiter, defType.DefenseType.ToString());
                        defType.Precentage = GetFloatByKey(defType.Precentage, searchKey);
                    }
                }

                gameManager.CorpseUnitTime = GetFloatByKey(gameManager.CorpseUnitTime, nameof (gameManager.CorpseUnitTime));
                
                plugin.Log.LogInfo("Finished in-game mod setup");
                initializedInGame = true;
            }

            #region Utilities
            //helper functions

            private float GetFloatByKey(float originalFloat, string key)
            {
                if (float.TryParse(GetValue(key), out float outVal))
                {
                    return outVal;
                }
                else
                {
                    plugin.Log.LogInfo(CombineStrings("Failed to parse Float: ", key, listDelimiter, originalFloat.ToString()));
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
                    plugin.Log.LogInfo(CombineStrings("Failed to parse Int: ", key, listDelimiter, originalInt.ToString()));
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
                    plugin.Log.LogInfo(CombineStrings("Failed to parse Bool: ", key, originalBool.ToString()));
                    return originalBool;
                }
            }

            private StringBuilder sb = new StringBuilder();
            private string CombineStrings(params string[] strings)
            {
                sb.Clear();
                foreach (string s in strings)
                {
                    sb.Append(s);
                }
                return sb.ToString();
            }

            /// <summary>
            /// "2 Wood" -> "Wood"
            /// </summary>
            private string GetSanitizedResourceName(string inputString)
            {
                if (inputString.ToLower().Contains("food")) return "Food";
                if (inputString.ToLower().Contains("wood")) return "Wood";
                if (inputString.ToLower().Contains("faith")) return "Faith";
                if (inputString.ToLower().Contains("wealth")) return "Wealth";
                return inputString;
            }


            private void WriteDefaultDataConfigs()
            {
                WriteDefaultDamageTypeModifierConfig();
                WriteDefaultFactionDataConfig();
                WriteDefaultUnitDataConfig();
            }
            /// <summary>
            /// Write separate config file with game's default damage modifier table
            /// </summary>
            private void WriteDefaultDamageTypeModifierConfig()
            {
                if (damageManager == null)
                {
                    plugin.Log.LogWarning("Cannot generate damage modifier data from main menu");
                    return;
                }
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
                WriteConfig(modRootPath + generatedConfigFolderPath + "DefaultDamageTypesConfig.txt", damageTypeLines);
            }

            /// <summary>
            /// Write separate config file with game's default faction data
            /// </summary>
            private void WriteDefaultFactionDataConfig()
            {
                List<string> factionDataLines = new List<string>();
                var factionsData = dataManager.FactionsOptions.Factions;
                List<string> modifiedBuildingsList = new List<string>();
                for (int i = 0; i < 5; i++)
                {
                    string factionName = factionsData[i].name;

                    HeroData heroData = factionsData[i].MainHero;
                    factionDataLines.Add(CombineStrings(factionName, wordDelimiter, nameof(heroData.DefualtMaxHealth), keyDelimiter, heroData.DefualtMaxHealth.ToString()));
                    factionDataLines.Add(CombineStrings(factionName, wordDelimiter, nameof(heroData.DefaultHealthRegen), keyDelimiter, heroData.DefaultHealthRegen.ToString()));
                    factionDataLines.Add(CombineStrings(factionName, wordDelimiter, nameof(heroData.Speed), keyDelimiter, heroData.Speed.ToString()));
                    factionDataLines.Add(CombineStrings(factionName, wordDelimiter, nameof(heroData.Armor), keyDelimiter, heroData.Armor.ToString()));
                    factionDataLines.Add(CombineStrings(factionName, wordDelimiter, nameof(heroData.MagicResistance), keyDelimiter, heroData.MagicResistance.ToString()));
                    factionDataLines.Add(CombineStrings(factionName, wordDelimiter, nameof(heroData.Visionrange), keyDelimiter, heroData.Visionrange.ToString()));
                    factionDataLines.Add(CombineStrings(factionName, wordDelimiter, nameof(heroData.XP), keyDelimiter, heroData.XP.ToString()));

                    for (int k = 0; k < factionsData[i].Construction.Length; k++)
                    {
                        string buildingName = factionsData[i].Construction[k].name;
                        if (!modifiedBuildingsList.Contains(buildingName))
                        {
                            modifiedBuildingsList.Add(buildingName);
                            for (int j = 0; j < factionsData[i].Construction[k].CostData.resources.Length; j++)
                            {
                                string resourceName = GetSanitizedResourceName(factionsData[i].Construction[k].CostData.resources[j].resource.name);
                                int resourceQuantity = factionsData[i].Construction[k].CostData.resources[j].amount;
                                string searchKey = CombineStrings(buildingName, wordDelimiter, resourceName, keyDelimiter, resourceQuantity.ToString());
                                factionDataLines.Add(searchKey);
                            }
                        }
                    }
                }
                WriteConfig(modRootPath + generatedConfigFolderPath + "DefaultFactionDataConfig.txt", factionDataLines);
            }

            private void WriteDefaultUnitDataConfig()
            {
                List<string> unitDataLines = new List<string>();
                foreach (var unit in unitDataList)
                {
                    unitDataLines.Add(CombineStrings("//", unit.name));
                    string baseSearchKey = CombineStrings("Unit_", unit.name);
                    unitDataLines.Add(CombineStrings(baseSearchKey, wordDelimiter, nameof (unit.DefualtMaxHealth), keyDelimiter, unit.DefualtMaxHealth.ToString()));
                    unitDataLines.Add(CombineStrings(baseSearchKey, wordDelimiter, nameof(unit.DefaultHealthRegen), keyDelimiter, unit.DefaultHealthRegen.ToString()));
                    unitDataLines.Add(CombineStrings(baseSearchKey, wordDelimiter, nameof(unit.Speed), keyDelimiter, unit.Speed.ToString()));
                    unitDataLines.Add(CombineStrings(baseSearchKey, wordDelimiter, nameof(unit.Armor), keyDelimiter, unit.Armor.ToString()));
                    unitDataLines.Add(CombineStrings(baseSearchKey, wordDelimiter, nameof(unit.MagicResistance), keyDelimiter, unit.MagicResistance.ToString()));
                    unitDataLines.Add(CombineStrings(baseSearchKey, wordDelimiter, nameof(unit.Visionrange), keyDelimiter, unit.Visionrange.ToString()));
                    unitDataLines.Add(CombineStrings(baseSearchKey, wordDelimiter, nameof(unit.XP), keyDelimiter, unit.XP.ToString()));
                    unitDataLines.Add(CombineStrings(baseSearchKey, wordDelimiter, nameof(unit.HousingUpkeep), keyDelimiter, unit.HousingUpkeep.ToString()));
                    //unit cost data
                    if (unit.CreationAbility != null)
                    {
                        for (int i = 0; i < unit.CreationAbility.CostData.resources.Count; i++)
                        {
                            string resourceName = GetSanitizedResourceName(unit.CreationAbility.CostData.resources[i].resource.name);
                            string searchKey = CombineStrings(baseSearchKey, wordDelimiter, resourceName);
                            unitDataLines.Add(CombineStrings(searchKey, keyDelimiter, unit.CreationAbility.CostData.resources[i].amount.ToString()));
                        }
                    }
                    else
                    {
                        plugin.Log.LogWarning(unit.name + "Missing Creation Data");
                    }
                }
                WriteConfig(modRootPath + generatedConfigFolderPath + "DefaultUnitDataConfig.txt", unitDataLines);
            }

            #endregion

            /// <summary>
            /// Read config text file from mod folder
            /// </summary>
            public void ReadModConfig()
            {
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
            /// <summary>
            /// Write to text file
            /// </summary>
            public void WriteConfig(string fileName, List<string> text)
            {
                StreamWriter writer = new StreamWriter(fileName, true);
                for (int i = 0; i < text.Count; i++)
                {
                    writer.Write('\n' + text[i]);
                }
                writer.Close();
            }

            /// <summary>
            /// Retrieve value from file
            /// </summary>
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
                plugin.Log.LogInfo(CombineStrings("Failed to find key: ", key));
                return null;
            }

        }

    }

}