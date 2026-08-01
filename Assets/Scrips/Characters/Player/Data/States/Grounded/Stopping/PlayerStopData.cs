using System;
using UnityEngine;

[Serializable]
public class PlayerStopData
{
    // TODO：MotionDriver PlayMode 验证通过后连同 Player.asset 中的旧序列化数据一起删除。
    [Obsolete("已由 AnimationMotionData.SpeedCurve 替代，完成 MotionDriver 验证后删除。")]
    [field: SerializeField]
    [field: Range(0f, 15f)]
    public float LightDecelerationForce { get; private set; } = 5f;

    [Obsolete("已由 AnimationMotionData.SpeedCurve 替代，完成 MotionDriver 验证后删除。")]
    [field: SerializeField]
    [field: Range(0f, 15f)]
    public float MediumDecelerationForce { get; private set; } = 6.5f;

    [Obsolete("已由 AnimationMotionData.SpeedCurve 替代，完成 MotionDriver 验证后删除。")]
    [field: SerializeField]
    [field: Range(0f, 15f)]
    public float HardDecelerationForce { get; private set; } = 5f;

    [Header("动画运动数据")]
    // 三档停止动画分别持有烘焙结果，旧减速度字段保留到 MotionDriver 接入完成。
    [SerializeField] private AnimationMotionData lightMotionData = new AnimationMotionData();
    [SerializeField] private AnimationMotionData mediumMotionData = new AnimationMotionData();
    [SerializeField] private AnimationMotionData hardMotionData = new AnimationMotionData();

    public AnimationMotionData LightMotionData => lightMotionData;
    public AnimationMotionData MediumMotionData => mediumMotionData;
    public AnimationMotionData HardMotionData => hardMotionData;
}
