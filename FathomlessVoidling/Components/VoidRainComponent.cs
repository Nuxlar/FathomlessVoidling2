using RoR2;
using UnityEngine;

namespace FathomlessVoidling
{
    public class VoidRainComponent : MonoBehaviour
    {
        public Ray aimRay;
        public float damageStat;
        private float duration = 1f;
        private float stopwatch = 0f;
        private Vector3 beamEndPos;
        private GameObject tracerEffectPrefab = Main.voidRainTracer;
        private Vector3 bonusBlastForce = new Vector3(0, 100, 0);
        private float blastDamageCoefficient = 1f;
        private float blastForceMagnitude = 3000f;
        private float blastRadius = 6f;
        private GameObject explosionEffectPrefab = Main.voidRainExplosion;
        private GameObject warningLaserVfxInstance;
        private GameObject warningLaserVfxPrefab = Main.voidRainWarning;
        private RayAttackIndicator warningLaserVfxInstanceRayAttackIndicator;

        private void Start()
        {
            VoidRainInfo info = this.GetComponent<VoidRainInfo>();
            if (info)
            {
                this.aimRay = info.aimRay;
                this.damageStat = info.damageStat;
                this.beamEndPos = info.endPos;
            }

            if (!(bool)this.warningLaserVfxPrefab)
                return;
            this.warningLaserVfxInstance = Object.Instantiate<GameObject>(this.warningLaserVfxPrefab);
            this.warningLaserVfxInstanceRayAttackIndicator = this.warningLaserVfxInstance.GetComponent<RayAttackIndicator>();
            this.UpdateWarningLaser();
        }

        private void FixedUpdate()
        {
            this.stopwatch += Time.fixedDeltaTime;
            if ((double)this.stopwatch < (double)this.duration)
                return;
            FireLaser();
            if (this.warningLaserVfxInstance)
                GameObject.Destroy(this.warningLaserVfxInstance);
            GameObject.Destroy(this.gameObject);
        }

        private void FireLaser()
        {
            Util.PlaySound("Play_voidRaid_snipe_shoot", this.gameObject);
            new BlastAttack()
            {
                attacker = this.gameObject,
                inflictor = this.gameObject,
                teamIndex = TeamComponent.GetObjectTeam(this.gameObject),
                baseDamage = this.damageStat * this.blastDamageCoefficient,
                baseForce = this.blastForceMagnitude,
                position = beamEndPos,
                radius = this.blastRadius,
                falloffModel = BlastAttack.FalloffModel.SweetSpot,
                bonusForce = this.bonusBlastForce,
                damageType = DamageType.Generic
            }.Fire();

            if ((bool)this.tracerEffectPrefab)
            {
                EffectData effectData = new EffectData()
                {
                    origin = beamEndPos,
                    start = aimRay.origin,
                    scale = this.blastRadius
                };
                EffectManager.SpawnEffect(this.tracerEffectPrefab, effectData, true);
                EffectManager.SpawnEffect(this.explosionEffectPrefab, effectData, true);
            }
        }

        private void UpdateWarningLaser()
        {
            if (!(bool)this.warningLaserVfxInstanceRayAttackIndicator)
                return;
            this.warningLaserVfxInstanceRayAttackIndicator.attackRange = 1000f;
            this.warningLaserVfxInstanceRayAttackIndicator.attackRay = this.aimRay;
        }
    }
}