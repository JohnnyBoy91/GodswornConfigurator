using System;
using System.Collections.Generic;
using System.Text;
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
        }
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

    #endregion
}
