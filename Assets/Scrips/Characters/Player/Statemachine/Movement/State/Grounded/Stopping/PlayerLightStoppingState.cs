public class PlayerLightStoppingState : PlayerStoppingState
{
    public PlayerLightStoppingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        stateMachine.Player.Animator.CrossFadeInFixedTime(
            AnimationData.LightStoppingAnimationHash,
            AnimationData.FixedTransitionDuration);
        BeginStoppingMotion(
            GroundedData.StopData.LightMotionData,
            AnimationData.LightStoppingAnimationHash);

        // TODO：MotionDriver PlayMode 验证通过后删除旧减速度配置。
        // stateMachine.ReusableData.MovementDecelerationForce =
        //     GroundedData.StopData.LightDecelerationForce;
        stateMachine.ReusableData.CurrentJumpForce = AirborneData.JumpData.WeakForce;
    }
}
