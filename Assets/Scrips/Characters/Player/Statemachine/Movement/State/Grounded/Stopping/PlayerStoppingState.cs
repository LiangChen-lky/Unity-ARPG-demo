using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStoppingState : PlayerGroundedState
{
    public PlayerStoppingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    #region IState Methods

    public override void Enter()
    {
        base.Enter();

        stateMachine.ReusableData.MovementSpeedModifier = 0f;
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        
        RotateTowardTargetRotation();

        if (!IsMovingHorizontally())
        {
            return;
        }
        
        DecelerateHorizontally();
    }

    public override void OnAnimationTransitionEvent()
    {
        base.OnAnimationTransitionEvent();
        
        stateMachine.ChangeState(stateMachine.IdlingState);
    }

    public override void OnAnimationExitEnvent()
    {
        base.OnAnimationExitEnvent();

        ResetHorizontalVelocity();
    }

    #endregion
    
    #region Reusable Methods

    protected override void OnMovementStarted(InputAction.CallbackContext context)
    {
        base.OnMovementStarted(context);
        
        OnMove();
    }

    protected override void AddInputActionCallbacks()
    {
        base.AddInputActionCallbacks();

        stateMachine.Player.Input.PlayerActions.Movement.started += OnMovementStarted;
    }

    protected override void RemoveInputActionCallbacks()
    {
        base.RemoveInputActionCallbacks();
        
        stateMachine.Player.Input.PlayerActions.Movement.started -= OnMovementStarted;
    }

    #endregion
}
