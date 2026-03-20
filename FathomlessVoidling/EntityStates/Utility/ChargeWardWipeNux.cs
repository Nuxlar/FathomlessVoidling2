using EntityStates;
using EntityStates.VoidRaidCrab;
using FathomlessVoidling.Controllers;
using RoR2;
using RoR2.Audio;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace FathomlessVoidling.EntityStates.Utility
{
    public class ChargeWardWipeNux : BaseWardWipeState
    {
        public float duration = 8f;
        public GameObject chargeEffectPrefab = Main.chargeWardWipeChargeEffect;
        public string muzzleName = "Pupil";
        public string animationLayerName = "Body";
        public string animationStateName = "ChargeWipe";
        public string animationPlaybackRateParam = "Wipe.playbackRate";
        public string enterSoundString = "Play_voidRaid_fog_chargeUp";
        public LoopSoundDef loopSound = Main.lsdVoidMegaCrabDeathBomb;
        public InteractableSpawnCard safeWardSpawnCard = Main.iscSafeWard;
        public AnimationCurve safeWardSpawnCurve = new AnimationCurve(
            new Keyframe(0f, 10f, 5.555556f, 5.555556f, 0f, 0.3333333f),
            new Keyframe(0.9f, 15f, 5.555556f, 5.555556f, 0.3333333f, 0f)
        )
        { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever };
        public float minDistanceBetweenConsecutiveWards = 200f;
        public float maxDistanceBetweenConsecutiveWards = 600f;
        public float maxDistanceToInitialWard = 600f;
        public float loopSoundDelay = 2.65f;
        private float loopSoundStopwatch = 0f;
        private bool loopSoundFired = false;
        private GameObject chargeEffectInstance;
        private List<LoopSoundManager.SoundLoopPtr> loopPtrs = new List<LoopSoundManager.SoundLoopPtr>();

        public override void OnEnter()
        {
            base.OnEnter();
            this.PlayAnimation(this.animationLayerName, this.animationStateName, this.animationPlaybackRateParam, this.duration);
            Util.PlaySound(this.enterSoundString, this.gameObject);
            ChildLocator modelChildLocator = this.GetModelChildLocator();
            if (modelChildLocator && this.chargeEffectPrefab)
            {
                Transform transform = modelChildLocator.FindChild(this.muzzleName) ?? this.characterBody.coreTransform;
                if (transform)
                {
                    this.chargeEffectInstance = GameObject.Instantiate(this.chargeEffectPrefab, transform.position, transform.rotation);
                    this.chargeEffectInstance.transform.parent = transform;
                    ScaleParticleSystemDuration component = this.chargeEffectInstance.GetComponent<ScaleParticleSystemDuration>();
                    if (component)
                        component.newDuration = this.duration;
                }
            }
            this.fogDamageController = this.GetComponent<FogDamageController>();
            this.fogDamageController.enabled = true;
            this.safeWards = new List<GameObject>();
            if (FathomlessMissionController.instance)
            {
                if (FathomlessMissionController.instance.wardWipeDriver)
                    FathomlessMissionController.instance.wardWipeDriver.enabled = false;
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            this.loopSoundStopwatch += Time.fixedDeltaTime;
            if (!this.loopSoundFired && this.loopSoundStopwatch >= this.loopSoundDelay)
            {
                this.loopSoundFired = true;
                foreach (CharacterBody playerBody in Main.GetPlayerBodies())
                    this.loopPtrs.Add(LoopSoundManager.PlaySoundLoopLocal(playerBody.gameObject, this.loopSound));
            }
            if (NetworkServer.active && this.safeWardSpawnCard)
            {
                float f = this.safeWardSpawnCurve.Evaluate(this.fixedAge / this.duration);
                DirectorPlacementRule placementRule = null;
                while (this.safeWards.Count < Mathf.Floor(f))
                {
                    if (placementRule == null)
                        placementRule = new DirectorPlacementRule()
                        {
                            placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                            maxDistance = this.maxDistanceToInitialWard,
                            minDistance = 0f,
                            spawnOnTarget = this.gameObject.transform,
                            preventOverhead = true
                        }
                    ;
                    if (this.safeWards.Count > 0)
                    {
                        placementRule.maxDistance = this.maxDistanceBetweenConsecutiveWards;
                        placementRule.minDistance = this.minDistanceBetweenConsecutiveWards;
                        placementRule.spawnOnTarget = this.safeWards[this.safeWards.Count - 1].transform;
                        placementRule.placementMode = DirectorPlacementRule.PlacementMode.Approximate;
                    }
                    GameObject ward = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest((SpawnCard)this.safeWardSpawnCard, placementRule, Run.instance.stageRng));
                    if (ward)
                    {
                        NetworkServer.Spawn(ward);
                        if (this.fogDamageController)
                            this.fogDamageController.AddSafeZone(ward.GetComponent<IZone>());
                        this.safeWards.Add(ward);
                    }
                    else
                        Debug.LogError("FathomlessVoidling: Unable to spawn safe ward instance.  Are there any ground nodes?");
                }
            }
            if (!this.isAuthority || this.fixedAge < this.duration)
                return;
            this.outer.SetNextState(new FireWardWipeNux());
        }

        public override void OnExit()
        {
            foreach (LoopSoundManager.SoundLoopPtr loopPtr in this.loopPtrs)
                LoopSoundManager.StopSoundLoopLocal(loopPtr);
            Destroy(this.chargeEffectInstance);
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Death;
    }
}
