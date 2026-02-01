using RoR2;
using EntityStates;
using EntityStates.VoidRaidCrab;
using UnityEngine;
using RoR2.Projectile;
using RoR2.VoidRaidCrab;

namespace FathomlessVoidling.EntityStates.Special
{
    public class WanderingSingularity : BaseState
    {
        private float duration;
        private float baseDuration = 6f;
        private float windDuration = 2f;
        private string animLayerName = "Body";
        private string animEnterStateName = "SuckEnter";
        private string animLoopStateName = "SuckLoop";
        private string animExitStateName = "SuckExit";
        private string animPlaybackRateParamName = "Suck.playbackRate";
        private Transform vacuumOrigin;
        private bool hasFired = false;
        private bool hasPlayedExit = false;
        private CentralLegController centralLegController;
        private CentralLegController.SuppressBreaksRequest suppressBreaksRequest;

        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = this.baseDuration / this.attackSpeedStat;
            this.centralLegController = this.GetComponent<CentralLegController>();
            if ((bool)centralLegController)
                this.suppressBreaksRequest = this.centralLegController.SuppressBreaks();
            if (!string.IsNullOrEmpty(this.animLayerName) && !string.IsNullOrEmpty(this.animEnterStateName) && !string.IsNullOrEmpty(this.animPlaybackRateParamName))
                this.PlayAnimation(this.animLayerName, this.animEnterStateName, this.animPlaybackRateParamName, this.windDuration);
            if (!string.IsNullOrEmpty(BaseVacuumAttackState.vacuumOriginChildLocatorName))
                this.vacuumOrigin = this.FindModelChild(BaseVacuumAttackState.vacuumOriginChildLocatorName);
            else
                this.vacuumOrigin = this.transform;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (this.fixedAge < this.windDuration)
                return;
            if (!this.hasFired)
            {
                this.PlayAnimation(this.animLayerName, this.animLoopStateName, this.animPlaybackRateParamName, this.windDuration);
                this.hasFired = true;
                if (this.isAuthority)
                {
                    ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                    {
                        projectilePrefab = Main.wSingularityProjectile,
                        position = this.vacuumOrigin.position,
                        rotation = this.vacuumOrigin.rotation,
                        owner = this.gameObject,
                        damage = 1f,
                        force = 0f,
                        crit = false,
                        damageColorIndex = DamageColorIndex.Void,
                        target = null,
                        damageTypeOverride = DamageType.BypassBlock | DamageType.VoidDeath
                    });
                }
            }
            if (this.fixedAge >= this.duration - this.windDuration && !this.hasPlayedExit)
            {
                this.PlayAnimation(this.animLayerName, this.animExitStateName, this.animPlaybackRateParamName, this.windDuration);
                this.hasPlayedExit = true;
            }
            if (!this.isAuthority || (double)this.fixedAge < (double)this.duration)
                return;
            this.outer.SetNextStateToMain();
        }

        public override void OnExit()
        {
            base.OnExit();
            this.suppressBreaksRequest?.Dispose();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Frozen;
    }
}