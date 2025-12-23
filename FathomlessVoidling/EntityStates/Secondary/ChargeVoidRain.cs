using EntityStates;
using EntityStates.VoidRaidCrab.Weapon;
using RoR2;
using UnityEngine;

namespace FathomlessVoidling
{
    public class ChargeVoidRain : BaseState
    {
        public float baseDuration = 1f;
        private string animationLayerName = "Gesture";
        private string animationStateName = "ChargeMultiBeam";
        private string animationPlaybackRateParam = "MultiBeam.playbackRate";
        private GameObject chargeEffectPrefab = Main.chargeVoidRain;
        private string enterSoundString = "Play_voidRaid_snipe_chargeUp";
        private bool isSoundScaledByAttackSpeed = false;
        private string muzzleName = "EyeProjectileCenter";
        private GameObject chargeEffectInstance;
        protected float duration { get; private set; }

        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = this.baseDuration / this.attackSpeedStat;
            this.PlayAnimation(this.animationLayerName, this.animationStateName, this.animationPlaybackRateParam, this.duration);
            ChildLocator modelChildLocator = this.GetModelChildLocator();
            if ((bool)modelChildLocator && (bool)this.chargeEffectPrefab)
            {
                Transform transform = modelChildLocator.FindChild(this.muzzleName) ?? this.characterBody.coreTransform;
                if ((bool)transform)
                {
                    this.chargeEffectInstance = Object.Instantiate<GameObject>(this.chargeEffectPrefab, transform.position, transform.rotation);
                    this.chargeEffectInstance.transform.parent = transform;
                    ScaleParticleSystemDuration component = this.chargeEffectInstance.GetComponent<ScaleParticleSystemDuration>();
                    if ((bool)component)
                        component.newDuration = this.duration;
                }
            }
            if (!string.IsNullOrEmpty(this.enterSoundString))
            {
                if (this.isSoundScaledByAttackSpeed)
                    Util.PlayAttackSpeedSound(this.enterSoundString, this.gameObject, this.attackSpeedStat);
                else
                    Util.PlaySound(this.enterSoundString, this.gameObject);
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!this.isAuthority || (double)this.fixedAge < (double)this.duration)
                return;
            this.outer.SetNextState(new FireVoidRain());
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}