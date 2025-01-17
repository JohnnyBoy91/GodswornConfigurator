using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using UnityEngine;
using static JCGodSwornConfigurator.Plugin.ModManager;
using static JCGodSwornConfigurator.Utilities;

namespace JCGodSwornConfigurator
{
    internal class DataHandler
    {

    }

    #region Data Templates
    public class DataConfig
    {
        public UnitData ownerUnit;
        public bool divineSkill;
        public bool rpgSkill;

        public string UnitName()
        {
            if(ownerUnit == null)
            {
                Log("Data Config Missing Unit Name");
                return "";
            }
            return ownerUnit.name;
        }

        public string BaseUnitKey()
        {
            string baseKey = CombineStrings(prefixUnit, dlmWord, ownerUnit.name, dlmWord);
            if (rpgSkill)
            {
                baseKey = CombineStrings(baseKey, BaseRPGSKillKey());
            }
            return baseKey;
        }

        private string BaseRPGSKillKey()
        {
            string baseKey = CombineStrings(prefixRPGSkill, dlmWord);
            return baseKey;
        }
    }

    public class ActionDataConfig : DataConfig
    {
        public ActionData actionData;
        public DivineSkillData refDivineSkillData;

        public ActionDataConfig(ActionData actionData, UnitData ownerUnit, bool divineSkill = false, bool rpgSkill = false, DivineSkillData refDivineSkillData = null)
        {
            this.actionData = actionData;
            this.ownerUnit = ownerUnit;
            this.divineSkill = divineSkill;
            this.rpgSkill = rpgSkill;
            this.refDivineSkillData = refDivineSkillData;

            ActionDataBlueprint newDataTemplate = new ActionDataBlueprint
            {
                charges = actionData.Charges,
                triggerDelay = actionData.TriggerDelay
            };
            Instance.actionDataTemplates.Add(newDataTemplate);
        }

    }

    [Serializable]
    public class ActionDataBlueprint
    {
        [JsonInclude] public int charges;
        [JsonInclude] public float triggerDelay;
    }

    public class TargetDataConfig : DataConfig
    {
        public TargetData targetData;
        public TargetDataConfig(TargetData targetData, UnitData ownerUnit, bool divineSkill = false, bool rpgSkill = false)
        {
            this.targetData = targetData;
            this.ownerUnit = ownerUnit;
            this.divineSkill = divineSkill;
            this.rpgSkill = rpgSkill;
        }
    }
    public class EffectDataConfig : DataConfig
    {
        public EffectData effectData;
        public EffectDataConfig(EffectData effectData, UnitData ownerUnit, bool divineSkill = false, bool rpgSkill = false)
        {
            this.effectData = effectData;
            this.ownerUnit = ownerUnit;
            this.divineSkill = divineSkill;
            this.rpgSkill = rpgSkill;
        }
    }
    public class CastsDataConfig : DataConfig
    {
        public Casts castsData;
        public CastsDataConfig(Casts castsData, UnitData ownerUnit, bool divineSkill = false, bool rpgSkill = false)
        {
            this.castsData = castsData;
            this.ownerUnit = ownerUnit;
            this.divineSkill = divineSkill;
            this.rpgSkill = rpgSkill;
        }
    }
    public class ProjectileDataConfig : DataConfig
    {
        public ProjectileData projectileData;
        public ProjectileDataConfig(ProjectileData projectileData, UnitData ownerUnit, bool divineSkill = false, bool rpgSkill = false)
        {
            this.projectileData = projectileData;
            this.ownerUnit = ownerUnit;
            this.divineSkill = divineSkill;
            this.rpgSkill = rpgSkill;
        }
    }

    public class CreationDataConfig : DataConfig
    {
        public CreationData creationData;
        public ActionData refActionData;
        public ActionDataConfig refActionDataConfig;
        public CreationDataConfig(CreationData creationData, ActionData refActionData, UnitData ownerUnit, bool divineSkill = false, bool rpgSkill = false)
        {
            this.creationData = creationData;
            this.refActionData = refActionData;
            this.ownerUnit = ownerUnit;
            this.divineSkill = divineSkill;
            this.rpgSkill = rpgSkill;
        }
    }

    public class DivineSkillTreeDataConfig
    {
        public DivineSkillData divineSkillData;
        public DivineSKillDataLvlGroups divineSKillDataLvlGroups;
    }

    [Serializable]
    public class DivineSkillTreeDataBlueprint
    {
        public string factionName;
        public bool rpgModeSkillTree;
        public List<DivineSkillGroup> divineSkillSets = new List<DivineSkillGroup>();
        [Serializable]
        public class DivineSkillGroup
        {
            public int level;
            public List<string> divineSkillNames = new List<string>();
        }
    }

    [Serializable]
    public class UnitDataBlueprint
    {
        public string key;
        public int maxHealth;
        public float maxHealthRegen;
        public float speed;
        public int armor;
        public int magicResistance;
        public int visionRange;
        public int xp;
        public int housingUpkeep;

        public List<string> defenseTypes = new List<string>();

    }

    [Serializable]
    public class CostData
    {
        public int food;
        public int wood;
        public int faith;
        public int wealth;
    }

    [Serializable]
    public class WaveManagerBlueprint
    {
        public string mapKey;
        public string scenarioName = "TerveteMod";
        public float easyUnitMultiplier = 0.8f;
        public float hardUnitMultiplier = 1.2f;
        public float insaneUnitMultiplier = 1.5f;
        public bool revealUnitsOnMiniMap;
        //public bool revealInVision;

        public List<WaveConfig> waveConfigs = new List<WaveConfig>();

        //waves
        [Serializable]
        public class WaveConfig
        {
            public string waveName;
            public float spawnTimeSeconds;
            public float spawnTimeVarianceFactor = 0.05f;
            //public int unitCountMultiplier;
            public List<WaveUnitConfig> spawnUnitGroups = new List<WaveUnitConfig>();
        }

        [Serializable]
        public class WaveUnitConfig
        {
            public string unitNameKey;
            public int quantity;
            public float quantityVarianceFactor = 0f;
        }
    }

    public class TreidenCommanderModData
    {
        public List<CommanderData> commanderDatas = new List<CommanderData>();
        public WaveManagers playerWaveManager;

        public void ResetData()
        {
            playerWaveManager = null;
            commanderDatas.Clear();
            commanderDatas.Add(new CommanderData());
            commanderDatas.Add(new CommanderData());
        }

        public Dictionary<string, int> balticUnits = new Dictionary<string, int>()
        {
            {"Tribesman", 120 },
            {"Marauder", 95 },
            {"Skirmisher", 130 },
            {"Werewolf", 80 },
            {"Ranger", 140 },//t2
            {"Witch", 160 },
            {"Herbalist", 150 },
            {"Pukis", 180 },
            {"Warrior", 270 },//t3
            {"WolfWarrior" , 310 },
            {"Raider", 160 },
            {"Leshi" , 410 },
            {"Spigana" , 330 },
            {"Stardaughter - Lunar" , 580 },//t4
            {"Stardaughter - Solar" , 500 },
            {"Skybull" , 520 }
        };
        public Dictionary<string, int> orderUnits = new Dictionary<string, int>()
        {
            {"Militant", 90 },
            {"Marksman", 125 },
            {"Footman", 150 },
            {"Cherub", 300 },
            {"Zealot", 190 },//t2
            {"Nurse", 170 },
            {"Tracker", 190 },
            {"Rogue" , 100 },
            {"LongbowMan" , 180 },//t3
            {"Avenging Angel", 380 },
            {"Catapult", 330 },
            {"Cannon" , 420 },
            {"Knight", 400 },
            {"Blackknight" , 660 },//t4
            {"Paladin" , 620 }
        };

        public class CommanderData
        {
            public int techLevel = 1;
            public int goldIncome = 20;
            public int faithIncome = 10;
            public bool isAI = false;
            public int currentGoldAI = 500;
            public List<TreidenUnitBuildData> unitBuildDatas = new List<TreidenUnitBuildData>();
            public List<TreidenUnitBuildData> aiUnitWishList = new List<TreidenUnitBuildData>();

            //0 is not picked early, 2 is picked buildmid etc
            public int aiBuildOrderStage = 0;
            public treidenBuildOrderEarly aiBuildEarly;
            public treidenBuildOrderMid aiBuildMid;
            public treidenBuildOrderMidLate aiBuildMidLate;
            public treidenBuildOrderLate aiBuildLate;

            public treidenBuildBalticEarly aiBuildBalticEarly;
            public treidenBuildBalticMid aiBuildBalticMid;
            public treidenBuildBalticMidLate aiBuildBalticMidLate;
            public treidenBuildBalticLate aiBuildBalticLate;
        }

        public class TreidenUnitBuildData
        {
            public string name;
            public int goldCost;
            public int quantityOwned;
            public TreidenUnitBuildData(string name, int goldCost, int quantityOwned)
            {
                this.name = name;
                this.goldCost = goldCost;
                this.quantityOwned = quantityOwned;
            }
        }
        
        //Treiden Order
        public enum treidenBuildOrderEarly
        {
            Footmen_Xbow,
            Militant_Cherub,
            Footmen_Cherub,
            Rogue_Bow,
            Tracker_Cherub
        }
        public enum treidenBuildOrderMid
        {
            Zealot_Nurse,
            Tracker_Rogue,
            Longbows,
            HolyRush
        }
        public enum treidenBuildOrderMidLate
        {
            Angels,
            Knights,
            Artillery
        }
        public enum treidenBuildOrderLate
        {
            Paladin,
            BlackKnight
        }

        //Treiden Baltic
        public enum treidenBuildBalticEarly
        {
            Wolves,
            Tribe_Rauder,
            Mix,
            PukiRush,
            MassTribe
        }
        public enum treidenBuildBalticMid
        {
            Witches,
            RangedMix,
            Puki_Tribe
        }
        public enum treidenBuildBalticMidLate
        {
            WolfWarriors,
            Leshis,
            Raiders,
            Spiganas
        }
        public enum treidenBuildBalticLate
        {
            Bulls,
            Lunardaughters,
            Solardaughters,
            T3Mix
        }
    }

    #endregion
}
