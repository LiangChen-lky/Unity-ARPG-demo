using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSprintingState : PlayerMovingState
{
    private float startTime;
    private bool keepSprint;
    private bool shouldResetSprintState;
    
    public PlayerSprintingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    #region IState Methods

    public override void Enter()
    {
        base.Enter();

        // TODO：Animancer 渐进迁移验证通过后删除旧 Animator 播放代码。
        // stateMachine.Player.Animator.Play(
        //     AnimationData.SprintingAnimationHash);

        stateMachine.Player.Animancer.Play(
            GroundedData.SprintData.Animation);

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

    public override void Exit()
    {
        base.Exit();
        
        if (shouldResetSprintState)
        {
            keepSprint = false;
            stateMachine.ReusableData.ShouldSprint = false;
        }
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

    protected override void OnFalling()
    {
        shouldResetSprintState = false;
        
        base.OnFalling();
    }

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
        
        stateMachine.ReusableData.ShouldSprint = true;
    }

    protected override void OnJumpStarted(InputAction.CallbackContext context)
    {
        shouldResetSprintState = false;
        
        base.OnJumpStarted(context);
    }

    #endregion
}
