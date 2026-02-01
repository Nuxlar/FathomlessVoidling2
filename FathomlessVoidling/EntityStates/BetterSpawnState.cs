using RoR2;
using RoR2.VoidRaidCrab;
using UnityEngine;
using UnityEngine.Networking;
using EntityStates;
using System;

namespace FathomlessVoidling.EntityStates
{
    public class BetterSpawnState : BaseState
    {
        public float duration = 7.5f;
        public float delay = 1f;
        public float camDuration = 7f;
        public string spawnSoundString = "Play_voidRaid_spawn";
        public GameObject spawnEffectPrefab = Main.spawnEffect;
        public string animationLayerName = "Body";
        public string animationStateName = "Spawn";
        public string animationPlaybackRateParam = "Spawn.playbackRate";
        public bool doLegs = true;
        public bool activatedEye = false;
        public bool disabledCam = false;
        public bool playedAnim = false;
        public CharacterSpawnCard jointSpawnCard = Main.jointCard;
        public string leg1Name = "FrontLegL";
        public string leg2Name = "FrontLegR";
        public string leg3Name = "MidLegL";
        public string leg4Name = "MidLegR";
        public string leg5Name = "BackLegL";
        public string leg6Name = "BackLegR";

        public override void OnEnter()
        {
            base.OnEnter();
            Util.PlaySound(this.spawnSoundString, GameObject.Find("SpawnCamera"));
            if ((bool)this.spawnEffectPrefab)
                EffectManager.SpawnEffect(this.spawnEffectPrefab, new EffectData() { origin = new Vector3(0, -10, 0), scale = 4, rotation = Quaternion.identity }, false);
            this.PlayAnimation(this.animationLayerName, this.animationStateName, this.animationPlaybackRateParam, this.duration);
            ChildLocator modelChildLocator = this.GetModelChildLocator();
            this.modelLocator.modelTransform.Find("VoidRaidCrabArmature/ROOT/HeadBase/eyeballRoot").gameObject.SetActive(false);
            if (!this.doLegs || !NetworkServer.active)
                return;
            if (!(bool)this.jointSpawnCard || !(bool)modelChildLocator)
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
            if ((double)this.fixedAge >= (double)this.delay && !playedAnim)
            {
                // FathomlessVoidling.CreateTube();
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
            if ((double)this.fixedAge < (double)this.duration || !this.isAuthority)
                return;
            this.outer.SetNextStateToMain();
        }

        public override void OnExit()
        {
            base.OnExit();
            this.characterBody.skillLocator.secondary.RemoveAllStocks();
            this.characterBody.skillLocator.utility.RemoveAllStocks();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Death;
    }
}