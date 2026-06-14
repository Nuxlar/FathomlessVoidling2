using EntityStates;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using FathomlessVoidling.Controllers;

namespace FathomlessVoidling.EntityStates.Primary
{
    public class FireEyeBlast : BaseState
    {
        public float baseInitialDelay = 0f;
        public static float baseDelayBetweenWaves = 0.5f;
        public float baseEndDelay = 0f;
        public static int numWaves = 5;
        public static int numMissilesPerWave = ModConfig.eyeBlastMissileCount.Value;
        public static float startRingRadius = 2f;
        public static float endRingRadius = 6f;
        public string muzzleName = "EyeProjectileCenter";
        public GameObject muzzleFlashPrefab = Main.eyeBlastMuzzleFlash;
        public GameObject projectilePrefab = Main.eyeMissileProjectile;
        public float damageCoefficient = 1f;
        public float force = 1000f;
        public string animationLayerName = "Gesture";
        public string animationStateName = "ChargeEyeBlast";
        public string animationPlaybackRateParam = "Eyeblast.playbackRate";
        private float delayBetweenWaves;
        private float duration;
        private int numWavesFired = 0;
        private float timeUntilNextWave;
        private Transform muzzleTransform;

        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = (this.baseInitialDelay + Mathf.Max(0.0f, FireEyeBlast.baseDelayBetweenWaves * (FireEyeBlast.numWaves - 1)) + this.baseEndDelay) / this.attackSpeedStat;
            this.characterBody.SetAimTimer(this.duration + 3f);
            this.timeUntilNextWave = this.baseInitialDelay / this.attackSpeedStat;
            this.delayBetweenWaves = FireEyeBlast.baseDelayBetweenWaves / this.attackSpeedStat;
            this.muzzleTransform = this.FindModelChild(this.muzzleName);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            this.timeUntilNextWave -= this.GetDeltaTime();
            while (this.timeUntilNextWave < 0.0 && this.numWavesFired < FireEyeBlast.numWaves)
            {
                this.PlayAnimation(this.animationLayerName, this.animationStateName, this.animationPlaybackRateParam, this.duration);
                this.timeUntilNextWave += this.delayBetweenWaves;
                int waveIndex = this.numWavesFired;
                ++this.numWavesFired;
                EffectManager.SpawnEffect(this.muzzleFlashPrefab, new EffectData()
                {
                    origin = this.muzzleTransform.position,
                    rotation = this.muzzleTransform.rotation
                }, false);
                if (this.isAuthority)
                {
                    Ray aimRay = this.GetAimRay();
                    Vector3 aimDir = aimRay.direction;
                    Vector3 right = Vector3.Cross(Vector3.up, aimDir);
                    if (right.sqrMagnitude < 0.001f)
                        right = Vector3.right;
                    else
                        right = right.normalized;

                    float waveT = FireEyeBlast.numWaves > 1
                        ? (float)waveIndex / (FireEyeBlast.numWaves - 1)
                        : 0f;
                    float ringRadius = Mathf.Lerp(FireEyeBlast.startRingRadius, FireEyeBlast.endRingRadius, waveT);
                    float anglePerMissile = 360f / FireEyeBlast.numMissilesPerWave;
                    float startAngle = FireEyeBlast.numWaves > 0 ? waveIndex * (anglePerMissile / FireEyeBlast.numWaves) : 0f;
                    Vector3 baseTilted = Quaternion.AngleAxis(ringRadius, right) * aimDir;
                    Vector3 radialDirAtZero = Vector3.Cross(right, aimDir);

                    FireProjectileInfo fireProjectileInfo = new FireProjectileInfo()
                    {
                        projectilePrefab = this.projectilePrefab,
                        position = this.muzzleTransform.position,
                        owner = this.gameObject,
                        damage = this.damageStat * this.damageCoefficient,
                        force = this.force
                    };
                    for (int index = 0; index < FireEyeBlast.numMissilesPerWave; ++index)
                    {
                        float ringAngle = startAngle + index * anglePerMissile;
                        Quaternion ringRotation = Quaternion.AngleAxis(ringAngle, aimDir);
                        Vector3 missileDir = ringRotation * baseTilted;
                        Vector3 radialOutward = ringRotation * radialDirAtZero;
                        Quaternion baseRotation = Util.QuaternionSafeLookRotation(missileDir);
                        Vector3 currentRight = baseRotation * Vector3.right;
                        float roll = Vector3.SignedAngle(currentRight, radialOutward, missileDir);
                        fireProjectileInfo.rotation = Quaternion.AngleAxis(roll, missileDir) * baseRotation;
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
    }
}
