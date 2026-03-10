using EntityStates;
using EntityStates.GrandParentBoss;
using EntityStates.VoidRaidCrab.Weapon;
using FathomlessVoidling.Components;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Types;

namespace FathomlessVoidling.EntityStates.Utility
{
    public class BaseMazeAttackState : BaseState
    {
        public static string headTransformNameInChildLocator = "Head";
        public static string muzzleTransformNameInChildLocator = "EyeMuzzle";
        public static int waves = 3;
        public static float beamDuration = 2f;
        public static bool dualBeams = false;
        public static bool alternatingBeams = false;

        private Transform headTransform;
        private Transform muzzleTransform;
        private int phaseIndex;
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

            PhasedInventorySetter inventorySetter = this.GetComponent<PhasedInventorySetter>();
            if ((bool)inventorySetter && NetworkServer.active)
            {
                this.phaseIndex = inventorySetter.phaseIndex;
                switch (this.phaseIndex)
                {
                    case 0:
                        BaseMazeAttackState.alternatingBeams = false;
                        BaseMazeAttackState.dualBeams = false;
                        break;
                    case 1:
                        BaseMazeAttackState.dualBeams = false;
                        BaseMazeAttackState.alternatingBeams = false;
                        break;
                    case 2:
                        BaseMazeAttackState.alternatingBeams = true;
                        BaseMazeAttackState.dualBeams = true;
                        break;
                }
            }
        }

        public override void OnExit()
        {
            this.DestroyBeamVFXInstance();
            base.OnExit();
        }

        protected void CreateBeamVFXInstance(GameObject beamVfxPrefab, Transform parent)
        {
            GameObject beamVfxInstance = UnityEngine.Object.Instantiate<GameObject>(beamVfxPrefab);
            beamVfxInstance.transform.SetParent(parent, true);
            beamVfxInstance.transform.SetPositionAndRotation(parent.position, Quaternion.LookRotation(parent.forward));
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
            return InterruptPriority.Death;
        }
    }
}