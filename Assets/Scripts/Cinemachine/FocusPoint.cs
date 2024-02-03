using UnityEngine;

[System.Serializable]
public class FocusPoint
{
    public Transform targetTransform;
    public float targetRadiusAdjustment;
    public float targetHeightAdjustment;

    public FocusPoint(Transform target, float radiusAdjustment, float heightAdjustment)
    {
        this.targetTransform = target;
        this.targetRadiusAdjustment = radiusAdjustment;
        this.targetHeightAdjustment = heightAdjustment;
    }
}