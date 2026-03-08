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
        [SyncVar]
        public int barnaclesKilled = 0;
        private CharacterBody jointBody;
        private List<CharacterMaster> membersList = new List<CharacterMaster>();
        private List<OnDestroyCallback> onDestroyCallbacksServer;
        private bool defeatedServer;

        private void OnDestroy()
        {
            if (NetworkServer.active)
            {
                GlobalEventManager.onCharacterDeathGlobal -= new Action<DamageReport>(this.OnCharacterDeathCallback);
            }
            for (int memberIndex = this.membersList.Count - 1; memberIndex >= 0; --memberIndex)
                this.RemoveMemberAt(memberIndex);
            this.onDestroyCallbacksServer = null;
        }

        private void Awake()
        {
            if (NetworkServer.active)
            {
                this.onDestroyCallbacksServer = new List<OnDestroyCallback>();
                GlobalEventManager.onCharacterDeathGlobal += new Action<DamageReport>(this.OnCharacterDeathCallback);
            }
        }

        private void Start()
        {
            this.jointBody = this.GetComponent<CharacterBody>();
        }

        public void TriggerThresholdEvent()
        {
            MasterSpawnSlotController slotController = this.GetComponent<MasterSpawnSlotController>();
            if (NetworkServer.active && (bool)slotController)
                slotController.SpawnRandomOpen(4, Run.instance.stageRng, this.gameObject, SpawnedBarnacle);
        }

        private void SpawnedBarnacle(MasterSpawnSlotController.ISlot slot, SpawnCard.SpawnResult result)
        {
            CharacterMaster master = result.spawnedInstance.GetComponent<CharacterMaster>();
            if (master)
                AddMember(master);
        }

        [Server]
        public void AddMember(CharacterMaster memberMaster)
        {
            if (!NetworkServer.active)
                Debug.LogWarning("FathomlessVoidling: [Server] function 'JointThresholdController::AddMember(RoR2.CharacterMaster)' called on client");
            else if (this.membersList.Count >= byte.MaxValue)
            {
                Debug.LogFormat("FathomlessVoidling: Cannot add character {0} to CombatGroup! Limit of {1} members already reached.", memberMaster, byte.MaxValue);
            }
            else
            {
                this.membersList.Add(memberMaster);
                this.onDestroyCallbacksServer.Add(OnDestroyCallback.AddCallback(memberMaster.gameObject, new Action<OnDestroyCallback>(this.OnMemberDestroyedServer)));
            }
        }

        [Server]
        private void OnCharacterDeathCallback(DamageReport damageReport)
        {
            if (!NetworkServer.active)
            {
                Debug.LogWarning("FathomlessVoidling: [Server] function 'JointThresholdController::OnCharacterDeathCallback(RoR2.DamageReport)' called on client");
            }
            else
            {
                CharacterMaster victimMaster = damageReport.victimMaster;
                if (!(bool)victimMaster)
                    return;
                int memberIndex = this.membersList.IndexOf(victimMaster);
                if (memberIndex < 0)
                    return;
                if (!victimMaster.IsDeadAndOutOfLivesServer())
                    return;
                this.RemoveMemberAt(memberIndex);
                if (this.defeatedServer || this.membersList.Count != 0)
                    return;
                this.TriggerDefeat();
            }
        }

        [Server]
        public void OnMemberDestroyedServer(OnDestroyCallback onDestroyCallback)
        {
            if (!NetworkServer.active)
            {
                Debug.LogWarning("FathomlessVoidling: [Server] function 'System.Void RoR2.CombatSquad::OnMemberDestroyedServer(RoR2.OnDestroyCallback)' called on client");
            }
            else
            {
                if (!(bool)onDestroyCallback)
                    return;
                GameObject gameObject = onDestroyCallback.gameObject;
                CharacterMaster component = (bool)gameObject ? gameObject.GetComponent<CharacterMaster>() : null;
                for (int index = 0; index < this.membersList.Count; ++index)
                {
                    if (this.membersList[index] == component)
                    {
                        this.membersList.RemoveAt(index);
                        break;
                    }
                }
            }
        }

        private void RemoveMemberAt(int memberIndex)
        {
            this.membersList.RemoveAt(memberIndex);
            if (this.onDestroyCallbacksServer != null)
                this.onDestroyCallbacksServer.RemoveAt(memberIndex);
        }

        private void TriggerDefeat()
        {
            this.defeatedServer = true;
            this.membersList?.Clear();
            this.jointBody.RemoveBuff(RoR2Content.Buffs.Immune);
        }

    }
}