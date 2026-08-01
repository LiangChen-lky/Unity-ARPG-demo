using UnityEngine;

/// <summary>
/// 根据动画运动曲线驱动 Rigidbody 的水平速度。
/// 状态负责提供动画进度与运动方向，本类不参与 Animator、输入和状态切换。
/// </summary>
public sealed class MotionDriver
{
    private readonly Rigidbody rigidbody;

    private AnimationMotionData motionData;
    private Vector3 motionDirection;
    private float normalizedTime;
    private bool isActive;
    private bool hasNormalizedTime;

    public MotionDriver(Rigidbody rigidbody)
    {
        this.rigidbody = rigidbody;
    }

    /// <summary>
    /// 开始一段动画运动并锁定水平运动方向；等待状态同步首个有效动画进度后才写入速度。
    /// </summary>
    public void Begin(AnimationMotionData data, Vector3 direction)
    {
        motionData = data;
        direction.y = 0f;
        motionDirection = direction.normalized;
        normalizedTime = 0f;
        hasNormalizedTime = false;
        isActive = true;
    }

    /// <summary>
    /// 缓存状态从 Animator 读取的归一化进度，实际 Rigidbody 写入留到固定物理帧。
    /// </summary>
    public void SetNormalizedTime(float time)
    {
        normalizedTime = time;
        hasNormalizedTime = true;
    }

    /// <summary>
    /// 按烘焙时长把归一化进度换算为曲线秒数，只替换水平速度并保留现有竖直速度。
    /// </summary>
    public void PhysicsUpdate()
    {
        if (!isActive || !hasNormalizedTime)
        {
            return;
        }

        float sampleTime = Mathf.Clamp01(normalizedTime) * motionData.BakedDuration;
        float speed = motionData.SpeedCurve.Evaluate(sampleTime);
        Vector3 velocity = rigidbody.linearVelocity;
        velocity.x = motionDirection.x * speed;
        velocity.z = motionDirection.z * speed;
        rigidbody.linearVelocity = velocity;
    }

    /// <summary>
    /// 停止继续写入速度，不主动清空 Rigidbody，后续状态自行接管当前运动。
    /// </summary>
    public void Stop()
    {
        motionData = null;
        hasNormalizedTime = false;
        isActive = false;
    }
}
