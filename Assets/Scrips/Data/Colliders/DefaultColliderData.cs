using System;
using UnityEngine;

[Serializable]
public class DefaultColliderData
{
    [field: SerializeField] public float Height { get; private set; } = 1.6f;
    [field: SerializeField] public float YCenter { get; private set; } = 0.8f;
    [field: SerializeField] public float Radius { get; private set; } = 0.2f;
}
