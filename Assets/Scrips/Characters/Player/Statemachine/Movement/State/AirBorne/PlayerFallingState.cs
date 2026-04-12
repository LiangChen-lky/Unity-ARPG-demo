public class PlayerFallingState : PlayerAirborneState
{
    public PlayerFallingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    #region IState Methods

    public override void Enter()
    {
        base.Enter();
        
        // stateMachine.Player.Animator.Play(AnimationData.GetAnimationHash(AnimationData.FallingAnimationName));
        stateMachine.Player.Animator.CrossFade(AnimationData.FallingAnimationHash,
            0.2f);
    }

    #endregion
}
