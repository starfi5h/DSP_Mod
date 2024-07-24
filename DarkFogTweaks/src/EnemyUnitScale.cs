using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Text;

namespace DarkFogTweaks
{
    public class EnemyUnitScale
    {
        static readonly List<EnemyUnitDesc> oEnemyUnitDescs = new();
        static readonly List<int> oModelIds = new();

        public static void ApplyAll()
        {
            //Plugin.Log.LogInfo("ApplyAll");
            oEnemyUnitDescs.Clear();
            oModelIds.Clear();
            foreach (var modelProto in LDB.models.dataArray)
            {
                if (modelProto.ObjectType != EObjectType.Enemy || !modelProto.prefabDesc.isEnemyUnit) continue;

                var enemyUnitDesc = new EnemyUnitDesc();
                BackUp(modelProto.prefabDesc, enemyUnitDesc);
                oModelIds.Add(modelProto.ID);
                oEnemyUnitDescs.Add(enemyUnitDesc);

                Apply(modelProto);
                //Print(modelProto);
            }
            Plugin.Log.LogInfo("EnemyUnit count: " + oModelIds.Count);
        }

        public static void RestoreAll()
        {
            for (int i = 0; i < oModelIds.Count; i++)
            {
                var modelProto = LDB.models.Select(oModelIds[i]);
                Restore(modelProto.prefabDesc, oEnemyUnitDescs[i]);
            }
        }

        static void BackUp(PrefabDesc prefabDesc, EnemyUnitDesc enemyUnitDesc)
        {
            enemyUnitDesc.maxMovementSpeed = prefabDesc.unitMaxMovementSpeed;
            enemyUnitDesc.maxMovementAcceleration = prefabDesc.unitMaxMovementAcceleration;
            enemyUnitDesc.marchMovementSpeed = prefabDesc.unitMarchMovementSpeed;
            enemyUnitDesc.assaultArriveRange = prefabDesc.unitAssaultArriveRange;
            enemyUnitDesc.engageArriveRange = prefabDesc.unitEngageArriveRange;
            enemyUnitDesc.sensorRange = prefabDesc.unitSensorRange;
            enemyUnitDesc.attackRange0 = prefabDesc.unitAttackRange0;
            enemyUnitDesc.attackInterval0 = prefabDesc.unitAttackInterval0;
            enemyUnitDesc.attackHeat0 = prefabDesc.unitAttackHeat0;
            enemyUnitDesc.attackDamage0 = prefabDesc.unitAttackDamage0;
            enemyUnitDesc.attackDamageInc0 = prefabDesc.unitAttackDamageInc0;
            enemyUnitDesc.attackRange1 = prefabDesc.unitAttackRange1;
            enemyUnitDesc.attackInterval1 = prefabDesc.unitAttackInterval1;
            enemyUnitDesc.attackHeat1 = prefabDesc.unitAttackHeat1;
            enemyUnitDesc.attackDamage1 = prefabDesc.unitAttackDamage1;
            enemyUnitDesc.attackDamageInc1 = prefabDesc.unitAttackDamageInc1;
            enemyUnitDesc.attackRange2 = prefabDesc.unitAttackRange2;
            enemyUnitDesc.attackInterval2 = prefabDesc.unitAttackInterval2;
            enemyUnitDesc.attackHeat2 = prefabDesc.unitAttackHeat2;
            enemyUnitDesc.attackDamage2 = prefabDesc.unitAttackDamage2;
            enemyUnitDesc.attackDamageInc2 = prefabDesc.unitAttackDamageInc2;
            enemyUnitDesc.coldSpeed = prefabDesc.unitColdSpeed;
            enemyUnitDesc.coldSpeedInc = prefabDesc.unitColdSpeedInc;
        }

        static void Restore(PrefabDesc prefabDesc, EnemyUnitDesc enemyUnitDesc)
        {
            prefabDesc.unitMaxMovementSpeed = enemyUnitDesc.maxMovementSpeed;
            prefabDesc.unitMaxMovementAcceleration = enemyUnitDesc.maxMovementAcceleration;
            prefabDesc.unitMarchMovementSpeed = enemyUnitDesc.marchMovementSpeed;
            prefabDesc.unitAssaultArriveRange = enemyUnitDesc.assaultArriveRange;
            prefabDesc.unitEngageArriveRange = enemyUnitDesc.engageArriveRange;
            prefabDesc.unitSensorRange = enemyUnitDesc.sensorRange;
            prefabDesc.unitAttackRange0 = enemyUnitDesc.attackRange0;
            prefabDesc.unitAttackInterval0 = enemyUnitDesc.attackInterval0;
            prefabDesc.unitAttackHeat0 = enemyUnitDesc.attackHeat0;
            prefabDesc.unitAttackDamage0 = enemyUnitDesc.attackDamage0;
            prefabDesc.unitAttackDamageInc0 = enemyUnitDesc.attackDamageInc0;
            prefabDesc.unitAttackRange1 = enemyUnitDesc.attackRange1;
            prefabDesc.unitAttackInterval1 = enemyUnitDesc.attackInterval1;
            prefabDesc.unitAttackHeat1 = enemyUnitDesc.attackHeat1;
            prefabDesc.unitAttackDamage1 = enemyUnitDesc.attackDamage1;
            prefabDesc.unitAttackDamageInc1 = enemyUnitDesc.attackDamageInc1;
            prefabDesc.unitAttackRange2 = enemyUnitDesc.attackRange2;
            prefabDesc.unitAttackInterval2 = enemyUnitDesc.attackInterval2;
            prefabDesc.unitAttackHeat2 = enemyUnitDesc.attackHeat2;
            prefabDesc.unitAttackDamage2 = enemyUnitDesc.attackDamage2;
            prefabDesc.unitAttackDamageInc2 = enemyUnitDesc.attackDamageInc2;
            prefabDesc.unitColdSpeed = enemyUnitDesc.coldSpeed;
            prefabDesc.unitColdSpeedInc = enemyUnitDesc.coldSpeedInc;
        }

        static ConfigEntry<float> hpMax;
        static ConfigEntry<float> hpInc;
        static ConfigEntry<float> hpRecover;
        static ConfigEntry<float> movementSpeed;
        static ConfigEntry<float> sensorRange;
        static ConfigEntry<float> engageRange;
        static ConfigEntry<float> attackRange;
        static ConfigEntry<float> attackDamage;
        static ConfigEntry<float> attackDamageInc;
        static ConfigEntry<float> attackCoolDownSpeed;
        static ConfigEntry<float> attackCoolDownSpeedInc;

        public static void LoadConfigs(ConfigFile configFile)
        {
            hpMax = configFile.Bind("EnemyUnitFactor", "HpMax", 1.0f);
            hpInc = configFile.Bind("EnemyUnitFactor", "HpInc", 1.0f, "Hp upgrade per level");
            hpRecover = configFile.Bind("EnemyUnitFactor", "HpRecover", 1.0f, "Hp recovery overtime");
            movementSpeed = configFile.Bind("EnemyUnitFactor", "MovementSpeed", 1.0f, "Including base speed, max speed and acceleration");
            sensorRange = configFile.Bind("EnemyUnitFactor", "SensorRange", 1.0f, "");
            engageRange = configFile.Bind("EnemyUnitFactor", "EngageRange", 1.0f, "The distance to engage and keep on firing");
            attackRange = configFile.Bind("EnemyUnitFactor", "AttackRange", 1.0f, "");
            attackDamage = configFile.Bind("EnemyUnitFactor", "AttackDamage", 1.0f, "");
            attackDamageInc = configFile.Bind("EnemyUnitFactor", "AttackDamageInc", 1.0f, "Damage upgrade per level");
            attackCoolDownSpeed = configFile.Bind("EnemyUnitFactor", "AttackCoolDownSpeed", 1.0f, "");
            attackCoolDownSpeedInc = configFile.Bind("EnemyUnitFactor", "AttackCoolDownSpeedInc", 1.0f, "Damage cooldown speed upgrade per level");
        }

        static void Apply(ModelProto modelProto)
        {
            var modelId = modelProto.ID;
            SkillSystem.HpMaxByModelIndex[modelId] = (int)(SkillSystem.HpMaxByModelIndex[modelId] * hpMax.Value + 0.5f);
            SkillSystem.HpUpgradeByModelIndex[modelId] = (int)(SkillSystem.HpUpgradeByModelIndex[modelId] * hpInc.Value + 0.5f);
            SkillSystem.HpRecoverByModelIndex[modelId] = (int)(SkillSystem.HpRecoverByModelIndex[modelId] * hpRecover.Value + 0.5f);

            var prefabDesc = modelProto.prefabDesc;

            prefabDesc.unitMaxMovementSpeed *= movementSpeed.Value;
            prefabDesc.unitMaxMovementAcceleration *= movementSpeed.Value;
            prefabDesc.unitMarchMovementSpeed *= movementSpeed.Value;
            
            prefabDesc.unitSensorRange *= sensorRange.Value;
            prefabDesc.unitAssaultArriveRange *= engageRange.Value;
            prefabDesc.unitEngageArriveRange *= engageRange.Value;

            prefabDesc.unitAttackRange0 *= attackRange.Value;
            prefabDesc.unitAttackRange1 *= attackRange.Value;
            prefabDesc.unitAttackRange2 *= attackRange.Value;
            prefabDesc.unitAttackDamage0 = (int)(prefabDesc.unitAttackDamage0 * attackDamage.Value + 0.5f);
            prefabDesc.unitAttackDamage1 = (int)(prefabDesc.unitAttackDamage1 * attackDamage.Value + 0.5f);
            prefabDesc.unitAttackDamage2 = (int)(prefabDesc.unitAttackDamage2 * attackDamage.Value + 0.5f);
            prefabDesc.unitAttackDamageInc0 = (int)(prefabDesc.unitAttackDamageInc0 * attackDamageInc.Value + 0.5f);
            prefabDesc.unitAttackDamageInc1 = (int)(prefabDesc.unitAttackDamageInc1 * attackDamageInc.Value + 0.5f);
            prefabDesc.unitAttackDamageInc2 = (int)(prefabDesc.unitAttackDamageInc2 * attackDamageInc.Value + 0.5f);

            prefabDesc.unitColdSpeed = (int)(prefabDesc.unitColdSpeed * attackCoolDownSpeed.Value + 0.5f);
            prefabDesc.unitColdSpeedInc = (int)(prefabDesc.unitColdSpeedInc * attackCoolDownSpeedInc.Value + 0.5f);
        }

        public static void Print(ModelProto modelProto)
        {
            var sb = new StringBuilder();

            sb.AppendLine("");            
            
            sb.AppendLine("Id: " + modelProto.ID.ToString());
            sb.AppendLine("name: " + modelProto.name);
            sb.AppendLine("displayName: " + modelProto.displayName);
            sb.AppendLine("HpMax: " + modelProto.HpMax);
            sb.AppendLine("HpRecover: " + modelProto.HpRecover);
            sb.AppendLine("HpUpgrade: " + modelProto.HpUpgrade);

            var desc = modelProto.prefabDesc;
            sb.AppendLine("unitMaxMovementSpeed: " + desc.unitMaxMovementSpeed);
            sb.AppendLine("unitMaxMovementAcceleration: " + desc.unitMaxMovementAcceleration);
            sb.AppendLine("unitMarchMovementSpeed: " + desc.unitMarchMovementSpeed);

            sb.AppendLine("unitAssaultArriveRange: " + desc.unitAssaultArriveRange);
            sb.AppendLine("unitEngageArriveRange : " + desc.unitEngageArriveRange);
            sb.AppendLine("unitSensorRange: " + desc.unitSensorRange);

            sb.AppendLine("unitAttackRange0: " + desc.unitAttackRange0);
            sb.AppendLine("unitAttackInterval0: " + desc.unitAttackInterval0);
            sb.AppendLine("unitAttackHeat0: " + desc.unitAttackHeat0);
            sb.AppendLine("unitAttackDamage0: " + desc.unitAttackDamage0);
            sb.AppendLine("unitAttackDamageInc0: " + desc.unitAttackDamageInc0);

            sb.AppendLine("unitAttackRange1: " + desc.unitAttackRange1);
            sb.AppendLine("unitAttackInterval1: " + desc.unitAttackInterval1);
            sb.AppendLine("unitAttackHeat1: " + desc.unitAttackHeat1);
            sb.AppendLine("unitAttackDamage1: " + desc.unitAttackDamage1);
            sb.AppendLine("unitAttackDamageInc1: " + desc.unitAttackDamageInc1);

            sb.AppendLine("unitAttackRange2: " + desc.unitAttackRange2);
            sb.AppendLine("unitAttackInterval2: " + desc.unitAttackInterval2);
            sb.AppendLine("unitAttackHeat2: " + desc.unitAttackHeat2);
            sb.AppendLine("unitAttackDamage2: " + desc.unitAttackDamage2);
            sb.AppendLine("unitAttackDamageInc2: " + desc.unitAttackDamageInc2);

            sb.AppendLine("unitColdSpeed: " + desc.unitColdSpeed);
            sb.AppendLine("unitColdSpeedInc: " + desc.unitColdSpeedInc);

            Plugin.Log.LogDebug(sb.ToString());
        }
    }
}
