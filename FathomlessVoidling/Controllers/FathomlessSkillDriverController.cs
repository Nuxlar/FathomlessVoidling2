using System.Collections.Generic;
using EntityStates;
using RoR2;
using RoR2.CharacterAI;
using UnityEngine;

namespace FathomlessVoidling.Controllers;

public class FathomlessSkillDriverController : MonoBehaviour
{
    private List<AISkillDriver> skillDrivers;
    private CharacterMaster master;

    private void Start()
    {
        CharacterBody body = this.GetComponent<CharacterBody>();
        if (body)
        {
            this.master = body.master;
            List<string> driverNames = new List<string>()
            {
                "WardWipe",
                "Vacuum Attack",
                "SpinBeam",
                "FireMissiles",
                "FireMultiBeam"
            };
            foreach (AISkillDriver driver in this.master.GetComponents<AISkillDriver>())
            {
                if (driverNames.Contains(driver.customName))
                    this.skillDrivers.Add(driver);
            }
        }
    }

    public void TriggerWardWipe()
    {
        foreach (AISkillDriver driver in this.skillDrivers)
        {
            if (driver.customName == "WardWipe")
                driver.enabled = true;
            else
                driver.enabled = false;
        }
    }

    public void EndWardWipe()
    {
        foreach (AISkillDriver driver in this.skillDrivers)
        {
            if (driver.customName == "WardWipe")
                driver.enabled = false;
            else
                driver.enabled = true;
        }
    }

    public void EnableSingularity()
    {
        AISkillDriver singularityDriver = this.skillDrivers.Find((driver) => driver.customName == "Vacuum Attack");
        if (singularityDriver)
            singularityDriver.enabled = true;
        else
            Debug.LogWarning("FathomlessVoidling: Singularity driver not present in FathomlessSkillDriverController");
    }

    public void EnableMaze()
    {
        AISkillDriver mazeDriver = this.skillDrivers.Find((driver) => driver.customName == "SpinBeam");
        if (mazeDriver)
            mazeDriver.enabled = true;
        else
            Debug.LogWarning("FathomlessVoidling: Maze driver not present in FathomlessSkillDriverController");
    }
}