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
            AnimationData.TransitionDuration);

        stateMachine.ReusableData.MovementDecelerationForce = GroundedData.StopData.HardDecelerationForce;
        stateMachine.ReusableData.CurrentJumpForce = AirborneData.JumpData.StrongForce;
    }

    #endregion
}
