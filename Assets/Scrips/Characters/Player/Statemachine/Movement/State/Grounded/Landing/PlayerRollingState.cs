using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerRollingState : PlayerLandingState
{
    private readonly PlayerRollData rollData;

    public PlayerRollingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
        rollData = GroundedData.RollData;
    }

    public override void Enter()
    {
        base.Enter();

        stateMachine.ReusableData.MovementSpeedModifier = rollData.SpeedModifier;
        // TODO：Roll 的 Animancer 迁移验证通过后删除旧 Animator 播放代码。
        // stateMachine.Player.Animator.CrossFade(
        //     AnimationData.RollingAnimationHash,
        //     AnimationData.NormalizedTransitionDuration);
        PlayLandingAnimation(rollData.Animation, OnRollingAnimationEnded);
        stateMachine.ReusableData.ShouldSprint = false;
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        if (stateMachine.ReusableData.MovementInput != Vector2.zero)
        {
            return;
        }

        RotateTowardTargetRotation();
    }

    // TODO：Roll 的 Animancer 迁移验证通过后删除旧动画事件回调。
    // public override void OnAnimationTransitionEvent()
    // {
    //     base.OnAnimationTransitionEvent();
    //
    //     if (stateMachine.ReusableData.MovementInput == Vector2.zero)
    //     {
    //         stateMachine.ChangeState(stateMachine.MediumStoppingState);
    //         return;
    //     }
    //
    //     OnMove();
    // }

    protected override void OnJumpStarted(InputAction.CallbackContext context)
    {
    }

    private void OnRollingAnimationEnded()
    {
        if (stateMachine.ReusableData.MovementInput == Vector2.zero)
        {
            stateMachine.ChangeState(stateMachine.MediumStoppingState);
            return;
        }

        OnMove();
    }
}
