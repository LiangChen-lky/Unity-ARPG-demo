using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHardLandingState : PlayerLandingState
{
    public PlayerHardLandingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        stateMachine.ReusableData.MovementSpeedModifier = 0f;
        // TODO：HardLand 的 Animancer 迁移验证通过后删除旧 Animator 播放代码。
        // stateMachine.Player.Animator.CrossFade(
        //     AnimationData.HardLandingAnimationHash,
        //     AnimationData.NormalizedTransitionDuration);
        PlayLandingAnimation(GroundedData.LandingData.HardAnimation, OnHardLandingAnimationEnded);
        stateMachine.Player.Input.PlayerActions.Movement.Disable();

        ResetVelocity();
    }

    public override void Exit()
    {
        base.Exit();

        // 整个状态期间移动输入都是禁用的，这里是唯一的恢复点：
        // 无论是 End 自然结束还是被其他状态提前打断，都必须走到这里，否则输入会被永久禁用。
        stateMachine.Player.Input.PlayerActions.Movement.Enable();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        if (!IsMovingHorizontally())
        {
            return;
        }

        ResetVelocity();
    }

    // TODO：HardLand 的 Animancer 迁移验证通过后删除旧动画事件回调。
    // public override void OnAnimationTransitionEvent()
    // {
    //     base.OnAnimationTransitionEvent();
    //
    //     stateMachine.ChangeState(stateMachine.IdlingState);
    // }

    // protected override void OnMove()
    // {
    //     if (stateMachine.ReusableData.ShouldWalk)
    //     {
    //         return;
    //     }

    //     stateMachine.ChangeState(stateMachine.RunningState);
    // }

    protected override void OnJumpStarted(InputAction.CallbackContext context)
    {
    }

    private void OnHardLandingAnimationEnded()
    {
        stateMachine.ChangeState(stateMachine.IdlingState);
    }
}
