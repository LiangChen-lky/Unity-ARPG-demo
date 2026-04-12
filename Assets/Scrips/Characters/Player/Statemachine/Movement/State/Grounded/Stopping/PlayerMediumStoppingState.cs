public class PlayerMediumStoppingState : PlayerStoppingState
{
    public PlayerMediumStoppingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    #region IState Methods

    public override void Enter()
    {
        base.Enter();

        stateMachine.Player.Animator.Play(AnimationData.MediumStoppingAnimationHash);

        stateMachine.ReusableData.MovementDecelerationForce = GroundedData.StopData.MediumDecelerationForce;
        stateMachine.ReusableData.CurrentJumpForce = AirborneData.JumpData.MediumForce;
    }

    #endregion
}
