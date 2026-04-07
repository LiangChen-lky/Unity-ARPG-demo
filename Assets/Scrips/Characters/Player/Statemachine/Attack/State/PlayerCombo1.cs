public class PlayerCombo1 : PlayerAttackState
{
    public PlayerCombo1(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    #region IState Methods

    public override void Enter()
    {
        base.Enter();

        stateMachine.ReusableData.MovementSpeedModifier = 0f;
        ResetVelocity();
        
        // stateMachine.Player.Animator.Play(AnimationData.Combo1AnimationName);
    }

    #endregion
}
