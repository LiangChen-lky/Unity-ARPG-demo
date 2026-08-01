using System;
using UnityEngine;

[Serializable]
public class PlayerAttackData
{
    [field: SerializeField] public ComboList CurrentComboList { get; private set; }
    [field: SerializeField] public LayerMask TargetLayer { get; private set; }

    // 攻击预输入的有效时长；连招窗口开启前按下的攻击超过该时长即失效。
    [field: SerializeField, Min(0.01f)] public float AttackBufferDuration { get; private set; } = 0.2f;
}
