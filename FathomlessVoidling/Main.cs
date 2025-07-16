using BepInEx;
using R2API;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Mecanim;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

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

    public void Awake()
    {
      Instance = this;

      Stopwatch stopwatch = Stopwatch.StartNew();

      Log.Init(Logger);

      ContentAddition.AddEntityState<BetterSpawnState>(out _);

      LoadAssets();
      TweakBigVoidling();

      PluginDirectory = System.IO.Path.GetDirectoryName(Info.Location);
      LanguageFolderHandler.Register(PluginDirectory);

      stopwatch.Stop();
      Log.Info_NoCallerPrefix($"Initialized in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
    }

    private static void TweakBigVoidling()
    {
      AssetReferenceT<GameObject> voidlingRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabBody_prefab);
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
                UnityEngine.Debug.LogWarning("CUNT " + esm.initialStateType);
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
            printController.age = 0f;
            printController.printTime = 6f;
            printController.disableWhenFinished = true;
            printController.startingPrintHeight = 200f;
            printController.maxPrintHeight = 500f;
            printController.startingPrintBias = -10f;
            printController.maxPrintBias = 0f;
            printController.animateFlowmapPower = false;
            printController.startingFlowmapPower = 0f;
            printController.maxFlowmapPower = 0f;
            printController.printCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1f, 1f);

            // Add eye blinking
            model.AddComponent<RandomBlinkController>();
          };
    }

    private static void LoadAssets()
    {
      AssetReferenceT<GameObject> spawnRef = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabSpawnEffect_prefab);
      AssetAsyncReferenceManager<GameObject>.LoadAsset(spawnRef).Completed += (x) => spawnEffect = x.Result;

      AssetReferenceT<CharacterSpawnCard> jointCardRef = new AssetReferenceT<CharacterSpawnCard>(RoR2BepInExPack.GameAssetPaths.RoR2_DLC1_VoidRaidCrab.cscVoidRaidCrabJoint_asset);
      AssetAsyncReferenceManager<CharacterSpawnCard>.LoadAsset(jointCardRef).Completed += (x) => jointCard = x.Result;
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