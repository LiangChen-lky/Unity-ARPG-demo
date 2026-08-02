using System;
using UnityEngine;

[Serializable]
public class PlayerStopData
{
    [Header("动画运动数据")]
    // 每档数据同时持有 ClipTransition 与烘焙曲线，保证播放动画和运动来源一致。
    [SerializeField] private AnimationMotionData lightMotionData = new AnimationMotionData();
    [SerializeField] private AnimationMotionData mediumMotionData = new AnimationMotionData();
    [SerializeField] private AnimationMotionData hardMotionData = new AnimationMotionData();

    public AnimationMotionData LightMotionData => lightMotionData;
    public AnimationMotionData MediumMotionData => mediumMotionData;
    public AnimationMotionData HardMotionData => hardMotionData;
}
