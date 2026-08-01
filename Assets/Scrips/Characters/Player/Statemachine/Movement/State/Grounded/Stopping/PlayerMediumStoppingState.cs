public class PlayerMediumStoppingState : PlayerStoppingState
{
    public PlayerMediumStoppingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    #region IState Methods

    public override void Enter()
    {
        base.Enter();

        stateMachine.Player.Animator.CrossFadeInFixedTime(
            AnimationData.MediumStoppingAnimationHash,
            AnimationData.FixedTransitionDuration);

        BeginStoppingMotion(
            GroundedData.StopData.MediumMotionData,
            AnimationData.MediumStoppingAnimationHash);

        stateMachine.ReusableData.CurrentJumpForce = AirborneData.JumpData.WeakForce;
    }

    #endregion
}
