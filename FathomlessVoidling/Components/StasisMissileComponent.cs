using System.Linq;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace FathomlessVoidling.Components
{
    public class StasisMissileComponent : MonoBehaviour
    {
        public Quaternion postPauseRotation;
        private ProjectileSteerTowardTarget steer;
        private ProjectileSimple projectileSimple;
        private float stopwatch = 0f;
        private float delayBeforePause = 1f;
        private float pauseDuration = 1f;
        private bool hasPaused = false;
        private float minSpreadDegrees = 0f;
        private float rangeSpreadDegrees = 10f;

        public void Start()
        {
            this.projectileSimple = this.GetComponent<ProjectileSimple>();
            this.steer = this.GetComponent<ProjectileSteerTowardTarget>();
        }

        private void FixedUpdate()
        {
            this.stopwatch += Time.fixedDeltaTime;
            if ((double)this.stopwatch < (double)this.delayBeforePause)
                return;
            if (!this.hasPaused)
            {
                this.hasPaused = true;
                projectileSimple.desiredForwardSpeed = 0f;
            }
            if ((double)this.stopwatch < (double)this.delayBeforePause + this.pauseDuration)
                return;
            RecalculateDirection();
        }

        private void RecalculateDirection()
        {
            projectileSimple.desiredForwardSpeed = 0f;
            Ray aimRay = new Ray(this.transform.position, transform.forward);
            BullseyeSearch enemyFinder = new BullseyeSearch();
            enemyFinder.maxDistanceFilter = 500f;
            enemyFinder.maxAngleFilter = 360f;
            enemyFinder.searchOrigin = aimRay.origin;
            enemyFinder.searchDirection = aimRay.direction;
            enemyFinder.filterByLoS = false;
            enemyFinder.sortMode = BullseyeSearch.SortMode.DistanceAndAngle;
            enemyFinder.teamMaskFilter = TeamMask.GetEnemyTeams(TeamIndex.Void);
            enemyFinder.RefreshCandidates();

            HurtBox targetHurtBox;
            targetHurtBox = enemyFinder.GetResults().First((hurtBox) =>
            {
                if (hurtBox.healthComponent && hurtBox.healthComponent.body && hurtBox.healthComponent.body.isPlayerControlled)
                    return true;
                else return false;
            });

            if (!targetHurtBox)
                targetHurtBox = enemyFinder.GetResults().FirstOrDefault();

            if (!targetHurtBox)
                return;

            Vector3 direction = targetHurtBox.transform.position - aimRay.origin;
            Quaternion quaternion = Util.QuaternionSafeLookRotation(direction);
            Quaternion newRotation = quaternion * this.GetRandomRollPitch();
            this.transform.rotation = newRotation;
            //  this.transform.forward = direction.normalized;
            this.projectileSimple.desiredForwardSpeed = 125f;
            this.steer.enabled = true;
            Destroy(this);
        }

        protected Quaternion GetRandomRollPitch()
        {
            return Quaternion.AngleAxis(Random.Range(0, 360), Vector3.forward) * Quaternion.AngleAxis(this.minSpreadDegrees + Random.Range(0.0f, this.rangeSpreadDegrees), Vector3.left);
        }
    }
}