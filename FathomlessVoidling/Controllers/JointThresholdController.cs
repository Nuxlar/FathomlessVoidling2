using RoR2;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace FathomlessVoidling.Controllers
{
    public class JointThresholdController : NetworkBehaviour
    {
        private CharacterBody jointBody;
        public bool reachedThreshold;
        public float nextCleansePercent = 0.9f;
        public static readonly float[] thresholds = { 0.75f, 0.5f };

        private void Start()
        {
            this.jointBody = this.GetComponent<CharacterBody>();
        }

        public float GetThresholdForPhase(int phase)
        {
            if (phase < 0 || phase >= thresholds.Length) return -1f;
            return thresholds[phase];
        }

        public void CleanseDebuffs()
        {
            if (NetworkServer.active)
                CleanseSystem.CleanseBodyServer(this.jointBody, true, false, false, false, false, false);
        }

        public void ResetForPhase()
        {
            this.reachedThreshold = false;
            this.jointBody.SetBuffCount(RoR2Content.Buffs.Immune.buffIndex, 0);
            float currentPercent = this.jointBody.healthComponent.health / this.jointBody.healthComponent.fullHealth;
            this.nextCleansePercent = Mathf.Floor(currentPercent * 10f) / 10f;
            if (this.nextCleansePercent >= currentPercent)
                this.nextCleansePercent -= 0.1f;
        }

        public static bool AllJointsReachedThreshold()
        {
            foreach (TeamComponent tc in TeamComponent.GetTeamMembers(TeamIndex.Void))
            {
                CharacterBody cb = tc.GetComponent<CharacterBody>();
                if (cb && cb.name == "VoidRaidCrabJointBody(Clone)")
                {
                    JointThresholdController jtc = cb.GetComponent<JointThresholdController>();
                    if (jtc && !jtc.reachedThreshold)
                        return false;
                }
            }
            return true;
        }

        public static void RemoveImmunityFromAllJoints()
        {
            foreach (TeamComponent tc in TeamComponent.GetTeamMembers(TeamIndex.Void).ToList())
            {
                CharacterBody cb = tc.GetComponent<CharacterBody>();
                if (cb && cb.name == "VoidRaidCrabJointBody(Clone)")
                {
                    JointThresholdController jtc = cb.GetComponent<JointThresholdController>();
                    if (jtc)
                        jtc.ResetForPhase();
                }
            }
        }
    }
}
