using System;
using UnityEngine;

[Serializable]
public class CapsuleColliderUtility
{
    public CapsuleColliderData CapsuleColliderData { get; private set; }
    [field: SerializeField] public DefaultColliderData DefaultColliderData { get; private set; }
    [field: SerializeField] public SlopeData SlopeData { get; private set; }

    public void Initialize(GameObject gameObject)
    {
        if (CapsuleColliderData != null)
        {
            return;
        }

        CapsuleColliderData = new CapsuleColliderData();
        CapsuleColliderData.Initialize(gameObject);
    }

    public void CalculateCapsuleColliderDimensions()
    {
        SetCapsuleColliderRadius(DefaultColliderData.Radius);

        SetCapsuleColliderHeight((1 - SlopeData.StepHeightPercentage) * DefaultColliderData.Height);

        CalculateCapsuleColliderCenter();

        float halfHeight = CapsuleColliderData.Collider.height / 2f;
        if (CapsuleColliderData.Collider.radius > halfHeight)
        {
            SetCapsuleColliderRadius(halfHeight);
        }
        
        CapsuleColliderData.UpdateColliderData();
    }

    private void SetCapsuleColliderRadius(float radius)
    {
        CapsuleColliderData.Collider.radius = radius;
    }
    
    private void SetCapsuleColliderHeight(float height)
    {
        CapsuleColliderData.Collider.height = height;
    }
    
    private void CalculateCapsuleColliderCenter()
    {
        float differenceHeight = DefaultColliderData.Height - CapsuleColliderData.Collider.height;

        Vector3 newCenter = new Vector3(0f, DefaultColliderData.YCenter + differenceHeight / 2, 0f);
        CapsuleColliderData.Collider.center = newCenter;
    }
}
