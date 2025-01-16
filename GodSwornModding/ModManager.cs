using System;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using System.Collections;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using static JCGodSwornConfigurator.Utilities;
using System.Reflection;
using System.Text.Json;
using UniverseLib;
using UnityEngine.SceneManagement;

namespace JCGodSwornConfigurator
{
    [BepInPlugin("JCGodSwornConfigurator", "GodSwornConfigurator", "1.0.04")]
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

            var harmony = new Harmony("JCGodSwornConfigurator");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Universe.Init();
        }
        #endregion

        #region HarmonyPatches
        [HarmonyPatch(typeof(DataManagerUI), "Start")]
        static class ModDataManagerUI
        {
            [HarmonyPriority(100)]
            private static void Postfix(DataManagerUI __instance)
            {
                ModManager.Log("DataManagerUIInjected");
                ModManager.Instance.dataManagerUI = __instance;
            }
        }
        #endregion

        public class ModManager : MonoBehaviour
        {
            private static ModManager instance;

            public string dataConfigPath;
            public string modSettingsPath;
            public string modRootPath;
            public string[] datalines;
            public string[] settingslines;

            public bool DisableModMasterSwitch;
            public bool RetrieveDataMode;
            public bool treidenCommanderModeEnabled;
            public bool ShowUIWidget;

            public Plugin plugin;

            private readonly ModSpectatorMode modSpectatorMode = new ModSpectatorMode();
            private readonly HandleWaveManager handleWaveManager = new HandleWaveManager();
            public readonly TreidenCommanderModData treidenCommanderModData = new TreidenCommanderModData();

            public GameManager gameManager;
            public DataManager dataManager;
            public DamageManager damageManager;
            public DataManagerUI dataManagerUI;

            public FactionDatabase FactionsDatabase => dataManager.FactionsOptions;

            public Dictionary<string, GameObject> unitGOs;
            public List<UnitData> unitDataList = new List<UnitData>();
            public List<BuildingData> buildingDataList = new List<BuildingData>();
            public List<HeroData> heroDataList = new List<HeroData>();
            public List<ActionDataConfig> actionDataList = new List<ActionDataConfig>();
            public List<EffectDataConfig> effectDataList = new List<EffectDataConfig>();
            public List<TargetDataConfig> targetDataList = new List<TargetDataConfig>();
            public List<CastsDataConfig> castsDataList = new List<CastsDataConfig>();
            public List<ProjectileDataConfig> projectileDataList = new List<ProjectileDataConfig>();
            public List<CreationDataConfig> creationDataList = new List<CreationDataConfig>();
            public List<DivineSkillData> divineSkillDataList = new List<DivineSkillData>();

            public List<ActionDataBlueprint> actionDataTemplates = new List<ActionDataBlueprint>();
            public List<DivineSkillTreeDataBlueprint> moddedDivineSkillTreeData = new List<DivineSkillTreeDataBlueprint>();

            public List<string> actionDataNameLink = new List<string>();

            private bool verboseLogging;
            public static bool VerboseLogging() => Instance.verboseLogging;

            //main menu initialization
            private bool initialized;
            //initialization part 2 because some managers & values exist in game scene only
            private bool initializedInGame;
            //wait before modding datamanager
            private int waitFrames = 0;

            public const string prefixUnit = "Unit";
            public const string prefixHero = "Hero";
            public const string prefixHeroSkill = "DivineSkill";
            public const string prefixRPGSkill = "RPGModeSkill";
            public const string prefixSpawnedUnit = "SpawnedUnit";

            //config delimiters
            public const string dlmList = ", ";
            public const string dlmWord = "_";
            public const string dlmKey = ":";
            public const string dlmComment = "//";
            public const string dlmNewLine = "\n";
            public const string generatedConfigFolderPath = @"DefaultConfigData\";
            public const string scenarioFolderPath = @"Scenarios\";

            public const int factionCount = 5;

            public static ModManager Instance
            {
                get
                {
                    return instance;
                }
            }

            internal void Awake()
            {
                instance = this;
            }

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

                if (initializedInGame && treidenCommanderModeEnabled && SceneManager.GetActiveScene().name == "MainMenu" && dataManager.GetCurrentMap().MapName.key == "$GaurdiansOfTreiden" && HandleWaveManager.TreidenData.init)
                {
                    treidenCommanderModData.ResetData();
                    HandleWaveManager.TreidenData.ResetData();
                    initTreiden = false;
                }

                //hotkey for dev commands
                if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyUp(KeyCode.F10))
                {
                    ShowUIWidget = !ShowUIWidget;
                }
            }

            private string consoleString;
            void OnGUI()
            {
                if (ShowUIWidget)
                {
                    GUI.Box(new Rect(10, 10, 600, 400), "Godsworn Configurator");

                    consoleString = GUI.TextField(new Rect(20, 130, 200, 20), consoleString);
                    if (consoleString == "dv")
                    {
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

                    GUI.Label(new Rect(20, 100, 200, 20), "Defense of Tervete");
                    GUI.Label(new Rect(240, 100, 220, 20), "No Modded Scenario Loaded");
                }

                if (treidenCommanderModeEnabled && dataManager.GetCurrentMap().MapName.key == "$GaurdiansOfTreiden" && SceneManager.GetActiveScene().name != "MainMenu" && HandleWaveManager.TreidenData.init)
                {
                    Vector2 panelSize = new Vector2(320, 400);
                    Vector2 panelPosition = new Vector2(Screen.width - panelSize.x, Screen.height - panelSize.y);
                    Vector2 buttonSize = new Vector2(40, 40);
                    GUI.Box(new Rect(panelPosition, new Vector2(panelSize.x, panelSize.y)), "Your Army Wave");

                    if (HandleWaveManager.TreidenData.playerTeam == 0)
                    {
                        if (gameManager.ParticipantMgrs[HandleWaveManager.TreidenData.playerID].Diplomatic_Ties[6] == DiplomacyState.Allied)
                        {
                            HandleWaveManager.TreidenData.playerTeam = 1;
                            InitTreidenFaction();
                        }
                        if (gameManager.ParticipantMgrs[HandleWaveManager.TreidenData.playerID].Diplomatic_Ties[7] == DiplomacyState.Allied)
                        {
                            HandleWaveManager.TreidenData.playerTeam = 2;
                            InitTreidenFaction();
                        }
                    }

                    string teamString = "Pick a Team";
                    if (HandleWaveManager.TreidenData.playerTeam == 1) teamString = "Baltic";
                    if (HandleWaveManager.TreidenData.playerTeam == 2) teamString = "Order";

                    ParticipantManager playerParticipant = gameManager.ParticipantMgrs[HandleWaveManager.TreidenData.playerID];
                    int playerTechLevel = treidenCommanderModData.commanderDatas[0].techLevel;
                    int nextTechLevelCost = 150 + (playerTechLevel * 100);
                    int nextWealthLevelCost = -250 + (playerParticipant.Wealth.Increase * 20) - (HandleWaveManager.TreidenData.wavesSpawned * 10);
                    int nextFaithLevelCost = 50 + (playerParticipant.Faith.Increase * 5);

                    GUI.Label(new Rect(panelPosition, new Vector2(100, 30)), teamString);
                    if (treidenCommanderModData.playerWaveManager != null) GUI.Label(new Rect(new Vector2(panelPosition.x, panelPosition.y + 30), new Vector2(200, 30)), "Next Wave in " + 
                        (HandleWaveManager.TreidenData.waveInterval + (int)HandleWaveManager.TreidenData.lastWaveSpawnTime - (int)treidenCommanderModData.playerWaveManager.TimeSinceStart).ToString() + "seconds");

                    if (GUI.Button(new Rect(new Vector2(panelPosition.x, panelPosition.y + 60), new Vector2(160, 30)), "+Faith Income:" + nextFaithLevelCost))
                    {
                        if (playerParticipant.Wealth.amount >= nextFaithLevelCost)
                        {
                            playerParticipant.Faith.Increase += 2;
                            playerParticipant.Wealth.amount -= nextFaithLevelCost;
                        }
                    }
                    if (GUI.Button(new Rect(new Vector2(panelPosition.x + 160, panelPosition.y + 60), new Vector2(160, 30)), "+Wealth Income:" + nextWealthLevelCost))
                    {
                        if (playerParticipant.Wealth.amount >= nextWealthLevelCost)
                        {
                            playerParticipant.Wealth.Increase += 2;
                            playerParticipant.Wealth.amount -= nextWealthLevelCost;
                        }
                    }
                    if (treidenCommanderModData.commanderDatas[0].techLevel < 4)
                    {
                        if (GUI.Button(new Rect(new Vector2(panelPosition.x, panelPosition.y + 90), new Vector2(160, 30)), "+Tech Level:" + nextTechLevelCost))
                        {
                            if (playerParticipant.Wealth.amount >= nextTechLevelCost)
                            {
                                treidenCommanderModData.commanderDatas[0].techLevel++;
                                playerParticipant.Wealth.amount -= nextTechLevelCost;
                            }
                        }
                    }
                    //if (GUI.Button(new Rect(new Vector2(panelPosition.x + 160, panelPosition.y + 90), new Vector2(160, 30)), teamString))
                    //{

                    //}

                    if (HandleWaveManager.TreidenData.playerTeam != 0)
                    {
                        int columnCount = 1;
                        int yOffset = 30;
                        int unitButtonWidth = 160;
                        int rowIndex = 1;
                        int columnIndex = 0;
                        int k = 0;
                        foreach (var treidenUnitData in HandleWaveManager.TreidenData.playerTeam == 1 ? treidenCommanderModData.balticUnits : treidenCommanderModData.orderUnits)
                        {
                            k++;
                            if (k > treidenCommanderModData.commanderDatas[0].techLevel * 4 + (treidenCommanderModData.commanderDatas[0].techLevel == 3 ? 1 : 0)) break;
                            string buttonUnitName = treidenUnitData.Key;
                            if (buttonUnitName.Contains("Stardaughter - Lunar")) buttonUnitName = buttonUnitName.Replace("Stardaughter - Lunar", "Lunardaughter");
                            if (buttonUnitName.Contains("Stardaughter - Solar")) buttonUnitName = buttonUnitName.Replace("Stardaughter - Solar", "Solardaughter");
                            if (GUI.Button(new Rect(new Vector2(panelPosition.x + (unitButtonWidth * columnIndex), panelPosition.y + 120 + (yOffset * rowIndex)), new Vector2(unitButtonWidth, yOffset)), CombineStrings("(", treidenCommanderModData.commanderDatas[0].unitBuildDatas.Where(x => x.name == treidenUnitData.Key)?.First()?.quantityOwned.ToString(), ")", buttonUnitName, dlmKey, treidenUnitData.Value.ToString())))
                            {
                                if (gameManager.ParticipantMgrs[HandleWaveManager.TreidenData.playerID].Wealth.amount >= treidenCommanderModData.commanderDatas[0].unitBuildDatas.Where(x => x.name == treidenUnitData.Key)?.First().goldCost)
                                {
                                    treidenCommanderModData.commanderDatas[0].unitBuildDatas.Where(x => x.name == treidenUnitData.Key).First().quantityOwned++;
                                    gameManager.ParticipantMgrs[HandleWaveManager.TreidenData.playerID].Wealth.amount -= treidenCommanderModData.commanderDatas[0].unitBuildDatas.Where(x => x.name == treidenUnitData.Key).First().goldCost;
                                }
                            }

                            columnIndex++;
                            if (columnIndex > columnCount)
                            {
                                columnIndex = 0;
                                rowIndex++;
                            }
                        }
                    }
                }
            }

            bool initTreiden;
            private void InitTreidenFaction()
            {
                if (initTreiden) return;

                foreach (var item in HandleWaveManager.TreidenData.playerTeam == 1 ? treidenCommanderModData.balticUnits : treidenCommanderModData.orderUnits)
                {
                    treidenCommanderModData.commanderDatas[0].unitBuildDatas.Add(new TreidenCommanderModData.TreidenUnitBuildData(item.Key, item.Value, 0));
                }

                treidenCommanderModData.commanderDatas[1].isAI = true;
                treidenCommanderModData.commanderDatas[1].goldIncome = 24;
                treidenCommanderModData.commanderDatas[1].currentGoldAI = 500;
                foreach (var item in HandleWaveManager.TreidenData.playerTeam == 1 ? treidenCommanderModData.orderUnits : treidenCommanderModData.balticUnits)
                {
                    treidenCommanderModData.commanderDatas[1].unitBuildDatas.Add(new TreidenCommanderModData.TreidenUnitBuildData(item.Key, item.Value, 0));
                    treidenCommanderModData.commanderDatas[1].aiUnitWishList.Add(new TreidenCommanderModData.TreidenUnitBuildData(item.Key, item.Value, 0));
                }
                initTreiden = true;
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

            #region HarmonyPatches
            #endregion

            [Serializable]
            public class Loc
            {
                public Loc() { }
                public string key { get; set; }
                public string value { get; set; }
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

                if (bool.TryParse(GetValue("TreidenCommanderModeEnabled"), out boolValue) && boolValue == true)
                {
                    treidenCommanderModeEnabled = true;
                    treidenCommanderModData.commanderDatas.Clear();
                    treidenCommanderModData.commanderDatas.Add(new TreidenCommanderModData.CommanderData());
                    treidenCommanderModData.commanderDatas.Add(new TreidenCommanderModData.CommanderData());
                }

                if (DisableModMasterSwitch && !RetrieveDataMode)
                {
                    initialized = true;
                    return;
                }

                //foreach (var unitData in RuntimeHelper.FindObjectsOfTypeAll<UnitData>())
                //{
                //    unitDataList.Add(unitData);
                //}
                
                List<Loc> locDump = new List<Loc>();
                foreach (var item in LocalizationManager.localisedEN)
                {
                    Loc newLocEntry = new Loc
                    {
                        key = item.Key,
                        value = item.Value
                    };
                    locDump.Add(newLocEntry);
                }
                WriteJsonConfig(CombineStrings(modRootPath, generatedConfigFolderPath, "Loc.json"), locDump);

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

                foreach (var unitData in RuntimeHelper.FindObjectsOfTypeAll<UnitData>())
                {
                    Log(CombineStrings('"'.ToString(), unitData.name, '"'.ToString(), ","));
                    if (LocManager.legalUnitNames.Contains(unitData.name)) unitDataList.Add(unitData);
                    //if (unitData.name == "Bear" || unitData.name == "BearSlayer") unitDataList.Add(unitData);
                }

                List<string> modifiedBuildingsList = new List<string>();

                for (int i = 0; i < factionCount; i++)
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

                    //get hero skills data from factions
                    foreach (var divineSkillPairs in factionsData[i].DivineSkills)
                    {
                        foreach (var divineSkill in divineSkillPairs.PossibleAbilitiesLvl)
                        {
                            if (!divineSkillDataList.Contains(divineSkill)) divineSkillDataList.Add(divineSkill);
                            foreach (var upgrade in divineSkill.upgrades)
                            {
                                foreach (var action in upgrade.AddActions)
                                {
                                    ActionDataConfig newActionData = new ActionDataConfig(action, heroData, true, false, divineSkill)
                                    { refDivineSkillData = divineSkill };
                                    if (!actionDataList.Any(x => x.actionData == newActionData.actionData))
                                    {
                                        actionDataList.Add(newActionData);
                                    }
                                }
                            }
                        }
                    }
                    //get rpg hero skills data from factions
                    foreach (var divineSkillPairs in factionsData[i].DivineSkills_RPG)
                    {
                        foreach (var divineSkill in divineSkillPairs.PossibleAbilitiesLvl)
                        {
                            if (!divineSkillDataList.Contains(divineSkill)) divineSkillDataList.Add(divineSkill);
                            foreach (var upgrade in divineSkill.upgrades)
                            {
                                foreach (var action in upgrade.AddActions)
                                {
                                    ActionDataConfig newActionData = new ActionDataConfig(action, heroData, true, true, divineSkill)
                                    { refDivineSkillData = divineSkill };
                                    if (!actionDataList.Any(x => x.actionData == newActionData.actionData))
                                    {
                                        actionDataList.Add(newActionData);
                                    }
                                }
                            }
                        }
                    }

                }

                TryMethod(HandleCustomDivineSkillTree);

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
                    string baseSearchKey = CombineStrings(actionData.UnitName(), dlmWord);
                    if (actionData.rpgSkill) baseSearchKey = CombineStrings(baseSearchKey, prefixRPGSkill, dlmWord);

                    data.CoolDown = GetFloatByKey(data.CoolDown, CombineStrings(baseSearchKey, nameof(ActionData), dlmWord, data.name, dlmWord, nameof (data.CoolDown)));
                    data.Duration = GetFloatByKey(data.Duration, CombineStrings(baseSearchKey, nameof(ActionData), dlmWord, data.name, dlmWord, nameof(data.Duration)));
                    data.TriggerDelay = GetFloatByKey(data.TriggerDelay, CombineStrings(baseSearchKey, nameof(ActionData), dlmWord, data.name, dlmWord, nameof(data.TriggerDelay)));
                    data.Charges = GetIntByKey(data.Charges, CombineStrings(baseSearchKey, nameof(ActionData), dlmWord, data.name, dlmWord, nameof(data.Charges)));
                    if (data.RandomizeData != null)
                    {
                        var randomizeData = data.RandomizeData;
                        randomizeData.RepeatAmount = GetIntByKey(randomizeData.RepeatAmount, CombineStrings(baseSearchKey, nameof(ActionData), dlmWord, data.name, dlmWord, "MultiShot", dlmWord, nameof(randomizeData.RepeatAmount)));
                        randomizeData.RepeatIntervall = GetFloatByKey(randomizeData.RepeatIntervall, CombineStrings(baseSearchKey, nameof(ActionData), dlmWord, data.name, dlmWord, "MultiShot", dlmWord, nameof(randomizeData.RepeatIntervall)));
                    }
                    if (data.CostData != null)
                    {
                        foreach (var resourceData in data.CostData.resources)
                        {
                            string resourceName = Utilities.GetSanitizedResourceName(resourceData.resource.name);
                            string searchKey = CombineStrings(baseSearchKey, nameof(ActionData), dlmWord, data.name, dlmWord, nameof(CostsData), dlmWord, resourceName);
                            resourceData.amount = GetIntByKey(resourceData.amount, searchKey);
                        }
                    }
                }

                //process effect data
                foreach (EffectDataConfig effectData in effectDataList)
                {
                    EffectData data = effectData.effectData;
                    string baseSearchKey = CombineStrings(effectData.UnitName(), dlmWord);
                    if (effectData.rpgSkill) baseSearchKey = CombineStrings(baseSearchKey, prefixRPGSkill, dlmWord);

                    data.Damage = GetIntByKey(data.Damage, CombineStrings(baseSearchKey, nameof(EffectData), dlmWord, data.name, dlmWord, nameof(data.Damage)));
                    data.ScaleWithStrenght = GetBoolByKey(data.ScaleWithStrenght, CombineStrings(baseSearchKey, nameof(EffectData), dlmWord, data.name, dlmWord, nameof(data.ScaleWithStrenght)));
                    data.ScaleWithPower = GetBoolByKey(data.ScaleWithPower, CombineStrings(baseSearchKey, nameof(EffectData), dlmWord, data.name, dlmWord, nameof(data.ScaleWithPower)));
                    data.LifestealPercentrage = GetFloatByKey(data.LifestealPercentrage, CombineStrings(baseSearchKey, nameof(EffectData), dlmWord, data.name, dlmWord, nameof(data.LifestealPercentrage)));
                    //TODO damage type
                }

                //process projectile data 
                foreach (ProjectileDataConfig projectileData in projectileDataList)
                {
                    ProjectileData data = projectileData.projectileData;
                    string baseSearchKey = CombineStrings(projectileData.UnitName(), dlmWord);
                    if (projectileData.rpgSkill) baseSearchKey = CombineStrings(baseSearchKey, prefixRPGSkill, dlmWord);

                    data.Homing = GetBoolByKey(data.Homing, CombineStrings(baseSearchKey, nameof(ProjectileData), dlmWord, data.name, dlmWord, nameof(data.Homing)));
                    data.LifeTime = GetFloatByKey(data.LifeTime, CombineStrings(baseSearchKey, nameof(ProjectileData), dlmWord, data.name, dlmWord, nameof(data.LifeTime)));
                    data.StartSpeed = GetFloatByKey(data.StartSpeed, CombineStrings(baseSearchKey, nameof(ProjectileData), dlmWord, data.name, dlmWord, nameof(data.StartSpeed)));
                    data.AccuracyPenalty = GetFloatByKey(data.AccuracyPenalty, CombineStrings(baseSearchKey, nameof(ProjectileData), dlmWord, data.name, dlmWord, nameof(data.AccuracyPenalty)));
                    
                }
                
                //process target data 
                foreach (TargetDataConfig targetData in targetDataList)
                {
                    TargetData data = targetData.targetData;
                    string baseSearchKey = CombineStrings(targetData.UnitName(), dlmWord);
                    if (targetData.rpgSkill) baseSearchKey = CombineStrings(baseSearchKey, prefixRPGSkill, dlmWord);

                    data.MinUseRange = GetFloatByKey(data.MinUseRange, CombineStrings(baseSearchKey, nameof(TargetData), dlmWord, data.name, dlmWord, nameof(data.MinUseRange)));
                    data.MaxUseRange = GetFloatByKey(data.MaxUseRange, CombineStrings(baseSearchKey, nameof(TargetData), dlmWord, data.name, dlmWord, nameof(data.MaxUseRange)));
                    
                    //data.MustHaveTarget = GetBoolByKey(data.MustHaveTarget, CombineStrings(targetData.unitName(), dlmWord, nameof(targetData), dlmWord, data.name, dlmWord, nameof(data.MustHaveTarget)));
                }

                //process creation data for spawned units

                unitGOs = new Dictionary<string, GameObject>();

                foreach (UnitData unitData in unitDataList.Concat(heroDataList))
                {
                    if (!unitGOs.ContainsKey(unitData.name))
                    {
                        if (verboseLogging) Log("Adding to unit GOs " + unitData.name, 1);
                        unitGOs.Add(unitData.name, unitData.Prefablink[0]);
                        if (unitData.Prefablink.Count > 1)
                        {
                            if (verboseLogging) Log(unitData.name + " contains more than one GO", 2);
                        }
                    }
                }



                foreach (CreationDataConfig creationData in creationDataList)
                {
                    CreationData data = creationData.creationData;
                    var creationObjects = data.Creation;
                    string unitName = GetValue(CombineStrings(creationData.BaseUnitKey(), nameof(CreationData), dlmWord, data.name, dlmWord, "SpawnedUnit"));
                    if (unitName != null)
                    {
                        if (unitGOs.ContainsKey(unitName))
                        {
                            var newUnit = unitGOs[unitName];
                            if (!data.Creation.Any(x => x.name == unitName))
                            {
                                if (newUnit.GetComponent<Unit>() != null)
                                {
                                    creationData.refActionData.IconUIData.ButtonIcon = newUnit.GetComponent<Unit>().DataUnit.Icon;
                                    if (creationData.refActionDataConfig != null && creationData.refActionDataConfig.refDivineSkillData != null)
                                    {
                                        creationData.refActionDataConfig.refDivineSkillData.Icon = newUnit.GetComponent<Unit>().DataUnit.Icon;
                                    }
                                }
                                for (int i = 0; i < data.Creation.Count; i++)
                                {
                                    if (data.Creation[i].GetComponent<Unit>() != null)
                                    {
                                        data.Creation[i] = newUnit;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (verboseLogging) Log("Unit GOs missing key " + unitName + "_", 1);
                        }
                    }
                    else
                    {
                        if (verboseLogging) Log("did not find " + unitName, 1);
                    }
                }

                //process cast data TODO 
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

                    //Il2CppStructArray<DamageManager.DefenseTypes> def = new Il2CppStructArray<DamageManager.DefenseTypes>(1);
                    //def[0] = (DamageManager.DefenseTypes)2;
                    //unit.DefenseType = unit.DefenseType.AddItem(def[0]).ToArray();

                    //unit cost data
                    if (unit.CreationAbility != null && unit.CreationAbility.CostData != null)
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

            private void HandleCustomDivineSkillTree()
            {
                bool rpgMode = false;
                if (RetrieveDataMode)
                {
                    moddedDivineSkillTreeData = new List<DivineSkillTreeDataBlueprint>();
                    //get defaults
                    rpgMode = false;
                    for (int i = 0; i < factionCount; i++)
                    {
                        DivineSkillTreeDataBlueprint newBlueprint = new DivineSkillTreeDataBlueprint();
                        FactionData faction = FactionsDatabase.Factions[i];
                        newBlueprint.factionName = faction.name;
                        newBlueprint.rpgModeSkillTree = rpgMode;
                        foreach (var divineSkillGroup in rpgMode ? faction.DivineSkills_RPG : faction.DivineSkills)
                        {
                            DivineSkillTreeDataBlueprint.DivineSkillGroup newSkillGroup = new DivineSkillTreeDataBlueprint.DivineSkillGroup { level = divineSkillGroup.Lvl };
                            foreach (var divineSkillName in divineSkillGroup.PossibleAbilitiesLvl)
                            {
                                newSkillGroup.divineSkillNames.Add(divineSkillName.name);
                            }
                            newBlueprint.divineSkillSets.Add(newSkillGroup);
                        }
                        moddedDivineSkillTreeData.Add(newBlueprint);
                        if (i == factionCount - 1 && !rpgMode)
                        {
                            rpgMode = true;
                            i = -1;
                        }
                    }
                    WriteJsonConfig(CombineStrings(modRootPath, generatedConfigFolderPath, "SkillTreeConfig.json"), moddedDivineSkillTreeData);
                }

                moddedDivineSkillTreeData = (List<DivineSkillTreeDataBlueprint>)ReadJsonConfig<List<DivineSkillTreeDataBlueprint>>(CombineStrings(modRootPath, "SkillTreeConfig.json"));

                if (DisableModMasterSwitch) return;

                rpgMode = false;
                for (int i = 0; i < factionCount; i++)
                {
                    FactionData faction = FactionsDatabase.Factions[i];
                    DivineSkillTreeDataBlueprint moddedSkillTree = moddedDivineSkillTreeData.Where(x => x.factionName == faction.name && x.rpgModeSkillTree == rpgMode).FirstOrDefault();
                    string factionName = faction.name;
                    HeroData heroData = faction.MainHero;

                    //divine skill tree
                    Il2CppReferenceArray<DivineSKillDataLvlGroups> newDivineSkillGroup = new Il2CppReferenceArray<DivineSKillDataLvlGroups>(moddedSkillTree.divineSkillSets.Count);
                    int k = 0;
                    foreach (var divineSkillGroup in moddedSkillTree.divineSkillSets)
                    {
                        DivineSKillDataLvlGroups newSkillGroup = new DivineSKillDataLvlGroups { Lvl = divineSkillGroup.level };
                        List<DivineSkillData> newDivineSkillData = new List<DivineSkillData>();
                        foreach (var divineSkillName in divineSkillGroup.divineSkillNames)
                        {
                            DivineSkillData skillPick = divineSkillDataList.Where(x => x.name == divineSkillName).FirstOrDefault();
                            if (skillPick != null && skillPick.upgrades != null && skillPick.upgrades.Count > 0)
                            {
                                foreach (var upgrade in skillPick.upgrades)
                                {
                                    //add to hero or global, depending on skill type
                                    if (skillPick.SkillType == DivineSkillType.HeroAbility || skillPick.SkillType == DivineSkillType.HeroPassive)
                                    {
                                        heroData.PossibleUpgrades.Add(upgrade);
                                    }
                                    else
                                    {
                                        faction.PossibleUpgrades.Add(upgrade);
                                    }
                                }
                                newDivineSkillData.Add(skillPick);
                            }
                        }
                        newSkillGroup.PossibleAbilitiesLvl = newDivineSkillData.ToArray();
                        newDivineSkillGroup[k++] = newSkillGroup;
                    }

                    if (rpgMode)
                    {
                        faction.DivineSkills_RPG = newDivineSkillGroup;
                    }
                    else
                    {
                        faction.DivineSkills = newDivineSkillGroup;
                    }
                    if (i == factionCount - 1 && !rpgMode)
                    {
                        rpgMode = true;
                        i = -1;
                    }
                }
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
                        targetDataList.Add(new TargetDataConfig(actionData.actionData.TargetData, actionData.ownerUnit, actionData.divineSkill, actionData.rpgSkill));
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
                        if (verboseLogging) Log("creationdata: " + actionData.actionData.name, 2);
                        if (!creationDataList.Any(x => x.creationData == actionData.actionData.CreationData))
                        {
                            CreationDataConfig newCreationData = new CreationDataConfig(actionData.actionData.CreationData, actionData.actionData, actionData.ownerUnit, actionData.divineSkill, actionData.rpgSkill)
                            { refActionDataConfig = actionData };
                            creationDataList.Add(newCreationData);
                        }
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
                        //todo buildings
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
                        targetDataList.Add(new TargetDataConfig(projectileData.projectileData.TarData, projectileData.ownerUnit, projectileData.divineSkill, projectileData.rpgSkill));
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
                            castsDataList.Add(new CastsDataConfig(targetData.targetData.CastTarget[i], targetData.ownerUnit, targetData.divineSkill, targetData.rpgSkill));
                        }
                    }
                    for (int i = 0; i < targetData.targetData.CastSelf.Count; i++)
                    {
                        if (!castsDataList.Any(x => x.castsData == targetData.targetData.CastSelf[i]))
                        {
                            castsDataList.Add(new CastsDataConfig(targetData.targetData.CastSelf[i], targetData.ownerUnit, targetData.divineSkill, targetData.rpgSkill));
                        }
                    }
                    for (int i = 0; i < targetData.targetData.CastProjecitle.Count; i++)
                    {
                        if (!castsDataList.Any(x => x.castsData == targetData.targetData.CastProjecitle[i]))
                        {
                            castsDataList.Add(new CastsDataConfig(targetData.targetData.CastProjecitle[i], targetData.ownerUnit, targetData.divineSkill, targetData.rpgSkill));
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
                        effectDataList.Add(new EffectDataConfig(castData.castsData.EffectDat, castData.ownerUnit, castData.divineSkill, castData.rpgSkill));
                    }
                    if (castData.castsData.ProjectileData != null && !projectileDataList.Any(x => x.projectileData == castData.castsData.ProjectileData))
                    {
                        if (logging && verboseLogging) Log(CombineStrings(castData.ownerUnit.name, dlmWord, castData.castsData.ProjectileData.name));
                        //Log("added more projectiles in 2nd cast pass");
                        projectileDataList.Add(new ProjectileDataConfig(castData.castsData.ProjectileData, castData.ownerUnit, castData.divineSkill, castData.rpgSkill));
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

                    //divine skill tree

                    factionDataLines.Add(CombineStrings(factionName, dlmWord, "TreeDepth", dlmKey, factionsData[i].DivineSkills.Count.ToString()));
                    for (int j = 0; j < factionsData[i].DivineSkills.Count; j++)
                    {
                        factionDataLines.Add(CombineStrings(factionName, dlmWord, nameof(DivineSKillDataLvlGroups.Lvl), dlmWord, j.ToString(), dlmKey, factionsData[i].DivineSkills[j].Lvl.ToString()));
                        factionDataLines.Add(CombineStrings(factionName, dlmWord, "ChoicesCount", dlmWord, j.ToString(), dlmKey, factionsData[i].DivineSkills[j].PossibleAbilitiesLvl?.Count.ToString()));
                        for (int k = 0; k < factionsData[i].DivineSkills[j].PossibleAbilitiesLvl.Count; k++)
                        {
                            factionDataLines.Add(CombineStrings(factionName, dlmWord, nameof(DivineSkillData), dlmWord, j.ToString(), dlmWord, k.ToString(), dlmKey, factionsData[i].DivineSkills[j].PossibleAbilitiesLvl[k].name));
                        }
                    }

                    //building data
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
                    if (!unitNames.Contains(actionData.UnitName()))
                    {
                        unitNames.Add(actionData.UnitName());
                    }
                }

                actionDataList = actionDataList.OrderBy(x => x.UnitName()).ToList();
                effectDataList = effectDataList.OrderBy(x => x.UnitName()).ToList();
                projectileDataList = projectileDataList.OrderBy(x => x.UnitName()).ToList();
                targetDataList = targetDataList.OrderBy(x => x.UnitName()).ToList();
                creationDataList = creationDataList.OrderBy(x => x.UnitName()).ToList();
                unitDataList = unitDataList.OrderBy(x => x.name).ToList();

                List<UnitDataBlueprint> unitDataBlueprints = new List<UnitDataBlueprint>();
                foreach (var unitName in unitNames)
                {
                    string baseUnitKey = CombineStrings(prefixUnit, dlmWord, unitName, dlmWord);
                    foreach (var unit in unitDataList.Where(x => x.name == unitName))
                    {
                        //UnitDataBlueprint unitBlueprint = new UnitDataBlueprint();
                        //unitBlueprint.key = unit.name;
                        //unitBlueprint.maxHealth = unit.DefualtMaxHealth;
                        //unitBlueprint.maxHealthRegen = unit.DefaultHealthRegen;
                        //unitBlueprint.speed = unit.Speed;
                        //unitBlueprint.armor = unit.Armor;
                        //unitBlueprint.magicResistance = unit.MagicResistance;
                        //unitBlueprint.visionRange = unit.Visionrange;
                        //unitBlueprint.xp = unit.XP;
                        //unitBlueprint.housingUpkeep = unit.HousingUpkeep;

                        //unitDataBlueprints.Add(unitBlueprint);
                        
                        unitDataLines.Add(CombineStrings(dlmNewLine, dlmComment, unit.name));
                        unitDataLines.Add(CombineStrings(baseUnitKey, nameof(unit.DefualtMaxHealth), dlmKey, unit.DefualtMaxHealth.ToString()));
                        unitDataLines.Add(CombineStrings(baseUnitKey, nameof(unit.DefaultHealthRegen), dlmKey, unit.DefaultHealthRegen.ToString()));
                        unitDataLines.Add(CombineStrings(baseUnitKey, nameof(unit.Speed), dlmKey, unit.Speed.ToString()));
                        unitDataLines.Add(CombineStrings(baseUnitKey, nameof(unit.Armor), dlmKey, unit.Armor.ToString()));
                        unitDataLines.Add(CombineStrings(baseUnitKey, nameof(unit.MagicResistance), dlmKey, unit.MagicResistance.ToString()));
                        unitDataLines.Add(CombineStrings(baseUnitKey, nameof(unit.Visionrange), dlmKey, unit.Visionrange.ToString()));
                        unitDataLines.Add(CombineStrings(baseUnitKey, nameof(unit.XP), dlmKey, unit.XP.ToString()));
                        unitDataLines.Add(CombineStrings(baseUnitKey, nameof(unit.HousingUpkeep), dlmKey, unit.HousingUpkeep.ToString()));
                        //unit cost data
                        if (unit.CreationAbility != null)
                        {
                            for (int i = 0; i < unit.CreationAbility.CostData.resources.Count; i++)
                            {
                                string resourceName = Utilities.GetSanitizedResourceName(unit.CreationAbility.CostData.resources[i].resource.name);
                                string searchKey = CombineStrings(baseUnitKey, resourceName);
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
                        string baseSearchKey = baseUnitKey;
                        if (actionData.rpgSkill) baseSearchKey = CombineStrings(baseUnitKey, prefixRPGSkill, dlmWord);

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
                        string baseSearchKey = baseUnitKey;
                        if (effectData.rpgSkill) baseSearchKey = CombineStrings(baseUnitKey, prefixRPGSkill, dlmWord);

                        unitDataLines.Add(CombineStrings(baseSearchKey, nameof(EffectData), dlmWord, data.name, dlmWord, nameof(data.Damage), dlmKey, data.Damage.ToString()));
                        unitDataLines.Add(CombineStrings(baseSearchKey, nameof(EffectData), dlmWord, data.name, dlmWord, nameof(data.ScaleWithStrenght), dlmKey, data.ScaleWithStrenght.ToString()));
                        unitDataLines.Add(CombineStrings(baseSearchKey, nameof(EffectData), dlmWord, data.name, dlmWord, nameof(data.ScaleWithPower), dlmKey, data.ScaleWithPower.ToString()));
                        unitDataLines.Add(CombineStrings(baseSearchKey, nameof(EffectData), dlmWord, data.name, dlmWord, nameof(data.LifestealPercentrage), dlmKey, data.LifestealPercentrage.ToString()));
                    }
                    foreach (ProjectileDataConfig projectileData in projectileDataList.Where(x => x.UnitName() == unitName))
                    {
                        ProjectileData data = projectileData.projectileData;
                        string baseSearchKey = baseUnitKey;
                        if (projectileData.rpgSkill) baseSearchKey = CombineStrings(baseUnitKey, prefixRPGSkill, dlmWord);

                        unitDataLines.Add(CombineStrings(baseSearchKey, nameof(ProjectileData), dlmWord, data.name, dlmWord, nameof(data.Homing), dlmKey, data.Homing.ToString()));
                        unitDataLines.Add(CombineStrings(baseSearchKey, nameof(ProjectileData), dlmWord, data.name, dlmWord, nameof(data.LifeTime), dlmKey, data.LifeTime.ToString()));
                        unitDataLines.Add(CombineStrings(baseSearchKey, nameof(ProjectileData), dlmWord, data.name, dlmWord, nameof(data.StartSpeed), dlmKey, data.StartSpeed.ToString()));
                        unitDataLines.Add(CombineStrings(baseSearchKey, nameof(ProjectileData), dlmWord, data.name, dlmWord, nameof(data.AccuracyPenalty), dlmKey, data.AccuracyPenalty.ToString()));
                    }
                    foreach (TargetDataConfig targetData in targetDataList.Where(x => x.UnitName() == unitName))
                    {
                        TargetData data = targetData.targetData;
                        string baseSearchKey = baseUnitKey;
                        if (targetData.rpgSkill) baseSearchKey = CombineStrings(baseUnitKey, prefixRPGSkill, dlmWord);

                        unitDataLines.Add(CombineStrings(baseSearchKey, nameof(TargetData), dlmWord, data.name, dlmWord, nameof(data.MinUseRange), dlmKey, data.MinUseRange.ToString()));
                        unitDataLines.Add(CombineStrings(baseSearchKey, nameof(TargetData), dlmWord, data.name, dlmWord, nameof(data.MaxUseRange), dlmKey, data.MaxUseRange.ToString()));
                    }
                    foreach (CreationDataConfig creationdata in creationDataList.Where(x => x.UnitName() == unitName))
                    {
                        CreationData data = creationdata.creationData;
                        if (data.Creation[0].GetComponent<Unit>() != null)
                        {
                            unitDataLines.Add(CombineStrings(creationdata.BaseUnitKey(), nameof(CreationData), dlmWord, data.name, dlmWord, prefixSpawnedUnit, dlmKey));
                        }
                    }
                }

                //WriteJsonConfig(CombineStrings(modRootPath, generatedConfigFolderPath, "UnitDataConfig.txt"), unitDataBlueprints);

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
            public static string GetValue(string key)
            {
                string[] linesToCheck;
                linesToCheck = Instance.datalines;

                foreach (string line in linesToCheck)
                {
                    //ignore commented lines
                    if (!line.Contains("//"))
                    {
                        if (line.Split(':')[0].Contains(key))
                        {
                            string value = line.Split(':')[1].TrimEnd();
                            return value;
                        }
                    }
                }
                if (VerboseLogging()) Log(CombineStrings("Failed to find key: ", key));
                return null;
            }

            public static void Log(string logString, int level = 1)
            {
                if (level == 1)
                {
                    Instance.plugin.Log.LogInfo(logString);
                }
                else if (level == 2)
                {
                    Instance.plugin.Log.LogWarning(logString);
                }
                else if (level == 3)
                {
                    Instance.plugin.Log.LogError(logString);
                }
            }

        }

    }

}