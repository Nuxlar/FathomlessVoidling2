using RoR2;
using EntityStates;
using FathomlessVoidling.Controllers;

namespace FathomlessVoidling.EntityStates.Mission;

public class FathomlessEncounterBaseState : EntityState
{
    protected FathomlessMissionController fathomlessMissionController
    {
        get => FathomlessMissionController.instance;
    }
}
