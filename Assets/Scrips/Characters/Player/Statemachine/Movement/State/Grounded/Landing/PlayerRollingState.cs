using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerRollingState : PlayerLandingState
{
    private readonly PlayerRollData rollData;

    public PlayerRollingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
        rollData = GroundedData.RollData;
    }

    public override void Enter()
    {
        base.Enter();

        stateMachine.ReusableData.MovementSpeedModifier = rollData.SpeedModifier;
        stateMachine.Player.Animator.CrossFade(AnimationData.RollingAnimationHash, AnimationData.TransitionDuration);
        stateMachine.ReusableData.ShouldSprint = false;
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        if (stateMachine.ReusableData.MovementInput != Vector2.zero)
        {
            return;
        }

        RotateTowardTargetRotation();
    }

    public override void OnAnimationTransitionEvent()
    {
        base.OnAnimationTransitionEvent();

        if (stateMachine.ReusableData.MovementInput == Vector2.zero)
        {
            stateMachine.ChangeState(stateMachine.MediumStoppingState);
            return;
        }

        OnMove();
    }

    protected override void OnJumpStarted(InputAction.CallbackContext context)
    {
    }
}
