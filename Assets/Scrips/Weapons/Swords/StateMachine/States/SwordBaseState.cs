using UnityEngine;
using UnityEngine.InputSystem;

public class SwordBaseState : IState
{
    protected SwordStateMachine stateMachine;
    
    public SwordBaseState(SwordStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
    }

    #region IState Methods
    public virtual void Enter()
    {
        // Debug.Log(GetType().Name);
        AddPlayerInputActionCallbacks();
    }

    public virtual void Exit()
    {
        RemovePlayerInputActionCallbacks();
    }

    public virtual void HandleInput()
    {
        
    }

    public virtual void Update()
    {
        
    }

    public virtual void PhysicsUpdate()
    {
        
    }

    public virtual void OnAnimationEnterEvent()
    {
        
    }

    public virtual void OnAnimationExitEnvent()
    {
        
    }

    public virtual void OnAnimationTransitionEvent()
    {
        
    }
    #endregion

    #region Reusable Methods

    protected virtual void AddPlayerInputActionCallbacks()
    {
        
    }

    protected virtual void RemovePlayerInputActionCallbacks()
    {
        
    }

    #endregion

    #region Input Methods

    
    
    

    #endregion
}
