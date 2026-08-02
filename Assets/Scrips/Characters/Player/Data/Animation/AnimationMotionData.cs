using System;
using Animancer;
using UnityEngine;

/// <summary>
/// 单个动画的位移数据，由离线烘焙器从 Root Motion 采样得到，用于按动画播放进度还原前向位移与转向。
/// 内嵌在 ComboConfig、PlayerSO 等配置资产中，对外只读，写入统一走 AnimationMotionBakeWriter。
/// </summary>
[Serializable]
public sealed class AnimationMotionData
{
    // 动画播放参数、事件时间与烘焙曲线归属于同一动画，避免出现两个 Clip 来源。
    [EventNames(typeof(PlayerAnimationEventNames))]
    [SerializeField] private ClipTransition animation = new ClipTransition();
    // 前向速度曲线：时间轴为秒（片段本地时间），值为角色本地 Z 轴有符号速度（米/秒），负值表示后退。
    [SerializeField] private AnimationCurve speedCurve = new AnimationCurve();
    // 累计偏航曲线：时间轴为秒，值为相对起始朝向的累计 Y 轴旋转（度），首值恒为 0，与 Y 轴欧拉角同向（右转为正）。
    [SerializeField] private AnimationCurve rotationCurve = new AnimationCurve();
    // 烘焙覆盖时长（秒），等于采样时的 Clip 长度，也是两条曲线时间轴的终点。
    [SerializeField, Min(0f)] private float bakedDuration;

    public ClipTransition Animation => animation;
    public AnimationClip Clip => animation.Clip;
    public AnimationCurve SpeedCurve => speedCurve;
    public AnimationCurve RotationCurve => rotationCurve;
    public float BakedDuration => bakedDuration;
}
