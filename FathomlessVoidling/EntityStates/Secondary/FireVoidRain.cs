using EntityStates;
using EntityStates.GrandParentBoss;
using EntityStates.VoidRaidCrab.Weapon;
using RoR2;
using RoR2.Projectile;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Types;

namespace FathomlessVoidling
{
    public class FireVoidRain : BaseState
    {
        private float stopwatch;
        private float missileStopwatch;
        public static float baseDuration = 6f;
        public static string muzzleString = BaseMultiBeamState.muzzleName;
        public static float missileSpawnFrequency = 6f;
        public static float missileSpawnDelay = 0.0f;
        public static float damageCoefficient;
        public static float maxSpread = 1f;
        public static GameObject projectilePrefab;
        public GameObject portalEffect = Main.voidRainPortalEffect;
        public static GameObject muzzleflashPrefab;
        private Transform muzzleTransform;
        private ChildLocator childLocator;

        public override void OnEnter()
        {
            base.OnEnter();
            PhasedInventorySetter inventorySetter = this.GetComponent<PhasedInventorySetter>();
            if ((bool)inventorySetter && NetworkServer.active)
            {
                switch (inventorySetter.phaseIndex)
                {
                    case 0:
                        FireVoidRain.missileSpawnFrequency = 3f;
                        break;
                    case 1:
                        FireVoidRain.missileSpawnFrequency = 5f;
                        break;
                    case 2:
                        FireVoidRain.missileSpawnFrequency = 7f;
                        break;
                }
            }
            this.missileStopwatch -= FireVoidRain.missileSpawnDelay;
            this.muzzleTransform = this.FindModelChild(BaseMultiBeamState.muzzleName);
            Transform modelTransform = this.GetModelTransform();
            if (!(bool)modelTransform)
                return;
            this.childLocator = modelTransform.GetComponent<ChildLocator>();
        }

        private void FireBlob(Ray projectileRay, Vector3 beamEnd)
        {
            EffectManager.SpawnEffect(this.portalEffect, new EffectData()
            {
                origin = projectileRay.origin,
                rotation = Util.QuaternionSafeLookRotation(projectileRay.direction)
            }, false);

            GameObject projectile = new GameObject("VoidRainProjectile");
            projectile.AddComponent<NetworkIdentity>();
            projectile.transform.position = projectileRay.origin;
            projectile.transform.rotation = Util.QuaternionSafeLookRotation(projectileRay.direction);

            VoidRainInfo info = projectile.AddComponent<VoidRainInfo>();
            info.aimRay = projectileRay;
            info.damageStat = this.damageStat;
            info.endPos = beamEnd;

            projectile.AddComponent<VoidRainComponent>();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            this.stopwatch += Time.fixedDeltaTime;
            this.missileStopwatch += Time.fixedDeltaTime;
            if ((double)this.missileStopwatch < 1.0 / (double)FireVoidRain.missileSpawnFrequency)
                return;
            this.missileStopwatch -= 1f / FireVoidRain.missileSpawnFrequency;
            Transform child = this.childLocator.FindChild(FireVoidRain.muzzleString);
            if ((bool)child)
            {
                Ray aimRay = this.GetAimRay();
                Ray projectileRay = new Ray();
                projectileRay.direction = aimRay.direction;
                Vector3 vector3_1 = new Vector3(UnityEngine.Random.Range(-200f, 200f), UnityEngine.Random.Range(75f, 100f), UnityEngine.Random.Range(-200f, 200f));
                Vector3 vector3_2 = child.position + vector3_1;
                projectileRay.origin = vector3_2;

                this.CalcBeamPathPredictive(projectileRay, out Vector3 direction, out Vector3 beamEndPos);
                if (direction != Vector3.zero)
                {
                    projectileRay.direction = direction;
                    this.FireBlob(projectileRay, beamEndPos);
                }
                else
                {
                    this.CalcBeamPath(out Ray beamRay, out Vector3 beamEnd);
                    projectileRay.direction = beamEnd - projectileRay.origin;
                    this.FireBlob(projectileRay, beamEnd);
                }
            }
            if ((double)this.stopwatch < (double)FireVoidRain.baseDuration || !this.isAuthority)
                return;
            this.outer.SetNextStateToMain();
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
            search.maxDistanceFilter = 1000f;
            search.minAngleFilter = 0.0f;
            search.maxAngleFilter = 180f;
            search.RefreshCandidates();

            HurtBox targetHurtBox = search.GetResults().FirstOrDefault();
            bool hasHurtbox = targetHurtBox && targetHurtBox.healthComponent && targetHurtBox.healthComponent.body && targetHurtBox.healthComponent.body.characterMotor;

            if (hasHurtbox)
            {
                CharacterBody targetBody = targetHurtBox.healthComponent.body;
                Vector3 targetPosition = targetHurtBox.transform.position;
                Vector3 targetVelocity = targetBody.characterMotor.velocity;
                if (targetVelocity.sqrMagnitude > 0f && !(targetBody && targetBody.hasCloakBuff))   //Dont bother predicting stationary targets
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