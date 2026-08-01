using System.Collections.Generic;

/// <summary>
/// 玩家离散动作输入缓冲器，记录每种动作最后一次输入时刻。
/// 不引用 Unity 输入系统、动画组件、招式配置和状态机，保持纯数据服务职责；
/// 时刻由调用方传入，因此可以在编辑器测试中用固定时间完整覆盖。
/// </summary>
public class PlayerActionBuffer
{
    private readonly Dictionary<PlayerActionType, float> recordedTimes = new();

    /// <summary>
    /// 记录一次动作输入；同类型已存在时直接覆盖为新时刻。
    /// </summary>
    public void Record(PlayerActionType actionType, float recordedTime)
    {
        recordedTimes[actionType] = recordedTime;
    }

    /// <summary>
    /// 尝试消费指定动作的记录；只有在有效期内才消费成功并删除记录。
    /// </summary>
    /// <returns>记录存在且未过期时返回 true 并删除记录；不存在或已过期返回 false。</returns>
    public bool TryConsume(PlayerActionType actionType, float currentTime, float validDuration)
    {
        if (!recordedTimes.TryGetValue(actionType, out float recordedTime))
        {
            return false;
        }

        if (currentTime - recordedTime > validDuration)
        {
            recordedTimes.Remove(actionType);
            return false;
        }

        recordedTimes.Remove(actionType);
        return true;
    }

    /// <summary>
    /// 清理指定动作的记录。
    /// </summary>
    public void Clear(PlayerActionType actionType)
    {
        recordedTimes.Remove(actionType);
    }

    /// <summary>
    /// 清空所有记录；预留给玩家死亡、禁用或完整重置场景。
    /// </summary>
    public void ClearAll()
    {
        recordedTimes.Clear();
    }
}
