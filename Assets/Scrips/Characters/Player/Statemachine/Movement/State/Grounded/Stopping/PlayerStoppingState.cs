using UnityEngine;
using Animancer;
using UnityEngine.InputSystem;

public class PlayerStoppingState : PlayerGroundedState
{
    private readonly MotionDriver motionDriver;
    private AnimancerState stoppingAnimationState;

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
        stoppingAnimationState = null;
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        motionDriver.SetNormalizedTime(stoppingAnimationState.NormalizedTime);
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        RotateTowardTargetRotation();

        motionDriver.PhysicsUpdate();
    }

    // public override void OnAnimationTransitionEvent()
    // {
    //     base.OnAnimationTransitionEvent();

    //     stateMachine.ChangeState(stateMachine.IdlingState);
    // }

    // public override void OnAnimationExitEvent()
    // {
    //     base.OnAnimationExitEvent();

    //     motionDriver.Stop();
    //     ResetHorizontalVelocity();
    // }

    #endregion

    #region Main Methods
    private void OnStoppingAnimationEnded()
    {
        stateMachine.ChangeState(stateMachine.IdlingState);
    }
    #endregion

    #region Reusable Methods

    /// <summary>
    /// 播放三档停止动画，并让位移曲线与同一个 AnimancerState 的播放进度保持同步。
    /// </summary>
    protected void PlayStoppingAnimation(AnimationMotionData motionData)
    {
        stoppingAnimationState = stateMachine.Player.Animancer.Play(motionData.Animation);

        stoppingAnimationState.Events(this).OnEnd = OnStoppingAnimationEnded;

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
