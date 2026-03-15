using EntityStates;
using EntityStates.VoidRaidCrab;
using FathomlessVoidling.Controllers;
using FathomlessVoidling.EntityStates.Haunt;
using RoR2;
using RoR2.Audio;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace FathomlessVoidling.EntityStates.Utility
{
    public class MazeAttack : BaseMazeAttackState
    {
        public static GameObject beamVfxPrefab = Main.mazeLaserPrefab;
        public static float beamRadius = 24f; // 8f orig
        public static float beamMaxDistance = 400f; // 400 orig
        public static float beamDpsCoefficient = 40f; // 40 orig
        public static float beamTickFrequency = 30f; // 30 orig
        public static GameObject beamImpactEffectPrefab = Main.mazeImpactEffect;
        public static GameObject portalEffectPrefab = Main.mazePortalEffect;
        public static GameObject chargeEffectPrefab = Main.mazeChargeUpPrefab;
        public static GameObject muzzleEffectPrefab = Main.mazeMuzzleEffect;
        public static LoopSoundDef loopSound = SpinBeamAttack.loopSound;
        public static string chargeSoundString = "Play_voidRaid_superLaser_chargeUp";
        public static string fireSoundString = "Play_voidRaid_superLaser_start";
        public static string endSoundString = "Play_voidRaid_superLaser_end";

        private float duration;
        private float fireStopwatch = 0f;
        private float delayStopwatch = 0f;
        private float beamTickTimer = 0f;
        private bool beamsFiring = false;
        private int lastFiredAxis = -1;
        private int wavesFired = 0;
        private List<LoopSoundManager.SoundLoopPtr> loopPtrs = new List<LoopSoundManager.SoundLoopPtr>();
        private List<GameObject> chargeEffectInstances = new List<GameObject>();
        private GameObject eyeEffectInstance;
        private List<List<int>> positionMatrix = new List<List<int>>()
        {
            // Top Left, Top Right
            new List<int>() { 1, 1 },
            // Bottom Left, Bottom Right
            new List<int>() { 1, 1 },
            // Bottom Up Left, Top Down Left
            new List<int>() { 1, 1 },
            // Bottom Up Right, Top Down Right
            new List<int>() { 1, 1 },
        };

        public override void OnEnter()
        {
            base.OnEnter();

            this.duration = (this.waves * (this.beamDelay + this.beamDuration)) + 0.5f;
            if (FathomlessMissionController.instance && NetworkServer.active && this.randomBeams)
            {
                if (FathomlessMissionController.instance.hauntBody)
                {
                    VoidlingHauntManager manager = (VoidlingHauntManager)FathomlessMissionController.instance.hauntBody.GetComponent<EntityStateMachine>().state;
                    manager.MazeOverride();
                }
                else Debug.LogWarning("FathomlessVoidling.MazeAttack: FathomlessMissionController does not have a HauntBody!");
            }
            ChildLocator modelChildLocator = this.GetModelChildLocator();
            if (modelChildLocator && MazeAttack.muzzleEffectPrefab)
            {
                Transform transform = this.muzzleTransform ?? this.characterBody.coreTransform;
                if (transform)
                {
                    this.eyeEffectInstance = Object.Instantiate<GameObject>(MazeAttack.muzzleEffectPrefab, transform.position, transform.rotation);
                    this.eyeEffectInstance.transform.parent = transform;
                    ScaleParticleSystemDuration component = this.eyeEffectInstance.GetComponent<ScaleParticleSystemDuration>();
                    if (component)
                        component.newDuration = this.duration / 2f;
                }
            }

            if (!MazeSpawnPointController.instance)
                return;
            BeginMaze();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!this.beamsFiring)
                this.delayStopwatch += Time.fixedDeltaTime;
            else
                this.fireStopwatch += Time.fixedDeltaTime;


            if (this.beamsFiring && this.isAuthority)
            {
                if (this.beamTickTimer <= 0.0)
                {
                    this.beamTickTimer += 1f / MazeAttack.beamTickFrequency;
                    this.FireBeamBulletAuthority();
                }
                this.beamTickTimer -= this.GetDeltaTime();
            }

            if (this.delayStopwatch >= this.beamDelay)
            {
                this.delayStopwatch = 0f;
                foreach (GameObject instance in this.chargeEffectInstances)
                {
                    Debug.LogWarning(instance.transform.parent);
                    Util.PlaySound(MazeAttack.fireSoundString, instance.transform.parent.gameObject);
                    GameObject beamVfxInstance = this.CreateBeamVFXInstance(MazeAttack.beamVfxPrefab, instance.transform.parent);
                    this.loopPtrs.Add(LoopSoundManager.PlaySoundLoopLocal(beamVfxInstance, MazeAttack.loopSound));
                    this.beamVfxInstances.Add(beamVfxInstance);
                }
                this.beamsFiring = true;
            }

            if (this.fireStopwatch >= this.beamDuration)
            {
                this.beamsFiring = false;
                this.fireStopwatch = 0f;
                this.beamTickTimer = 0f;
                ResetMaze();
            }

            if (this.wavesFired < this.waves || this.fixedAge < this.duration || !this.isAuthority)
                return;
            this.outer.SetNextState(new ExitMaze());
        }

        public override void OnExit()
        {
            EntityState.Destroy(this.eyeEffectInstance);
            base.OnExit();
        }

        // Row 0 & 1 = Horizontal (LR) lasers
        // Row 2 & 3 = Vertical (UD) lasers
        // Overlap rule: max 1 selection per row
        private List<int> SelectBeamPositions(bool randomizeBeams)
        {
            List<int> horizontalRows = new List<int> { 0, 1 };
            List<int> verticalRows = new List<int> { 2, 3 };
            List<int> availableRows;

            if (!randomizeBeams)
            {
                if (this.lastFiredAxis == -1)
                {
                    if (Random.value > 0.5f)
                    {
                        availableRows = horizontalRows;
                        this.lastFiredAxis = 0;
                    }
                    else
                    {
                        availableRows = verticalRows;
                        this.lastFiredAxis = 1;
                    }
                }
                else
                {
                    if (this.lastFiredAxis == 0)
                    {
                        availableRows = verticalRows;
                        this.lastFiredAxis = 1;
                    }
                    else
                    {
                        availableRows = horizontalRows;
                        this.lastFiredAxis = 0;
                    }
                }
            }
            else
                availableRows = new List<int> { 0, 1, 2, 3 };

            availableRows = availableRows.OrderBy(_ => Random.value).ToList();

            int beamCount = 2;
            List<int> selected = new List<int>();

            for (int i = 0; i < beamCount; i++)
            {
                int rowIndex = availableRows[i];
                List<int> row = positionMatrix[rowIndex];
                int posIndex = Random.Range(0, row.Count);
                int selectedIndex = rowIndex * 2 + posIndex;
                selected.Add(selectedIndex);
            }

            return selected;
        }

        private void ResetMaze()
        {
            foreach (GameObject instance in this.chargeEffectInstances)
            {
                EntityState.Destroy(instance);
            }
            this.chargeEffectInstances.Clear();

            foreach (GameObject beamInstance in this.beamVfxInstances)
            {
                Util.PlaySound(MazeAttack.endSoundString, beamInstance);
                VfxKillBehavior.KillVfxObject(beamInstance);
            }
            this.beamVfxInstances.Clear();
            foreach (LoopSoundManager.SoundLoopPtr loopPtr in this.loopPtrs)
            {
                LoopSoundManager.StopSoundLoopLocal(loopPtr);
            }
            if (this.wavesFired < this.waves)
                BeginMaze();
        }

        private void BeginMaze()
        {
            this.wavesFired++;
            List<int> spawnPositions = SelectBeamPositions(this.randomBeams);

            foreach (int position in spawnPositions)
            {
                Debug.LogWarning("SPAWN POSITION: " + position);
                Transform child = MazeSpawnPointController.instance.transform.Find("MazeAnchor" + position);
                if (child)
                {
                    EffectManager.SpawnEffect(MazeAttack.portalEffectPrefab, new EffectData()
                    {
                        origin = child.position,
                        rotation = Util.QuaternionSafeLookRotation(child.forward)
                    }, false);

                    GameObject chargeUpEffect = this.CreateBeamVFXInstance(MazeAttack.chargeEffectPrefab, child);
                    Util.PlaySound(MazeAttack.chargeSoundString, chargeUpEffect);
                    this.chargeEffectInstances.Add(chargeUpEffect);
                }
            }
            Debug.LogWarning("~~~~~~END SELECTION~~~~~~~~");
        }

        private void FireBeamBulletAuthority()
        {
            if (this.beamVfxInstances.Count == 0)
                return;

            foreach (GameObject beamInstance in this.beamVfxInstances)
            {
                Ray beamRay = new Ray();
                beamRay.origin = beamInstance.transform.position;
                beamRay.direction = beamInstance.transform.forward;

                new BulletAttack()
                {
                    muzzleName = BaseMazeAttackState.muzzleTransformNameInChildLocator,
                    origin = beamRay.origin,
                    aimVector = beamRay.direction,
                    minSpread = 0.0f,
                    maxSpread = 0.0f,
                    maxDistance = 400f,
                    hitMask = LayerIndex.CommonMasks.bullet,
                    stopperMask = (LayerMask)0,
                    bulletCount = 1U,
                    radius = MazeAttack.beamRadius,
                    smartCollision = false,
                    queryTriggerInteraction = QueryTriggerInteraction.Ignore,
                    procCoefficient = 1f,
                    procChainMask = new ProcChainMask(),
                    owner = this.gameObject,
                    weapon = this.gameObject,
                    damage = MazeAttack.beamDpsCoefficient * this.damageStat / MazeAttack.beamTickFrequency,
                    damageColorIndex = DamageColorIndex.Default,
                    damageType = ((DamageTypeCombo)DamageType.Generic),
                    falloffModel = BulletAttack.FalloffModel.None,
                    force = 0.0f,
                    hitEffectPrefab = MazeAttack.beamImpactEffectPrefab,
                    tracerEffectPrefab = null,
                    isCrit = false,
                    HitEffectNormal = false
                }.Fire();
            }
        }
    }
}