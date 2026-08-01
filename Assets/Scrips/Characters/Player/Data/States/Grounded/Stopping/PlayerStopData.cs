using System;
using UnityEngine;

[Serializable]
public class PlayerStopData
{
    [Header("动画运动数据")]
    // 三档停止动画分别持有烘焙结果，由 MotionDriver 按动画进度驱动水平速度。
    [SerializeField] private AnimationMotionData lightMotionData = new AnimationMotionData();
    [SerializeField] private AnimationMotionData mediumMotionData = new AnimationMotionData();
    [SerializeField] private AnimationMotionData hardMotionData = new AnimationMotionData();

    public AnimationMotionData LightMotionData => lightMotionData;
    public AnimationMotionData MediumMotionData => mediumMotionData;
    public AnimationMotionData HardMotionData => hardMotionData;
}
