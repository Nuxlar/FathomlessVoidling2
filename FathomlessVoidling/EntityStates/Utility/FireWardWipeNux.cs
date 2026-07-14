using EntityStates;
using EntityStates.VoidRaidCrab;
using FathomlessVoidling.Controllers;
using RoR2;
using RoR2.Skills;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace FathomlessVoidling.EntityStates.Utility
{
    public class FireWardWipeNux : BaseWardWipeState
    {
        public float duration = 6f;
        public string muzzleName = "Root";
        public GameObject muzzleFlashPrefab = Main.fireWardWipeMuzzleFlash;
        public string animationLayerName = "Body";
        public string animationStateName = "FireWipe";
        public string animationPlaybackRateParam = "Wipe.playbackRate";
        public string enterSoundString;
        public SkillDef skillDefToReplaceAtStocksEmpty = Main.sdSingularity;
        public SkillDef nextSkillDef = Main.sdSingularity;
        public BuffDef requiredBuffToKill = Main.bdWardWipeFog;
        public float teleportDelay = 0.55f;
        private float teleportStopwatch = 0f;
        private bool teleportFired = false;
        private float teleportRetryStopwatch = 0f;
        private int lastDonutIndex = -1;
        private readonly List<(CharacterBody body, Vector3 pos)> pendingTeleports = new();

        public override void OnEnter()
        {
            base.OnEnter();
            this.PlayAnimation(this.animationLayerName, this.animationStateName, this.animationPlaybackRateParam, this.duration);
            Util.PlaySound(this.enterSoundString, this.gameObject);
            if (this.muzzleFlashPrefab)
                EffectManager.SimpleMuzzleFlash(this.muzzleFlashPrefab, this.gameObject, this.muzzleName, false);
            if (this.nextSkillDef)
                this.skillLocator.special.SetSkillOverride(this.outer, this.nextSkillDef, GenericSkill.SkillOverridePriority.Contextual);
            if (!this.fogDamageController)
                return;
            if (NetworkServer.active)
            {
                foreach (CharacterBody affectedBody in this.fogDamageController.GetAffectedBodies())
                {
                    if (affectedBody.isPlayerControlled && affectedBody.HasBuff(this.requiredBuffToKill))
                        affectedBody.master.TrueKill(this.gameObject, this.gameObject, DamageType.VoidDeath);
                }
            }
            this.fogDamageController.enabled = false;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            this.teleportStopwatch += Time.fixedDeltaTime;
            if (!this.teleportFired && this.teleportStopwatch >= this.teleportDelay)
            {
                this.teleportFired = true;
                this.NextDonut();
            }
            
            if (this.pendingTeleports.Count > 0 && NetworkServer.active)
            {
                this.teleportRetryStopwatch += Time.fixedDeltaTime;
                if (this.teleportRetryStopwatch >= 1f)
                {
                    this.teleportRetryStopwatch = 0f;
                    this.pendingTeleports.RemoveAll(p => !p.body || !p.body.healthComponent.alive || (p.body.footPosition - p.pos).sqrMagnitude < 10000f);
                    foreach ((CharacterBody body, Vector3 pos) in this.pendingTeleports)
                    {
                        if (VoidRaidGauntletController.instance && this.lastDonutIndex >= 0)
                            VoidRaidGauntletController.instance.CallRpcActivateDonut(this.lastDonutIndex);
                        body.CallRpcTeleportWithLocalAuthority(new TeleportHelper.TeleportBodyArgs
                        {
                            body = body,
                            targetPosition = pos,
                            teleportMinions = true,
                            resetStateMachines = true
                        });
                    }
                }
            }
            if (!this.isAuthority || this.fixedAge < this.duration)
                return;
            this.outer.SetNextStateToMain();
        }

        private void KillBarnacles()
        {
            foreach (TeamComponent teamComponent in TeamComponent.GetTeamMembers(TeamIndex.Void).ToList())
            {
                CharacterBody characterBody = teamComponent.GetComponent<CharacterBody>();
                if (characterBody && characterBody.name == "VoidBarnacleBody(Clone)")
                    characterBody.healthComponent.Suicide();
            }
        }

        private void NextDonut()
        {
            if (!VoidRaidGauntletController.instance || !NetworkServer.active)
                return;

            VoidRaidGauntletController.instance.previousGauntlet = VoidRaidGauntletController.instance.currentGauntlet;
            VoidRaidGauntletController.instance.previousDonut = VoidRaidGauntletController.instance.currentDonut;
            if (VoidRaidGauntletController.instance.previousDonut != null && (bool)VoidRaidGauntletController.instance.previousDonut.combatDirector)
            {
                VoidRaidGauntletController.instance.previousDonut.combatDirector.monsterCredit = 0.0f;
                VoidRaidGauntletController.instance.previousDonut.combatDirector.enabled = false;
            }
            int donutIndex = VoidRaidGauntletController.instance.gauntletIndex % VoidRaidGauntletController.instance.followingDonuts.Length;
            this.lastDonutIndex = donutIndex;
            VoidRaidGauntletController.instance.currentDonut = VoidRaidGauntletController.instance.followingDonuts[donutIndex];
            VoidRaidGauntletController.instance.currentGauntlet = VoidRaidGauntletController.instance.gauntlets[VoidRaidGauntletController.instance.gauntletIndex % VoidRaidGauntletController.instance.gauntlets.Length];
            ++VoidRaidGauntletController.instance.gauntletIndex;
            VoidRaidGauntletController.instance.CallRpcTryShuffleData(VoidRaidGauntletController.instance.rngSeed);

            if ((bool)VoidRaidGauntletController.instance.currentDonut.root)
            {
                VoidRaidGauntletController.instance.currentDonut.root.SetActive(true);
                VoidRaidGauntletController.instance.CallRpcActivateDonut(donutIndex);
            }
            if (SceneInfo.instance != null && !string.IsNullOrEmpty(VoidRaidGauntletController.instance.currentGauntlet?.gateName))
            {
                SceneInfo.instance.SetGateState(VoidRaidGauntletController.instance.currentGauntlet.gateName, true);
                VoidRaidGauntletController.instance.CallRpcActivateGate(VoidRaidGauntletController.instance.currentGauntlet?.gateName);
            }

            if ((bool)VoidRaidGauntletController.instance.previousDonut?.combatDirector)
                VoidRaidGauntletController.instance.previousDonut.combatDirector.enabled = false;

            Vector3 crabPosition = VoidRaidGauntletController.instance.currentDonut.root.transform.Find("HOLDER: Terrain").Find("RaidTerrainHG").position;
            Vector3 crabSpawnPos = new Vector3(crabPosition.x, crabPosition.y - 15f, crabPosition.z);
            TeleportHelper.TeleportBody(this.characterBody, crabSpawnPos, false);
            if (FathomlessMissionController.instance && FathomlessMissionController.instance.hauntBody)
            {
                TeleportHelper.TeleportBody(FathomlessMissionController.instance.hauntBody, crabSpawnPos, false);
            }

            List<CharacterBody> playerBodies = Main.GetPlayerBodies();
            foreach (CharacterBody playerBody in playerBodies)
            {
                Vector3? teleportPos = TeleportHelper.FindSafeTeleportDestination(VoidRaidGauntletController.instance.currentDonut.returnPoint.position, playerBody, Run.instance.runRNG);
                if (!teleportPos.HasValue)
                    continue;

                TeleportHelper.TeleportBodyArgs teleportArgs = new()
                {
                    body = playerBody,
                    targetPosition = teleportPos.Value,
                    teleportMinions = true,
                    resetStateMachines = true
                };
                playerBody.CallRpcTeleportWithLocalAuthority(teleportArgs);
                this.pendingTeleports.Add((playerBody, teleportPos.Value));

                GameObject effectPrefab = Main.raidTeleportEffect;
                if (effectPrefab)
                    EffectManager.SimpleEffect(effectPrefab, teleportPos.Value, Quaternion.identity, true);
            }
            KillBarnacles();
        }

        public override void OnExit()
        {
            base.OnExit();
            this.skillLocator.special.UnsetSkillOverride(this.outer, Main.sdWardWipe, GenericSkill.SkillOverridePriority.Contextual);
            if (NetworkServer.active)
            {
                PhasedInventorySetter phasedInventorySetter = this.characterBody.GetComponent<PhasedInventorySetter>();
                if (phasedInventorySetter)
                    phasedInventorySetter.AdvancePhase();
            }
            JointThresholdController.RemoveImmunityFromAllJoints();
            FathomlessMissionController mc = FathomlessMissionController.instance;
            if (mc)
            {
                int phase = mc.GetCurrentPhase();
                if (phase == 1)
                {
                    GameObject phases = GameObject.Find("EncounterPhases");
                    if (phases)
                    {
                        Transform music = phases.transform.GetChild(0).Find("Phase2Music");
                        if (music)
                            music.gameObject.SetActive(true);
                    }
                }
                if (phase == 2)
                {
                    this.skillLocator.special.AddOneStock();
                    this.skillLocator.utility.AddOneStock();
                }
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Death;
    }
}
