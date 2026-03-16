
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
}
