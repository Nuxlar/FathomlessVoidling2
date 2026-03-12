using RoR2;
using UnityEngine;
using EntityStates;
using RoR2.Projectile;
using R2API;

namespace FathomlessVoidling.EntityStates.Barnacle
{

    public class FireGravityBullet : GenericProjectileBaseState
    {
        public int numberOfFireballs = 1;
        public string animationLayerName = "Gesture";
        public string animationStateName = "Fire";
        public string animationPlaybackRateName = "firePlaybackRate";
        private float _interFireballDuration;
        private float _animationDuration;
        private Transform muzzleTransform;

        public override void OnEnter()
        {
            // serialized stuff
            this.bloom = 1f;
            this.baseDuration = 1.2f;
            this.projectilePrefab = Main.gravityBulletProjectile;
            this.attackSoundString = "Play_voidBarnacle_m1_shoot";
            this.damageCoefficient = 0f; // 3f orig
            this.projectilePitchBonus = -5f;
            this.minSpread = 0f;
            this.maxSpread = 1f;
            this.force = 10f;
            this.targetMuzzle = "MuzzleMouth";
            this.recoilAmplitude = 0f;
            this.effectPrefab = Main.barnacleMuzzleFlash;

            this.duration = this.baseDuration / this.attackSpeedStat;
            this._interFireballDuration = this.duration / (float)this.numberOfFireballs;
            this._animationDuration = this._interFireballDuration;
            this.muzzleTransform = this.FindModelChild(this.targetMuzzle);
            base.OnEnter();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if ((double)this.stopwatch < (double)this._animationDuration || this.numberOfFireballs <= 0)
                return;
            this._animationDuration += this._animationDuration;
            this.PlayAnimation(this._animationDuration);
        }

        public override void PlayAnimation(float duration)
        {
            this.PlayAnimation(this.animationLayerName, this.animationStateName, this.animationPlaybackRateName, this._interFireballDuration);
        }

        public override void FireProjectile()
        {
            if (!this.isAuthority)
                return;
            Ray ray = this.ModifyProjectileAimRay(this.GetAimRay());
            ray.direction = Util.ApplySpread(ray.direction, this.minSpread, this.maxSpread, 1f, 1f, bonusPitch: this.projectilePitchBonus);
            FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
            fireProjectileInfo.projectilePrefab = this.projectilePrefab;
            fireProjectileInfo.owner = this.gameObject;
            fireProjectileInfo.damage = 1f;//this.damageStat * this.damageCoefficient;
            fireProjectileInfo.position = ray.origin;
            fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(ray.direction);
            fireProjectileInfo.force = this.force;
            fireProjectileInfo.crit = Util.CheckRoll(this.critStat, this.characterBody.master);
            DamageTypeCombo damageType = DamageType.Generic | DamageType.BypassBlock;
            damageType.AddModdedDamageType(Main.gravityDamageType);
            fireProjectileInfo.damageTypeOverride = damageType;
            this.ModifyProjectileInfo(ref fireProjectileInfo);
            ProjectileManager.instance.FireProjectile(fireProjectileInfo);

            if (this.numberOfFireballs > 1)
            {
                this.firedProjectile = false;
                this.delayBeforeFiringProjectile += this._interFireballDuration;
            }
            --this.numberOfFireballs;
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Skill;
    }
}