using UnityEngine;

namespace FathomlessVoidling.Components
{
    public class VoidRainComponent : MonoBehaviour
    {
        public Transform originRecipient;
        public Transform furthestHitRecipient;

        public void UpdateBeamIndicator(Vector3 originPos, Vector3 endPos)
        {
            originRecipient.position = originPos;
            furthestHitRecipient.position = endPos;
        }

    }
}