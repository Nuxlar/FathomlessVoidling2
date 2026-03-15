using RoR2;
using RoR2.VoidRaidCrab;
using UnityEngine;
using UnityEngine.Networking;
using EntityStates;
using System;
using FathomlessVoidling.Controllers;

namespace FathomlessVoidling.EntityStates
{
    public class BetterSpawnState : BaseState
    {
        public float duration = 8f;
        public float delay = 1f;
        public float camDuration = 7f;
        public string spawnSoundString = "Play_voidRaid_spawn";
        public GameObject spawnEffectPrefab = Main.spawnEffect;
        public string animationLayerName = "Body";
        public string animationStateName = "Spawn";
        public string animationPlaybackRateParam = "Spawn.playbackRate";
        public bool doLegs = true;
        public CharacterSpawnCard jointSpawnCard = Main.jointCard;

        private string leg1Name = "FrontLegL";
        private string leg2Name = "FrontLegR";
        private string leg3Name = "MidLegL";
        private string leg4Name = "MidLegR";
        private string leg5Name = "BackLegL";
        private string leg6Name = "BackLegR";
        private bool activatedEye = false;
        private bool disabledCam = false;
        private bool spawnedTube = false;

        public override void OnEnter()
        {
            base.OnEnter();
            Util.PlaySound(this.spawnSoundString, GameObject.Find("SpawnCamera"));
            if ((bool)this.spawnEffectPrefab)
                EffectManager.SpawnEffect(this.spawnEffectPrefab, new EffectData() { origin = new Vector3(0, 0, 0), scale = 4, rotation = Quaternion.identity }, false);
            this.PlayAnimation(this.animationLayerName, this.animationStateName, this.animationPlaybackRateParam, this.duration);
            ChildLocator modelChildLocator = this.GetModelChildLocator();
            this.modelLocator.modelTransform.Find("VoidRaidCrabArmature/ROOT/HeadBase/eyeballRoot").gameObject.SetActive(false);

            this.characterBody.AddBuff(RoR2Content.Buffs.HiddenInvincibility);

            if (!NetworkServer.active)
                return;

            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(
                Main.voidlingHauntCard,
                new DirectorPlacementRule
                {
                    placementMode = DirectorPlacementRule.PlacementMode.Direct,
                    position = Vector3.zero
                },
                RoR2Application.rng
            );
            spawnRequest.teamIndexOverride = this.GetTeam();
            spawnRequest.ignoreTeamMemberLimit = true;

            SpawnCard.SpawnResult spawnResult = Main.voidlingHauntCard.DoSpawn(Vector3.zero, Quaternion.identity, spawnRequest);
            if (FathomlessMissionController.instance && spawnResult.spawnedInstance)
                FathomlessMissionController.instance.hauntBody = spawnResult.spawnedInstance.GetComponent<CharacterMaster>().GetBody();

            if (!this.doLegs || !this.jointSpawnCard || !modelChildLocator)
                return;

            DirectorPlacementRule placementRule = new DirectorPlacementRule()
            {
                placementMode = DirectorPlacementRule.PlacementMode.Direct,
                spawnOnTarget = this.GetModelTransform()
            };
            this.SpawnJointBodyForLegServer(this.leg1Name, modelChildLocator, placementRule);
            this.SpawnJointBodyForLegServer(this.leg2Name, modelChildLocator, placementRule);
            this.SpawnJointBodyForLegServer(this.leg3Name, modelChildLocator, placementRule);
            this.SpawnJointBodyForLegServer(this.leg4Name, modelChildLocator, placementRule);
            this.SpawnJointBodyForLegServer(this.leg5Name, modelChildLocator, placementRule);
            this.SpawnJointBodyForLegServer(this.leg6Name, modelChildLocator, placementRule);
        }

        private void SpawnJointBodyForLegServer(
          string legName,
          ChildLocator childLocator,
          DirectorPlacementRule placementRule)
        {
            GameObject gameObject = DirectorCore.instance?.TrySpawnObject(new DirectorSpawnRequest(this.jointSpawnCard, placementRule, Run.instance.stageRng)
            {
                summonerBodyObject = this.gameObject,
            });
            Transform child = childLocator.FindChild(legName);
            if (!(bool)gameObject && !(bool)child)
                return;
            CharacterMaster component1 = gameObject.GetComponent<CharacterMaster>();
            if (!(bool)component1)
                return;
            LegController component2 = child.GetComponent<LegController>();
            if (!(bool)component2)
                return;
            component2.SetJointMaster(component1, child.GetComponent<ChildLocator>());
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (this.fixedAge >= this.delay && !spawnedTube)
            {
                this.spawnedTube = true;
                Main.CreateTube();
            }
            if (this.fixedAge >= 4.5f && !this.activatedEye)
            {
                this.modelLocator.modelTransform.Find("VoidRaidCrabArmature/ROOT/HeadBase/eyeballRoot").gameObject.SetActive(true);
                this.activatedEye = true;
            }
            if (this.fixedAge >= this.camDuration && !this.disabledCam)
            {
                GameObject.Find("Forced Camera").SetActive(false);
                this.disabledCam = true;
            }
            if (this.fixedAge < this.duration || !this.isAuthority)
                return;
            this.outer.SetNextStateToMain();
        }

        public override void OnExit()
        {
            base.OnExit();
            //  this.characterBody.skillLocator.secondary.RemoveAllStocks();
            this.characterBody.skillLocator.utility.RemoveAllStocks();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Death;
    }
}