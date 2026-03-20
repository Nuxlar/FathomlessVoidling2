
using RoR2;
using RoR2.CharacterAI;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace FathomlessVoidling.Controllers;

public class FathomlessMissionController : NetworkBehaviour
{
    public static FathomlessMissionController instance { get; private set; }
    public CombatSquad bossCombatSquad;
    public CharacterBody hauntBody;
    public CharacterMaster voidlingMaster;
    public AISkillDriver wardWipeDriver;
    public AISkillDriver singularityDriver;
    public AISkillDriver mazeDriver;
    public AISkillDriver fireMissileDriver;
    public CharacterBody voidlingBody;
    public PhasedInventorySetter inventorySetter;

    private void OnEnable()
    {
        FathomlessMissionController.instance = SingletonHelper.Assign<FathomlessMissionController>(FathomlessMissionController.instance, this);
        this.bossCombatSquad = this.GetComponent<CombatSquad>();
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
        foreach (AISkillDriver driver in characterMaster.GetComponents<AISkillDriver>())
        {
            switch (driver.customName)
            {
                case "WardWipe": this.wardWipeDriver = driver; break;
                case "Vacuum Attack": this.singularityDriver = driver; break;
                case "SpinBeam": this.mazeDriver = driver; break;
                case "FireMissiles": this.fireMissileDriver = driver; break;
            }
        }
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

}
