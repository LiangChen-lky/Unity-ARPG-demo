using System;
using Animancer;
using UnityEngine;

[Serializable]
public class PlayerLandingData
{
    [field: Header("动画")]
    [field: SerializeField] public ClipTransition LightAnimation { get; private set; }

    [field: SerializeField] public ClipTransition HardAnimation { get; private set; }
}
