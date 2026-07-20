using UnityEngine;

public abstract class StateMachine
{
    protected IState currentState;

    public void HandleInput()
    {
        currentState?.HandleInput();
    }
    
    public void Update()
    {
        currentState?.Update();
    }

    public void PhysicsUpdate()
    {
        currentState?.PhysicsUpdate();
    }

    public void OnAnimationEnterEvent()
    {
        currentState?.OnAnimationEnterEvent();
    }

    public void OnAnimationExitEvent(AnimationEvent animationEvent)
    {
        currentState?.OnAnimationExitEnvent(animationEvent);
    }

    public void OnAnimationTransitionEvent()
    {
        currentState?.OnAnimationTransitionEvent();
    }
    
    public void ChangeState(IState newState)
    {
        if (newState != null)
        {
            currentState?.Exit();
            currentState = newState;
            currentState.Enter();
        }
    }
}
