using UnityEngine.InputSystem;

public class PlayerWalkingState : PlayerMovingState
{
    public PlayerWalkingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        stateMachine.ReusableData.MovementSpeedModifier = GroundedData.WalkData.SpeedModifier;
        stateMachine.ReusableData.CurrentJumpForce = AirborneData.JumpData.WeakForce;
        stateMachine.Player.Animator.speed = GroundedData.WalkData.AnimationSpeedModifier;
        stateMachine.Player.Animator.CrossFade(AnimationData.WalkingAnimationHash, AnimationData.TransitionDuration);
    }

    public override void Exit()
    {
        stateMachine.Player.Animator.speed = 1f;

        base.Exit();
    }

    protected override void OnMovementCanceled(InputAction.CallbackContext context)
    {
        base.OnMovementCanceled(context);

        stateMachine.ChangeState(stateMachine.LightStoppingState);
    }

    protected override void OnWalkToggleStarted(InputAction.CallbackContext context)
    {
        base.OnWalkToggleStarted(context);

        stateMachine.ChangeState(stateMachine.RunningState);
    }
}
