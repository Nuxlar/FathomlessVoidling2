using RoR2;
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
    public CharacterBody voidlingBody;

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
        this.voidlingBody = characterMaster.GetBody();
    }

    public int GetCurrentPhase()
    {
        if (!this.voidlingBody) return -1;
        int itemCount = this.voidlingBody.inventory.GetItemCountEffective(RoR2Content.Items.MinHealthPercentage);
        if (itemCount == 5) return 2;
        if (itemCount == 33) return 1;
        if (itemCount == 66) return 0;
        return -1;
    }
}
