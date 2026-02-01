using System.Collections.ObjectModel;
using RoR2;
using UnityEngine;

namespace FathomlessVoidling.Components
{
    public class SingularityComponent : MonoBehaviour
    {
        private SphereCollider collider;

        public void Start()
        {
            this.collider = this.GetComponent<SphereCollider>();
        }

        private void FixedUpdate()
        {
            ReadOnlyCollection<CharacterBody> onlyInstancesList = CharacterBody.readOnlyInstancesList;
            float magnitude = 2f;
            for (int index = 0; index < onlyInstancesList.Count; ++index)
            {
                CharacterBody characterBody = onlyInstancesList[index];
                if (characterBody.teamComponent.teamIndex == TeamIndex.Player)
                {
                    if (characterBody.hasEffectiveAuthority)
                    {
                        IDisplacementReceiver component = characterBody.GetComponent<IDisplacementReceiver>();
                        if (component != null)
                        {
                            Vector3 vector3 = this.transform.position - characterBody.transform.position;
                            component.AddDisplacement(vector3.normalized * (magnitude * Time.fixedDeltaTime));
                        }
                    }
                }
            }
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