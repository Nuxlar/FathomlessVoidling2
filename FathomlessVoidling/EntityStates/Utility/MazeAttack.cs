using EntityStates;
using EntityStates.GrandParentBoss;
using EntityStates.VoidRaidCrab;
using EntityStates.VoidRaidCrab.Weapon;
using FathomlessVoidling.Components;
using FathomlessVoidling.Controllers;
using RoR2;
using RoR2.Audio;
using RoR2.Projectile;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Types;

namespace FathomlessVoidling.EntityStates.Utility
{
    public class MazeAttack : BaseMazeAttackState
    {
        // choose X spots to spawn the beams -> spawn portal(s) + warning vfx -> attack
        // beamtick 30
        // beamradius 8
        // beammaxdistance 400
        // beamdpscoeff 40
        public static GameObject beamVfxPrefab = Main.mazeLaserPrefab;
        public static float beamRadius = 16f;
        public static float beamMaxDistance = 400f;
        public static float beamDpsCoefficient = 40f;
        public static float beamTickFrequency = 30f;
        public static GameObject beamImpactEffectPrefab = Main.mazeImpactEffect;
        public static GameObject portalEffectPrefab = Main.mazePortalEffect;
        public static GameObject chargeEffectPrefab = Main.mazeChargeUpPrefab;
        public static GameObject muzzleEffectPrefab = Main.mazeMuzzleEffect;
        public static LoopSoundDef loopSound = SpinBeamAttack.loopSound;
        // Play_voidRaid_superLaser_chargeUp
        public static string chargeSoundString = "Play_voidRaid_superLaser_chargeUp";
        public static string fireSoundString = "Play_voidRaid_superLaser_start";
        public float laserDelayDuration = 2f;
        public float laserFireDuration = 3f;
        public float baseDuration = 10f; // 8f orig
        private float duration;
        private float fireStopwatch = 0f;
        private float delayStopwatch = 0f;
        private float beamTickTimer = 0f;
        private bool beamsFiring = false;
        private int previousAnchorIndex = -1;
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

        // TODO prevent same selection on single
        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = this.waves * (this.laserDelayDuration + this.laserFireDuration);
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
                        component.newDuration = this.duration;
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

            if (this.delayStopwatch >= this.laserDelayDuration)
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

            if (this.fireStopwatch >= this.laserFireDuration)
            {
                this.beamsFiring = false;
                this.fireStopwatch = 0f;
                this.beamTickTimer = 0f;
                ResetMaze();
            }

            if (this.fixedAge < this.duration || !this.isAuthority)
                return;
            this.outer.SetNextState(new ExitMaze());
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        // Row 0 & 1 = Horizontal (LR) lasers
        // Row 2 & 3 = Vertical (UD) lasers
        // Overlap rule: max 1 selection per row
        // Perpendicular rule: if false, restrict to horizontal OR vertical rows only
        // 8 values, idx up to 7
        // rows and columns but uneven
        // convert 2, 1 -> 6 
        // row * 2 + pos
        private List<int> SelectBeamPositions(bool dualBeam, bool allowPerpendicular)
        {
            List<int> horizontalRows = new List<int> { 0, 1 };
            List<int> verticalRows = new List<int> { 2, 3 };
            List<int> availableRows;

            if (!allowPerpendicular)
                availableRows = (Random.value > 0.5f) ? horizontalRows : verticalRows;
            else
                availableRows = new List<int> { 0, 1, 2, 3 };

            availableRows = availableRows.OrderBy(_ => Random.value).ToList();

            int beamCount = dualBeam ? 2 : 1;
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
                VfxKillBehavior.KillVfxObject(beamInstance);
            }
            this.beamVfxInstances.Clear();
            foreach (LoopSoundManager.SoundLoopPtr loopPtr in this.loopPtrs)
            {
                LoopSoundManager.StopSoundLoopLocal(loopPtr);
            }

            BeginMaze();
        }

        private void BeginMaze()
        {
            List<int> spawnPositions = SelectBeamPositions(this.dualBeams, this.alternatingBeams);
            if (!this.dualBeams)
            {
                if (this.previousAnchorIndex != -1)
                {
                    int newIdx = spawnPositions[0];
                    if (newIdx == this.previousAnchorIndex)
                    {
                        while (newIdx == this.previousAnchorIndex)
                        {
                            spawnPositions = SelectBeamPositions(this.dualBeams, this.alternatingBeams);
                            newIdx = spawnPositions[0];
                        }
                    }
                }
                else
                    this.previousAnchorIndex = spawnPositions[0];
            }
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