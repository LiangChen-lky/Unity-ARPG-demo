public class PlayerLightStoppingState : PlayerStoppingState
{
    public PlayerLightStoppingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        // TODO：Animancer 渐进迁移验证通过后删除旧 Animator 播放代码。
        // stateMachine.Player.Animator.CrossFadeInFixedTime(
        //     AnimationData.LightStoppingAnimationHash,
        //     AnimationData.FixedTransitionDuration);
        PlayStoppingAnimation(GroundedData.StopData.LightMotionData);

        stateMachine.ReusableData.CurrentJumpForce = AirborneData.JumpData.WeakForce;
    }
}
