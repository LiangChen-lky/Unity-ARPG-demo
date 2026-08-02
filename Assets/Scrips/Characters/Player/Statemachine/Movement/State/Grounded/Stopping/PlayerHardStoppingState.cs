public class PlayerHardStoppingState : PlayerStoppingState
{
    public PlayerHardStoppingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    #region IState Methods

    public override void Enter()
    {
        base.Enter();
        
        // TODO：Animancer 渐进迁移验证通过后删除旧 Animator 播放代码。
        // stateMachine.Player.Animator.CrossFadeInFixedTime(
        //     AnimationData.HardStoppingAnimationHash,
        //     AnimationData.FixedTransitionDuration);

        PlayStoppingAnimation(GroundedData.StopData.HardMotionData);

        stateMachine.ReusableData.CurrentJumpForce = AirborneData.JumpData.StrongForce;
    }

    #endregion
}
