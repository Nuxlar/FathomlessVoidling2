using EntityStates;
using RoR2;
using RoR2.Skills;
using RoR2.Projectile;
using UnityEngine;

namespace FathomlessVoidling.EntityStates.Primary
{
    public class FireEyeBlast : BaseState
    {
        public float baseInitialDelay = 0f;
        public float baseDelayBetweenWaves = 0.5f;
        public float baseEndDelay = 3f;
        public int numWaves = 5;
        public int numMissilesPerWave = 8; // 6 orig
        public string muzzleName = "EyeProjectileCenter";
        public GameObject muzzleFlashPrefab = Main.eyeBlastMuzzleFlash;
        public GameObject projectilePrefab = Main.eyeMissileProjectile;
        public float damageCoefficient = 2f;
        public float force = 1000f;
        public float minSpreadDegrees = 0f;
        public float rangeSpreadDegrees = 5f;
        public string fireWaveSoundString;
        public bool isSoundScaledByAttackSpeed;
        public string animationLayerName = "Gesture";
        public string animationStateName = "ChargeEyeBlast";
        public string animationPlaybackRateParam = "Eyeblast.playbackRate";
        public SkillDef skillDefToReplaceAtStocksEmpty;
        public SkillDef nextSkillDef;
        private float delayBetweenWaves;
        private float duration;
        private int numWavesFired;
        private float timeUntilNextWave;
        private Transform muzzleTransform;

        public override void OnEnter()
        {
            base.OnEnter();
            if ((bool)this.nextSkillDef)
            {
                GenericSkill skillByDef = this.skillLocator.FindSkillByDef(this.skillDefToReplaceAtStocksEmpty);
                if ((bool)skillByDef && skillByDef.stock == 0)
                    skillByDef.SetBaseSkill(this.nextSkillDef);
            }
            this.duration = (this.baseInitialDelay + Mathf.Max(0.0f, this.baseDelayBetweenWaves * (this.numWaves - 1)) + this.baseEndDelay) / this.attackSpeedStat;
            this.characterBody.SetAimTimer(this.duration + 3f);
            this.timeUntilNextWave = this.baseInitialDelay / this.attackSpeedStat;
            this.delayBetweenWaves = this.baseDelayBetweenWaves / this.attackSpeedStat;
            this.muzzleTransform = this.FindModelChild(this.muzzleName);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            this.timeUntilNextWave -= this.GetDeltaTime();
            while (this.timeUntilNextWave < 0.0 && this.numWavesFired < this.numWaves)
            {
                this.PlayAnimation(this.animationLayerName, this.animationStateName, this.animationPlaybackRateParam, this.duration);
                this.timeUntilNextWave += this.delayBetweenWaves;
                ++this.numWavesFired;
                // EffectManager.SimpleMuzzleFlash(this.muzzleFlashPrefab, this.gameObject, this.muzzleName, false);
                EffectManager.SpawnEffect(this.muzzleFlashPrefab, new EffectData()
                {
                    origin = this.muzzleTransform.position + new Vector3(0f, 50f, 0f),
                    rotation = this.muzzleTransform.rotation
                }, false);
                if (this.isAuthority)
                {
                    Quaternion quaternion = Util.QuaternionSafeLookRotation(this.GetAimRay().direction);
                    FireProjectileInfo fireProjectileInfo = new FireProjectileInfo()
                    {
                        projectilePrefab = this.projectilePrefab,
                        position = this.muzzleTransform.position + new Vector3(0f, 50f, 0f),
                        owner = this.gameObject,
                        damage = this.damageStat * this.damageCoefficient,
                        force = this.force
                    };
                    for (int index = 0; index < this.numMissilesPerWave; ++index)
                    {
                        fireProjectileInfo.rotation = quaternion * this.GetRandomRollPitch();
                        fireProjectileInfo.crit = Util.CheckRoll(this.critStat, this.characterBody.master);
                        ProjectileManager.instance.FireProjectile(fireProjectileInfo);
                    }
                }
            }
            if (!this.isAuthority || (double)this.fixedAge < this.duration)
                return;
            this.outer.SetNextStateToMain();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }

        protected Quaternion GetRandomRollPitch()
        {
            return Quaternion.AngleAxis(Random.Range(0, 360), Vector3.forward) * Quaternion.AngleAxis(this.minSpreadDegrees + Random.Range(0.0f, this.rangeSpreadDegrees), Vector3.left);
        }
    }
}