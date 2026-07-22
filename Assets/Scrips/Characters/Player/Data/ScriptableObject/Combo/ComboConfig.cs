using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ComboConfig", menuName = "ScriptableObject/Combat/ComboConfig")]
public class ComboConfig : ScriptableObject
{
    [Header("基础数据")]
    public string ComboName;

    [Header("命中交互数据")]
    public ComboInteractionConfig[] InteractionConfig;

    [Header("攻击检测数据")]
    public AttackDetectionConfig[] AttackDetectionConfig;

    [Header("打击感数据")]
    public AttackFeedbackConfig[] AttackFeedbackConfig;

    [Header("特效数据")]
    public FXConfig[] FXConfig;

    [Header("音效数据")]
    public SFXConfig[] SFXConfig;

}

[Serializable]
public class ComboInteractionConfig
{
    // 攻击方只传递命中力度与伤害，目标表现由受击方自行决定。
    public AttackForce AttackForce;
    public float Damage;
}

[Serializable]
public class AttackDetectionConfig
{
    // 触发时间点和碰撞盒信息。
    public float StartTime;
    public Vector3 Position;
    public Vector3 Rotation;
    public Vector3 Scale;
}

[Serializable]
public class FXConfig
{
    // 触发时间点和特效信息。
    public float StartTime;
    public GameObject FXObject;
    public Vector3 Position;
    public Vector3 Rotation;
    public Vector3 Scale;
}

[Serializable]
public class SFXConfig
{
    // 触发时间点和音效信息。
    public float StartTime;
    public AudioClip SFXClip;
    public float Volume;
    public float Duration;
}

[Serializable]
public class AttackFeedbackConfig
{
    // 屏幕震动参数。
    public float Strength;
    public float Frequency;
    public float Duration;
}
