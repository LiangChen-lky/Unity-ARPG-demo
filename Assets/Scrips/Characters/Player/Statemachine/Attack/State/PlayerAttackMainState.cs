public class PlayerAttackMainState : IState
{
    public PlayerMovementStateMachine MovementStateMachine { get; }
    
    public PlayerAttackStateMachine attackStateMachine;

    public PlayerAttackMainState(PlayerMovementStateMachine movementStateMachine)
    {
        MovementStateMachine = movementStateMachine;
        
    }
    
    public void Enter()
    {
        
    }

    public void Exit()
    {
        
    }

    public void HandleInput()
    {
        
    }

    public void Update()
    {
        
    }

    public void PhysicsUpdate()
    {
        
    }

    public void OnAnimationEnterEvent()
    {
        
    }

    public void OnAnimationExitEnvent()
    {
        
    }

    public void OnAnimationTransitionEvent()
    {
        
    }
}
