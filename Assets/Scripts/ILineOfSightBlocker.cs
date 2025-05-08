using UnityEngine;

public interface ILineOfSightBlocker
{
    bool CheckLineOfSight(Vector3 fromPosition, Vector3 toPosition, bool hasThermalVision);
}