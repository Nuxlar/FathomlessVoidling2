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

            On.EntityStates.VoidBarnacle.Weapon.ChargeFire.OnEnter += LazyMf;
            /*
                PhaseControllerStateMachine (GameObject)
                has NetworkIdentity, EntityStateMachine, and NetworkStateMachine
            */

            // On.EntityStates.VoidRaidCrab.VacuumAttack.OnEnter += IncreaseSingularitySize;
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

                if (hc.health - damageReport.damageDealt <= hc.fullHealth * 0.75f)
                {
                    JointThresholdController jointThresholdController = body.GetComponent<JointThresholdController>();
                    if (!jointThresholdController.defeatedServer)
                    {
                        body.AddBuff(RoR2Content.Buffs.Immune);
                        body.GetComponent<JointThresholdController>().TriggerThresholdEvent();
                        hc.health = hc.fullHealth * 0.75f;
                        damageReport.damageDealt = 1f;
                    }
                }
            }
            orig(damageReport);
        }

        private void TweakBossDirector(On.RoR2.SceneDirector.orig_Start orig, RoR2.SceneDirector self)
        {
            if (SceneManager.GetActiveScene().name == "voidraid")
            {
                GameObject missionObj = GameObject.Find("EncounterPhases");
                GameObject phase1Obj = missionObj.transform.GetChild(0).gameObject;
                if (missionObj && phase1Obj)
                {
                    missionObj.AddComponent<FathomlessMissionController>();
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

                    if (NetworkServer.active)
                    {
                        GameObject mazeController = new GameObject("MazeSpawnPointController");
                        mazeController.transform.localPosition = Vector3.zero;
                        mazeController.AddComponent<MazeSpawnPointController>();
                    }
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