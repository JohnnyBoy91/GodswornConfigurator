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
    [BepInPlugin("JCGodSwornConfigurator", "GodSwornConfigurator", "1.0.01")]
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
                UnityEngine.Object.DontDestroyOnLoad(GodSwornMainModObject);
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
            public string dataConfigPath;
            public string modSettingsPath;
            public string modRootPath;
            public string[] datalines;
            public string[] settingslines;

            public bool DisableModMasterSwitch;
            public bool RetrieveDataMode;
            public bool ShowUIWidget;

            public Plugin plugin;

            private ModSpectatorMode modSpectatorMode = new ModSpectatorMode();

            public GameManager gameManager;
            public DataManager dataManager;
            public DamageManager damageManager;

            public List<UnitData> unitDataList = new List<UnitData>();
            public List<BuildingData> buildingDataList = new List<BuildingData>();
            public List<HeroData> heroDataList = new List<HeroData>();
            public List<ActionDataConfig> actionDataList = new List<ActionDataConfig>();
            public List<EffectDataConfig> effectDataList = new List<EffectDataConfig>();
            public List<TargetDataConfig> targetDataList = new List<TargetDataConfig>();
            public List<CastsDataConfig> castsDataList = new List<CastsDataConfig>();
            public List<ProjectileDataConfig> projectileDataList = new List<ProjectileDataConfig>();
            public List<string> actionDataNameLink = new List<string>();

            private bool verboseLogging;

            //main menu initialization
            private bool initialized;
            //initialization part 2 because some managers & values exist in game scene only
            private bool initializedInGame;
            //wait before modding datamanager
            private int waitFrames = 0;

            private const string prefixUnit = "Unit";
            private const string prefixHero = "Hero";
            private const string prefixHeroSkill = "DivineSkill";

            //config delimiters
            private readonly string dlmList = ", ";
            private readonly string dlmWord = "_";
            private readonly string dlmKey = ":";
            private readonly string dlmComment = "//";
            private readonly string dlmNewLine = "\n";
            private readonly string generatedConfigFolderPath = @"DefaultConfigData\";

            internal void Update()
            {
                //Takes time to grab refs here, probably some time between inject and unity/scene load?
                //Can't use unity event either here for some reason(stripped?)
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

                if (dataManager?.gameMgr != null && !initializedInGame)
                {
                    gameManager = dataManager.gameMgr;
                    damageManager = gameManager.DmgMgr;

                    //WriteDefaultDamageTypeModifierConfig();   //internal use for creating default config

                    InGameSetup();
                    Log("In-Game Init Complete");
                    initializedInGame = true;
                }

                //hotkey for dev commands
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

                        Log("Reloading mod config");
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
                dataConfigPath = modRootPath + "ModDataConfig.txt";
                modSettingsPath = modRootPath + "ModSettings.txt";
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

                verboseLogging = GetBoolByKey(verboseLogging, "VerboseLogging");

                if (bool.TryParse(GetValue("RetrieveDataMode"), out bool boolValue) && boolValue == true)
                {
                    RetrieveDataMode = true;
                }

                if (bool.TryParse(GetValue("EnableSpectatorMode"), out boolValue) && boolValue == true)
                {
                    modSpectatorMode.InitializeSpectatorMode(dataManager);
                }

                if (DisableModMasterSwitch && !RetrieveDataMode)
                {
                    initialized = true;
                    return;
                }

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

                ClearDataCache();


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
                    heroDataList.Add(heroData);
                    heroData.DefualtMaxHealth = GetIntByKey(heroData.DefualtMaxHealth, CombineStrings(factionName, dlmWord, nameof(heroData.DefualtMaxHealth)));
                    heroData.DefaultHealthRegen = GetFloatByKey(heroData.DefaultHealthRegen, CombineStrings(factionName, dlmWord, nameof(heroData.DefaultHealthRegen)));
                    heroData.Speed = GetFloatByKey(heroData.Speed, CombineStrings(factionName, dlmWord, nameof(heroData.Speed)));
                    heroData.Armor = GetIntByKey(heroData.Armor, CombineStrings(factionName, dlmWord, nameof(heroData.Armor)));
                    heroData.MagicResistance = GetIntByKey(heroData.MagicResistance, CombineStrings(factionName, dlmWord, nameof(heroData.MagicResistance)));
                    heroData.Visionrange = GetIntByKey(heroData.Visionrange, CombineStrings(factionName, dlmWord, nameof(heroData.Visionrange)));
                    heroData.XP = GetIntByKey(heroData.XP, CombineStrings(factionName, dlmWord, nameof(heroData.XP)));

                    //building data
                    for (int j = 0; j < factionsData[i].Construction.Length; j++)
                    {
                        string buildingName = factionsData[i].Construction[j].name;
                        if (!modifiedBuildingsList.Contains(buildingName))
                        {
                            var buildingData = factionsData[i].Construction[j].CreationData.Creation[0].GetComponent<Building>()?.BData;
                            if (buildingData != null) buildingDataList.Add(buildingData);
                            modifiedBuildingsList.Add(buildingName);
                            //building cost
                            for (int k = 0; k < factionsData[i].Construction[j].CostData.resources.Length; k++)
                            {
                                string resourceName = Utilities.GetSanitizedResourceName(factionsData[i].Construction[j].CostData.resources[k].resource.name);
                                string searchKey = CombineStrings(buildingName, dlmWord, resourceName);
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
                                            Log(CombineStrings(unitData.name, " missing creation data, patching with building action data"));
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

                    //get ability data from factions
                    foreach (var divineSkillPairs in factionsData[i].DivineSkills)
                    {
                        foreach (var divineSkill in divineSkillPairs.PossibleAbilitiesLvl)
                        {
                            foreach (var upgrade in divineSkill.upgrades)
                            {
                                foreach (var action in upgrade.AddActions)
                                {
                                    actionDataList.Add(new ActionDataConfig(action, heroData, true));
                                }
                            }
                        }
                    }

                }

                //collect spawnedUnit Data
                CollectSpawnedUnitDataFromActions();

                var unitsAndHeroList = new List<UnitData>();
                unitsAndHeroList.AddRange(unitDataList);
                unitsAndHeroList.AddRange(heroDataList);

                //actionData has cooldown and duration(swing time?) trigger delay?
                //Cast has damage data for projectiles

                //TargetData has damage for melee attacks - casttarget.effectDat

                //TargetData has projectile launch data for ranged attacks - TargetData.CastProjectile
                //Casts.ProjectileData is projectile
                //ProjectileData has TargetData - TargetData.CastProjectile deals damage for meness,ranger maybe all? casttarget on projectile is maybe just vfx or interchangeable?


                //collectActionData
                for (int i = 0; i < unitsAndHeroList.Count; i++)
                {
                    for (int j = 0; j < unitsAndHeroList[i].Actions.Count; j++)
                    {
                        if(!actionDataList.Any(x => x.actionData == unitsAndHeroList[i].Actions[j]))
                        {
                            actionDataList.Add(new ActionDataConfig(unitsAndHeroList[i].Actions[j], unitsAndHeroList[i]) );
                            actionDataNameLink.Add(CombineStrings(unitsAndHeroList[i].name, dlmWord));
                        }
                    }

                    for (int j = 0; j < unitsAndHeroList[i].DefaultUpgrades.Count; j++)
                    {
                        for (int k = 0; k < unitsAndHeroList[i].DefaultUpgrades[j].AddActions.Count; k++)
                        {
                            if (!actionDataList.Any(x => x.actionData == unitsAndHeroList[i].DefaultUpgrades[j].AddActions[k]))
                            {
                                actionDataList.Add(new ActionDataConfig(unitsAndHeroList[i].DefaultUpgrades[j].AddActions[k], unitsAndHeroList[i]));
                                actionDataNameLink.Add(CombineStrings(unitsAndHeroList[i].name, dlmWord));
                            }
                        }
                    }
                }

                CollectDataFromActions();


                if (DisableModMasterSwitch)
                {
                    initialized = true;
                    return;
                }

                //process action data
                foreach (ActionDataConfig actionData in actionDataList)
                {
                    ActionData data = actionData.actionData;
                    data.CoolDown = GetFloatByKey(data.CoolDown, CombineStrings(actionData.UnitName(), dlmWord, nameof(ActionData), dlmWord, data.name, dlmWord, nameof (data.CoolDown)));
                    data.Duration = GetFloatByKey(data.Duration, CombineStrings(actionData.UnitName(), dlmWord, nameof(ActionData), dlmWord, data.name, dlmWord, nameof(data.Duration)));
                    data.TriggerDelay = GetFloatByKey(data.TriggerDelay, CombineStrings(actionData.UnitName(), dlmWord, nameof(ActionData), dlmWord, data.name, dlmWord, nameof(data.TriggerDelay)));
                    data.Charges = GetIntByKey(data.Charges, CombineStrings(actionData.UnitName(), dlmWord, nameof(ActionData), dlmWord, data.name, dlmWord, nameof(data.Charges)));
                    if (data.RandomizeData != null)
                    {
                        var randomizeData = data.RandomizeData;
                        randomizeData.RepeatAmount = GetIntByKey(randomizeData.RepeatAmount, CombineStrings(actionData.UnitName(), dlmWord, nameof(ActionData), dlmWord, data.name, dlmWord, "MultiShot", dlmWord, nameof(randomizeData.RepeatAmount)));
                        randomizeData.RepeatIntervall = GetFloatByKey(randomizeData.RepeatIntervall, CombineStrings(actionData.UnitName(), dlmWord, nameof(ActionData), dlmWord, data.name, dlmWord, "MultiShot", dlmWord, nameof(randomizeData.RepeatIntervall)));
                    }
                    if (data.CostData != null)
                    {
                        foreach (var resourceData in data.CostData.resources)
                        {
                            string resourceName = Utilities.GetSanitizedResourceName(resourceData.resource.name);
                            string searchKey = CombineStrings(actionData.UnitName(), dlmWord, nameof(ActionData), dlmWord, data.name, dlmWord, nameof(CostsData), dlmWord, resourceName);
                            resourceData.amount = GetIntByKey(resourceData.amount, searchKey);
                        }
                    }
                }

                //process effect data
                foreach (EffectDataConfig effectData in effectDataList)
                {
                    EffectData data = effectData.effectData;
                    data.Damage = GetIntByKey(data.Damage, CombineStrings(effectData.UnitName(), dlmWord, nameof(EffectData), dlmWord, data.name, dlmWord, nameof(data.Damage)));
                    data.ScaleWithStrenght = GetBoolByKey(data.ScaleWithStrenght, CombineStrings(effectData.UnitName(), dlmWord, nameof(EffectData), dlmWord, data.name, dlmWord, nameof(data.ScaleWithStrenght)));
                    data.ScaleWithPower = GetBoolByKey(data.ScaleWithPower, CombineStrings(effectData.UnitName(), dlmWord, nameof(EffectData), dlmWord, data.name, dlmWord, nameof(data.ScaleWithPower)));
                    data.LifestealPercentrage = GetFloatByKey(data.LifestealPercentrage, CombineStrings(effectData.UnitName(), dlmWord, nameof(EffectData), dlmWord, data.name, dlmWord, nameof(data.LifestealPercentrage)));
                    //TODO damage type
                }

                //process projectile data 
                foreach (ProjectileDataConfig projectileData in projectileDataList)
                {
                    ProjectileData data = projectileData.projectileData;
                    data.Homing = GetBoolByKey(data.Homing, CombineStrings(projectileData.UnitName(), dlmWord, nameof(ProjectileData), dlmWord, data.name, dlmWord, nameof(data.Homing)));
                    data.LifeTime = GetFloatByKey(data.LifeTime, CombineStrings(projectileData.UnitName(), dlmWord, nameof(ProjectileData), dlmWord, data.name, dlmWord, nameof(data.LifeTime)));
                    data.StartSpeed = GetFloatByKey(data.StartSpeed, CombineStrings(projectileData.UnitName(), dlmWord, nameof(ProjectileData), dlmWord, data.name, dlmWord, nameof(data.StartSpeed)));
                    data.AccuracyPenalty = GetFloatByKey(data.AccuracyPenalty, CombineStrings(projectileData.UnitName(), dlmWord, nameof(ProjectileData), dlmWord, data.name, dlmWord, nameof(data.AccuracyPenalty)));
                    
                }
                
                //process target data 
                foreach (TargetDataConfig targetData in targetDataList)
                {
                    TargetData data = targetData.targetData;
                    data.MinUseRange = GetFloatByKey(data.MinUseRange, CombineStrings(targetData.UnitName(), dlmWord, nameof(TargetData), dlmWord, data.name, dlmWord, nameof(data.MinUseRange)));
                    data.MaxUseRange = GetFloatByKey(data.MaxUseRange, CombineStrings(targetData.UnitName(), dlmWord, nameof(TargetData), dlmWord, data.name, dlmWord, nameof(data.MaxUseRange)));
                    
                    //data.MustHaveTarget = GetBoolByKey(data.MustHaveTarget, CombineStrings(targetData.unitName(), dlmWord, nameof(targetData), dlmWord, data.name, dlmWord, nameof(data.MustHaveTarget)));
                }

                //process effect data TODO 
                //foreach (CastsDataConfig castData in castsDataList)
                //{
                //    Casts data = castData.castsData;
                //}

                //2nd effect pass
                //WriteDefaultUnitDataConfig();

                //process unit mods
                foreach (var unit in unitDataList)
                {
                    if (verboseLogging) Log(unit.name);
                    string baseSearchKey = CombineStrings(prefixUnit, dlmWord, unit.name);
                    unit.DefualtMaxHealth = GetIntByKey(unit.DefualtMaxHealth, CombineStrings(baseSearchKey, dlmWord, nameof(unit.DefualtMaxHealth)));
                    unit.DefaultHealthRegen = GetFloatByKey(unit.DefaultHealthRegen, CombineStrings(baseSearchKey, dlmWord, nameof(unit.DefaultHealthRegen)));
                    unit.Speed = GetFloatByKey(unit.Speed, CombineStrings(baseSearchKey, dlmWord, nameof(unit.Speed)));
                    unit.Armor = GetIntByKey(unit.Armor, CombineStrings(baseSearchKey, dlmWord, nameof(unit.Armor)));
                    unit.MagicResistance = GetIntByKey(unit.MagicResistance, CombineStrings(baseSearchKey, dlmWord, nameof(unit.MagicResistance)));
                    unit.Visionrange = GetIntByKey(unit.Visionrange, CombineStrings(baseSearchKey, dlmWord, nameof(unit.Visionrange)));
                    unit.XP = GetIntByKey(unit.XP, CombineStrings(baseSearchKey, dlmWord, nameof(unit.XP)));
                    unit.HousingUpkeep = GetIntByKey(unit.HousingUpkeep, CombineStrings(baseSearchKey, dlmWord, nameof(unit.HousingUpkeep)));
                    //unit cost data
                    if (unit.CreationAbility != null)
                    {
                        for (int i = 0; i < unit.CreationAbility.CostData.resources.Count; i++)
                        {
                            string resourceName = Utilities.GetSanitizedResourceName(unit.CreationAbility.CostData.resources[i].resource.name);
                            string searchKey = CombineStrings(baseSearchKey, dlmWord, resourceName);
                            unit.CreationAbility.CostData.resources[i].amount = GetIntByKey(unit.CreationAbility.CostData.resources[i].amount, searchKey);
                        }
                    }
                    else
                    {
                        plugin.Log.LogWarning(unit.name + "Missing Creation Data");
                    }
                }

                //foreach (var item in buildingDataList)
                //{
                //    plugin.Log.LogInfo(item.name);
                //}

                //saule marauders
                if (bool.TryParse(GetValue("AddMarauderToSauleWarcamp"), out boolValue) && boolValue == true)
                {
                    //factionsData[0].DefaultUpgrades[0].AddActions = factionsData[1].DefaultUpgrades[1].AddActions;
                    ActionData newData1 = factionsData[1].DefaultUpgrades[1].AddActions[2];     //marauder
                    ActionData newData2 = factionsData[1].DefaultUpgrades[1].AddActions[4];     //axe upgrade

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

                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                Log(CombineStrings("Finished main menu mod setup in ", elapsedMs.ToString(), " ms"));
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
                        
                        string searchKey = CombineStrings(i.ToString(), dlmWord, comparisonSheet[i].dmgType.ToString(), dlmWord, defType.DefenseType.ToString());
                        defType.Precentage = GetFloatByKey(defType.Precentage, searchKey);
                    }
                }

                gameManager.CorpseUnitTime = GetFloatByKey(gameManager.CorpseUnitTime, nameof (gameManager.CorpseUnitTime));
                
                Log("Finished in-game mod setup");
                initializedInGame = true;
            }

            #region Data Collection

            private void ClearDataCache()
            {
                unitDataList.Clear();
                heroDataList.Clear();
                actionDataList.Clear();
                buildingDataList.Clear();
                projectileDataList.Clear();
                castsDataList.Clear();
                effectDataList.Clear();
                targetDataList.Clear();
            }

            private void CollectDataFromActions()
            {
                //collect targetData
                CollectTargetDataFromActions();

                //collect castData
                CollectCastsData();

                //collect effect & projectileData
                CollectEffectAndProjectileData();

                //2nd pass for targetdata since they can be chained for aoe etc
                CollectTargetDataFromProjectiles();

                //2nd targetdata pass
                CollectCastsData(true);

                //2nd castdata pass
                CollectEffectAndProjectileData(true);
            }

            private void CollectTargetDataFromActions()
            {
                foreach (var actionData in actionDataList)
                {
                    if (actionData.actionData.TargetData != null)
                    {
                        if (verboseLogging) Log(CombineStrings("CollectTargetDataFromActions", actionData.ownerUnit.name, dlmWord, actionData.actionData.name));
                        targetDataList.Add(new TargetDataConfig(actionData.actionData.TargetData, actionData.ownerUnit));
                    }
                    else
                    {
                        if (verboseLogging) Log(CombineStrings(actionData.actionData.name, " has no target data"));
                    }
                    //actionData.actionData.CostData
                    //actionData.actionData.TargetData.CastTarget
                    //actionData.actionData.TargetData.Ranged
                    //actionData.actionData.CoolDown
                }
            }

            private void CollectSpawnedUnitDataFromActions()
            {
                foreach (var actionData in actionDataList)
                {
                    if (actionData.actionData.CreationData != null)
                    {
                        //get units created by abilities
                        foreach (var creationData in actionData.actionData.CreationData.Creation)
                        {
                            if (creationData.GetComponent<Unit>() != null)
                            {
                                var createdUnit = creationData.GetComponent<Unit>().DataUnit;
                                if (!unitDataList.Contains(createdUnit))
                                {
                                    unitDataList.Add(createdUnit);
                                }
                            }
                        }
                    }
                }
            }

            private void CollectTargetDataFromProjectiles()
            {
                foreach (var projectileData in projectileDataList)
                {
                    if (verboseLogging) Log(CombineStrings(projectileData.ownerUnit.name, dlmWord, projectileData.projectileData.name));
                    if (projectileData.projectileData.TarData != null)
                    {
                        targetDataList.Add(new TargetDataConfig(projectileData.projectileData.TarData, projectileData.ownerUnit));
                    }
                }
            }

            private void CollectProjectileData()
            {

            }

            private void CollectCastsData(bool logging = false)
            {
                foreach (var targetData in targetDataList)
                {
                    if (logging && verboseLogging) Log(CombineStrings(targetData.ownerUnit.name, dlmWord, targetData.targetData.name));
                    for (int i = 0; i < targetData.targetData.CastTarget.Count; i++)
                    {
                        if (!castsDataList.Any(x => x.castsData == targetData.targetData.CastTarget[i]))
                        {
                            castsDataList.Add(new CastsDataConfig(targetData.targetData.CastTarget[i], targetData.ownerUnit));
                        }
                    }
                    for (int i = 0; i < targetData.targetData.CastSelf.Count; i++)
                    {
                        if (!castsDataList.Any(x => x.castsData == targetData.targetData.CastSelf[i]))
                        {
                            castsDataList.Add(new CastsDataConfig(targetData.targetData.CastSelf[i], targetData.ownerUnit));
                        }
                    }
                    for (int i = 0; i < targetData.targetData.CastProjecitle.Count; i++)
                    {
                        if (!castsDataList.Any(x => x.castsData == targetData.targetData.CastProjecitle[i]))
                        {
                            castsDataList.Add(new CastsDataConfig(targetData.targetData.CastProjecitle[i], targetData.ownerUnit));
                        }
                    }
                }
            }

            private void CollectEffectAndProjectileData(bool logging = false)
            {
                foreach (var castData in castsDataList)
                {
                    if (castData.castsData.EffectDat != null && !effectDataList.Any(x => x.effectData == castData.castsData.EffectDat))
                    {
                        effectDataList.Add(new EffectDataConfig(castData.castsData.EffectDat, castData.ownerUnit));
                    }
                    if (castData.castsData.ProjectileData != null && !projectileDataList.Any(x => x.projectileData == castData.castsData.ProjectileData))
                    {
                        if (logging && verboseLogging) Log(CombineStrings(castData.ownerUnit.name, dlmWord, castData.castsData.ProjectileData.name));
                        //Log("added more projectiles in 2nd cast pass");
                        projectileDataList.Add(new ProjectileDataConfig(castData.castsData.ProjectileData, castData.ownerUnit));
                    }
                }

                if (logging)
                {
                    foreach (var effectData in effectDataList)
                    {
                        if (verboseLogging) Log(CombineStrings(effectData.ownerUnit.name, dlmWord, effectData.effectData.name));
                    }
                }
            }

            #endregion

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
                    if(verboseLogging) Log(CombineStrings("Failed to parse Float: ", key, dlmKey, originalFloat.ToString()));
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
                    if (verboseLogging) Log(CombineStrings("Failed to parse Int: ", key, dlmKey, originalInt.ToString()));
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
                    if (verboseLogging) Log(CombineStrings("Failed to parse Bool: ", key, dlmKey, originalBool.ToString()));
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
                    Log(new StringBuilder(i.ToString()).Append(dlmList).Append(comparisonSheet[i].dmgType).ToString());
                    foreach (var defType in comparisonSheet[i].defType)
                    {
                        Log(new StringBuilder(defType.DefenseType.ToString()).Append(dlmList).Append(defType.Precentage).ToString());
                        damageTypeLines.Add(new StringBuilder(i.ToString()).Append(dlmWord).Append(comparisonSheet[i].dmgType).Append(dlmWord).Append(defType.DefenseType.ToString()).Append(dlmKey).Append(defType.Precentage).ToString());
                    }
                }
                Utilities.WriteConfig(modRootPath + generatedConfigFolderPath + "DefaultDamageTypesConfig.txt", damageTypeLines);
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
                    factionDataLines.Add(CombineStrings(factionName, dlmWord, nameof(heroData.DefualtMaxHealth), dlmKey, heroData.DefualtMaxHealth.ToString()));
                    factionDataLines.Add(CombineStrings(factionName, dlmWord, nameof(heroData.DefaultHealthRegen), dlmKey, heroData.DefaultHealthRegen.ToString()));
                    factionDataLines.Add(CombineStrings(factionName, dlmWord, nameof(heroData.Speed), dlmKey, heroData.Speed.ToString()));
                    factionDataLines.Add(CombineStrings(factionName, dlmWord, nameof(heroData.Armor), dlmKey, heroData.Armor.ToString()));
                    factionDataLines.Add(CombineStrings(factionName, dlmWord, nameof(heroData.MagicResistance), dlmKey, heroData.MagicResistance.ToString()));
                    factionDataLines.Add(CombineStrings(factionName, dlmWord, nameof(heroData.Visionrange), dlmKey, heroData.Visionrange.ToString()));
                    factionDataLines.Add(CombineStrings(factionName, dlmWord, nameof(heroData.XP), dlmKey, heroData.XP.ToString()));

                    for (int k = 0; k < factionsData[i].Construction.Length; k++)
                    {
                        string buildingName = factionsData[i].Construction[k].name;
                        if (!modifiedBuildingsList.Contains(buildingName))
                        {
                            modifiedBuildingsList.Add(buildingName);
                            for (int j = 0; j < factionsData[i].Construction[k].CostData.resources.Length; j++)
                            {
                                string resourceName = Utilities.GetSanitizedResourceName(factionsData[i].Construction[k].CostData.resources[j].resource.name);
                                int resourceQuantity = factionsData[i].Construction[k].CostData.resources[j].amount;
                                string searchKey = CombineStrings(buildingName, dlmWord, resourceName, dlmKey, resourceQuantity.ToString());
                                factionDataLines.Add(searchKey);
                            }
                        }
                    }
                }
                Utilities.WriteConfig(modRootPath + generatedConfigFolderPath + "DefaultFactionDataConfig.txt", factionDataLines);
            }

            private void WriteDefaultUnitDataConfig()
            {
                List<string> unitNames = new List<string>();
                List<string> unitDataLines = new List<string>();

                foreach (var actionData in actionDataList)
                {
                    if (!unitNames.Contains(actionData.ownerUnit.name))
                    {
                        unitNames.Add(actionData.ownerUnit.name);
                    }
                }

                actionDataList = actionDataList.OrderBy(x => x.UnitName()).ToList();
                effectDataList = effectDataList.OrderBy(x => x.UnitName()).ToList();
                projectileDataList = projectileDataList.OrderBy(x => x.UnitName()).ToList();
                targetDataList = targetDataList.OrderBy(x => x.UnitName()).ToList();
                unitDataList = unitDataList.OrderBy(x => x.name).ToList();

                foreach (var unitName in unitNames)
                {
                    string baseSearchKey = CombineStrings(prefixUnit, dlmWord, unitName, dlmWord);
                    foreach (var unit in unitDataList.Where(x => x.name == unitName))
                    {
                        unitDataLines.Add(CombineStrings(dlmNewLine, dlmComment, unit.name));
                        unitDataLines.Add(CombineStrings(baseSearchKey, nameof(unit.DefualtMaxHealth), dlmKey, unit.DefualtMaxHealth.ToString()));
                        unitDataLines.Add(CombineStrings(baseSearchKey, nameof(unit.DefaultHealthRegen), dlmKey, unit.DefaultHealthRegen.ToString()));
                        unitDataLines.Add(CombineStrings(baseSearchKey, nameof(unit.Speed), dlmKey, unit.Speed.ToString()));
                        unitDataLines.Add(CombineStrings(baseSearchKey, nameof(unit.Armor), dlmKey, unit.Armor.ToString()));
                        unitDataLines.Add(CombineStrings(baseSearchKey, nameof(unit.MagicResistance), dlmKey, unit.MagicResistance.ToString()));
                        unitDataLines.Add(CombineStrings(baseSearchKey, nameof(unit.Visionrange), dlmKey, unit.Visionrange.ToString()));
                        unitDataLines.Add(CombineStrings(baseSearchKey, nameof(unit.XP), dlmKey, unit.XP.ToString()));
                        unitDataLines.Add(CombineStrings(baseSearchKey, nameof(unit.HousingUpkeep), dlmKey, unit.HousingUpkeep.ToString()));
                        //unit cost data
                        if (unit.CreationAbility != null)
                        {
                            for (int i = 0; i < unit.CreationAbility.CostData.resources.Count; i++)
                            {
                                string resourceName = Utilities.GetSanitizedResourceName(unit.CreationAbility.CostData.resources[i].resource.name);
                                string searchKey = CombineStrings(baseSearchKey, resourceName);
                                unitDataLines.Add(CombineStrings(searchKey, dlmKey, unit.CreationAbility.CostData.resources[i].amount.ToString()));
                            }
                        }
                        else
                        {
                            plugin.Log.LogWarning(unit.name + "Missing Creation Data");
                        }
                    }

                    unitDataLines.Add("");
                    foreach (ActionDataConfig actionData in actionDataList.Where(x => x.UnitName() == unitName))
                    {
                        ActionData data = actionData.actionData;
                        unitDataLines.Add(CombineStrings(baseSearchKey, nameof(ActionData), dlmWord, data.name, dlmWord, nameof(data.CoolDown), dlmKey, data.CoolDown.ToString()));
                        unitDataLines.Add(CombineStrings(baseSearchKey, nameof(ActionData), dlmWord, data.name, dlmWord, nameof(data.Duration), dlmKey, data.Duration.ToString()));
                        unitDataLines.Add(CombineStrings(baseSearchKey, nameof(ActionData), dlmWord, data.name, dlmWord, nameof(data.TriggerDelay), dlmKey, data.TriggerDelay.ToString()));
                        unitDataLines.Add(CombineStrings(baseSearchKey, nameof(ActionData), dlmWord, data.name, dlmWord, nameof(data.Charges), dlmKey, data.Charges.ToString()));
                        if (data.RandomizeData != null)
                        {
                            var randomizeData = data.RandomizeData;
                            unitDataLines.Add(CombineStrings(baseSearchKey, nameof(ActionData), dlmWord, data.name, dlmWord, "MultiShot", dlmWord, nameof(randomizeData.RepeatAmount), dlmKey, randomizeData.RepeatAmount.ToString()));
                            unitDataLines.Add(CombineStrings(baseSearchKey, nameof(ActionData), dlmWord, data.name, dlmWord, "MultiShot", dlmWord, nameof(randomizeData.RepeatIntervall), dlmKey, randomizeData.RepeatIntervall.ToString()));
                        }
                        if (data.CostData != null)
                        {
                            foreach (var resourceData in data.CostData.resources)
                            {
                                string resourceName = Utilities.GetSanitizedResourceName(resourceData.resource.name);
                                string searchKey = CombineStrings(baseSearchKey, nameof(ActionData), dlmWord, data.name, dlmWord, nameof(CostsData), dlmWord, resourceName);
                                unitDataLines.Add(CombineStrings(searchKey, dlmKey, resourceData.amount.ToString()));
                            }
                        }
                    }
                    foreach (EffectDataConfig effectData in effectDataList.Where(x => x.UnitName() == unitName))
                    {
                        EffectData data = effectData.effectData;
                        unitDataLines.Add(CombineStrings(baseSearchKey, nameof(EffectData), dlmWord, data.name, dlmWord, nameof(data.Damage), dlmKey, data.Damage.ToString()));
                        unitDataLines.Add(CombineStrings(baseSearchKey, nameof(EffectData), dlmWord, data.name, dlmWord, nameof(data.ScaleWithStrenght), dlmKey, data.ScaleWithStrenght.ToString()));
                        unitDataLines.Add(CombineStrings(baseSearchKey, nameof(EffectData), dlmWord, data.name, dlmWord, nameof(data.ScaleWithPower), dlmKey, data.ScaleWithPower.ToString()));
                        unitDataLines.Add(CombineStrings(baseSearchKey, nameof(EffectData), dlmWord, data.name, dlmWord, nameof(data.LifestealPercentrage), dlmKey, data.LifestealPercentrage.ToString()));
                    }
                    foreach (ProjectileDataConfig projectileData in projectileDataList.Where(x => x.UnitName() == unitName))
                    {
                        ProjectileData data = projectileData.projectileData;
                        unitDataLines.Add(CombineStrings(baseSearchKey, nameof(ProjectileData), dlmWord, data.name, dlmWord, nameof(data.Homing), dlmKey, data.Homing.ToString()));
                        unitDataLines.Add(CombineStrings(baseSearchKey, nameof(ProjectileData), dlmWord, data.name, dlmWord, nameof(data.LifeTime), dlmKey, data.LifeTime.ToString()));
                        unitDataLines.Add(CombineStrings(baseSearchKey, nameof(ProjectileData), dlmWord, data.name, dlmWord, nameof(data.StartSpeed), dlmKey, data.StartSpeed.ToString()));
                        unitDataLines.Add(CombineStrings(baseSearchKey, nameof(ProjectileData), dlmWord, data.name, dlmWord, nameof(data.AccuracyPenalty), dlmKey, data.AccuracyPenalty.ToString()));
                    }
                    foreach (TargetDataConfig targetData in targetDataList.Where(x => x.UnitName() == unitName))
                    {
                        TargetData data = targetData.targetData;
                        unitDataLines.Add(CombineStrings(baseSearchKey, nameof(TargetData), dlmWord, data.name, dlmWord, nameof(data.MinUseRange), dlmKey, data.MinUseRange.ToString()));
                        unitDataLines.Add(CombineStrings(baseSearchKey, nameof(TargetData), dlmWord, data.name, dlmWord, nameof(data.MaxUseRange), dlmKey, data.MaxUseRange.ToString()));
                    }
                }

                Utilities.WriteConfig(modRootPath + generatedConfigFolderPath + "DefaultUnitDataConfig.txt", unitDataLines);
            }

            #endregion

            /// <summary>
            /// Read config text file from mod folder
            /// </summary>
            public void ReadModConfig()
            {
                Log(dataConfigPath);
                datalines = null;
                StreamReader reader = new StreamReader(dataConfigPath, true);
                datalines = reader.ReadToEnd().Split('\n');
                reader.Close();

                reader = new StreamReader(modSettingsPath, true);
                settingslines = reader.ReadToEnd().Split("\n");
                reader.Close();

                datalines = datalines.Concat(settingslines).ToArray();

                //master disable switch field
                if (bool.TryParse(GetValue("DisableThisMod"), out bool boolVal))
                {
                    DisableModMasterSwitch = boolVal;
                }
            }

            /// <summary>
            /// Retrieve value from file
            /// </summary>
            public string GetValue(string key)
            {
                string[] linesToCheck;
                linesToCheck = datalines;

                foreach (string line in linesToCheck)
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
                //Log(CombineStrings("Failed to find key: ", key));
                return null;
            }

            private void Log(string logString, int level = 1)
            {
                if (level == 1)
                {
                    plugin.Log.LogInfo(logString);
                }
                else if (level == 2)
                {
                    plugin.Log.LogWarning(logString);
                }
                else if (level == 3)
                {
                    plugin.Log.LogError(logString);
                }
            }

        }

    }

}