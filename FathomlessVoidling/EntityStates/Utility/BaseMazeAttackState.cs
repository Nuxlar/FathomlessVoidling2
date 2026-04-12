using EntityStates;
using FathomlessVoidling.Controllers;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace FathomlessVoidling.EntityStates.Utility
{
    public class BaseMazeAttackState : BaseState
    {
        public static string headTransformNameInChildLocator = "Head";
        public static string muzzleTransformNameInChildLocator = "EyeMuzzle";
        public float beamDuration = 3f;
        public float beamDelay = 2f;
        public bool randomBeams = false;

        public Transform headTransform;
        public Transform muzzleTransform;
        protected Animator modelAnimator { get; private set; }
        protected List<GameObject> beamVfxInstances { get; private set; }

        public override void OnEnter()
        {
            base.OnEnter();
            this.modelAnimator = this.GetModelAnimator();
            this.headTransform = this.FindModelChild(BaseMazeAttackState.headTransformNameInChildLocator);
            this.muzzleTransform = this.FindModelChild(BaseMazeAttackState.muzzleTransformNameInChildLocator);
            this.beamVfxInstances = new List<GameObject>();

            if ((bool)this.modelAnimator)
                this.modelAnimator.GetComponent<AimAnimator>().enabled = true;

            int phaseItemCount = this.characterBody.inventory.GetItemCountEffective(RoR2Content.Items.MinHealthPercentage);
            if (phaseItemCount == 5)
                this.randomBeams = true;
            else
                this.randomBeams = false;
        }

        public override void OnExit()
        {
            this.DestroyBeamVFXInstance();
            base.OnExit();
        }

        protected GameObject CreateBeamVFXInstance(GameObject beamVfxPrefab, Transform parent)
        {
            GameObject beamVfxInstance = UnityEngine.Object.Instantiate<GameObject>(beamVfxPrefab);
            beamVfxInstance.transform.SetParent(parent, true);
            beamVfxInstance.transform.SetPositionAndRotation(parent.position, Quaternion.LookRotation(parent.forward));
            return beamVfxInstance;
        }

        protected void DestroyBeamVFXInstance()
        {
            if (this.beamVfxInstances.Count() == 0)
                return;
            foreach (GameObject instance in this.beamVfxInstances)
                VfxKillBehavior.KillVfxObject(instance);
            this.beamVfxInstances.Clear();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Frozen;
        }
    }
}