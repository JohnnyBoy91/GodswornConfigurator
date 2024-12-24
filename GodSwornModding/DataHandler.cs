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
        public string unitName()
        {
            return ownerUnit.name;
        }
    }

    public class ActionDataConfig : DataConfig
    {
        public ActionData actionData;

        public ActionDataConfig(ActionData actionData, UnitData ownerUnit)
        {
            this.actionData = actionData;
            this.ownerUnit = ownerUnit;
        }
    }
    public class TargetDataConfig : DataConfig
    {
        public TargetData targetData;
        public TargetDataConfig(TargetData targetData, UnitData ownerUnit)
        {
            this.targetData = targetData;
            this.ownerUnit = ownerUnit;
        }
    }
    public class EffectDataConfig : DataConfig
    {
        public EffectData effectData;
        public EffectDataConfig(EffectData effectData, UnitData ownerUnit)
        {
            this.effectData = effectData;
            this.ownerUnit = ownerUnit;
        }
    }
    public class CastsDataConfig : DataConfig
    {
        public Casts castsData;
        public CastsDataConfig(Casts castsData, UnitData ownerUnit)
        {
            this.castsData = castsData;
            this.ownerUnit = ownerUnit;
        }
    }
    public class ProjectileDataConfig : DataConfig
    {
        public ProjectileData projectileData;
        public ProjectileDataConfig(ProjectileData projectileData, UnitData ownerUnit)
        {
            this.projectileData = projectileData;
            this.ownerUnit = ownerUnit;
        }
    }

    #endregion
}
