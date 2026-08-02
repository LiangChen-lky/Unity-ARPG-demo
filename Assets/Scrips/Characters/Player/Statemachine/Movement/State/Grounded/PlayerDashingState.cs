using Animancer;
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
    private AnimancerState dashingAnimationState;
    
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

        PlayDashingAnimation(dashDirection);
        
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
        // Dash 被其他状态提前打断时，旧动画不得在淡出结束后再次切换 HFSM。
        dashingAnimationState.Events(this).OnEnd = null;
        dashingAnimationState = null;

        base.Exit();
        
        SetBaseRotationData();
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

    private void PlayDashingAnimation(Vector3 dashDirection)
    {
        ClipTransition transition = GetDashingAnimation(dashDirection);

        // Dash 是可重复触发的一次性动作，每次进入状态都必须从头播放。
        dashingAnimationState = stateMachine.Player.Animancer.Play(
            transition,
            transition.FadeDuration,
            FadeMode.FromStart);
        dashingAnimationState.Events(this).OnEnd = OnDashingAnimationEnded;
    }

    private ClipTransition GetDashingAnimation(Vector3 dashDirection)
    {
        DashDirection direction = GetDashDirectionRelativeToPlayer(dashDirection);
        return direction switch
        {
            DashDirection.Backward => GroundedData.DashData.BackwardAnimation,
            DashDirection.Left => GroundedData.DashData.LeftAnimation,
            DashDirection.Right => GroundedData.DashData.RightAnimation,
            _ => GroundedData.DashData.ForwardAnimation
        };
    }

    private void OnDashingAnimationEnded()
    {
        if (stateMachine.ReusableData.MovementInput == Vector2.zero)
        {
            stateMachine.ChangeState(stateMachine.HardStoppingState);
            return;
        }

        stateMachine.ChangeState(stateMachine.SprintingState);
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
