
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace FathomlessVoidling.Controllers;

public class FathomlessMissionController : NetworkBehaviour
{
    public static FathomlessMissionController instance { get; private set; }
    public CombatSquad bossCombatSquad;
    public CharacterBody hauntBody;
    public float bossMinInvulnerabilityHealthFraction = 0.2f;
    public float bossHealthPercentageToDealFromCombinedPodDestruction = 0.8f;
    private CharacterMaster voidlingMaster;
    private CharacterBody voidlingBody;
    private PhasedInventorySetter inventorySetter;

    private void OnEnable()
    {
        FathomlessMissionController.instance = SingletonHelper.Assign<FathomlessMissionController>(FathomlessMissionController.instance, this);
        this.bossCombatSquad.onMemberDiscovered += new Action<CharacterMaster>(this.BossCombatSquad_onMemberDiscovered);
    }

    private void OnDisable()
    {
        FathomlessMissionController.instance = SingletonHelper.Unassign<FathomlessMissionController>(FathomlessMissionController.instance, this);
        this.bossCombatSquad.onMemberDiscovered -= new Action<CharacterMaster>(this.BossCombatSquad_onMemberDiscovered);
    }

    private void BossCombatSquad_onMemberDiscovered(CharacterMaster characterMaster)
    {
        this.voidlingMaster = characterMaster;
        this.voidlingBody = characterMaster.GetBody();
        PhasedInventorySetter setter = this.voidlingBody.GetComponent<PhasedInventorySetter>();
        if (setter)
            this.inventorySetter = setter;
    }

    public int GetCurrentPhase()
    {
        if (this.inventorySetter)
            return this.inventorySetter.phaseIndex;
        else
            return -1;
    }

    private class PodDeathListener : MonoBehaviour, IOnKilledServerReceiver
    {
        public FathomlessMissionController listener;

        public void OnKilledServer(DamageReport damageReport)
        {
        }
    }

    private class SolusWingDamageFilter : MonoBehaviour, IOnIncomingDamageServerReceiver
    {
        private HealthComponent healthComponent;
        private CharacterBody characterBody;
        public float minInvulnerabilityHealthFraction;
        private bool _invulnBuffDeployed;

        private static BuffDef invulnBuff => DLC3Content.Buffs.SolusWingInvulnerability;

        private bool invulnBuffDeployed
        {
            get => this._invulnBuffDeployed;
            set
            {
                if (this._invulnBuffDeployed == value)
                    return;
                this._invulnBuffDeployed = value;
                if (!(bool)this.characterBody)
                    return;
                if (this._invulnBuffDeployed)
                    this.characterBody.AddBuff(FathomlessMissionController.SolusWingDamageFilter.invulnBuff);
                else
                    this.characterBody.RemoveBuff(FathomlessMissionController.SolusWingDamageFilter.invulnBuff);
            }
        }

        private bool shouldBeInvulnerable
        {
            get
            {
                return (double)this.healthComponent.combinedHealthFraction > (double)this.minInvulnerabilityHealthFraction;
            }
        }

        private void Awake()
        {
            this.healthComponent = this.GetComponent<HealthComponent>();
            this.characterBody = this.GetComponent<CharacterBody>();
        }

        private void OnEnable()
        {
            this.healthComponent.AddOnIncomingDamageServerReceiver((IOnIncomingDamageServerReceiver)this);
            this.invulnBuffDeployed = true;
        }

        private void OnDisable()
        {
            this.invulnBuffDeployed = false;
            this.healthComponent.RemoveOnIncomingDamageServerReceiver((IOnIncomingDamageServerReceiver)this);
        }

        private void FixedUpdate() => this.invulnBuffDeployed = this.shouldBeInvulnerable;

        public void OnIncomingDamageServer(DamageInfo damageInfo)
        {
            if (damageInfo.attacker == this.gameObject)
                return;
            damageInfo.rejected = true;
        }
    }
}
