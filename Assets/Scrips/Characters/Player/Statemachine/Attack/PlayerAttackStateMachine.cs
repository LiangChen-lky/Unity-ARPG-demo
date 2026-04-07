public class PlayerAttackStateMachine : StateMachine
{
    public PlayerCombo1 Combo1 { get; }
    public PlayerAttackState AttackState { get; }
    
    public PlayerAttackStateMachine(PlayerMovementStateMachine movementStateMachine)
    {
        Combo1 = new PlayerCombo1(movementStateMachine);
        AttackState = new PlayerAttackState(movementStateMachine);
    }
}
