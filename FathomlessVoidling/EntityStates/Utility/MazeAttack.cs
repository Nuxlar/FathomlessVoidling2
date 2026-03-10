using EntityStates;
using EntityStates.GrandParentBoss;
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
        public static GameObject beamVfxPrefab;
        public static float beamRadius = 16f;
        public static float beamMaxDistance = 400f;
        public static float beamDpsCoefficient = 40f;
        public static float beamTickFrequency = 30f;
        public static GameObject beamImpactEffectPrefab;
        public static LoopSoundDef loopSound;
        public static string enterSoundString = "Play_voidRaid_superLaser_start";

        public string animLayerName = "Body";
        public string animStateName = "SpinBeamLoop";
        public string animPlaybackRateParamName = "SpinBeam.playbackRate";
        public float baseDuration = 9f; // 8f orig
        private float duration;
        private float beamStopwatch = 0f;
        private float beamTickTimer;
        private bool beamsFiring = false;
        private LoopSoundManager.SoundLoopPtr loopPtr;

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
            this.duration = this.baseDuration;
            if (!string.IsNullOrEmpty(this.animLayerName) && !string.IsNullOrEmpty(this.animStateName))
            {
                if (!string.IsNullOrEmpty(this.animPlaybackRateParamName))
                    this.PlayAnimation(this.animLayerName, this.animStateName, this.animPlaybackRateParamName, this.duration);
                else
                    this.PlayAnimation(this.animLayerName, this.animStateName);
            }
            if (!MazeSpawnPointController.instance)
                return;
            List<int> spawnPositions = SelectBeamPositions(BaseMazeAttackState.dualBeams, BaseMazeAttackState.alternatingBeams);
            foreach (int position in spawnPositions)
            {
                Debug.LogWarning("SPAWN POSITION: " + position);
                Transform child = MazeSpawnPointController.instance.transform.Find("MazeAnchor" + position);
                if (child)
                {
                    Ray randomRay = new Ray();
                    randomRay.origin = child.position;
                    randomRay.direction = child.forward;
                    EffectManager.SpawnEffect(Main.voidRainPortalEffect, new EffectData()
                    {
                        origin = child.position,
                        rotation = Util.QuaternionSafeLookRotation(child.forward)
                    }, false);
                    GameObject warningLaserVfxInstance = Object.Instantiate<GameObject>(Main.voidRainWarning);
                    RayAttackIndicator warningLaserVfxInstanceRayAttackIndicator = warningLaserVfxInstance.GetComponent<RayAttackIndicator>();
                    warningLaserVfxInstanceRayAttackIndicator.attackRange = 1000f;
                    warningLaserVfxInstanceRayAttackIndicator.attackRay = randomRay;
                }
            }
            // this.CreateBeamVFXInstance(MazeAttack.beamVfxPrefab);
            //this.loopPtr = LoopSoundManager.PlaySoundLoopLocal(this.gameObject, MazeAttack.loopSound);
            Util.PlaySound(MazeAttack.enterSoundString, this.gameObject);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if ((double)this.fixedAge < this.duration || !this.isAuthority)
                return;
            this.outer.SetNextState(new ExitMaze());
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
    }
}