using UnityEngine;
using RoR2;
using RoR2.VoidRaidCrab;
using UnityEngine.SceneManagement;
using FathomlessVoidling.EntityStates.Barnacle;
using FathomlessVoidling.Controllers;
using UnityEngine.Playables;
using UnityEngine.Networking;
using EntityStates.VoidBarnacle.Weapon;
using R2API;
using EntityStates.VoidRaidCrab;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Rendering.PostProcessing;
using FathomlessVoidling.EntityStates.Utility;
using FathomlessVoidling.EntityStates.Joint;
using EntityStates.VoidRaidCrab.Joint;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Reflection;

namespace FathomlessVoidling.Hooks
{
    public class ConnectHooks
    {
        public ConnectHooks()
        {
            GlobalEventManager.onServerDamageDealt += ApplyGravityDamageType;

            On.RoR2.SceneDirector.Start += TweakBossDirector;
            On.RoR2.HealthComponent.SendDamageDealt += ThresholdCheck;
            On.RoR2.CharacterMaster.OnBodyStart += FixPipReviveBug;
            On.RoR2.VoidRaidCrab.LegController.RegenerateServer += PreventJointRegen;
            On.RoR2.VoidRaidGauntletController.RpcActivateDonut += DeactivateDonutRoof;

            On.EntityStates.VoidBarnacle.Weapon.ChargeFire.OnEnter += LazyMf;
            IL.RoR2.VoidRaidCrab.LegController.CompleteBreakAuthority += ReplaceJointBreakState;
            /*
                PhaseControllerStateMachine (GameObject)
                has NetworkIdentity, EntityStateMachine, and NetworkStateMachine
            */
        }

        private void ReplaceJointBreakState(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.After, x => x.MatchIsinst<PreDeathState>()))
                c.Prev.Operand = il.Import(typeof(JointPreDeathNux));
            if (c.TryGotoNext(MoveType.After, x => x.MatchStfld<PreDeathState>("canProceed")))
                c.Prev.Operand = il.Import(typeof(JointPreDeathNux).GetField("canProceed"));
        }

        private void DeactivateDonutRoof(On.RoR2.VoidRaidGauntletController.orig_RpcActivateDonut orig, VoidRaidGauntletController self, int donutIndex)
        {
            orig(self, donutIndex);
            if (self.currentDonut?.root?.name == "RaidDC")
            {
                Transform roof = self.currentDonut.root.transform.Find("HOLDER: ROOF");
                if (roof && roof.gameObject.activeSelf)
                    roof.gameObject.SetActive(false);
            }
        }

        private void PreventJointRegen(On.RoR2.VoidRaidCrab.LegController.orig_RegenerateServer orig, LegController self)
        {

        }

        private void FixPipReviveBug(On.RoR2.CharacterMaster.orig_OnBodyStart orig, CharacterMaster self, CharacterBody body)
        {
            orig(self, body);
            if (body.isPlayerControlled && SceneManager.GetActiveScene().name == "voidraid" && body.HasBuff(RoR2Content.Buffs.Immune))
            {
                GameObject crab = GameObject.Find("VoidRaidCrabBody(Clone)");
                if (crab)
                    crab.GetComponent<VoidRaidCrabHealthBarOverlayProvider>().OnEnable();
            }
        }

        private void LazyMf(On.EntityStates.VoidBarnacle.Weapon.ChargeFire.orig_OnEnter orig, ChargeFire self)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "voidraid")
                self.outer.SetState(new ChargeGravityBullet());
            else
                orig(self);
        }

        private void ThresholdCheck(On.RoR2.HealthComponent.orig_SendDamageDealt orig, DamageReport damageReport)
        {
            HealthComponent hc = damageReport.victim.gameObject.GetComponent<HealthComponent>();
            CharacterBody body = hc.body;

            if (body && hc && body.name == "VoidRaidCrabJointBody(Clone)")
            {
                JointThresholdController jointThresholdController = body.GetComponent<JointThresholdController>();
                // heal other joints on a death blow
                if (hc.health - damageReport.damageDealt <= 0f)
                {
                    if (jointThresholdController && jointThresholdController.defeatedServer)
                    {
                        List<CharacterBody> jointBodies = new List<CharacterBody>();
                        foreach (TeamComponent teamComponent in TeamComponent.GetTeamMembers(TeamIndex.Void).ToList())
                        {
                            CharacterBody characterBody = teamComponent.GetComponent<CharacterBody>();
                            if (characterBody && characterBody.name == "VoidRaidCrabJointBody(Clone)" && characterBody.netId != body.netId)
                                jointBodies.Add(characterBody);
                        }

                        foreach (CharacterBody jointBody in jointBodies)
                        {
                            jointBody.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, 5f);
                            jointBody.healthComponent.Heal(hc.fullHealth, new ProcChainMask());
                        }
                        // damage main body
                        if (FathomlessMissionController.instance && FathomlessMissionController.instance.voidlingBody)
                        {
                            CharacterBody bossBody = FathomlessMissionController.instance.voidlingBody;
                            bossBody.RemoveBuff(RoR2Content.Buffs.HiddenInvincibility);
                            bossBody.healthComponent.TakeDamage(new DamageInfo()
                            {
                                damage = 9999999f,
                                crit = false,
                                rejected = false,
                                delayedDamageSecondHalf = false,
                                firstHitOfDelayedDamageSecondHalf = false,
                            });
                            bossBody.AddBuff(RoR2Content.Buffs.HiddenInvincibility);
                            // swap special skill to ward wipe and enable its driver
                            SkillLocator skillLocator = bossBody.GetComponent<SkillLocator>();

                            if (skillLocator && Main.sdWardWipe)
                            {
                                EntityStateMachine esm = bossBody.gameObject.GetComponent<EntityStateMachine>();
                                skillLocator.special.SetSkillOverride(esm, Main.sdWardWipe, GenericSkill.SkillOverridePriority.Contextual);
                            }
                            if (FathomlessMissionController.instance)
                            {
                                if (FathomlessMissionController.instance.wardWipeDriver)
                                    FathomlessMissionController.instance.wardWipeDriver.enabled = true;
                                if (FathomlessMissionController.instance.singularityDriver)
                                    FathomlessMissionController.instance.singularityDriver.enabled = false;
                                if (FathomlessMissionController.instance.mazeDriver)
                                    FathomlessMissionController.instance.mazeDriver.enabled = false;
                                if (FathomlessMissionController.instance.fireMissileDriver)
                                    FathomlessMissionController.instance.fireMissileDriver.enabled = false;
                            }
                        }
                    }
                }
                if (jointThresholdController && !jointThresholdController.defeatedServer && hc.health - damageReport.damageDealt <= hc.fullHealth * 0.75f)
                {
                    body.AddBuff(RoR2Content.Buffs.Immune);
                    jointThresholdController.TriggerThresholdEvent();
                    hc.health = hc.fullHealth * 0.75f;
                    damageReport.damageDealt = 1f;
                    if (FathomlessMissionController.instance)
                    {
                        int phase = FathomlessMissionController.instance.GetCurrentPhase();
                        if (phase == 0 && FathomlessMissionController.instance.singularityDriver)
                            FathomlessMissionController.instance.singularityDriver.enabled = true;
                        else if (phase == 1 && FathomlessMissionController.instance.mazeDriver)
                            FathomlessMissionController.instance.mazeDriver.enabled = true;
                    }
                }
            }
            orig(damageReport);
        }

        private void TweakBossDirector(On.RoR2.SceneDirector.orig_Start orig, RoR2.SceneDirector self)
        {
            if (SceneManager.GetActiveScene().name == "voidraid")
            {
                // Weather, Void Raid Starry Night Variant PP + Amb postprocessvolume rampfog setting fogcolorstart 0.1887 0.1629 0.1629 0
                // Weather Tweaks
                //   PostProcessVolume ppv = GameObject.Find("Weather, Void Raid Starry Night Variant").transform.Find("PP + Amb").GetComponent<PostProcessVolume>();
                //   ppv.profile.GetSetting<RampFog>().fogColorStart.value = new Color(0.1887f, 0.1629f, 0.1629f, 0.2f);
                GameObject missionObj = GameObject.Find("EncounterPhases");
                GameObject phase1Obj = missionObj.transform.GetChild(0).gameObject;
                if (missionObj && phase1Obj)
                {
                    phase1Obj.AddComponent<FathomlessMissionController>();
                    missionObj.transform.GetChild(1).gameObject.SetActive(false);
                    missionObj.transform.GetChild(2).gameObject.SetActive(false);

                    Transform transform = new GameObject().transform;
                    transform.position = new Vector3(0, -15, 0);

                    ScriptedCombatEncounter.SpawnInfo spawnInfo = new ScriptedCombatEncounter.SpawnInfo();
                    spawnInfo.explicitSpawnPosition = transform;
                    spawnInfo.spawnCard = Main.bigVoidlingCard;

                    phase1Obj.GetComponent<ScriptedCombatEncounter>().spawns = [spawnInfo];

                    Transform cam = GameObject.Find("RaidVoid").transform.GetChild(5);
                    Transform forcedCam = cam.GetChild(1);
                    forcedCam.GetComponent<PlayableDirector>().playableAsset = Main.introTimeline;
                    Transform curve = cam.GetChild(2);
                    // y -6.812038f
                    curve.position = new Vector3(-110.27766f, 15f, -300f);
                    curve.GetChild(0).position = new Vector3(-50f, 28.9719f, -396.993f); // orig -6.215 28.9719 -396.993
                }
            }
            orig(self);
        }

        private void ApplyGravityDamageType(DamageReport obj)
        {
            if (obj.damageInfo.HasModdedDamageType(Main.gravityDamageType) && obj.victimBody)
            {
                CharacterDirection direction = obj.victimBody.GetComponent<CharacterDirection>();
                CharacterMotor motor = obj.victimBody.GetComponent<CharacterMotor>();
                if (direction && motor)
                {
                    float airborneForce = 3000f;
                    float groundedForce = 3000f; // 7000 orig
                    Vector3 airborneForceVector;
                    Vector3 groundedForceVector;
                    bool preventAirControl = false;
                    bool isLeft = (double)UnityEngine.Random.value > 0.5;
                    Vector3 vector3 = Vector3.Cross(direction.forward, Vector3.up);

                    if (!isLeft)
                        vector3 *= -1f;

                    airborneForceVector = Vector3.up * -1f * airborneForce + vector3 * airborneForce;
                    groundedForceVector = Vector3.up * groundedForce + vector3 * groundedForce;

                    EffectData effectData = new EffectData()
                    {
                        origin = obj.victimBody.transform.position
                    };
                    GameObject effectPrefab;
                    if (motor.isGrounded)
                    {
                        // this.disableAirControlUntilCollision
                        motor.ApplyForce(groundedForceVector, true, preventAirControl);
                        effectPrefab = Main.groundedGravityEffect;
                        effectData.rotation = Util.QuaternionSafeLookRotation(groundedForceVector);
                    }
                    else
                    {
                        motor.ApplyForce(airborneForceVector, true, preventAirControl);
                        effectPrefab = Main.airborneGravityEffect;
                        effectData.rotation = Util.QuaternionSafeLookRotation(airborneForceVector);
                    }
                    EffectManager.SpawnEffect(effectPrefab, effectData, true);
                }
            }
        }

        /*
    private void IncreaseSingularitySize(On.EntityStates.VoidRaidCrab.VacuumAttack.orig_OnEnter orig, VacuumAttack self)
    {
      AnimationCurve newRadiusCurve = new AnimationCurve();
      newRadiusCurve.preWrapMode = WrapMode.ClampForever;
      newRadiusCurve.postWrapMode = WrapMode.ClampForever;
      newRadiusCurve.AddKey(new Keyframe() { time = 0f, value = 0f, inTangent = 125f, outTangent = 125f, inWeight = 0f, outWeight = 0.3333333432674408f, weightedMode = WeightedMode.None, tangentModeInternal = 34 });
      newRadiusCurve.AddKey(new Keyframe() { time = 1f, value = 125f, inTangent = 125f, outTangent = 125f, inWeight = 0.3333333432674408f, outWeight = 0f, weightedMode = WeightedMode.None, tangentModeInternal = 34 });
      // original curve
      // {"preWrapMode":8,"postWrapMode":8,"keys":[{"time":0.0,"value":0.0,"inTangent":50.0,"outTangent":50.0,"inWeight":0.0,"outWeight":0.3333333432674408,"weightedMode":0,"tangentMode":34},{"time":1.0,"value":50.0,"inTangent":50.0,"outTangent":50.0,"inWeight":0.3333333432674408,"outWeight":0.0,"weightedMode":0,"tangentMode":34}]}
      VacuumAttack.killRadiusCurve = newRadiusCurve;
      //   VacuumAttack.killRadiusCurve = AnimationCurve.Linear(0, 0, 1, 150); // 50
      // VacuumAttack.pullMagnitudeCurve = AnimationCurve.Linear(0, 0, 1, 45);
      // TODO tweak pull magnitude if it's too much in testing
      orig(self);
    }
*/
    }
}