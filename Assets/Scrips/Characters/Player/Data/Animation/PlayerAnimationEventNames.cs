using Animancer;

/// <summary>
/// 统一定义玩家动画事件名称，供 ClipTransition 配置与运行时事件注册共同使用。
/// </summary>
public static class PlayerAnimationEventNames
{
    public static readonly StringReference Enter = "MovementAnimationEnter";
    public static readonly StringReference Exit = "MovementAnimationExit";
    public static readonly StringReference Transition = "MovementAnimationTransition";
}
