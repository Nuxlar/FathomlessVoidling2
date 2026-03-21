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
using System.Linq;
using System.Collections.Generic;

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
            On.RoR2.VoidRaidGauntletController.TryOpenGauntlet += BlockGauntletInPhase3;

            On.EntityStates.VoidBarnacle.Weapon.ChargeFire.OnEnter += LazyMf;
            On.EntityStates.VoidRaidCrab.DeathState.OnEnter += FixDeathState;
            On.EntityStates.VoidRaidCrab.DeathState.OnExit += KillJointsOnDeath;
            /*
                PhaseControllerStateMachine (GameObject)
                has NetworkIdentity, EntityStateMachine, and NetworkStateMachine
            */
        }

        private void DeactivateDonutRoof(On.RoR2.VoidRaidGauntletController.orig_RpcActivateDonut orig, VoidRaidGauntletController self, int donutIndex)
        {
            orig(self, donutIndex);
            Transform root = self.currentDonut?.root?.transform;
            if (!root) return;

            if (root.name == "RaidDC")
            {
                Transform roof = root.Find("HOLDER: ROOF");
                if (roof && roof.gameObject.activeSelf)
                    roof.gameObject.SetActive(false);
            }

            Transform scripting = root.Find("HOLDER: Scripting");
            if (scripting)
            {
                Transform combatDirectorObj = scripting.Find("CombatDirector");
                if (combatDirectorObj)
                {
                    CombatDirector combatDirector = combatDirectorObj.GetComponent<CombatDirector>();
                    if (combatDirector)
                        combatDirector.enabled = false;
                }
            }
        }

        private bool BlockGauntletInPhase3(On.RoR2.VoidRaidGauntletController.orig_TryOpenGauntlet orig, VoidRaidGauntletController self, Vector3 entrancePosition, NetworkInstanceId bossMasterId)
        {
            if (FathomlessMissionController.instance?.GetCurrentPhase() == 2)
                return false;
            return orig(self, entrancePosition, bossMasterId);
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

        private void FixDeathState(On.EntityStates.VoidRaidCrab.DeathState.orig_OnEnter orig, global::EntityStates.VoidRaidCrab.DeathState self)
        {
            if (self.characterBody.name == "VoidRaidCrabBody(Clone)")
            {
                self.animationStateName = "ChargeWipe";
                self.animationPlaybackRateParam = "Wipe.playbackRate";
                self.addPrintController = false;
                orig(self);
                PrintController printController = self.modelTransform.gameObject.AddComponent<PrintController>();
                printController.printTime = self.printDuration;
                printController.enabled = true;
                printController.startingPrintHeight = 200f;
                printController.maxPrintHeight = 500f;
                printController.startingPrintBias = self.startingPrintBias;
                printController.maxPrintBias = self.maxPrintBias;
                printController.disableWhenFinished = false;
                printController.printCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1f, 1f);
            }
            else orig(self);
        }

        private void KillJointsOnDeath(On.EntityStates.VoidRaidCrab.DeathState.orig_OnExit orig, global::EntityStates.VoidRaidCrab.DeathState self)
        {
            orig(self);
            if (self.characterBody.name != "VoidRaidCrabBody(Clone)")
                return;
            if (VoidRaidGauntletController.instance)
            {
                VoidRaidGauntletController.instance.SpawnOutroPortal();
            }
            foreach (TeamComponent tc in TeamComponent.GetTeamMembers(TeamIndex.Void).ToList())
            {
                CharacterBody cb = tc.GetComponent<CharacterBody>();
                if (!cb || cb.name != "VoidRaidCrabJointBody(Clone)") continue;

                EntityStateMachine esm = cb.GetComponents<EntityStateMachine>().FirstOrDefault(e => e.customName == "Body");
                if (esm)
                    esm.SetState(new global::EntityStates.VoidRaidCrab.Joint.DeathState());
            }
        }

        private List<CharacterBody> GetOtherJoints(CharacterBody exclude)
        {
            List<CharacterBody> result = new List<CharacterBody>();
            foreach (TeamComponent tc in TeamComponent.GetTeamMembers(TeamIndex.Void).ToList())
            {
                CharacterBody cb = tc.GetComponent<CharacterBody>();
                if (cb && cb.name == "VoidRaidCrabJointBody(Clone)" && cb.netId != exclude.netId)
                    result.Add(cb);
            }
            return result;
        }

        private void OnFinalJointDeathBlow(DamageReport damageReport, HealthComponent hc, CharacterBody body)
        {
            damageReport.damageDealt = 0f;
            hc.health = 1f;
            body.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, 5f);

            List<CharacterBody> otherJoints = GetOtherJoints(body);
            foreach (CharacterBody joint in otherJoints)
                joint.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, 5f);

            FathomlessMissionController.instance?.voidlingBody?.healthComponent.Suicide();

            foreach (CharacterBody joint in otherJoints)
                joint.healthComponent.Suicide();
        }

        private void OnJointDeathBlow(DamageReport damageReport, HealthComponent hc, CharacterBody body)
        {
            foreach (CharacterBody joint in GetOtherJoints(body))
            {
                joint.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, 5f);
                joint.healthComponent.Heal(hc.fullHealth, new ProcChainMask());
            }

            FathomlessMissionController mc = FathomlessMissionController.instance;
            if (!mc || !mc.voidlingBody) return;

            CharacterBody bossBody = mc.voidlingBody;
            bossBody.RemoveBuff(RoR2Content.Buffs.HiddenInvincibility);
            bossBody.healthComponent.TakeDamage(new DamageInfo() { damage = 9999999f });
            bossBody.AddBuff(RoR2Content.Buffs.HiddenInvincibility);

            SkillLocator skillLocator = bossBody.GetComponent<SkillLocator>();
            if (skillLocator && Main.sdWardWipe)
                skillLocator.special.SetSkillOverride(bossBody.gameObject.GetComponent<EntityStateMachine>(), Main.sdWardWipe, GenericSkill.SkillOverridePriority.Contextual);

            if (mc.wardWipeDriver)
                mc.wardWipeDriver.enabled = true;
            if (mc.singularityDriver)
                mc.singularityDriver.enabled = false;
            if (mc.mazeDriver)
                mc.mazeDriver.enabled = false;
            if (mc.fireMissileDriver)
                mc.fireMissileDriver.enabled = false;
        }

        private void OnJointThreshold(DamageReport damageReport, HealthComponent hc, CharacterBody body, JointThresholdController jtc)
        {
            body.AddBuff(RoR2Content.Buffs.Immune);
            jtc.TriggerThresholdEvent();
            hc.health = hc.fullHealth * 0.8f;
            damageReport.damageDealt = 1f;

            FathomlessMissionController mc = FathomlessMissionController.instance;
            if (!mc)
                return;
            int phase = mc.GetCurrentPhase();
            if (phase == 0 && mc.singularityDriver)
                mc.singularityDriver.enabled = true;
            else if (phase == 1 && mc.mazeDriver)
                mc.mazeDriver.enabled = true;
        }

        private void ThresholdCheck(On.RoR2.HealthComponent.orig_SendDamageDealt orig, DamageReport damageReport)
        {
            HealthComponent hc = damageReport.victim.gameObject.GetComponent<HealthComponent>();
            CharacterBody body = hc.body;

            if (body && body.name == "VoidRaidCrabJointBody(Clone)")
            {
                JointThresholdController jtc = body.GetComponent<JointThresholdController>();
                bool isDeath = hc.health - damageReport.damageDealt <= 0f;

                if (isDeath && jtc && jtc.defeatedServer)
                {
                    int phase = FathomlessMissionController.instance?.GetCurrentPhase() ?? -1;
                    if (phase == 2)
                        OnFinalJointDeathBlow(damageReport, hc, body);
                    else
                        OnJointDeathBlow(damageReport, hc, body);
                }
                else if (jtc && !jtc.defeatedServer && hc.health - damageReport.damageDealt <= hc.fullHealth * 0.8f)
                    OnJointThreshold(damageReport, hc, body, jtc);
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
    }
}