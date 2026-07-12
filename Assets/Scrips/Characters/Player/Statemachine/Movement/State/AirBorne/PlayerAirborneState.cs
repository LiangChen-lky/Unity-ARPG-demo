using UnityEngine.InputSystem;

public class PlayerAirborneState : PlayerMovementState
{
    public PlayerAirborneState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    #region IState Methods

    public override void Enter()
    {
        base.Enter();
    }

    #endregion

    #region Reusable Methods

    protected override void OnAttackStarted(InputAction.CallbackContext context)
    {
    }

    protected override void OnContactWithGround()
    {
        stateMachine.ChangeState(stateMachine.IdlingState);
    }

    #endregion
}
