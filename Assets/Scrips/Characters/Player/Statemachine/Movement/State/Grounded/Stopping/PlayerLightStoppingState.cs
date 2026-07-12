public class PlayerLightStoppingState : PlayerStoppingState
{
    public PlayerLightStoppingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        stateMachine.Player.Animator.CrossFadeInFixedTime(
            AnimationData.LightStoppingAnimationHash,
            AnimationData.FixedTransitionDuration);
        stateMachine.ReusableData.MovementDecelerationForce = GroundedData.StopData.LightDecelerationForce;
        stateMachine.ReusableData.CurrentJumpForce = AirborneData.JumpData.WeakForce;
    }
}
