using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDashingState : PlayerGroundedState
{
    private float startTime;
    private int consecutiveDashes;
    private bool shouldKeepRotate;
    
    public PlayerDashingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
        consecutiveDashes = 0;
    }

    #region IState Methods

    public override void Enter()
    {
        base.Enter();

        stateMachine.ReusableData.MovementSpeedModifier = GroundedData.DashData.SpeedModifier;
        stateMachine.ReusableData.CurrentJumpForce = AirborneData.JumpData.StrongForce;
        stateMachine.Player.Animator.CrossFade(AnimationData.DashingAnimationHash,
            0);
        
        SetRotationData(GroundedData.DashData.RotationData);
        
        Dash();

        shouldKeepRotate = stateMachine.ReusableData.MovementInput != Vector2.zero;

        UpdateConsecutiveDashes();
        startTime = Time.time;
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        if (!shouldKeepRotate)
        {
            return;
        }

        RotateTowardTargetRotation();
    }

    public override void Exit()
    {
        base.Exit();
        
        SetBaseRotationData();
    }

    public override void OnAnimationTransitionEvent()
    {
        base.OnAnimationTransitionEvent();
        

        if (stateMachine.ReusableData.MovementInput == Vector2.zero)
        {
            stateMachine.ChangeState(stateMachine.HardStoppingState);
            return;
        }
        stateMachine.ChangeState(stateMachine.SprintingState);
    }

    #endregion

    #region Main Methods

    private void Dash()
    {
        if (consecutiveDashes >= GroundedData.DashData.ConsecutiveDashesLimitAmount)
        {
            return;
        }
        Vector2 movementInput = stateMachine.ReusableData.MovementInput; 
        
        Vector3 dashDirection = stateMachine.Player.transform.forward;
        dashDirection.y = 0f;
        if (movementInput != Vector2.zero)
        {
            dashDirection = new Vector3(movementInput.x, 0f, movementInput.y);
        }

        UpdateTargetRotation(dashDirection, false);

        stateMachine.Player.Rigidbody.linearVelocity = dashDirection * GetMovementSpeed();
    }

    private void UpdateConsecutiveDashes()
    {
        if (!IsConsecutive())
        {
            consecutiveDashes = 0;
        }

        consecutiveDashes++;

        if (consecutiveDashes >= GroundedData.DashData.ConsecutiveDashesLimitAmount)
        {
            consecutiveDashes = 0;

            stateMachine.Player.Input.DisableActionFor(stateMachine.Player.Input.PlayerActions.Dash,
                GroundedData.DashData.DashLimitReachedCooldown);
        }
    }

    private bool IsConsecutive()
    {
        return Time.time - startTime < GroundedData.DashData.TimeToBeConsideredConsecutive;
    }

    #endregion

    #region Reusable Methods

    protected override void AddInputActionCallbacks()
    {
        base.AddInputActionCallbacks();

        stateMachine.Player.Input.PlayerActions.Movement.performed += OnMovementPerformed;
    }

    protected override void RemoveInputActionCallbacks()
    {
        base.RemoveInputActionCallbacks();
        
        stateMachine.Player.Input.PlayerActions.Movement.performed -= OnMovementPerformed;
    }

    #endregion
    
    #region Input Methods

    protected override void OnDashStarted(InputAction.CallbackContext context)
    {
        
    }
    
    private void OnMovementPerformed(InputAction.CallbackContext context)
    {
        shouldKeepRotate = true;
    }

    #endregion
    
}
