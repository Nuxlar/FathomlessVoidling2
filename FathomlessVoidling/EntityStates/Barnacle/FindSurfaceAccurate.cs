using RoR2;
using System;
using UnityEngine;
using EntityStates.VoidBarnacle;

namespace FathomlessVoidling.EntityStates.Barnacle;

public class FindSurfaceAccurate : NoCastSpawn
{
    public override void OnEnter()
    {
        base.OnEnter();
        RaycastHit hitInfo = new RaycastHit();
        Vector3 origin = new Vector3(this.characterBody.corePosition.x, this.characterBody.corePosition.y + FindSurface.raycastSphereYOffset, this.characterBody.corePosition.z);
        if (!this.isAuthority)
            return;
        FindSurface.raycastMinimumAngle = Mathf.Clamp(FindSurface.raycastMinimumAngle, 0.0f, FindSurface.raycastMaximumAngle);
        FindSurface.raycastMaximumAngle = Mathf.Clamp(FindSurface.raycastMaximumAngle, FindSurface.raycastMinimumAngle, 90f);
        FindSurface.raycastCount = Mathf.Abs(FindSurface.raycastCount);
        float num = 360f / (float)FindSurface.raycastCount;
        for (int index = 0; index < FindSurface.raycastCount; ++index)
        {
            float f1 = UnityEngine.Random.Range(num * (float)index, (float)((double)num * (double)(index + 1) - 1.0)) * ((float)Math.PI / 180f);
            double f2 = (double)UnityEngine.Random.Range(FindSurface.raycastMinimumAngle, FindSurface.raycastMaximumAngle) * (Math.PI / 180.0);
            Vector3 direction = new Vector3(Mathf.Cos(f1), Mathf.Sin((float)f2), Mathf.Sin(f1));
            if (Physics.Raycast(origin, direction, out hitInfo, FindSurface.maxRaycastLength, (int)LayerIndex.CommonMasks.bullet))
            {
                this.transform.position = hitInfo.point;
                this.transform.up = hitInfo.normal;
                break;
            }
        }
    }
}
