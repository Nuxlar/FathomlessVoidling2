using EntityStates;
using RoR2;
using RoR2.Skills;
using RoR2.Projectile;
using UnityEngine;
using System;
using UnityEngine.Networking;

namespace FathomlessVoidling.EntityStates.Primary
{
    public class FireEyeBlast : BaseState
    {
        public float baseInitialDelay = 0f;
        public static float baseDelayBetweenWaves = 0.5f;
        public float baseEndDelay = 0f;
        public int numWaves = 5;
        public static int numMissilesPerWave = 6; // 6 orig
        public string muzzleName = "EyeProjectileCenter";
        public GameObject muzzleFlashPrefab = Main.eyeBlastMuzzleFlash;
        public GameObject projectilePrefab = Main.eyeMissileProjectile;
        public float damageCoefficient = 1f;
        public float force = 1000f;
        public float minSpreadDegrees = 0f;
        public float rangeSpreadDegrees = 5f;
        public string fireWaveSoundString;
        public bool isSoundScaledByAttackSpeed;
        public string animationLayerName = "Gesture";
        public string animationStateName = "ChargeEyeBlast";
        public string animationPlaybackRateParam = "Eyeblast.playbackRate";
        private float delayBetweenWaves;
        private float duration;
        private int numWavesFired = 0;
        private float timeUntilNextWave;
        private Transform muzzleTransform;
        private AimAnimator.DirectionOverrideRequest animatorDirectionOverrideRequest;

        public override void OnEnter()
        {
            base.OnEnter();
            PhasedInventorySetter inventorySetter = this.GetComponent<PhasedInventorySetter>();
            if ((bool)inventorySetter && NetworkServer.active)
            {
                switch (inventorySetter.phaseIndex)
                {
                    case 0:
                        FireEyeBlast.numMissilesPerWave = 6;
                        FireEyeBlast.baseDelayBetweenWaves = 0.5f;
                        break;
                    case 1:
                        FireEyeBlast.numMissilesPerWave = 8;
                        break;
                    case 2:
                        FireEyeBlast.baseDelayBetweenWaves = 0.25f;
                        break;
                }
            }
            this.duration = (this.baseInitialDelay + Mathf.Max(0.0f, FireEyeBlast.baseDelayBetweenWaves * (this.numWaves - 1)) + this.baseEndDelay) / this.attackSpeedStat;
            this.characterBody.SetAimTimer(this.duration + 3f);
            this.timeUntilNextWave = this.baseInitialDelay / this.attackSpeedStat;
            this.delayBetweenWaves = FireEyeBlast.baseDelayBetweenWaves / this.attackSpeedStat;
            this.muzzleTransform = this.FindModelChild(this.muzzleName);
            AimAnimator aimAnimator = this.GetAimAnimator();
            if (aimAnimator)
                this.animatorDirectionOverrideRequest = aimAnimator.RequestDirectionOverride(new Func<Vector3>(this.GetAimDirection));
        }

        private Vector3 GetAimDirection()
        {
            Ray aimRay = this.GetAimRay();
            float degrees = 60f;
            float angleFromForward = Vector3.SignedAngle(Vector3.forward, new Vector3(aimRay.direction.x, 0, aimRay.direction.z), Vector3.up);
            Vector3 newRight = Quaternion.AngleAxis(angleFromForward, Vector3.up) * Vector3.right;
            return (Quaternion.AngleAxis(-degrees, newRight) * aimRay.direction).normalized;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            this.timeUntilNextWave -= this.GetDeltaTime();
            while (this.timeUntilNextWave < 0.0 && this.numWavesFired < FireEyeBlast.numMissilesPerWave)
            {
                this.PlayAnimation(this.animationLayerName, this.animationStateName, this.animationPlaybackRateParam, this.duration);
                this.timeUntilNextWave += this.delayBetweenWaves;
                ++this.numWavesFired;
                // EffectManager.SimpleMuzzleFlash(this.muzzleFlashPrefab, this.gameObject, this.muzzleName, false);
                EffectManager.SpawnEffect(this.muzzleFlashPrefab, new EffectData()
                {
                    origin = this.muzzleTransform.position,
                    rotation = this.muzzleTransform.rotation
                }, false);
                if (this.isAuthority)
                {
                    Ray aimRay = this.GetAimRay();
                    float degrees = 60f;
                    float angleFromForward = Vector3.SignedAngle(Vector3.forward, new Vector3(aimRay.direction.x, 0, aimRay.direction.z), Vector3.up); // we find how far are we from forward ignoring y axis, so it doesn't affect the angle from forward
                    Vector3 newRight = Quaternion.AngleAxis(angleFromForward, Vector3.up) * Vector3.right; // using the angle we find our new right to our aim direction
                    Vector3 newDirection = (Quaternion.AngleAxis(-degrees, newRight) * aimRay.direction).normalized; // here we angle aim direction 30 degrees towards the sky
                    Quaternion quaternion = Util.QuaternionSafeLookRotation(newDirection);
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
                        fireProjectileInfo.rotation = quaternion;
                        fireProjectileInfo.crit = Util.CheckRoll(this.critStat, this.characterBody.master);
                        ProjectileManager.instance.FireProjectile(fireProjectileInfo);
                    }
                }
            }
            if (!this.isAuthority || (double)this.fixedAge < this.duration)
                return;
            this.outer.SetNextStateToMain();
        }

        public override void OnExit()
        {
            this.animatorDirectionOverrideRequest?.Dispose();
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill; // TODO testing if the interrupt would be fun
        }
    }
}