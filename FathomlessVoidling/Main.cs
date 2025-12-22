using BepInEx;
using HG;
using R2API;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Mecanim;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

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

    public void Awake()
    {
      Instance = this;

      Log.Init(Logger);

      ContentAddition.AddEntityState<BetterSpawnState>(out _);
      ContentAddition.AddEntityState<JointSpawnState>(out _);

      LoadAssets();
      TweakBigVoidling();

      On.RoR2.SceneDirector.Start += TweakBossDirector;
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
          Transform curve = cam.GetChild(2);
          curve.GetChild(0).position = new Vector3(-0.2f, 217.13f, -442.84f);
          curve.GetChild(1).position = new Vector3(-12.5f, 29.7f, -181.4f);
        }
      }
      orig(self);
    }

    private static void TweakBigVoidling()
    {
      AssetReferenceT<GameObject> voidlingRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabBody_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(voidlingRef).Completed += (x) =>
          {
            GameObject body = x.Result;
            ModelLocator modelLocator = body.GetComponent<ModelLocator>();

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
            printController.printTime = 10f; // 6f orig
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
      AssetReferenceT<GameObject> spawnRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabSpawnEffect_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(spawnRef).Completed += (x) =>
      {
        spawnEffect = x.Result;
        spawnEffect.transform.localScale = new Vector3(15, 15, 15);
        spawnEffect.GetComponent<DestroyOnTimer>().duration = 12f; // 6f orig
        foreach (ParticleSystem item in spawnEffect.transform.GetChild(0).GetChild(1).GetComponentsInChildren<ParticleSystem>())
        {
          ParticleSystem.MainModule main = item.main;
          main.duration *= 2f;
          main.startLifetimeMultiplier *= 2f;
        }
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

        modelLocator.modelTransform.Find("VoidRaidCrabArmature/ROOT/HeadBase/eyeballRoot").gameObject.SetActive(false);
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