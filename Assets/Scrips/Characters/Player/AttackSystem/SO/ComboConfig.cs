using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ComboConfig", menuName = "ScriptableObject/Combat/ComboConfig")]
public class ComboConfig : ScriptableObject
{
    [Header("基础数据")]
    public string ComboName;
    public float ColdTime;
    
    [Header("交互数据")]
    public ComboInteractionConfig[] InteractionConfig;
    
    [Header("攻击检测数据")]
    public AttackDetectionConfig[] AttackDetectionConfig;
    
    [Header("打击感数据")]
    public AttackFeedbackConfig[] AttackFeedbackConfig;
    
    [Header("特效数据")]
    public FXConfig[] FXConfig;
    
    [Header("音效数据")]
    public SFXConfig[] SFXConfig;
    
    [Header("自身位移补偿数据")]
    public MoveOffsetConfig SelfMoveOffsetConfig;
    
    [Header("目标位移补偿数据")]
    public MoveOffsetConfig[] TargetMoveOffsetConfig;
}

[Serializable]
public class ComboInteractionConfig
{
    public string HitName;
    public string Hit_AirName;
    //武器类型
    public Weapon Weapon;
    //攻击力度
    public AttackForce AttackForce;
    public float Damage;
}

[Serializable]
public class AttackDetectionConfig //攻击检测数据
{
    //触发时间点和碰撞盒信息
    public float StartTime;
    public Vector3 Position;
    public Vector3 Rotation;
    public Vector3 Scale;
}

[Serializable]
public class FXConfig //特效数据
{
    //触发时间点和特效信息
    public float StartTime;
    public GameObject FXObject;
    public Vector3 Position;
    public Vector3 Rotation;
    public Vector3 Scale;
}

[Serializable]
public class SFXConfig //音效数据
{
    //触发时间点和音效信息
    public float StartTime;
    public AudioClip SFXClip;
    public float Volume;
    public float Duration;
}

[Serializable]
public class AttackFeedbackConfig //打击感数据
{
    //屏幕震动
    public float Strength;
    public float Frequency; //震动频率
    public float Duration;
}

[Serializable]
public class MoveOffsetConfig //自身位移补偿数据
{
    public float StartTime;
    public AnimationCurve MoveCurve; //位移补偿曲线
    //方向
    public MoveOffsetDirection MoveOffsetDirection;
    public float Scale;
    public float Duration;
}