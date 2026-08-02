public class PlayerHardStoppingState : PlayerStoppingState
{
    public PlayerHardStoppingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    #region IState Methods

    public override void Enter()
    {
        base.Enter();

        PlayStoppingAnimation(GroundedData.StopData.HardMotionData);

        stateMachine.ReusableData.CurrentJumpForce = AirborneData.JumpData.StrongForce;
    }

    #endregion
}
