using RoR2.Projectile;
using UnityEngine;

namespace FathomlessVoidling.Components
{
    public class RadialOutwardOscillation : MonoBehaviour
    {
        private Vector3 radialDirection;
        private Rigidbody rb;
        private ProjectileSimple ps;
        private float oscillateMagnitude;
        private float oscillateSpeed;
        private float stopwatch = 0f;

        private void Start()
        {
            this.radialDirection = this.transform.right;
            this.rb = this.GetComponent<Rigidbody>();
            this.ps = this.GetComponent<ProjectileSimple>();
            if (this.ps)
            {
                this.oscillateMagnitude = this.ps.oscillateMagnitude;
                this.oscillateSpeed = this.ps.oscillateSpeed;
                this.ps.oscillate = false;
                this.ps.updateAfterFiring = false;
                this.ps.enableVelocityOverLifetime = false;
            }
        }

        private void FixedUpdate()
        {
            if (!this.rb || !this.ps) return;
            this.stopwatch += Time.fixedDeltaTime;
            float deltaHeight = Mathf.Sin(this.stopwatch * this.oscillateSpeed);
            this.rb.velocity = this.transform.forward * this.ps.desiredForwardSpeed + this.radialDirection * (deltaHeight * this.oscillateMagnitude);
        }
    }
}
