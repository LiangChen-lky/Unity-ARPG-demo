using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStoppingState : PlayerGroundedState
{
    private readonly MotionDriver motionDriver;
    private int stoppingAnimationHash;

    public PlayerStoppingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
        motionDriver = new MotionDriver(stateMachine.Player.Rigidbody);
    }

    #region IState Methods

    public override void Enter()
    {
        base.Enter();

        stateMachine.ReusableData.MovementSpeedModifier = 0f;
    }

    public override void Exit()
    {
        motionDriver.Stop();
        stoppingAnimationHash = 0;
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        Animator animator = stateMachine.Player.Animator;
        AnimatorStateInfo stateInfo = animator.IsInTransition(0)
            ? animator.GetNextAnimatorStateInfo(0)
            : animator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.shortNameHash == stoppingAnimationHash)
        {
            motionDriver.SetNormalizedTime(stateInfo.normalizedTime);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        
        RotateTowardTargetRotation();

        motionDriver.PhysicsUpdate();
    }

    public override void OnAnimationTransitionEvent()
    {
        base.OnAnimationTransitionEvent();
        
        stateMachine.ChangeState(stateMachine.IdlingState);
    }

    public override void OnAnimationExitEvent()
    {
        base.OnAnimationExitEvent();

        motionDriver.Stop();
        ResetHorizontalVelocity();
    }

    #endregion
    
    #region Reusable Methods

    /// <summary>
    /// 三档停止状态在播放动画后调用，锁定入场水平速度方向并绑定对应的烘焙数据。
    /// </summary>
    protected void BeginStoppingMotion(
        AnimationMotionData motionData,
        int animationHash)
    {
        stoppingAnimationHash = animationHash;
        motionDriver.Begin(motionData, GetPlayerHorizontalVelocity());
    }

    protected override void OnMovementStarted(InputAction.CallbackContext context)
    {
        base.OnMovementStarted(context);
        
        OnMove();
    }

    protected override void AddInputActionCallbacks()
    {
        base.AddInputActionCallbacks();

        stateMachine.Player.Input.PlayerActions.Movement.started += OnMovementStarted;
    }

    protected override void RemoveInputActionCallbacks()
    {
        base.RemoveInputActionCallbacks();
        
        stateMachine.Player.Input.PlayerActions.Movement.started -= OnMovementStarted;
    }

    #endregion
}
