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

        stateMachine.ReusableData.CurrentJumpForce = AirborneData.JumpData.StrongForce;
    }

    #endregion
}
