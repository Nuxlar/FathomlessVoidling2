using System;
using EntityStates;
using RoR2;
using UnityEngine;

namespace FathomlessVoidling.EntityStates.Primary
{
    public class ChargeEyeBlast : BaseState
    {
        public float baseDuration = 2f;
        public GameObject chargeEffectPrefab = Main.eyeBlastChargeEffect;
        public string muzzleName = "EyeProjectileCenter";
        public string enterSoundString = "Play_voidRaid_m1_chargeup";
        public string animationLayerName = "Gesture";
        public string animationStateName = "ChargeEyeBlast";
        public string animationPlaybackRateParam = "Eyeblast.playbackRate";
        private float duration;
        private GameObject chargeEffectInstance;
        private AimAnimator.DirectionOverrideRequest animatorDirectionOverrideRequest;

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
                    this.chargeEffectInstance = UnityEngine.Object.Instantiate<GameObject>(this.chargeEffectPrefab, transform.position, transform.rotation);
                    this.chargeEffectInstance.transform.parent = transform;
                }
            }
            AimAnimator aimAnimator = this.GetAimAnimator();
            this.animatorDirectionOverrideRequest = aimAnimator.RequestDirectionOverride(new Func<Vector3>(this.GetAimDirection));
            if (string.IsNullOrEmpty(this.enterSoundString))
                return;
            Util.PlayAttackSpeedSound(this.enterSoundString, this.gameObject, this.attackSpeedStat);
        }

        public override void OnExit()
        {
            EntityState.Destroy(this.chargeEffectInstance);
            base.OnExit();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!this.isAuthority || (double)this.fixedAge < this.duration)
                return;
            this.outer.SetNextState(new FireEyeBlast() { animatorDirectionOverrideRequest = this.animatorDirectionOverrideRequest });
        }

        private Vector3 GetAimDirection()
        {
            Ray aimRay = this.GetAimRay();
            Vector3 direction = Quaternion.AngleAxis(-80, Vector3.left) * aimRay.direction;
            return direction;
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}