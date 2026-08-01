public class PlayerHardStoppingState : PlayerStoppingState
{
    public PlayerHardStoppingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    #region IState Methods

    public override void Enter()
    {
        base.Enter();
        
        stateMachine.Player.Animator.CrossFadeInFixedTime(
            AnimationData.HardStoppingAnimationHash,
            AnimationData.FixedTransitionDuration);

        BeginStoppingMotion(
            GroundedData.StopData.HardMotionData,
            AnimationData.HardStoppingAnimationHash);

        // TODO：MotionDriver PlayMode 验证通过后删除旧减速度配置。
        // stateMachine.ReusableData.MovementDecelerationForce =
        //     GroundedData.StopData.HardDecelerationForce;
        stateMachine.ReusableData.CurrentJumpForce = AirborneData.JumpData.StrongForce;
    }

    #endregion
}
