using BepInEx;
using HG;
using R2API;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Mecanim;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using System.Reflection;
using RoR2.Audio;
using RoR2.Projectile;

namespace FathomlessVoidling
{
  [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
  public class Main : BaseUnityPlugin
  {
    public const string PluginGUID = PluginAuthor + "." + PluginName;
    public const string PluginAuthor = "Nuxlar";
    public const string PluginName = "FathomlessVoidling";
    public const string PluginVersion = "1.0.0";

    internal static Main Instance { get; private set; }
    public static string PluginDirectory { get; private set; }

    public static GameObject spawnEffect;
    public static CharacterSpawnCard jointCard;
    public static SpawnCard bigVoidlingCard;
    public static TimelineAsset introTimeline;
    public static GameObject chargeVoidRain;
    public static GameObject voidRainTracer;
    public static GameObject voidRainExplosion;
    public static GameObject voidRainWarning;
    public static GameObject voidRainPortalEffect;
    public static GameObject wSingularityProjectile;
    /*
    public static AnimationClip newClip;
    public static AssetBundle assetBundle;
    public const string bundleName = "fathomlessvoidling.bundle";
    */

    public void Awake()
    {
      Instance = this;

      Log.Init(Logger);
      /*
Main.assetBundle = AssetBundle.LoadFromFile(Assembly.GetExecutingAssembly().Location.Replace("FathomlessVoidling.dll", "fathomlessvoidling.bundle"));
newClip = Main.assetBundle.LoadAsset<AnimationClip>("Assets/Recorded.anim");
*/
      AddContent();
      LoadAssets();
      CreateSingularityProjectile();
      TweakBigVoidling();

      On.RoR2.SceneDirector.Start += TweakBossDirector;
    }

    private void AddContent()
    {
      ContentAddition.AddEntityState<BetterSpawnState>(out _);
      ContentAddition.AddEntityState<JointSpawnState>(out _);
      ContentAddition.AddEntityState<ChargeVoidRain>(out _);
      ContentAddition.AddEntityState<FireVoidRain>(out _);
      ContentAddition.AddEntityState<WanderingSingularity>(out _);
    }

    private void TweakBossDirector(On.RoR2.SceneDirector.orig_Start orig, RoR2.SceneDirector self)
    {
      if (SceneManager.GetActiveScene().name == "voidraid")
      {
        GameObject missionObj = GameObject.Find("EncounterPhases");
        GameObject phase1Obj = missionObj.transform.GetChild(0).gameObject;
        if (missionObj && phase1Obj)
        {
          missionObj.transform.GetChild(1).gameObject.SetActive(false);
          missionObj.transform.GetChild(2).gameObject.SetActive(false);

          Transform transform = new GameObject().transform;
          transform.position = new Vector3(0, -10, 0);

          ScriptedCombatEncounter.SpawnInfo spawnInfo = new ScriptedCombatEncounter.SpawnInfo();
          spawnInfo.explicitSpawnPosition = transform;
          spawnInfo.spawnCard = Main.bigVoidlingCard;

          phase1Obj.GetComponent<ScriptedCombatEncounter>().spawns = [spawnInfo];

          Transform cam = GameObject.Find("RaidVoid").transform.GetChild(5);
          Transform forcedCam = cam.GetChild(1);
          forcedCam.GetComponent<PlayableDirector>().playableAsset = introTimeline;
          Transform curve = cam.GetChild(2);
          // y -6.812038f
          curve.position = new Vector3(-110.27766f, 15f, -300f);
          curve.GetChild(0).position = new Vector3(-50f, 28.9719f, -396.993f); // orig -6.215 28.9719 -396.993
        }
      }
      orig(self);
    }

    private static void CreateSingularityProjectile()
    {
      AssetReferenceT<GameObject> suckSphereRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.KillSphereVfxPlaceholder_prefab);
      GameObject sphereEffect = AssetAsyncReferenceManager<GameObject>.LoadAsset(suckSphereRef).WaitForCompletion();
      sphereEffect = PrefabAPI.InstantiateClone(sphereEffect, "WSingularitySphere", true);
      AssetReferenceT<GameObject> suckCenterRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabSuckLoopFX_prefab);
      GameObject centerEffect = AssetAsyncReferenceManager<GameObject>.LoadAsset(suckCenterRef).WaitForCompletion();
      centerEffect = PrefabAPI.InstantiateClone(centerEffect, "WSingularityCenter", true);
      AssetReferenceT<GameObject> projectileRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabMissileProjectile_prefab);
      GameObject projectile = AssetAsyncReferenceManager<GameObject>.LoadAsset(projectileRef).WaitForCompletion();
      projectile = PrefabAPI.InstantiateClone(projectile, "WSingularityProjectile", true);
      AssetReferenceT<GameObject> ghostRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_LunarWisp.LunarWispTrackingBombGhost_prefab);
      GameObject ghost = AssetAsyncReferenceManager<GameObject>.LoadAsset(ghostRef).WaitForCompletion();
      ghost = PrefabAPI.InstantiateClone(ghost, "WSingularityGhost", true);
      AssetReferenceT<LoopSoundDef> lsdRef = new AssetReferenceT<LoopSoundDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.lsdVoidRaidCrabVacuumAttack_asset);
      LoopSoundDef singularityLSD = AssetAsyncReferenceManager<LoopSoundDef>.LoadAsset(lsdRef).WaitForCompletion();

      Destroy(sphereEffect.GetComponent<VFXHelper.VFXTransformController>());
      Destroy(centerEffect.GetComponent<VFXHelper.VFXTransformController>());
      sphereEffect.transform.localScale = new Vector3(20f, 20f, 20f);

      foreach (Transform child in ghost.transform)
      {
        Destroy(child.gameObject);
      }

      centerEffect.transform.parent = ghost.transform;
      sphereEffect.transform.parent = ghost.transform;
      sphereEffect.transform.GetChild(1).GetChild(1).gameObject.SetActive(false);
      centerEffect.GetComponent<VFXAttributes>().vfxIntensity = VFXAttributes.VFXIntensity.Low;
      sphereEffect.GetComponent<VFXAttributes>().vfxIntensity = VFXAttributes.VFXIntensity.Low;

      foreach (Transform child in projectile.transform)
      {
        Destroy(child.gameObject);
      }
      Destroy(projectile.GetComponent<BoxCollider>());
      Destroy(projectile.GetComponent<ProjectileSingleTargetImpact>());
      projectile.AddComponent<SingularityComponent>();
      SphereCollider sphereCollider = projectile.AddComponent<SphereCollider>();
      sphereCollider.radius = 19f;
      sphereCollider.isTrigger = true;
      projectile.GetComponent<Rigidbody>().useGravity = false;
      // projectile.GetComponent<ProjectileSingleTargetImpact>().destroyOnWorld = false;
      ProjectileDirectionalTargetFinder targetFinder = projectile.GetComponent<ProjectileDirectionalTargetFinder>();
      targetFinder.allowTargetLoss = false;
      targetFinder.lookCone = 360f;
      targetFinder.lookRange = 1000f;
      ProjectileController projectileController = projectile.GetComponent<ProjectileController>();
      projectileController.ghostPrefab = ghost;
      projectileController.cannotBeDeleted = true;
      projectileController.flightSoundLoop = singularityLSD;
      projectileController.myColliders = new Collider[1] { sphereCollider };
      ProjectileSimple projectileSimple = projectile.GetComponent<ProjectileSimple>();
      projectileSimple.desiredForwardSpeed = 10f;
      projectileSimple.lifetime = 15f;
      projectile.transform.localScale = Vector3.one;

      ContentAddition.AddProjectile(projectile);
      wSingularityProjectile = projectile;
    }

    private static void TweakBigVoidling()
    {
      AssetReferenceT<GameObject> voidlingRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabBody_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(voidlingRef).Completed += (x) =>
          {
            GameObject body = x.Result;
            ModelLocator modelLocator = body.GetComponent<ModelLocator>();

            body.GetComponent<SkillLocator>().secondary.skillFamily.variants[0].skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(WanderingSingularity));
            body.GetComponent<SkillLocator>().secondary.skillFamily.variants[0].skillDef.baseMaxStock = 1;

            // Add new spawn state
            List<EntityStateMachine> list = body.GetComponents<EntityStateMachine>().ToList();
            for (int i = 0; i < list.Count; i++)
            {
              EntityStateMachine esm = list[i];
              if (esm.customName == "Body")
              {
                esm.initialStateType = new EntityStates.SerializableEntityStateType(typeof(BetterSpawnState));
              }
            }

            // Fix leg rotation animation
            Animator animator = modelLocator.modelTransform.gameObject.GetComponent<Animator>();
            animator.applyRootMotion = true;
            animator.avatar = AvatarBuilder.BuildGenericAvatar(animator.gameObject, "ROOT");

            // Fix invisible model
            GameObject model = modelLocator.modelTransform.gameObject;
            PrintController printController = model.AddComponent<PrintController>();
            printController.printTime = 8.5f; // 6f orig
            printController.disableWhenFinished = true;
            printController.startingPrintHeight = -20f;
            printController.maxPrintHeight = 200f; //500f
            printController.startingPrintBias = -10f;
            printController.maxPrintBias = 0f;
            printController.animateFlowmapPower = false;
            printController.startingFlowmapPower = 0f;
            printController.maxFlowmapPower = 0f;
            printController.printCurve = AnimationCurve.Linear(0.0f, 0.0f, 1f, 1f);
            printController.gameObject.SetActive(true);

            // Add eye blinking
            model.AddComponent<RandomBlinkController>();
          };
    }

    private static void LoadAssets()
    {
      AssetReferenceT<GameObject> portalRainRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidMegaCrab.VoidMegaCrabSpawnEffect_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(portalRainRef).Completed += (x) =>
      {
        voidRainPortalEffect = PrefabAPI.InstantiateClone(x.Result, "VoidRainPortalEffect", true);
        ContentAddition.AddEffect(voidRainPortalEffect);
      };

      AssetReferenceT<GameObject> warningRainRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.MultiBeamRayIndicator_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(warningRainRef).Completed += (x) => voidRainWarning = x.Result;

      AssetReferenceT<GameObject> chargeRainRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabTripleBeamChargeUp_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(chargeRainRef).Completed += (x) => chargeVoidRain = x.Result;

      AssetReferenceT<GameObject> tracerRainRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.TracerVoidRaidCrabTripleBeamSmall_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(tracerRainRef).Completed += (x) => voidRainTracer = x.Result;

      AssetReferenceT<GameObject> explosionRainRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabTripleBeamExplosion_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(explosionRainRef).Completed += (x) => voidRainExplosion = x.Result;

      AssetReferenceT<TimelineAsset> timelineRef = new AssetReferenceT<TimelineAsset>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabIntroTimeiline_playable);
      AssetAsyncReferenceManager<TimelineAsset>.LoadAsset(timelineRef).Completed += (x) =>
      {
        introTimeline = x.Result;
      };
      AssetReferenceT<GameObject> spawnRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabSpawnEffect_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(spawnRef).Completed += (x) =>
      {
        GameObject result = x.Result;
        spawnEffect = PrefabAPI.InstantiateClone(result, "FathomlessVoidlingSpawnEffect", true);
        spawnEffect.transform.localScale = new Vector3(15, 15, 15);
        spawnEffect.GetComponent<DestroyOnTimer>().duration = 12f; // 6f orig
        foreach (ParticleSystem item in spawnEffect.transform.GetChild(0).GetChild(1).GetComponentsInChildren<ParticleSystem>())
        {
          ParticleSystem.MainModule main = item.main;
          main.duration *= 2f;
          main.startLifetimeMultiplier *= 2f;
        }
        ContentAddition.AddEffect(spawnEffect);
      };

      AssetReferenceT<CharacterSpawnCard> jointCardRef = new AssetReferenceT<CharacterSpawnCard>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.cscVoidRaidCrabJoint_asset);
      AssetAsyncReferenceManager<CharacterSpawnCard>.LoadAsset(jointCardRef).Completed += (x) => jointCard = x.Result;

      AssetReferenceT<GameObject> jointBodyRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabJointBody_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(jointBodyRef).Completed += (x) =>
      {
        GameObject body = x.Result;
        body.GetComponent<EntityStateMachine>().initialStateType = new EntityStates.SerializableEntityStateType(typeof(JointSpawnState));

        ModelLocator modelLocator = body.GetComponent<ModelLocator>();
        GameObject model = modelLocator.modelTransform.gameObject;
      };


      AssetReferenceT<CharacterSpawnCard> voidlingCardRef = new AssetReferenceT<CharacterSpawnCard>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.cscVoidRaidCrab_asset);
      AssetAsyncReferenceManager<CharacterSpawnCard>.LoadAsset(voidlingCardRef).Completed += (x) => bigVoidlingCard = x.Result;
    }

    private void TweakEntityState(string path, string fieldName, string value)
    {
      AssetReferenceT<EntityStateConfiguration> escRef = new AssetReferenceT<EntityStateConfiguration>(path);
      AssetAsyncReferenceManager<EntityStateConfiguration>.LoadAsset(escRef).Completed += (x) =>
      {
        EntityStateConfiguration esc = x.Result;
        for (int i = 0; i < esc.serializedFieldsCollection.serializedFields.Length; i++)
        {
          if (esc.serializedFieldsCollection.serializedFields[i].fieldName == fieldName)
          {
            esc.serializedFieldsCollection.serializedFields[i].fieldValue.stringValue = value;
          }
        }
      };
    }
  }
}