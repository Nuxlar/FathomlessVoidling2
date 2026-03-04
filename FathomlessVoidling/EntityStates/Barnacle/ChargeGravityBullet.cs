using RoR2;
using UnityEngine;
using EntityStates;

namespace FathomlessVoidling.EntityStates.Barnacle
{

    public class ChargeGravityBullet : BaseState
    {
        public float baseDuration = 1f;
        public GameObject chargeVfxPrefab = Main.gravityBulletChargeEffect;
        public string attackSoundEffect = "Play_voidBarnacle_m1_chargeUp";
        public string animationLayerName = "Gesture";
        public string animationStateName = "ChargeFire";
        public string animationPlaybackRateName = "chargeFirePlaybackRate";
        private float _chargingDuration;
        private float _totalDuration;
        private float _crossFadeDuration;
        private GameObject _chargeVfxInstance;

        public override void OnEnter()
        {
            base.OnEnter();
            this._totalDuration = this.baseDuration / this.attackSpeedStat;
            this._crossFadeDuration = this._totalDuration * 0.25f;
            this._chargingDuration = this._totalDuration - this._crossFadeDuration;
            Transform modelTransform = this.GetModelTransform();
            Util.PlayAttackSpeedSound(this.attackSoundEffect, this.gameObject, this.attackSpeedStat);
            if (modelTransform != null)
            {
                ChildLocator component1 = modelTransform.GetComponent<ChildLocator>();
                if ((bool)component1)
                {
                    Transform child = component1.FindChild("MuzzleMouth");
                    if ((bool)child && (bool)this.chargeVfxPrefab)
                    {
                        this._chargeVfxInstance = Object.Instantiate<GameObject>(this.chargeVfxPrefab, child.position, child.rotation, child);
                        ScaleParticleSystemDuration component2 = this._chargeVfxInstance.GetComponent<ScaleParticleSystemDuration>();
                        if ((bool)component2)
                            component2.newDuration = this._totalDuration;
                    }
                }
            }
            this.PlayCrossfade(this.animationLayerName, this.animationStateName, this.animationPlaybackRateName, this._chargingDuration, this._crossFadeDuration);
        }

        public override void Update()
        {
            base.Update();
            if (!(bool)this._chargeVfxInstance)
                return;
            this._chargeVfxInstance.transform.forward = this.GetAimRay().direction;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if ((double)this.fixedAge < (double)this._totalDuration || !this.isAuthority)
                return;
            this.outer.SetNextState(new FireGravityBullet());
        }

        public override void OnExit()
        {
            base.OnExit();
            if (!(bool)this._chargeVfxInstance)
                return;
            EntityState.Destroy(this._chargeVfxInstance);
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Skill;
    }
}