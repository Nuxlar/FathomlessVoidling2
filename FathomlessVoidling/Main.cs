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
using RoR2.CharacterAI;
using EntityStates.VoidRaidCrab;
using RoR2.Skills;
using EntityStates.VoidBarnacle.Weapon;
using UnityEngine.Networking;
using UnityEngine.Rendering.PostProcessing;

using FathomlessVoidling.Controllers;
using FathomlessVoidling.EntityStates;
using FathomlessVoidling.EntityStates.Haunt;
using FathomlessVoidling.EntityStates.Primary;
using FathomlessVoidling.EntityStates.Secondary;
using FathomlessVoidling.EntityStates.Utility;
using FathomlessVoidling.EntityStates.Special;
using FathomlessVoidling.EntityStates.Barnacle;
using FathomlessVoidling.Components;
using FathomlessVoidling.Hooks;
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
    public static SpawnCard barnacleSpawnCard;
    public static GameObject barnacleMaster;
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

    // Voidling Haunt Variables
    public static GameObject barnacleMuzzleFlash;
    public static GameObject gravityBulletChargeEffect;
    public static GameObject gravityBulletProjectile;
    public static GameObject gravityBombProjectile;
    public static SpawnCard voidlingHauntCard = ScriptableObject.CreateInstance<CharacterSpawnCard>();
    public static SpawnCard attachableBarnacleCard = ScriptableObject.CreateInstance<CharacterSpawnCard>();
    public static GameObject groundedGravityEffect;
    public static GameObject airborneGravityEffect;
    public static DirectorCardCategorySelection barnacleDccs = ScriptableObject.CreateInstance<DirectorCardCategorySelection>();

    // Maze Variables
    public static GameObject mazePortalEffect;
    public static GameObject mazeMuzzleEffect;
    public static GameObject mazeLaserPrefab;
    public static GameObject mazeChargeUpPrefab;
    public static GameObject mazeImpactEffect;

    // Wandering Singularity Variables
    public static GameObject wSingularityProjectile;

    private static Material voidCylinderMat;

    public void Awake()
    {
      Instance = this;

      Log.Init(Logger);

      AddContent();
      LoadAssets();
      CreateVoidlingHaunt();
      CreateNewEyeMissiles();
      CreateGravityProjectiles();
      CreateSingularityProjectile();
      TweakBigVoidling();
      TweakBigVoidlingMaster();
      //CreateAttachableBarnacle();
      // VoidRaidCrabAISkillDriverController

      new ConnectHooks();
    }

    private void AddContent()
    {
      ContentAddition.AddEntityState<BetterSpawnState>(out _);
      ContentAddition.AddEntityState<JointSpawnState>(out _);
      ContentAddition.AddEntityState<ChargeVoidRain>(out _);
      ContentAddition.AddEntityState<FireVoidRain>(out _);
      ContentAddition.AddEntityState<ChargeEyeBlast>(out _);
      ContentAddition.AddEntityState<FireEyeBlast>(out _);
      ContentAddition.AddEntityState<BaseMazeAttackState>(out _);
      ContentAddition.AddEntityState<EnterMaze>(out _);
      ContentAddition.AddEntityState<ExitMaze>(out _);
      ContentAddition.AddEntityState<MazeAttack>(out _);
      ContentAddition.AddEntityState<WanderingSingularity>(out _);

      ContentAddition.AddEntityState<VoidlingHauntManager>(out _);
      ContentAddition.AddEntityState<ChargeGravityBullet>(out _);
      ContentAddition.AddEntityState<FireGravityBullet>(out _);
      ContentAddition.AddEntityState<FindSurfaceAccurate>(out _);
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

    private static void CreateAttachableBarnacle()
    {
      AssetReferenceT<GameObject> barnacleMasterRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidBarnacle.VoidBarnacleMaster_prefab);
      GameObject barnacleMaster = AssetAsyncReferenceManager<GameObject>.LoadAsset(barnacleMasterRef).WaitForCompletion();

      AssetReferenceT<GameObject> barnacleBodyRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidBarnacle.VoidBarnacleBody_prefab);
      GameObject barnacleBody = AssetAsyncReferenceManager<GameObject>.LoadAsset(barnacleBodyRef).WaitForCompletion();

      GameObject newBarnacleMaster = PrefabAPI.InstantiateClone(barnacleMaster, "VoidBarnacleAttachableMasterNux");
      GameObject newBarnacleBody = PrefabAPI.InstantiateClone(barnacleBody, "VoidBarnacleAttachableBodyNux");

      CharacterBody body = newBarnacleBody.GetComponent<CharacterBody>();
      body.baseMaxHealth = 275f; // 225 vanilla
      body.levelMaxHealth = 85f; // 68 vanilla
      newBarnacleBody.transform.localScale *= 1.15f;
      newBarnacleBody.GetComponent<ModelLocator>().modelTransform.localScale *= 1.15f;
      newBarnacleBody.AddComponent<NetworkedBodyAttachment>().shouldParentToAttachedBody = true;
      EntityStateMachine esm = newBarnacleBody.GetComponent<EntityStateMachine>();
      esm.initialStateType = new SerializableEntityStateType(typeof(FindSurfaceAccurate));

      newBarnacleMaster.GetComponent<CharacterMaster>().bodyPrefab = newBarnacleBody;

      ContentAddition.AddBody(newBarnacleBody);
      ContentAddition.AddMaster(newBarnacleMaster);

      attachableBarnacleCard.prefab = newBarnacleMaster;
      attachableBarnacleCard.sendOverNetwork = true;
      attachableBarnacleCard.hullSize = HullClassification.Human;
      attachableBarnacleCard.nodeGraphType = RoR2.Navigation.MapNodeGroup.GraphType.Ground;
      attachableBarnacleCard.requiredFlags = RoR2.Navigation.NodeFlags.None;
      attachableBarnacleCard.forbiddenFlags = RoR2.Navigation.NodeFlags.NoCharacterSpawn | RoR2.Navigation.NodeFlags.NoChestSpawn | RoR2.Navigation.NodeFlags.NoShrineSpawn;
      attachableBarnacleCard.directorCreditCost = 50;
    }

    private static void CreateVoidlingHaunt()
    {
      AssetReferenceT<GameObject> barnacleMasterRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidBarnacle.VoidBarnacleMaster_prefab);
      barnacleMaster = AssetAsyncReferenceManager<GameObject>.LoadAsset(barnacleMasterRef).WaitForCompletion();

      AssetReferenceT<GameObject> barnacleBodyRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidBarnacle.VoidBarnacleBody_prefab);
      GameObject barnacleBody = AssetAsyncReferenceManager<GameObject>.LoadAsset(barnacleBodyRef).WaitForCompletion();

      AssetReferenceT<SpawnCard> barnacleCardRef = new AssetReferenceT<SpawnCard>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidBarnacle.cscVoidBarnacle_asset);
      barnacleSpawnCard = AssetAsyncReferenceManager<SpawnCard>.LoadAsset(barnacleCardRef).WaitForCompletion();
      DirectorCard barnacleDirectorCard = new DirectorCard
      {
        selectionWeight = 1,
        spawnCard = barnacleSpawnCard,
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
            body.GetComponent<SkillLocator>().secondary.skillFamily.variants[0].skillDef.activationState = new SerializableEntityStateType(typeof(WanderingSingularity));
            //   body.GetComponent<SkillLocator>().secondary.skillFamily.variants[0].skillDef.activationState = new SerializableEntityStateType(typeof(ChargeVoidRain));
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
      AssetReferenceT<Material> voidCylinderMatRef = new AssetReferenceT<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_GameModes_InfiniteTowerRun_ITAssets.matITSafeWardAreaIndicator1_mat);
      AssetAsyncReferenceManager<Material>.LoadAsset(voidCylinderMatRef).Completed += (x) => voidCylinderMat = x.Result;

      AssetReferenceT<Material> spinBeamMatRef = new AssetReferenceT<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.matVoidRaidCrabSpinBeamSphere1_mat);
      Material spinBeamSphereMat = AssetAsyncReferenceManager<Material>.LoadAsset(spinBeamMatRef).WaitForCompletion();

      AssetReferenceT<Material> voidRainPortalMatRef = new AssetReferenceT<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_PortalVoid.matPortalVoidCenter_mat);
      Material voidRainPortalMat = AssetAsyncReferenceManager<Material>.LoadAsset(voidRainPortalMatRef).WaitForCompletion();

      AssetReferenceT<GameObject> mazeChargeUpRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabSpinBeamChargeUp_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(mazeChargeUpRef).Completed += (x) =>
      {
        GameObject prefab = x.Result;
        mazeChargeUpPrefab = PrefabAPI.InstantiateClone(prefab, "MazeChargeUpEffectNux", false);
        foreach (ParticleSystem item in mazeChargeUpPrefab.transform.GetComponentsInChildren<ParticleSystem>())
        {
          ParticleSystem.MainModule main = item.main;
          main.startSizeMultiplier *= 1.5f;
        }
      };

      AssetReferenceT<GameObject> mazeImpactRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.LaserImpactEffect_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(mazeImpactRef).Completed += (x) =>
      {
        GameObject prefab = x.Result;
        mazeImpactEffect = PrefabAPI.InstantiateClone(prefab, "MazeImpactEffectNux", false);
        foreach (ParticleSystem item in mazeImpactEffect.transform.GetComponentsInChildren<ParticleSystem>())
        {
          ParticleSystem.MainModule main = item.main;
          main.startSizeMultiplier /= 2f;
        }
        ContentAddition.AddEffect(mazeImpactEffect);
      };

      AssetReferenceT<PostProcessProfile> ppRef = new AssetReferenceT<PostProcessProfile>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_title_PostProcessing.ppLocalNullifier_asset);
      PostProcessProfile ppProfile = AssetAsyncReferenceManager<PostProcessProfile>.LoadAsset(ppRef).WaitForCompletion();

      AssetReferenceT<GameObject> mazeLaserRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabSpinBeamVFX_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(mazeLaserRef).Completed += (x) =>
      {
        GameObject prefab = x.Result;
        mazeLaserPrefab = PrefabAPI.InstantiateClone(prefab, "MazeLaserVFXNux");
        mazeLaserPrefab.AddComponent<NetworkIdentity>();
        Transform meshTransform = mazeLaserPrefab.transform.Find("Mesh, Additive");
        meshTransform.localScale *= 3f;
        meshTransform.localPosition = new Vector3(0f, 0f, 50.16f); // 16.72 -> 33.44
        // 0 0 33.5
        Transform ppTransform = mazeLaserPrefab.transform.Find("Point Light, End").GetChild(0);
        ppTransform.gameObject.SetActive(true);
        ppTransform.GetComponent<PostProcessVolume>().sharedProfile = ppProfile; //ppLocalNullifier_asset or ppLocalDoppelganger_asset
        foreach (ParticleSystem item in mazeLaserPrefab.transform.GetComponentsInChildren<ParticleSystem>())
        {
          if (item.gameObject.name != "MuzzleRayParticles")
          {
            ParticleSystem.MainModule main = item.main;
            main.startSizeMultiplier *= 3f;
          }
        }
      };
      AssetReferenceT<GameObject> mazeMuzzleRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabSpinBeamChargeUp_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(mazeMuzzleRef).Completed += (x) =>
      {
        GameObject gravityPrefab = x.Result;
        GameObject newMuzzlePrefab = PrefabAPI.InstantiateClone(gravityPrefab, "MazeMuzzleEffectNux", false);
        foreach (ParticleSystem item in newMuzzlePrefab.transform.GetComponentsInChildren<ParticleSystem>())
        {
          ParticleSystem.MainModule main = item.main;
          main.duration *= 1.25f;
          main.startSizeMultiplier *= 1.25f;
        }
        mazeMuzzleEffect = newMuzzlePrefab;
      };
      AssetReferenceT<GameObject> mazePortalRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidMegaCrab.VoidMegaCrabSpawnEffect_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(mazePortalRef).Completed += (x) =>
      {
        GameObject portalPrefab = x.Result;
        GameObject newPortalPrefab = PrefabAPI.InstantiateClone(portalPrefab, "MazePortalEffectNux", false);
        newPortalPrefab.GetComponent<DestroyOnTimer>().duration = 5f;
        foreach (ParticleSystem ps in newPortalPrefab.GetComponentsInChildren<ParticleSystem>())
        {
          var mainModule = ps.main;
          mainModule.startSizeMultiplier *= 2f;
          mainModule.startLifetimeMultiplier *= 4f;
        }
        Transform ring = newPortalPrefab.transform.Find("Ring");
        ParticleSystemRenderer psr = ring.GetChild(0).GetComponent<ParticleSystemRenderer>();
        psr.sharedMaterial = voidRainPortalMat;

        mazePortalEffect = newPortalPrefab;
        ContentAddition.AddEffect(mazePortalEffect);
      };

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
          // main.startLifetimeMultiplier *= 1.75f;
        }
        ContentAddition.AddEffect(spawnEffect);
      };

      AssetReferenceT<CharacterSpawnCard> jointCardRef = new AssetReferenceT<CharacterSpawnCard>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.cscVoidRaidCrabJoint_asset);
      AssetAsyncReferenceManager<CharacterSpawnCard>.LoadAsset(jointCardRef).Completed += (x) => jointCard = x.Result;

      CreateAttachableBarnacle();

      AssetReferenceT<GameObject> jointBodyRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabJointBody_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(jointBodyRef).Completed += (x) =>
      {
        GameObject body = x.Result;
        JointThresholdController thresholdController = body.AddComponent<JointThresholdController>();
        /*
        Using the Xi Construct's implementation, there needs to be a NetworkedBodySpawnSlot for each spawn
Needs: spawncard, owner body, owner child locator, owner attach child name, spawn effect prefab (can be null), and kill effect prefab
BodyPrefab -> Model Base -> mockModel -> Toe -> ToeJoint
0 -> 0 -> 2 -> 0
z -0.44 0.44
x -0.44 0.44
        */
        body.AddComponent<MasterSpawnSlotController>();
        ChildLocator childLocator = body.GetComponent<ModelLocator>().modelChildLocator;

        Transform toeJoint = body.transform.GetChild(0).GetChild(0).GetChild(2).GetChild(0);
        for (int i = 0; i < 4; i++)
        {
          string attachName = "AttachPoint" + i;
          NetworkedBodySpawnSlot spawnSlot = body.AddComponent<NetworkedBodySpawnSlot>();
          GameObject newAttachment = new GameObject(attachName);
          newAttachment.transform.parent = toeJoint;
          if (i < 2)
          {
            float zVector = i == 0 ? 0.44f : -0.66f;
            newAttachment.transform.localPosition = new Vector3(0f, -0.1f, zVector);
          }
          else
          {
            float xVector = i == 2 ? 0.44f : -0.66f;
            newAttachment.transform.localPosition = new Vector3(xVector, -0.1f, 0f);
          }
          childLocator.AddChild(attachName, newAttachment.transform);
          spawnSlot.spawnCard = attachableBarnacleCard;
          spawnSlot.ownerBody = body.GetComponent<CharacterBody>();
          spawnSlot.ownerChildLocator = childLocator;
          spawnSlot.ownerAttachChildName = attachName;
        }

        body.GetComponent<EntityStateMachine>().initialStateType = new SerializableEntityStateType(typeof(JointSpawnState));
      };

      AssetReferenceT<CharacterSpawnCard> voidlingCardRef = new AssetReferenceT<CharacterSpawnCard>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.cscVoidRaidCrab_asset);
      AssetAsyncReferenceManager<CharacterSpawnCard>.LoadAsset(voidlingCardRef).Completed += (x) => bigVoidlingCard = x.Result;
    }

    private static void CreateSingularityProjectile()
    {
      AssetReferenceT<GameObject> suckSphereRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.KillSphereVfxPlaceholder_prefab);
      GameObject sphereEffect = AssetAsyncReferenceManager<GameObject>.LoadAsset(suckSphereRef).WaitForCompletion();
      sphereEffect = PrefabAPI.InstantiateClone(sphereEffect, "WSingularitySphere", false);
      AssetReferenceT<GameObject> suckCenterRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabSuckLoopFX_prefab);
      GameObject centerEffect = AssetAsyncReferenceManager<GameObject>.LoadAsset(suckCenterRef).WaitForCompletion();
      centerEffect = PrefabAPI.InstantiateClone(centerEffect, "WSingularityCenter", false);
      AssetReferenceT<GameObject> projectileRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabMissileProjectile_prefab);
      GameObject projectile = AssetAsyncReferenceManager<GameObject>.LoadAsset(projectileRef).WaitForCompletion();
      projectile = PrefabAPI.InstantiateClone(projectile, "WSingularityProjectile", true);
      AssetReferenceT<GameObject> ghostRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_LunarWisp.LunarWispTrackingBombGhost_prefab);
      GameObject ghost = AssetAsyncReferenceManager<GameObject>.LoadAsset(ghostRef).WaitForCompletion();
      ghost = PrefabAPI.InstantiateClone(ghost, "WSingularityGhost", false);
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
      projectileSimple.desiredForwardSpeed = 20f; // 10f orig
      projectileSimple.lifetime = 20f; // 15 orig

      projectileSimple.enableVelocityOverLifetime = true;
      projectileSimple.velocityOverLifetime = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f);

      projectile.transform.localScale = Vector3.one;

      ContentAddition.AddProjectile(projectile);
      wSingularityProjectile = projectile;
    }

    public static void CreateTube()
    {
      GameObject gameObject = new("WallHolder");
      gameObject.transform.position = new Vector3(-2.5f, 0.0f, 0.0f);
      GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
      primitive.GetComponent<MeshRenderer>().material = voidCylinderMat;
      UnityEngine.Object.Destroy(primitive.GetComponent<CapsuleCollider>());
      MeshCollider collider = primitive.AddComponent<MeshCollider>();
      // primitive.AddComponent<ReverseNormals>();
      primitive.transform.localScale = new Vector3(110f, 1250f, 110f);
      primitive.name = "Cheese Deterrent";
      primitive.transform.SetParent(gameObject.transform);
      primitive.transform.localPosition = Vector3.zero;
      primitive.layer = LayerIndex.world.intVal;

      GameObject disableCollisions = new("DisableCollisions");
      disableCollisions.transform.parent = primitive.transform;
      disableCollisions.transform.localScale = new Vector3(0.0091f, 0.0008f, 0.0091f);
      disableCollisions.layer = LayerIndex.entityPrecise.intVal;
      DisableCollisionsIfInTrigger disableTrigger = disableCollisions.AddComponent<DisableCollisionsIfInTrigger>();
      disableTrigger.colliderToIgnore = collider;
      SphereCollider sphereCollider = disableCollisions.GetComponent<SphereCollider>();
      sphereCollider.radius = 85f;
      sphereCollider.isTrigger = true;
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