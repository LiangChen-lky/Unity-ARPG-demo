using System;
using Animancer;
using UnityEngine;

[Serializable]
public class PlayerIdleData
{
    [field: SerializeField] public ClipTransition Animation { get; private set; }
}
