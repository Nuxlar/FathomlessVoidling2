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
using EntityStates;
using FathomlessVoidling.EntityStates;
using FathomlessVoidling.EntityStates.Primary;
using FathomlessVoidling.EntityStates.Secondary;
//using FathomlessVoidling.EntityStates.Special;
using FathomlessVoidling.EntityStates.Barnacle;
using FathomlessVoidling.Components;
using RoR2.CharacterAI;
using EntityStates.VoidRaidCrab;
using RoR2.Skills;
using FathomlessVoidling.EntityStates.Haunt;
using EntityStates.VoidBarnacle.Weapon;

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

    public static DamageAPI.ModdedDamageType gravityDamageType = DamageAPI.ReserveDamageType();
    public static GameObject spawnEffect;
    public static CharacterSpawnCard jointCard;
    public static SpawnCard bigVoidlingCard;
    public static TimelineAsset introTimeline;
    public static GameObject chargeVoidRain;
    public static GameObject voidRainTracer;
    public static GameObject voidRainExplosion;
    public static GameObject voidRainWarning;
    public static GameObject voidRainPortalEffect;
    public static GameObject eyeMissileProjectile;
    public static GameObject eyeBlastChargeEffect;
    public static GameObject eyeBlastMuzzleFlash;

    public static GameObject barnacleMuzzleFlash;
    public static GameObject gravityBulletChargeEffect;
    public static GameObject gravityBulletProjectile;
    public static GameObject gravityBombProjectile;
    public static SpawnCard voidlingHauntCard = ScriptableObject.CreateInstance<CharacterSpawnCard>();

    private static GameObject groundedGravityEffect;
    private static GameObject airborneGravityEffect;
    private static DirectorCardCategorySelection barnacleDccs = ScriptableObject.CreateInstance<DirectorCardCategorySelection>();
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
      CreateVoidlingHaunt();
      CreateGravityProjectiles();
      CreateNewEyeMissiles();
      TweakBigVoidling();
      TweakBigVoidlingMaster();

      On.RoR2.SceneDirector.Start += TweakBossDirector;
      GlobalEventManager.onServerDamageDealt += ApplyGravityDamageType;
      On.EntityStates.VoidBarnacle.Weapon.ChargeFire.OnEnter += LazyMf;
      On.EntityStates.VoidRaidCrab.VacuumAttack.OnEnter += IncreaseSingularitySize;

    }

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

    private void LazyMf(On.EntityStates.VoidBarnacle.Weapon.ChargeFire.orig_OnEnter orig, ChargeFire self)
    {
      string sceneName = SceneManager.GetActiveScene().name;
      if (sceneName == "voidraid")
        self.outer.SetState(new ChargeGravityBullet());
      else
        orig(self);
    }

    private void AddContent()
    {
      ContentAddition.AddEntityState<BetterSpawnState>(out _);
      ContentAddition.AddEntityState<JointSpawnState>(out _);
      ContentAddition.AddEntityState<ChargeVoidRain>(out _);
      ContentAddition.AddEntityState<FireVoidRain>(out _);
      ContentAddition.AddEntityState<ChargeEyeBlast>(out _);
      ContentAddition.AddEntityState<FireEyeBlast>(out _);

      ContentAddition.AddEntityState<VoidlingHauntManager>(out _);
      ContentAddition.AddEntityState<ChargeGravityBullet>(out _);
      ContentAddition.AddEntityState<FireGravityBullet>(out _);
    }

    private void ApplyGravityDamageType(DamageReport obj)
    {
      if (obj.damageInfo.HasModdedDamageType(gravityDamageType) && obj.victimBody)
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
          bool isLeft = (double)Random.value > 0.5;
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
            effectPrefab = groundedGravityEffect;
            effectData.rotation = Util.QuaternionSafeLookRotation(groundedForceVector);
          }
          else
          {
            motor.ApplyForce(airborneForceVector, true, preventAirControl);
            effectPrefab = airborneGravityEffect;
            effectData.rotation = Util.QuaternionSafeLookRotation(airborneForceVector);
          }
          EffectManager.SpawnEffect(effectPrefab, effectData, true);
        }
      }
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
          transform.position = new Vector3(0, -20, 0);

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

    private static void CreateNewEyeMissiles()
    {
      AssetReferenceT<GameObject> impactRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabImpact1_prefab);
      GameObject missileImpact = AssetAsyncReferenceManager<GameObject>.LoadAsset(impactRef).WaitForCompletion();
      AssetReferenceT<GameObject> ghostRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabMissileGhost_prefab);
      GameObject missileProjectileGhost = AssetAsyncReferenceManager<GameObject>.LoadAsset(ghostRef).WaitForCompletion();
      AssetReferenceT<GameObject> projectileRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabMissileProjectile_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(projectileRef).Completed += (x) =>
      {
        GameObject missileProjectile = PrefabAPI.InstantiateClone(x.Result, "FathomlessEyeProjectile", false);
        missileProjectileGhost = PrefabAPI.InstantiateClone(missileProjectileGhost, "FathomlessEyeProjectileGhost", false);
        missileImpact = PrefabAPI.InstantiateClone(missileImpact, "FathomlessEyeProjectileImpact", false);

        missileProjectile.GetComponent<ProjectileController>().ghostPrefab = missileProjectileGhost;

        missileProjectile.AddComponent<StasisMissileComponent>();
        ProjectileSimple ps = missileProjectile.GetComponent<ProjectileSimple>();
        /*
         ps.oscillate = true;
         ps.oscillateSpeed = 5f;
         ps.oscillateMagnitude = 30f; // 20f orig
         */
        //ps.lifetime = 
        ps.desiredForwardSpeed = 125f; // 70f orig

        ProjectileImpactExplosion pie = missileProjectile.AddComponent<ProjectileImpactExplosion>();
        pie.blastRadius = 6f;
        pie.impactEffect = missileImpact;
        pie.destroyOnWorld = true;
        pie.lifetime = 5f;
        pie.blastDamageCoefficient = 1f;
        pie.falloffModel = BlastAttack.FalloffModel.SweetSpot;

        GameObject.Destroy(missileProjectile.GetComponent<ProjectileSingleTargetImpact>());

        ProjectileSteerTowardTarget steer = missileProjectile.GetComponent<ProjectileSteerTowardTarget>();
        steer.rotationSpeed = 20f;
        steer.enabled = false;
        ProjectileDirectionalTargetFinder targetFinder = missileProjectile.GetComponent<ProjectileDirectionalTargetFinder>();
        targetFinder.lookCone = 360f;
        targetFinder.lookRange = 100f;
        // allowTargetLoss is true
        // sortMode is distance and angle
        missileProjectile.transform.localScale *= 4;

        Transform ring = missileProjectileGhost.transform.Find("FlashRing");
        if (ring)
        {
          ParticleSystem ghostPs = ring.GetComponent<ParticleSystem>();
          ParticleSystem.MainModule main = ghostPs.main;
          main.duration *= 3f;
          main.startLifetimeMultiplier *= 3f;
        }

        foreach (Transform child in missileProjectileGhost.transform)
        {
          child.localScale *= 4;
        }

        foreach (Transform child in missileImpact.transform)
        {
          child.localScale *= 4;
        }

        ContentAddition.AddEffect(missileImpact);
        ContentAddition.AddProjectile(missileProjectile);

        eyeMissileProjectile = missileProjectile;
      };

    }

    private static void TweakBigVoidlingMaster()
    {
      AssetReferenceT<GameObject> voidlingMasterRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabMaster_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(voidlingMasterRef).Completed += (x) =>
      {
        GameObject masterPrefab = x.Result;
        Debug.LogWarning("AISKILLDRIVERNAMES");
        List<AISkillDriver> driverList = masterPrefab.GetComponents<AISkillDriver>().ToList();
        foreach (AISkillDriver driver in driverList)
        {
          Debug.LogWarning(driver.customName);
          switch (driver.customName)
          {
            case "Channel Gauntlet 1":
              //driver.maxUserHealthFraction = 0.66f;
              driver.requiredSkill = null;
              break;
            case "Channel Gauntlet 2":
              // driver.maxUserHealthFraction = 0.33f;
              driver.requiredSkill = null;
              break;
            case "FireMissiles":
              driver.movementType = AISkillDriver.MovementType.ChaseMoveTarget;
              // driver.activationRequiresAimConfirmation = true;
              break;
            case "FireMultiBeam":
              driver.movementType = AISkillDriver.MovementType.ChaseMoveTarget;
              driver.aimType = AISkillDriver.AimType.AtMoveTarget;
              break;
            case "SpinBeam":
              driver.maxUserHealthFraction = 1f;
              driver.requiredSkill = null;
              break;
            case "GravityBump":
              driver.maxUserHealthFraction = 1f;
              driver.requiredSkill = null;
              driver.skillSlot = SkillSlot.Utility;
              break;
            case "Vacuum Attack":
              driver.maxUserHealthFraction = 0.9f;
              driver.requiredSkill = null;
              break;
            case "LookAtTarget":
              driver.movementType = AISkillDriver.MovementType.ChaseMoveTarget;
              break;
          }
        }
      };
    }


    private static void CreateGravityProjectiles()
    {
      AssetReferenceT<Material> matGravitySphereRef = new AssetReferenceT<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.matVoidRaidCrabGravityBumpSphere_mat);
      Material matGravitySphere = AssetAsyncReferenceManager<Material>.LoadAsset(matGravitySphereRef).WaitForCompletion();
      AssetReferenceT<Material> matGravitySphere2Ref = new AssetReferenceT<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.matVoidRaidCrabGravityBumpGem_mat);
      Material matGravitySphere2 = AssetAsyncReferenceManager<Material>.LoadAsset(matGravitySphere2Ref).WaitForCompletion();

      AssetReferenceT<Material> matGravityStarRef = new AssetReferenceT<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.matVoidRaidCrabParticleBlue_mat);
      Material matGravityStar = AssetAsyncReferenceManager<Material>.LoadAsset(matGravityStarRef).WaitForCompletion();
      AssetReferenceT<Material> matGravityIndicatorRef = new AssetReferenceT<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.matVoidRaidCrabGravityBumpWarning_mat);
      Material matGravityIndicator = AssetAsyncReferenceManager<Material>.LoadAsset(matGravityIndicatorRef).WaitForCompletion();

      AssetReferenceT<GameObject> preBombGhostRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Nullifier.NullifierPreBombGhost_prefab);
      GameObject preBombGhost = AssetAsyncReferenceManager<GameObject>.LoadAsset(preBombGhostRef).WaitForCompletion();

      AssetReferenceT<GameObject> explosionRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Nullifier.NullifierExplosion_prefab);
      GameObject explosionEffect = AssetAsyncReferenceManager<GameObject>.LoadAsset(explosionRef).WaitForCompletion();

      AssetReferenceT<GameObject> bombRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Nullifier.NullifierBombProjectile_prefab);
      GameObject bombProjectile = AssetAsyncReferenceManager<GameObject>.LoadAsset(bombRef).WaitForCompletion();

      Material[] gravSphereMatArray = new Material[] { matGravitySphere, matGravitySphere2 };
      // Gravity Bullets  // Ghost Charge Explosion
      AssetReferenceT<GameObject> explosion2Ref = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidBarnacle.VoidBarnacleImpactExplosion_prefab);
      GameObject explosionEffect2 = AssetAsyncReferenceManager<GameObject>.LoadAsset(explosion2Ref).WaitForCompletion();
      AssetReferenceT<GameObject> charge2Ref = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidBarnacle.VoidBarnacleCharge_prefab);
      GameObject chargeEffect2 = AssetAsyncReferenceManager<GameObject>.LoadAsset(charge2Ref).WaitForCompletion();

      AssetReferenceT<GameObject> bulletRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidBarnacle.VoidBarnacleBullet_prefab);
      GameObject bulletPrefab = AssetAsyncReferenceManager<GameObject>.LoadAsset(bulletRef).WaitForCompletion();

      AssetReferenceT<GameObject> ghostRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidBarnacle.VoidBarnacleBulletGhost_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(ghostRef).Completed += (x) =>
      {
        GameObject prefab = x.Result;
        GameObject newGhost = PrefabAPI.InstantiateClone(prefab, "GravityBulletGhostNux", false);
        newGhost.transform.GetChild(3).GetComponent<MeshRenderer>().sharedMaterials = gravSphereMatArray;
        newGhost.transform.Find("Point Light").GetComponent<Light>().color = new Color(18f / 255f, 18f / 255f, 1f, 1f); // 255 18 18 255
        newGhost.transform.Find("Flames").GetComponent<ParticleSystemRenderer>().sharedMaterial = matGravityStar;

        gravityBulletChargeEffect = PrefabAPI.InstantiateClone(chargeEffect2, "GravityBulletChargeEffectNux", false);
        Transform chargeBase = gravityBulletChargeEffect.transform.GetChild(0);
        chargeBase.Find("Fire").GetComponent<ParticleSystemRenderer>().sharedMaterial = matGravityStar;
        chargeBase.Find("Point Light").GetComponent<Light>().color = new Color(169f / 255f, 235f / 255f, 250f / 255f, 1f); // 250 169 235 255
        chargeBase.Find("Sphere").GetComponent<MeshRenderer>().sharedMaterials = gravSphereMatArray;

        GameObject newExplosion = PrefabAPI.InstantiateClone(explosionEffect2, "GravityBulletExplosionEffectNux", false);
        newExplosion.transform.Find("Point Light").GetComponent<Light>().color = new Color(27f / 255f, 123f / 255f, 248f / 255f, 1f); // 123 27 248 255
        newExplosion.transform.Find("ExplosionSphere, Stars (1)").GetComponent<ParticleSystemRenderer>().sharedMaterial = matGravitySphere;

        gravityBulletProjectile = PrefabAPI.InstantiateClone(bulletPrefab, "GravityBulletNux", true);
        gravityBulletProjectile.GetComponent<ProjectileController>().ghostPrefab = newGhost;

        gravityBulletProjectile.GetComponent<ProjectileImpactExplosion>().impactEffect = newExplosion;

        ContentAddition.AddEffect(gravityBulletChargeEffect);
        ContentAddition.AddEffect(newExplosion);
        ContentAddition.AddProjectile(gravityBulletProjectile);
      };

      // Gravity Bombs
      AssetReferenceT<GameObject> preBombRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Nullifier.NullifierPreBombProjectile_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(preBombRef).Completed += (x) =>
      {
        GameObject prefab = x.Result;
        GameObject newPrebombProjectile = PrefabAPI.InstantiateClone(prefab, "GravityPreBombProjectileNux", true);
        GameObject newPrebombGhost = PrefabAPI.InstantiateClone(preBombGhost, "GravityPreBombGhostNux", false);
        GameObject newExplosionEffect = PrefabAPI.InstantiateClone(explosionEffect, "GravityBombExplosionEffectNux", false);

        ProjectileController controller = newPrebombProjectile.GetComponent<ProjectileController>();
        controller.ghostPrefab = newPrebombGhost;
        controller.cannotBeDeleted = true;

        ProjectileImpactExplosion pie = newPrebombProjectile.GetComponent<ProjectileImpactExplosion>();
        pie.blastRadius = 10f;
        pie.impactEffect = newExplosionEffect;
        pie.lifetime = 2f;
        pie.childrenProjectilePrefab = null;

        gravityBombProjectile = newPrebombProjectile;
        gravityBombProjectile.transform.localScale *= 2f;

        newPrebombGhost.transform.localScale *= 2f;
        newPrebombGhost.transform.Find("Vacuum Radial").GetComponent<ParticleSystemRenderer>().sharedMaterial = matGravityIndicator;
        Transform sphere = newPrebombGhost.transform.Find("Sphere");
        sphere.localScale *= 2f;
        ObjectScaleCurve curve = sphere.GetComponent<ObjectScaleCurve>();
        curve.timeMax = 2f;
        curve.baseScale = new Vector3(4f, 4f, 4f);
        sphere.GetComponent<MeshRenderer>().sharedMaterials = gravSphereMatArray;
        newPrebombGhost.transform.Find("Vacuum Stars").GetComponent<ParticleSystemRenderer>().sharedMaterial = matGravityStar;
        foreach (ParticleSystem item in newPrebombGhost.transform.GetComponentsInChildren<ParticleSystem>())
        {
          ParticleSystem.MainModule main = item.main;
          main.startSizeMultiplier *= 1.5f;
          main.duration *= 2f;
          main.startLifetimeMultiplier *= 2f;
        }

        newExplosionEffect.transform.Find("Vacuum Stars").GetComponent<ParticleSystemRenderer>().sharedMaterial = matGravityStar;
        newExplosionEffect.transform.Find("Vacuum Radial").GetComponent<ParticleSystemRenderer>().sharedMaterial = matGravityIndicator;
        Transform sphere2 = newExplosionEffect.transform.Find("Sphere");
        sphere2.GetComponent<MeshRenderer>().sharedMaterial = matGravitySphere;
        sphere2.transform.localScale *= 2f;
        foreach (ParticleSystem item in newExplosionEffect.transform.GetComponentsInChildren<ParticleSystem>())
        {
          ParticleSystem.MainModule main = item.main;
          main.startSizeMultiplier *= 1.2f;
        }

        ContentAddition.AddEffect(newExplosionEffect);
        ContentAddition.AddProjectile(gravityBombProjectile);
      };
    }
    private static void CreateVoidlingHaunt()
    {
      AssetReferenceT<SpawnCard> barnacleCardRef = new AssetReferenceT<SpawnCard>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidBarnacle.cscVoidBarnacle_asset);
      SpawnCard barnacleCard = AssetAsyncReferenceManager<SpawnCard>.LoadAsset(barnacleCardRef).WaitForCompletion();
      DirectorCard barnacleDirectorCard = new DirectorCard
      {
        selectionWeight = 1,
        spawnCard = barnacleCard,
      };

      AssetReferenceT<GameObject> hauntMasterRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_BrotherHaunt.BrotherHauntMaster_prefab);
      GameObject hauntMaster = AssetAsyncReferenceManager<GameObject>.LoadAsset(hauntMasterRef).WaitForCompletion();

      AssetReferenceT<GameObject> hauntRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_BrotherHaunt.BrotherHauntBody_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(hauntRef).Completed += (x) =>
      {
        GameObject prefab = x.Result;
        GameObject voidlingHaunt = PrefabAPI.InstantiateClone(prefab, "VoidlingHauntNux", true);
        GameObject voidlingHauntMaster = PrefabAPI.InstantiateClone(hauntMaster, "VoidlingHauntNuxMaster", true);

        GameObject director = new GameObject("Barnacle Director");
        director.SetActive(false);
        director.transform.parent = voidlingHaunt.transform;
        CombatDirector combatDirector = director.AddComponent<CombatDirector>();
        barnacleDccs.Clear();
        barnacleDccs.AddCategory("BarnacleMania", 4f);
        barnacleDccs.AddCard(0, barnacleDirectorCard);
        combatDirector.customName = "Barnacle Director";
        combatDirector.expRewardCoefficient = 0.2f;
        combatDirector.minSeriesSpawnInterval = 0.1f;
        combatDirector.maxSeriesSpawnInterval = 1f;
        combatDirector.minRerollSpawnInterval = 2.333333f;
        combatDirector.maxRerollSpawnInterval = 4.333333f;
        combatDirector.creditMultiplier = 0.3f;
        combatDirector.targetPlayers = true;
        combatDirector.monsterCards = barnacleDccs;
        combatDirector.teamIndex = TeamIndex.Void;
        combatDirector.monsterCredit = 150f;
        combatDirector.onSpawnedServer = new();
        combatDirector.moneyWaveIntervals = new RangeFloat[1]
        {
              new RangeFloat() { min = 1f, max = 1f }
        };

        CharacterBody body = voidlingHaunt.GetComponent<CharacterBody>();
        body.baseNameToken = "Voidling Haunt";

        voidlingHauntMaster.GetComponent<CharacterMaster>().bodyPrefab = voidlingHaunt;

        SkillDef skillDef = ScriptableObject.CreateInstance<SkillDef>();

        skillDef.skillName = "Gravity Well Nux";
        (skillDef as ScriptableObject).name = "Gravity Well Nux";
        skillDef.skillNameToken = "Gravity Well Nux";

        skillDef.activationState = new SerializableEntityStateType(typeof(VoidlingHauntManager));
        skillDef.activationStateMachineName = "Weapon";
        skillDef.interruptPriority = InterruptPriority.Death;

        skillDef.baseMaxStock = 1;
        skillDef.baseRechargeInterval = 60f;

        skillDef.rechargeStock = 1;
        skillDef.requiredStock = 1;
        skillDef.stockToConsume = 1;

        skillDef.dontAllowPastMaxStocks = true;
        skillDef.beginSkillCooldownOnSkillEnd = true;
        skillDef.canceledFromSprinting = false;
        skillDef.forceSprintDuringState = false;
        skillDef.fullRestockOnAssign = false;
        skillDef.resetCooldownTimerOnUse = false;
        skillDef.isCombatSkill = true;
        skillDef.mustKeyPress = false;
        skillDef.cancelSprintingOnActivation = false;

        ContentAddition.AddSkillDef(skillDef);

        GameObject.Destroy(voidlingHaunt.GetComponent<GenericSkill>());
        EntityStateMachine esm = voidlingHaunt.GetComponent<EntityStateMachine>();
        esm.initialStateType = new SerializableEntityStateType(typeof(VoidlingHauntManager));
        esm.mainStateType = new SerializableEntityStateType(typeof(VoidlingHauntManager));

        SkillLocator skillLocator = voidlingHaunt.GetComponent<SkillLocator>();

        GenericSkill primarySkill = voidlingHaunt.AddComponent<GenericSkill>();
        primarySkill.skillName = "VHauntNuxPrimary";

        SkillFamily newFamily = ScriptableObject.CreateInstance<SkillFamily>();
        (newFamily as ScriptableObject).name = "VHauntNuxPrimaryFamily";
        newFamily.variants = new SkillFamily.Variant[] { new SkillFamily.Variant() { skillDef = skillDef } };

        primarySkill._skillFamily = newFamily;
        ContentAddition.AddSkillFamily(newFamily);
        skillLocator.primary = primarySkill;

        ContentAddition.AddBody(voidlingHaunt);
        ContentAddition.AddMaster(voidlingHauntMaster);

        voidlingHauntCard.prefab = voidlingHauntMaster;
        voidlingHauntCard.sendOverNetwork = true;
        voidlingHauntCard.nodeGraphType = RoR2.Navigation.MapNodeGroup.GraphType.Air;
      };
    }

    private static void TweakBigVoidling()
    {
      AssetReferenceT<GameObject> voidlingRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabBody_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(voidlingRef).Completed += (x) =>
          {
            GameObject body = x.Result;
            ModelLocator modelLocator = body.GetComponent<ModelLocator>();

            body.GetComponent<SkillLocator>().primary.skillFamily.variants[0].skillDef.activationState = new SerializableEntityStateType(typeof(ChargeEyeBlast));
            body.GetComponent<SkillLocator>().secondary.skillFamily.variants[0].skillDef.activationState = new SerializableEntityStateType(typeof(ChargeVoidRain));
            body.GetComponent<SkillLocator>().secondary.skillFamily.variants[0].skillDef.baseMaxStock = 1;

            // Add new spawn state
            List<EntityStateMachine> list = body.GetComponents<EntityStateMachine>().ToList();
            for (int i = 0; i < list.Count; i++)
            {
              EntityStateMachine esm = list[i];
              if (esm.customName == "Body")
              {
                esm.initialStateType = new SerializableEntityStateType(typeof(BetterSpawnState));
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
      AssetReferenceT<Material> voidRainPortalMatRef = new AssetReferenceT<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_PortalVoid.matPortalVoidCenter_mat);
      Material voidRainPortalMat = AssetAsyncReferenceManager<Material>.LoadAsset(voidRainPortalMatRef).WaitForCompletion();

      AssetReferenceT<GameObject> barnacleFlashRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidBarnacle.VoidBarnacleMuzzleflash_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(barnacleFlashRef).Completed += (x) => barnacleMuzzleFlash = x.Result;

      AssetReferenceT<GameObject> gravityEffectARef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabGravityBumpExplosionAir_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(gravityEffectARef).Completed += (x) => airborneGravityEffect = x.Result;
      AssetReferenceT<GameObject> gravityEffectGRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabGravityBumpExplosionGround_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(gravityEffectGRef).Completed += (x) => groundedGravityEffect = x.Result;

      AssetReferenceT<GameObject> muzzleFlashRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabMuzzleflashEyeMissiles_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(muzzleFlashRef).Completed += (x) =>
      {
        GameObject prefab = x.Result;
        eyeBlastMuzzleFlash = PrefabAPI.InstantiateClone(prefab, "EyeBlastMuzzleFlashNux", false);
        ContentAddition.AddEffect(eyeBlastMuzzleFlash);
      };

      AssetReferenceT<GameObject> eyeBlastChargeRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabChargeEyeMissiles_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(eyeBlastChargeRef).Completed += (x) =>
      {
        GameObject prefab = x.Result;
        eyeBlastChargeEffect = PrefabAPI.InstantiateClone(prefab, "EyeBlastChargeEffectNux", false);
        foreach (ParticleSystem item in eyeBlastChargeEffect.transform.GetComponentsInChildren<ParticleSystem>())
        {
          ParticleSystem.MainModule main = item.main;
          main.startSizeMultiplier *= 2f;
        }
        // ContentAddition.AddEffect(eyeBlastChargeEffect);
      };

      AssetReferenceT<GameObject> portalRainRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Nullifier.NullifierSpawnEffect_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(portalRainRef).Completed += (x) =>
      {
        voidRainPortalEffect = PrefabAPI.InstantiateClone(x.Result, "VoidRainPortalEffect", false);
        Transform ring = voidRainPortalEffect.transform.Find("Ring");
        ParticleSystemRenderer psr = ring.GetChild(0).GetComponent<ParticleSystemRenderer>();
        psr.sharedMaterial = voidRainPortalMat;
        ContentAddition.AddEffect(voidRainPortalEffect);
      };

      AssetReferenceT<GameObject> warningRainRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.MultiBeamRayIndicator_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(warningRainRef).Completed += (x) => voidRainWarning = x.Result;

      AssetReferenceT<GameObject> chargeRainRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabTripleBeamChargeUp_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(chargeRainRef).Completed += (x) =>
      {
        GameObject prefab = x.Result;
        chargeVoidRain = PrefabAPI.InstantiateClone(prefab, "ChargeVoidRainNuxEffect", false);
        foreach (ParticleSystem item in chargeVoidRain.transform.GetComponentsInChildren<ParticleSystem>())
        {
          ParticleSystem.MainModule main = item.main;
          main.startSizeMultiplier *= 1.5f;
          main.duration *= 7f;
          main.startLifetimeMultiplier *= 7f;
        }
        // ContentAddition.AddEffect(chargeVoidRain);
      };

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
        spawnEffect = PrefabAPI.InstantiateClone(result, "FathomlessVoidlingSpawnEffect", false);
        spawnEffect.transform.localScale = new Vector3(15, 15, 15);
        spawnEffect.GetComponent<DestroyOnTimer>().duration = 12f; // 6f orig
        foreach (ParticleSystem item in spawnEffect.transform.GetChild(0).GetChild(1).GetComponentsInChildren<ParticleSystem>())
        {
          ParticleSystem.MainModule main = item.main;
          main.duration *= 1.75f;
          main.startLifetimeMultiplier *= 1.75f;
        }
        ContentAddition.AddEffect(spawnEffect);
      };

      AssetReferenceT<CharacterSpawnCard> jointCardRef = new AssetReferenceT<CharacterSpawnCard>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.cscVoidRaidCrabJoint_asset);
      AssetAsyncReferenceManager<CharacterSpawnCard>.LoadAsset(jointCardRef).Completed += (x) => jointCard = x.Result;

      AssetReferenceT<GameObject> jointBodyRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabJointBody_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(jointBodyRef).Completed += (x) =>
      {
        GameObject body = x.Result;
        body.GetComponent<EntityStateMachine>().initialStateType = new SerializableEntityStateType(typeof(JointSpawnState));

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