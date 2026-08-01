using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHardLandingState : PlayerLandingState
{
    public PlayerHardLandingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        stateMachine.ReusableData.MovementSpeedModifier = 0f;
        stateMachine.Player.Animator.CrossFade(
            AnimationData.HardLandingAnimationHash,
            AnimationData.NormalizedTransitionDuration);
        stateMachine.Player.Input.PlayerActions.Movement.Disable();

        ResetVelocity();
    }

    public override void Exit()
    {
        base.Exit();

        stateMachine.Player.Input.PlayerActions.Movement.Enable();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        if (!IsMovingHorizontally())
        {
            return;
        }

        ResetVelocity();
    }

    public override void OnAnimationExitEvent()
    {
        stateMachine.Player.Input.PlayerActions.Movement.Enable();
    }

    public override void OnAnimationTransitionEvent()
    {
        base.OnAnimationTransitionEvent();

        stateMachine.ChangeState(stateMachine.IdlingState);
    }

    protected override void AddInputActionCallbacks()
    {
        base.AddInputActionCallbacks();

        stateMachine.Player.Input.PlayerActions.Movement.started += OnHardLandingMovementStarted;
    }

    protected override void RemoveInputActionCallbacks()
    {
        base.RemoveInputActionCallbacks();

        stateMachine.Player.Input.PlayerActions.Movement.started -= OnHardLandingMovementStarted;
    }

    protected override void OnMove()
    {
        if (stateMachine.ReusableData.ShouldWalk)
        {
            return;
        }

        stateMachine.ChangeState(stateMachine.RunningState);
    }

    protected override void OnJumpStarted(InputAction.CallbackContext context)
    {
    }

    private void OnHardLandingMovementStarted(InputAction.CallbackContext context)
    {
        OnMove();
    }
}
