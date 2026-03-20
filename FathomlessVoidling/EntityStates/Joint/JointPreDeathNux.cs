using EntityStates;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace FathomlessVoidling.EntityStates.Joint
{
    public class JointPreDeathNux : BaseState
    {
        public float minDuration = 0.1f;
        public string joint1Name = "Calf";
        public string joint2Name = "Foot";
        public string joint3Name = "Toe";
        public GameObject jointEffectPrefab = Main.jointPendingEffect;
        public bool canProceed;
        private List<GameObject> jointEffects = new List<GameObject>();

        public override void OnEnter()
        {
            base.OnEnter();
            this.canProceed = false;
            this.jointEffects.Clear();
            ChildLocator modelChildLocator = this.GetModelChildLocator();
            if (!modelChildLocator)
                return;
            this.SpawnJointEffect(this.joint1Name, modelChildLocator);
            this.SpawnJointEffect(this.joint2Name, modelChildLocator);
            this.SpawnJointEffect(this.joint3Name, modelChildLocator);
        }

        public override void OnExit()
        {
            base.OnExit();
            if (NetworkServer.active)
            {
                foreach (GameObject jointEffect in this.jointEffects)
                    if (jointEffect) NetworkServer.Destroy(jointEffect);
            }
            this.jointEffects.Clear();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!this.isAuthority || this.fixedAge <= this.minDuration || !this.canProceed)
                return;
            this.outer.SetNextState(new JointDeathNux());
        }

        private void SpawnJointEffect(string jointName, ChildLocator childLocator)
        {
            if (!NetworkServer.active)
                return;
            Transform child = childLocator.FindChild(jointName);
            if (!child)
                return;
            GameObject effect = Object.Instantiate(this.jointEffectPrefab, child.position, child.rotation);
            NetworkServer.Spawn(effect);
            this.jointEffects.Add(effect);
        }
    }
}
