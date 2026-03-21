using UnityEngine;
using UnityEngine.AddressableAssets;
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
using RoR2.EntityLogic;

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

            // Void Moon Stuff, thanks viliger 
            On.RoR2.VoidStageMissionController.RequestFog += PreventFog;
            RoR2.Stage.onServerStageBegin += SpawnCauldrons;
            On.RoR2.TeleporterInteraction.AttemptToSpawnAllEligiblePortals += SpawnVoidMoonPortal;
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
                Transform wallHolder = self.characterBody.transform.Find("WallHolder");
                if (wallHolder)
                    GameObject.Destroy(wallHolder.gameObject);
                CharacterBody hauntBody = FathomlessMissionController.instance?.hauntBody;
                if (hauntBody)
                    hauntBody.healthComponent.Suicide();
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
            {
                skillLocator.special.SetSkillOverride(bossBody.gameObject.GetComponent<EntityStateMachine>(), Main.sdWardWipe, GenericSkill.SkillOverridePriority.Contextual);
                skillLocator.special.AddOneStock();
            }

            if (mc.wardWipeDriver)
                mc.wardWipeDriver.enabled = true;
            if (mc.singularityDriver)
                mc.singularityDriver.enabled = false;
            if (mc.mazeDriver)
                mc.mazeDriver.enabled = false;
            if (mc.fireMissileDriver)
                mc.fireMissileDriver.enabled = false;
            if (mc.multibeamDriver)
                mc.multibeamDriver.enabled = false;
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
                    DelayedEvent delayedEvent = phase1Obj.GetComponent<DelayedEvent>();
                    if (delayedEvent)
                        GameObject.Destroy(delayedEvent);

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

        private static VoidStageMissionController.FogRequest PreventFog(On.RoR2.VoidStageMissionController.orig_RequestFog orig, VoidStageMissionController self, IZone zone)
        {
            return null;
        }

        private static void SpawnVoidMoonPortal(On.RoR2.TeleporterInteraction.orig_AttemptToSpawnAllEligiblePortals orig, TeleporterInteraction self)
        {
            if (self.beginContextString.Contains("LUNAR"))
            {
                List<PortalSpawner> list = self.portalSpawners.ToList<PortalSpawner>();
                PortalSpawner portalSpawner = list.Find((PortalSpawner x) => x.portalSpawnCard == Main.locusPortalCard);
                if (portalSpawner != null)
                {
                    list.Remove(portalSpawner);
                    self.portalSpawners = list.ToArray();
                }
                if (!NetworkServer.active)
                {
                    return;
                }

                DirectorCore instance = DirectorCore.instance;
                DirectorPlacementRule directorPlacementRule = new DirectorPlacementRule
                {
                    minDistance = 10f,
                    maxDistance = 40f,
                    placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                    position = self.transform.position,
                    spawnOnTarget = self.transform
                };
                DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(Main.locusPortalCard, directorPlacementRule, self.rng);
                GameObject gameObject = instance.TrySpawnObject(directorSpawnRequest);
                if (gameObject)
                {
                    NetworkServer.Spawn(gameObject);
                    Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                    {
                        baseToken = "PORTAL_VOID_OPEN"
                    });
                }
            }
            orig.Invoke(self);
        }

        private static void SpawnCauldrons(Stage stage)
        {
            if (stage.sceneDef.cachedName == "voidstage")
            {
                var handle1 = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_LunarCauldrons.LunarCauldron__RedToWhite_Variant_prefab);
                if (handle1.IsValid())
                {
                    handle1.Completed += (result) =>
                    {
                        if (result.IsDone && result.Result)
                        {
                            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(result.Result, new Vector3(-142.67f, 29.94f, 242.74f), Quaternion.identity);
                            gameObject.transform.eulerAngles = new Vector3(0f, 66f, 0f);
                            NetworkServer.Spawn(gameObject);
                        }
                        Addressables.Release(handle1);
                    };
                }

                var handle2 = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_LunarCauldrons.LunarCauldron__GreenToRed_Variant_prefab);
                if (handle2.IsValid())
                {
                    handle2.Completed += (result) =>
                    {
                        if (result.IsDone && result.Result)
                        {
                            GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(result.Result, new Vector3(-136.76f, 29.94f, 246.51f), Quaternion.identity);
                            gameObject2.transform.eulerAngles = new Vector3(0f, 66f, 0f);
                            NetworkServer.Spawn(gameObject2);
                            GameObject gameObject3 = UnityEngine.Object.Instantiate<GameObject>(result.Result, new Vector3(-149.74f, 29.93f, 239.7f), Quaternion.identity);
                            gameObject3.transform.eulerAngles = new Vector3(0f, 66f, 0f);
                            NetworkServer.Spawn(gameObject3);
                        }
                        Addressables.Release(handle2);
                    };
                }

                var handle3 = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_LunarCauldrons.LunarCauldron__WhiteToGreen_prefab);
                if (handle3.IsValid())
                {
                    handle3.Completed += (result) =>
                    {
                        if (result.IsDone && result.Result)
                        {
                            GameObject gameObject4 = UnityEngine.Object.Instantiate<GameObject>(result.Result, new Vector3(-157.41f, 29.97f, 237.12f), Quaternion.identity);
                            gameObject4.transform.eulerAngles = new Vector3(0f, 66f, 0f);
                            NetworkServer.Spawn(gameObject4);
                            GameObject gameObject5 = UnityEngine.Object.Instantiate<GameObject>(result.Result, new Vector3(-126.63f, 29.93f, 249.1f), Quaternion.identity);
                            gameObject5.transform.eulerAngles = new Vector3(0f, 66f, 0f);
                            NetworkServer.Spawn(gameObject5);
                        }
                        Addressables.Release(handle2);
                    };
                }
            }
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