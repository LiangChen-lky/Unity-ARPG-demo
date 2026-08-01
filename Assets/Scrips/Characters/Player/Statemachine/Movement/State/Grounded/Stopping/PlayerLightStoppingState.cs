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
        BeginStoppingMotion(
            GroundedData.StopData.LightMotionData,
            AnimationData.LightStoppingAnimationHash);

        stateMachine.ReusableData.CurrentJumpForce = AirborneData.JumpData.WeakForce;
    }
}
