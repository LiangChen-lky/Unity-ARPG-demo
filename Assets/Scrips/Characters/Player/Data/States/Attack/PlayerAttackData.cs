using System;
using UnityEngine;

[Serializable]
public class PlayerAttackData
{
    [field: SerializeField] public ComboList CurrentComboList { get; private set; }
    [field: SerializeField] public LayerMask TargetLayer { get; private set; }
    [field: SerializeField] public HitFXConfig[] HitFXList { get; private set; }
    [field: SerializeField] public Transform[] FXPositionList { get; private set; }
}