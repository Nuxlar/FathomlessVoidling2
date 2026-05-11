using System.Collections.Generic;
using RoR2;
using RoR2.CharacterAI;
using UnityEngine;

namespace FathomlessVoidling.Controllers;

public class FathomlessSkillDriverController : MonoBehaviour
{
    private List<AISkillDriver> skillDrivers = [];
    private CharacterBody characterBody;

    private void Start()
    {
        this.characterBody = this.GetComponent<CharacterBody>();
        if (!this.characterBody) return;

        CharacterMaster master = this.characterBody.master;
        List<string> driverNames = ["WardWipe", "Vacuum Attack", "SpinBeam", "FireMissiles", "FireMultiBeam"];
        foreach (AISkillDriver driver in master.GetComponents<AISkillDriver>())
        {
            if (driverNames.Contains(driver.customName))
                this.skillDrivers.Add(driver);
        }
    }

    public void TriggerWardWipe()
    {
        foreach (AISkillDriver driver in this.skillDrivers)
            driver.enabled = driver.customName == "WardWipe";
    }

    public void EndWardWipe()
    {
        int itemCount = this.characterBody.inventory.GetItemCountEffective(RoR2Content.Items.MinHealthPercentage);
        foreach (AISkillDriver driver in this.skillDrivers)
        {
            if (driver.customName == "WardWipe")
                driver.enabled = false;
            else if (driver.customName == "SpinBeam")
                driver.enabled = itemCount == 5;
            else
                driver.enabled = true;
        }
    }

    public void EnableSingularity()
    {
        AISkillDriver singularityDriver = this.skillDrivers.Find(d => d.customName == "Vacuum Attack");
        if (singularityDriver)
            singularityDriver.enabled = true;
        else
            Debug.LogWarning("FathomlessVoidling: Singularity driver not present in FathomlessSkillDriverController");
    }

    public void EnableMaze()
    {
        AISkillDriver mazeDriver = this.skillDrivers.Find(d => d.customName == "SpinBeam");
        if (mazeDriver)
            mazeDriver.enabled = true;
        else
            Debug.LogWarning("FathomlessVoidling: Maze driver not present in FathomlessSkillDriverController");
    }

    public bool IsSingularityEnabled()
    {
        AISkillDriver singularityDriver = this.skillDrivers.Find(d => d.customName == "Vacuum Attack");
        return singularityDriver && singularityDriver.enabled;
    }

    public bool IsMazeEnabled()
    {
        AISkillDriver mazeDriver = this.skillDrivers.Find(d => d.customName == "SpinBeam");
        return mazeDriver && mazeDriver.enabled;
    }
}
