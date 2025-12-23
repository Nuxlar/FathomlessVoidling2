using RoR2;
using UnityEngine;

namespace FathomlessVoidling
{
    public class SingularityKillComponent : MonoBehaviour
    {
        private SphereCollider collider;

        public void Start()
        {
            this.collider = this.GetComponent<SphereCollider>();
        }

        public void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.GetComponent<HurtBox>())
            {
                HurtBox hb = other.gameObject.GetComponent<HurtBox>();
                if (hb.healthComponent.body && hb.healthComponent.body.teamComponent && hb.healthComponent.body.teamComponent.teamIndex == TeamIndex.Player && hb.healthComponent)
                {
                    hb.healthComponent.Suicide(this.gameObject, this.gameObject, DamageType.VoidDeath);
                }
            }
        }
    }
}