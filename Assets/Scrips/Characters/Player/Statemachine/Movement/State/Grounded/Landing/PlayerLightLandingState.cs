using UnityEngine;

public class PlayerLightLandingState : PlayerLandingState
{
    public PlayerLightLandingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        stateMachine.ReusableData.MovementSpeedModifier = 0f;
        stateMachine.ReusableData.CurrentJumpForce = AirborneData.JumpData.StationaryForce;
        // TODO：LightLand 的 Animancer 迁移验证通过后删除旧 Animator 播放代码。
        // stateMachine.Player.Animator.CrossFade(
        //     AnimationData.LightLandingAnimationHash,
        //     AnimationData.NormalizedTransitionDuration);
        PlayLandingAnimation(GroundedData.LandingData.LightAnimation, OnLightLandingAnimationEnded);

        ResetVelocity();
    }

    public override void Update()
    {
        base.Update();

        if (stateMachine.ReusableData.MovementInput == Vector2.zero)
        {
            return;
        }

        OnMove();
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

    // TODO：LightLand 的 Animancer 迁移验证通过后删除旧动画事件回调。
    // public override void OnAnimationTransitionEvent()
    // {
    //     stateMachine.ChangeState(stateMachine.IdlingState);
    // }

    private void OnLightLandingAnimationEnded()
    {
        stateMachine.ChangeState(stateMachine.IdlingState);
    }
}
