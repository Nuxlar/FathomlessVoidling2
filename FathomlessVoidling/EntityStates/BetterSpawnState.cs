using RoR2;
using RoR2.VoidRaidCrab;
using UnityEngine;
using UnityEngine.Networking;
using EntityStates;
using System;

namespace FathomlessVoidling
{
    public class BetterSpawnState : BaseState
    {
        public float duration = 10f;
        public float delay = 2f;
        public string spawnSoundString = "Play_voidRaid_spawn";
        public GameObject spawnEffectPrefab = Main.spawnEffect;
        public string animationLayerName = "Body";
        public string animationStateName = "Spawn";
        public string animationPlaybackRateParam = "Spawn.playbackRate";
        public bool doLegs = true;
        public bool playedAnim = false;
        public CharacterSpawnCard jointSpawnCard = Main.jointCard;
        public string leg1Name = "FrontLegL";
        public string leg2Name = "FrontLegR";
        public string leg3Name = "MidLegL";
        public string leg4Name = "MidLegR";
        public string leg5Name = "BackLegL";
        public string leg6Name = "BackLegR";
        private CharacterModel characterModel;

        public override void OnEnter()
        {
            base.OnEnter();
            Util.PlaySound(this.spawnSoundString, GameObject.Find("SpawnCamera"));
            this.characterModel = this.GetModelTransform().GetComponent<CharacterModel>();
            if ((bool)this.spawnEffectPrefab)
                EffectManager.SpawnEffect(this.spawnEffectPrefab, new EffectData() { origin = new Vector3(0, -60, 0), scale = 2, rotation = Quaternion.identity }, false);
            if (!this.doLegs || !NetworkServer.active)
                return;
            ChildLocator modelChildLocator = this.GetModelChildLocator();
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
            if ((bool)this.characterModel)
                ++this.characterModel.invisibilityCount;
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
                TeleportHelper.TeleportGameObject(this.gameObject, new Vector3(0, -10, 0));
                // TeleportHelper.TeleportBody(this.characterBody, new Vector3(0, -10, 0));
                this.PlayAnimation(this.animationLayerName, this.animationStateName, this.animationPlaybackRateParam, this.duration);
                this.playedAnim = true;
                if ((bool)this.characterModel)
                    --this.characterModel.invisibilityCount;
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