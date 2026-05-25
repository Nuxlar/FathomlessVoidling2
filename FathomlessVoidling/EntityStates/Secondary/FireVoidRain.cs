using EntityStates;
using EntityStates.VoidRaidCrab.Weapon;
using FathomlessVoidling.Components;
using FathomlessVoidling.Controllers;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FathomlessVoidling.EntityStates.Secondary
{
    public class FireVoidRain : BaseState
    {
        private float stopwatch;
        private float missileStopwatch;
        public static float baseDuration = 6f;
        public static string muzzleString = BaseMultiBeamState.muzzleName;
        public static float missileSpawnFrequency = 3.75f;
        public static float missileSpawnDelay = 0.0f;
        public static float shotDelay = 1f;
        private static readonly Vector3 bonusBlastForce = new Vector3(0, 100, 0);
        private static float blastDamageCoefficient = 1f;
        private static float blastForceMagnitude = 3000f;
        private static float blastRadius = 6f;
        public GameObject portalEffect = Main.voidRainPortalEffect;
        private GameObject tracerEffectPrefab = Main.voidRainTracer;
        private GameObject explosionEffectPrefab = Main.voidRainExplosion;
        private GameObject warningLaserVfxPrefab = Main.voidRainWarning;
        private Transform muzzleTransform;
        private ChildLocator childLocator;
        private List<PendingShot> pendingShots = new List<PendingShot>();
        private System.Random rng;
        private Vector3 lastSpawnPos;
        private bool hasLastSpawnPos;
        private class PendingShot
        {
            public Ray aimRay;
            public Vector3 endPos;
            public float damageStat;
            public float timer;
            public GameObject indicatorInstance;
        }

        public override void OnEnter()
        {
            base.OnEnter();
            int seed = (int)(RoR2.Run.instance.GetStartTimeUtc().Ticks ^ (long)(RoR2.Run.instance.stageClearCount << 16));
            rng = new System.Random(seed);

            this.missileStopwatch -= FireVoidRain.missileSpawnDelay;
            this.muzzleTransform = this.FindModelChild(BaseMultiBeamState.muzzleName);
            Transform modelTransform = this.GetModelTransform();
            if (!(bool)modelTransform)
                return;
            this.childLocator = modelTransform.GetComponent<ChildLocator>();
        }

        private void SpawnShot(Ray projectileRay, Vector3 beamEnd)
        {
            EffectManager.SpawnEffect(this.portalEffect, new EffectData()
            {
                origin = projectileRay.origin,
                rotation = Util.QuaternionSafeLookRotation(projectileRay.direction)
            }, false);

            GameObject indicatorInstance = null;
            if ((bool)this.warningLaserVfxPrefab)
            {
                indicatorInstance = Object.Instantiate<GameObject>(this.warningLaserVfxPrefab);
                VoidRainComponent vrc = indicatorInstance.GetComponent<VoidRainComponent>();
                if (vrc)
                    vrc.UpdateBeamIndicator(projectileRay.origin, beamEnd);
            }

            this.pendingShots.Add(new PendingShot()
            {
                aimRay = projectileRay,
                endPos = beamEnd,
                damageStat = this.damageStat,
                timer = FireVoidRain.shotDelay,
                indicatorInstance = indicatorInstance
            });
        }

        private void FireShot(PendingShot shot)
        {
            Util.PlaySound("Play_voidRaid_snipe_shoot", this.gameObject);
            new BlastAttack()
            {
                attacker = this.gameObject,
                inflictor = this.gameObject,
                teamIndex = TeamComponent.GetObjectTeam(this.gameObject),
                baseDamage = shot.damageStat * FireVoidRain.blastDamageCoefficient,
                baseForce = FireVoidRain.blastForceMagnitude,
                position = shot.endPos,
                radius = FireVoidRain.blastRadius,
                falloffModel = BlastAttack.FalloffModel.SweetSpot,
                bonusForce = FireVoidRain.bonusBlastForce,
                damageType = DamageType.Generic
            }.Fire();

            if ((bool)this.tracerEffectPrefab)
            {
                EffectData effectData = new EffectData()
                {
                    origin = shot.endPos,
                    start = shot.aimRay.origin,
                    scale = FireVoidRain.blastRadius
                };
                EffectManager.SpawnEffect(this.tracerEffectPrefab, effectData, true);
                EffectManager.SpawnEffect(this.explosionEffectPrefab, effectData, true);
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            this.stopwatch += Time.fixedDeltaTime;
            this.missileStopwatch += Time.fixedDeltaTime;

            if ((double)this.stopwatch <= (double)FireVoidRain.baseDuration)
            {
                if ((double)this.missileStopwatch >= 1.0 / (double)FireVoidRain.missileSpawnFrequency)
                {
                    this.missileStopwatch -= 1f / FireVoidRain.missileSpawnFrequency;
                    Transform child = this.childLocator.FindChild(FireVoidRain.muzzleString);
                    if ((bool)child)
                    {
                        Ray aimRay = this.GetAimRay();
                        Ray projectileRay = new Ray();
                        projectileRay.direction = aimRay.direction;

                        Vector3 forward = new Vector3(aimRay.direction.x, 0f, aimRay.direction.z).normalized;
                        Vector3 right = Vector3.Cross(Vector3.up, forward);
                        float minSpacing = rng.Next(30, 60);
                        float minSpacingSqr = minSpacing * minSpacing;
                        Vector3 candidate = Vector3.zero;
                        for (int attempt = 0; attempt < 20; attempt++)
                        {
                            candidate = this.characterBody.corePosition + forward * rng.Next(40, 60) + right * rng.Next(-75, 75) + Vector3.up * rng.Next(-30, 30);
                            if (!this.hasLastSpawnPos || (candidate - this.lastSpawnPos).sqrMagnitude >= minSpacingSqr)
                                break;
                        }
                        projectileRay.origin = candidate;
                        this.lastSpawnPos = candidate;
                        this.hasLastSpawnPos = true;

                        this.CalcBeamPathPredictive(projectileRay, out Vector3 direction, out Vector3 beamEndPos);
                        if (direction != Vector3.zero)
                        {
                            projectileRay.direction = direction;
                            this.SpawnShot(projectileRay, beamEndPos);
                        }
                        else
                        {
                            this.CalcBeamPath(out Ray beamRay, out Vector3 beamEnd);
                            projectileRay.direction = beamEnd - projectileRay.origin;
                            this.SpawnShot(projectileRay, beamEnd);
                        }
                    }
                }
            }

            if (this.pendingShots.Count > 0)
            {
                for (int i = this.pendingShots.Count - 1; i >= 0; i--)
                {
                    PendingShot shot = this.pendingShots[i];
                    shot.timer -= Time.fixedDeltaTime;
                    if (shot.timer <= 0f)
                    {
                        if ((bool)shot.indicatorInstance)
                            Object.Destroy(shot.indicatorInstance);
                        this.FireShot(shot);
                        this.pendingShots.RemoveAt(i);
                    }
                }
            }

            if ((double)this.stopwatch >= (double)FireVoidRain.baseDuration && this.pendingShots.Count == 0 && this.isAuthority)
                this.outer.SetNextStateToMain();
        }

        public override void OnExit()
        {
            foreach (PendingShot shot in this.pendingShots)
            {
                if ((bool)shot.indicatorInstance)
                    Object.Destroy(shot.indicatorInstance);
            }
            this.pendingShots.Clear();

            base.OnExit();
            if (this.isAuthority)
            {
                FathomlessSkillDriverController sdc = this.characterBody.GetComponent<FathomlessSkillDriverController>();
                if (sdc)
                {
                    if (sdc.IsSingularityEnabled() && this.skillLocator.special.IsReady())
                        this.skillLocator.special.ExecuteIfReady();
                    else if (sdc.IsMazeEnabled() && this.skillLocator.utility.IsReady())
                        this.skillLocator.utility.ExecuteIfReady();
                }
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }

        protected void CalcBeamPathPredictive(Ray aimRay, out Vector3 direction, out Vector3 beamEndPoint)
        {
            BullseyeSearch search = new BullseyeSearch();

            search.teamMaskFilter = TeamMask.GetEnemyTeams(this.GetTeam());
            search.filterByDistinctEntity = true;
            search.viewer = null;
            search.filterByLoS = true;
            search.searchOrigin = aimRay.origin;
            search.sortMode = BullseyeSearch.SortMode.DistanceAndAngle;
            search.maxDistanceFilter = 500f;
            search.minAngleFilter = 0.0f;
            search.maxAngleFilter = 180f;
            search.RefreshCandidates();

            HurtBox targetHurtBox;
            targetHurtBox = search.GetResults().FirstOrDefault((hurtBox) =>
            {
                if (hurtBox.healthComponent && hurtBox.healthComponent.body && hurtBox.healthComponent.body.isPlayerControlled)
                    return true;
                else return false;
            });

            bool hasHurtbox = targetHurtBox && targetHurtBox.healthComponent.body.characterMotor;

            if (hasHurtbox)
            {
                CharacterBody targetBody = targetHurtBox.healthComponent.body;
                Vector3 targetPosition = targetHurtBox.transform.position;
                Vector3 targetVelocity = targetBody.characterMotor.velocity;
                if (targetVelocity.sqrMagnitude > 0f && !(targetBody && targetBody.hasCloakBuff))
                {
                    Vector3 lateralVelocity = new Vector3(targetVelocity.x, 0f, targetVelocity.z);
                    Vector3 futurePosition = targetPosition + lateralVelocity;

                    if (targetBody.characterMotor && !targetBody.characterMotor.isGrounded && targetVelocity.y > 0f)
                    {
                        Vector3 predictedPosition = targetPosition + targetVelocity * 0.5f;
                        direction = (predictedPosition - aimRay.origin).normalized;
                        beamEndPoint = predictedPosition;
                    }
                    else
                    {
                        direction = (futurePosition - aimRay.origin).normalized;
                        beamEndPoint = futurePosition;
                    }
                }
                else
                {
                    direction = (targetPosition - aimRay.origin).normalized;
                    beamEndPoint = targetPosition;
                }
            }
            else
            {
                direction = Vector3.zero;
                beamEndPoint = Vector3.zero;
            }
        }

        protected void CalcBeamPath(out Ray beamRay, out Vector3 beamEndPos)
        {
            Ray aimRay = this.GetAimRay();
            float a = float.PositiveInfinity;
            RaycastHit[] raycastHitArray = Physics.RaycastAll(aimRay, 1000f, (int)LayerIndex.CommonMasks.bullet, QueryTriggerInteraction.Ignore);
            Transform root = this.GetModelTransform().root;
            for (int index = 0; index < raycastHitArray.Length; ++index)
            {
                ref RaycastHit local = ref raycastHitArray[index];
                float distance = local.distance;
                if ((double)distance < (double)a && local.collider.transform.root != root)
                    a = distance;
            }
            float distance1 = Mathf.Min(a, BaseMultiBeamState.beamMaxDistance);
            beamEndPos = aimRay.GetPoint(distance1);
            Vector3 position = this.muzzleTransform.position;
            beamRay = new Ray(position, beamEndPos - position);
        }
    }
}
