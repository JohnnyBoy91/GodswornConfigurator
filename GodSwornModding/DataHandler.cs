using System;
using System.Collections.Generic;
using System.Text;

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
        public string UnitName()
        {
            return ownerUnit.name;
        }
    }

    public class ActionDataConfig : DataConfig
    {
        public ActionData actionData;

        public ActionDataConfig(ActionData actionData, UnitData ownerUnit, bool divineSkill = false)
        {
            this.actionData = actionData;
            this.ownerUnit = ownerUnit;
            this.divineSkill = divineSkill;
        }
    }
    public class TargetDataConfig : DataConfig
    {
        public TargetData targetData;
        public TargetDataConfig(TargetData targetData, UnitData ownerUnit, bool divineSkill = false)
        {
            this.targetData = targetData;
            this.ownerUnit = ownerUnit;
            this.divineSkill = divineSkill;
        }
    }
    public class EffectDataConfig : DataConfig
    {
        public EffectData effectData;
        public EffectDataConfig(EffectData effectData, UnitData ownerUnit, bool divineSkill = false)
        {
            this.effectData = effectData;
            this.ownerUnit = ownerUnit;
            this.divineSkill = divineSkill;
        }
    }
    public class CastsDataConfig : DataConfig
    {
        public Casts castsData;
        public CastsDataConfig(Casts castsData, UnitData ownerUnit, bool divineSkill = false)
        {
            this.castsData = castsData;
            this.ownerUnit = ownerUnit;
            this.divineSkill = divineSkill;
        }
    }
    public class ProjectileDataConfig : DataConfig
    {
        public ProjectileData projectileData;
        public ProjectileDataConfig(ProjectileData projectileData, UnitData ownerUnit, bool divineSkill = false)
        {
            this.projectileData = projectileData;
            this.ownerUnit = ownerUnit;
            this.divineSkill = divineSkill;
        }
    }

    #endregion
}
