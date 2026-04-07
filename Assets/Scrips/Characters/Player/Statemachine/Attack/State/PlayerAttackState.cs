public class PlayerAttackState : PlayerGroundedState
{
    public PlayerAttackState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }
    
    public override void Enter()
    {
        base.Enter();

        stateMachine.ReusableData.MovementSpeedModifier = 0f;
        ResetVelocity();
        
        // stateMachine.Player.Animator.Play(AnimationData.Combo1AnimationName);
    }
}
