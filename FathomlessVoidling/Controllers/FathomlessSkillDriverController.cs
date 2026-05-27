using System.Collections.Generic;
using EntityStates;
using RoR2;
using RoR2.CharacterAI;
using UnityEngine;

namespace FathomlessVoidling.Controllers;

public class FathomlessSkillDriverController : MonoBehaviour
{
    private const string bodyStateMachineName = "Body";

    private CharacterBody body;
    private HealthComponent healthComponent;
    private CharacterMaster master;
    private EntityStateMachine bodyStateMachine;

    private List<JointThresholdController> jtcs = [];

    private List<AISkillDriver> weaponDrivers = [];
    private AISkillDriver wardWipeDriver;
    private AISkillDriver singularityDriver;
    private AISkillDriver mazeDriver;

    private bool isWardWipeAvailable;
    private bool singularityUnlocked;
    private bool mazeUnlocked;

    private void Start()
    {
        this.body = this.GetComponent<CharacterBody>();
        this.master = this.body.master;
        this.healthComponent = this.body.healthComponent;
        foreach (AISkillDriver driver in this.master.GetComponents<AISkillDriver>())
        {
            switch (driver.customName)
            {
                case "WardWipe":
                    this.wardWipeDriver = driver;
                    break;
                case "FireMissiles":
                case "FireMultiBeam":
                    this.weaponDrivers.Add(driver);
                    break;
                case "SpinBeam":
                    this.mazeDriver = driver;
                    break;
                case "Vacuum Attack":
                    this.singularityDriver = driver;
                    break;
            }
        }
        foreach (TeamComponent tc in TeamComponent.GetTeamMembers(TeamIndex.Void))
        {
            CharacterBody cb = tc.GetComponent<CharacterBody>();
            if (cb && cb.name == "VoidRaidCrabJointBody(Clone)")
            {
                JointThresholdController jtc = cb.GetComponent<JointThresholdController>();
                if (jtc)
                    jtcs.Add(jtc);
            }
        }
    }

    private void FixedUpdate()
    {
        if (!this.body || !this.master)
            return;
        if (!this.bodyStateMachine)
        {
            this.bodyStateMachine = EntityStateMachine.FindByCustomName(this.gameObject, bodyStateMachineName);
            if (!this.bodyStateMachine)
                return;
        }

        this.isWardWipeAvailable = this.CanUseWardWipe();

        CheckMazeUnlock();
        CheckSingularityUnlock();

        SetEnabled(this.wardWipeDriver, isWardWipeAvailable);
        SetEnabled(this.weaponDrivers, this.CanUseWeaponSkills());
        SetEnabled(this.singularityDriver, this.CanUseBodySkills() && this.singularityUnlocked);
        SetEnabled(this.mazeDriver, this.CanUseBodySkills() && this.mazeUnlocked);
    }

    private static void SetEnabled(List<AISkillDriver> drivers, bool enabled)
    {
        foreach (AISkillDriver driver in drivers)
        {
            if (driver.enabled != enabled)
                driver.enabled = enabled;
        }
    }

    private static void SetEnabled(AISkillDriver driver, bool enabled)
    {
        if (driver.enabled != enabled)
            driver.enabled = enabled;
    }

    private bool CanUseWardWipe()
    {
        bool allJointsReachedThreshold = true;
        foreach (JointThresholdController jtc in jtcs)
        {
            if (!jtc.reachedThreshold)
            {
                allJointsReachedThreshold = false;
                break;
            }
        }
        return allJointsReachedThreshold;
    }

    private bool CheckSingularityUnlock()
    {
        if (!this.singularityUnlocked)
        {
            foreach (JointThresholdController jtc in jtcs)
            {
                if (jtc.reachedThreshold)
                {
                    this.singularityUnlocked = true;
                    return true;
                }
            }
        }
        return false;
    }
    // maze unlocks at the full threshold
    private bool CheckMazeUnlock()
    {
        if (!this.mazeUnlocked && this.healthComponent.combinedHealthFraction < 1f && !this.isWardWipeAvailable)
        {
            foreach (JointThresholdController jtc in jtcs)
            {
                if (jtc.reachedThreshold)
                {
                    this.mazeUnlocked = true;
                    return true;
                }
            }
        }
        return false;
    }

    public bool CanUseWeaponSkills()
    {
        return this.bodyStateMachine.state is GenericCharacterMain && !this.isWardWipeAvailable;
    }

    public bool CanUseBodySkills()
    {
        return this.bodyStateMachine.IsInMainState() && !this.isWardWipeAvailable;
    }
}
