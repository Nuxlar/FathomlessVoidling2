using RoR2;
using RoR2.Navigation;
using RoR2.Projectile;
using EntityStates;
using System.Collections.Generic;
using UnityEngine;
using R2API;
using FathomlessVoidling.Controllers;
using UnityEngine.Networking;

namespace FathomlessVoidling.EntityStates.Haunt;

public class VoidlingHauntManager : BaseState
{
    // TODO: add "director boost" functionality, giving X credits to the barnacle director
    public static GameObject projectilePrefab = Main.gravityBombProjectile;
    public static float damageCoefficient = 1f;
    public float duration = 20f;
    public float cooldown = 40f;
    public float chanceToFirePerSecond = 0.15f;
    private float chargeTimer;
    private float cooldownTimer;
    private GameObject barnacleDirector;
    private int phaseNumber = 0;

    public override void OnEnter()
    {
        base.OnEnter();
        Transform directorTransform = this.characterBody.transform.Find("Barnacle Director");
        if (directorTransform)
            barnacleDirector = directorTransform.gameObject;
        this.chargeTimer = 0f;
        this.cooldownTimer = cooldown;
        CheckCurrentPhase();
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        if (!this.isAuthority)
            return;
        this.chargeTimer -= this.GetDeltaTime();

        if (this.chargeTimer <= 0f)
        {
            this.cooldownTimer -= this.GetDeltaTime();
            if (this.cooldownTimer <= 0f)
            {
                CheckCurrentPhase();
                this.chargeTimer = duration;
                this.cooldownTimer = cooldown;
                if (barnacleDirector)
                {
                    if (!barnacleDirector.activeSelf)
                        barnacleDirector.SetActive(true);
                    else
                    {
                        CombatDirector cd = barnacleDirector.GetComponent<CombatDirector>();
                        if (cd)
                            cd.monsterCredit += 50f;
                    }
                }
            }
        }
        else
        {
            if ((double)Random.value < this.chanceToFirePerSecond)
                this.FireProjectile();
        }
    }

    public void MazeOverride()
    {
        CheckCurrentPhase();
        this.chargeTimer = duration;
        this.cooldownTimer = cooldown;
    }

    public void WardWipeOverride()
    {
        CheckCurrentPhase();
        this.chargeTimer = duration;
        this.cooldownTimer = cooldown;
        if (barnacleDirector)
        {
            CombatDirector cd = barnacleDirector.GetComponent<CombatDirector>();
            if (cd)
                cd.monsterCredit += 150f;
        }
    }

    private void CheckCurrentPhase()
    {
        if (FathomlessMissionController.instance && NetworkServer.active)
        {
            int phaseIndex = FathomlessMissionController.instance.GetCurrentPhase();
            if (phaseIndex != -1)
            {
                this.phaseNumber = phaseIndex;
                switch (this.phaseNumber)
                {
                    case 0:
                        this.cooldown = 30f;
                        break;
                    case 1:
                        this.cooldown = 20f;
                        break;
                }
            }
        }
    }

    private void FireProjectile()
    {
        NodeGraph groundNodes = SceneInfo.instance.groundNodes;
        if (!(bool)groundNodes)
            return;
        List<NodeGraph.NodeIndex> nodesInRange = groundNodes.FindNodesInRange(this.characterBody.corePosition, 25f, 200f, HullMask.Human);
        NodeGraph.NodeIndex nodeIndex = nodesInRange[Random.Range(0, nodesInRange.Count)];
        Vector3 position;
        groundNodes.GetNodePosition(nodeIndex, out position);
        FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
        fireProjectileInfo.projectilePrefab = VoidlingHauntManager.projectilePrefab;
        fireProjectileInfo.owner = this.gameObject;
        fireProjectileInfo.damage = 1f;// this.damageStat * VoidlingHauntManager.damageCoefficient;
        fireProjectileInfo.position = position;
        DamageTypeCombo damageType = DamageType.Generic | DamageType.BypassBlock;
        damageType.AddModdedDamageType(Main.gravityDamageType);
        fireProjectileInfo.damageTypeOverride = damageType;
        ProjectileManager.instance.FireProjectile(fireProjectileInfo);
    }

    public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Death;
}
