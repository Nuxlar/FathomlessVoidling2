using EntityStates;
using EntityStates.VoidRaidCrab;
using FathomlessVoidling.Controllers;
using RoR2;
using RoR2.Skills;
using System.Collections.Generic;
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

        public override void OnEnter()
        {
            base.OnEnter();
            this.PlayAnimation(this.animationLayerName, this.animationStateName, this.animationPlaybackRateParam, this.duration);
            Util.PlaySound(this.enterSoundString, this.gameObject);
            if (this.muzzleFlashPrefab)
                EffectManager.SimpleMuzzleFlash(this.muzzleFlashPrefab, this.gameObject, this.muzzleName, false);
            if (this.nextSkillDef)
            {
                GenericSkill skillByDef = this.skillLocator.FindSkillByDef(this.skillDefToReplaceAtStocksEmpty);
                if (skillByDef && skillByDef.stock == 0)
                    skillByDef.SetBaseSkill(this.nextSkillDef);
            }
            if (!this.fogDamageController)
                return;
            if (NetworkServer.active)
            {
                foreach (CharacterBody affectedBody in this.fogDamageController.GetAffectedBodies())
                {
                    if (!this.requiredBuffToKill || affectedBody.HasBuff(this.requiredBuffToKill))
                        affectedBody.healthComponent.Suicide(this.gameObject, this.gameObject, DamageType.VoidDeath);
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
            if (!this.isAuthority || this.fixedAge < this.duration)
                return;
            this.outer.SetNextStateToMain();
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
                Debug.LogWarning("teleporting john haunt");
                TeleportHelper.TeleportBody(FathomlessMissionController.instance.hauntBody, crabSpawnPos, false);
            }

            List<CharacterBody> playerBodies = Main.GetPlayerBodies();
            // RaidDC
            if (VoidRaidGauntletController.instance.currentDonut.root.name == "RaidDC")
            {
                Transform roof = VoidRaidGauntletController.instance.currentDonut.root.transform.Find("HOLDER: ROOF");
                if (roof.gameObject.activeSelf)
                    roof.gameObject.SetActive(false);
            }
            /*
            if (VoidRaidGauntletController.instance.currentDonut.root.name == "RaidBB")
            {
                Transform pp = VoidRaidGauntletController.instance.currentDonut.root.transform.Find("HOLDER: Skybox+PP");
                pp.Find("PP + Amb").GetComponent<SetAmbientLight>().ApplyLighting();
            }
            */
            if (playerBodies.Count > 0)
            {
                foreach (CharacterBody playerBody in playerBodies)
                {

                    Vector3? teleportPos = TeleportHelper.FindSafeTeleportDestination(VoidRaidGauntletController.instance.currentDonut.returnPoint.position, playerBody, Run.instance.runRNG);
                    //  TeleportHelper.TeleportBody(playerBody, (Vector3)teleportPos, false);
                    TeleportHelper.TeleportBody(new TeleportHelper.TeleportBodyArgs()
                    {
                        body = playerBody,
                        targetPosition = (Vector3)teleportPos,
                        forceOutOfVehicle = true,
                        teleportMinions = true,
                        resetStateMachines = true
                    });
                    GameObject effectPrefab = Run.instance.GetTeleportEffectPrefab(playerBody.gameObject);
                    effectPrefab = Main.raidTeleportEffect;
                    if (effectPrefab)
                        EffectManager.SimpleEffect(effectPrefab, (Vector3)teleportPos, Quaternion.identity, true);
                }
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Death;
    }
}
