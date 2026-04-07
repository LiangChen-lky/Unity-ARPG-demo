public class PlayerAirborneState : PlayerMovementState
{
    public PlayerAirborneState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    #region IState Methods

    protected override void OnContactWithGround()
    {
        stateMachine.ChangeState(stateMachine.IdlingState);
    }

    #endregion
}
