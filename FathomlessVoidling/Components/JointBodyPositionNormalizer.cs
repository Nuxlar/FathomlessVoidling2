using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace FathomlessVoidling.Components
{
    public class JointBodyPositionNormalizer : MonoBehaviour
    {
        private Transform toeTransform;

        private void Start()
        {
            ModelLocator modelLocator = this.GetComponent<ModelLocator>();
            if (modelLocator && modelLocator.modelTransform)
            {
                ChildLocator childLocator = modelLocator.modelTransform.GetComponent<ChildLocator>();
                if (childLocator)
                    this.toeTransform = childLocator.FindChild("Toe");
            }
        }

        private void FixedUpdate()
        {
            if (!NetworkServer.active || !this.toeTransform)
                return;

            this.transform.position = this.toeTransform.position;
        }
    }
}
