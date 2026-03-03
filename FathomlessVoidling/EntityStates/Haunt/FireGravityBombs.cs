using RoR2;
using RoR2.Navigation;
using RoR2.Projectile;
using EntityStates;
using System.Collections.Generic;
using UnityEngine;
using R2API;

namespace FathomlessVoidling.EntityStates.Haunt;

public class FireGravityBombs : BaseState
{
    public static GameObject projectilePrefab = Main.gravityBombProjectile;
    public static float damageCoefficient = 1f;
    public static float duration = 40f;
    public static float cooldown = 10f;
    public static float chanceToFirePerSecond = 0.15f;
    private float chargeTimer;
    private float cooldownTimer;

    public override void OnEnter()
    {
        base.OnEnter();
        this.chargeTimer = 0f;
        this.cooldownTimer = cooldown;
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
                this.chargeTimer = duration;
                this.cooldownTimer = cooldown;
            }
        }
        else
        {
            if ((double)Random.value < FireGravityBombs.chanceToFirePerSecond)
                this.FireProjectile();
        }
    }

    private void FireProjectile()
    {
        NodeGraph groundNodes = SceneInfo.instance.groundNodes;
        //  NodeGraph airNodes = SceneInfo.instance.airNodes;
        if (!(bool)groundNodes)
            return;
        List<NodeGraph.NodeIndex> nodesInRange = groundNodes.FindNodesInRange(Vector3.zero, 25f, 200f, HullMask.Human);
        NodeGraph.NodeIndex nodeIndex = nodesInRange[Random.Range(0, nodesInRange.Count)];
        Vector3 position;
        groundNodes.GetNodePosition(nodeIndex, out position);
        FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
        fireProjectileInfo.projectilePrefab = FireGravityBombs.projectilePrefab;
        fireProjectileInfo.owner = this.gameObject;
        fireProjectileInfo.damage = this.damageStat * FireGravityBombs.damageCoefficient;
        fireProjectileInfo.position = position;
        DamageTypeCombo damageType = DamageType.Generic | DamageType.BypassBlock;
        damageType.AddModdedDamageType(Main.gravityDamageType);
        fireProjectileInfo.damageTypeOverride = damageType;
        ProjectileManager.instance.FireProjectile(fireProjectileInfo);
    }

    public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Death;
}
