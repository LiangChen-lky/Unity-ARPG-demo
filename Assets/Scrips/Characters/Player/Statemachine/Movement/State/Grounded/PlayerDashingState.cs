using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDashingState : PlayerGroundedState
{
    private enum DashDirection
    {
        Forward,
        Backward,
        Left,
        Right
    }

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

        Vector3 dashDirection = GetDashDirection();
        stateMachine.Player.Animator.CrossFade(
            GetDashingAnimationHash(dashDirection),
            0);
        
        SetRotationData(GroundedData.DashData.RotationData);
        
        Dash(dashDirection);

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

    private Vector3 GetDashDirection()
    {
        Vector2 movementInput = stateMachine.ReusableData.MovementInput;
        Vector3 dashDirection = stateMachine.Player.transform.forward;
        dashDirection.y = 0f;

        if (movementInput != Vector2.zero)
        {
            dashDirection = new Vector3(movementInput.x, 0f, movementInput.y);
        }

        return dashDirection;
    }

    private void Dash(Vector3 dashDirection)
    {
        if (consecutiveDashes >= GroundedData.DashData.ConsecutiveDashesLimitAmount)
        {
            return;
        }

        UpdateTargetRotation(dashDirection, false);

        stateMachine.Player.Rigidbody.linearVelocity = dashDirection * GetMovementSpeed();
    }

    private int GetDashingAnimationHash(Vector3 dashDirection)
    {
        DashDirection direction = GetDashDirectionRelativeToPlayer(dashDirection);
        int directionalHash = direction switch
        {
            DashDirection.Backward => AnimationData.DodgeBackwardAnimationHash,
            DashDirection.Left => AnimationData.DodgeLeftAnimationHash,
            DashDirection.Right => AnimationData.DodgeRightAnimationHash,
            _ => AnimationData.DodgeForwardAnimationHash
        };

        if (stateMachine.Player.Animator.HasState(0, directionalHash))
        {
            return directionalHash;
        }

        return AnimationData.DashingAnimationHash;
    }

    private DashDirection GetDashDirectionRelativeToPlayer(Vector3 dashDirection)
    {
        // 闪避方向必须基于角色触发闪避时的朝向判断，而不是基于世界坐标判断。
        Vector3 flatDashDirection = dashDirection;
        flatDashDirection.y = 0f;
        // 向量大小接近0
        if (flatDashDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            return DashDirection.Forward;
        }

        Vector3 flatForward = stateMachine.Player.transform.forward;
        flatForward.y = 0f;

        flatDashDirection.Normalize();
        flatForward.Normalize();

        // 点积分别表示闪避方向在角色前方轴和右方轴上的投影。
        Vector3 flatRight = Vector3.Cross(Vector3.up, flatForward);
        float forwardAlignment = Vector3.Dot(flatForward, flatDashDirection);
        float rightAlignment = Vector3.Dot(flatRight, flatDashDirection);

        // 比较两个投影的绝对值，将斜向输入归类到夹角更小的四方向动画。
        if (Mathf.Abs(forwardAlignment) >= Mathf.Abs(rightAlignment))
        {
            return forwardAlignment >= 0f ? DashDirection.Forward : DashDirection.Backward;
        }

        return rightAlignment >= 0f ? DashDirection.Right : DashDirection.Left;
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
