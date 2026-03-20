using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace FathomlessVoidling.Components
{
    public class LegControllerNetworkHelper : NetworkBehaviour
    {
        [ClientRpc]
        public void RpcMirrorLegJoints(GameObject voidlingBodyObj, string legName)
        {
            if (!voidlingBodyObj)
                return;
            ModelLocator modelLocator = voidlingBodyObj.GetComponent<ModelLocator>();
            if (!modelLocator || !modelLocator.modelTransform)
                return;
            ChildLocator mainCL = modelLocator.modelTransform.GetComponent<ChildLocator>();
            if (!mainCL)
                return;
            Transform child = mainCL.FindChild(legName);
            if (!child || !child.TryGetComponent(out ChildLocator legCL))
                return;
            if (TryGetComponent(out ChildLocatorMirrorController clmc))
                clmc.referenceLocator = legCL;
            else
                Debug.LogError("LegControllerNetworkHelper.RpcMirrorLegJoints : failed to find clmc");
        }
    }
}
