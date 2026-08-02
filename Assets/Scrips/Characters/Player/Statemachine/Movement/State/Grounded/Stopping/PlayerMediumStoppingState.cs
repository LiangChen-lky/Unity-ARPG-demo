public class PlayerMediumStoppingState : PlayerStoppingState
{
    public PlayerMediumStoppingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    #region IState Methods

    public override void Enter()
    {
        base.Enter();

        PlayStoppingAnimation(GroundedData.StopData.MediumMotionData);

        stateMachine.ReusableData.CurrentJumpForce = AirborneData.JumpData.WeakForce;
    }

    #endregion
}
