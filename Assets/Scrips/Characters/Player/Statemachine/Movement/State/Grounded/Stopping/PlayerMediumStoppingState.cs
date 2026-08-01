public class PlayerMediumStoppingState : PlayerStoppingState
{
    public PlayerMediumStoppingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    #region IState Methods

    public override void Enter()
    {
        base.Enter();

        stateMachine.Player.Animator.CrossFadeInFixedTime(
            AnimationData.MediumStoppingAnimationHash,
            AnimationData.FixedTransitionDuration);

        BeginStoppingMotion(
            GroundedData.StopData.MediumMotionData,
            AnimationData.MediumStoppingAnimationHash);

        // TODO：MotionDriver PlayMode 验证通过后删除旧减速度配置。
        // stateMachine.ReusableData.MovementDecelerationForce =
        //     GroundedData.StopData.MediumDecelerationForce;
        stateMachine.ReusableData.CurrentJumpForce = AirborneData.JumpData.WeakForce;
    }

    #endregion
}
