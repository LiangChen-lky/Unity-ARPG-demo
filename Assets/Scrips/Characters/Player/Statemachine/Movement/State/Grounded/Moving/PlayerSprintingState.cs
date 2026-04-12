using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSprintingState : PlayerMovingState
{
    private float startTime;
    private bool keepSprint;
    
    public PlayerSprintingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    #region IState Methods

    public override void Enter()
    {
        base.Enter();

        stateMachine.Player.Animator.Play(AnimationData.SprintingAnimationHash);
        stateMachine.ReusableData.MovementSpeedModifier = GroundedData.SprintData.SpeedModifier;
        stateMachine.ReusableData.CurrentJumpForce = AirborneData.JumpData.StrongForce;

        startTime = Time.time;
    }

    public override void Update()
    {
        base.Update();

        if (keepSprint)
        {
            return;
        }

        if (Time.time - startTime < GroundedData.SprintData.SprintToRunTime)
        {
            return;
        }

        StopSprint();
    }

    #endregion

    #region Main Methods

    private void StopSprint()
    {
        if (stateMachine.ReusableData.MovementInput == Vector2.zero)
        {
            stateMachine.ChangeState(stateMachine.HardStoppingState);
            return;
        }
        stateMachine.ChangeState(stateMachine.RunningState);
    }

    #endregion
    
    #region Reusable Methods

    protected override void AddInputActionCallbacks()
    {
        base.AddInputActionCallbacks();
        
        stateMachine.Player.Input.PlayerActions.Sprint.performed += OnSprintPerformed;
    }

    protected override void RemoveInputActionCallbacks()
    {
        base.RemoveInputActionCallbacks();

        stateMachine.Player.Input.PlayerActions.Sprint.performed -= OnSprintPerformed;
    }

    #endregion
    
    #region Input Methods

    protected override void OnMovementCanceled(InputAction.CallbackContext context)
    {
        base.OnMovementCanceled(context);
        
        stateMachine.ChangeState(stateMachine.HardStoppingState);
    }

    private void OnSprintPerformed(InputAction.CallbackContext obj)
    {
        keepSprint = true;
    }
    #endregion
}
