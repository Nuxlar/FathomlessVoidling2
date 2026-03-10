using EntityStates;
using EntityStates.GrandParentBoss;
using EntityStates.VoidRaidCrab.Weapon;
using FathomlessVoidling.Components;
using RoR2;
using RoR2.Projectile;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Types;

namespace FathomlessVoidling.EntityStates.Utility
{
    public class ExitMaze : BaseMazeAttackState
    {
        public string animLayerName = "Body";
        public string animStateName = "SpinBeamExit";
        public string animPlaybackRateParamName = "SpinBeam.playbackRate";
        public float baseDuration = 2f; // 3f orig
        private float duration;

        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = this.baseDuration;
            if (!string.IsNullOrEmpty(this.animLayerName) && !string.IsNullOrEmpty(this.animStateName))
            {
                if (!string.IsNullOrEmpty(this.animPlaybackRateParamName))
                    this.PlayAnimation(this.animLayerName, this.animStateName, this.animPlaybackRateParamName, this.duration);
                else
                    this.PlayAnimation(this.animLayerName, this.animStateName);
            }
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if ((double)this.fixedAge < this.duration || !this.isAuthority)
                return;
            this.outer.SetNextStateToMain();
        }
    }
}