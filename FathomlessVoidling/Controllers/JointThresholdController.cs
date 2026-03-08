using RoR2;
using RoR2.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace FathomlessVoidling.Controllers
{
    public class JointThresholdController : NetworkBehaviour
    {
        public static event Action<JointThresholdController> onDefeatedServerGlobal;
        public CombatDirector combatDirector;
        private CharacterBody jointBody;

        public void Start()
        {
            if (!NetworkServer.active)
                return;
            this.jointBody = this.GetComponent<CharacterBody>();
            this.combatDirector = this.GetComponent<CombatDirector>();
            this.combatDirector.monsterCards = Main.barnacleDccs;
            this.combatDirector.teamIndex = TeamIndex.Void;
            this.combatDirector.currentSpawnTarget = this.jointBody.gameObject;
            //  this.combatDirector.combatSquad.AddMember(Main.barnacleMaster.GetComponent<CharacterMaster>());
            this.combatDirector.combatSquad.onDefeatedServer += new Action(this.OnDefeatedServer);
        }

        private void OnDefeatedServer()
        {
            this.jointBody.RemoveBuff(RoR2Content.Buffs.Immune);
            this.combatDirector.enabled = false;
            /*
            Action<JointThresholdController> defeatedServerGlobal = JointThresholdController.onDefeatedServerGlobal;
            if (defeatedServerGlobal == null)
                return;
            defeatedServerGlobal(this);
            */
        }
    }
}