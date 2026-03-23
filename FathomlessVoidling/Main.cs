using BepInEx;
using R2API;
using RoR2;
using RoR2.Mecanim;
using RoR2.VoidRaidCrab;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Timeline;
using RoR2.Audio;
using RoR2.Projectile;
using EntityStates;
using RoR2.CharacterAI;
using EntityStates.VoidRaidCrab;
using RoR2.Skills;
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
/*
  STUFF TO REFERENCE
    SFX
    // Play_voidRaid_fog_explode ominous, subtle explosion
    // Play_voidDevastator_spawn_loop ominous portal sounds
    // Play_voidDevastator_death quick "explosion"
    // Play_voidDevastator_death_vortex_explode crunchier nullifier explosion
    // Play_voidJailer_death_vortex_explode quick distored explosion
    // Play_voidRaid_fog_chargeUp good starter for wardwipe
    // Play_voidRaid_fog_affectPlayer whispering loop
    // Play_item_void_slowOnHit weird sorta tp?

*/
namespace FathomlessVoidling
{
  [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
  public class Main : BaseUnityPlugin
  {
    public const string PluginGUID = PluginAuthor + "." + PluginName;
    public const string PluginAuthor = "Nuxlar";
    public const string PluginName = "FathomlessVoidling";
    public const string PluginVersion = "0.9.13";

    internal static Main Instance { get; private set; }
    public static string PluginDirectory { get; private set; }

    public static DamageAPI.ModdedDamageType gravityDamageType = DamageAPI.ReserveDamageType();
    public static GameObject barnacleMaster;
    public static SpawnCard barnacleSpawnCard;
    public static GameObject spawnEffect;
    public static CharacterSpawnCard jointCard = Addressables.LoadAssetAsync<CharacterSpawnCard>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.cscVoidRaidCrabJoint_asset).WaitForCompletion();
    public static SpawnCard bigVoidlingCard = Addressables.LoadAssetAsync<CharacterSpawnCard>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.cscVoidRaidCrab_asset).WaitForCompletion();
    public static TimelineAsset introTimeline = Addressables.LoadAssetAsync<TimelineAsset>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabIntroTimeiline_playable).WaitForCompletion();
    public static GameObject chargeVoidRain;
    public static GameObject voidRainTracer = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.TracerVoidRaidCrabTripleBeamSmall_prefab).WaitForCompletion();
    public static GameObject voidRainExplosion = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabTripleBeamExplosion_prefab).WaitForCompletion();
    public static GameObject voidRainWarning = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.MultiBeamRayIndicator_prefab).WaitForCompletion();
    public static GameObject voidRainPortalEffect;
    public static GameObject eyeMissileProjectile;
    public static GameObject eyeBlastChargeEffect;
    public static GameObject eyeBlastMuzzleFlash;
    public static GameObject raidTeleportEffect;
    public static LoopSoundDef lsdVoidMegaCrabDeathBomb = Addressables.LoadAssetAsync<LoopSoundDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidMegaCrab.lsdVoidMegaCrabDeathBomb_asset).WaitForCompletion();
    public static GameObject chargeWardWipeChargeEffect = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabWardWipeChargeup_prefab).WaitForCompletion();
    public static GameObject fireWardWipeMuzzleFlash = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabWardWipeMuzzleflash_prefab).WaitForCompletion();
    public static BuffDef bdWardWipeFog = Addressables.LoadAssetAsync<BuffDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.bdVoidRaidCrabWardWipeFog_asset).WaitForCompletion();
    public static InteractableSpawnCard iscSafeWard = Addressables.LoadAssetAsync<InteractableSpawnCard>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.iscVoidRaidSafeWard_asset).WaitForCompletion();
    public static SkillDef sdWardWipe;
    public static SkillDef sdSingularity;
    public static SkillDef sdMaze;
    public static SkillDef sdMultiBeam;

    // Voidling Haunt Variables
    public static GameObject barnacleMuzzleFlash = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidBarnacle.VoidBarnacleMuzzleflash_prefab).WaitForCompletion();
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
    public static GameObject mazeWarningPrefab;
    public static GameObject mazeChargeUpPrefab;
    public static GameObject mazeImpactEffect;

    // Wandering Singularity Variables
    public static GameObject wSingularityProjectile;

    public static SpawnCard locusPortalCard = Addressables.LoadAssetAsync<SpawnCard>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_PortalVoid.iscVoidPortal_asset).WaitForCompletion();

    private static Material voidCylinderMat = Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_GameModes_InfiniteTowerRun_ITAssets.matITSafeWardAreaIndicator1_mat).WaitForCompletion();

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
      ContentAddition.AddEntityState<ChargeWardWipeNux>(out _);
      ContentAddition.AddEntityState<FireWardWipeNux>(out _);
    }

    public static List<CharacterBody> GetPlayerBodies()
    {
      List<CharacterBody> playerBodies = new List<CharacterBody>();
      foreach (PlayerCharacterMasterController playerMasterController in PlayerCharacterMasterController.instances)
      {
        if (playerMasterController && playerMasterController.master)
        {
          CharacterBody playerBody = playerMasterController.master.GetBody();
          if (playerBody)
            playerBodies.Add(playerBody);
        }
      }
      return playerBodies;
    }

    private static void CreateNewEyeMissiles()
    {
      GameObject missileImpact = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabImpact1_prefab).WaitForCompletion();
      GameObject missileProjectileGhost = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabMissileGhost_prefab).WaitForCompletion();
      GameObject missileProjectile = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabMissileProjectile_prefab).WaitForCompletion();
      missileProjectile = PrefabAPI.InstantiateClone(missileProjectile, "FathomlessEyeProjectile", true);
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
    }

    private static void TweakBigVoidlingMaster()
    {
      GameObject masterPrefab = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabMaster_prefab).WaitForCompletion();

      GameObject.Destroy(masterPrefab.GetComponent<VoidRaidCrabAISkillDriverController>());

      List<AISkillDriver> driverList = masterPrefab.GetComponents<AISkillDriver>().ToList();
      foreach (AISkillDriver driver in driverList)
      {
        switch (driver.customName)
        {
          case "Channel Gauntlet 1":
          case "Channel Gauntlet 2":
          case "GravityBump":
            GameObject.Destroy(driver);
            break;
          case "FireMissiles":
            driver.movementType = AISkillDriver.MovementType.ChaseMoveTarget;
            driver.enabled = true;
            driver.noRepeat = true;
            break;
          case "FireMultiBeam":
            driver.movementType = AISkillDriver.MovementType.ChaseMoveTarget;
            driver.aimType = AISkillDriver.AimType.AtMoveTarget;
            driver.skillSlot = SkillSlot.Secondary;
            driver.enabled = true;
            driver.maxUserHealthFraction = float.PositiveInfinity;
            driver.activationRequiresAimConfirmation = false;
            driver.noRepeat = true;
            break;
          case "SpinBeam":
            driver.noRepeat = true;
            driver.skillSlot = SkillSlot.Utility;
            driver.maxUserHealthFraction = float.PositiveInfinity;
            break;
          case "Vacuum Attack":
            driver.noRepeat = true;
            driver.maxUserHealthFraction = float.PositiveInfinity;
            break;
          case "WardWipe":
            driver.noRepeat = true;
            driver.maxUserHealthFraction = 0.66f; // 0.67 orig
            break;
          case "LookAtTarget":
            driver.movementType = AISkillDriver.MovementType.ChaseMoveTarget;
            break;
        }
      }
    }


    private static void CreateGravityProjectiles()
    {
      groundedGravityEffect = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabGravityBumpExplosionGround_prefab).WaitForCompletion();
      groundedGravityEffect.GetComponent<EffectComponent>().soundName = "Play_voidRaid_fog_explode";
      airborneGravityEffect = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabGravityBumpExplosionAir_prefab).WaitForCompletion();
      airborneGravityEffect.GetComponent<EffectComponent>().soundName = "Play_voidRaid_fog_explode";

      Material matGravitySphere = Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.matVoidRaidCrabGravityBumpSphere_mat).WaitForCompletion();
      Material matGravitySphere2 = Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.matVoidRaidCrabGravityBumpGem_mat).WaitForCompletion();
      Material matGravityStar = Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.matVoidRaidCrabParticleBlue_mat).WaitForCompletion();
      Material matGravityIndicator = Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.matVoidRaidCrabGravityBumpWarning_mat).WaitForCompletion();

      GameObject preBombGhost = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Nullifier.NullifierPreBombGhost_prefab).WaitForCompletion();
      GameObject explosionEffect = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Nullifier.NullifierExplosion_prefab).WaitForCompletion();
      GameObject bombProjectile = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Nullifier.NullifierBombProjectile_prefab).WaitForCompletion();

      Material[] gravSphereMatArray = new Material[] { matGravitySphere, matGravitySphere2 };
      // Gravity Bullets  // Ghost Charge Explosion
      GameObject explosionEffect2 = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidBarnacle.VoidBarnacleImpactExplosion_prefab).WaitForCompletion();
      GameObject chargeEffect2 = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidBarnacle.VoidBarnacleCharge_prefab).WaitForCompletion();
      GameObject bulletPrefab = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidBarnacle.VoidBarnacleBullet_prefab).WaitForCompletion();

      GameObject ghostPrefab = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidBarnacle.VoidBarnacleBulletGhost_prefab).WaitForCompletion();
      GameObject newGhost = PrefabAPI.InstantiateClone(ghostPrefab, "GravityBulletGhostNux", false);
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

      // Gravity Bombs
      GameObject preBombPrefab = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Nullifier.NullifierPreBombProjectile_prefab).WaitForCompletion();
      GameObject newPrebombProjectile = PrefabAPI.InstantiateClone(preBombPrefab, "GravityPreBombProjectileNux", true);
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
    }

    private static void CreateAttachableBarnacle()
    {
      GameObject barnacleMasterLocal = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidBarnacle.VoidBarnacleMaster_prefab).WaitForCompletion();
      GameObject barnacleBody = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidBarnacle.VoidBarnacleBody_prefab).WaitForCompletion();

      GameObject newBarnacleMaster = PrefabAPI.InstantiateClone(barnacleMasterLocal, "VoidBarnacleAttachableMasterNux");
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
      barnacleMaster = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidBarnacle.VoidBarnacleMaster_prefab).WaitForCompletion();
      GameObject barnacleBody = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidBarnacle.VoidBarnacleBody_prefab).WaitForCompletion();
      barnacleSpawnCard = Addressables.LoadAssetAsync<SpawnCard>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidBarnacle.cscVoidBarnacle_asset).WaitForCompletion();
      DirectorCard barnacleDirectorCard = new DirectorCard
      {
        selectionWeight = 1,
        spawnCard = barnacleSpawnCard,
      };

      GameObject hauntMaster = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_BrotherHaunt.BrotherHauntMaster_prefab).WaitForCompletion();
      GameObject hauntBodyPrefab = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_BrotherHaunt.BrotherHauntBody_prefab).WaitForCompletion();
      GameObject voidlingHaunt = PrefabAPI.InstantiateClone(hauntBodyPrefab, "VoidlingHauntNux", true);
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
    }

    private static void TweakBigVoidling()
    {
      GameObject body = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabBody_prefab).WaitForCompletion();
      ModelLocator modelLocator = body.GetComponent<ModelLocator>();

      body.GetComponent<CharacterBody>().baseMaxHealth = 2000f;
      FogDamageController fogDamageController = body.GetComponent<FogDamageController>();
      if (fogDamageController)
      {
        fogDamageController.healthFractionPerSecond = 0.01f;
        fogDamageController.healthFractionRampCoefficientPerSecond = 0.1f;
      }
      SkillLocator skillLocator = body.GetComponent<SkillLocator>();
      skillLocator.primary.skillFamily.variants[0].skillDef.activationState = new SerializableEntityStateType(typeof(ChargeEyeBlast));
      skillLocator.secondary.skillFamily.variants = new SkillFamily.Variant[] { new SkillFamily.Variant() { skillDef = sdMultiBeam } };
      skillLocator.utility.skillFamily.variants = new SkillFamily.Variant[] { new SkillFamily.Variant() { skillDef = sdMaze } };

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

      // Change legs to be on a normal entity layer instead of World
      Transform legBase = model.transform.Find("VoidRaidCrabArmature").Find("ROOT").Find("LegBase");
      List<SurfaceDefProvider> providers = legBase.GetComponentsInChildren<SurfaceDefProvider>().ToList();
      foreach (SurfaceDefProvider provider in providers)
      {
        provider.gameObject.layer = (int)LayerIndex.defaultLayer;
      }
    }

    private static void LoadAssets()
    {
      SceneDef voidRaid = Addressables.LoadAssetAsync<SceneDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_voidraid.voidraid_asset).WaitForCompletion();
      voidRaid.blockOrbitalSkills = false;

      raidTeleportEffect = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_gauntlets.VoidRaidCrabGauntletTeleportEffect_prefab).WaitForCompletion();
      raidTeleportEffect.GetComponent<EffectComponent>().soundName = "Play_voidRaid_fog_explode";
      ParticleSystem.MinMaxGradient omniSparksColor = raidTeleportEffect.transform.Find("OmniSparks").GetComponent<ParticleSystem>().main.startColor;
      ParticleSystem.MainModule sphereBrief = raidTeleportEffect.transform.Find("Sphere, Brief").GetComponent<ParticleSystem>().main;
      ParticleSystem.MainModule sphereLong = raidTeleportEffect.transform.Find("Sphere, Long").GetComponent<ParticleSystem>().main;
      sphereBrief.startColor = omniSparksColor;
      sphereLong.startColor = omniSparksColor;

      GameObject safeWard = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidSafeWard_prefab).WaitForCompletion();
      //  Transform indicatorTransform = safeWard.transform.Find("Indicator").GetChild(0);
      // indicatorTransform.localScale = new Vector3(8f, 120f, 8f); // 2 120 2
      safeWard.GetComponent<VerticalTubeZone>().radius = 16f; // 4

      Material spinBeamSphereMat = Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.matVoidRaidCrabSpinBeamSphere1_mat).WaitForCompletion();
      Material voidRainPortalMat = Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_PortalVoid.matPortalVoidCenter_mat).WaitForCompletion();

      mazeChargeUpPrefab = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabSpinBeamChargeUp_prefab).WaitForCompletion(), "MazeChargeUpEffectNux", false);
      foreach (ParticleSystem item in mazeChargeUpPrefab.transform.GetComponentsInChildren<ParticleSystem>())
      {
        ParticleSystem.MainModule main = item.main;
        main.startSizeMultiplier *= 1.5f;
      }

      mazeImpactEffect = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.LaserImpactEffect_prefab).WaitForCompletion(), "MazeImpactEffectNux", false);
      foreach (ParticleSystem item in mazeImpactEffect.transform.GetComponentsInChildren<ParticleSystem>())
      {
        ParticleSystem.MainModule main = item.main;
        main.startSizeMultiplier /= 2f;
      }
      ContentAddition.AddEffect(mazeImpactEffect);

      PostProcessProfile ppProfile = Addressables.LoadAssetAsync<PostProcessProfile>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_title_PostProcessing.ppLocalNullifier_asset).WaitForCompletion();

      mazeLaserPrefab = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabSpinBeamVFX_prefab).WaitForCompletion(), "MazeLaserVFXNux");
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

      mazeWarningPrefab = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabSpinBeamVFX_prefab).WaitForCompletion(), "MazeWarningVFXNux");
      for (int i = mazeWarningPrefab.transform.childCount - 1; i >= 0; i--)
      {
        Transform child = mazeWarningPrefab.transform.GetChild(i);
        if (child.name != "Mesh, Additive" && child.name != "Mesh, Transparent")
          GameObject.Destroy(child.gameObject);
      }
      VfxKillBehavior warningVfxKill = mazeWarningPrefab.GetComponent<VfxKillBehavior>();
      if (warningVfxKill)
        GameObject.Destroy(warningVfxKill);
      DestroyOnTimer warningTimer = mazeWarningPrefab.GetComponent<DestroyOnTimer>();
      if (warningTimer)
        warningTimer.duration = 2f;
      Material matInnerWarning = Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.matWarningBeamOuterCylinder_mat).WaitForCompletion();
      Material matWarning = Object.Instantiate(Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.matWarningBeamOuterCylinder_mat).WaitForCompletion());
      Color warningColor = matWarning.color;
      matWarning.color = new Color(warningColor.r, warningColor.g, warningColor.b, 0.3f);
      Transform warningMeshTransform = mazeWarningPrefab.transform.Find("Mesh, Additive");
      if (warningMeshTransform)
      {
        warningMeshTransform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = matInnerWarning;
        warningMeshTransform.localScale *= 3f;
        warningMeshTransform.localPosition = new Vector3(0f, 0f, 50.16f);
        MeshRenderer warningMeshRenderer = warningMeshTransform.GetComponent<MeshRenderer>();
        if (warningMeshRenderer)
          warningMeshRenderer.sharedMaterial = matWarning;
      }

      GameObject newMuzzlePrefab = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabSpinBeamChargeUp_prefab).WaitForCompletion(), "MazeMuzzleEffectNux", false);
      newMuzzlePrefab.transform.eulerAngles = new Vector3(90f, 0f, 0f);
      foreach (ParticleSystem item in newMuzzlePrefab.transform.GetComponentsInChildren<ParticleSystem>())
      {
        ParticleSystem.MainModule main = item.main;
        main.duration *= 1.25f;
        //   main.startSizeMultiplier *= 1.25f;
      }
      mazeMuzzleEffect = newMuzzlePrefab;

      GameObject newPortalPrefab = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidMegaCrab.VoidMegaCrabSpawnEffect_prefab).WaitForCompletion(), "MazePortalEffectNux", false);
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

      eyeBlastMuzzleFlash = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabMuzzleflashEyeMissiles_prefab).WaitForCompletion(), "EyeBlastMuzzleFlashNux", false);
      ContentAddition.AddEffect(eyeBlastMuzzleFlash);

      eyeBlastChargeEffect = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabChargeEyeMissiles_prefab).WaitForCompletion(), "EyeBlastChargeEffectNux", false);
      foreach (ParticleSystem item in eyeBlastChargeEffect.transform.GetComponentsInChildren<ParticleSystem>())
      {
        ParticleSystem.MainModule main = item.main;
        main.startSizeMultiplier *= 2f;
      }
      // ContentAddition.AddEffect(eyeBlastChargeEffect);

      voidRainPortalEffect = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Nullifier.NullifierSpawnEffect_prefab).WaitForCompletion(), "VoidRainPortalEffect", false);
      Transform voidRainRing = voidRainPortalEffect.transform.Find("Ring");
      ParticleSystemRenderer voidRainPsr = voidRainRing.GetChild(0).GetComponent<ParticleSystemRenderer>();
      voidRainPsr.sharedMaterial = voidRainPortalMat;
      ContentAddition.AddEffect(voidRainPortalEffect);

      chargeVoidRain = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabTripleBeamChargeUp_prefab).WaitForCompletion(), "ChargeVoidRainNuxEffect", false);
      foreach (ParticleSystem item in chargeVoidRain.transform.GetComponentsInChildren<ParticleSystem>())
      {
        ParticleSystem.MainModule main = item.main;
        main.startSizeMultiplier *= 1.5f;
        main.duration *= 7f;
        main.startLifetimeMultiplier *= 7f;
      }
      // ContentAddition.AddEffect(chargeVoidRain);

      spawnEffect = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabSpawnEffect_prefab).WaitForCompletion(), "FathomlessVoidlingSpawnEffect", false);
      spawnEffect.transform.localScale = new Vector3(15, 15, 15);
      spawnEffect.GetComponent<DestroyOnTimer>().duration = 12f; // 6f orig
      foreach (ParticleSystem item in spawnEffect.transform.GetChild(0).GetChild(1).GetComponentsInChildren<ParticleSystem>())
      {
        ParticleSystem.MainModule main = item.main;
        main.duration *= 1.75f;
        // main.startLifetimeMultiplier *= 1.75f;
      }
      ContentAddition.AddEffect(spawnEffect);

      CreateAttachableBarnacle();

      GameObject jointBodyPrefab = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabJointBody_prefab).WaitForCompletion();
      JointThresholdController thresholdController = jointBodyPrefab.AddComponent<JointThresholdController>();
      jointBodyPrefab.AddComponent<LegControllerNetworkHelper>();
      CharacterBody jointBody = jointBodyPrefab.GetComponent<CharacterBody>();
      jointBody.baseMaxHealth = 1250f; // 1000f mithrix
      jointBody.levelMaxHealth = 350f; // 325f mithrix
      /*
      Using the Xi Construct's implementation, there needs to be a NetworkedBodySpawnSlot for each spawn
Needs: spawncard, owner body, owner child locator, owner attach child name, spawn effect prefab (can be null), and kill effect prefab
 BodyPrefab -> Model Base -> mockModel -> Toe -> ToeJoint
0 -> 0 -> 2 -> 0
z -0.44 0.44
x -0.44 0.44
      */
      jointBodyPrefab.AddComponent<MasterSpawnSlotController>();
      ChildLocator childLocator = jointBodyPrefab.GetComponent<ModelLocator>().modelChildLocator;

      Transform toeJoint = jointBodyPrefab.transform.GetChild(0).GetChild(0).GetChild(2).GetChild(0);
      for (int i = 0; i < 4; i++)
      {
        string attachName = "AttachPoint" + i;
        NetworkedBodySpawnSlot spawnSlot = jointBodyPrefab.AddComponent<NetworkedBodySpawnSlot>();
        GameObject newAttachment = new GameObject(attachName);
        newAttachment.transform.parent = toeJoint;
        if (i < 2)
        {
          float zVector = i == 0 ? 0.46f : -0.66f;
          newAttachment.transform.localPosition = new Vector3(0f, 0.25f, zVector);
        }
        else
        {
          float xVector = i == 2 ? 0.66f : -0.66f;
          newAttachment.transform.localPosition = new Vector3(xVector, 0.25f, 0f);
        }
        childLocator.AddChild(attachName, newAttachment.transform);
        spawnSlot.spawnCard = attachableBarnacleCard;
        spawnSlot.ownerBody = jointBody;
        spawnSlot.ownerChildLocator = childLocator;
        spawnSlot.ownerAttachChildName = attachName;
      }

      jointBodyPrefab.GetComponent<EntityStateMachine>().initialStateType = new SerializableEntityStateType(typeof(JointSpawnState));

      sdWardWipe = Addressables.LoadAssetAsync<SkillDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.RaidCrabWardWipe_asset).WaitForCompletion();
      sdWardWipe.activationState = new SerializableEntityStateType(typeof(ChargeWardWipeNux));
      //sdWardWipe.interruptPriority = InterruptPriority.Death;

      sdSingularity = Addressables.LoadAssetAsync<SkillDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.RaidCrabVacuumAttack_asset).WaitForCompletion();
      sdSingularity.activationState = new SerializableEntityStateType(typeof(WanderingSingularity));
      sdSingularity.interruptPriority = InterruptPriority.PrioritySkill;

      sdMaze = Addressables.LoadAssetAsync<SkillDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.RaidCrabSpinBeam_asset).WaitForCompletion();
      sdMaze.activationState = new SerializableEntityStateType(typeof(EnterMaze));
      sdMaze.baseRechargeInterval = 45f; // 40s orig
      sdMaze.interruptPriority = InterruptPriority.PrioritySkill;

      sdMultiBeam = Addressables.LoadAssetAsync<SkillDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.RaidCrabMultiBeam_asset).WaitForCompletion();
      sdMultiBeam.activationState = new SerializableEntityStateType(typeof(ChargeVoidRain));
      sdMultiBeam.baseRechargeInterval = 15f; // 10s orig
    }

    private static void CreateSingularityProjectile()
    {
      GameObject sphereEffect = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.KillSphereVfxPlaceholder_prefab).WaitForCompletion();
      sphereEffect = PrefabAPI.InstantiateClone(sphereEffect, "WSingularitySphere", false);
      GameObject centerEffect = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabSuckLoopFX_prefab).WaitForCompletion();
      centerEffect = PrefabAPI.InstantiateClone(centerEffect, "WSingularityCenter", false);
      GameObject projectile = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabMissileProjectile_prefab).WaitForCompletion();
      projectile = PrefabAPI.InstantiateClone(projectile, "WSingularityProjectile", true);
      GameObject ghost = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_LunarWisp.LunarWispTrackingBombGhost_prefab).WaitForCompletion();
      ghost = PrefabAPI.InstantiateClone(ghost, "WSingularityGhost", false);
      LoopSoundDef singularityLSD = Addressables.LoadAssetAsync<LoopSoundDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.lsdVoidRaidCrabVacuumAttack_asset).WaitForCompletion();

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

    public static void CreateTube(Transform parent)
    {
      GameObject gameObject = new("WallHolder");
      gameObject.transform.parent = parent;
      gameObject.transform.position = Vector3.zero;
      gameObject.transform.localPosition = new Vector3(-2.5f, 0.0f, 0.0f);
      GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
      primitive.GetComponent<MeshRenderer>().material = voidCylinderMat;
      UnityEngine.Object.Destroy(primitive.GetComponent<CapsuleCollider>());
      MeshCollider collider = primitive.AddComponent<MeshCollider>();
      // primitive.AddComponent<ReverseNormals>();
      primitive.transform.localScale = new Vector3(115f, 1250f, 115f); // 110 orig
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

      GameObject mazeController = new GameObject("MazeSpawnPointController");
      mazeController.transform.parent = parent;
      mazeController.transform.position = Vector3.zero;
      mazeController.AddComponent<MazeSpawnPointController>();
    }
  }
}
